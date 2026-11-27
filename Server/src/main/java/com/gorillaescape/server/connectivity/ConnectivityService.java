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
    public ConnectivityService(MobileRuntime mobile,@Value("${gorilla.mobile.public-origin:}")String origin,@Value("${server.address}")String address,@Value("${gorilla.mobile.lab-enabled:false}")boolean lab){this.mobile=mobile;this.origin=origin;this.address=address;this.lab=lab;}
    @EventListener @Order(-100)
    public void ready(ApplicationReadyEvent event)throws Exception{
        int port=((WebServerApplicationContext)event.getApplicationContext()).getWebServer().getPort();
        URI cabin=lab?URI.create("http://127.0.0.1:"+port+"/mobile-lab/index.html"):MobileOrigin.page(origin,address,port);
        ConnectionBootstrap.Mode mode;try{mode=ConnectionBootstrap.Mode.valueOf(System.getenv().getOrDefault("GORILLA_CONNECTION_MODE","AUTO"));}catch(IllegalArgumentException e){throw new IllegalArgumentException("CONNECTION_MODE_INVALID");}
        long timeout;try{timeout=Long.parseLong(System.getenv().getOrDefault("GORILLA_TUNNEL_STARTUP_MS","15000"));}catch(NumberFormatException e){throw new IllegalArgumentException("BOOTSTRAP_BUDGET");}
        String packaged=System.getenv("GORILLA_PACKAGE_ROOT");
        String supplied=System.getenv("GORILLA_CLOUDFLARED_PATH");
        if(supplied==null&&packaged!=null)supplied=java.nio.file.Path.of(packaged,"Tools","cloudflared").toString();
        var binary=CloudflaredSupervisor.discover(supplied,null);
        // Missing optional executable requires neither a listener nor a download.
        if(binary!=null&&mode!=ConnectionBootstrap.Mode.CABIN)try{ingress=PublicIngress.forRuntime(mobile);}catch(Exception optional){/* Cabin stays usable. */}
        firewall=new FirewallPreflight(lab,address,System.getenv("GORILLA_PACKAGE_ROOT"));
        bootstrap=new ConnectionBootstrap(mode,cabin,new CloudflaredSupervisor(binary),timeout);bootstrap.start(ingress==null?null:ingress.origin());
    }
    public synchronized Map<String,Object> snapshot(){if(bootstrap==null)return Map.of("bootstrapState","STOPPED");var result=new java.util.LinkedHashMap<String,Object>(bootstrap.snapshot());
        if(firewall!=null)firewall.poll();
        result.put("firewallState",firewall==null?"NOT_REQUIRED":firewall.state());
        result.put("recoveryRequested",recoveryRequested);
        result.put("admissionMode",admissionPage==null?"NONE":admissionPage.getHost().endsWith(".trycloudflare.com")?"CLOUDFLARED":"CABIN");
        if(ingress!=null){if(bootstrap.ready()&&"CLOUDFLARED".equals(result.get("resolvedMode")))ingress.publicOrigin(URI.create("https://"+bootstrap.page().getHost()));else if(!"STARTING".equals(result.get("bootstrapState"))){ingress.close();ingress=null;}}
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
    public synchronized com.gorillaescape.server.protocol.QaAdmission.View qa(boolean regenerate){
        if(regenerate)pendingQrRequest=true;
        snapshot();if(firewall!=null&&!firewall.ready())return new com.gorillaescape.server.protocol.QaAdmission.View("FIREWALL_NOT_READY",0,0,"");
        if(bootstrap==null||!bootstrap.ready()&&(admissionPage==null||regenerate||pendingQrRequest))return new com.gorillaescape.server.protocol.QaAdmission.View("CONNECTION_NOT_READY",0,0,"");
        boolean issue=regenerate||pendingQrRequest;
        if(issue||admissionPage==null)admissionPage=bootstrap.page();
        pendingQrRequest=false;
        // Freeze the chosen origin for this admission, including late readiness and WAN loss.
        return mobile.qa(admissionPage,"http".equals(admissionPage.getScheme()),issue);
    }
    public synchronized void checkFirewall(){if(firewall!=null)firewall.check();}
    public synchronized void installFirewall(boolean recovery){if(firewall!=null)firewall.install(recovery);}
    public synchronized void recovery(boolean enabled){recoveryRequested=enabled;if(firewall!=null){if(enabled)firewall.requireRecovery();else firewall.endRecovery();}}
    public synchronized boolean recoveryReady(){return recoveryRequested&&(firewall==null||firewall.ready());}
    public synchronized URI page(){snapshot();if(bootstrap==null||!bootstrap.ready()||firewall!=null&&!firewall.ready())throw new IllegalStateException("CONNECTION_NOT_READY");return bootstrap.page();}
    @Override public void destroy(){if(firewall!=null)firewall.close();if(bootstrap!=null)bootstrap.close();if(ingress!=null)ingress.close();}
}
