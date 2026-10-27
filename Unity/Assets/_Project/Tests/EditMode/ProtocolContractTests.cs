using System.IO;
using GorillaEscape.Contracts;
using NUnit.Framework;
using UnityEngine;

public class ProtocolContractTests
{
    [Test]
    public void UnityReadsTheSameWireFixtureAsJavaAndReact()
    {
        string path = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Shared/Protocol/fixtures/sensor.json"));
        var envelope = JsonUtility.FromJson<SensorEnvelope>(File.ReadAllText(path));
        Assert.That(envelope.version, Is.EqualTo(ProtocolVersion.Current));
        Assert.That(envelope.playerId, Is.EqualTo(2));
        Assert.That(envelope.sequence, Is.EqualTo(12834));
        Assert.That(envelope.payload.orientation.w, Is.EqualTo(1f));
    }
}
