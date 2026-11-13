using System;
using System.Net;
using LootLocker.Requests;

namespace LootLocker.Requests
{
    //==================================================
    // Request Definitions
    //==================================================

    /// <summary>
    /// Request to equip an inventory stack into an equipment slot on an entity.
    /// </summary>
    public class LootLockerEquipmentEquipRequest
    {
        /// <summary>
        /// The entity the loadout hangs off. Only "player" is resolvable today.
        /// </summary>
        public string entity_type { get; set; }
        /// <summary>
        /// The ULID of the entity. For the "player" entity type this is the player ULID.
        /// </summary>
        public string source_id { get; set; }
        /// <summary>
        /// The ULID of the equipment slot to equip into.
        /// </summary>
        public string slot_id { get; set; }
        /// <summary>
        /// The ULID of the inventory stack to equip.
        /// </summary>
        public string inventory_id { get; set; }
        /// <summary>
        /// Optional : The amount to equip. Defaults to the whole stack, capped by the slot's max_items.
        /// </summary>
        public int? count { get; set; }
        /// <summary>
        /// Optional : Whether a stack already occupying the slot should be auto-unequipped. Defaults to true on the server.
        /// Pass false to receive a conflict instead of replacing the occupant.
        /// </summary>
        public bool? replace { get; set; }
    }

    //==================================================
    // Data Definitions
    //==================================================

    /// <summary>
    /// The summary of the item template an equipped stack was created from.
    /// </summary>
    public class LootLockerEquipmentItemTemplate
    {
        /// <summary>
        /// The ULID of the item template.
        /// </summary>
        public string id { get; set; }
        /// <summary>
        /// The name of the item template.
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// Whether an inventory row of this template may be deleted.
        /// </summary>
        public bool deletable { get; set; }
    }

    /// <summary>
    /// An equipment slot definition belonging to an equipment group.
    /// </summary>
    public class LootLockerEquipmentSlot
    {
        /// <summary>
        /// The ULID of the slot.
        /// </summary>
        public string id { get; set; }
        /// <summary>
        /// The stable machine key of the slot, unique within its group.
        /// </summary>
        public string slot_key { get; set; }
        /// <summary>
        /// The display name of the slot.
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// The maximum number of items the slot holds. Equipping more than this splits the stack.
        /// </summary>
        public int max_items { get; set; }
        /// <summary>
        /// The tags an item template must share at least one of for the slot to accept it.
        /// </summary>
        public string[] tags { get; set; }
    }

    /// <summary>
    /// A stack currently attached to an equipment slot.
    /// </summary>
    public class LootLockerEquipmentStack
    {
        /// <summary>
        /// The equipment entry ULID. Use this to unequip the stack.
        /// </summary>
        public string entry_id { get; set; }
        /// <summary>
        /// The ULID of the inventory row holding the equipped items.
        /// </summary>
        public string inventory_id { get; set; }
        /// <summary>
        /// The number of items currently in the slot.
        /// </summary>
        public int count { get; set; }
        /// <summary>
        /// A summary of the item template this stack was created from.
        /// </summary>
        public LootLockerEquipmentItemTemplate item_template { get; set; }
        /// <summary>
        /// False when the item template's tags and the slot's tags no longer intersect. The stack stays
        /// attached and is flagged rather than silently removed.
        /// </summary>
        public bool valid { get; set; }
        /// <summary>
        /// True when the attached count exceeds the slot's max_items, for example after max_items was lowered.
        /// </summary>
        public bool over_limit { get; set; }
        /// <summary>
        /// When the stack was attached to the slot.
        /// </summary>
        public DateTime mounted_at { get; set; }
    }

    /// <summary>
    /// An equipment slot together with the stack attached to it, if any.
    /// </summary>
    public class LootLockerEquipmentSlotAndStack
    {
        /// <summary>
        /// The slot definition.
        /// </summary>
        public LootLockerEquipmentSlot slot { get; set; }
        /// <summary>
        /// The attached stack, or null when the slot is empty.
        /// </summary>
        public LootLockerEquipmentStack stack { get; set; }
    }

    /// <summary>
    /// An equipment group as reported by a loadout read. A group is bound to one entity type and owns its slots.
    /// </summary>
    public class LootLockerEquipmentGroup
    {
        /// <summary>
        /// The ULID of the group.
        /// </summary>
        public string id { get; set; }
        /// <summary>
        /// The stable machine key of the group.
        /// </summary>
        public string group_key { get; set; }
        /// <summary>
        /// The display name of the group.
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// The entity type this group serves. Only "player" is resolvable today.
        /// </summary>
        public string entity_type { get; set; }
        /// <summary>
        /// The group's slots with the stack attached to each.
        /// </summary>
        public LootLockerEquipmentSlotAndStack[] slots { get; set; }
    }

    /// <summary>
    /// An unequipped inventory stack that a slot would accept.
    /// </summary>
    public class LootLockerEquipmentCandidate
    {
        /// <summary>
        /// The ULID of the inventory stack.
        /// </summary>
        public string inventory_id { get; set; }
        /// <summary>
        /// A summary of the item template this stack was created from.
        /// </summary>
        public LootLockerEquipmentItemTemplate item_template { get; set; }
        /// <summary>
        /// The item type of the stack.
        /// </summary>
        public string item_type { get; set; }
        /// <summary>
        /// The number of items in the inventory stack.
        /// </summary>
        public int count { get; set; }
        /// <summary>
        /// The amount an equip would attach right now, which is count capped at the slot's max_items.
        /// </summary>
        public int equippable { get; set; }
    }

    //==================================================
    // Response Definitions
    //==================================================

