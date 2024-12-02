using System;

namespace LootLockerTestConfigurationUtils
{
    public class LootLockerTestConfigurationUtilities
    {
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

        public static string GetRandomNoun()
        {
            return OneHundredNouns[UnityEngine.Random.Range(0, 99)];
        }

        public static string GetRandomVerb()
        {
            return OneHundredVerbs[UnityEngine.Random.Range(0, 99)];
        }
    }
}