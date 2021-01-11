using LootLocker.Admin.Requests;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LootLocker.Admin
{
    public class AdminEventsTest : MonoBehaviour
    {
        [Header("Creating Event")]
        public bool sendAssetID;
        public bool sendPosterPath, sendRounds, sendRoundLength, sendCompletionBonus,
            sendDifficultyName, sendDifficultyMultiplier, sendTimeScoreMultiplier, sendGoals,
            sendCheckpoints, sendFilters;

        public LootLockerCreatingEventRequest eventToCreate;

        [Header("Updating Event")]
        [Header("---------------------------")]
        public int eventID;
        public bool protectName, sendAssetID_U, sendPosterPath_U, sendRounds_U, sendRoundLength_U, sendCompletionBonus_U,
        sendDifficultyName_U, sendDifficultyMultiplier_U, sendTimeScoreMultiplier_U, sendGoals_U,
        sendCheckpoints_U, sendFilters_U;

        public LootLockerCreatingEventRequest UpdatedEventData;

        [Header("Getting All Events")]
        [Header("---------------------------")]
        public int gameID;

        [ContextMenu("CreatingEvent")]
        public void CreatingEvent()
        {
            LootLockerSDKAdminManager.CreatingEvent(eventToCreate.GetCreatingEventRequestDictionary(sendAssetID, sendPosterPath, sendRounds, sendRoundLength, sendCompletionBonus,
            sendDifficultyName, sendDifficultyMultiplier, sendTimeScoreMultiplier, sendGoals,
            sendCheckpoints, sendFilters),

            (response) =>
            {
                if (response.success)
                {
                    Debug.LogError("Successful created event: " + response.text);
                }
                else
                {
                    Debug.LogError("failed to create event: " + response.Error);
                }
            });
        }

        [ContextMenu("UpdatingEvent")]
        public void UpdatingEvent()
        {
            LootLockerSDKAdminManager.UpdatingEvent(eventID, UpdatedEventData.GetUpdatingEventRequestDictionary(protectName, sendAssetID_U, sendPosterPath_U, sendRounds_U, sendRoundLength_U, sendCompletionBonus_U,
            sendDifficultyName_U, sendDifficultyMultiplier_U, sendTimeScoreMultiplier_U, sendGoals_U,
            sendCheckpoints_U, sendFilters_U),

            (response) =>
            {
                if (response.success)
                {
                    Debug.LogError("Successful updated event: " + response.text);
                }
                else
                {
                    Debug.LogError("failed to update event: " + response.Error);
                }
            });
        }

        [ContextMenu("GettingAllEvents")]
        public void GettingAllEvents()
        {
            LootLockerSDKAdminManager.GettingAllEvents(gameID,

            (response) =>
            {
                if (response.success)
                {
                    Debug.LogError("Successful got events: " + response.text);
                }
                else
                {
                    Debug.LogError("failed to get events: " + response.Error);
                }
            });
        }

    }
}