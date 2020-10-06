using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker;
using LootLockerRequests;
using Newtonsoft.Json;
using System;
using System.Linq;
using UnityEngine.UI;

namespace LootLockerRequests
{

    #region GettingCollectable

    public class GettingCollectablesResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public Collectable[] collectables { get; set; }
    }

    public class Collectable
    {
        public string name { get; set; }
        public Group[] groups { get; set; }
        public int completion_percentage { get; set; }
        public Reward[] rewards { get; set; }
    }

    public class Reward 
    {
        public Asset asset { get; set; }
        public int asset_variation_id { get; set; }
        public object asset_rental_option_id { get; set; }
    }

    public class Group
    {
        public string name { get; set; }
        public int completion_percentage { get; set; }
        public Item[] items { get; set; }
        public bool grants_all_rewards { get; set; }
        public Reward[] rewards { get; set; }
    }

    public class Item 
    {
        public string name { get; set; }
        public bool collected { get; set; }
        public bool grants_all_rewards { get; set; }
        public Reward[] rewards { get; set; }
        public string url { get; set; }
        public Image preview { get; set; }
        public int downloadAttempts { get; set; }
        public File[] files { get; set; }


    }

    #endregion

    #region CollectingAnItem

    public class CollectingAnItemRequest
    {
        public string slug { get; set; }

    }

    public class CollectingAnItemResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public Collectable[] collectables { get; set; }

        public Collectable mainCollectable;

        public Group mainGroup;

        public Item mainItem;
    }



    #endregion

}

namespace LootLocker
{

    public partial class LootLockerAPIManager
    {

        public static void GettingCollectables(Action<GettingCollectablesResponse> onComplete)
        {
            EndPointClass endPoint = LootLockerEndPoints.current.gettingCollectables;

            ServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, "", (serverResponse) =>
            {
                GettingCollectablesResponse response = new GettingCollectablesResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<GettingCollectablesResponse>(serverResponse.text);
                    response.text = serverResponse.text;
                    onComplete?.Invoke(response);
                }
                else
                {
                    response.text = serverResponse.text;
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, true);
        }

        public static void CollectingAnItem(CollectingAnItemRequest data, Action<CollectingAnItemResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = LootLockerEndPoints.current.collectingAnItem;

            ServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) =>
            {
                CollectingAnItemResponse response = new CollectingAnItemResponse();
                if (string.IsNullOrEmpty(serverResponse.Error))
                {
                    response = JsonConvert.DeserializeObject<CollectingAnItemResponse>(serverResponse.text);

                    string[] collectableStrings = data.slug.Split('.');

                    string collectable = collectableStrings[0];
                    string group = collectableStrings[1];
                    string item = collectableStrings[2];

                    response.mainCollectable = response.collectables?.FirstOrDefault(x => x.name == collectable);

                    response.mainGroup = response.mainCollectable?.groups?.FirstOrDefault(x => x.name == group);

                    response.mainItem = response.mainGroup?.items?.FirstOrDefault(x => x.name == item);

                    response.text = serverResponse.text;

                    onComplete?.Invoke(response);
                }
                else
                {
                    response.text = serverResponse.text;
                    response.message = serverResponse.message;
                    response.Error = serverResponse.Error;
                    onComplete?.Invoke(response);
                }
            }, true);
        }
    }

}