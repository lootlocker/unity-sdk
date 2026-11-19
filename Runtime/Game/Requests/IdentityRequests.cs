using System;
using System.Collections.Generic;
using LootLocker.LootLockerEnums;
using LootLocker.Requests;

namespace LootLocker.LootLockerEnums
{
    /// <summary>
    /// Platforms that can be presented as a credential when linking a provider to an identity.
    ///
    /// Member names are the wire values the backend accepts, so they are sent as-is. The values are the
    /// platform catalog slugs used by LootLocker, which are not the same catalogue as
    /// <see cref="LootLockerAccountProvider"/>.
        ///
        /// The backend additionally accepts the aliases <c>ios</c> (apple_app_store), <c>android</c>
        /// (google_play_store) and <c>luna</c> (amazon_luna) for these members.
    /// </summary>
    public enum LootLockerIdentityProvider
    {
        steam = 0,
        psn = 1,
        apple_app_store = 2,
        google_play_store = 3,
        xbox_one = 4,
        nintendo_switch = 5,
        amazon_luna = 6,
        white_label_login = 7,
        guest = 8,
        apple_sign_in = 9,
        google_sign_in = 10,
        epic_games = 11,
        apple_game_center = 12,
        meta = 13,
        twitch = 14,
        google_play_games = 15,
            discord = 16,
            lootlocker_delegation = 17
    }
}

namespace LootLocker.Requests
{
    //==================================================
    // Data Definitions
    //==================================================

    /// <summary>
    /// One game in which a player of this identity has been active.
    /// </summary>
    public class LootLockerIdentityGameActivity
    {
        /// <summary>
        /// The legacy integer id of the game.
        /// </summary>
        public int game_id { get; set; }
        /// <summary>
        /// The first time a session was started for this identity in the game.
        /// </summary>
        public DateTime first_session_at { get; set; }
        /// <summary>
        /// The most recent time a session was started for this identity in the game.
        /// </summary>
        public DateTime last_session_at { get; set; }
        /// <summary>
        /// The number of sessions started for this identity in the game.
        /// </summary>
        public int session_count { get; set; }
    }

    /// <summary>
    /// A provider credential linked to an identity.
    ///
    /// <see cref="id"/> is the value to pass to <c>UnlinkProvider</c>. The provider's own user identifier is
    /// deliberately not returned: identity-scoped reads span every game the identity has played, so exposing
    /// it would leak account identifiers from credentials linked through other games.
    /// </summary>
    public class LootLockerIdentityProviderInfo
    {
        /// <summary>
        /// The ULID of the platform credential. Pass this to <c>UnlinkProvider</c>.
        /// </summary>
        public string id { get; set; }
        /// <summary>
        /// The provider slug, for example <c>steam</c>, <c>psn</c> or <c>xbox_one</c>.
        /// </summary>
        public string provider { get; set; }
        /// <summary>
        /// When the credential was linked to the identity.
        /// </summary>
        public DateTime created_at { get; set; }
    }

    /// <summary>
    /// A cross-game identity: the human behind one or more player accounts in the publisher.
    /// </summary>
    public class LootLockerIdentity
    {
        /// <summary>
        /// The ULID of the identity.
        /// </summary>
        public string id { get; set; }
        /// <summary>
        /// The legacy integer id of the publisher that owns the identity.
        /// </summary>
        public int publisher_id { get; set; }
        /// <summary>
        /// When the identity was created.
        /// </summary>
        public DateTime created_at { get; set; }
        /// <summary>
        /// The games this identity has been active in. Only populated where the request asked for game
        /// activity; otherwise an empty array.
        /// </summary>
        public LootLockerIdentityGameActivity[] game_activity { get; set; }
        /// <summary>
        /// The provider credentials linked to this identity.
        ///
        /// Only <c>GetIdentity</c> loads these. Lookup and merge share this shape but have no reason to carry
        /// them, so expect <c>null</c> there, which is distinguishable from an identity with no credentials
        /// linked (<c>[]</c>).
        /// </summary>
        public LootLockerIdentityProviderInfo[] providers { get; set; }

