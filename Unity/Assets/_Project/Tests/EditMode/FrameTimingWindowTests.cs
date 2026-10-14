using NUnit.Framework;
using GorillaEscape.CameraTracking;
namespace GorillaEscape.Tests
{
    public sealed class FrameTimingWindowTests
    {
        [Test] public void PercentilesUseReciprocalIntervalsAndRejectInvalidValues()
        {
            var w = new FrameTimingWindow(); w.Add(10); w.Add(20); w.Add(40); w.Add(double.NaN); w.Add(0);
            var s = w.Report(); Assert.AreEqual(3, s.N); Assert.AreEqual(2, s.errors);
            Assert.AreEqual(20, s.frameTimeP50Ms); Assert.AreEqual(40, s.frameTimeP95Ms);
            Assert.AreEqual(50, s.fpsP50); Assert.AreEqual(100, s.fpsP95);
            Assert.AreEqual(3000.0 / 70, s.effectiveFps, .00001);
        }
        [Test] public void RingRetainsLatestWindowAndReportsEvictionsSeparately()
        {
            var w = new FrameTimingWindow(); for (int i = 0; i < 5000; i++) w.Add(i < 904 ? 100 : 10);
            var s = w.Report(); Assert.AreEqual(5000, s.N); Assert.AreEqual(4096, s.retained);
            Assert.AreEqual(904, s.overwritten); Assert.AreEqual(10, s.frameTimeP95Ms);
            Assert.Less(s.effectiveFps, 100);
        }
    }
}
