# Verifying Changes: Compilation & Tests

This repo ships as a Unity UPM package. Unity C# must be verified through the Unity
Editor's compilation pipeline — `dotnet build` does **not** work on this codebase.

---

## Cloud coding agent (GitHub Copilot agent / CI)

When you push commits to a work branch the **`Compile Check`** workflow
(`.github/workflows/compile-check.yml`) runs automatically and verifies:

1. All SDK C# code compiles without errors (including Samples).
2. The configured Unity **editmode** tests pass (see the workflow for exact coverage).

### Workflow to follow after each batch of commits

1. Push your work branch (e.g. `feat/my-feature`, `fix/something`).
2. Navigate to the **Actions** tab → **Compile Check** → your branch's run.
3. Wait for it to complete and confirm it is **green** before opening a PR.

If the workflow fails:

1. **Job Summary** (fastest) — the run's Summary page shows a compact
   `Compilation Errors` table with file path, line number, and error code/message.
   This is the primary place to read errors; no log-digging required.
2. **Annotations** — the Summary page also lists inline annotations (e.g.
   `sdk/Runtime/Game/Requests/TriggersRequests.cs#L3`) that link directly to the
   offending line. These also appear in the PR diff view.
3. **Raw log** (fallback) — if neither of the above is present the compile step itself
   may have crashed before producing output; click the failed step and search for
   `compilationhadfailure: True` to find the relevant section.

Fix the reported errors and push again.

> The full CI suite (`run-tests-and-package.yml`) runs on PRs to `dev` and `main`.
> The compile check is a smaller, faster subset for in-progress work branches.

---

## Local (human developer or local Copilot instance)

### Prerequisites

1. Unity is installed locally (any Unity **2019.2+** version works; best to match the
   project's minimum version in `package.json`).
2. You have a `unity-dev-settings.json` at the repo root (gitignored).
3. Python 3 is installed and available on your `PATH` as `python3` (used by the
   local verification scripts to parse `unity-dev-settings.json`).

### One-time setup

Open or create `unity-dev-settings.json` and fill in the two fields:

```json
{
  "unity_executable": "<absolute path to Unity binary>",
  "test_project_path": "<relative path to a Unity project including the SDK>"
}
```

**`unity_executable` examples by platform:**

| Platform | Example path |
|---|---|
| macOS   | `/Applications/Unity/Hub/Editor/2022.3.22f1/Unity.app/Contents/MacOS/Unity` |
| Windows | `C:\Program Files\Unity\Hub\Editor\2022.3.22f1\Editor\Unity.exe` |
| Linux   | `/opt/unity/Editor/Unity` |

**`test_project_path`**: leave empty to let the script auto-create a temporary project
that references the SDK. Set to an absolute path only if you already maintain a
dedicated local Unity project that points at this SDK via a local package reference.

### Running the check

**Linux / macOS (bash):**
```bash
.github/scripts/verify-compilation.sh
```

**Windows (PowerShell):**
```powershell
.github\scripts\verify-compilation.ps1
```

The script will:

1. Read `unity-dev-settings.json`.
2. Create a temporary Unity project at `Temp~/VerificationProject` (gitignored) that
   references the SDK as a local package and includes the Samples.
3. Launch Unity in batch mode with `-batchmode -nographics -quit`.
4. Print Unity's compilation output filtered to errors and warnings.
5. Exit `0` on success, `1` on any compilation error.

---

## What counts as "verified"

A change is verified when **either** of the following is true:

- The `Compile Check` CI workflow is green on your branch, **or**
- The local verification script exits `0`.

**Additionally** ensure:

- No new `error CS` compiler errors appear.
- No existing public API signatures were changed without going through the deprecation
  flow described in `.github/instructions/style-guide.md`.

Running the full integration tests (`run-tests-and-package.yml`) is a CI-only step and
is not required for local verification.
