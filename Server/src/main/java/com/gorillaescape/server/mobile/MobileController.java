package com.gorillaescape.server.mobile;
import com.gorillaescape.server.protocol.*;
import org.springframework.web.bind.annotation.*;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import jakarta.servlet.http.HttpServletRequest;
import java.net.URI;
import java.util.*;
import java.security.MessageDigest;
import java.nio.charset.StandardCharsets;
import java.io.*;
import java.awt.image.BufferedImage;
import javax.imageio.ImageIO;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.web.server.ResponseStatusException;

@RestController
@RequestMapping("/mobile")
@ConditionalOnProperty(name="gorilla.mobile.rtc-enabled",havingValue="true")
public final class MobileController {
    @org.springframework.beans.factory.annotation.Autowired(required=false) private com.gorillaescape.server.connectivity.ConnectivityService connectivity;
    private final MobileRuntime runtime;private final String operator, origin, address;private final boolean lab;
    public MobileController(MobileRuntime runtime,
        @org.springframework.beans.factory.annotation.Value("${gorilla.mobile.public-origin:}") String origin,
        @org.springframework.beans.factory.annotation.Value("${server.address}") String address,
        @org.springframework.beans.factory.annotation.Value("${gorilla.mobile.lab-enabled:false}") boolean lab) {
        this.runtime=runtime;this.origin=origin;this.address=address;this.lab=lab;
        operator=System.getenv("GORILLA_MOBILE_OPERATOR");
        if(operator==null||!operator.matches("[A-Za-z0-9_-]{43}"))throw new IllegalStateException("MOBILE_OPERATOR_CONFIG");
        if(!lab && origin.isBlank())throw new IllegalStateException("MOBILE_ORIGIN_REQUIRED");
    }
    private void operator(HttpServletRequest request,String supplied){if(!(lab?"127.0.0.1":address).equals(request.getRemoteAddr())||supplied==null||!MessageDigest.isEqual(operator.getBytes(StandardCharsets.US_ASCII),supplied.getBytes(StandardCharsets.US_ASCII)))throw new ResponseStatusException(HttpStatus.FORBIDDEN,"OPERATOR_REQUIRED");}
    @PostMapping("/admission") public Map<String,Object> admission(HttpServletRequest r,@RequestHeader(value="X-Gorilla-Operator",required=false)String supplied)throws Exception{
        operator(r,supplied);
        URI page=lab?URI.create("http://127.0.0.1:"+r.getLocalPort()+"/mobile-lab/index.html"):
            MobileOrigin.page(origin,address,r.getLocalPort());
        if(connectivity!=null)try{page=connectivity.page();}catch(IllegalStateException e){throw new ResponseStatusException(HttpStatus.SERVICE_UNAVAILABLE,"CONNECTION_NOT_READY");}
        var admission=runtime.admission();URI uri="http".equals(page.getScheme())?JoinQrPoc.labLink(page,admission):JoinQrPoc.link(page,admission);
        var bits=JoinQrPoc.encode(uri);var image=new BufferedImage(bits.getWidth(),bits.getHeight(),BufferedImage.TYPE_INT_RGB);
        for(int y=0;y<bits.getHeight();y++)for(int x=0;x<bits.getWidth();x++)image.setRGB(x,y,bits.get(x,y)?0:0xffffff);
        var bytes=new ByteArrayOutputStream();ImageIO.write(image,"PNG",bytes);
        return Map.of("protocolVersion",1,"url",uri.toASCIIString(),"qrPng",Base64.getEncoder().encodeToString(bytes.toByteArray()),"expiresInSeconds",30,"roomCode",runtime.roomCode(),"scope",lab?"loopback software/lab; not phone onboarding":"DEC-016 prepared-device LAN HTTPS");
    }
    public record PinAdmissionRequest(String pin) {}
    @PostMapping("/pin-admission")public ResponseEntity<?> pinAdmission(@RequestBody PinAdmissionRequest request){
        if(request==null||request.pin()==null||request.pin().isBlank())return ResponseEntity.badRequest().body(Map.of("error","PIN_REQUIRED"));
        try{
            var admission=runtime.admissionByPin(request.pin());
            return ResponseEntity.ok(Map.of("sessionId",admission.sessionId(),"admission",admission.token(),"expiresInSeconds",30));
        }catch(IllegalArgumentException e){
            return ResponseEntity.badRequest().body(Map.of("error","PIN_INVALID"));
        }catch(IllegalStateException e){
            String code=Set.of("PIN_RATE_LIMITED","SESSION_FULL","SESSION_CLOSED","LOBBY_ADMISSION_CLOSED").contains(String.valueOf(e.getMessage()))?e.getMessage():"PIN_FAILED";
            return ResponseEntity.status("PIN_RATE_LIMITED".equals(code)?429:400).body(Map.of("error",code));
        }catch(Exception e){
            return ResponseEntity.badRequest().body(Map.of("error","PIN_FAILED"));
        }
    }
    @PostMapping("/join")public ResponseEntity<?> join(@RequestBody MobileRuntime.Signal signal){
        try{return ResponseEntity.ok(runtime.join(signal));}
        catch(MobileRuntime.JoinFailure e){return ResponseEntity.badRequest().body(Map.of("error","JOIN_REJECTED","code",e.code()));}
        catch(Exception e){return ResponseEntity.badRequest().body(Map.of("error","JOIN_REJECTED","code","SIGNAL_FAILED_OTHER"));}
    }
    public record Resume(String sessionId,int playerId,String resumeToken,String offer){}
    @PostMapping("/reconnect")public MobileRuntime.Answer reconnect(@RequestBody Resume r){try{return runtime.reconnect(r.sessionId(),r.playerId(),r.resumeToken(),r.offer());}catch(Exception e){throw new ResponseStatusException(HttpStatus.BAD_REQUEST,"RESUME_REJECTED");}}
    public record Leave(String peerId,String resumeToken){}
    @PostMapping("/disconnect")public Map<String,String> disconnect(@RequestBody Leave r){try{runtime.disconnect(r.peerId(),r.resumeToken());return Map.of("state","DISCONNECTED");}catch(Exception e){throw new ResponseStatusException(HttpStatus.BAD_REQUEST,"DISCONNECT_REJECTED");}}
    @PostMapping("/profile")public Map<String,Object> profile(@RequestBody MobileRuntime.ProfileRequest request){try{return runtime.createProfile(request);}catch(Exception e){throw new ResponseStatusException(HttpStatus.BAD_REQUEST,"PROFILE_NOT_CONFIRMED");}}
    @GetMapping("/diagnostics")public Map<String,Object> diagnostics(HttpServletRequest r,@RequestHeader(value="X-Gorilla-Operator",required=false)String supplied){operator(r,supplied);return Map.of("peers",runtime.peerCount(),"inputs",runtime.inputs.metrics(),"joinAttempts",runtime.joinDiagnostics());}
    @PostMapping("/end")public Map<String,String> end(HttpServletRequest r,@RequestHeader(value="X-Gorilla-Operator",required=false)String supplied){operator(r,supplied);runtime.destroy();return Map.of("state","CLOSED");}
}
