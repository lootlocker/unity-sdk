using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker.Requests;
using UnityEngine.UI;

namespace LootLocker
{
    public class LootLockerMultipleProgressions : MonoBehaviour
    {
        // Progression 1
        private static string progressionKey1 = "connected_progression_1";

        [Header("Progression 1")]

        public Text pointsAmountText1;

        public Slider pointsAmountSlider1;

        public Slider progressionTierStatus1;

        public Text currentTierText1;

        public Text currentPointsText1;

        public Text pointsToNextThreshold1;


        // Progression 2
        private static string progressionKey2 = "connected_progression_2";

        [Header("Progression 2")]

        public Text pointsAmountText2;

        public Slider pointsAmountSlider2;

        public Slider progressionTierStatus2;

        public Text currentTierText2;

        public Text currentPointsText2;

        public Text pointsToNextThreshold2;
        // Start is called before the first frame update
        void Start()
        {
            /* 
            * Override settings to use the Example game setup
            */
            LootLockerSettingsOverrider.OverrideSettings();

            /* Start guest session without an identifier.
             * LootLocker will create an identifier for the user and store it in PlayerPrefs.
             * If you want to create a new player when testing, you can use PlayerPrefs.DeleteKey("LootLockerGuestPlayerID");
             */
            LootLockerSDKManager.StartGuestSession((startGuestSessionResponse) =>
            {
                if (startGuestSessionResponse.success)
                {
                        // Register the progression to the player; this will create the progression if it doesn't already exist.
                        // It is the same as adding 0 to the progression.
                        LootLockerSDKManager.RegisterPlayerProgression(progressionKey1, (registerPlayerProgressionResponse) =>
                        {
                            if (registerPlayerProgressionResponse.success)
                            {
                                Debug.Log("Progression registered");
                                UpdateProgressionUI1(registerPlayerProgressionResponse.step, registerPlayerProgressionResponse.points, registerPlayerProgressionResponse.previous_threshold, registerPlayerProgressionResponse.next_threshold);
                            }
                            else
                            {
                                Debug.Log("Error registering progression");
                            }
                        });
                        // Same with progression 2
                        LootLockerSDKManager.RegisterPlayerProgression(progressionKey2, (registerPlayerProgressionResponse) =>
                        {
                            if (registerPlayerProgressionResponse.success)
                            {
                                Debug.Log("Progression registered");
                                UpdateProgressionUI2(registerPlayerProgressionResponse.step, registerPlayerProgressionResponse.points, registerPlayerProgressionResponse.previous_threshold, registerPlayerProgressionResponse.next_threshold);
                            }
                            else
                            {
                                Debug.Log("Error registering progression");
                            }
                        });
                }
                else
                {
                    Debug.Log("Error starting guest session");
                }
            });
        }



        private void Update()
        {
            UpdatePointsAmountText();
        }
        void UpdatePointsAmountText()
        {
            pointsAmountText1.text = "Points to add:"+pointsAmountSlider1.value.ToString();
            pointsAmountText2.text = "Points to add:" + pointsAmountSlider2.value.ToString();
        }

        public void AddPointsToProgression1()
        {
            // Add X amount of points to the progression
            // All progressions uses ulong as the type for the points, so you need to cast the value to ulong.
            // Progressions will not be able to go below 0 (no negative progressions).
            LootLockerSDKManager.AddPointsToPlayerProgression(progressionKey1, (ulong)pointsAmountSlider1.value, (addPointsToPlayerProgressionResponse) =>
            {
                if(addPointsToPlayerProgressionResponse.success)
                {
                    Debug.Log("Points added to progression");
                    // If the player leveled up, the count of awarded_tiers will be greater than 0
                    UpdateProgressionUI1(addPointsToPlayerProgressionResponse.step, addPointsToPlayerProgressionResponse.points, addPointsToPlayerProgressionResponse.previous_threshold, addPointsToPlayerProgressionResponse.next_threshold);

                    // Update both progressions
                    LootLockerSDKManager.GetPlayerProgressions((getPlayerProgressionsResponse) =>
                    {
                        if(getPlayerProgressionsResponse.success)
                        {
                            var progression1 = getPlayerProgressionsResponse.items.Find(x => x.progression_key == progressionKey1);
                            var progression2 = getPlayerProgressionsResponse.items.Find(x => x.progression_key == progressionKey2);
                            UpdateProgressionUI1(progression1.step, progression1.points, progression1.previous_threshold, progression1.next_threshold);
                            UpdateProgressionUI2(progression2.step, progression2.points, progression2.previous_threshold, progression2.next_threshold);
                        }
                    });
                }
                else
                {
                    Debug.Log("Error adding points to progression");
                }
            });
        }
        public void AddPointsToProgression2()
        {
            // Add X amount of points to the progression
            // All progressions uses ulong as the type for the points, so you need to cast the value to ulong.
            // Progressions will not be able to go below 0 (no negative progressions).
            LootLockerSDKManager.AddPointsToPlayerProgression(progressionKey2, (ulong)pointsAmountSlider2.value, (addPointsToPlayerProgressionResponse) =>
            {
                if (addPointsToPlayerProgressionResponse.success)
                {
                    Debug.Log("Points added to progression");

                    // If the player leveled up, the count of awarded_tiers will be greater than 0
                    if (addPointsToPlayerProgressionResponse.awarded_tiers.Count > 0)
                    {
                        Debug.Log("Player leveled up");
                    }

                    UpdateProgressionUI2(addPointsToPlayerProgressionResponse.step, addPointsToPlayerProgressionResponse.points, addPointsToPlayerProgressionResponse.previous_threshold, addPointsToPlayerProgressionResponse.next_threshold);

                    // Since progression 1 is connected to progression 2, we need to update it as well
                    LootLockerSDKManager.GetPlayerProgression(progressionKey1, (getPlayerProgressionResponse) =>
                    {
                        if (getPlayerProgressionResponse.success)
                        {
                            /*
                             * getPlayerProgressionResponse.step is the current tier of the progression
                             * getPlayerProgressionResponse.points is the current amount of points taht the player has in the progression
                             * getPlayerProgressionResponse.previous_threshold is the amount of points needed to reach the previous tier
                             * getPlayerProgressionResponse.next_threshold is the amount of points needed to reach the next tier
                             * */
                            UpdateProgressionUI1(getPlayerProgressionResponse.step, getPlayerProgressionResponse.points, getPlayerProgressionResponse.previous_threshold, getPlayerProgressionResponse.next_threshold);
                        }
                        else
                        {
                            Debug.Log("Error getting progression");
                        }
                    });
                }
                else
                {
                    Debug.Log("Error adding points to progression");
                }
            });
        }

