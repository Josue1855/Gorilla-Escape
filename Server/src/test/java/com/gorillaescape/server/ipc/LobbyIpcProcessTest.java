package com.gorillaescape.server.ipc;
import org.junit.jupiter.api.Test;
import java.io.*;
import java.net.*;
import java.nio.file.*;
import java.util.concurrent.*;
import com.gorillaescape.server.protocol.GorillaProtocol;
import tools.jackson.databind.node.ObjectNode;
import static org.junit.jupiter.api.Assertions.*;
class LobbyIpcProcessTest {
    ObjectNode ping(int sequence){return (ObjectNode)GorillaProtocol.JSON.readTree("{\"ipcVersion\":1,\"type\":\"PING\",\"instanceId\":\""+ProbeCodecTest.INSTANCE+"\",\"connectionId\":\""+ProbeCodecTest.CONNECTION+"\",\"sequence\":"+sequence+",\"payload\":{}}");}
    Socket connect(int port)throws IOException{var s=new Socket();s.connect(new InetSocketAddress("127.0.0.1",port),2000);s.setSoTimeout(2000);return s;}
    ObjectNode exchange(Socket s,ObjectNode n)throws IOException{ProbeCodec.write(s.getOutputStream(),GorillaProtocol.JSON.writeValueAsString(n));return (ObjectNode)GorillaProtocol.JSON.readTree(ProbeCodec.read(s.getInputStream()));}
    @Test void realAuthenticatedIpcNegotiatesBoundsAndRejectsCommandReplay()throws Exception{
        var dir=Files.createTempDirectory("gorilla-lobby-test");var launch=new ProcessBuilder(Path.of(System.getProperty("java.home"),"bin","java").toString(),"-Djava.net.preferIPv4Stack=true","-cp",System.getProperty("java.class.path"),"com.gorillaescape.server.GorillaEscapeApplication","--gorilla.ipc.managed=true","--gorilla.mobile.rtc-enabled=true","--gorilla.mobile.lab-enabled=true","--server.address=127.0.0.1","--server.port=0","--logging.config=classpath:ipc-logback.xml");
        launch.environment().put("GORILLA_IPC_LOCK_DIR",dir.toString());launch.environment().put("GORILLA_IPC_INSTANCE",ProbeCodecTest.INSTANCE);launch.environment().put("GORILLA_IPC_TOKEN",ProbeCodecTest.TOKEN);launch.environment().put("GORILLA_MOBILE_OPERATOR","b".repeat(43));
        var child=launch.start();var drain=new Thread(()->{try(var e=child.errorReader()){while(e.read()!=-1){}}catch(IOException ignored){}});drain.setDaemon(true);drain.start();var executor=Executors.newSingleThreadExecutor();
        try{
            var ready=executor.submit(()->{var r=child.inputReader();String line;while((line=r.readLine())!=null)if(line.startsWith("GORILLA_IPC_READY "))return GorillaProtocol.JSON.readTree(line.substring(18));throw new EOFException();}).get(15,TimeUnit.SECONDS);int port=ready.get("ipcPort").intValue();
            try(var bad=connect(port)){var p=ping(1);var payload=(ObjectNode)p.get("payload");payload.put("launchToken","a".repeat(43));payload.put("phoneInputVersion",1);payload.put("lobbyVersion",1);assertThrows(IOException.class,()->exchange(bad,p));}
            try(var socket=connect(port)){
                var p=ping(1);var payload=(ObjectNode)p.get("payload");payload.put("launchToken",ProbeCodecTest.TOKEN);payload.put("phoneInputVersion",1);payload.put("lobbyVersion",1);
                var first=exchange(socket,p);assertFalse(first.get("payload").get("hasLobbyResult").booleanValue());assertEquals("OPEN",first.get("payload").get("lobby").get("phase").asString());assertEquals(0,first.get("payload").get("phoneInputs").size());
                String sid=first.get("payload").get("lobby").get("sessionId").asString();var command=GorillaProtocol.JSON.createObjectNode();command.put("kind","SET_PHASE");command.put("phase","STARTING");command.put("sessionId",sid);command.put("playerId",0);command.put("deviceSessionId","");command.put("connectionEpoch",0);command.put("ready",false);
                var next=ping(2);((ObjectNode)next.get("payload")).set("lobbyCommand",command);var reply=exchange(socket,next);
                assertTrue(reply.get("payload").get("hasLobbyResult").booleanValue());assertFalse(reply.get("payload").get("lobbyResult").get("accepted").booleanValue());assertEquals("LOBBY_PREREQUISITES",reply.get("payload").get("lobbyResult").get("code").asString());
                assertTrue(GorillaProtocol.JSON.writeValueAsBytes(reply).length<=4096);assertThrows(IOException.class,()->exchange(socket,next));
            }
            child.getOutputStream().close();assertTrue(child.waitFor(10,TimeUnit.SECONDS));assertEquals(0,child.exitValue());
        }finally{child.getOutputStream().close();if(child.isAlive()){child.destroy();if(!child.waitFor(2,TimeUnit.SECONDS))child.destroyForcibly().waitFor(2,TimeUnit.SECONDS);}executor.shutdownNow();drain.join(2000);try(var paths=Files.walk(dir)){for(var p:paths.sorted(java.util.Comparator.reverseOrder()).toList())Files.deleteIfExists(p);}}
    }
}
