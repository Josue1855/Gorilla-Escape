import hashlib,json,pathlib,tempfile,unittest,subprocess,sys
from bundle import verify
class BundleTests(unittest.TestCase):
 def test_manifest_hash_validation(self):
  with tempfile.TemporaryDirectory() as d:
   root=pathlib.Path(d);p=root/'worker.py';p.write_text('fixture');(root/'manifest.json').write_text(json.dumps({'files':[{'path':'worker.py','sha256':hashlib.sha256(p.read_bytes()).hexdigest()}]}));verify(root);p.write_text('changed')
   with self.assertRaisesRegex(ValueError,'VISION_MANIFEST_INVALID'):verify(root)
 def test_manifest_path_escape(self):
  with tempfile.TemporaryDirectory() as d:
   root=pathlib.Path(d);(root/'manifest.json').write_text(json.dumps({'files':[{'path':'../outside','sha256':'x'}]}))
   with self.assertRaisesRegex(ValueError,'VISION_MANIFEST_INVALID'):verify(root)
 def test_missing_model_fails_without_camera(self):
  worker=pathlib.Path(__file__).resolve().parents[1]/'spikes/camera/worker.py'
  result=subprocess.run([sys.executable,str(worker),'--mode','physical','--model','/missing/model'],input='',capture_output=True,text=True,timeout=5)
  self.assertEqual(2,result.returncode);self.assertIn('CAMERA_MODEL_MISSING',result.stderr);self.assertNotIn('CAMERA_READY',result.stdout)
 def test_detector_creation_failure_releases_camera(self):
  import types
  from unittest.mock import patch
  sys.path.insert(0,str(pathlib.Path(__file__).resolve().parents[1]/'spikes/camera'))
  import worker
  class Camera:
   released=False
   def set(self,*args):pass
   def isOpened(self):return True
   def release(self):self.released=True
  camera=Camera();cv2=types.SimpleNamespace(VideoCapture=lambda *args:camera,VideoWriter_fourcc=lambda *args:0)
  for key in ['CAP_V4L2','CAP_PROP_FOURCC','CAP_PROP_FRAME_WIDTH','CAP_PROP_FRAME_HEIGHT','CAP_PROP_FPS','CAP_PROP_BUFFERSIZE']:setattr(cv2,key,0)
  def fail(*args,**kwargs):raise RuntimeError('DETECTOR_FAILURE_FIXTURE')
  mp=types.SimpleNamespace(tasks=types.SimpleNamespace(BaseOptions=lambda **kw:None,vision=types.SimpleNamespace(PoseLandmarker=types.SimpleNamespace(create_from_options=fail),PoseLandmarkerOptions=lambda **kw:None,RunningMode=types.SimpleNamespace(VIDEO=1))))
  session=types.SimpleNamespace(before={},describe=lambda *a:{},close=lambda:None)
  with tempfile.NamedTemporaryFile() as model,patch.dict(sys.modules,{'cv2':cv2,'mediapipe':mp}),patch.object(worker,'CameraSession',return_value=session),patch.object(sys,'argv',['worker','--mode','physical','--model',model.name]),patch.object(worker.os,'read',return_value=b''):
   with self.assertRaisesRegex(RuntimeError,'DETECTOR_FAILURE_FIXTURE'):worker.main()
  self.assertTrue(camera.released)
if __name__=='__main__':unittest.main()
