package com.gorillaescape.server.mobile;
import com.gorillaescape.server.connectivity.PublicIngress;
import com.gorillaescape.server.protocol.*;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.Timeout;
import org.junit.jupiter.api.io.TempDir;
import static org.junit.jupiter.api.Assertions.*;
import static org.junit.jupiter.api.Assumptions.*;
import java.io.*;
import java.nio.file.*;
import java.net.*;
import java.net.http.*;
import java.util.*;
import java.util.concurrent.*;

class MobilePersistenceLabTest {
 @TempDir Path data;
 @Test @Timeout(90) void realRtcDisplaysSevenGamesAndPersistsPlayerAcrossRestart()throws Exception{
  String jar=System.getProperty("gorilla.test.playersJar");
  assumeTrue(jar!=null&&System.getenv("GORILLA_TEST_NODE")!=null,"Requires explicit backend and browser lab opt-in");
  var report=new LinkedHashMap<String,Object>();var pool=Executors.newSingleThreadExecutor();
  var backend=new PlayersBackend(Path.of(jar),data);var mobile=new MobileRuntime();
  Process node=null;
  try(var ingress=PublicIngress.forRuntime(mobile)){
   long end=System.nanoTime()+30_000_000_000L;while(!backend.ready()&&System.nanoTime()<end)Thread.sleep(50);assertTrue(backend.ready());
   var field=MobileRuntime.class.getDeclaredField("playersBackend");field.setAccessible(true);field.set(mobile,backend);
   ingress.publicOrigin(ingress.origin());
   String url=JoinQrPoc.link(URI.create("https://192.168.1.2:8443/"),mobile.admission()).toASCIIString();
   node=new ProcessBuilder(System.getenv("GORILLA_TEST_NODE"),Path.of("../tools/integration/mobile-persistence-lab.mjs").toAbsolutePath().normalize().toString()).redirectError(ProcessBuilder.Redirect.DISCARD).start();
   try(var writer=new PrintWriter(node.getOutputStream(),true);var reader=new BufferedReader(node.inputReader())){
    writer.println(GorillaProtocol.JSON.writeValueAsString(Map.of("url",url,"ingress",ingress.origin().toString())));
    String line=pool.submit(reader::readLine).get(25,TimeUnit.SECONDS);assertNotNull(line);var registered=GorillaProtocol.JSON.readTree(line);assertEquals("REGISTERED",registered.path("event").asString(),registered.toString());
    String id=registered.path("id").asString();assertDoesNotThrow(()->UUID.fromString(id));
    for(String game:List.of("bowling","boxing","baseball","drums","coconut-slice","whack","log-balance")){
     var context=GorillaProtocol.JSON.createObjectNode();context.put("version",2);context.put("gameId",game);context.put("phase","Playing");context.put("playerId",1);
     var stats=context.putObject("stats");for(String key:List.of("Score","Progress","Elapsed","Balance","Distance")){stats.put("has"+key,key.equals("Score"));stats.put(Character.toLowerCase(key.charAt(0))+key.substring(1),key.equals("Score")?123:0);}
     mobile.presentGame(context);writer.println(GorillaProtocol.JSON.writeValueAsString(Map.of("gameId",game)));
     line=pool.submit(reader::readLine).get(7,TimeUnit.SECONDS);assertNotNull(line);assertEquals(game,GorillaProtocol.JSON.readTree(line).path("gameId").asString());
    }
    line=pool.submit(reader::readLine).get(7,TimeUnit.SECONDS);assertNotNull(line);var completed=GorillaProtocol.JSON.readTree(line);assertEquals("PASS",completed.path("event").asString());assertTrue(completed.path("independentDisplay").asBoolean());
    assertTrue(node.waitFor(5,TimeUnit.SECONDS));assertEquals(0,node.exitValue());
    backend.destroy();assertTrue(backend.cleanupComplete());
    var restarted=new PlayersBackend(Path.of(jar),data);
    try{
     end=System.nanoTime()+30_000_000_000L;while(!restarted.ready()&&System.nanoTime()<end)Thread.sleep(50);assertTrue(restarted.ready());
     var port=PlayersBackend.class.getDeclaredField("port");port.setAccessible(true);
     var client=HttpClient.newHttpClient();var response=client.send(HttpRequest.newBuilder(URI.create("http://127.0.0.1:"+port.getInt(restarted)+"/api/players")).GET().build(),HttpResponse.BodyHandlers.ofString());client.close();
     var items=GorillaProtocol.JSON.readTree(response.body()).path("items");assertEquals(1,items.size());assertEquals(id,items.get(0).path("id").asString());
     report.put("gate","PASS");report.put("games",7);report.put("authenticatedRegistration",true);report.put("invalidProfileStatus",registered.path("invalidStatus").asInt());report.put("playersAfterRestart",items.size());report.put("sameUuidAfterRestart",true);
    }finally{restarted.destroy();assertTrue(restarted.cleanupComplete());}
   }
  }finally{
   mobile.destroy();backend.destroy();pool.shutdownNow();if(node!=null&&node.isAlive()){node.destroyForcibly();node.waitFor(2,TimeUnit.SECONDS);}
   String output=System.getenv("GORILLA_MOBILE_INTEGRATION_REPORT");if(output!=null)Files.writeString(Path.of(output),GorillaProtocol.JSON.writeValueAsString(report)+"\n");
  }
 }
}
