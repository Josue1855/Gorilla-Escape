using System;
using System.Collections.Generic;
using UnityEngine;

namespace GorillaEscape.Minigames.GorillaSmash
{
    /// <summary>
    /// Pure domain model for Gorilla Smash minigame.
    /// Manages attempt progression (3 attempts per player), anti-cheat state validation,
    /// high striker physics substeps, scoring formula according to Specification v4 Section 17,
    /// and final tournament result establishment.
    /// Completely decoupled from UnityEngine.MonoBehaviour for rapid EditMode unit testing.
    /// </summary>
    public sealed class GorillaSmashModel
    {
        readonly GorillaSmashRules rules;
        readonly List<GorillaSmashAttempt> attempts = new List<GorillaSmashAttempt>();

        int playerId;
        float stateTimer;
        float cooldownTimer;
        float armingDwellTimer;
        float targetPuckHeight;
        float puckHeight;
        float puckVelocity;
        bool bellRungThisAttempt;

        public GorillaSmashRules Rules => rules;
        public GorillaSmashState State { get; private set; }
        public int PlayerId => playerId;
        public int CurrentAttemptNumber => attempts.Count + (State == GorillaSmashState.Completed ? 0 : 1);
        public IReadOnlyList<GorillaSmashAttempt> Attempts => attempts;
        public GorillaSmashAttempt LatestAttempt { get; private set; }
        public GorillaSmashResult Result { get; private set; }
        public int BestScore { get; private set; }
        public GorillaSmashTier BestTier { get; private set; } = GorillaSmashTier.MissWeak;
        public float PuckHeight => puckHeight;
        public float TargetPuckHeight => targetPuckHeight;
        public bool BellRung => bellRungThisAttempt;
        public float ArmingDwellTimer => armingDwellTimer;
        public float CooldownTimer => cooldownTimer;

        public GorillaSmashModel(GorillaSmashRules rules = null)
        {
            this.rules = rules != null ? rules.Copy() : new GorillaSmashRules();
            this.rules.Validate();
            Reset();
        }

        public void Reset()
        {
            State = GorillaSmashState.Ready;
            playerId = 0;
            stateTimer = 0;
            cooldownTimer = 0;
            armingDwellTimer = 0;
            targetPuckHeight = 0;
            puckHeight = 0;
            puckVelocity = 0;
            bellRungThisAttempt = false;
            attempts.Clear();
            LatestAttempt = null;
            Result = null;
            BestScore = 0;
            BestTier = GorillaSmashTier.MissWeak;
        }

        public void Begin(int player)
        {
            if (player < 1 || player > 4)
                throw new ArgumentOutOfRangeException(nameof(player), "PlayerId must be between 1 and 4.");

            Reset();
            playerId = player;
            State = GorillaSmashState.Ready;
        }

        /// <summary>
        /// Signal arming posture (phone raised, mallet cocked overhead).
        /// Required by Section 17 anti-cheat before downward swing can count.
        /// </summary>
        public void SetArming(bool armed)
        {
            if (State != GorillaSmashState.Ready && State != GorillaSmashState.Armed)
                return;

            if (armed)
            {
                State = GorillaSmashState.Armed;
            }
            else
            {
                armingDwellTimer = 0;
                State = GorillaSmashState.Ready;
            }
        }

        /// <summary>
        /// Triggers a smash strike execution.
        /// Validates state, anti-cheat arming hold, and cooldown.
        /// </summary>
        public bool TriggerStrike(float power, float directionAccuracy, float timing, float gestureConfidence, long timestamp = 0)
        {
            if (State != GorillaSmashState.Armed && State != GorillaSmashState.Ready)
                return false;

            if (cooldownTimer > 0)
                return false;

            // Anti-cheat: Must have dwelt in armed state unless configured to 0
            if (rules.armingHoldSeconds > 0 && armingDwellTimer < rules.armingHoldSeconds)
                return false;

            // Clamp normalized inputs to [0, 1]
            power = Mathf.Clamp01(power);
            directionAccuracy = Mathf.Clamp01(directionAccuracy);
            timing = Mathf.Clamp01(timing);
            gestureConfidence = Mathf.Clamp01(gestureConfidence);

            // Specification v4 Section 17:
            // Technique = Power * 0.45 + DirectionAccuracy * 0.25 + Timing * 0.20 + GestureConfidence * 0.10
            float technique = CalculateTechnique(power, directionAccuracy, timing, gestureConfidence, rules);
            int score = CalculateScore(technique);
            GorillaSmashTier tier = GetTier(score);

            targetPuckHeight = (score / 10000f) * rules.towerHeight;
            bool hitBell = score >= (int)rules.bellScoreThreshold;

            LatestAttempt = new GorillaSmashAttempt(
                attemptNumber: attempts.Count + 1,
                power: power,
                directionAccuracy: directionAccuracy,
                timing: timing,
                gestureConfidence: gestureConfidence,
                technique: technique,
                score: score,
                tier: tier,
                bellHit: hitBell,
                peakHeight: targetPuckHeight,
                timestamp: timestamp > 0 ? timestamp : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            );

            bellRungThisAttempt = false;
            puckHeight = 0f;
            // Launch impulse calculated so puck reaches target height
            puckVelocity = Mathf.Sqrt(2f * rules.puckGravity * Mathf.Max(0.1f, targetPuckHeight));

            State = GorillaSmashState.Impact;
            stateTimer = 0f;
            return true;
        }

