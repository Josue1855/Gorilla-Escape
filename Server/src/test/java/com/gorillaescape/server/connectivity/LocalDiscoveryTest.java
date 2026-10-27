package com.gorillaescape.server.connectivity;
import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;
import java.net.URI;
class LocalDiscoveryTest {
 @Test void announcementContainsOnlyLocalDiscoveryMetadata(){
  var txt=LocalDiscovery.attributes(URI.create("https://game.trycloudflare.com/"));
  assertEquals(3,txt.size());assertEquals("1",txt.get("v"));assertEquals("https://game.trycloudflare.com",txt.get("origin"));
  assertFalse(txt.containsKey("pin"));assertFalse(txt.containsKey("admission"));
 }
 @Test void rejectsCredentialsPathsAndNonHttps(){
  for(var uri:new String[]{"http://example.com","https://u:p@example.com","https://example.com/#admission=x","https://example.com/path","https://example.com/?pin=x"})assertThrows(IllegalArgumentException.class,()->LocalDiscovery.attributes(URI.create(uri)));
 }
 @Test void stoppingBeforeDiscoveryStartsDoesNotCreateAPublisher(){
  var discovery=new LocalDiscovery("127.0.0.1",8443);discovery.close();discovery.close();
  assertDoesNotThrow(()->discovery.observe(URI.create("https://game.trycloudflare.com")));
 }
}
