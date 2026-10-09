package com.gorillaescape.server.hosting;

import static org.junit.jupiter.api.Assertions.*;
import java.io.*;
import java.net.*;
import java.net.http.*;
import java.nio.file.*;
import java.security.*;
import java.security.cert.*;
import java.time.Duration;
import java.util.*;
import java.util.concurrent.*;
import javax.net.ssl.*;
import com.gorillaescape.server.protocol.GorillaProtocol;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;

class LocalTrustProcessTest {
    @TempDir Path directory;
    static int free(String address)throws IOException {try(var socket=new ServerSocket(0,1,InetAddress.getByName(address))){return socket.getLocalPort();}}
    @Test void realLanTlsBootstrapProductQrOperatorIsolationAndEof() throws Exception {
        LanInterface.Candidate lan;
        try {lan=LanInterface.select(null,null);}catch(Exception e){org.junit.jupiter.api.Assumptions.assumeTrue(false,"No unique physical LAN; run this gate on explicit QA LAN");return;}
        Path trust=directory.resolve("trust");var material=LocalTrust.prepare(trust,lan.address());
        int https=free(lan.address()),bootstrap=free(lan.address());
        String operator=Base64.getUrlEncoder().withoutPadding().encodeToString(SecureRandom.getSeed(32));
        var command=new ProcessBuilder(Path.of(System.getProperty("java.home"),"bin/java").toString(),"-Djava.net.preferIPv4Stack=true","-cp",System.getProperty("java.class.path"),"com.gorillaescape.server.GorillaEscapeApplication","--gorilla.ipc.managed=true","--logging.config=classpath:ipc-logback.xml");
        var env=command.environment();env.remove("GORILLA_MOBILE_CONFIG");env.remove("GORILLA_MOBILE_LAB");
        env.put("GORILLA_LOCAL_TRUST_DIR",trust.toString());env.put("GORILLA_LAN_INTERFACE",lan.name());env.put("GORILLA_LAN_ADDRESS",lan.address());env.put("GORILLA_HTTPS_PORT",Integer.toString(https));env.put("GORILLA_TRUST_BOOTSTRAP_PORT",Integer.toString(bootstrap));
        env.put("GORILLA_MOBILE_OPERATOR",operator);env.put("GORILLA_IPC_INSTANCE",UUID.randomUUID().toString());env.put("GORILLA_IPC_TOKEN","A".repeat(43));env.put("GORILLA_IPC_LOCK_DIR",directory.resolve("lock").toString());
        var process=command.redirectError(ProcessBuilder.Redirect.DISCARD).start();
        ExecutorService reader=Executors.newSingleThreadExecutor();
        try {
            var ready=reader.submit(()->{try(var lines=new BufferedReader(new InputStreamReader(process.getInputStream()))){String line;while((line=lines.readLine())!=null)if(line.startsWith("GORILLA_IPC_READY "))return GorillaProtocol.JSON.readTree(line.substring(18));}throw new IOException("READY_MISSING");});
            var message=ready.get(20,TimeUnit.SECONDS);assertEquals(https,message.get("httpPort").intValue());
            var roots=KeyStore.getInstance("PKCS12");roots.load(null);roots.setCertificateEntry("installation",material.root());
            var factory=TrustManagerFactory.getInstance(TrustManagerFactory.getDefaultAlgorithm());factory.init(roots);var ssl=SSLContext.getInstance("TLS");ssl.init(null,factory.getTrustManagers(),null);
            var client=HttpClient.newBuilder().sslContext(ssl).connectTimeout(Duration.ofSeconds(2)).build();
            String origin="https://"+lan.address()+":"+https;
            var publicCertificate=client.send(HttpRequest.newBuilder(URI.create(origin+"/gorilla-root.cer")).timeout(Duration.ofSeconds(5)).build(),HttpResponse.BodyHandlers.ofByteArray());
            assertEquals(200,publicCertificate.statusCode());assertArrayEquals(material.root().getEncoded(),publicCertificate.body());
            for(String path:List.of("/root.p12","/password","/server.p12"))assertEquals(404,client.send(HttpRequest.newBuilder(URI.create(origin+path)).timeout(Duration.ofSeconds(5)).build(),HttpResponse.BodyHandlers.discarding()).statusCode());
            var untrusted=HttpClient.newBuilder().connectTimeout(Duration.ofSeconds(2)).build();
            assertThrows(SSLHandshakeException.class,()->untrusted.send(HttpRequest.newBuilder(URI.create(origin+"/prepare")).timeout(Duration.ofSeconds(5)).build(),HttpResponse.BodyHandlers.discarding()));
            try(var raw=new Socket(lan.address(),https);var wrong=(SSLSocket)ssl.getSocketFactory().createSocket(raw,"192.168.254.253",https,true)){
                wrong.setSoTimeout(3000);var parameters=wrong.getSSLParameters();parameters.setEndpointIdentificationAlgorithm("HTTPS");wrong.setSSLParameters(parameters);assertThrows(SSLHandshakeException.class,wrong::startHandshake);
            }
            var admissionRequest=HttpRequest.newBuilder(URI.create(origin+"/mobile/admission")).timeout(Duration.ofSeconds(5)).header("X-Gorilla-Operator",operator).POST(HttpRequest.BodyPublishers.noBody()).build();
            var admission=client.send(admissionRequest,HttpResponse.BodyHandlers.ofString());assertEquals(200,admission.statusCode());
            var qr=tools.jackson.databind.json.JsonMapper.builder().build().readTree(admission.body());var url=URI.create(qr.get("url").asString());assertEquals(lan.address(),url.getHost());assertEquals(https,url.getPort());assertEquals("https",url.getScheme());assertFalse(url.toString().contains(operator));assertFalse(url.toString().contains("127.0.0.1"));
            assertEquals(403,client.send(HttpRequest.newBuilder(URI.create(origin+"/mobile/admission")).timeout(Duration.ofSeconds(5)).POST(HttpRequest.BodyPublishers.noBody()).build(),HttpResponse.BodyHandlers.discarding()).statusCode());
            var plain=HttpClient.newBuilder().connectTimeout(Duration.ofSeconds(2)).build();String initial="http://"+lan.address()+":"+bootstrap;
            assertEquals(200,plain.send(HttpRequest.newBuilder(URI.create(initial+"/gorilla-root.cer")).timeout(Duration.ofSeconds(3)).build(),HttpResponse.BodyHandlers.discarding()).statusCode());
            for(String path:List.of("/mobile/join","/root.p12","/password","/"))assertEquals(404,plain.send(HttpRequest.newBuilder(URI.create(initial+path)).timeout(Duration.ofSeconds(3)).build(),HttpResponse.BodyHandlers.discarding()).statusCode());
            process.getOutputStream().close();assertTrue(process.waitFor(12,TimeUnit.SECONDS));assertEquals(0,process.exitValue());
            try(var s=new ServerSocket(bootstrap,1,InetAddress.getByName(lan.address()))){assertTrue(s.isBound());}
        }finally {
            reader.shutdownNow();if(process.isAlive()){process.getOutputStream().close();if(!process.waitFor(12,TimeUnit.SECONDS)){process.destroyForcibly();process.waitFor(2,TimeUnit.SECONDS);}}
        }
    }
}
