using System;
using GorillaEscape.Minigames.GorillaSmash;
using NUnit.Framework;

public sealed class GorillaSmashTests
{
    static GorillaSmashRules TestRules() => new GorillaSmashRules
    {
        maxAttempts = 3,
        powerWeight = 0.45f,
        directionWeight = 0.25f,
        timingWeight = 0.20f,
        confidenceWeight = 0.10f,
        armingHoldSeconds = 0f, // Instant arming for unit tests
        cooldownSeconds = 0.5f,
        towerHeight = 10f,
        bellScoreThreshold = 9500f
    };

    [Test]
    public void ScoringFormulaMatchesSpecificationV4Section17()
    {
        var rules = TestRules();
        // Power: 1.0 (0.45), Direction: 0.8 (0.20), Timing: 0.5 (0.10), Conf: 1.0 (0.10)
        // Expected Technique = 0.45 + 0.20 + 0.10 + 0.10 = 0.85
        // Expected Score = 8500
        float technique = GorillaSmashModel.CalculateTechnique(1.0f, 0.8f, 0.5f, 1.0f, rules);
        int score = GorillaSmashModel.CalculateScore(technique);

        Assert.That(technique, Is.EqualTo(0.85f).Within(0.001f));
        Assert.That(score, Is.EqualTo(8500));
        Assert.That(GorillaSmashModel.GetTier(score), Is.EqualTo(GorillaSmashTier.Perfect));
    }

    [TestCase(0, GorillaSmashTier.MissWeak)]
    [TestCase(2499, GorillaSmashTier.MissWeak)]
    [TestCase(2500, GorillaSmashTier.Good)]
    [TestCase(4999, GorillaSmashTier.Good)]
    [TestCase(5000, GorillaSmashTier.Great)]
    [TestCase(7499, GorillaSmashTier.Great)]
    [TestCase(7500, GorillaSmashTier.Perfect)]
    [TestCase(9499, GorillaSmashTier.Perfect)]
    [TestCase(9500, GorillaSmashTier.Legendary)]
    [TestCase(10000, GorillaSmashTier.Legendary)]
    public void TierScoreBandsMapCorrectly(int score, GorillaSmashTier expectedTier)
    {
        Assert.That(GorillaSmashModel.GetTier(score), Is.EqualTo(expectedTier));
    }

    [Test]
    public void ExactlyThreeAttemptsAllowedPerPlayer()
    {
        var model = new GorillaSmashModel(TestRules());
        model.Begin(1);

        for (int i = 1; i <= 3; i++)
        {
            Assert.That(model.CurrentAttemptNumber, Is.EqualTo(i));
            Assert.That(model.State, Is.EqualTo(GorillaSmashState.Ready));

            model.SetArming(true);
            bool struck = model.TriggerStrike(0.8f, 0.9f, 0.7f, 1.0f);
            Assert.That(struck, Is.True);
            Assert.That(model.State, Is.EqualTo(GorillaSmashState.Impact));

            // Tick through impact and elevation until attempt settles
            for (int t = 0; t < 100 && model.State != GorillaSmashState.AttemptFinished && model.State != GorillaSmashState.Completed; t++)
            {
                model.Tick(0.05f);
            }

            if (i < 3)
            {
                Assert.That(model.State, Is.EqualTo(GorillaSmashState.AttemptFinished));
                Assert.That(model.Attempts.Count, Is.EqualTo(i));
                bool next = model.NextAttempt();
                Assert.That(next, Is.True);
            }
        }

        // After 3rd attempt, should be in Completed state
        Assert.That(model.State, Is.EqualTo(GorillaSmashState.Completed));
        Assert.That(model.Attempts.Count, Is.EqualTo(3));
        Assert.That(model.Result, Is.Not.Null);
        Assert.That(model.Result.Completed, Is.True);
        Assert.That(model.Result.PlayerId, Is.EqualTo(1));
        Assert.That(model.Result.Attempts.Length, Is.EqualTo(3));

        // Attempting to advance further should be safely rejected
        Assert.That(model.NextAttempt(), Is.False);
    }

