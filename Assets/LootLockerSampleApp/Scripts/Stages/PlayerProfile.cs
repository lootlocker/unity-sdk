using LootLocker.Requests;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Newtonsoft.Json;
using LootLocker;

namespace LootLockerDemoApp
{
    public class PlayerProfile : MonoBehaviour
    {
        public Text username;
        public Text playerId;
        public Text className;
        public Text credits;
        public Text level;
        public Text characterName;
        public string creditsSprite = "Credits";
        public string xpSprite = "Xp";
        public Text message;
        public PlayerDataObject playerDataObject;

        public void UpdateScreen(LootLockerSessionResponse sessionResponse)
        {
          //  if (sessionResponse == null) return;
            username.text = playerDataObject.playerName;
            playerId.text = playerDataObject.session.player_id.ToString();
            className.text = playerDataObject.lootLockerCharacter.type;
            credits.text = playerDataObject.session.account_balance.ToString();
            characterName.text = playerDataObject.lootLockerCharacter.name;

            level.text = playerDataObject.session.level.ToString();
            if (message != null)
                message.text = "";
            LootLockerSDKManager.GetMessages((response) =>
            {
                LoadingManager.HideLoadingScreen();
                if (response.success)
                {
                    if (message != null)
                        message.text = response.messages.Length > 0 ? response.messages.First().title : "";
                }
                else
                {
                    Debug.LogError("failed to get all messages: " + response.Error);
                }
            });
        }

        public void Grant250XP()
        {
            List<LootLockerRewardObject> rewardObjects = new List<LootLockerRewardObject>();
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("XP", "250");
            PopupSystem.ShowPopup("XP Reward", data, "Continue", () =>
            {
                LoadingManager.ShowLoadingScreen();
                LootLockerSDKManager.TriggeringAnEvent("250 XP", (response) =>
                {
                    Debug.Log("Response: " + response.message);
                    if (response.success)
                    {
                    //if (response.check_grant_notifications)
                    //{
                    LootLockerSDKManager.GetAssetNotification((res) =>
                        {
                            if (res.success)
                            {
                                for (int i = 0; i < res.objects.Length; i++)
                                {
                                    if (res.objects[i].acquisition_source == "reward_level_up")
                                    {
                                        rewardObjects.Add(res.objects[i]);
                                    }
                                }
                            }
                        });
                    //   }
                    SelectPlayer(Grant.XP, rewardObjects);
                    }
                    else
                    {
                        Close();
                    }
                });
            }, url: xpSprite);
        }

        private void SelectPlayer(Grant grant, List<LootLockerRewardObject> rewardObjects = null)
        {
            string header = "";
            string normalTextMessage = "";
            Dictionary<string, string> data = new Dictionary<string, string>();
            string icon = grant == Grant.Credits ? creditsSprite : xpSprite;
            LootLockerSDKManager.StartSession(LootLockerConfig.current.deviceID, (response) =>
            {
                LoadingManager.HideLoadingScreen();
                if (response.success)
                {
                    playerDataObject.SaveSession(response);
                    header = "Success";

                    if (grant == Grant.Credits)
                    {
                        normalTextMessage = "Successfully granted Credits.";
                    }
                    if (grant == Grant.XP)
                    {
                        normalTextMessage = "Successfully granted XP.";
                    }

                    data.Clear();
                //Preparing data to display or error messages we have
                data.Add("1", normalTextMessage);
                    StagesManager.instance.GoToStage(StagesManager.StageID.Home, response);
                    if (rewardObjects != null && rewardObjects.Count > 0)
                    {
                        for (int i = 0; i < rewardObjects.Count; i++)
                        {
                            PopupData PopupData = new PopupData();
                            PopupData.buttonText = "Ok";
                            PopupData.url = rewardObjects[i].asset.links.thumbnail;
                            PopupData.withKey = true;
                            PopupData.normalText = new Dictionary<string, string>() { { "Reward", rewardObjects[i].asset.name } };
                            PopupData.header = "You got a reward";
                            PopupSystem.ShowScheduledPopup(PopupData);
                        }
                    }
                }
                else
                {
                    header = "Failed";

                    string correctedResponse = response.Error.First() == '{' ? response.Error : response.Error.Substring(response.Error.IndexOf('{'));
                    ResponseError errorMessage = new ResponseError();
                    errorMessage = JsonConvert.DeserializeObject<ResponseError>(correctedResponse);

                    normalTextMessage = errorMessage.messages[0];

                    data.Clear();
                //Preparing data to display or error messages we have
                data.Add("1", normalTextMessage);
                }
                PopupSystem.ShowApprovalFailPopUp(header, data, icon);
            });
        }

        public void Close()
        {
            LoadingManager.HideLoadingScreen();
            PopupSystem.CloseNow();
        }

        public void Grant1000XP()
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("Credits", "1000");
            PopupSystem.ShowPopup("Credits Reward", data, "Continue", () =>
            {
                LoadingManager.ShowLoadingScreen();
                LootLockerSDKManager.TriggeringAnEvent("1000 Credits", (response) =>
                {
                    if (response.success)
                    {
                        SelectPlayer(Grant.Credits);
                    }
                    else
                    {
                        Close();
                    }
                });
            }, url: creditsSprite);
        }

        public void OpenPlayerStorage()
        {
            LoadingManager.ShowLoadingScreen();
            StagesManager.instance.GoToStage(StagesManager.StageID.Storage, null);
        }

        private enum Grant
        {
            XP,
            Credits
        }
    }
}