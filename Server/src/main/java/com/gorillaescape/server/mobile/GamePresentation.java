package com.gorillaescape.server.mobile;
import java.io.IOException;
import java.util.Set;
import tools.jackson.databind.JsonNode;
/** Bounded display context only. Unity retains gameplay/result authority. */
public final class GamePresentation {
    private GamePresentation(){}
    public static void validate(JsonNode n)throws IOException {
        if(n==null||!n.isObject()||n.size()!=4||!n.path("version").isInt()||n.path("version").intValue()!=1
            ||!n.path("gameId").isTextual()||!Set.of("lobby","log-balance").contains(n.path("gameId").asString())
            ||!n.path("phase").isTextual()||!Set.of("Ready","Countdown","Crossing","Paused","Won","Fell").contains(n.path("phase").asString())
            ||!n.path("playerId").isInt()||n.path("playerId").intValue()<0||n.path("playerId").intValue()>4)
            throw new IOException("IPC_GAME_CONTEXT");
    }
}
