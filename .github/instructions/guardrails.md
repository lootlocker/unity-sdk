# Coding Agent Guardrails (LootLocker Unity SDK)

These rules exist to keep changes safe, reviewable, and aligned with how this repo ships.

## Branching + PRs
- Never commit directly to `dev` or `main`.
- Create a work branch (e.g. `docs/...`, `fix/...`, `feat/...`).
- Open PRs targeting `dev` (never `main`).

## Release / versioning prohibitions
Unless the task explicitly asks for it, do **not**:
- Create tags, GitHub Releases, or publish packages.
- Bump versions or edit release metadata (for example `package.json` version).

## Change discipline
- Keep diffs minimal and scoped to the task.
- Do not move/rename files or restructure folders unless explicitly requested.
- Search before adding new helpers/utilities to avoid duplication.
- Avoid drive-by refactors (formatting, naming, reorganization) unless requested.

## Runtime vs Editor boundary
- Runtime code must be build-safe (no unguarded `UnityEditor` dependencies).
- Put editor tooling under `Runtime/Editor/` (this repo’s editor-only area) and/or guard editor-only code with `#if UNITY_EDITOR`.

## When unsure
If a change would require guessing architecture, conventions, or customer-facing API behavior:
- Stop and ask for clarification rather than inventing a new pattern.

## Reference
- Architecture & structure overview: `.github/instructions/architecture.md`
- Style Guide: `.github/instructions/style-guide.md`
- Code patterns: `.github/instructions/patterns.md`
