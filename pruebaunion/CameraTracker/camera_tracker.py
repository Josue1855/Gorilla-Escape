import math
import json
import os
import sys
import threading
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from urllib.parse import parse_qs, urlparse

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
    "bodyY": 0.5,
    "confidence": 0.0,
    "time": 0,
}
data_lock = threading.Lock()
rooms = {}
preview_frame = None


class PoseHandler(BaseHTTPRequestHandler):
    def do_GET(self):
        request = urlparse(self.path)
        if request.path == "/pose":
            with data_lock:
                payload = json.dumps(pose_data).encode("utf-8")
            self.send_response(200)
            self.send_header("Content-Type", "application/json")
            self.send_header("Cache-Control", "no-store")
            self.send_header("Access-Control-Allow-Origin", "*")
            self.send_header("Content-Length", str(len(payload)))
            self.end_headers()
            self.wfile.write(payload)
        elif request.path == "/motion":
            room = parse_qs(request.query).get("room", [""])[0].upper()
            with data_lock:
                sample = rooms.get(room)
            if sample is None:
                self.send_response(204)
                self.end_headers()
                return
            _, payload = sample
            encoded = payload.encode("utf-8")
            self.send_response(200)
            self.send_header("Content-Type", "application/json")
            self.send_header("Cache-Control", "no-store")
            self.send_header("Content-Length", str(len(encoded)))
            self.end_headers()
            self.wfile.write(encoded)
        elif request.path in ("/fonts/LilitaOne.ttf", "/fonts/NunitoSans.ttf"):
            font_path = os.path.join(ROOT, "..", "MotionRelay", "Fonts", os.path.basename(request.path))
            try:
                with open(font_path, "rb") as font:
                    payload = font.read()
            except FileNotFoundError:
                self.send_error(404)
                return
            self.send_response(200)
            self.send_header("Content-Type", "font/ttf")
            self.send_header("Cache-Control", "public, max-age=86400")
            self.send_header("Content-Length", str(len(payload)))
            self.end_headers()
            self.wfile.write(payload)
        elif request.path in ("/art/jungle.png", "/art/leaves.png"):
            art_path = os.path.join(ROOT, "..", "MotionRelay", "Art", os.path.basename(request.path))
            try:
                with open(art_path, "rb") as image:
                    payload = image.read()
            except FileNotFoundError:
                self.send_error(404)
                return
            self.send_response(200)
            self.send_header("Content-Type", "image/png")
            self.send_header("Cache-Control", "public, max-age=86400")
            self.send_header("Content-Length", str(len(payload)))
            self.end_headers()
            self.wfile.write(payload)
        elif request.path == "/":
            page_path = os.path.join(ROOT, "..", "MotionRelay", "index.html")
            with open(page_path, "rb") as page:
                payload = page.read()
            self.send_response(200)
            self.send_header("Content-Type", "text/html; charset=utf-8")
            self.send_header("Cache-Control", "no-store")
            self.send_header("Content-Length", str(len(payload)))
            self.end_headers()
            self.wfile.write(payload)
        else:
            self.send_response(404)
            self.end_headers()

    def do_POST(self):
        request = urlparse(self.path)
        if request.path != "/motion":
            self.send_response(404)
            self.end_headers()
            return
        room = parse_qs(request.query).get("room", [""])[0].upper()
        length = int(self.headers.get("Content-Length", "0"))
        payload = self.rfile.read(length).decode("utf-8")
        try:
            timestamp = json.loads(payload)["time"]
            if type(timestamp) is not int or timestamp <= 0:
                raise ValueError("Invalid motion timestamp")
        except (ValueError, TypeError, KeyError):
            self.send_error(400, "Invalid motion packet")
            return
        with data_lock:
            previous = rooms.get(room)
            if previous is None or timestamp > previous[0]:
                rooms[room] = (timestamp, payload)
        self.send_response(200)
        self.send_header("Cache-Control", "no-store")
        self.end_headers()
        self.wfile.write(b"ok")

    def log_message(self, *_):
        return


def run_server():
    ThreadingHTTPServer(("127.0.0.1", 8788), PoseHandler).serve_forever()


