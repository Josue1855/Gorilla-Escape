using System;
using System.Globalization;
using GorillaEscape.Contracts;
using UnityEngine;
namespace GorillaEscape.QA {
    public sealed class LiveQaModel {
        public const string Unknown="NOT MEASURED";
        public bool Measuring {get;private set;} public long StartMs {get;private set;} public long EndMs {get;private set;}
        public string DeviceState="NOT CONFIRMED";
        public void Start(long now){Measuring=true;StartMs=now;EndMs=0;}
        public void Stop(long now){Measuring=false;EndMs=StartMs>0?now:0;}
        public void Reset(){Measuring=false;StartMs=EndMs=0;DeviceState="NOT CONFIRMED";}
        public static string Slot(LobbyPlayerWire p)=>p==null?"EMPTY":p.connectionState=="CONNECTED"&&!p.networkReady?"CONNECTING":p.connectionState;
        public static string Quality(PhoneInputWire input,LobbyPlayerWire player)=>player?.connectionState=="DISCONNECTED"?"DISCONNECTED":player?.inputReady==true?"INPUT_READY":input?.quality?.status=="degraded"?"INPUT_LIMITED":"NO_SENSOR_INPUT";
        public static string Value(bool present,float value)=>present?value.ToString("F2",CultureInfo.InvariantCulture):Unknown;
        public static string Triple(PhoneValues v,bool angular=false)=>v==null?Unknown:angular?"α "+Value(v.hasAlpha,v.alpha)+"  β "+Value(v.hasBeta,v.beta)+"  γ "+Value(v.hasGamma,v.gamma):"X "+Value(v.hasX,v.x)+"  Y "+Value(v.hasY,v.y)+"  Z "+Value(v.hasZ,v.z);
        public static bool HasFreshResponse(PhoneInputWire input,bool fresh)=>fresh&&input?.connectionState=="ACTIVE"&&input.acceleration?.hasX==true&&input.acceleration.hasY;
        public static Vector2 Response(PhoneInputWire p)=>new Vector2(p?.acceleration?.hasX==true?Mathf.Clamp(p.acceleration.x/20,-1,1):0,p?.acceleration?.hasY==true?Mathf.Clamp(p.acceleration.y/20,-1,1):0);
        public static Rect Letterbox(Rect area,float width,float height){float scale=Mathf.Min(area.width/width,area.height/height);return new Rect(area.center.x-width*scale/2,area.center.y-height*scale/2,width*scale,height*scale);}
        public static Vector2 Point(Rect picture,CameraLandmark l)=>new Vector2(picture.x+l.x*picture.width,picture.y+l.y*picture.height);
        // Browser source labels are claims, never proof of physical hardware.
        public static string Classification(string source)=>source=="replay"?"REPLAY":source=="synthetic"?"LAB":source=="emulator"?"EMULATOR":"NOT VERIFIED";
    }
}
