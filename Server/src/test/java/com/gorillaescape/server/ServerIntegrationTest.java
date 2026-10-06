package com.gorillaescape.server;

import static org.junit.jupiter.api.Assertions.*;
import java.net.URI;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.test.context.SpringBootTest;

@SpringBootTest(webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
class ServerIntegrationTest {
    @Value("${local.server.port}") int port;
    private HttpResponse<String> get(String path) throws Exception {
        return HttpClient.newHttpClient().send(
            HttpRequest.newBuilder(URI.create("http://127.0.0.1:" + port + path)).build(),
            HttpResponse.BodyHandlers.ofString());
    }
    @Test void healthExposesProtocolWithoutPlayerData() throws Exception {
        var r = get("/api/health");
        assertEquals(200, r.statusCode());
        assertTrue(r.body().contains("\"protocolVersion\":1"));
        assertTrue(r.body().contains("\"status\":\"ok\""));
        assertFalse(r.body().contains("playerId"));
    }
    @Test void servesPackagedReactShell() throws Exception {
        var r = get("/");
        assertEquals(200, r.statusCode());
        assertTrue(r.body().contains("id=\"root\""));
        assertTrue(r.body().contains("manifest.webmanifest"));
    }
    @Test void unknownApiDoesNotReturnTheApplicationShell() throws Exception {
        assertEquals(404, get("/api/not-a-route").statusCode());
    }
}
