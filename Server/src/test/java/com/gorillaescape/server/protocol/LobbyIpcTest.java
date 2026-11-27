package com.gorillaescape.server.protocol;
import org.junit.jupiter.api.Test;
import tools.jackson.databind.node.ObjectNode;
import static org.junit.jupiter.api.Assertions.*;
class LobbyIpcTest {
    final TechnicalSession session=new TechnicalSession(System::nanoTime,30_000_000_000L,5_000_000_000L,30_000_000_000L);
    ObjectNode phase(String value){var n=GorillaProtocol.JSON.createObjectNode();n.put("kind","SET_PHASE");n.put("phase",value);n.put("sessionId",session.lobby().sessionId());n.put("playerId",0);n.put("deviceSessionId","");n.put("connectionEpoch",0);n.put("ready",false);return n;}
    @Test void exactFieldsAndTypesNoCoercion(){var n=phase("STARTING");n.put("ready","true");assertFalse(LobbyIpc.apply(n,session).accepted());n=phase("STARTING");n.put("extra",true);assertFalse(LobbyIpc.apply(n,session).accepted());assertFalse(LobbyIpc.apply(null,session).accepted());}
    @Test void unknownPhaseWrongSessionAndWrongIdentityCannotMutate(){var n=phase("STARTED");assertFalse(LobbyIpc.apply(n,session).accepted());n=phase("INVALID");assertFalse(LobbyIpc.apply(n,session).accepted());n=phase("STARTING");n.put("sessionId","wrong");assertFalse(LobbyIpc.apply(n,session).accepted());assertEquals("OPEN",session.lobby().phase());}
    @Test void explicitUnityReadyThenStartAndSafeDuplicate(){var a=session.createAdmission();var c=session.join(a.sessionId(),a.token());session.networkReady(c.sessionId(),1,c.sessionDeviceId(),1);session.inputReady(c.sessionId(),1,c.sessionDeviceId(),1,true);
        var n=phase("");n.put("kind","SET_PLAYER_READY");n.put("playerId",1);n.put("deviceSessionId",c.sessionDeviceId());n.put("connectionEpoch",1);n.put("ready",true);
        assertTrue(LobbyIpc.apply(n,session).accepted());assertTrue(LobbyIpc.apply(phase("STARTING"),session).accepted());assertTrue(LobbyIpc.apply(phase("STARTED"),session).accepted());assertTrue(LobbyIpc.apply(phase("STARTED"),session).accepted());
        assertEquals("LOBBY_READY_FROZEN",LobbyIpc.apply(n,session).code());
    }
    @Test void phoneMessageTypeCannotInvokeUnityCommand(){var n=phase("STARTING");n.put("kind","HELLO");assertFalse(LobbyIpc.apply(n,session).accepted());assertEquals("OPEN",session.lobby().phase());}
}
