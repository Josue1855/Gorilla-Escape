using System;
using System.IO;
using System.Text;

namespace GorillaEscape.Contracts
{
    // Only probe v1 objects: six envelope fields and five READY fields. No JSON DOM,
    // arrays, arbitrary values, extension registry or general serialization API.
    public static class IpcProbeContract
    {
        public static IpcReady Ready(string json, string instance, int pid)
        {
            var r = new Reader(json); var value = new IpcReady(); int seen = 0;
            r.Open();
            do {
                string key = r.Key();
                switch (key) {
                    case "ipcVersion": Mark(ref seen,1); value.ipcVersion=r.Integer(); break;
                    case "instanceId": Mark(ref seen,2); value.instanceId=r.String(); break;
                    case "pid": Mark(ref seen,4); value.pid=r.Integer(); break;
                    case "ipcPort": Mark(ref seen,8); value.ipcPort=r.Integer(); break;
                    case "httpPort": Mark(ref seen,16); value.httpPort=r.Integer(); break;
                    default: throw Bad("UNKNOWN_FIELD");
                }
            } while(r.More());
            r.End();
            if(seen!=31 || value.ipcVersion!=1 || !Uuid(value.instanceId) || value.instanceId!=instance || value.pid!=pid
                || value.ipcPort<1 || value.ipcPort>65535 || value.httpPort<1 || value.httpPort>65535) throw Bad("READY_INVALID");
            return value;
        }
        public static IpcPong Pong(string json,string instance,string connection,int sequence) => Envelope(json,instance,connection,sequence,null,"PONG");
        public static void Ping(string json,string instance,string connection,int sequence,string token) => Envelope(json,instance,connection,sequence,token,"PING");
        private static IpcPong Envelope(string json,string instance,string connection,int sequence,string token,string type)
        {
            var r=new Reader(json);var value=new IpcPong();int seen=0;
            r.Open();
            do {
                string key=r.Key();
                switch(key) {
                    case "ipcVersion":Mark(ref seen,1);value.ipcVersion=r.Integer();break;
                    case "type":Mark(ref seen,2);value.type=r.String();break;
                    case "instanceId":Mark(ref seen,4);value.instanceId=r.String();break;
                    case "connectionId":Mark(ref seen,8);value.connectionId=r.String();break;
                    case "sequence":Mark(ref seen,16);value.sequence=r.Integer();break;
                    case "payload":Mark(ref seen,32);r.Payload(token);break;
                    default:throw Bad("UNKNOWN_FIELD");
                }
            } while(r.More());
            r.End();
            if(seen!=63 || value.ipcVersion!=1 || value.type!=type || !Uuid(value.instanceId) || !Uuid(value.connectionId)
                || value.instanceId!=instance || (connection!=null && value.connectionId!=connection)
                || sequence<1 || value.sequence!=sequence) throw Bad("CONTRACT");
            return value;
        }
        private static void Mark(ref int seen,int bit) {if((seen&bit)!=0)throw Bad("DUPLICATE_FIELD");seen|=bit;}
        private static bool Uuid(string s) {Guid g;return s!=null && s.Length==36 && Guid.TryParse(s,out g) && g.ToString()==s;}
        private static InvalidDataException Bad(string code) => new InvalidDataException("IPC_"+code);
        private sealed class Reader
        {
            private readonly string text;private int at;
            public Reader(string input) {if(input==null || input.Length>4096)throw Bad("FRAME_SIZE");text=input;}
            private void Space() {while(at<text.Length && (text[at]==' '||text[at]=='\t'||text[at]=='\r'||text[at]=='\n'))at++;}
            private bool Take(char c) {Space();if(at<text.Length && text[at]==c){at++;return true;}return false;}
            private void Need(char c) {if(!Take(c))throw Bad("JSON_INVALID");}
            public void Open() {Need('{');}
            public string Key() {string s=String();Need(':');return s;}
            public bool More() {if(Take('}'))return false;Need(',');return true;}
            public void End() {Space();if(at!=text.Length)throw Bad("JSON_INVALID");}
            public int Integer()
            {
                Space();int start=at,value=0;
                while(at<text.Length && text[at]>='0' && text[at]<='9') {
                    int n=text[at++]-'0';if(value>(int.MaxValue-n)/10)throw Bad("CONTRACT_TYPE");value=value*10+n;
                }
                if(at==start || (at-start>1 && text[start]=='0'))throw Bad("CONTRACT_TYPE");
                // Decimal/exponent/quoted/negative numbers never enter int32 fields.
                if(at<text.Length && text[at]!=',' && text[at]!='}' && text[at]!=' ' && text[at]!='\t' && text[at]!='\n' && text[at]!='\r')throw Bad("CONTRACT_TYPE");
                return value;
            }
            public void Payload(string token)
            {
                Open();if(token==null){Need('}');return;}
                if(Key()!="launchToken")throw Bad("CONTRACT_FIELDS");string supplied=String();
                if(token.Length!=43 || supplied.Length!=43)throw Bad("TOKEN");int difference=0;
                for(int i=0;i<43;i++) {char c=supplied[i];if(!((c>='A'&&c<='Z')||(c>='a'&&c<='z')||(c>='0'&&c<='9')||c=='_'||c=='-'))throw Bad("TOKEN");difference|=c^token[i];}
                if(difference!=0)throw Bad("TOKEN");Need('}');
            }
            public string String()
            {
                Need('"');var result=new StringBuilder(43);
                while(at<text.Length) {
                    char c=text[at++];if(c=='"')return result.ToString();if(c<32)throw Bad("JSON_INVALID");
                    if(c=='\\') {
                        if(at==text.Length)throw Bad("JSON_INVALID");c=text[at++];
                        switch(c) {
                            case '"':case '\\':case '/':break;
                            case 'b':c='\b';break;case 'f':c='\f';break;case 'n':c='\n';break;case 'r':c='\r';break;case 't':c='\t';break;
                            case 'u':c=Hex();break;default:throw Bad("JSON_INVALID");
                        }
                    }
                    if(char.IsHighSurrogate(c)) {
                        // IPC keys and values are ASCII. Validate a pair before rejecting
                        // it by the known-field contract; never accept an unpaired escape.
                        if(at+1>=text.Length || text[at++]!='\\' || text[at++]!='u' || !char.IsLowSurrogate(Hex()))throw Bad("JSON_INVALID");
                        throw Bad("CONTRACT_TYPE");
                    }
                    if(char.IsLowSurrogate(c))throw Bad("JSON_INVALID");
                    if(c>127 || result.Length==43)throw Bad("CONTRACT_TYPE");result.Append(c);
                }
                throw Bad("JSON_INVALID");
            }
            private char Hex()
            {
                int value=0;for(int i=0;i<4;i++) {
                    if(at==text.Length)throw Bad("JSON_INVALID");char c=text[at++];int n=c>='0'&&c<='9'?c-'0':c>='a'&&c<='f'?c-'a'+10:c>='A'&&c<='F'?c-'A'+10:-1;
                    if(n<0)throw Bad("JSON_INVALID");value=value*16+n;
                }return (char)value;
            }
        }
    }
}
