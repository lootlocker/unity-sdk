# Unity SDK architecture & repo structure

- Repo layout: **UPM package at repository root** (customers receive an `Assets/` wrapper via CI packaging).
- Public entrypoint: `Runtime/Game/LootLockerSDKManager.cs`

For agent operating rules, see: `.github/instructions/guardrails.md`.

## Repository map (root)
- `Runtime/` — Shippable SDK code (runtime + editor-guarded pieces). Main entrypoint is in `Runtime/Game/`.
- `Tests/` — Unity Test Runner tests and test utilities.
- `Samples~/` — UPM samples (example scenes/scripts).
- `Prefabs/` — Exported `.unitypackage` artifacts (not runtime code).
- `.github/` — Internal repo automation + agent docs (excluded from customer packaging).
- `docs/` — Internal developer documentation (excluded from customer packaging).
- `package.json` — UPM package manifest (do not bump version unless explicitly tasked).

## Where the code lives

### Public API surface
- `Runtime/Game/LootLockerSDKManager.cs`
  - The user-facing SDK class. Most SDK features are exposed as static methods here.
  - The class is `partial` and very large; changes here are inherently API-surface changes.

### Runtime “core” services / transport
- `Runtime/Client/LootLockerHTTPClient.cs` — HTTP transport built on `UnityEngine.Networking.UnityWebRequest`.
- `Runtime/Client/LootLockerHttpRequestData.cs` — Request data model/formatting used by the HTTP client.
- `Runtime/Client/LootLockerHTTPExecutionQueueItem.cs` — Execution/queue bookkeeping for HTTP requests.
- `Runtime/Client/LootLockerRateLimiter.cs` — Client-side rate limiting.
- `Runtime/Client/LootLockerEndPoints.cs` + `Runtime/Client/EndPointClass.cs` — Endpoint path/method definitions.

### Requests + response models
- `Runtime/Game/Requests/` — Feature-focused request code.
  - Convention observed in this repo: many request files also define their **response DTOs** in the same file (e.g., classes deriving from `LootLockerResponse`).

Shared response base + errors:
- `Runtime/Client/LootLockerResponse.cs` — Base response type + deserialization helper.
- `Runtime/Client/LootLockerErrorData.cs` — Error payload shape.
- `Runtime/Client/LootLockerRequestContext.cs` — Context attached to responses.

### Serialization
- `Runtime/Client/LootLockerJson.cs` — Serialization wrapper.
  - Uses Newtonsoft JSON when `LOOTLOCKER_USE_NEWTONSOFTJSON` is defined.
  - Otherwise uses `Runtime/Libraries/ZeroDepJson/`.
- `Runtime/Libraries/ZeroDepJson/` — Built-in JSON implementation (no external dependency).

### Session / player state (auth tokens, persistence)
- `Runtime/Client/LootLockerPlayerData.cs` — In-memory player/session token fields.
- `Runtime/Client/LootLockerStateData.cs` — Service that persists player state and reacts to session lifecycle events.
- `Runtime/Client/LootLockerStateWriter.cs` — `ILootLockerStateWriter` abstraction; default uses `PlayerPrefs` unless disabled.

### Lifecycle + events
- `Runtime/Client/ILootLockerService.cs` — Service interface.
- `Runtime/Client/LootLockerLifecycleManager.cs` — Central manager that instantiates services and coordinates Unity lifecycle.
- `Runtime/Client/LootLockerEventSystem.cs` — Eventing used by services (e.g., session started/refreshed/ended).

### Configuration
- `Runtime/Game/Resources/LootLockerConfig.cs` — ScriptableObject settings.
  - Uses `Resources.Load` at runtime.
  - Editor-only asset creation/editor integrations are guarded by `#if UNITY_EDITOR`.

### Logging / utilities
- `Runtime/Game/LootLockerLogger.cs` — SDK logging.
- `Runtime/Game/LootLockerObfuscator.cs` — Sensitive log obfuscation.
- `Runtime/Game/Utilities/` — General helpers used by the SDK.

### Editor tooling (repo’s “Editor” area)
- `Runtime/Editor/` — Editor-only tooling (LootLocker extension UI, log viewer, editor data, etc.).
  - This is the effective “Editor/” boundary in this repo.

### Tests
- `Tests/LootLockerTests/PlayMode/` — PlayMode tests + `PlayModeTests.asmdef`.
- `Tests/LootLockerTestUtils/` — Shared test configuration helpers + `LootLockerTestUtils.asmdef`.

### Samples
- `Samples~/LootLockerExamples/` — UPM samples + `LootLockerExamples.asmdef`.

## Related instructions (rules live elsewhere)

This document is intentionally focused on **structure and navigation** (where things live).

For behavioral rules, conventions, and safety constraints, follow:
- `.github/instructions/guardrails.md` (change discipline + operational rules)
- `.github/instructions/style-guide.md` (repo-wide coding conventions)
- Path-specific instruction files under `.github/instructions/**.instructions.md` (highest priority for matching paths)

## Where do I implement X?

| Change you want to make | Put it here (real paths in this repo) |
|---|---|
| Add/modify a customer-facing SDK method or its documentation | `Runtime/Game/LootLockerSDKManager.cs` |
| Add a new feature request (API call) | `Runtime/Game/Requests/<Feature>*.cs` contains the DTO structs and their documentation and customer exposure is added through `Runtime/Game/LootLockerSDKManager.cs` and the necessary endpoint constants are added in `Runtime/Client/LootLockerEndPoints.cs` |
| Add/adjust response DTO fields for a request | Usually in the same `Runtime/Game/Requests/<Feature>*.cs` file (classes deriving `LootLockerResponse`) |
| Add/adjust endpoint URL/method constants | `Runtime/Client/LootLockerEndPoints.cs` and/or `Runtime/Client/EndPointClass.cs` |
| Change HTTP behavior (retries, headers, request creation) | `Runtime/Client/LootLockerHTTPClient.cs` + `Runtime/Client/LootLockerHttpRequestData.cs` |
| Change serialization rules | `Runtime/Client/LootLockerJson.cs` (and `Runtime/Libraries/ZeroDepJson/` if not using Newtonsoft) |
| Change session/persistence behavior | `Runtime/Client/LootLockerStateData.cs`, `Runtime/Client/LootLockerStateWriter.cs`, `Runtime/Client/LootLockerPlayerData.cs` |
| Add/adjust config settings | `Runtime/Game/Resources/LootLockerConfig.cs` |
| Add editor window/extension UI | `Runtime/Editor/` |
| Add/adjust tests | `Tests/LootLockerTests/PlayMode/` (tests) and `Tests/LootLockerTestUtils/` (shared config) |
| Add/adjust samples | `Samples~/LootLockerExamples/` |

## Links
- Coding Agent guardrails: `.github/instructions/guardrails.md`

## Future expansion (placeholders)
- Build & test (to be filled by later tasks)
- CI packaging notes (to be filled by later tasks)
