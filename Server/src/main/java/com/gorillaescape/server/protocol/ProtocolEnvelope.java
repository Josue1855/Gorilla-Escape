package com.gorillaescape.server.protocol;

/** Wire DTO from specification section 8. No game authority or transport logic. */
public record ProtocolEnvelope<T>(int version, String type, String sessionId,
        int playerId, long sequence, long timestamp, T payload) {}
