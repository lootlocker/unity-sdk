using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using LootLocker.Requests;

namespace LootLocker.Admin
{
    public class AttributionHandler : MonoBehaviour
    {
        [InitializeOnLoadMethod]
        static void SubscribeToEventOnStartup()
        {
            Debug.Log("Subscribing to event");
            ProjectSettings.APIKeyEnteredEvent += EventFired;
        }

        static void EventFired()
        {
            Debug.Log("Will send attribution!");
        }
    }
}
