import subprocess, time, urllib.request, urllib.parse, json, os, shutil, re, sys, base64
from cdp_pwa_client import MinimalCDPClient, open_pwa_tab

OPERATOR = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
ROOM_CODE = "jngl"
BASE_URL = "http://127.0.0.1:8080"
CHROME_PATH = r"C:\Program Files\Google\Chrome\Application\chrome.exe"
REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
JAR_PATH = os.path.join(REPO_ROOT, "Server", "target", "local-server-0.1.0-SNAPSHOT.jar")
ARTIFACT_DIR = r"C:\Users\equipo\.gemini\antigravity\brain\2c221f95-4be5-4c9d-a5ca-eccb82e6b254"

def get_admission():
    req = urllib.request.Request(
        f"{BASE_URL}/mobile/admission",
        headers={"X-Gorilla-Operator": OPERATOR},
        method="POST"
    )
    with urllib.request.urlopen(req) as resp:
        return json.loads(resp.read().decode())

def get_diagnostics():
    req = urllib.request.Request(
        f"{BASE_URL}/mobile/diagnostics",
        headers={"X-Gorilla-Operator": OPERATOR}
    )
    with urllib.request.urlopen(req) as resp:
        return json.loads(resp.read().decode())

def start_backend():
    print("--- [1] Starting Java Backend Server ---")
    env = os.environ.copy()
    env["GORILLA_MOBILE_OPERATOR"] = OPERATOR
    env["GORILLA_MOBILE_LAB"] = "1"
    env["GORILLA_ROOM_CODE"] = ROOM_CODE
    cmd = [
        "java", "-jar",
        JAR_PATH,
        "--server.port=8080",
        "--gorilla.mobile.rtc-enabled=true",
        "--gorilla.mobile.lab-enabled=true"
    ]
    proc = subprocess.Popen(cmd, env=env, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
    # Wait for health check
    for _ in range(30):
        time.sleep(0.5)
        try:
            with urllib.request.urlopen(f"{BASE_URL}/api/health", timeout=1) as resp:
                if resp.status == 200:
                    print("Backend Server is READY and Healthy on port 8080!")
                    return proc
        except Exception:
            pass
    raise RuntimeError("Failed to start Java backend server within 15 seconds")

def connect_pwa(port, player_num):
    print(f"--- Connecting PWA Player {player_num} via Chrome CDP ---")
    data = get_admission()
    pwa_url = data["url"]
    tab = open_pwa_tab(port, "about:blank")
    ws_url = tab["webSocketDebuggerUrl"]
    client = MinimalCDPClient(ws_url)
    client.send_cdp("Page.navigate", {"url": pwa_url})
    time.sleep(2)
    # Click JOIN GAME
    client.eval("document.querySelector('#join').click()")
    time.sleep(2.5)
    state = client.eval("document.querySelector('#state').textContent")
    identity = client.eval("window.client?.identity")
    diag = client.eval("window.client?.diagnostic()")
    print(f"PWA Player {player_num} state: {state}")
    print(f"PWA Player {player_num} identity: {identity}")
    
    # Capture screenshot via CDP
    snap = client.send_cdp("Page.captureScreenshot", {"format": "png"})
    resp = client.recv_cdp(expected_id=snap, timeout=10)
    img_data = resp.get("result", {}).get("data")
    if img_data:
        out_path = os.path.join(ARTIFACT_DIR, f"pwa_player{player_num}_screen.png")
        with open(out_path, "wb") as f:
            f.write(base64.b64decode(img_data))
        print(f"Saved PWA Player {player_num} screenshot to {out_path}")
    return client, identity

def find_bounds(xml, pattern):
    m = re.search(pattern, xml)
    if m:
        cx = (int(m.group(1)) + int(m.group(3))) // 2
        cy = (int(m.group(2)) + int(m.group(4))) // 2
        return cx, cy
    return None

def connect_emulator(device, player_num):
    print(f"--- Connecting APK Player {player_num} on {device} ---")
    # Ensure adb reverse
    subprocess.run(["adb", "-s", device, "reverse", "tcp:8080", "tcp:8080"], check=True)
    
    data = get_admission()
    frag = data["url"].split("#")[1]
    url = f"http://127.0.0.1:8080/#{frag}"
    
    # Ensure launcher clings / dialogs are dismissed
    subprocess.run(["adb", "-s", device, "shell", "input", "keyevent", "KEYCODE_BACK"])
    time.sleep(0.5)

    # Restart app to reset UI state
    subprocess.run(["adb", "-s", device, "shell", "am", "force-stop", "com.gorillaescape.gorilla_companion"])
    time.sleep(1)
    subprocess.run(["adb", "-s", device, "shell", "am", "start", "-n", "com.gorillaescape.gorilla_companion/.MainActivity"])
    time.sleep(3)
    
    # Dump UI to find "Tengo un enlace de partida"
    subprocess.run(["adb", "-s", device, "shell", "uiautomator", "dump", "/data/local/tmp/step1.xml"])
    xml1 = subprocess.check_output(["adb", "-s", device, "shell", "cat", "/data/local/tmp/step1.xml"]).decode("utf-8", errors="ignore")
    pt1 = find_bounds(xml1, r'(?:text|content-desc)="[^"]*Tengo un enlace de partida[^"]*"[^>]*bounds="\[(\d+),(\d+)\]\[(\d+),(\d+)\]"')
    if pt1:
        print(f"[{device}] Expanding tile at {pt1}")
        subprocess.run(["adb", "-s", device, "shell", "input", "tap", str(pt1[0]), str(pt1[1])])
        time.sleep(1)
    else:
        # Fallback scroll and try again
        subprocess.run(["adb", "-s", device, "shell", "input", "swipe", "240", "650", "240", "350", "300"])
        time.sleep(1)
        subprocess.run(["adb", "-s", device, "shell", "uiautomator", "dump", "/data/local/tmp/step1b.xml"])
        xml1b = subprocess.check_output(["adb", "-s", device, "shell", "cat", "/data/local/tmp/step1b.xml"]).decode("utf-8", errors="ignore")
        pt1b = find_bounds(xml1b, r'(?:text|content-desc)="[^"]*Tengo un enlace de partida[^"]*"[^>]*bounds="\[(\d+),(\d+)\]\[(\d+),(\d+)\]"')
        if pt1b:
            print(f"[{device}] Expanding tile at {pt1b}")
            subprocess.run(["adb", "-s", device, "shell", "input", "tap", str(pt1b[0]), str(pt1b[1])])
            time.sleep(1)
    
    # Scroll to expose EditText
    subprocess.run(["adb", "-s", device, "shell", "input", "swipe", "240", "650", "240", "250", "300"])
    time.sleep(1)
    subprocess.run(["adb", "-s", device, "shell", "uiautomator", "dump", "/data/local/tmp/step2.xml"])
    xml2 = subprocess.check_output(["adb", "-s", device, "shell", "cat", "/data/local/tmp/step2.xml"]).decode("utf-8", errors="ignore")
    pt2 = find_bounds(xml2, r'class="android.widget.EditText"[^>]*bounds="\[(\d+),(\d+)\]\[(\d+),(\d+)\]"')
    if pt2:
        print(f"[{device}] Tapping EditText at {pt2}")
        subprocess.run(["adb", "-s", device, "shell", "input", "tap", str(pt2[0]), str(pt2[1])])
        time.sleep(1)
    
    # Type URL via piped shell with communicate()
    p = subprocess.Popen(["adb", "-s", device, "shell"], stdin=subprocess.PIPE, text=True)
    try:
        p.communicate(input=f"input text '{url}'\nexit\n", timeout=10)
    except subprocess.TimeoutExpired:
        p.kill()
    time.sleep(1)
    
    # Dump UI to find "ABRIR CONTROL"
    subprocess.run(["adb", "-s", device, "shell", "uiautomator", "dump", "/data/local/tmp/step3.xml"])
    xml3 = subprocess.check_output(["adb", "-s", device, "shell", "cat", "/data/local/tmp/step3.xml"]).decode("utf-8", errors="ignore")
    pt3 = find_bounds(xml3, r'(?:text|content-desc)="[^"]*ABRIR CONTROL[^"]*"[^>]*bounds="\[(\d+),(\d+)\]\[(\d+),(\d+)\]"')
    if pt3:
        print(f"[{device}] Tapping ABRIR CONTROL at {pt3}")
        subprocess.run(["adb", "-s", device, "shell", "input", "tap", str(pt3[0]), str(pt3[1])])
    else:
        # Fallback click
        print(f"[{device}] Fallback clicking ABRIR CONTROL at (240, 427)")
        subprocess.run(["adb", "-s", device, "shell", "input", "tap", "240", "427"])
    time.sleep(4)
    
    # Capture screenshot
    screen_remote = f"/data/local/tmp/{device}_screen.png"
    screen_local = os.path.join(ARTIFACT_DIR, f"companion_player{player_num}_{device}.png")
    subprocess.run(["adb", "-s", device, "shell", "screencap", "-p", screen_remote])
    subprocess.run(["adb", "-s", device, "pull", screen_remote, screen_local])
    print(f"Captured screenshot for {device} at {screen_local}")

def measure_resources():
    print("--- Measuring System Resource Usage ---")
    ps_cmd = (
        'Get-Process -Name "java", "Unity*", "qemu*", "chrome" -ErrorAction SilentlyContinue | '
        'Select-Object Id, ProcessName, WorkingSet64, CPU | ConvertTo-Json'
    )
    out = subprocess.check_output(["powershell", "-NoProfile", "-Command", ps_cmd]).decode("utf-8")
    try:
        procs = json.loads(out)
        metrics = []
        for p in (procs if isinstance(procs, list) else [procs]):
            ram_mb = p.get("WorkingSet64", 0) / (1024 * 1024)
            metrics.append({
                "pid": p.get("Id"),
                "name": p.get("ProcessName"),
                "ram_mb": round(ram_mb, 2),
                "cpu": p.get("CPU")
            })
        return metrics
    except Exception as e:
        return {"error": str(e), "raw": out}

def main():
    backend_proc = None
    chrome_proc = None
    pwa1_client = None
    pwa2_client = None
    results = {
        "timestamp": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
        "architecture": {
            "room_code": ROOM_CODE,
            "transport": "WebRTC DataChannels (control + motion) via UDP",
            "backend": "Spring Boot / Java WebRTC runtime",
            "player_1": "PWA (Headless Chrome Tab 1)",
            "player_2": "PWA (Headless Chrome Tab 2)",
            "player_3": "Native Flutter APK on Android Emulator 7.0 API 24 (emulator-5554)",
            "player_4": "Native Flutter APK on Android Emulator 7.0 API 24 (emulator-5556)"
        },
        "admissions": [],
        "connections": {},
        "adversarial_tests": {},
        "resource_usage": []
    }
    
    try:
        # Step 1: Start backend
        backend_proc = start_backend()
        
        # Step 2: Start Chrome
        user_data = os.path.join(os.environ["LOCALAPPDATA"], "Temp", "gorilla_4player_chrome")
        if os.path.exists(user_data):
            shutil.rmtree(user_data, ignore_errors=True)
        chrome_proc = subprocess.Popen([
            CHROME_PATH,
            "--headless=new",
            "--remote-debugging-port=9222",
            f"--user-data-dir={user_data}",
            "about:blank"
        ])
        time.sleep(2)
        
        # Step 3: Connect Player 1 (PWA)
        pwa1_client, id1 = connect_pwa(9222, 1)
        results["connections"]["player_1"] = id1
        
        # Step 4: Connect Player 2 (PWA)
        pwa2_client, id2 = connect_pwa(9222, 2)
        results["connections"]["player_2"] = id2
        
        # Step 5: Connect Player 3 (Android Emulator 5554)
        connect_emulator("emulator-5554", 3)
        results["connections"]["player_3"] = {"device": "emulator-5554", "status": "CONNECTED"}
        
        # Step 6: Connect Player 4 (Android Emulator 5556)
        connect_emulator("emulator-5556", 4)
        results["connections"]["player_4"] = {"device": "emulator-5556", "status": "CONNECTED"}
        
        # Step 7: Verify Diagnostics
        diag = get_diagnostics()
        print("=== 4-PLAYER LAB SERVER DIAGNOSTICS ===")
        print(json.dumps(diag, indent=2))
        results["server_diagnostics"] = diag
        peers = diag.get("peers", 0)
        assert peers == 4, f"Expected 4 active WebRTC peers, but got {peers}"
        print(f"\nSUCCESS: ALL 4 CLIENTS CONCURRENTLY CONNECTED! Active Peers: {peers}/4\n")
        
        # Step 8: Adversarial Test - 5th Admission Rejection
        print("--- Testing 5th Player Admission Rejection ---")
        try:
            req5 = urllib.request.Request(
                f"{BASE_URL}/mobile/admission",
                headers={"X-Gorilla-Operator": OPERATOR},
                method="POST"
            )
            with urllib.request.urlopen(req5) as r5:
                res5 = json.loads(r5.read().decode())
            results["adversarial_tests"]["fifth_admission"] = "UNEXPECTED_SUCCESS"
        except urllib.error.HTTPError as he:
            print(f"5th Admission correctly rejected with HTTP {he.code}")
            results["adversarial_tests"]["fifth_admission"] = f"PASS_HTTP_{he.code}"
            
        # Step 9: Stream Inputs from PWA clients
        print("--- Streaming Motion Inputs from PWA Clients ---")
        pwa1_client.eval("""
            window.streamInterval = setInterval(() => {
                if (window.client && window.client.state === 'RUNNING') {
                    window.client.motion(normalizeMotion({
                        acceleration: {x: 0, y: 1, z: 0},
                        accelerationIncludingGravity: {x: 0, y: 9.8, z: 0},
                        rotationRate: {alpha: 0, beta: 0, gamma: 0},
                        interval: 20
                    }));
                }
            }, 50);
        """)
        pwa2_client.eval("""
            window.streamInterval = setInterval(() => {
                if (window.client && window.client.state === 'RUNNING') {
                    window.client.motion(normalizeMotion({
                        acceleration: {x: 0, y: -1, z: 0},
                        accelerationIncludingGravity: {x: 0, y: 9.8, z: 0},
                        rotationRate: {alpha: 0, beta: 0, gamma: 0},
                        interval: 20
                    }));
                }
            }, 50);
        """)
        time.sleep(3)
        diag_after_motion = get_diagnostics()
        inputs_received = diag_after_motion.get("inputs", {}).get("received", 0)
        print(f"Server Phone Inputs Received: {inputs_received}")
        results["inputs_streaming"] = diag_after_motion.get("inputs")
        assert inputs_received > 0, "No inputs received by PhoneInputHub"
        
        # Step 10: Adversarial Test - Reconnection / Resume Token
        print("--- Testing PWA Player 1 Disconnect & Reconnect ---")
        resume_res = pwa1_client.eval("""
            (async () => {
                const prior = {...window.client.identity};
                await window.client.send('CLIENT_STATE', {state: 'suspended'});
                await window.client.close(false);
                await window.client.connect(location.href, prior);
                return {
                    samePlayer: window.client.identity.playerId === prior.playerId,
                    newEpoch: window.client.identity.connectionEpoch > prior.connectionEpoch,
                    newEpochValue: window.client.identity.connectionEpoch,
                    priorEpochValue: prior.connectionEpoch,
                    state: window.client.state
                };
            })()
        """, await_promise=True)
        print("Resume result:", resume_res)
        results["adversarial_tests"]["reconnect_resume"] = resume_res
        assert resume_res.get("samePlayer") is True, "Reconnected player ID mismatch"
        assert resume_res.get("newEpoch") is True, "Connection epoch not incremented"
        
        # Step 11: Measure Resources
        results["resource_usage"] = measure_resources()
        print("Resource metrics collected:", len(results["resource_usage"]), "processes")
        
        # Save output JSON
        out_json_path = os.path.join(ARTIFACT_DIR, "hybrid_4player_lab_evidence.json")
        with open(out_json_path, "w", encoding="utf-8") as f:
            json.dump(results, f, indent=2)
        print(f"SUCCESS: Hybrid 4-Player Evidence recorded to {out_json_path}")
        
    finally:
        print("--- Cleaning up Lab ---")
        if pwa1_client:
            try: pwa1_client.close()
            except: pass
        if pwa2_client:
            try: pwa2_client.close()
            except: pass
        if chrome_proc:
            chrome_proc.terminate()
            chrome_proc.wait()
        if backend_proc:
            backend_proc.terminate()
            backend_proc.wait()

if __name__ == "__main__":
    try:
        sys.stdout.reconfigure(line_buffering=True)
    except:
        pass
    main()
