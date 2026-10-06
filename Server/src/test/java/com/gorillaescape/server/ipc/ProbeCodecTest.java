package com.gorillaescape.server.ipc;

import org.junit.jupiter.api.Test;
import java.io.*;
import java.nio.charset.StandardCharsets;
import static org.junit.jupiter.api.Assertions.*;

class ProbeCodecTest {
    static final String INSTANCE="00000000-0000-4000-8000-000000000001";
    static final String CONNECTION="00000000-0000-4000-8000-000000000002";
    static final String TOKEN="AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
    static String fixture(String name) throws IOException {
        try(var in=ProbeCodecTest.class.getResourceAsStream("/ipc-fixtures/"+name+".json")) {
            assertNotNull(in); return new String(in.readAllBytes(),StandardCharsets.UTF_8).trim();
        }
    }
    @Test void sharedContractAndBigEndianFrame() throws IOException {
        String first=fixture("ping-first");
        var ping=ProbeCodec.ping(first,INSTANCE,null,1,TOKEN);
        assertEquals(fixture("pong"),ProbeCodec.pong(ping));
        var out=new ByteArrayOutputStream(); ProbeCodec.write(out,first);
        assertEquals(first.getBytes(StandardCharsets.UTF_8).length,new DataInputStream(new ByteArrayInputStream(out.toByteArray())).readInt());
        var fragmented=new FilterInputStream(new ByteArrayInputStream(out.toByteArray())) {
            @Override public int read(byte[] b,int off,int len)throws IOException {return super.read(b,off,Math.min(1,len));}
        };
        assertEquals(first,ProbeCodec.read(fragmented));
    }
    @Test void rejectsLengthsTruncationAndUtf8() throws IOException {
        for(int size:new int[]{0,4097,-1}) {
            var out=new ByteArrayOutputStream();new DataOutputStream(out).writeInt(size);
            assertThrows(IOException.class,()->ProbeCodec.read(new ByteArrayInputStream(out.toByteArray())));
        }
        assertThrows(EOFException.class,()->ProbeCodec.read(new ByteArrayInputStream(new byte[]{0,0,0,2,123})));
        assertThrows(IOException.class,()->ProbeCodec.read(new ByteArrayInputStream(new byte[]{0,0,0,1,(byte)255})));
    }
    @Test void rejectsTokenIdentitySequenceAndDuplicateFields() throws IOException {
        String first=fixture("ping-first");
        assertThrows(IOException.class,()->ProbeCodec.ping(first,INSTANCE,null,1,"wrong"));
        assertThrows(IOException.class,()->ProbeCodec.ping(first,INSTANCE,CONNECTION,2,null));
        assertThrows(IOException.class,()->ProbeCodec.ping(first,"other",null,1,TOKEN));
        assertThrows(IOException.class,()->ProbeCodec.ping(first.replace("\"ipcVersion\":1","\"ipcVersion\":1,\"ipcVersion\":1"),INSTANCE,null,1,TOKEN));
        assertThrows(IOException.class,()->ProbeCodec.ping(first.replace("\"sequence\":1","\"sequence\":1.5"),INSTANCE,null,1,TOKEN));
    }
}
