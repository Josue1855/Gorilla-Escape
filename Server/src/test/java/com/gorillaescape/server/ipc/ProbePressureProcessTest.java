package com.gorillaescape.server.ipc;
import java.io.*;
import java.net.*;
import java.nio.file.*;
import java.util.*;
import java.util.concurrent.*;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;
import tools.jackson.databind.json.JsonMapper;
import static org.junit.jupiter.api.Assertions.*;
/** Finite socket adversarial cases against the real managed Spring JVM. */
class ProbePressureProcessTest {
    @TempDir Path directory;
    private final JsonMapper json=JsonMapper.builder().build();
    private final List<String> diagnostics=new CopyOnWriteArrayList<>();
    private Process child;private int port;private long initialRSS, peakRSS;
    private void start() throws Exception {
        var b=new ProcessBuilder(Path.of(System.getProperty("java.home"),"bin","java").toString(),"-cp",System.getProperty("java.class.path"),"com.gorillaescape.server.GorillaEscapeApplication","--gorilla.ipc.managed=true","--server.address=127.0.0.1","--server.port=0","--logging.config=classpath:ipc-logback.xml");
        b.environment().put("GORILLA_IPC_INSTANCE",ProbeCodecTest.INSTANCE);b.environment().put("GORILLA_IPC_TOKEN",ProbeCodecTest.TOKEN);b.environment().put("GORILLA_IPC_LOCK_DIR",directory.toString());child=b.start();
        Thread drain=new Thread(()->{try(var r=child.errorReader()){String l;while((l=r.readLine())!=null)if(l.startsWith("{\"component\":\"ipc-java\"")){if(diagnostics.size()>=1024)throw new AssertionError("Diagnostics limit");diagnostics.add(l);}}catch(IOException ignored){}});drain.setDaemon(true);drain.start();
        var e=Executors.newSingleThreadExecutor();try {String ready=e.submit(()->{String l;var r=child.inputReader();while((l=r.readLine())!=null)if(l.startsWith("GORILLA_IPC_READY "))return l.substring(18);throw new EOFException();}).get(15,TimeUnit.SECONDS);port=json.readTree(ready).get("ipcPort").intValue();initialRSS=rss();peakRSS=initialRSS;}finally{e.shutdownNow();}
    }
    private long rss() throws IOException {
        Path status=Path.of("/proc",Long.toString(child.pid()),"status");
        if(!Files.exists(status))return -1;
        for(String line:Files.readAllLines(status))if(line.startsWith("VmRSS:"))return Long.parseLong(line.trim().split("\\s+")[1])*1024;
        return -1;
    }
    private Socket connect() throws IOException {peakRSS=Math.max(peakRSS,rss());var s=new Socket();s.setReceiveBufferSize(4096);s.connect(new InetSocketAddress("127.0.0.1",port),2000);s.setSoTimeout(4000);return s;}
    private String ping(int n) throws IOException {return n==1?ProbeCodecTest.fixture("ping-first"):"{\"ipcVersion\":1,\"type\":\"PING\",\"instanceId\":\""+ProbeCodecTest.INSTANCE+"\",\"connectionId\":\""+ProbeCodecTest.CONNECTION+"\",\"sequence\":"+n+",\"payload\":{}}";}
    private void healthy() throws Exception {assertTrue(child.isAlive());try(var s=connect()){ProbeCodec.write(s.getOutputStream(),ping(1));assertEquals(ProbeCodecTest.fixture("pong"),ProbeCodec.read(s.getInputStream()));}}
    private void finish() throws Exception {
        if(child==null)return;long finalRSS=child.isAlive()?rss():-1;peakRSS=Math.max(peakRSS,finalRSS);child.getOutputStream().close();
        try {assertTrue(child.waitFor(10,TimeUnit.SECONDS));assertEquals(0,child.exitValue());}
        finally {if(child.isAlive())child.destroyForcibly().waitFor(2,TimeUnit.SECONDS);}
        assertFalse(child.isAlive());
        long end=System.nanoTime()+TimeUnit.SECONDS.toNanos(2);while(diagnostics.stream().noneMatch(l->l.contains("RESOURCE_SUMMARY"))&&System.nanoTime()<end)Thread.sleep(5);
        var summary=json.readTree(diagnostics.stream().filter(l->l.contains("RESOURCE_SUMMARY")).findFirst().orElseThrow());
        assertEquals(0,summary.get("pendingDeadlines").intValue());assertTrue(summary.get("maximumPendingDeadlines").intValue()<=1);assertTrue(summary.get("deadlineTerminated").asBoolean());
        assertTrue(diagnostics.stream().noneMatch(l->l.contains(ProbeCodecTest.TOKEN)),"Secret in diagnostic");
        System.out.println("IPC_2C_PRESSURE {\"initialRSS\":"+initialRSS+",\"finalRSS\":"+finalRSS+",\"peakObservedRSS\":"+peakRSS+",\"resources\":"+summary+",\"diagnostics\":"+diagnostics.size()+"}");
    }
    @Test void allInvalidFramesCloseOnlyTheirConnection() throws Exception {
        start();try(var fixture=getClass().getResourceAsStream("/ipc-fixtures/cases/corpus.json")) {
            int count=0;var cases=json.readTree(fixture).get("cases");
            for(var c:cases)if(!c.get("accepted").asBoolean()) {
                try(var s=connect()) {if(!c.get("first").asBoolean()) {ProbeCodec.write(s.getOutputStream(),ping(1));ProbeCodec.read(s.getInputStream());}
                    s.getOutputStream().write(HexFormat.of().parseHex(c.get("hex").asString()));s.shutdownOutput();assertEquals(-1,s.getInputStream().read(),c.get("name").asString());}
                healthy();count++;
            }
            assertTrue(count>=60);
        }finally{finish();}
    }
    @Test void repeatedOversizedAndInvalidFrames() throws Exception {
        start();try {
            for(int i=0;i<100;i++)try(var s=connect()){new DataOutputStream(s.getOutputStream()).writeInt(i%2==0?4097:-1);assertEquals(-1,s.getInputStream().read());}
            for(int i=0;i<100;i++)try(var s=connect()){ProbeCodec.write(s.getOutputStream(),"{}");assertEquals(-1,s.getInputStream().read());}
            healthy();
        }finally{finish();}
    }
    @Test void thousandConcatenatedWithSlowConsumerAndMaximumFrames() throws Exception {
        start();try {
            try(var s=connect()) {
                var sender=Executors.newSingleThreadExecutor();
                try {var work=sender.submit(()->{try{for(int n=1;n<=1000;n++)ProbeCodec.write(s.getOutputStream(),ping(n));}catch(IOException e){throw new UncheckedIOException(e);}});
                    for(int n=1;n<=1000;n++){assertEquals(n,json.readTree(ProbeCodec.read(s.getInputStream())).get("sequence").intValue());Thread.sleep(1);}work.get(5,TimeUnit.SECONDS);
                }finally{sender.shutdownNow();}
            }
            try(var s=connect()){for(int n=1;n<=100;n++){String body=ping(n);ProbeCodec.write(s.getOutputStream(),body+" ".repeat(4096-body.length()));assertEquals(n,json.readTree(ProbeCodec.read(s.getInputStream())).get("sequence").intValue());}}
            healthy();
        }finally{finish();}
    }
    @Test void stalledAndTricklingFramesUseAbsoluteDeadline() throws Exception {
        start();try {
            for(boolean body:new boolean[]{false,true}) {
                try(var s=connect()) {
                    long begin=System.nanoTime();if(body)new DataOutputStream(s.getOutputStream()).writeInt(20);
                    for(int i=0;i<3;i++){s.getOutputStream().write(body?' ':0);s.getOutputStream().flush();Thread.sleep(500);}
                    assertEquals(-1,s.getInputStream().read());double elapsed=(System.nanoTime()-begin)/1e9;assertTrue(elapsed>=1.7&&elapsed<2.7,"Frame deadline "+elapsed);
                }
                healthy(); // Old read/write deadlines must not close this new connection.
            }
        }finally{finish();}
    }
    @Test void nonReadingProducerHasFinitePressureAndCleanup() throws Exception {
        start();try {
            try(var s=connect()) {
                var sender=Executors.newSingleThreadExecutor();try {
                    var work=sender.submit(()->{try{int bytes=0;for(int n=1;bytes<1024*1024;n++){String b=ping(n);bytes+=b.length()+4;ProbeCodec.write(s.getOutputStream(),b);}}catch(IOException expected){}});
                    // Intentional stalled peer duration, never startup/readiness.
                    Thread.sleep(3100);work.get(7,TimeUnit.SECONDS);
                    // Drain the bounded kernel buffer before EOF.
                    byte[] bytes=new byte[4096];while(s.getInputStream().read(bytes)!=-1){}
                }finally{sender.shutdownNow();}
            }
            healthy();
            assertTrue(diagnostics.stream().anyMatch(l->l.contains("WRITE")||l.contains("JSON_OR_IO_INVALID")||l.contains("READ_TIMEOUT")),"Expected closed stalled writer");
        }finally{finish();}
    }
}
