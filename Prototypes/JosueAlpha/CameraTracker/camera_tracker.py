import json
import os
import sys
import threading
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer

ROOT = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(ROOT, "vendor"))

import cv2
import mediapipe as mp


pose_data = {
    "detected": False,
    "hand": "right",
    "wristX": 0.5,
    "wristY": 0.55,
    "leftDetected": False,
    "rightDetected": False,
    "leftWristX": 0.35,
    "leftWristY": 0.55,
    "rightWristX": 0.65,
    "rightWristY": 0.55,
    "bodyX": 0.5,
    "confidence": 0.0,
    "time": 0,
}
data_lock = threading.Lock()


class PoseHandler(BaseHTTPRequestHandler):
    def do_GET(self):
        if self.path.startswith("/pose"):
            with data_lock:
                payload = json.dumps(pose_data).encode("utf-8")
            self.send_response(200)
            self.send_header("Content-Type", "application/json")
            self.send_header("Cache-Control", "no-store")
            self.send_header("Access-Control-Allow-Origin", "*")
            self.send_header("Content-Length", str(len(payload)))
            self.end_headers()
            self.wfile.write(payload)
        else:
            self.send_response(404)
            self.end_headers()

    def log_message(self, *_):
        return


def run_server():
    ThreadingHTTPServer(("127.0.0.1", 8790), PoseHandler).serve_forever()


def point(landmarks, index):
    item = landmarks[index]
    return int(item.x * 960), int(item.y * 540)


def draw_pose(frame, landmarks, active_wrist):
    connections = [
        (11, 12), (11, 13), (13, 15), (12, 14), (14, 16),
        (11, 23), (12, 24), (23, 24), (23, 25), (24, 26),
    ]
    for first, second in connections:
        if landmarks[first].visibility > 0.45 and landmarks[second].visibility > 0.45:
            cv2.line(frame, point(landmarks, first), point(landmarks, second), (70, 230, 130), 4)
    for index in (11, 12, 13, 14, 15, 16, 23, 24):
        if landmarks[index].visibility > 0.45:
            color = (0, 220, 255) if index == active_wrist else (255, 210, 60)
            cv2.circle(frame, point(landmarks, index), 9, color, -1)


def main():
    threading.Thread(target=run_server, daemon=True).start()
    camera = cv2.VideoCapture(0, cv2.CAP_DSHOW)
    camera.set(cv2.CAP_PROP_FRAME_WIDTH, 960)
    camera.set(cv2.CAP_PROP_FRAME_HEIGHT, 540)
    camera.set(cv2.CAP_PROP_FPS, 30)
    if not camera.isOpened():
        raise RuntimeError("No se pudo abrir la cámara. Cierra otras aplicaciones que la estén usando.")

    options = mp.tasks.vision.PoseLandmarkerOptions(
        base_options=mp.tasks.BaseOptions(
            model_asset_path=os.path.join(ROOT, "models", "pose_landmarker_lite.task")
        ),
        running_mode=mp.tasks.vision.RunningMode.VIDEO,
        num_poses=1,
        min_pose_detection_confidence=0.45,
        min_pose_presence_confidence=0.45,
        min_tracking_confidence=0.45,
    )
    active_hand = "right"
    # La imagen se analiza espejada para que el movimiento sea intuitivo.
    # Por ello, las etiquetas izquierda/derecha del modelo quedan intercambiadas.
    active_wrist = 15
    smooth_x, smooth_y, smooth_body = 0.5, 0.55, 0.5
    smooth_lx, smooth_ly, smooth_rx, smooth_ry = 0.35, 0.55, 0.65, 0.55
    start = time.monotonic()

    with mp.tasks.vision.PoseLandmarker.create_from_options(options) as landmarker:
        while True:
            ok, frame = camera.read()
            if not ok:
                continue
            frame = cv2.flip(frame, 1)
            frame = cv2.resize(frame, (960, 540))
            rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            timestamp = int((time.monotonic() - start) * 1000)
            result = landmarker.detect_for_video(
                mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb), timestamp
            )

            detected = False
            left_detected = False
            right_detected = False
            confidence = 0.0
            if result.pose_landmarks:
                landmarks = result.pose_landmarks[0]
                wrist = landmarks[active_wrist]
                left_wrist = landmarks[16]
                right_wrist = landmarks[15]
                shoulders = (landmarks[11], landmarks[12])
                confidence = min(wrist.visibility, wrist.presence)
                detected = confidence > 0.42
                left_detected = min(left_wrist.visibility, left_wrist.presence) > 0.38
                right_detected = min(right_wrist.visibility, right_wrist.presence) > 0.38
                if left_detected:
                    smooth_lx = smooth_lx * 0.35 + left_wrist.x * 0.65
                    smooth_ly = smooth_ly * 0.35 + left_wrist.y * 0.65
                if right_detected:
                    smooth_rx = smooth_rx * 0.35 + right_wrist.x * 0.65
                    smooth_ry = smooth_ry * 0.35 + right_wrist.y * 0.65
                if detected:
                    smooth_x = smooth_x * 0.42 + wrist.x * 0.58
                    smooth_y = smooth_y * 0.42 + wrist.y * 0.58
                    body_x = (shoulders[0].x + shoulders[1].x) * 0.5
                    smooth_body = smooth_body * 0.68 + body_x * 0.32
                draw_pose(frame, landmarks, active_wrist)

            with data_lock:
                pose_data.update(
                    detected=detected,
                    hand=active_hand,
                    wristX=max(0.0, min(1.0, smooth_x)),
                    wristY=max(0.0, min(1.0, smooth_y)),
                    leftDetected=left_detected,
                    rightDetected=right_detected,
                    leftWristX=max(0.0, min(1.0, smooth_lx)),
                    leftWristY=max(0.0, min(1.0, smooth_ly)),
                    rightWristX=max(0.0, min(1.0, smooth_rx)),
                    rightWristY=max(0.0, min(1.0, smooth_ry)),
                    bodyX=max(0.0, min(1.0, smooth_body)),
                    confidence=float(confidence),
                    time=int(time.time() * 1000),
                )

            status = "DOS MANOS DETECTADAS" if left_detected and right_detected else "MUESTRA AMBAS MANOS"
            color = (70, 240, 120) if detected else (30, 80, 255)
            cv2.rectangle(frame, (0, 0), (960, 78), (12, 22, 28), -1)
            cv2.putText(frame, status, (24, 34), cv2.FONT_HERSHEY_SIMPLEX, 0.82, color, 2)
            cv2.putText(
                frame,
                f"Control: mano {active_hand.upper()}  |  H cambia mano  |  Q cierra",
                (24, 65), cv2.FONT_HERSHEY_SIMPLEX, 0.58, (235, 235, 235), 1,
            )
            cv2.imshow("Jungle Party - Rastreo corporal", frame)
            key = cv2.waitKey(1) & 0xFF
            if key in (ord("q"), 27):
                break
            if key == ord("h"):
                active_hand = "left" if active_hand == "right" else "right"
                active_wrist = 16 if active_hand == "left" else 15

    camera.release()
    cv2.destroyAllWindows()


if __name__ == "__main__":
    main()
