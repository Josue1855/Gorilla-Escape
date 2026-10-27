package com.gorillaescape.server.mobile;
import java.io.IOException;
import java.util.Set;
import tools.jackson.databind.JsonNode;
/** Bounded display context only. Unity retains gameplay/result authority. */
public final class GamePresentation {
    private GamePresentation(){}
    public static void validate(JsonNode n)throws IOException {
        if(n==null||!n.isObject()||(n.size()!=4&&n.size()!=5)||!n.path("version").isInt()||n.path("version").intValue()!=1
            ||!n.path("gameId").isTextual()||!Set.of("lobby","log-balance").contains(n.path("gameId").asString())
            ||!n.path("phase").isTextual()||!Set.of("Ready","Countdown","Crossing","Paused","Won","Fell").contains(n.path("phase").asString())
            ||!n.path("playerId").isInt()||n.path("playerId").intValue()<0||n.path("playerId").intValue()>4)
            throw new IOException("IPC_GAME_CONTEXT");
        if(n.size()==5) {
            var stats=n.path("stats");
            if(!"log-balance".equals(n.path("gameId").asString()) || !stats.isObject() || (stats.size()!=8&&stats.size()!=9)) throw new IOException("IPC_GAME_STATS");
            range(stats,"balance",-1,1);range(stats,"tendency",-1,1);range(stats,"progress",0,1);
            range(stats,"distance",0,10000);range(stats,"elapsed",0,3600);range(stats,"roll",-3600,3600);
            range(stats,"speed",0,100);range(stats,"wind",-100,100);
            if(stats.size()==9)range(stats,"danger",0,1);
        }
    }
    private static void range(JsonNode stats,String key,double min,double max)throws IOException {
        var v=stats.path(key);double value=v.asDouble(Double.NaN);
        if(!v.isNumber()||!Double.isFinite(value)||value<min||value>max)throw new IOException("IPC_GAME_STATS");
    }
}
