# T020 — minimal build on clean physical PC

2026-10-13. #34 OPEN; mandatory dependency #33 OPEN. #10/#17 satisfied. Package candidate prepared independently; no clean-PC execution until T019 Done and physical PC available.

## Candidate

Linux x86_64/reference glibc2.39; Unity6000.3.23f1 Mono development Player, relative Vision/Python/MediaPipe lite model, local Server JAR/PWA assets, stock jlink OpenJDK21.0.12.1 runtime (no javac/jmods/Editor), exact supplier JDK source/distro patches and embedded dependency notices. No runtime download. `run-base.sh` uses only relative bundled executables and verifies SHA256 inventory before launch. This diagnostic base is not a playable tournament or Academic MVP fallback. Windows/macOS NOT VALIDATED; compatible Linux graphics/OpenGL/X11/V4L2 and host ABI remain required. Server source/license obligations are indexed from actual embedded metadata; no legal certification claimed by packaging.

Reproduce with `python3 tools/t020/package.py --vision <prepared-Vision> --player <current-Player-directory> --materials <accepted-T016-materials> --jar <current-Server.jar> --jdk <installed-supplier-JDK21> --jdk-sources <apt-source-download-directory> --output <new-directory>`. Obtain exact supplier archives with apt-get source --download-only openjdk-21=21.0.12.1+1-1~24.04.4; script verifies dsc SHA256/files and preserves notices. Preparation needs SDK; recipient execution uses only bundled runtime. `--resume-prepared` is only for completing an existing finalize_distribution output without overwriting runtimes.

## Physical acceptance steps

1. After T019 closes, provide a separate physical Linux PC with compatible ABI/graphics/webcam. No Unity Editor, SDK, Java/Python install or Gorilla development caches. Record actual hardware, OS, graphics and device capabilities; do not label the development PC/cache isolation/VM a clean physical PC.
2. Copy the complete directory, including manifests/licenses/source notices. Check runtime-manifest SHA256 against release candidate receipt. Run bundled `Vision/python -I verify-package.py .`; expected PACKAGE_INTEGRITY_PASS.
3. Disconnect WAN for this recipient while preserving required local LAN. Run ./run-base.sh, confirm bootstrap/window/backend RUNNING and physical camera startup on supported capabilities. Verify local resources and no runtime download. Capability failure is FAIL/unsupported observation, not universal support.
4. Close window normally; confirm own Unity/Java/Vision gone, camera released/controls restored. Inspect sanitized errors; test recovery with own processes only. Never kill unrelated processes. Record duration/N/result and limitations; no video/photos/sensor histories.
5. Record exact AC1 (works without SDK/Editor), AC2 (local resources/closure/errors), AC3 (technical foundation viability distinct from later playable slice); preserve failed attempts. PlanB needs evidence/PO decision. Publish evidence by normal PR/checks/merge, then close #34 only if real acceptance passes.

Current classification: package preparation and developer-host WAN/cache isolation = LAB. Physical clean-PC AC1/AC2 **NOT RUN**. AC3 documentary distinction prepared, does not substitute missing physical execution.

Repeat lifecycle smoke with `python3 tools/t020/startup.py --player <player> --java <bundled-java> --jar <bundled-jar> --output <new-report.json> --repetitions 20`. Optional --soak-seconds1801 holds the first IPC lifecycle for at least30minutes; all evidence is LAB, camera/Vision NOT RUN in that specific harness. Read the separate combined preflight/camera soak reports instead of inferring camera survival from IPC-only runs.
