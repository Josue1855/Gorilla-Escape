import importlib.util
import pathlib
import threading
import unittest
import sys
sys.path.insert(0,str(pathlib.Path(__file__).resolve().parents[1]/'spikes/camera'))
from pipeline import CapturedFrame, CapturePump, LatestFrameSlot, Metrics

class PipelineTests(unittest.TestCase):
    def frame(self,n):return CapturedFrame(n,object(),0,n)
    def test_latest_overwrite_bounded(self):
        s=LatestFrameSlot()
        for i in range(1,1001):s.publish(self.frame(i))
        self.assertEqual(999,s.overwritten);self.assertEqual(1000,s.take(threading.Event()).sequence);self.assertIsNone(s.pending)
    def test_no_duplicate_or_reordered_publish(self):
        s=LatestFrameSlot();s.publish(self.frame(2))
        for n in [2,1]:
            with self.assertRaises(ValueError):s.publish(self.frame(n))
    def test_close_wakes_waiting_consumer(self):
        s=LatestFrameSlot();result=[];t=threading.Thread(target=lambda:result.append(s.take(threading.Event())));t.start();s.close();t.join(1);self.assertFalse(t.is_alive());self.assertEqual([None],result)
    def test_camera_failure_explicit_and_released(self):
        class Camera:
            def read(self):return False,None
            def release(self):self.released=True
        c=Camera();p=CapturePump(c,threading.Event(),Metrics());p.start();p.thread.join(1)
        with self.assertRaisesRegex(RuntimeError,'CAMERA_READ_FAILED'):p.slot.take(p.stop)
        with self.assertRaisesRegex(RuntimeError,'CAMERA_READ_FAILED'):p.close()
        self.assertTrue(c.released)
    def test_capture_owned_copy_and_counters(self):
        class Image:
            def __init__(self,n):self.n=n
            def copy(self):return Image(self.n)
        class Camera:
            def __init__(self):self.image=Image(0);self.release_called=False
            def read(self):self.image.n+=1;return True,self.image
            def release(self):self.release_called=True
        c=Camera();stop=threading.Event();p=CapturePump(c,stop,Metrics());p.start()
        f=p.slot.take(stop);n=f.image.n;c.image.n=-1;self.assertEqual(n,f.image.n);p.close();self.assertTrue(p.released);self.assertFalse(p.thread.is_alive());self.assertLessEqual(p.slot.taken,p.slot.published)
    def test_stop_while_capture_waiting(self):
        stop=threading.Event();entered=threading.Event()
        class Camera:
            def read(self):entered.set();stop.wait();return False,None
            def release(self):pass
        p=CapturePump(Camera(),stop,Metrics());p.start();self.assertTrue(entered.wait(1));p.close();self.assertFalse(p.thread.is_alive())
    def test_stop_while_inference_owns_frame(self):
        s=LatestFrameSlot();s.publish(self.frame(1));f=s.take(threading.Event());s.publish(self.frame(2));s.close();self.assertEqual(1,f.sequence);self.assertIsNone(s.pending)
    def test_capture_inference_output_metrics_separate_and_monotonic(self):
        clock=[0];m=Metrics(warmup=1,clock=lambda:clock[0]);m.event('capture',0)
        for i in range(1,5):m.event('capture',i*1_000_000_000)
        m.event('inference',2_000_000_000,40);m.event('inference',4_000_000_000,60);m.event('output',4_000_000_000)
        clock[0]=5_000_000_000;r=m.report();self.assertEqual(4,r['captureFrames']);self.assertEqual(1,r['captureFps']);self.assertEqual(.5,r['inferenceFps']);self.assertEqual(1,r['outputFrames']);self.assertEqual(60,r['inferenceP95Ms']);self.assertEqual(4,r['measurementElapsedSeconds'])
    def test_timing_storage_bounded(self):
        m=Metrics(clock=lambda:0)
        for i in range(5000):m.event('captureRead',i,1)
        self.assertEqual(4096,len(m.timings['captureRead']))
    def test_latency_window_bounds_memory_but_preserves_lifetime_count_and_extremes(self):
        m=Metrics(warmup=1,clock=lambda:0)
        m.observe('age',99999,0)
        for i in range(5000):m.observe('age',i,2_000_000_000)
        stats=m.report()['ageStats']
        self.assertEqual(5000,stats['N']);self.assertEqual(4096,stats['retained'])
        self.assertEqual(0,stats['min']);self.assertEqual(4999,stats['max'])
        self.assertLessEqual(stats['P50'],stats['P95']);self.assertLessEqual(stats['P95'],stats['max'])

if __name__=='__main__':unittest.main()
