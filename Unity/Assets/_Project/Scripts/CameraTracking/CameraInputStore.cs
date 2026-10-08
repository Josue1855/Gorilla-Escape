using System;
using System.Collections.Generic;
using GorillaEscape.Contracts;
namespace GorillaEscape.CameraTracking
{
    public sealed class CameraInputStore {
        public const int Capacity=16;
        readonly List<CameraInputWire> frames=new List<CameraInputWire>();
        public long Accepted {get;private set;} public long Rejected {get;private set;}
        public CameraInputWire Latest => frames.Count==0?null:frames[frames.Count-1];
        public IReadOnlyList<CameraInputWire> Frames => frames;
        public void Clear(){frames.Clear();}
        public bool Accept(CameraInputWire f,long received){
            if(!Valid(f)||received<f.processTimestamp||Latest!=null&&f.frameSequence<=Latest.frameSequence){Rejected++;return false;}
            f.unityReceiveTimestamp=received;frames.Add(f);if(frames.Count>Capacity)frames.RemoveAt(0);Accepted++;return true;
        }
        public static bool Valid(CameraInputWire f){
            if(f==null||f.version!=1||f.frameSequence<1||f.captureTimestamp<0||f.processTimestamp<f.captureTimestamp||f.width<1||f.width>4096||f.height<1||f.height>4096||f.clockDomain!="pc-unix-ms"||f.captureTimeSource!="read-complete"&&f.captureTimeSource!="fixture"||f.source!="physical-webcam"&&f.source!="replay"&&f.source!="synthetic"||f.trackingState!="TRACKING"&&f.trackingState!="LOST"||f.quality!="observed"&&f.quality!="fixture"||f.subjects==null||f.subjects.Length>2)return false;
            if((f.subjects.Length==0)!=(f.trackingState=="LOST"))return false;
            var ids=new HashSet<int>();
            foreach(var s in f.subjects){
                if(s==null||s.subjectId<1||!ids.Add(s.subjectId)||!Unit(s.centerX)||!Unit(s.centerY)||!Unit(s.confidence)||s.landmarks==null||s.landmarks.Length>8)return false;
                var names=new HashSet<string>();
                foreach(var l in s.landmarks){if(l==null||!Known(l.name)||!names.Add(l.name)||!Finite(l.x)||!Finite(l.y)||!Finite(l.z)||!Unit(l.visibility)||!Unit(l.presence))return false;}
                foreach(var name in new[]{"left-shoulder","right-shoulder","left-elbow","right-elbow","left-wrist","right-wrist"})if(!names.Contains(name))return false;
            }
            return true;
        }
        static bool Known(string n){return n=="left-shoulder"||n=="right-shoulder"||n=="left-elbow"||n=="right-elbow"||n=="left-wrist"||n=="right-wrist"||n=="left-hand"||n=="right-hand";}
        static bool Finite(float x)=>!float.IsNaN(x)&&!float.IsInfinity(x);
        static bool Unit(float x)=>Finite(x)&&x>=0&&x<=1;
        public long Age(long now)=>Latest==null?long.MaxValue:Math.Max(0,now-Latest.captureTimestamp);
        public bool Fresh(long now,long maxAge,long futureTolerance)=>Latest!=null&&Latest.captureTimestamp<=now+futureTolerance&&Age(now)<=maxAge;
    }
}
