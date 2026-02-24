# LootLocker Unity SDK — Coding conventions & style guide

Scope: This document describes conventions *observed in this repository* and rules we want contributors/agents to follow when changing or adding code.

Note: This is the repo-wide baseline. More specific rules are defined in path-specific instruction files under `.github/instructions/**.instructions.md` and should be treated as higher priority for matching files.

This repo ships as a Unity UPM package.

## 0) Golden rules

- Keep changes scoped: large formatting-only work, refactors, renames, and moves should only be done if explicitly requested and then always in their own PRs.
- Search-first: before adding a helper/DTO/utility, search under `Runtime/Client/`, `Runtime/Game/`, and `Runtime/Game/Requests/`.
- Runtime vs Editor boundary: runtime code must not depend on `UnityEditor` unless behind `#if UNITY_EDITOR`.

## 1) Public API discipline (Unity)

### 1.1 The API surface

- **Primary customer-facing API surface**: `Runtime/Game/LootLockerSDKManager.cs`.
  - This class is `partial` and very large; edits here are customer-visible.
  - Any new `public` method, signature change, rename, or behavior change should be treated as an API change and should go through a deprecation period if needed.
  - For file-specific rules (callback guarantees, parameter conventions, doc expectations), follow `.github/instructions/Runtime/Game/LootLockerSDKManager.cs.instructions.md`.

### 1.2 When to add a new public method

Add a new public method in `LootLockerSDKManager` when:
- The feature is new customer-facing functionality.
- The existing method(s) would become misleading if extended (e.g., parameters would become ambiguous or overload set becomes confusing).

Prefer extending existing public methods when:
- You are adding a small optional behavior that fits an existing concept.
- The change can be represented as an optional parameter with a safe default.

## 1.3 When and how to deprecate

Go through a deprecation flow when:
- A public method/DTO is going out of support.
- A public method/DTO signature has been updated in a way that is not backwards compatible.

Deprecations are done by marking the method/DTO with `[Obsolete("This method is deprecated, please use <Replacement method if applicable>")] // Deprecation date <today's date>`.
Deprecations should be done in their own commits and explicitly stated.
Deperecations also require mention in release notes as well as an update of user facing documentation.

### 1.4 Visibility defaults

- Default to `private`/`internal` for new helpers and types.
- Make a type `public` only when it is intentionally part of the SDK surface (e.g., response DTOs used in callbacks or facade methods).

### 1.5 Naming conventions

- Public methods: **PascalCase** (e.g., `Init`, `CheckInitialized`, `StartGuestSession`).
- Types: **PascalCase** prefixed with `LootLocker` for SDK DTOs.
- Request/response DTO properties: **match the server JSON field names**.
  - Commonly `snake_case` (e.g., `session_token`, `provider_name`).
  - Some DTOs contain PascalCase properties (e.g., `Code`, `Nonce`) — when adding fields, follow the server contract and surrounding file conventions.

### 1.6 Documentation expectations

Documentation requirements vary by surface area:

- Public API: XML docs should be clear and complete for any new/changed `public` members. For `LootLockerSDKManager.cs`, follow the scoped instructions in `.github/instructions/Runtime/Game/LootLockerSDKManager.cs.instructions.md`.
- Requests folder: follow `.github/instructions/Runtime/Game/Requests.instructions.md`.
- Internal code: keep docs light; prefer clarity through naming and small helpers.

## 2) Request/response & endpoint patterns

### 2.1 Where request code lives

For the repo map and “where do I implement X?”, see `.github/instructions/architecture.md`.

For folder-specific conventions (namespaces, validation/error patterns, endpoint building), follow `.github/instructions/Runtime/Game/Requests.instructions.md`.

### 2.2 How API calls are executed (transport)

- The common transport entry is `LootLockerServerRequest.CallAPI(...)`, defined as a `struct` inside `Runtime/Client/LootLockerHTTPClient.cs`.
- Typical pattern:
  1) Choose an endpoint from `Runtime/Client/LootLockerEndPoints.cs`.
  2) Format it using `EndPointClass.WithPathParameter(s)` when needed (`Runtime/Client/EndPointClass.cs`).
  3) Serialize the request body with `LootLockerJson.SerializeObject(...)`.
  4) Call `LootLockerServerRequest.CallAPI(...)`.
  5) Convert the raw response into your typed response using `LootLockerResponse.Deserialize(onComplete, serverResponse)`.

See `.github/instructions/patterns.md` for copy/paste templates.

### 2.3 Response & error handling conventions

- All typed responses inherit `LootLockerResponse` (`Runtime/Client/LootLockerResponse.cs`).
  - Key fields used by consumers:
    - `success` (bool)
    - `statusCode` (int)
    - `errorData` (`LootLockerErrorData`, `Runtime/Client/LootLockerErrorData.cs`)
    - `text` (raw response body)

- Use `LootLockerResponse.Deserialize<T>(...)` to create typed DTOs from raw responses.
  - If `serverResponse.errorData != null`, deserialization returns a failed `T` that carries `errorData` and the HTTP fields.

- For client-side validation failures (e.g., null input), return a typed error response via `LootLockerResponseFactory`:
  - Example observed: `LootLockerResponseFactory.InputUnserializableError<T>(forPlayerWithUlid)`.

### 2.4 Endpoint constants (`LootLockerEndPoints`)

- Endpoint constants are declared in `Runtime/Client/LootLockerEndPoints.cs` as `static EndPointClass` fields.
- Add new endpoints near related ones and keep the section headers (`[Header("...")]`) consistent.
- Naming is not fully consistent across the file (both `camelCase` and `PascalCase` exist).
  - Default rule for new endpoints: **match the naming style of the surrounding section**.
  - If you’re unsure (or the section is mixed), ask for a decision before introducing a new naming style.

## 3) Logging & secrets

### 3.1 Logger to use

- Use `LootLockerLogger.Log(...)` (`Runtime/Game/LootLockerLogger.cs`).
- Avoid calling `UnityEngine.Debug.Log*` directly in SDK code unless you are inside the logger implementation or temporarily debugging.

### 3.2 Do not log secrets

- Never log API keys, domain keys, passwords, session tokens, refresh tokens, or other credentials unless obfuscated.
- When logging HTTP request/response bodies:
  - Route logs through the logger’s obfuscation/prettify pipeline.
  - The SDK supports obfuscation using `LootLockerObfuscator.ObfuscateJsonStringForLogging(...)` (`Runtime/Game/LootLockerObfuscator.cs`) and `LootLockerConfig.current.obfuscateLogs`.

## 4) Serialization conventions

- Use `LootLockerJson` (`Runtime/Client/LootLockerJson.cs`) for all serialization/deserialization.
- Serializer selection is compile-time:
  - If `LOOTLOCKER_USE_NEWTONSOFTJSON` is defined, the SDK uses Newtonsoft.
  - Otherwise it uses the built-in `ZeroDepJson` implementation under `Runtime/Libraries/ZeroDepJson/`.
- Do **not** introduce new serialization libraries.
- Prefer DTO property names that match the server’s JSON field names to ensure consistent behavior across serializers.

## 5) Formatting & tooling

- No repo-level `.editorconfig` is currently present.
- Rule: **match surrounding style** and do not reformat unrelated code.
  - Keep braces/spacing consistent with the file you are editing.
  - Keep using directives consistent with existing patterns in that file.

## 6) Diff hygiene

- Keep diffs minimal and localized to the requested change.
- Avoid large renames/moves; avoid mechanical formatting changes.
- Search-first to prevent duplicated helpers/utilities.
