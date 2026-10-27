package com.gorillaescape.server.connectivity;

import com.gorillaescape.server.mobile.MobileRuntime;
import com.gorillaescape.server.mobile.MobileOrigin;
import java.net.URI;
import java.util.Map;
import org.springframework.stereotype.Component;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.beans.factory.DisposableBean;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.event.EventListener;
import org.springframework.core.annotation.Order;
import org.springframework.boot.context.event.ApplicationReadyEvent;
import org.springframework.boot.web.server.context.WebServerApplicationContext;

@Component
@ConditionalOnProperty(name="gorilla.mobile.rtc-enabled",havingValue="true")
public final class ConnectivityService implements DisposableBean {
    private final MobileRuntime mobile;
    private final String origin,address;private final boolean lab;
    private volatile ConnectionBootstrap bootstrap;private PublicIngress ingress;private Map<String,String> preflight=Map.of();
    private FirewallPreflight firewall;
    private boolean recoveryRequested;
    private boolean pendingQrRequest;private URI admissionPage;
    private final String roomOwner=java.util.UUID.randomUUID().toString();
    private volatile URI registeredTunnel;
    private volatile long lastRoomRegistrationNanos;
    private volatile int roomRenewalFailures;
    private com.gorillaescape.server.protocol.QaAdmission.View preparedQr;
    private URI preparedPage; private long preparedRevision=-1;
    public ConnectivityService(MobileRuntime mobile,@Value("${gorilla.mobile.public-origin:}")String origin,@Value("${server.address}")String address,@Value("${gorilla.mobile.lab-enabled:false}")boolean lab){this.mobile=mobile;this.origin=origin;this.address=address;this.lab=lab;}
    @EventListener @Order(-100)
    public void ready(ApplicationReadyEvent event)throws Exception{
        int port=((WebServerApplicationContext)event.getApplicationContext()).getWebServer().getPort();
        URI cabin=lab?URI.create("http://127.0.0.1:"+port+"/mobile-lab/index.html"):MobileOrigin.page(origin,address,port);
        ConnectionBootstrap.Mode mode;try{mode=ConnectionBootstrap.Mode.valueOf(System.getenv().getOrDefault("GORILLA_CONNECTION_MODE","AUTO"));}catch(IllegalArgumentException e){throw new IllegalArgumentException("CONNECTION_MODE_INVALID");}
        long timeout;try{timeout=Long.parseLong(System.getenv().getOrDefault("GORILLA_TUNNEL_STARTUP_MS",Long.toString(ConnectionBootstrap.DEFAULT_STARTUP_MS)));}catch(NumberFormatException e){throw new IllegalArgumentException("BOOTSTRAP_BUDGET");}
        String packaged=System.getenv("GORILLA_PACKAGE_ROOT");
        String supplied=System.getenv("GORILLA_CLOUDFLARED_PATH");
        if(supplied==null&&packaged!=null)supplied=java.nio.file.Path.of(packaged,"Tools","cloudflared").toString();
        var binary=CloudflaredSupervisor.discover(supplied,null);
        // Missing optional executable requires neither a listener nor a download.
        if(binary!=null&&mode!=ConnectionBootstrap.Mode.CABIN)try{ingress=PublicIngress.forRuntime(mobile);}catch(Exception optional){/* Cabin stays usable. */}
        firewall=new FirewallPreflight(lab,address,System.getenv("GORILLA_PACKAGE_ROOT"));
        var supervisor=new CloudflaredSupervisor(binary,originUri->registerRoom(originUri,mobile.roomCode()));
        bootstrap=new ConnectionBootstrap(mode,cabin,supervisor,timeout,()->!mobile.lobby().players().isEmpty());bootstrap.start(ingress==null?null:ingress.origin());
        poller=java.util.concurrent.Executors.newSingleThreadScheduledExecutor(r->{var t=new Thread(r,"connectivity-poller");t.setDaemon(true);return t;});
        poller.scheduleWithFixedDelay(this::snapshot,2,3,java.util.concurrent.TimeUnit.SECONDS);
        poller.scheduleWithFixedDelay(this::prepareFirstQr,0,100,java.util.concurrent.TimeUnit.MILLISECONDS);
        renewalPoller=java.util.concurrent.Executors.newSingleThreadScheduledExecutor(r->{var t=new Thread(r,"room-renewal");t.setDaemon(true);return t;});
        renewalPoller.scheduleWithFixedDelay(() -> {
            URI current=registeredTunnel;
            if(current==null || bootstrap==null || !"CLOUDFLARED".equals(bootstrap.snapshot().get("resolvedMode")) || System.nanoTime()-lastRoomRegistrationNanos<java.util.concurrent.TimeUnit.MINUTES.toNanos(30)) return;
            try { registerRoom(current,mobile.roomCode()); roomRenewalFailures=0; }
            catch(Exception failure) {
                if(++roomRenewalFailures>=3){supervisor.registrationLost();roomRenewalFailures=0;}
                org.slf4j.LoggerFactory.getLogger(ConnectivityService.class).warn("Room renewal failed");
            }
        },30,30,java.util.concurrent.TimeUnit.SECONDS);
    }
    static String registrationSecret(String supplied,String xdg,String home){
        if(supplied!=null&&!supplied.isBlank())return supplied;
        var paths=new java.util.ArrayList<java.nio.file.Path>();
        if(xdg!=null&&!xdg.isBlank())paths.add(java.nio.file.Path.of(xdg,"gorilla-escape","room-registration-secret"));
        if(home!=null&&!home.isBlank())paths.add(java.nio.file.Path.of(home,".config","gorilla-escape","room-registration-secret"));
        for(var path:paths)try{String value=java.nio.file.Files.readString(path).trim();if(!value.isEmpty())return value;}catch(java.io.IOException ignored){}
        return null;
    }
    private void registerRoom(URI tunnelOrigin,String roomCode){
        String secret=registrationSecret(System.getenv("GORILLA_ROOM_REGISTRATION_SECRET"),System.getenv("XDG_CONFIG_HOME"),System.getProperty("user.home"));
        if(secret==null||secret.isBlank()||tunnelOrigin==null||roomCode==null)throw new IllegalStateException("ROOM_REGISTRATION_UNCONFIGURED");
        if(!tunnelOrigin.getHost().endsWith(".trycloudflare.com"))throw new IllegalStateException("ROOM_TUNNEL_INVALID");
        try{
            var client=java.net.http.HttpClient.newBuilder().connectTimeout(java.time.Duration.ofSeconds(3)).build();
            String payload="{\"roomCode\":\""+roomCode+"\",\"targetUrl\":\""+tunnelOrigin.toASCIIString()+"\",\"owner\":\""+roomOwner+"\"}";
            var req=java.net.http.HttpRequest.newBuilder(URI.create("https://play.gorillas.run/api/register-room"))
                .header("Content-Type","application/json").header("Authorization","Bearer "+secret)
                .timeout(java.time.Duration.ofSeconds(5)).POST(java.net.http.HttpRequest.BodyPublishers.ofString(payload)).build();
            var resp=client.send(req,java.net.http.HttpResponse.BodyHandlers.ofString());
            if(resp.statusCode()==409)throw new IllegalStateException("ROOM_IN_USE");
            if(resp.statusCode()!=200||!"{\"ok\":true,\"version\":2}".equals(resp.body()))throw new IllegalStateException("ROOM_REGISTRATION_REJECTED");
            var check=java.net.http.HttpRequest.newBuilder(URI.create("https://play.gorillas.run/"+roomCode+"/"))
                .timeout(java.time.Duration.ofSeconds(5)).GET().build();
            var page=client.send(check,java.net.http.HttpResponse.BodyHandlers.ofString());
            if(page.statusCode()!=200||(!page.body().contains("Gorilla Escape")||!page.body().contains("<base href=\"/"+roomCode+"/\">")))throw new IllegalStateException("ROOM_ROUTE_UNAVAILABLE");
            registeredTunnel=tunnelOrigin;lastRoomRegistrationNanos=System.nanoTime();roomRenewalFailures=0;
        }catch(IllegalStateException e){throw e;}
        catch(Exception e){throw new IllegalStateException("ROOM_REGISTRATION_FAILED",e);}
    }

