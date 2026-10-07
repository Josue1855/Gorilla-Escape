package com.gorillaescape.server.ipc;

import java.io.IOException;
import java.nio.channels.FileChannel;
import java.nio.channels.FileLock;
import java.nio.channels.OverlappingFileLockException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.StandardOpenOption;

/** Cooperative singleton for managed children of this user/product. Never delete the lock file. */
public final class ManagedProcessGuard implements AutoCloseable {
    private final FileChannel channel;
    private final FileLock lock;
    public ManagedProcessGuard(Path directory) throws IOException {
        if (!directory.isAbsolute()) throw new IOException("LOCK_UNAVAILABLE");
        Files.createDirectories(directory);
        channel = FileChannel.open(directory.resolve("managed-java.lock"), StandardOpenOption.CREATE, StandardOpenOption.WRITE);
        FileLock acquired;
        try { acquired = channel.tryLock(); }
        catch (OverlappingFileLockException busy) { channel.close(); throw new IOException("ALREADY_RUNNING", busy); }
        catch (IOException failure) { channel.close(); throw failure; }
        if (acquired == null) { channel.close(); throw new IOException("ALREADY_RUNNING"); }
        lock = acquired;
    }
    @Override public void close() throws IOException {
        try { if (lock.isValid()) lock.release(); }
        finally { channel.close(); }
    }
}
