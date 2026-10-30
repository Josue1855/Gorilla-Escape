package com.gorillaescape.server.mobile;

import org.springframework.stereotype.Component;
import org.springframework.beans.factory.DisposableBean;
import com.gorillaescape.server.protocol.GorillaProtocol;
import java.nio.file.*;
import java.net.*;
import java.net.http.*;
import java.time.Duration;
import java.io.*;
import java.util.Map;
import java.util.concurrent.*;

/** Optional owned persistence child. Fixed loopback endpoints, bounded startup and no write retries. */
@Component
public final class PlayersBackend implements DisposableBean {
    private static final org.slf4j.Logger LOG=org.slf4j.LoggerFactory.getLogger(PlayersBackend.class);
    private volatile Process process;
    private volatile int port;
    private volatile boolean closing,healthy;
    private final HttpClient http=HttpClient.newBuilder().connectTimeout(Duration.ofSeconds(2)).build();
    private final ExecutorService reader=Executors.newSingleThreadExecutor(r->{var t=new Thread(r,"players-backend-output");t.setDaemon(true);return t;});
    private final ScheduledExecutorService deadline=Executors.newSingleThreadScheduledExecutor(r->{var t=new Thread(r,"players-backend-deadline");t.setDaemon(true);return t;});
    public PlayersBackend(){this(packageJar(),null);}
    private static Path packageJar(){String root=System.getenv("GORILLA_PACKAGE_ROOT");return root==null?null:Path.of(root,"Java","backend.jar");}
    PlayersBackend(Path jar,Path data){if(jar!=null&&Files.isRegularFile(jar))reader.submit(()->start(jar,data));}
    boolean ready(){return !closing&&healthy&&port>0&&process!=null&&process.isAlive();}
    boolean cleanupComplete(){return process==null||!process.isAlive();}
    private void start(Path jar,Path data){
        try{
            var javaExecutable=Path.of(System.getProperty("java.home"),"bin",System.getProperty("os.name").startsWith("Windows")?"java.exe":"java");
            var builder=new ProcessBuilder(javaExecutable.toString(),"-jar",jar.toAbsolutePath().toString(),"--server.address=127.0.0.1","--server.port=0","--server.ssl.enabled=false","--gorilla.http.allowed-origins=");
            String storage=System.getenv("GORILLA_DATA_DIR");
            builder.environment().keySet().removeIf(k->k.startsWith("GORILLA_")||k.startsWith("CLOUDFLARE_")||k.startsWith("TUNNEL_"));
            builder.environment().put("GORILLA_DATA_DIR",data!=null?data.toAbsolutePath().toString():storage!=null&&!storage.isBlank()?storage:Path.of(System.getProperty("user.home"),".gorilla-escape","data").toString());
            builder.redirectErrorStream(true);
            synchronized(this){if(closing||Thread.currentThread().isInterrupted())return;process=builder.start();}
            var owned=process;
            var timeout=deadline.schedule(()->{if(!healthy&&!closing){LOG.warn("event=players_backend_startup_timeout");owned.destroy();deadline.schedule(()->{if(owned.isAlive())owned.destroyForcibly();},1,TimeUnit.SECONDS);}},30,TimeUnit.SECONDS);
            var pattern=java.util.regex.Pattern.compile("Tomcat started on port ([0-9]+)");
            try(var output=owned.inputReader()){
                // A bounded line buffer avoids forwarding names, paths or inherited credentials.
                var line=new StringBuilder();boolean oversized=false;int c;
                while((c=output.read())!=-1){
                    if(c!='\n'){if(line.length()<4096&&!oversized)line.append((char)c);else{line.setLength(0);oversized=true;}continue;}
                    if(!oversized&&!healthy&&!closing){
                        var match=pattern.matcher(line);
                        if(match.find()){
                            int candidate=Integer.parseInt(match.group(1));
                            if(candidate>0&&candidate<=65535){
                                var check=HttpRequest.newBuilder(URI.create("http://127.0.0.1:"+candidate+"/api/players?limit=1&offset=0")).timeout(Duration.ofSeconds(2)).GET().build();
                                if(http.send(check,HttpResponse.BodyHandlers.discarding()).statusCode()==200){port=candidate;healthy=true;timeout.cancel(false);LOG.info("event=players_backend_ready");}
                            }
                        }
                    }
                    line.setLength(0);oversized=false;
                }
            }finally{timeout.cancel(false);healthy=false;port=0;}
        }catch(Exception error){healthy=false;port=0;if(!closing)LOG.warn("event=players_backend_unavailable cause={}",error.getClass().getSimpleName());var owned=process;if(owned!=null)owned.destroyForcibly();}
    }
    public Map<String,Object> create(String name)throws Exception{
        if(!ready())throw new IOException("PLAYERS_UNAVAILABLE");
        var payload=GorillaProtocol.JSON.writeValueAsString(Map.of("displayName",name));
        var request=HttpRequest.newBuilder(URI.create("http://127.0.0.1:"+port+"/api/players"))
            .timeout(Duration.ofSeconds(5)).header("Content-Type","application/json").POST(HttpRequest.BodyPublishers.ofString(payload)).build();
        // One attempt only: uncertain commit must not create a second player.
        var response=http.send(request,HttpResponse.BodyHandlers.ofInputStream());
        byte[] bytes;try(var body=response.body()){bytes=body.readNBytes(2049);}
        if(response.statusCode()!=201||bytes.length>2048)throw new IOException("PROFILE_NOT_CONFIRMED");
        var body=GorillaProtocol.JSON.readTree(bytes);
        var id=java.util.UUID.fromString(body.path("id").asString()).toString();
        if(!name.equals(body.path("displayName").asString()))throw new IOException("PROFILE_NOT_CONFIRMED");
        return Map.of("id",id,"displayName",body.path("displayName").asString());
    }
    @Override public void destroy()throws Exception{
        synchronized(this){closing=true;healthy=false;port=0;}
        deadline.shutdownNow();
        reader.shutdownNow();http.shutdownNow();
        var owned=process;
        if(owned!=null){
            var descendants=owned.descendants().toList();descendants.forEach(ProcessHandle::destroy);owned.destroy();
            if(!owned.waitFor(5,TimeUnit.SECONDS)){descendants.forEach(ProcessHandle::destroyForcibly);owned.destroyForcibly();owned.waitFor(1,TimeUnit.SECONDS);}
            if(owned.isAlive()||descendants.stream().anyMatch(ProcessHandle::isAlive))throw new IOException("PLAYERS_CLEANUP_INCOMPLETE");
        }
        reader.awaitTermination(1,TimeUnit.SECONDS);
    }
}
