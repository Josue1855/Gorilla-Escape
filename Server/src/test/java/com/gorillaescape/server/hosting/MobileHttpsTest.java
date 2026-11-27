package com.gorillaescape.server.hosting;

import static org.junit.jupiter.api.Assertions.*;
import java.io.*;
import java.net.*;
import java.net.http.*;
import java.nio.file.*;
import java.security.KeyStore;
import java.security.cert.CertificateFactory;
import java.time.Duration;
import java.util.Map;
import java.util.UUID;
import java.util.concurrent.*;
import javax.net.ssl.*;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;
import com.gorillaescape.server.ipc.ProbeCodec;
import tools.jackson.databind.json.JsonMapper;

/** Certificates are generated into a temporary directory, never versioned or reused for a demo. */
class MobileHttpsTest {
    @TempDir Path directory;
    private final String address = System.getProperty("gorilla.test.lanAddress", "127.0.0.1");
    private final String iface = System.getProperty("gorilla.test.lanInterface", "lo");
    private final String instance = UUID.randomUUID().toString();
    private final String connection = UUID.randomUUID().toString();
    private static final String HOSTNAME = "probe.gorilla.test";
    private static final String TOKEN = "A".repeat(43);

    private void command(String... args) throws Exception {
        var process = new ProcessBuilder(args).directory(directory.toFile()).redirectOutput(ProcessBuilder.Redirect.DISCARD)
                .redirectError(ProcessBuilder.Redirect.DISCARD).start();
        try { assertTrue(process.waitFor(10, TimeUnit.SECONDS)); assertEquals(0, process.exitValue()); }
        finally { if (process.isAlive()) process.destroyForcibly().waitFor(2, TimeUnit.SECONDS); }
    }
    private Path material(String san) throws Exception {
        command("openssl", "req", "-x509", "-newkey", "rsa:2048", "-nodes", "-keyout", "ca.key", "-out", "ca.crt", "-days", "1", "-subj", "/CN=Ephemeral test CA");
        command("openssl", "req", "-new", "-newkey", "rsa:2048", "-nodes", "-keyout", "leaf.key", "-out", "leaf.csr", "-subj", "/CN=Ephemeral test leaf");
        Files.writeString(directory.resolve("san.conf"), "subjectAltName=" + (san.startsWith("IP:") ? san : "DNS:" + san) + "\nbasicConstraints=CA:FALSE\nkeyUsage=digitalSignature,keyEncipherment\nextendedKeyUsage=serverAuth\n");
        command("openssl", "x509", "-req", "-in", "leaf.csr", "-CA", "ca.crt", "-CAkey", "ca.key", "-CAcreateserial", "-out", "leaf.crt", "-days", "1", "-extfile", "san.conf");
        command("openssl", "pkcs12", "-export", "-out", "server.p12", "-inkey", "leaf.key", "-in", "leaf.crt", "-certfile", "ca.crt", "-passout", "pass:test-only-password");
        Files.writeString(directory.resolve("password"), "test-only-password\n");
        for (String name : new String[]{"server.p12", "password"}) protect(directory.resolve(name));
        return directory.resolve("server.p12");
    }
    private Path config(int port) throws Exception {
        assertNotNull(address, "Set explicit -Dgorilla.test.lanAddress for the real LAN test");
        assertNotNull(iface, "Set explicit -Dgorilla.test.lanInterface");
        Path file = directory.resolve("mobile.properties");
        Files.writeString(file, "address=" + address + "\ninterface=" + iface + "\noperatorConfirmed=true\nport=" + port
                + "\nhostname=" + HOSTNAME + "\npublicOrigin=https://" + HOSTNAME + "\nkeyStore=" + directory.resolve("server.p12") + "\npasswordFile=" + directory.resolve("password") + "\n");
        protect(file);
        return file;
    }
    private void protect(Path path) throws IOException {
        if (Files.getFileStore(path).supportsFileAttributeView("posix"))
            Files.setPosixFilePermissions(path, java.nio.file.attribute.PosixFilePermissions.fromString("rw-------"));
    }
    private HttpClient client() throws Exception {
        var trust = KeyStore.getInstance(KeyStore.getDefaultType()); trust.load(null);
        try (var input = Files.newInputStream(directory.resolve("ca.crt"))) {
            trust.setCertificateEntry("test-root", CertificateFactory.getInstance("X.509").generateCertificate(input));
        }
        var factory = TrustManagerFactory.getInstance(TrustManagerFactory.getDefaultAlgorithm()); factory.init(trust);
        var context = SSLContext.getInstance("TLS"); context.init(null, factory.getTrustManagers(), null);
        return HttpClient.newBuilder().sslContext(context).connectTimeout(Duration.ofSeconds(2)).build();
    }
    private record Response(int statusCode, HttpHeaders headers, String body) { }
    private String line(InputStream input) throws IOException {
        var bytes = new ByteArrayOutputStream();
        int next;
        while ((next = input.read()) != -1) {
            if (next == '\n') return bytes.toString(java.nio.charset.StandardCharsets.UTF_8).replaceFirst("\\r$", "");
            bytes.write(next);
            if (bytes.size() > 8192) throw new IOException("Test HTTP header too large");
        }
        throw new EOFException();
    }
    private Response get(HttpClient client, int port, String path) throws Exception {
        // Test-only route: direct IPv4 with real DNS SNI and endpoint verification.
        // Never modifies system DNS or disables certificate/hostname validation.
        try (var raw = new Socket()) {
            raw.connect(new InetSocketAddress(address, port), 2000); raw.setSoTimeout(3000);
            try (var socket = (SSLSocket) client.sslContext().getSocketFactory().createSocket(raw, HOSTNAME, port, true)) {
                var parameters = socket.getSSLParameters();
                parameters.setEndpointIdentificationAlgorithm("HTTPS");
                parameters.setServerNames(java.util.List.of(new SNIHostName(HOSTNAME)));
                socket.setSSLParameters(parameters); socket.startHandshake();
                socket.getOutputStream().write(("GET " + path + " HTTP/1.1\r\nHost: " + HOSTNAME + ":" + port
                    + "\r\nConnection: close\r\n\r\n").getBytes(java.nio.charset.StandardCharsets.US_ASCII));
                var input = new BufferedInputStream(socket.getInputStream());
                int status = Integer.parseInt(line(input).split(" ")[1]);
                var headers = new java.util.HashMap<String, java.util.List<String>>();
                String header;
                while (!(header = line(input)).isEmpty()) {
                    int colon = header.indexOf(':');
                    headers.put(header.substring(0, colon).toLowerCase(java.util.Locale.ROOT), java.util.List.of(header.substring(colon + 1).trim()));
                }
                var body = new ByteArrayOutputStream();
                if (headers.getOrDefault("transfer-encoding", java.util.List.of("")).get(0).equalsIgnoreCase("chunked")) {
                    int length;
                    while ((length = Integer.parseInt(line(input).split(";")[0], 16)) != 0) {
                        body.write(input.readNBytes(length)); assertEquals("", line(input));
                    }
                } else body.write(input.readAllBytes());
                return new Response(status, HttpHeaders.of(headers, (key, value) -> true), body.toString(java.nio.charset.StandardCharsets.UTF_8));
            }
        }
    }
    private Process launch(Path config) throws Exception {
        var builder = new ProcessBuilder(Path.of(System.getProperty("java.home"), "bin", "java").toString(),
                "-Djava.net.preferIPv4Stack=true", "-cp", System.getProperty("java.class.path"),
                "com.gorillaescape.server.GorillaEscapeApplication", "--gorilla.ipc.managed=true",
                "--server.address=127.0.0.1", "--server.port=0", "--logging.config=classpath:ipc-logback.xml");
        builder.environment().put("GORILLA_MOBILE_CONFIG", config.toString());
        builder.environment().put("GORILLA_IPC_LOCK_DIR", directory.resolve("lock").toString());
        builder.environment().put("GORILLA_IPC_INSTANCE", instance); builder.environment().put("GORILLA_IPC_TOKEN", TOKEN);
        if (address.equals("127.0.0.1")) {
            // Portable CI exercises real TLS on loopback. Explicit LAN invocation additionally
            // exercises the opt-in operator settings; neither substitutes for a physical phone.
            builder.environment().remove("GORILLA_MOBILE_CONFIG");
            builder.command().remove("--server.port=0");
            builder.command().addAll(java.util.List.of("--server.port=" + loadConfig(config).getProperty("port"),
                "--server.ssl.enabled=true", "--server.ssl.key-store=" + directory.resolve("server.p12").toUri(),
                "--server.ssl.key-store-type=PKCS12", "--server.ssl.key-store-password=test-only-password"));
        }
        return builder.redirectError(ProcessBuilder.Redirect.DISCARD).start();
    }
    private java.util.Properties loadConfig(Path path) throws Exception {
        var properties = new java.util.Properties();
        try (var input = Files.newInputStream(path)) { properties.load(input); }
        return properties;
    }
    private void cleanup(Process child) throws Exception {
        child.getOutputStream().close();
        if (!child.waitFor(10, TimeUnit.SECONDS)) { child.destroyForcibly(); assertTrue(child.waitFor(2, TimeUnit.SECONDS)); }
        assertFalse(child.isAlive(), "Owned Java residual");
    }
    private java.security.cert.Certificate[] chain() throws Exception {
        var store = KeyStore.getInstance("PKCS12");
        try (var input = Files.newInputStream(directory.resolve("server.p12"))) {
            store.load(input, "test-only-password".toCharArray());
        }
        return store.getCertificateChain(store.aliases().nextElement());
    }
    @Test void dnsHostnameAndPublicOriginPolicy() {
        assertTrue(MobileHttpsSettings.dnsHostname(HOSTNAME));
        for (String bad : new String[]{"localhost", "10.1.125.17", "*.gorilla.test", "https://gorilla.test", "gorilla.test.", "UPPER.test", "-bad.test", "bad..test"})
            assertFalse(MobileHttpsSettings.dnsHostname(bad), bad);
    }
    @Test void completeOrderedChainAndValidityAreRequired() throws Exception {
        material(HOSTNAME);
        var valid = chain();
        assertDoesNotThrow(() -> MobileHttpsSettings.validateChain(valid));
        assertThrows(Exception.class, () -> MobileHttpsSettings.validateChain(new java.security.cert.Certificate[]{valid[0]}));
        assertThrows(Exception.class, () -> MobileHttpsSettings.validateChain(new java.security.cert.Certificate[]{valid[1], valid[0]}));
        assertThrows(Exception.class, () -> MobileHttpsSettings.validateChain(new java.security.cert.Certificate[]{valid[0], valid[0]}));
        command("openssl", "x509", "-req", "-in", "leaf.csr", "-CA", "ca.crt", "-CAkey", "ca.key", "-out", "expired.crt", "-days", "-1", "-extfile", "san.conf");
        try (var input = Files.newInputStream(directory.resolve("expired.crt"))) {
            var expired = CertificateFactory.getInstance("X.509").generateCertificate(input);
            assertThrows(Exception.class, () -> MobileHttpsSettings.validateChain(new java.security.cert.Certificate[]{expired, valid[1]}));
        }
    }
    @Test void privateAddressPolicyRejectsWildcardLoopbackIpv6VpnAndMissingConfirmation() throws Exception {
        for (String invalid : new String[]{"0.0.0.0", "::", "127.0.0.1", "169.254.1.2", "8.8.8.8", "10.001.2.3", "10.256.2.3", "localhost"}) assertFalse(MobileHttpsSettings.privateIpv4(invalid));
        for (String valid : new String[]{"10.1.125.17", "172.16.1.2", "172.31.1.2", "192.168.1.2"}) assertTrue(MobileHttpsSettings.privateIpv4(valid));
        assertThrows(IllegalArgumentException.class, () -> MobileHttpsSettings.read(Path.of("relative.properties")));
        Path file = config(8443);
        for (String patch : new String[]{"operatorConfirmed=false", "interface=lo", "interface=tun-not-present", "port=0", "address=127.0.0.1", "unexpected=value", "hostname=*.gorilla.test", "hostname=10.1.125.17", "publicOrigin=http://probe.gorilla.test", "publicOrigin=https://probe.gorilla.test:8443", "publicOrigin=https://probe.gorilla.test/join"}) {
            config(8443); Files.writeString(file, patch + "\n", StandardOpenOption.APPEND);
            assertEquals("MOBILE_HTTPS_CONFIG_INVALID", assertThrows(IllegalArgumentException.class, () -> MobileHttpsSettings.read(file)).getMessage());
        }
    }
    @org.junit.jupiter.api.condition.EnabledIfSystemProperty(named="gorilla.test.lanAddress", matches=".+")
    @Test void incorrectSanAndPasswordFailBeforeOpeningServer() throws Exception {
        material("wrong.gorilla.test"); Path file = config(8443);
        assertThrows(IllegalArgumentException.class, () -> MobileHttpsSettings.read(file));
        material("IP:" + address); assertThrows(IllegalArgumentException.class, () -> MobileHttpsSettings.read(file));
        material(HOSTNAME);
        var valid = MobileHttpsSettings.read(file);
        assertEquals(address, valid.get("server.address"));
        assertEquals("https://" + HOSTNAME, valid.get("gorilla.mobile.public-origin"));
        assertEquals(8443, valid.get("server.port"));
        if (Files.getFileStore(file).supportsFileAttributeView("posix")) {
            Files.setPosixFilePermissions(directory.resolve("password"), java.nio.file.attribute.PosixFilePermissions.fromString("rw-r--r--"));
            assertThrows(IllegalArgumentException.class, () -> MobileHttpsSettings.read(file));
            protect(directory.resolve("password"));
        }
        Files.writeString(directory.resolve("password"), "wrong-private-value");
        assertEquals("MOBILE_HTTPS_CONFIG_INVALID", assertThrows(IllegalArgumentException.class, () -> MobileHttpsSettings.read(file)).getMessage());
    }
    @org.junit.jupiter.api.condition.EnabledIfSystemProperty(named="gorilla.test.lanAddress", matches=".+")
    @Test void invalidOriginAndMissingHostnameFailWithOtherwiseValidConfiguration() throws Exception {
        material(HOSTNAME);
        for (String patch : new String[]{"hostname=", "hostname=*.gorilla.test", "hostname=10.1.125.17",
                "publicOrigin=http://probe.gorilla.test", "publicOrigin=https://other.gorilla.test",
                "publicOrigin=https://probe.gorilla.test:8443", "publicOrigin=https://probe.gorilla.test:443",
                "publicOrigin=https://probe.gorilla.test/join", "publicOrigin=https://user@probe.gorilla.test"}) {
            Path file = config(8443);
            Files.writeString(file, patch + "\n", StandardOpenOption.APPEND);
            assertEquals("MOBILE_HTTPS_CONFIG_INVALID", assertThrows(IllegalArgumentException.class, () -> MobileHttpsSettings.read(file)).getMessage());
        }
    }
    @Test void rootMayBeOmittedOnlyWhenFinalIssuerIsKnown() throws Exception {
        material(HOSTNAME);
        command("openssl", "req", "-new", "-newkey", "rsa:2048", "-nodes", "-keyout", "intermediate.key", "-out", "intermediate.csr", "-subj", "/CN=Temporary intermediate");
        Files.writeString(directory.resolve("intermediate.conf"), "basicConstraints=critical,CA:TRUE,pathlen:0\nkeyUsage=critical,keyCertSign,cRLSign\n");
        command("openssl", "x509", "-req", "-in", "intermediate.csr", "-CA", "ca.crt", "-CAkey", "ca.key", "-out", "intermediate.crt", "-days", "1", "-extfile", "intermediate.conf");
        command("openssl", "x509", "-req", "-in", "leaf.csr", "-CA", "intermediate.crt", "-CAkey", "intermediate.key", "-out", "leaf.crt", "-days", "1", "-extfile", "san.conf");
        var certificates = new java.security.cert.Certificate[3];
        String[] names = {"leaf.crt", "intermediate.crt", "ca.crt"};
        for (int i = 0; i < names.length; i++) {
            try (var input = Files.newInputStream(directory.resolve(names[i]))) {
                certificates[i] = CertificateFactory.getInstance("X.509").generateCertificate(input);
            }
        }
        assertDoesNotThrow(() -> MobileHttpsSettings.validateChain(certificates));
        var withoutRoot = java.util.Arrays.copyOf(certificates, 2);
        assertThrows(Exception.class, () -> MobileHttpsSettings.validateChain(withoutRoot));
        var trust = KeyStore.getInstance("PKCS12"); trust.load(null); trust.setCertificateEntry("test-root", certificates[2]);
        Path file = directory.resolve("test-trust.p12");
        try (var output = Files.newOutputStream(file)) { trust.store(output, "test-only-password".toCharArray()); }
        String[] properties = {"javax.net.ssl.trustStore", "javax.net.ssl.trustStorePassword", "javax.net.ssl.trustStoreType"};
        String[] before = java.util.Arrays.stream(properties).map(System::getProperty).toArray(String[]::new);
        try {
            System.setProperty(properties[0], file.toString()); System.setProperty(properties[1], "test-only-password"); System.setProperty(properties[2], "PKCS12");
            assertDoesNotThrow(() -> MobileHttpsSettings.validateChain(withoutRoot));
        } finally {
            for (int i = 0; i < properties.length; i++) {
                if (before[i] == null) System.clearProperty(properties[i]); else System.setProperty(properties[i], before[i]);
            }
        }
    }
    @Test void realHttpsTrustedUntrustedSanAssetsBindingIpcAndEof() throws Exception {
        material(HOSTNAME);
        int port; try (var reserve = new ServerSocket(0, 1, InetAddress.getByName(address))) { port = reserve.getLocalPort(); }
        Process child = launch(config(port));
        var readerPool = Executors.newSingleThreadExecutor();
        int ipcPort = 0;
        try {
            String ready = readerPool.submit(() -> {
                String line; var reader = child.inputReader();
                while ((line = reader.readLine()) != null) if (line.startsWith("GORILLA_IPC_READY ")) return line.substring(18);
                throw new EOFException();
            }).get(15, TimeUnit.SECONDS);
            var node = JsonMapper.builder().build().readTree(ready);
            assertEquals(port, node.get("httpPort").intValue()); ipcPort = node.get("ipcPort").intValue();
            HttpClient trusted = client();
            var health = get(trusted, port, "/api/health"); assertEquals(200, health.statusCode());
            assertEquals("no-store", health.headers().firstValue("cache-control").orElseThrow());
            var data = JsonMapper.builder().build().readTree(health.body());
            assertTrue(data.get("secure").booleanValue()); assertEquals("gorilla-escape-local", data.get("service").asString());
            assertNotNull(UUID.fromString(data.get("serverInstanceId").asString()));
            assertEquals(2, JsonMapper.builder().build().readTree(get(trusted, port, "/api/health").body()).get("requestNumber").intValue());
            var root = get(trusted, port, "/"); assertEquals(200, root.statusCode()); assertTrue(root.body().contains("id=\"root\""));
            var scripts = java.util.regex.Pattern.compile("(?:src|href)=\"(/assets/[^\"]+)\"").matcher(root.body());
            int assets = 0; while (scripts.find()) { assertEquals(200, get(trusted, port, scripts.group(1)).statusCode()); assets++; }
            assertTrue(assets >= 2);
            assertEquals(200, get(trusted, port, "/manifest.webmanifest").statusCode());
            String worker = get(trusted, port, "/sw.js").body(); assertTrue(worker.contains("url.pathname.startsWith('/api/')"));
            for (String path : new String[]{"/api/not-a-route", "/join", "/api/onboarding/join", "/server.p12", "/rootCA-key.pem", "/actuator/env"}) assertEquals(404, get(trusted, port, path).statusCode());
            assertThrows(SSLHandshakeException.class, () -> get(HttpClient.newBuilder().connectTimeout(Duration.ofSeconds(2)).build(), port, "/api/health"));
            // Correct CA, wrong SAN: Java HttpClient's hostname verification remains enabled.
            var serverFactory = SSLContext.getInstance("TLS"); var store = KeyStore.getInstance("PKCS12");
            try (var input = Files.newInputStream(directory.resolve("server.p12"))) { store.load(input, "test-only-password".toCharArray()); }
            var km = KeyManagerFactory.getInstance(KeyManagerFactory.getDefaultAlgorithm()); km.init(store, "test-only-password".toCharArray());
            serverFactory.init(km.getKeyManagers(), null, null);
            try (var wrongHost = (SSLServerSocket) serverFactory.getServerSocketFactory().createServerSocket(0, 1, InetAddress.getLoopbackAddress())) {
                var handshake = readerPool.submit(() -> { try (var socket = (SSLSocket) wrongHost.accept()) { socket.startHandshake(); } catch (IOException expected) {} });
                assertThrows(SSLHandshakeException.class, () -> trusted.send(HttpRequest.newBuilder(URI.create("https://localhost:" + wrongHost.getLocalPort() + "/"))
                        .timeout(Duration.ofSeconds(3)).build(), HttpResponse.BodyHandlers.ofString()));
                handshake.get(5, TimeUnit.SECONDS);
            }
            try (var socket = new Socket()) {
                assertThrows(IOException.class, () -> socket.connect(new InetSocketAddress(address.equals("127.0.0.1") ? "127.0.0.2" : "127.0.0.1", port), 500));
            }
            final int privatePort = ipcPort;
            try (var socket = new Socket()) { assertThrows(IOException.class, () -> socket.connect(new InetSocketAddress(address.equals("127.0.0.1") ? "127.0.0.2" : address, privatePort), 500)); }
            try (var ipc = new Socket("127.0.0.1", ipcPort)) {
                ipc.setSoTimeout(2000);
                ProbeCodec.write(ipc.getOutputStream(), "{\"ipcVersion\":1,\"type\":\"PING\",\"instanceId\":\"" + instance
                    + "\",\"connectionId\":\"" + connection + "\",\"sequence\":1,\"payload\":{\"launchToken\":\"" + TOKEN + "\"}}");
                assertTrue(ProbeCodec.read(ipc.getInputStream()).contains("\"type\":\"PONG\""));
            }
            cleanup(child); assertEquals(0, child.exitValue());
            assertThrows(IOException.class, () -> get(trusted, port, "/api/health"));
        } finally { cleanup(child); readerPool.shutdownNow(); }
    }
    @Test void occupiedLanPortDoesNotPublishReadyOrFallbackToHttp() throws Exception {
        material(HOSTNAME);
        try (var occupied = new ServerSocket(0, 1, InetAddress.getByName(address))) {
            Process child = launch(config(occupied.getLocalPort()));
            try {
                assertTrue(child.waitFor(15, TimeUnit.SECONDS)); assertNotEquals(0, child.exitValue());
                assertFalse(new String(child.getInputStream().readAllBytes()).contains("GORILLA_IPC_READY"));
            } finally { cleanup(child); }
        }
    }
}
