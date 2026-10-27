package com.gorillaescape.server.connectivity;

import java.io.*;
import java.net.URI;
import java.nio.file.*;
import java.util.*;
import java.util.concurrent.*;
import java.util.regex.*;

/** Owns only explicitly launched children. Raw output never reaches application logs. */
public final class CloudflaredSupervisor implements Tunnel {
    private volatile State state=State.STOPPED;
    private volatile Process child;
    private volatile Thread reader;
    private volatile boolean clean=true;
    private Path config;
    private String version="UNAVAILABLE";
    private volatile String failure="NONE",binarySha="NOT_CHECKED";
    @Override public String failureReason(){return failure;}
    public String binarySha(){return binarySha;}
    public static final String PINNED_SHA256="d33ff2d14475178d2012c2c56beba87389ac5ded27649519f198a7d3134a99db";
    private final Path binary;
    private final boolean verifyBinary;
    private volatile boolean registered,cancelled;
    private volatile int activeConnections;
    private ScheduledExecutorService monitor;
    private int healthFailures;
    public static boolean verified(Path binary){
        try(var in=Files.newInputStream(binary)){
            var digest=java.security.MessageDigest.getInstance("SHA-256");
            byte[] buffer=new byte[65536];int n;while((n=in.read(buffer))!=-1)digest.update(buffer,0,n);
            return PINNED_SHA256.equals(java.util.HexFormat.of().formatHex(digest.digest()));
        }catch(Exception e){return false;}
    }
    @FunctionalInterface interface HealthProbe {boolean ready(URI origin,long remainingMs)throws Exception;}
    private final HealthProbe probe;
    // Package-private injection is exclusively for deterministic process fixtures.
    CloudflaredSupervisor(Path binary,HealthProbe probe){this(binary,probe,false);}
    private CloudflaredSupervisor(Path binary,HealthProbe probe,boolean verifyBinary){this.binary=binary;this.probe=probe;this.verifyBinary=verifyBinary;}
    private static final Pattern URL=Pattern.compile("(?<![A-Za-z0-9./_-])https://[a-z0-9]+(?:-[a-z0-9]+)*\\.trycloudflare\\.com(?=[\\s|]|$)");
    private volatile int healthAttempts,lastHttpsStatus;
    private volatile java.net.http.HttpClient httpClient;
    private volatile String probeFailure="NONE";
    public Map<String,Object> diagnostics(){return Map.of("binarySha",binarySha,"registered",registered,"healthAttempts",healthAttempts,"lastHttpsStatus",lastHttpsStatus,"probeFailure",probeFailure,"failure",failure,"cleanupComplete",cleanupComplete());}
    public CloudflaredSupervisor(Path binary){this.binary=binary;this.verifyBinary=true;this.probe=this::httpReady;}
    private boolean httpReady(URI origin,long remainingMs)throws Exception{
        if(cancelled)throw new InterruptedException();
        var client=httpClient;
        if(client==null)httpClient=client=java.net.http.HttpClient.newBuilder().connectTimeout(java.time.Duration.ofMillis(3000)).build();
        var request=java.net.http.HttpRequest.newBuilder(origin.resolve("/")).timeout(java.time.Duration.ofMillis(Math.max(1,Math.min(3000,remainingMs)))).GET().build();
        lastHttpsStatus=client.send(request,java.net.http.HttpResponse.BodyHandlers.discarding()).statusCode();return lastHttpsStatus==200;
    }
    // Discovery never downloads. Every production execution independently verifies the pinned hash.
    public static Path discover(String explicit,String path){
        if(explicit!=null&&!explicit.isBlank()){var p=Path.of(explicit);if(p.isAbsolute()&&Files.isRegularFile(p)&&Files.isExecutable(p))return p;}
        if(path!=null)for(String part:path.split(java.io.File.pathSeparator)){if(part.isBlank())continue;var p=Path.of(part,"cloudflared");if(p.isAbsolute()&&Files.isRegularFile(p)&&Files.isExecutable(p))return p;}
        return null;
    }
    public static URI validatePublicOrigin(String value){
        if(value==null||value.length()>253||!value.matches("https://[a-z0-9]+(?:-[a-z0-9]+)*\\.trycloudflare\\.com"))throw new IllegalArgumentException("TUNNEL_ORIGIN_INVALID");
        var uri=URI.create(value);if(uri.getHost().length()>253||uri.getHost().split("\\.")[0].length()>63)throw new IllegalArgumentException("TUNNEL_ORIGIN_INVALID");return uri;
    }
    static URI parseOrigin(String line){if(line.length()>2048)return null;var m=URL.matcher(line);if(!m.find())return null;try{return validatePublicOrigin(m.group());}catch(IllegalArgumentException e){return null;}}
    @Override public URI start(URI ingress,long timeoutMs)throws Exception{
        if(state==State.STARTING||state==State.RUNNING)throw new IllegalStateException("TUNNEL_ALREADY_STARTED");
        if(!"http".equals(ingress.getScheme())||!"127.0.0.1".equals(ingress.getHost())||ingress.getPort()<1||!ingress.getRawPath().isEmpty()||ingress.getRawQuery()!=null||ingress.getRawFragment()!=null||ingress.getUserInfo()!=null)throw new IllegalArgumentException("TUNNEL_INGRESS_INVALID");
        synchronized(this){cancelled=false;state=State.STARTING;clean=false;}
        long deadline=System.nanoTime()+TimeUnit.MILLISECONDS.toNanos(timeoutMs);
        try{
            if(binary==null)throw new IOException("TUNNEL_UNAVAILABLE");
            if(verifyBinary){binarySha=verified(binary)?"PASS":"INVALID";if(!binarySha.equals("PASS"))throw new IOException("TUNNEL_SHA_INVALID");}
            registered=false;
            launch(binary.toString(),"--version");
            if(!child.waitFor(Math.min(500,timeoutMs),TimeUnit.MILLISECONDS))throw new IOException("TUNNEL_VERSION_TIMEOUT");
            byte[] output=child.getInputStream().readNBytes(257);String info=new String(output,java.nio.charset.StandardCharsets.US_ASCII).trim();
            if(child.exitValue()!=0||output.length>256||!info.matches("cloudflared version [0-9]{4}\\.[0-9]{1,2}\\.[0-9]{1,3}(?: \\(built [A-Za-z0-9: .+-]+\\))?"))throw new IOException("TUNNEL_VERSION_INVALID");
            version=info;child=null;
            long left=deadline-System.nanoTime();if(left<=0)throw new IOException("TUNNEL_TIMEOUT");
            config=Files.createTempFile("gorilla-quick-tunnel-",".yaml");Files.writeString(config,"# Isolated temporary QA tunnel; no account, token or named ingress.\n");
            launch(binary.toString(),"tunnel","--config",config.toString(),"--no-autoupdate","--url",ingress.toASCIIString(),"--metrics","127.0.0.1:0");
            var ready=new CompletableFuture<URI>();Process owned=child;
            reader=new Thread(()->read(owned,ready),"cloudflared-output");reader.setDaemon(true);reader.start();
            URI origin=ready.get(Math.max(1,deadline-System.nanoTime()),TimeUnit.NANOSECONDS);
            if(!owned.isAlive())throw new IOException("TUNNEL_DIED");
            boolean healthy=false;
            while(owned.isAlive()&&registered&&System.nanoTime()<deadline){
                long remaining=Math.max(1,TimeUnit.NANOSECONDS.toMillis(deadline-System.nanoTime()));
                try{healthAttempts++;healthy=probe.ready(origin,Math.min(3000,remaining));}catch(Exception early){probeFailure=early.getClass().getSimpleName()+":"+early.getMessage();healthy=false;}
                if(healthy)break;
                remaining=TimeUnit.NANOSECONDS.toMillis(deadline-System.nanoTime());
                if(remaining>0)Thread.sleep(Math.min(500,remaining));
            }
            if(!healthy||!registered||!owned.isAlive()||System.nanoTime()>deadline){
                System.err.println("[TUNNEL_FAIL] origin="+origin+" healthy="+healthy+" registered="+registered+" alive="+owned.isAlive()+" attempts="+healthAttempts+" lastStatus="+lastHttpsStatus+" probeFailure="+probeFailure);
                throw new IOException("TUNNEL_HTTPS_UNAVAILABLE");
            }
            synchronized(this){
                if(cancelled)throw new IOException("TUNNEL_CANCELLED");
                state=State.RUNNING;
                if(verifyBinary){
                    monitor=Executors.newSingleThreadScheduledExecutor(r->{var t=new Thread(r,"cloudflared-health");t.setDaemon(true);return t;});
                    monitor.scheduleWithFixedDelay(()->{
                        if(state!=State.RUNNING)return;
                        boolean ok=false;try{ok=probe.ready(origin,3000);}catch(Exception ignored){}
                        healthFailures=ok?0:healthFailures+1;if(healthFailures>=4)state=State.FAILED;
                    },3,3,TimeUnit.SECONDS);
                }
            }
            return origin;
        }catch(Exception e){
            failure=e instanceof IOException&&Set.of("TUNNEL_UNAVAILABLE","TUNNEL_SHA_INVALID","TUNNEL_VERSION_TIMEOUT","TUNNEL_VERSION_INVALID","TUNNEL_HTTPS_UNAVAILABLE","TUNNEL_TIMEOUT","TUNNEL_DIED","TUNNEL_CANCELLED").contains(e.getMessage())?e.getMessage():"TUNNEL_START_FAILED";
            close();state=State.FAILED;throw new IOException(failure);
        }
    }
    private synchronized void launch(String... arguments)throws IOException{
        if(cancelled)throw new IOException("TUNNEL_CANCELLED");
        child=command(arguments).start();
    }
    private static ProcessBuilder command(String... arguments){
        var builder=new ProcessBuilder(arguments).redirectErrorStream(true);
        builder.environment().keySet().removeIf(k->k.startsWith("GORILLA_")||k.startsWith("TUNNEL_")||k.startsWith("CLOUDFLARE_")||k.startsWith("CF_"));
        return builder;
    }
    private void read(Process owned,CompletableFuture<URI> ready){
        URI origin=null;
        try(var input=new InputStreamReader(owned.getInputStream(),java.nio.charset.StandardCharsets.UTF_8)){
            var line=new StringBuilder();boolean oversized=false;int c;
            while((c=input.read())!=-1){
                if(c!='\n'){if(line.length()<2048&&!oversized)line.append((char)c);else{oversized=true;line.setLength(0);}continue;}
                if(!oversized){String text=line.toString();URI parsed=parseOrigin(text);if(parsed!=null)origin=parsed;
                    if(text.contains("Registered tunnel connection")){activeConnections++;registered=true;}
                    if(text.contains("Unregistered tunnel connection")){activeConnections=Math.max(0,activeConnections-1);if(activeConnections==0&&!owned.isAlive()){registered=false;if(state==State.RUNNING)state=State.FAILED;}}
                    if(origin!=null&&registered)ready.complete(origin);
                }line.setLength(0);oversized=false;
            }
        }catch(IOException ignored){}finally{ready.completeExceptionally(new IOException("TUNNEL_DIED"));if(state==State.RUNNING)state=State.FAILED;}
    }
    @Override public State state(){if(state==State.RUNNING&&(child==null||!child.isAlive()))state=State.FAILED;return state;}
    @Override public String version(){return version;}
    @Override public boolean cleanupComplete(){return clean&&(child==null||!child.isAlive())&&(reader==null||!reader.isAlive())&&(monitor==null||monitor.isTerminated());}
    @Override public synchronized void close(){
        cancelled=true;
        if(monitor!=null){monitor.shutdownNow();try{monitor.awaitTermination(500,TimeUnit.MILLISECONDS);}catch(InterruptedException e){Thread.currentThread().interrupt();}}
        if(httpClient!=null){httpClient.shutdownNow();httpClient=null;}
        if(state==State.STOPPED&&cleanupComplete())return;state=State.STOPPING;var owned=child;
        if(owned!=null){var descendants=owned.descendants().toList();descendants.forEach(ProcessHandle::destroy);owned.destroy();
            try{if(!owned.waitFor(500,TimeUnit.MILLISECONDS)){descendants.forEach(ProcessHandle::destroyForcibly);owned.destroyForcibly();owned.waitFor(500,TimeUnit.MILLISECONDS);}owned.getInputStream().close();}
            catch(InterruptedException e){Thread.currentThread().interrupt();owned.destroyForcibly();}catch(IOException ignored){}
            clean=!owned.isAlive()&&descendants.stream().noneMatch(ProcessHandle::isAlive);
        }else clean=true;
        if(reader!=null&&reader!=Thread.currentThread())try{reader.join(500);}catch(InterruptedException e){Thread.currentThread().interrupt();}
        if(config!=null)try{Files.deleteIfExists(config);config=null;}catch(IOException e){clean=false;}
        clean=clean&&(reader==null||!reader.isAlive());state=clean?State.STOPPED:State.FAILED;
    }
}
