package com.gorillaescape.server.hosting;

import java.net.*;
import java.nio.file.*;
import java.util.*;

/** Select only linked physical LAN adapters; ambiguity is an operator decision. */
public final class LanInterface {
    public record Candidate(String name, String address) { }
    private LanInterface() { }
    public static Candidate select(String name, String address) throws Exception {
        var candidates = new ArrayList<Candidate>();
        for (var n : Collections.list(NetworkInterface.getNetworkInterfaces())) {
            if (!n.isUp() || n.isLoopback() || n.isVirtual() || n.isPointToPoint()
                || n.getName().matches("(?i)^(tun|tap|wg|vpn|docker|veth|virbr|vmnet|vboxnet|tailscale|zt|br[-0-9]).*")) continue;
            if (System.getProperty("os.name").toLowerCase(Locale.ROOT).contains("linux")
                && !Files.exists(Path.of("/sys/class/net", n.getName(), "device"))) continue;
            for (var ip : Collections.list(n.getInetAddresses()))
                if (ip instanceof Inet4Address && MobileHttpsSettings.privateIpv4(ip.getHostAddress()))
                    candidates.add(new Candidate(n.getName(), ip.getHostAddress()));
        }
        return choose(candidates, name, address);
    }
    public static Candidate choose(List<Candidate> candidates, String name, String address) {
        var matched = candidates.stream().filter(c -> MobileHttpsSettings.privateIpv4(c.address()))
            .filter(c -> name == null || name.isBlank() || name.equals(c.name()))
            .filter(c -> address == null || address.isBlank() || address.equals(c.address())).toList();
        if (matched.size() != 1) throw new IllegalArgumentException(matched.isEmpty() ? "LAN_NOT_AVAILABLE" : "LAN_SELECTION_REQUIRED");
        return matched.getFirst();
    }
}
