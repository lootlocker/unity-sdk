using System.Collections.Generic;

#if UNITY_EDITOR && UNITY_2021_3_OR_NEWER && LOOTLOCKER_ENABLE_EXTENSION
using LootLocker.Extension.DataTypes;
namespace LootLocker.Extension.Requests
{
    public class MfaAdminLoginRequest
    {
        public string mfa_key { get; set; }
        public string secret { get; set; }
    }

    public class AdminLoginRequest
    {
        public string email { get; set; }
        public string password { get; set; }
    }

    public class KeyCreationRequest
    {
        public string name { get; set; }
        public string api_type { get; set; }
    }
}

namespace LootLocker.Extension.DataTypes
{
    public class Game
    {
        public int id { get; set; }
        public bool is_demo { get; set; }
        public string name { get; set; }
        public string badge_url { get; set; }
        public string logo_url { get; set; }
        public Game development { get; set; }
    }

    public class Organisation
    {
        public int id { get; set; }
        public string name { get; set; }
        public Game[] games { get; set; }

        public Game GetGameByID(int id)
        {
            foreach (var game in games)
            {
                if (game.id == id)
                {
                    return game;
                }
            }
            return null;
        }
    }

    public class User
    {
        public int id { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public long signed_up { get; set; }
        public Organisation[] organisations { get; set; }
        private Dictionary<int, int> organisationIndexByID { get; set; }

        public Organisation GetOrganisationByID(int id)
        {
            foreach (var org in organisations)
            {
                if (org.id == id)
                {
                    return org;
                }
            }
            return null;
        }
    }
}

namespace LootLocker.Extension.Responses
{
    public class LoginResponse : LootLockerResponse
    {

        public string mfa_key { get; set; }
        public string auth_token { get; set; }
        public User user { get; set; }
    }

    public class UserRoleResponse : LootLockerResponse
    {
        public string[] permissions { get; set; }
        public bool self { get; set; }
    }

    public class KeysResponse : LootLockerResponse
    {
        public KeyResponse[] api_keys { get; set; }
    }
    public class KeyResponse : LootLockerResponse
    {
        public int id { get; set; }
        public int game_id { get; set; }
        public string api_key { get; set; }
        public string api_type { get; set; }
        public string name { get; set; }
        public string created_at { get; set; }
        public string updated_at { get; set; }
    }
}
#endif