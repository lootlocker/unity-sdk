# LootLocker Unity SDK — Patterns cookbook

Scope: Copy/paste-friendly templates based on how this repo already implements features.

This file is intentionally **templates**, not policy. For rules and conventions, follow:
- `.github/instructions/style-guide.md`
- `.github/instructions/guardrails.md`
- Path-specific instruction files under `.github/instructions/**.instructions.md`

## A) Adding a new API call (typical flow)

1) Add endpoint constant in `Runtime/Client/LootLockerEndPoints.cs`:
- Add `public static EndPointClass <name> = new EndPointClass("path", LootLockerHTTPMethod.<METHOD>);`
- Place it under the matching `[Header("...")]` section or create a new one if this is a new feature set.
- Match local naming style (some sections use `camelCase`, some use `PascalCase`).

2) Add request/response DTOs + request method under `Runtime/Game/Requests/<Feature>Request(s).cs`:
- Co-locate DTOs and the execution method in the same file (common in this repo).
- Put DTOs in the `LootLocker.Requests` namespace.
- Put execution method(s) in `namespace LootLocker { public partial class LootLockerAPIManager { ... } }`.

For folder-specific conventions (validation, error responses, endpoint building, transport usage), follow `.github/instructions/Runtime/Game/Requests.instructions.md`.

3) Expose it publicly via `Runtime/Game/LootLockerSDKManager.cs` if it is customer-facing.

For public API surface rules (compatibility, callback guarantees, parameter conventions, XML docs expectations), follow `.github/instructions/Runtime/Game/LootLockerSDKManager.cs.instructions.md`.

## B) Request method template (CallAPI + typed response)

Pattern:

- Validate inputs early; if invalid, invoke `onComplete?.Invoke(LootLockerResponseFactory.<ErrorType><T>(forPlayerWithUlid))` and return.
- Build endpoint using `EndPointClass.WithPathParameter(s)` for path parameters and `LootLocker.Utilities.HTTP.QueryParamaterBuilder` for query parameters.
- Serialize body using `LootLockerJson.SerializeObject(request)`.
- Call transport using `LootLockerServerRequest.CallAPI(...)`.
- Deserialize into typed response using `LootLockerResponse.Deserialize(onComplete, serverResponse)`.

## C) DTO conventions (request/response/data)

- Responses: `class <Name>Response : LootLockerResponse { ... }`
- Requests: plain classes, often named `LootLocker<Name>Request`.
- Properties commonly use `snake_case` to match JSON field names.
- Add XML docs on DTO fields that are user-facing or otherwise important.

## D) Public API docs (LootLockerSDKManager)

See `.github/instructions/Runtime/Game/LootLockerSDKManager.cs.instructions.md`.

## E) Logging

See `.github/instructions/style-guide.md` (Logging & secrets).
