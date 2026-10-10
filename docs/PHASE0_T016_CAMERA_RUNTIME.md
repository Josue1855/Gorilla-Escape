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

## Product profile validation — latest local continuation

2026-10-09 America/Chihuahua. This section supersedes earlier candidate measurements without deleting their history. PO authorized evaluation only; DEC-025 remains **LOCAL CANDIDATE / VALIDATION IN PROGRESS**. Original remains the default. The candidate is opt-in and session-owned; no global persistent settings, push, PR or issue cascade.

### New controlled comparison

Five seconds warmup plus 61 seconds requested measurement per paired run, actual first-to-last capture >60 seconds. Original: auto3, exposure156 inactive, priority1, buffer1. Candidate: manual1, exposure100 V4L2 units (10ms), priority preserved1, buffer2. Same MJPG640×480/30, num_poses=2, one capture thread/latest-slot(1)/inference. Same ambient scene, no human participation.

|Run|Capture FPS (literal)|Inference FPS|Output FPS|Age P50/P95/max ms|Age N|Overwritten / stale skipped|
|---|---:|---:|---:|---|---:|---|
|paired-candidate-1.json|30.224215376583782|30.191347599050747|30.19134794247807|33.531436 / 37.995365 / 61.578192|1843|1 / 0|
|paired-candidate-2.json|30.225402700303786|30.18693694690809|30.18693710390082|33.434668 / 36.346701 / 61.723291|1841|3 / 0|
|paired-candidate-3.json|30.225225678964854|30.22342847330064|30.223430877131435|33.497143 / 36.617483 / 65.139837|1844|0 / 0|
|paired-original-1.json|19.95006159447478|19.906832216364982|19.906832081224344|34.622154 / 39.750378 / 66.809819|1214|3 / 0|
|paired-original-2.json|19.966789070751005|19.967356186098513|19.967357016877447|37.176786 / 39.449995 / 73.181687|1218|0 / 0|
|paired-original-3.json|19.964108287928628|19.898428201812244|19.89842802079838|35.996065 / 50.430139 / 80.109317|1214|4 / 0|

Each candidate capture run passes >=30.000 individually. No averaging of failures. Earlier explicitly configured paired runs have source revisions recorded individually; candidate3 uses final worker/pipeline/control source. Empty-scene throughput does **not** prove human-pose inference performance. Measurement/lifetime captured-consumed counts, capture/inference P50/P95/max/N, delivery, handover, frame age and output latency remain in [paired comparison](evidence/t016-camera-runtime-2026-10-09/profile/paired-comparison.json).

### Buffer / priority matrix

Diagnostic matrix uses 5s warmup+35s measurement; selection is provisional, paired runs validate longer duration.

|Manual10ms|Priority|Capture FPS|Inference FPS|Frame age P95 ms|
|---|---:|---:|---:|---:|
|buffers=1|1|21.859596381592723|21.862612409336645|36.585236|
|buffers=2|1|30.224011234295514|30.220775221846115|36.329266|
|buffers=3|1|30.223288801289755|30.196658159718552|37.077287|
|buffers=2|0|30.22397378899492|30.001488244559436|41.238849|

Minimum measured passing buffers = **2**; priority0 is unnecessary here. This does not make the profile accepted or universal. Capture buffers belong to the handle and are released on exit.

### Quality and ownership

User explicitly cannot participate or change lighting. **BRIGHT, NORMAL and LOW-LIGHT human quality: NOT RUN.** Both baseline/candidate have zero subjects, zero visible landmarks/confidence and constant mean pixel value4.691848958333333/255. This scene is unusable for evaluating pose quality; its physical cause is unproven. Do not attribute darkness to10ms, claim stable tracking, or invent motion/blur results. No adaptive exposure policy is justified from this evidence. AC1 remains **FAIL acceptance / quality unproven**, despite three throughput passes.

Linux standard V4L2 capability/range/menu checks guard opt-in manual10ms. No hardware-name allowlist; unsupported profiles fail explicitly and restore partial application. Snapshot precedes mutation and is sent over private stdout to the owning supervisor. Worker restores after capture release on EOF/signals/failure; parent restores saved controls/full format/frame interval after owned-child termination. Parent crash/OS failure itself is not proven recoverable. PlayMode verifies forced-child recovery, unit tests verify partial startup rollback. No user v4l2-ctl setup. The original auto configuration remains default; stale frames >100ms are explicitly skipped (provisional budget, not a validated gameplay threshold).

