package com.gorillaescape.server.connectivity;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.Timeout;
import org.junit.jupiter.api.condition.EnabledIfEnvironmentVariable;
import static org.junit.jupiter.api.Assertions.*;
import java.nio.file.*;
import java.net.http.*;
import java.time.Duration;
import java.util.*;
import com.gorillaescape.server.protocol.GorillaProtocol;
/** Explicit opt-in EXTERNAL NETWORK LAB; absent prerequisite is NOT RUN, never physical PASS. */
@EnabledIfEnvironmentVariable(named="GORILLA_REAL_TUNNEL",matches=".+")
class RealQuickTunnelTest {
    @Test @Timeout(60) void publicHttpsBootstrapOnly()throws Exception{
        DnsRetryPolicy.configure();
        var result=new LinkedHashMap<String,Object>();result.put("classification","EXTERNAL NETWORK LAB / no physical phone");
        var tunnel=new CloudflaredSupervisor(Path.of(System.getenv("GORILLA_REAL_TUNNEL")));
        try(var ingress=new PublicIngress((path,body)->{throw new IllegalArgumentException("INVALID_TEST_ADMISSION");});tunnel){
            long start=System.nanoTime();var origin=tunnel.start(ingress.origin(),15000);ingress.publicOrigin(origin);result.put("tunnelStartupMs",(System.nanoTime()-start)/1_000_000);result.put("version",tunnel.version());
            var client=HttpClient.newBuilder().connectTimeout(Duration.ofSeconds(5)).build();
            var response=client.send(HttpRequest.newBuilder(origin.resolve("/")).timeout(Duration.ofSeconds(10)).GET().build(),HttpResponse.BodyHandlers.ofString());
            result.put("publicHttpsStatus",response.statusCode());assertEquals(200,response.statusCode());assertTrue(response.body().contains("Gorilla Escape"));
            var codes=new LinkedHashMap<String,Integer>();for(String path:List.of("/mobile/admission","/mobile/diagnostics","/mobile/end","/ipc","/qa/preview","/camera/preview","/prepare","/ca.key","/actuator")){
                var r=client.send(HttpRequest.newBuilder(origin.resolve(path)).timeout(Duration.ofSeconds(3)).GET().build(),HttpResponse.BodyHandlers.discarding());codes.put(path,r.statusCode());assertEquals(404,r.statusCode());
            }result.put("forbidden",codes);
            var join=client.send(HttpRequest.newBuilder(origin.resolve("/mobile/join")).timeout(Duration.ofSeconds(5)).header("Content-Type","application/json").POST(HttpRequest.BodyPublishers.ofString("{}")).build(),HttpResponse.BodyHandlers.ofString());assertEquals(400,join.statusCode());result.put("invalidAdmissionStatus",join.statusCode());assertTrue(join.headers().firstValue("Cache-Control").orElseThrow().contains("no-store"));
            tunnel.close();assertTrue(tunnel.cleanupComplete());result.put("cleanupComplete",true);result.put("gate","PASS");
        }catch(Exception e){result.put("gate","BLOCKED");result.put("reason",e.getClass().getSimpleName());throw e;}
        finally{result.put("supervisor",tunnel.diagnostics());String output=System.getenv("GORILLA_REAL_TUNNEL_REPORT");if(output!=null)Files.writeString(Path.of(output),GorillaProtocol.JSON.writeValueAsString(result)+"\n");}
    }
}
