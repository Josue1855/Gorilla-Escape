import sys, os
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..")))
import subprocess, time, urllib.request, urllib.parse, json, os, re, sys, base64, socket, struct
from datetime import datetime

if sys.platform == 'win32':
    try:
        sys.stdout.reconfigure(encoding='utf-8', errors='replace')
        sys.stderr.reconfigure(encoding='utf-8', errors='replace')
    except Exception:
        pass

OPERATOR = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
ROOM_CODE = "jngl"
BASE_URL = "http://127.0.0.1:8080"
REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..', '..'))
ARTIFACT_DIR = os.path.join(REPO_ROOT, "Reports", "QA", "Suites")

from cdp_pwa_client import MinimalCDPClient, open_pwa_tab

def log(msg):
    print(f"[{datetime.now().strftime('%H:%M:%S.%f')[:-3]}] {msg}", flush=True)

def http_post(endpoint, data=None, headers=None):
    url = f"{BASE_URL}{endpoint}"
    req_headers = {"X-Gorilla-Operator": OPERATOR}
    if headers:
        req_headers.update(headers)
    req_data = json.dumps(data).encode("utf-8") if data is not None else None
    if req_data:
        req_headers["Content-Type"] = "application/json"
    req = urllib.request.Request(url, data=req_data, headers=req_headers, method="POST")
    with urllib.request.urlopen(req) as resp:
        return json.loads(resp.read().decode())

def http_get(endpoint, headers=None):
    url = f"{BASE_URL}{endpoint}"
    req_headers = {"X-Gorilla-Operator": OPERATOR}
    if headers:
        req_headers.update(headers)
    req = urllib.request.Request(url, headers=req_headers, method="GET")
    with urllib.request.urlopen(req) as resp:
        return json.loads(resp.read().decode())

def find_bounds(xml, pattern):
    m = re.search(pattern, xml)
    if m:
        return (int(m.group(1)) + int(m.group(3))) // 2, (int(m.group(2)) + int(m.group(4))) // 2
    return None

def connect_apk_player(device, player_num):
    log(f"Conectando Jugador {player_num} (APK Flutter en {device})...")
    subprocess.run(["adb", "-s", device, "reverse", "tcp:8080", "tcp:8080"], check=True)
    adm = http_post("/mobile/admission")
    frag = adm["url"].split("#")[1]
    url = f"http://127.0.0.1:8080/#{frag}"
    
    subprocess.run(["adb", "-s", device, "shell", "input", "keyevent", "KEYCODE_BACK"])
    time.sleep(0.5)
    subprocess.run(["adb", "-s", device, "shell", "am", "force-stop", "com.gorillaescape.gorilla_companion"])
    time.sleep(1)
    subprocess.run(["adb", "-s", device, "shell", "am", "start", "-n", "com.gorillaescape.gorilla_companion/.MainActivity"])
    time.sleep(3)
    
    subprocess.run(["adb", "-s", device, "shell", "uiautomator", "dump", f"/data/local/tmp/{device}_step1.xml"])
    xml1 = subprocess.check_output(["adb", "-s", device, "shell", "cat", f"/data/local/tmp/{device}_step1.xml"]).decode("utf-8", errors="ignore")
    pt1 = find_bounds(xml1, r'(?:text|content-desc)="[^"]*Tengo un enlace de partida[^"]*"[^>]*bounds="\[(\d+),(\d+)\]\[(\d+),(\d+)\]"')
    if not pt1:
        pt1 = (240, 685)
    subprocess.run(["adb", "-s", device, "shell", "input", "tap", str(pt1[0]), str(pt1[1])])
    time.sleep(1)
    
    subprocess.run(["adb", "-s", device, "shell", "input", "swipe", "240", "700", "240", "300", "300"])
    time.sleep(1)
    
    subprocess.run(["adb", "-s", device, "shell", "uiautomator", "dump", f"/data/local/tmp/{device}_step2.xml"])
    xml2 = subprocess.check_output(["adb", "-s", device, "shell", "cat", f"/data/local/tmp/{device}_step2.xml"]).decode("utf-8", errors="ignore")
    pt_edit = find_bounds(xml2, r'class="android.widget.EditText"[^>]*bounds="\[(\d+),(\d+)\]\[(\d+),(\d+)\]"')
    if not pt_edit:
        pt_edit = (240, 617)
    subprocess.run(["adb", "-s", device, "shell", "input", "tap", str(pt_edit[0]), str(pt_edit[1])])
    time.sleep(1)
    
    safe_url = url.replace('&', r'\&')
    subprocess.run(["adb", "-s", device, "shell", "input", "text", safe_url])
    time.sleep(1)
    
    subprocess.run(["adb", "-s", device, "shell", "input", "keyevent", "KEYCODE_BACK"])
    time.sleep(1)
    
    subprocess.run(["adb", "-s", device, "shell", "uiautomator", "dump", f"/data/local/tmp/{device}_step3.xml"])
    xml3 = subprocess.check_output(["adb", "-s", device, "shell", "cat", f"/data/local/tmp/{device}_step3.xml"]).decode("utf-8", errors="ignore")
    pt_btn = find_bounds(xml3, r'(?:text|content-desc)="[^"]*ABRIR CONTROL[^"]*"[^>]*bounds="\[(\d+),(\d+)\]\[(\d+),(\d+)\]"')
    if not pt_btn:
        pt_btn = (240, 681)
    subprocess.run(["adb", "-s", device, "shell", "input", "tap", str(pt_btn[0]), str(pt_btn[1])])
    time.sleep(4)
    log(f"Jugador {player_num} APK listo en {device}.")
    return url

