using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class CoconutSliceGeometryTests
{
    private static float Distance(Vector2 point, Vector2 start, Vector2 end)
    {
        var controller = Type.GetType("CoconutSliceController, Assembly-CSharp", true);
        var method = controller.GetMethod("DistanceToSegment", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(method, Is.Not.Null);
        return (float)method.Invoke(null, new object[] { point, start, end });
    }

    [Test]
    public void CrossingSegmentHitsAndDistantObjectDoesNot()
    {
        Assert.That(Distance(new Vector2(.5f, .5f), new Vector2(.2f, .5f), new Vector2(.8f, .5f)), Is.LessThan(.075f));
        Assert.That(Distance(new Vector2(.5f, .7f), new Vector2(.2f, .5f), new Vector2(.8f, .5f)), Is.GreaterThan(.075f));
    }

    [Test]
    public void DistanceIsClampedToSegmentEndpoints()
    {
        Assert.That(Distance(new Vector2(.9f, .5f), new Vector2(.2f, .5f), new Vector2(.8f, .5f)), Is.EqualTo(.1f).Within(.0001f));
    }

    [Test]
    public void StationaryHandUsesDistanceToPoint()
    {
        Assert.That(Distance(new Vector2(.5f, .6f), new Vector2(.5f, .5f), new Vector2(.5f, .5f)), Is.EqualTo(.1f).Within(.0001f));
    }

    [TestCase(1, 115)]
    [TestCase(20, 400)]
    [TestCase(21, 400)]
    [TestCase(-1, 100)]
    public void FruitScoreHasBoundedComboBonus(int combo, int expected)
    {
        var controller = Type.GetType("CoconutSliceController, Assembly-CSharp", true);
        var method = controller.GetMethod("FruitPointsForCombo", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(method, Is.Not.Null);
        Assert.That((int)method.Invoke(null, new object[] { combo }), Is.EqualTo(expected));
    }

    [Test]
    public void ExpiredRoundTransitionsToFinaleBeforeFurtherPlay()
    {
        var controller = Type.GetType("CoconutSliceController, Assembly-CSharp", true);
        var host = new GameObject("Slice round test");
        host.SetActive(false);
        try
        {
            var instance = host.AddComponent(controller);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var state = controller.GetField("state", flags);
            state.SetValue(instance, Enum.Parse(state.FieldType, "Playing"));
            controller.GetField("roundEndsAt", flags).SetValue(instance, -1f);
            controller.GetMethod("EndRoundIfExpired", flags).Invoke(instance, null);
            Assert.That(state.GetValue(instance).ToString(), Is.EqualTo("Finale"));
        }
        finally { UnityEngine.Object.DestroyImmediate(host); }
    }
}
