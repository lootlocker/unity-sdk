using LootLocker;
using LootLocker.LootLockerEnums;
using LootLocker.Requests;
using NUnit.Framework;

namespace LootLockerTests.PlayMode
{
    public class ReferralTests
    {
        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Referral_DeserializingInvite_MapsProgramsAndCap()
        {
            // Given
            const string json = "{\"code\":\"PLAYER-UID\",\"programs\":[{\"program_key\":\"invite_a_friend\",\"name\":\"Invite a friend\",\"qualifier\":\"session_started\",\"referral_count\":3,\"referrer_cap\":10},{\"program_key\":\"uncapped\",\"name\":\"Uncapped\",\"qualifier\":\"game_asserted\",\"referral_count\":0,\"referrer_cap\":null}]}";

            // When
            var response = LootLockerJson.DeserializeObject<LootLockerReferralInviteResponse>(json);

            // Then
            Assert.IsNotNull(response);
            Assert.AreEqual("PLAYER-UID", response.code);
            Assert.AreEqual(2, response.programs.Length);

            var capped = response.programs[0];
            Assert.AreEqual("invite_a_friend", capped.program_key);
            Assert.AreEqual("Invite a friend", capped.name);
            Assert.AreEqual("session_started", capped.qualifier);
            Assert.AreEqual(3, capped.referral_count);
            Assert.AreEqual(10, capped.referrer_cap);

            Assert.IsNull(response.programs[1].referrer_cap, "An uncapped program must deserialize referrer_cap to null");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Referral_DeserializingList_MapsStatusAndQualifiedAt()
        {
            // Given
            const string json = "{\"referrals\":[{\"id\":\"01K9AAAAAAAAAAAAAAAAAAAAAA\",\"program_key\":\"invite_a_friend\",\"program_name\":\"Invite a friend\",\"referee_public_uid\":\"FRIEND-UID\",\"status\":\"qualified\",\"claimed_at\":\"2026-10-19T20:27:03Z\",\"qualified_at\":\"2026-10-19T20:30:00Z\"},{\"id\":\"01K9BBBBBBBBBBBBBBBBBBBBBB\",\"program_key\":\"invite_a_friend\",\"program_name\":\"Invite a friend\",\"referee_public_uid\":null,\"status\":\"claimed\",\"claimed_at\":\"2026-10-20T08:00:00Z\",\"qualified_at\":null}],\"pagination\":{\"errors\":[],\"per_page\":10,\"offset\":0,\"total\":2,\"last_page\":1,\"current_page\":1,\"next_page\":null,\"prev_page\":null}}";

            // When
            var response = LootLockerJson.DeserializeObject<LootLockerReferralListResponse>(json);

            // Then
            Assert.IsNotNull(response);
            Assert.AreEqual(2, response.referrals.Length);

            var qualified = response.referrals[0];
            Assert.AreEqual("01K9AAAAAAAAAAAAAAAAAAAAAA", qualified.id);
            Assert.AreEqual("invite_a_friend", qualified.program_key);
            Assert.AreEqual("FRIEND-UID", qualified.referee_public_uid);
            Assert.AreEqual("qualified", qualified.status);
            Assert.IsNotNull(qualified.qualified_at);

            var pending = response.referrals[1];
            Assert.AreEqual("claimed", pending.status);
            Assert.IsNull(pending.qualified_at, "A pending referral must deserialize qualified_at to null");
            Assert.IsNull(pending.referee_public_uid, "A removed referee must deserialize referee_public_uid to null");

            Assert.IsNotNull(response.pagination);
            Assert.AreEqual(2, response.pagination.total);
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Referral_SerializingSessionOptionals_IncludesReferralFields()
        {
            // Given
            var optionals = new LootLockerSessionOptionals
            {
                referral_code = "FRIEND-UID",
                referral_program_key = "invite_a_friend"
            };

            // When
            var json = LootLockerJson.SerializeObject(optionals).Replace(" ", "");

            // Then
            StringAssert.Contains("\"referral_code\":\"FRIEND-UID\"", json);
            StringAssert.Contains("\"referral_program_key\":\"invite_a_friend\"", json);
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Referral_NotificationSource_IsRecognised()
        {
            // Given
            const string json = "{\"notifications\":[{\"id\":\"01K9AAAAAAAAAAAAAAAAAAAAAA\",\"player_id\":\"01K9BBBBBBBBBBBBBBBBBBBBBB\",\"notification_type\":\"pull.reward.acquired\",\"priority\":\"medium\",\"source\":\"referral\",\"read\":false,\"custom\":false,\"content\":{\"context\":[{\"key\":\"currencies\",\"value\":\"01K9CCCCCCCCCCCCCCCCCCCCCC\"}],\"body\":null}}],\"pagination\":{\"errors\":[],\"per_page\":10,\"offset\":0,\"total\":1,\"last_page\":1,\"current_page\":1,\"next_page\":null,\"prev_page\":null}}";

            // When
            var response = LootLockerJson.DeserializeObject<LootLockerListNotificationsResponse>(json);
            response.PopulateConvenienceStructures();

            // Then
            Assert.AreEqual(1, response.Notifications.Length);
            Assert.AreEqual(LootLockerNotificationSource.referral, response.Notifications[0].SourceEnum,
                "A referral notification must not fall back to the unknown source");
        }
    }
}
