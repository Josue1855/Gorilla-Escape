package com.gorillaescape.server.mobile;

import com.gorillaescape.server.protocol.*;
import org.junit.jupiter.api.*;
import java.net.URI;
import java.util.*;
import java.util.concurrent.*;
import java.util.concurrent.atomic.AtomicLong;
import static org.junit.jupiter.api.Assertions.*;

class MobileAdmissionConcurrencyTest {

    private final AtomicLong clock = new AtomicLong(1000L);

    private TechnicalSession session() {
        return new TechnicalSession(clock::get, 30_000_000_000L, 5_000_000_000L, 30_000_000_000L);
    }

    @Test
    void fourPlayersJoiningSimultaneously() throws Exception {
        var s = session();
        var admissions = new ArrayList<TechnicalSession.Admission>();
        for (int i = 0; i < 4; i++) {
            admissions.add(s.createAdmission());
        }
        var start = new CountDownLatch(1);
        var pool = Executors.newFixedThreadPool(4);
        try {
            var tasks = new ArrayList<Future<TechnicalSession.Joined>>();
            for (var a : admissions) {
                tasks.add(pool.submit(() -> {
                    start.await();
                    return s.join(a.sessionId(), a.token());
                }));
            }
            start.countDown();
            var playerIds = new HashSet<Integer>();
            var deviceIds = new HashSet<String>();
            for (var task : tasks) {
                var joined = task.get(5, TimeUnit.SECONDS);
                assertNotNull(joined);
                assertTrue(playerIds.add(joined.playerId()));
                assertTrue(deviceIds.add(joined.sessionDeviceId()));
            }
            assertEquals(Set.of(1, 2, 3, 4), playerIds);
            assertEquals(4, s.retainedClients());
        } finally {
            pool.shutdownNow();
            assertTrue(pool.awaitTermination(5, TimeUnit.SECONDS));
        }
    }

    @Test
    void twoPlayersScanningSameQr() throws Exception {
        var s = session();
        var a = s.createAdmission();
        var start = new CountDownLatch(1);
        var pool = Executors.newFixedThreadPool(2);
        try {
            var tasks = new ArrayList<Future<Boolean>>();
            for (int i = 0; i < 2; i++) {
                tasks.add(pool.submit(() -> {
                    start.await();
                    try {
                        s.join(a.sessionId(), a.token());
                        return true;
                    } catch (IllegalArgumentException rejected) {
                        assertEquals("ADMISSION_EXPIRED_OR_USED", s.admissionFailure(a.sessionId(), a.token()));
                        return false;
                    }
                }));
            }
            start.countDown();
            int accepted = 0;
            int rejected = 0;
            for (var t : tasks) {
                if (t.get(5, TimeUnit.SECONDS)) accepted++;
                else rejected++;
            }
            assertEquals(1, accepted, "Exactamente 1 jugador debe ser admitido con el QR");
            assertEquals(1, rejected, "El segundo jugador debe ser rechazado por reuso");
            assertEquals(1, s.retainedClients());
        } finally {
            pool.shutdownNow();
            assertTrue(pool.awaitTermination(5, TimeUnit.SECONDS));
        }
    }

    @Test
    void pendingAdmissionsExpireAndReleaseSlots() {
        var s = session();
        var a = s.createAdmission();
        assertEquals("AVAILABLE", s.admissionState(a));
        // Clock advances beyond TTL (30 seconds)
        clock.addAndGet(30_000_000_001L);
        assertEquals("EXPIRED", s.admissionState(a));
        assertThrows(IllegalArgumentException.class, () -> s.join(a.sessionId(), a.token()));
        // Slot is released; a new admission can be minted
        var fresh = s.createAdmission();
        assertEquals("AVAILABLE", s.admissionState(fresh));
        var joined = s.join(fresh.sessionId(), fresh.token());
        assertEquals(1, joined.playerId());
    }

    @Test
    void retriesAfterFailureSucceedWithNewAdmission() {
        var s = session();
        var a = s.createAdmission();
        // Client joins
        var c1 = s.join(a.sessionId(), a.token());
        assertEquals(1, c1.playerId());
        // Another attempt with same token fails
        assertThrows(IllegalArgumentException.class, () -> s.join(a.sessionId(), a.token()));
        // Retry with a freshly issued token succeeds
        var retryAdmission = s.createAdmission();
        var c2 = s.join(retryAdmission.sessionId(), retryAdmission.token());
        assertEquals(2, c2.playerId());
    }

    @Test
    void disconnectFollowedByReconnectPreservesSlot() {
        var s = session();
        var a = s.createAdmission();
        var joined = s.join(a.sessionId(), a.token());
        assertEquals(1, joined.playerId());
        assertEquals(1, joined.epoch());

        // Disconnect
        s.disconnect(joined.sessionId(), joined.playerId(), joined.sessionDeviceId(), joined.epoch());
        assertEquals(TechnicalSession.State.DISCONNECTED, s.state(joined.playerId()));

        // Reconnect with valid resumeToken
        var resumed = s.reconnect(joined.sessionId(), joined.playerId(), joined.resumeToken());
        assertEquals(1, resumed.playerId());
        assertEquals(2, resumed.epoch(), "Epoch must increment on reconnect");
        assertEquals(joined.sessionDeviceId(), resumed.sessionDeviceId());
        assertNotEquals(joined.resumeToken(), resumed.resumeToken(), "Resume token must rotate");
        assertEquals(TechnicalSession.State.CONNECTED, s.state(joined.playerId()));
    }

