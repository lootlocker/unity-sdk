using System;
using System.Collections;
using System.Collections.Generic;
using LootLocker;
using LootLocker.Requests;
using LootLockerTestConfigurationUtils;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public class RequestData
{
    public double requestDurationMs { get; set; } = 0.0f;
    public string requestIdentifier { get; set; } = Guid.NewGuid().ToString();
    public string requestPath { get; set; }
    public bool success { get; set; }
}

public class AverageRequestTimeTest
{
    private LootLockerTestGame gameUnderTest = null;
    private LootLockerConfig configCopy = null;
    private static int TestCounter = 0;
    private bool SetupFailed = false;
    string leaderboardKey = "gl_leaderboard";

    [UnitySetUp]
    public IEnumerator Setup()
    {
        yield return null;
    }

    public IEnumerator _Setup()
    {
        TestCounter++;
        configCopy = LootLockerConfig.current;

        if (!LootLockerConfig.ClearSettings())
        {
            Debug.LogError("Could not clear LootLocker config");
        }

        // Create game
        bool gameCreationCallCompleted = false;
        LootLockerTestGame.CreateGame(testName: "AverageRequestTimeTest" + TestCounter + " ", onComplete: (success, errorMessage, game) =>
        {
            if (!success)
            {
                SetupFailed = true;
                gameCreationCallCompleted = true;
                return;
            }
            gameUnderTest = game;
            gameCreationCallCompleted = true;
        });
        yield return new WaitUntil(() => gameCreationCallCompleted);
        if (SetupFailed)
        {
            yield break;
        }
        gameUnderTest?.SwitchToStageEnvironment();

        // Enable guest platform
        bool enableGuestLoginCallCompleted = false;
        gameUnderTest?.EnableGuestLogin((success, errorMessage) =>
        {
            if (!success)
            {
                SetupFailed = true;
            }
            enableGuestLoginCallCompleted = true;
        });
        yield return new WaitUntil(() => enableGuestLoginCallCompleted);
        if (SetupFailed)
        {
            yield break;
        }
        Assert.IsTrue(gameUnderTest?.InitializeLootLockerSDK(), "Failed to initialize LootLocker");

        for (int i = 0; i < 5; i++)
        {
            var createLeaderboardRequest = new CreateLootLockerLeaderboardRequest
            {
                name = $"Global Leaderboard {i}",
                key = $"{leaderboardKey}_{i}",
                direction_method = LootLockerLeaderboardSortDirection.descending.ToString(),
                enable_game_api_writes = true,
                has_metadata = true,
                overwrite_score_on_submit = false,
                type = "player"
            };

            bool leaderboardCreated = false;
            bool leaderboardSuccess = false;
            gameUnderTest.CreateLeaderboard(createLeaderboardRequest, (response) =>
            {
                leaderboardSuccess = response.success;
                SetupFailed |= !leaderboardSuccess;
                leaderboardCreated = true;

            });
            yield return new WaitUntil(() => leaderboardCreated);

            Assert.IsTrue(leaderboardSuccess, "Failed to create leaderboard");
        }
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        yield return _TearDown();
    }

    public IEnumerator _TearDown()
    {
        if (gameUnderTest != null)
        {
            bool gameDeletionCallCompleted = false;
            gameUnderTest.DeleteGame(((success, errorMessage) =>
            {
                if (!success)
                {
                    Debug.LogError(errorMessage);
                }

                gameUnderTest = null;
                gameDeletionCallCompleted = true;
            }));
            yield return new WaitUntil(() => gameDeletionCallCompleted);
        }

        LootLockerConfig.CreateNewSettings(configCopy.apiKey, configCopy.game_version, configCopy.domainKey,
            configCopy.currentDebugLevel, configCopy.allowTokenRefresh);
    }

