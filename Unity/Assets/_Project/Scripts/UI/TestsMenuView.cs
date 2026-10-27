using System;
using GorillaEscape.QA;
using GorillaEscape.Network;
using GorillaEscape.Contracts;
using GorillaEscape.Input.Fusion;
using UnityEngine;
namespace GorillaEscape.UI {
    public sealed class TestsMenuView {
        readonly QaRuntimeService s; readonly TestsMenuController controller;
        GUIStyle title,text,small;bool connectionPanel;
        public TestsMenuView(QaRuntimeService runtime,TestsMenuController controller){s=runtime;this.controller=controller;}
        static long Now()=>DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        static string Rate(bool? present,float? value)=>present==true&&value.HasValue?Number(value.Value):LiveQaModel.Unknown;
        static string Number(double n)=>n<0||double.IsNaN(n)?LiveQaModel.Unknown:n.ToString("F1",System.Globalization.CultureInfo.InvariantCulture);
        void Label(float x,float y,float w,string value,GUIStyle style=null)=>GUI.Label(new Rect(x,y,w,26),value,style??text);
        public void Draw(){if(title==null){title=new GUIStyle(GUI.skin.label){fontSize=20,fontStyle=FontStyle.Bold};text=new GUIStyle(GUI.skin.label){fontSize=15};small=new GUIStyle(text){fontSize=14};}
            GUI.color=new Color(.06f,.08f,.11f);GUI.DrawTexture(new Rect(0,0,1280,940),Texture2D.whiteTexture);GUI.color=Color.white;
            Label(20,12,1230,"GORILLA ESCAPE  /  CENTRO DE PRUEBAS",title);
            Label(20,46,720,"Reference device: iPhone 15 • Source: "+(s.physicalConfirmed&&s.sample?.quality?.source=="physical"?"PHYSICAL (operator confirmed)":LiveQaModel.Classification(s.sample?.quality?.source)));
            string cameraState=s.CameraStatus??(s.vision.State=="RUNNING"&&s.preview?.State=="READY"&&s.cameraTexture!=null?"CAMERA READY":s.vision.State=="FAILED"||s.preview?.State=="FAILED"||s.error=="PREVIEW INVALID"?"CAMERA FAILED":"CAMERA STARTING");
            Label(20,77,720,cameraState+" • "+(s.cameras.Fresh(Now(),250,20)?s.cameras.Latest.trackingState:"LOST")+" • frame age "+(s.cameras.Latest==null?LiveQaModel.Unknown:(Now()-s.cameras.Latest.captureTimestamp)+" ms")+" • "+s.error);
            Rect imageArea=new Rect(20,110,720,410);GUI.DrawTexture(imageArea,Texture2D.blackTexture);var picture=LiveQaModel.Letterbox(imageArea,s.cameraTexture?.width??4,s.cameraTexture?.height??3);
            if(s.cameraTexture!=null)GUI.DrawTexture(picture,s.cameraTexture,ScaleMode.StretchToFill,false);
            if(s.cameras.Fresh(Now(),250,20)){GUI.BeginGroup(picture);var local=new Rect(0,0,picture.width,picture.height);DrawCamera(local,s.cameras.Latest);GUI.EndGroup();}
            Label(20,501,720,"Latest preview and landmarks are independent • no frame alignment claim",small);
            var association=s.fusion.Association;Label(20,550,720,"PlayerLock: "+association.State+" • player "+association.PlayerId+" • subject "+association.SubjectId);
            Label(20,580,720,"RESPUESTA VISIBLE — aceleración aceptada X/Y / ±20 m/s²",title);
            Rect visual=new Rect(20,616,720,126);GUI.DrawTexture(visual,Texture2D.grayTexture);var response=LiveQaModel.Response(s.sample);bool fresh=LiveQaModel.HasFreshResponse(s.sample,s.java.PhoneInputs.Fresh(s.selected,Now(),250));
            if(fresh){if(Event.current.type==EventType.Repaint)s.renderedFrame=Time.frameCount;GUI.color=Color.cyan;GUI.DrawTexture(new Rect(visual.center.x+response.x*(visual.width/2-12)-8,visual.center.y-response.y*(visual.height/2-12)-8,16,16),Texture2D.whiteTexture);GUI.color=Color.white;}else Label(35,662,680,"NO FRESH ACCEPTED XY INPUT — indicator inactive");
            Label(20,750,720,"Source event UTC: "+Stamp(s.sample?.clientTimestamp??0)+" • Java receipt: "+Stamp(s.sample?.serverReceiveTimestamp??0),small);
            Label(20,776,720,"Unity receipt: "+Stamp(s.sample?.unityReceiveTimestamp??0)+" • accept: "+Stamp(s.sample?.unityAcceptTimestamp??0),small);
            Label(20,800,720,"Visual applied UTC: "+Stamp(s.applied)+" / frame "+s.appliedFrame+" • end-of-frame: "+Stamp(s.presented)+" / "+s.presentedFrame,small);
            Label(20,824,720,"source→Java: NOT MEASURED (clock offset unknown) • Java→Unity: "+Delta(s.sample?.serverReceiveTimestamp??0,s.sample?.unityReceiveTimestamp??0)+" ms",small);
            if(GUI.Button(new Rect(530,48,205,26),"Export summary only"))controller.ExportSummary();
            Label(20,875,720,s.exportMessage,small);
            Label(20,848,720,"Unity processing: "+Delta(s.sample?.unityReceiveTimestamp??0,s.sample?.unityAcceptTimestamp??0)+" ms • accept→apply: "+Delta(s.sample?.unityAcceptTimestamp??0,s.applied)+" ms; no scanout/photon",small);
            Label(20,100,720,"Prepare: "+(s.java.QaHosting?.prepareUrl??"WAITING"),small);
            Label(20,526,720,"CA SHA256: "+(s.java.QaHosting?.fingerprint??"NOT MEASURED"),small);
            DrawRight();
            if(GUI.Button(new Rect(320,12,190,28),"CONEXIÓN / QA"))connectionPanel=!connectionPanel;
            if(connectionPanel)DrawConnection();
            if(GUI.Button(new Rect(20,904,180,32),"← VOLVER"))controller.Back();
            Label(220,905,1030,"Phase0 · Respuesta técnica Unity · T019 físico pendiente · Sin gameplay",small);
        }
        void DrawConnection(){
            var box=new Rect(260,160,760,600);GUI.Box(box,"");
            var n=s.java.QaConnection;float x=280,y=185;
            Label(x,y,710,"CONECTIVIDAD — QA",title);y+=38;
            string[] rows={"Requested Mode: "+(n?.requestedMode??Environment.GetEnvironmentVariable("GORILLA_CONNECTION_MODE")??"AUTO"),"Resolved Mode: "+(n?.resolvedMode??"NOT RUN"),"Bootstrap State: "+(n?.bootstrapState??"NOT RUN"),"Tunnel State: "+(n?.tunnelState??"STOPPED"),"Public Origin: "+(n?.publicOrigin??"NOT RUN"),"LAN Origin: "+(n?.lanOrigin??"NOT RUN"),"Fallback Reason: "+(n?.fallbackReason??"NONE"),"Tunnel Startup ms: "+(n?.tunnelStartupMs.ToString()??"NOT RUN")+" / Fallback ms: "+(n?.fallbackMs.ToString()??"NOT RUN"),"WAN availability: "+(n?.wanAvailability??"NOT MEASURED"),"GAMEPLAY PATH: LAN DIRECT","CLOUDFLARED DOES NOT CARRY GAMEPLAY"};
            foreach(var row in rows){Label(x,y,710,row,small);y+=27;}
            if(n?.resolvedMode=="CABIN")Label(x,y,710,"PREPARAR TELÉFONO si es la primera conexión; ver guía de preparación",small);y+=35;
            bool before=GUI.enabled;GUI.enabled=s.java.State==IpcState.RUNNING&&(s.java.Lobby.Snapshot?.players?.Length??0)==0&&n?.bootstrapState!="STARTING";
            int i=0;foreach(string mode in new[]{"AUTO","CABIN","CLOUDFLARED"}){if(GUI.Button(new Rect(x+i*230,y,220,32),mode=="CABIN"?"CABAÑA":mode))s.ChangeConnectionMode(mode);i++;}GUI.enabled=before;y+=40;
            Label(x,y,710,"Selector disponible sin jugadores. Pruebas oficiales offline: CABAÑA.",small);
            if(GUI.Button(new Rect(780,700,200,32),"CERRAR"))connectionPanel=false;
        }
        static string Stamp(long n)=>n<=0?LiveQaModel.Unknown:n.ToString();
        static string Delta(long a,long b)=>a<=0||b<a?LiveQaModel.Unknown:(b-a).ToString();
        void DrawCamera(Rect picture,CameraInputWire frame){var settings=new FusionSettings();GUI.color=Color.yellow;Line(new Vector2(picture.width*settings.roiMinX,0),new Vector2(picture.width*settings.roiMinX,picture.height));Line(new Vector2(picture.width*settings.roiMaxX,0),new Vector2(picture.width*settings.roiMaxX,picture.height));
            foreach(var s in frame.subjects){GUI.color=s.ambiguous?Color.red:Color.green;foreach(var point in s.landmarks)if(point.visible){var p=LiveQaModel.Point(picture,point);GUI.DrawTexture(new Rect(p.x-3,p.y-3,6,6),Texture2D.whiteTexture);}
                foreach(var pair in new[]{"left-shoulder,right-shoulder","left-shoulder,left-elbow","left-elbow,left-wrist","left-wrist,left-hand","right-shoulder,right-elbow","right-elbow,right-wrist","right-wrist,right-hand"}){var names=pair.Split(',');var a=Array.Find(s.landmarks,l=>l.name==names[0]);var b=Array.Find(s.landmarks,l=>l.name==names[1]);if(a?.visible==true&&b?.visible==true)Line(LiveQaModel.Point(picture,a),LiveQaModel.Point(picture,b));}
                Label(5,5+(s.subjectId%2)*24,picture.width-10,"Subject #"+s.subjectId+" • confidence "+s.confidence.ToString("F2")+" • ambiguous "+s.ambiguous,small);}
            GUI.color=Color.white;
        }
        static void Line(Vector2 a,Vector2 b){var matrix=GUI.matrix;GUIUtility.RotateAroundPivot(Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg,a);GUI.DrawTexture(new Rect(a.x,a.y,Vector2.Distance(a,b),2),Texture2D.whiteTexture);GUI.matrix=matrix;}
        void DrawRight(){float x=770;Label(x,76,490,"TELÉFONO • "+s.FirewallState,title);
            GUI.Label(new Rect(1025,80,240,55),s.QrLifecycle+"\nTTL "+(s.java.Qa==null?LiveQaModel.Unknown:Math.Ceiling(s.java.Qa.ttlMs/1000d)+" s"),new GUIStyle(small){wordWrap=true});
            if(s.qrTexture!=null){GUI.DrawTexture(new Rect(x,110,240,240),s.qrTexture,ScaleMode.ScaleToFit,false);if(s.qrVisible==0&&Event.current.type==EventType.Repaint)s.qrVisible=Now();}else Label(x,160,460,!s.ServicesEnabled?"QR NOT AVAILABLE":s.qrState=="STARTING"?"Waiting for secure LAN backend…":"Regenerate when ready");
            Label(1025,115,240,"DEVICE STATE (confirm)",small);if(GUI.Button(new Rect(1025,145,230,30),"Prepared"))controller.SetDeviceState("Prepared");if(GUI.Button(new Rect(1025,182,230,30),"Unprepared"))controller.SetDeviceState("Unprepared");Label(1025,221,230,s.model.DeviceState,small);
            GUI.enabled=s.java.State==IpcState.RUNNING;if(GUI.Button(new Rect(1025,257,230,34),"GENERAR NUEVO QR"))controller.RegenerateQr();GUI.enabled=true;
            Label(1025,301,230,"Platform/browser: NOT MEASURED",small);
            s.physicalConfirmed=GUI.Toggle(new Rect(1025,325,230,24),s.physicalConfirmed,"I confirm a physical phone");
            Label(x,49,490,s.java.QaHosting?.origin??(!s.ServicesEnabled?s.JavaStatus:"Secure LAN origin: WAITING"),small);
            var lobby=s.java.Lobby.Snapshot;Label(x,358,490,"Lobby "+(lobby?.phase??"WAITING")+" • players "+(lobby?.players?.Length.ToString()??LiveQaModel.Unknown)+" • IPC "+s.java.State);
            for(int i=1;i<=4;i++){var p=lobby?.players==null?null:Array.Find(lobby.players,v=>v.playerId==i);float y=389+(i-1)*38;if(GUI.Button(new Rect(x,y,80,32),"Player "+i))controller.SelectPlayer(i);Label(x+90,y,395,LiveQaModel.Slot(p)+"  N:"+(p?.networkReady.ToString()??"—")+" I:"+(p?.inputReady.ToString()??"—")+" P:"+(p?.playerReady.ToString()??"—")+" epoch "+(p?.connectionEpoch.ToString()??"—"),small);}
            var chosen=s.java.Lobby.Snapshot?.players==null?null:Array.Find(s.java.Lobby.Snapshot.players,v=>v.playerId==s.selected);
            Label(x,546,490,"PLAYER "+s.selected+" • "+(chosen?.networkReady==true?"NETWORK_READY":"NETWORK WAITING")+" • "+LiveQaModel.Quality(s.sample,chosen)+" / "+(s.sample?.connectionState??"WAITING"));
            Label(x,572,490,"Acceleration: "+LiveQaModel.Triple(s.sample?.acceleration),small);Label(x,595,490,"With gravity: "+LiveQaModel.Triple(s.sample?.accelerationIncludingGravity),small);Label(x,618,490,"Rotation rate: "+LiveQaModel.Triple(s.sample?.rotationRate,true),small);Label(x,641,490,"Orientation: "+LiveQaModel.Triple(s.sample?.orientation,true),small);
            Label(x,664,490,"Screen: "+(s.sample?.screenOrientation==null?LiveQaModel.Unknown:LiveQaModel.Value(s.sample.screenOrientation.hasAngle,s.sample.screenOrientation.angle))+" • seq "+(s.sample?.sequence.ToString()??"—")+" • epoch "+(s.sample?.connectionEpoch.ToString()??"—"),small);
            var phone=s.PhoneMetrics;
            Label(x,687,490,"Input age: "+(s.sample==null?LiveQaModel.Unknown:s.java.PhoneInputs.AgeMilliseconds(s.selected,Now())+" ms")+" • Hz M/O/emit: "+Rate(phone?.hasMotionHz,phone?.motionHz)+" / "+Rate(phone?.hasOrientationHz,phone?.orientationHz)+" / "+Rate(phone?.hasEmissionHz,phone?.emissionHz),small);
            var m=s.java.QaMetrics;Label(x,710,490,m==null?"Connections/reconnects: NOT MEASURED":"Connect HELLO: "+(m.succeeded-s.baseline.succeeded)+" / "+(m.attempted-s.baseline.attempted)+" • reconnect: "+(m.reconnectSucceeded-s.baseline.reconnectSucceeded)+" / "+(m.reconnectAttempted-s.baseline.reconnectAttempted),small);
            Label(x,733,490,"Success: "+(m==null||m.attempted==s.baseline.attempted?LiveQaModel.Unknown:Number(100d*(m.succeeded-s.baseline.succeeded)/(m.attempted-s.baseline.attempted))+"%")+" • RTT P50/P95 ms: "+Rate(phone?.hasRttP50,phone?.rttP50)+" / "+Rate(phone?.hasRttP95,phone?.rttP95),small);
            var c=s.vision.QaMetrics;Label(x,756,490,"FPS capture "+Number(c?.captureFps??-1)+" • inference "+Number(c?.inferenceFps??-1)+" • preview "+Number(c?.previewFps??-1)+" • Unity FPS "+Number(s.fps)+" / ms "+Number(s.fps>0?1000/s.fps:-1),small);
            Label(x,779,490,"Preview overwrites "+(c?.overwritten.ToString()??LiveQaModel.Unknown)+" / errors "+(c?.errors.ToString()??LiveQaModel.Unknown)+" • CPU/RSS: NOT MEASURED",small);
            if(GUI.Button(new Rect(x,809,150,30),"START TEST"))controller.StartTest();if(GUI.Button(new Rect(x+160,809,140,30),"STOP TEST"))controller.StopTest();if(GUI.Button(new Rect(x+310,809,175,30),"RESET QA SESSION"))controller.ResetMetrics();
            Label(x,846,490,"QR→technical ready: "+Delta(s.qrVisible,s.joinEnd)+" ms • "+s.joinClass,small);Label(x,870,490,"Start "+Stamp(s.qrVisible)+" • end "+Stamp(s.joinEnd)+" • measuring "+s.model.Measuring,small);
        }
    }
}
