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

/** Owns one optional loopback-only persistence process. No arbitrary URL/proxy and no POST retry. */
@Component
public final class PlayersBackend implements DisposableBean {
    private volatile Process process;
    private volatile int port;
    private final HttpClient http=HttpClient.newBuilder().connectTimeout(Duration.ofSeconds(2)).build();
    private final ExecutorService worker=Executors.newSingleThreadExecutor(r->{var t=new Thread(r,"players-backend");t.setDaemon(true);return t;});
    private volatile boolean closing;
    public PlayersBackend(){this(packageJar(),null);}
    private static Path packageJar(){String root=System.getenv("GORILLA_PACKAGE_ROOT");return root==null?null:Path.of(root,"Java","backend.jar");}
    PlayersBackend(Path jar,Path data){if(jar!=null&&Files.isRegularFile(jar))worker.submit(()->start(jar,data));}
    boolean ready(){return port>0&&process!=null&&process.isAlive();}
    private void start(Path jar,Path data){
        try {
            var javaExecutable=Path.of(System.getProperty("java.home"),"bin",System.getProperty("os.name").startsWith("Windows")?"java.exe":"java");
            var builder=new ProcessBuilder(javaExecutable.toString(),"-jar",jar.toString(),"--server.address=127.0.0.1","--server.port=0");
            builder.environment().putIfAbsent("GORILLA_DATA_DIR",Path.of(System.getProperty("user.home"),".gorilla-escape","data").toString());
            if(data!=null)builder.environment().put("GORILLA_DATA_DIR",data.toString());
            builder.redirectErrorStream(true);
            synchronized(this){if(closing)return;process=builder.start();}
            var pattern=java.util.regex.Pattern.compile("Tomcat started on port ([0-9]+)");
            try(var reader=process.inputReader()){
                String line;while((line=reader.readLine())!=null){
                    // Startup logging is consumed, never forwarded with names or filesystem paths.
                    if(line.length()>4096)continue;
                    var match=pattern.matcher(line);if(match.find())port=Integer.parseInt(match.group(1));
                }
            }finally{port=0;}
        }catch(Exception e){port=0;}
    }
    public Map<String,Object> create(String name)throws Exception {
        int target=port;
        if(target<1||process==null||!process.isAlive())throw new IOException("PLAYERS_UNAVAILABLE");
        var payload=GorillaProtocol.JSON.writeValueAsString(Map.of("displayName",name));
        var request=HttpRequest.newBuilder(URI.create("http://127.0.0.1:"+target+"/api/players"))
            .timeout(Duration.ofSeconds(5)).header("Content-Type","application/json").POST(HttpRequest.BodyPublishers.ofString(payload)).build();
        var response=http.send(request,HttpResponse.BodyHandlers.ofString());
        if(response.statusCode()!=201||response.body().length()>2048)throw new IOException("PROFILE_NOT_CONFIRMED");
        var body=GorillaProtocol.JSON.readTree(response.body());
        var id=java.util.UUID.fromString(body.path("id").asString()).toString();
        return Map.of("id",id,"displayName",body.path("displayName").asString());
    }
    @Override public void destroy()throws Exception {
        synchronized(this){closing=true;}
        var owned=process;if(owned!=null){owned.destroy();if(!owned.waitFor(6,TimeUnit.SECONDS)){owned.destroyForcibly();owned.waitFor(2,TimeUnit.SECONDS);}}
        worker.shutdownNow();http.close();
    }
}
