package com.gorillaescape.server.connectivity;

import static org.junit.jupiter.api.Assertions.*;
import java.net.InetAddress;
import java.net.UnknownHostException;
import java.nio.file.*;
import java.util.concurrent.TimeUnit;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;

class DnsRetryPolicyTest {
    @TempDir Path directory;
    @Test void startupRecoversFromCachedMissingNameWithoutWaitingTenSeconds() throws Exception {
        Path hosts=directory.resolve("hosts");Files.writeString(hosts, "");
        var child=new ProcessBuilder(Path.of(System.getProperty("java.home"),"bin","java").toString(),
            "-Djdk.net.hosts.file="+hosts, "-cp", System.getProperty("java.class.path"),
            Fixture.class.getName(),hosts.toString()).redirectErrorStream(true).start();
        try {
            assertTrue(child.waitFor(6,TimeUnit.SECONDS),"DNS retry fixture exceeded its bound");
            assertEquals(0,child.exitValue(),new String(child.getInputStream().readAllBytes()));
        } finally { if(child.isAlive()){child.destroyForcibly();child.waitFor(2,TimeUnit.SECONDS);} }
    }
    public static class Fixture {
        public static void main(String[] args) throws Exception {
            String positive=java.security.Security.getProperty("networkaddress.cache.ttl");
            DnsRetryPolicy.configure();
            assertEquals(positive,java.security.Security.getProperty("networkaddress.cache.ttl"));
            String name="becomes-ready.gorilla.test";
            assertThrows(UnknownHostException.class,()->InetAddress.getByName(name));
            Files.writeString(Path.of(args[0]),"127.0.0.1 "+name+"\n");
            long end=System.nanoTime()+TimeUnit.SECONDS.toNanos(3);
            while(System.nanoTime()<end){
                try {assertTrue(InetAddress.getByName(name).isLoopbackAddress());return;}
                catch(UnknownHostException stale){Thread.sleep(50);}
            }
            throw new AssertionError("Cached negative lookup prevented recovery inside readiness budget");
        }
    }
}
