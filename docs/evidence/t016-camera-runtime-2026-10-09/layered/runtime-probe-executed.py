import json,pathlib,cv2
from mediapipe.tasks import python
from mediapipe.tasks.python import vision
root=pathlib.Path(__file__).parent
with vision.PoseLandmarker.create_from_options(vision.PoseLandmarkerOptions(base_options=python.BaseOptions(model_asset_path=str(root/'pose_landmarker_lite-v1.task')),running_mode=vision.RunningMode.VIDEO,num_poses=2)):
 paths=sorted({line.split()[-1] for line in pathlib.Path('/proc/self/maps').read_text().splitlines() if len(line.split())>=6 and line.split()[-1].startswith('/')})
 print(json.dumps({'opencv':cv2.__version__,'loadedFiles':[str(pathlib.Path(p).relative_to(root)) if pathlib.Path(p).is_relative_to(root) else p for p in paths]},indent=2))
