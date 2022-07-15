using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker.Requests;

namespace LootLocker
{
    public class AttributionHandler : MonoBehaviour
    {
        /*
         * In LootLockerServerRequest, add:
         *            if (response.success == true)
            {
                requestCompletedEvent?.Invoke();
            }
        to line 95, just before returning the session response.
        Also in LootLockerServerRequest, add:
        public delegate void SendAttributionDelegate();
        public static event SendAttributionDelegate requestCompletedEvent;
        at the top, then uncomment this and send in some stuff for the attribution-stuff in EventFired()

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnBeforeSceneLoadRuntimeMethod()
        {
            LootLockerResponse.requestCompletedEvent += EventFired;
        }

        static void EventFired()
        {
            Debug.Log("Will send attribution!");
        }
        */
    }
}
