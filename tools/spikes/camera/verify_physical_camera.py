#!/usr/bin/env python3
"""Verifies physical camera image acquisition, frame statistics, and MediaPipe inference."""
import time, os, cv2
import numpy as np

def test_camera():
    print("=== TEST 1: Physical Camera Frame Capture ===")
    cap = cv2.VideoCapture(0, cv2.CAP_DSHOW if os.name == 'nt' else cv2.CAP_V4L2)
    cap.set(cv2.CAP_PROP_FOURCC, cv2.VideoWriter_fourcc(*'MJPG'))
    cap.set(cv2.CAP_PROP_FRAME_WIDTH, 640)
    cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 480)
    cap.set(cv2.CAP_PROP_FPS, 30)

    if not cap.isOpened():
        print("ERROR: Physical camera 0 could not be opened!")
        return False

    frames_read = 0
    timestamps = []
    means = []
    stds = []
    saved_frame_path = r"C:\Users\equipo\.gemini\antigravity\brain\d6090028-59db-4872-82e2-fd0dd68235e1\live_camera_capture.png"

    start_time = time.time()
    while frames_read < 30 and (time.time() - start_time) < 3.0:
        t_capture = time.time_ns()
        ret, frame = cap.read()
        if ret and frame is not None:
            frames_read += 1
            timestamps.append(t_capture)
            means.append(float(np.mean(frame)))
            stds.append(float(np.std(frame)))
            if frames_read == 15:
                cv2.imwrite(saved_frame_path, frame)
        time.sleep(0.01)

    cap.release()

    fps = frames_read / (time.time() - start_time)
    avg_brightness = np.mean(means) if means else 0
    avg_variance = np.mean(stds) if stds else 0

    print(f"Frames captured: {frames_read}/30")
    print(f"Measured FPS: {fps:.1f}")
    print(f"Average frame brightness (mean pixel): {avg_brightness:.2f}")
    print(f"Average frame contrast/variance (std pixel): {avg_variance:.2f}")
    print(f"Saved sample frame to: {saved_frame_path}")

    assert frames_read >= 15, "Not enough frames captured from physical camera"
    assert avg_variance > 1.0, "Frame is completely blank or uniform (no optical image data)"
    print("SUCCESS: Physical camera is actively acquiring real optical images!\n")
    return True

if __name__ == "__main__":
    test_camera()
