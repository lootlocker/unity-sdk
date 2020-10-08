using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

[System.Serializable]
public class TextureSaveClass
{
    public string url;
    public Sprite sprite;
    public List<Action<Sprite>> actions;
    public IScreenShotOwner owner;
}
[System.Serializable]
public class TextureSaveClassDictionary
{
    public List<TextureSaveClass> textureSaveClasses = new List<TextureSaveClass>();
    public bool Contains(string url)
    {
        TextureSaveClass textureSaveClass = textureSaveClasses.FirstOrDefault(x => x.url == url);
        if (textureSaveClass != null)
            return true;
        return false;
    }
    public bool Contains(IScreenShotOwner url)
    {
        TextureSaveClass textureSaveClass = textureSaveClasses.FirstOrDefault(x => x.owner == url);
        if (textureSaveClass != null)
            return true;
        return false;
    }

    public void Add(TextureSaveClass textureSaveClass)
    {
        if (!Contains(textureSaveClass.url))
            textureSaveClasses.Add(textureSaveClass);
    }
    public void Remove(TextureSaveClass textureSaveClass)
    {
        if (Contains(textureSaveClass.url))
            textureSaveClasses.Remove(textureSaveClass);
    }
    public void Remove(IScreenShotOwner url)
    {
        TextureSaveClass[] textureSaveClass = textureSaveClasses.Where(x => x.owner == url).ToArray();

        if (textureSaveClass != null)
        {
            for (int i = 0; i < textureSaveClass.Length; i++)
            {
                if (textureSaveClasses.Contains(textureSaveClasses[i]))
                    textureSaveClasses.Remove(textureSaveClass[i]);
            }
        }
    }
    public void Remove(string url)
    {
        TextureSaveClass textureSaveClass = textureSaveClasses.FirstOrDefault(x => x.url == url);
        if (textureSaveClass != null)
            Remove(textureSaveClass);
    }
    public TextureSaveClass GetTextureSaveClass(string url)
    {
        TextureSaveClass textureSaveClass = textureSaveClasses.FirstOrDefault(x => x.url == url);
        if (textureSaveClass != null)
            return textureSaveClass;
        return null;
    }
    public TextureSaveClass GetTextureSaveClass(IScreenShotOwner url)
    {
        TextureSaveClass textureSaveClass = textureSaveClasses.FirstOrDefault(x => x.owner == url);
        if (textureSaveClass != null)
            return textureSaveClass;
        return null;
    }
    public TextureSaveClass First()
    {
        TextureSaveClass textureSaveClass = textureSaveClasses.First();
        if (textureSaveClass != null)
            return textureSaveClass;
        return null;
    }

    public int Count()
    {
        return textureSaveClasses.Count();
    }
}
public class TexturesSaver : MonoBehaviour
{
    public static TexturesSaver Instance;
    public TextureSaveClassDictionary screenShotDownloadQueue;
    public TextureSaveClassDictionary downloadedTextures = new TextureSaveClassDictionary();
    public Texture2D defaultTexture;
    private static List<IScreenShotOwner> previewImageDownloadQueue = new List<IScreenShotOwner>();
    public List<IScreenShotOwner> previewImageDownloadQueueAll
    {
        get
        {
            return previewImageDownloadQueue;
        }
    }
    private static Coroutine ImageDownloadThread1;
    private static Coroutine ImageDownloadThread2;
    private static Coroutine ImageDownloadThread3;
    private static Coroutine ImageDownloadThread4;
    private static Coroutine ImageDownloadThread5;
    private static Coroutine ImageDownloadThread6;
    private static Coroutine ImageDownloadThread7;
    private static Coroutine ImageDownloadThread8;
    private static Coroutine ImageDownloadThread9;

    public void Awake()
    {
        Instance = this;
    }
    public static void QueueForDownload(IScreenShotOwner previewOwner)
    {
        if (string.IsNullOrEmpty(previewOwner?.url)) return;
        if (Instance.downloadedTextures.Contains(previewOwner.url))
        {
            previewOwner.SaveTexture(Instance.downloadedTextures.GetTextureSaveClass(previewOwner.url)?.sprite);
            return;
        }

        if(!previewOwner.url.Contains("http"))
        {
            return;
        }

        if (!previewImageDownloadQueue.Contains(previewOwner))
        {
            previewImageDownloadQueue.Add(previewOwner);
            if (ImageDownloadThread1 == null)
                ImageDownloadThread1 = Instance.StartCoroutine(DownloadPreviewImagesAsync());
            if (ImageDownloadThread2 == null)
                ImageDownloadThread2 = Instance.StartCoroutine(DownloadPreviewImagesAsync());
            if (ImageDownloadThread3 == null)
                ImageDownloadThread3 = Instance.StartCoroutine(DownloadPreviewImagesAsync());
            if (ImageDownloadThread4 == null)
                ImageDownloadThread4 = Instance.StartCoroutine(DownloadPreviewImagesAsync());
            if (ImageDownloadThread5 == null)
                ImageDownloadThread5 = Instance.StartCoroutine(DownloadPreviewImagesAsync());
            if (ImageDownloadThread6 == null)
                ImageDownloadThread6 = Instance.StartCoroutine(DownloadPreviewImagesAsync());
            if (ImageDownloadThread7 == null)
                ImageDownloadThread7 = Instance.StartCoroutine(DownloadPreviewImagesAsync());
            if (ImageDownloadThread8 == null)
                ImageDownloadThread8 = Instance.StartCoroutine(DownloadPreviewImagesAsync());
            if (ImageDownloadThread9 == null)
                ImageDownloadThread9 = Instance.StartCoroutine(DownloadPreviewImagesAsync());
        }
    }

    /// <summary>
    /// used to download image previews
    /// </summary>
    /// <returns></returns>
    private static IEnumerator DownloadPreviewImagesAsync()
    {
        if (previewImageDownloadQueue.Count > 0)
        {
            IScreenShotOwner processingObject;
            lock (previewImageDownloadQueue)
            {
                processingObject = previewImageDownloadQueue[0];
                previewImageDownloadQueue.RemoveAt(0);
                processingObject.downloadAttempts += 1;
            }
            string newUrl = processingObject.url;
            UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(newUrl);
            yield return uwr.SendWebRequest();
            if (uwr.isNetworkError || uwr.isHttpError)
            {
                Debug.LogError("Error: " + processingObject.url + "/" + uwr.error + uwr.downloadHandler?.text);
                if (processingObject.downloadAttempts <= 3)
                    previewImageDownloadQueue.Add(processingObject);
            }
            else
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                processingObject.SaveTexture(sprite);
                if (!Instance.downloadedTextures.Contains(processingObject.url))
                    Instance.downloadedTextures.Add(new TextureSaveClass { url = processingObject.url, sprite = sprite });
            }
        }
        else
        {
            yield return new WaitForSeconds(.5f);
        }
        yield return Instance.StartCoroutine(DownloadPreviewImagesAsync());
    }
}

