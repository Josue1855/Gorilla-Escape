package com.gorillaescape.server.connectivity;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;
import java.io.IOException;
import java.net.URI;
import java.nio.file.*;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.*;
import java.util.function.BooleanSupplier;
import static org.junit.jupiter.api.Assertions.*;

class ConnectivityRecoveryTest {
    static final URI LAN=URI.create("https://192.168.1.2:8443/"),INGRESS=URI.create("http://127.0.0.1:9");
    @TempDir Path temp;
    static final class Fake implements Tunnel {
        volatile State state=State.STOPPED;
        volatile boolean fail=true,clean=true,cleanupWorks=true;
        final AtomicInteger starts=new AtomicInteger();
        public URI start(URI ingress,long budget)throws Exception{
            starts.incrementAndGet();clean=false;state=State.STARTING;
            if(fail)throw new IOException("FIXTURE_UNAVAILABLE");
            state=State.RUNNING;return URI.create("https://recovered.trycloudflare.com");
        }
        public State state(){return state;}
        public String version(){return "fixture";}
        public boolean cleanupComplete(){return clean;}
        public void close(){state=State.STOPPED;clean=cleanupWorks;}
    }
    static void until(BooleanSupplier done)throws Exception{
        long end=System.nanoTime()+TimeUnit.SECONDS.toNanos(3);
        while(!done.getAsBoolean()&&System.nanoTime()<end)Thread.sleep(2);
        assertTrue(done.getAsBoolean(),"fixture did not settle");
    }
    static void settled(ConnectionBootstrap bootstrap)throws Exception{
        until(()->!"STARTING".equals(bootstrap.snapshot().get("bootstrapState")));
    }
    static ConnectionBootstrap auto(Fake fake,AtomicBoolean active,AtomicLong clock){
        return new ConnectionBootstrap(ConnectionBootstrap.Mode.AUTO,LAN,fake,30000,active::get,clock::get);
    }
    @Test void thirtySecondDefaultAndSixtySecondMaximumAreAccepted(){
        assertEquals(30000,ConnectionBootstrap.DEFAULT_STARTUP_MS);
        assertEquals(60000,ConnectionBootstrap.MAX_STARTUP_MS);
        for(long budget:new long[]{100,30000,60000})try(var b=new ConnectionBootstrap(ConnectionBootstrap.Mode.AUTO,LAN,new Fake(),budget)){assertFalse(b.ready());}
        assertThrows(IllegalArgumentException.class,()->new ConnectionBootstrap(ConnectionBootstrap.Mode.AUTO,LAN,new Fake(),60001));
    }
    @Test void retryWaitsTwentySecondsThenCanRecover()throws Exception{
        var fake=new Fake();var clock=new AtomicLong(-TimeUnit.SECONDS.toNanos(60)+1);var active=new AtomicBoolean();
        try(var b=auto(fake,active,clock)){
            b.start(INGRESS);settled(b);assertEquals(1,fake.starts.get());
            fake.fail=false;clock.addAndGet(TimeUnit.SECONDS.toNanos(19));b.poll();assertEquals(1,fake.starts.get());
            clock.addAndGet(TimeUnit.SECONDS.toNanos(1));b.poll();settled(b);
            assertEquals(2,fake.starts.get());assertEquals("CLOUDFLARED",b.snapshot().get("resolvedMode"));
            for(int i=0;i<100;i++)b.poll();assertEquals(2,fake.starts.get());
        }
    }
    @Test void activePlayersDeferRetryUntilTheyLeave()throws Exception{
        var fake=new Fake();var clock=new AtomicLong(1);var active=new AtomicBoolean(true);
        try(var b=auto(fake,active,clock)){
            b.start(INGRESS);settled(b);clock.addAndGet(TimeUnit.SECONDS.toNanos(60));
            b.poll();assertEquals(1,fake.starts.get());assertTrue(b.ready());assertEquals(LAN,b.page());
            active.set(false);fake.fail=false;b.poll();settled(b);assertEquals(2,fake.starts.get());
        }
    }
    @Test void incompleteCleanupPreventsASecondProcess()throws Exception{
        var fake=new Fake();fake.cleanupWorks=false;var clock=new AtomicLong(1);
        try(var b=auto(fake,new AtomicBoolean(),clock)){
            b.start(INGRESS);settled(b);clock.addAndGet(TimeUnit.SECONDS.toNanos(60));
            for(int i=0;i<100;i++)b.poll();assertEquals(1,fake.starts.get());assertTrue(b.ready());
            fake.clean=true;fake.fail=false;b.poll();settled(b);assertEquals(2,fake.starts.get());
        }
    }
    @Test void forcedModesAndMissingIngressNeverRetry()throws Exception{
        for(var mode:new ConnectionBootstrap.Mode[]{ConnectionBootstrap.Mode.CABIN,ConnectionBootstrap.Mode.CLOUDFLARED}){
            var fake=new Fake();var clock=new AtomicLong(1);
            try(var b=new ConnectionBootstrap(mode,LAN,fake,30000,()->false,clock::get)){
                b.start(INGRESS);settled(b);int before=fake.starts.get();clock.addAndGet(TimeUnit.HOURS.toNanos(1));b.poll();assertEquals(before,fake.starts.get());
            }
        }
        var fake=new Fake();var clock=new AtomicLong(1);
        try(var b=auto(fake,new AtomicBoolean(),clock)){b.start(null);settled(b);clock.addAndGet(TimeUnit.HOURS.toNanos(1));b.poll();assertEquals(0,fake.starts.get());}
    }
    @Test void closeCancelsScheduledRecovery()throws Exception{
        var fake=new Fake();var clock=new AtomicLong(1);var b=auto(fake,new AtomicBoolean(),clock);
        b.start(INGRESS);settled(b);b.close();clock.addAndGet(TimeUnit.HOURS.toNanos(1));b.poll();
        assertEquals(1,fake.starts.get());assertEquals("STOPPED",b.snapshot().get("bootstrapState"));assertTrue(fake.clean);
    }
    @Test void lostRunningTunnelRecoversOnlyAfterCleanup()throws Exception{
        var fake=new Fake();fake.fail=false;var clock=new AtomicLong(1);
        try(var b=auto(fake,new AtomicBoolean(),clock)){
            b.start(INGRESS);settled(b);fake.state=Tunnel.State.FAILED;b.poll();until(()->fake.clean);
            assertEquals(LAN,b.page());clock.addAndGet(TimeUnit.SECONDS.toNanos(20));
            until(()->{b.poll();return fake.starts.get()==2;});settled(b);assertEquals("CLOUDFLARED",b.snapshot().get("resolvedMode"));
        }
    }
    @Test void serviceRetainsIngressAndFrozenAdmissionAcrossAutomaticRetry()throws Exception{
        var fake=new Fake();var clock=new AtomicLong(1);
        var mobile=new com.gorillaescape.server.mobile.MobileRuntime();
        try(var ingress=new PublicIngress((path,body)->java.util.Map.of());var b=auto(fake,new AtomicBoolean(),clock)){
            var service=new ConnectivityService(mobile,"","127.0.0.1",true);
            var bootstrap=ConnectivityService.class.getDeclaredField("bootstrap");bootstrap.setAccessible(true);bootstrap.set(service,b);
            var gateway=ConnectivityService.class.getDeclaredField("ingress");gateway.setAccessible(true);gateway.set(service,ingress);
            b.start(ingress.origin());settled(b);assertEquals("AVAILABLE",service.qa(true).state());assertSame(ingress,gateway.get(service));
            fake.fail=false;clock.addAndGet(TimeUnit.SECONDS.toNanos(20));b.poll();settled(b);
            assertEquals("CABIN",service.snapshot().get("admissionMode"));assertEquals("AVAILABLE",service.qa(false).state());
            assertTrue(service.qa(false).modules().isEmpty());assertEquals("AVAILABLE",service.qa(true).state());
            assertEquals("CLOUDFLARED",service.snapshot().get("admissionMode"));
        }finally{mobile.destroy();}
    }
    @Test void partialAndDuplicateRegistrationsDoNotInvalidateOtherConnections(){
        var connections=new CloudflaredSupervisor.RegistrationTracker();
        connections.observe("Registered tunnel connection connIndex=0");connections.observe("Registered tunnel connection connIndex=0");
        connections.observe("Registered tunnel connection connIndex=1");assertEquals(2,connections.size());
        connections.observe("Unregistered tunnel connection connIndex=0");assertEquals(1,connections.size());
        connections.observe("Unregistered tunnel connection connIndex=0");assertEquals(1,connections.size());
        connections.observe("Serve tunnel error connIndex=1");assertEquals(0,connections.size());
    }
    Path script(String output)throws Exception{
        return TestCloudflaredFixture.create(temp,output+"\nexec sleep 20\n");
    }
    @Test void realReaderKeepsSurvivingConnection()throws Exception{
        var binary=script("echo 'Registered tunnel connection connIndex=0'\necho 'Registered tunnel connection connIndex=1'\necho 'Unregistered tunnel connection connIndex=0'\necho 'https://partial.trycloudflare.com'");
        try(var supervisor=new CloudflaredSupervisor(binary,(origin,budget)->true)){
            supervisor.start(INGRESS,1000);until(()->Integer.valueOf(1).equals(supervisor.diagnostics().get("activeConnections")));assertEquals(1,supervisor.diagnostics().get("activeConnections"));assertEquals(Tunnel.State.RUNNING,supervisor.state());
            supervisor.close();assertTrue(supervisor.cleanupComplete());
        }
    }
    @Test void healthNeedsFourConsecutiveFailuresAndSuccessResetsCounter()throws Exception{
        var fail=new AtomicBoolean();var budget=new AtomicLong();
        var binary=script("echo 'https://health.trycloudflare.com'\necho 'Registered tunnel connection connIndex=0'");
        try(var s=new CloudflaredSupervisor(binary,(origin,remaining)->{budget.set(remaining);return !fail.get();})){
            var origin=s.start(INGRESS,1000);fail.set(true);
            for(int i=0;i<3;i++)s.checkHealth(origin);assertEquals(Tunnel.State.RUNNING,s.state());
            fail.set(false);s.checkHealth(origin);fail.set(true);
            for(int i=0;i<3;i++)s.checkHealth(origin);assertEquals(Tunnel.State.RUNNING,s.state());
            s.checkHealth(origin);assertEquals(Tunnel.State.FAILED,s.state());assertEquals(3000,budget.get());
        }
    }
    @Test void interruptedRecoveryCannotSpawnAfterShutdown()throws Exception{
        var binary=script("echo 'https://cancelled.trycloudflare.com'\necho 'Registered tunnel connection'");
        try(var supervisor=new CloudflaredSupervisor(binary,(origin,budget)->true)){
            Thread.currentThread().interrupt();
            try{assertThrows(InterruptedException.class,()->supervisor.start(INGRESS,1000));}
            finally{Thread.interrupted();}
            assertTrue(supervisor.cleanupComplete());assertEquals(Tunnel.State.STOPPED,supervisor.state());
        }
    }
    @Test void diagnosticIncludesCauseTypesWithoutExceptionPayload(){
        var error=new IOException("secret-url-or-payload",new java.net.ConnectException("sensitive-address"));
        assertEquals("IOException <- ConnectException",CloudflaredSupervisor.describeFailure(error));
    }
}
