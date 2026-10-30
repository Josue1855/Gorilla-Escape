using System;
using System.Collections.Generic;
using GorillaEscape.Contracts;
using GorillaEscape.Core;
static class MobileChecks
{
    static void Require(bool value){if(!value)throw new Exception("assertion failed");}
    public static IEnumerable<(string,Action)> All()
    {
        yield return ("mobile presentation validates all eight real games",()=>{foreach(var id in new[]{"bowling","boxing","baseball","drums","coconut-slice","jungle-keeper","whack","log-balance"})Require(new GamePresentationWire{gameId=id,screen="game",playerId=1,phase="Playing"}.Valid());});
        yield return ("display statistics reject nonfinite and out of bounds data",()=>{var c=new GamePresentationWire();c.stats.score=double.NaN;Require(!c.Valid());c.stats.score=0;c.stats.progress=2;Require(!c.Valid());});
        yield return ("phone navigation rejects stale scenes, spectators and replaced connections",()=>{
            var context=new GamePresentationWire();var lobby=new LobbySnapshotWire{sessionId="session",players=new[]{new LobbyPlayerWire{playerId=1,deviceSessionId="device",connectionEpoch=2,connectionState="CONNECTED",networkReady=true}}};
            var command=new MobileUiCommandWire{action="select-game",target="bowling",contextId=context.contextId,playerId=1,sessionId="session",deviceSessionId="device",connectionEpoch=2,receivedAt=1000};
            Require(command.Valid(context,lobby,1001));Require(!command.Valid(context,lobby,3501));
            command.connectionEpoch=1;Require(!command.Valid(context,lobby,1001));command.connectionEpoch=2;
            command.playerId=2;Require(!command.Valid(context,lobby,1001));command.playerId=1;
            context.contextId=Guid.NewGuid().ToString("N");Require(!command.Valid(context,lobby,1001));
        });
        yield return ("display copies cannot mutate the original authoritative snapshot",()=>{var c=new GamePresentationWire();var copy=c.Copy();copy.stats.balance=1;Require(c.stats.balance==0);});
        yield return ("packaged runtime preserves explicit settings and creates one operator secret",()=>{var env=new Dictionary<string,string>{{"GORILLA_CONNECTION_MODE","CABIN"}};Func<string,string> read=k=>env.TryGetValue(k,out var v)?v:null;PackagedRuntimeEnvironment.Configure("/package",false,read,(k,v)=>env[k]=v,"/home/test");var secret=env["GORILLA_MOBILE_OPERATOR"];Require(secret.Length==43&&env["GORILLA_CONNECTION_MODE"]=="CABIN"&&env["GORILLA_IPC_JAR"]=="/package/Java/server.jar");PackagedRuntimeEnvironment.Configure("/package",false,read,(k,v)=>env[k]=v,"/home/test");Require(secret==env["GORILLA_MOBILE_OPERATOR"]);});
        yield return ("explicit emulator lab never silently enables LAN certificate setup",()=>{var env=new Dictionary<string,string>{{"GORILLA_MOBILE_LAB","1"}};PackagedRuntimeEnvironment.Configure("/package",true,k=>env.TryGetValue(k,out var v)?v:null,(k,v)=>env[k]=v,"/home/test");Require(!env.ContainsKey("GORILLA_LOCAL_TRUST_DIR")&&env["GORILLA_IPC_JAVA"].EndsWith("java.exe"));});
    }
}
