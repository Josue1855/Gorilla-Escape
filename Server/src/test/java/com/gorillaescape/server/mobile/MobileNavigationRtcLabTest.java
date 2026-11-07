package com.gorillaescape.server.mobile;

import com.gorillaescape.server.connectivity.PublicIngress;
import com.gorillaescape.server.protocol.*;
import org.junit.jupiter.api.*;
import org.junit.jupiter.api.condition.EnabledIfEnvironmentVariable;
import java.io.*;
import java.nio.file.Path;
import java.net.URI;
import java.util.Map;
import java.util.concurrent.*;
import static org.junit.jupiter.api.Assertions.*;

@EnabledIfEnvironmentVariable(named="GORILLA_TEST_NODE",matches=".+")
class MobileNavigationRtcLabTest {
    @Test @Timeout(75) void menuDominantHandAndForegroundResumeUseRealRtc()throws Exception {
        var runtime=new MobileRuntime();var pool=Executors.newSingleThreadExecutor();var publisher=Executors.newSingleThreadScheduledExecutor();
        var latest=new java.util.concurrent.atomic.AtomicReference<tools.jackson.databind.JsonNode>();Process node=null;
        try(var ingress=PublicIngress.forRuntime(runtime)){
            ingress.publicOrigin(ingress.origin());var context=MobileUiCommandsTest.context("lobby");runtime.presentGame(context);latest.set(context.deepCopy());
            publisher.scheduleAtFixedRate(()->{try{runtime.presentGame(latest.get());}catch(Exception ignored){}},0,200,TimeUnit.MILLISECONDS);
            var url=JoinQrPoc.link(URI.create("https://192.168.1.2:8443/"),runtime.admission()).toASCIIString();
            node=new ProcessBuilder(System.getenv("GORILLA_TEST_NODE"),Path.of("../tools/integration/mobile-navigation-lab.mjs").toAbsolutePath().normalize().toString()).redirectError(ProcessBuilder.Redirect.INHERIT).start();
            try(var writer=new PrintWriter(node.getOutputStream(),true);var reader=new BufferedReader(new InputStreamReader(node.getInputStream()))){
                writer.println(GorillaProtocol.JSON.writeValueAsString(Map.of("ingress",ingress.origin().toString(),"url",url)));
                assertEquals("SELECTED",event(pool,reader));var select=runtime.uiCommands.drain();assertNotNull(select);assertEquals("bowling",select.path("target").asString());
                context=MobileUiCommandsTest.context("bowling");context.put("contextId","b".repeat(32));latest.set(context.deepCopy());runtime.presentGame(context);writer.println("{}");
                assertEquals("HAND",event(pool,reader));var hand=runtime.uiCommands.drain();assertNotNull(hand);assertEquals("left",hand.path("target").asString());
                context.put("hand","left");latest.set(context.deepCopy());runtime.presentGame(context);writer.println("{}");
                assertEquals("RETURNED",event(pool,reader));var back=runtime.uiCommands.drain();assertNotNull(back);assertEquals("menu",back.path("action").asString());assertTrue(back.path("connectionEpoch").asLong()>1);
                latest.set(MobileUiCommandsTest.context("lobby"));runtime.presentGame(latest.get());writer.println("{}");assertEquals("PASS",event(pool,reader));
                assertTrue(node.waitFor(5,TimeUnit.SECONDS));assertEquals(0,node.exitValue());
            }
        } finally {publisher.shutdownNow();runtime.destroy();pool.shutdownNow();if(node!=null&&node.isAlive()){node.destroyForcibly();node.waitFor(2,TimeUnit.SECONDS);}}
    }
    static String event(ExecutorService pool,BufferedReader reader)throws Exception {
        var line=pool.submit(reader::readLine).get(20,TimeUnit.SECONDS);assertNotNull(line);var value=GorillaProtocol.JSON.readTree(line);assertNotEquals("FAIL",value.path("event").asString(),"Browser lab: "+value.path("code").asString());return value.path("event").asString();
    }
}
