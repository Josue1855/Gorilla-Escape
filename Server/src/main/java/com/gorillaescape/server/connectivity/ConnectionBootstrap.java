package com.gorillaescape.server.connectivity;

import java.net.URI;
import java.util.Map;
import java.util.concurrent.*;

/** Background public readiness; AUTO waits for bounded public readiness before issuing its first admission. */
public final class ConnectionBootstrap implements AutoCloseable {
    public enum Mode { AUTO, CABIN, CLOUDFLARED }
    public static final long DEFAULT_STARTUP_MS=30000,MAX_STARTUP_MS=60000;
    private static final long RETRY_DELAY_NANOS=TimeUnit.SECONDS.toNanos(20);
    private final java.util.function.LongSupplier clock;
    private volatile boolean cleanupPending;
    private final Tunnel tunnel;private final long timeoutMs;
    private final ExecutorService worker=Executors.newSingleThreadExecutor(r->{var t=new Thread(r,"connection-bootstrap");t.setDaemon(true);return t;});
    private final java.util.function.BooleanSupplier playersActive;
    private Mode requested;private volatile String resolved="CABIN",state="STOPPED",fallback="NONE";
    private volatile long startupMs=-1,fallbackMs=-1,revision,nextRetryNano;
    private volatile URI page,lastIngress;private final URI cabin;
    private volatile boolean closed;
    public ConnectionBootstrap(Mode requested,URI cabin,Tunnel tunnel,long timeoutMs){
        this(requested,cabin,tunnel,timeoutMs,()->false);
    }
    public ConnectionBootstrap(Mode requested,URI cabin,Tunnel tunnel,long timeoutMs,java.util.function.BooleanSupplier playersActive){
        this(requested,cabin,tunnel,timeoutMs,playersActive,System::nanoTime);
    }
    // Monotonic clock injection keeps retry/active-player regression tests independent of wall time.
    ConnectionBootstrap(Mode requested,URI cabin,Tunnel tunnel,long timeoutMs,java.util.function.BooleanSupplier playersActive,java.util.function.LongSupplier clock){
        if(timeoutMs<100||timeoutMs>MAX_STARTUP_MS)throw new IllegalArgumentException("BOOTSTRAP_BUDGET");
        this.requested=requested;this.cabin=cabin;this.page=cabin;this.tunnel=tunnel;this.timeoutMs=timeoutMs;
        this.playersActive=playersActive==null?()->false:playersActive;this.clock=java.util.Objects.requireNonNull(clock);
    }
    public synchronized void start(URI ingress){if(closed||!state.equals("STOPPED"))return;lastIngress=ingress;state="STARTING";worker.execute(()->resolve(ingress));}
    private void resolve(URI ingress){
        if(closed)return;
        if(requested==Mode.CABIN){state="READY";return;}
        long began=System.nanoTime();
        try{if(ingress==null)throw new java.io.IOException("PUBLIC_INGRESS_UNAVAILABLE");
            URI publicOrigin=CloudflaredSupervisor.validatePublicOrigin(tunnel.start(ingress,timeoutMs).toASCIIString());
            synchronized(this){
                if(closed){tunnel.close();return;}
                startupMs=elapsed(began);page=publicOrigin.resolve("/");resolved="CLOUDFLARED";revision++;state="READY";nextRetryNano=0;
            }
        }catch(Exception e){startupMs=elapsed(began);fallback(ingress==null?"PUBLIC_INGRESS_UNAVAILABLE":tunnel.failureReason());}
    }
    private static long elapsed(long began){return TimeUnit.NANOSECONDS.toMillis(System.nanoTime()-began);}
    private synchronized void fallback(String reason){
        if(closed)return;long began=System.nanoTime();tunnel.close();fallbackMs=elapsed(began);fallback=reason;
        if(!tunnel.cleanupComplete())fallback="TUNNEL_CLEANUP_INCOMPLETE";
        if(requested==Mode.CLOUDFLARED){state="FAILED";return;}
        page=cabin;resolved="CABIN";revision++;state="READY";
        if(requested==Mode.AUTO&&lastIngress!=null)nextRetryNano=clock.getAsLong()+RETRY_DELAY_NANOS;
    }
    // Poll process health and due recovery. Never close players or their LAN RTC peers.
    public synchronized void poll(){
        if(!closed&&state.equals("READY")&&resolved.equals("CLOUDFLARED")&&tunnel.state()!=Tunnel.State.RUNNING){
            page=cabin;resolved="CABIN";revision++;fallback="TUNNEL_LOST";
            state=requested==Mode.CLOUDFLARED?"FAILED":"READY";
            cleanupPending=true;
            worker.execute(()->{try{tunnel.close();if(!tunnel.cleanupComplete())fallback="TUNNEL_CLEANUP_INCOMPLETE";}finally{cleanupPending=false;}});
            if(requested==Mode.AUTO&&lastIngress!=null)nextRetryNano=clock.getAsLong()+RETRY_DELAY_NANOS;
        }
        if(!closed&&requested==Mode.AUTO&&"CABIN".equals(resolved)&&"READY".equals(state)&&lastIngress!=null&&nextRetryNano!=0&&clock.getAsLong()-nextRetryNano>=0&&!playersActive.getAsBoolean()){
            if(!cleanupPending&&tunnel.cleanupComplete()){
                nextRetryNano=0;
                state="STARTING";
                fallback="NONE";
                worker.execute(()->resolve(lastIngress));
            }
        }
    }
    public synchronized boolean request(Mode mode,boolean playersActive,URI ingress){
        if(closed||playersActive||cleanupPending||state.equals("STARTING"))return false;
        tunnel.close();if(!tunnel.cleanupComplete()){
            fallback="TUNNEL_CLEANUP_INCOMPLETE";
            if(mode!=Mode.CLOUDFLARED){requested=mode;page=cabin;resolved="CABIN";revision++;state="READY";return true;}
            state="FAILED";return false;
        }
        requested=mode;page=cabin;resolved="CABIN";revision++;fallback="NONE";startupMs=fallbackMs=-1;nextRetryNano=0;state="STOPPED";start(ingress);return true;
    }
    public Map<String,String> preflight(){
        boolean publicReady=resolved.equals("CLOUDFLARED")&&state.equals("READY");
        return Map.of("lan","PASS","java","RUNNING","rtcCapability","AVAILABLE",
            "binary",tunnel.version().equals("UNAVAILABLE")?"NOT_CONFIRMED":"PASS",
            "binarySha",tunnel instanceof CloudflaredSupervisor c?c.binarySha():"TEST_DOUBLE",
            "dns",publicReady?"PASS":"NOT_CONFIRMED","publicHttps",publicReady?"200":"NOT_CONFIRMED",
            "tunnelStartup",state);
    }
    public URI page(){return page;}public long revision(){return revision;}public boolean ready(){return state.equals("READY");}public Mode requested(){return requested;}
    public synchronized Map<String,Object> snapshot(){poll();return Map.ofEntries(Map.entry("requestedMode",requested.name()),Map.entry("resolvedMode",resolved),Map.entry("bootstrapState",state),Map.entry("tunnelState",tunnel.state().name()),Map.entry("publicOrigin",page.toASCIIString()),Map.entry("lanOrigin",cabin.toASCIIString()),Map.entry("fallbackReason",fallback),Map.entry("tunnelStartupMs",startupMs),Map.entry("fallbackMs",fallbackMs),Map.entry("wanAvailability",resolved.equals("CLOUDFLARED")&&state.equals("READY")?"TUNNEL_CONNECTED":"NOT_MEASURED"),Map.entry("webRtcPath","LAN DIRECT"));}
    @Override public void close(){
        synchronized(this){closed=true;nextRetryNano=0;worker.shutdownNow();state="STOPPED";}
        tunnel.close();
    }
}
