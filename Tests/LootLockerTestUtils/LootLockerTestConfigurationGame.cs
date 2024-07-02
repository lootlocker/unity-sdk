using LootLocker;
using LootLocker.Requests;
using System;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;

namespace LootLockerTestConfigurationUtils
{
    public class LootLockerTestGame
    {
        public int ActiveGameId { get; set; }
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

        private static readonly string[] OneHundredNouns = { "time", "year", "people", "way", "day", "man", "thing", "woman", "life", "child", "world", "school",
            "state", "family", "student", "group", "country", "problem", "hand", "part", "place", "case", "week",
            "company", "system", "program", "question", "work", "government", "number", "night", "point", "home",
            "water", "room", "mother", "area", "money", "story", "fact", "month", "lot", "right", "study", "book",
            "eye", "job", "word", "business", "issue", "side", "kind", "head", "house", "service", "friend", "father",
            "power", "hour", "game", "line", "end", "member", "law", "car", "city", "community", "name", "president",
            "team", "minute", "idea", "kid", "body", "information", "back", "parent", "face", "others", "level",
            "office", "door", "health", "person", "art", "war", "history", "party", "result", "change", "morning",
            "reason", "research", "girl", "guy", "moment", "air", "teacher", "force", "education"
        };

        private static readonly string[] OneHundredVerbs = {
            "be", "have", "do", "say", "go", "can", "get", "would", "make", "know", "will", "think", "take", "see",
            "come", "could", "want", "look", "use", "find", "give", "tell", "work", "may", "should", "call", "try",
            "ask", "need", "feel", "become", "leave", "put", "mean", "keep", "let", "begin", "seem", "help", "talk",
            "turn", "start", "might", "show", "hear", "play", "run", "move", "like", "live", "believe", "hold", "bring",
            "happen", "must", "write", "provide", "sit", "stand", "lose", "pay", "meet", "include", "continue", "set",
            "learn", "change", "lead", "understand", "watch", "follow", "stop", "create", "speak", "read", "allow",
            "add", "spend", "grow", "open", "walk", "win", "offer", "remember", "love", "consider", "appear", "buy",
            "wait", "serve", "die", "send", "expect", "build", "stay", "fall", "cut", "reach", "kill", "remain"
        };

        public static void CreateGame(Action<bool /*success*/, string /*errorMessage*/, LootLockerTestGame /*game*/> onComplete, string testName = "")
        {
            LootLockerTestUser.GetUserOrSignIn((success, errorMessage, user) =>
            {
                if (!success)
                {
                    onComplete?.Invoke(false, errorMessage, null);
                    return;
                }
                Debug.Log(user);
                if (string.IsNullOrEmpty(LootLockerConfig.current.adminToken))
                {
                    Debug.LogWarning("We set admin token: " + user.auth_token);
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
                var noun = OneHundredNouns[UnityEngine.Random.Range(0, 99)];
                var verb = OneHundredVerbs[UnityEngine.Random.Range(0, 99)];

                string gameName = testName + " " + verb + noun + " d-" + DateTime.Now + " " + guid;

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

                        ApiKey.CreateKey("prod", createdGame.GameId, prodKeyResponse =>
                        {
                            if (!prodKeyResponse.success)
                            {
                                onComplete?.Invoke(false, prodKeyResponse.errorData.ToString(), null);
                                return;
                            }
                            createdGame.GameApiKey = prodKeyResponse.api_key;

                            ApiKey.CreateKey("stage", createdGame.DevelopmentGameId, stageKeyResponse =>
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

            EndPointClass endPoint = LootLockerTestConfigurationEndpoints.DeleteGame;
            var formattedEndpoint = string.Format(endPoint.endPoint, GameId);

            LootLockerAdminRequest.Send(formattedEndpoint, endPoint.httpMethod, null, (serverResponse) => {onComplete?.Invoke(serverResponse.success, serverResponse?.errorData?.message);}, true);
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
            LootLockerTestPlatform.UpdatePlatform("guest", true, new Dictionary<string, object>(), ActiveGameId,
                (success, errorMessage, Platform) =>
                {
                    if (success)
                    {
                        EnabledPlatforms.Add(Platform);
                    }
                    onComplete?.Invoke(success, errorMessage);
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