package com.gorillaescape.server.hosting;

import java.io.IOException;
import java.nio.file.*;
import java.nio.file.attribute.*;
import java.util.*;

/**
 * Owner-only file permissions on POSIX (rwx------ / rw-------) and on Windows/NTFS
 * (a single ACL entry granting the current owner full control, no inheritance).
 */
final class OwnerOnlyFiles {
    private OwnerOnlyFiles() { }

    static boolean posix(Path any) throws IOException {
        Path probe = any;
        while (probe != null && !Files.exists(probe)) probe = probe.getParent();
        return probe != null && Files.getFileStore(probe).supportsFileAttributeView("posix");
    }

    static void createDirectories(Path dir) throws IOException {
        if (posix(dir)) Files.createDirectories(dir, PosixFilePermissions.asFileAttribute(PosixFilePermissions.fromString("rwx------")));
        else {
            boolean existed = Files.exists(dir, LinkOption.NOFOLLOW_LINKS);
            Files.createDirectories(dir);
            if (!existed) restrict(dir);
        }
    }

    static Path createTempDirectory(Path parent, String prefix) throws IOException {
        if (posix(parent)) return Files.createTempDirectory(parent, prefix, PosixFilePermissions.asFileAttribute(PosixFilePermissions.fromString("rwx------")));
        Path dir = Files.createTempDirectory(parent, prefix); restrict(dir); return dir;
    }

    /** True when the file is accessible only by its owner (and, on Windows, SYSTEM/Administrators). */
    static boolean isOwnerOnly(Path f, boolean directory) throws IOException {
        if (posix(f)) return Files.getPosixFilePermissions(f).equals(PosixFilePermissions.fromString(directory ? "rwx------" : "rw-------"));
        var view = Files.getFileAttributeView(f, AclFileAttributeView.class, LinkOption.NOFOLLOW_LINKS);
        if (view == null) throw new IOException("TRUST_PERMISSIONS_UNSUPPORTED");
        var owner = view.getOwner();
        boolean hasOwner = false;
        for (AclEntry e : view.getAcl()) {
            if (e.type() != AclEntryType.ALLOW) continue;
            var p = e.principal();
            if (p.equals(owner)) { hasOwner = true; continue; }
            if (trustedSystem(p)) continue;
            return false;
        }
        return hasOwner;
    }

    static void restrict(Path f) throws IOException {
        if (posix(f)) {
            Files.setPosixFilePermissions(f, PosixFilePermissions.fromString(Files.isDirectory(f) ? "rwx------" : "rw-------"));
            return;
        }
        var view = Files.getFileAttributeView(f, AclFileAttributeView.class, LinkOption.NOFOLLOW_LINKS);
        if (view == null) throw new IOException("TRUST_PERMISSIONS_UNSUPPORTED");
        var flags = Files.isDirectory(f) ? EnumSet.of(AclEntryFlag.FILE_INHERIT, AclEntryFlag.DIRECTORY_INHERIT) : EnumSet.noneOf(AclEntryFlag.class);
        var entry = AclEntry.newBuilder().setType(AclEntryType.ALLOW).setPrincipal(view.getOwner())
            .setPermissions(EnumSet.allOf(AclEntryPermission.class)).setFlags(flags).build();
        view.setAcl(List.of(entry));
    }

    private static boolean trustedSystem(UserPrincipal p) {
        String n = p.getName().toUpperCase(Locale.ROOT);
        return n.endsWith("\\SYSTEM") || n.equals("SYSTEM") || n.endsWith("\\ADMINISTRATORS") || n.endsWith("\\ADMINISTRADORES");
    }
}
