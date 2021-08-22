using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLocker.Requests;
using Newtonsoft.Json;
using System;
using System.Linq;
using UnityEngine.UI;

namespace LootLocker.Requests
{

    #region GettingCollectable

    public class LootLockerGettingCollectablesResponse : LootLockerResponse
    {
        
        public LootLockerCollectable[] collectables { get; set; }
    }

    public class LootLockerCollectable
    {
        public string name { get; set; }
        public LootLockerGroup[] groups { get; set; }
        public int completion_percentage { get; set; }
        public LootLockerReward[] rewards { get; set; }
    }

    public class LootLockerReward 
    {
        public LootLockerCommonAsset asset { get; set; }
        public int asset_variation_id { get; set; }
        public object asset_rental_option_id { get; set; }
    }

    public class LootLockerGroup
    {
        public string name { get; set; }
        public int completion_percentage { get; set; }
        public LootLockerItem[] items { get; set; }
        public bool grants_all_rewards { get; set; }
        public LootLockerReward[] rewards { get; set; }
    }

    public class LootLockerItem 
    {
        public string name { get; set; }
        public bool collected { get; set; }
        public bool grants_all_rewards { get; set; }
        public LootLockerReward[] rewards { get; set; }
        public string url { get; set; }
        public Image preview { get; set; }
        public int downloadAttempts { get; set; }
        public LootLockerFile[] files { get; set; }


    }

    #endregion

    #region CollectingAnItem

    public class LootLockerCollectingAnItemRequest
    {
        public string slug { get; set; }

    }

    public class LootLockerCollectingAnItemResponse : LootLockerResponse
    {
        
        public LootLockerCollectable[] collectables { get; set; }

        public LootLockerCollectable mainCollectable;

        public LootLockerGroup mainGroup;

        public LootLockerItem mainItem;
    }



    #endregion

}

namespace LootLocker
{

    public partial class LootLockerAPIManager
    {

        public static void GettingCollectables(Action<LootLockerGettingCollectablesResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.gettingCollectables;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, "", (serverResponse) =>
            {
                LootLockerGettingCollectablesResponse response = new LootLockerGettingCollectablesResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                    response = JsonConvert.DeserializeObject<LootLockerGettingCollectablesResponse>(serverResponse.text);

                //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                     response.success = serverResponse.success;
            response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }

        public static void CollectingAnItem(LootLockerCollectingAnItemRequest data, Action<LootLockerCollectingAnItemResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.collectingAnItem;

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                LootLockerCollectingAnItemResponse response = new LootLockerCollectingAnItemResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<LootLockerCollectingAnItemResponse>(serverResponse.text);
                    string[] collectableStrings = data.slug.Split('.');

                    string collectable = collectableStrings[0];
                    string group = collectableStrings[1];
                    string item = collectableStrings[2];

                    response.mainCollectable = response.collectables?.FirstOrDefault(x => x.name == collectable);
                    response.mainGroup = response.mainCollectable?.groups?.FirstOrDefault(x => x.name == group);
                    response.mainItem = response.mainGroup?.items?.FirstOrDefault(x => x.name == item);
                }

                //LootLockerSDKManager.DebugMessage(serverResponse.text, !string.IsNullOrEmpty(serverResponse.Error));
                response.text = serverResponse.text;
                     response.success = serverResponse.success;
            response.Error = serverResponse.Error; response.statusCode = serverResponse.statusCode;
                onComplete?.Invoke(response);
            }, true);
        }
    }

}