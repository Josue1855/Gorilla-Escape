import javax.imageio.ImageIO;
import java.io.ByteArrayInputStream;
import com.google.zxing.*;
import com.google.zxing.common.HybridBinarizer;
// Lab-only QR image decoder. Payload stays in a private pipe, never evidence/logs.
class DecodeQr {
 public static void main(String[] args)throws Exception {
  byte[] input=System.in.readNBytes(65537);if(input.length>65536)throw new Exception("QR_BOUND");
  var image=ImageIO.read(new ByteArrayInputStream(input));if(image==null||image.getWidth()>512||image.getHeight()>512)throw new Exception("QR_IMAGE");
  var source=new LuminanceSource(image.getWidth(),image.getHeight()){
   public byte[] getRow(int y,byte[] row){if(row==null||row.length<getWidth())row=new byte[getWidth()];for(int x=0;x<getWidth();x++)row[x]=(byte)(image.getRGB(x,y)&255);return row;}
   public byte[] getMatrix(){byte[] a=new byte[getWidth()*getHeight()];for(int y=0;y<getHeight();y++)System.arraycopy(getRow(y,null),0,a,y*getWidth(),getWidth());return a;}
  };
  System.out.print(new MultiFormatReader().decode(new BinaryBitmap(new HybridBinarizer(source))).getText());
 }
}
