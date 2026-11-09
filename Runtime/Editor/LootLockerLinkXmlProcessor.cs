// IUnityLinkerProcessor and UnityLinkerBuildPipelineData were added in Unity 2019.3. The package
// declares 2019.2 as its minimum, so the whole processor has to be gated or editor compilation
// fails on 2019.2 before the linker fix can run.
#if UNITY_EDITOR && UNITY_2019_3_OR_NEWER
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.UnityLinker;
using UnityEngine;

namespace LootLocker.Admin
{
    /// <summary>
    /// Registers the SDK's link.xml with the Unity linker.
    /// </summary>
    /// <remarks>
    /// The Unity linker only reads link.xml files that live under the project's Assets folder.
    /// A link.xml shipped inside a UPM package is imported as an asset but is never applied, so
    /// the preservations it declares are silently ignored and managed code stripping removes the
    /// members the SDK reaches through reflection. This processor hands the linker the absolute
    /// path to the package's link.xml so the same file is used regardless of install location.
    /// </remarks>
    internal class LootLockerLinkXmlProcessor : IUnityLinkerProcessor
    {
        // Run late so other processors have already contributed their preservations.
        public int callbackOrder => 1000;

        public string GenerateAdditionalLinkXmlFile(BuildReport report, UnityLinkerBuildPipelineData data)
        {
            string linkXmlPath = FindLinkXml();
            if (!string.IsNullOrEmpty(linkXmlPath))
            {
                return linkXmlPath;
            }

            // No package to resolve means a .unitypackage install, where the SDK lives under
            // Assets/ and the linker finds link.xml on its own. Only warn when the package was
            // found but the file is missing, which means the install is broken.
            if (IsInstalledAsPackage())
            {
                Debug.LogWarning(
                    $"[{LootLockerConfig.PackageShortName}] Could not locate link.xml in the installed package. " +
                    "Managed code stripping may remove types the SDK deserializes through reflection, causing " +
                    "responses to come back with default values.");
            }

            return null;
        }

#if !UNITY_2021_1_OR_NEWER
        // Unity 2020.3 and earlier declare these on the interface; 2021.1 removed them.
        public void OnBeforeRun(BuildReport report, UnityLinkerBuildPipelineData data)
        {
        }

        public void OnAfterRun(BuildReport report, UnityLinkerBuildPipelineData data)
        {
        }
#endif

        private static string FindLinkXml()
        {
            // UPM install (registry, git, or local file reference): resolve the package root.
            UnityEditor.PackageManager.PackageInfo packageInfo =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(LootLockerConfig).Assembly);
            if (packageInfo != null && !string.IsNullOrEmpty(packageInfo.resolvedPath))
            {
                string fromPackage = Path.Combine(packageInfo.resolvedPath, "link.xml");
                if (File.Exists(fromPackage))
                {
                    return fromPackage;
                }
            }

            // .unitypackage install: the SDK is copied into Assets/, so there is no package to
            // resolve. The linker already picks up link.xml files under Assets/ on its own, so
            // returning null here is correct and must not warn.
            return null;
        }

        private static bool IsInstalledAsPackage()
        {
            UnityEditor.PackageManager.PackageInfo packageInfo =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(LootLockerConfig).Assembly);
            return packageInfo != null && !string.IsNullOrEmpty(packageInfo.resolvedPath);
        }
    }
}
#elif UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LootLocker.Admin
{
    /// <summary>
    /// Unity 2019.2 fallback for registering the SDK's link.xml with the Unity linker.
    /// </summary>
    /// <remarks>
    /// IUnityLinkerProcessor does not exist before 2019.3, so the processor above cannot be used.
    /// The linker only reads link.xml files under the project's Assets folder, so this copies the
    /// package's link.xml into Assets/ before the build and removes it afterwards. The copy is
    /// transient: it exists only for the duration of the build and is never committed.
    /// </remarks>
    internal class LootLockerLinkXmlProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private const string StagedAssetPath = "Assets/LootLockerLink.xml";

        public int callbackOrder => 1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            string source = FindLinkXml();
            if (string.IsNullOrEmpty(source))
            {
                return;
            }

            try
            {
                File.Copy(source, StagedAssetPath, true);
                AssetDatabase.ImportAsset(StagedAssetPath, ImportAssetOptions.ForceUpdate);
            }
            catch (IOException e)
            {
                Debug.LogWarning(
                    $"[{LootLockerConfig.PackageShortName}] Could not stage link.xml for the build. " +
                    $"Managed code stripping may remove types the SDK deserializes through reflection. {e.Message}");
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            if (File.Exists(StagedAssetPath))
            {
                AssetDatabase.DeleteAsset(StagedAssetPath);
            }
        }

        private static string FindLinkXml()
        {
            UnityEditor.PackageManager.PackageInfo packageInfo =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(LootLockerConfig).Assembly);
            if (packageInfo == null || string.IsNullOrEmpty(packageInfo.resolvedPath))
            {
                return null;
            }

            string candidate = Path.Combine(packageInfo.resolvedPath, "link.xml");
            return File.Exists(candidate) ? candidate : null;
        }
    }
}
#endif
