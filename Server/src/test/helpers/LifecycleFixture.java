import java.net.*;

/** Deliberately incomplete test JVM. Not packaged in the server; never certifies real server interoperability. */
public final class LifecycleFixture {
    public static void main(String[] args) throws Exception {
        String jar = LifecycleFixture.class.getProtectionDomain().getCodeSource().getLocation().getPath();
        if (jar.contains("connect-refused")) {
            int port;
            try (var socket = new ServerSocket(0,1,InetAddress.getByName("127.0.0.1"))) { port=socket.getLocalPort(); }
            System.out.println("GORILLA_IPC_READY {\"ipcVersion\":1,\"instanceId\":\""+System.getenv("GORILLA_IPC_INSTANCE")
                +"\",\"pid\":"+ProcessHandle.current().pid()+",\"ipcPort\":"+port+",\"httpPort\":1}");
            System.out.flush();
        }
        while (System.in.read()!=-1) { }
    }
}
