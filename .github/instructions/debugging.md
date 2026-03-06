# Debugging issues using the test infrastructure

When investigating a bug, you can create **temporary** debugging tests that use the same
`LootLockerTestGame` / `LootLockerSDKManager` infrastructure as the production tests. This
lets you reproduce, isolate, and fix an issue with a tight feedback loop before cleaning
everything up.

> **Critical rule: debugging tests must never be committed to a PR.**
> They exist only as a local working aid. Always delete them before committing.

---

## Workflow overview

1. **Create** a temporary test file (or method) that reproduces the symptom.
2. **Run** it with the targeted debug category or filter.
3. **Iterate** — fix code, re-run — until the test passes.
4. **Delete** the temporary test code.
5. Continue with the fix and any supporting production tests.

---

## Creating the debug test file

Create a file `Tests/LootLockerTests/PlayMode/DebugTests.cs`. This name is conventional
and signals that the file is ephemeral.

Use the same `[UnitySetUp]` / `[UnityTearDown]` pattern as production tests (see
`.github/instructions/Tests/LootLockerTests/PlayMode.instructions.md`), but mark every
method with `Category("LootLockerDebug")` instead of the production categories:

```csharp
using System.Collections;
using LootLocker;
using LootLocker.Requests;
using LootLockerTestConfigurationUtils;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace LootLockerTests.PlayMode
{
    /// <summary>
    /// TEMPORARY DEBUG TESTS — delete this file before committing.
    /// </summary>
    public class DebugTests
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

            if (!LootLockerConfig.ClearSettings())
            {
                Debug.LogError("Could not clear LootLocker config");
            }

            bool gameCreationCallCompleted = false;
            LootLockerTestGame.CreateGame(testName: "Debug_" + TestCounter + " ", onComplete: (success, errorMessage, game) =>
            {
                if (!success) { SetupFailed = true; }
                gameUnderTest = game;
                gameCreationCallCompleted = true;
            });
            yield return new WaitUntil(() => gameCreationCallCompleted);
            if (SetupFailed) { yield break; }

            gameUnderTest?.SwitchToStageEnvironment();

            // Enable platforms as needed for the specific issue being debugged
            bool enableGuestLoginCallCompleted = false;
            gameUnderTest?.EnableGuestLogin((success, errorMessage) =>
            {
                if (!success) { SetupFailed = true; }
                enableGuestLoginCallCompleted = true;
            });
            yield return new WaitUntil(() => enableGuestLoginCallCompleted);
            if (SetupFailed) { yield break; }

            Assert.IsTrue(gameUnderTest?.InitializeLootLockerSDK(), "Failed to initialize LootLocker");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
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
        }

        // -------------------------------------------------------------------------
        // Debugging test — add/remove methods freely; this whole file gets deleted
        // -------------------------------------------------------------------------

        [UnityTest, Category("LootLockerDebug")]
        public IEnumerator Debug_ScoresReturnedInWrongOrder_Reproduces()
        {
            Assert.IsFalse(SetupFailed, "Failed to setup game");

            // Reproduce the bug by setting up the minimal state that triggers it,
            // then assert the expected (correct) behavior. When the test goes green
            // the bug is fixed.

            // ... reproduction steps ...

            yield return null;
        }
    }
}
```

---

## Running the debug test

**Locally:**
```powershell
.github\scripts\run-tests.ps1 -TestCategory LootLockerDebug
```

Or run a single method by name:
```powershell
.github\scripts\run-tests.ps1 -TestFilter "DebugTests.Debug_ScoresReturnedInWrongOrder_Reproduces"
```

**In CI** (if you need cloud execution to reproduce an issue):
```
Actions → Run Tests → Run workflow
  testCategory: LootLockerDebug
```

> Tip: push only to a `fix/<name>` branch when running debug tests in CI, and squash/drop
> the `DebugTests.cs` commits before opening a PR.

---

## Checklist before opening a PR

- [ ] `Tests/LootLockerTests/PlayMode/DebugTests.cs` has been **deleted**.
- [ ] No `Category("LootLockerDebug")` remains anywhere in the diff.
- [ ] Any permanent test covering the fixed behavior has been added as a proper production
      test with `Category("LootLocker")` and `Category("LootLockerCI")`.

---

## Tips

- Keep debug tests minimal — reproduce only the symptom, not a full user scenario.
- If the root cause is unclear, start with a failing assertion that captures incorrect
  behavior, then work backwards into the code.
- `Debug.Log` freely inside `DebugTests.cs`; none of it will survive to the PR.
- If the issue turns out to be a missing test case rather than a bug, convert the repro
  into a proper production test (rename the file/method, remove `LootLockerDebug` category,
  add `LootLockerCI` and `LootLockerCIFast` as appropriate).
