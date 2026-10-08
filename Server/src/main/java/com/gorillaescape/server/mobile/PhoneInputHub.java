package com.gorillaescape.server.mobile;

import com.gorillaescape.server.protocol.GorillaProtocol;
import tools.jackson.databind.JsonNode;
import tools.jackson.databind.node.*;
import java.util.*;

/** Four latest-only slots; no motion queue. Drained only by the existing IPC writer. */
public final class PhoneInputHub {
    private final Map<Integer,ObjectNode> pending=new LinkedHashMap<>();
    public long received,forwarded,overwritten,rejected;
    public synchronized void accept(ObjectNode n,long epoch,String state){
        int player=n.get("playerId").intValue();if(player<1||player>4){rejected++;return;}
        ObjectNode wire=GorillaProtocol.JSON.createObjectNode();
        for(String name:List.of("playerId","sequence","clientTimestamp","serverReceiveTimestamp","capabilities","quality"))wire.set(name,n.get(name));
        wire.put("messageType",n.get("messageType").asString());wire.put("deviceSessionId",n.get("deviceSessionId").asString());wire.put("sessionId",n.get("sessionId").asString());wire.put("connectionEpoch",epoch);wire.put("connectionState",state);
        JsonNode payload=n.get("payload");
        for(String name:List.of("acceleration","accelerationIncludingGravity","rotationRate","orientation","screenOrientation","touch")){
            JsonNode group=payload.get(name);ObjectNode value=GorillaProtocol.JSON.createObjectNode();
            value.put("availability",group==null?"unavailable":group.get("availability").asString());
            if(group!=null&&group.get("values").isObject())for(var entry:((ObjectNode)group.get("values")).properties()){
                String axis=entry.getKey();JsonNode v=entry.getValue();value.put("has"+Character.toUpperCase(axis.charAt(0))+axis.substring(1),!v.isNull());if(!v.isNull())value.set(axis,v);
            }
            wire.set(name,value);
        }
        if(pending.put(player,wire)!=null)overwritten++;received++;
    }
    public synchronized ArrayNode drain(){
        ArrayNode batch=GorillaProtocol.JSON.createArrayNode();int size=2;
        for(var it=pending.entrySet().iterator();it.hasNext();){var entry=it.next();int bytes=GorillaProtocol.JSON.writeValueAsBytes(entry.getValue()).length;
            if(size+bytes+1>3500)continue;batch.add(entry.getValue());size+=bytes+1;it.remove();forwarded++;}
        return batch;
    }
    public synchronized void reject(){rejected++;}
    public synchronized Map<String,Long> metrics(){return Map.of("received",received,"forwarded",forwarded,"overwritten",overwritten,"rejected",rejected,"pendingSlots",(long)pending.size());}
    public synchronized int pendingSlots(){return pending.size();}
    public synchronized void clear(){pending.clear();}
}
