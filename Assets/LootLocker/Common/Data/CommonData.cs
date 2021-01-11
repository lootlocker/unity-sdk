using LootLocker;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LootLocker
{
    public class LootLockerAuthResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public string auth_token { get; set; }
        public LootLockerUser user { get; set; }
        public string mfa_key { get; set; }
    }

    public class LootLockerInitialAuthRequest
    {
        public string email { get; set; }
        public string password { get; set; }
    }

    public class LootLockerTwoFactorAuthVerficationRequest
    {
        public string mfa_key { get; set; }
        public string secret { get; set; }
    }


    #region SubsequentRequests

    public class LootLockerSubsequentRequestsResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public LootLockerGame[] games { get; set; }
    }

    #endregion

    public class LootLockerUser
    {
        public string name { get; set; }
        public LootLockerOrganisation[] organisations { get; set; }
    }

    public class LootLockerOrganisation
    {
        public int id { get; set; }
        public string name { get; set; }
        public LootLockerGame[] games { get; set; }
    }

    public class LootLockerGame
    {
        public int id { get; set; }
        public string name { get; set; }
        public bool sandbox_mode { get; set; }

        public bool is_demo;
    }


    public class LootLockerCreatingAGameResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public LootLockerCAGGame game { get; set; }
    }

    public class LootLockerCAGGame
    {
        public int id { get; set; }
        public string name { get; set; }
        public string game_key { get; set; }
        public string steam_app_id { get; set; }
        public string steam_api_key { get; set; }
        public bool sandbox_mode { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public LootLockerCAGGame development { get; set; }
    }

    public class LootLockerCreatingAGameRequest
    {

        public string name, steam_app_id;
        public bool sandbox_mode;
        public int organisation_id;
        public bool demo;

    }

    public class LootLockerCreateTriggersRequest
    {

        public string name;
        public int times, game_id;
        public bool grant_all;
        public LootLockerReward[] rewards;

    }

    public class LootLockerListTriggersResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public LootLockerTrigger[] triggers { get; set; }
    }
    public class LootLockerTrigger
    {
        public int id { get; set; }
        public string name { get; set; }
        public int times { get; set; }
        public bool grant_all { get; set; }
        public LootLockerReward[] rewards { get; set; }
    }

    public class LootLockerReward
    {
        public int asset_id { get; set; }
        public object asset_variation_id { get; set; }
        public object asset_image { get; set; }
        public string asset_name { get; set; }
        public int score { get; set; }
    }
}