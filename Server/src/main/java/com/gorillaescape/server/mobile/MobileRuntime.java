package com.gorillaescape.server.mobile;

import com.gorillaescape.server.protocol.*;
import dev.onvoid.webrtc.*;
import tools.jackson.databind.node.ObjectNode;
import java.io.*;
import java.nio.ByteBuffer;
import java.util.*;
import java.util.concurrent.*;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.stereotype.Component;
import org.springframework.beans.factory.DisposableBean;

/** Phase-0 runtime adapter: local signaling, two DataChannels, no media tracks or gameplay. */
@Component
@ConditionalOnProperty(name="gorilla.mobile.rtc-enabled",havingValue="true")
public final class MobileRuntime implements DisposableBean {
    public final PhoneInputHub inputs=new PhoneInputHub();
    private final TechnicalSession session=new TechnicalSession(System::nanoTime,30_000_000_000L,5_000_000_000L,30_000_000_000L);
    private final Map<String,Peer> peers=new HashMap<>();
    private final PeerConnectionFactory factory=new PeerConnectionFactory();
    private final ScheduledThreadPoolExecutor expiry=new ScheduledThreadPoolExecutor(1,r->{Thread t=new Thread(r,"mobile-liveness");t.setDaemon(true);return t;});
    private boolean closed;
    private final Deque<JoinDiagnostic> attempts=new ArrayDeque<>();
    public static final class JoinFailure extends IOException {
        private final JoinDiagnostic.Code code;
        JoinFailure(JoinDiagnostic.Code code){super(code.name());this.code=code;}
        public String code(){return code.name();}
    }
    private JoinDiagnostic attempt(){var d=new JoinDiagnostic();if(attempts.size()==8)attempts.removeFirst();attempts.addLast(d);return d;}
    public synchronized List<Map<String,Object>> joinDiagnostics(){return attempts.stream().map(JoinDiagnostic::snapshot).toList();}
    private static JoinFailure failure(JoinDiagnostic d,JoinDiagnostic.Code code){d.fail(code);return new JoinFailure(code);}