    [UnityTest]
    [Timeout(7200000)]
    public IEnumerator AverageRequestTimeTest_DoesNotIncrease()
    {
        Assert.IsFalse(SetupFailed, "Setup did not succeed");

        double totalRequestsCount = 0.0f;
        double totalTotalRequestTime = 0.0f;
        double totalP10RequestTime = 0.0f;
        double totalP99RequestTime = 0.0f;
        double totalLongestRequestTime = 0.0f;
        for (int testRepeatTimes = 0; testRepeatTimes < 1; testRepeatTimes++)
        {
            yield return _Setup();

            Assert.IsFalse(SetupFailed, "Setup did not succeed");
            // Given
            List<RequestData> executedRequests = new List<RequestData>();
            int requestChainsToRun = 30;
            float secondsBetweenChains = 1.0f;
            DateTime TimeBeforeRequest = DateTime.MinValue;
            DateTime TimeAfterRequest = DateTime.MaxValue;
            bool requestSuccess = false;
            bool callCompleted = false;
            string playerPublicUID = null;
            int playerID = 0;

            // When
            for (int requestNr = 0; requestNr < requestChainsToRun; requestNr++)
            {
                // Sign in client
                TimeBeforeRequest = DateTime.Now;
                LootLockerSDKManager.StartGuestSession(GUID.Generate().ToString(), response =>
                {
                    requestSuccess = response.success;
                    playerPublicUID = response.public_uid;
                    playerID = response.player_id;
                    callCompleted = true;
                });
                yield return new WaitUntil(() => callCompleted);
                TimeAfterRequest = DateTime.Now;

                executedRequests.Add(new RequestData
                {
                    requestDurationMs = TimeAfterRequest.Subtract(TimeBeforeRequest).TotalMilliseconds,
                    requestPath = "v2/session/guest",
                    success = requestSuccess
                });

                LootLockerLeaderboardDetails[] leaderboards = new LootLockerLeaderboardDetails[] { };
                callCompleted = false;
                TimeBeforeRequest = DateTime.Now;
                LootLockerSDKManager.ListLeaderboards(100, 0, response =>
                {
                    requestSuccess = response.success;
                    leaderboards = response.items;
                    callCompleted = true;
                });
                yield return new WaitUntil(() => callCompleted);
                TimeAfterRequest = DateTime.Now;
                callCompleted = false;

                executedRequests.Add(new RequestData
                {
                    requestDurationMs = TimeAfterRequest.Subtract(TimeBeforeRequest).TotalMilliseconds,
                    requestPath = "leaderboards/",
                    success = requestSuccess
                });

                int i = 0;
                foreach (var leaderboard in leaderboards)
                {
                    i++;
                    TimeBeforeRequest = DateTime.Now;
                    callCompleted = false;
                    LootLockerSDKManager.SubmitScore(playerPublicUID, i * 312, leaderboard.key, response =>
                    {
                        requestSuccess = response.success;
                        callCompleted = true;
                    });
                    yield return new WaitUntil(() => callCompleted);
                    TimeAfterRequest = DateTime.Now;

                    executedRequests.Add(new RequestData
                    {
                        requestDurationMs = TimeAfterRequest.Subtract(TimeBeforeRequest).TotalMilliseconds,
                        requestPath = $"leaderboards/{leaderboard.key}/submit",
                        success = requestSuccess
                    });

                    TimeBeforeRequest = DateTime.Now;
                    callCompleted = false;
                    LootLockerSDKManager.GetScoreList(leaderboard.key, 100, 0, response =>
                    {
                        requestSuccess = response.success;
                        callCompleted = true;
                    });
                    yield return new WaitUntil(() => callCompleted);
                    TimeAfterRequest = DateTime.Now;

                    executedRequests.Add(new RequestData
                    {
                        requestDurationMs = TimeAfterRequest.Subtract(TimeBeforeRequest).TotalMilliseconds,
                        requestPath = $"leaderboards/{leaderboard.key}/list?count=100",
                        success = requestSuccess
                    });
                }

                TimeBeforeRequest = DateTime.Now;
                callCompleted = false;
                LootLockerSDKManager.GetAllMemberRanks(playerID, 100, response =>
                {
                    requestSuccess = response.success;
                    callCompleted = true;
                });
                yield return new WaitUntil(() => callCompleted);
                TimeAfterRequest = DateTime.Now;

                executedRequests.Add(new RequestData
                {
                    requestDurationMs = TimeAfterRequest.Subtract(TimeBeforeRequest).TotalMilliseconds,
                    requestPath = $"leaderboards/member/{playerID}?count=100",
                    success = requestSuccess
                });

                yield return new WaitForSeconds(secondsBetweenChains);
            }

            // Then
            double averageRequestTime = 0.0f;
            double totalRequestTime = 0.0f;
            int requestsCount = executedRequests.Count;
            int p99RequestCount = (int)Math.Floor((float)(requestsCount / 100));
            int p10RequestCount = (int)Math.Floor((float)(requestsCount / 10));
            List<double> p99Requests = new List<double>();
            List<double> p10Requests = new List<double>();

            double longestRequestTime = 0.0f;

            int j = 0;
            foreach (var request in executedRequests)
            {
                totalRequestTime += request.requestDurationMs;
                if (j <= p10RequestCount)
                {
                    p10Requests.Add(request.requestDurationMs);
                }
                else if (request.requestDurationMs < p10Requests[0])
                {
                    p10Requests[0] = request.requestDurationMs;
                }
                if (j <= p99RequestCount)
                {
                    p99Requests.Add(request.requestDurationMs);
                }
                else if (request.requestDurationMs > p99Requests[0])
                {
                    p99Requests[0] = request.requestDurationMs;
                }

                p10Requests.Sort();
                p10Requests.Reverse();
                p99Requests.Sort();
                if (request.requestDurationMs > longestRequestTime)
                {
                    longestRequestTime = request.requestDurationMs;
                }
                j++;
            }

            averageRequestTime = totalRequestTime / requestsCount;
            double p10RequestTime = p10Requests[0];
            double p99RequestTime = p99Requests[0];

            Debug.LogWarning($"Test Repeat #{testRepeatTimes}. {requestsCount} requests executed. Average time = {averageRequestTime}ms. The lowest 10% of requests were executed in {p10RequestTime}ms and the top 1% in {p99RequestTime}ms. The absolutely longest request took {longestRequestTime}ms");

            totalTotalRequestTime += totalRequestTime;
            totalRequestsCount += requestsCount;
            if(p10RequestTime > totalP10RequestTime)
            {
                totalP10RequestTime = p10RequestTime;
            }
            if (p99RequestTime > totalP99RequestTime)
            {
                totalP99RequestTime = p99RequestTime;
            }
            if (longestRequestTime > totalLongestRequestTime)
            {
                totalLongestRequestTime = longestRequestTime;
            }
            yield return _TearDown();
            /*Assert.IsTrue(averageRequestTime <= 75.0f, $"Average Request Time exceeded 75ms. Was {averageRequestTime}ms");
            Assert.IsTrue(p10RequestTime <= 10.0f, $"p10 Request Time exceeded 10ms. Was {p10RequestTime}ms");
            Assert.IsTrue(p99RequestTime <= 250.0f, $"p99 Request Time exceeded 250ms. Was {p99RequestTime}ms");
            Assert.IsTrue(longestRequestTime <= 700.0f, $"Longest Request Time exceeded 700ms. Was {longestRequestTime}ms");*/
        }
        var totalAverageRequestTime = totalTotalRequestTime / totalRequestsCount;
        Debug.LogWarning($"Total. {totalRequestsCount} requests executed. Average time = {totalAverageRequestTime}ms. The lowest 10% of requests were executed in {totalP10RequestTime}ms and the top 1% in {totalP99RequestTime}ms. The absolutely longest request took {totalLongestRequestTime}ms");

        Assert.IsTrue(totalAverageRequestTime <= 75.0f, $"Average Request Time exceeded 75ms. Was {totalAverageRequestTime}ms");
        Assert.IsTrue(totalP10RequestTime <= 10.0f, $"p10 Request Time exceeded 10ms. Was {totalP10RequestTime}ms");
        Assert.IsTrue(totalP99RequestTime <= 250.0f, $"p99 Request Time exceeded 250ms. Was {totalP99RequestTime}ms");
        Assert.IsTrue(totalLongestRequestTime <= 700.0f, $"Longest Request Time exceeded 700ms. Was {totalLongestRequestTime}ms");

        // NOTE: TIME LIMITS ARE SOURCED FROM A BUNCH OF PRE-REFACTORING TEST RUNS
        yield return null;
    }

