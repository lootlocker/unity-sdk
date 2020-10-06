using UnityEngine;

public class ServerManager : MonoBehaviour
{
    static ServerManager i;
    public static ServerManager I
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
            var existingObj = GameObject.FindObjectOfType<ServerManager>();

            if (existingObj != null)
            {
                i = existingObj;
            }
            else
            {
                i = new GameObject("ServerManager").AddComponent<ServerManager>();
            }

            GameServerAPI.Init(i);


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