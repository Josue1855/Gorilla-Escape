package com.gorillaescape.server.protocol;
import com.gorillaescape.server.mobile.PhoneInputHub;
import tools.jackson.databind.node.ObjectNode;
import org.junit.jupiter.api.Test;import java.nio.file.*;import static org.junit.jupiter.api.Assertions.*;
class PhoneInputHubTest {
 @Test void latestOnlySlotsAndNullMask()throws Exception{var hub=new PhoneInputHub();var n=(ObjectNode)GorillaProtocol.JSON.readTree(Files.readAllBytes(Path.of("../Shared/Protocol/mobile/motion-v1.json")));n.put("serverReceiveTimestamp",100000);for(int p=1;p<=4;p++)for(int seq=1;seq<=100;seq++){n.put("playerId",p);n.put("sequence",seq);hub.accept(n,1,"ACTIVE");}assertEquals(4,hub.pendingSlots());assertEquals(396,hub.overwritten);var batch=hub.drain();assertTrue(batch.size()>0);assertTrue(batch.get(0).get("acceleration").get("hasX").asBoolean());assertFalse(batch.get(0).get("rotationRate").has("hasAlpha"));while(hub.pendingSlots()>0)hub.drain();assertEquals(4,hub.forwarded);}
}
