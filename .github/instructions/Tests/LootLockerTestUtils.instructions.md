---
applyTo: "Tests/LootLockerTestUtils/**/*.cs"
---

# Scoped instructions: `Tests/LootLockerTestUtils/` (test configuration utilities)

This folder holds the admin-API client and helpers that tests use to provision and tear down
real LootLocker game configurations. It is **not** test code itself — it is infrastructure
that backs the tests under `Tests/LootLockerTests/PlayMode/`.

## Key classes

| Class / File | Responsibility |
|---|---|
| `LootLockerTestGame` | Creates/deletes a game, exposes helpers for common game setup (enable platforms, create leaderboards, etc.). |
| `LootLockerTestUser` | Gets or signs up a time-scoped admin user so the admin API calls are authenticated. |
| `Auth` | Low-level admin login / signup via the LootLocker admin API. |
| `LootLockerAdminRequest` | Thin wrapper around `LootLockerServerRequest.CallAPI` that injects the `Admin` caller role and handles rate-limit retries. |
| `LootLockerTestConfigurationEndpoints` | Constants for all admin API endpoints used by the utilities. |

## How authentication works

`LootLockerTestUser.GetUserOrSignIn` auto-derives a deterministic email from the current
date (e.g. `unity+ci-testrun+2024-04-17-14h@lootlocker.com`) and the same string as the
password, then attempts login and falls back to signup on a 401. This means test runs
are self-contained and need no pre-provisioned secrets in stage environments.

## Transport pattern

All admin API calls use `LootLockerAdminRequest.Send(...)`, which sets
`callerRole: LootLockerCallerRole.Admin`. Never call `LootLockerServerRequest.CallAPI`
directly from this folder — go through `LootLockerAdminRequest.Send`.

```csharp
LootLockerAdminRequest.Send(endPoint.endPoint, endPoint.httpMethod, json,
    onComplete: (serverResponse) =>
    {
        var response = LootLockerResponse.Deserialize<MyResponse>(serverResponse);
        onComplete?.Invoke(response);
    }, useAuthToken: true);
```

## Adding a new admin API helper

1. Add the endpoint constant to `LootLockerTestConfigurationEndpoints.cs`.
2. Create a request/response DTO (plain class or `LootLockerResponse` subclass) in the
   relevant file, or add a new file for a new domain area.
3. Add a static method to the appropriate class (`LootLockerTestGame`, `LootLockerTestAssets`,
   etc.) that:
   - Checks `if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))` first and
     invokes `onComplete` with a failed response if not authenticated.
   - Uses `LootLockerAdminRequest.Send(...)` for transport.
   - Deserializes with `LootLockerResponse.Deserialize<T>(serverResponse)`.
4. Map the requests to the correct endpoint according to the api docs: https://ref.lootlocker.com/admin

## Namespace

All classes in this folder live in `namespace LootLockerTestConfigurationUtils`.

## Conventions

- Keep methods callback-based (`Action<...> onComplete`) to match the async Unity pattern
  used by tests (`yield return new WaitUntil(() => done)`).
- Do not reference `LootLockerTests` from this utility layer — the dependency runs one way:
  tests depend on utils, not the other way.
- Use `LootLockerJson.SerializeObject(...)` for request body serialization — same as runtime.
- Error paths must always invoke `onComplete` exactly once.
- Endpoint strings that include a game ID placeholder use `#GAMEID#`, which
  `LootLockerAdminRequest` replaces automatically from `LootLockerAdminRequest.ActiveGameId`.
