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
    private static final Set<String> KEYS = Set.of("address", "interface", "operatorConfirmed", "port", "hostname", "publicOrigin", "keyStore", "passwordFile");
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
            privateFile(file.toString());
            Properties config = new Properties();
            try (var input = Files.newInputStream(file)) { config.load(input); }
            if (!KEYS.containsAll(config.stringPropertyNames()) || !"true".equals(config.getProperty("operatorConfirmed"))) throw invalid();
            String address = required(config, "address"), name = required(config, "interface");
            if (!privateIpv4(address)) throw invalid();
            String hostname = required(config, "hostname");
            String origin = required(config, "publicOrigin");
            if (!dnsHostname(hostname) || !origin.equals("https://" + hostname)) throw invalid();
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
            Path store = privateFile(required(config, "keyStore"));
            if (Files.size(store) > 1024 * 1024) throw invalid();
            Path secret = privateFile(required(config, "passwordFile"));
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
                validateChain(keys.getCertificateChain(alias));
                certificate.checkValidity();
                if (certificate.getBasicConstraints() >= 0) throw invalid();
                var sans = certificate.getSubjectAlternativeNames();
                if (sans == null || sans.stream().noneMatch(san -> Integer.valueOf(2).equals(san.get(0)) && hostname.equalsIgnoreCase(String.valueOf(san.get(1))))) throw invalid();
                leaves++;
            }
            if (leaves != 1) throw invalid();
            Map<String, Object> values = new HashMap<>();
            values.put("server.address", address); values.put("server.port", port);
            values.put("server.ssl.enabled", true); values.put("server.ssl.key-store", store.toUri().toString());
            values.put("server.ssl.key-store-type", "PKCS12"); values.put("server.ssl.key-store-password", pass);
            values.put("server.ssl.enabled-protocols", "TLSv1.2,TLSv1.3");
            values.put("gorilla.mobile.enabled", true);
            values.put("gorilla.mobile.public-origin", origin);
            return values;
        } catch (Exception failure) {
            // Never surface file contents, password, payload or nested provider exception messages.
            throw invalid();
        } finally { if (password != null) Arrays.fill(password, '\0'); }
    }

    // A specific DNS hostname, never a wildcard, IP literal, URL or local single-label name.
    static boolean dnsHostname(String value) {
        if (value.length() > 253 || !value.equals(value.toLowerCase(java.util.Locale.ROOT))) return false;
        String[] labels = value.split("\\.", -1);
        if (labels.length < 2 || !labels[labels.length - 1].matches("[a-z]{2,63}")) return false;
        for (String label : labels) {
            if (!label.matches("[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?")) return false;
        }
        return true;
    }

    // Check an ordered, complete issuer chain. This is not a claim of phone/public trust:
    // ephemeral test roots are permitted; operators must supply a publicly trusted certificate.
    static void validateChain(java.security.cert.Certificate[] chain) throws Exception {
        if (chain == null || chain.length < 2 || chain.length > 8) throw invalid();
        for (int i = 0; i < chain.length; i++) {
            if (!(chain[i] instanceof X509Certificate current)) throw invalid();
            current.checkValidity();
            if (i == 0) {
                if (current.getBasicConstraints() >= 0) throw invalid();
                var usage = current.getExtendedKeyUsage();
                if (usage != null && !usage.contains("1.3.6.1.5.5.7.3.1")) throw invalid();
                if (current.getKeyUsage() != null && !current.getKeyUsage()[0]) throw invalid();
            } else {
                if (current.getBasicConstraints() < i - 1) throw invalid();
                if (current.getKeyUsage() != null && (current.getKeyUsage().length <= 5 || !current.getKeyUsage()[5])) throw invalid();
            }
            if (i + 1 < chain.length) {
                var issuer = (X509Certificate) chain[i + 1];
                if (!current.getIssuerX500Principal().equals(issuer.getSubjectX500Principal())) throw invalid();
                current.verify(issuer.getPublicKey());
            }
        }
        var last = (X509Certificate) chain[chain.length - 1];
        if (last.getSubjectX500Principal().equals(last.getIssuerX500Principal())) {
            last.verify(last.getPublicKey());
            validatePath(chain, last, true);
            return;
        }
        // Public servers normally omit the root. Resolve that final issuer in the JRE trust store.
        var factory = javax.net.ssl.TrustManagerFactory.getInstance(javax.net.ssl.TrustManagerFactory.getDefaultAlgorithm());
        factory.init((KeyStore) null);
        for (var manager : factory.getTrustManagers()) {
            if (manager instanceof javax.net.ssl.X509TrustManager trust) {
                for (var root : trust.getAcceptedIssuers()) {
                    if (last.getIssuerX500Principal().equals(root.getSubjectX500Principal())) {
                        try { root.checkValidity(); last.verify(root.getPublicKey()); validatePath(chain, root, false); return; }
                        catch (java.security.GeneralSecurityException mismatch) { /* Try another matching root. */ }
                    }
                }
            }
        }
        throw invalid();
    }

    private static void validatePath(java.security.cert.Certificate[] chain, X509Certificate root, boolean rootIncluded)
            throws java.security.GeneralSecurityException {
        var certificates = Arrays.asList(chain).subList(0, chain.length - (rootIncluded ? 1 : 0));
        var path = java.security.cert.CertificateFactory.getInstance("X.509").generateCertPath(certificates);
        var parameters = new java.security.cert.PKIXParameters(Set.of(new java.security.cert.TrustAnchor(root, null)));
        // Offline preparation validates the path; no network revocation lookup during startup.
        parameters.setRevocationEnabled(false);
        java.security.cert.CertPathValidator.getInstance("PKIX").validate(path, parameters);
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
    private static Path privateFile(String value) throws IOException {
        Path file = absoluteFile(value);
        if (Files.getFileStore(file).supportsFileAttributeView("posix")) {
            var permissions = Files.getPosixFilePermissions(file);
            if (permissions.stream().anyMatch(permission -> permission.name().startsWith("GROUP_")
                    || permission.name().startsWith("OTHERS_"))) throw invalid();
        }
        return file;
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
