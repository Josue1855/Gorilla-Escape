using System;
using System.IO;
using System.Text;
using GorillaEscape.Contracts;
using NUnit.Framework;

public class IpcFrameCodecTests
{
    [Test] public void CommonFixtureRoundTripsWithBigEndianLength()
    {
        string root = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "../../Shared/Protocol/ipc"));
        foreach (string name in new[] {"ping-first.json", "pong.json"})
        {
            string body = File.ReadAllText(Path.Combine(root, name)).Trim();
            using (var stream = new MemoryStream())
            {
                IpcFrameCodec.Write(stream, body);
                byte[] bytes = stream.ToArray(); int n = Encoding.UTF8.GetByteCount(body);
                Assert.That(bytes[0], Is.EqualTo(0)); Assert.That(bytes[3], Is.EqualTo(n & 255));
                stream.Position = 0; Assert.That(IpcFrameCodec.Read(stream), Is.EqualTo(body));
            }
        }
    }
    [TestCase(0)] [TestCase(4097)] [TestCase(-1)]
    public void RejectsFrameSizeBeforeBodyAllocation(int size)
    {
        byte[] header = {(byte)(size >> 24), (byte)(size >> 16), (byte)(size >> 8), (byte)size};
        Assert.Throws<InvalidDataException>(() => IpcFrameCodec.Read(new MemoryStream(header)));
    }
    [Test] public void ReadsPartialFramesAndRejectsTruncationAndInvalidUtf8()
    {
        using (var stream = new MemoryStream())
        {
            IpcFrameCodec.Write(stream, "{}{}");
            Assert.That(IpcFrameCodec.Read(new Fragmented(stream.ToArray())), Is.EqualTo("{}{}"));
        }
        Assert.Throws<EndOfStreamException>(() => IpcFrameCodec.Read(new MemoryStream(new byte[]{0,0,0,2,123})));
        Assert.Throws<DecoderFallbackException>(() => IpcFrameCodec.Read(new MemoryStream(new byte[]{0,0,0,1,255})));
    }
    private sealed class Fragmented : MemoryStream
    {
        public Fragmented(byte[] bytes) : base(bytes) {}
        public override int Read(byte[] buffer,int offset,int count) => base.Read(buffer,offset,Math.Min(1,count));
    }
}
