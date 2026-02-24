---
applyTo: "Runtime/Game/Requests/**/*.cs"
---

# Scoped instructions: `Runtime/Game/Requests/` (request implementations)

- Follow the established split:
  - DTOs/response models in `namespace LootLocker.Requests`.
  - Request methods in `namespace LootLocker` inside `public partial class LootLockerAPIManager`.
- Responses should inherit `LootLockerResponse` and use property names that match server JSON (commonly `snake_case`), as seen throughout this folder.
- Validate inputs early. For an unserializable/missing body, return a consistent error response via `LootLockerResponseFactory` and invoke `onComplete` once.
- Serialize request bodies with `LootLockerJson.SerializeObject(...)` (never call Newtonsoft/ZeroDep JSON APIs directly).
- Build endpoints via `LootLockerEndPoints` + `EndPointClass.WithPathParameter(s)`; avoid manual string concatenation for path params.
- For query parameters, prefer `LootLocker.Utilities.HTTP.QueryParamaterBuilder` when available; otherwise ensure values are URL-encoded (for example `WebUtility.UrlEncode`).
- Use the standard transport pattern:
  - `LootLockerServerRequest.CallAPI(..., onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, useAuthToken: true/false)`
  - Keep `useAuthToken` consistent with the endpoint’s auth requirements and nearby examples. This variable is almost always `true`.
- Keep methods small and consistent with surrounding files; don’t move DTOs or rename existing public types unless explicitly requested.
