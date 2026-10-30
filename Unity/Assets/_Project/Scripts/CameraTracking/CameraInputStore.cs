using System;
using System.Collections.Generic;
using GorillaEscape.Contracts;
namespace GorillaEscape.CameraTracking
{
    public static class CameraGuidance
    {
        static bool Visible(CameraSubject subject,string name){
            var p=Array.Find(subject.landmarks,l=>l!=null&&l.name==name);
            return p!=null&&p.visible&&p.visibility>=.6f&&p.presence>=.6f&&p.x>=0&&p.x<=1&&p.y>=0&&p.y<=1;
        }
        public static string Describe(bool receiving,CameraInputWire frame,bool bothHands,string activeHand=null){
            if(!receiving)return "La cámara no está enviando imágenes. Comprueba permisos y conexión.";
            if(frame?.subjects==null||frame.subjects.Length==0)return "Ponte frente a la cámara. Busca buena luz y evita una ventana detrás de ti.";
            if(frame.subjects.Length>1||frame.subjects[0].ambiguous)return "Hay varias personas o el seguimiento es ambiguo. Deja una sola persona en el área.";
            var person=frame.subjects[0];
            if(person.landmarks==null)return "Ponte en el centro y muestra hombros y manos.";
            if(!Visible(person,"left-shoulder")||!Visible(person,"right-shoulder"))return "Muestra ambos hombros. Si no cabes en la imagen, aléjate un poco.";
            bool left=Visible(person,"left-wrist"),right=Visible(person,"right-wrist");
            if(bothHands&&!left&&!right)return "Muestra ambas manos dentro de la imagen.";
            if(bothHands&&!left)return "No se ve bien la mano izquierda. Muéstrala dentro de la imagen.";
            if(activeHand=="left"&&!left)return "Muestra tu mano izquierda. Evita taparla con el teléfono; la otra mano puede descansar.";
            if(activeHand!="left"&&!right)return "Muestra tu mano derecha. Evita taparla con el teléfono.";
            if(activeHand!=null)return "La cámara detecta tu mano de lanzamiento. No necesitas levantar la otra mano.";
            if(person.confidence<.6f)return "Mantente en el centro, con los brazos visibles y buena iluminación.";
            return "La cámara detecta tus hombros y manos. Mantente dentro de la imagen.";
        }
    }
    public sealed class CameraInputStore {
        static bool VisiblePoint(CameraSubject s,string name){
            var p=Array.Find(s.landmarks,l=>l!=null&&l.name==name);
            return p!=null&&p.visible&&p.visibility>=.6f&&p.presence>=.6f&&p.x>=0&&p.x<=1&&p.y>=0&&p.y<=1;
        }
        public static bool SliceSubjectUsable(CameraSubject s)=>s!=null&&!s.ambiguous&&s.landmarks!=null
            &&VisiblePoint(s,"left-shoulder")&&VisiblePoint(s,"right-shoulder")
            &&(VisiblePoint(s,"left-wrist")||VisiblePoint(s,"right-wrist"));
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
        // Presentation only: estimate the current visible landmark, never a missing person or limb.
        public bool TryVisualLandmark(int subjectId,string name,long now,out float x,out float y){
            x=y=0;var latest=Latest;
            if(latest==null||latest.trackingState!="TRACKING"||now<latest.captureTimestamp||now-latest.captureTimestamp>120)return false;
            var subject=Array.Find(latest.subjects,s=>s.subjectId==subjectId);
            if(subject==null||subject.ambiguous)return false;
            var point=Array.Find(subject.landmarks,p=>p.name==name);
            if(point==null||!point.visible||point.x<0||point.x>1||point.y<0||point.y>1)return false;
            x=point.x;y=point.y;
            if(frames.Count<2)return true;
            var prior=frames[frames.Count-2];long dt=latest.captureTimestamp-prior.captureTimestamp;
            if(dt<8||dt>250||prior.trackingState!="TRACKING")return true;
            var oldSubject=Array.Find(prior.subjects,s=>s.subjectId==subjectId);
            if(oldSubject==null||oldSubject.ambiguous)return true;
            var old=Array.Find(oldSubject.landmarks,p=>p.name==name);
            if(old==null||!old.visible||old.x<0||old.x>1||old.y<0||old.y>1)return true;
            float ahead=Math.Min(now-latest.captureTimestamp,80)/(float)dt;
            float dx=(point.x-old.x)*ahead,dy=(point.y-old.y)*ahead;
            float distance=(float)Math.Sqrt(dx*dx+dy*dy);
            if(distance>.05f){dx*=.05f/distance;dy*=.05f/distance;}
            x=Math.Max(0,Math.Min(1,x+dx));y=Math.Max(0,Math.Min(1,y+dy));return true;
        }
    }
}
