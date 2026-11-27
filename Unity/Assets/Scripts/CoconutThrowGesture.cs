namespace GorillaEscape.Gameplay.Input
{
    // Captures a short swing through its peak instead of releasing at its first threshold crossing.
    internal sealed class CoconutThrowGesture
    {
        private long previousAt, startedAt;
        private float peak;
        private bool armed, swinging;

        internal void Reset()
        {
            previousAt = startedAt = 0;
            peak = 0;
            armed = true;
            swinging = false;
        }

        internal bool Observe(long time, float power, float handSpeed, out float releasePower)
        {
            releasePower = 0;
            if (time <= previousAt) return false;
            if (previousAt > 0 && time - previousAt > 700) Reset();
            previousAt = time;
            if (!swinging)
            {
                if (power < 100f) armed = true;
                if (!armed || power < 120f) return false;
                swinging = true;
                startedAt = time;
                peak = power;
                return false;
            }

            peak = System.Math.Max(peak, power);
            long elapsed = time - startedAt;
            if (elapsed < 50 || (power > peak * .78f && elapsed < 150)) return false;
            releasePower = peak;
            swinging = armed = false;
            return true;
        }
    }
}

