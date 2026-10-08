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
import org.springframework.web.server.ResponseStatusException;

@RestController
@RequestMapping("/mobile")
@ConditionalOnProperty(name="gorilla.mobile.rtc-enabled",havingValue="true")
public final class MobileController {
    private final MobileRuntime runtime;private final String operator;
    public MobileController(MobileRuntime runtime){this.runtime=runtime;operator=System.getenv("GORILLA_MOBILE_OPERATOR");if(operator==null||!operator.matches("[A-Za-z0-9_-]{43}"))throw new IllegalStateException("MOBILE_OPERATOR_CONFIG");}
    private void operator(HttpServletRequest request,String supplied){if(!"127.0.0.1".equals(request.getRemoteAddr())||supplied==null||!MessageDigest.isEqual(operator.getBytes(StandardCharsets.US_ASCII),supplied.getBytes(StandardCharsets.US_ASCII)))throw new ResponseStatusException(HttpStatus.FORBIDDEN,"OPERATOR_REQUIRED");}
    @PostMapping("/admission") public Map<String,Object> admission(HttpServletRequest r,@RequestHeader(value="X-Gorilla-Operator",required=false)String supplied)throws Exception{
        operator(r,supplied);var admission=runtime.admission();URI uri=JoinQrPoc.labLink(URI.create("http://127.0.0.1:"+r.getLocalPort()+"/mobile-lab/index.html"),admission);
        var bits=JoinQrPoc.encode(uri);var image=new BufferedImage(bits.getWidth(),bits.getHeight(),BufferedImage.TYPE_INT_RGB);
        for(int y=0;y<bits.getHeight();y++)for(int x=0;x<bits.getWidth();x++)image.setRGB(x,y,bits.get(x,y)?0:0xffffff);
        var bytes=new ByteArrayOutputStream();ImageIO.write(image,"PNG",bytes);
        return Map.of("protocolVersion",1,"url",uri.toASCIIString(),"qrPng",Base64.getEncoder().encodeToString(bytes.toByteArray()),"expiresInSeconds",30,"scope","loopback software/lab; not phone onboarding");
    }
    @PostMapping("/join")public MobileRuntime.Answer join(@RequestBody MobileRuntime.Signal signal){try{return runtime.join(signal);}catch(Exception e){throw new ResponseStatusException(HttpStatus.BAD_REQUEST,"JOIN_REJECTED");}}
    public record Resume(String sessionId,int playerId,String resumeToken,String offer){}
    @PostMapping("/reconnect")public MobileRuntime.Answer reconnect(@RequestBody Resume r){try{return runtime.reconnect(r.sessionId(),r.playerId(),r.resumeToken(),r.offer());}catch(Exception e){throw new ResponseStatusException(HttpStatus.BAD_REQUEST,"RESUME_REJECTED");}}
    public record Leave(String peerId,String resumeToken){}
    @PostMapping("/disconnect")public Map<String,String> disconnect(@RequestBody Leave r){try{runtime.disconnect(r.peerId(),r.resumeToken());return Map.of("state","DISCONNECTED");}catch(Exception e){throw new ResponseStatusException(HttpStatus.BAD_REQUEST,"DISCONNECT_REJECTED");}}
    @GetMapping("/diagnostics")public Map<String,Object> diagnostics(HttpServletRequest r,@RequestHeader(value="X-Gorilla-Operator",required=false)String supplied){operator(r,supplied);return Map.of("peers",runtime.peerCount(),"inputs",runtime.inputs.metrics());}
    @PostMapping("/end")public Map<String,String> end(HttpServletRequest r,@RequestHeader(value="X-Gorilla-Operator",required=false)String supplied){operator(r,supplied);runtime.destroy();return Map.of("state","CLOSED");}
}
