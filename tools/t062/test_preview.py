import sys,unittest,threading,socket,struct,time,tempfile
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'spikes/camera'))
from preview import Preview,MAX_BYTES
from pipeline import CapturedFrame,Metrics
class Tests(unittest.TestCase):
 def test_latest_bounds_loopback_clean(self):
  stop=threading.Event();p=Preview(stop,Metrics(),lambda image:image)
  self.assertEqual(p.listener.getsockname()[0],'127.0.0.1')
  p.publish(CapturedFrame(1,b'outside-tests',0,0));self.assertIsNone(p.slot.pending);self.assertIsNone(p.latest)
  c=socket.create_connection(('127.0.0.1',p.port));c.settimeout(2);c.sendall(b'P');self.assertEqual(c.recv(4),b'\0'*4)
  for i in range(2,101):p.publish(CapturedFrame(i,b'frame'+str(i).encode(),0,0))
  time.sleep(.3);self.assertGreater(p.slot.overwritten,0)
  c.sendall(b'P');n=struct.unpack('!I',c.recv(4))[0];self.assertLessEqual(n,MAX_BYTES);self.assertEqual(c.recv(n),b'frame100')
  p.publish(CapturedFrame(101,b'x'*(MAX_BYTES+1),0,0));time.sleep(.3);self.assertEqual(p.errors,1)
  c.close();time.sleep(.3);self.assertFalse(p.enabled.is_set());self.assertIsNone(p.latest);self.assertIsNone(p.slot.pending)
  p.publish(CapturedFrame(102,b'outside-again',0,0));self.assertIsNone(p.slot.pending)
  port=p.port;p.close();self.assertIsNone(p.latest);self.assertIsNone(p.slot.pending)
  self.assertTrue(all(not t.is_alive() for t in p.threads))
  with self.assertRaises(OSError):socket.create_connection(('127.0.0.1',port),timeout=.2)
 def test_no_image_disk_or_protocol_route(self):
  s=(Path(__file__).resolve().parents[1]/'spikes/camera/preview.py').read_text()
  for bad in ['VideoCapture','imwrite','open(','write_bytes','write_text','base64','print(']:self.assertNotIn(bad,s)
  s=(Path(__file__).resolve().parents[1]/'spikes/camera/worker.py').read_text();self.assertIn("os.environ.get('GORILLA_QA_OVERLAY')=='1'",s)
if __name__=='__main__':unittest.main()
