package com.gorillaescape.server.protocol;
import java.net.URI;
import java.util.Base64;
/** Only authenticated managed IPC exposes modules. No URL/token/stringification/persistence. */
public final class QaAdmission implements AutoCloseable {
    private TechnicalSession.Admission current;
    private String state="NOT_REQUESTED";
    public record View(String state,long ttlMs,int size,String modules) { @Override public String toString(){return "QaAdmission["+state+", ttlMs="+ttlMs+"]";} }
    public synchronized View view(TechnicalSession session,URI page,boolean lab,boolean regenerate) {
        if(regenerate){if(current!=null)session.revokeAdmission(current.token());current=null;
            try{current=session.createAdmission();}catch(IllegalStateException e){state="SESSION_FULL";return new View(state,0,0,"");}}
        if(current==null)return new View(state,0,0,"");
        if(!regenerate&&"SCANNED/JOINED".equals(state))return new View(state,0,0,"");
        state=session.admissionState(current);long ttl=session.admissionRemainingMs(current);
        if(!"AVAILABLE".equals(state))return new View(state,ttl,0,"");
        if(!regenerate)return new View(state,ttl,0,"");
        try{var link=lab?JoinQrPoc.labLink(page,current):JoinQrPoc.link(page,current,session.roomCode());
            return raster(link,state,ttl);
        }catch(Exception e){session.revokeAdmission(current.token());current=null;state="CONFIG_REQUIRED";return new View(state,0,0,"");}
    }
    public static View raster(URI link,String state,long ttl)throws Exception {
            var bits=JoinQrPoc.modules(link);int size=bits.getWidth();
            if(size>89)throw new IllegalArgumentException("QR_BOUND");
            byte[] packed=new byte[(size*size+7)/8];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)if(bits.get(x,y)){int i=y*size+x;packed[i/8]|=(byte)(1<<(7-i%8));}
            return new View(state,ttl,size,Base64.getEncoder().encodeToString(packed));
    }
    public synchronized void invalidate(){current=null;state="ORIGIN_CHANGED";}
    @Override public synchronized void close(){current=null;}
}