Timing semantics: V4L2 CAP_PROP_POS_MSEC derives kernel buffer timestamp, checked for monotonicity/range; this is an acquisition proxy, not optical ground truth. Delivery = kernel timestamp→read complete; handover = read complete→inference start; inference = conversion/detection/adapter processing; read→output includes handover/processing/serialization. Unity transit = processTimestamp→pipe receive on same-PC UTC, millisecond resolution, not optical/gameplay latency. No cross-clock optical correlation or T018 acceptance claimed. Bounded4096 sample windows retain lifetimeN/extremes; percentile window semantics documented by retained count.

### Packaging and redistribution

Headless4.13.0.92 evaluation: import/MediaPipe/V4L2 PASS, GUI NONE, FFmpeg YES; physical capture30.22491833686343/inference30.22523864434588/output30.22523922232573. **Not adopted:** full physical quality/adoption/license gates unresolved. Original locked18-package environment is preserved. Controlled Qt removal breaks loader; isolated FFmpeg-only removal breaks libavcodec load. First broad removal also matchedAVIF and is explicitly NOT ISOLATED; corrected experiment preservesAVIF. Original binaries retained.

Fresh final staging `/tmp/gorilla-t016-profile-final` is separate from all earlier staging. Vision inventory2881files/193ELF/57components,525976819manifested bytes; sources increase size over456MB payload. Authentic model Apache2 notice/text and Flatbuffers license included. Exact FFmpeg8.0.1 and QtBase5.15.18 upstream tarballs provided in SOURCES/upstream with URLs/SHA; FFmpeg exported license/config recorded, no observed GPL/nonfree flags. **PARTIAL:** wheel-specific patches/build correspondence, applicable LGPL replacement/relinking materials and copied GPL/LGPL distro sources remain unverified. Concrete required materials and delivery directory documented; no unfulfilled source offer asserted. Sources/binaries stay in local staging, no repository binary upload.

Model exact5777746-byte hash59929e1d1ee95287735ddd833b19cf4ac46d29bc7afddbbf6753c459690d574a, official family model-card Apache2 and creator attribution are retained in authentic notice; no additional model publisher NOTICE found. Player inventory separates221actual files/9ELF from Vision and host. Mono runtime export identifies6.13.0(explicit/9d7c9afd); matching Player/Mono/libdecor provenance and applicable notices remain OPEN. Editor/tool notices are not substituted. Runtime maps still show dynamic host audio/graphics dependencies beyond the copied ldd closure; Vision native closure is not falsely declared complete. **AC2 PARTIAL**. Size alone does not block.

### Verification and delivery boundary

Components19/19, UnityEdit99/99, UnityPlay20/20, LinuxBuildSucceeded errors0/warnings0, Management/Foundation validatorsPASS. PlayMode includes CameraInputStore/player-lock/fusion/T015 regressions and packaged physical cleanup plus forced-child restoration. CI remote checks NOT RUN (no push/PR). [Validation aggregate](evidence/t016-camera-runtime-2026-10-09/profile/validation.json). Integrated final physical/offline/privacy results are appended below after completion.

T016/#29 OPEN/InProgress, T017/#30 OPEN/Validation+blocked, T018/#32 OPEN/Validation+blocked, F04/#28 OPEN/InProgress, E0/#1 OPEN/InProgress; Phase0 INPROGRESS, E1/#2Backlog/Phase1NOTSTARTED. No acceptance/DEC adoption/cascade without all ACs.


Fresh final integrated Player (5s warmup+61s): capture1845frames/61.010225535s=**30.224441621546614FPS**, inference1823frames=29.854089942381933FPS, output1823frames=29.85409429745793FPS. Lifetimecaptured1989/consumed1964/emitted1964; Unityaccepted1963 (shutdown boundary), worker-overwrites25, parent-overwrites0, staleframesSkipped0, pendingdiscard0, timestampUnavailable0. Frame ageP50/P95/max33.355845/46.324587/66.864654ms N1822; delivery32.833892/33.981744/64.359287 N1845; handover0.297707/13.589672/36.102638 N1822; processing25.147072/40.748903/99.31088 N1823; read→output27.809445/47.529048/110.6894 N1823. UTCprocess→UnityreceiveP50/P95/max7/15/32ms N1964 invalid0. Independent boundary timestamps explain differing metricN. No sum of percentiles or end-to-end optical claim.

