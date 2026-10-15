package com.gorillaescape.server.connectivity;

import static org.junit.jupiter.api.Assertions.*;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;
import java.nio.file.*;
import java.net.*;
import java.net.http.*;
import java.util.*;
import java.util.concurrent.*;

class ConnectivityTest {
    @TempDir Path temp;
    static class Fake implements Tunnel {
        State state=State.STOPPED;boolean clean=true;int starts,closes;String failure;
        public URI start(URI ingress,long ms)throws Exception{starts++;clean=false;state=State.STARTING;if(failure!=null)throw new Exception(failure);state=State.RUNNING;return URI.create("https://honest-tunnel.trycloudflare.com");}
        public State state(){return state;}public String version(){return "stub";}public boolean cleanupComplete(){return clean;}public void close(){closes++;state=State.STOPPED;clean=true;}
    }
    static void waitReady(ConnectionBootstrap b)throws Exception{long end=System.nanoTime()+TimeUnit.SECONDS.toNanos(3);while(b.snapshot().get("bootstrapState").equals("STARTING")&&System.nanoTime()<end)Thread.sleep(5);assertNotEquals("STARTING",b.snapshot().get("bootstrapState"));}
    @Test void autoFallsBackAfterCleanupAndForcedFails()throws Exception{
        for(var mode:List.of(ConnectionBootstrap.Mode.AUTO,ConnectionBootstrap.Mode.CLOUDFLARED)){
            var f=new Fake();f.failure="missing";try(var b=new ConnectionBootstrap(mode,URI.create("https://192.168.1.2:8443/"),f,100)){
                b.start(URI.create("http://127.0.0.1:9"));waitReady(b);assertTrue(f.clean);assertEquals(mode==ConnectionBootstrap.Mode.AUTO?"READY":"FAILED",b.snapshot().get("bootstrapState"));assertEquals("CABIN",b.snapshot().get("resolvedMode"));
            }
        }
    }
    @Test void cabinNeverStartsTunnelAndActivePlayerRejectsModeChange()throws Exception{
        var f=new Fake();try(var b=new ConnectionBootstrap(ConnectionBootstrap.Mode.CABIN,URI.create("https://192.168.1.2:8443/"),f,100)){
            b.start(URI.create("http://127.0.0.1:9"));waitReady(b);assertEquals(0,f.starts);assertFalse(b.request(ConnectionBootstrap.Mode.AUTO,true,URI.create("http://127.0.0.1:9")));assertEquals("CABIN",b.snapshot().get("requestedMode"));
        }
    }
    @Test void tunnelLossRotatesFutureOriginOnlyAndKeepsSessionOutsideResolver()throws Exception{
        var f=new Fake();try(var b=new ConnectionBootstrap(ConnectionBootstrap.Mode.AUTO,URI.create("https://192.168.1.2:8443/"),f,100)){
            b.start(URI.create("http://127.0.0.1:9"));waitReady(b);long revision=b.revision();assertEquals("CLOUDFLARED",b.snapshot().get("resolvedMode"));f.state=Tunnel.State.FAILED;b.poll();assertEquals("CABIN",b.snapshot().get("resolvedMode"));assertEquals("TUNNEL_LOST",b.snapshot().get("fallbackReason"));assertTrue(b.revision()>revision);assertTrue(f.clean);
        }
    }
    @Test void onlyExactTemporaryOriginAccepted(){
        assertEquals("honest-tunnel.trycloudflare.com",CloudflaredSupervisor.validatePublicOrigin("https://honest-tunnel.trycloudflare.com").getHost());
        for(String s:List.of("http://foo.trycloudflare.com","https://foo.trycloudflare.com.evil.org","https://x@foo.trycloudflare.com","https://foo.trycloudflare.com/","https://foo.trycloudflare.com:443","https://foo.trycloudflare.com?secret=x","https://"+"x".repeat(500)+".trycloudflare.com","https://foo;touch-pwn.trycloudflare.com"))assertThrows(IllegalArgumentException.class,()->CloudflaredSupervisor.validatePublicOrigin(s));
        assertNull(CloudflaredSupervisor.parseOrigin("https://foo.trycloudflare.com.evil.org"));assertNull(CloudflaredSupervisor.parseOrigin("x".repeat(2049)));
    }
    Path script(String scenario)throws Exception{
        var p=temp.resolve("cloudflared "+UUID.randomUUID());
        Files.writeString(p,"#!/bin/sh\nif [ \"$1\" = '--version' ]; then echo 'cloudflared version 2026.10.0'; exit 0; fi\n"+scenario);p.toFile().setExecutable(true);return p;
    }
    @Test void ownedProcessReadyTimeoutInvalidOutputDeathAndCleanup()throws Exception{
        for(String output:List.of("echo 'https://qa-test.trycloudflare.com'\necho 'Registered tunnel connection'\nexec sleep 20\n","echo 'https://qa-test.trycloudflare.com.evil.org'\necho 'Registered tunnel connection'\nexec sleep 20\n","exec sleep 20\n","exit 1\n")){
            var binary=script(output);try(var s=new CloudflaredSupervisor(binary,(origin,remaining)->true)){
                if(output.startsWith("echo 'https://qa-test.trycloudflare.com'")){assertEquals("qa-test.trycloudflare.com",s.start(URI.create("http://127.0.0.1:9"),500).getHost());assertEquals(Tunnel.State.RUNNING,s.state());}
                else assertThrows(Exception.class,()->s.start(URI.create("http://127.0.0.1:9"),150));
                s.close();assertTrue(s.cleanupComplete());assertEquals(Tunnel.State.STOPPED,s.state());
            }
        }
    }
    @Test void discoveryNoImplicitDownloadOrShell(){
        assertNull(CloudflaredSupervisor.discover("/missing","/missing"));assertNull(CloudflaredSupervisor.discover("$(touch /tmp/should-never-exist)",""));
    }
    @Test void ingressExactAllowlistNoPrivateHandlersCacheOrOversize()throws Exception{
        var calls=new java.util.concurrent.atomic.AtomicInteger();
        try(var ingress=new PublicIngress((path,body)->{calls.incrementAndGet();return Map.of("state","REJECTED_TEST");})){var client=HttpClient.newHttpClient();assertEquals("127.0.0.1",ingress.origin().getHost());
            for(String path:List.of("/mobile/admission","/mobile/diagnostics","/mobile/end","/api/health","/actuator","/ipc","/qa/preview","/camera/preview","/prepare","/ca.key","/trust","/filesystem","/metrics","/mobile-lab/index.html","/assets/../application.properties","/%2e%2e/application.properties","/mobile/join?admission=secret")){
                for(String method:List.of("GET","POST")){var request=HttpRequest.newBuilder(ingress.origin().resolve(path));if(method.equals("POST"))request.header("Content-Type","application/json").POST(HttpRequest.BodyPublishers.ofString("{}"));else request.GET();var r=client.send(request.build(),HttpResponse.BodyHandlers.ofString());assertEquals(404,r.statusCode(),method+" "+path);}
            }
            for(String path:PublicIngress.SIGNAL){var r=client.send(HttpRequest.newBuilder(ingress.origin().resolve(path)).header("Content-Type","application/json").POST(HttpRequest.BodyPublishers.ofString("{}")).build(),HttpResponse.BodyHandlers.ofString());assertEquals(200,r.statusCode());assertTrue(r.headers().firstValue("Cache-Control").orElseThrow().contains("no-store"));}
            assertEquals(3,calls.get());var large=client.send(HttpRequest.newBuilder(ingress.origin().resolve("/mobile/join")).header("Content-Type","application/json").POST(HttpRequest.BodyPublishers.ofString("x".repeat(PublicIngress.MAX_BODY+1))).build(),HttpResponse.BodyHandlers.ofString());assertEquals(413,large.statusCode());assertEquals(3,calls.get());
            var foreign=client.send(HttpRequest.newBuilder(ingress.origin().resolve("/mobile/join")).header("Origin","https://evil.org").header("Content-Type","application/json").POST(HttpRequest.BodyPublishers.ofString("{}")).build(),HttpResponse.BodyHandlers.ofString());assertEquals(403,foreign.statusCode());
        }
    }

