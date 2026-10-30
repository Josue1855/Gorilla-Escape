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
 @Test void validatesAuthoritativeStatsAndRejectsUnboundedValues()throws Exception {
  var context=GorillaProtocol.JSON.readTree("{\"version\":1,\"gameId\":\"log-balance\",\"phase\":\"Won\",\"playerId\":1,\"stats\":{\"balance\":0.2,\"tendency\":0.3,\"progress\":1,\"distance\":18,\"elapsed\":22.7,\"roll\":10,\"speed\":0.88,\"wind\":0.2}}");
  GamePresentation.validate(context);
  ((tools.jackson.databind.node.ObjectNode)context.path("stats")).put("progress",2);
  assertThrows(java.io.IOException.class,()->GamePresentation.validate(context));
  ((tools.jackson.databind.node.ObjectNode)context.path("stats")).remove("progress");
  assertThrows(java.io.IOException.class,()->GamePresentation.validate(context));
 }

 @Test void v2ValidatesAllSevenGamesAndRejectsExtraOrNonFiniteStats()throws Exception{
   for(String game:java.util.List.of("bowling","boxing","baseball","drums","coconut-slice","whack","log-balance")){
     var context=GorillaProtocol.JSON.createObjectNode();context.put("version",2);context.put("gameId",game);context.put("phase","Playing");context.put("playerId",1);
     var stats=context.putObject("stats");for(String key:java.util.List.of("Score","Progress","Elapsed","Balance","Distance")){stats.put("has"+key,true);stats.put(Character.toLowerCase(key.charAt(0))+key.substring(1),0);}
     GamePresentation.validate(context);stats.put("score",Double.NaN);assertThrows(java.io.IOException.class,()->GamePresentation.validate(context));
     stats.put("score",123);stats.put("extra",0);assertThrows(java.io.IOException.class,()->GamePresentation.validate(context));
   }
 }

}
