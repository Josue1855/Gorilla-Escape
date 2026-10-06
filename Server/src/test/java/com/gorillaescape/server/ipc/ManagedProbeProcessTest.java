package com.gorillaescape.server.ipc;

import org.junit.jupiter.api.Test;
import java.io.*;
import java.net.*;
import java.nio.file.Path;
import java.util.concurrent.*;
import tools.jackson.databind.json.JsonMapper;
import static org.junit.jupiter.api.Assertions.*;

class ManagedProbeProcessTest {
    @Test void realChildReadinessFramesAndStdinEof() throws Exception {
        var launch=new ProcessBuilder(Path.of(System.getProperty("java.home"),"bin","java").toString(),
                "-Djava.net.preferIPv4Stack=true","-cp",System.getProperty("java.class.path"),"com.gorillaescape.server.GorillaEscapeApplication",
                "--gorilla.ipc.managed=true","--server.address=127.0.0.1","--server.port=0",
                "--logging.config=classpath:ipc-logback.xml");
        launch.environment().put("GORILLA_IPC_INSTANCE",ProbeCodecTest.INSTANCE);
        launch.environment().put("GORILLA_IPC_TOKEN",ProbeCodecTest.TOKEN);
        Process child=launch.start();
        Thread drain=new Thread(()-> {try(var err=child.errorReader()){char[] b=new char[1024];while(err.read(b)>0){}}catch(IOException ignored){}});
        drain.setDaemon(true);drain.start();
        var executor=Executors.newSingleThreadExecutor(r->{var t=new Thread(r);t.setDaemon(true);return t;});
        int port=0;
        try {
            String ready=executor.submit(()-> {
                var reader=child.inputReader(); String line;
                while((line=reader.readLine())!=null)if(line.startsWith("GORILLA_IPC_READY "))return line.substring(18);
                throw new EOFException("READY missing");
            }).get(15,TimeUnit.SECONDS);
            var node=JsonMapper.builder().build().readTree(ready);
            assertEquals(child.pid(),node.get("pid").longValue());
            assertEquals(ProbeCodecTest.INSTANCE,node.get("instanceId").asString());
            assertEquals(1,node.get("ipcVersion").intValue());
            port=node.get("ipcPort").intValue();assertTrue(port>0);
            try(var socket=new Socket()) {
                socket.connect(new InetSocketAddress("127.0.0.1",port),2000);socket.setSoTimeout(2000);
                // Actual split writes verify framing on the listener, not just an in-memory codec.
                var bytes=new ByteArrayOutputStream();ProbeCodec.write(bytes,ProbeCodecTest.fixture("ping-first"));
                for(byte b:bytes.toByteArray())socket.getOutputStream().write(b);socket.getOutputStream().flush();
                assertEquals(ProbeCodecTest.fixture("pong"),ProbeCodec.read(socket.getInputStream()));
                ProbeCodec.write(socket.getOutputStream(),"{\"ipcVersion\":1,\"type\":\"PING\",\"instanceId\":\""+ProbeCodecTest.INSTANCE
                    +"\",\"connectionId\":\""+ProbeCodecTest.CONNECTION+"\",\"sequence\":2,\"payload\":{}}");
                assertTrue(ProbeCodec.read(socket.getInputStream()).contains("\"sequence\":2"));
            }
            child.getOutputStream().close();
            assertTrue(child.waitFor(10,TimeUnit.SECONDS),"Java residual after normal EOF");
            assertEquals(0,child.exitValue());assertFalse(child.isAlive());
            final int closedPort=port;
            assertThrows(IOException.class,()->{try(var socket=new Socket()){socket.connect(new InetSocketAddress("127.0.0.1",closedPort),500);}});
        } finally {
            child.getOutputStream().close();
            if(child.isAlive()){child.destroy();if(!child.waitFor(2,TimeUnit.SECONDS))child.destroyForcibly().waitFor(2,TimeUnit.SECONDS);}
            executor.shutdownNow();drain.join(2000);
        }
    }
}
