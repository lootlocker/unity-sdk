using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LootLocker
{
    public interface IStageOwner
    {
        void UpdateScreenData(IStageData stageData);
    }

    public interface IStageData
    {

    }

    public interface ItemData
    {
    }

    public interface IScreenShotOwner
    {
        string url { get; }
        void SaveTexture(Sprite texture2D);
        Image preview { get; }
        int downloadAttempts { get; set; }

    }

    public interface IPopupData
    {
        string header { get; }
        Dictionary<string, string> normalText { get; }
        string buttonText { get; }
        Action btnAction { get; }
        Sprite sprite { get; }
        bool ignoreCurrentSprite { get; }
        string url { get; }
        bool withKey { get; }
    }
}

namespace LootLockerEnums
{
    public enum DownloadState
    {
        Downloading, Downloaded, Failed
    }
}

