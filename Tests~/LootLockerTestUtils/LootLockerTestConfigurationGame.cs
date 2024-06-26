using LootLocker;
using System;
using static LootLockerTestConfigurationUtils.AuthUtil;
using static UnityEngine.Random;

namespace LootLockerTestConfigurationUtils
{
    public class LootLockerTestConfigurationGame
    {
        public int GameId { get; set; }
        public string GameDomainKey { get; set; }
        public string GameApiKey { get; set; }
        public int DevelopmentGameId { get; set; }
        public string DevelopmentGameApiKey { get; set; }
        public int OrganisationId { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }

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

        public class CreateGameRequest
        {
            public string name { get; set; }
            public int genre { get; set; } = 1;
            public int organisation_id { get; set; }
        }

        public class DevelopmentGame
        {
            public int id { get; set; }
        }

        public class CreateGameResponse : LootLockerResponse
        {
            public int id { get; set; }
            public string domain_key { get; set; }
            public DevelopmentGame development { get; set; }
        }

        public static void CreateGame(Action<bool /*success*/, string /*errorMessage*/, LootLockerTestConfigurationGame /*game*/> onComplete)
        {
            var userGuid = Guid.NewGuid();
            UnityEngine.Random.InitState(userGuid.GetHashCode());
            var userNoun = OneHundredNouns[UnityEngine.Random.Range(0, 99)];
            var userVerb = OneHundredVerbs[UnityEngine.Random.Range(0, 99)];
            var userName = userVerb + userNoun + userGuid;

            AuthUtil.Signup("sdk+ci-"+userName, userGuid.ToString(), userName, userName, signupResponse =>
            {
                if (!signupResponse.success)
                {
                    onComplete?.Invoke(false, signupResponse.errorData.ToString(), null);
                    return;
                }

                LootLockerTestConfigurationGame createdGame = new LootLockerTestConfigurationGame();
                createdGame.User = signupResponse.user;
                createdGame.UserId = createdGame.User.id;
                createdGame.OrganisationId = createdGame.User.organisations[0].id;

                EndPointClass endPoint = LootLockerTestConfigurationEndpoints.CreateGame;

                CreateGameRequest createGameRequest = new CreateGameRequest
                    { name = "game-by-" + userName, organisation_id = createdGame.OrganisationId };

                string json = LootLockerJson.SerializeObject(createGameRequest);

                LootLockerAdminRequest.Send(endPoint.endPoint, endPoint.httpMethod, json,
                    onComplete: (serverResponse) =>
                    {
                        var createGameResponse = LootLockerResponse.Deserialize<CreateGameResponse>(serverResponse);
                        if (createGameResponse == null || !createGameResponse.success)
                        {
                            onComplete?.Invoke(false, signupResponse.errorData.ToString(), null);
                            return;
                        }

                        createdGame.GameId = createGameResponse.id;
                        createdGame.DevelopmentGameId = createGameResponse.development.id;
                        createdGame.GameDomainKey = createGameResponse.domain_key;

                        ApiKeyUtil.CreateKey("prod", createdGame.GameId, prodKeyResponse =>
                        {
                            if (!prodKeyResponse.success)
                            {
                                onComplete?.Invoke(false, prodKeyResponse.errorData.ToString(), null);
                                return;
                            }
                            createdGame.GameApiKey = prodKeyResponse.api_key;

                            ApiKeyUtil.CreateKey("stage", createdGame.DevelopmentGameId, stageKeyResponse =>
                            {
                                if (!stageKeyResponse.success)
                                {
                                    onComplete?.Invoke(false, stageKeyResponse.errorData.ToString(), null);
                                    return;
                                }

                                createdGame.DevelopmentGameApiKey = stageKeyResponse.api_key;
                                LootLockerConfig.current.apiKey = createdGame.DevelopmentGameApiKey;
                                LootLockerConfig.current.domainKey = createdGame.GameDomainKey;
                                LootLockerConfig.current.game_version = "0.0.1";

                                onComplete?.Invoke(true, "", createdGame);
                            });
                        });
                    }, true);
            });
            
        }
    }
}