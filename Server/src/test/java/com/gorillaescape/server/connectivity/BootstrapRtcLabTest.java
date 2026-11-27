package com.gorillaescape.server.connectivity;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.Timeout;
import org.junit.jupiter.api.condition.EnabledIfEnvironmentVariable;
import static org.junit.jupiter.api.Assertions.*;
import com.gorillaescape.server.mobile.MobileRuntime;
import com.gorillaescape.server.protocol.*;
import java.net.URI;
import java.nio.file.*;
import java.util.*;
import java.util.concurrent.*;
import java.io.*;
/** Explicit test-only fake tunnel with real ingress/native RTC. No external WAN or physical QA. */
@EnabledIfEnvironmentVariable(named="GORILLA_TEST_NODE",matches=".+")
class BootstrapRtcLabTest {
 @Test @Timeout(75) void oneTwoFourPeersSurviveBootstrapLoss()throws Exception{
  var report=new LinkedHashMap<String,Object>();var pool=Executors.newSingleThreadExecutor();
  try(var runtime=new RuntimeOwner()){
   for(var mode:List.of(ConnectionBootstrap.Mode.CABIN,ConnectionBootstrap.Mode.AUTO)){
    var fake=new ConnectivityTest.Fake();try(var ingress=PublicIngress.forRuntime(runtime.mobile);var b=new ConnectionBootstrap(mode,URI.create("https://192.168.1.2:8443/"),fake,100)){
     ingress.publicOrigin(ingress.origin());b.start(ingress.origin());ConnectivityTest.waitReady(b);var urls=new ArrayList<String>();
     for(int i=0;i<4;i++)urls.add(JoinQrPoc.link(b.page(),runtime.mobile.admission()).toASCIIString());
     var node=new ProcessBuilder(System.getenv("GORILLA_TEST_NODE"),Path.of("../tools/t063/bootstrap-rtc-lab.mjs").toAbsolutePath().normalize().toString()).redirectError(ProcessBuilder.Redirect.DISCARD).start();
     try(var out=new PrintWriter(node.getOutputStream(),true);var in=new BufferedReader(new InputStreamReader(node.getInputStream()))){
      out.println(GorillaProtocol.JSON.writeValueAsString(Map.of("mode",mode.name(),"urls",urls,"ingress",ingress.origin().toASCIIString())));
      String established=pool.submit(in::readLine).get(25,TimeUnit.SECONDS);assertNotNull(established);var first=tools.jackson.databind.json.JsonMapper.builder().build().readTree(established);assertEquals("ESTABLISHED",first.get("event").asString(),established);assertEquals(4,runtime.mobile.peerCount());
      long statsDeadline=System.nanoTime()+TimeUnit.SECONDS.toNanos(3);
      while(runtime.mobile.joinDiagnostics().stream().filter(d->((Map<?,?>)d.get("selectedPair")).get("state").equals("SELECTED")).count()<4&&System.nanoTime()<statsDeadline)Thread.sleep(10);
      assertEquals(4,runtime.mobile.joinDiagnostics().stream().filter(d->((Map<?,?>)d.get("selectedPair")).get("state").equals("SELECTED")).count());
      report.put(mode.name()+"SelectedPairs",runtime.mobile.joinDiagnostics().stream().map(d->d.get("selectedPair")).toList());

      fake.state=Tunnel.State.FAILED;b.poll();assertEquals("CABIN",b.snapshot().get("resolvedMode"));assertEquals(4,runtime.mobile.peerCount());
      out.println("{\"action\":\"CONTINUE_AFTER_LOSS\"}");String completed=pool.submit(in::readLine).get(25,TimeUnit.SECONDS);assertNotNull(completed);var finalResult=tools.jackson.databind.json.JsonMapper.builder().build().readTree(completed);assertEquals("PASS",finalResult.get("gate").asString(),completed);assertTrue(node.waitFor(5,TimeUnit.SECONDS));assertEquals(0,node.exitValue());assertEquals(0,runtime.mobile.peerCount());report.put(mode.name(),finalResult);
     }finally{if(node.isAlive()){node.destroy();if(!node.waitFor(1,TimeUnit.SECONDS))node.destroyForcibly().waitFor(1,TimeUnit.SECONDS);}}
    }
    // Disconnected slots intentionally retain identity until expiry. A new session is explicit per run.
    runtime.reset();
   }
  }finally{pool.shutdownNow();String output=System.getenv("GORILLA_BOOTSTRAP_LAB_REPORT");if(output!=null)Files.writeString(Path.of(output),GorillaProtocol.JSON.writeValueAsString(report)+"\n");}
 }
 static final class RuntimeOwner implements AutoCloseable {MobileRuntime mobile=new MobileRuntime();void reset(){mobile.destroy();mobile=new MobileRuntime();}public void close(){mobile.destroy();}}
}
