namespace GorillaEscape.Input
{
    // Unity-only projections remain separate from the engine-independent accepted input store.
    public sealed partial class PhoneInputStore
    {
        public bool TryPhoneInput(int player,out PhoneInput input){
            input=default;lock(gate){if(!latest.TryGetValue(player,out var s))return false;
                var a=s.acceleration;var r=s.rotationRate;
                input=new PhoneInput{PlayerId=player,ProtocolSample=s,Sequence=s.sequence,Timestamp=s.serverReceiveTimestamp,IsConnected=s.connectionState!="DISCONNECTED",HasAcceleration=a!=null&&a.hasX&&a.hasY&&a.hasZ,HasAngularVelocity=r!=null&&r.hasAlpha&&r.hasBeta&&r.hasGamma,HasOrientation=false,Orientation=UnityEngine.Quaternion.identity};
                if(input.HasAcceleration)input.Acceleration=new UnityEngine.Vector3(a.x,a.y,a.z);
                if(input.HasAngularVelocity)input.AngularVelocity=new UnityEngine.Vector3(r.alpha,r.beta,r.gamma);
                input.ActionPressed=s.touch!=null&&s.touch.hasPressed&&s.touch.pressed;return true;}
        }
    }
}