    @Test
    void fifthPlayerAttemptBlockedAtCapacity() {
        var s = session();
        for (int i = 0; i < 4; i++) {
            var a = s.createAdmission();
            s.join(a.sessionId(), a.token());
        }
        assertEquals(4, s.retainedClients());
        // 5th admission request throws SESSION_FULL
        var ex = assertThrows(IllegalStateException.class, s::createAdmission);
        assertEquals("SESSION_FULL", ex.getMessage());
    }

    @Test
    void twoConcurrentAdmissionRequestsDoNotInvalidateEachOther() throws Exception {
        var s = session();
        var start = new CountDownLatch(1);
        var pool = Executors.newFixedThreadPool(2);
        try {
            var t1 = pool.submit(() -> { start.await(); return s.createAdmission(); });
            var t2 = pool.submit(() -> { start.await(); return s.createAdmission(); });
            start.countDown();
            var a1 = t1.get(5, TimeUnit.SECONDS);
            var a2 = t2.get(5, TimeUnit.SECONDS);
            assertNotNull(a1);
            assertNotNull(a2);
            assertNotEquals(a1.token(), a2.token(), "Cada admisión debe tener un token único");

            // Both must be valid and consumable
            var j1 = s.join(a1.sessionId(), a1.token());
            var j2 = s.join(a2.sessionId(), a2.token());
            assertNotNull(j1);
            assertNotNull(j2);
            assertNotEquals(j1.playerId(), j2.playerId());
            assertEquals(2, s.retainedClients());
        } finally {
            pool.shutdownNow();
            assertTrue(pool.awaitTermination(5, TimeUnit.SECONDS));
        }
    }

    @Test
    void revokingOneAdmissionDoesNotAffectOtherValidAdmissions() {
        var s = session();
        var a1 = s.createAdmission();
        var a2 = s.createAdmission();
        var a3 = s.createAdmission();

        // Revoke only a2
        s.revokeAdmission(a2.token());
        assertEquals("AVAILABLE", s.admissionState(a1));
        assertThrows(IllegalArgumentException.class, () -> s.join(a2.sessionId(), a2.token()));
        assertEquals("AVAILABLE", s.admissionState(a3));

        // a1 and a3 can still join
        var j1 = s.join(a1.sessionId(), a1.token());
        var j3 = s.join(a3.sessionId(), a3.token());
        assertEquals(1, j1.playerId());
        assertEquals(2, j3.playerId());
        assertEquals(2, s.retainedClients());
    }

    @Test
    void absenceOfDuplicateSlotsAndSessionsUnderHeavyConcurrency() throws Exception {
        var s = session();
        var pool = Executors.newFixedThreadPool(16);
        var start = new CountDownLatch(1);
        try {
            var tasks = new ArrayList<Future<TechnicalSession.Joined>>();
            for (int i = 0; i < 16; i++) {
                tasks.add(pool.submit(() -> {
                    start.await();
                    try {
                        var a = s.createAdmission();
                        return s.join(a.sessionId(), a.token());
                    } catch (IllegalStateException full) {
                        return null; // Capacidad alcanzada
                    }
                }));
            }
            start.countDown();
            var acceptedPids = new HashSet<Integer>();
            var acceptedDevices = new HashSet<String>();
            int count = 0;
            for (var t : tasks) {
                var joined = t.get(5, TimeUnit.SECONDS);
                if (joined != null) {
                    count++;
                    assertTrue(acceptedPids.add(joined.playerId()), "PlayerId duplicado detectado!");
                    assertTrue(acceptedDevices.add(joined.sessionDeviceId()), "DeviceId duplicado detectado!");
                }
            }
            assertEquals(4, count, "Exactamente 4 jugadores deben ser admitidos");
            assertEquals(4, s.retainedClients());
            assertEquals(Set.of(1, 2, 3, 4), acceptedPids);
        } finally {
            pool.shutdownNow();
            assertTrue(pool.awaitTermination(5, TimeUnit.SECONDS));
        }
    }

    @Test
    void standingQaAdmissionReleasedWhenCapacityNeeded() {
        var s = session();
        // 3 players connect
        for (int i = 0; i < 3; i++) {
            var a = s.createAdmission();
            s.join(a.sessionId(), a.token());
        }
        assertEquals(3, s.retainedClients());

        // Background QaAdmission prepares a TV QR code
        var qa = new QaAdmission();
        var view = qa.view(s, URI.create("http://127.0.0.1:8080/"), true, true);
        assertEquals("AVAILABLE", view.state());
        assertTrue(qa.hasCurrent());

        // Now session has 3 clients + 1 standing QA admission = 4 total reservations
        // A direct session.createAdmission() would throw SESSION_FULL
        assertThrows(IllegalStateException.class, s::createAdmission);

        // Releasing the standing QaAdmission frees up the slot without touching clients
        qa.release(s);
        assertFalse(qa.hasCurrent());
        assertEquals(3, s.retainedClients());

        // Now the 4th admission can be minted and joined!
        var a4 = s.createAdmission();
        assertNotNull(a4);
        var j4 = s.join(a4.sessionId(), a4.token());
        assertEquals(4, j4.playerId());
        assertEquals(4, s.retainedClients());
    }
}