    [Test]
    public void BestScoreAndTierAreTrackedAccurately()
    {
        var model = new GorillaSmashModel(TestRules());
        model.Begin(2);

        // Attempt 1: Weak strike (0.2 everywhere -> 2000 pts)
        model.SetArming(true);
        model.TriggerStrike(0.2f, 0.2f, 0.2f, 0.2f);
        RunUntilSettled(model);
        Assert.That(model.BestScore, Is.EqualTo(2000));
        Assert.That(model.BestTier, Is.EqualTo(GorillaSmashTier.MissWeak));

        model.NextAttempt();

        // Attempt 2: Legendary strike (1.0 everywhere -> 10000 pts)
        model.SetArming(true);
        model.TriggerStrike(1.0f, 1.0f, 1.0f, 1.0f);
        RunUntilSettled(model);
        Assert.That(model.BestScore, Is.EqualTo(10000));
        Assert.That(model.BestTier, Is.EqualTo(GorillaSmashTier.Legendary));
        Assert.That(model.BellRung, Is.True);

        model.NextAttempt();

        // Attempt 3: Good strike (0.4 everywhere -> 4000 pts; lower than best)
        model.SetArming(true);
        model.TriggerStrike(0.4f, 0.4f, 0.4f, 0.4f);
        RunUntilSettled(model);

        // Best score remains 10000
        Assert.That(model.BestScore, Is.EqualTo(10000));
        Assert.That(model.BestTier, Is.EqualTo(GorillaSmashTier.Legendary));
        Assert.That(model.Result.BestScore, Is.EqualTo(10000));
    }

    [Test]
    public void AntiCheatRequiresArmedDwellTime()
    {
        var rules = TestRules();
        rules.armingHoldSeconds = 0.35f; // Requires 0.35s arm dwell
        var model = new GorillaSmashModel(rules);
        model.Begin(1);

        model.SetArming(true);
        // Too fast: tried to strike immediately without arm dwell
        bool immediateStrike = model.TriggerStrike(1.0f, 1.0f, 1.0f, 1.0f);
        Assert.That(immediateStrike, Is.False, "Immediate strike without arming dwell must be rejected");

        // Dwell partially (0.15s < 0.35s)
        model.Tick(0.15f);
        bool prematureStrike = model.TriggerStrike(1.0f, 1.0f, 1.0f, 1.0f);
        Assert.That(prematureStrike, Is.False, "Premature strike before arming hold completed must be rejected");

        // Finish dwell (0.25s more -> 0.40s >= 0.35s)
        model.Tick(0.25f);
        bool validStrike = model.TriggerStrike(1.0f, 1.0f, 1.0f, 1.0f);
        Assert.That(validStrike, Is.True, "Valid strike after arming dwell must succeed");
    }

    [Test]
    public void CooldownPreventsStrikeSpam()
    {
        var rules = TestRules();
        rules.cooldownSeconds = 1.0f;
        var model = new GorillaSmashModel(rules);
        model.Begin(1);

        model.SetArming(true);
        model.TriggerStrike(0.8f, 0.8f, 0.8f, 1.0f);
        RunUntilSettled(model);

        model.NextAttempt();
        model.SetArming(true);

        // Cooldown timer is still active right after attempt settles
        Assert.That(model.CooldownTimer, Is.GreaterThan(0));
        bool spammedStrike = model.TriggerStrike(0.8f, 0.8f, 0.8f, 1.0f);
        Assert.That(spammedStrike, Is.False, "Strike during cooldown must be rejected");

        // Wait out cooldown
        model.Tick(1.1f);
        Assert.That(model.CooldownTimer, Is.Zero);
        bool legalStrike = model.TriggerStrike(0.8f, 0.8f, 0.8f, 1.0f);
        Assert.That(legalStrike, Is.True, "Strike after cooldown must succeed");
    }

    [Test]
    public void RulesValidationEnforcesWeightSumAndBounds()
    {
        var badRules = new GorillaSmashRules
        {
            powerWeight = 0.50f,
            directionWeight = 0.50f,
            timingWeight = 0.50f // Sum = 1.6 > 1.0
        };
        Assert.Throws<ArgumentException>(() => badRules.Validate());

        var negativeAttempts = new GorillaSmashRules { maxAttempts = 0 };
        Assert.Throws<ArgumentException>(() => negativeAttempts.Validate());
    }

    static void RunUntilSettled(GorillaSmashModel m)
    {
        for (int i = 0; i < 200 && m.State != GorillaSmashState.AttemptFinished && m.State != GorillaSmashState.Completed; i++)
        {
            m.Tick(0.02f);
        }
    }
}
