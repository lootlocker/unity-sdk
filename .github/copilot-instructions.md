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

## Conventions & style
- Coding conventions & style guide: `.github/instructions/style-guide.md`
- Patterns cookbook (templates): `.github/instructions/patterns.md`
- Path-specific instructions:
  - Public API surface (`Runtime/Game/LootLockerSDKManager.cs`): `.github/instructions/Runtime/Game/LootLockerSDKManager.cs.instructions.md`
  - Request implementations (`Runtime/Game/Requests/**`): `.github/instructions/Runtime/Game/Requests.instructions.md`
