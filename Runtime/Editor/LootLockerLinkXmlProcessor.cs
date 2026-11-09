#if UNITY_EDITOR
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
            if (string.IsNullOrEmpty(linkXmlPath))
            {
                Debug.LogWarning(
                    $"[{LootLockerConfig.PackageShortName}] Could not locate link.xml. Managed code stripping may remove " +
                    "types the SDK deserializes through reflection, causing responses to come back with default values.");
                return null;
            }

            return linkXmlPath;
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
