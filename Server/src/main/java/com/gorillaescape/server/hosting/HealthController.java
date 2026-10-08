package com.gorillaescape.server.hosting;

import com.gorillaescape.server.protocol.ProtocolVersion;
import java.util.Map;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RestController;
import jakarta.servlet.http.HttpServletRequest;

@RestController
public class HealthController {
    private final HostingDiagnostics diagnostics;
    public HealthController(HostingDiagnostics diagnostics) { this.diagnostics = diagnostics; }
    @GetMapping("/api/health")
    public Map<String, Object> health(HttpServletRequest request) {
        return Map.of("status", "ok", "protocolVersion", ProtocolVersion.CURRENT,
                "service", "gorilla-escape-local", "serverInstanceId", diagnostics.instanceId(),
                "requestNumber", diagnostics.confirmHealth(), "secure", request.isSecure());
    }
}
