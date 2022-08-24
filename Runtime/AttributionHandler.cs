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
            // Only subscribe if needed
            if (EditorPrefs.GetBool("attributionChecked") == false)
            {
                ProjectSettings.APIKeyEnteredEvent += EventFired;
            }
        }

        static void EventFired()
        {
            //TODO: Actually implement this
            // 1. Do a request for the API-key-stuff
            // 2. If everything goes well, great
            // 3. Failed for some reason: EditorPrefs.SetBool("attributionChecked", false);
            //    This will make it check again if it has failed.
        }
    }
}
