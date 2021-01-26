# unity-sdk
Source of the Unity SDK for LootLocker
## Installation

The following steps outline how to install the LootLocker SDK into your Unity project.

1. Download the latest Unity Package Release from the [Github](https://github.com/LootLocker/unity-sdk) repository or from the [Unity Asset Store](https://assetstore.unity.com/packages/slug/180099)**.**
2. Double-click on the package or drag the package into your Unity scene to import it.
3. LootLocker requires “Editor Coroutine” from the package manager to function properly. If you currently have this in your project you can click exit in the window that displays after importing.
   Note: If you imported from github release and not Unity. You might need to open the package manager and install "Editor Coroutine". Otherwise project might not compile.
4. If you are importing the package into an already existing project, please untick the project settings so your settings are not overwritten.
5. If you already have the package “JsonDotNet” or “NewtonJson” from the Unity Asset Store or Github, please untick the “Dependencies” folder
6. Click import to complete the SDK installation

**Note**: If you do not intend to use the Admin SDK, Unity Samples or Sample App, you can safely delete the folders within the LootLocker folder to reduce the size of the project.
