package com.gorillaescape.spike;

import io.netty.bootstrap.Bootstrap;
import io.netty.buffer.ByteBuf;
import io.netty.buffer.Unpooled;
import io.netty.channel.*;
import io.netty.channel.nio.NioEventLoopGroup;
import io.netty.channel.socket.nio.NioDatagramChannel;
import io.netty.handler.codec.ByteToMessageDecoder;
import io.netty.handler.codec.http3.Http3;
import io.netty.handler.codec.quic.*;
import io.netty.handler.timeout.IdleStateHandler;
import io.netty.handler.timeout.IdleStateEvent;
import io.netty.util.ReferenceCountUtil;
import io.netty.util.AttributeKey;
import io.suboptimal.netty.webtransport.*;
import java.net.InetSocketAddress;
import java.nio.charset.StandardCharsets;
import java.nio.file.*;
import java.security.MessageDigest;
import java.util.*;
import java.util.concurrent.*;
import java.util.concurrent.atomic.AtomicInteger;

/** Isolated lab protocol; no Gorilla Protocol, input, gameplay, HTTP backend or IPC. */
public final class ProbeServer {
    static final AttributeKey<Boolean> AUTH = AttributeKey.valueOf("probe.auth");
    static final Set<WebTransportSession> sessions = ConcurrentHashMap.newKeySet();
    static final AtomicInteger invalid = new AtomicInteger(), datagrams = new AtomicInteger();
    static byte[] secret;
    static boolean valid(String text) { return text.matches("PING:[0-9]{1,4}"); }
    static void close(WebTransportSession s, int code) { s.close(code, "probe"); }
    public static void main(String[] args) throws Exception {
        secret = Files.readString(Path.of(System.getenv("WT_SECRET_FILE"))).trim().getBytes(StandardCharsets.US_ASCII);
        if (secret.length != 64) throw new IllegalArgumentException("invalid lab credential");
        var ssl = QuicSslContextBuilder.forServer(new java.io.File(args[1]), null, new java.io.File(args[0]))
            .applicationProtocols("h3").build();
        var protocol = WebTransportServerProtocolHandler.builder()
            .initialMaxStreamsBidi(2).initialMaxStreamsUni(0).initialMaxData(65536)
            .session(new WebTransportSessionInitializer() {
                protected void initSession(QuicStreamChannel ch, WebTransportSession s) {
                    sessions.add(s);
                    if (sessions.size() > 4) { close(s, 429); sessions.remove(s); return; }
                    ch.attr(AUTH).set(false);
                    var deadline = ch.eventLoop().schedule(() -> { if (!Boolean.TRUE.equals(ch.attr(AUTH).get())) close(s, 401); }, 2, TimeUnit.SECONDS);
                    ch.closeFuture().addListener(f -> { deadline.cancel(false); sessions.remove(s); });
                    ch.pipeline().addLast(new ChannelInboundHandlerAdapter() {
                        public void channelRead(ChannelHandlerContext ctx, Object msg) {
                            if (msg instanceof WebTransportDatagramFrame d) {
                                try {
                                    var b=d.content();
                                    if (!Boolean.TRUE.equals(ch.attr(AUTH).get()) || b.readableBytes()>256
                                        || !valid(b.toString(StandardCharsets.US_ASCII))) { invalid.incrementAndGet(); return; }
                                    if (!ch.isWritable()) { close(s, 429); return; }
                                    datagrams.incrementAndGet(); ctx.writeAndFlush(new WebTransportDatagramFrame(b.retainedDuplicate()));
                                } finally { ReferenceCountUtil.release(msg); }
                            } else ctx.fireChannelRead(msg);
                        }
                    });
                }
            })
            .bidiStream(new WebTransportStreamInitializer() {
                protected void initStream(QuicStreamChannel ch, WebTransportSession s) {
                    ch.pipeline().addLast(new IdleStateHandler(3,3,0));
                    ch.pipeline().addLast(new ByteToMessageDecoder() {
                        protected void decode(ChannelHandlerContext ctx, ByteBuf in, List<Object> out) {
                            while(in.readableBytes()>=2) {
                                int n=in.getUnsignedShort(in.readerIndex());
                                if(n<1 || n>256) { invalid.incrementAndGet(); close(s,413); in.skipBytes(in.readableBytes()); return; }
                                if(in.readableBytes()<n+2) return;
                                in.skipBytes(2); String text=in.readCharSequence(n,StandardCharsets.US_ASCII).toString();
                                if(!Boolean.TRUE.equals(s.sessionChannel().attr(AUTH).get())) {
                                    byte[] supplied=text.startsWith("AUTH:")?text.substring(5).getBytes(StandardCharsets.US_ASCII):new byte[0];
                                    if(!MessageDigest.isEqual(secret,supplied)) { invalid.incrementAndGet(); close(s,401); return; }
                                    s.sessionChannel().attr(AUTH).set(true); reply(ctx,"READY");
                                } else if(valid(text)) reply(ctx,text);
                                else { invalid.incrementAndGet(); reply(ctx,"INVALID"); }
                            }
                        }
                        public void exceptionCaught(ChannelHandlerContext ctx, Throwable cause) { invalid.incrementAndGet(); close(s,400); }
                        public void userEventTriggered(ChannelHandlerContext ctx,Object event) {
                            if(event instanceof IdleStateEvent) close(s,408); else ctx.fireUserEventTriggered(event);
                        }
                        private void reply(ChannelHandlerContext ctx,String text) {
                            if(!ctx.channel().isWritable()) { close(s,429); return; }
                            byte[] bytes=text.getBytes(StandardCharsets.US_ASCII);
                            ctx.writeAndFlush(Unpooled.buffer(2+bytes.length).writeShort(bytes.length).writeBytes(bytes));
                        }
                    });
                }
            }).build();
        EventLoopGroup group=new NioEventLoopGroup(1);
        Channel udp=null;
        try {
            var codec=Http3.newQuicServerCodecBuilder().sslContext(ssl)
                .tokenHandler(InsecureQuicTokenHandler.INSTANCE)
                .maxIdleTimeout(5,TimeUnit.SECONDS).initialMaxData(65536)
                .initialMaxStreamDataBidirectionalLocal(4096).initialMaxStreamDataBidirectionalRemote(4096)
                .initialMaxStreamDataUnidirectional(4096).initialMaxStreamsBidirectional(3).initialMaxStreamsUnidirectional(3)
                .datagram(256,256).handler(new ChannelInitializer<QuicChannel>() {
                    protected void initChannel(QuicChannel ch) { ch.pipeline().addLast(protocol); }
                }).build();
            udp=new Bootstrap().group(group).channel(NioDatagramChannel.class).handler(codec)
                .bind(new InetSocketAddress(args[2],0)).sync().channel();
            System.out.println("READY "+((InetSocketAddress)udp.localAddress()).getPort()); System.out.flush();
            while(System.in.read()!=-1) { }
        } finally {
            for(var s:sessions) close(s,0);
            if(udp!=null) udp.close().sync();
            group.shutdownGracefully(0,2,TimeUnit.SECONDS).sync();
            Arrays.fill(secret,(byte)0);
            System.out.println("STOPPED invalid="+invalid.get()+" datagrams="+datagrams.get());
        }
    }
}
