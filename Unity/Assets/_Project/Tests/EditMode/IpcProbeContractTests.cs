using System;
using System.IO;
using GorillaEscape.Contracts;
using NUnit.Framework;
using UnityEngine;
public class IpcProbeContractTests
{
    const string Instance="00000000-0000-4000-8000-000000000001",Connection="00000000-0000-4000-8000-000000000002",Token="AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
    [Serializable] private sealed class Corpus {public Case[] cases;}
    [Serializable] private sealed class Case {public string name,kind,hex;public bool accepted,first;public int sequence;}
    [Test] public void SharedCorpus()
    {
        string path=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Shared/Protocol/ipc/cases/corpus.json"));
        foreach(var c in JsonUtility.FromJson<Corpus>(File.ReadAllText(path)).cases) {
            bool accepted=true;
            try {
                var bytes=new byte[c.hex.Length/2];for(int i=0;i<bytes.Length;i++)bytes[i]=Convert.ToByte(c.hex.Substring(i*2,2),16);
                string body=IpcFrameCodec.Read(new MemoryStream(bytes));
                if(c.kind=="PING")IpcProbeContract.Ping(body,Instance,Connection,c.sequence,c.first?Token:null);
                else IpcProbeContract.Pong(body,Instance,Connection,c.sequence);
            } catch(Exception e) when(e is IOException || e is InvalidDataException || e is System.Text.DecoderFallbackException) {accepted=false;}
            Assert.That(accepted,Is.EqualTo(c.accepted),c.name);
        }
    }
    [Test] public void ReadyExactSchemaAndDuplicates()
    {
        string json="{\"ipcVersion\":1,\"instanceId\":\""+Instance+"\",\"pid\":123,\"ipcPort\":1234,\"httpPort\":4321}";
        Assert.That(IpcProbeContract.Ready(json,Instance,123).ipcPort,Is.EqualTo(1234));
        foreach(string bad in new[]{json+"{}",json.Replace("\"pid\":123","\"pid\":123,\"pid\":123"),json.Replace("\"pid\":123","\"pid\":\"123\""),json.Replace("\"pid\":123","\"unknown\":123"),json.Replace("1234","65536")})
            Assert.Throws<InvalidDataException>(()=>IpcProbeContract.Ready(bad,Instance,123));
    }
    [Test] public void ConcatenatedFramesAndBoundedOutput()
    {
        using(var s=new MemoryStream()) {IpcFrameCodec.Write(s,"{}");IpcFrameCodec.Write(s,"{}");s.Position=0;Assert.That(IpcFrameCodec.Read(s),Is.EqualTo("{}"));Assert.That(IpcFrameCodec.Read(s),Is.EqualTo("{}"));Assert.That(s.Position,Is.EqualTo(s.Length));}
        Assert.Throws<InvalidDataException>(()=>IpcFrameCodec.Write(new MemoryStream(),new string('x',4097)));
    }
    [Test] public void InvalidSizeDoesNotRequestBodyAndUtf8OutputIsBounded()
    {
        Assert.Throws<InvalidDataException>(()=>IpcFrameCodec.Read(new PrefixOnly()));
        using(var s=new MemoryStream()) {
            IpcFrameCodec.Write(s,new string('é',2048));Assert.That(s.Length,Is.EqualTo(4100));s.Position=0;Assert.That(IpcFrameCodec.Read(s).Length,Is.EqualTo(2048));
        }
        Assert.Throws<InvalidDataException>(()=>IpcFrameCodec.Write(new MemoryStream(),new string('é',2049)));
        Assert.Throws<System.Text.EncoderFallbackException>(()=>IpcFrameCodec.Write(new MemoryStream(),"\ud800"));
    }
    private sealed class PrefixOnly : Stream
    {
        private int at;private readonly byte[] prefix={0,0,16,1};
        public override int Read(byte[] b,int offset,int count) {if(at>=4)Assert.Fail("Oversized body read requested");int n=Math.Min(count,4-at);Array.Copy(prefix,at,b,offset,n);at+=n;return n;}
        public override bool CanRead=>true;public override bool CanSeek=>false;public override bool CanWrite=>false;
        public override long Length=>4;public override long Position {get=>at;set=>throw new NotSupportedException();}
        public override void Flush(){} public override long Seek(long o,SeekOrigin k)=>throw new NotSupportedException();
        public override void SetLength(long n)=>throw new NotSupportedException();public override void Write(byte[] b,int o,int n)=>throw new NotSupportedException();
    }

}
