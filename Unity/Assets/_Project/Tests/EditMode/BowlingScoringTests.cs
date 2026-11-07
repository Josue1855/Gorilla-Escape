using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// These tests describe the current ten-frame implementation, not a product decision.
public sealed class BowlingScoringTests
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private GameObject host;
    private Component controller;
    private Type controllerType;

    [SetUp]
    public void SetUp()
    {
        controllerType = Type.GetType("BowlingController, Assembly-CSharp", true);
        host = new GameObject("Bowling scoring test");
        host.SetActive(false);
        controller = host.AddComponent(controllerType);
        var frames = (List<int>[])controllerType.GetField("bowlingFrames", Flags).GetValue(controller);
        for (int i = 0; i < frames.Length; i++) frames[i] = new List<int>();
    }

    [TearDown]
    public void TearDown() => UnityEngine.Object.DestroyImmediate(host);

    private void Roll(params int[] values)
    {
        var register = controllerType.GetMethod("RegisterBowlingRoll", Flags);
        foreach (int value in values) register.Invoke(controller, new object[] { value });
    }

    private int[] Scores() => (int[])controllerType.GetMethod("CalculateBowlingScores", Flags).Invoke(controller, null);

    [Test]
    public void PerfectGameScoresThreeHundred()
    {
        Roll(10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10);
        Assert.That(Scores()[9], Is.EqualTo(300));
    }

    [Test]
    public void AllZeroGameScoresZero()
    {
        Roll(new int[20]);
        Assert.That(Scores()[9], Is.Zero);
    }

    [Test]
    public void SpareAndStrikeWaitForTheirBonusRolls()
    {
        Roll(10, 7);
        Assert.That(Scores()[0], Is.EqualTo(-1));
        Roll(3);
        Assert.That(Scores()[0], Is.EqualTo(20));
        Assert.That(Scores()[1], Is.EqualTo(-1));
        Roll(4);
        Assert.That(Scores()[1], Is.EqualTo(34));
    }

    [Test]
    public void TenthFrameSpareUsesOneExtraRoll()
    {
        Roll(new int[18]);
        Roll(7, 3);
        Assert.That(Scores()[9], Is.EqualTo(-1));
        Roll(6);
        Assert.That(Scores()[9], Is.EqualTo(16));
    }

    [Test]
    public void TenthFrameStrikeUsesTwoExtraRolls()
    {
        Roll(new int[18]);
        Roll(10, 4);
        Assert.That(Scores()[9], Is.EqualTo(-1));
        Roll(5);
        Assert.That(Scores()[9], Is.EqualTo(19));
    }
}
