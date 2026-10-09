package com.gorillaescape.server.ipc;

import java.io.*;
import java.net.*;
import java.util.UUID;
import com.gorillaescape.server.GorillaEscapeApplication;
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
    @org.springframework.beans.factory.annotation.Autowired(required=false)
    private com.gorillaescape.server.mobile.MobileRuntime mobile;
    private final AtomicBoolean running = new AtomicBoolean();
    private ServerSocket listener;
    private volatile Socket client;
    private Thread worker;
    private final java.util.concurrent.ScheduledThreadPoolExecutor deadlines = new java.util.concurrent.ScheduledThreadPoolExecutor(1, r -> {
        Thread t = new Thread(r, "gorilla-ipc-deadline"); t.setDaemon(true); return t;
    });
    private long validFrames, closedConnections;
    private int maximumPendingDeadlines;
    private String instance;
    private String token;
    public ManagedProbe() { deadlines.setRemoveOnCancelPolicy(true); }
    @EventListener
    public void ready(ApplicationReadyEvent event) throws IOException {
        instance = System.getenv("GORILLA_IPC_INSTANCE");
        token = System.getenv("GORILLA_IPC_TOKEN");
        if (instance == null || !UUID.fromString(instance).toString().equals(instance)
                || token == null || !token.matches("[A-Za-z0-9_-]{43}")) throw new IOException("IPC_LAUNCH_CONFIG");
        listener = new ServerSocket();
        listener.bind(new InetSocketAddress(InetAddress.getByName("127.0.0.1"), 0), 2);
        running.set(true);
        worker = daemon("gorilla-ipc-accept", this::accept);
        ConfigurableApplicationContext context = event.getApplicationContext();
        int httpPort = ((WebServerApplicationContext) context).getWebServer().getPort();
        ParentLifetime parent = GorillaEscapeApplication.managedParent();
        if (parent == null) throw new IOException("IPC_PARENT_MISSING");
        parent.publishIfAlive(() -> {
            System.out.println("GORILLA_IPC_READY {\"ipcVersion\":1,\"instanceId\":\"" + instance
                    + "\",\"pid\":" + ProcessHandle.current().pid() + ",\"ipcPort\":" + listener.getLocalPort()
                    + ",\"httpPort\":" + httpPort + "}");
            System.out.flush();
            log("READY");
        });
    }
    private void accept() {
        while (running.get()) {
            boolean lobby=false;
            try (Socket socket = listener.accept()) {
                client = socket;
                socket.setSoTimeout(2000);
                socket.setTcpNoDelay(true);
                String connection = null;
                int sequence = 1; boolean phone=false;
                while (running.get()) {
                    String body=readFrame(socket);
                    tools.jackson.databind.JsonNode lobbyCommand=null;
                    var extension=com.gorillaescape.server.protocol.GorillaProtocol.JSON.readTree(body);
                    var extendedPayload=extension.get("payload");
                    boolean negotiateLobby=false;
                    if(extendedPayload!=null&&extendedPayload.has("lobbyVersion")) {
                        if(connection!=null||mobile==null||!extendedPayload.get("lobbyVersion").isInt()||extendedPayload.get("lobbyVersion").intValue()!=1||!extendedPayload.has("phoneInputVersion"))throw new IOException("IPC_LOBBY_VERSION");
                        ((tools.jackson.databind.node.ObjectNode)extendedPayload).remove("lobbyVersion");negotiateLobby=true;
                    }
                    if(extendedPayload!=null&&extendedPayload.has("lobbyCommand")) {
                        if(!lobby||connection==null)throw new IOException("IPC_LOBBY_AUTHORITY");
                        lobbyCommand=((tools.jackson.databind.node.ObjectNode)extendedPayload).remove("lobbyCommand");
                    }
                    body=com.gorillaescape.server.protocol.GorillaProtocol.JSON.writeValueAsString(extension);
                    if(connection==null && mobile!=null) {
                        var root=com.gorillaescape.server.protocol.GorillaProtocol.JSON.readTree(body);
                        var payload=root.get("payload");
                        if(payload!=null && payload.has("phoneInputVersion")) {
                            if(!payload.get("phoneInputVersion").isInt()||payload.get("phoneInputVersion").intValue()!=1)throw new IOException("IPC_CONTRACT");
                            ((tools.jackson.databind.node.ObjectNode)payload).remove("phoneInputVersion");
                            body=com.gorillaescape.server.protocol.GorillaProtocol.JSON.writeValueAsString(root);phone=true;
                        }
                    }
                    var ping = ProbeCodec.ping(body, instance, connection,
                            sequence, connection == null ? token : null);
                    connection = ping.connectionId();
                    // Extensions execute only AFTER instance/token/connection/sequence validation.
                    if(negotiateLobby)lobby=true;
                    String pong=ProbeCodec.pong(ping);
                    if(phone){var root=(tools.jackson.databind.node.ObjectNode)com.gorillaescape.server.protocol.GorillaProtocol.JSON.readTree(pong);
                        var pongPayload=(tools.jackson.databind.node.ObjectNode)root.get("payload");
                        if(lobby){
                            pongPayload.put("hasLobbyResult",lobbyCommand!=null);
                            if(lobbyCommand!=null)pongPayload.set("lobbyResult",com.gorillaescape.server.protocol.GorillaProtocol.JSON.valueToTree(mobile.unityLobbyCommand(lobbyCommand)));
                            pongPayload.set("lobby",com.gorillaescape.server.protocol.GorillaProtocol.JSON.valueToTree(mobile.lobby()));
                        }
                        int overhead=com.gorillaescape.server.protocol.GorillaProtocol.JSON.writeValueAsBytes(root).length;
                        pongPayload.set("phoneInputs",mobile.inputs.drain(Math.min(3500,ProbeCodec.MAX_FRAME-overhead-20)));
                        pong=com.gorillaescape.server.protocol.GorillaProtocol.JSON.writeValueAsString(root);}
                    writeFrame(socket,pong);
                    validFrames++;
                    if (sequence == Integer.MAX_VALUE) break;
                    sequence++;
                }
            } catch (EOFException ignored) { log("CLIENT_EOF"); }
            catch (IOException e) { if (running.get()) log("CONNECTION_CLOSED", reason(e)); }
            catch (RuntimeException e) { if (running.get()) log("CONNECTION_CLOSED", "IPC_CONTRACT"); }
            finally { if(lobby&&mobile!=null)mobile.unityDisconnected();client = null; closedConnections++; }
        }
    }
    // Package scope solely for deterministic real-socket deadline tests.
    void writeFrame(Socket socket, String body) throws IOException {
        var expired = new AtomicBoolean();
        var deadline = deadlines.schedule(() -> {
            expired.set(true);
            try { socket.close(); } catch (IOException ignored) { }
        }, 2, java.util.concurrent.TimeUnit.SECONDS);
        maximumPendingDeadlines = Math.max(maximumPendingDeadlines, deadlines.getQueue().size());
        try {
            ProbeCodec.write(socket.getOutputStream(), body);
            if (expired.get()) throw new IOException("IPC_WRITE_TIMEOUT");
        } catch (IOException error) {
            if (expired.get()) throw new IOException("IPC_WRITE_TIMEOUT");
            throw error;
        } finally { deadline.cancel(false); }
    }
    int pendingDeadlines() { return deadlines.getQueue().size(); }
    private static String readFrame(Socket socket) throws IOException {
        long end = System.nanoTime() + java.util.concurrent.TimeUnit.SECONDS.toNanos(2);
        var input = new FilterInputStream(socket.getInputStream()) {
            private void remaining() throws IOException {
                long left = end - System.nanoTime();
                if (left <= 0) throw new SocketTimeoutException("IPC_READ_TIMEOUT");
                socket.setSoTimeout((int)Math.max(1, java.util.concurrent.TimeUnit.NANOSECONDS.toMillis(left)));
            }
            @Override public int read() throws IOException { remaining(); return in.read(); }
            @Override public int read(byte[] b, int off, int len) throws IOException { remaining(); return in.read(b, off, len); }
        };
        return ProbeCodec.read(input);
    }
    private static String reason(IOException error) {
        if (error instanceof SocketTimeoutException) return "READ_TIMEOUT";
        String code = error.getMessage();
        // Never log parser exception text or input. Only a fixed vocabulary is public.
        return code != null && java.util.Set.of("IPC_FRAME_SIZE", "IPC_UTF8", "IPC_FIELDS", "IPC_CONTRACT", "IPC_IDENTITY", "IPC_TOKEN", "IPC_JSON_INVALID", "IPC_WRITE_TIMEOUT").contains(code)
                ? code.substring(4) : "JSON_OR_IO_INVALID";
    }
    private static Thread daemon(String name, Runnable action) {
        Thread thread = new Thread(action, name); thread.setDaemon(true); thread.start(); return thread;
    }
    private void log(String code) { log(code, "NONE"); }
    private void log(String code, String reason) {
        System.err.println("{\"component\":\"ipc-java\",\"event\":\"" + code + "\",\"reason\":\"" + reason + "\",\"instanceId\":\"" + instance + "\"}");
    }
    @Override public void destroy() throws IOException {
        running.set(false);
        IOException failure = null;
        try { if (listener != null) listener.close(); }
        catch (IOException error) { failure = error; }
        Socket socket = client;
        try { if (socket != null) socket.close(); }
        catch (IOException error) { if (failure == null) failure = error; else failure.addSuppressed(error); }
        deadlines.shutdownNow();
        if (worker != null && worker != Thread.currentThread()) {
            try { worker.join(2000); }
            catch (InterruptedException interrupted) { Thread.currentThread().interrupt(); }
            if (worker.isAlive()) failure = new IOException("IPC_WORKER_RESIDUAL");
        }
        try { if (!deadlines.awaitTermination(2, java.util.concurrent.TimeUnit.SECONDS)) throw new IOException("IPC_DEADLINE_RESIDUAL"); }
        catch (InterruptedException e) { Thread.currentThread().interrupt(); throw new IOException("IPC_DEADLINE_INTERRUPTED"); }
        System.err.println("{\"component\":\"ipc-java\",\"event\":\"RESOURCE_SUMMARY\",\"validFrames\":" + validFrames
                + ",\"closedConnections\":" + closedConnections + ",\"maximumPendingDeadlines\":" + maximumPendingDeadlines
                + ",\"pendingDeadlines\":" + deadlines.getQueue().size() + ",\"deadlineTerminated\":" + deadlines.isTerminated() + "}");
        token = null;
        log("SHUTDOWN");
        if (failure != null) throw failure;
    }
}
