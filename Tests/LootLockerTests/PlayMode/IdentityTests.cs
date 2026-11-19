using LootLocker;
using LootLocker.LootLockerEnums;
using LootLocker.Requests;
using NUnit.Framework;

namespace LootLockerTests.PlayMode
{
    public class IdentityTests
    {
        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Identity_DeserializingIdentity_MapsGameActivity()
        {
            // Given
            const string json = "{\"id\":\"01K8Q3KH8N4R2V6W3Z0P5M9XDT\",\"publisher_id\":42,\"created_at\":\"2026-10-19T20:27:03Z\",\"game_activity\":[{\"game_id\":1234,\"first_session_at\":\"2026-10-19T20:27:03Z\",\"last_session_at\":\"2026-10-20T08:15:00Z\",\"session_count\":7}]}";

            // When
            var response = LootLockerJson.DeserializeObject<LootLockerIdentityResponse>(json);

            // Then
            Assert.IsNotNull(response);
            Assert.AreEqual("01K8Q3KH8N4R2V6W3Z0P5M9XDT", response.id);
            Assert.AreEqual(42, response.publisher_id);
            Assert.AreEqual(2026, response.created_at.Year);
            Assert.AreEqual(1, response.game_activity.Length);
            Assert.AreEqual(1234, response.game_activity[0].game_id);
            Assert.AreEqual(7, response.game_activity[0].session_count);
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Identity_DeserializingMergePreview_MapsPlayerConflicts()
        {
            // Given
            const string json = "{\"keep_identity_id\":\"01K8Q3KH8N4R2V6W3Z0P5M9XDT\",\"merge_identity_id\":\"01K8Q3V6ZC9S2W4G7K3M8X0BQR\",\"player_conflicts\":[{\"game_id\":1234,\"keep_player_id\":1001,\"merge_player_id\":1002}]}";

            // When
            var response = LootLockerJson.DeserializeObject<LootLockerIdentityMergePreviewResponse>(json);

            // Then
            Assert.AreEqual("01K8Q3KH8N4R2V6W3Z0P5M9XDT", response.keep_identity_id);
            Assert.AreEqual("01K8Q3V6ZC9S2W4G7K3M8X0BQR", response.merge_identity_id);
            Assert.AreEqual(1, response.player_conflicts.Length);
            Assert.AreEqual(1234, response.player_conflicts[0].game_id);
            Assert.AreEqual(1001, response.player_conflicts[0].keep_player_id);
            Assert.AreEqual(1002, response.player_conflicts[0].merge_player_id);
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Identity_DeserializingMergePreviewWithNoConflicts_LeavesEmptyArray()
        {
            // Given
            const string json = "{\"keep_identity_id\":\"01K8Q3KH8N4R2V6W3Z0P5M9XDT\",\"merge_identity_id\":\"01K8Q3V6ZC9S2W4G7K3M8X0BQR\",\"player_conflicts\":[]}";

            // When
            var response = LootLockerJson.DeserializeObject<LootLockerIdentityMergePreviewResponse>(json);

            // Then
            Assert.IsNotNull(response.player_conflicts);
            Assert.AreEqual(0, response.player_conflicts.Length, "A merge with no conflicts needs no resolution");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Identity_DeserializingLinkConflict_CarriesMergeOffer()
        {
            // Given: the 409 body the backend returns, which is not an ErrorBody so carries no trace_id/code.
            const string json = "{\"message\":\"This platform is already linked to another account\",\"conflicting_identity_id\":\"01K8Q3V6ZC9S2W4G7K3M8X0BQR\",\"merge_preview\":{\"keep_identity_id\":\"01K8Q3KH8N4R2V6W3Z0P5M9XDT\",\"merge_identity_id\":\"01K8Q3V6ZC9S2W4G7K3M8X0BQR\",\"player_conflicts\":[]}}";

            // When
            var conflict = LootLockerJson.DeserializeObject<LootLockerIdentityLinkConflict>(json);

            // Then
            Assert.AreEqual("01K8Q3V6ZC9S2W4G7K3M8X0BQR", conflict.conflicting_identity_id);
            Assert.IsNotNull(conflict.merge_preview, "The merge offer is always present on a 409");
            Assert.AreEqual("01K8Q3KH8N4R2V6W3Z0P5M9XDT", conflict.merge_preview.keep_identity_id);
            Assert.AreEqual(0, conflict.merge_preview.player_conflicts.Length);
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Identity_SerializingLookupRequest_OmitsUnsetFilters()
        {
            // Given: the backend rejects a lookup with no filter, and omits game_activity unless asked for.
            var request = new LootLockerIdentityLookupRequest
            {
                identity_ids = new[] { "01K8Q3KH8N4R2V6W3Z0P5M9XDT" }
            };

            // When
            var json = LootLockerJson.SerializeObject(request).Replace(" ", "");

            // Then
            StringAssert.Contains("\"identity_ids\":[\"01K8Q3KH8N4R2V6W3Z0P5M9XDT\"]", json);
            StringAssert.DoesNotContain("\"game_ids\"", json, "An unset game_ids must not be sent at all");
            StringAssert.DoesNotContain("\"include_game_activity\"", json, "A false include_game_activity must not be sent, so the server default applies");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Identity_SerializingLookupRequest_IncludesGameActivityWhenEnabled()
        {
            // Given
            var request = new LootLockerIdentityLookupRequest
            {
                game_ids = new[] { 1234 },
                include_game_activity = true
            };

            // When
            var json = LootLockerJson.SerializeObject(request).Replace(" ", "");

            // Then
            StringAssert.Contains("\"game_ids\":[1234]", json);
            StringAssert.Contains("\"include_game_activity\":true", json);
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Identity_SerializingMergeRequest_MapsResolutions()
        {
            // Given
            var request = new LootLockerIdentityMergeRequest
            {
                merge_identity_id = "01K8Q3V6ZC9S2W4G7K3M8X0BQR",
                resolutions = new[]
                {
                    new LootLockerIdentityMergeResolution { game_id = 1234, keep_player_id = 1001 }
                },
                trigger_provider = "steam",
                trigger_provider_uid = "76561198000000000"
            };

            // When
            var json = LootLockerJson.SerializeObject(request).Replace(" ", "");

            // Then
            StringAssert.Contains("\"merge_identity_id\":\"01K8Q3V6ZC9S2W4G7K3M8X0BQR\"", json);
            StringAssert.Contains("\"game_id\":1234", json);
            StringAssert.Contains("\"keep_player_id\":1001", json);
            StringAssert.Contains("\"trigger_provider\":\"steam\"", json);
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Identity_DeserializingIdentity_MapsProviders()
        {
            // Given: the providers list carries the credential id a client needs for UnlinkProvider, and
            // deliberately omits the provider's own user identifier.
            const string json = "{\"id\":\"01K8Q3KH8N4R2V6W3Z0P5M9XDT\",\"publisher_id\":42,\"created_at\":\"2026-10-19T20:27:03Z\",\"game_activity\":[],\"providers\":[{\"id\":\"01K9CCCCCCCCCCCCCCCCCCCCCC\",\"provider\":\"steam\",\"created_at\":\"2026-10-19T20:27:03Z\"},{\"id\":\"01K9DDDDDDDDDDDDDDDDDDDDDD\",\"provider\":\"xbox_one\",\"created_at\":\"2026-10-20T08:15:00Z\"}]}";

            // When
            var response = LootLockerJson.DeserializeObject<LootLockerIdentityResponse>(json);

            // Then
            Assert.IsNotNull(response.providers);
            Assert.AreEqual(2, response.providers.Length);
            Assert.AreEqual("01K9CCCCCCCCCCCCCCCCCCCCCC", response.providers[0].id);
            Assert.AreEqual("steam", response.providers[0].provider);
            Assert.AreEqual("01K9DDDDDDDDDDDDDDDDDDDDDD", response.providers[1].id);
            Assert.AreEqual("xbox_one", response.providers[1].provider);
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Identity_DeserializingIdentityWithEmptyProviders_LeavesEmptyArray()
        {
            // Given: an identity with no credentials linked answers [], which is distinguishable from null.
            const string json = "{\"id\":\"01K8Q3KH8N4R2V6W3Z0P5M9XDT\",\"publisher_id\":42,\"created_at\":\"2026-10-19T20:27:03Z\",\"game_activity\":[],\"providers\":[]}";

            // When
            var response = LootLockerJson.DeserializeObject<LootLockerIdentityResponse>(json);

            // Then
            Assert.IsNotNull(response.providers, "An identity with no credentials answers [], not null");
            Assert.AreEqual(0, response.providers.Length);
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Identity_GetProviderCredentialId_ReturnsIdForLinkedProvider()
        {
            // Given
            const string json = "{\"id\":\"01K8Q3KH8N4R2V6W3Z0P5M9XDT\",\"publisher_id\":42,\"created_at\":\"2026-10-19T20:27:03Z\",\"game_activity\":[],\"providers\":[{\"id\":\"01K9CCCCCCCCCCCCCCCCCCCCCC\",\"provider\":\"steam\",\"created_at\":\"2026-10-19T20:27:03Z\"}]}";
            var response = LootLockerJson.DeserializeObject<LootLockerIdentityResponse>(json);

            // When/Then
            Assert.AreEqual("01K9CCCCCCCCCCCCCCCCCCCCCC", response.GetProviderCredentialId(LootLockerIdentityProvider.steam));
            Assert.IsNull(response.GetProviderCredentialId(LootLockerIdentityProvider.xbox_one), "An unlinked provider has no credential id");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Identity_DeserializingLookupIdentity_LeavesProvidersNull()
        {
            // Given: lookup does not load credentials, so providers is null rather than [].
            const string json = "[{\"id\":\"01K8Q3KH8N4R2V6W3Z0P5M9XDT\",\"publisher_id\":42,\"created_at\":\"2026-10-19T20:27:03Z\",\"game_activity\":[],\"providers\":null}]";

            // When
            var identities = LootLockerJson.DeserializeObjectArray<LootLockerIdentity>(json);

            // Then
            Assert.AreEqual(1, identities.Length);
            Assert.IsNull(identities[0].providers, "Lookup does not load credentials");
            Assert.IsNull(identities[0].GetProviderCredentialId(LootLockerIdentityProvider.steam), "A null providers list yields no credential id");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Identity_GetProviderCredentialId_AcceptsStoredAlias()
        {
            // Given: the backend accepts the aliases ios/android/luna when linking and stores whatever the client
            // sent, so a credential recorded through an alias carries the alias as its provider slug.
            const string json = "{\"id\":\"01K8Q3KH8N4R2V6W3Z0P5M9XDT\",\"publisher_id\":42,\"created_at\":\"2026-10-19T20:27:03Z\",\"game_activity\":[],\"providers\":[{\"id\":\"01K9CCCCCCCCCCCCCCCCCCCCCC\",\"provider\":\"ios\",\"created_at\":\"2026-10-19T20:27:03Z\"},{\"id\":\"01K9DDDDDDDDDDDDDDDDDDDDDD\",\"provider\":\"android\",\"created_at\":\"2026-10-19T20:27:03Z\"}]}";
            var response = LootLockerJson.DeserializeObject<LootLockerIdentityResponse>(json);

            // When/Then
            Assert.AreEqual("01K9CCCCCCCCCCCCCCCCCCCCCC", response.GetProviderCredentialId(LootLockerIdentityProvider.apple_app_store));
            Assert.AreEqual("01K9DDDDDDDDDDDDDDDDDDDDDD", response.GetProviderCredentialId(LootLockerIdentityProvider.google_play_store));
            Assert.IsNull(response.GetProviderCredentialId(LootLockerIdentityProvider.amazon_luna), "luna is not linked here");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Identity_DeserializingLookupArray_MapsIdentities()
        {
            // Given: the lookup answers with a bare JSON array, not an object, so it must be parsed as an array.
            const string json = "[{\"id\":\"01K8Q3KH8N4R2V6W3Z0P5M9XDT\",\"publisher_id\":42,\"created_at\":\"2026-10-19T20:27:03Z\",\"game_activity\":[]},{\"id\":\"01K8Q3V6ZC9S2W4G7K3M8X0BQR\",\"publisher_id\":42,\"created_at\":\"2026-10-20T08:15:00Z\",\"game_activity\":[]}]";

            // When
            var identities = LootLockerJson.DeserializeObjectArray<LootLockerIdentity>(json);

            // Then
            Assert.IsNotNull(identities);
            Assert.AreEqual(2, identities.Length);
            Assert.AreEqual("01K8Q3KH8N4R2V6W3Z0P5M9XDT", identities[0].id);
            Assert.AreEqual(42, identities[0].publisher_id);
            Assert.AreEqual("01K8Q3V6ZC9S2W4G7K3M8X0BQR", identities[1].id);
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Identity_DeserializingLookupArray_HandlesEmptyArray()
        {
            // Given
            const string json = "[]";

            // When
            var identities = LootLockerJson.DeserializeObjectArray<LootLockerIdentity>(json);

            // Then
            Assert.IsNotNull(identities);
            Assert.AreEqual(0, identities.Length);
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Identity_ProviderEnumMemberNames_MatchBackendPlatformSlugs()
        {
            // Given/When/Then: the enum member name is the wire value, so these must not drift from the
            // backend platform catalog. Enum.GetName avoids a cast through the numeric value.
            Assert.AreEqual("steam", nameof(LootLockerIdentityProvider.steam));
            Assert.AreEqual("psn", nameof(LootLockerIdentityProvider.psn));
            Assert.AreEqual("white_label_login", nameof(LootLockerIdentityProvider.white_label_login));
            Assert.AreEqual("apple_app_store", nameof(LootLockerIdentityProvider.apple_app_store));
            Assert.AreEqual("nintendo_switch", nameof(LootLockerIdentityProvider.nintendo_switch));
            Assert.AreEqual("google_play_games", nameof(LootLockerIdentityProvider.google_play_games));
        }
    }
}