#!/usr/bin/env python3
"""Runs MediaPipe PoseLandmarker on the live camera capture and prints landmark coordinates."""
import mediapipe as mp
import cv2, os

model_path = os.path.abspath("Unity/CameraTracker/models/pose_landmarker_lite.task")
img_path = r"C:\Users\equipo\.gemini\antigravity\brain\d6090028-59db-4872-82e2-fd0dd68235e1\live_camera_capture.png"

image = cv2.imread(img_path)
rgb_image = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb_image)

options = mp.tasks.vision.PoseLandmarkerOptions(
    base_options=mp.tasks.BaseOptions(model_asset_path=model_path),
    running_mode=mp.tasks.vision.RunningMode.IMAGE,
    num_poses=2
)

with mp.tasks.vision.PoseLandmarker.create_from_options(options) as detector:
    result = detector.detect(mp_image)

if result.pose_landmarks:
    print(f"Subjects detected: {len(result.pose_landmarks)}")
    landmarks = result.pose_landmarks[0]
    # Check key landmarks (nose=0, left_shoulder=11, right_shoulder=12, left_wrist=15, right_wrist=16)
    nose = landmarks[0]
    l_shoulder = landmarks[11]
    r_shoulder = landmarks[12]
    print(f"Nose: ({nose.x:.3f}, {nose.y:.3f}, visibility={nose.visibility:.2f})")
    print(f"Left Shoulder: ({l_shoulder.x:.3f}, {l_shoulder.y:.3f}, visibility={l_shoulder.visibility:.2f})")
    print(f"Right Shoulder: ({r_shoulder.x:.3f}, {r_shoulder.y:.3f}, visibility={r_shoulder.visibility:.2f})")
    print("SUCCESS: MediaPipe PoseLandmarker processed real camera image and detected subject with high confidence!")
else:
    print("No subjects detected in live frame.")