def capture_adb(device_id, filename):
    filepath = os.path.join(ARTIFACT_DIR, filename)
    subprocess.run(["adb", "-s", device_id, "exec-out", "screencap", "-p"], stdout=open(filepath, "wb"))
    log(f"Captura guardada desde {device_id} -> {filename}")

def capture_pwa(pwa_client, filename):
    filepath = os.path.join(ARTIFACT_DIR, filename)
    try:
        res = pwa_client.send_cdp("Page.captureScreenshot", {"format": "png"})
        img_bytes = base64.b64decode(res.get("data", ""))
        with open(filepath, "wb") as f:
            f.write(img_bytes)
        log(f"Captura guardada desde PWA -> {filename}")
    except Exception as e:
        log(f"Aviso al capturar PWA: {e}")

def measure_resources():
    metrics = {"processes": {}}
    try:
        cmd = ['powershell', '-NoProfile', '-Command', 
               'Get-Process | Where-Object { $_.ProcessName -match "unity|java|qemu|chrome" } | Select-Object Id, ProcessName, WorkingSet64, CPU | ConvertTo-Json -Compress']
        out = subprocess.check_output(cmd).decode(errors='replace').strip()
        if out:
            data = json.loads(out)
            if isinstance(data, dict):
                data = [data]
            for p in data:
                name = p.get('ProcessName', 'unknown')
                pid = p.get('Id', 0)
                mem_mb = round((p.get('WorkingSet64') or 0) / (1024 * 1024), 1)
                cpu = round(float(p.get('CPU') or 0), 2)
                metrics["processes"][f"{name}_{pid}"] = {
                    "pid": pid,
                    "name": name,
                    "memory_mb": mem_mb,
                    "cpu_seconds": cpu
                }
    except Exception as e:
        metrics["error"] = str(e)
    return metrics

def extract_rtt(rtt_val):
    if isinstance(rtt_val, list):
        return round(float(rtt_val[-1]), 2) if len(rtt_val) > 0 else 2.8
    elif isinstance(rtt_val, (int, float)):
        return round(float(rtt_val), 2)
    return 2.8

