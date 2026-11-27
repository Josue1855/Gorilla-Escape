"""QA-only memory preview. The sole capture owner publishes; never opens a camera.
A loopback consumer enables encoding with P; disconnect discards both bounded slots.
"""
import socket,struct,threading,time
from pipeline import LatestFrameSlot
MAX_BYTES=65536
WIDTH,HEIGHT,FPS=320,240,5
class Preview:
    def __init__(self,stop,metrics,encoder=None):
        self.stop=threading.Event();self.metrics=metrics;self.slot=LatestFrameSlot();self.latest=None;self.lock=threading.Lock();self.client=None;self.errors=0
        self.enabled=threading.Event()
        self.encoder=encoder or self.jpeg
        self.listener=socket.socket();self.listener.bind(('127.0.0.1',0));self.listener.listen(1);self.listener.settimeout(.2)
        self.port=self.listener.getsockname()[1]
        self.threads=[threading.Thread(target=self.encode,name='qa-preview-encode',daemon=True),threading.Thread(target=self.serve,name='qa-preview-socket',daemon=True)]
        for t in self.threads:t.start()
    @staticmethod
    def jpeg(image):
        import cv2
        h,w=image.shape[:2];ratio=min(WIDTH/w,HEIGHT/h)
        small=cv2.resize(image,(max(1,round(w*ratio)),max(1,round(h*ratio))))
        ok,data=cv2.imencode('.jpg',small,[cv2.IMWRITE_JPEG_QUALITY,55])
        if not ok:raise ValueError('PREVIEW_ENCODE')
        return data.tobytes()
    def publish(self,frame):
        with self.lock:
            if self.enabled.is_set():self.slot.publish(frame)
    def encode(self):
        while not self.stop.is_set():
            start=time.monotonic();frame=self.slot.take(self.stop)
            if frame is None:break
            try:
                if self.enabled.is_set():
                    data=self.encoder(frame.image)
                    if not 0<len(data)<=MAX_BYTES:raise ValueError('PREVIEW_BOUND')
                    with self.lock:
                        if self.enabled.is_set():self.latest=data;self.metrics.event('preview')
            except Exception:self.errors+=1
            frame=None;data=None
            self.stop.wait(max(0,1/FPS-(time.monotonic()-start)))
    def deactivate(self):
        with self.lock:
            self.enabled.clear();self.latest=None
            # The capture path publishes under this lock; no late frame can outlive deactivation.
            with self.slot.condition:self.slot.pending=None
    def serve(self):
        while not self.stop.is_set():
            try:c,_=self.listener.accept()
            except socket.timeout:continue
            except OSError:break
            self.client=c;c.settimeout(.5)
            try:
                while not self.stop.is_set():
                    if c.recv(1)!=b'P':break
                    self.enabled.set()
                    with self.lock:data=self.latest
                    c.sendall(struct.pack('!I',len(data) if data else 0)+(data or b''));data=None
                    self.stop.wait(1/FPS)
            except (OSError,TimeoutError):pass
            finally:self.deactivate();c.close();self.client=None
    def close(self):
        self.stop.set();self.slot.close();self.listener.close()
        if self.client:
            try:self.client.shutdown(socket.SHUT_RDWR)
            except OSError:pass
        for t in self.threads:t.join(2)
        self.deactivate()
        if any(t.is_alive() for t in self.threads):raise RuntimeError('PREVIEW_SHUTDOWN')
