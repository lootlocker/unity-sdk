# Testing: writing and running SDK integration tests

This document explains how tests are structured, how to write new tests, and how to run
them locally or in CI.

For path-specific rules (templates, naming, categories), see the scoped instructions:
- `Tests/LootLockerTests/PlayMode/**`: `.github/instructions/Tests/LootLockerTests/PlayMode.instructions.md`
- `Tests/LootLockerTestUtils/**`: `.github/instructions/Tests/LootLockerTestUtils.instructions.md`

---

## Repository layout

```
Tests/
  LootLockerTests/
    PlayMode/           ← integration tests (one file per feature)
      PlayModeTests.asmdef
  LootLockerTestUtils/  ← admin-API helpers consumed by tests
      LootLockerTestUtils.asmdef
```

All tests are **Unity PlayMode tests** under `LootLockerTests/PlayMode/`. There are no
EditMode tests in this folder (except `BroadcastTests.cs`, which is intentionally `[Test]`
not `[UnityTest]` because it tests pure C# logic).

---

## How tests provision game configuration

Tests do **not** rely on pre-existing API keys or shared game state. Instead, every test
class creates a fresh LootLocker game via the **admin API** in `[UnitySetUp]` and deletes
it in `[UnityTearDown]`:

1. `LootLockerTestGame.CreateGame(...)` calls the admin API to create a new game, returning
   a `LootLockerTestGame` object with the game's API key and helpers.
2. `gameUnderTest.SwitchToStageEnvironment()` targets the stage environment.
3. Platform/feature enablement — e.g. `gameUnderTest.EnableGuestLogin(...)`, 
   `gameUnderTest.CreateLeaderboard(...)` — configures the game for the test.
4. `gameUnderTest.InitializeLootLockerSDK()` configures the SDK singleton with the fresh
   game's API key so subsequent `LootLockerSDKManager.*` calls hit the correct game.
5. `[UnityTearDown]` always calls `gameUnderTest.DeleteGame(...)` to clean up.

Admin authentication is handled automatically by `LootLockerTestUser.GetUserOrSignIn`, which
derives credentials from the current date and signs up or logs in as needed — no pre-stored
secrets are required for stage environments.

---

## Test categories

| Category | Run by | Use when |
|---|---|---|
| `LootLocker` | Manual / local run (all tests) | Always add this |
| `LootLockerCI` | Full CI run | Always add this for new tests |
| `LootLockerCIFast` | Fast CI subset | Add only when the test reliably finishes in < ~10 s |
| `LootLockerDebug` | Targeted debug run | **Temporary only** — never commit; see `debugging.md` |

Every production test method should be decorated (but choose categories according to the above table):
```csharp
[UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
```

---

## Writing a new test

1. Create a file `Tests/LootLockerTests/PlayMode/<FeatureName>Tests.cs`.
2. Follow the `[UnitySetUp]` / `[UnityTearDown]` template in
   `.github/instructions/Tests/LootLockerTests/PlayMode.instructions.md`.
3. In `Setup`, enable only the platforms and game features the test actually needs.
4. Write test methods following the `<Feature>_<Action>_<ExpectedOutcome>` naming pattern.
5. Follow the **Given / When / Then** structure in the test body.
6. Tag each method with the appropriate `Category` attributes.

Do not create shared state between test methods. With `[UnitySetUp]`/`[UnityTearDown]`,
each test method gets its own fresh game, so tests can and will run in any order.

---

## Adding admin API helpers (LootLockerTestUtils)

If your test needs game configuration that no existing helper covers:

1. Add the admin API endpoint constant to `LootLockerTestConfigurationEndpoints.cs`.
2. Add a method to the relevant class in `LootLockerTestUtils/`
   (e.g. `LootLockerTestGame`, `LootLockerTestAssets`) or create a new file for a new
   domain area.
3. Follow the patterns in `.github/instructions/Tests/LootLockerTestUtils.instructions.md`.

---

## Running tests locally

Use the PowerShell script `.github/scripts/run-tests.ps1`:

```powershell
# Run fast subset (LootLockerCIFast)
.github\scripts\run-tests.ps1 -TestCategory LootLockerCIFast

# Run a specific test class by name
.github\scripts\run-tests.ps1 -TestFilter "LeaderboardTest"

# Run a specific test method by full name
.github\scripts\run-tests.ps1 -TestFilter "LootLockerTests.PlayMode.LeaderboardTest.Leaderboard_ListTopTen_Succeeds"

# Run all SDK tests
.github\scripts\run-tests.ps1 -TestCategory LootLocker
```

Requirements: `unity-dev-settings.json` at repo root with a valid `unity_executable`.
See `.github/instructions/verification.md` for setup instructions.

---

## Running tests in CI

Push to a work branch — the **`Run Tests`** workflow
(`.github/workflows/run-tests.yml`) can be triggered manually via `workflow_dispatch`
from the **Actions** tab with optional `testCategory` and `testFilter` inputs.

The full integration test suite (`LootLockerCI`) runs automatically on PRs to `dev` and
`main` via the existing `run-tests-and-package.yml` workflow.

---

## What counts as "tested"

A test pass is verified when **either** of the following is true after your change:

- The `Run Tests` CI workflow is green on your branch, **or**
- The local `.github/scripts/run-tests.ps1` exits `0`.

Always run the fast subset (`LootLockerCIFast`) at minimum before opening a PR. If your
change directly touches a feature area with existing tests, run those tests specifically.