[Physical offline aggregate](evidence/t016-camera-runtime-2026-10-09/profile/physical-offline-final.json): technical gatePASS, actualcapture thresholdPASS, numposes2/MJPG640×480; WANprobeunreachable101, developer .cachehidden, HOMEisolated, relativeVisionruntime/localmodel loaded. Playerexit0, ownworkerexit0, noforcedshutdown, capture/detectorreleased/threadjoined, parentcontrolrestorePASS, noresidualworker/helper. Image/video writes0; physical per-frame logs0; onlyaggregates retained. **AC3 PASS** for static source plus this physical Linux runtime audit. Human quality remains NOTRUN; AC1 is not accepted. T020clean-PC/platform testing remains independent.

Final Visiontotal526652611bytes (including675792-byte manifest), wholePlayer+Vision683362308bytes/3103files. Visionmanifested525976819bytes/2881files,193ELF,57componentnotices; allhashes verified after run. Runtime-manifest SHA3f2fcf9f49d26dd3ef701962809074c77566ce638bac86cea91464d8e121f07f. [Final staging aggregate](evidence/t016-camera-runtime-2026-10-09/profile/staging-final.json), [final source obligations](evidence/t016-camera-runtime-2026-10-09/profile/final-source-obligations.json), [actual Player inventory](evidence/t016-camera-runtime-2026-10-09/profile/player-files-final.json). Final camera settings equal first measured matrix settings: auto3/exposure156/priority1/MJPG640×480/1÷30; each session also verifies its own full saved format/parameters readback. [Restoration aggregate](evidence/t016-camera-runtime-2026-10-09/profile/final-camera-restoration.json). No settings are left applied outside Gorilla-owned sessions.

Current source deliberately retains authorized candidate defaultbuffers3 when no buffer argument is supplied; the selected minimal evaluated profile is explicitly `--profile candidate --buffers 2` / corresponding environment values. Original remains default. No candidate profile is accepted for normal product use.

Latest remote read confirms T016InProgress; T017/T018Validation with blocked labels; F04/E0InProgress; E1Backlog; noT016PR. [Remote state](evidence/t016-camera-runtime-2026-10-09/profile/remote-state-final.json). Remaining real gaps: participating movinghuman comparison inBRIGHT/NORMAL/LOW-LIGHT, quality-loaded inference/latency acceptance, matching wheel/distro corresponding-source and exact Player notices/provenance, dynamic Vision native closure. No dependency cascade or publication.

## BRIGHT human validation — 2026-10-09

User prepared BRIGHT and a person; executed only the requested human validation using existing candidate manual10ms/priorityoriginal1/buffers2/MJPG640×480/numposes2. Added aggregate-only presence/ambiguity/ephemeral ID-set-change counters, no model/pipeline/control/threshold changes. Warmup5s, measured45.135454782s (capture first-to-last45.031968715s). Capture30.22297356381524PASS>=30.000; inference30.119919887187127/output30.11992251455186FPS. Withsubject0/without1358/detectionratio0; tracking0/gaps1358; confidenceP05/P50/P950/0/0 and visiblelandmarks0/0/0; ambiguous0/subjectIDchanges0/producteligible0. Zero ambiguity/ID changes cannot establish stability with no detections. BRIGHT functional pose/tracking FAIL/not demonstrated. Constantbrightness4.691848958333333/255 matches earlier dark-scene aggregate; physicalcauseunproven and no attribution to10ms. No additional diagnosis or baseline rerun. Individual requested movements/exit/return cannot be verified from zero-detection aggregates.

FrameageP50/P9533.162552/40.953695ms, read→output24.549858/40.759737ms (N1358); process→Unity7/16ms N1500 across entire session includingwarmup. Worker lifetimeoverwrites6/staleskips0/Unityoverwrites0. Unityaccepted1499; player/workerexit0, noforcedshutdown, capture/detectorreleased/threadjoined, controlsrestoredPASS and finaltypedfullsnapshot identicaltoinitial (including before redundant final restore/readback). Noresidualworker, image/video writes0, no physical per-frame logs; aggregates only. [BRIGHT evidence](evidence/t016-camera-runtime-2026-10-09/bright-human/physical-bright.json), [snapshot verification](evidence/t016-camera-runtime-2026-10-09/bright-human/final-snapshot.json). Existing product thresholds unchanged. AC1 pending NORMAL/LOW-LIGHT and functional quality; DEC025candidate. No push/PR/acceptance; stopped afterBRIGHT.