    private java.util.concurrent.ScheduledExecutorService poller,renewalPoller;
    public synchronized Map<String,Object> snapshot(){if(bootstrap==null)return Map.of("bootstrapState","STOPPED");var result=new java.util.LinkedHashMap<String,Object>(bootstrap.snapshot());
        if("TUNNEL_LOST".equals(result.get("fallbackReason")) && admissionPage!=null
                && (admissionPage.getHost().endsWith(".trycloudflare.com") || admissionPage.getHost().endsWith(".gorillas.run"))){
            mobile.revokePendingAdmissions();admissionPage=null;preparedQr=null;pendingQrRequest=true;
        }
        if(firewall!=null)firewall.poll();
        result.put("firewallState",firewall==null?"NOT_REQUIRED":firewall.state());
        boolean isCloudflared=admissionPage!=null&&(admissionPage.getHost().endsWith(".trycloudflare.com")||admissionPage.getHost().endsWith("gorillas.run")||admissionPage.getHost().equals(System.getenv("GORILLA_TUNNEL_DOMAIN")));
        result.put("admissionMode",admissionPage==null?"NONE":isCloudflared?"CLOUDFLARED":"CABIN");
        result.put("roomCode",mobile.roomCode());
        if(ingress!=null){if(bootstrap.ready()&&"CLOUDFLARED".equals(result.get("resolvedMode")))ingress.publicOrigin(URI.create("https://play.gorillas.run"));else if(!"STARTING".equals(result.get("bootstrapState"))&&"CABIN".equals(result.get("requestedMode"))){ingress.close();ingress=null;}}
        var preflight=new java.util.LinkedHashMap<String,String>(bootstrap.preflight());
        preflight.put("localTrust",lab?"LAB_NOT_REQUIRED":com.gorillaescape.server.hosting.LocalTrust.current()!=null?"PASS":"UNAVAILABLE");
        preflight.put("publicIngress",ingress!=null?"BOUND":"UNAVAILABLE");this.preflight=Map.copyOf(preflight);
        return result;}
    public synchronized Map<String,String> preflight(){snapshot();return preflight;}
    public synchronized boolean request(String mode){
        ConnectionBootstrap.Mode requested;try{requested=ConnectionBootstrap.Mode.valueOf(mode);}catch(Exception e){return false;}
        if(bootstrap==null||!mobile.lobby().players().isEmpty())return false;
        if(ingress==null&&requested!=ConnectionBootstrap.Mode.CABIN)try{ingress=PublicIngress.forRuntime(mobile);}catch(Exception e){return false;}
        return bootstrap.request(requested,!mobile.lobby().players().isEmpty(),ingress==null?null:ingress.origin());
    }
    // Precompute only after the chosen route and firewall have actually become ready.
    synchronized void prepareFirstQr(){
        if(bootstrap==null || !bootstrap.ready() || admissionPage!=null || (firewall!=null&&!firewall.ready()))return;
        long revision=bootstrap.revision(); URI page=bootstrap.page();
        if(preparedQr!=null && preparedRevision==revision && page.equals(preparedPage)) {
            var live=mobile.qa(page,"http".equals(page.getScheme()),false);
            if("AVAILABLE".equals(live.state()) && live.ttlMs()>2000)return;
        }
        var qr=mobile.qa(page,"http".equals(page.getScheme()),true);
        preparedQr="AVAILABLE".equals(qr.state())&&!qr.modules().isEmpty()?qr:null;
        preparedPage=page;preparedRevision=revision;
    }
    public synchronized com.gorillaescape.server.protocol.QaAdmission.View qa(boolean regenerate){
        if(regenerate)pendingQrRequest=true;
        snapshot();if(firewall!=null&&!firewall.ready())return new com.gorillaescape.server.protocol.QaAdmission.View("FIREWALL_NOT_READY",0,0,"");
        boolean issue=regenerate||pendingQrRequest;
        if(!issue && admissionPage==null && preparedQr!=null && bootstrap!=null && bootstrap.ready()
                && preparedRevision==bootstrap.revision() && preparedPage.equals(bootstrap.page())) {
            var live=mobile.qa(preparedPage,"http".equals(preparedPage.getScheme()),false);
            return new com.gorillaescape.server.protocol.QaAdmission.View(live.state(),live.ttlMs(),0,"");
        }
        if(!issue && admissionPage==null && bootstrap!=null && bootstrap.ready()) return new com.gorillaescape.server.protocol.QaAdmission.View("NOT_REQUESTED",0,0,"");
        if(issue && admissionPage==null && preparedQr!=null && bootstrap!=null && bootstrap.ready()
                && preparedRevision==bootstrap.revision() && preparedPage.equals(bootstrap.page())) {
            var live=mobile.qa(preparedPage,"http".equals(preparedPage.getScheme()),false);
            if("AVAILABLE".equals(live.state()) && live.ttlMs()>2000){
                var result=new com.gorillaescape.server.protocol.QaAdmission.View(live.state(),live.ttlMs(),preparedQr.size(),preparedQr.modules());
                admissionPage=preparedPage;preparedQr=null;pendingQrRequest=false;return result;
            }
        }
        if(issue || bootstrap==null || !bootstrap.ready() || preparedRevision!=bootstrap.revision())preparedQr=null;
        if(bootstrap!=null&&bootstrap.ready()&&(issue||admissionPage==null)){
            admissionPage=bootstrap.page();
        }else if(admissionPage==null||!bootstrap.ready()){
            return new com.gorillaescape.server.protocol.QaAdmission.View("CONNECTION_NOT_READY",0,0,"");
        }
        pendingQrRequest=false;
        // Freeze the chosen origin for this admission, including late readiness and WAN loss.
        return mobile.qa(admissionPage,"http".equals(admissionPage.getScheme()),issue);
    }
    public synchronized void checkFirewall(){if(firewall!=null)firewall.check();}
    public synchronized void installFirewall(boolean recovery){if(firewall!=null)firewall.install(recovery);}
    public synchronized void recovery(boolean enabled){recoveryRequested=enabled;if(firewall!=null){if(enabled)firewall.requireRecovery();else firewall.endRecovery();}}
    public synchronized boolean recoveryReady(){return recoveryRequested&&(firewall==null||firewall.ready());}
    public synchronized URI page(){snapshot();if(bootstrap==null||!bootstrap.ready()||firewall!=null&&!firewall.ready())throw new IllegalStateException("CONNECTION_NOT_READY");return bootstrap.page();}
    @Override public void destroy(){if(renewalPoller!=null){renewalPoller.shutdownNow();renewalPoller=null;}if(poller!=null){poller.shutdownNow();poller=null;}if(firewall!=null)firewall.close();if(bootstrap!=null)bootstrap.close();if(ingress!=null)ingress.close();}
}
