using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker.Requests;
using UnityEngine.UI;
using System;

public class PlayerTypeLeaderboard : MonoBehaviour
{
    public InputField scoreInputField;
    public Text infoText;
    public Text playerIDText;
    public Text leaderboardTop10Text;
    public Text leaderboardCenteredText;

    /*
    * leaderboardKey or leaderboardID can be used.
    * leaderboardKey can be the same between stage and live /development mode on/off.
    * So if you use the key instead of the ID, you don't need to change any code when switching development_mode.
    */
    string leaderboardKey = "genericLeaderboard";
    // int leaderboardID = 4705;

    string memberID;

    // Start is called before the first frame update
    void Start()
    {
        /* 
         * Override settings to use the Example game setup
         */
        LootLockerSettingsOverrider.OverrideSettings();
        StartGuestSession();
    }

    public void StartGuestSession()
    {
        /* Start guest session without an identifier.
         * LootLocker will create an identifier for the user and store it in PlayerPrefs.
         * If you want to create a new player when testing, you can use PlayerPrefs.DeleteKey("LootLockerGuestPlayerID");
         */
        PlayerPrefs.DeleteKey("LootLockerGuestPlayerID");
        LootLockerSDKManager.StartGuestSession((response) =>
        {
            if (response.success)
            {
                infoText.text = "Guest session started";
                playerIDText.text = "Player ID:" + response.player_id.ToString();
                memberID = response.player_id.ToString();
                UpdateLeaderboardTop10();
                UpdateLeaderboardCentered();
            }
            else
            {
                infoText.text = "Error" + response.Error;
            }
        });
    }

    public void UploadScore()
    {
        /*
         * Get the players System language and send it as metadata
         */
        string metadata = Application.systemLanguage.ToString();

        /*
         * Since this is a player leaderboard, member_id is not needed, 
         * the logged in user is the one that will upload the score.
         */
        LootLockerSDKManager.SubmitScore("", int.Parse(scoreInputField.text), leaderboardKey, metadata, (response) =>
        {
            if (response.success)
            {
                infoText.text = "Player score was submitted";
                /*
                 * Update the leaderboards when the new score was sent so we can see them
                 */
                UpdateLeaderboardCentered();
                UpdateLeaderboardTop10();
            }
            else
            {
                infoText.text = "Error submitting score:" + response.Error;
            }
        });
    }
    void UpdateLeaderboardCentered()
    {
        LootLockerSDKManager.GetMemberRank(leaderboardKey, memberID, (memberResponse) =>
        {
            if (memberResponse.success)
            {
                if(memberResponse.rank == 0)
                {
                    leaderboardCenteredText.text = "Upload score to see centered";
                    return;
                }
                int playerRank = memberResponse.rank;
                int count = 10;
                /*
                 * Set "after" to 5 below and 4 above the rank for the current player.
                 * "after" means where to start fetch the leaderboard entries.
                 */
                int after = playerRank < 6 ? 0 : playerRank - 5;

                LootLockerSDKManager.GetScoreList(leaderboardKey, count, after, (scoreResponse) =>
                {
                    if (scoreResponse.success)
                    {
                        infoText.text = "Centered scores updated";

                        /*
                         * Format the leaderboard
                         */
                        string leaderboardText = "";
                        for (int i = 0; i < scoreResponse.items.Length; i++)
                        {
                            LootLockerLeaderboardMember currentEntry = scoreResponse.items[i];

                            /*
                             * Highlight the player with rich text
                             */
                            if(currentEntry.rank == playerRank)
                            {
                                leaderboardText += "<color=yellow>";
                            }

                            leaderboardText += currentEntry.rank + ".";
                            leaderboardText += currentEntry.player.id;
                            leaderboardText += " - ";
                            leaderboardText += currentEntry.score;
                            leaderboardText += " - ";
                            leaderboardText += currentEntry.metadata;

                            /*
                            * End highlighting the player
                            */
                            if (currentEntry.rank == playerRank)
                            {
                                leaderboardText += "</color>";
                            }
                            leaderboardText += "\n";
                        }
                        leaderboardCenteredText.text = leaderboardText;
                    }
                    else
                    {
                        infoText.text = "Could not update centered scores:"+ scoreResponse.Error;
                    }
                });
            }
            else
            {
                infoText.text = "Could not get member rank:" + memberResponse.Error;
            }
        });
    }

    void UpdateLeaderboardTop10()
    {
        LootLockerSDKManager.GetScoreList(leaderboardKey, 10, (response) =>
        {
            if (response.success)
            {
                infoText.text = "Top 10 leaderboard updated";
                infoText.text = "Centered scores updated";

                /*
                 * Format the leaderboard
                 */
                string leaderboardText = "";
                for (int i = 0; i < response.items.Length; i++)
                {
                    LootLockerLeaderboardMember currentEntry = response.items[i];
                    leaderboardText += currentEntry.rank + ".";
                    leaderboardText += currentEntry.player.id;
                    leaderboardText += " - ";
                    leaderboardText += currentEntry.score;
                    leaderboardText += " - ";
                    leaderboardText += currentEntry.metadata;
                    leaderboardText += "\n";
                }
                leaderboardTop10Text.text = leaderboardText;
            }
            else
            {
                infoText.text = "Error updating Top 10 leaderboard";
            }
        });
    }
}
