using System.IO;
using GorillaEscape.Contracts;
using GorillaEscape.QA;
using NUnit.Framework;
using UnityEngine;
public sealed class QaHostingContractTests {
    [Test] public void JavaIpcFixtureUsesTheExistingNestedPrepareContract(){
        var root=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Shared/Protocol/fixtures/qa-hosting.json"));
        string json=File.ReadAllText(root);var pong=JsonUtility.FromJson<PhonePong>(json);
        Assert.AreEqual(1,pong.ipcVersion);Assert.AreEqual("PONG",pong.type);
        var prepare=pong.payload.qaHosting.prepare;Assert.AreEqual("PREPARE",prepare.state);Assert.AreEqual(0,prepare.ttlMs);
        var texture=QaRuntimeService.DecodeQr(prepare);try{Assert.AreEqual(prepare.size,texture.width);Assert.AreEqual(prepare.size,texture.height);}finally{Object.DestroyImmediate(texture);}
        Assert.False(json.Contains("prepareModules"));Assert.False(json.Contains("prepareSize"));
    }
}
