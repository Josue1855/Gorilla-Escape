# PC camera / Input Fusion laboratory

Scope: local Linux spike, not a distributed product runtime or minigame. Unity owns the vision child and
fusion; existing Java/mobile/IPC owns its prior responsibilities. No frames/video are sent over network.
[Contract](../../../Shared/Protocol/camera/README.md) / [validation](../../../docs/PHASE0_CAMERA_FUSION_VALIDATION.md).

## Reproduce

Prepare a private virtual environment with Python 3.12 on Linux, install the exact
`requirements-linux-lab.lock`, and prepare the official Pose Landmarker lite float16 v1 model before the run.
The tested paths are `$HOME/.cache/gorilla-camera-venv` and `$HOME/.cache/gorilla-camera-models/pose_landmarker_lite-v1.task`.
No dependency/model download happens during capture or gameplay. Source/model/native hashes and package
license metadata are in the evidence inventory. Future product packaging requires its own approval/audit.

With the existing Linux Player and Java JAR built:

```
node tools/spikes/camera/run.mjs <new-result.json> replay
node tools/spikes/camera/run.mjs <new-result.json> unavailable
node tools/spikes/camera/run.mjs <new-result.json> failure
node tools/spikes/camera/run.mjs <new-result.json> physical
```

Uses existing installed Playwright/Chrome and QR decoder; does not add a second mobile client/protocol.
Never overwrite an evidence output. Managed Java tests must run serially; preserve the singleton guard.
Own Player is closed through X11 WM_DELETE_WINDOW, then Unity waits for normal Java/vision cleanup.
Harness records and fails forced cleanup, residual own children or missing clean exit.
Private operator/resume credentials stay in process memory; no raw SDP, QR credential or token in results.
Lab turn commands in an operator-owned local cache file contain revision/playerId/READY only.
No network/configuration changes, phone flags, public hosting or global package installation.

## Dependency decision

Reuse native MediaPipe official Python Tasks wheel 0.10.33 + one OpenCV contrib wheel 4.13.0.92, in an isolated
venv. No pre-existing desktop MediaPipe C#/Java detector/plugin was available. Building a C++/Unity plugin
would add platform toolchains and native packaging before testing the required lab gate. The small supervised
Python adapter is therefore limited to this Linux lab and optional; it does not replace Java or Unity and
is not a new public/server backend. Fixture mode uses stdlib only, suitable for deterministic CI.

Direct packages are Apache-2.0; wheel/native transitives retain their own BSD/MIT/Apache/LGPL notices.
`dependencies.json` enumerates 18 installed packages and native hashes, including NumPy/OpenBLAS,
OpenCV/Qt/FFmpeg components and MediaPipe Tasks native code. Matplotlib and sounddevice are required
wheel transitives; microphone/audio APIs are never opened. No duplicate cv2 package is installed.
The BlazePose GHUM family model card states Apache-2.0; exact bundle and all transitive redistribution
notices must be checked before shipping binaries. Model stays private cache, not repository/product bundle.

Primary sources:
- [official Pose Landmarker Python API](https://developers.google.com/edge/mediapipe/solutions/vision/pose_landmarker/python)
- [MediaPipe license](https://github.com/google-ai-edge/mediapipe/blob/master/LICENSE)
- [OpenCV license](https://github.com/opencv/opencv/blob/4.x/LICENSE)
- [BlazePose GHUM model card](https://storage.googleapis.com/mediapipe-assets/Model%20Card%20BlazePose%20GHUM%203D.pdf)

## Supervisor

`GORILLA_CAMERA_LAB=1` enables diagnostic wiring only. Configurable Python/script/model/fixture/device paths,
mode and JSON lab FusionSettings. Existing Java is started once by IpcProbeRunner; camera never starts Java.
Startup deadline 10 s, normal stdin EOF stop 3 s, own-child fallback and total 5 s cleanup bound. Stdout
has bounded lines/latest-only handover, stderr is drained without publishing contents. No automatic retry.
Normal camera loss/recovery is data, not process restart. Native hangs/host crashes require later hardening.

Physical test saves only aggregate timings/confidence/counts. No pictures or physical body-landmark traces.
In replay, detailed sanitized synthetic FusionFrames may be retained. Lack of a person is not a detector PASS.
One active camera player per turn; explicit READY assigns the technical PlayerId to an ephemeral ROI track.
Physical reacquisition after a subject ID changes requires explicit READY; no biometric identity is inferred.
EOF uses unbuffered descriptor reading to avoid Python buffered stdin finalization lock on early error exit.
EOF/failure tests verify normal exit0 / deliberately failed exit2 separately.
No sporting action, competitive result, global leak guarantee or Windows behaviour is established.
