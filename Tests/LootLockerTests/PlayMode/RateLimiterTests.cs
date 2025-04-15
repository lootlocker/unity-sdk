using System;
using System.Collections;
using System.Collections.Generic;
using LootLocker;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LootLockerTests.PlayMode
{
    public class RateLimiterTests
    {

        class TestRateLimiter : RateLimiter
        {
            public TestRateLimiter() : base()
            {
                EnableRateLimiter = true;
                FirstRequestSent = true;
            }
            private DateTime _currentTime;
            protected override DateTime GetTimeNow()
            {
                return _currentTime;
            } 

            public void SetTime(DateTime newTime)
            {
                _currentTime = newTime;
            }

            public DateTime GetCurrentTime()
            {
                return _currentTime;
            }

            public void AddSecondsToCurrentTime(int seconds)
            {
                _currentTime = _currentTime.AddSeconds(seconds);
            }

            public int GetRateLimitSecondsTimeFrame() { return TripWireTimeFrameSeconds; }
            public int GetCountMovingAverageAcrossNTimeFrames() { return CountMovingAverageAcrossNTripWireTimeFrames; }
            public int GetRateLimitMaxRequestsPerTimeFrame() { return MaxRequestsPerTripWireTimeFrame; }
            public int GetRateLimitSecondsBucketSize() { return SecondsPerBucket; }
            public int GetBucketsPerTimeFrame() { return BucketsPerTimeFrame; }
            public int GetRateLimitMovingAverageBucketCount() { return RateLimitMovingAverageBucketCount; }

            public override bool AddRequestAndCheckIfRateLimitHit()
            {
                bool wasRateLimited = isRateLimited;
                bool didHitRateLimit = base.AddRequestAndCheckIfRateLimitHit();

                // Debugging
                if (!wasRateLimited && didHitRateLimit)
                {
                    DrawDebugGraph();
                }
                return didHitRateLimit;
            }

            // Visualize the buckets
            public void DrawDebugGraph()
            {
                char[][] bucketsCharMatrix = GetBucketsAsCharMatrix();

                List<string> graphStrings = new List<string>();
                string firstAndLastRow = "";
                for (int widthIndex = 0; widthIndex < bucketsCharMatrix.Length + 4; widthIndex++)
                {
                    firstAndLastRow += '@';
                }


                int startOfTimeFrameIndex = (lastBucket + 1 - BucketsPerTimeFrame) < 0
                    ? buckets.Length + (lastBucket + 1 - BucketsPerTimeFrame)
                    : lastBucket - BucketsPerTimeFrame;
                int endOfTimeFrameIndex = lastBucket;
                graphStrings.Add(firstAndLastRow);
                for (int heightIndex = 0; heightIndex < bucketsCharMatrix[0].Length; heightIndex++)
                {
                    string row = "";
                    row += '@';
                    for (int widthIndex = 0; widthIndex < bucketsCharMatrix.Length; widthIndex++)
                    {
                        if (widthIndex == startOfTimeFrameIndex)
                            row += '|';
                        row += bucketsCharMatrix[widthIndex][heightIndex];
                        if (widthIndex == endOfTimeFrameIndex)
                            row += '|';
                    }
                    row += '@';

                    graphStrings.Add(row);
                }

                graphStrings.Add(firstAndLastRow);
                Debug.Log("### Rate Limiting graph ###");
                foreach (string s in graphStrings)
                {
                    Debug.Log(s);
                }
            }

            private char[][] GetBucketsAsCharMatrix()
            {
                char[][] visualized = new char[RateLimitMovingAverageBucketCount][];
                int maxVal = GetMaxRequestsInSingleBucket();

                for (var i = 0; i < visualized.Length; i++)
                {
                    visualized[i] = new char[maxVal];
                }

                for (var i = 0; i < visualized.Length; i++)
                {
                    int barHeight = buckets[i];
                    for (var heightIndex = visualized[i].Length - 1; heightIndex >= 0; heightIndex--)
                    {
                        if (visualized[i].Length - heightIndex <= barHeight)
                        {
                            visualized[i][heightIndex] = '#';
                        }
                        else
                        {
                            visualized[i][heightIndex] = '-';
                        }
                    }
                }
                return visualized;
            }
        }

        private TestRateLimiter _rateLimiterUnderTest = null;

        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            _rateLimiterUnderTest = new TestRateLimiter();
            _rateLimiterUnderTest.SetTime(new DateTime(2021, 1, 1, 0, 0, 0));
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            // Cleanup
            _rateLimiterUnderTest = null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator RateLimiter_NormalAmountOfAverageRequests_DoesNotHitRateLimit()
        {
            // Given
            int secondsToRunTest = 360;
            int requestsPerSecond = 3;
            bool wasRateLimitHit = false;

            _rateLimiterUnderTest.AddRequestAndCheckIfRateLimitHit();
            // When
            for (int i = 0; i < secondsToRunTest; i++)
            {
                for (int j = 0; j < requestsPerSecond; j++)
                {
                    wasRateLimitHit |= _rateLimiterUnderTest.AddRequestAndCheckIfRateLimitHit();
                }
                _rateLimiterUnderTest.AddSecondsToCurrentTime(1);
            }

            // Then
            Assert.IsFalse(wasRateLimitHit, "Rate limit was hit when it should not have been");
            yield return null;
        }

        [UnityTest]
        public IEnumerator RateLimiter_UndulatingLowLevelOfRequests_DoesNotHitRateLimit()
        {
            // Given
            int secondsToRunTest = 360;
            int undulatingModuloMax = 6; // 1 + 2 + 3 + 4 + 5 + 6 gives an average of 3.5 which is less than the 18 per bucket that triggers moving average rate limit
            bool wasRateLimitHit = false;
            
            // When
            for (int i = 0; i < secondsToRunTest; i++)
            {
                int requestsThisSecond = (i % undulatingModuloMax) + 1;
                for (int j = 0; j < requestsThisSecond; j++)
                {
                    wasRateLimitHit |= _rateLimiterUnderTest.AddRequestAndCheckIfRateLimitHit();
                }
                _rateLimiterUnderTest.AddSecondsToCurrentTime(1);

                if (wasRateLimitHit)
                {
                    break;
                }
            }
            
            // Then
            Assert.IsFalse(wasRateLimitHit, "Rate limit was hit when it should not have been");
            yield return null;
        }

        [UnityTest]
        public IEnumerator RateLimiter_FrequentSmallBursts_DoesNotHitRateLimit()
        {
            // Given
            int secondsToRunTest = 360;
            int requestPerBurst = 9;
            int sendRequestsEveryXSeconds = 3;
            bool wasRateLimitHit = false;
            int rateLimitHitAfterSeconds = -1;

            _rateLimiterUnderTest.AddRequestAndCheckIfRateLimitHit();
            // When
            for (int i = 0; i < secondsToRunTest; i++)
            {
                if (i % sendRequestsEveryXSeconds == 0)
                {
                    for (int j = 0; j < requestPerBurst; j++)
                    {
                        wasRateLimitHit |= _rateLimiterUnderTest.AddRequestAndCheckIfRateLimitHit();
                    }
                }

                if (wasRateLimitHit)
                {
                    rateLimitHitAfterSeconds = i;
                    break;
                }
                _rateLimiterUnderTest.AddSecondsToCurrentTime(1);
            }

            // Then
            Assert.IsTrue(rateLimitHitAfterSeconds < 0, "Rate limit was hit after " + rateLimitHitAfterSeconds + " seconds, expected it not to be hit");
            Assert.IsFalse(wasRateLimitHit, "Rate limit was hit when it should not have been");
            yield return null;
        }

        [UnityTest]
        public IEnumerator RateLimiter_InfrequentLargeBursts_DoesNotHitRateLimit()
        {
            // Given
            int secondsToRunTest = 360;
            int sendBurstsEveryXSeconds = 10;
            int requestsPerBurst = 35;
            bool wasRateLimitHit = false;
            int rateLimitHitAfterSeconds = -1;

            _rateLimiterUnderTest.AddRequestAndCheckIfRateLimitHit();
            // When
            for (int i = 0; i < secondsToRunTest; i++)
            {
                if (i % sendBurstsEveryXSeconds == 0)
                {
                    for (int j = 0; j < requestsPerBurst; j++)
                    {
                        wasRateLimitHit |= _rateLimiterUnderTest.AddRequestAndCheckIfRateLimitHit();
                        if (wasRateLimitHit)
                        {
                            break;
                        }
                    }
                }

                if (wasRateLimitHit)
                {
                    rateLimitHitAfterSeconds = i;
                    break;
                }
                _rateLimiterUnderTest.AddSecondsToCurrentTime(1);
            }

            // Then
            Assert.IsFalse(wasRateLimitHit, "Rate limit was hit when it should not have been");
            Assert.IsTrue(rateLimitHitAfterSeconds < 0, "Rate limit was hit after " + rateLimitHitAfterSeconds + " seconds, expected it not to be hit");
            yield return null;
        }

        [UnityTest]
        public IEnumerator RateLimiter_ExcessiveQuickSuccessionRequests_HitsTripwireRateLimit()
        {
            // Given
            int maxSecondsToRunTest = 90;
            int requestsPerSecond = 6;
            bool wasRateLimitHit = false;
            int rateLimitHitAfterSeconds = -1;

            _rateLimiterUnderTest.AddRequestAndCheckIfRateLimitHit();
            // When
            for (int i = 0; i < maxSecondsToRunTest; i++)
            {
                for (int j = 0; j < requestsPerSecond; j++)
                {
                    wasRateLimitHit |= _rateLimiterUnderTest.AddRequestAndCheckIfRateLimitHit();
                }

                if (wasRateLimitHit)
                {
                    rateLimitHitAfterSeconds = i;
                    break;
                }
                _rateLimiterUnderTest.AddSecondsToCurrentTime(1);
            }

            // Then
            Assert.IsTrue(wasRateLimitHit, "Rate limit wasn't hit within the time limit");
            Assert.IsTrue(rateLimitHitAfterSeconds < 56, "Rate limit was hit after " + rateLimitHitAfterSeconds + " seconds, expected less than 56");
            yield return null;
        }

        [UnityTest]
        public IEnumerator RateLimiter_LowLevelBackgroundRequestsWithIntermittentBursts_HitsRateLimit()
        {
            // Given
            int maxSecondsToRunTest = 360;
            int requestsPerSecond = 2;
            int requestsPerBurst = 110;
            int sendBurstsEveryXSeconds = 29;
            bool wasRateLimitHit = false;

            // When
            for (int i = 0; i < maxSecondsToRunTest; i++)
            {
                for (int j = 0; j < requestsPerSecond; j++)
                {
                    wasRateLimitHit |= _rateLimiterUnderTest.AddRequestAndCheckIfRateLimitHit();
                }

                if (i % sendBurstsEveryXSeconds == 0)
                {
                    for (int j = 0; j < requestsPerBurst; j++)
                    {
                        wasRateLimitHit |= _rateLimiterUnderTest.AddRequestAndCheckIfRateLimitHit();
                        if (wasRateLimitHit)
                        {
                            break;
                        }
                    }
                }

                if (wasRateLimitHit)
                {
                    break;
                }
                _rateLimiterUnderTest.AddSecondsToCurrentTime(1);
            }

            // Then
            Assert.IsTrue(wasRateLimitHit, "Rate limit wasn't hit within the time limit");
            yield return null;
        }

        [UnityTest]
        public IEnumerator RateLimiter_SuddenHugeBurstBelowLimit_DoesNotTriggerRateLimit()
        {
            // Given
            int maxSecondsToRunTest = 90;
            int requestsPerBurst = 275;
            int sendBurstsEveryXSeconds = 80;
            bool wasRateLimitHit = false;

            _rateLimiterUnderTest.AddRequestAndCheckIfRateLimitHit();
            _rateLimiterUnderTest.AddSecondsToCurrentTime(1);
            // When
            for (int i = 1; i < maxSecondsToRunTest; i++)
            {
                if (i % sendBurstsEveryXSeconds == 0)
                {
                    for (int j = 0; j < requestsPerBurst; j++)
                    {
                        wasRateLimitHit |= _rateLimiterUnderTest.AddRequestAndCheckIfRateLimitHit();
                        if (wasRateLimitHit)
                        {
                            break;
                        }
                    }
                }

                if (wasRateLimitHit)
                {
                    break;
                }
                _rateLimiterUnderTest.AddSecondsToCurrentTime(1);
            }

            // Then
            Assert.IsFalse(wasRateLimitHit, "Rate limit was hit when it should not have been");
            yield return null;
        }

        [UnityTest]
        public IEnumerator RateLimiter_SuddenHugeBurstAbove_LimitTriggersRateLimit()
        {
            // Given
            int maxSecondsToRunTest = 90;
            int requestsPerBurst = 300;
            int sendBurstsEveryXSeconds = 80;
            bool wasRateLimitHit = false;

            _rateLimiterUnderTest.AddRequestAndCheckIfRateLimitHit();
            _rateLimiterUnderTest.AddSecondsToCurrentTime(1);
            // When
            for (int i = 1; i < maxSecondsToRunTest; i++)
            {
                if (i % sendBurstsEveryXSeconds == 0)
                {
                    for (int j = 0; j < requestsPerBurst; j++)
                    {
                        wasRateLimitHit |= _rateLimiterUnderTest.AddRequestAndCheckIfRateLimitHit();
                        if (wasRateLimitHit)
                        {
                            break;
                        }
                    }
                }

                if (wasRateLimitHit)
                {
                    break;
                }
                _rateLimiterUnderTest.AddSecondsToCurrentTime(1);
            }

            // Then
            Assert.IsTrue(wasRateLimitHit, "Rate limit wasn't hit within the time limit");
            yield return null;
        }

        [UnityTest]
        public IEnumerator RateLimiter_SuddenHugeBurstBelowLimitFollowedByAFewRequests_TriggersRateLimit()
        {
            // Given
            int maxSecondsToRunTest = 120;
            int requestsPerBurst = 260;
            int requestsPerSecondAfterBurst = 2;
            int sendBurstsEveryXSeconds = 80;
            bool wasRateLimitHit = false;

            _rateLimiterUnderTest.AddRequestAndCheckIfRateLimitHit();
            _rateLimiterUnderTest.AddSecondsToCurrentTime(1);
            // When
            for (int i = 1; i < maxSecondsToRunTest; i++)
            {
                if (i % sendBurstsEveryXSeconds == 0)
                {
                    for (int j = 0; j < requestsPerBurst; j++)
                    {
                        wasRateLimitHit |= _rateLimiterUnderTest.AddRequestAndCheckIfRateLimitHit();
                        if (wasRateLimitHit)
                        {
                            break;
                        }
                    }
                }

                if (i > sendBurstsEveryXSeconds)
                {
                    for (int j = 0; j < requestsPerSecondAfterBurst; j++)
                    {
                        wasRateLimitHit |= _rateLimiterUnderTest.AddRequestAndCheckIfRateLimitHit();
                        if (wasRateLimitHit)
                        {
                            break;
                        }
                    }
                }

                if (wasRateLimitHit)
                {
                    break;
                }
                _rateLimiterUnderTest.AddSecondsToCurrentTime(1);
            }

            // Then
            Assert.IsTrue(wasRateLimitHit, "Rate limit wasn't hit within the time limit");
            yield return null;
        }

        [UnityTest]
        public IEnumerator RateLimiter_ConstantRequestsBelowTripWire_HitsMovingAverageRateLimit()
        {
            // Given
            int maxSecondsToRunTest = 360;
            int requestsPerSecond = 4;
            bool wasRateLimitHit = false;

            _rateLimiterUnderTest.AddRequestAndCheckIfRateLimitHit();
            _rateLimiterUnderTest.AddSecondsToCurrentTime(1);

            // When
            for (int i = 0; i < maxSecondsToRunTest; i++)
            {
                for (int j = 0; j < requestsPerSecond; j++)
                {
                    wasRateLimitHit |= _rateLimiterUnderTest.AddRequestAndCheckIfRateLimitHit();
                }
                _rateLimiterUnderTest.AddSecondsToCurrentTime(1);
            }

            // Then
            Assert.IsTrue(wasRateLimitHit, "Rate limit wasn't hit within the time limit");
            yield return null;
        }

        [UnityTest]
        public IEnumerator RateLimiter_RateLimiterHit_ResetsAfter3Minutes()
        {
            // Given
            int maxSecondsToRunTest = 480;
            int expectedMaxSecondsToReset = 180;
            int expectedMinSecondsToReset = 120;
            int actualSecondsToReset = 0;
            int requestsPerSecond = 20;
            bool isRateLimited = false;

            _rateLimiterUnderTest.AddRequestAndCheckIfRateLimitHit();

            // When
            for (int i = 0; i < maxSecondsToRunTest; i++)
            {
                bool wasRateLimited = isRateLimited;
                for (int j = 0; j < requestsPerSecond; j++)
                {
                    isRateLimited = _rateLimiterUnderTest.AddRequestAndCheckIfRateLimitHit();
                }

                if (isRateLimited)
                {
                    actualSecondsToReset++;
                }

                if (wasRateLimited && !isRateLimited)
                {
                    break;
                }
                _rateLimiterUnderTest.AddSecondsToCurrentTime(1);
            }

            // Then
            Assert.IsFalse(isRateLimited, "Rate Limit did not reset in the allotted period");
            Assert.IsTrue(actualSecondsToReset < expectedMaxSecondsToReset, "Rate Limiting was not reset in the expected time frame. Reset too slowly.");
            Assert.IsTrue(actualSecondsToReset > expectedMinSecondsToReset, "Rate Limiting was not reset in the expected time frame. Reset too quickly.");
            yield return null;
        }
    }
}
