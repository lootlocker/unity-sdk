using Newtonsoft.Json;
using System;
using UnityEngine;
using UnityEditor;
using System.Threading.Tasks;

#if UNITY_EDITOR
namespace LootLocker.Admin
{

    [ExecuteInEditMode]
    public class AttributionHandler : MonoBehaviour
    {
        [InitializeOnLoadMethod]
        static void SubscribeToEventOnStartup()
        {
            // Only subscribe if needed
            if (EditorPrefs.GetBool("attributionChecked") == false)
            {
                ProjectSettings.APIKeyEnteredEvent += EventFired;
            }
        }

        private static void EventFired()
        {
            // Only in Editor
            if(Application.isPlaying)
            {
                return;
            }
            // New ServerManager
            LootLockerServerManager.CheckInit();

            // Disable logging
            LootLockerConfig.current.currentDebugLevel = LootLockerConfig.DebugLevel.Off;

            string apiKey = string.IsNullOrWhiteSpace(LootLockerConfig.current.apiKey) ? "invalidGameAPIKey" : LootLockerConfig.current.apiKey;
            SendAttributionEvent(apiKey, (response) => {
                if(response.success)
                {
                    // Send attribution event
                    UnityEditor.VSAttribution.VSAttribution.SendAttributionEvent("Entered API Key", "LootLocker", response.hash);

                    DestroyImmediate(LootLockerServerManager.I.gameObject);
                    
                    // Unsubscribe
                    ProjectSettings.APIKeyEnteredEvent -= EventFired;
                    // Re-enable logging
                    LootLockerConfig.current.currentDebugLevel = LootLockerConfig.DebugLevel.All;
                }
                else
                {
                    DestroyImmediate(LootLockerServerManager.I.gameObject);

                    // Unsubscribe
                    ProjectSettings.APIKeyEnteredEvent -= EventFired;
                    // Re-enable logging
                    LootLockerConfig.current.currentDebugLevel = LootLockerConfig.DebugLevel.All;

                    // Resubscribe in 1 seconds
                    Task task = ResetAttributionCheckAfterXSeconds(1);
                }
            });
        }

        private static async Task ResetAttributionCheckAfterXSeconds(int seconds)
        {
            await Task.Delay((int)(seconds*1000f));

            EditorPrefs.SetBool("attributionChecked", false);
            ProjectSettings.APIKeyEnteredEvent += EventFired;
        }

        public static void SendAttributionEvent(string gameApiKey, Action<AttributionResponse> onComplete)
        {
            VerifyAttributionRequest verifyRequest = new VerifyAttributionRequest(gameApiKey);
            LootLockerAPIManager.Verify(verifyRequest, onComplete);
        }
    }

    public class AttributionResponse : LootLockerResponse
    {
        public string hash;
    }
    public class VerifyAttributionRequest
    {
        public string game_key;
        public VerifyAttributionRequest(string gameApiKey)
        {
            this.game_key = gameApiKey;
        }
    }

    public partial class LootLockerAPIManager
    {
        public static void Verify(VerifyAttributionRequest data, Action<AttributionResponse> onComplete)
        {
            string json = "";
            if (data == null) return;
            else json = JsonConvert.SerializeObject(data);

            EndPointClass endPoint = new EndPointClass("game/attribution/unity", LootLockerHTTPMethod.POST);

            LootLockerServerRequest.CallAPI(endPoint.endPoint, endPoint.httpMethod, json, (serverResponse) => { LootLockerResponse.Serialize(onComplete, serverResponse); }, false, LootLockerEnums.LootLockerCallerRole.Base);
        }
    }
}
#endif
