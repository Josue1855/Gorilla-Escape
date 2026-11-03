import subprocess, time, urllib.request, json, os, re

BASE_URL = "http://127.0.0.1:8080"
OPERATOR = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
JAR_PATH = os.path.join(REPO_ROOT, "Server", "target", "local-server-0.1.0-SNAPSHOT.jar")

def start_server():
    print("Checking if server is running...")
    try:
        with urllib.request.urlopen(f"{BASE_URL}/api/health", timeout=1) as resp:
            if resp.status == 200:
                print("Server is already running.")
                return None
    except Exception:
        pass
    print("Starting Java Server...")
    env = os.environ.copy()
    env["GORILLA_MOBILE_OPERATOR"] = OPERATOR
    env["GORILLA_MOBILE_LAB"] = "1"
    env["GORILLA_ROOM_CODE"] = "jngl"
    cmd = [
        "java", "-jar",
        JAR_PATH,
        "--server.port=8080",
        "--gorilla.mobile.rtc-enabled=true",
        "--gorilla.mobile.lab-enabled=true"
    ]
    proc = subprocess.Popen(cmd, env=env, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
    for _ in range(30):
        time.sleep(0.5)
        try:
            with urllib.request.urlopen(f"{BASE_URL}/api/health", timeout=1) as resp:
                if resp.status == 200:
                    print("Server started successfully.")
                    return proc
        except Exception:
            pass
    raise RuntimeError("Failed to start Java backend")

def find_bounds(xml, pattern):
    m = re.search(pattern, xml)
    if m:
        return (int(m.group(1)) + int(m.group(3))) // 2, (int(m.group(2)) + int(m.group(4))) // 2
    return None

def connect_device(device):
    print(f"Connecting device {device}...")
    subprocess.run(["adb", "-s", device, "reverse", "tcp:8080", "tcp:8080"], check=True)
    
    # Get admission
    req = urllib.request.Request(f"{BASE_URL}/mobile/admission", headers={"X-Gorilla-Operator": OPERATOR}, method="POST")
    with urllib.request.urlopen(req) as resp:
        data = json.loads(resp.read().decode())
    frag = data["url"].split("#")[1]
    url = f"http://127.0.0.1:8080/#{frag}"
    print(f"[{device}] Got admission URL: {url}")
    
    # Reset app
    subprocess.run(["adb", "-s", device, "shell", "am", "force-stop", "com.gorillaescape.gorilla_companion"])
    time.sleep(1)
    subprocess.run(["adb", "-s", device, "shell", "am", "start", "-n", "com.gorillaescape.gorilla_companion/.MainActivity"])
    time.sleep(3)
    
    # Tap expansion tile
    subprocess.run(["adb", "-s", device, "shell", "input", "tap", "240", "685"])
    time.sleep(1)
    
    # Scroll to expose input
    subprocess.run(["adb", "-s", device, "shell", "input", "swipe", "240", "650", "240", "250", "300"])
    time.sleep(1)
    
    # Dump UI to find EditText bounds
    subprocess.run(["adb", "-s", device, "shell", "uiautomator", "dump", "/data/local/tmp/step_edit.xml"])
    xml = subprocess.check_output(["adb", "-s", device, "shell", "cat", "/data/local/tmp/step_edit.xml"]).decode("utf-8", errors="ignore")
    pt = find_bounds(xml, r'class="android.widget.EditText"[^>]*bounds="\[(\d+),(\d+)\]\[(\d+),(\d+)\]"')
    if not pt:
        pt = (240, 617)
    print(f"[{device}] Tapping EditText at {pt}")
    subprocess.run(["adb", "-s", device, "shell", "input", "tap", str(pt[0]), str(pt[1])])
    time.sleep(1)
    
    # Type URL
    safe_url = url.replace('&', r'\&')
    print(f"[{device}] Typing URL...")
    subprocess.run(["adb", "-s", device, "shell", "input", "text", safe_url])
    time.sleep(1)
    
    # Tap ABRIR CONTROL
    subprocess.run(["adb", "-s", device, "shell", "uiautomator", "dump", "/data/local/tmp/step_btn.xml"])
    xml2 = subprocess.check_output(["adb", "-s", device, "shell", "cat", "/data/local/tmp/step_btn.xml"]).decode("utf-8", errors="ignore")
    btn_pt = find_bounds(xml2, r'(?:text|content-desc)="[^"]*ABRIR CONTROL[^"]*"[^>]*bounds="\[(\d+),(\d+)\]\[(\d+),(\d+)\]"')
    if not btn_pt:
        btn_pt = (240, 427)
    print(f"[{device}] Tapping ABRIR CONTROL at {btn_pt}")
    subprocess.run(["adb", "-s", device, "shell", "input", "tap", str(btn_pt[0]), str(btn_pt[1])])
    time.sleep(4)
    
    # Check diagnostics
    diag_req = urllib.request.Request(f"{BASE_URL}/mobile/diagnostics", headers={"X-Gorilla-Operator": OPERATOR})
    with urllib.request.urlopen(diag_req) as resp:
        diag = json.loads(resp.read().decode())
    print(f"[{device}] Diagnostics after join:")
    print(json.dumps(diag, indent=2))

if __name__ == "__main__":
    proc = start_server()
    try:
        connect_device("emulator-5556")
    finally:
        pass