        /// <summary>
        /// The credential id for a provider, or null when no credential for that provider is linked.
        ///
        /// Pass the result to <c>UnlinkProvider</c>. Returns null when <see cref="providers"/> is null, which is
        /// the case for identities returned by lookup and merge.
        /// </summary>
        /// <param name="provider">The provider to find the credential id for.</param>
        public string GetProviderCredentialId(LootLockerIdentityProvider provider)
        {
            if (providers == null)
            {
                return null;
            }

            foreach (var linked in providers)
            {
                if (linked != null && LootLockerIdentityProviderSlugs.Matches(provider, linked.provider))
                {
                    return linked.id;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// One game in which both identities being merged have a live player, and therefore needs a choice of
    /// which player to keep.
    /// </summary>
    public class LootLockerIdentityPlayerConflict
    {
        /// <summary>
        /// The legacy integer id of the game the conflict is in.
        /// </summary>
        public int game_id { get; set; }
        /// <summary>
        /// The player id belonging to the identity that is kept.
        /// </summary>
        public int keep_player_id { get; set; }
        /// <summary>
        /// The player id belonging to the identity that is merged away.
        /// </summary>
        public int merge_player_id { get; set; }
    }

    /// <summary>
    /// What merging two identities would resolve, without executing anything.
    ///
    /// An empty <see cref="player_conflicts"/> means the merge needs no further choices.
    /// </summary>
    public class LootLockerIdentityMergePreview
    {
        /// <summary>
        /// The ULID of the identity the caller keeps.
        /// </summary>
        public string keep_identity_id { get; set; }
        /// <summary>
        /// The ULID of the identity that would be merged into the kept identity.
        /// </summary>
        public string merge_identity_id { get; set; }
        /// <summary>
        /// The games in which both identities have a live player. Empty when the merge resolves without a
        /// choice.
        /// </summary>
        public LootLockerIdentityPlayerConflict[] player_conflicts { get; set; }
    }

    /// <summary>
    /// The body of the 409 returned when the credential being linked already belongs to another identity.
    ///
    /// LootLocker never merges on the player's behalf. Read this, show the player the conflicts, then call
    /// <c>MergeIdentities</c> with one resolution per conflict.
    /// </summary>
    public class LootLockerIdentityLinkConflict
    {
        /// <summary>
        /// The message the server attached to the conflict.
        /// </summary>
        public string message { get; set; }
        /// <summary>
        /// The ULID of the identity that already owns the credential.
        /// </summary>
        public string conflicting_identity_id { get; set; }
        /// <summary>
        /// What a merge of the requesting identity (keep) into the conflicting identity (merge) would resolve.
        /// </summary>
        public LootLockerIdentityMergePreview merge_preview { get; set; }
    }

    //==================================================
    // Request Definitions
    //==================================================

    /// <summary>
    /// Request to look identities up by id and/or by the games they have been active in.
    ///
    /// At least one of <see cref="identity_ids"/> and <see cref="game_ids"/> must be supplied. Results are
    /// always scoped to the caller's publisher.
    /// </summary>
    public class LootLockerIdentityLookupRequest
    {
        /// <summary>
        /// Optional : The ULIDs of the identities to look up.
        /// </summary>
        public string[] identity_ids { get; set; }
        /// <summary>
        /// Optional : The legacy integer ids of games to filter the identities by. An identity matches when it
        /// has been active in the game.
        /// </summary>
        public int[] game_ids { get; set; }
        /// <summary>
        /// Optional : Whether to populate <see cref="LootLockerIdentity.game_activity"/> on each result.
        /// Defaults to false on the server.
        /// </summary>
        public bool include_game_activity { get; set; }

        public bool ShouldSerializeinclude_game_activity()
        {
            // Omit the flag entirely when false so the server default applies under both JSON backends.
            return include_game_activity;
        }

        public bool ShouldSerializeidentity_ids()
        {
            // Omit the array entirely when unset. Only ZeroDep skips nulls, so without this Newtonsoft would
            // send "identity_ids": null, which the request contract says is unset.
            return identity_ids != null;
        }

        public bool ShouldSerializegame_ids()
        {
            // Omit the array entirely when unset, for the same reason as identity_ids.
            return game_ids != null;
        }
    }

    /// <summary>
    /// Request to record a provider credential against the player's identity.
    /// </summary>
    public class LootLockerIdentityLinkRequest
    {
        /// <summary>
        /// The provider platform the credential belongs to.
        /// </summary>
        public LootLockerIdentityProvider provider { get; set; }
        /// <summary>
        /// The provider's identifier for the player, for example the Steam ID.
        /// </summary>
        public string provider_uid { get; set; }
    }

    /// <summary>
    /// Request to preview what merging another identity into the current one would resolve.
    /// </summary>
    public class LootLockerIdentityMergePreviewRequest
    {
        /// <summary>
        /// The ULID of the identity to merge into the authenticated identity.
        /// </summary>
        public string merge_identity_id { get; set; }
    }

    /// <summary>
    /// The choice of which player to keep in one game where both identities have a live player.
    /// </summary>
    public class LootLockerIdentityMergeResolution
    {
        /// <summary>
        /// The legacy integer id of the game this resolution is for.
        /// </summary>
        public int game_id { get; set; }
        /// <summary>
        /// The player to keep in this game. Must equal either the keep or the merge player id of the conflict.
        /// </summary>
        public int keep_player_id { get; set; }
    }

    /// <summary>
    /// Request to merge another identity into the current one.
    ///
    /// This is destructive and never runs automatically. Get the conflicts from a merge preview (or from the
    /// 409 of a link), let the player choose, then send one resolution per conflict.
    /// </summary>
    public class LootLockerIdentityMergeRequest
    {
        /// <summary>
        /// The ULID of the identity to merge into the authenticated identity.
        /// </summary>
        public string merge_identity_id { get; set; }
        /// <summary>
        /// One entry per game listed in the merge preview's player_conflicts. A missing resolution for a
        /// conflict fails the merge with 400 and writes nothing.
        /// </summary>
        public LootLockerIdentityMergeResolution[] resolutions { get; set; }
        /// <summary>
        /// Optional : The provider whose credential conflict led to this merge, recorded in the merge log.
        /// </summary>
        public string trigger_provider { get; set; }
        /// <summary>
        /// Optional : The provider credential that led to this merge, recorded in the merge log.
        /// </summary>
        public string trigger_provider_uid { get; set; }
    }

    //==================================================
    // Response Definitions
    //==================================================

    /// <summary>
    /// Response containing a single identity.
    /// </summary>
    public class LootLockerIdentityResponse : LootLockerResponse
    {
        /// <summary>
        /// The ULID of the identity.
        /// </summary>
        public string id { get; set; }
        /// <summary>
        /// The legacy integer id of the publisher that owns the identity.
        /// </summary>
        public int publisher_id { get; set; }
        /// <summary>
        /// When the identity was created.
        /// </summary>
        public DateTime created_at { get; set; }
        /// <summary>
        /// The games this identity has been active in.
        /// </summary>
        public LootLockerIdentityGameActivity[] game_activity { get; set; }
        /// <summary>
        /// The provider credentials linked to this identity. Each entry's <see cref="LootLockerIdentityProviderInfo.id"/>
        /// is the value to pass to <c>UnlinkProvider</c>.
        /// </summary>
        public LootLockerIdentityProviderInfo[] providers { get; set; }

        /// <summary>
        /// The credential id for a provider, or null when no credential for that provider is linked.
        ///
        /// Pass the result to <c>UnlinkProvider</c>. Returns null when <see cref="providers"/> is null, which is
        /// the case for identities returned by lookup and merge.
        ///
        /// The match accepts the platform alias as well as the canonical slug, because the backend accepts
        /// either when a credential is linked and stores whatever the client sent.
        /// </summary>
        /// <param name="provider">The provider to find the credential id for.</param>
        public string GetProviderCredentialId(LootLockerIdentityProvider provider)
        {
            if (providers == null)
            {
                return null;
            }

            foreach (var linked in providers)
            {
                if (linked != null && LootLockerIdentityProviderSlugs.Matches(provider, linked.provider))
                {
                    return linked.id;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Helpers for comparing a provider enum member against the slug the backend stored.
    /// </summary>
    public static class LootLockerIdentityProviderSlugs
    {
        /// <summary>
        /// The alias the backend accepts and may therefore have stored for a provider, or null when the
        /// provider has none.
        /// </summary>
        public static string AliasFor(LootLockerIdentityProvider provider)
        {
            switch (provider)
            {
                case LootLockerIdentityProvider.apple_app_store: return "ios";
                case LootLockerIdentityProvider.google_play_store: return "android";
                case LootLockerIdentityProvider.amazon_luna: return "luna";
                default: return null;
            }
        }

        /// <summary>
        /// Whether a stored slug refers to the given provider, accepting either the canonical slug or its alias.
        /// </summary>
        public static bool Matches(LootLockerIdentityProvider provider, string storedSlug)
        {
            if (string.IsNullOrEmpty(storedSlug))
            {
                return false;
            }

            if (storedSlug == provider.ToString())
            {
                return true;
            }

            var alias = AliasFor(provider);
            return alias != null && storedSlug == alias;
        }
    }

    /// <summary>
    /// Response containing zero or more identities.
    /// </summary>
    public class LootLockerIdentityLookupResponse : LootLockerResponse
    {
        /// <summary>
        /// The identities matching the lookup.
        /// </summary>
        public LootLockerIdentity[] identities { get; set; }
    }

    /// <summary>
    /// Response returned after linking a provider credential.
    /// </summary>
    public class LootLockerIdentityLinkResponse : LootLockerResponse
    {
        /// <summary>
        /// True when a new credential was recorded, false when the identity already owned it.
        /// </summary>
        public bool created { get; set; }
        /// <summary>
        /// Set when the request was answered with 409 because the credential belongs to another identity.
        /// Null on every other outcome.
        /// </summary>
        public LootLockerIdentityLinkConflict conflict { get; set; }
    }

    /// <summary>
    /// Response containing a merge preview.
    /// </summary>
    public class LootLockerIdentityMergePreviewResponse : LootLockerResponse
    {
        /// <summary>
        /// The ULID of the identity the caller keeps.
        /// </summary>
        public string keep_identity_id { get; set; }
        /// <summary>
        /// The ULID of the identity that would be merged into the kept identity.
        /// </summary>
        public string merge_identity_id { get; set; }
        /// <summary>
        /// The games in which both identities have a live player.
        /// </summary>
        public LootLockerIdentityPlayerConflict[] player_conflicts { get; set; }
    }

    /// <summary>
    /// Response returned after merging two identities, containing the surviving identity.
    /// </summary>
    public class LootLockerIdentityMergeResponse : LootLockerResponse
    {
        /// <summary>
        /// The ULID of the surviving identity.
        /// </summary>
        public string id { get; set; }
        /// <summary>
        /// The legacy integer id of the publisher that owns the identity.
        /// </summary>
        public int publisher_id { get; set; }
        /// <summary>
        /// When the surviving identity was created.
        /// </summary>
        public DateTime created_at { get; set; }
        /// <summary>
        /// The games the surviving identity has been active in.
        /// </summary>
        public LootLockerIdentityGameActivity[] game_activity { get; set; }
        /// <summary>
        /// The provider credentials linked to the surviving identity.
        ///
        /// Merge does not load these, so this is <c>null</c> rather than an empty array. Call
        /// <c>GetIdentity</c> after a merge if the caller needs the current credential list.
        /// </summary>
        public LootLockerIdentityProviderInfo[] providers { get; set; }
    }
}

//==================================================
// API Class Definition
//==================================================
namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void GetIdentity(string forPlayerWithUlid, Action<LootLockerIdentityResponse> onComplete)
        {
            var endPoint = LootLockerEndPoints.getIdentity;
            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint, endPoint.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, callerRole: endPoint.callerRole);
        }

        public static void LookupIdentities(string forPlayerWithUlid, LootLockerIdentityLookupRequest request, Action<LootLockerIdentityLookupResponse> onComplete)
        {
            if (request == null || (!HasItems(request.identity_ids) && !HasItems(request.game_ids)))
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerIdentityLookupResponse>(forPlayerWithUlid));
                return;
            }

            var endPoint = LootLockerEndPoints.lookupIdentities;
            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint, endPoint.httpMethod, LootLockerJson.SerializeObject(request), (serverResponse) =>
            {
                // The lookup answers with a bare JSON array of identities, which cannot be deserialized into a
                // response object: Newtonsoft throws and ZeroDep rejects the shape. The response is therefore
                // built from the transport metadata and only the array is parsed from the body.
                if (serverResponse == null)
                {
                    onComplete?.Invoke(LootLockerResponseFactory.ClientError<LootLockerIdentityLookupResponse>("Unknown error, please check your internet connection.", forPlayerWithUlid));
                    return;
                }

                var response = new LootLockerIdentityLookupResponse
                {
                    success = serverResponse.success,
                    statusCode = serverResponse.statusCode,
                    text = serverResponse.text,
                    errorData = serverResponse.errorData,
                    requestContext = serverResponse.requestContext,
                    EventId = serverResponse.EventId,
                    identities = new LootLockerIdentity[0]
                };

                if (response.success && !string.IsNullOrEmpty(serverResponse.text))
                {
                    response.identities = LootLockerJson.DeserializeObjectArray<LootLockerIdentity>(serverResponse.text);
                }

                onComplete?.Invoke(response);
            }, callerRole: endPoint.callerRole);
        }

        public static void LinkProvider(string forPlayerWithUlid, LootLockerIdentityLinkRequest request, Action<LootLockerIdentityLinkResponse> onComplete)
        {
            if (request == null || string.IsNullOrEmpty(request.provider_uid))
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerIdentityLinkResponse>(forPlayerWithUlid));
                return;
            }

            var endPoint = LootLockerEndPoints.linkIdentityProvider;
            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint, endPoint.httpMethod, LootLockerJson.SerializeObject(request), (serverResponse) =>
            {
                var response = LootLockerResponse.Deserialize<LootLockerIdentityLinkResponse>(serverResponse);

                // A 409 carries the merge offer the player must act on, so it is surfaced on the response
                // instead of only in errorData.
                if (!response.success && response.statusCode == 409 && !string.IsNullOrEmpty(serverResponse?.text))
                {
                    if (LootLockerJson.TryDeserializeObject(serverResponse.text, out LootLockerIdentityLinkConflict conflict))
                    {
                        response.conflict = conflict;
                    }
                }

                onComplete?.Invoke(response);
            }, callerRole: endPoint.callerRole);
        }

