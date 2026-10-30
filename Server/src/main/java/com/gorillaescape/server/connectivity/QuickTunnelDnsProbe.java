package com.gorillaescape.server.connectivity;

import java.io.*;
import java.net.*;
import java.net.http.*;
import java.nio.charset.StandardCharsets;
import java.time.Duration;
import java.util.List;
import javax.net.ssl.*;
import tools.jackson.databind.json.JsonMapper;

/** HTTPS readiness fallback for a freshly created Quick Tunnel when local DNS caches NXDOMAIN. */
final class QuickTunnelDnsProbe {
    private static final JsonMapper JSON = JsonMapper.builder().build();
    private QuickTunnelDnsProbe() {}

    static InetAddress addressFromJson(String body) throws IOException {
        var root=JSON.readTree(body);
        if(root.path("Status").asInt(-1)!=0)throw new IOException("DNS_NOT_READY");
        var answers=root.path("Answer");
        if(!answers.isArray() || answers.size()>32)throw new IOException("DNS_INVALID_RESPONSE");
        for(var answer:answers){
            if(answer.path("type").asInt(-1)!=1)continue;
            String ip=answer.path("data").asText("");
            String[] parts=ip.split("\\.",-1);
            if(parts.length!=4)continue;
            byte[] bytes=new byte[4];boolean valid=true;
            for(int i=0;i<4;i++)try{
                if(!parts[i].matches("(?:0|[1-9][0-9]{0,2})"))throw new NumberFormatException();
                int n=Integer.parseInt(parts[i]);if(n>255)throw new NumberFormatException();bytes[i]=(byte)n;
            }catch(NumberFormatException e){valid=false;break;}
            if(valid){InetAddress address=InetAddress.getByAddress(bytes);
                int first=bytes[0]&255, second=bytes[1]&255;
                boolean reserved=first==0||first>=224||(first==100&&second>=64&&second<=127)||(first==198&&(second==18||second==19));
                if(!reserved&&!address.isAnyLocalAddress()&&!address.isLoopbackAddress()&&!address.isSiteLocalAddress()&&!address.isLinkLocalAddress()&&!address.isMulticastAddress())return address;}
        }
        throw new IOException("DNS_NO_PUBLIC_IPV4");
    }

    static boolean ready(URI origin,long budgetMs) throws Exception {
        String host=origin.getHost();
        if(host==null||!host.matches("[a-z0-9]+(?:-[a-z0-9]+)*\\.trycloudflare\\.com"))return false;
        long deadline=System.nanoTime()+java.util.concurrent.TimeUnit.MILLISECONDS.toNanos(budgetMs);
        int timeout=(int)Math.max(1,Math.min(1500,budgetMs));
        var client=HttpClient.newBuilder().connectTimeout(Duration.ofMillis(timeout)).build();
        try {
            var query=URI.create("https://cloudflare-dns.com/dns-query?name="+host+"&type=A");
            var request=HttpRequest.newBuilder(query).timeout(Duration.ofMillis(timeout)).header("accept","application/dns-json").GET().build();
            var response=client.send(request,HttpResponse.BodyHandlers.ofInputStream());
            if(response.statusCode()!=200)throw new IOException("DNS_HTTP_ERROR");
            byte[] bytes;try(var in=response.body()){bytes=in.readNBytes(8193);}
            if(bytes.length>8192)throw new IOException("DNS_RESPONSE_TOO_LARGE");
            var address=addressFromJson(new String(bytes,StandardCharsets.UTF_8));
            int left=(int)Math.min(Integer.MAX_VALUE,java.util.concurrent.TimeUnit.NANOSECONDS.toMillis(deadline-System.nanoTime()));
            if(left<=0)throw new java.net.SocketTimeoutException();
            try(Socket raw=new Socket()){
                raw.connect(new InetSocketAddress(address,443),left);
                left=(int)java.util.concurrent.TimeUnit.NANOSECONDS.toMillis(deadline-System.nanoTime());if(left<=0)throw new java.net.SocketTimeoutException();
                try(SSLSocket tls=(SSLSocket)((SSLSocketFactory)SSLSocketFactory.getDefault()).createSocket(raw,host,443,true)){
                    tls.setSoTimeout(left);SSLParameters params=tls.getSSLParameters();params.setEndpointIdentificationAlgorithm("HTTPS");params.setServerNames(List.of(new SNIHostName(host)));tls.setSSLParameters(params);tls.startHandshake();
                    tls.getOutputStream().write(("GET / HTTP/1.1\r\nHost: "+host+"\r\nConnection: close\r\n\r\n").getBytes(StandardCharsets.US_ASCII));
                    tls.getOutputStream().flush();
                    var reader=new BufferedReader(new InputStreamReader(tls.getInputStream(),StandardCharsets.US_ASCII));
                    String status=reader.readLine();return status!=null&&status.matches("HTTP/1\\.[01] 200(?: .*)?");
                }
            }
        } finally {client.shutdownNow();}
    }
}
