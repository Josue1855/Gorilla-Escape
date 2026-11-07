package com.gorillaescape.server.connectivity;

import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;
import java.net.URI;
import java.nio.file.*;
import java.util.Map;
import static org.junit.jupiter.api.Assertions.*;

class PortableTunnelTest {
    @TempDir Path home;
    private final URI ingress=URI.create("http://127.0.0.1:4567");

    @Test void freshPcUsesQuickTunnelWithoutCredentials(){
        assertNull(CloudflaredSupervisor.resolveNamedTunnel(ingress,Map.of(),home));
    }
    @Test void existingUnrelatedCredentialsAreNeverSelected()throws Exception{
        Files.createDirectories(home.resolve(".cloudflared"));
        Files.writeString(home.resolve(".cloudflared/another-project.json"),"{}");
        assertNull(CloudflaredSupervisor.resolveNamedTunnel(ingress,Map.of(),home));
        assertNull(CloudflaredSupervisor.resolveNamedTunnel(ingress,
            Map.of("GORILLA_TUNNEL_DOMAIN","play.gorillas.run"),home));
    }
    @Test void explicitNamedTunnelStillWorks()throws Exception{
        Files.createDirectories(home.resolve(".cloudflared"));
        Files.writeString(home.resolve(".cloudflared/selected.json"),"{}");
        var spec=CloudflaredSupervisor.resolveNamedTunnel(ingress,Map.of("GORILLA_TUNNEL_ID","selected"),home);
        assertNotNull(spec);
        try{
            assertEquals("selected",spec.tunnelId());
            assertTrue(Files.readString(spec.configFile()).contains(ingress.toString()));
        }finally{Files.deleteIfExists(spec.configFile());}
    }
}
