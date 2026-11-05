package com.gorillaescape.server.mobile;
import org.junit.jupiter.api.Test;import java.io.IOException;import static org.junit.jupiter.api.Assertions.*;
class LocalSignalingTest {
 @Test void hostOnlyCandidates(){assertDoesNotThrow(()->MobileRuntime.localOffer("v=0\r\na=candidate:1 1 udp 123 192.168.1.2 5555 typ host\r\n"));assertDoesNotThrow(()->MobileRuntime.localOffer("v=0\r\na=candidate:1 1 udp 123 abc-def.local 5555 typ host\r\n"));assertThrows(IOException.class,()->MobileRuntime.localOffer("v=0\r\na=candidate:1 1 udp 123 8.8.8.8 5555 typ host\r\n"));assertThrows(IOException.class,()->MobileRuntime.localOffer("v=0\r\na=candidate:1 1 udp 123 192.168.1.2 5555 typ relay\r\n"));}
 @Test void publicTunnelAcceptsSrflxCandidates(){assertDoesNotThrow(()->MobileRuntime.localOffer("v=0\r\na=candidate:1 1 udp 123 192.168.1.2 5555 typ host\r\na=candidate:2 1 udp 124 189.200.50.25 5556 typ srflx\r\n",true));assertDoesNotThrow(()->MobileRuntime.localOffer("v=0\r\na=candidate:1 1 udp 123 abc-def.local 5555 typ host\r\na=candidate:2 1 udp 124 189.200.50.25 5556 typ srflx\r\n",true));}
}
