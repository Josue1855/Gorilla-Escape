using System;
using UnityEngine;

namespace GorillaEscape.Minigames.GorillaSmash
{
    public enum GorillaSmashState
    {
        Ready,           // Waiting to start an attempt
        Armed,           // Player held weapon ready; awaiting downward strike
        Striking,        // Active downward swing in progress
        Impact,          // Struck the base/anvil; evaluating technique and score
        Elevating,       // Indicator puck ascending the high striker tower
        AttemptFinished, // Puck reached peak, score & feedback shown, ready for next attempt
        Completed        // 3 attempts finished; final result published
    }

    public enum GorillaSmashTier
    {
        MissWeak,  // < 2500
        Good,      // 2500 - 4999
        Great,     // 5000 - 7499
        Perfect,   // 7500 - 9499
        Legendary  // 9500+
    }

    [Serializable]
    public sealed class GorillaSmashAttempt
    {
        public int attemptNumber;
        public float power;
        public float directionAccuracy;
        public float timing;
        public float gestureConfidence;
        public float technique;
        public int score;
        public GorillaSmashTier tier;
        public bool bellHit;
        public float peakHeight;
        public long timestamp;

        public GorillaSmashAttempt(
            int attemptNumber,
            float power,
            float directionAccuracy,
            float timing,
            float gestureConfidence,
            float technique,
            int score,
            GorillaSmashTier tier,
            bool bellHit,
            float peakHeight,
            long timestamp)
        {
            this.attemptNumber = attemptNumber;
            this.power = power;
            this.directionAccuracy = directionAccuracy;
            this.timing = timing;
            this.gestureConfidence = gestureConfidence;
            this.technique = technique;
            this.score = score;
            this.tier = tier;
            this.bellHit = bellHit;
            this.peakHeight = peakHeight;
            this.timestamp = timestamp;
        }
    }

    [Serializable]
    public sealed class GorillaSmashResult
    {
        public int PlayerId { get; }
        public bool Completed { get; }
        public int TotalScore { get; }
        public int BestScore { get; }
        public GorillaSmashTier BestTier { get; }
        public GorillaSmashAttempt[] Attempts { get; }
        public string Reason { get; }

        public GorillaSmashResult(
            int playerId,
            bool completed,
            int totalScore,
            int bestScore,
            GorillaSmashTier bestTier,
            GorillaSmashAttempt[] attempts,
            string reason)
        {
            PlayerId = playerId;
            Completed = completed;
            TotalScore = totalScore;
            BestScore = bestScore;
            BestTier = bestTier;
            Attempts = attempts;
            Reason = reason;
        }
    }

    [Serializable]
    public sealed class GorillaSmashRules
    {
        public int maxAttempts = 3;

        // Weights conforming strictly to Specification Section 17
        public float powerWeight = 0.45f;
        public float directionWeight = 0.25f;
        public float timingWeight = 0.20f;
        public float confidenceWeight = 0.10f;

        // Anti-cheat & gesture recognition constraints (DEC-009)
        public float armingHoldSeconds = 0.25f;
        public float minStrikeAcceleration = 1.2f;
        public float maxTransverseTolerance = 0.45f;
        public float cooldownSeconds = 0.60f;
        public int maxShakeReversals = 2;
        public float sweetSpotTimingWindow = 0.45f;

        // Tower & puck physics simulation
        public float towerHeight = 10f;
        public float bellScoreThreshold = 9500f;
        public float puckAscentSpeed = 16f;
        public float puckGravity = 18f;
        public float dwellResultSeconds = 1.5f;

        public void Validate()
        {
            if (maxAttempts < 1)
                throw new ArgumentException("maxAttempts must be at least 1.");

            float weightSum = powerWeight + directionWeight + timingWeight + confidenceWeight;
            if (Math.Abs(weightSum - 1.0f) > 0.005f)
                throw new ArgumentException($"Weights must sum to 1.0 (actual: {weightSum:F4}).");

            foreach (float v in new[] {
                powerWeight, directionWeight, timingWeight, confidenceWeight,
                armingHoldSeconds, minStrikeAcceleration, maxTransverseTolerance,
                cooldownSeconds, sweetSpotTimingWindow, towerHeight,
                bellScoreThreshold, puckAscentSpeed, puckGravity, dwellResultSeconds
            })
            {
                if (float.IsNaN(v) || float.IsInfinity(v))
                    throw new ArgumentException("Rules parameters must be finite.");
            }

            if (towerHeight <= 0 || armingHoldSeconds < 0 || cooldownSeconds < 0 || puckGravity <= 0)
                throw new ArgumentException("Physical parameters must be strictly positive.");
        }

        public GorillaSmashRules Copy() => (GorillaSmashRules)MemberwiseClone();
    }
}
