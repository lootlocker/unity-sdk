using System;
using System.Collections.Generic;
#if LOOTLOCKER_USE_NEWTONSOFTJSON
using Newtonsoft.Json;
#else
using LLlibs.ZeroDepJson;
#endif
using LootLocker.LootLockerEnums;
using LootLocker.Requests;

namespace LootLocker.LootLockerEnums
{
    /// <summary>
    /// Platforms that can be presented as a credential when linking a provider to an identity, and that can
    /// therefore appear in an identity's provider list.
    ///
    /// Member names are the wire values the backend accepts, so they are sent as-is. The values are the
    /// platform catalog slugs used by LootLocker, which are not the same catalogue as
    /// <see cref="LootLockerAccountProvider"/>.
    ///
    /// Always send the canonical member when linking. The backend accepts the aliases <c>ios</c>,
    /// <c>android</c> and <c>luna</c>, and stores whichever form was sent, but a stored alias is reported as
    /// <see cref="unknown"/> because this SDK only maps canonical slugs.
    /// </summary>
    public enum LootLockerIdentityProvider
    {
        /// <summary>
        /// A provider this SDK does not recognise, or one the response did not populate.
        ///
        /// Zero on purpose: an unpopulated or unparseable value must not land on a real provider. Both JSON
        /// backends resolve an unrecognised slug to this member.
        /// </summary>
        unknown = 0,
        steam = 1,
        psn = 2,
        apple_app_store = 3,
        google_play_store = 4,
        xbox_one = 5,
        nintendo_switch = 6,
        amazon_luna = 7,
        white_label_login = 8,
        guest = 9,
        apple_sign_in = 10,
        google_sign_in = 11,
        epic_games = 12,
        apple_game_center = 13,
        meta = 14,
        lootlocker_delegation = 15,
        twitch = 16,
        google_play_games = 17,
        discord = 18,
        /// <summary>
        /// A publisher account credential (email). Every identity created through publisher sign-in carries one,
        /// so it appears in the provider list. It is not linkable through the link endpoint.
        /// </summary>
        publisher_account = 19
    }

#if LOOTLOCKER_USE_NEWTONSOFTJSON
    /// <summary>
    /// Maps a provider slug to <see cref="LootLockerIdentityProvider"/> without throwing on a value the enum
    /// does not know.
    ///
    /// Newtonsoft's StringEnumConverter throws on an unrecognised name, which would fail the whole response.
    /// The ZeroDep backend resolves enums itself and already degrades to <see cref="LootLockerIdentityProvider.unknown"/>,
    /// so this converter exists only to give Newtonsoft the same behaviour.
    /// </summary>
    internal sealed class LootLockerIdentityProviderConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(LootLockerIdentityProvider);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return LootLockerIdentityProvider.unknown;
            }

            if (reader.TokenType != JsonToken.String)
            {
                return LootLockerIdentityProvider.unknown;
            }

            return LootLockerIdentityProviderSlugs.FromString((string)reader.Value);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue(((LootLockerIdentityProvider)value).ToString());
        }
    }
#endif
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
        /// The provider this credential belongs to.
        ///
        /// <see cref="LootLockerIdentityProvider.unknown"/> when the backend reported a slug this SDK does not
        /// map. A credential linked through a platform alias (<c>ios</c>, <c>android</c>, <c>luna</c>) also
        /// reports unknown, because this SDK only maps canonical slugs.
        /// </summary>
#if LOOTLOCKER_USE_NEWTONSOFTJSON
        [JsonConverter(typeof(LootLockerIdentityProviderConverter))]
#endif
        public LootLockerIdentityProvider provider { get; set; }
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
            if (providers == null || provider == LootLockerIdentityProvider.unknown)
            {
                return null;
            }

            foreach (var linked in providers)
            {
                if (linked != null && linked.provider == provider)
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
        /// </summary>
        /// <param name="provider">The provider to find the credential id for.</param>
        public string GetProviderCredentialId(LootLockerIdentityProvider provider)
        {
            if (providers == null || provider == LootLockerIdentityProvider.unknown)
            {
                return null;
            }

            foreach (var linked in providers)
            {
                if (linked != null && linked.provider == provider)
                {
                    return linked.id;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Helpers for converting between <see cref="LootLockerIdentityProvider"/> and the slug the backend reports.
    ///
    /// Both JSON backends resolve a provider slug by exact member name and fall back to
    /// <see cref="LootLockerIdentityProvider.unknown"/>, which is also what these helpers do.
    /// </summary>
    public static class LootLockerIdentityProviderSlugs
    {
        /// <summary>
        /// Resolves a provider slug to a provider, matching the member name exactly.
        ///
        /// Returns <see cref="LootLockerIdentityProvider.unknown"/> for any value this SDK does not map, so a
        /// provider added to the platform catalog later degrades instead of throwing or being misattributed.
        /// </summary>
        /// <param name="storedSlug">The provider slug as reported by the backend.</param>
        public static LootLockerIdentityProvider FromString(string storedSlug)
        {
            if (string.IsNullOrEmpty(storedSlug))
            {
                return LootLockerIdentityProvider.unknown;
            }

            foreach (LootLockerIdentityProvider provider in Enum.GetValues(typeof(LootLockerIdentityProvider)))
            {
                if (provider != LootLockerIdentityProvider.unknown &&
                    string.Equals(provider.ToString(), storedSlug, StringComparison.Ordinal))
                {
                    return provider;
                }
            }

            return LootLockerIdentityProvider.unknown;
        }

        /// <summary>
        /// The wire slug for a provider, which is the enumerator name.
        /// </summary>
        /// <param name="provider">The provider to get the slug for.</param>
        public static string ToSlug(LootLockerIdentityProvider provider)
        {
            return provider.ToString();
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

            // unknown is not a provider; sending it would fail the backend's platform catalog check with a 400.
            if (request.provider == LootLockerIdentityProvider.unknown)
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