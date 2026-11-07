package com.gorillaescape.server.mobile;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.io.TempDir;
import static org.junit.jupiter.api.Assertions.*;
import static org.junit.jupiter.api.Assumptions.*;
import java.nio.file.Path;
class PlayersBackendTest {
 @TempDir Path data;
 @Test void registeredPlayerHasPersistentUuidAndOwnedProcessStops()throws Exception {
  String jar=System.getProperty("gorilla.test.playersJar");assumeTrue(jar!=null,"Requires packaged players backend");
  var backend=new PlayersBackend(Path.of(jar),data);
  try {
   long deadline=System.nanoTime()+30_000_000_000L;
   while(!backend.ready()&&System.nanoTime()<deadline)Thread.sleep(100);
   assertTrue(backend.ready(),"Backend must start with loopback ephemeral port");
   var profile=backend.create("Náufrago QA");
   assertEquals("Náufrago QA",profile.get("displayName"));
   assertDoesNotThrow(()->java.util.UUID.fromString((String)profile.get("id")));
   assertTrue(java.nio.file.Files.list(data).findAny().isPresent());
  }finally{backend.destroy();}
  assertFalse(backend.ready());assertTrue(backend.cleanupComplete());
 }
 @Test void unconfiguredBackendDoesNotPretendToSave()throws Exception {
  var backend=new PlayersBackend(null,data);
  try{assertThrows(java.io.IOException.class,()->backend.create("QA"));}finally{backend.destroy();}
 }
 @Test void unjoinedPeerCannotRegisterAPlayer()throws Exception {
  var runtime=new MobileRuntime();
  try{assertThrows(java.io.IOException.class,()->runtime.createProfile(new MobileRuntime.ProfileRequest("not-a-peer","not-a-token","QA")));}
  finally{runtime.destroy();}
 }
}