## BRIGHT retry2 after user reframing — 2026-10-09

User requested another BRIGHT run after discussing wider arm framing. Same explicitcandidate/MJPG640×480/manual10ms/priority1/buffers2/numposes2; no throughput/backend/parameter investigation. Warmup5s; measured45.164637829s, capturewindow45.032781193s. Capture30.22242828323375PASS>=30.000, inference30.10811240638611/output30.108110379624087FPS. Subjectframes0/absent1358/ratio0; tracking0/gaps1358; confidence andvisiblelandmarksP05/P50/P950/0/0; ambiguous0/IDchanges0, producteligible0. FrameageP50/P9533.227591/42.585739ms N1357, read→output25.654315/42.369898ms N1358, process→Unity7/15ms N1500 includingwarmup. Worker lifetimeoverwrites6/parentoverwrites1/skips0. Meanpixelvalueagainconstant4.691848958333333/255: reframing did not resolve zero detections; exactcauseunproven and no attribution to10ms or user absence. BRIGHT functionalcriterionFAIL. Player/worker normalexit0, finalsnapshotidenticaltoinitial (1920×1080/MJPG30, auto3/exposure156/priority1 restored), noresidualvision/imagewrites/perframelogs. Original snapshots and firstBRIGHT preserved. [Second BRIGHT evidence](evidence/t016-camera-runtime-2026-10-09/bright-human-retry-2/physical-bright.json). NoNORMAL/LOW-LIGHT run, push/PR/AC1PASS/DECacceptance. Stopped afterBRIGHT.

## BRIGHT retry3 — functional human poses observed — 2026-10-09

User authorized another BRIGHT run. Same explicit profile unchanged (manual10ms/priority1/buffers2/MJPG640×480/numposes2), warmup5s/measured45.178347797s. Capture30.225933928018822PASS>=30.000; inference22.442031127976787/output22.44203175118115FPS with real detected poses. Capture first-to-last45.02755823s N1362, inference/outputN1012. Withsubject1008/without4/detectionratio.9960474308300395; tracking1008/gaps4; existing product eligible794/1012 (.7845849802371542), not a new acceptance threshold. ConfidenceP05/P50/P95 .067171111702919/.990685760974884/.9997640252113342; visiblelandmarks4/8/10 (sum across up to2subjects, not per-person); ambiguous3/ID-setchanges8. Maxsubjects2 observed; cannot claim stability, recognize individual gestures, assign a physical cause to ID changes or establish comparative exposure degradation from aggregates. SpatialIDs are ephemeral, not biometric. Pose functionality observed, not complete AC1 acceptance.

FrameageP50/P9549.785872/65.255785ms N1011; read→output59.726447/84.185577ms N1012; process→Unity11/33ms N1118 includingwarmup. Worker lifetimeoverwrites388/pendingdiscard1/staleskips0; parentoverwrites1, Unityaccepted1116. Confidence low tail and processingcost recorded, not hidden by high detectionratio. MeanpixelP50 now28.924440104166667/255 (previous constant4.69); cause of earlier dark feed undetermined, no further camera investigation. Normalplayer/workerexit0, camera/detectorrelease/threadjoinPASS, noresidualvision/imagewrites/perframelogs.

External preflight snapshot read succeeded but its close attempted S_FMT while camera was busy (EBUSY), so initial external full-byte snapshot was not persisted. Run worker independently took/sent its own full snapshot before applying candidate; normal worker restoration and parent saved-snapshot readback bothPASS. Final independent read matches initial worker-reported controls/format/interval (1920×1080/MJPG30/auto3/exposure156/priority1). [Restoration basis and final snapshot](evidence/t016-camera-runtime-2026-10-09/bright-human-retry-3/final-snapshot.json). No fabricated independent full-byte before/after comparison. [Complete BRIGHT aggregates](evidence/t016-camera-runtime-2026-10-09/bright-human-retry-3/physical-bright.json). Prior failed runs preserved. No NORMAL/LOW-LIGHT, baseline/buffer/V4L2 investigation, push/PR/AC1PASS/DECacceptance. Stopped afterBRIGHT.
