package com.gorillaescape.server.protocol;
import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;
import java.net.URI;
import java.nio.file.*;
import java.util.Map;
import com.gorillaescape.server.ipc.ProbeCodec;
class QaHostingContractTest {
    @Test void prepareRasterHasOneSharedShapeInsideBoundedIpc()throws Exception{
        var raster=QaAdmission.raster(URI.create("http://192.168.1.5:8081/prepare"),"PREPARE",0);
        var hosting=Map.of("prepare",raster,"origin","https://192.168.1.5:8443","fingerprint","public-test-fingerprint","prepareUrl","http://192.168.1.5:8081/prepare");
        var node=GorillaProtocol.JSON.valueToTree(Map.of("ipcVersion",1,"type","PONG","sequence",1,"payload",Map.of("qaHosting",hosting)));
        byte[] body=GorillaProtocol.JSON.writeValueAsBytes(node);
        assertTrue(body.length<ProbeCodec.MAX_FRAME);assertFalse(node.get("payload").get("qaHosting").has("prepareModules"));assertFalse(node.get("payload").get("qaHosting").has("prepareSize"));
        var output=new java.io.ByteArrayOutputStream();ProbeCodec.write(output,GorillaProtocol.JSON.writeValueAsString(node));
        String framed=ProbeCodec.read(new java.io.ByteArrayInputStream(output.toByteArray()));assertEquals(GorillaProtocol.JSON.readTree(GorillaProtocol.JSON.writeValueAsString(node)),GorillaProtocol.JSON.readTree(framed));
        Path fixture=Path.of("../Shared/Protocol/fixtures/qa-hosting.json");
        if("1".equals(System.getenv("GORILLA_GENERATE_CONTRACT_FIXTURE")))Files.writeString(fixture,framed+"\n");
        assertEquals(GorillaProtocol.JSON.readTree(framed),GorillaProtocol.JSON.readTree(Files.readString(fixture)),"Java and Unity consume the same nested prepare fixture");
    }
}