        public void Tick(float deltaSeconds)
        {
            if (float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds) || deltaSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), "Delta seconds must be finite and positive.");

            if (cooldownTimer > 0)
                cooldownTimer = Mathf.Max(0, cooldownTimer - deltaSeconds);

            if (State == GorillaSmashState.Armed)
            {
                armingDwellTimer += deltaSeconds;
            }

            if (State == GorillaSmashState.Impact)
            {
                stateTimer += deltaSeconds;
                // Brief impact frame (mallet hits plate)
                if (stateTimer >= 0.05f)
                {
                    State = GorillaSmashState.Elevating;
                    stateTimer = 0f;
                }
            }
            else if (State == GorillaSmashState.Elevating)
            {
                // Ascend towards target height
                puckHeight += puckVelocity * deltaSeconds;
                puckVelocity -= rules.puckGravity * deltaSeconds;

                if (puckHeight >= targetPuckHeight || puckVelocity <= 0f)
                {
                    puckHeight = targetPuckHeight;
                    puckVelocity = 0f;

                    if (LatestAttempt != null && LatestAttempt.bellHit)
                    {
                        bellRungThisAttempt = true;
                    }

                    State = GorillaSmashState.AttemptFinished;
                    stateTimer = 0f;
                    OnAttemptSettled();
                }
            }
            else if (State == GorillaSmashState.AttemptFinished)
            {
                stateTimer += deltaSeconds;
            }
        }

        void OnAttemptSettled()
        {
            if (LatestAttempt == null) return;

            attempts.Add(LatestAttempt);
            if (LatestAttempt.score > BestScore)
            {
                BestScore = LatestAttempt.score;
                BestTier = LatestAttempt.tier;
            }

            cooldownTimer = rules.cooldownSeconds;

            if (attempts.Count >= rules.maxAttempts)
            {
                FinishTournament();
            }
        }

        public bool NextAttempt()
        {
            if (State != GorillaSmashState.AttemptFinished)
                return false;

            if (attempts.Count >= rules.maxAttempts)
            {
                FinishTournament();
                return false;
            }

            puckHeight = 0;
            puckVelocity = 0;
            targetPuckHeight = 0;
            bellRungThisAttempt = false;
            armingDwellTimer = 0;
            stateTimer = 0;
            State = GorillaSmashState.Ready;
            return true;
        }

        void FinishTournament()
        {
            State = GorillaSmashState.Completed;
            int total = 0;
            for (int i = 0; i < attempts.Count; i++)
                total += attempts[i].score;

            Result = new GorillaSmashResult(
                playerId: playerId,
                completed: true,
                totalScore: total,
                bestScore: BestScore,
                bestTier: BestTier,
                attempts: attempts.ToArray(),
                reason: "Completed all 3 attempts successfully."
            );
        }

        public static float CalculateTechnique(float power, float direction, float timing, float confidence, GorillaSmashRules rules)
        {
            return Mathf.Clamp01(
                power * rules.powerWeight +
                direction * rules.directionWeight +
                timing * rules.timingWeight +
                confidence * rules.confidenceWeight
            );
        }

        public static int CalculateScore(float technique)
        {
            return Mathf.Clamp((int)Math.Round(technique * 10000f), 0, 10000);
        }

        public static GorillaSmashTier GetTier(int score)
        {
            if (score < 2500) return GorillaSmashTier.MissWeak;
            if (score < 5000) return GorillaSmashTier.Good;
            if (score < 7500) return GorillaSmashTier.Great;
            if (score < 9500) return GorillaSmashTier.Perfect;
            return GorillaSmashTier.Legendary;
        }

        public static string GetTierDisplayName(GorillaSmashTier tier)
        {
            switch (tier)
            {
                case GorillaSmashTier.MissWeak: return "MISS / WEAK";
                case GorillaSmashTier.Good: return "GOOD";
                case GorillaSmashTier.Great: return "GREAT";
                case GorillaSmashTier.Perfect: return "PERFECT";
                case GorillaSmashTier.Legendary: return "LEGENDARY";
                default: return "UNKNOWN";
            }
        }
    }
}
