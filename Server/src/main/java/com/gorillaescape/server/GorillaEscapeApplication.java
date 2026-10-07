package com.gorillaescape.server;

import com.gorillaescape.server.ipc.ManagedProcessGuard;
import com.gorillaescape.server.ipc.ParentLifetime;
import java.io.IOException;
import java.nio.file.Path;
import java.util.Arrays;
import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;

@SpringBootApplication
public class GorillaEscapeApplication {
    private static volatile ParentLifetime parent;
    public static ParentLifetime managedParent() { return parent; }
    public static void main(String[] args) throws Exception {
        if (!Arrays.asList(args).contains("--gorilla.ipc.managed=true")) {
            SpringApplication.run(GorillaEscapeApplication.class, args);
            return;
        }
        String directory = System.getenv("GORILLA_IPC_LOCK_DIR");
        if (directory == null || directory.isBlank()) {
            bootstrapError("LOCK_UNAVAILABLE"); System.exit(74); return;
        }
        ManagedProcessGuard guard;
        try { guard = new ManagedProcessGuard(Path.of(directory)); }
        catch (IOException | RuntimeException failure) {
            boolean busy = "ALREADY_RUNNING".equals(failure.getMessage());
            bootstrapError(busy ? "ALREADY_RUNNING" : "LOCK_UNAVAILABLE");
            System.exit(busy ? 73 : 74); return;
        }
        try (guard) {
            parent = new ParentLifetime();
            try (var context = SpringApplication.run(GorillaEscapeApplication.class, args)) {
                parent.awaitEof();
            }
        } finally { parent = null; }
    }
    private static void bootstrapError(String code) {
        System.err.println("{\"component\":\"ipc-java\",\"event\":\"" + code + "\"}");
    }
}
