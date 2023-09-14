using System;
using LootLocker.Requests;

namespace LootLocker.Requests
{

    public class LootLockerHeroResponse : LootLockerResponse
    {
        LootLockerHero hero;
    }


    public class LootLockerPlayerHeroResponse : LootLockerResponse
    {
        LootLockerPlayerHero hero;
    }

    public class LootLockerGameHeroResponse : LootLockerResponse
    {
        public LootLockerHero[] game_heroes;
    }

    public class LootLockerListHeroResponse : LootLockerResponse
    {
        public LootLockerPlayerHero[] heroes;
    }

    public class LootLockerHeroLoadoutResponse : LootLockerResponse
    {
        public int variation_id { get; set; }
        public int instance_id { get; set; }
        public string mounted_at { get; set; }
        public LootLockerCommonAsset[] asset { get; set; }
        public LootLockerRental rental { get; set; }
    }

    public class LootLockerHero
    {
        public int hero_id { get; set; }
        public int character_type_id { get; set; }
        public int character_type_name { get; set; }
        public string name { get; set; }
        public bool player_has_hero { get; set; }
        LootLockerCommonAsset asset { get; set; }

    }

    public class LootLockerPlayerHero
    {

        public int id { get; set; }
        public int hero_id { get; set; }
        public int instance_id { get; set; }
        public string hero_name { get; set; }
        public string character_name { get; set; }
        public bool is_default { get; set; }
        public LootLockerCommonAsset asset { get; set; }
    }

    public class LootLockerCreateHeroRequest
    {
        public int hero_id { get; set; }
        public string name { get; set; }
        public bool is_default { get; set; }
    }

    public class LootLockerCreateHeroWithVariationRequest
    {
        public int hero_id { get; set; }
        public string name { get; set; }
        public int asset_variation_id { get; set; }
        public bool is_default { get; set; }
    }

    public class LootLockerUpdateHeroRequest
    {
        public string name { get; set; }
        public bool is_default { get; set; }
    }

    public class LootLockerAddAssetToHeroLoadoutRequest
    {
        public int hero_id { get; set; }
        public int asset_instance_id { get; set; }
    }

    public class LootLockerAddAssetVariationToHeroLoadoutRequest
    {
        public int hero_id { get; set; }
        public int asset_id { get; set; }
        public int asset_variation_id { get; set; }
    }

    public class LootLockerHeroLoadout
    {
        public int variation_id { get; set; }
        public int instance_id { get; set; }
        public string mounted_at { get; set; }
        public LootLockerCommonAsset[] asset { get; set; }
        public LootLockerRental rental { get; set; }
    }

    public class LootLockerHeroAssetInstance
    {
        public int instance_id { get; set; }
    }

    public class LootLockerHeroAsset
    {
        public int asset_id { get; set; }
        public int asset_variation_id { get; set; }
    }

}

namespace LootLocker
{
    public partial class LootLockerAPIManager
    {

        public static void GetGameHeroes(Action<LootLockerGameHeroResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getGameHeroes;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void ListPlayerHeroes(Action<LootLockerListHeroResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.listPlayerHeroes;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse);  });
        }

        public static void ListOtherPlayersHeroesBySteamID64(int SteamID64, Action<LootLockerPlayerHeroResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.listOtherPlayersHeroesBySteamID64;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, SteamID64.ToString(), (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void CreateHero(LootLockerCreateHeroRequest data, Action<LootLockerHeroLoadoutResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.createHero;
            if (data == null)
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerHeroLoadoutResponse>());
                return;
            }
            string json = LootLockerJson.SerializeObject(data);

            string getVariable = endPoint.endPoint;
            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void CreateHeroWithVariation(LootLockerCreateHeroWithVariationRequest data, Action<LootLockerHeroLoadoutResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.createHero;
            if (data == null)
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerHeroLoadoutResponse>());
                return;
            }
            string json = LootLockerJson.SerializeObject(data);

            string getVariable = endPoint.endPoint;
            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetHero(int HeroID, Action<LootLockerPlayerHeroResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getHero;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, HeroID.ToString(), (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetOtherPlayersDefaultHeroBySteamID64(int steamID64, Action<LootLockerPlayerHeroResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getOtherPlayersDefaultHeroBySteamID64;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, steamID64.ToString(), (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void UpdateHero(LootLockerGetRequest lootLockerGetRequest,  LootLockerUpdateHeroRequest data, Action<LootLockerPlayerHeroResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.updateHero;

            if (data == null)
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerPlayerHeroResponse>());
                return;
            }
            string json = LootLockerJson.SerializeObject(data);

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void DeleteHero(int HeroID, Action<LootLockerPlayerHeroResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.deleteHero;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, HeroID.ToString(), (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetHeroInventory(int HeroID, Action<LootLockerInventoryResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getHeroInventory;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, HeroID.ToString(), (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetHeroLoadout(Action<LootLockerHeroLoadoutResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getHeroLoadout;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void GetOtherPlayersHeroLoadout(int HeroID, Action<LootLockerHeroLoadoutResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.getOtherPlayersHeroLoadout;

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, HeroID.ToString(), (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void AddAssetToHeroLoadout(LootLockerAddAssetToHeroLoadoutRequest data, Action<LootLockerHeroLoadoutResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.addAssetToHeroLoadout;

            if (data == null)
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerHeroLoadoutResponse>());
                return;
            }
            string json = LootLockerJson.SerializeObject(data);

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void AddAssetVariationToHeroLoadout(LootLockerAddAssetVariationToHeroLoadoutRequest data, Action<LootLockerHeroLoadoutResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.addAssetVariationToHeroLoadout;

            if (data == null)
            {
                onComplete?.Invoke(LootLockerResponseFactory.InputUnserializableError<LootLockerHeroLoadoutResponse>());
                return;
            }
            string json = LootLockerJson.SerializeObject(data);

            string getVariable = endPoint.endPoint;

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); });
        }

        public static void RemoveAssetFromHeroLoadout(LootLockerGetRequest lootLockerGetRequest, Action<LootLockerHeroLoadoutResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.removeAssetFromHeroLoadout;

            string getVariable = string.Format(endPoint.endPoint, lootLockerGetRequest.getRequests[0], lootLockerGetRequest.getRequests[1]);

            LootLockerServerRequest.CallAPI(getVariable, endPoint.httpMethod, null, (serverResponse) => { LootLockerResponse.Deserialize(onComplete, serverResponse); }); ;

        }

    }
}

