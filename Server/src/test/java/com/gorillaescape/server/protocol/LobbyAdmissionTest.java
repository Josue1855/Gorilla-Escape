package com.gorillaescape.server.protocol;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.params.ParameterizedTest;
import org.junit.jupiter.params.provider.ValueSource;
import java.util.*;
import java.util.concurrent.*;
import java.util.concurrent.atomic.AtomicLong;
import static org.junit.jupiter.api.Assertions.*;

class LobbyAdmissionTest {
    final AtomicLong clock=new AtomicLong();
    TechnicalSession session(){return new TechnicalSession(clock::get,30_000_000_000L,5_000_000_000L,30_000_000_000L);}
    TechnicalSession.Joined join(TechnicalSession s){var a=s.createAdmission();return s.join(a.sessionId(),a.token());}
    void network(TechnicalSession s,TechnicalSession.Joined c){s.networkReady(c.sessionId(),c.playerId(),c.sessionDeviceId(),c.epoch());}
    void input(TechnicalSession s,TechnicalSession.Joined c){s.inputReady(c.sessionId(),c.playerId(),c.sessionDeviceId(),c.epoch(),true);}
    void ready(TechnicalSession s,TechnicalSession.Joined c){network(s,c);input(s,c);s.unityPlayerReady(c.sessionId(),c.playerId(),c.sessionDeviceId(),c.epoch(),true);}
    @ParameterizedTest @ValueSource(ints={1,2,3,4}) void oneToFourDistinctAssociations(int count){
        var s=session();var devices=new HashSet<String>();for(int i=1;i<=count;i++){var c=join(s);assertEquals(i,c.playerId());assertTrue(devices.add(c.sessionDeviceId()));}
        assertEquals(count,s.lobby().players().size());assertTrue(s.lobby().players().stream().noneMatch(TechnicalSession.LobbyPlayer::playerReady));
        if(count==4)assertThrows(IllegalStateException.class,s::createAdmission);
    }
    @Test void unityStartClosesBothIssuanceAndAlreadyIssuedAdmission(){
        var s=session();var c=join(s);var pending=s.createAdmission();ready(s,c);
        s.unityPhase(c.sessionId(),TechnicalSession.LobbyPhase.STARTING);
        assertThrows(IllegalStateException.class,s::createAdmission);
        assertThrows(IllegalStateException.class,()->s.join(pending.sessionId(),pending.token()));
        s.unityPhase(c.sessionId(),TechnicalSession.LobbyPhase.STARTED);
        assertThrows(IllegalStateException.class,()->s.join(pending.sessionId(),pending.token()));
        s.unityPhase(c.sessionId(),TechnicalSession.LobbyPhase.STARTED); // safe duplicate from Unity
        assertThrows(IllegalStateException.class,()->s.unityPhase(c.sessionId(),TechnicalSession.LobbyPhase.OPEN));
    }
    @Test void noImplicitReadyFromNetworkOrRawInput(){
        var s=session();var c=join(s);
        assertThrows(IllegalStateException.class,()->s.unityPlayerReady(c.sessionId(),1,c.sessionDeviceId(),1,true));
        network(s,c);assertTrue(s.lobby().players().getFirst().networkReady());assertFalse(s.lobby().players().getFirst().inputReady());
        assertThrows(IllegalStateException.class,()->s.unityPlayerReady(c.sessionId(),1,c.sessionDeviceId(),1,true));
        input(s,c);assertTrue(s.lobby().players().getFirst().inputReady());assertFalse(s.lobby().players().getFirst().playerReady());
        s.unityPlayerReady(c.sessionId(),1,c.sessionDeviceId(),1,true);assertTrue(s.lobby().players().getFirst().playerReady());
    }
    @Test void startRequiresPlayersAllReadyAndValidOrder(){
        var s=session();assertThrows(IllegalStateException.class,()->s.unityPhase(s.lobby().sessionId(),TechnicalSession.LobbyPhase.STARTING));
        var a=join(s);ready(s,a);var b=join(s);
        assertThrows(IllegalStateException.class,()->s.unityPhase(a.sessionId(),TechnicalSession.LobbyPhase.STARTING));
        ready(s,b);assertThrows(IllegalStateException.class,()->s.unityPhase(a.sessionId(),TechnicalSession.LobbyPhase.STARTED));
        assertThrows(IllegalArgumentException.class,()->s.unityPhase(UUID.randomUUID().toString(),TechnicalSession.LobbyPhase.STARTING));
        s.unityPhase(a.sessionId(),TechnicalSession.LobbyPhase.STARTING);assertEquals("STARTING",s.lobby().phase());
    }
    @Test void inputStaleUnavailableSuspendedAndAuthorityLossRevokeReady(){
        var s=session();var c=join(s);ready(s,c);clock.set(2_000_000_000L);
        assertFalse(s.lobby().players().getFirst().inputReady());assertFalse(s.lobby().players().getFirst().playerReady());
        ready(s,c);s.inputReady(c.sessionId(),1,c.sessionDeviceId(),1,false);assertFalse(s.lobby().players().getFirst().playerReady());
        ready(s,c);s.suspendInput(c.sessionId(),1,c.sessionDeviceId(),1);assertFalse(s.lobby().players().getFirst().playerReady());
        ready(s,c);s.unityDisconnected();assertFalse(s.lobby().players().getFirst().playerReady());
    }
    @Test void disconnectBeforeStartRetainsSlotAndReconnectResetsReadiness(){
        var s=session();var c=join(s);ready(s,c);s.disconnect(c.sessionId(),1,c.sessionDeviceId(),1);
        assertFalse(s.lobby().players().getFirst().networkReady());assertFalse(s.lobby().players().getFirst().playerReady());
        assertEquals(2,join(s).playerId());var n=s.reconnect(c.sessionId(),1,c.resumeToken());
        assertEquals(c.playerId(),n.playerId());assertEquals(c.sessionDeviceId(),n.sessionDeviceId());assertEquals(2,n.epoch());
        assertNotEquals(c.resumeToken(),n.resumeToken());assertFalse(s.lobby().players().getFirst().inputReady());
        assertThrows(IllegalArgumentException.class,()->s.unityPlayerReady(c.sessionId(),1,c.sessionDeviceId(),1,true));
    }
    @Test void legitimateResumeAfterStartButNoNewAdmissionEvenAfterSlotExpires(){
        var s=session();var c=join(s);ready(s,c);s.unityPhase(c.sessionId(),TechnicalSession.LobbyPhase.STARTING);s.unityPhase(c.sessionId(),TechnicalSession.LobbyPhase.STARTED);
        s.disconnect(c.sessionId(),1,c.sessionDeviceId(),1);var n=s.reconnect(c.sessionId(),1,c.resumeToken());assertEquals(1,n.playerId());
        assertEquals("STARTED",s.lobby().phase());assertThrows(IllegalStateException.class,s::createAdmission);
        clock.set(30_000_000_000L);assertEquals(0,s.retainedClients());assertThrows(IllegalStateException.class,s::createAdmission);
    }
    @Test void expiryReplayWrongSessionAndWrongIdentityAreRejected(){
        var s=session();var a=s.createAdmission();assertThrows(IllegalArgumentException.class,()->s.join("wrong",a.token()));var c=s.join(a.sessionId(),a.token());
        assertThrows(IllegalArgumentException.class,()->s.join(a.sessionId(),a.token()));
        var expired=s.createAdmission();clock.set(30_000_000_000L);assertThrows(IllegalArgumentException.class,()->s.join(expired.sessionId(),expired.token()));
        var d=join(s);network(s,d);input(s,d);
        assertThrows(IllegalArgumentException.class,()->s.unityPlayerReady(d.sessionId(),4,d.sessionDeviceId(),d.epoch(),true));
        assertThrows(IllegalArgumentException.class,()->s.unityPlayerReady(d.sessionId(),d.playerId(),c.sessionDeviceId(),d.epoch(),true));
        s.disconnect(d.sessionId(),d.playerId(),d.sessionDeviceId(),d.epoch());
        assertThrows(IllegalArgumentException.class,()->s.reconnect(d.sessionId(),d.playerId(),c.resumeToken()));
        var n=s.reconnect(d.sessionId(),d.playerId(),d.resumeToken());s.disconnect(n.sessionId(),n.playerId(),n.sessionDeviceId(),n.epoch());
        assertThrows(IllegalArgumentException.class,()->s.reconnect(d.sessionId(),d.playerId(),d.resumeToken()));
    }
    @Test void sixteenConcurrentJoinAttemptsNeverOwnFiveSlots()throws Exception{
        var s=session();var start=new CountDownLatch(1);var pool=Executors.newFixedThreadPool(16);
        try{var tasks=new ArrayList<Future<TechnicalSession.Joined>>();for(int i=0;i<16;i++)tasks.add(pool.submit(()->{start.await();try{return join(s);}catch(IllegalStateException rejected){return null;}}));
            start.countDown();var ids=new HashSet<Integer>();var devices=new HashSet<String>();int accepted=0;
            for(var task:tasks){var c=task.get(5,TimeUnit.SECONDS);if(c!=null){accepted++;assertTrue(ids.add(c.playerId()));assertTrue(devices.add(c.sessionDeviceId()));}}
            assertEquals(4,accepted);assertEquals(4,s.retainedClients());
        }finally{pool.shutdownNow();assertTrue(pool.awaitTermination(5,TimeUnit.SECONDS));}
    }
    @Test void concurrentReplayConsumesAdmissionExactlyOnce()throws Exception{
        var s=session();var a=s.createAdmission();var start=new CountDownLatch(1);var pool=Executors.newFixedThreadPool(8);
        try{var tasks=new ArrayList<Future<Boolean>>();for(int i=0;i<8;i++)tasks.add(pool.submit(()->{start.await();try{s.join(a.sessionId(),a.token());return true;}catch(IllegalArgumentException rejected){return false;}}));start.countDown();int accepted=0;
            for(var task:tasks)if(task.get(5,TimeUnit.SECONDS))accepted++;assertEquals(1,accepted);assertEquals(1,s.retainedClients());
        }finally{pool.shutdownNow();assertTrue(pool.awaitTermination(5,TimeUnit.SECONDS));}
    }
    @Test void joinRacingUnityStartIsAtomic()throws Exception{
        for(int trial=0;trial<30;trial++){
            var s=session();var c=join(s);ready(s,c);var a=s.createAdmission();var start=new CountDownLatch(1);var pool=Executors.newFixedThreadPool(2);
            try{var joining=pool.submit(()->{start.await();try{s.join(a.sessionId(),a.token());return true;}catch(IllegalStateException rejected){return false;}});
                var starting=pool.submit(()->{start.await();try{s.unityPhase(c.sessionId(),TechnicalSession.LobbyPhase.STARTING);return true;}catch(IllegalStateException rejected){return false;}});
                start.countDown();boolean admitted=joining.get(5,TimeUnit.SECONDS),started=starting.get(5,TimeUnit.SECONDS);
                assertNotEquals(admitted,started);assertEquals(started?1:2,s.retainedClients());assertEquals(started?"STARTING":"OPEN",s.lobby().phase());
            }finally{pool.shutdownNow();assertTrue(pool.awaitTermination(5,TimeUnit.SECONDS));}
        }
    }
    @Test void disconnectBetweenStartingAndStartedPreventsStart(){
        var s=session();var c=join(s);ready(s,c);s.unityPhase(c.sessionId(),TechnicalSession.LobbyPhase.STARTING);s.disconnect(c.sessionId(),1,c.sessionDeviceId(),1);
        assertThrows(IllegalStateException.class,()->s.unityPhase(c.sessionId(),TechnicalSession.LobbyPhase.STARTED));assertThrows(IllegalStateException.class,s::createAdmission);
        var resumed=s.reconnect(c.sessionId(),1,c.resumeToken());ready(s,resumed);s.unityPhase(c.sessionId(),TechnicalSession.LobbyPhase.STARTED);assertEquals("STARTED",s.lobby().phase());
    }
    @Test void snapshotRemainsCoherentAtExactInputExpiryBoundary(){
        var step=new AtomicLong();var s=new TechnicalSession(()->clock.getAndAdd(step.get()),30_000_000_000L,5_000_000_000L,30_000_000_000L);
        var c=join(s);ready(s,c);clock.set(1_999_999_998L);step.set(1);
        var p=s.lobby().players().getFirst();assertFalse(p.inputReady());assertFalse(p.playerReady());
    }
    @Test void cleanupContainsNoResumeOrAdmissionSecrets(){
        var s=session();var c=join(s);String snapshot=GorillaProtocol.JSON.writeValueAsString(s.lobby());assertFalse(snapshot.contains(c.resumeToken()));assertFalse(snapshot.contains("admission"));
        s.close();assertThrows(IllegalStateException.class,s::lobby);assertThrows(IllegalStateException.class,()->s.reconnect(c.sessionId(),1,c.resumeToken()));
    }
}
