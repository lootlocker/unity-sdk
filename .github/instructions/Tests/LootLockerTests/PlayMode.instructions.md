---
applyTo: "Tests/LootLockerTests/PlayMode/**/*.cs"
---

# Scoped instructions: `Tests/LootLockerTests/PlayMode/` (PlayMode integration tests)

These are the SDK's integration tests. They run as Unity PlayMode tests and use the
`LootLockerTestUtils` infrastructure to create and tear down a real, isolated LootLocker
game via the admin API for every test run.

## Anatomy of a test file

Every production test file follows this structure exactly:

```csharp
using System.Collections;
using LootLocker;
using LootLocker.Requests;
using LootLockerTestConfigurationUtils;
using NUnit.Framework;
using UnityEditor;        // only if required by the feature under test
using UnityEngine;
using UnityEngine.TestTools;

namespace LootLockerTests.PlayMode
{
    public class MyFeatureTest
    {
        private LootLockerTestGame gameUnderTest = null;
        private LootLockerConfig configCopy = null;
        private static int TestCounter = 0;
        private bool SetupFailed = false;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            TestCounter++;
            configCopy = LootLockerConfig.current;
            Debug.Log($"##### Start of {this.GetType().Name} test no.{TestCounter} setup #####");

            if (!LootLockerConfig.ClearSettings())
            {
                Debug.LogError("Could not clear LootLocker config");
            }

            // Create isolated game
            bool gameCreationCallCompleted = false;
            LootLockerTestGame.CreateGame(testName: this.GetType().Name + TestCounter + " ", onComplete: (success, errorMessage, game) =>
            {
                if (!success)
                {
                    SetupFailed = true;
                    gameCreationCallCompleted = true;
                    return;
                }
                gameUnderTest = game;
                gameCreationCallCompleted = true;
            });
            yield return new WaitUntil(() => gameCreationCallCompleted);
            if (SetupFailed) { yield break; }

            gameUnderTest?.SwitchToStageEnvironment();

            // Enable required platform(s) — add only what the feature under test needs
            bool enableGuestLoginCallCompleted = false;
            gameUnderTest?.EnableGuestLogin((success, errorMessage) =>
            {
                if (!success) { SetupFailed = true; }
                enableGuestLoginCallCompleted = true;
            });
            yield return new WaitUntil(() => enableGuestLoginCallCompleted);
            if (SetupFailed) { yield break; }

            Assert.IsTrue(gameUnderTest?.InitializeLootLockerSDK(), "Failed to initialize LootLocker");

            // Start a default session for the test
            bool sessionCompleted = false;
            LootLockerSDKManager.StartGuestSession(GUID.Generate().ToString(), response =>
            {
                SetupFailed |= !response.success;
                sessionCompleted = true;
            });
            yield return new WaitUntil(() => sessionCompleted);

            Debug.Log($"##### Start of {this.GetType().Name} test no.{TestCounter} test case #####");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Debug.Log($"##### End of {this.GetType().Name} test no.{TestCounter} test case #####");
            if (gameUnderTest != null)
            {
                bool gameDeletionCallCompleted = false;
                gameUnderTest.DeleteGame(((success, errorMessage) =>
                {
                    if (!success) { Debug.LogError(errorMessage); }
                    gameUnderTest = null;
                    gameDeletionCallCompleted = true;
                }));
                yield return new WaitUntil(() => gameDeletionCallCompleted);
            }
            LootLockerStateData.ClearAllSavedStates();
            LootLockerConfig.CreateNewSettings(configCopy);
            Debug.Log($"##### End of {this.GetType().Name} test no.{TestCounter} tear down #####");
        }

        [UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public IEnumerator MyFeature_DoesExpectedThing_Succeeds()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // Given
            // ...

            // When
            MyResponseType actualResponse = null;
            bool callCompleted = false;
            LootLockerSDKManager.SomeMethod(param, response =>
            {
                actualResponse = response;
                callCompleted = true;
            });
            yield return new WaitUntil(() => callCompleted);

            // Then
            Assert.IsTrue(actualResponse.success, "Expected call to succeed");
            Assert.AreEqual(expected, actualResponse.someField, "Got unexpected value");
        }
    }
}
```

## Test method naming

Format: `<Feature>_<Action>_<ExpectedOutcome>`

Examples:
- `Leaderboard_ListTopTen_ReturnsScoresInDescendingOrder`
- `GuestSession_StartWithValidId_Succeeds`
- `PlayerStorage_UpdateNonExistentKey_CreatesIt`

## Category attributes

Every production test method must carry the appropriate category tags:

| Category | Meaning |
|---|---|
| `LootLocker` | All SDK tests — the catch-all for manual / local runs. |
| `LootLockerCI` | Included in the standard CI run (full suite). |
| `LootLockerCIFast` | Included in the fast CI subset. **Only add this if the test consistently finishes in under ~10 seconds.** |

Typical annotation: `[UnityTest, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]`

Do **not** use `Category("LootLockerDebug")` on production tests — that category is reserved
for temporary debugging tests that must be removed before any PR.

## Game lifecycle rules

Every test class creates a **fresh isolated game** per test run; there is no shared state
between test methods. This keeps tests hermetic but requires strict discipline:

- Always call `gameUnderTest.DeleteGame(...)` in `TearDown` even if `SetupFailed` — otherwise
  orphaned games accumulate in the admin account.
- Always guard teardown with `if (gameUnderTest != null)`.
- Always check `if (SetupFailed) { yield break; }` after every blocking setup step.
- Call `LootLockerStateData.ClearAllSavedStates()` and `LootLockerConfig.CreateNewSettings(configCopy)`
  at the end of every `TearDown` to restore SDK state for the next test.

## Test body conventions

- First line of every test: `Assert.IsFalse(SetupFailed, "Failed to setup game");`
- Follow **Given / When / Then** comment structure inside the test body.
- Use `yield return new WaitUntil(() => done)` to await async SDK calls; always
  pair with a `bool done = false` flag set in the callback.
- Use NUnit `Assert.*`; never use `Debug.LogError` as a substitute for assertions.
- Do not add `using UnityEditor` unless the test must run in edit mode.

## One test class per test file

One file = one public class. Keep feature scope narrow — a file named `LeaderboardTest.cs`
should only cover the Leaderboard feature.

## What NOT to do

- Do not leave debugging logic (`Debug.Log` spam, disabled asserts as comments, etc.) in
  committed code.
- Do not skip the `TearDown` game deletion — orphaned admin games are costly.
- Do not commit any test with `Category("LootLockerDebug")` — see `.github/instructions/debugging.md`.
- Do not add new test helpers or shared utilities directly in this folder; put them under
  `Tests/LootLockerTestUtils/` instead (see `.github/instructions/Tests/LootLockerTestUtils.instructions.md`).
