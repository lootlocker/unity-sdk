using LootLocker.Requests;
using System;

namespace LootLocker.Requests
{
    [Serializable]
    public class LootLockerMapsResponse : LootLockerResponse
    {
        public LootLockerMap[] maps { get; set; }
    }

    [Serializable]
    public class LootLockerMap
    {
        public int map_id { get; set; }
        public int asset_id { get; set; }
        public LootLockerSpawn_Points[] spawn_points { get; set; }
        public bool player_access { get; set; }
    }

    [Serializable]
    public class LootLockerSpawn_Points
    {
        public int asset_id { get; set; }
        public string position { get; set; }
        public string rotation { get; set; }
        public LootLockerCamera[] cameras { get; set; }
        public bool player_access { get; set; }
    }

    [Serializable]
    public class LootLockerCamera
    {
        public string position { get; set; }
        public string rotation { get; set; }
    }
}


namespace LootLocker
{
    public partial class LootLockerAPIManager
    {
        public static void GetAllMaps(Action<LootLockerMapsResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.gettingAllMaps;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, onComplete: (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }
    }
}