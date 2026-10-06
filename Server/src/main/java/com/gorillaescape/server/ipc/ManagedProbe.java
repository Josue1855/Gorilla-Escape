package com.gorillaescape.server.ipc;

import java.io.*;
import java.net.*;
import java.util.UUID;
import java.util.concurrent.atomic.AtomicBoolean;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.boot.context.event.ApplicationReadyEvent;
import org.springframework.boot.web.server.context.WebServerApplicationContext;
import org.springframework.context.ConfigurableApplicationContext;
import org.springframework.context.event.EventListener;
import org.springframework.stereotype.Component;
import org.springframework.beans.factory.DisposableBean;

/** Opt-in child-process probe; normal HTTP startup remains unchanged. */
@Component
@ConditionalOnProperty(name = "gorilla.ipc.managed", havingValue = "true")
public final class ManagedProbe implements DisposableBean {
    private final AtomicBoolean running = new AtomicBoolean();
    private ServerSocket listener;
    private volatile Socket client;
    private String instance;
    private String token;
    @EventListener
    public void ready(ApplicationReadyEvent event) throws IOException {
        instance = System.getenv("GORILLA_IPC_INSTANCE");
        token = System.getenv("GORILLA_IPC_TOKEN");
        if (instance == null || !UUID.fromString(instance).toString().equals(instance)
                || token == null || !token.matches("[A-Za-z0-9_-]{43}")) throw new IOException("IPC_LAUNCH_CONFIG");
        listener = new ServerSocket();
        listener.bind(new InetSocketAddress(InetAddress.getByName("127.0.0.1"), 0), 2);
        running.set(true);
        daemon("gorilla-ipc-accept", this::accept);
        ConfigurableApplicationContext context = event.getApplicationContext();
        daemon("gorilla-ipc-parent", () -> {
            try { while (System.in.read() != -1) { /* EOF owns shutdown; no wire messages here. */ } }
            catch (IOException ignored) { /* Lost pipe is equivalent to lost parent. */ }
            log("STDIN_EOF");
            context.close();
        });
        int httpPort = ((WebServerApplicationContext) context).getWebServer().getPort();
        System.out.println("GORILLA_IPC_READY {\"ipcVersion\":1,\"instanceId\":\"" + instance
                + "\",\"pid\":" + ProcessHandle.current().pid() + ",\"ipcPort\":" + listener.getLocalPort()
                + ",\"httpPort\":" + httpPort + "}");
        System.out.flush();
        log("READY");
    }
    private void accept() {
        while (running.get()) {
            try (Socket socket = listener.accept()) {
                client = socket;
                socket.setSoTimeout(2000);
                socket.setTcpNoDelay(true);
                String connection = null;
                int sequence = 1;
                while (running.get()) {
                    var ping = ProbeCodec.ping(ProbeCodec.read(socket.getInputStream()), instance, connection,
                            sequence, connection == null ? token : null);
                    connection = ping.connectionId();
                    // Tiny bounded response. Independent timeout closes a stalled write.
                    final var output = socket.getOutputStream();
                    var write = java.util.concurrent.CompletableFuture.runAsync(() -> {
                        try { ProbeCodec.write(output, ProbeCodec.pong(ping)); }
                        catch (IOException e) { throw new java.util.concurrent.CompletionException(e); }
                    });
                    try { write.get(2, java.util.concurrent.TimeUnit.SECONDS); }
                    catch (Exception e) { socket.close(); throw new IOException("IPC_WRITE", e); }
                    if (sequence == Integer.MAX_VALUE) break;
                    sequence++;
                }
            } catch (EOFException ignored) { log("CLIENT_EOF"); }
            catch (IOException e) { if (running.get()) log("CONNECTION_CLOSED"); }
            finally { client = null; }
        }
    }
    private static void daemon(String name, Runnable action) {
        Thread thread = new Thread(action, name); thread.setDaemon(true); thread.start();
    }
    private void log(String code) {
        System.err.println("{\"component\":\"ipc-java\",\"event\":\"" + code + "\",\"instanceId\":\"" + instance + "\"}");
    }
    @Override public void destroy() throws IOException {
        if (!running.getAndSet(false)) return;
        if (listener != null) listener.close();
        Socket socket = client;
        if (socket != null) socket.close();
        token = null;
        log("SHUTDOWN");
    }
}
