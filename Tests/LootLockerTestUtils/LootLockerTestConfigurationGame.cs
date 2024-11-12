using System;
using LootLocker;
using LootLocker.Requests;
using System.Collections.Generic;
using UnityEngine;

namespace LootLockerTestConfigurationUtils
{
    public class LootLockerTestGame
    {
        private int _actualActiveGameId { get; set; }
        public int ActiveGameId
        {
            get => _actualActiveGameId;
            set { _actualActiveGameId = value;
                LootLockerAdminRequest.ActiveGameId = value;
            }
        }
        public int GameId { get; set; }
        public string GameName { get; set; }
        public string GameDomainKey { get; set; }
        public string GameApiKey { get; set; }
        public int DevelopmentGameId { get; set; }
        public string DevelopmentGameApiKey { get; set; }
        public string GameVersion { get; set; } = "0.0.1.0";
        public int OrganisationId { get; set; }
        public int UserId { get; set; }
        public LootLockerTestUser User { get; set; }
        public List<LootLockerTestPlatform> EnabledPlatforms { get; set; } = new List<LootLockerTestPlatform>();
        public List<LootLockerTestLeaderboard> Leaderboards { get; set; } = new List<LootLockerTestLeaderboard>();
        public List<LootLockerTestTrigger> Triggers { get; set; } = new List<LootLockerTestTrigger>();

        public static void CreateGame(Action<bool /*success*/, string /*errorMessage*/, LootLockerTestGame /*game*/> onComplete, string testName = "")
        {
            LootLockerTestUser.GetUserOrSignIn((success, errorMessage, user) =>
            {
                if (!success)
                {
                    onComplete?.Invoke(false, errorMessage, null);
                    return;
                }
                if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
                {
                    LootLockerConfig.current.adminToken = user.auth_token;
                }

                LootLockerTestGame createdGame = new LootLockerTestGame
                {
                    User = user,
                    UserId = user.id,
                    OrganisationId = user.organisations[0].id
                };

                EndPointClass endPoint = LootLockerTestConfigurationEndpoints.CreateGame;

                var guid = Guid.NewGuid();
                UnityEngine.Random.InitState(guid.GetHashCode());
                var noun = LootLockerTestConfigurationUtilities.GetRandomNoun();
                var verb = LootLockerTestConfigurationUtilities.GetRandomVerb();

                string gameName = testName + " " + verb + noun + " d-" + DateTime.Now + " " + guid;
                if(gameName.Length > 99)
                {
                    gameName = gameName.Substring(0, 99);
                }

                CreateGameRequest createGameRequest = new CreateGameRequest
                { name = gameName, organisation_id = createdGame.OrganisationId };

                string json = LootLockerJson.SerializeObject(createGameRequest);

                LootLockerAdminRequest.Send(endPoint.endPoint, endPoint.httpMethod, json,
                    onComplete: (serverResponse) =>
                    {
                        var createGameResponse = LootLockerResponse.Deserialize<CreateGameResponse>(serverResponse);
                        if (createGameResponse == null || !createGameResponse.success)
                        {
                            onComplete?.Invoke(false, createGameResponse?.errorData?.ToString(), null);
                            return;
                        }

                        createdGame.ActiveGameId = createdGame.GameId = createGameResponse.game.id;
                        createdGame.GameName = createGameResponse.game.name;
                        createdGame.DevelopmentGameId = createGameResponse.game.development.id;
                        createdGame.GameDomainKey = createGameResponse.game.domain_key;
                        Debug.Log("Created test game with name \"" + createdGame.GameName + "\"");

                        createdGame.SwitchToProdEnvironment();
                        ApiKey.CreateKey("prod", prodKeyResponse =>
                        {
                            if (!prodKeyResponse.success)
                            {
                                onComplete?.Invoke(false, prodKeyResponse.errorData.ToString(), null);
                                return;
                            }
                            createdGame.GameApiKey = prodKeyResponse.api_key;

                            createdGame.SwitchToStageEnvironment();
                            ApiKey.CreateKey("stage", stageKeyResponse =>
                            {
                                if (!stageKeyResponse.success)
                                {
                                    onComplete?.Invoke(false, stageKeyResponse.errorData.ToString(), null);
                                    return;
                                }

                                createdGame.DevelopmentGameApiKey = stageKeyResponse.api_key;

                                onComplete?.Invoke(true, "", createdGame);
                            });
                        });
                    }, true);
            });

        }

