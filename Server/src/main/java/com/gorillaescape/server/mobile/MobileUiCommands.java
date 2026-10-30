package com.gorillaescape.server.mobile;

import com.gorillaescape.server.protocol.TechnicalSession;
import com.gorillaescape.server.protocol.GorillaProtocol;
import java.io.IOException;
import java.util.*;
import java.util.function.LongSupplier;
import tools.jackson.databind.JsonNode;

/** Navigation requests only. Unity rechecks scene, ownership and expiry before executing. */
public final class MobileUiCommands {
    public record Request(String requestId,String contextId,String action,String target) {}
    public record Command(String requestId,String contextId,String action,String target,String sessionId,int playerId,String deviceSessionId,long connectionEpoch,long receivedAt) {}
    private final LongSupplier clock;
    private final Deque<Command> pending=new ArrayDeque<>();
    private final Map<String,Long> seen=new LinkedHashMap<>();
    private long rateWindow=Long.MIN_VALUE/2;
    private int accepted;
    public MobileUiCommands(LongSupplier clock){this.clock=clock;}
    public synchronized Map<String,Object> offer(Request r,JsonNode context,TechnicalSession.Joined owner)throws IOException {
        long now=clock.getAsLong();seen.entrySet().removeIf(e->now-e.getValue()>30000);
        if(r==null||r.requestId()==null||!r.requestId().matches("[a-f0-9]{32}")||context==null||context.path("version").asInt()!=3
            ||!Objects.equals(r.contextId(),context.path("contextId").asString())||owner.playerId()!=1)throw new IOException("COMMAND_CONTEXT");
        if(seen.containsKey(r.requestId()))return Map.of("queued",true,"requestId",r.requestId());
        String target=r.target();boolean lobby=context.path("gameId").asString().equals("lobby");
        boolean valid=switch(r.action()==null?"":r.action()){
            case "select-game" -> lobby&&GamePresentation.GAMES.contains(target)&&!"lobby".equals(target);
            case "menu" -> "".equals(target);
            case "restart" -> !lobby&&"".equals(target);
            case "start" -> !lobby&&context.path("phase").asString().equals("Ready")&&"".equals(target);
            case "set-hand" -> (lobby||context.path("gameId").asString().equals("bowling"))&&Set.of("left","right").contains(target==null?"":target);
            default -> false;
        };
        if(now-rateWindow>=1000){rateWindow=now;accepted=0;}
        if(!valid||accepted>=4||pending.size()>=8)throw new IOException("COMMAND_REJECTED");
        if(seen.size()>=64)seen.remove(seen.keySet().iterator().next());
        seen.put(r.requestId(),now);accepted++;
        pending.addLast(new Command(r.requestId(),r.contextId(),r.action(),target,owner.sessionId(),owner.playerId(),owner.sessionDeviceId(),owner.epoch(),now));
        return Map.of("queued",true,"requestId",r.requestId());
    }
    public synchronized JsonNode drain(){
        long now=clock.getAsLong();
        while(!pending.isEmpty()) {var command=pending.removeFirst();if(now>=command.receivedAt()&&now-command.receivedAt()<=2500)return GorillaProtocol.JSON.valueToTree(command);}
        return null;
    }
}
