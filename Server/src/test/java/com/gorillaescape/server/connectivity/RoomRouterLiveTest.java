package com.gorillaescape.server.connectivity;
import org.junit.jupiter.api.*;
import org.junit.jupiter.api.condition.EnabledIfEnvironmentVariable;
import static org.junit.jupiter.api.Assertions.*;
import java.net.*;
import java.net.http.*;
import java.nio.file.*;
import java.time.Duration;
import java.util.UUID;
@EnabledIfEnvironmentVariable(named="GORILLA_REAL_TUNNEL",matches=".+")
class RoomRouterLiveTest {
 @Test @Timeout(90) void routerAndTunnel() throws Exception {
  DnsRetryPolicy.configure();
  String room=UUID.randomUUID().toString().replace("-","").substring(0,8),owner=UUID.randomUUID().toString();
  String secret=Files.readString(Path.of(System.getProperty("user.home"),".config/gorilla-escape/room-registration-secret")).trim();
  var client=HttpClient.newBuilder().connectTimeout(Duration.ofSeconds(5)).build();
  try(var ingress=new PublicIngress((path,body)->{throw new IllegalArgumentException("TEST");});var tunnel=new CloudflaredSupervisor(Path.of(System.getenv("GORILLA_REAL_TUNNEL")))) {
   URI origin=tunnel.start(ingress.origin(),ConnectionBootstrap.DEFAULT_STARTUP_MS);ingress.publicOrigin(URI.create("https://play.gorillas.run"));
   String body="{\"roomCode\":\""+room+"\",\"targetUrl\":\""+origin+"\",\"owner\":\""+owner+"\"}";
   var registration=client.send(HttpRequest.newBuilder(URI.create("https://play.gorillas.run/api/register-room")).timeout(Duration.ofSeconds(10)).header("Content-Type","application/json").header("Authorization","Bearer "+secret).POST(HttpRequest.BodyPublishers.ofString(body)).build(),HttpResponse.BodyHandlers.ofString());assertEquals(200,registration.statusCode(), registration.body());assertEquals("{\"ok\":true,\"version\":2}",registration.body());
   URI page=URI.create("https://play.gorillas.run/"+room+"/");
   var result=client.send(HttpRequest.newBuilder(page).timeout(Duration.ofSeconds(10)).GET().build(),HttpResponse.BodyHandlers.ofString());assertEquals(200,result.statusCode());assertTrue(result.body().contains("<base href=\"/"+room+"/\">"));
   var join=client.send(HttpRequest.newBuilder(page.resolve("mobile/join")).timeout(Duration.ofSeconds(10)).header("Origin","https://play.gorillas.run").header("Content-Type","application/json").POST(HttpRequest.BodyPublishers.ofString("{}" )).build(),HttpResponse.BodyHandlers.ofString());assertEquals(400,join.statusCode());
  }
 }
}
