using System;
using System.Collections.Generic;
using GorillaEscape.Contracts;
using GorillaEscape.Core;
static class MobileChecks
{
    static void Require(bool value){if(!value)throw new Exception("assertion failed");}
    public static IEnumerable<(string,Action)> All()
    {
        yield return ("prepared devices survive mode changes and transient loss but never a different room or phone",()=>{
            var prepared=new GorillaEscape.Gameplay.Input.SessionPreparation();
            var player=new LobbyPlayerWire{playerId=1,deviceSessionId="phone-one",displayName="Josué",connectionState="ACTIVE",networkReady=true,inputReady=true};
            var lobby=new LobbySnapshotWire{sessionId="room-one",players=new[]{player}};
            Require(!prepared.CanReuse(lobby,true,true,1));prepared.Record(true,lobby);
            Require(prepared.CanReuse(lobby,true,true,1));
            player.connectionState="BACKGROUND";player.inputReady=false;Require(prepared.CanReuse(lobby,true,true,1));
            player.connectionState="DISCONNECTED";Require(prepared.CanReuse(lobby,true,true,1));
            Require(!prepared.CanReuse(lobby,false,true,1));Require(!prepared.CanReuse(lobby,true,true,2));
            player.deviceSessionId="new-phone";Require(!prepared.CanReuse(lobby,true,true,1));
            player.deviceSessionId="phone-one";lobby.sessionId="different-room";Require(!prepared.CanReuse(lobby,true,true,1));
            lobby.sessionId="room-one";player.connectionState="FORFEITED";Require(!prepared.CanReuse(lobby,true,true,1));
        });
        yield return ("physical camera contract accepts hands and hips while rejecting unknown or duplicate points",()=>{
            var frame=Checks.Camera();var subject=frame.subjects[0];
            var points=new List<CameraLandmark>(subject.landmarks);
            foreach(var name in new[]{"left-hand","right-hand","left-hip","right-hip"})
                points.Add(new CameraLandmark{name=name,x=.5f,y=.7f,visibility=1,presence=1,visible=true});
            subject.landmarks=points.ToArray();
            Require(GorillaEscape.CameraTracking.CameraInputStore.Valid(frame));
            points[9].name="unknown";Require(!GorillaEscape.CameraTracking.CameraInputStore.Valid(frame));
            points[9].name="left-hip";Require(!GorillaEscape.CameraTracking.CameraInputStore.Valid(frame));
            points[9].name="right-hip";points.Add(new CameraLandmark{name="extra"});subject.landmarks=points.ToArray();
            Require(!GorillaEscape.CameraTracking.CameraInputStore.Valid(frame));
        });
        yield return ("lobby lifecycle transitions preserve IPC and distinguish network from sensors",()=>{
            var bridge=new GorillaEscape.Network.LobbyAuthorityBridge();
            var player=new LobbyPlayerWire{playerId=1,deviceSessionId=Guid.NewGuid().ToString(),connectionEpoch=1,networkReady=true};
            var lobby=new LobbySnapshotWire{lobbyVersion=1,sessionId=Guid.NewGuid().ToString(),phase="OPEN",players=new[]{player}};
            foreach(var state in new[]{"CONNECTED","ACTIVE","BACKGROUND","ACTIVE","AWAY","FORFEITED","DISCONNECTED"}){
                player.connectionState=state;player.networkReady=state!="DISCONNECTED";
                bridge.Accept(lobby,null);
                Require(bridge.IsPhoneConnected(1)==(state=="CONNECTED"||state=="ACTIVE"));
                Require(!bridge.Snapshot.players[0].inputReady&&!bridge.Snapshot.players[0].playerReady);
            }
            bridge.Reset();Require(!bridge.IsPhoneConnected(1));
        });
        yield return ("mobile presentation validates all eight real games",()=>{foreach(var id in new[]{"bowling","boxing","baseball","drums","coconut-slice","jungle-keeper","whack","log-balance"})Require(new GamePresentationWire{gameId=id,screen="game",playerId=1,phase="Playing"}.Valid());});
        yield return ("display statistics reject nonfinite and out of bounds data",()=>{var c=new GamePresentationWire();c.stats.score=double.NaN;Require(!c.Valid());c.stats.score=0;c.stats.progress=2;Require(!c.Valid());});
        yield return ("phone navigation rejects stale scenes, spectators and replaced connections",()=>{
            var context=new GamePresentationWire();var lobby=new LobbySnapshotWire{sessionId="session",players=new[]{new LobbyPlayerWire{playerId=1,deviceSessionId="device",connectionEpoch=2,connectionState="CONNECTED",networkReady=true}}};
            var command=new MobileUiCommandWire{action="select-game",target="bowling",contextId=context.contextId,playerId=1,sessionId="session",deviceSessionId="device",connectionEpoch=2,receivedAt=1000};
            Require(command.Valid(context,lobby,1001));Require(!command.Valid(context,lobby,3501));
            context.screen="connection";Require(!command.Valid(context,lobby,1001));context.screen="menu";
            command.connectionEpoch=1;Require(!command.Valid(context,lobby,1001));command.connectionEpoch=2;
            command.playerId=2;Require(!command.Valid(context,lobby,1001));command.playerId=1;
            context.contextId=Guid.NewGuid().ToString("N");Require(!command.Valid(context,lobby,1001));
        });
        yield return ("display copies cannot mutate the original authoritative snapshot",()=>{var c=new GamePresentationWire();var copy=c.Copy();copy.stats.balance=1;Require(c.stats.balance==0);});
        yield return ("packaged runtime preserves explicit settings and creates one operator secret",()=>{var env=new Dictionary<string,string>{{"GORILLA_CONNECTION_MODE","CABIN"}};Func<string,string> read=k=>env.TryGetValue(k,out var v)?v:null;PackagedRuntimeEnvironment.Configure("/package",false,read,(k,v)=>env[k]=v,"/home/test");var secret=env["GORILLA_MOBILE_OPERATOR"];Require(secret.Length==43&&env["GORILLA_CONNECTION_MODE"]=="CABIN"&&env["GORILLA_IPC_JAR"]=="/package/Java/server.jar");PackagedRuntimeEnvironment.Configure("/package",false,read,(k,v)=>env[k]=v,"/home/test");Require(secret==env["GORILLA_MOBILE_OPERATOR"]);});
        yield return ("explicit emulator lab never silently enables LAN certificate setup",()=>{var env=new Dictionary<string,string>{{"GORILLA_MOBILE_LAB","1"}};PackagedRuntimeEnvironment.Configure("/package",true,k=>env.TryGetValue(k,out var v)?v:null,(k,v)=>env[k]=v,"/home/test");Require(!env.ContainsKey("GORILLA_LOCAL_TRUST_DIR")&&env["GORILLA_IPC_JAVA"].EndsWith("java.exe"));});
    }
}
