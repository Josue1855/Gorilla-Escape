package com.gorillaescape.spike;

import dev.onvoid.webrtc.*;
import java.io.*;
import java.nio.ByteBuffer;
import java.nio.charset.StandardCharsets;
import java.util.*;
import java.util.concurrent.*;

/** Isolated data-only comparator. Signaling travels only through the owned process pipes. */
public final class RtcProbe {
    private static final Map<String,Peer> peers = new HashMap<>();
    private static final PeerConnectionFactory factory = new PeerConnectionFactory();
    private static synchronized void emit(String s) { System.out.println(s); System.out.flush(); }
    private static final class Peer implements AutoCloseable {
        final CountDownLatch gathered = new CountDownLatch(1);
        final List<RTCDataChannel> channels = new ArrayList<>();
        final String credential;
        RTCPeerConnection pc;
        volatile boolean closed;
        Peer(String credential) {
            this.credential = credential;
            RTCConfiguration c = new RTCConfiguration();
            c.iceServers.clear();
            c.portAllocatorConfig = new PortAllocatorConfig().setDisableStun(true).setDisableRelay(true).setDisableTcp(true);
            pc = factory.createPeerConnection(c, new PeerConnectionObserver() {
                public void onIceCandidate(RTCIceCandidate candidate) { }
                public void onIceGatheringChange(RTCIceGatheringState state) {
                    if (state == RTCIceGatheringState.COMPLETE) gathered.countDown();
                }
                public void onDataChannel(RTCDataChannel channel) { attach(channel); }
            });
        }
        synchronized void attach(RTCDataChannel ch) {
            if (closed || channels.size() >= 2 || !Set.of("reliable","unordered").contains(ch.getLabel())) { ch.close(); ch.dispose(); return; }
            channels.add(ch);
            ch.registerObserver(new RTCDataChannelObserver() {
                boolean admitted;
                public void onBufferedAmountChange(long amount) { }
                public void onStateChange() { }
                public void onMessage(RTCDataChannelBuffer buf) {
                    if (closed) return;
                    // Callback owns buffer only until return. No asynchronous queue is introduced.
                    int n = buf.data.remaining();
                    if (n > 4096 || buf.binary) { ch.close(); return; }
                    byte[] bytes = new byte[n]; buf.data.get(bytes);
                    String text = new String(bytes, StandardCharsets.US_ASCII);
                    if (!admitted) {
                        if (!text.equals("AUTH:" + credential)) { ch.close(); return; }
                        admitted = true; text = "READY";
                    } else if (!text.matches("PING:[0-9]{1,6}:[a-z]{1,2000}")) text = "INVALID";
                    if (ch.getBufferedAmount() > 8192) { ch.close(); return; }
                    try { ch.send(new RTCDataChannelBuffer(ByteBuffer.wrap(text.getBytes(StandardCharsets.US_ASCII)), false)); }
                    catch (Exception e) { ch.close(); }
                }
            });
        }
        public synchronized void close() {
            if (closed) return;
            closed = true;
            for (RTCDataChannel ch: channels) { ch.unregisterObserver(); ch.close(); ch.dispose(); }
            channels.clear(); pc.close();
        }
    }
    private static SetSessionDescriptionObserver setter(CompletableFuture<Void> f) {
        return new SetSessionDescriptionObserver() {
            public void onSuccess() { f.complete(null); }
            public void onFailure(String error) { f.completeExceptionally(new IOException("SDP rejected")); }
        };
    }
    private static String boundedLine(Reader r) throws IOException {
        StringBuilder s = new StringBuilder(); int x;
        while ((x = r.read()) != -1) { if (x == '\n') return s.toString(); if (s.length() >= 90000) throw new IOException("command limit"); s.append((char)x); }
        return s.isEmpty() ? null : s.toString();
    }
    public static void main(String[] args) throws Exception {
        emit("READY");
        try (Reader r = new InputStreamReader(System.in, StandardCharsets.US_ASCII)) {
            String line;
            while ((line = boundedLine(r)) != null) {
                String[] a = line.split(" ", 4);
                if (a.length < 2 || !a[1].matches("[a-z0-9]{1,16}")) { emit("ERROR unknown"); continue; }
                String id = a[1];
                Peer allocated = null;
                try {
                    if (a[0].equals("CLOSE")) {
                        Peer p = peers.remove(id); if (p != null) p.close(); emit("CLOSED " + id); continue;
                    }
                    if (!a[0].equals("OFFER") || a.length != 4 || peers.size() >= 4 || peers.containsKey(id) || !a[2].matches("[a-f0-9]{64}")) throw new IOException("command rejected");
                    String sdp = new String(Base64.getDecoder().decode(a[3]), StandardCharsets.UTF_8);
                    if (sdp.length() > 65536 || !sdp.startsWith("v=0") || sdp.contains("m=audio") || sdp.contains("m=video")) throw new IOException("SDP rejected");
                    Peer p = new Peer(a[2]); allocated=p; peers.put(id,p);
                    CompletableFuture<Void> remote = new CompletableFuture<>();
                    p.pc.setRemoteDescription(new RTCSessionDescription(RTCSdpType.OFFER,sdp), setter(remote)); remote.get(10,TimeUnit.SECONDS);
                    CompletableFuture<RTCSessionDescription> answer = new CompletableFuture<>();
                    p.pc.createAnswer(new RTCAnswerOptions(), new CreateSessionDescriptionObserver() {
                        public void onSuccess(RTCSessionDescription d) { answer.complete(d); }
                        public void onFailure(String error) { answer.completeExceptionally(new IOException("answer failed")); }
                    });
                    CompletableFuture<Void> local = new CompletableFuture<>(); p.pc.setLocalDescription(answer.get(10,TimeUnit.SECONDS), setter(local)); local.get(10,TimeUnit.SECONDS);
                    if (!p.gathered.await(10,TimeUnit.SECONDS)) throw new IOException("gather timeout");
                    emit("ANSWER " + id + " " + Base64.getEncoder().encodeToString(p.pc.getLocalDescription().sdp.getBytes(StandardCharsets.UTF_8)));
                } catch (Exception e) {
                    if (allocated != null) { peers.remove(id,allocated); allocated.close(); } emit("ERROR " + id);
                }
            }
        } finally {
            for (Peer p: peers.values()) p.close(); peers.clear(); factory.dispose(); emit("STOPPED residualPeers=0");
        }
    }
}
