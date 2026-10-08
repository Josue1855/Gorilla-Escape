package com.gorillaescape.server.protocol;
import org.junit.jupiter.api.Test;
import java.nio.file.*;
import java.io.IOException;
import tools.jackson.databind.node.ObjectNode;
import static org.junit.jupiter.api.Assertions.*;
class GorillaProtocolTest {
 ObjectNode sample()throws Exception{return (ObjectNode)GorillaProtocol.JSON.readTree(Files.readAllBytes(Path.of("../Shared/Protocol/mobile/motion-v1.json")));}
 GorillaProtocol.Gate gate(){return new GorillaProtocol.Gate(new GorillaProtocol.Identity("11111111-1111-1111-1111-111111111111",1,"22222222-2222-2222-2222-222222222222"),()->100000,2000,250,100);}
 byte[] bytes(ObjectNode n){return GorillaProtocol.JSON.writeValueAsBytes(n);}
 @Test void sharedCorpus()throws Exception{
  var corpus=GorillaProtocol.JSON.readTree(Files.readAllBytes(Path.of("../Shared/Protocol/mobile/corpus-v1.json")));
  for(var c:corpus.get("cases")){
   var n=sample();if(c.has("field"))n.set(c.get("field").asString(),c.get("value"));if(c.has("remove"))n.remove(c.get("remove").asString());
   if(c.has("partial")){var a=(ObjectNode)n.get("payload").get("acceleration");a.put("availability","partial");((ObjectNode)a.get("values")).putNull("y");}
   String raw=GorillaProtocol.JSON.writeValueAsString(n);if(c.has("raw"))raw=c.get("raw").asString();if(c.has("suffix"))raw+=c.get("suffix").asString();if(c.has("duplicate"))raw=raw.replace("{\"protocolVersion\":1", "{\"protocolVersion\":1,\"protocolVersion\":1");if(c.has("oversized"))raw=" ".repeat(2049);
   final byte[] frame=raw.getBytes(java.nio.charset.StandardCharsets.UTF_8);
   if(c.get("expected").asString().equals("PASS"))assertNotNull(gate().accept(frame),c.get("name").asString());else assertThrows(IOException.class,()->gate().accept(frame),c.get("name").asString());
  }
 }
 @Test void validAndServerStamp()throws Exception{var v=gate().accept(bytes(sample()));assertEquals(100000,v.get("serverReceiveTimestamp").longValue());assertTrue(v.get("payload").get("rotationRate").get("values").isNull());}
 @Test void invalidCorpus()throws Exception{
  for(String field:new String[]{"protocolVersion","messageType","sessionId","playerId","deviceSessionId","clientTimestamp","serverReceiveTimestamp"}){
   var n=sample();switch(field){case "protocolVersion"->n.put(field,2);case "messageType"->n.put(field,"SMASH");case "playerId"->n.put(field,2);case "clientTimestamp"->n.put(field,99900-3000);case "serverReceiveTimestamp"->n.put(field,100000);default->n.put(field,"wrong");}assertThrows(IOException.class,()->gate().accept(bytes(n)),field);
  }
  var missing=sample();missing.remove("quality");assertThrows(IOException.class,()->gate().accept(bytes(missing)));
  var future=sample();future.put("clientTimestamp",100251);assertThrows(IOException.class,()->gate().accept(bytes(future)));
  assertThrows(IOException.class,()->gate().accept(new byte[2049]));assertThrows(IOException.class,()->gate().accept("{".getBytes()));
 }
 @Test void duplicateOldReorderAndRecovery()throws Exception{var g=gate();var n=sample();n.put("sequence",4);g.accept(bytes(n));for(int i:new int[]{4,3,2}){n.put("sequence",i);assertThrows(IOException.class,()->g.accept(bytes(n)));}n.put("sequence",5);assertNotNull(g.accept(bytes(n)));}
 @Test void partialValuesPreserveNullAndRejectFalseAvailability()throws Exception{var n=sample();var a=(ObjectNode)n.get("payload").get("acceleration");a.put("availability","partial");((ObjectNode)a.get("values")).putNull("y");assertTrue(gate().accept(bytes(n)).get("payload").get("acceleration").get("values").get("y").isNull());a.put("availability","present");assertThrows(IOException.class,()->gate().accept(bytes(n)));}
 @Test void rateBoundAndIndependentPeers()throws Exception{var g=gate();for(int i=0;i<100;i++){var n=sample();n.put("sequence",i);g.accept(bytes(n));}var n=sample();n.put("sequence",101);assertThrows(IOException.class,()->g.accept(bytes(n)));assertNotNull(gate().accept(bytes(sample())));}
 @Test void reliableDomainIsIndependentFromMotion()throws Exception{var g=gate();var n=sample();n.put("sequence",100);g.accept(bytes(n));n.put("messageType","HEARTBEAT");n.put("sequence",1);n.set("payload",GorillaProtocol.JSON.createObjectNode());assertNotNull(g.accept(bytes(n)));}
}
