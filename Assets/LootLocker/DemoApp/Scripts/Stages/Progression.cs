using LootLockerRequests;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Progression : MonoBehaviour
{
    public Text firstLevel;
    public Text secondLevel;
    public Text xpCalculation;
    public Image xpProgress;
    public Image[] rewards;

    public void UpdateScreen(SessionResponse sessionResponse)
    {
        firstLevel.text = sessionResponse.level.ToString();
        secondLevel.text = (sessionResponse.level + 1).ToString();
        float numerator = sessionResponse.xp - sessionResponse.level_thresholds.current;
        float denominator = sessionResponse.level_thresholds.next - sessionResponse.level_thresholds.current;
        float fillAmount = numerator / denominator;
        xpProgress.fillAmount = fillAmount;
        xpCalculation.text = sessionResponse.xp + " / " + sessionResponse.level_thresholds.next + " XP";
    }
}
