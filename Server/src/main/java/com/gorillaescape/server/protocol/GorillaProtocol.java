package com.gorillaescape.server.protocol;

import java.io.IOException;
import java.util.*;
import java.util.function.LongSupplier;
import tools.jackson.core.StreamReadConstraints;
import tools.jackson.core.StreamReadFeature;
import tools.jackson.core.json.JsonFactory;
import tools.jackson.databind.DeserializationFeature;
import tools.jackson.databind.JsonNode;
import tools.jackson.databind.json.JsonMapper;
import tools.jackson.databind.node.ObjectNode;

/** Mobile v1 validation. No gameplay interpretation; clock and budgets are injectable. */
public final class GorillaProtocol {
    public static final int MAX_BYTES=2048;
    public static final Set<String> TYPES=Set.of("HELLO","JOIN","JOIN_ACK","MOTION_SAMPLE","TOUCH","HEARTBEAT","CLIENT_STATE","DISCONNECT","ERROR");
    private static final Set<String> CLIENT_TYPES=Set.of("HELLO","MOTION_SAMPLE","TOUCH","HEARTBEAT","CLIENT_STATE","DISCONNECT");
    private static final Set<String> ROOT=Set.of("protocolVersion","messageType","sessionId","playerId","deviceSessionId","sequence","clientTimestamp","serverReceiveTimestamp","capabilities","quality","payload");
    public static final JsonMapper JSON=JsonMapper.builder(JsonFactory.builder()
        .streamReadConstraints(StreamReadConstraints.builder().maxNestingDepth(8).maxStringLength(512).maxNumberLength(32).build()).build())
        .enable(StreamReadFeature.STRICT_DUPLICATE_DETECTION).enable(DeserializationFeature.FAIL_ON_TRAILING_TOKENS).build();
    public record Identity(String sessionId,int playerId,String deviceSessionId) {}
    public static final class Gate {
        private final Identity identity;
        private final LongSupplier milliseconds;
        private final long maximumAge,futureTolerance;
        private final int rate;
        private long motionSequence=-1,controlSequence=-1,window=-1;
        private int count;
        public Gate(Identity identity,LongSupplier milliseconds,long maximumAge,long futureTolerance,int rate){
            if(maximumAge<1||futureTolerance<0||rate<1||rate>1000)throw new IllegalArgumentException("PROTOCOL_CONFIG");
            this.identity=identity;this.milliseconds=milliseconds;this.maximumAge=maximumAge;this.futureTolerance=futureTolerance;this.rate=rate;
        }
        public synchronized ObjectNode accept(byte[] bytes) throws IOException {return accept(bytes,null);}
        public synchronized ObjectNode accept(byte[] bytes,String channel) throws IOException {
            if(bytes.length<1||bytes.length>MAX_BYTES)throw bad("SIZE");
            long now=milliseconds.getAsLong();
            long tick=Math.floorDiv(now,1000);
            if(window!=tick){window=tick;count=0;} if(++count>rate)throw bad("RATE");
            final JsonNode node;
            try {node=JSON.readTree(bytes);}catch(RuntimeException e){throw bad("JSON");}
            exact(node,ROOT);
            if(!node.get("protocolVersion").isInt()||node.get("protocolVersion").intValue()!=1)throw bad("VERSION");
            String type=text(node,"messageType");if(!CLIENT_TYPES.contains(type))throw bad("TYPE");
            if(!identity.sessionId.equals(text(node,"sessionId"))||!identity.deviceSessionId.equals(text(node,"deviceSessionId"))
                ||!node.get("playerId").isInt()||node.get("playerId").intValue()!=identity.playerId)throw bad("IDENTITY");
            long sequence=integer(node,"sequence"),timestamp=integer(node,"clientTimestamp");
            if(sequence<0||sequence>9007199254740991L||timestamp<0||timestamp>9007199254740991L)throw bad("NUMBER");
            if(timestamp<now-maximumAge)throw bad("STALE");if(timestamp>now+futureTolerance)throw bad("FUTURE");
            if(!node.get("serverReceiveTimestamp").isNull())throw bad("SERVER_TIMESTAMP");
            boolean motion=type.equals("MOTION_SAMPLE");if(channel!=null&&motion!=channel.equals("motion"))throw bad("CHANNEL");
            if(sequence<=(motion?motionSequence:controlSequence))throw bad("SEQUENCE");
            JsonNode capabilities=node.get("capabilities");
            if(!capabilities.isArray()||capabilities.size()>8)throw bad("CAPABILITIES");
            Set<String> seen=new HashSet<>();
            for(JsonNode c:capabilities)if(!c.isTextual()||!Set.of("acceleration","accelerationIncludingGravity","rotationRate","orientation","screenOrientation","touch").contains(c.asString())||!seen.add(c.asString()))throw bad("CAPABILITIES");
            JsonNode quality=node.get("quality");exact(quality,Set.of("source","status"));
            if(!Set.of("synthetic","replay","emulator","physical").contains(text(quality,"source"))||!Set.of("available","degraded","unavailable").contains(text(quality,"status")))throw bad("QUALITY");
            JsonNode payload=node.get("payload");
            if(motion){
                exact(payload,Set.of("acceleration","accelerationIncludingGravity","rotationRate","orientation","screenOrientation","touch"));
                for(String group:List.of("acceleration","accelerationIncludingGravity"))group(payload.get(group),Set.of("x","y","z"));
                for(String group:List.of("rotationRate","orientation"))group(payload.get(group),Set.of("alpha","beta","gamma"));
                group(payload.get("screenOrientation"),Set.of("angle"));group(payload.get("touch"),Set.of("x","y","pressed"));
            }else if(type.equals("TOUCH")){exact(payload,Set.of("touch"));group(payload.get("touch"),Set.of("x","y","pressed"));}
            else if(type.equals("CLIENT_STATE")){exact(payload,Set.of("state"));if(!Set.of("active","suspended").contains(text(payload,"state")))throw bad("STATE");}
            else exact(payload,Set.of());
            if(motion)motionSequence=sequence;else controlSequence=sequence;
            ObjectNode accepted=((ObjectNode)node).deepCopy();accepted.put("serverReceiveTimestamp",now);return accepted;
        }
    }
    private static void group(JsonNode group,Set<String> axes) throws IOException {
        exact(group,Set.of("availability","values"));String availability=text(group,"availability");
        JsonNode values=group.get("values");
        if(availability.equals("unavailable")){if(!values.isNull())throw bad("UNAVAILABLE");return;}
        if(!Set.of("present","partial").contains(availability))throw bad("AVAILABILITY");exact(values,axes);
        int present=0;
        for(String axis:axes){JsonNode value=values.get(axis);if(value.isNull())continue;
            if(axis.equals("pressed")){if(!value.isBoolean())throw bad("TOUCH");}
            else if(!value.isNumber()||!Double.isFinite(value.doubleValue())||Math.abs(value.doubleValue())>1000000)throw bad("AXIS");present++;}
        if(availability.equals("present")&&present!=axes.size()||availability.equals("partial")&&(present==0||present==axes.size()))throw bad("AVAILABILITY");
    }
    private static void exact(JsonNode n,Set<String> fields)throws IOException{if(n==null||!n.isObject()||n.size()!=fields.size())throw bad("FIELDS");for(String f:fields)if(!n.has(f))throw bad("FIELDS");}
    private static String text(JsonNode n,String key)throws IOException{if(n.get(key)==null||!n.get(key).isTextual())throw bad("TEXT");return n.get(key).asString();}
    private static long integer(JsonNode n,String key)throws IOException{if(n.get(key)==null||!n.get(key).isIntegralNumber()||!n.get(key).canConvertToLong())throw bad("NUMBER");return n.get(key).longValue();}
    private static IOException bad(String code){return new IOException("MOBILE_"+code);}
}
