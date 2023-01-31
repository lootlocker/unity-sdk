using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker.Requests;
using UnityEngine.UI;
using System;

namespace LootLocker {
public class GenericTypeLeaderboard : MonoBehaviour
{
    public InputField scoreInputField;
    public InputField playerNameInputField;
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
                //UpdateLeaderboardCentered();
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
         * Generic leaderboard, 
         * metadata is used for the name and a unique identifier tied to the memberID
         * is used for a player to upload as many scores as they want with any name they want
         * ensuring that every new score gets its' own post on the leaderboard.
         */
        string infiniteScores = memberID + GetAndIncrementScoreCharacters();
        LootLockerSDKManager.SubmitScore(infiniteScores, int.Parse(scoreInputField.text), leaderboardKey, playerNameInputField.text, (response) =>
        {
            if (response.success)
            {
                infoText.text = "Player score was submitted";
                /*
                 * Update the leaderboards when the new score was sent so we can see them
                 */
                UpdateLeaderboardCentered(infiniteScores);
                UpdateLeaderboardTop10();
            }
            else
            {
                infoText.text = "Error submitting score:" + response.Error;
            }
        });
    }
    void UpdateLeaderboardCentered(string memberID)
    {
        LootLockerSDKManager.GetMemberRank(leaderboardKey, memberID, (memberResponse) =>
        {
            if (memberResponse.success)
            {
                if (memberResponse.rank == 0)
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
                            if (currentEntry.rank == playerRank)
                            {
                                leaderboardText += "<color=yellow>";
                            }

                            leaderboardText += currentEntry.rank + ".";
                            leaderboardText += currentEntry.metadata;
                            leaderboardText += " - ";
                            leaderboardText += currentEntry.score;
                            leaderboardText += "\n";

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
                        infoText.text = "Could not update centered scores:" + scoreResponse.Error;
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
                    leaderboardText += currentEntry.metadata;
                    leaderboardText += " - ";
                    leaderboardText += currentEntry.score;
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

    // Increment and save a string that goes from a to z, then za to zz, zza to zzz etc.
    string GetAndIncrementScoreCharacters()
    {
        // Get the current score string
        string incrementalScoreString = PlayerPrefs.GetString(nameof(incrementalScoreString), "a");

        // Get the current character
        char incrementalCharacter = PlayerPrefs.GetString(nameof(incrementalCharacter), "a")[0];

        // If the previous character we added was 'z', add one more character to the string
        // Otherwise, replace last character of the string with the current incrementalCharacter
        if (incrementalScoreString[incrementalScoreString.Length - 1] == 'z')
        {
            // Add one more character
            incrementalScoreString += incrementalCharacter;
        }
        else
        {
            // Replace character
            incrementalScoreString = incrementalScoreString.Substring(0, incrementalScoreString.Length - 1) + incrementalCharacter.ToString();
        }

        // If the letter int is lower than 'z' add to it otherwise start from 'a' again
        if ((int)incrementalCharacter < 122)
        {
            incrementalCharacter++;
        }
        else
        {
            incrementalCharacter = 'a';
        }

        // Save the current incremental values to PlayerPrefs
        PlayerPrefs.SetString(nameof(incrementalCharacter), incrementalCharacter.ToString());
        PlayerPrefs.SetString(nameof(incrementalScoreString), incrementalScoreString.ToString());

        // Return the updated string
        return incrementalScoreString;
    }
}
}
