package com.gorillaescape.server.ipc;
import java.io.ByteArrayInputStream;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.TimeUnit;
import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;
class ParentLifetimeTest {
    @Test void stuckShutdownCannotKeepManagedServerLockedForever() throws Exception {
        var forced = new CountDownLatch(1);
        try (var lifetime = new ParentLifetime(new ByteArrayInputStream(new byte[0]), 50, code -> { if(code == 75) forced.countDown(); })) {
            lifetime.awaitEof();
            assertTrue(forced.await(2, TimeUnit.SECONDS));
            assertFalse(lifetime.publishIfAlive(() -> fail("must not publish after parent loss")));
        }
    }
    @Test void completedCleanupCancelsEmergencyExit() throws Exception {
        var forced = new CountDownLatch(1);
        var lifetime = new ParentLifetime(new ByteArrayInputStream(new byte[0]), 50, code -> forced.countDown());
        lifetime.awaitEof(); lifetime.close();
        assertFalse(forced.await(150, TimeUnit.MILLISECONDS));
    }
}
