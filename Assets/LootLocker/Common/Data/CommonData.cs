using LootLocker;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LootLocker
{
    public class AuthResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public string auth_token { get; set; }
        public User user { get; set; }
        public string mfa_key { get; set; }
    }

    public class InitialAuthRequest
    {
        public string email { get; set; }
        public string password { get; set; }
    }

    public class TwoFactorAuthVerficationRequest
    {
        public string mfa_key { get; set; }
        public string secret { get; set; }
    }


    #region SubsequentRequests

    public class SubsequentRequestsResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public Game[] games { get; set; }
    }

    #endregion

    public class User
    {
        public string name { get; set; }
        public Organisation[] organisations { get; set; }
    }

    public class Organisation
    {
        public int id { get; set; }
        public string name { get; set; }
        public Game[] games { get; set; }
    }

    public class Game
    {
        public int id { get; set; }
        public string name { get; set; }
        public bool sandbox_mode { get; set; }

        public bool is_demo;
    }


    public class CreatingAGameResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public CAGGame game { get; set; }
    }

    public class CAGGame
    {
        public int id { get; set; }
        public string name { get; set; }
        public string game_key { get; set; }
        public string steam_app_id { get; set; }
        public string steam_api_key { get; set; }
        public bool sandbox_mode { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }
        public CAGGame development { get; set; }
    }

    public class CreatingAGameRequest
    {

        public string name, steam_app_id;
        public bool sandbox_mode;
        public int organisation_id;
        public bool demo;

    }

    public class CreateTriggersRequest
    {

        public string name;
        public int times, game_id;
        public bool grant_all;
        public Reward[] rewards;

    }

    public class ListTriggersResponse : LootLockerResponse
    {
        public bool success { get; set; }
        public Trigger[] triggers { get; set; }
    }
    public class Trigger
    {
        public int id { get; set; }
        public string name { get; set; }
        public int times { get; set; }
        public bool grant_all { get; set; }
        public Reward[] rewards { get; set; }
    }

    public class Reward
    {
        public int asset_id { get; set; }
        public object asset_variation_id { get; set; }
        public object asset_image { get; set; }
        public string asset_name { get; set; }
        public int score { get; set; }
    }
}