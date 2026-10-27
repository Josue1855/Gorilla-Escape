namespace GorillaEscape.Gameplay.Input
{
    // A technical pause owns only the scale it changed. Never resumes a pre-existing pause.
    public sealed class JunglePauseLease
    {
        public bool Owned {get;private set;}
        float prior;
        public float Apply(bool paused,float current)
        {
            if(paused)
            {
                if(!Owned&&current>0){prior=current;Owned=true;}
                return Owned?0:current;
            }
            if(!Owned)return current;
            Owned=false;
            return current==0?prior:current;
        }
    }
}
