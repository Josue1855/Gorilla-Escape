package com.gorillaescape.server.connectivity;
import java.net.URI;
/** Bootstrap-only boundary; never owns a player, session, or RTC transport. */
public interface Tunnel extends AutoCloseable {
    enum State { STOPPED, STARTING, RUNNING, STOPPING, FAILED }
    URI start(URI ingress, long timeoutMs) throws Exception;
    State state();
    String version();
    boolean cleanupComplete();
    void close();
}
