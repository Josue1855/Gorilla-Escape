import java.io.*;
import java.net.*;
import java.nio.charset.StandardCharsets;
/** Test-only JVM, never packaged in the production JAR. Invalid READY/PONG fixture. */
public final class ProbeInvalidFixture {
    private static volatile Socket client;
    public static void main(String[] args) throws Exception {
        String jar=ProbeInvalidFixture.class.getProtectionDomain().getCodeSource().getLocation().getPath();
        try(var listener=new ServerSocket(0,1,InetAddress.getByName("127.0.0.1"))) {
            String duplicate=jar.contains("ready-duplicate")?",\"ipcVersion\":1":"";
            System.out.println("GORILLA_IPC_READY {\"ipcVersion\":1,\"instanceId\":\""+System.getenv("GORILLA_IPC_INSTANCE")+"\",\"pid\":"+ProcessHandle.current().pid()+",\"ipcPort\":"+listener.getLocalPort()+",\"httpPort\":1"+duplicate+"}");System.out.flush();
            Thread worker=new Thread(()->{
                try(var socket=listener.accept()) {
                    client=socket;var in=new DataInputStream(socket.getInputStream());var out=new DataOutputStream(socket.getOutputStream());
                    for(int n=0;n<2;n++) {
                        int size=in.readInt();if(size<1||size>4096)return;
                        byte[] frame=new byte[size];in.readFully(frame);
                        String pong=new String(frame,StandardCharsets.UTF_8).replace("\"type\":\"PING\"","\"type\":\"PONG\"").replaceAll("\"payload\":\\{[^}]*\\}","\"payload\":{}");
                        if(n==1)pong=pong.substring(0,pong.length()-1)+",\"sequence\":2}";
                        byte[] body=pong.getBytes(StandardCharsets.UTF_8);out.writeInt(body.length);out.write(body);out.flush();
                    }
                }catch(IOException ignored){}
            });worker.setDaemon(true);worker.start();
            while(System.in.read()!=-1){}
            listener.close();if(client!=null)client.close();worker.join(2000);
        }
    }
}
