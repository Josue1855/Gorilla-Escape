using System.Collections;
using GorillaEscape.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class BootstrapTests
{
    [UnityTest]
    public IEnumerator BootstrapAppliesAndRestoresConfiguredFrameRate()
    {
        int before = Application.targetFrameRate;
        var settings = ScriptableObject.CreateInstance<ApplicationSettings>();
        settings.TargetFrameRate = 45;
        var go = new GameObject("BootstrapTest");
        go.SetActive(false);
        var bootstrap = go.AddComponent<Bootstrap>();
        typeof(Bootstrap).GetField("settings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(bootstrap, settings);
        go.SetActive(true);
        Assert.That(Application.targetFrameRate, Is.EqualTo(45));
        Object.Destroy(go);
        yield return null;
        Assert.That(Application.targetFrameRate, Is.EqualTo(before));
        Object.Destroy(settings);
    }
}