    public MobileRuntime(){expiry.setRemoveOnCancelPolicy(true);expiry.scheduleWithFixedDelay(this::expire,1,1,TimeUnit.SECONDS);}
    public synchronized TechnicalSession.Admission admission(){if(closed)throw new IllegalStateException("SESSION_CLOSED");return session.createAdmission();}
    public record Signal(int protocolVersion,String messageType,String sessionId,String admission,String offer) {}
    public record Answer(int protocolVersion,String messageType,String sessionId,int playerId,String deviceSessionId,String resumeToken,long connectionEpoch,String peerId,String answer) {}
    public synchronized Answer join(Signal signal)throws Exception{
        var d=attempt();d.stage(JoinDiagnostic.Stage.VALIDATION);
        if(closed||peers.size()>=4)throw failure(d,JoinDiagnostic.Code.SESSION_FULL);
        if(signal==null||signal.protocolVersion()!=1||!"JOIN".equals(signal.messageType())||signal.offer()==null||signal.offer().length()>65536||!signal.offer().startsWith("v=0")||signal.offer().contains("m=audio")||signal.offer().contains("m=video"))throw failure(d,JoinDiagnostic.Code.SIGNAL_INVALID);
        try{localOffer(signal.offer());}catch(Exception e){throw failure(d,JoinDiagnostic.Code.SIGNAL_INVALID);}
        d.stage(JoinDiagnostic.Stage.ADMISSION);
        TechnicalSession.Joined joined;
        try{joined=session.join(signal.sessionId(),signal.admission());}
        catch(IllegalArgumentException e){throw failure(d,JoinDiagnostic.Code.valueOf(session.admissionFailure(signal.sessionId(),signal.admission())));}
        catch(IllegalStateException e){throw failure(d,JoinDiagnostic.Code.SESSION_FULL);}
        return connect(joined,signal.offer(),d,true);
    }
    public synchronized Answer reconnect(String sid,int player,String resume,String offer)throws Exception{
        if(closed||offer==null||offer.length()>65536||!offer.startsWith("v=0")||offer.contains("m=audio")||offer.contains("m=video"))throw new IOException("SIGNAL_INVALID");
        localOffer(offer);var joined=session.reconnect(sid,player,resume);return connect(joined,offer,attempt(),false);
    }
    static void localOffer(String offer)throws IOException{
        int count=0;
        for(String line:offer.split("\\r?\\n"))if(line.startsWith("a=candidate:")){
            if(++count>32)throw new IOException("ICE_BOUND");String[] fields=line.trim().split("\\s+");
            if(fields.length<8||!fields[6].equals("typ")||!fields[7].equals("host"))throw new IOException("ICE_LOCAL_ONLY");
            String address=fields[4];if(address.matches("[A-Za-z0-9-]{1,63}\\.local"))continue;
            if(!address.matches("[0-9.]+")&&!address.matches("[0-9a-fA-F:]+"))throw new IOException("ICE_LOCAL_ONLY");
            var ip=java.net.InetAddress.getByName(address);byte[] raw=ip.getAddress();
            if(!ip.isLoopbackAddress()&&!ip.isSiteLocalAddress()&&!ip.isLinkLocalAddress()&&!(raw.length==16&&(raw[0]&0xfe)==0xfc))throw new IOException("ICE_LOCAL_ONLY");
        }
    }
    private Answer connect(TechnicalSession.Joined joined,String offer,JoinDiagnostic d,boolean initial)throws Exception{
        long deadline=System.nanoTime()+TimeUnit.SECONDS.toNanos(10);
        String id=UUID.randomUUID().toString();Peer p=null;
        try{
            d.stage(JoinDiagnostic.Stage.PEER_CREATE);p=new Peer(joined,d,initial);peers.put(id,p);
            d.stage(JoinDiagnostic.Stage.REMOTE_SDP);
            CompletableFuture<Void> remote=new CompletableFuture<>();p.pc.setRemoteDescription(new RTCSessionDescription(RTCSdpType.OFFER,offer),setter(remote));remote.get(remaining(deadline),TimeUnit.NANOSECONDS);
            d.stage(JoinDiagnostic.Stage.ANSWER_CREATE);
            CompletableFuture<RTCSessionDescription> answer=new CompletableFuture<>();p.pc.createAnswer(new RTCAnswerOptions(),new CreateSessionDescriptionObserver(){public void onSuccess(RTCSessionDescription description){answer.complete(description);}public void onFailure(String ignored){answer.completeExceptionally(new IOException("SDP_FAILED"));}});
            var description=answer.get(remaining(deadline),TimeUnit.NANOSECONDS);
            d.stage(JoinDiagnostic.Stage.LOCAL_SDP);
            CompletableFuture<Void> local=new CompletableFuture<>();p.pc.setLocalDescription(description,setter(local));local.get(remaining(deadline),TimeUnit.NANOSECONDS);
            d.stage(JoinDiagnostic.Stage.SERVER_ICE);
            if(!p.gathered.await(remaining(deadline),TimeUnit.NANOSECONDS))throw new IOException("ICE_TIMEOUT");
            p.lastSeen=System.nanoTime();d.stage(JoinDiagnostic.Stage.ANSWER_RETURNED);
            return new Answer(1,"JOIN_ACK",joined.sessionId(),joined.playerId(),joined.sessionDeviceId(),joined.resumeToken(),joined.epoch(),id,p.pc.getLocalDescription().sdp);
        }catch(Exception|Error e){
            // JNI SDP parsing throws java.lang.Error, not its async observer failure.
            // Classify that native failure without swallowing fatal JVM conditions.
            if(e instanceof VirtualMachineError fatal)throw fatal;
            if(e instanceof ThreadDeath fatal)throw fatal;
            var code=d.stageFailure();peers.remove(id);
            if(p!=null)p.close();else if(initial)session.abortUnconfirmedJoin(joined);else session.disconnect(joined.sessionId(),joined.playerId(),joined.sessionDeviceId(),joined.epoch());
            throw failure(d,code);
        }
    }
    private static long remaining(long deadline)throws IOException{long left=deadline-System.nanoTime();if(left<=0)throw new IOException("SIGNAL_TIMEOUT");return left;}
    private static SetSessionDescriptionObserver setter(CompletableFuture<Void> f){return new SetSessionDescriptionObserver(){public void onSuccess(){f.complete(null);}public void onFailure(String e){f.completeExceptionally(new IOException("SDP_FAILED"));}};}
    public synchronized void disconnect(String peerId,String resume)throws IOException{
        Peer p=peers.get(peerId);if(p==null||!java.security.MessageDigest.isEqual(p.joined.resumeToken().getBytes(java.nio.charset.StandardCharsets.US_ASCII),resume.getBytes(java.nio.charset.StandardCharsets.US_ASCII)))throw new IOException("PEER_IDENTITY");peers.remove(peerId);p.close();
    }
    public synchronized int peerCount(){return peers.size();}
    private synchronized void expire(){if(closed)return;for(var it=peers.entrySet().iterator();it.hasNext();){var p=it.next().getValue();if(System.nanoTime()-p.lastSeen>5_000_000_000L){it.remove();p.close();}}}
    private final class Peer {
        final TechnicalSession.Joined joined;final GorillaProtocol.Gate gate;final CountDownLatch gathered=new CountDownLatch(1);
        final List<RTCDataChannel> channels=new ArrayList<>();final RTCPeerConnection pc;
        volatile long lastSeen=System.nanoTime();volatile boolean stopped;volatile ObjectNode lastInput;
        final JoinDiagnostic diagnostic;final boolean initial;boolean helloConfirmed;
        Peer(TechnicalSession.Joined joined,JoinDiagnostic diagnostic,boolean initial){this.joined=joined;this.diagnostic=diagnostic;this.initial=initial;gate=new GorillaProtocol.Gate(new GorillaProtocol.Identity(joined.sessionId(),joined.playerId(),joined.sessionDeviceId()),System::currentTimeMillis,2000,250,120);
            var c=new RTCConfiguration();c.iceServers.clear();c.portAllocatorConfig=new PortAllocatorConfig().setDisableStun(true).setDisableRelay(true).setDisableTcp(true);
            pc=factory.createPeerConnection(c,new PeerConnectionObserver(){public void onIceCandidate(RTCIceCandidate candidate){diagnostic.candidate(candidate.sdp);}public void onIceGatheringChange(RTCIceGatheringState s){diagnostic.event("iceGatheringState",s.name());if(s==RTCIceGatheringState.COMPLETE)gathered.countDown();}public void onIceConnectionChange(RTCIceConnectionState s){diagnostic.event("iceConnectionState",s.name());}public void onConnectionChange(RTCPeerConnectionState s){diagnostic.event("connectionState",s.name());}public void onDataChannel(RTCDataChannel channel){attach(channel);}});
        }
        synchronized void attach(RTCDataChannel ch){if(stopped||channels.size()>=2||channels.stream().anyMatch(c->c.getLabel().equals(ch.getLabel()))||!Set.of("control","motion").contains(ch.getLabel())||ch.getLabel().equals("control")&&(!ch.isOrdered()||!ch.isReliable())||ch.getLabel().equals("motion")&&(ch.isOrdered()||ch.getMaxRetransmits()!=0)){ch.close();ch.dispose();return;}channels.add(ch);
            ch.registerObserver(new RTCDataChannelObserver(){public void onBufferedAmountChange(long a){}public void onStateChange(){diagnostic.event(ch.getLabel(),ch.getState().name());}public void onMessage(RTCDataChannelBuffer buffer){synchronized(Peer.this){
                if(stopped)return;String error=null;
                try{
                    if(buffer.binary||buffer.data.remaining()>GorillaProtocol.MAX_BYTES)throw new IOException("MOBILE_SIZE");byte[] bytes=new byte[buffer.data.remaining()];buffer.data.get(bytes);ObjectNode n=gate.accept(bytes,ch.getLabel());
                    String type=n.get("messageType").asString();if(type.equals("MOTION_SAMPLE")!=ch.getLabel().equals("motion"))throw new IOException("MOBILE_CHANNEL");
                    session.heartbeat(joined.sessionId(),joined.playerId(),joined.sessionDeviceId(),joined.epoch());lastSeen=System.nanoTime();
                    if(type.equals("HELLO"))helloConfirmed=true;
                    if(type.equals("MOTION_SAMPLE")||type.equals("TOUCH")){session.active(joined.sessionId(),joined.playerId(),joined.sessionDeviceId(),joined.epoch());lastInput=n;inputs.accept(n,joined.epoch(),"ACTIVE");}
                    if(type.equals("CLIENT_STATE")){var state=lastInput==null?n:lastInput.deepCopy();state.put("messageType","SERVER_STATE");inputs.accept(state,joined.epoch(),n.get("payload").get("state").asString().equals("suspended")?"SUSPENDED":"CONNECTED");}
                    if(type.equals("DISCONNECT")){inputs.accept(n,joined.epoch(),"DISCONNECTED");lastSeen=0;}
                    if(ch.getBufferedAmount()<8192){var ack=n.deepCopy();ack.put("messageType","ACK");ack.set("payload",GorillaProtocol.JSON.createObjectNode());send(ch,GorillaProtocol.JSON.writeValueAsBytes(ack));}
                }catch(Exception e){inputs.reject();error=e.getMessage();}
                if(error!=null&&ch.getBufferedAmount()<8192){var reply=GorillaProtocol.JSON.createObjectNode();reply.put("protocolVersion",1);reply.put("messageType","ERROR");reply.put("sessionId",joined.sessionId());reply.put("playerId",joined.playerId());reply.put("deviceSessionId",joined.sessionDeviceId());reply.putNull("sequence");reply.putNull("clientTimestamp");reply.put("serverReceiveTimestamp",System.currentTimeMillis());reply.set("capabilities",GorillaProtocol.JSON.createArrayNode());reply.putNull("quality");var payload=GorillaProtocol.JSON.createObjectNode();payload.put("code","INVALID_INPUT");reply.set("payload",payload);send(ch,GorillaProtocol.JSON.writeValueAsBytes(reply));}
            }}});
        }
        synchronized void close(){if(stopped)return;stopped=true;if(lastInput!=null){var state=lastInput.deepCopy();state.put("messageType","SERVER_STATE");state.put("serverReceiveTimestamp",System.currentTimeMillis());inputs.accept(state,joined.epoch(),"DISCONNECTED");}for(var ch:channels){ch.unregisterObserver();ch.close();ch.dispose();}channels.clear();pc.close();try{if(initial&&!helloConfirmed)session.abortUnconfirmedJoin(joined);else session.disconnect(joined.sessionId(),joined.playerId(),joined.sessionDeviceId(),joined.epoch());}catch(RuntimeException ignored){}}
    }
    private static void send(RTCDataChannel ch,byte[] bytes){try{ch.send(new RTCDataChannelBuffer(ByteBuffer.wrap(bytes),false));}catch(Exception ignored){ch.close();}}
    @Override public synchronized void destroy(){if(closed)return;closed=true;expiry.shutdownNow();for(var p:peers.values())p.close();peers.clear();session.close();factory.dispose();}
}