    [UnityTest]
    public IEnumerator SimultaneousSimilarRequests_GetsIndividualAnswers()
    {
        yield return _Setup();
        Assert.IsFalse(SetupFailed, "Setup did not succeed");

        // Given
        List<string> playerPublicUIDS = new List<string>();
        string leaderboardKey = null;
        for(int i = 0; i < 250; i++)
        {
            bool guestLoginCompleted = false;
            LootLockerSDKManager.StartGuestSession(Guid.NewGuid().ToString(), response =>
            {
                playerPublicUIDS.Add(response.public_uid);
                guestLoginCompleted = true;
            });
            yield return new WaitUntil(() => guestLoginCompleted);

            if (leaderboardKey == null)
            {
                bool listingCompleted = false;
                LootLockerLeaderboardDetails[] leaderboards = null;
                LootLockerSDKManager.ListLeaderboards(100, 0, response =>
                {
                    leaderboards = response.items;
                    listingCompleted = true;
                });
                yield return new WaitUntil(() => listingCompleted);

                leaderboardKey = leaderboards[0].key;
            }

            bool submitCompleted = false;
            LootLockerSDKManager.SubmitScore(null, 100, leaderboardKey, response =>
            {
                submitCompleted = true;
            });
            yield return new WaitUntil(() => submitCompleted);
        }

        // When
        List<bool> fetchesCompleted = new List<bool>();
        List<LootLockerGetByListOfMembersResponse> fetchResponses = new List<LootLockerGetByListOfMembersResponse>();
        foreach(var ignored in playerPublicUIDS)
        {
            fetchesCompleted.Add(false);
            fetchResponses.Add(null);
        }

        int j = 0;
        foreach(var UID in playerPublicUIDS)
        {
            int fetchIndex = j;
            LootLockerSDKManager.GetByListOfMembers(new string[] { UID }, leaderboardKey, (LootLockerGetByListOfMembersResponse response) =>
            {
                fetchResponses[fetchIndex] = response;
                fetchesCompleted[fetchIndex] = true;
            });
            j++;
        }

        yield return new WaitUntil(() => { foreach (bool completed in fetchesCompleted) { if (!completed) return false; } return true; });

        // Then
        for(int k = 0; k < fetchResponses.Count; k++)
        {
            Assert.AreEqual(playerPublicUIDS[k], fetchResponses[k].members[0].member_id, "The right UID was not here");
        }
    }

