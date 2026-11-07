package com.gorillaescape.server.ipc;

import java.io.IOException;
import java.io.InputStream;
import java.util.concurrent.TimeUnit;
import java.util.function.IntConsumer;
import java.util.concurrent.CountDownLatch;

/** Observe the parent pipe before Spring boot; close the context only after refresh finishes. */
public final class ParentLifetime implements AutoCloseable {
    private final CountDownLatch eof = new CountDownLatch(1);
    private final CountDownLatch closed = new CountDownLatch(1);
    private boolean stopping;
    public ParentLifetime() { this(System.in, 10_000, code -> Runtime.getRuntime().halt(code)); }
    ParentLifetime(InputStream input, long shutdownTimeoutMs, IntConsumer emergencyExit) {
        Thread reader = new Thread(() -> {
            try { while (input.read() != -1) { } }
            catch (IOException ignored) { /* Broken pipe is parent loss. */ }
            synchronized (this) { stopping = true; }
            System.err.println("{\"component\":\"ipc-java\",\"event\":\"STDIN_EOF\"}");
            eof.countDown();
            // Unity can unload its domain before it reaps a stuck native destructor.
            try {
                if (!closed.await(shutdownTimeoutMs, TimeUnit.MILLISECONDS)) {
                    System.err.println("{\"component\":\"ipc-java\",\"event\":\"SHUTDOWN_TIMEOUT\"}");
                    emergencyExit.accept(75);
                }
            } catch (InterruptedException interrupted) { Thread.currentThread().interrupt(); }
        }, "gorilla-ipc-parent");
        reader.setDaemon(true);
        reader.start();
    }
    public synchronized boolean publishIfAlive(Runnable publication) {
        if (stopping) return false;
        publication.run();
        return true;
    }
    @Override public void close() { closed.countDown(); }
    public boolean eofWithin(long milliseconds) throws InterruptedException { return eof.await(milliseconds, TimeUnit.MILLISECONDS); }
    public void awaitEof() throws InterruptedException { eof.await(); }
}
