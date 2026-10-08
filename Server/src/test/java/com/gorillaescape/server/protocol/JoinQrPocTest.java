package com.gorillaescape.server.protocol;
import org.junit.jupiter.api.Test;
import java.net.URI;
import com.google.zxing.*;
import com.google.zxing.common.*;
import java.util.Map;
import static org.junit.jupiter.api.Assertions.*;
class JoinQrPocTest {
 @Test void qrRoundTripToOneUseAdmission() throws Exception {
  var session=new TechnicalSession(()->0L,30,3,60);var admission=session.createAdmission();
  var link=JoinQrPoc.link(URI.create("https://pwa.example.invalid/join"),admission);var bits=JoinQrPoc.encode(link);
  var source=new LuminanceSource(bits.getWidth(),bits.getHeight()){
   public byte[] getRow(int y,byte[] row){if(row==null||row.length<getWidth())row=new byte[getWidth()];for(int x=0;x<getWidth();x++)row[x]=(byte)(bits.get(x,y)?0:255);return row;}
   public byte[] getMatrix(){byte[] b=new byte[getWidth()*getHeight()];for(int y=0;y<getHeight();y++)System.arraycopy(getRow(y,null),0,b,y*getWidth(),getWidth());return b;}
  };
  var decoded=new MultiFormatReader().decode(new BinaryBitmap(new HybridBinarizer(source)),Map.of(DecodeHintType.POSSIBLE_FORMATS,java.util.List.of(BarcodeFormat.QR_CODE)));
  assertEquals(link.toASCIIString(),decoded.getText());assertNull(link.getQuery());assertTrue(link.getFragment().contains(admission.token()));
  var player=session.join(admission.sessionId(),admission.token());assertEquals(1,player.playerId());
  assertThrows(IllegalArgumentException.class,()->session.join(admission.sessionId(),admission.token()));
 }
 @Test void insecureOrSecretBearingOriginsRejected(){var s=new TechnicalSession(()->0L,30,3,60);var a=s.createAdmission();for(String url:java.util.List.of("http://192.168.1.1/join","https://user:pass@example.invalid/join","https://example.invalid/join?token=x","https://example.invalid/join#old"))assertThrows(IllegalArgumentException.class,()->JoinQrPoc.link(URI.create(url),a));}
}
