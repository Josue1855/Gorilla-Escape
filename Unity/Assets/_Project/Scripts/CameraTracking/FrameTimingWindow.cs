using System;
namespace GorillaEscape.CameraTracking
{
    // Numeric intervals only. The common A/B observer is intentionally minimal in both arms.
    public sealed class FrameTimingWindow
    {
        public const int Capacity = 4096;
        readonly double[] milliseconds = new double[Capacity];
        int cursor, size; long count, errors; double elapsed;
        public void Add(double ms)
        {
            if (double.IsNaN(ms) || double.IsInfinity(ms) || ms <= 0) { errors++; return; }
            milliseconds[cursor] = ms; cursor = (cursor + 1) % Capacity;
            size = Math.Min(size + 1, Capacity); count++; elapsed += ms;
        }
        public Snapshot Report()
        {
            var frames = new double[size]; var fps = new double[size];
            for (int i = 0; i < size; i++) { frames[i] = milliseconds[i]; fps[i] = 1000 / frames[i]; }
            Array.Sort(frames); Array.Sort(fps);
            return new Snapshot { N = count, retained = size, capacity = Capacity, overwritten = Math.Max(0, count - size), errors = errors,
                frameTimeP50Ms = Percentile(frames, .5), frameTimeP95Ms = Percentile(frames, .95),
                fpsP50 = Percentile(fps, .5), fpsP95 = Percentile(fps, .95), effectiveFps = elapsed > 0 ? count * 1000 / elapsed : -1,
                sampledSeconds = elapsed / 1000, clock = "Unity System.Diagnostics.Stopwatch; process-local monotonic ticks / Frequency -> ms",
                percentileScope = "nearest rank over retained intervals; N and effectiveFps cover full measurement; FPS percentiles sort reciprocal intervals" };
        }
        static double Percentile(double[] values, double p) => values.Length == 0 ? -1 : values[(int)Math.Ceiling(values.Length * p) - 1];
        [Serializable] public sealed class Snapshot
        {
            public long N, overwritten, errors; public int retained, capacity;
            public double frameTimeP50Ms, frameTimeP95Ms, fpsP50, fpsP95, effectiveFps, sampledSeconds;
            public string clock, percentileScope;
        }
    }
}