        public void ResetProgression1()
        {
            // Reset the progression to 0
            LootLockerSDKManager.ResetPlayerProgression(progressionKey1, (resetPlayerProgressionResponse) =>
            {
                if(resetPlayerProgressionResponse.success)
                {
                    Debug.Log("Progression reset");
                    UpdateProgressionUI1(resetPlayerProgressionResponse.step, resetPlayerProgressionResponse.points, resetPlayerProgressionResponse.previous_threshold, resetPlayerProgressionResponse.next_threshold);
                }
                else
                {
                    Debug.Log("Error resetting progression");
                }
            });
        }

        public void ResetProgression2()
        {
            // Reset the progression to 0
            LootLockerSDKManager.ResetPlayerProgression(progressionKey2, (resetPlayerProgressionResponse) =>
            {
                if (resetPlayerProgressionResponse.success)
                {
                    Debug.Log("Progression reset");
                    UpdateProgressionUI2(resetPlayerProgressionResponse.step, resetPlayerProgressionResponse.points, resetPlayerProgressionResponse.previous_threshold, resetPlayerProgressionResponse.next_threshold);
                }
                else
                {
                    Debug.Log("Error resetting progression");
                }
            });
        }

        void UpdateProgressionUI1(ulong currentTier, ulong currentPoints, ulong previousTierPoints, ulong? nextTierPoints, bool leveledUp = false)
        {
            // Do something with leveled up, shoiw text or such?

            // Update the UI with the progression information
            currentTierText1.text = "Current tier: " + currentTier;

            // Next tier points can be null if the progression is at max tier, if so set current points as the max value.
            if(nextTierPoints == null)
            {
                nextTierPoints = currentPoints;
            }
            // Set the min/max values of the slider to the next/previous tier points
            // This makes it show the progression between the tiers
            progressionTierStatus1.minValue = (float)previousTierPoints;
            progressionTierStatus1.maxValue = (float)nextTierPoints;
            progressionTierStatus1.value = (float)currentPoints;

            // Calculate the points needed to advance the next threshold
            ulong pointsDelta = (ulong)nextTierPoints - previousTierPoints;
            pointsToNextThreshold1.text = "Points to next threshold: " + (pointsDelta - (currentPoints - previousTierPoints)).ToString();
            
            // Update the current points text
            currentPointsText1.text = "Current points: " + currentPoints;
        }

        void UpdateProgressionUI2(ulong currentTier, ulong currentPoints, ulong previousTierPoints, ulong? nextTierPoints)
        {
            // Update the UI with the progression information
            currentTierText2.text = "Current tier: " + currentTier;

            // Next tier points can be null if the progression is at max tier, if so set current points as the max value.
            if (nextTierPoints == null)
            {
                nextTierPoints = currentPoints;
            }
            // Set the min/max values of the slider to the next/previous tier points
            // This makes it show the progression between the tiers
            progressionTierStatus2.minValue = (float)previousTierPoints;
            progressionTierStatus2.maxValue = (float)nextTierPoints;
            progressionTierStatus2.value = (float)currentPoints;

            // Calculate the points needed to advance the next threshold
            ulong pointsDelta = (ulong)nextTierPoints - previousTierPoints;
            pointsToNextThreshold2.text = "Points to next threshold: " + (pointsDelta - (currentPoints - previousTierPoints)).ToString();

            // Update the current points text
            currentPointsText2.text = "Current points: " + currentPoints;
        }
    }
}
