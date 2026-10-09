package com.gorillaescape.server.mobile;
import org.junit.jupiter.api.Test;
import static org.junit.jupiter.api.Assertions.*;
class JoinDiagnosticTest {
 @Test void candidateMetadataAndBoundsNeverRetainAddresses(){var d=new JoinDiagnostic();for(var address:new String[]{"192.168.1.5","fd00::5","secret-name.local"}){var safe=JoinDiagnostic.sanitizeCandidate("candidate:1 1 UDP 123 "+address+" 52741 typ host");assertNotNull(safe);assertFalse(safe.toString().contains(address));assertEquals(52741,safe.get("port"));}assertNull(JoinDiagnostic.sanitizeCandidate("invalid secret"));for(int i=0;i<100;i++){d.candidate("candidate:1 1 udp 123 secret-name.local 52741 typ host");d.stage(JoinDiagnostic.Stage.SERVER_ICE);}var snapshot=d.snapshot();assertEquals(32,((java.util.List<?>)snapshot.get("candidates")).size());assertEquals(64,((java.util.List<?>)snapshot.get("events")).size());assertFalse(snapshot.toString().contains("secret-name"));}
 @Test void errorCodesFollowExactServerStage(){var d=new JoinDiagnostic();d.stage(JoinDiagnostic.Stage.REMOTE_SDP);assertEquals(JoinDiagnostic.Code.REMOTE_SDP_FAILED,d.stageFailure());d.stage(JoinDiagnostic.Stage.ANSWER_CREATE);assertEquals(JoinDiagnostic.Code.ANSWER_CREATE_FAILED,d.stageFailure());d.stage(JoinDiagnostic.Stage.LOCAL_SDP);assertEquals(JoinDiagnostic.Code.LOCAL_SDP_FAILED,d.stageFailure());d.stage(JoinDiagnostic.Stage.SERVER_ICE);assertEquals(JoinDiagnostic.Code.SERVER_ICE_TIMEOUT,d.stageFailure());}
 @Test void nativeFailedJoinConsumesAdmissionButReleasesSlotAndFreshQrRetries()throws Exception{
  var runtime=new MobileRuntime();try{
   for(int i=0;i<5;i++){
    var a=runtime.admission();var signal=new MobileRuntime.Signal(1,"JOIN",a.sessionId(),a.token(),"v=0\r\n");
    var failed=assertThrows(MobileRuntime.JoinFailure.class,()->runtime.join(signal));assertEquals("REMOTE_SDP_FAILED",failed.code());assertEquals(0,runtime.peerCount());
    var reused=assertThrows(MobileRuntime.JoinFailure.class,()->runtime.join(signal));assertEquals("ADMISSION_EXPIRED_OR_USED",reused.code());
   }
   assertNotNull(runtime.admission());assertEquals(8,runtime.joinDiagnostics().size());
  }finally{runtime.destroy();}
 }
}
