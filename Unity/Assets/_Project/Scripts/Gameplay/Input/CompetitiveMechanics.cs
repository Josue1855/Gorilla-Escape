using System;
namespace GorillaEscape.Gameplay.Input
{
    // Shared, deterministic sporting rules. Presentation randomness never changes an outcome.
    public static class CompetitiveMechanics
    {
        static double Number(double value) { if(double.IsNaN(value)||double.IsInfinity(value))throw new ArgumentException("NONFINITE_MECHANICS_INPUT");return value; }
        static double Unit(double value) => Math.Max(0,Math.Min(1,Number(value)));
        public static float TimingQuality(long phoneTimestamp,long cameraTimestamp)
            => phoneTimestamp<=0||cameraTimestamp<=0?0f:(float)Unit(1-Math.Abs((double)phoneTimestamp-cameraTimestamp)/200d);
        public static int SmashScore(float cameraSpeed,float phonePower,float handX,float timing)
        {
            Number(cameraSpeed);double power=PhoneStrikePower(phonePower);
            double direction=Unit(1-Math.Abs(handX-.5)*1.5);
            return (int)Math.Max(200,Math.Min(10000,Math.Round((power*.45+direction*.25+Unit(timing)*.20+.10)*10000,MidpointRounding.AwayFromZero)));
        }
        public static float PhoneStrikePower(float phonePower) => (float)Unit((Number(phonePower)-100)/320);
        public static int SmashImpactScore(float phonePower,float accuracy,float timing)
            => Unit(accuracy)<=0?0:(int)Math.Round((PhoneStrikePower(phonePower)*.45+Unit(accuracy)*.35+Unit(timing)*.20)*10000,MidpointRounding.AwayFromZero);
        public static int WhackScore(float phonePower,float accuracy,int combo)
            => 100+(int)Math.Round(40*Unit(accuracy)+60*PhoneStrikePower(phonePower))+Math.Min(6,Math.Max(0,combo))*10;
        public static float ThrowPower(float cameraSpeed,float phonePower,float elevation,float lateral,float timing)
            => (float)(Unit(Math.Max(cameraSpeed/2.6,phonePower/450d))*.60+(1-Unit(Math.Abs(elevation-43)/40d))*.20+Unit(timing)*.15+(1-Unit(Math.Abs(lateral)/18d))*.05);
        public static double GroundTime(double height,double verticalSpeed,double ground,double gravity)
        {
            Number(height);Number(verticalSpeed);Number(ground);Number(gravity);
            if(gravity>=0||height<ground)throw new ArgumentOutOfRangeException();
            return (verticalSpeed+Math.Sqrt(verticalSpeed*verticalSpeed-2*gravity*(height-ground)))/-gravity;
        }
        public static double Position(double origin,double velocity,double gravity,double seconds)
            => Number(origin)+Number(velocity)*Number(seconds)+.5*Number(gravity)*seconds*seconds;
        public static float VerticalVelocityForLanding(float origin,float ground,float gravity,float seconds)
        {
            Number(origin);Number(ground);Number(gravity);Number(seconds);
            if(seconds<=0||gravity>=0)throw new ArgumentOutOfRangeException();
            return (ground-origin-.5f*gravity*seconds*seconds)/seconds;
        }
        public const float DrumHitWindow=.24f;
        public static float BaseballTiming(float ballZ,float plateZ,float speed)
        {
            Number(ballZ);Number(plateZ);Number(speed);
            if(speed<=0)throw new ArgumentOutOfRangeException(nameof(speed));
            return (float)Unit(1-Math.Abs(ballZ-plateZ)/speed/.24);
        }
        public static float BaseballDistance(float timing,float handSpeed)
            => 25f+55f*(float)Unit(timing)+55f*(float)Unit((handSpeed-.5)/2);
    }
}