    [UnityTest]
    [Ignore("Grouping as a feature was removed")]
    public IEnumerator SimultaneousSameRequests_AreGrouped()
    {
        yield return _Setup();
        Assert.IsFalse(SetupFailed, "Setup did not succeed");

        // Given
        List<string> playerPublicUIDS = new List<string>();
        string leaderboardKey = null;
        for (int i = 0; i < 250; i++)
        {
            bool guestLoginCompleted = false;
            LootLockerSDKManager.StartGuestSession(Guid.NewGuid().ToString(), response =>
            {
                playerPublicUIDS.Add(response.public_uid);
                guestLoginCompleted = true;
            });
            yield return new WaitUntil(() => guestLoginCompleted);

            if (leaderboardKey == null)
            {
                bool listingCompleted = false;
                LootLockerLeaderboardDetails[] leaderboards = null;
                LootLockerSDKManager.ListLeaderboards(100, 0, response =>
                {
                    leaderboards = response.items;
                    listingCompleted = true;
                });
                yield return new WaitUntil(() => listingCompleted);

                leaderboardKey = leaderboards[0].key;
            }

            bool submitCompleted = false;
            LootLockerSDKManager.SubmitScore(null, 100, leaderboardKey, response =>
            {
                submitCompleted = true;
            });
            yield return new WaitUntil(() => submitCompleted);
        }

        // When
        List<bool> fetchesCompleted = new List<bool>();
        List<LootLockerGetByListOfMembersResponse> fetchResponses = new List<LootLockerGetByListOfMembersResponse>();
        foreach (var ignored in playerPublicUIDS)
        {
            fetchesCompleted.Add(false);
            fetchResponses.Add(null);
        }

        int j = 0;
        foreach (var UID in playerPublicUIDS)
        {
            int fetchIndex = j;
            LootLockerSDKManager.GetByListOfMembers(new string[] { playerPublicUIDS[0] }, leaderboardKey, (LootLockerGetByListOfMembersResponse response) =>
            {
                fetchResponses[fetchIndex] = response;
                fetchesCompleted[fetchIndex] = true;
            });
            j++;

            if (j % 25 == 0)
            {
                yield return new WaitForSeconds(0.1f);
            }
        }

        yield return new WaitUntil(() => { foreach (bool completed in fetchesCompleted) { if (!completed) return false; } return true; });

        Dictionary<string, int> eventIdCounts = new Dictionary<string, int>();
        // Then
        foreach(var resp in fetchResponses)
        {
            if(eventIdCounts.TryGetValue(resp.EventId, out var val))
            {
                eventIdCounts[resp.EventId] = eventIdCounts[resp.EventId]+1;
                continue;
            }
            eventIdCounts.Add(resp.EventId, 1);
        }
        Assert.AreNotEqual(eventIdCounts.Keys.Count, fetchResponses.Count);
        int greatest = 0;
        foreach(var val in eventIdCounts.Values)
        {
            if(val > greatest)
            {
                greatest = val;
            }
        }
        Debug.Log("Largest Request Grouping was " + greatest);
        Assert.Greater(greatest, 1);
    }

}
