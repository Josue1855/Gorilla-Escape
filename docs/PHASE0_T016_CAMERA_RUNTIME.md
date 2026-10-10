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

## Layered Capture Diagnosis

2026-10-09 America/Chihuahua; continuation of local commits `f4f110d` and `768ec10`, no reset/rebase or replacement of prior evidence. Original candidate remains unchanged. Thirteen additional isolated physical trials:5s warmup + at least35s first-to-last measured interval. All temporary controls/mode/FPS restored after **every** trial. No lighting comparison: PO cannot change lighting now; current ambient illuminance unmeasured.

Raw path is `v4l2-ctl --verbose --stream-mmap=N --stream-to=/dev/null`. Python supervises dequeue **text**, not pixel bytes; aggregate FPS `(frames−1)/(last−first)` uses V4L2 monotonic SOE timestamps, cross-checked against monotonic text-receipt elapsed time. No OpenCV, Python image processing or MediaPipe in the raw byte path. Kernel timestamps have microsecond precision; no inference from driver-reported30.

| Layer | Mode | FPS | P50 interval ms | P95 interval ms | Result |
|---|---|---:|---:|---:|---|
|A V4L2 MMAP|MJPG 640×480 @30; buffers3; auto3/exposure156/priority1|19.966963958|51.871000|52.067000|<30|
|B OpenCV minimal|MJPG 640×480 @30; buffers1; auto3/exposure156/priority1|19.967754949|50.856279|53.074693|<30|
|C Gorilla CapturePump|MJPG 640×480 @30; buffers1; auto3/exposure156/priority1|19.967252111|50.822751|52.769884|<30|
|A V4L2 MMAP|MJPG 640×480 @30; buffers3; auto3/exposure156/priority0|29.836819721|32.041000|36.042000|<30|
|B OpenCV minimal|MJPG 640×480 @30; buffers1; auto3/exposure156/priority0|19.760770055|62.761349|68.266411|<30|
|A V4L2 MMAP|MJPG 1280×720 @30; buffers3; auto3/exposure156/priority1|19.964706740|51.864000|52.068000|<30|
|A V4L2 MMAP|MJPG 1920×1080 @30; buffers3; auto3/exposure156/priority1|19.964727268|51.868000|52.079000|<30|
|A V4L2 MMAP|MJPG 640×480 @30; buffers3; auto1/exposure100/priority0|30.225314867|32.033000|36.013000|>=30 diagnostic only|
|B OpenCV minimal|MJPG 640×480 @30; buffers1; auto1/exposure100/priority0|21.965685098|36.097071|64.452903|<30|
|A V4L2 MMAP|MJPG 640×480 @30; buffers1; auto3/exposure156/priority0|19.874543656|67.834000|68.076000|<30|
|B OpenCV minimal|MJPG 640×480 @30; buffers3; auto3/exposure156/priority0|29.840090869|32.484208|36.685271|<30|
|A V4L2 MMAP|MJPG 640×480 @30; buffers1; auto1/exposure100/priority0|22.006096288|35.947000|67.996000|<30|
|B OpenCV minimal|MJPG 640×480 @30; buffers3; auto1/exposure100/priority0|30.225872048|32.246183|36.474782|>=30 diagnostic only|
|D unchanged Gorilla capture + MediaPipe (prior final source)|MJPG640×480 @30, buffer1, original controls|capture19.940556525; inference19.953212790|Not previously instrumented|Not previously instrumented|Historical final candidate; AC1 FAIL|

For D, historical capture **read duration**, distinct from inter-capture interval: P50/P95 50.174531/53.547268ms; inference duration31.092922/52.245931ms. No fabricated interval percentiles and no rerun/tuning of MediaPipe. Current minimal baseline read P50/P95 50.839656/53.039687ms.

### Controls before, temporary tests and exact restore

Initial/after every test:MJPG640×480, requested/reported30, `auto_exposure=3`, `exposure_time_absolute=156` (inactive under auto), `exposure_dynamic_framerate=1`. Control IDs `0x009a0901/02/03` match installed kernel definitions EXPOSURE_AUTO/ABSOLUTE/AUTO_PRIORITY; tool aliases are the actual exposed names, not guessed names. Temporary priority0; temporary manual1/exposure100 (100µs units →10ms, below33.33ms frame interval), priority0. Not adopted as product/global presets. OpenCV v2 diagnostic reads controls back after actual capture; manual100/priority0 remain applied. Mode1280×720 and1920×1080 returned as requested and then restored to640×480.

