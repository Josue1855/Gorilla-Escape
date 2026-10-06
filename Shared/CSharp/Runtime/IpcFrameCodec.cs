using System;
using System.IO;
using System.Text;

namespace GorillaEscape.Contracts
{
    public static class IpcFrameCodec
    {
        public const int MaximumBytes = 4096;
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        public static string Read(Stream stream)
        {
            var header = new byte[4]; ReadExactly(stream, header);
            uint size = ((uint)header[0] << 24) | ((uint)header[1] << 16) | ((uint)header[2] << 8) | header[3];
            if (size == 0 || size > MaximumBytes) throw new InvalidDataException("IPC_FRAME_SIZE");
            var body = new byte[(int)size]; ReadExactly(stream, body);
            return Utf8.GetString(body);
        }
        public static void Write(Stream stream, string json)
        {
            byte[] body = Utf8.GetBytes(json);
            if (body.Length == 0 || body.Length > MaximumBytes) throw new InvalidDataException("IPC_FRAME_SIZE");
            int size = body.Length;
            stream.Write(new[] {(byte)(size >> 24), (byte)(size >> 16), (byte)(size >> 8), (byte)size}, 0, 4);
            stream.Write(body, 0, size); stream.Flush();
        }
        private static void ReadExactly(Stream stream, byte[] bytes)
        {
            int offset = 0;
            while (offset < bytes.Length)
            {
                int count = stream.Read(bytes, offset, bytes.Length - offset);
                if (count == 0) throw new EndOfStreamException("IPC_FRAME_EOF");
                offset += count;
            }
        }
    }
    [Serializable] public sealed class IpcReady
    {
        public int ipcVersion; public string instanceId; public int pid; public int ipcPort; public int httpPort;
    }
    [Serializable] public sealed class IpcPong
    {
        public int ipcVersion; public string type; public string instanceId; public string connectionId; public int sequence;
    }
}
