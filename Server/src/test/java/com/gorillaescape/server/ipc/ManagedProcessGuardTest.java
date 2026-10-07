package com.gorillaescape.server.ipc;

import java.io.IOException;
import java.nio.file.Files;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;
import java.nio.file.Path;
import static org.junit.jupiter.api.Assertions.*;

class ManagedProcessGuardTest {
    @TempDir Path directory;
    @Test void staleFileCanBeLockedAndReusedWithoutDeletion() throws Exception {
        Files.writeString(directory.resolve("managed-java.lock"), "stale marker");
        try (var first = new ManagedProcessGuard(directory)) {
            assertEquals("ALREADY_RUNNING",assertThrows(IOException.class,()->new ManagedProcessGuard(directory)).getMessage());
        }
        assertTrue(Files.exists(directory.resolve("managed-java.lock")));
        try (var next = new ManagedProcessGuard(directory)) { assertNotNull(next); }
    }
    @Test void relativePathIsRejected() { assertThrows(IOException.class,()->new ManagedProcessGuard(Path.of("relative"))); }
}
