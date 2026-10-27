using System;
using System.IO;
using GorillaEscape.Contracts;
using GorillaEscape.Network;
using NUnit.Framework;
public class LobbyAuthorityBridgeTests {
    private LobbySnapshotWire Snapshot()=>new LobbySnapshotWire{lobbyVersion=1,sessionId="11111111-1111-4111-8111-111111111111",phase="OPEN",players=new[]{new LobbyPlayerWire{playerId=1,deviceSessionId="22222222-2222-4222-8222-222222222222",connectionEpoch=1,connectionState="CONNECTED",networkReady=true}}};
    [Test] public void ActualFirstPongJsonDeserializesWithoutCommandReceipt(){
        string wire="{\"ipcVersion\":1,\"type\":\"PONG\",\"instanceId\":\"11111111-1111-4111-8111-111111111111\",\"connectionId\":\"22222222-2222-4222-8222-222222222222\",\"sequence\":1,\"payload\":{\"phoneInputs\":[],\"lobby\":{\"lobbyVersion\":1,\"sessionId\":\"33333333-3333-4333-8333-333333333333\",\"phase\":\"OPEN\",\"players\":[]}}}";
        var pong=UnityEngine.JsonUtility.FromJson<PhonePong>(wire);
        Assert.IsFalse(pong.payload.hasLobbyResult);
        new LobbyAuthorityBridge().Accept(pong.payload.lobby,pong.payload.hasLobbyResult?pong.payload.lobbyResult:null);
    }
    [Test] public void NetworkReadyNeverBecomesPlayerReady(){var b=new LobbyAuthorityBridge();b.Accept(Snapshot(),null);Assert.IsFalse(b.Snapshot.players[0].playerReady);Assert.IsFalse(b.Snapshot.players[0].inputReady);}
    [Test] public void CommandCarriesOwnedIdentityAndWaitsForReceipt(){var b=new LobbyAuthorityBridge();b.Accept(Snapshot(),null);var t=b.SetPlayerReadyAsync(1,true);var c=b.TakeCommand();Assert.AreEqual(1,c.playerId);Assert.AreEqual(b.Snapshot.sessionId,c.sessionId);Assert.AreEqual(b.Snapshot.players[0].deviceSessionId,c.deviceSessionId);Assert.AreEqual(1,c.connectionEpoch);Assert.IsFalse(t.IsCompleted);Assert.IsNull(b.TakeCommand());b.Accept(Snapshot(),new LobbyCommandResult{accepted=false,code="LOBBY_PREREQUISITES"});Assert.IsTrue(t.IsCompleted);Assert.IsFalse(t.Result.accepted);}
    [Test] public void OnePendingCommandBoundAndCleanupCancels(){var b=new LobbyAuthorityBridge();b.Accept(Snapshot(),null);var t=b.SetPhaseAsync("STARTING");Assert.Throws<InvalidOperationException>(()=>b.SetPhaseAsync("STARTED"));b.Reset();Assert.IsTrue(t.IsCanceled);Assert.IsNull(b.Snapshot);Assert.Throws<InvalidOperationException>(()=>b.SetPhaseAsync("STARTING"));}
    [Test] public void SnapshotsAreCopiedAndIdentityMustBeUnique(){var b=new LobbyAuthorityBridge();var s=Snapshot();b.Accept(s,null);s.players[0].playerId=4;b.Snapshot.players[0].playerId=3;Assert.AreEqual(1,b.Snapshot.players[0].playerId);s=Snapshot();s.players=new[]{s.players[0],s.players[0]};Assert.Throws<InvalidDataException>(()=>b.Accept(s,null));}
    [Test] public void ImpossibleReadyAndUnsolicitedReceiptRejected(){var b=new LobbyAuthorityBridge();var s=Snapshot();s.players[0].playerReady=true;Assert.Throws<InvalidDataException>(()=>b.Accept(s,null));Assert.Throws<InvalidDataException>(()=>b.Accept(Snapshot(),new LobbyCommandResult{accepted=true,code="ACCEPTED"}));}
    [Test] public void ReceiptCannotInventSuccess(){var b=new LobbyAuthorityBridge();b.Accept(Snapshot(),null);b.SetPhaseAsync("STARTING");b.TakeCommand();Assert.Throws<InvalidDataException>(()=>b.Accept(Snapshot(),new LobbyCommandResult{accepted=true,code="LOBBY_PREREQUISITES"}));b.Reset();}
}