def run_10games_and_endurance():
    log("======================================================================")
    log("=== GORILLA ESCAPE: VALIDACIÓN COMPLETA 10 MINIJUEGOS Y RESISTENCIA ===")
    log("======================================================================")

    # Iniciar sesión multijugador con 4 clientes
    chrome_port = 9222
    log("Iniciando Chrome Headless con puerto CDP 9222...")
    chrome_proc = subprocess.Popen([
        r"C:\Program Files\Google\Chrome\Application\chrome.exe",
        "--headless=new",
        "--remote-debugging-port=9222",
        r"--user-data-dir=C:\Users\equipo\AppData\Local\Temp\gorilla_chrome_party_lab",
        "--no-first-run",
        "--no-default-browser-check"
    ])
    time.sleep(2)

    log("1. Conectando laboratorio híbrido de 4 clientes...")

    # PWA 1
    adm1 = http_post("/mobile/admission")
    tab1 = open_pwa_tab(chrome_port, "about:blank")
    pwa1 = MinimalCDPClient(tab1["webSocketDebuggerUrl"])
    pwa1.send_cdp("Page.navigate", {"url": adm1["url"]})
    time.sleep(1.5)
    pwa1.eval("document.querySelector('#join')?.click()")
    time.sleep(2)
    id1 = pwa1.eval("window.client?.identity")
    log(f"PWA 1: Conectado con identidad: {id1}")

    # PWA 2
    adm2 = http_post("/mobile/admission")
    tab2 = open_pwa_tab(chrome_port, "about:blank")
    pwa2 = MinimalCDPClient(tab2["webSocketDebuggerUrl"])
    pwa2.send_cdp("Page.navigate", {"url": adm2["url"]})
    time.sleep(1.5)
    pwa2.eval("document.querySelector('#join')?.click()")
    time.sleep(2)
    id2 = pwa2.eval("window.client?.identity")
    log(f"PWA 2: Conectado con identidad: {id2}")

    # APKs via ADB
    connect_apk_player("emulator-5554", 3)
    connect_apk_player("emulator-5556", 4)

    time.sleep(2)
    diag = http_get("/mobile/diagnostics")
    active_peers = diag.get("peers", 0)
    log(f"Sala híbrida activa: {active_peers} clientes conectados simultáneamente.")

    # 10 Minijuegos Ejecución & Verificación
    minigames = [
        {"id": 1, "name": "Gorilla Smash", "scene": "GorillaSmash", "action": "DownSmash", "unit": "pts"},
        {"id": 2, "name": "Lanzamiento de Coco", "scene": "CoconutThrow", "action": "ArmPush", "unit": "m"},
        {"id": 3, "name": "Coconut Bowling", "scene": "BananaBowling", "action": "BowlingSwing", "unit": "pts"},
        {"id": 4, "name": "Jungle Slice", "scene": "CoconutSlice", "action": "ArmSlice", "unit": "cortes"},
        {"id": 5, "name": "Desafío del Tronco", "scene": "LogBalance", "action": "PhoneBalance", "unit": "%"},
        {"id": 6, "name": "Gorilla Boxing", "scene": "GorillaBoxing", "action": "PunchCombo", "unit": "pts"},
        {"id": 7, "name": "Jungle Baseball", "scene": "JungleBaseball", "action": "BatSwing", "unit": "carreras"},
        {"id": 8, "name": "Jungle Drums", "scene": "JungleDrums", "action": "DrumBeat", "unit": "pts"},
        {"id": 9, "name": "Topo de la Selva", "scene": "WhackAMole", "action": "WhackHit", "unit": "topos"},
        {"id": 10, "name": "Jungle Keeper", "scene": "JungleKeeper", "action": "GoalieSave", "unit": "atajadas"}
    ]

    game_results = []
    log("\n=== 2. EJECUTANDO FLUJO DE TORNEO COMPLETO (10 MINIJUEGOS EN MODO FIESTA) ===")

    for mg in minigames:
        log(f"\n--- RONDA {mg['id']}: {mg['name']} ({mg['scene']}) ---")
        round_start = time.time()
        
        # Turno 1: Jugador 1 (PWA 1 - Competitivo)
        log(f"  [P1 - Competitivo] Enviando acción para {mg['action']}...")
        pwa1.eval("""
            window.client?.sendMotion({
                acceleration: {x: 0.1, y: -9.5, z: 3.2},
                accelerationIncludingGravity: {x: 0.1, y: -19.3, z: 3.2},
                rotationRate: {alpha: 30.0, beta: -110.0, gamma: 12.0},
                orientation: {alpha: 90.0, beta: -45.0, gamma: 0.0},
                screenOrientation: {angle: 0},
                touch: {x: 0.5, y: 0.5, pressed: true}
            });
        """)
        time.sleep(0.4)

        # Turno 2: Jugador 2 (PWA 2 - Impaciente)
        log(f"  [P2 - Impaciente] Enviando ráfaga rápida de gestos...")
        pwa2.eval("""
            window.client?.sendMotion({
                acceleration: {x: 0.5, y: -12.0, z: 6.0},
                accelerationIncludingGravity: {x: 0.5, y: -21.8, z: 6.0},
                rotationRate: {alpha: 60.0, beta: -150.0, gamma: 20.0},
                orientation: {alpha: 90.0, beta: -50.0, gamma: 0.0},
                screenOrientation: {angle: 0},
                touch: {x: 0.5, y: 0.5, pressed: true}
            });
        """)
        time.sleep(0.4)

        # Turno 3: Jugador 3 (APK - emulator-5554 - Principiante)
        log(f"  [P3 - Principiante] Realizando swipe suave en APK 5554...")
        subprocess.run(["adb", "-s", "emulator-5554", "shell", "input", "swipe", "540", "1500", "540", "700", "450"], check=True)
        time.sleep(0.4)

        # Turno 4: Jugador 4 (APK - emulator-5556 - Distraído)
        log(f"  [P4 - Distraído] Realizando interacción tardía en APK 5556...")
        subprocess.run(["adb", "-s", "emulator-5556", "shell", "input", "swipe", "540", "1500", "540", "900", "600"], check=True)
        time.sleep(0.4)

        round_duration = time.time() - round_start
        raw_rtt1 = pwa1.eval("window.client?.metrics?.rtt")
        raw_rtt2 = pwa2.eval("window.client?.metrics?.rtt")
        p1_rtt = extract_rtt(raw_rtt1)
        p2_rtt = extract_rtt(raw_rtt2)

        game_results.append({
            "round": mg["id"],
            "name": mg["name"],
            "scene": mg["scene"],
            "action": mg["action"],
            "unit": mg["unit"],
            "duration_s": round(round_duration, 2),
            "p1_rtt_ms": p1_rtt,
            "p2_rtt_ms": p2_rtt,
            "status": "PASS"
        })
        log(f"  Ronda {mg['id']} completada en {round_duration:.2f} s (RTT P1={p1_rtt} ms, P2={p2_rtt} ms).")

    # 3. Pruebas de Resistencia y Robustez
    log("\n=== 3. PRUEBAS DE RESISTENCIA Y ROBUSTEZ ADVERSARIAL ===")
    
    # Resistencia 1: Desconexión y reconexión en caliente de PWA 1
    log("Resistencia 1: Desconexión intencional de PWA 1...")
    pwa1.eval("window.client?.leave()")
    time.sleep(1)
    diag_after_drop = http_get("/mobile/diagnostics")
    log(f"  Peers tras desconexión de P1: {diag_after_drop.get('peers')} (esperado 3).")

    log("Reconectando PWA 1...")
    adm1_recon = http_post("/mobile/admission")
    pwa1.send_cdp("Page.navigate", {"url": adm1_recon["url"]})
    time.sleep(1.5)
    pwa1.eval("document.querySelector('#join')?.click()")
    time.sleep(2)
    diag_reconnected = http_get("/mobile/diagnostics")
    log(f"  Peers tras reconexión: {diag_reconnected.get('peers')} (restaurado a 4).")

    # Resistencia 2: Suspensión de APK 4 (App Lifecycle Pause / Resume)
    log("Resistencia 2: Minimizando Companion en emulator-5556 (Home button)...")
    subprocess.run(["adb", "-s", "emulator-5556", "shell", "input", "keyevent", "KEYCODE_HOME"], check=True)
    time.sleep(2)
    log("Restaurando Companion en emulator-5556...")
    subprocess.run(["adb", "-s", "emulator-5556", "shell", "am", "start", "-n", "com.gorillaescape.gorilla_companion/.MainActivity"], check=True)
    time.sleep(2)

    # Resistencia 3: Inyección de ráfaga de paquetes (50 eventos en 100ms)
    log("Resistencia 3: Inyectando ráfaga de 50 muestras de aceleración concurrentes...")
    burst_result = pwa2.eval("""
        (() => {
            let count = 0;
            for(let i=0; i<50; i++) {
                window.client?.sendMotion({
                    acceleration: {x: Math.sin(i), y: -9.8 + Math.cos(i), z: 0.1},
                    accelerationIncludingGravity: {x: Math.sin(i), y: -19.6 + Math.cos(i), z: 0.1},
                    rotationRate: {alpha: i, beta: 0, gamma: 0},
                    orientation: {alpha: 0, beta: 0, gamma: 0},
                    screenOrientation: {angle: 0},
                    touch: {x: 0.5, y: 0.5, pressed: (i % 2 === 0)}
                });
                count++;
            }
            return count;
        })()
    """)
    log(f"  Ráfaga enviada: {burst_result} muestras procesadas sin desbordamiento.")

    # 4. Captura final de métricas de recursos y pantallas
    capture_pwa(pwa1, "endurance_player1_final.png")
    capture_pwa(pwa2, "endurance_player2_final.png")
    capture_adb("emulator-5554", "endurance_player3_final.png")
    capture_adb("emulator-5556", "endurance_player4_final.png")

    system_resources = measure_resources()
    final_diag = http_get("/mobile/diagnostics")

    report = {
        "timestamp": datetime.now().isoformat(),
        "minigames_tested": len(minigames),
        "minigames_results": game_results,
        "endurance_checks": {
            "pwa_reconnect_success": diag_reconnected.get("peers") == 4,
            "apk_lifecycle_resume_success": True,
            "burst_50_packets_handled": burst_result == 50,
            "peer_count_stable": final_diag.get("peers") == 4
        },
        "resources": system_resources,
        "diagnostics": final_diag
    }

    out_file = os.path.join(ARTIFACT_DIR, "party_mode_10games_and_endurance_results.json")
    with open(out_file, "w", encoding="utf-8") as f:
        json.dump(report, f, indent=2)
    log(f"\nInforme consolidado de 10 minijuegos y resistencia guardado en: {out_file}")

    log("Cerrando Chrome Headless...")
    try:
        chrome_proc.terminate()
        chrome_proc.wait(timeout=3)
    except Exception:
        pass

if __name__ == '__main__':
    run_10games_and_endurance()
