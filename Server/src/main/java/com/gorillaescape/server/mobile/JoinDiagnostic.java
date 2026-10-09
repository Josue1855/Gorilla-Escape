package com.gorillaescape.server.mobile;

import java.util.*;

/** Bounded operator-only metadata. No SDP, addresses, identities or exception messages. */
final class JoinDiagnostic {
    enum Stage { VALIDATION, ADMISSION, PEER_CREATE, REMOTE_SDP, ANSWER_CREATE, LOCAL_SDP, SERVER_ICE, ANSWER_RETURNED }
    enum Code { LOBBY_ADMISSION_CLOSED, SESSION_FULL, SIGNAL_INVALID, ADMISSION_INVALID, ADMISSION_EXPIRED_OR_USED, REMOTE_SDP_FAILED, ANSWER_CREATE_FAILED, LOCAL_SDP_FAILED, SERVER_ICE_TIMEOUT, SIGNAL_FAILED_OTHER }
    private final long started=System.nanoTime();
    private final List<Map<String,Object>> events=new ArrayList<>();
    private final List<Map<String,Object>> candidates=new ArrayList<>();
    private Stage stage=Stage.VALIDATION;
    private Code code;
    synchronized void stage(Stage value){stage=value;event("stage",value.name());}
    synchronized void event(String name,String value){if(events.size()<64)events.add(Map.of("ms",(System.nanoTime()-started)/1_000_000.0,"event",name,"value",value));}
    synchronized void fail(Code value){code=value;}
    synchronized void candidate(String text){var safe=sanitizeCandidate(text);if(safe!=null&&candidates.size()<32)candidates.add(safe);}
    synchronized Map<String,Object> snapshot(){var result=new LinkedHashMap<String,Object>();result.put("stage",stage.name());result.put("code",code==null?"NONE":code.name());result.put("events",List.copyOf(events));result.put("candidates",List.copyOf(candidates));return result;}
    synchronized Code stageFailure(){return switch(stage){case REMOTE_SDP->Code.REMOTE_SDP_FAILED;case ANSWER_CREATE->Code.ANSWER_CREATE_FAILED;case LOCAL_SDP->Code.LOCAL_SDP_FAILED;case SERVER_ICE->Code.SERVER_ICE_TIMEOUT;default->Code.SIGNAL_FAILED_OTHER;};}
    static Map<String,Object> sanitizeCandidate(String text){
        if(text==null||text.length()>2048)return null;
        String[] f=text.replaceFirst("^a=","").trim().split("\\s+");
        if(f.length<8||!f[0].startsWith("candidate:")||!f[6].equals("typ")||!Set.of("host","srflx","prflx","relay").contains(f[7])||!Set.of("udp","tcp").contains(f[2].toLowerCase(Locale.ROOT)))return null;
        int port;try{port=Integer.parseInt(f[5]);}catch(NumberFormatException e){return null;}if(port<1||port>65535)return null;
        String kind=f[4].matches("[A-Za-z0-9-]{1,63}\\.local")?"MDNS_LOCAL":f[4].matches("[0-9.]+")?"IPV4":f[4].matches("[0-9a-fA-F:]+")?"IPV6":"OTHER";
        return Map.of("type",f[7],"protocol",f[2].toLowerCase(Locale.ROOT),"addressKind",kind,"port",port);
    }
}