    /// <summary>
    /// Response containing a player's loadout: the groups for the entity's type, each with its slots and attached stacks.
    /// </summary>
    public class LootLockerEquipmentLoadoutResponse : LootLockerResponse
    {
        /// <summary>
        /// The equipment groups serving the entity's type, paged over groups.
        /// </summary>
        public LootLockerEquipmentGroup[] groups { get; set; }
        /// <summary>
        /// Pagination information for the groups.
        /// </summary>
        public LootLockerExtendedPagination pagination { get; set; }
    }

    /// <summary>
    /// Response containing the inventory stacks that a slot would accept for the calling player.
    /// </summary>
    public class LootLockerEquipmentSlotCandidatesResponse : LootLockerResponse
    {
        /// <summary>
        /// The slot the candidates were listed for.
        /// </summary>
        public LootLockerEquipmentSlot slot { get; set; }
        /// <summary>
        /// The caller's unequipped stacks whose item template shares a tag with the slot.
        /// </summary>
        public LootLockerEquipmentCandidate[] candidates { get; set; }
        /// <summary>
        /// Pagination information for the candidates.
        /// </summary>
        public LootLockerExtendedPagination pagination { get; set; }
    }

    /// <summary>
    /// Response describing what an equip did.
    /// </summary>
    public class LootLockerEquipmentEquipResponse : LootLockerResponse
    {
        /// <summary>
        /// The equipment entry ULID, used to unequip the stack.
        /// </summary>
        public string entry_id { get; set; }
        /// <summary>
        /// The ULID of the slot the stack was equipped into.
        /// </summary>
        public string slot_id { get; set; }
        /// <summary>
        /// The ULID of the attached inventory row. Equals the requested stack when it fitted whole,
        /// otherwise a freshly created split row.
        /// </summary>
        public string inventory_id { get; set; }
        /// <summary>
        /// The amount now attached to the slot.
        /// </summary>
        public int equipped { get; set; }
        /// <summary>
        /// The amount left on the source inventory stack.
        /// </summary>
        public int remainder { get; set; }
        /// <summary>
        /// The entry that was auto-unequipped to make room, if any. Null when no stack was replaced.
        /// </summary>
        public string replaced_entry_id { get; set; }
    }
}

//==================================================
// API Class Definition
//==================================================
namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void GetEquipmentLoadout(string forPlayerWithUlid, int page, int perPage, Action<LootLockerEquipmentLoadoutResponse> onComplete)
        {
            var endPoint = LootLockerEndPoints.getEquipmentLoadout;
            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint + BuildEquipmentPageQuery(page, perPage), endPoint.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetEntityEquipmentLoadout(string forPlayerWithUlid, string entityType, string sourceId, int page, int perPage, Action<LootLockerEquipmentLoadoutResponse> onComplete)
        {
            var endPoint = LootLockerEndPoints.getEntityEquipmentLoadout.WithPathParameters(entityType, sourceId);
            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint + BuildEquipmentPageQuery(page, perPage), LootLockerEndPoints.getEntityEquipmentLoadout.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetOtherPlayersEquipmentLoadout(string forPlayerWithUlid, string playerPublicUid, int page, int perPage, Action<LootLockerEquipmentLoadoutResponse> onComplete)
        {
            var endPoint = LootLockerEndPoints.getOtherPlayersEquipmentLoadout.WithPathParameter(playerPublicUid);
            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint + BuildEquipmentPageQuery(page, perPage), LootLockerEndPoints.getOtherPlayersEquipmentLoadout.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void ListEquipmentSlotCandidates(string forPlayerWithUlid, string slotId, int page, int perPage, Action<LootLockerEquipmentSlotCandidatesResponse> onComplete)
        {
            var endPoint = LootLockerEndPoints.listEquipmentSlotCandidates.WithPathParameter(slotId);
            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint + BuildEquipmentPageQuery(page, perPage), LootLockerEndPoints.listEquipmentSlotCandidates.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void EquipItemToEquipmentSlot(string forPlayerWithUlid, LootLockerEquipmentEquipRequest request, Action<LootLockerEquipmentEquipResponse> onComplete)
        {
            if (request == null || string.IsNullOrEmpty(request.entity_type) || string.IsNullOrEmpty(request.source_id) || string.IsNullOrEmpty(request.slot_id) || string.IsNullOrEmpty(request.inventory_id))
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerEquipmentEquipResponse>(forPlayerWithUlid));
                return;
            }

            var endPoint = LootLockerEndPoints.equipToEquipmentSlot;
            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint.endPoint, endPoint.httpMethod, LootLockerJson.SerializeObject(request), onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void UnequipItemFromEquipmentSlot(string forPlayerWithUlid, string entryId, Action<LootLockerResponse> onComplete)
        {
            if (string.IsNullOrEmpty(entryId))
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerResponse>(forPlayerWithUlid));
                return;
            }

            var endPoint = LootLockerEndPoints.unequipFromEquipmentSlot.WithPathParameter(entryId);
            LootLockerServerRequest.CallAPI(forPlayerWithUlid, endPoint, LootLockerEndPoints.unequipFromEquipmentSlot.httpMethod, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        /// <summary>
        /// Builds the page/per_page query string shared by every equipment read. Both fall back to the
        /// server defaults (page 1, 10 per page) when not given.
        /// </summary>
        private static string BuildEquipmentPageQuery(int page, int perPage)
        {
            var queryParams = new LootLocker.Utilities.HTTP.QueryParamaterBuilder();
            if (page > 0)
            {
                queryParams.Add("page", page);
            }
            if (perPage > 0)
            {
                queryParams.Add("per_page", perPage);
            }
            return queryParams.Build();
        }
    }
}
