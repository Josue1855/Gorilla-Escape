package com.gorillaescape.server.connectivity;

import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;

class QuickTunnelDnsProbeTest {
    @Test void selectsOnlyValidPublicIpv4Answers() throws Exception {
        var response="{\"Status\":0,\"Answer\":[{\"type\":5,\"data\":\"alias.example\"},{\"type\":1,\"data\":\"127.0.0.1\"},{\"type\":1,\"data\":\"104.16.1.2\"}]}";
        assertEquals("104.16.1.2",QuickTunnelDnsProbe.addressFromJson(response).getHostAddress());
    }
    @Test void rejectsNegativePrivateAndMalformedAnswers(){
        for(String response:new String[]{"{\"Status\":3}","{\"Status\":0,\"Answer\":[{\"type\":1,\"data\":\"192.168.1.1\"}]}","{\"Status\":0,\"Answer\":[{\"type\":1,\"data\":\"100.64.0.1\"}]}","{\"Status\":0,\"Answer\":[{\"type\":1,\"data\":\"1.2.3.999\"}]}"})
            assertThrows(Exception.class,()->QuickTunnelDnsProbe.addressFromJson(response));
    }
    @Test void fallbackDoesNotApplyToOtherOrigins() throws Exception {
        assertFalse(QuickTunnelDnsProbe.ready(java.net.URI.create("https://example.com"),100));
    }
}
