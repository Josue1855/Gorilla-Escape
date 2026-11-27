package com.gorillaescape.server.hosting;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;
import java.nio.file.*;
import java.net.*;
import java.net.http.*;
import java.time.Duration;
import static org.junit.jupiter.api.Assertions.*;
class TrustRecoveryTest {
    @TempDir Path temp;
    @Test void bootstrapIsExplicitRecoveryOnlyAndNeverHostsGameplay()throws Exception{
        var field=LocalTrust.class.getDeclaredField("current");field.setAccessible(true);var before=field.get(null);
        field.set(null,LocalTrust.prepare(temp.resolve("trust"),"192.168.1.5"));
        int port;try(var free=new java.net.ServerSocket(0,1,InetAddress.getLoopbackAddress())){port=free.getLocalPort();}
        var client=HttpClient.newBuilder().connectTimeout(Duration.ofMillis(500)).build();
        URI origin=URI.create("http://127.0.0.1:"+port);
        try(var owner=new Owner(new TrustOnboarding("127.0.0.1","https://192.168.1.5:8443",port))){
            assertThrows(Exception.class,()->client.send(HttpRequest.newBuilder(origin.resolve("/prepare")).timeout(Duration.ofSeconds(1)).GET().build(),HttpResponse.BodyHandlers.discarding()));
            owner.value.recovery(true);var response=client.send(HttpRequest.newBuilder(origin.resolve("/prepare")).GET().build(),HttpResponse.BodyHandlers.ofString());
            assertEquals(200,response.statusCode());assertTrue(response.body().contains("Si no coincide"));assertTrue(response.body().contains("Android"));assertTrue(response.body().contains("iPhone"));
            assertEquals(200,client.send(HttpRequest.newBuilder(origin.resolve("/gorilla-root.cer")).GET().build(),HttpResponse.BodyHandlers.discarding()).statusCode());
            for(String path:new String[]{"/mobile/join","/","/password","/ipc"})assertEquals(404,client.send(HttpRequest.newBuilder(origin.resolve(path)).GET().build(),HttpResponse.BodyHandlers.discarding()).statusCode());
            owner.value.recovery(false);
            try(var freed=new java.net.ServerSocket(port,1,InetAddress.getLoopbackAddress())){assertTrue(freed.isBound());}
        }finally{field.set(null,before);}
    }
    record Owner(TrustOnboarding value) implements AutoCloseable {public void close(){value.destroy();}}
}
