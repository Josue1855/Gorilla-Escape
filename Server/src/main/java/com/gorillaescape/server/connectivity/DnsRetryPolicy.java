package com.gorillaescape.server.connectivity;

import java.security.Security;

/** Process-local startup policy: a new Quick Tunnel name may briefly return NXDOMAIN. */
public final class DnsRetryPolicy {
    private DnsRetryPolicy() { }
    public static void configure() {
        // Must precede InetAddress initialization. Keep successful DNS and TLS policy unchanged.
        // One second bounds duplicate failed lookups without spending ten seconds of readiness on a stale miss.
        Security.setProperty("networkaddress.cache.negative.ttl", "1");
    }
}
