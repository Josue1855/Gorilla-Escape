package com.gorillaescape.server.protocol;
import org.junit.jupiter.api.Test;
import java.util.concurrent.atomic.AtomicLong;
import static org.junit.jupiter.api.Assertions.*;
class TechnicalSessionTest {
 final AtomicLong clock=new AtomicLong();
 TechnicalSession session(){return new TechnicalSession(clock::get,30,3,60);}
 @Test void admissionOneUseAndWrongSession(){var s=session();var a=s.createAdmission();assertThrows(IllegalArgumentException.class,()->s.join("wrong",a.token()));var c=s.join(a.sessionId(),a.token());assertEquals(1,c.playerId());assertThrows(IllegalArgumentException.class,()->s.join(a.sessionId(),a.token()));}
 @Test void fourPlayersUniqueAndBounded(){var s=session();var ids=new java.util.HashSet<String>();for(int i=1;i<=4;i++){var a=s.createAdmission();var c=s.join(a.sessionId(),a.token());assertEquals(i,c.playerId());assertTrue(ids.add(c.sessionDeviceId()));}assertThrows(IllegalStateException.class,s::createAdmission);assertEquals(4,s.retainedClients());}
 @Test void pendingAdmissionsBoundedAndExpire(){var s=session();for(int i=0;i<4;i++)s.createAdmission();assertThrows(IllegalStateException.class,s::createAdmission);clock.set(30);assertNotNull(s.createAdmission());}
 @Test void exactAdmissionExpiry(){var s=session();var a=s.createAdmission();clock.set(30);assertThrows(IllegalArgumentException.class,()->s.join(a.sessionId(),a.token()));}
 @Test void livenessAndManualReconnectInvalidateOldEpoch(){var s=session();var a=s.createAdmission();var c=s.join(a.sessionId(),a.token());clock.set(3);assertEquals(TechnicalSession.State.DISCONNECTED,s.state(c.playerId()));var n=s.reconnect(c.sessionId(),c.playerId(),c.resumeToken());assertEquals(c.playerId(),n.playerId());assertEquals(c.sessionDeviceId(),n.sessionDeviceId());assertEquals(2,n.epoch());assertNotEquals(c.resumeToken(),n.resumeToken());assertThrows(IllegalArgumentException.class,()->s.heartbeat(c.sessionId(),c.playerId(),c.sessionDeviceId(),c.epoch()));s.heartbeat(n.sessionId(),n.playerId(),n.sessionDeviceId(),n.epoch());}
 @Test void disconnectIsolationAndWrongDevice(){var s=session();var a=s.createAdmission();var c=s.join(a.sessionId(),a.token());var b=s.createAdmission();var d=s.join(b.sessionId(),b.token());assertThrows(IllegalArgumentException.class,()->s.disconnect(c.sessionId(),c.playerId(),d.sessionDeviceId(),c.epoch()));s.disconnect(c.sessionId(),c.playerId(),c.sessionDeviceId(),c.epoch());assertEquals(TechnicalSession.State.CONNECTED,s.state(d.playerId()));assertThrows(IllegalArgumentException.class,()->s.reconnect(c.sessionId(),c.playerId(),"wrong"));}
 @Test void expiredResumeAndShutdown(){var s=session();var a=s.createAdmission();var c=s.join(a.sessionId(),a.token());clock.set(60);assertThrows(IllegalArgumentException.class,()->s.reconnect(c.sessionId(),c.playerId(),c.resumeToken()));assertEquals(0,s.retainedClients());s.close();assertThrows(IllegalStateException.class,s::createAdmission);}
}
