package com.gorillaescape.server.connectivity;

import java.io.*;
import java.net.URI;
import java.nio.file.*;
import java.util.*;
import java.util.concurrent.*;
import java.util.regex.*;

/** Owns only explicitly launched children. Raw output never reaches application logs. */
public final class CloudflaredSupervisor implements Tunnel {
    private static final org.slf4j.Logger LOG=org.slf4j.LoggerFactory.getLogger(CloudflaredSupervisor.class);
    static final int HTTPS_TIMEOUT_MS=3000,HEALTH_FAILURE_LIMIT=4;
    private volatile int activeConnections;
    private volatile State state=State.STOPPED;
    private volatile Process child;
    private volatile Thread reader;
    private volatile boolean clean=true;
    private Path config;
    private String version="UNAVAILABLE";
    private volatile String failure="NONE",binarySha="NOT_CHECKED";
    @Override public String failureReason(){return failure;}
    public String binarySha(){return binarySha;}
    public static final String LINUX_AMD64_SHA256="d33ff2d14475178d2012c2c56beba87389ac5ded27649519f198a7d3134a99db";
    public static final String WINDOWS_AMD64_SHA256="86aee4017b26625cee8484c113558f48effa4cd47f7aa05fcf425604e5d2b23c";
    /** Retained for Linux package compatibility. */
    public static final String PINNED_SHA256=LINUX_AMD64_SHA256;
    private final Path binary;
    private final boolean verifyBinary;
    private volatile boolean registered,cancelled;
    private ScheduledExecutorService monitor;
    private int healthFailures;
    public static boolean verified(Path binary){
        try(var in=Files.newInputStream(binary)){
            var digest=java.security.MessageDigest.getInstance("SHA-256");
            byte[] buffer=new byte[65536];int n;while((n=in.read(buffer))!=-1)digest.update(buffer,0,n);
            String os=System.getProperty("os.name","").toLowerCase(Locale.ROOT);
            String expected=os.contains("win")?WINDOWS_AMD64_SHA256:LINUX_AMD64_SHA256;
            return expected.equals(java.util.HexFormat.of().formatHex(digest.digest()));
        }catch(Exception e){return false;}
    }
    @FunctionalInterface interface HealthProbe {boolean ready(URI origin,long remainingMs)throws Exception;}
    private final HealthProbe probe;
    private final java.util.function.Consumer<URI> onRegistered;
    CloudflaredSupervisor(Path binary,HealthProbe probe){this(binary,probe,false,null);}
    CloudflaredSupervisor(Path binary,HealthProbe probe,boolean verifyBinary){this(binary,probe,verifyBinary,null);}
    private CloudflaredSupervisor(Path binary,HealthProbe probe,boolean verifyBinary,java.util.function.Consumer<URI> onRegistered){this.binary=binary;this.probe=probe;this.verifyBinary=verifyBinary;this.onRegistered=onRegistered;}
    private static final String DEFAULT_NAMED_HOST="play.gorillas.run";
    private static final Pattern URL=Pattern.compile("(?<![A-Za-z0-9./_-])https://(?:[a-z0-9]+(?:-[a-z0-9]+)*\\.trycloudflare\\.com|(?:play\\.)?gorillas\\.run)(?=[\\s|]|$)");
    private volatile int healthAttempts,lastHttpsStatus;
    private volatile java.net.http.HttpClient httpClient;
    private volatile String probeFailure="NONE";
    public Map<String,Object> diagnostics(){return Map.of("binarySha",binarySha,"registered",registered,"activeConnections",activeConnections,"healthAttempts",healthAttempts,"lastHttpsStatus",lastHttpsStatus,"probeFailure",probeFailure,"failure",failure,"cleanupComplete",cleanupComplete());}
    public CloudflaredSupervisor(Path binary){this(binary,(java.util.function.Consumer<URI>)null);}
    public CloudflaredSupervisor(Path binary,java.util.function.Consumer<URI> onRegistered){this.binary=binary;this.verifyBinary=true;this.probe=this::httpReady;this.onRegistered=onRegistered;}
    private boolean httpReady(URI origin,long remainingMs)throws Exception{
        if(cancelled)throw new InterruptedException();
        var client=httpClient;
        if(client==null)httpClient=client=java.net.http.HttpClient.newBuilder().connectTimeout(java.time.Duration.ofMillis(HTTPS_TIMEOUT_MS)).build();
        long started=System.nanoTime();
        boolean quick=origin.getHost()!=null&&origin.getHost().endsWith(".trycloudflare.com");
        long directBudget=Math.max(1,Math.min(quick?1000:HTTPS_TIMEOUT_MS,remainingMs));
        var request=java.net.http.HttpRequest.newBuilder(origin.resolve("/")).timeout(java.time.Duration.ofMillis(directBudget)).GET().build();
        try {lastHttpsStatus=client.send(request,java.net.http.HttpResponse.BodyHandlers.discarding()).statusCode();return lastHttpsStatus==200;}
        catch(java.io.IOException failure){
            long left=remainingMs-TimeUnit.NANOSECONDS.toMillis(System.nanoTime()-started);
            if(left<=0||!quick)throw failure;
            boolean ready=QuickTunnelDnsProbe.ready(origin,left);
            if(ready)lastHttpsStatus=200;
            return ready;
        }
    }
    // Discovery never downloads. Every production execution independently verifies the pinned hash.
    public static Path discover(String explicit,String path){
        if(explicit!=null&&!explicit.isBlank())try{var p=Path.of(explicit);if(p.isAbsolute()&&Files.isRegularFile(p)&&Files.isExecutable(p))return p;}catch(InvalidPathException ignored){}
        String executable=System.getProperty("os.name","").toLowerCase(Locale.ROOT).contains("win")?"cloudflared.exe":"cloudflared";
        if(path!=null)for(String part:path.split(java.io.File.pathSeparator)){if(part.isBlank())continue;try{var p=Path.of(part,executable);if(p.isAbsolute()&&Files.isRegularFile(p)&&Files.isExecutable(p))return p;}catch(InvalidPathException ignored){}}
        return null;
    }
    public static URI validatePublicOrigin(String value){
        if(value==null||value.length()>253)throw new IllegalArgumentException("TUNNEL_ORIGIN_INVALID");
        boolean matchesQuick=value.matches("https://[a-z0-9]+(?:-[a-z0-9]+)*\\.trycloudflare\\.com");
        boolean matchesCustom=value.matches("https://(?:play\\.)?gorillas\\.run");
        String customDomain=System.getenv("GORILLA_TUNNEL_DOMAIN");
        if(customDomain!=null&&!customDomain.isBlank()&&value.equals("https://"+customDomain.trim()))matchesCustom=true;
        if(!matchesQuick&&!matchesCustom)throw new IllegalArgumentException("TUNNEL_ORIGIN_INVALID");
        var uri=URI.create(value);if(uri.getHost().length()>253||uri.getHost().split("\\.")[0].length()>63)throw new IllegalArgumentException("TUNNEL_ORIGIN_INVALID");return uri;
    }
    static URI parseOrigin(String line){if(line.length()>2048)return null;var m=URL.matcher(line);if(!m.find())return null;try{return validatePublicOrigin(m.group());}catch(IllegalArgumentException e){return null;}}
    @Override public URI start(URI ingress,long timeoutMs)throws Exception{
        if(state==State.STARTING||state==State.RUNNING)throw new IllegalStateException("TUNNEL_ALREADY_STARTED");
        if(!"http".equals(ingress.getScheme())||!"127.0.0.1".equals(ingress.getHost())||ingress.getPort()<1||!ingress.getRawPath().isEmpty()||ingress.getRawQuery()!=null||ingress.getRawFragment()!=null||ingress.getUserInfo()!=null)throw new IllegalArgumentException("TUNNEL_INGRESS_INVALID");
        synchronized(this){if(Thread.currentThread().isInterrupted())throw new InterruptedException("TUNNEL_CANCELLED");cancelled=false;state=State.STARTING;clean=false;}
        long deadline=System.nanoTime()+TimeUnit.MILLISECONDS.toNanos(timeoutMs);
        try{
            registered=false;activeConnections=healthFailures=healthAttempts=lastHttpsStatus=0;probeFailure="NONE";failure="NONE";binarySha="NOT_CHECKED";
            if(binary==null)throw new IOException("TUNNEL_UNAVAILABLE");
            if(verifyBinary){binarySha=verified(binary)?"PASS":"INVALID";if(!binarySha.equals("PASS"))throw new IOException("TUNNEL_SHA_INVALID");}
            launch(binary.toString(),"--version");
            if(!child.waitFor(Math.min(5000,timeoutMs),TimeUnit.MILLISECONDS))throw new IOException("TUNNEL_VERSION_TIMEOUT");
            byte[] output=child.getInputStream().readNBytes(257);String info=new String(output,java.nio.charset.StandardCharsets.US_ASCII).trim();
            if(child.exitValue()!=0||output.length>256||!info.matches("cloudflared version [0-9]{4}\\.[0-9]{1,2}\\.[0-9]{1,3}(?: \\(built [A-Za-z0-9: .+-]+\\))?"))throw new IOException("TUNNEL_VERSION_INVALID");
            version=info;child=null;
            long left=deadline-System.nanoTime();if(left<=0)throw new IOException("TUNNEL_TIMEOUT");

            // Check if a named tunnel credentials file is available (in production / verified binary or explicit opt-in)
            NamedTunnelSpec named=onRegistered==null&&System.getenv("GORILLA_TUNNEL_ID")!=null?resolveNamedTunnel(ingress):null;
            URI expectedOrigin=null;
            if(named!=null){
                config=named.configFile;
                expectedOrigin=named.origin;
                launch(binary.toString(),"tunnel","--config",config.toString(),"--no-autoupdate","--metrics","127.0.0.1:0","run",named.tunnelId);
            }else{
                config=Files.createTempFile("gorilla-quick-tunnel-",".yaml");Files.writeString(config,"# Isolated temporary QA tunnel; no account, token or named ingress.\n");
                launch(binary.toString(),"tunnel","--config",config.toString(),"--no-autoupdate","--url",ingress.toASCIIString(),"--metrics","127.0.0.1:0");
            }
            var ready=new CompletableFuture<URI>();Process owned=child;
            final URI defaultOrigin=expectedOrigin;
            reader=new Thread(()->read(owned,ready,defaultOrigin),"cloudflared-output");reader.setDaemon(true);reader.start();
            URI origin=ready.get(Math.max(1,deadline-System.nanoTime()),TimeUnit.NANOSECONDS);
            if(!owned.isAlive())throw new IOException("TUNNEL_DIED");
            boolean healthy=false;
            while(owned.isAlive()&&registered&&System.nanoTime()<deadline){
                long remaining=Math.max(1,TimeUnit.NANOSECONDS.toMillis(deadline-System.nanoTime()));
                try{healthAttempts++;healthy=probe.ready(origin,Math.min(HTTPS_TIMEOUT_MS,remaining));probeFailure="NONE";}catch(Exception early){probeFailure=describeFailure(early);healthy=false;}
                if(healthy)break;
                remaining=TimeUnit.NANOSECONDS.toMillis(deadline-System.nanoTime());
                if(remaining>0)Thread.sleep(Math.min(500,remaining));
            }
            if(!healthy||!registered||!owned.isAlive()||System.nanoTime()>deadline)throw new IOException("TUNNEL_HTTPS_UNAVAILABLE");
            synchronized(this){
                if(cancelled)throw new IOException("TUNNEL_CANCELLED");
                state=State.RUNNING;
                if(verifyBinary){
                    monitor=Executors.newSingleThreadScheduledExecutor(r->{var t=new Thread(r,"cloudflared-health");t.setDaemon(true);return t;});
                    monitor.scheduleWithFixedDelay(()->{
                        checkHealth(origin);
                    },3,3,TimeUnit.SECONDS);
                }
            }
            if(onRegistered!=null)onRegistered.accept(origin);
            return origin;
        }catch(Exception e){
            failure=(e instanceof IOException&&Set.of("TUNNEL_UNAVAILABLE","TUNNEL_SHA_INVALID","TUNNEL_VERSION_TIMEOUT","TUNNEL_VERSION_INVALID","TUNNEL_HTTPS_UNAVAILABLE","TUNNEL_TIMEOUT","TUNNEL_DIED","TUNNEL_CANCELLED").contains(e.getMessage()))
                ||(e instanceof IllegalStateException&&Set.of("ROOM_IN_USE","ROOM_REGISTRATION_REJECTED","ROOM_REGISTRATION_UNCONFIGURED","ROOM_TUNNEL_INVALID","ROOM_ROUTE_UNAVAILABLE","ROOM_REGISTRATION_FAILED").contains(e.getMessage())) ? e.getMessage():"TUNNEL_START_FAILED";
            LOG.warn("Cloudflared startup failed: reason={} attempts={} httpStatus={} cause={} activeConnections={}",
                failure,healthAttempts,lastHttpsStatus,probeFailure,activeConnections);
            close();state=State.FAILED;throw new IOException(failure);
        }
    }
    // Log exception types, including the root cause, never arbitrary exception text or admission URLs.
    static String describeFailure(Throwable error){
        StringBuilder types=new StringBuilder();
        for(int i=0;error!=null&&i<4;i++,error=error.getCause()){
            if(i>0)types.append(" <- ");types.append(error.getClass().getSimpleName());
        }
        return types.toString();
    }
    synchronized void registrationLost(){
        if(state==State.RUNNING){failure="ROOM_REGISTRATION_LOST";state=State.FAILED;}
    }
    void checkHealth(URI origin){
        if(state!=State.RUNNING)return;
        boolean ok=false;try{healthAttempts++;ok=probe.ready(origin,HTTPS_TIMEOUT_MS);probeFailure="NONE";}
        catch(Exception error){probeFailure=describeFailure(error);}
        healthFailures=ok?0:healthFailures+1;
        if(healthFailures>=HEALTH_FAILURE_LIMIT){
            failure="TUNNEL_HTTPS_UNAVAILABLE";state=State.FAILED;
            LOG.warn("Cloudflared health failed: consecutiveFailures={} attempts={} httpStatus={} cause={}",
                healthFailures,healthAttempts,lastHttpsStatus,probeFailure);
        }
    }
    // Cloudflared multiplexes connections. Duplicate registration lines must not inflate the count.
    static final class RegistrationTracker {
        private static final Pattern INDEX=Pattern.compile("\\bconnIndex=([0-9]{1,2})\\b");
        private final Set<String> connections=new HashSet<>();
        void observe(String text){
            var match=INDEX.matcher(text);String index=match.find()?match.group(1):"unindexed";
            if(text.contains("Unregistered tunnel connection"))connections.remove(index);
            else if(text.contains("Registered tunnel connection"))connections.add(index);
            else if(text.contains("Serve tunnel error")||text.contains("failed to serve tunnel connection"))connections.remove(index);
        }
        int size(){return connections.size();}
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
    record NamedTunnelSpec(String tunnelId,Path configFile,URI origin){}
    private static NamedTunnelSpec resolveNamedTunnel(URI ingress){
        try{
            String tunnelId=System.getenv("GORILLA_TUNNEL_ID");
            String credPath=System.getenv("GORILLA_TUNNEL_CREDENTIALS");
            Path credFile=credPath!=null&&!credPath.isBlank()?Path.of(credPath):null;
            if(credFile==null||!Files.isRegularFile(credFile)){
                Path defaultCloudflaredDir=Path.of(System.getProperty("user.home"),".cloudflared");
                if(Files.isDirectory(defaultCloudflaredDir)){
                    if(tunnelId!=null&&!tunnelId.isBlank()){
                        Path candidate=defaultCloudflaredDir.resolve(tunnelId+".json");
                        if(Files.isRegularFile(candidate))credFile=candidate;
                    }else{
                        try(var stream=Files.list(defaultCloudflaredDir)){
                            var list=stream.filter(p->p.getFileName().toString().endsWith(".json")).toList();
                            if(!list.isEmpty()){
                                credFile=list.get(0);
                                String fn=credFile.getFileName().toString();
                                tunnelId=fn.substring(0,fn.length()-5);
                            }
                        }
                    }
                }
            }
            if(credFile==null||tunnelId==null||tunnelId.isBlank()||!Files.isRegularFile(credFile))return null;
            String domain=System.getenv("GORILLA_TUNNEL_DOMAIN");
            String hostname=domain!=null&&!domain.isBlank()?domain.trim():DEFAULT_NAMED_HOST;
            URI origin=validatePublicOrigin("https://"+hostname);
            Path tempConfig=Files.createTempFile("gorilla-named-tunnel-",".yaml");
            StringBuilder yaml=new StringBuilder();
            yaml.append("tunnel: ").append(tunnelId).append("\n");
            yaml.append("credentials-file: ").append(credFile.toAbsolutePath()).append("\n");
            yaml.append("ingress:\n");
            yaml.append("  - hostname: ").append(hostname).append("\n");
            yaml.append("    service: ").append(ingress.toASCIIString()).append("\n");
            if(hostname.startsWith("play.")){
                String rootDomain=hostname.substring(5);
                yaml.append("  - hostname: ").append(rootDomain).append("\n");
                yaml.append("    service: ").append(ingress.toASCIIString()).append("\n");
            }
            yaml.append("  - service: http_status:404\n");
            Files.writeString(tempConfig,yaml.toString());
            return new NamedTunnelSpec(tunnelId,tempConfig,origin);
        }catch(Exception e){
            LOG.warn("Failed resolving named tunnel spec: {}",e.getMessage());
            return null;
        }
    }
    private void read(Process owned,CompletableFuture<URI> ready,URI defaultOrigin){
        URI origin=defaultOrigin;var registrations=new RegistrationTracker();
        try(var input=new InputStreamReader(owned.getInputStream(),java.nio.charset.StandardCharsets.UTF_8)){
            var line=new StringBuilder();boolean oversized=false;int c;
            while((c=input.read())!=-1){
                if(c!='\n'){if(line.length()<2048&&!oversized)line.append((char)c);else{oversized=true;line.setLength(0);}continue;}
                if(!oversized){String text=line.toString();URI parsed=parseOrigin(text);if(parsed!=null)origin=parsed;
                    registrations.observe(text);activeConnections=registrations.size();registered=activeConnections>0;
                    if(!registered&&state==State.RUNNING)state=State.FAILED;
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
