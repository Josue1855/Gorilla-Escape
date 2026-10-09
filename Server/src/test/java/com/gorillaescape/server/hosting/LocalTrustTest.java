package com.gorillaescape.server.hosting;

import static org.junit.jupiter.api.Assertions.*;
import java.nio.file.*;
import java.security.cert.*;
import java.time.*;
import java.util.*;
import com.gorillaescape.server.mobile.MobileOrigin;
import org.junit.jupiter.api.*;
import org.junit.jupiter.api.io.TempDir;

class LocalTrustTest {
    @TempDir Path directory;
    @Test void uniqueRootReuseDhcpLeafAndRealCertificateRules() throws Exception {
        var a=LocalTrust.prepare(directory.resolve("installation-a"),"192.168.1.37");
        var b=LocalTrust.prepare(directory.resolve("installation-b"),"192.168.1.37");
        assertNotEquals(a.fingerprint(),b.fingerprint());
        assertEquals(-1,a.leaf().getBasicConstraints());
        assertTrue(a.leaf().getSubjectAlternativeNames().stream().anyMatch(s->Integer.valueOf(7).equals(s.get(0))&&s.get(1).equals("192.168.1.37")));
        var reused=LocalTrust.prepare(directory.resolve("installation-a"),"192.168.1.37");
        assertArrayEquals(a.leaf().getEncoded(),reused.leaf().getEncoded());
        var dhcp=LocalTrust.prepare(directory.resolve("installation-a"),"192.168.1.38");
        assertEquals(a.fingerprint(),dhcp.fingerprint());assertFalse(Arrays.equals(a.leaf().getEncoded(),dhcp.leaf().getEncoded()));
        assertThrows(CertificateException.class,()->LocalTrust.validateLeaf(a.leaf(),a.root(),"192.168.1.38",Instant.now()));
        assertThrows(CertificateExpiredException.class,()->LocalTrust.validateLeaf(a.leaf(),a.root(),"192.168.1.37",Instant.now().plus(Duration.ofDays(8))));
        assertThrows(java.security.GeneralSecurityException.class,()->LocalTrust.validateLeaf(a.leaf(),b.root(),"192.168.1.37",Instant.now()));
        assertEquals(Set.of(java.nio.file.attribute.PosixFilePermission.OWNER_READ,java.nio.file.attribute.PosixFilePermission.OWNER_WRITE),Files.getPosixFilePermissions(a.passwordFile()));
        var factory=CertificateFactory.getInstance("X.509");
        try(var in=Files.newInputStream(directory.resolve("installation-a/root.cer"))) {
            assertArrayEquals(a.root().getEncoded(),factory.generateCertificate(in).getEncoded());
        }
    }
    @Test void noSilentCaRecoveryOrUnsafeStore() throws Exception {
        var installation=directory.resolve("partial");Files.createDirectory(installation);Files.setPosixFilePermissions(installation,java.nio.file.attribute.PosixFilePermissions.fromString("rwx------"));
        Files.writeString(installation.resolve("root.p12"),"incomplete");
        assertThrows(java.io.IOException.class,()->LocalTrust.prepare(installation,"192.168.1.37"));
        assertThrows(java.io.IOException.class,()->LocalTrust.prepare(directory.resolve("loopback"),"127.0.0.1"));
        var unsafe=directory.resolve("unsafe");Files.createDirectory(unsafe);Files.setPosixFilePermissions(unsafe,java.nio.file.attribute.PosixFilePermissions.fromString("rwxrwxrwx"));
        assertThrows(java.io.IOException.class,()->LocalTrust.prepare(unsafe,"192.168.1.37"));
    }
    @Test void interfaceSelectionAndProductOrigins() {
        var wifi=new LanInterface.Candidate("wifi","192.168.1.37");var ethernet=new LanInterface.Candidate("ethernet","10.0.0.7");
        assertEquals(wifi,LanInterface.choose(List.of(wifi),null,null));
        assertEquals(ethernet,LanInterface.choose(List.of(wifi,ethernet),"ethernet",null));
        assertThrows(IllegalArgumentException.class,()->LanInterface.choose(List.of(wifi,ethernet),null,null));
        assertThrows(IllegalArgumentException.class,()->LanInterface.choose(List.of(),null,null));
        assertThrows(IllegalArgumentException.class,()->LanInterface.choose(List.of(new LanInterface.Candidate("lo","127.0.0.1")),null,null));
        assertEquals("https://192.168.1.37:8443/mobile-lab/index.html",MobileOrigin.page("https://192.168.1.37:8443","192.168.1.37",8443).toString());
        for(String bad:List.of("http://192.168.1.37:8443","https://127.0.0.1:8443","https://localhost:8443","https://fake.example:8443","https://192.168.1.38:8443","https://192.168.1.37:8443?secret=x","https://192.168.1.37:8443/old"))
            assertThrows(IllegalArgumentException.class,()->MobileOrigin.page(bad,"192.168.1.37",8443));
    }
}
