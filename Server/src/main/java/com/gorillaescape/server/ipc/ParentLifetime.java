package com.gorillaescape.server.ipc;

import java.io.IOException;
import java.util.concurrent.CountDownLatch;

/** Observe the parent pipe before Spring boot; close the context only after refresh finishes. */
public final class ParentLifetime {
    private final CountDownLatch eof = new CountDownLatch(1);
    private boolean stopping;
    public ParentLifetime() {
        Thread reader = new Thread(() -> {
            try { while (System.in.read() != -1) { } }
            catch (IOException ignored) { /* Broken pipe is parent loss. */ }
            synchronized (this) { stopping = true; }
            System.err.println("{\"component\":\"ipc-java\",\"event\":\"STDIN_EOF\"}");
            eof.countDown();
        }, "gorilla-ipc-parent");
        reader.setDaemon(true);
        reader.start();
    }
    public synchronized boolean publishIfAlive(Runnable publication) {
        if (stopping) return false;
        publication.run();
        return true;
    }
    public void awaitEof() throws InterruptedException { eof.await(); }
}