class PreviewHandler(BaseHTTPRequestHandler):
    def do_GET(self):
        path = urlparse(self.path).path
        if path == "/qr.png":
            url = parse_qs(urlparse(self.path).query).get("url", [""])[0]
            target = urlparse(url)
            if target.scheme != "https" or not (target.hostname or "").endswith(".trycloudflare.com") or len(url) > 200:
                self.send_error(400)
                return
            matrix = cv2.QRCodeEncoder_create().encode(url)
            matrix = cv2.copyMakeBorder(matrix, 4, 4, 4, 4, cv2.BORDER_CONSTANT, value=255)
            image = cv2.resize(matrix, None, fx=12, fy=12, interpolation=cv2.INTER_NEAREST)
            ok, png = cv2.imencode(".png", image)
            if ok:
                self.send_payload("image/png", png.tobytes())
            else:
                self.send_error(500)
            return
        if path == "/":
            self.send_payload("text/html; charset=utf-8", PREVIEW_PAGE.encode("utf-8"))
            return
        if path != "/frame.jpg":
            self.send_error(404)
            return

        with data_lock:
            frame = preview_frame
            wrists = (
                ("IZQ", pose_data["leftDetected"], pose_data["leftWristX"], pose_data["leftWristY"]),
                ("DER", pose_data["rightDetected"], pose_data["rightWristX"], pose_data["rightWristY"]),
            )
        if frame is None:
            self.send_error(503, "Esperando el primer fotograma")
            return

        image = frame.copy()
        height, width = image.shape[:2]
        for label, detected, x, y in wrists:
            if detected:
                point = (round(x * (width - 1)), round(y * (height - 1)))
                cv2.circle(image, point, 10, (0, 220, 255), -1)
                cv2.putText(image, label, (point[0] + 12, point[1]), cv2.FONT_HERSHEY_SIMPLEX,
                            .65, (0, 220, 255), 2)
        ok, jpeg = cv2.imencode(".jpg", image, [cv2.IMWRITE_JPEG_QUALITY, 75])
        if ok:
            self.send_payload("image/jpeg", jpeg.tobytes())
        else:
            self.send_error(500)

    def send_payload(self, mime_type, payload):
        self.send_response(200)
        self.send_header("Content-Type", mime_type)
        self.send_header("Cache-Control", "no-store")
        self.send_header("Content-Length", str(len(payload)))
        self.end_headers()
        try:
            self.wfile.write(payload)
        except (BrokenPipeError, ConnectionResetError):
            pass

    def log_message(self, *_):
        return


PREVIEW_PAGE = """<!doctype html>
<html lang="es"><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>Vista previa de la cámara</title>
<style>
body {background:#14211b;color:#fff;font:18px system-ui;margin:2rem}
img {display:block;width:min(100%,960px);height:auto;background:#000}
</style>
<h1>Vista previa de la cámara</h1>
<p>Mueve las manos dentro de la imagen. Los puntos amarillos indican las muñecas detectadas.</p>
<p id="status" role="status">Esperando el primer fotograma…</p>
<img id="camera" alt="Imagen local de la cámara">
<script>
const camera = document.querySelector('#camera');
const status = document.querySelector('#status');
function refresh() { camera.src = '/frame.jpg?t=' + Date.now(); }
camera.onload = () => { status.textContent = ''; setTimeout(refresh, 150); };
camera.onerror = () => { status.textContent = 'Esperando fotogramas de Unity…'; setTimeout(refresh, 700); };
refresh();
</script></html>"""


def start_preview():
    # Separate loopback port. Cloudflare forwards only the controller port (8788).
    try:
        server = ThreadingHTTPServer(("127.0.0.1", 8790), PreviewHandler)
    except OSError as error:
        print(f"Vista previa local no disponible: {error}", file=sys.stderr)
        return
    threading.Thread(target=server.serve_forever, daemon=True).start()


def find_hand_wrists(landmarker, image, timestamp):
    result = landmarker.detect_for_video(image, timestamp)
    wrists = {}
    for landmarks, categories in zip(result.hand_landmarks, result.handedness):
        if not landmarks or not categories:
            continue
        category = categories[0]
        side = category.category_name.lower()
        wrist = landmarks[0]
        if side not in ("left", "right") or not (0 <= wrist.x <= 1 and 0 <= wrist.y <= 1):
            continue
        if side not in wrists or category.score > wrists[side][1]:
            wrists[side] = (wrist, category.score)
    return wrists


