package com.gorillaescape.server.mobile;
import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;
import com.gorillaescape.server.protocol.GorillaProtocol;
class GamePresentationTest {
 @Test void validatesBoundedContext()throws Exception {
  GamePresentation.validate(GorillaProtocol.JSON.readTree("{\"version\":1,\"gameId\":\"log-balance\",\"phase\":\"Paused\",\"playerId\":2}"));
  for(String text:new String[]{"{}","{\"version\":1,\"gameId\":\"unknown\",\"phase\":\"Ready\",\"playerId\":2}","{\"version\":1,\"gameId\":\"lobby\",\"phase\":\"Ready\",\"playerId\":9}"})
   assertThrows(java.io.IOException.class,()->GamePresentation.validate(GorillaProtocol.JSON.readTree(text)));
 }
}
