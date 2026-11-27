package com.gorillaescape.server.ipc;
import java.io.*;
import java.util.HexFormat;
import org.junit.jupiter.api.Test;
import tools.jackson.databind.json.JsonMapper;
import static org.junit.jupiter.api.Assertions.*;
class ProbeCorpusTest {
    @Test void sharedCorpus() throws Exception {
        try(var input=getClass().getResourceAsStream("/ipc-fixtures/cases/corpus.json")) {
            var cases=JsonMapper.builder().build().readTree(input).get("cases");
            for(var c:cases) {
                boolean accepted=true;
                try {
                    String body=ProbeCodec.read(new ByteArrayInputStream(HexFormat.of().parseHex(c.get("hex").asString())));
                    if(c.get("kind").asString().equals("PING"))ProbeCodec.ping(body,ProbeCodecTest.INSTANCE,ProbeCodecTest.CONNECTION,c.get("sequence").intValue(),c.get("first").asBoolean()?ProbeCodecTest.TOKEN:null);
                    else ProbeCodec.validatePong(body,ProbeCodecTest.INSTANCE,ProbeCodecTest.CONNECTION,c.get("sequence").intValue());
                } catch(IOException e) {accepted=false;}
                assertEquals(c.get("accepted").asBoolean(),accepted,c.get("name").asString());
            }
        }
    }
    @Test void concatenationAndNoBodyReadForInvalidSize() throws Exception {
        var out=new ByteArrayOutputStream();ProbeCodec.write(out,ProbeCodecTest.fixture("ping-first"));ProbeCodec.write(out,ProbeCodecTest.fixture("pong"));
        var in=new ByteArrayInputStream(out.toByteArray());assertTrue(ProbeCodec.read(in).contains("PING"));assertTrue(ProbeCodec.read(in).contains("PONG"));assertEquals(0,in.available());
        var spy=new InputStream() { int at; final byte[] prefix={0,0,16,1}; @Override public int read(){if(at>=4)fail("Oversized body requested");return prefix[at++] & 255;} };
        assertThrows(IOException.class,()->ProbeCodec.read(spy));
        assertThrows(IOException.class,()->ProbeCodec.write(new ByteArrayOutputStream(),"x".repeat(4097)));
    }
    @Test void outputUtf8BoundedAndMalformedSurrogateRejected() throws Exception {
        var out=new ByteArrayOutputStream();ProbeCodec.write(out,"é".repeat(2048));assertEquals(4100,out.size());
        assertEquals(2048,ProbeCodec.read(new ByteArrayInputStream(out.toByteArray())).length());
        assertThrows(IOException.class,()->ProbeCodec.write(new ByteArrayOutputStream(),"é".repeat(2049)));
        assertThrows(IOException.class,()->ProbeCodec.write(new ByteArrayOutputStream(),"\ud800"));
    }

}
