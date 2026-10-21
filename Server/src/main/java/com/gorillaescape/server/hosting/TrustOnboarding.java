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
        instructions="<!doctype html><html lang='es'><head><meta charset='utf-8'><meta name='viewport' content='width=device-width, initial-scale=1'><title>Gorilla Escape — Preparar Teléfono</title>"
            +"<style>"
            +"body{font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;background:#0f172a;color:#f8fafc;margin:0;padding:20px;line-height:1.5;}"
            +".card{max-width:480px;margin:0 auto;background:#1e293b;border-radius:16px;padding:22px;box-shadow:0 10px 25px rgba(0,0,0,0.4);border:1px solid #334155;}"
            +"h1{font-size:22px;margin-top:0;color:#38bdf8;text-align:center;}"
            +"p{font-size:14px;color:#cbd5e1;}"
            +".btn{display:block;text-align:center;background:#0284c7;color:#fff;font-weight:bold;text-decoration:none;padding:14px 20px;border-radius:10px;margin:16px 0;font-size:16px;}"
            +".step{background:#0f172a;border-radius:8px;padding:14px;margin-bottom:14px;border-left:4px solid #38bdf8;}"
            +".step-num{font-weight:bold;color:#38bdf8;font-size:13px;text-transform:uppercase;margin-bottom:4px;}"
            +"code{display:block;background:#0f172a;padding:8px;border-radius:6px;font-size:11px;word-break:break-all;color:#a5f3fc;margin:6px 0;}"
            +".verify-btn{display:block;text-align:center;background:#059669;color:#fff;font-weight:bold;text-decoration:none;padding:12px 18px;border-radius:8px;margin-top:14px;font-size:15px;}"
            +"</style></head><body>"
            +"<div class='card'>"
            +"<h1>🎮 Gorilla Escape</h1>"
            +"<p>Para usar este teléfono como mando de movimiento en tu red local privada, Apple iOS requiere autorizar la conexión segura local.</p>"
            +"<div class='step'>"
            +"<div class='step-num'>Paso 1: Descargar Certificado</div>"
            +"<p style='margin:4px 0;'>Descarga el certificado de seguridad de esta PC:</p>"
            +"<a class='btn' href='/gorilla-root.cer'>📥 Descargar Certificado CA</a>"
            +"<p style='font-size:12px;margin:0;'>Huella SHA-256 de seguridad:</p>"
            +"<code>"+material.fingerprint()+"</code>"
            +"</div>"
            +"<div class='step'>"
            +"<div class='step-num'>Paso 2: Instalar Perfil (iPhone)</div>"
            +"<p style='margin:4px 0;'>Abre <b>Ajustes</b> en tu iPhone → Toca <b>Perfil descargado</b> (o ve a <b>General → VPN y gestión de dispositivos</b>) e <b>Instalar</b>.</p>"
            +"</div>"
            +"<div class='step'>"
            +"<div class='step-num'>Paso 3: Activar Confianza SSL (iPhone)</div>"
            +"<p style='margin:4px 0;'>En tu iPhone ve a <b>Ajustes → General → Información → Ajustes de confianza de certificados</b> (abajo del todo) y <b>activa</b> el interruptor de Gorilla Escape.</p>"
            +"</div>"
            +"<div class='step'>"
            +"<div class='step-num'>Paso 4: ¡Todo listo!</div>"
            +"<p style='margin:4px 0;'>Comprueba que la conexión segura funcione abriendo:</p>"
            +"<a class='verify-btn' href='"+origin+"/prepare'>✅ Probar Conexión Segura</a>"
            +"<p style='margin-top:12px;font-size:13px;'>Luego regresa a la pantalla de la PC y escanea el <b>QR de Partida (Paso 2)</b> para jugar.</p>"
            +"</div>"
            +"</div></body></html>";
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
