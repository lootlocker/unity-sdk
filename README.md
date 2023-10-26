<h1 align="center">LootLocker Unity SDK</h1>

<h1 align="center">
  <a href="https://www.lootlocker.io/"><img src="https://s3.eu-west-1.amazonaws.com/cdn.lootlocker.io/public/lootLocker_wide_dark_whiteBG.png" alt="LootLocker"></a>
</h1>

<p align="center">
  <a href="#about">About</a> •
  <a href="#Installation">Installation</a> •
  <a href="#configuration">Configuration</a> •
  <a href="#updating">Updating</a> •
  <a href="#support">Support</a> •
  <a href="#Development">Development</a>
</p>

---

## About

LootLocker is a game backend-as-a-service that unlocks cross-platform systems so you can build, ship, and run your best games.

Full documentation is available here https://docs.lootlocker.io/getting-started/unity-tutorials

## Installation

### Using Git

Before you start, Git should be installed on your computer, Mac users should have this automatically, for windows you can use for example [Git for Windows](https://gitforwindows.org/). Quick note: You might need to restart Unity and/or your computer before Unity recognizes you’ve installed Git.

- In the Unity Editor go to `Window > Package Manager`
- Click the + at the top left of the Package Manager window
- Select `Add package from git URL...`
- Paste the following URL `https://github.com/LootLocker/unity-sdk.git` and click `Add`

### Using the Entire Repo

- Download the code from here by choosing `Code` and then `Download Zip`
- Unzip the files and place them in a folder in the Packages folder of your Unity Project

## Configuration

For a full walkthrough of how to configure the SDK, please see our [Guide to Configuring the Unity SDK](https://docs.lootlocker.io/getting-started/unity-tutorials/getting-started-with-unity/configure-the-sdk)

### For a short version

- Log on to the [LootLocker management console](https://my.lootlocker.io/login) and find your Game Settings.
- Find your Game Key in the API section of the settings
- Open `Edit > Project Settings` in the Unity Editor and find the `LootLocker SDK` tab.
- Input Api Key using the Game Key from the LootLocker console page

## Updating

If you installed the SDK using the Git URL you can simply open the package manager and re-paste the Git URL.
This should force a download of the latest code.

## Support

If you have any issues or just wanna chat you can reach us on our [Discord Server](https://discord.lootlocker.io/)

## Development

### Testing the SDK

Status: [![Test SDK with Unity](https://github.com/LootLocker/unity-sdk/actions/workflows/main.yml/badge.svg?event=push)](https://github.com/LootLocker/unity-sdk/actions/workflows/main.yml)

There is a Test Suite for the SDK, but it is in an [external repo](https://github.com/LootLocker/unity-sdk-tests) to keep the size of this one down. It is run automatically on any pull requests towards or updates to main. You can also run it locally, just follow the steps in the test repo.