    @Test void originRotationRevokesPendingButRetainsPlayerAndEpoch(){
        var session=new com.gorillaescape.server.protocol.TechnicalSession(()->0L,30_000_000_000L,5_000_000_000L,30_000_000_000L);
        var first=session.createAdmission();var player=session.join(first.sessionId(),first.token());var pending=session.createAdmission();var before=session.lobby();
        session.revokePendingAdmissions();assertEquals(before,session.lobby());assertThrows(IllegalArgumentException.class,()->session.join(pending.sessionId(),pending.token()));
        session.heartbeat(player.sessionId(),player.playerId(),player.sessionDeviceId(),player.epoch());
        var next=session.createAdmission();assertEquals(first.sessionId(),next.sessionId());assertNotEquals(pending.token(),next.token());
        var link=com.gorillaescape.server.protocol.JoinQrPoc.link(URI.create("https://qa-test.trycloudflare.com/"),next);
        assertNull(link.getRawQuery());assertTrue(link.getRawFragment().contains("admission="));assertFalse(link.toString().split("#")[0].contains(next.token()));
    }
    @Test void startupBudgetRejectsUnboundedValues(){
        for(long value:new long[]{0,99,15001,Long.MAX_VALUE})assertThrows(IllegalArgumentException.class,()->new ConnectionBootstrap(ConnectionBootstrap.Mode.AUTO,URI.create("https://192.168.1.2:8443/"),new Fake(),value));
    }
    @Test void httpHealthFailureCannotCreateReadyAndVersionTimeoutCleansOwnedChild()throws Exception{
        var binary=script("echo 'https://qa-test.trycloudflare.com'\necho 'Registered tunnel connection'\nexec sleep 20\n");
        try(var supervisor=new CloudflaredSupervisor(binary,(origin,budget)->false)){assertThrows(Exception.class,()->supervisor.start(URI.create("http://127.0.0.1:9"),200));assertTrue(supervisor.cleanupComplete());assertEquals(Tunnel.State.FAILED,supervisor.state());}
        var hung=temp.resolve("hung-version");Files.writeString(hung,"#!/bin/sh\nexec sleep 20\n");hung.toFile().setExecutable(true);
        try(var supervisor=new CloudflaredSupervisor(hung,(origin,budget)->true)){assertThrows(Exception.class,()->supervisor.start(URI.create("http://127.0.0.1:9"),100));assertTrue(supervisor.cleanupComplete());}
    }

