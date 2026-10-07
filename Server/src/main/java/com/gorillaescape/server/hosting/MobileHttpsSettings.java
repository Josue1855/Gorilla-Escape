package com.gorillaescape.server.hosting;

import java.io.IOException;
import java.net.Inet4Address;
import java.net.InetAddress;
import java.net.NetworkInterface;
import java.nio.file.Files;
import java.nio.file.Path;
import java.security.KeyStore;
import java.security.cert.X509Certificate;
import java.util.Arrays;
import java.util.HashMap;
import java.util.Map;
import java.util.Properties;
import java.util.Set;
import org.springframework.boot.SpringApplication;
import org.springframework.core.env.MapPropertySource;

/** Opt-in deployment settings. No changes to the separately bound IPC listener. */
public final class MobileHttpsSettings {
    private static final Set<String> KEYS = Set.of("address", "interface", "operatorConfirmed", "port", "keyStore", "passwordFile");
    private MobileHttpsSettings() { }

    public static void configure(SpringApplication application, String file) {
        if (file == null) return;
        Map<String, Object> settings = read(Path.of(file));
        // Explicit mobile configuration wins over the supervisor's legacy HTTP-only arguments.
        // No mobile file means the existing loopback profile is completely unchanged.
        application.addInitializers(context -> context.getEnvironment().getPropertySources()
                .addFirst(new MapPropertySource("mobile-https", settings)));
    }

    static Map<String, Object> read(Path file) {
        char[] password = null;
        try {
            if (!file.isAbsolute() || !Files.isRegularFile(file) || Files.size(file) > 4096) throw invalid();
            Properties config = new Properties();
            try (var input = Files.newInputStream(file)) { config.load(input); }
            if (!KEYS.containsAll(config.stringPropertyNames()) || !"true".equals(config.getProperty("operatorConfirmed"))) throw invalid();
            String address = required(config, "address"), name = required(config, "interface");
            if (!privateIpv4(address)) throw invalid();
            NetworkInterface network = NetworkInterface.getByName(name);
            if (network == null || !network.isUp() || network.isLoopback() || network.isPointToPoint() || network.isVirtual()
                    || name.toLowerCase(java.util.Locale.ROOT).matches("^(tun|tap|wg|vpn|docker|veth|virbr|vmnet|vboxnet|tailscale|zt|br[-0-9]).*")) throw invalid();
            // Java isVirtual() describes subinterfaces, not every Linux virtual adapter.
            if (System.getProperty("os.name").toLowerCase(java.util.Locale.ROOT).contains("linux")
                    && !Files.exists(Path.of("/sys/class/net", name, "device"))) throw invalid();
            boolean assigned = network.inetAddresses().anyMatch(ip -> ip instanceof Inet4Address && address.equals(ip.getHostAddress()));
            if (!assigned) throw invalid();
            int port = Integer.parseInt(config.getProperty("port", "8443"));
            if (port < 1024 || port > 65535) throw invalid();
            Path store = absoluteFile(required(config, "keyStore"));
            Path secret = absoluteFile(required(config, "passwordFile"));
            if (Files.size(secret) > 1024) throw invalid();
            String text = Files.readString(secret);
            // A single trailing newline from an operator-created password file is allowed.
            String pass = text.replaceFirst("\\r?\\n$", "");
            if (pass.isEmpty() || pass.contains("\n") || pass.contains("\r")) throw invalid();
            password = pass.toCharArray();
            KeyStore keys = KeyStore.getInstance("PKCS12");
            try (var input = Files.newInputStream(store)) { keys.load(input, password); }
            int leaves = 0;
            var aliases = keys.aliases();
            while (aliases.hasMoreElements()) {
                String alias = aliases.nextElement();
                if (!keys.isKeyEntry(alias)) continue;
                if (!(keys.getKey(alias, password) instanceof java.security.PrivateKey)) throw invalid();
                var certificate = (X509Certificate) keys.getCertificate(alias);
                certificate.checkValidity();
                if (certificate.getBasicConstraints() >= 0) throw invalid();
                var sans = certificate.getSubjectAlternativeNames();
                if (sans == null || sans.stream().noneMatch(san -> Integer.valueOf(7).equals(san.get(0)) && address.equals(san.get(1)))) throw invalid();
                leaves++;
            }
            if (leaves != 1) throw invalid();
            Map<String, Object> values = new HashMap<>();
            values.put("server.address", address); values.put("server.port", port);
            values.put("server.ssl.enabled", true); values.put("server.ssl.key-store", store.toUri().toString());
            values.put("server.ssl.key-store-type", "PKCS12"); values.put("server.ssl.key-store-password", pass);
            values.put("server.ssl.enabled-protocols", "TLSv1.2,TLSv1.3");
            values.put("gorilla.mobile.enabled", true);
            return values;
        } catch (Exception failure) {
            // Never surface file contents, password, payload or nested provider exception messages.
            throw invalid();
        } finally { if (password != null) Arrays.fill(password, '\0'); }
    }

    static boolean privateIpv4(String text) {
        if (!text.matches("[0-9]{1,3}(\\.[0-9]{1,3}){3}")) return false;
        String[] parts = text.split("\\."); int[] octets = new int[4];
        for (int i = 0; i < 4; i++) {
            octets[i] = Integer.parseInt(parts[i]);
            if (octets[i] > 255 || !Integer.toString(octets[i]).equals(parts[i])) return false;
        }
        return octets[0] == 10 || (octets[0] == 172 && octets[1] >= 16 && octets[1] <= 31)
                || (octets[0] == 192 && octets[1] == 168);
    }
    private static Path absoluteFile(String value) throws IOException {
        Path path = Path.of(value);
        if (!path.isAbsolute() || !Files.isRegularFile(path)) throw invalid();
        Path real = path.toRealPath();
        for (Path parent = real.getParent(); parent != null; parent = parent.getParent()) {
            if (Files.exists(parent.resolve(".git"))) throw invalid();
        }
        return real;
    }
    private static String required(Properties config, String key) {
        String value = config.getProperty(key);
        if (value == null || value.isBlank()) throw invalid();
        return value;
    }
    private static IllegalArgumentException invalid() { return new IllegalArgumentException("MOBILE_HTTPS_CONFIG_INVALID"); }
}