        public static void UnlinkProvider(string forPlayerWithUlid, string credentialId, Action<LootLockerResponse> onComplete)
        {
            if (string.IsNullOrEmpty(credentialId))
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerResponse>(forPlayerWithUlid));
                return;
            }

            var endPoint = LootLockerEndPoints.unlinkIdentityProvider.WithPathParameter(credentialId);
            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint, LootLockerEndPoints.unlinkIdentityProvider.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, callerRole: LootLockerEndPoints.unlinkIdentityProvider.callerRole);
        }

        public static void PreviewIdentityMerge(string forPlayerWithUlid, LootLockerIdentityMergePreviewRequest request, Action<LootLockerIdentityMergePreviewResponse> onComplete)
        {
            if (request == null || string.IsNullOrEmpty(request.merge_identity_id))
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerIdentityMergePreviewResponse>(forPlayerWithUlid));
                return;
            }

            var endPoint = LootLockerEndPoints.previewIdentityMerge;
            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint, endPoint.httpMethod, LootLockerJson.SerializeObject(request), (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, callerRole: endPoint.callerRole);
        }

        public static void MergeIdentities(string forPlayerWithUlid, LootLockerIdentityMergeRequest request, Action<LootLockerIdentityMergeResponse> onComplete)
        {
            if (request == null || string.IsNullOrEmpty(request.merge_identity_id))
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerIdentityMergeResponse>(forPlayerWithUlid));
                return;
            }

            var endPoint = LootLockerEndPoints.mergeIdentities;
            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint, endPoint.httpMethod, LootLockerJson.SerializeObject(request), (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }, callerRole: endPoint.callerRole);
        }

        private static bool HasItems<T>(T[] values)
        {
            return values != null && values.Length > 0;
        }
    }
}