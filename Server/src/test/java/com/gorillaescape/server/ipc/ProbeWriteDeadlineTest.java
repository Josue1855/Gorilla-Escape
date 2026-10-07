package com.gorillaescape.server.ipc;
import java.io.*;
import java.net.*;
import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;
class ProbeWriteDeadlineTest {
    @Test void stalledRealSocketClosesWithinDeadlineAndNewSocketIsUnaffected() throws Exception {
        var probe=new ManagedProbe();
        try(var listener=new ServerSocket(0,1,InetAddress.getByName("127.0.0.1"))) {
            try(var peer=new Socket()) {
                peer.setReceiveBufferSize(1024);peer.connect(listener.getLocalSocketAddress());
                try(var server=listener.accept()) {
                    server.setSendBufferSize(1024);
                    long start=System.nanoTime();
                    IOException failure=assertThrows(IOException.class,()->{for(int i=0;i<256;i++)probe.writeFrame(server," ".repeat(4096));});
                    assertEquals("IPC_WRITE_TIMEOUT",failure.getMessage());
                    double elapsed=(System.nanoTime()-start)/1e9;assertTrue(elapsed>=1.8&&elapsed<3.5,"write timeout "+elapsed);
                    assertTrue(server.isClosed());assertEquals(0,probe.pendingDeadlines());
                }
            }
            try(var peer=new Socket("127.0.0.1",listener.getLocalPort());var server=listener.accept()) {
                peer.setSoTimeout(2000);
                for(int i=0;i<3;i++){probe.writeFrame(server,"{}");assertEquals("{}",ProbeCodec.read(peer.getInputStream()));assertEquals(0,probe.pendingDeadlines());if(i<2)Thread.sleep(1100);}
                assertFalse(server.isClosed());
            }
        }finally{probe.destroy();}
    }
}
