package com.gorillaescape.server.connectivity;
import org.junit.jupiter.api.*;
import org.junit.jupiter.api.condition.EnabledIfEnvironmentVariable;
import static org.junit.jupiter.api.Assertions.*;
import com.gorillaescape.server.mobile.MobileRuntime;
import com.gorillaescape.server.protocol.JoinQrPoc;
import java.net.URI;
import java.net.http.*;
import java.nio.file.*;
import java.time.Duration;
import java.util.UUID;
import java.util.concurrent.TimeUnit;
@EnabledIfEnvironmentVariable(named="GORILLA_REAL_TUNNEL",matches=".+")
@EnabledIfEnvironmentVariable(named="GORILLA_TEST_NODE",matches=".+")
class PublicRoomRtcLiveTest {
 @Test @Timeout(100) void publicPageConnectsRealLocalRtc() throws Exception {
  DnsRetryPolicy.configure();String owner=UUID.randomUUID().toString();
  String secret=Files.readString(Path.of(System.getProperty("user.home"),".config/gorilla-escape/room-registration-secret")).trim();
  var client=HttpClient.newBuilder().connectTimeout(Duration.ofSeconds(5)).build();var mobile=new MobileRuntime();
  try(var ingress=PublicIngress.forRuntime(mobile);var tunnel=new CloudflaredSupervisor(Path.of(System.getenv("GORILLA_REAL_TUNNEL")))) {
   URI origin=tunnel.start(ingress.origin(),ConnectionBootstrap.DEFAULT_STARTUP_MS);ingress.publicOrigin(URI.create("https://play.gorillas.run"));
   String body="{\"roomCode\":\""+mobile.roomCode()+"\",\"targetUrl\":\""+origin+"\",\"owner\":\""+owner+"\"}";
   var registration=client.send(HttpRequest.newBuilder(URI.create("https://play.gorillas.run/api/register-room")).timeout(Duration.ofSeconds(10)).header("Content-Type","application/json").header("Authorization","Bearer "+secret).POST(HttpRequest.BodyPublishers.ofString(body)).build(),HttpResponse.BodyHandlers.ofString());
   assertEquals(200,registration.statusCode());
   URI join=JoinQrPoc.link(URI.create("https://play.gorillas.run/"),mobile.admission(),mobile.roomCode());
   var process=new ProcessBuilder(System.getenv("GORILLA_TEST_NODE"),Path.of("../tools/cloudflare-worker/tests/live-rtc.mjs").toAbsolutePath().normalize().toString()).redirectErrorStream(true).start();
   try {
    process.getOutputStream().write((join.toASCIIString()+"\n").getBytes(java.nio.charset.StandardCharsets.UTF_8));process.getOutputStream().close();
    assertTrue(process.waitFor(50,TimeUnit.SECONDS),"Browser test timed out");
    String output=new String(process.getInputStream().readNBytes(4096),java.nio.charset.StandardCharsets.UTF_8);
    assertEquals(0,process.exitValue(),output);assertTrue(output.contains("PASS:"));
    assertEquals("Jugador prueba",mobile.lobby().players().get(0).displayName());
   } finally {if(process.isAlive())process.destroyForcibly();}
  } finally {mobile.destroy();}
 }
}
