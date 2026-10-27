package com.gorillaescape.server.connectivity;
import static org.junit.jupiter.api.Assertions.*;
import org.junit.jupiter.api.Test;
import com.gorillaescape.server.mobile.MobileRuntime;
import com.gorillaescape.server.protocol.GorillaProtocol;
import java.net.http.*;
import java.util.Map;
class RoomPinTest {
 @Test void pinAdmissionUsesExistingOneUseIdentityAndLimitsGuesses()throws Exception{
  var runtime=new MobileRuntime();
  try(var ingress=PublicIngress.forRuntime(runtime);var client=HttpClient.newHttpClient()){
   String pin=runtime.roomCode();
   var request=HttpRequest.newBuilder(ingress.origin().resolve("/mobile/pin-admission")).header("Content-Type","application/json").POST(HttpRequest.BodyPublishers.ofString("{\"pin\":\""+pin.toLowerCase()+"\"}")).build();
   var response=client.send(request,HttpResponse.BodyHandlers.ofString());assertEquals(200,response.statusCode());
   var data=GorillaProtocol.JSON.readTree(response.body());assertTrue(data.path("admission").asString().matches("[A-Za-z0-9_-]{43}"));assertTrue(data.has("sessionId"));
   for(int i=0;i<8;i++)assertThrows(IllegalArgumentException.class,()->runtime.admissionByPin("!!!!"));
   assertEquals("PIN_RATE_LIMITED",assertThrows(IllegalStateException.class,()->runtime.admissionByPin(pin)).getMessage());
   assertEquals(429,client.send(request,HttpResponse.BodyHandlers.ofString()).statusCode());
   assertTrue(PublicIngress.allowed("GET","/brand/LilitaOne.ttf"));assertTrue(PublicIngress.allowed("GET","/brand/island-route.png"));
   assertFalse(PublicIngress.allowed("GET","/brand/../server.jar"));
  }finally{runtime.destroy();}
 }
}