Kernel [camera control documentation](https://www.kernel.org/doc/html/latest/userspace-api/media/v4l/ext-ctrls-camera.html) defines dynamic cadence under auto-priority and the100µs exposure units. Stored156 in automatic mode is an inactive control value, **not a measurement of actual auto exposure**. Manual100 is a causal experiment, not image-quality acceptance.

### Interpretation and limits

The first ~20FPS is already present in A under original controls:19.966964FPS. B19.967755 and C19.967252 introduce no material additional baseline loss; MediaPipe does not cause that original cadence. Disabling auto-priority with MMAP3 changes A to29.836820, still below literal30. Manual10ms plus MMAP3 produces30.225315; independent OpenCV3/manual10ms produces30.225872. Thus this hardware can deliver>=30 in a temporary diagnostic mode; a blanket claim that it cannot is unsupported.

A second controlled limit appears with **one V4L2 buffer**: priority0/MMAP1 delivers19.874544 vs MMAP3 29.836820; manual10ms/MMAP1 delivers22.006096 vs MMAP3 30.225315. OpenCV1 vs3 follows the same pattern. Driver dequeue sequence gaps349/288 in those MMAP1 trials are counted observations, not an invented sensor/USB hardware-drop total. Raw3 trials have no observed sequence gaps. Changing buffers preserves the bounded latest slot in principle, but no product change or latency/quality claim is made here.

Classification: **exposure/dynamic-cadence controls and V4L2 buffer count have measured causal effects**. Underlying sensor/firmware/UVC mechanism remains **ROOT CAUSE NOT PROVEN / UNKNOWN BELOW V4L2 USERSPACE**. USB bus3/port6,480M,uvcvideo observed; no evidence establishes USB transport defect. MJPG720p and1080p default-control raw both ~19.9647, so the baseline limitation is not specific to640×480. YUYV not selected as a remedy; optional YUYV/compliance tests not run. No physical lighting or human quality comparison available.

WirePlumber PID3947 held a device fd before diagnostics; MMAP STREAMON succeeded and the raw baseline completed without stopping that unrelated service. No competing streaming owner observed; no foreign process killed.

### Actual redistribution inventory

Fresh augmented local staging retains the original candidate manifest and payload unchanged. New independent runtime-manifest hashes2868 files, including128 authentic license/notice texts in LICENSES and57 component notices in THIRD_PARTY_NOTICES.18 wheel inventories come from **actual retained RECORD files**, omissions are explicit;35 copied system libraries have dpkg source-package/version provenance, matching host hashes and authentic copyright/common-license texts. Qt and FFmpeg are also separately identified native subcomponents, not inferred solely from package names. FlatBuffers25.12.19 wheel lacks bundled license text; added [authentic tagged upstream LICENSE](https://raw.githubusercontent.com/google/flatbuffers/v25.12.19/LICENSE), recorded origin/hash.

193 actual ELF files in Vision include the prior158 plus35 copied system libraries. DT_NEEDED is recorded for each. Import+PoseLandmarker creation probe (no webcam/inference) observed80 bundled native files loaded,113 not loaded in that probe, and141 system-provided mapped files. Unloaded in this probe does **not** establish removable. Host-loaded PortAudio/ALSA plugins and distro FFmpeg60 are separate from the wheel FFmpeg62. These dynamic host paths were missed by ldd-only closure: reference-PC offline operation still passed historically, while clean-host portability/native closure remains incomplete.

Qt5.15.18 Core/Gui/Widgets/Test and FFmpeg avcodec62.11.100/avformat62.3.100/avutil60.8.100/swscale9/avdevice62 are bundled, direct DT_NEEDED of cv2, and runtime-loaded. Current binary cannot be safely pruned of them. No headless wheel/dependency swap performed. Authentic OpenCV notices identify Qt LGPL3 and FFmpeg LGPL2.1; exact build-option/source correspondence is still pending. [FFmpeg legal guidance](https://ffmpeg.org/legal.html) reinforces source/build-configuration verification; copied notice text alone does not fulfill source obligations.

Source fulfillment remains OPEN: matching Qt/FFmpeg source revisions, wheel modifications/build recipes, applicable library replacement/relinking materials; matching copyleft-covered distro source versions/patches and license-compliant delivery mechanism. Planned delivery is verified matching sources/build materials alongside eventual binaries; no source archive/offer has been assembled or falsely claimed. Per-file exceptions in full copyright files control applicability. Model family Apache2 remains historical evidence; artifact-specific notice/provenance and NOTICE applicability are unverified.

A supplemental whole-Player inventory covers221 files outside Vision, including9 ELF/debug artifacts. Unity6000.3.23f1/Mono and Unity-supplied libdecor/Cairo plugin provenance/license/source notices are explicitly unresolved; unrelated editor-tool notices were not copied as player proof. No final-distribution-wide legal compliance claim. **AC2 PARTIAL** despite improved inventory.

[All physical aggregate trials and sanitized snapshots](evidence/t016-camera-runtime-2026-10-09/layered/summary.json), [actual redistribution ledger](evidence/t016-camera-runtime-2026-10-09/layered/redistribution.json), [new exact file hashes](evidence/t016-camera-runtime-2026-10-09/layered/runtime-manifest.json), [whole Player inventory](evidence/t016-camera-runtime-2026-10-09/layered/player-files.json), [explicit source gaps](evidence/t016-camera-runtime-2026-10-09/layered/source-obligations.json). Original manifests/results above are preserved.

Reproduce with tools/t016/layered_capture.py --layer raw|minimal|thread --output <new-path>, optional --priority0 (syntax `--priority 0`), --manual-exposure100 (syntax `--manual-exposure 100`), --buffers1|3 (syntax `--buffers 3`). Use the prepared pinned Python for OpenCV. Every trial restores its own initial snapshot; never run camera diagnostics concurrently. runtime_library_probe.py --root <Vision> emits loaded paths only; redistribution.py --root <fresh-Vision> --loaded <probe.json> adds authentic notices/inventory, not a source-compliance certification. player_inventory.py audits the preserved actual build.

AC1 FAIL for unchanged product defaults; AC2 PARTIAL; AC3 PASS retained, all new evidence aggregate/text/file-inventory only.13 component tests pass again; original99 EditMode/19 PlayMode/build0errors evidence remains applicable to unchanged product sources, no unnecessary Unity/Java/PWA rerun. No push/PR/merge/closure. T016 OPEN/In Progress; T017/T018 OPEN/blocked; F04/E0 OPEN/In Progress; Phase0 IN PROGRESS; Phase1 NOT STARTED. PO next decision: authorize a separate controlled product-quality/latency validation of manual10ms+buffers3 at unchanged>=30 threshold, or select different reference hardware / explicitly revise requirement / commission further driver-firmware investigation. No option automatically chosen. Source/native-closure gaps must also close before acceptance.
