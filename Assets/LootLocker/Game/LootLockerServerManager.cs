using UnityEngine;
using LootLocker;

namespace LootLocker
{
    public class LootLockerServerManager : MonoBehaviour
    {
        static LootLockerServerManager i;
        public static LootLockerServerManager I
        {
            get
            {
                CheckInit();
                return i;
            }
        }

        public static void CheckInit()
        {
            if (i == null)
            {
                var existingObj = GameObject.FindObjectOfType<LootLockerServerManager>();

                if (existingObj != null)
                {
                    i = existingObj;
                }
                else
                {
                    i = new GameObject("ServerManager").AddComponent<LootLockerServerManager>();
                }

                LootLockerGameServerAPI.Init(i);


                if (Application.isPlaying)
                    DontDestroyOnLoad(i.gameObject);
            }
        }

        void Awake()
        {
            CheckInit();
            DontDestroyOnLoad(gameObject); //the same as i.gameobject (is there's only one instance in the scene)
        }

    }
}