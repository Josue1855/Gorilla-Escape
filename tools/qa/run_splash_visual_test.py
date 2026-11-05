import os
import subprocess
import time
import sys
from pathlib import Path

repo_root = Path(r"C:\Users\equipo\Documents\Repos\Gorilla Escape")
exe_path = repo_root / "Builds" / "Windows" / "GorillaEscape.exe"
screenshot_dir = repo_root / "Reports" / "QA" / "P0_SPLASH_ASSETS" / "screenshots"

screenshot_dir.mkdir(parents=True, exist_ok=True)

print(f"Checking executable at {exe_path}...")
if not exe_path.exists():
    print(f"Error: {exe_path} does not exist yet!")
    sys.exit(1)

mtime = os.path.getmtime(exe_path)
print(f"Executable timestamp: {time.ctime(mtime)}")

env = os.environ.copy()
env["GORILLA_AUTO_FLOW"] = "1"
env["GORILLA_AUTO_SCREENSHOT_DIR"] = str(screenshot_dir)
env["GORILLA_AUTO_QUIT"] = "1"

print(f"Launching GorillaEscape.exe with GORILLA_AUTO_FLOW=1...")
start_time = time.time()
proc = subprocess.Popen([str(exe_path), "-screen-fullscreen", "0", "-screen-width", "1280", "-screen-height", "720"], env=env)

# Wait up to 35 seconds for the automated flow
try:
    proc.wait(timeout=35)
    print(f"Game exited with code {proc.returncode} in {time.time() - start_time:.1f}s")
except subprocess.TimeoutExpired:
    print("Game timed out during auto flow, terminating...")
    proc.kill()

# Check generated screenshots
expected = [
    "evidence_01_splash_first_frame.png",
    "evidence_02_gorilla_running.png",
    "evidence_03_menu_3d_signs.png",
    "evidence_04_sign_focus.png",
    "evidence_05_party_modal.png",
    "evidence_06_comic_panel_1.png",
    "evidence_07_comic_panel_final.png",
    "evidence_08_personal_preparation.png"
]

print("\n--- SCREENSHOT VERIFICATION ---")
missing = []
for name in expected:
    p = screenshot_dir / name
    if p.exists() and p.stat().st_size > 0:
        print(f" [PASS] {name} ({p.stat().st_size / 1024:.1f} KB)")
    else:
        print(f" [FAIL] {name} MISSING or EMPTY")
        missing.append(name)

if missing:
    print(f"\nMissing {len(missing)} screenshots: {missing}")
    sys.exit(1)
else:
    print("\nAll 8 evidence screenshots successfully captured!")
    sys.exit(0)