    @Test void actualMissingExecutableFallbackTimingIsBounded()throws Exception{
        var rows=new ArrayList<Map<String,Object>>();
        for(var mode:List.of(ConnectionBootstrap.Mode.AUTO,ConnectionBootstrap.Mode.CLOUDFLARED)){
            try(var b=new ConnectionBootstrap(mode,URI.create("https://192.168.1.2:8443/"),new CloudflaredSupervisor(null),6000)){
                long began=System.nanoTime();b.start(URI.create("http://127.0.0.1:9"));waitReady(b);var snapshot=b.snapshot();rows.add(snapshot);
                assertTrue((System.nanoTime()-began)/1_000_000<1000);assertEquals("CABIN",snapshot.get("resolvedMode"));assertEquals(mode==ConnectionBootstrap.Mode.AUTO?"READY":"FAILED",snapshot.get("bootstrapState"));
            }
        }
        String output=System.getenv("GORILLA_BOOTSTRAP_TIMING_REPORT");if(output!=null)Files.writeString(Path.of(output),com.gorillaescape.server.protocol.GorillaProtocol.JSON.writeValueAsString(rows)+"\n");
    }

    @Test void qrRequestWhileBootstrapIsStartingIsFulfilledWhenReady()throws Exception{
        var release=new CountDownLatch(1);
        var fake=new Fake(){@Override public URI start(URI ingress,long budget)throws Exception{release.await(1,TimeUnit.SECONDS);return super.start(ingress,budget);}};
        var mobile=new com.gorillaescape.server.mobile.MobileRuntime();
        try(var bootstrap=new ConnectionBootstrap(ConnectionBootstrap.Mode.AUTO,URI.create("https://192.168.1.2:8443/"),fake,1000)){
            var service=new ConnectivityService(mobile,"","127.0.0.1",true);
            var field=ConnectivityService.class.getDeclaredField("bootstrap");field.setAccessible(true);field.set(service,bootstrap);
            bootstrap.start(URI.create("http://127.0.0.1:9"));
            assertEquals("CONNECTION_NOT_READY",service.qa(true).state());release.countDown();waitReady(bootstrap);
            var ready=service.qa(false);assertEquals("AVAILABLE",ready.state());assertFalse(ready.modules().isEmpty());
        }finally{release.countDown();mobile.destroy();}
    }

    @Test void actualRunningProcessDeathIsDetectedAndMalformedOutputIsBounded()throws Exception{
        var dying=script("echo 'https://qa-test.trycloudflare.com'\necho 'Registered tunnel connection'\nsleep 0.1\n");
        try(var tunnel=new CloudflaredSupervisor(dying,(origin,budget)->true)){
            tunnel.start(URI.create("http://127.0.0.1:9"),500);long end=System.nanoTime()+TimeUnit.SECONDS.toNanos(1);
            while(tunnel.state()==Tunnel.State.RUNNING&&System.nanoTime()<end)Thread.sleep(10);assertEquals(Tunnel.State.FAILED,tunnel.state());tunnel.close();assertTrue(tunnel.cleanupComplete());
        }
        var bad=script("printf '%s\\n' 'https://qa-test.trycloudflare.com;$(touch should-never-exist)'\necho 'Registered tunnel connection'\nexec sleep 20\n");
        try(var tunnel=new CloudflaredSupervisor(bad,(origin,budget)->true)){assertThrows(Exception.class,()->tunnel.start(URI.create("http://127.0.0.1:9"),150));assertTrue(tunnel.cleanupComplete());}
    }
}
