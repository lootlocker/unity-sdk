
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LootLocker
{
    #region Rate Limiting Support

    public class RateLimiter
    {
        protected bool EnableRateLimiter = true;

        protected bool FirstRequestSent = false;
        /* -- Configurable constants -- */
        // Tripwire settings, allow for a max total of n requests per x seconds
        protected const int TripWireTimeFrameSeconds = 60;
        protected const int MaxRequestsPerTripWireTimeFrame = 280;
        protected const int SecondsPerBucket = 5; // Needs to evenly divide the time frame

        // Moving average settings, allow for a max average of n requests per x seconds
        protected const float AllowXPercentOfTripWireMaxForMovingAverage = 0.8f; // Moving average threshold (the average number of requests per bucket) is set slightly lower to stop constant abusive call behaviour just under the tripwire limit
        protected const int CountMovingAverageAcrossNTripWireTimeFrames = 3; // Count Moving average across a longer time period

        /* -- Calculated constants -- */
        protected const int BucketsPerTimeFrame = TripWireTimeFrameSeconds / SecondsPerBucket;
        protected const int RateLimitMovingAverageBucketCount = CountMovingAverageAcrossNTripWireTimeFrames * BucketsPerTimeFrame;
        private const int MaxRequestsPerBucketOnMovingAverage = (int)((MaxRequestsPerTripWireTimeFrame * AllowXPercentOfTripWireMaxForMovingAverage) / (BucketsPerTimeFrame)); 


        /* -- Functionality -- */
        protected readonly int[] buckets = new int[RateLimitMovingAverageBucketCount];

        protected int lastBucket = -1;
        private DateTime _lastBucketChangeTime = DateTime.MinValue;
        private int _totalRequestsInBuckets;
        private int _totalRequestsInBucketsInTripWireTimeFrame;

        protected bool isRateLimited = false;
        private DateTime _rateLimitResolvesAt = DateTime.MinValue;

        protected virtual DateTime GetTimeNow()
        {
            return DateTime.Now;
        }

        public int GetSecondsLeftOfRateLimit()
        {
            if (!isRateLimited)
            {
                return 0;
            }
            return (int)Math.Ceiling((_rateLimitResolvesAt - GetTimeNow()).TotalSeconds);
        }
        private int MoveCurrentBucket(DateTime now)
        {
            int moveOverXBuckets = _lastBucketChangeTime == DateTime.MinValue ? 1 : (int)Math.Floor((now - _lastBucketChangeTime).TotalSeconds / SecondsPerBucket);
            if (moveOverXBuckets == 0)
            {
                return lastBucket;
            }

            for (int stepIndex = 1; stepIndex <= moveOverXBuckets; stepIndex++)
            {
                int bucketIndex = (lastBucket + stepIndex) % buckets.Length;
                if (bucketIndex == lastBucket)
                {
                    continue;
                }
                int bucketMovingOutOfTripWireTimeFrame = (bucketIndex - BucketsPerTimeFrame) < 0 ? buckets.Length + (bucketIndex - BucketsPerTimeFrame) : bucketIndex - BucketsPerTimeFrame;
                _totalRequestsInBucketsInTripWireTimeFrame -= buckets[bucketMovingOutOfTripWireTimeFrame]; // Remove the request count from the bucket that is moving out of the time frame from trip wire count
                _totalRequestsInBuckets -= buckets[bucketIndex]; // Remove the count from the bucket we're moving into from the total before emptying it
                buckets[bucketIndex] = 0;
            }

            return (lastBucket + moveOverXBuckets) % buckets.Length; // Step to next bucket and wrap around if necessary;
        }

        public virtual bool AddRequestAndCheckIfRateLimitHit()
        {
            //Disable local ratelimiter when not targeting production
            if (!FirstRequestSent)
            {
                EnableRateLimiter = LootLockerConfig.IsTargetingProductionEnvironment();
                FirstRequestSent = true;
            }

            if (!EnableRateLimiter)
            {
                return false;
            }

            DateTime now = GetTimeNow();
            var currentBucket = MoveCurrentBucket(now);

            if (isRateLimited)
            {
                if (_totalRequestsInBuckets <= 0)
                {
                    isRateLimited = false;
                    _rateLimitResolvesAt = DateTime.MinValue;
                }
            }
            else
            {
                buckets[currentBucket]++; // Increment the current bucket
                _totalRequestsInBuckets++; // Increment the total request count
                _totalRequestsInBucketsInTripWireTimeFrame++; // Increment the request count for the current time frame

                isRateLimited |= _totalRequestsInBucketsInTripWireTimeFrame >= MaxRequestsPerTripWireTimeFrame; // If the request count for the time frame is greater than the max requests per time frame, set isRateLimited to true
                isRateLimited |= _totalRequestsInBuckets / RateLimitMovingAverageBucketCount > MaxRequestsPerBucketOnMovingAverage; // If the average number of requests per bucket is greater than the max requests on moving average, set isRateLimited to true
#if UNITY_EDITOR
                if (_totalRequestsInBucketsInTripWireTimeFrame >= MaxRequestsPerTripWireTimeFrame) LootLockerLogger.Log("Rate Limit Hit due to Trip Wire, count = " + _totalRequestsInBucketsInTripWireTimeFrame + " out of allowed " + MaxRequestsPerTripWireTimeFrame);
                if (_totalRequestsInBuckets / RateLimitMovingAverageBucketCount > MaxRequestsPerBucketOnMovingAverage) LootLockerLogger.Log("Rate Limit Hit due to Moving Average, count = " + _totalRequestsInBuckets / RateLimitMovingAverageBucketCount + " out of allowed " + MaxRequestsPerBucketOnMovingAverage);
#endif
                if (isRateLimited)
                {
                    _rateLimitResolvesAt = (now - TimeSpan.FromSeconds(now.Second % SecondsPerBucket)) + TimeSpan.FromSeconds(buckets.Length*SecondsPerBucket);
                }
            }
            if (currentBucket != lastBucket)
            {
                _lastBucketChangeTime = now;
                lastBucket = currentBucket;
            }
            return isRateLimited;
        }

        protected int GetMaxRequestsInSingleBucket()
        {
            int maxRequests = 0;
            foreach (var t in buckets)
            {
                maxRequests = Math.Max(maxRequests, t);
            }

            return maxRequests;
        }

        private static RateLimiter _rateLimiter = null;

        public static RateLimiter Get()
        {
            if (_rateLimiter == null)
            {
                Reset();
            }
            return _rateLimiter;
        }

        public static void Reset()
        {
            _rateLimiter = new RateLimiter();
        }

#if UNITY_EDITOR
        [InitializeOnEnterPlayMode]
        static void OnEnterPlaymodeInEditor(EnterPlayModeOptions options)
        {
            Reset();
        }
#endif
    }
    #endregion
}
