package com.gorillaescape.server.ipc;

import java.io.*;
import java.nio.charset.*;
import java.util.*;
import tools.jackson.core.StreamReadFeature;
import tools.jackson.core.StreamReadConstraints;
import tools.jackson.core.json.JsonFactory;
import tools.jackson.databind.DeserializationFeature;
import tools.jackson.databind.JsonNode;
import tools.jackson.databind.json.JsonMapper;

/** Internal probe contract, unrelated to the mobile sensor envelope. */
public final class ProbeCodec {
    public static final int MAX_FRAME = 4096;
    private static final JsonMapper JSON = JsonMapper.builder(JsonFactory.builder().streamReadConstraints(StreamReadConstraints.builder().maxNestingDepth(4).build()).build())
            .enable(StreamReadFeature.STRICT_DUPLICATE_DETECTION)
            .enable(DeserializationFeature.FAIL_ON_TRAILING_TOKENS).build();
    private ProbeCodec() {}
    public static String read(InputStream input) throws IOException {
        var data = new DataInputStream(input);
        int size = data.readInt();
        if (size < 1 || size > MAX_FRAME) throw new IOException("IPC_FRAME_SIZE");
        byte[] body = new byte[size];
        data.readFully(body);
        try {
            return StandardCharsets.UTF_8.newDecoder().onMalformedInput(CodingErrorAction.REPORT)
                    .onUnmappableCharacter(CodingErrorAction.REPORT).decode(java.nio.ByteBuffer.wrap(body)).toString();
        } catch (CharacterCodingException e) { throw new IOException("IPC_UTF8", e); }
    }
    public static void write(OutputStream output, String body) throws IOException {
        if (body == null || body.length() > MAX_FRAME) throw new IOException("IPC_FRAME_SIZE");
        final byte[] bytes;
        try {
            var encoded = java.nio.ByteBuffer.allocate(MAX_FRAME);
            var encoder = StandardCharsets.UTF_8.newEncoder().onMalformedInput(CodingErrorAction.REPORT)
                    .onUnmappableCharacter(CodingErrorAction.REPORT);
            var status = encoder.encode(java.nio.CharBuffer.wrap(body), encoded, true);
            if (status.isOverflow()) throw new IOException("IPC_FRAME_SIZE");
            if (status.isError()) status.throwException();
            status = encoder.flush(encoded);
            if (status.isOverflow()) throw new IOException("IPC_FRAME_SIZE");
            if (status.isError()) status.throwException();
            encoded.flip();
            bytes = new byte[encoded.remaining()]; encoded.get(bytes);
        } catch (CharacterCodingException e) { throw new IOException("IPC_UTF8"); }
        if (bytes.length < 1 || bytes.length > MAX_FRAME) throw new IOException("IPC_FRAME_SIZE");
        var data = new DataOutputStream(output);
        data.writeInt(bytes.length); data.write(bytes); data.flush();
    }
    public record Ping(String instanceId, String connectionId, int sequence) {}
    public static Ping ping(String body, String instance, String connection, int expectedSequence,
                            String token) throws IOException {
        return envelope(body, instance, connection, expectedSequence, token, "PING");
    }
    public static Ping validatePong(String body, String instance, String connection, int sequence) throws IOException {
        return envelope(body, instance, connection, sequence, null, "PONG");
    }
    private static Ping envelope(String body, String instance, String connection, int expectedSequence, String token, String type) throws IOException {
        try {
            JsonNode node = JSON.readTree(body);
            exact(node, Set.of("ipcVersion", "type", "instanceId", "connectionId", "sequence", "payload"));
            if (!node.get("ipcVersion").isInt() || node.get("ipcVersion").intValue() != 1
                    || !node.get("type").isTextual() || !type.equals(node.get("type").asString())
                    || !node.get("instanceId").isTextual() || !node.get("connectionId").isTextual()
                    || !instance.equals(node.get("instanceId").asString())
                    || expectedSequence < 1 || !node.get("sequence").isInt() || node.get("sequence").intValue() != expectedSequence)
                throw new IOException("IPC_CONTRACT");
            String id = node.get("connectionId").asString();
            if (!UUID.fromString(id).toString().equals(id) || (connection != null && !connection.equals(id)))
                throw new IOException("IPC_IDENTITY");
            JsonNode payload = node.get("payload");
            exact(payload, token == null ? Set.of() : Set.of("launchToken"));
            if (token != null && (!payload.get("launchToken").isTextual() || !payload.get("launchToken").asString().matches("[A-Za-z0-9_-]{43}") || !java.security.MessageDigest.isEqual(token.getBytes(StandardCharsets.US_ASCII),
                    payload.get("launchToken").asString().getBytes(StandardCharsets.US_ASCII))))
                throw new IOException("IPC_TOKEN");
            return new Ping(instance, id, expectedSequence);
        } catch (IOException e) { throw e; }
        catch (RuntimeException e) { throw new IOException("IPC_JSON_INVALID"); }
    }
    private static void exact(JsonNode node, Set<String> expected) throws IOException {
        if (node == null || !node.isObject() || node.size() != expected.size()) throw new IOException("IPC_FIELDS");
        for (String field : expected) if (!node.has(field)) throw new IOException("IPC_FIELDS");
    }
    public static String pong(Ping ping) {
        return "{\"ipcVersion\":1,\"type\":\"PONG\",\"instanceId\":\"" + ping.instanceId()
                + "\",\"connectionId\":\"" + ping.connectionId() + "\",\"sequence\":" + ping.sequence()
                + ",\"payload\":{}}";
    }
}
