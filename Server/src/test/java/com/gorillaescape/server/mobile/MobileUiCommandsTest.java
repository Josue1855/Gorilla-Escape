package com.gorillaescape.server.mobile;

import com.gorillaescape.server.protocol.*;
import org.junit.jupiter.api.Test;
import java.util.concurrent.atomic.AtomicLong;
import static org.junit.jupiter.api.Assertions.*;

class MobileUiCommandsTest {
    static final String ID="a".repeat(32);
    static tools.jackson.databind.node.ObjectNode context(String game)throws Exception {
        var n=GorillaProtocol.JSON.createObjectNode();n.put("version",3);n.put("gameId",game);n.put("screen",game.equals("lobby")?"menu":"game");
        n.put("playerId",game.equals("lobby")?0:1);n.put("phoneRequired",!game.equals("lobby")&&!game.equals("coconut-slice")&&!game.equals("jungle-keeper"));
        n.put("contextId",ID);n.put("hand","right");n.put("phase","Ready");var stats=n.putObject("stats");
        for(String key:java.util.List.of("Score","Progress","Elapsed","Balance","Distance")){stats.put("has"+key,false);stats.put(Character.toLowerCase(key.charAt(0))+key.substring(1),0);}
        return n;
    }
    static TechnicalSession.Joined owner(int player){return new TechnicalSession.Joined("session",player,"device","resume",2);}
    static MobileUiCommands.Request request(String action,String target){return new MobileUiCommands.Request(ID,ID,action,target);}
    @Test void contextV3SeparatesMenuPhoneAndCameraGames()throws Exception {
        for(String game:GamePresentation.GAMES)GamePresentation.validate(context(game));
        var c=context("bowling");c.put("contextId","wrong");assertThrows(java.io.IOException.class,()->GamePresentation.validate(c));
        c.put("contextId",ID);c.put("hand","both");assertThrows(java.io.IOException.class,()->GamePresentation.validate(c));
    }
    @Test void duplicatesAreAcceptedOnlyOnceAndCommandsCarryEpoch()throws Exception {
        var now=new AtomicLong(1000);var queue=new MobileUiCommands(now::get);
        queue.offer(request("select-game","bowling"),context("lobby"),owner(1));
        queue.offer(request("select-game","bowling"),context("lobby"),owner(1));
        var next=queue.drain();assertEquals("bowling",next.path("target").asString());assertEquals(2,next.path("connectionEpoch").asInt());assertNull(queue.drain());
    }
    @Test void rejectsSpectatorsOldContextsAndGameplayCommands()throws Exception {
        var queue=new MobileUiCommands(()->1000);
        assertThrows(java.io.IOException.class,()->queue.offer(request("select-game","bowling"),context("lobby"),owner(2)));
        assertThrows(java.io.IOException.class,()->queue.offer(request("select-game","bowling"),context("boxing"),owner(1)));
        assertThrows(java.io.IOException.class,()->queue.offer(request("set-score","999"),context("bowling"),owner(1)));
        var c=context("lobby");c.put("contextId","b".repeat(32));assertThrows(java.io.IOException.class,()->queue.offer(request("menu",""),c,owner(1)));
    }
    @Test void queuedRequestsExpireWithoutBeingReplayed()throws Exception {
        var now=new AtomicLong(1000);var queue=new MobileUiCommands(now::get);
        queue.offer(request("menu",""),context("bowling"),owner(1));now.set(3501);assertNull(queue.drain());
    }
}
