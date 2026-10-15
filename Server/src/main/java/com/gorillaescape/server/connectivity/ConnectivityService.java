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
    private volatile ConnectionBootstrap bootstrap;private PublicIngress ingress;private long qrRevision=-1,observedRevision=-1;private boolean qrIssued;
    public ConnectivityService(MobileRuntime mobile,@Value("${gorilla.mobile.public-origin:}")String origin,@Value("${server.address}")String address,@Value("${gorilla.mobile.lab-enabled:false}")boolean lab){this.mobile=mobile;this.origin=origin;this.address=address;this.lab=lab;}
    @EventListener @Order(-100)
    public void ready(ApplicationReadyEvent event)throws Exception{
        int port=((WebServerApplicationContext)event.getApplicationContext()).getWebServer().getPort();
        URI cabin=lab?URI.create("http://127.0.0.1:"+port+"/mobile-lab/index.html"):MobileOrigin.page(origin,address,port);
        ConnectionBootstrap.Mode mode;try{mode=ConnectionBootstrap.Mode.valueOf(System.getenv().getOrDefault("GORILLA_CONNECTION_MODE","AUTO"));}catch(IllegalArgumentException e){throw new IllegalArgumentException("CONNECTION_MODE_INVALID");}
        long timeout;try{timeout=Long.parseLong(System.getenv().getOrDefault("GORILLA_TUNNEL_STARTUP_MS","6000"));}catch(NumberFormatException e){throw new IllegalArgumentException("BOOTSTRAP_BUDGET");}
        var binary=CloudflaredSupervisor.discover(System.getenv("GORILLA_CLOUDFLARED_PATH"),System.getenv("PATH"));
        // Missing optional executable requires neither a listener nor a download.
        if(binary!=null&&mode!=ConnectionBootstrap.Mode.CABIN)ingress=PublicIngress.forRuntime(mobile);
        bootstrap=new ConnectionBootstrap(mode,cabin,new CloudflaredSupervisor(binary),timeout);bootstrap.start(ingress==null?URI.create("http://127.0.0.1:9"):ingress.origin());
    }
    public synchronized Map<String,Object> snapshot(){if(bootstrap==null)return Map.of("bootstrapState","STOPPED");var result=bootstrap.snapshot();
        if(observedRevision!=bootstrap.revision()){if(observedRevision>=0)mobile.revokePendingAdmissions();observedRevision=bootstrap.revision();}
        if(ingress!=null){if(bootstrap.ready()&&"CLOUDFLARED".equals(result.get("resolvedMode")))ingress.publicOrigin(bootstrap.page().resolve("/"));else if(!"STARTING".equals(result.get("bootstrapState"))){ingress.close();ingress=null;}}
        return result;}
    public synchronized boolean request(String mode){
        ConnectionBootstrap.Mode requested;try{requested=ConnectionBootstrap.Mode.valueOf(mode);}catch(Exception e){return false;}
        if(bootstrap==null||!mobile.lobby().players().isEmpty())return false;
        if(ingress==null&&requested!=ConnectionBootstrap.Mode.CABIN)try{ingress=PublicIngress.forRuntime(mobile);}catch(Exception e){return false;}
        return bootstrap.request(requested,!mobile.lobby().players().isEmpty(),ingress==null?URI.create("http://127.0.0.1:9"):ingress.origin());
    }
    public synchronized com.gorillaescape.server.protocol.QaAdmission.View qa(boolean regenerate){
        snapshot();if(bootstrap==null||!bootstrap.ready())return new com.gorillaescape.server.protocol.QaAdmission.View("CONNECTION_NOT_READY",0,0,"");
        boolean rotated=qrIssued&&qrRevision!=bootstrap.revision();if(regenerate)qrIssued=true;qrRevision=bootstrap.revision();URI page=bootstrap.page();
        return mobile.qa(page,"http".equals(page.getScheme()),regenerate||rotated);
    }
    public synchronized URI page(){snapshot();if(bootstrap==null||!bootstrap.ready())throw new IllegalStateException("CONNECTION_NOT_READY");return bootstrap.page();}
    @Override public void destroy(){if(bootstrap!=null)bootstrap.close();if(ingress!=null)ingress.close();}
}
