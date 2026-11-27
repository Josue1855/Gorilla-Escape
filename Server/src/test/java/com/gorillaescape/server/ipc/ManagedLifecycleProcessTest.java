package com.gorillaescape.server.ipc;

import java.io.*;
import java.net.*;
import java.nio.file.*;
import java.util.concurrent.*;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;
import tools.jackson.databind.json.JsonMapper;
import static org.junit.jupiter.api.Assertions.*;

class ManagedLifecycleProcessTest {
    @TempDir Path directory;
    private Process launch() throws IOException {
        var builder = new ProcessBuilder(Path.of(System.getProperty("java.home"),"bin","java").toString(),
            "-cp",System.getProperty("java.class.path"),"com.gorillaescape.server.GorillaEscapeApplication",
            "--gorilla.ipc.managed=true","--server.address=127.0.0.1","--server.port=0","--logging.config=classpath:ipc-logback.xml");
        builder.environment().put("GORILLA_IPC_INSTANCE",ProbeCodecTest.INSTANCE);
        builder.environment().put("GORILLA_IPC_TOKEN",ProbeCodecTest.TOKEN);
        builder.environment().put("GORILLA_IPC_LOCK_DIR",directory.toString());
        builder.redirectError(ProcessBuilder.Redirect.DISCARD);
        return builder.start();
    }
    private String ready(Process child) throws Exception {
        var executor = Executors.newSingleThreadExecutor();
        try { return executor.submit(()-> {
            String line; var reader=child.inputReader();
            while ((line=reader.readLine())!=null) if(line.startsWith("GORILLA_IPC_READY ")) return line.substring(18);
            throw new EOFException();
        }).get(15,TimeUnit.SECONDS); }
        finally { executor.shutdownNow(); }
    }
    private void eof(Process child) throws Exception {
        child.getOutputStream().close(); assertTrue(child.waitFor(10,TimeUnit.SECONDS));
        assertEquals(0,child.exitValue()); assertFalse(child.isAlive());
    }
    private void cleanup(Process child) throws Exception {
        if(child.isAlive()) { child.getOutputStream().close(); if(!child.waitFor(10,TimeUnit.SECONDS)) child.destroyForcibly().waitFor(2,TimeUnit.SECONDS); }
        assertFalse(child.isAlive(),"Owned Java residual");
    }
    @Test void competingJvmFailsBeforeSpringAndOwnerStillResponds() throws Exception {
        Process a=launch(), b=null;
        try {
            int port=JsonMapper.builder().build().readTree(ready(a)).get("ipcPort").intValue();
            b=launch(); assertTrue(b.waitFor(5,TimeUnit.SECONDS)); assertEquals(73,b.exitValue());
            assertFalse(new String(b.getInputStream().readAllBytes()).contains("GORILLA_IPC_READY"));
            try(var socket=new Socket("127.0.0.1",port)) {
                socket.setSoTimeout(2000); ProbeCodec.write(socket.getOutputStream(),ProbeCodecTest.fixture("ping-first"));
                assertEquals(ProbeCodecTest.fixture("pong"),ProbeCodec.read(socket.getInputStream()));
            }
            eof(a);
            Process next=launch(); try { ready(next); eof(next); } finally { cleanup(next); }
        } finally { cleanup(a); if(b!=null) cleanup(b); }
    }
    @Test void immediateParentEofDuringBootstrapExitsCleanly() throws Exception {
        Process child=launch(); try { eof(child); } finally { cleanup(child); }
    }
    @Test void killedOwnerReleasesKernelLockForNextRealJvm() throws Exception {
        Process first=launch();
        try { ready(first); first.destroyForcibly(); assertTrue(first.waitFor(2,TimeUnit.SECONDS)); }
        finally { cleanup(first); }
        Process next=launch(); try { ready(next); eof(next); } finally { cleanup(next); }
    }
}
