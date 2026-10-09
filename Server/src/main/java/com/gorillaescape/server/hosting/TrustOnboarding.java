package com.gorillaescape.server.hosting;

import com.sun.net.httpserver.HttpServer;
import java.net.*;
import java.nio.charset.StandardCharsets;
import java.util.concurrent.*;
import org.springframework.beans.factory.DisposableBean;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.autoconfigure.condition.ConditionalOnProperty;
import org.springframework.http.*;
import org.springframework.web.bind.annotation.*;

/** Public CA only. Optional bounded bootstrap listener in the same Java process, never a PWA/signaling HTTP listener. */
@RestController
@ConditionalOnProperty(name="gorilla.trust.enabled",havingValue="true")
public final class TrustOnboarding implements DisposableBean {
    private HttpServer bootstrap;
    private final ThreadPoolExecutor workers;
    private final byte[] certificate;
    private final String instructions;
    public TrustOnboarding(@Value("${server.address}") String address,
                           @Value("${gorilla.mobile.public-origin}") String origin,
                           @Value("${gorilla.trust.bootstrap-port:0}") int port) throws Exception {
        var material=LocalTrust.current();certificate=material.root().getEncoded();
        instructions="<!doctype html><html lang='es'><meta charset='utf-8'><meta name='viewport' content='width=device-width'><title>Gorilla Escape — PREPARE DEVICE</title>"
            +"<h1>PREPARE DEVICE</h1><p>Preparación única para esta PC. Descarga únicamente el certificado público. Nunca aceptes una advertencia TLS.</p>"
            +"<p>Compara este SHA-256 con el mostrado en la PC por un canal que controles:</p><code>"+material.fingerprint()+"</code>"
            +"<p><a href='/gorilla-root.cer'>Descargar CA pública</a></p>"
            +"<p>iPhone: Ajustes → General → VPN y gestión de dispositivos → instalar perfil. Después General → Información → Ajustes de confianza de certificados → activar confianza SSL de esta CA.</p>"
            +"<p>Android: instala la CA como certificado CA en los ajustes de seguridad del dispositivo. La ruta depende del fabricante; debe comprobarse la confianza real de Chrome.</p>"
            +"<p>Tras confiar, abre <a href='"+origin+"/prepare'>HTTPS de Gorilla Escape</a>. JOIN GAME es un flujo distinto: escanea el QR vigente en la PC.</p>"
            +"<p>La descarga HTTP inicial no está autenticada: si el fingerprint no coincide con la PC, NO instales el perfil. Retira la CA desde Ajustes cuando dejes de confiar en esta instalación.</p></html>";
        workers=new ThreadPoolExecutor(1,2,30,TimeUnit.SECONDS,new ArrayBlockingQueue<>(8),r->{var t=new Thread(r,"trust-bootstrap");t.setDaemon(true);return t;},new ThreadPoolExecutor.AbortPolicy());
        if(port!=0) {
            if(port<1024||port>65535||port==URI.create(origin).getPort())throw new IllegalArgumentException("TRUST_BOOTSTRAP_PORT_INVALID");
            bootstrap=HttpServer.create(new InetSocketAddress(address,port),8);bootstrap.setExecutor(workers);
            bootstrap.createContext("/",exchange->{
                try(exchange) {
                    var headers=exchange.getResponseHeaders();headers.set("Cache-Control","no-store");headers.set("X-Content-Type-Options","nosniff");headers.set("Referrer-Policy","no-referrer");
                    boolean root=exchange.getRequestURI().getPath().equals("/gorilla-root.cer");
                    if(!exchange.getRequestMethod().equals("GET")||(!root&&!exchange.getRequestURI().getPath().equals("/prepare"))) {exchange.sendResponseHeaders(404,-1);return;}
                    byte[] body=root?certificate:instructions.getBytes(StandardCharsets.UTF_8);
                    headers.set("Content-Type",root?"application/x-x509-ca-cert":"text/html; charset=utf-8");
                    if(root)headers.set("Content-Disposition","attachment; filename=gorilla-root.cer");
                    exchange.sendResponseHeaders(200,body.length);exchange.getResponseBody().write(body);
                }
            });bootstrap.start();
        }
    }
    @GetMapping(value="/prepare",produces="text/html;charset=UTF-8") public String prepare(){return instructions;}
    @GetMapping("/gorilla-root.cer") public ResponseEntity<byte[]> certificate(){return ResponseEntity.ok().header("Cache-Control","no-store").header("Content-Disposition","attachment; filename=gorilla-root.cer").contentType(MediaType.parseMediaType("application/x-x509-ca-cert")).body(certificate);}
    @Override public void destroy(){if(bootstrap!=null)bootstrap.stop(0);workers.shutdownNow();}
}