        public void DeleteGame(Action<bool /*success*/, string /*errorMessage*/> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                // Not signed in
                onComplete?.Invoke(false, "Not logged in");
                return;
            }

            SwitchToProdEnvironment();
            EndPointClass endPoint = LootLockerTestConfigurationEndpoints.DeleteGame;
            LootLockerAdminRequest.Send(endPoint.endPoint, endPoint.httpMethod, null, (stageGameDelete) =>
            {
                onComplete?.Invoke(stageGameDelete.success, stageGameDelete?.errorData?.message);
            }, true);
        }

        public bool InitializeLootLockerSDK()
        {
            string adminToken = LootLockerConfig.current.adminToken;
            bool result = LootLockerSDKManager.Init(GetApiKeyForActiveEnvironment(), GameVersion, GameDomainKey);
            LootLockerConfig.current.adminToken = adminToken;
            return result;
        }

        public void SwitchToStageEnvironment()
        {
            ActiveGameId = DevelopmentGameId;
        }

        public void SwitchToProdEnvironment()
        {
            ActiveGameId = GameId;
        }

        public string GetApiKeyForActiveEnvironment()
        {
            if (ActiveGameId == GameId)
            {
                return GameApiKey;
            }

            return DevelopmentGameApiKey;
        }

        public void EnableGuestLogin(Action<bool /*success*/, string /*errorMessage*/> onComplete)
        {
            LootLockerTestPlatform.UpdatePlatform("guest", true, new Dictionary<string, object>(),
                (success, errorMessage, Platform) =>
                {
                    if (success)
                    {
                        EnabledPlatforms.Add(Platform);
                    }
                    onComplete?.Invoke(success, errorMessage);
                });
        }

        public void EnableWhiteLabelLogin(Action<bool /*success*/, string /*errorMessage*/> onComplete)
        {
            LootLockerTestPlatform.UpdatePlatform("white_label_login", true, new Dictionary<string, object>(),
                (success, errorMessage, Platform) =>
                {
                    if (success)
                    {
                        EnabledPlatforms.Add(Platform);
                    }
                    onComplete?.Invoke(success, errorMessage);
                });
        }

        public LootLockerTestLeaderboard GetLeaderboardByKey(string key)
        {
            return Leaderboards.Find(lb => lb.key == key);
        }

        public void CreateLeaderboard(CreateLootLockerLeaderboardRequest request, Action<LootLockerTestLeaderboard /*leaderboard*/> onComplete)
        {
            if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
            {
                // Already signed in
                onComplete?.Invoke(null);
                return;
            }

            EndPointClass endPoint = LootLockerTestConfigurationEndpoints.createLeaderboard;

            string json = LootLockerJson.SerializeObject(request);

            LootLockerAdminRequest.Send(endPoint.endPoint, endPoint.httpMethod, json,
                onComplete: (serverResponse) =>
                {
                    var leaderboardResponse = LootLockerResponse.Deserialize<LootLockerTestLeaderboard>(serverResponse);
                    Leaderboards.Add(leaderboardResponse);
                    onComplete?.Invoke(leaderboardResponse);
                }, true);
        }

        public void CreateTrigger(string key, string name, int limit, string rewardId, Action<bool /*success*/, string /*errorMessage*/, LootLockerTestTrigger /*createdTrigger*/> onComplete)
        {
            LootLockerTestConfigurationTrigger.CreateTrigger(key, name, limit, rewardId, response =>
            {
                if (response.success)
                {
                    var createdTrigger = new LootLockerTestTrigger
                    {
                        key = key,
                        name = name,
                        limit = limit,
                        reward_id = rewardId
                    };
                    Triggers.Add(createdTrigger);
                    onComplete?.Invoke(true, "", createdTrigger);
                }
                else
                {
                    onComplete?.Invoke(false, response.errorData?.message, null);
                }
            });
        }

    }

    public class CreateGameRequest
    {
        public string name { get; set; }
        public int genre { get; set; } = 1;
        public int organisation_id { get; set; }
    }

    public class CreateGameResponseGame
    {
        public int id { get; set; }
        public string name { get; set; }
        public string domain_key { get; set; }
        public CreateGameResponseGame development { get; set; } = null;
    }

    public class CreateGameResponse : LootLockerResponse
    {
        public CreateGameResponseGame game { get; set; }
    }
}
