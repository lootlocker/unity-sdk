# unity-sdk
Full documentation available here https://docs.lootlocker.io/getting-started/unity-tutorials

## Install from Unity Package Manager (Recommended)

### Using GIT

Before you start, make sure you have git installed on your computer.
Open the Unity editor and navigate to the Package Manager by clicking on Window and then Package Manager.

Click on the + at the top of the Package Manager window and select Add package from git URL.

Enter the URL below into the text box and click Add. 
https://github.com/LootLocker/unity-sdk.git

The SDK is now installed in your project, and you are ready to configure the SDK and make your first API calls.

### Using the Entire Repo

If you do not have git installed on your computer. You can install the SDK by downloading the entire repository and pasting in the packages folder of your project.
You can download the entire repo by clicking Code and Download Zip

The SDK is now installed in your project, and you are ready to configure the SDK and make your first API calls.

## Configure the SDK

It’s time to connect the SDK in your project to the LootLocker Management Console. Depending on the game engine, there might be different ways to achieve this, so make sure you read everything before getting started.
Now that you have installed the SDK into your Unity project, you will want to connect the Unity project to your game in the LootLocker Management Console. The following steps walk you through configuring the LootLocker Unity SDK to work with a game that has already been created in the LootLocker Management Console. If you haven’t created a game or account yet, please visit my.lootlocker.io or follow this guide.
There are two methods for setting up the SDK: setting it up manually in the project settings in the editor or using the Admin SDK. This tutorial will explain both ways.

### Configure via Project Settings
Login to the LootLocker Management Console by visiting my.lootlocker.io.

Click on Game Settings and copy your API Key.

Return to the Unity Editor, Click on Edit, and then Project Settings.

Click on LootLocker SDK in the list to the left and fill in your API Key copied from the LootLocker dashboard.

Fill in all information based on data from the LootLocker Settings Menu.

API Key is found in Game Settings in the LootLocker Management Console.

Game Version refers to the current version of the game in the format 1.2.3.4 (the 3 and 4 being optional but recommended).

Platform is the name of the platform the game will be built for (e.g Steam, PSN, Android, iOS).

Environment lets you test your unpublished changes in the LootLocker Management Console by selecting Development instead of Live.

Current Debug Level allows you to configure the debug level of the SDK. This can be set to Errors Only, Normal Only, or Off.

Allow Token Refresh can be selected so that the SDK automatically attempts to refresh the session token if it expires. Otherwise the session token needs to be renewed manually.

You have now configured the LootLocker SDK. In the next section you will learn how to make your first API calls.
