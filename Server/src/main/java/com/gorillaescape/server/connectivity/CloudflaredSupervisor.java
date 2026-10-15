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
    private final Path binary;
    @FunctionalInterface interface HealthProbe {boolean ready(URI origin,long remainingMs)throws Exception;}
    private final HealthProbe probe;
    CloudflaredSupervisor(Path binary,HealthProbe probe){this.binary=binary;this.probe=probe;}
    private static final Pattern URL=Pattern.compile("(?<![A-Za-z0-9./_-])https://[a-z0-9]+(?:-[a-z0-9]+)*\\.trycloudflare\\.com(?=[\\s|]|$)");
    public CloudflaredSupervisor(Path binary){this(binary,(origin,remainingMs)->{
        var client=java.net.http.HttpClient.newBuilder().connectTimeout(java.time.Duration.ofMillis(Math.max(1,Math.min(1500,remainingMs)))).build();
        var request=java.net.http.HttpRequest.newBuilder(origin.resolve("/")).timeout(java.time.Duration.ofMillis(Math.max(1,remainingMs))).GET().build();
        return client.send(request,java.net.http.HttpResponse.BodyHandlers.discarding()).statusCode()==200;
    });}
    // Packaged binaries are intentionally not trusted until a pinned provenance policy is supplied.
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
    @Override public synchronized URI start(URI ingress,long timeoutMs)throws Exception{
        if(state==State.STARTING||state==State.RUNNING)throw new IllegalStateException("TUNNEL_ALREADY_STARTED");
        if(!"http".equals(ingress.getScheme())||!"127.0.0.1".equals(ingress.getHost())||ingress.getPort()<1||!ingress.getRawPath().isEmpty()||ingress.getRawQuery()!=null||ingress.getRawFragment()!=null||ingress.getUserInfo()!=null)throw new IllegalArgumentException("TUNNEL_INGRESS_INVALID");
        state=State.STARTING;clean=false;long deadline=System.nanoTime()+TimeUnit.MILLISECONDS.toNanos(timeoutMs);
        try{
            if(binary==null)throw new IOException("TUNNEL_UNAVAILABLE");
            child=command(binary.toString(),"--version").start();
            if(!child.waitFor(Math.min(500,timeoutMs),TimeUnit.MILLISECONDS))throw new IOException("TUNNEL_VERSION_TIMEOUT");
            byte[] output=child.getInputStream().readNBytes(257);String info=new String(output,java.nio.charset.StandardCharsets.US_ASCII).trim();
            if(child.exitValue()!=0||output.length>256||!info.matches("cloudflared version [0-9]{4}\\.[0-9]{1,2}\\.[0-9]{1,3}(?: \\(built [A-Za-z0-9: .+-]+\\))?"))throw new IOException("TUNNEL_VERSION_INVALID");
            version=info;child=null;
            long left=deadline-System.nanoTime();if(left<=0)throw new IOException("TUNNEL_TIMEOUT");
            config=Files.createTempFile("gorilla-quick-tunnel-",".yaml");Files.writeString(config,"# Isolated temporary QA tunnel; no account, token or named ingress.\n");
            child=command(binary.toString(),"tunnel","--config",config.toString(),"--no-autoupdate","--url",ingress.toASCIIString(),"--protocol","http2","--metrics","127.0.0.1:0").start();
            var ready=new CompletableFuture<URI>();Process owned=child;
            reader=new Thread(()->read(owned,ready),"cloudflared-output");reader.setDaemon(true);reader.start();
            URI origin=ready.get(Math.max(1,deadline-System.nanoTime()),TimeUnit.NANOSECONDS);
            if(!owned.isAlive())throw new IOException("TUNNEL_DIED");
            if(!probe.ready(origin,Math.max(1,TimeUnit.NANOSECONDS.toMillis(deadline-System.nanoTime()))))throw new IOException("TUNNEL_HTTPS_UNAVAILABLE");
            if(!owned.isAlive()||System.nanoTime()>deadline)throw new IOException("TUNNEL_TIMEOUT");state=State.RUNNING;return origin;
        }catch(Exception e){close();state=State.FAILED;throw new IOException(binary==null?"TUNNEL_UNAVAILABLE":"TUNNEL_START_FAILED");}
    }
    private static ProcessBuilder command(String... arguments){
        var builder=new ProcessBuilder(arguments).redirectErrorStream(true);
        builder.environment().keySet().removeIf(k->k.startsWith("GORILLA_")||k.startsWith("TUNNEL_")||k.startsWith("CLOUDFLARE_")||k.startsWith("CF_"));
        return builder;
    }
    private void read(Process owned,CompletableFuture<URI> ready){
        URI origin=null;boolean registered=false;
        try(var input=new InputStreamReader(owned.getInputStream(),java.nio.charset.StandardCharsets.UTF_8)){
            var line=new StringBuilder();boolean oversized=false;int c;
            while((c=input.read())!=-1){
                if(c!='\n'){if(line.length()<2048&&!oversized)line.append((char)c);else{oversized=true;line.setLength(0);}continue;}
                if(!oversized){String text=line.toString();URI parsed=parseOrigin(text);if(parsed!=null)origin=parsed;
                    if(text.contains("Registered tunnel connection"))registered=true;
                    if(state==State.RUNNING&&(text.contains("Unregistered tunnel connection")||text.contains("Serve tunnel error")||text.contains("failed to serve tunnel connection")))state=State.FAILED;
                    if(origin!=null&&registered)ready.complete(origin);
                }line.setLength(0);oversized=false;
            }
        }catch(IOException ignored){}finally{ready.completeExceptionally(new IOException("TUNNEL_DIED"));if(state==State.RUNNING)state=State.FAILED;}
    }
    @Override public State state(){if(state==State.RUNNING&&(child==null||!child.isAlive()))state=State.FAILED;return state;}
    @Override public String version(){return version;}
    @Override public boolean cleanupComplete(){return clean&&(child==null||!child.isAlive())&&(reader==null||!reader.isAlive());}
    @Override public synchronized void close(){
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
