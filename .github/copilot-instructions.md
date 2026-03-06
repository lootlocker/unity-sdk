# Copilot / Coding Agent instructions (LootLocker Unity SDK)

Follow these rules for any work in this repo:

## Non-negotiables
- Never commit directly to `dev` or `main`.
- PRs must target `dev`.
- Do not tag/publish/create releases.
- Do not bump versions or edit release metadata (for example `package.json` version) unless explicitly asked.
- Keep diffs minimal; do not move/rename files unless explicitly requested.
- Search first to avoid duplicating helpers/utilities.

## Architecture references
- Repo structure + “where do I implement X?”: `.github/instructions/architecture.md`
- Guardrails (agent operating rules): `.github/instructions/guardrails.md`

## Verification (compile & test before PR)
- How to verify changes (local + CI): `.github/instructions/verification.md`
  - Cloud agent: push to work branch → wait for **Compile Check** workflow.
  - Local: run `.github/scripts/verify-compilation.sh` (Linux/macOS) or `.github\scripts\verify-compilation.ps1` (Windows) after creating `unity-dev-settings.json` from the example:
    ```json
    {
      "unity_executable": "<absolute path to Unity binary>",
      "test_project_path": ""
    }
    ```

## Conventions & style
- Coding conventions & style guide: `.github/instructions/style-guide.md`
- Patterns cookbook (templates): `.github/instructions/patterns.md`
- Path-specific instructions:
  - Public API surface (`Runtime/Game/LootLockerSDKManager.cs`): `.github/instructions/Runtime/Game/LootLockerSDKManager.cs.instructions.md`
  - Request implementations (`Runtime/Game/Requests/**`): `.github/instructions/Runtime/Game/Requests.instructions.md`

## Issue Tracking & Lifecycle

All SDK work is driven by a tracking issue in [lootlocker/index](https://github.com/lootlocker/index). That issue is the single source of truth for status, decisions, and acceptance criteria. **You must keep it up to date throughout your work.**

### Project Status

This issue will almost always be tracked in project https://github.com/orgs/lootlocker/projects/75. Update the issue's project status as your work progresses:

| Situation | Status to set |
|-----------|--------------|
| You start working on the task | **In Progress** |
| You are blocked and need input from a human | **Blocked** |
| A PR has been opened and is ready for review | **In Review** |

### Architectural Decisions & Questions

Do not make undocumented assumptions. If a question or decision arises during implementation:
- Leave a comment on the tracking issue describing the question or decision clearly.
- Tag @kirre-bylund so it can be addressed.
- Set the project status to **Blocked** and stop work on the affected area until answered.

### Linking PRs

As soon as you open a PR in this repo, post a comment on the tracking issue with the PR link. Also link the PR formally via GitHub's "Development" section on the tracking issue.

### Acceptance Criteria & Definition of Done

Check off items in the tracking issue's Definition of Done as they are completed. If scope changes during implementation, update the acceptance criteria in the tracking issue and leave a comment explaining what changed and why.
