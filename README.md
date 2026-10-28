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

### Multiple configurations

You can keep several named config files in one project and switch between them without
recompiling. This is how you keep, for example, a `dev` config with verbose logging and a `prod`
config with logging off, or one config per platform.

**Naming.** Put the files next to the config asset, in
`Assets/LootLockerSDK/Resources/Config/`, and name them `LootLockerConfig-<id>.bytes`. The `<id>` may
contain letters, digits, `.`, `_` and `-`, and must start with a letter or digit. The unsuffixed
`LootLockerConfig.bytes` keeps working exactly as before and is shown as `Default`.

**Selecting.** Open `Edit > Project Settings > LootLocker SDK`. When more than one config file is
present a `Config File` dropdown appears. Files that cannot be parsed, or that have no `api_key`,
are listed greyed out with the reason, so a broken file is visible but cannot be selected. With a
single config file there is nothing to choose and no dropdown is shown.

**Local overrides.** A shared config is the right place for team-wide values, but not for a one-off
change. Click the closed padlock next to a setting to write a gitignored
`Assets/LootLockerSDK/LocalOverrides/LootLockerConfig.local.bytes` that is layered on top of the
selected config. The file is sparse: only the fields it contains are applied, so overriding
`log_level` leaves every other setting shared. Overridden settings are marked `(local override)` in
Project Settings, and the open padlock resets them. Switching config file also clears the file,
because an override written against one config does not apply to another.

The override file is **editor only**, and deliberately outside `Resources/`. Anything under
`Resources/` is packed into every build made on that machine, so an override stored there would ship
a local `api_key` or `log_in_builds` value to players even if the SDK never read it. It is a
developer convenience, not a build input.

**CI.** Select a config without touching the editor:

```
Unity.exe -batchmode -quit -lootlockerconfig prod-ps5
Unity.exe -batchmode -quit -lootlockerconfig none
```

An invalid identifier is logged as an error and treated as `none` rather than guessed at.

**Precedence**, highest first:

1. An explicit `LootLockerSDKManager.Init(...)` call. Process lifetime only; never written to disk.
2. `-lootlockerconfig` on the command line.
3. The local override file (editor only).
4. The selected config file.
5. The legacy `LootLockerConfig.bytes`.
6. The values stored in the config asset.

`-lootlockerconfig none` disables file config entirely, including the local override.

**Only the selected config ships.** Files under `Resources/` are packed into the build and are
trivially readable from a shipped game, so "the SDK only reads one of them" is not key hygiene — the
others would still be in the build data. Before a build, every config file except the one in effect is
renamed with a trailing `~`, which Unity ignores at import, and restored afterwards. The player reads
the surviving config through a generated `LootLockerConfig.build.bytes` pointer.

So a `dev` key on disk cannot end up in a `prod` build. What gets stripped is decided by the same
precedence as above, so `-lootlockerconfig prod` on a CI build strips everything else.

Two things to know:

- During a build, `git status` briefly shows the other configs as renamed. Restore is automatic.
- If the editor is killed mid-build, the files stay hidden until the next editor start, which
  restores them automatically. Nothing is lost — the files are renamed, not deleted.

**Two limitations worth knowing:**

1. The selection is a single committed value, so it is shared by everyone on the project. Per-build
   target selection is not implemented yet.
3. **API keys in config files are not secrets.** They are readable by anyone with the build. This
   feature improves key hygiene — keeping the right key on the right target — not secrecy.

## Updating
If you have installed the SDK from Open UPM then all you have to do in Package Manager is press the Update button when that tells you there's a new version.

For other install methods and more information, head over to our [official documentation](https://docs.lootlocker.com/the-basics/unity-quick-start/updating-sdk).

## Support
If you have any issues or just wanna chat you can reach us on our [Discord Server](https://discord.lootlocker.io/)

## Development

### Testing the SDK
Status: [![Test SDK with Unity](https://github.com/LootLocker/unity-sdk/actions/workflows/run-tests-and-package.yml/badge.svg?branch=main)](https://github.com/LootLocker/unity-sdk/actions/workflows/run-tests-and-package.yml?query=branch%3Amain)

To set up the tests, you must add our package to the manifest.json `testables` underneath `dependencies`. Your manifest.json should look something like this:

```json
{
  "dependencies": {
    "com.lootlocker.lootlockersdk": "https://github.com/lootlocker/unity-sdk.git",
    many more dependencies...
  },
  "testables": ["com.lootlocker.lootlockersdk"]
}
```

Once you have done this, you can go into Unity and go to `Window` -> `General` -> `Test Runner`. This will open a new window which should already include all the tests available, Then all you have to do is press `Run All` and the tests will begin.

If you are running the tests towards the LootLocker production environment you will need to provide a username and password to an existing user either by setting the values directly in LootLockerTestConfigurationUser::GetProductionUser or via command line arguments -adminemail and -adminpassword. Bear in mind that your tests will be rate limited when running towards production (and in general this is a bad idea).
