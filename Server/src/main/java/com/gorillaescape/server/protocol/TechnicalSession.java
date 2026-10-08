package com.gorillaescape.server.protocol;

import java.security.SecureRandom;
import java.util.*;
import java.util.function.LongSupplier;

/** Bounded technical admission only; Unity owns gameplay and competitive state. */
public final class TechnicalSession {
    public enum State { CONNECTED, ACTIVE, DISCONNECTED }
    public record Admission(String sessionId, String token, long expiresAtNanos) {}
    public record Joined(String sessionId, int playerId, String sessionDeviceId, String resumeToken, long epoch) {}
    private static final class Client {
        final int player; final String device; String resume;
        long lastSeen,epoch=1; State state=State.CONNECTED;
        Client(int player,String device,String resume,long now){this.player=player;this.device=device;this.resume=resume;lastSeen=now;}
    }
    private final String session=UUID.randomUUID().toString();
    private final LongSupplier clock;
    private final SecureRandom random=new SecureRandom();
    private final Map<String,Long> admissions=new HashMap<>();
    private final Map<Integer,Client> clients=new HashMap<>();
    private final long admissionTtl,liveness,resumeTtl;
    private boolean closed;
    public TechnicalSession(LongSupplier clock,long admissionTtl,long liveness,long resumeTtl){
        if(admissionTtl<=0||liveness<=0||resumeTtl<liveness)throw new IllegalArgumentException("SESSION_CONFIG");
        this.clock=clock;this.admissionTtl=admissionTtl;this.liveness=liveness;this.resumeTtl=resumeTtl;
    }
    private String token(){byte[] b=new byte[32];random.nextBytes(b);return Base64.getUrlEncoder().withoutPadding().encodeToString(b);}
    private void live(){if(closed)throw new IllegalStateException("SESSION_CLOSED");expire();}
    private void expire(){long now=clock.getAsLong();admissions.values().removeIf(end->now>=end);
        for(Client c:clients.values())if(c.state!=State.DISCONNECTED&&now-c.lastSeen>=liveness)c.state=State.DISCONNECTED;
        clients.values().removeIf(c->c.state==State.DISCONNECTED&&now-c.lastSeen>=resumeTtl);
    }
    public synchronized Admission createAdmission(){live();if(admissions.size()+clients.size()>=4)throw new IllegalStateException("SESSION_FULL");String t=token();long end=Math.addExact(clock.getAsLong(),admissionTtl);admissions.put(t,end);return new Admission(session,t,end);}
    public synchronized Joined join(String sessionId,String admission){live();
        if(!session.equals(sessionId)||admission==null||!admissions.containsKey(admission))throw new IllegalArgumentException("ADMISSION_INVALID");
        if(clients.size()>=4)throw new IllegalStateException("SESSION_FULL");admissions.remove(admission);
        int p=1;while(clients.containsKey(p))p++;Client c=new Client(p,UUID.randomUUID().toString(),token(),clock.getAsLong());clients.put(p,c);return joined(c);
    }
    private Joined joined(Client c){return new Joined(session,c.player,c.device,c.resume,c.epoch);}
    private Client owned(String sid,int pid,String device,long epoch){live();Client c=clients.get(pid);
        if(!session.equals(sid)||c==null||!c.device.equals(device)||c.epoch!=epoch)throw new IllegalArgumentException("CLIENT_IDENTITY");return c;}
    public synchronized void heartbeat(String sid,int pid,String device,long epoch){Client c=owned(sid,pid,device,epoch);if(c.state==State.DISCONNECTED)throw new IllegalStateException("CLIENT_DISCONNECTED");c.lastSeen=clock.getAsLong();}
    public synchronized void disconnect(String sid,int pid,String device,long epoch){Client c=owned(sid,pid,device,epoch);c.state=State.DISCONNECTED;c.lastSeen=clock.getAsLong();}
    public synchronized Joined reconnect(String sid,int pid,String resume){live();Client c=clients.get(pid);
        if(!session.equals(sid)||c==null||resume==null||!c.resume.equals(resume)||c.state!=State.DISCONNECTED)throw new IllegalArgumentException("RESUME_INVALID");
        c.epoch=Math.addExact(c.epoch,1);c.resume=token();c.state=State.CONNECTED;c.lastSeen=clock.getAsLong();return joined(c);
    }
    public synchronized void active(String sid,int pid,String device,long epoch){Client c=owned(sid,pid,device,epoch);if(c.state==State.DISCONNECTED)throw new IllegalStateException("CLIENT_DISCONNECTED");c.state=State.ACTIVE;c.lastSeen=clock.getAsLong();}
    public synchronized State state(int pid){live();Client c=clients.get(pid);if(c==null)throw new IllegalArgumentException("PLAYER_ABSENT");return c.state;}
    public synchronized int retainedClients(){live();return clients.size();}
    public synchronized void close(){closed=true;admissions.clear();clients.clear();}
}
