using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

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

#if UNITY_EDITOR
        [InitializeOnEnterPlayMode]
        static void OnEnterPlaymodeInEditor(EnterPlayModeOptions options)
        {
            i = null;
        }
#endif

        void Awake()
        {
            CheckInit();
            DontDestroyOnLoad(gameObject); //the same as i.gameobject (if there's only one instance in the scene)
        }

    }
}