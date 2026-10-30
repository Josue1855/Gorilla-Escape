package com.gorillaescape.server.protocol;

import java.net.URI;
import com.google.zxing.BarcodeFormat;
import com.google.zxing.MultiFormatWriter;
import com.google.zxing.WriterException;
import com.google.zxing.common.BitMatrix;

/** Technical QR only: bounded one-use admission in fragment, no IPC or permanent secret. */
public final class JoinQrPoc {
    private JoinQrPoc() {}
    public static URI link(URI page,TechnicalSession.Admission admission) {
        return link(page,admission,null);
    }
    public static URI link(URI page,TechnicalSession.Admission admission,String roomCode) {
        if (!"https".equals(page.getScheme()) || page.getHost()==null || page.getUserInfo()!=null
                || page.getRawQuery()!=null || page.getRawFragment()!=null || page.toASCIIString().length()>512
                || !admission.sessionId().matches("[a-f0-9-]{36}") || !admission.token().matches("[A-Za-z0-9_-]{43}"))
            throw new IllegalArgumentException("QR_CONFIG");
        String base=page.toASCIIString();
        if(roomCode!=null&&!roomCode.isBlank()){
            if(!roomCode.matches("[a-z0-9]{4,8}"))throw new IllegalArgumentException("QR_CONFIG");
            String customDomain=System.getenv("GORILLA_TUNNEL_DOMAIN");
            String publicHost=(customDomain!=null&&!customDomain.isBlank())?customDomain.trim():"play.gorillas.run";
            if(page.getHost().equals("play.gorillas.run")||page.getHost().endsWith(".gorillas.run")||page.getHost().equals("gorillas.run")
                    ||page.getHost().endsWith(".trycloudflare.com")||page.getHost().equals(publicHost)){
                base="https://"+publicHost+"/"+roomCode+"/";
            }
        }
        return URI.create(base+"#v=1&sessionId="+admission.sessionId()+"&admission="+admission.token());
    }
    /** Explicit loopback LAB concession; not a LAN/IP product URL. */
    public static URI labLink(URI page,TechnicalSession.Admission admission) {
        if(!"http".equals(page.getScheme())||!"127.0.0.1".equals(page.getHost())||page.getUserInfo()!=null||page.getRawQuery()!=null||page.getRawFragment()!=null)
            throw new IllegalArgumentException("QR_LAB_ORIGIN");
        URI validated=link(URI.create("https://lab.example.invalid/"),admission);
        return URI.create(page.toASCIIString()+"#"+validated.getRawFragment());
    }
    public static BitMatrix modules(URI link) throws WriterException {
        if(link.toASCIIString().length()>1024)throw new IllegalArgumentException("QR_SIZE");
        return new MultiFormatWriter().encode(link.toASCIIString(),BarcodeFormat.QR_CODE,1,1);
    }
    public static BitMatrix encode(URI link) throws WriterException {
        if(link.toASCIIString().length()>1024)throw new IllegalArgumentException("QR_SIZE");
        return new MultiFormatWriter().encode(link.toASCIIString(),BarcodeFormat.QR_CODE,384,384);
    }
}
