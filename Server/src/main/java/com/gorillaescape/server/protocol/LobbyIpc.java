package com.gorillaescape.server.protocol;

import tools.jackson.databind.JsonNode;
import java.util.Set;

/** Optional commands on the existing authenticated Unity IPC. Never routed from RTC/HTTP. */
public final class LobbyIpc {
    private LobbyIpc() {}
    public record Result(boolean accepted,String code) {}
    private static final Set<String> FIELDS=Set.of("kind","phase","sessionId","playerId","deviceSessionId","connectionEpoch","ready");
    public static Result apply(JsonNode n,TechnicalSession session){
        try {
            if(n==null||!n.isObject()||n.size()!=FIELDS.size())throw new IllegalArgumentException();
            for(String key:FIELDS)if(!n.has(key))throw new IllegalArgumentException();
            if(!n.get("kind").isTextual()||!n.get("phase").isTextual()||!n.get("sessionId").isTextual()
                ||!n.get("deviceSessionId").isTextual()||!n.get("playerId").isInt()
                ||!n.get("connectionEpoch").isIntegralNumber()||!n.get("connectionEpoch").canConvertToLong()||!n.get("ready").isBoolean())throw new IllegalArgumentException();
            String sid=n.get("sessionId").asString();
            switch(n.get("kind").asString()) {
                case "SET_PHASE" -> {
                    if(n.get("playerId").intValue()!=0||n.get("connectionEpoch").longValue()!=0||!n.get("deviceSessionId").asString().isEmpty()||n.get("ready").booleanValue())throw new IllegalArgumentException();
                    session.unityPhase(sid,TechnicalSession.LobbyPhase.valueOf(n.get("phase").asString()));
                }
                case "SET_PLAYER_READY" -> {
                    if(!n.get("phase").asString().isEmpty())throw new IllegalArgumentException();
                    session.unityPlayerReady(sid,n.get("playerId").intValue(),n.get("deviceSessionId").asString(),n.get("connectionEpoch").longValue(),n.get("ready").booleanValue());
                }
                default -> throw new IllegalArgumentException();
            }
            return new Result(true,"ACCEPTED");
        }catch(IllegalStateException e){return new Result(false,switch(e.getMessage()){
            case "LOBBY_READY_FROZEN","LOBBY_TRANSITION","LOBBY_PREREQUISITES","SESSION_CLOSED" -> e.getMessage();
            default -> "LOBBY_COMMAND_INVALID";
        });}catch(IllegalArgumentException e){return new Result(false,"LOBBY_COMMAND_INVALID");}
    }
}
