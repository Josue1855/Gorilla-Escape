import subprocess
import sys
import time
import os

unity_exe = r"C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe"
project_path = r"C:\Users\equipo\Documents\Repos\Gorilla Escape\Unity"
log_path = r"C:\Users\equipo\Documents\Repos\Gorilla Escape\unity_build.log"

cmd = [
    unity_exe,
    "-batchmode",
    "-quit",
    "-projectPath", project_path,
    "-executeMethod", "GorillaEscape.Editor.ProductPlayMode.BuildWindowsStandalone",
    "-logFile", log_path
]

print("Starting Unity Windows Standalone build...")
start_time = time.time()

# Ensure previous log is cleared or ready
with open(log_path, "w", encoding="utf-8") as f:
    f.write("=== UNITY BUILD STARTED ===\n")

proc = subprocess.Popen(cmd)

timeout_sec = 600 # 10 minutes
while proc.poll() is None:
    time.sleep(5)
    elapsed = int(time.time() - start_time)
    print(f"Building... elapsed {elapsed}s")
    if elapsed > timeout_sec:
        print("Build timed out!")
        proc.kill()
        sys.exit(1)

ret = proc.returncode
print(f"Unity build finished with exit code {ret} in {int(time.time() - start_time)}s")
sys.exit(ret)