def track_camera():
    global preview_frame
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
    hand_options = mp.tasks.vision.HandLandmarkerOptions(
        base_options=mp.tasks.BaseOptions(
            model_asset_path=os.path.join(ROOT, "models", "hand_landmarker.task")
        ),
        running_mode=mp.tasks.vision.RunningMode.VIDEO,
        num_hands=2,
    )
    active_hand = "right"
    # La imagen se analiza espejada para que el movimiento sea intuitivo.
    # Por ello, las etiquetas izquierda/derecha del modelo quedan intercambiadas.
    smooth_x, smooth_y, smooth_body = 0.5, 0.55, 0.5
    smooth_body_y = 0.5
    smooth_lx, smooth_ly, smooth_rx, smooth_ry = 0.35, 0.55, 0.65, 0.55
    start = time.monotonic()

    with (
        mp.tasks.vision.PoseLandmarker.create_from_options(options) as landmarker,
        mp.tasks.vision.HandLandmarker.create_from_options(hand_options) as hand_landmarker,
    ):
        while True:
            ok, frame = camera.read()
            if not ok:
                continue
            frame = cv2.flip(frame, 1)
            rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            timestamp = int((time.monotonic() - start) * 1000)
            image = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb)
            result = landmarker.detect_for_video(image, timestamp)

            left_wrist = right_wrist = None
            confidence = 0.0
            head_detected = False
            head_yaw = head_pitch = head_roll = 0.0
            if result.pose_landmarks:
                landmarks = result.pose_landmarks[0]
                left_candidate, right_candidate = landmarks[16], landmarks[15]
                left_confidence = min(left_candidate.visibility, left_candidate.presence)
                right_confidence = min(right_candidate.visibility, right_candidate.presence)
                if left_confidence > 0.38:
                    left_wrist = left_candidate
                if right_confidence > 0.38:
                    right_wrist = right_candidate
                    confidence = right_confidence

                nose, eye_a, eye_b = landmarks[0], landmarks[2], landmarks[5]
                if min(nose.visibility, eye_a.visibility, eye_b.visibility) > 0.55:
                    eye_left, eye_right = sorted((eye_a, eye_b), key=lambda eye: eye.x)
                    eye_dx = eye_right.x - eye_left.x
                    eye_dy = (eye_right.y - eye_left.y) * frame.shape[0] / frame.shape[1]
                    eye_span = math.hypot(eye_dx, eye_dy)
                    if eye_span > 0.012:
                        eye_x = (eye_left.x + eye_right.x) * 0.5
                        eye_y = (eye_left.y + eye_right.y) * 0.5
                        head_yaw = float((nose.x - eye_x) / eye_span * -50.0)
                        nose_height = (nose.y - eye_y) * frame.shape[0] / frame.shape[1]
                        head_pitch = float((nose_height / eye_span - 0.60) * -38.0)
                        head_roll = -math.degrees(math.atan2(eye_dy, eye_dx))
                        head_detected = True

                left_shoulder, right_shoulder = landmarks[11], landmarks[12]
                shoulder_x = (left_shoulder.x + right_shoulder.x) * 0.5
                shoulder_y = (left_shoulder.y + right_shoulder.y) * 0.5
                smooth_body = smooth_body * 0.68 + shoulder_x * 0.32
                smooth_body_y = smooth_body_y * 0.68 + shoulder_y * 0.32

            if left_wrist is None or right_wrist is None:
                # The hand model's handedness uses the mirrored selfie frame.
                wrists = find_hand_wrists(hand_landmarker, image, timestamp)
                if left_wrist is None and "left" in wrists:
                    left_wrist, _ = wrists["left"]
                if right_wrist is None and "right" in wrists:
                    right_wrist, confidence = wrists["right"]

            left_detected = left_wrist is not None
            right_detected = right_wrist is not None
            detected = right_detected and confidence > 0.42
            if left_detected:
                smooth_lx = smooth_lx * 0.35 + left_wrist.x * 0.65
                smooth_ly = smooth_ly * 0.35 + left_wrist.y * 0.65
            if right_detected:
                smooth_rx = smooth_rx * 0.35 + right_wrist.x * 0.65
                smooth_ry = smooth_ry * 0.35 + right_wrist.y * 0.65
            if detected:
                smooth_x = smooth_x * 0.42 + right_wrist.x * 0.58
                smooth_y = smooth_y * 0.42 + right_wrist.y * 0.58

            with data_lock:
                preview_frame = frame
                pose_data.update(
                    detected=detected,
                    headDetected=head_detected,
                    headYaw=head_yaw,
                    headPitch=head_pitch,
                    headRoll=head_roll,
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
                    bodyY=max(0.0, min(1.0, smooth_body_y)),
                    confidence=float(confidence),
                    time=int(time.time() * 1000),
                )

    camera.release()


def main():
    start_preview()
    threading.Thread(target=track_camera, daemon=True).start()
    run_server()


if __name__ == "__main__":
    main()

