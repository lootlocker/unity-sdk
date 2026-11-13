using LootLocker;
using LootLocker.Requests;
using NUnit.Framework;

namespace LootLockerTests.PlayMode
{
    public class EquipmentTests
    {
        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Equipment_DeserializingLoadout_MapsSlotsAndFlags()
        {
            // Given
            const string json = "{\"groups\":[{\"id\":\"01K9AAAAAAAAAAAAAAAAAAAAAA\",\"group_key\":\"player_gear\",\"name\":\"Player gear\",\"entity_type\":\"player\",\"slots\":[{\"slot\":{\"id\":\"01K9BBBBBBBBBBBBBBBBBBBBBB\",\"slot_key\":\"head\",\"name\":\"Head\",\"max_items\":1,\"tags\":[\"headgear\"]},\"stack\":{\"entry_id\":\"01K9CCCCCCCCCCCCCCCCCCCCCC\",\"inventory_id\":\"01K9DDDDDDDDDDDDDDDDDDDDDD\",\"count\":1,\"item_template\":{\"id\":\"01K9EEEEEEEEEEEEEEEEEEEEEE\",\"name\":\"Helmet\",\"deletable\":false},\"valid\":true,\"over_limit\":false,\"mounted_at\":\"2026-10-19T20:27:03Z\"}},{\"slot\":{\"id\":\"01K9FFFFFFFFFFFFFFFFFFFFFF\",\"slot_key\":\"belt\",\"name\":\"Belt\",\"max_items\":10,\"tags\":[\"toolbelt\"]},\"stack\":null}]}],\"pagination\":{\"errors\":[],\"per_page\":10,\"offset\":0,\"total\":1,\"last_page\":1,\"current_page\":1,\"next_page\":null,\"prev_page\":null}}";

            // When
            var response = LootLockerJson.DeserializeObject<LootLockerEquipmentLoadoutResponse>(json);

            // Then
            Assert.IsNotNull(response);
            Assert.AreEqual(1, response.groups.Length);
            var group = response.groups[0];
            Assert.AreEqual("player_gear", group.group_key);
            Assert.AreEqual("player", group.entity_type);
            Assert.AreEqual(2, group.slots.Length);

            var equipped = group.slots[0];
            Assert.AreEqual("head", equipped.slot.slot_key);
            Assert.AreEqual(1, equipped.slot.max_items);
            Assert.AreEqual("headgear", equipped.slot.tags[0]);
            Assert.IsNotNull(equipped.stack);
            Assert.AreEqual("01K9CCCCCCCCCCCCCCCCCCCCCC", equipped.stack.entry_id);
            Assert.AreEqual("Helmet", equipped.stack.item_template.name);
            Assert.IsTrue(equipped.stack.valid);
            Assert.IsFalse(equipped.stack.over_limit);

            Assert.IsNull(group.slots[1].stack, "An empty slot must deserialize to a null stack");
            Assert.AreEqual(10, group.slots[1].slot.max_items);

            Assert.IsNotNull(response.pagination);
            Assert.AreEqual(10, response.pagination.per_page);
            Assert.AreEqual(1, response.pagination.total);
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Equipment_DeserializingEquipResponse_ReportsSplitAndReplace()
        {
            // Given
            const string json = "{\"entry_id\":\"01K9CCCCCCCCCCCCCCCCCCCCCC\",\"slot_id\":\"01K9BBBBBBBBBBBBBBBBBBBBBB\",\"inventory_id\":\"01K9DDDDDDDDDDDDDDDDDDDDDD\",\"equipped\":3,\"remainder\":7,\"replaced_entry_id\":\"01K9GGGGGGGGGGGGGGGGGGGGGG\"}";

            // When
            var response = LootLockerJson.DeserializeObject<LootLockerEquipmentEquipResponse>(json);

            // Then
            Assert.AreEqual(3, response.equipped);
            Assert.AreEqual(7, response.remainder);
            Assert.AreEqual("01K9GGGGGGGGGGGGGGGGGGGGGG", response.replaced_entry_id);
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Equipment_DeserializingEquipResponseWithNullReplacedEntry_LeavesNull()
        {
            // Given
            const string json = "{\"entry_id\":\"01K9CCCCCCCCCCCCCCCCCCCCCC\",\"slot_id\":\"01K9BBBBBBBBBBBBBBBBBBBBBB\",\"inventory_id\":\"01K9DDDDDDDDDDDDDDDDDDDDDD\",\"equipped\":2,\"remainder\":0,\"replaced_entry_id\":null}";

            // When
            var response = LootLockerJson.DeserializeObject<LootLockerEquipmentEquipResponse>(json);

            // Then
            Assert.AreEqual(2, response.equipped);
            Assert.AreEqual(0, response.remainder);
            Assert.IsNull(response.replaced_entry_id);
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Equipment_DeserializingSlotCandidates_MapsEquippableAmount()
        {
            // Given
            const string json = "{\"slot\":{\"id\":\"01K9BBBBBBBBBBBBBBBBBBBBBB\",\"slot_key\":\"belt\",\"name\":\"Belt\",\"max_items\":10,\"tags\":[\"toolbelt\"]},\"candidates\":[{\"inventory_id\":\"01K9DDDDDDDDDDDDDDDDDDDDDD\",\"item_template\":{\"id\":\"01K9EEEEEEEEEEEEEEEEEEEEEE\",\"name\":\"Potion\",\"deletable\":true},\"item_type\":\"stackable\",\"count\":42,\"equippable\":10}],\"pagination\":{\"errors\":[],\"per_page\":10,\"offset\":0,\"total\":1,\"last_page\":1,\"current_page\":1,\"next_page\":null,\"prev_page\":null}}";

            // When
            var response = LootLockerJson.DeserializeObject<LootLockerEquipmentSlotCandidatesResponse>(json);

            // Then
            Assert.AreEqual("belt", response.slot.slot_key);
            Assert.AreEqual(1, response.candidates.Length);
            Assert.AreEqual(42, response.candidates[0].count);
            Assert.AreEqual(10, response.candidates[0].equippable, "equippable is the stack count capped at max_items");
            Assert.AreEqual("stackable", response.candidates[0].item_type);
            Assert.IsTrue(response.candidates[0].item_template.deletable);
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Equipment_SerializingEquipRequest_OmitsUnsetOptionalFields()
        {
            // Given: count and replace are optional on the server, and an explicit 0/false would be
            // rejected or would override the server default. They must not be sent at all, under
            // either JSON backend.
            var request = new LootLockerEquipmentEquipRequest
            {
                entity_type = "player",
                source_id = "01K9AAAAAAAAAAAAAAAAAAAAAA",
                slot_id = "01K9BBBBBBBBBBBBBBBBBBBBBB",
                inventory_id = "01K9DDDDDDDDDDDDDDDDDDDDDD"
            };

            // When
            var json = LootLockerJson.SerializeObject(request).Replace(" ", "");

            // Then
            StringAssert.Contains("\"entity_type\":\"player\"", json);
            StringAssert.Contains("\"inventory_id\":\"01K9DDDDDDDDDDDDDDDDDDDDDD\"", json);
            StringAssert.DoesNotContain("\"count\"", json, "An unset count must not be sent at all");
            StringAssert.DoesNotContain("\"replace\"", json, "An unset replace must not be sent at all");
        }

        [Test, Category("LootLocker"), Category("LootLockerCI"), Category("LootLockerCIFast")]
        public void Equipment_SerializingEquipRequest_IncludesSetOptionalFields()
        {
            // Given
            var request = new LootLockerEquipmentEquipRequest
            {
                entity_type = "player",
                source_id = "01K9AAAAAAAAAAAAAAAAAAAAAA",
                slot_id = "01K9BBBBBBBBBBBBBBBBBBBBBB",
                inventory_id = "01K9DDDDDDDDDDDDDDDDDDDDDD",
                count = 3,
                replace = false
            };

            // When
            var json = LootLockerJson.SerializeObject(request).Replace(" ", "");

            // Then
            StringAssert.Contains("\"count\":3", json);
            StringAssert.Contains("\"replace\":false", json);
        }
    }
}
