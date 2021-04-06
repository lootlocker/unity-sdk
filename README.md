# unity-sdk
Full documentation available here https://docs.lootlocker.io/unity-sdk/

## Installation

The following steps outline how to install the LootLocker SDK into your Unity project.
1. Install from Unity Package Manager (Recommended)
1. Make sure you have git installed on your computer.
2. Open your Unity Editor of choice.
3. Navigate to the package manager by clicking on "Window" and then "Package Manager".
4. Click on the Plus button on top of the package manager window and then select "Add package from git URL"
5. Enter the Url "https://github.com/LootLocker/unity-sdk.git" into the text box and click on "Add". 

## Set Up

Now that you have installed the SDK into your Unity project, you will want to connect the Unity project to your game in the LootLocker Management Console.

The following steps walks you through configuring the LootLocker Unity SDK to work with a game that has been created in the LootLocker Management Console at [my.lootlocker.io](https://my.lootlocker.io)

There are two methods for setting up the SDK: using the Admin SDK or by manually editing the config file.

### Using the Admin SDK

The Admin SDK is provided as a simple tool to login to your LootLocker account and configure your Unity project to a game you have set up in the LootLocker Management Console.

1. Navigate to the menu bar and select Window/Open LootLocker Admin Panel.
2. Enter the email and password for your LootLocker account. If you do not have a LootLocker account you can create one by visiting [lootlocker.io/sign-up](https://www.lootlocker.io/sign-up).
3. In the LootLocker Admin Panel, select the game you wish to use.
4. In the Project view, navigate to LootLocker/Game/Resources/Config.
5. Click on LootLockerConfig.
6. Modify your the following:
   1. Game Version e.g 1.0.0.0
   2. Platform e.g Android, Steam
   3. Environment e.g Development
You are now ready to use LootLocker and connect to your game's APIs.

### Modifying the Config

Alternatively, you can configure your Unity project manually by excluding the Admin SDK from the installation.

1. Login to the LootLocker Management Console by visiting [my.lootlocker.io](https://my.lootlocker.io)
2. Click on Settings
3. Copy your Game Name and API Key
4. Return to Unity
5. In the Project view, navigate to LootLocker/Game/Resources/Config
6. Fill in all information based on data from the LootLocker Settings Menu
   1. Game Version e.g 1.0.0.0
   2. Platform e.g Android, Steam
   3. Environment e.g Development

You are now ready to use LootLocker and connect to your game's APIs.
