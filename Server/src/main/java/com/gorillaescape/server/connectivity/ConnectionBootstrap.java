package com.gorillaescape.server.connectivity;

import java.net.URI;
import java.util.Map;
import java.util.concurrent.*;

/** Deterministic resolver: one attempt, bounded budget, complete cleanup before Cabin fallback. */
public final class ConnectionBootstrap implements AutoCloseable {
    public enum Mode { AUTO, CABIN, CLOUDFLARED }
    private final Tunnel tunnel;private final long timeoutMs;
    private final ExecutorService worker=Executors.newSingleThreadExecutor(r->{var t=new Thread(r,"connection-bootstrap");t.setDaemon(true);return t;});
    private Mode requested;private volatile String resolved="CABIN",state="STOPPED",fallback="NONE";
    private volatile long startupMs=-1,fallbackMs=-1,revision;
    private volatile URI page;private final URI cabin;
    private volatile boolean closed;
    public ConnectionBootstrap(Mode requested,URI cabin,Tunnel tunnel,long timeoutMs){
        if(timeoutMs<100||timeoutMs>15000)throw new IllegalArgumentException("BOOTSTRAP_BUDGET");
        this.requested=requested;this.cabin=cabin;this.page=cabin;this.tunnel=tunnel;this.timeoutMs=timeoutMs;
    }
    public synchronized void start(URI ingress){if(closed||!state.equals("STOPPED"))return;state="STARTING";worker.execute(()->resolve(ingress));}
    private void resolve(URI ingress){
        if(requested==Mode.CABIN){state="READY";return;}
        long began=System.nanoTime();
        try{URI publicOrigin=CloudflaredSupervisor.validatePublicOrigin(tunnel.start(ingress,timeoutMs).toASCIIString());
            if(closed){tunnel.close();return;}
            startupMs=elapsed(began);page=publicOrigin.resolve("/");resolved="CLOUDFLARED";revision++;state="READY";
        }catch(Exception e){startupMs=elapsed(began);fallback("TUNNEL_UNAVAILABLE_OR_START_FAILED");}
    }
    private static long elapsed(long began){return TimeUnit.NANOSECONDS.toMillis(System.nanoTime()-began);}
    private synchronized void fallback(String reason){
        if(closed)return;long began=System.nanoTime();tunnel.close();fallbackMs=elapsed(began);fallback=reason;
        if(!tunnel.cleanupComplete()){state="FAILED";fallback="TUNNEL_CLEANUP_INCOMPLETE";return;}
        if(requested==Mode.CLOUDFLARED){state="FAILED";return;}
        page=cabin;resolved="CABIN";revision++;state="READY";
    }
    // Poll only process health. A tunnel failure never closes players or their LAN RTC peers.
    public synchronized void poll(){if(!closed&&state.equals("READY")&&resolved.equals("CLOUDFLARED")&&tunnel.state()!=Tunnel.State.RUNNING)fallback("TUNNEL_LOST");}
    public synchronized boolean request(Mode mode,boolean playersActive,URI ingress){
        if(closed||playersActive||state.equals("STARTING"))return false;
        tunnel.close();if(!tunnel.cleanupComplete()){state="FAILED";return false;}
        requested=mode;page=cabin;resolved="CABIN";revision++;fallback="NONE";startupMs=fallbackMs=-1;state="STOPPED";start(ingress);return true;
    }
    public URI page(){return page;}public long revision(){return revision;}public boolean ready(){return state.equals("READY");}
    public synchronized Map<String,Object> snapshot(){poll();return Map.ofEntries(Map.entry("requestedMode",requested.name()),Map.entry("resolvedMode",resolved),Map.entry("bootstrapState",state),Map.entry("tunnelState",tunnel.state().name()),Map.entry("publicOrigin",page.toASCIIString()),Map.entry("lanOrigin",cabin.toASCIIString()),Map.entry("fallbackReason",fallback),Map.entry("tunnelStartupMs",startupMs),Map.entry("fallbackMs",fallbackMs),Map.entry("wanAvailability",resolved.equals("CLOUDFLARED")&&state.equals("READY")?"TUNNEL_CONNECTED":"NOT_MEASURED"),Map.entry("webRtcPath","LAN DIRECT"));}
    @Override public void close(){closed=true;worker.shutdownNow();tunnel.close();state="STOPPED";}
}
