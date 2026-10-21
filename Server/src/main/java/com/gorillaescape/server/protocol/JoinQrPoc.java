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
        if (!"https".equals(page.getScheme()) || page.getHost()==null || page.getUserInfo()!=null
                || page.getRawQuery()!=null || page.getRawFragment()!=null || page.toASCIIString().length()>512
                || !admission.sessionId().matches("[a-f0-9-]{36}") || !admission.token().matches("[A-Za-z0-9_-]{43}"))
            throw new IllegalArgumentException("QR_CONFIG");
        return URI.create(page.toASCIIString()+"#v=1&sessionId="+admission.sessionId()+"&admission="+admission.token());
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
    public record EncodedModules(int size, String modules) {}
    public static EncodedModules pack(URI link) throws WriterException {
        var bits=modules(link);int size=bits.getWidth();
        if(size>89)throw new IllegalArgumentException("QR_BOUND");
        byte[] packed=new byte[(size*size+7)/8];
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)if(bits.get(x,y)){int i=y*size+x;packed[i/8]|=(byte)(1<<(7-i%8));}
        return new EncodedModules(size, java.util.Base64.getEncoder().encodeToString(packed));
    }
}
