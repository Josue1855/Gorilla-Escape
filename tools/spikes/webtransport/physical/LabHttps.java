// Lab-only static HTTPS host. Never packaged with the product.
import com.sun.net.httpserver.*;
import javax.net.ssl.*;
import java.net.*;
import java.nio.file.*;
import java.security.*;
import java.util.*;
import java.util.concurrent.*;

public final class LabHttps {
  public static void main(String[] args) throws Exception {
    Path privateDir = Path.of(args[0]), publicDir = Path.of(args[1]);
    String bind = args[2];
    char[] password = Files.readString(privateDir.resolve("password")).trim().toCharArray();
    KeyStore store = KeyStore.getInstance("PKCS12");
    try (var in = Files.newInputStream(privateDir.resolve("server.p12"))) { store.load(in, password); }
    KeyManagerFactory km = KeyManagerFactory.getInstance(KeyManagerFactory.getDefaultAlgorithm());
    km.init(store, password); Arrays.fill(password, '\0');
    SSLContext tls = SSLContext.getInstance("TLS"); tls.init(km.getKeyManagers(), null, null);
    int httpsPort = args.length > 3 ? Integer.parseInt(args[3]) : 8443;
    int caPort = args.length > 4 ? Integer.parseInt(args[4]) : 8000;
    var https = HttpsServer.create(new InetSocketAddress(bind, httpsPort), 8);
    https.setHttpsConfigurator(new HttpsConfigurator(tls));
    var bootstrap = HttpServer.create(new InetSocketAddress(bind, caPort), 8);
    var workers = new ThreadPoolExecutor(2, 4, 30, TimeUnit.SECONDS,
        new ArrayBlockingQueue<Runnable>(8), new ThreadPoolExecutor.AbortPolicy());
    https.setExecutor(workers); bootstrap.setExecutor(workers);
    Set<String> allowed = Set.of("index.html", "lab.js", "wt.js", "capture.js");
    String config = Base64.getEncoder().encodeToString(Files.readAllBytes(privateDir.resolve("config.json")));
    https.createContext("/", exchange -> {
      try (exchange) {
        if (!exchange.getRequestMethod().equals("GET") || exchange.getRequestURI().toString().length() > 256) {
          exchange.sendResponseHeaders(400, -1); return;
        }
        String file = exchange.getRequestURI().getPath().substring(1);
        if (file.isEmpty()) file = "index.html";
        if (!allowed.contains(file)) { exchange.sendResponseHeaders(404, -1); return; }
        byte[] data = Files.readAllBytes(publicDir.resolve(file));
        if (file.equals("index.html")) {
          String html = new String(data, java.nio.charset.StandardCharsets.UTF_8);
          html = html.replace("<script type=\"module\"", "<script>location.hash='cfg=" + config + "';</script><script type=\"module\"");
          html = html.replace("<button id=\"prepare\">", "<button hidden disabled id=\"prepare\">");
          html = html.replace("Offline aún no comprobado.", "Laboratorio: contenido servido directamente por Java. No usar Service Worker.");
          html = html.replace("no se envían al hosting", "no se envían a Internet");
          data = html.getBytes(java.nio.charset.StandardCharsets.UTF_8);
        }
        exchange.getResponseHeaders().set("Content-Type", file.endsWith(".js") ? "text/javascript; charset=utf-8" : "text/html; charset=utf-8");
        exchange.getResponseHeaders().set("Cache-Control", "no-store");
        exchange.getResponseHeaders().set("Referrer-Policy", "no-referrer");
        exchange.getResponseHeaders().set("X-Content-Type-Options", "nosniff");
        exchange.sendResponseHeaders(200, data.length); exchange.getResponseBody().write(data);
      }
    });
    bootstrap.createContext("/", exchange -> {
      try (exchange) {
        if (!exchange.getRequestMethod().equals("GET") || !exchange.getRequestURI().getPath().equals("/gorilla-lab.cer")) {
          exchange.sendResponseHeaders(404, -1); return;
        }
        byte[] data = Files.readAllBytes(privateDir.resolve("gorilla-lab.cer"));
        exchange.getResponseHeaders().set("Content-Type", "application/x-x509-ca-cert");
        exchange.getResponseHeaders().set("Cache-Control", "no-store");
        exchange.sendResponseHeaders(200, data.length); exchange.getResponseBody().write(data);
      }
    });
    try { https.start(); bootstrap.start(); System.out.println("READY HTTPS " + httpsPort + " CA " + caPort); System.out.flush();
      while (System.in.read() != -1) { }
    } finally { https.stop(0); bootstrap.stop(0); workers.shutdownNow(); workers.awaitTermination(5, TimeUnit.SECONDS); }
  }
}
