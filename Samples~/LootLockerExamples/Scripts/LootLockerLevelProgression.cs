using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker.Requests;
using UnityEngine.UI;

namespace LootLocker
{
    public class LootLockerLevelProgression : MonoBehaviour
    {
        private static string progressionKey = "simple_progression";

        public Text pointsAmountText;

        public Slider pointsAmountSlider;

        public Slider progressionTierStatus;

        public Text currentTierText;

        public Text currentPointsText;

        public Text pointsToNextThreshold;
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
            LootLockerSDKManager.StartGuestSession((response) =>
            {
                if (response.success)
                {
                    // New player
                    if(response.seen_before == false)
                    {
                        // Register the progression to the player; this will create the progression if it doesn't already exist.
                        // It is the same as adding 0 to the progression.
                        LootLockerSDKManager.RegisterPlayerProgression(progressionKey, (response) =>
                        {
                            if (response.success)
                            {
                                Debug.Log("Progression registered");
                                UpdateProgressionUI(response.step, response.points, response.previous_threshold, response.next_threshold);
                            }
                            else
                            {
                                Debug.Log("Error registering progression");
                            }
                        });
                    }
                    else
                    {
                        // This player is old and has already registered the progression
                        // So we just need to get it.
                        LootLockerSDKManager.GetPlayerProgression(progressionKey, (response) =>
                        {
                            if (response.success)
                            {
                                /*
                                 * response.step is the current tier of the progression
                                 * response.points is the current amount of points taht the player has in the progression
                                 * response.previous_threshold is the amount of points needed to reach the previous tier
                                 * response.next_threshold is the amount of points needed to reach the next tier
                                 * */
                                UpdateProgressionUI(response.step, response.points, response.previous_threshold, response.next_threshold);
                            }
                            else
                            {
                                Debug.Log("Error getting progression");
                            }
                        });
                    }
                }
                else
                {

                }
            });
        }

        private void Update()
        {
            UpdatePointsAmountText();
        }
        void UpdatePointsAmountText()
        {
            pointsAmountText.text = "Points to add:"+pointsAmountSlider.value.ToString();
        }

        public void AddPointsToProgression()
        {
            // Add X amount of points to the progression
            // All progressions uses ulong as the type for the points, so you need to cast the value to ulong.
            // Progressions will not be able to go below 0 (no negative progressions).
            LootLockerSDKManager.AddPointsToPlayerProgression(progressionKey, (ulong)pointsAmountSlider.value, (response) =>
            {
                if(response.success)
                {
                    Debug.Log("Points added to progression");
                    // If the player leveled up, the count of awarded_tiers will be greater than 0
                    UpdateProgressionUI(response.step, response.points, response.previous_threshold, response.next_threshold);
                }
                else
                {
                    Debug.Log("Error adding points to progression");
                }
            });
        }

        public void ResetProgression()
        {
            // Reset the progression to 0
            LootLockerSDKManager.ResetPlayerProgression(progressionKey, (response) =>
            {
                if(response.success)
                {
                    Debug.Log("Progression reset");
                    UpdateProgressionUI(response.step, response.points, response.previous_threshold, response.next_threshold);
                }
                else
                {
                    Debug.Log("Error resetting progression");
                }
            });
        }

        void UpdateProgressionUI(ulong currentTier, ulong currentPoints, ulong previousTierPoints, ulong? nextTierPoints, bool leveledUp = false)
        {
            // Do something with leveled up, shoiw text or such?

            // Update the UI with the progression information
            currentTierText.text = "Current tier: " + currentTier;

            // Next tier points can be null if the progression is at max tier, if so set current points as the max value.
            if(nextTierPoints == null)
            {
                nextTierPoints = currentPoints;
            }
            // Set the min/max values of the slider to the next/previous tier points
            // This makes it show the progression between the tiers
            progressionTierStatus.minValue = (float)previousTierPoints;
            progressionTierStatus.maxValue = (float)nextTierPoints;
            progressionTierStatus.value = (float)currentPoints;

            // Calculate the points needed to advance the next threshold
            ulong pointsDelta = (ulong)nextTierPoints - previousTierPoints;
            pointsToNextThreshold.text = "Points to next threshold: " + (pointsDelta - (currentPoints - previousTierPoints)).ToString();
            
            // Update the current points text
            currentPointsText.text = "Current points: " + currentPoints;
        }
    }
}
