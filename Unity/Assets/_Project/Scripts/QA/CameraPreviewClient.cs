using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
namespace GorillaEscape.QA {
    /// <summary>Single bounded memory slot, loopback only. Unity decoding stays on its frame thread.</summary>
    public sealed class CameraPreviewClient : IDisposable {
        public const int MaxBytes=65536;
        readonly object gate=new object();byte[] latest;TcpClient client;readonly CancellationTokenSource stop=new CancellationTokenSource();
        public Task Run {get;private set;} public long Overwritten {get;private set;} public string State {get;private set;}="STARTING";
        public void Start(int port){if(Run!=null||Environment.GetEnvironmentVariable("GORILLA_QA_OVERLAY")!="1")throw new InvalidOperationException("PREVIEW_DISABLED_OR_STARTED");Run=Receive(port);}
        async Task Receive(int port){try{client=new TcpClient(AddressFamily.InterNetwork);await client.ConnectAsync(IPAddress.Loopback,port);client.ReceiveTimeout=1000;using(var stream=client.GetStream())while(!stop.IsCancellationRequested){await stream.WriteAsync(new byte[]{(byte)'P'},0,1,stop.Token);var header=new byte[4];await Read(stream,header);int size=Length(header);if(size>0){var bytes=new byte[size];await Read(stream,bytes);lock(gate){if(latest!=null){Overwritten++;Array.Clear(latest,0,latest.Length);}latest=bytes;}State="READY";}await Task.Delay(200,stop.Token);}}catch(Exception){State=stop.IsCancellationRequested?"STOPPED":"FAILED";}finally{client?.Close();lock(gate){if(latest!=null)Array.Clear(latest,0,latest.Length);latest=null;}}}
        public static bool ValidJpeg(byte[] b){if(b==null||b.Length<4||b.Length>MaxBytes||b[0]!=255||b[1]!=216)return false;int at=2;while(at+3<b.Length){if(b[at++]!=255)return false;byte marker=b[at++];if(marker==218||marker==217)return false;int size=(b[at]<<8)|b[at+1];if(size<2||at+size>b.Length)return false;if(marker==192||marker==194){if(size<8)return false;int height=(b[at+3]<<8)|b[at+4],width=(b[at+5]<<8)|b[at+6];return width>0&&width<=320&&height>0&&height<=240;}at+=size;}return false;}
        public static int Length(byte[] h){long size=((long)h[0]<<24)|((long)h[1]<<16)|((long)h[2]<<8)|h[3];if(size>MaxBytes)throw new InvalidDataException("PREVIEW_BOUND");return (int)size;}
        async Task Read(NetworkStream stream,byte[] bytes){int at=0;while(at<bytes.Length){var read=stream.ReadAsync(bytes,at,bytes.Length-at,stop.Token);if(await Task.WhenAny(read,Task.Delay(1000,stop.Token))!=read)throw new IOException("PREVIEW_TIMEOUT");int n=await read;if(n==0)throw new EndOfStreamException();at+=n;}}
        public byte[] Take(){lock(gate){var result=latest;latest=null;return result;}}
        public void Dispose(){stop.Cancel();client?.Close();lock(gate){if(latest!=null)Array.Clear(latest,0,latest.Length);latest=null;}}
    }
}
