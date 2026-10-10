"""Bounded capture/inference handover. No persistence, interpolation or duplicate frames."""
from collections import deque
from dataclasses import dataclass
import math
import threading
import time

@dataclass(frozen=True)
class CapturedFrame:
    sequence: int
    image: object
    capture_timestamp: int
    monotonic_ns: int
    acquisition_ns: int | None = None

class Metrics:
    def __init__(self, warmup=0, clock=time.monotonic_ns):
        self.clock=clock; self.start=clock(); self.after=self.start+int(warmup*1e9)
        self.counts={}; self.first={}; self.last={}; self.timings={}; self.lock=threading.Lock();self.samples={};self.sample_counts={};self.extremes={}
    def observe(self,name,value,stamp=None):
        stamp=self.clock() if stamp is None else stamp
        if stamp<self.after:return
        with self.lock:
            self.samples.setdefault(name,deque(maxlen=4096)).append(value)
            self.sample_counts[name]=self.sample_counts.get(name,0)+1
            low,high=self.extremes.get(name,(value,value));self.extremes[name]=(min(low,value),max(high,value))
    def event(self, name, stamp=None, duration=None):
        stamp=self.clock() if stamp is None else stamp
        if stamp<self.after:return
        with self.lock:
            self.counts[name]=self.counts.get(name,0)+1
            self.first.setdefault(name,stamp);self.last[name]=stamp
            if duration is not None:self.timings.setdefault(name,deque(maxlen=4096)).append(duration)
    def report(self):
        with self.lock:
            result={'measurementElapsedSeconds':max(0,(self.clock()-self.after)/1e9)}
            for name,n in self.counts.items():
                elapsed=(self.last[name]-self.first[name])/1e9
                result[name+'Frames']=n;result[name+'Duration']=elapsed
                result[name+'Fps']=(n-1)/elapsed if elapsed>0 else None
            for name,values in self.timings.items():
                values=sorted(values);result[name+'P50Ms']=values[math.ceil(len(values)*.5)-1];result[name+'P95Ms']=values[math.ceil(len(values)*.95)-1]
            for name,values in self.samples.items():
                values=sorted(values);low,high=self.extremes[name]
                result[name+'Stats']=dict(N=self.sample_counts[name],retained=len(values),P05=values[math.ceil(len(values)*.05)-1],P50=values[math.ceil(len(values)*.5)-1],P95=values[math.ceil(len(values)*.95)-1],min=low,max=high)
            return result

class LatestFrameSlot:
    def __init__(self):
        self.condition=threading.Condition();self.pending=None;self.closed=False;self.error=None
        self.published=0;self.taken=0;self.overwritten=0;self.last_sequence=0
    def publish(self, frame):
        with self.condition:
            if self.closed:return False
            if frame.sequence<=self.last_sequence:raise ValueError('CAPTURE_SEQUENCE_INVALID')
            self.last_sequence=frame.sequence;self.published+=1
            if self.pending is not None:self.overwritten+=1
            self.pending=frame;self.condition.notify();return True
    def take(self, stop):
        with self.condition:
            while self.pending is None and not self.closed and not stop.is_set():self.condition.wait(.1)
            if self.error is not None:raise RuntimeError(self.error)
            if stop.is_set() or self.closed:return None
            frame=self.pending;self.pending=None;self.taken+=1;return frame
    def close(self, error=None):
        with self.condition:self.closed=True;self.error=error if error is not None else self.error;self.pending=None;self.condition.notify_all()

class CapturePump:
    def __init__(self, capture, stop, metrics, clock=time.monotonic_ns, acquisition_clock=None):
        self.capture=capture;self.stop=stop;self.metrics=metrics;self.clock=clock
        self.slot=LatestFrameSlot();self.frames=0;self.released=False
        self.thread=threading.Thread(target=self.run,name='camera-capture',daemon=True)
        self.acquisition_clock=acquisition_clock;self.previous_acquisition=None;self.timestamp_unavailable=0;self.previous_read=None
    def start(self):self.thread.start()
    def run(self):
        try:
            while not self.stop.is_set():
                before=self.clock();ok,image=self.capture.read();stamp=self.clock();utc=time.time_ns()//1_000_000
                if self.stop.is_set():break
                if not ok:raise RuntimeError('CAMERA_READ_FAILED')
                self.frames+=1
                self.metrics.event('capture',stamp)
                self.metrics.event('captureRead',stamp,(stamp-before)/1e6)
                self.metrics.observe('captureReadDurationMs',(stamp-before)/1e6,stamp)
                if self.previous_read is not None:self.metrics.observe('captureIntervalMs',(stamp-self.previous_read)/1e6,stamp)
                self.previous_read=stamp
                try:acquired=self.acquisition_clock() if self.acquisition_clock else None
                except (ValueError,TypeError,RuntimeError):acquired=None
                if acquired is not None and 0<=stamp-acquired<=10_000_000_000 and (self.previous_acquisition is None or acquired>self.previous_acquisition):
                    self.previous_acquisition=acquired;self.metrics.observe('cameraDeliveryMs',(stamp-acquired)/1e6,stamp)
                else:acquired=None;self.timestamp_unavailable+=1
                # Producer relinquishes a detached owned buffer; never mutates the pending/inferred image.
                frame=CapturedFrame(self.frames,image.copy(),utc,stamp,acquired)
                self.slot.publish(frame)
        except Exception:
            self.slot.close('CAMERA_READ_FAILED');self.stop.set()
        finally:
            self.capture.release();self.released=True
    def close(self, timeout=2):
        self.stop.set();self.slot.close();self.thread.join(timeout)
        if self.thread.is_alive():raise RuntimeError('CAPTURE_JOIN_TIMEOUT')
        if self.slot.error is not None:raise RuntimeError(self.slot.error)
