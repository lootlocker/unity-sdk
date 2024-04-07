<h1 align="center">LootLocker Unity SDK</h1>

<h1 align="center">
  <a href="https://www.lootlocker.com/"><img src="https://s3.eu-west-1.amazonaws.com/cdn.lootlocker.io/public/lootLocker_wide_dark_whiteBG.png" alt="LootLocker"></a>
</h1>

<p align="center">
  <a href="#about">About</a> •
  <a href="#installation">Installation</a> •
  <a href="#configuration">Configuration</a> •
  <a href="#updating">Updating</a> •
  <a href="#support">Support</a> •
  <a href="#development">Development</a>
</p>

---

## About
LootLocker is a game backend-as-a-service that unlocks cross-platform systems so you can build, ship, and run your best games.

Full documentation is available here https://docs.lootlocker.com/

## Installation

### From Open UPM [![openupm](https://img.shields.io/npm/v/com.lootlocker.lootlockersdk?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.lootlocker.lootlockersdk/)
The **preferred way** of installing the LootLocker SDK is through Open UPM. This way you will receive future updates in a streamlined way. Please refer to OpenUPM for their [simple install instructions](https://openupm.com/packages/com.lootlocker.lootlockersdk/#modal-manualinstallation) or our [official documentation](https://docs.lootlocker.com/the-basics/unity-quick-start/install-the-sdk).

### From Git
Before you start you need to have [git](https://git-scm.com/) installed on your computer.

- In the Unity Editor go to `Window > Package Manager`
- Click the + at the top left of the Package Manager window
- Select `Add package from git URL...`
- Paste the following URL `https://github.com/LootLocker/unity-sdk.git` and click `Add`

### Other install methods
For other methods, please refer to our [official documentation](https://docs.lootlocker.com/the-basics/unity-quick-start/install-the-sdk).

## Configuration
For a full walkthrough of how to configure the SDK, please see our [Guide to Configuring the Unity SDK](https://docs.lootlocker.com/the-basics/unity-quick-start/configure-the-sdk). But here's a short explanation:

### Using the LootLocker Unity Extension (since Unity v2021)
- Open the LootLocker Extension in Unity (Windows > LootLocker)
- Sign in with your LootLocker account
- Select your game, environment, and game key.

### Configuring the SDK manually
- Log on to the [LootLocker management console](https://console.lootlocker.com/login) and find your Game Settings.
- Find your Game Key in the [API section of the settings](https://console.lootlocker.com/settings/api-keys)
- Open `Edit > Project Settings` in the Unity Editor and find the `LootLocker SDK` tab.
- Input Api Key using the Game Key from the LootLocker console page

## Updating
If you have installed the SDK from Open UPM then all you have to do in Package Manager is press the Update button when that tells you there's a new version.

For other install methods and more information, head over to our [official documentation](https://docs.lootlocker.com/the-basics/unity-quick-start/updating-sdk).

## Support
If you have any issues or just wanna chat you can reach us on our [Discord Server](https://discord.lootlocker.io/)

## Development

### Testing the SDK
Status: [![Test SDK with Unity](https://github.com/LootLocker/unity-sdk/actions/workflows/run-tests-and-package.yml/badge.svg?branch=main)](https://github.com/LootLocker/unity-sdk/actions/workflows/run-tests-and-package.yml?query=branch%3Amain)

There is a Test Suite for the SDK, but it is in an [external repo](https://github.com/LootLocker/unity-sdk-tests) to keep the size of this one down. It is run automatically on any pull requests towards or updates to main. You can also run it locally, just follow the steps in the test repo.
