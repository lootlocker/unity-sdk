using LootLocker.Requests;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LootLocker
{
    public class LocalPlayer : ILootLockerStageData
    {
        public string playerName, uniqueID;
        public LootLockerCharacter characterClass;

    }

    public class LootLockerConfig : LootLockerGenericConfig
    {

        public static LootLockerGenericConfig current;

    }
}