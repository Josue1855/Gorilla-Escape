# T016 — Camera capture/inference and Linux runtime candidate

2026-10-09 America/Chihuahua. Base/develop `be6980a4651c5278a53804219e6cbbb11f9412ea`; branch `feature/camera-t016-performance-packaging`. **LOCAL CANDIDATE ONLY; T016 OPEN.** AC1 FAIL; AC2 PARTIAL; AC3 PASS static + physical runtime audit. No PR/merge/closure because acceptance gates are unmet. E0 OPEN, Phase0 IN PROGRESS, Phase1 NOT STARTED.

## Measured cause and architecture

Historical17.611159FPS was capture+inference/output in a synchronous loop. Historical processingP50/P9539.341/65.884ms, read7.655/36.538ms. That architecture prevents camera cadence from being independent of inference. These are historical observations, not a new measurement of the old source.

New architecture preserves OpenCV/V4L2→MediaPipe Tasks VIDEO with num_poses=2→private stdout→VisionSupervisor→CameraInput. One capture thread reads real frames and transfers a detached owned array through one latest-frame slot. Main worker thread consumes each capture sequence once, does BGR→RGB/detect_for_video and serializes the bounded eight-landmark/two-subject contract. No extra inference thread/callback pipeline, interpolation, artificial duplication, frame history or image queue. The [official API](https://developers.google.com/edge/mediapipe/solutions/vision/pose_landmarker/python) documents VIDEO as blocking its caller; keeping a separate reader is simpler than introducing LIVE_STREAM callbacks.

Latest-only slot overwrites pending images; inference owns the consumed frame and never accesses a producer-mutated buffer. At most one waiting and one consumed frame; producer read/clone and MediaPipe RGB/Image conversions are bounded transient buffers. No image persists to disk/network. Increasing capture frameSequence may have gaps; Unity's existing ordered-sequence rejection remains valid. Ephemeral subjectId/ambiguity/0–2 subjects/explicit READY/no automatic replacement remain unchanged.

Capture/read, inference and output FPS use monotonic first/last event intervals `(N-1)/seconds`; bounded timing history4096. UTC captureTimestamp remains PC read-complete proxy, not exposure time. `captureFrames`, `inferenceFrames`, `outputFrames` count after warmup; `capturedFrames`, `inferredFrames`, `cameraInputEmitted`, overwrite/discard counts include warmup. `trackingGapFrames` counts absent pose, never hardware drops. Hardware dropped frames UNKNOWN.

## Hardware and physical evidence

Official DEC-015 reference: Dell Latitude5540, Intel i7-1355U, Iris Xe, MemTotal32538848kB; Pop!_OS24.04LTS x86_64/kernel7.1.5-76070105-generic; Integrated_Webcam_FHD `/dev/video0`, uvcvideo/V4L2. Actual negotiated MJPG640×480, driver30FPS, buffer1; detector Pose Landmarker lite float16v1, num_poses2. No resolution or pose-count reduction.

Every benchmark run:5s warmup +35s measurement, real camera. **Driver-reported30 != measured>=30.** All attempts retained in [evidence](evidence/t016-camera-runtime-2026-10-09/). No human Player Lock acceptance/20 entries/occlusion/return test; no landmarks/images retained. Tracking-present/low confidence does not prove reliable human tracking.

| Group | Capture FPS, each run | Inference FPS, each run | Purpose |
|---|---|---|---|
| Initial, original controls |19.96690930576716;19.965562937094447;19.964207540620894|19.963582582308824;19.970942697250287;19.9295345047082|Three physical trials of decoupled pipeline|
| Dynamic-framerate disabled temporarily |19.68290442337904;19.79649302267055;19.796497494422095|19.677897701315246;19.789461621079713;19.791527439801566|No improvement; all controls restored|
| Manual exposure diagnostic, bundle offline |22.00154154345133;21.971955988039472;21.97302491228105|21.988957494224355;21.976343000152255;21.968978547166707|Auto-exposure1, exposure156, dynamic-framerate0; not adopted product preset; quality NOT VALIDATED; restored|
| Final source/default controls/bundle offline |19.940556524680336|19.95321279017395|Final candidate; cache hidden and isolated WAN|
| Capture-only, no inference |19.967887056140583|NOT RUN|Acquisition limit also exists without MediaPipe|

Final output19.95321650054743FPS; captureReadP50/P9550.174531/53.547268ms; inferenceP50/P9531.092922/52.245931ms. Final captured/inferred/emitted795/795/795 including warmup, measuredcapture698/inference699/output699; overwrites0, pendingdiscard0. Initial group overwrites0/0/2; manual0/0/0. Duration35.116174413s after warmup. Counts may differ across warmup/end boundaries because acquisition and completion are independently timestamped.

The camera-only20FPS result proves the current acquisition ceiling independently of inference; temporary exposure controls did not meet30. **Exact underlying sensor/USB/firmware cause remains unproven.** Do not infer hardware defect or dropped-frame counts. No rounding/tolerance/threshold change; AC1 FAIL. Further acquisition/backend investigation or explicit PO change of hardware/criterion is required before closure. Nine exploratory runs plus one final candidate run are kept separately; early experiments preceded final cleanup/UTC-stamping hardening and are not claimed as final-source acceptance.

## Runtime bundle

`tools/t016/bundle.py` builds a fresh `Vision/` beside the Unity Linux Player from prepared pinned inputs. No PyInstaller/new freezer/new dependency/version upgrade. Includes copied Python3.12.3 executable/stdlib,18 pinned MediaPipe/OpenCV/NumPy/transitives, required dynamic libraries, worker/pipeline, fixture, local model, package notices/Python license and relative per-file SHA-256 manifest. No pyvenv.cfg/activation/pip, developer venv fallback, pip install or runtime download. Launcher isolates Python user/site paths and verifies every manifested file before starting the worker.

Model: Pose Landmarker lite float16v1,5777746bytes, SHA-256 `59929e1d1ee95287735ddd833b19cf4ac46d29bc7afddbbf6753c459690d574a`. [Exact source](https://storage.googleapis.com/mediapipe-models/pose_landmarker/pose_landmarker_lite/float16/1/pose_landmarker_lite.task); [family model card](https://storage.googleapis.com/mediapipe-assets/Model%20Card%20BlazePose%20GHUM%203D.pdf) states Apache2.0. Model/binaries are local staging only, not uploaded to repository.

Vision payload approximately456MB uncompressed. Keeping existing wheels avoids a new freezer/runtime stack but includes Qt5.15.18 and FFmpeg (avcodec62.11.100/avformat62.3.100/avutil60.8.100) transitives. MediaPipe native library, OpenCV, NumPy/OpenBLAS, Python extensions and copied system dependencies have manifest hashes/inventory. Host Linux loader/glibc2.39 ABI remains explicit; this is reference-platform staging, not universal self-contained Linux/Windows/macOS distribution.

**AC2 PARTIAL:** technical relative/offline bundle works, but external redistribution remains unapproved: complete license/copyright inventory for copied system libraries and corresponding-source obligations for included Qt/FFmpeg remain unfinished. [OpenCV third-party notices](https://github.com/opencv/opencv-python/blob/4.x/LICENSE-3RD-PARTY.txt) and actual pinned-wheel notices are retained, not treated as proof of complete fulfillment. No externally shipped binary or claimed legal-complete distribution. Windows/macOS NOT VALIDATED; clean-PC/T020 NOT RUN.

## Unity defaults and privacy

`CameraRuntimePaths.Resolve(productDirectory)` defaults to relative Vision/python, worker.py/model/fixture; physical mode. Absolute development overrides require explicit GORILLA_CAMERA_LAB=1; replay/fault fixtures preserved. GORILLA_CAMERA_ENABLED=1 opts into the packaged path; does not start Java. CameraFusionLabRunner now maintains a CameraInputStore independently of mobile availability; same raw contract/association/fusion semantics. Missing runtime/worker/model/manifest fails explicitly; no replay/download fallback. Manifest/hash rejection occurs in the launcher.

Physical per-frame Camera/Fusion logs are suppressed; private landmark pipe remains memory-only and Unity exposes the bounded store. Summary/cleanup expose counts/timing/source only. Static audit checks no imwrite/VideoWriter recording/save/dump/face-recognition/embedding APIs; VIDEO mode means detector timestamp API, not recording. Native runtime audit uses temporary strace open/create/connect events, retains aggregate file-write counts only and deletes its trace. No screenshot of webcam content.

EOF stops capture; capture thread joins, owns camera.release; detector closes on the main thread, including error paths. Pending slot is discarded, capture errors remain explicit even during last inference. Native read may still hang; join2s reports CAPTURE_JOIN_TIMEOUT, Unity's existing3s normal/5s fallback applies only to its own child. No global hang/leak guarantee.

## Reproduce and acceptance

Component tests: `python3 -m unittest discover -s tools/t016 -p 'test_*.py' -v`. Bundle: `python3 tools/t016/bundle.py --venv <prepared-pinned-venv> --model <approved-task> --output <new-build>/Vision`. Never overwrite evidence/staging. Verify: `python3 tools/t016/bundle.py --verify <new-build>/Vision`.

Benchmark: `python3 tools/t016/benchmark.py --python <build>/Vision/python --worker <build>/Vision/worker.py --model <build>/Vision/pose_landmarker_lite-v1.task --output <new-aggregate.json>`. Default3runs. Benchmarks retain only aggregates, counts, ordered-output/cleanup gates. Offline Player: `python3 tools/t016/offline_player.py --build <new-linux-build> --output <new-aggregate.json>`; temporary network namespace, developer cache hidden, isolated HOME. No system network changes or personal cache deletion.

Unity6000.3.23f1: isolated Assets/Packages/ProjectSettings/Shared copy; EditMode/PlayMode serially, then FoundationSetup.BuildDevelopmentLinux. PlayMode uses existing Java/RTC/camera fixtures plus GORILLA_TEST_CAMERA_BUNDLE pointing to parent of Vision. Repeat phone/lobby/action regressions; Java/PWA/GorillaProtocol/T015 unchanged. AC1/AC2 original boxes remain unchecked; AC3 only marked after final audits. T017/T018 remain blocked by openT016; F04/E0 remain OPEN. No Phase1.

## Final verification

EditMode99/99, PlayMode19/19 (new physical packaged child→Unity store), component13/13; Linux development build0errors/0warnings. Actual built Player launched from relocated distribution with developer cache hidden, isolated HOME and network namespace: WAN probe failed as expected; model loaded/camera opened/692 physical CameraInputs accepted; Player/vision exit0, normal cleanup, capture thread joined/camera released/detector closed, own residual0. Native privacy audit successful image/video writes0, physical per-frame log records0. AC3 PASS.

[Sanitized summary/source hashes](evidence/t016-camera-runtime-2026-10-09/summary.json), [offline Player](evidence/t016-camera-runtime-2026-10-09/offline-player-final.json), [runtime file hashes/native inventory](evidence/t016-camera-runtime-2026-10-09/runtime-manifest.json). Final Player metrics19.942035FPS capture/19.956071FPS inference/19.956072FPS output are an additional startup-inclusive audit, not a warmup-qualified acceptance replacement. One overwrite,693 output/692 accepted; final pipe slot may be discarded at shutdown. Capture hardware drops remain UNKNOWN.

Initial Player attemptFAIL preserved; independent camera path initially stalled while app unfocused without Java supervisor. Runner now temporarily enables runInBackground, restores it on destruction and processes CameraInput independently; final isolated Player proves normal cleanup. No unrelated processes killed or personal caches deleted. Scope of privacy audit is application/native open/create/rename/connect behavior and aggregate logs, not a universal audit of third-party internals.

T016 remains OPEN/In Progress with needs-device-test/performance. AC1 FAIL; AC2 PARTIAL; no PR/push/merge because required acceptance conditions fail. T017/T018 remain OPEN/blocked by T016; F04/E0 OPEN, Phase0 IN PROGRESS, Phase1 NOT STARTED. Next gap: real acquisition>=30 without reducing640×480/two poses or changing criterion; complete external runtime redistribution notices/corresponding source.

## Aggregate by configuration

Weighted rates = sum(N−1)/sum(first-to-last monotonic durations), retaining each run independently. No pooling of different exposure configurations. [All aggregate values](evidence/t016-camera-runtime-2026-10-09/aggregates.json).

- originalControls: capture19.965559866893 / inference19.954674533740 / output19.954677795279 FPS; runs3, overwrites2.
- dynamicFramerateOff: capture19.758632723809 / inference19.752928156158 / output19.752927288280 FPS; runs3, overwrites0.
- manualExposureDiagnostic: capture21.982174020774 / inference21.978099350689 / output21.978099807088 FPS; runs3, overwrites0.
- finalDefaultControls: capture19.940556524680 / inference19.953212790174 / output19.953216500547 FPS; runs1, overwrites0.
