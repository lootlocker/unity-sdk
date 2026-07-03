# Release Notes Template

Use this template when drafting a GitHub release for the Unity SDK.

## Title Format

```
LootLocker_UnitySDKv{MAJOR}.{MINOR}.{PATCH}
```

Examples: `LootLocker_UnitySDKv8.0.0`, `LootLocker_UnitySDKv7.2.1`

---

## Body Template — Major/Minor Release

```markdown
### Features

• 🚀 **Feature Title** — Description of the feature. Keep it user-facing and
actionable. Mention what problem it solves or what new capability it unlocks.
• 🔗 **Another Feature** — Another description. Use emojis to visually group
related features (e.g. 💳 for payments, 📱 for platform-specific, 🎮 for auth).
• 📚 **Documentation / Tooling** — If applicable.
• Minor additions without their own emoji header.

### Fixes

• Fixed an issue where X would happen when Y.
• Fixed a crash when Z was null.
• Fixed compilation errors on platform/engine version.

### Breaking Changes

• Removed `DeprecatedMethod` — use `NewMethod` instead.
• `OldClass` renamed to `NewClass` — update your references.

### Deprecations

• `OldMethod` is now deprecated and will be removed in v{NEXT_MAJOR}. Migrate to `NewMethod`.

Full Changelog: [v{PREV_VERSION}...v{NEW_VERSION}](https://github.com/lootlocker/unity-sdk/compare/v{PREV_VERSION}...v{NEW_VERSION})
```

---

## Body Template — Patch Release

```markdown
### Fix

• Fixed a crash when X is null.
• Fixed incorrect parsing of Y responses.

Full Changelog: [v{PREV_VERSION}...v{NEW_VERSION}](https://github.com/lootlocker/unity-sdk/compare/v{PREV_VERSION}...v{NEW_VERSION})
```

---

## Writing Guidelines

- **Audience:** Game developers using LootLocker in Unity. Keep descriptions
  user-facing — describe what _they_ can do or what was fixed for _them_.
- **Links:** Use full URLs for cross-references (blog posts, docs, compare links).
- **Emojis:** Use to visually group features. Common ones:
  - 🚀 Major new capability
  - 💳 Payments / purchases
  - 🎮 Authentication / platforms
  - 📱 Platform-specific (mobile, console)
  - 📚 Documentation
  - 🧪 Testing / CI
  - ⚡ Performance / real-time (Presence, WebSockets)
  - 🔍 Filtering / search
  - 🔗 Linking / connected accounts
  - 🛒 Store / catalog
  - 📣 Broadcasts / notifications
- **Sections:** Only include sections that have content. Don't add empty `### Features` etc.
- **Compare link:** Always include the `Full Changelog` link at the bottom.
