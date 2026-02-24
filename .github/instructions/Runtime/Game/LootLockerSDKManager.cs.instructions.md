---
applyTo: "Runtime/Game/LootLockerSDKManager.cs"
---

# Scoped instructions: `LootLockerSDKManager.cs` (public API surface)

- Treat this file as the primary, customer-facing API surface. Preserve backward compatibility.
- Prefer additive changes (new methods/overloads) over breaking changes to existing signatures, default values, namespaces, or behavior.
- Keep API methods thin: route work to existing request implementations under `Runtime/Game/Requests/` (or shared helpers) rather than duplicating endpoint/transport logic here.
- Always place methods in this file in a `#region` block corresponding to their feature set (for example, Authentication, Inventory, etc.) and keep them organized with related methods.
- XML docs are required for any `public` API you add or change. Match the existing doc style in this file, including clear `param` descriptions and any practical usage notes.

## XML doc template (adjust as needed)

```csharp
/// <summary>
/// One-sentence description of what the method does.
///
/// Optional additional details or usage notes.
/// </summary>
/// <param name="forPlayerWithUlid"> Optional : Execute the request for the specified player. If not supplied, the default player will be used. </param>
/// <param name="onComplete"> onComplete Action for handling the response </param>
/// <returns>
/// Return value semantics if this API returns a value. Otherwise omit this tag.
/// </returns>
public static void ExampleMethod(string forPlayerWithUlid, Action<LootLockerExampleResponse> onComplete)
{
	// Keep facade thin; validate input early; call onComplete once on all paths.
}
```
- If a method takes a player selector, follow the existing `forPlayerWithUlid` convention (optional when appropriate; don’t invent new parameter names for the same concept).
- For callback-based APIs, call `onComplete` exactly once on all code paths (including validation failures).
- Do not log secrets or raw tokens from this layer. Use `LootLockerLogger` (not `Debug.Log`) and rely on log obfuscation (`LootLockerConfig.current.obfuscateLogs`).
- Use the repo’s JSON wrapper (`LootLockerJson`) and existing response/error helpers; do not introduce new serialization/logging dependencies.
- Keep diffs minimal and localized; avoid unrelated refactors in this large file.
