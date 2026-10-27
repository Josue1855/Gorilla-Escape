package com.gorillaescape.server.connectivity;

import com.gorillaescape.server.mobile.MobileRuntime;
import com.gorillaescape.server.mobile.MobileController;
import com.gorillaescape.server.protocol.GorillaProtocol;
import com.sun.net.httpserver.*;
import java.net.*;
import java.io.*;
import java.util.*;
import java.util.concurrent.*;

/** A separate loopback HTTP server. Does not forward requests to Spring or its private endpoints. */
public final class PublicIngress implements AutoCloseable {
    public static final int MAX_BODY=73728;
    // Signaling SDP has its own bounds; mobile sample JSON intentionally caps strings at512.
    private static final tools.jackson.databind.json.JsonMapper SIGNAL_JSON=tools.jackson.databind.json.JsonMapper.builder(tools.jackson.core.json.JsonFactory.builder().streamReadConstraints(tools.jackson.core.StreamReadConstraints.builder().maxNestingDepth(8).maxStringLength(65536).maxNumberLength(32).build()).build()).enable(tools.jackson.core.StreamReadFeature.STRICT_DUPLICATE_DETECTION).enable(tools.jackson.databind.DeserializationFeature.FAIL_ON_TRAILING_TOKENS).build();
    public static final Set<String> SIGNAL=Set.of("/mobile/join","/mobile/pin-admission","/mobile/reconnect","/mobile/disconnect","/mobile/profile");
    private static final Set<String> STATIC=Set.of("/","/index.html","/manifest.webmanifest","/sw.js","/icons/icon-192.png","/icons/icon-512.png");
    @FunctionalInterface public interface Signaling { Object apply(String path,byte[] body)throws Exception; }
    private final HttpServer server;
    private final ThreadPoolExecutor workers=new ThreadPoolExecutor(4,4,0,TimeUnit.SECONDS,new ArrayBlockingQueue<>(4),r->{var t=new Thread(r,"public-ingress");t.setDaemon(true);return t;},new ThreadPoolExecutor.AbortPolicy());
    private final ScheduledThreadPoolExecutor deadlines=new ScheduledThreadPoolExecutor(1,r->{var t=new Thread(r,"public-ingress-deadline");t.setDaemon(true);return t;});
    private final Set<HttpExchange> active=ConcurrentHashMap.newKeySet();
    private final Signaling signaling;
    private volatile URI publicOrigin;
    private long window=System.nanoTime();private int requests;
    public PublicIngress(Signaling signaling)throws IOException{
        this.signaling=signaling;deadlines.setRemoveOnCancelPolicy(true);
        // JVM-local server limits; never changes machine networking.
        System.setProperty("jdk.httpserver.maxConnections","32");System.setProperty("sun.net.httpserver.maxReqTime","12");System.setProperty("sun.net.httpserver.maxRspTime","12");
        server=HttpServer.create(new InetSocketAddress(InetAddress.getByName("127.0.0.1"),0),16);server.setExecutor(workers);server.createContext("/",this::handle);server.start();
    }
    public static PublicIngress forRuntime(MobileRuntime runtime)throws IOException{return new PublicIngress((path,body)->switch(path){
        case "/mobile/pin-admission" -> {
            var r=SIGNAL_JSON.readValue(body,MobileController.PinAdmissionRequest.class);
            var a=runtime.admissionByPin(r.pin());yield Map.of("sessionId",a.sessionId(),"admission",a.token(),"expiresInSeconds",30);
        }
        case "/mobile/join" -> runtime.join(SIGNAL_JSON.readValue(body,MobileRuntime.Signal.class));
        case "/mobile/reconnect" -> {var r=SIGNAL_JSON.readValue(body,MobileController.Resume.class);yield runtime.reconnect(r.sessionId(),r.playerId(),r.resumeToken(),r.offer());}
        case "/mobile/disconnect" -> {var r=SIGNAL_JSON.readValue(body,MobileController.Leave.class);runtime.disconnect(r.peerId(),r.resumeToken());yield Map.of("state","DISCONNECTED");}
        case "/mobile/profile" -> runtime.createProfile(SIGNAL_JSON.readValue(body,MobileRuntime.ProfileRequest.class));
        default -> throw new IOException("ROUTE_DENIED");
    });}
    public URI origin(){return URI.create("http://127.0.0.1:"+server.getAddress().getPort());}
    public void publicOrigin(URI origin){publicOrigin=origin;}
    public static boolean allowed(String method,String rawPath){
        if(rawPath==null||rawPath.length()>180||rawPath.contains("%")||rawPath.contains("..")||rawPath.contains("\\"))return false;
        return "POST".equals(method)?SIGNAL.contains(rawPath):"GET".equals(method)&&(STATIC.contains(rawPath)||rawPath.matches("/brand/[A-Za-z0-9_-]{1,100}\\.(png|ttf|txt)")||rawPath.matches("/assets/[A-Za-z0-9_-]{1,100}\\.(js|css)"));
    }
    private synchronized boolean budget(){long now=System.nanoTime();if(now-window>=1_000_000_000L){window=now;requests=0;}return ++requests<=60;}
    private void handle(HttpExchange x)throws IOException{
        active.add(x);var timeout=deadlines.schedule(x::close,12,TimeUnit.SECONDS);
        try(x){
            var h=x.getResponseHeaders();h.set("Cache-Control","no-store, max-age=0");h.set("Pragma","no-cache");h.set("Referrer-Policy","no-referrer");h.set("X-Content-Type-Options","nosniff");h.set("Content-Security-Policy","frame-ancestors 'none'; base-uri 'self'; object-src 'none'");
            String path=x.getRequestURI().getRawPath();
            if(x.getRequestURI().getRawQuery()!=null||!allowed(x.getRequestMethod(),path)){reply(x,404,Map.of("error","NOT_FOUND"));return;}
            if(!budget()){reply(x,429,Map.of("error","RATE_BOUND"));return;}
            if("POST".equals(x.getRequestMethod())){
                String origin=x.getRequestHeaders().getFirst("Origin");if(origin!=null&&(publicOrigin==null||!origin.equals(publicOrigin.toASCIIString()))){reply(x,403,Map.of("error","ORIGIN_DENIED"));return;}
                String content=x.getRequestHeaders().getFirst("Content-Type");if(content==null||!content.toLowerCase(Locale.ROOT).matches("application/json(?:;\\s*charset=utf-8)?")){reply(x,415,Map.of("error","CONTENT_TYPE"));return;}
                var body=x.getRequestBody().readNBytes(MAX_BODY+1);if(body.length>MAX_BODY){reply(x,413,Map.of("error","BODY_BOUND"));return;}
                try{reply(x,200,signaling.apply(path,body));}catch(MobileRuntime.JoinFailure e){reply(x,400,Map.of("error","JOIN_REJECTED","code",e.code()));}
                catch(Exception e){
                    String code="/mobile/pin-admission".equals(path)&&Set.of("PIN_INVALID","PIN_RATE_LIMITED","SESSION_FULL","SESSION_CLOSED","LOBBY_ADMISSION_CLOSED").contains(String.valueOf(e.getMessage()))?e.getMessage():"SIGNAL_FAILED_OTHER";
                    reply(x,"PIN_RATE_LIMITED".equals(code)?429:400,Map.of("error","SIGNAL_REJECTED","code",code));
                }finally{Arrays.fill(body,(byte)0);}
            }else{
                String resource="static"+(path.equals("/")?"/index.html":path);
                try(var in=PublicIngress.class.getClassLoader().getResourceAsStream(resource)){
                    if(in==null){reply(x,404,Map.of("error","NOT_FOUND"));return;}
                    byte[] data=in.readNBytes(2_000_001);if(data.length>2_000_000){reply(x,404,Map.of("error","NOT_FOUND"));return;}
                    h.set("Content-Type",path.endsWith(".js")?"text/javascript; charset=utf-8":path.endsWith(".css")?"text/css; charset=utf-8":path.endsWith(".png")?"image/png":path.endsWith(".ttf")?"font/ttf":path.endsWith(".txt")?"text/plain; charset=utf-8":path.endsWith(".webmanifest")?"application/manifest+json":"text/html; charset=utf-8");
                    x.sendResponseHeaders(200,data.length);x.getResponseBody().write(data);
                }
            }
        }finally{timeout.cancel(false);active.remove(x);}
    }
    private static void reply(HttpExchange x,int status,Object body)throws IOException{var data=GorillaProtocol.JSON.writeValueAsBytes(body);x.getResponseHeaders().set("Content-Type","application/json");x.sendResponseHeaders(status,data.length);x.getResponseBody().write(data);}
    @Override public void close(){active.forEach(HttpExchange::close);server.stop(0);workers.shutdownNow();deadlines.shutdownNow();}
    public boolean cleanupComplete(){return workers.isTerminated()&&deadlines.isTerminated()&&active.isEmpty();}
}
