package com.gorillaescape.server;

import static org.junit.jupiter.api.Assertions.*;
import com.gorillaescape.server.protocol.ProtocolEnvelope;
import org.junit.jupiter.api.Test;
import tools.jackson.databind.ObjectMapper;

class ProtocolContractTest {
    @Test void readsAndRoundTripsTheSharedSensorFixture() throws Exception {
        try (var stream = getClass().getResourceAsStream("/protocol-fixtures/sensor.json")) {
            assertNotNull(stream);
            var mapper = new ObjectMapper();
            var source = mapper.readTree(stream);
            var envelope = mapper.treeToValue(source, ProtocolEnvelope.class);
            assertEquals(1, envelope.version());
            assertEquals(2, envelope.playerId());
            assertEquals(source, mapper.readTree(mapper.writeValueAsString(envelope)));
        }
    }
}
