package com.gorillaescape.server.protocol;

import java.security.SecureRandom;
import java.util.*;
import java.util.function.LongSupplier;

/** Bounded technical admission only; Unity owns gameplay and competitive state. */
public final class TechnicalSession {
    public enum State { CONNECTED, ACTIVE, DISCONNECTED }
    public enum LobbyPhase { OPEN, STARTING, STARTED }
    public record LobbyPlayer(int playerId, String deviceSessionId, long connectionEpoch,
                              String connectionState, boolean networkReady, boolean inputReady, boolean playerReady) {}
    public record LobbySnapshot(int lobbyVersion, String sessionId, String phase, List<LobbyPlayer> players) {}
    private static final long INPUT_FRESHNESS_NANOS=2_000_000_000L;
    public record Admission(String sessionId, String token, long expiresAtNanos) {}
    public record Joined(String sessionId, int playerId, String sessionDeviceId, String resumeToken, long epoch) {}
    private static final class Client {
        final int player; final String device; String resume;
        long lastSeen,epoch=1,inputSeen; boolean networkReady,inputAvailable,playerReady; State state=State.CONNECTED;
        Client(int player,String device,String resume,long now){this.player=player;this.device=device;this.resume=resume;lastSeen=now;}
    }
    private final String session=UUID.randomUUID().toString();
    private final LongSupplier clock;
    private final SecureRandom random=new SecureRandom();
    private final Map<String,Long> admissions=new HashMap<>();
    private final Map<Integer,Client> clients=new HashMap<>();
    private final long admissionTtl,liveness,resumeTtl;
    private boolean closed;
    private LobbyPhase phase=LobbyPhase.OPEN;
    public TechnicalSession(LongSupplier clock,long admissionTtl,long liveness,long resumeTtl){
        if(admissionTtl<=0||liveness<=0||resumeTtl<liveness)throw new IllegalArgumentException("SESSION_CONFIG");
        this.clock=clock;this.admissionTtl=admissionTtl;this.liveness=liveness;this.resumeTtl=resumeTtl;
    }
    private String token(){byte[] b=new byte[32];random.nextBytes(b);return Base64.getUrlEncoder().withoutPadding().encodeToString(b);}
    private void live(){if(closed)throw new IllegalStateException("SESSION_CLOSED");expire();}
    private void expire(){long now=clock.getAsLong();admissions.values().removeIf(end->now>=end);
        for(Client c:clients.values()) {
            if(c.state!=State.DISCONNECTED&&now-c.lastSeen>=liveness){c.state=State.DISCONNECTED;clearReady(c);}
            if(!inputReady(c))c.playerReady=false;
        }
        clients.values().removeIf(c->c.state==State.DISCONNECTED&&now-c.lastSeen>=resumeTtl);
    }
    public synchronized Admission createAdmission(){live();admissionOpen();if(admissions.size()+clients.size()>=4)throw new IllegalStateException("SESSION_FULL");String t=token();long end=Math.addExact(clock.getAsLong(),admissionTtl);admissions.put(t,end);return new Admission(session,t,end);}
    public synchronized Joined join(String sessionId,String admission){live();admissionOpen();
        if(!session.equals(sessionId)||admission==null||!admissions.containsKey(admission))throw new IllegalArgumentException("ADMISSION_INVALID");
        if(clients.size()>=4)throw new IllegalStateException("SESSION_FULL");admissions.remove(admission);
        int p=1;while(clients.containsKey(p))p++;Client c=new Client(p,UUID.randomUUID().toString(),token(),clock.getAsLong());clients.put(p,c);return joined(c);
    }
    /** Only an unconfirmed initial RTC allocation may be rolled back. Admission stays one-use. */
    public synchronized void abortUnconfirmedJoin(Joined joined){
        Client c=owned(joined.sessionId(),joined.playerId(),joined.sessionDeviceId(),joined.epoch());
        if(!c.resume.equals(joined.resumeToken()))throw new IllegalArgumentException("CLIENT_IDENTITY");
        clients.remove(c.player);
    }
    public synchronized String admissionFailure(String sid,String token){
        live();
        if(!session.equals(sid)||token==null||!token.matches("[A-Za-z0-9_-]{43}"))return "ADMISSION_INVALID";
        // Missing well-formed tokens are indistinguishable: expired, consumed or never issued.
        return "ADMISSION_EXPIRED_OR_USED";
    }
    private Joined joined(Client c){return new Joined(session,c.player,c.device,c.resume,c.epoch);}
    private Client owned(String sid,int pid,String device,long epoch){live();Client c=clients.get(pid);
        if(!session.equals(sid)||c==null||!c.device.equals(device)||c.epoch!=epoch)throw new IllegalArgumentException("CLIENT_IDENTITY");return c;}
    public synchronized void heartbeat(String sid,int pid,String device,long epoch){Client c=owned(sid,pid,device,epoch);if(c.state==State.DISCONNECTED)throw new IllegalStateException("CLIENT_DISCONNECTED");c.lastSeen=clock.getAsLong();}
    public synchronized void disconnect(String sid,int pid,String device,long epoch){Client c=owned(sid,pid,device,epoch);c.state=State.DISCONNECTED;clearReady(c);c.lastSeen=clock.getAsLong();}
    public synchronized Joined reconnect(String sid,int pid,String resume){live();Client c=clients.get(pid);
        if(!session.equals(sid)||c==null||resume==null||!c.resume.equals(resume)||c.state!=State.DISCONNECTED)throw new IllegalArgumentException("RESUME_INVALID");
        c.epoch=Math.addExact(c.epoch,1);c.resume=token();c.state=State.CONNECTED;clearReady(c);c.lastSeen=clock.getAsLong();return joined(c);
    }
    public synchronized void active(String sid,int pid,String device,long epoch){Client c=owned(sid,pid,device,epoch);if(c.state==State.DISCONNECTED)throw new IllegalStateException("CLIENT_DISCONNECTED");c.state=State.ACTIVE;c.lastSeen=clock.getAsLong();}
    private void admissionOpen(){if(phase!=LobbyPhase.OPEN)throw new IllegalStateException("LOBBY_ADMISSION_CLOSED");}
    private static void clearReady(Client c){c.networkReady=false;c.inputAvailable=false;c.playerReady=false;}
    private boolean inputReady(Client c){return c.networkReady&&c.inputAvailable&&c.state!=State.DISCONNECTED&&clock.getAsLong()-c.inputSeen<INPUT_FRESHNESS_NANOS;}
    public synchronized void networkReady(String sid,int pid,String device,long epoch){
        Client c=owned(sid,pid,device,epoch);if(c.state==State.DISCONNECTED)throw new IllegalStateException("CLIENT_DISCONNECTED");c.networkReady=true;
    }
    /** Validated fresh raw movement, not a client claim of official READY. */
    public synchronized void inputReady(String sid,int pid,String device,long epoch,boolean available){
        Client c=owned(sid,pid,device,epoch);if(c.state==State.DISCONNECTED)throw new IllegalStateException("CLIENT_DISCONNECTED");
        c.inputAvailable=available;c.inputSeen=clock.getAsLong();if(!inputReady(c))c.playerReady=false;
    }
    public synchronized void suspendInput(String sid,int pid,String device,long epoch){
        Client c=owned(sid,pid,device,epoch);c.inputAvailable=false;c.playerReady=false;
    }
    /** Only the authenticated Unity IPC consumer invokes this command. No mobile endpoint. */
    public synchronized void unityPlayerReady(String sid,int pid,String device,long epoch,boolean ready){
        Client c=owned(sid,pid,device,epoch);
        if(phase==LobbyPhase.STARTED)throw new IllegalStateException("LOBBY_READY_FROZEN");
        if(ready&&!inputReady(c))throw new IllegalStateException("LOBBY_PREREQUISITES");c.playerReady=ready;
    }
    /** Replicate Unity's official state; Java never starts a game autonomously. */
    public synchronized void unityPhase(String sid,LobbyPhase next){
        live();if(!session.equals(sid)||next==null)throw new IllegalArgumentException("LOBBY_IDENTITY");
        if(next==phase)return;
        if(!(phase==LobbyPhase.OPEN&&next==LobbyPhase.STARTING||phase==LobbyPhase.STARTING&&next==LobbyPhase.STARTED))throw new IllegalStateException("LOBBY_TRANSITION");
        if(clients.isEmpty()||clients.values().stream().anyMatch(c->!c.playerReady||!inputReady(c)))throw new IllegalStateException("LOBBY_PREREQUISITES");
        phase=next;admissions.clear();
    }
    public synchronized void unityDisconnected(){for(Client c:clients.values())c.playerReady=false;}
    public synchronized LobbySnapshot lobby(){
        live();return new LobbySnapshot(1,session,phase.name(),clients.values().stream().sorted(Comparator.comparingInt(c->c.player))
            .map(c->{boolean input=inputReady(c);if(!input)c.playerReady=false;
                return new LobbyPlayer(c.player,c.device,c.epoch,c.state.name(),c.networkReady,input,c.playerReady);}).toList());
    }
    public synchronized State state(int pid){live();Client c=clients.get(pid);if(c==null)throw new IllegalArgumentException("PLAYER_ABSENT");return c.state;}
    public synchronized int retainedClients(){live();return clients.size();}
    public synchronized void close(){closed=true;admissions.clear();clients.clear();}
}
