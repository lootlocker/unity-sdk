@mainpage LootLocker Unity SDK — API Reference

@image html lootlocker-logo.png width=280px

[LootLocker](https://lootlocker.com) is a **game backend-as-a-service** that ships fully-managed, cross-platform
systems so your team can focus on making the game instead of maintaining infrastructure.
This reference covers every public method and type in the SDK.

---

@section mainpage_quickstart Quick Start

**1. Install** (via Open UPM — recommended)

```text
openupm add com.lootlocker.lootlockersdk
```

Or from Git: `Window → Package Manager → Add package from git URL →`  
`https://github.com/LootLocker/unity-sdk.git`

**2. Configure**

Open **Edit → Project Settings → LootLocker SDK** and paste your **Game API Key**
from the [LootLocker console](https://console.lootlocker.com/settings/api-keys).

**3. Start a session**

```csharp
LootLockerSDKManager.StartGuestSession(response =>
{
    if (!response.success)
    {
        Debug.LogError("Session failed: " + response.errorData?.message);
        return;
    }
    Debug.Log("Session started for player: " + response.player_ulid);
});
```

All SDK calls are **static methods on `LootLockerSDKManager`** and return results
asynchronously via a callback delegate whose first argument derives from
@ref LootLocker.LootLockerResponse "LootLockerResponse".
Check `response.success` before accessing the payload fields.

---

@section mainpage_navigation Navigating This Reference

The **Topics** tab (left sidebar or top nav) is the recommended entry point.
Methods are grouped by feature — start there to find everything related to a
particular system without needing to know which class or file it lives in.

| Topic | What it covers |
|-------|---------------|
| @ref Init "Initialization" | Set up and configure the SDK before starting a session. |
| @ref SDKCustomization "SDK Customization" | Override SDK internals for testing or advanced integration scenarios. |
| @ref MultiUserManagement "Multi-User Management" | Manage concurrent player sessions on a single device. |
| @ref Authentication "Authentication" | Start and end player sessions across all supported identity providers. |
| @ref WhiteLabel "White Label Login" | Email/password authentication using LootLocker's built-in identity system. |
| @ref RemoteSessions "Remote Sessions" | QR-code / deep-link secondary-device login flow. |
| @ref ConnectedAccounts "Connected Accounts" | Link or unlink third-party identity providers to an existing session. |
| @ref Presence "Presence" | Real-time online/offline visibility and status for players. |
| @ref Friends "Friends" | Friend list management — list, add, remove, and respond to friend requests. |
| @ref Followers "Followers" | Follow and unfollow players; query follower/following counts. |
| @ref Player "Player" | Core player profile operations. |
| @ref PlayerFiles "Player Files" | Per-player cloud-stored binary or JSON files. |
| @ref PlayerProgressions "Player Progressions" | Track player XP and level advancement through progression tiers. |
| @ref PlayerStorage "Player Storage" | Simple key-value store scoped to the player. |
| @ref Hero "Hero" | Hero-based character selection and loadout management. |
| @ref CharacterProgressions "Character Progressions" | Progression tracking scoped to an individual character. |
| @ref Currency "Currency" | Define and query the in-game currency types configured in the LootLocker console. |
| @ref Balances "Balances" | Read and update a player's currency balances. |
| @ref Catalog "Catalog" | Browse item listings in the LootLocker storefront catalog. |
| @ref Purchasing "Purchasing" | Initiate and validate item purchases (IAP and virtual currency). |
| @ref Entitlements "Entitlements" | Query what a player has been granted — including IAP entitlements. |
| @ref DropTables "Drop Tables" | Evaluate drop-table loot rolls and claim results for the player. |
| @ref Assets "Assets" | Browse the asset catalogue defined in the LootLocker console. |
| @ref AssetInstance "Asset Instances" | Operations on asset instances owned by the current player. |
| @ref AssetInstanceProgressions "Asset Instance Progressions" | Progression tracking scoped to a specific asset instance. |
| @ref UserGeneratedContent "User-Generated Content (UGC)" | Create, edit, and publish player-authored assets. |
| @ref Collectables "Collectables" | Collectable items — groups, items, and player collection state. |
| @ref Progressions "Progressions" | Shared progression infrastructure (tiers, rewards, reset). |
| @ref Leaderboard "Leaderboards" | Submit scores and read ranked results. |
| @ref Missions "Missions" | Time-limited challenges with completion tracking. |
| @ref Maps "Maps" | Retrieve map configurations and node data. |
| @ref Triggers "Triggers" | Fire named trigger events that award items or currency server-side. |
| @ref Metadata "Metadata" | Attach and retrieve arbitrary key-value metadata on any entity. |
| @ref Messages "Messages" | Read player inbox messages from the LootLocker console. |
| @ref Notifications "Notifications" | Receive and dismiss push/persistent notifications for the player. |
| @ref Broadcasts "Broadcasts" | Read game-wide broadcast messages configured in the console. |
| @ref Reports "Reports" | Submit player-generated moderation reports to LootLocker. |
| @ref Feedback "Feedback" | Submit structured feedback (feature requests, bug reports, general). |
| @ref Misc "Miscellaneous Utilities" | Helpers that don't fit neatly into a feature group. |
| @ref EventSystem "Event System" | Subscribe to SDK lifecycle events (session start/end, token refresh). |
---

@section mainpage_response Response Conventions

Every callback receives a response object deriving from
@ref LootLocker.LootLockerResponse "LootLockerResponse":

| Field | Type | Meaning |
|-------|------|---------|
| `success` | `bool` | `true` if the call succeeded |
| `statusCode` | `int` | HTTP status (200, 401, 422, …) |
| `errorData` | @ref LootLocker.LootLockerErrorData "LootLockerErrorData" | Populated on failure |
| `requestContext` | @ref LootLocker.LootLockerRequestContext "LootLockerRequestContext" | Contains contextual information about the request such as who it was made for and the ID of the request |
| `text` | `string` | Raw JSON body (useful for debugging) |

Always check `response.success` first. Never access payload fields if
`success` is `false`.

---

@section mainpage_multi_player Multi-Player Device Support

The SDK can maintain **independent sessions for up to N players** on the same
device (local co-op or couch multiplayer). Each call accepts an optional
`forPlayerWithUlid` overload parameter. Use @ref MultiUserManagement to
enumerate cached players and switch the default player.

---

@section mainpage_links Useful Links

- [Full developer documentation](https://docs.lootlocker.com/)
- [LootLocker console](https://console.lootlocker.com/)
- [GitHub repository](https://github.com/lootlocker/unity-sdk)
- [Discord community](https://discord.lootlocker.io/)
- [REST API reference](https://ref.lootlocker.com/)
