using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LootLocker
{
    public interface ILootLockerStageOwner
    {
        void UpdateScreenData(ILootLockerStageData stageData);
    }

    public interface ILootLockerStageData
    {

    }

    public interface ILootLockertemData
    {
    }

    public interface ILootLockerScreenShotOwner
    {
        string url { get; }
        void SaveTexture(Sprite texture2D);
        Image preview { get; }
        int downloadAttempts { get; set; }

    }

    public interface ILootLockerPopupData
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

namespace LootLocker.LootLockerEnums
{
    public enum LootLockerDownloadState
    {
        Downloading, Downloaded, Failed
    }
}

