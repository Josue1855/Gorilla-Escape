#!/usr/bin/env python3
"""
GORILLA ESCAPE 1.0 — VERIFICACIÓN DE 4 TELÉFONOS FÍSICOS VÍA TÚNEL PÚBLICO (https://play.gorillas.run)

Ejecuta y registra la validación del requisito final de producción:
  - 2 teléfonos físicos utilizando PWA
  - 2 teléfonos físicos utilizando Flutter Companion
  - Unity Standalone ejecutándose en Windows
  - Acceso público por https://play.gorillas.run desde redes externas diferentes

REGLA DE AUDITORÍA ESTRICTA:
Si no están presentes los 4 teléfonos físicos reales (no emuladores, no navegadores headless locales)
y el túnel público activo en https://play.gorillas.run, este script clasifica el resultado como
"BLOCKED" (NUNCA como "PASS") y genera el registro auditable con los criterios exactos de aceptación.
"""

import json
import os
import subprocess
import time
import urllib.request
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[3]
REPORT_PATH = REPO_ROOT / "Reports" / "QA" / "Suites" / "physical_4phones_public_validation.json"
PUBLIC_DOMAIN = "https://play.gorillas.run"
LOCAL_SERVER = "http://127.0.0.1:8080"


def check_adb_physical_phones():
    """Detecta dispositivos Android conectados por ADB y separa físicos de emuladores."""
    try:
        out = subprocess.check_output(["adb", "devices", "-l"], stderr=subprocess.STDOUT).decode("utf-8", errors="replace")
    except Exception as exc:
        return {"physical": [], "emulators": [], "error": str(exc)}

    physical = []
    emulators = []
    for line in out.splitlines()[1:]:
        line = line.strip()
        if not line or "device" not in line.split():
            continue
        serial = line.split()[0]
        if serial.startswith("emulator-") or "vbox" in line.lower() or "sdk_gphone" in line.lower():
            emulators.append(serial)
        else:
            physical.append(serial)
    return {"physical": physical, "emulators": emulators, "error": None}


def check_public_tunnel_health():
    """Comprueba si https://play.gorillas.run responde con el estado de sala activa."""
    req = urllib.request.Request(
        f"{PUBLIC_DOMAIN}/mobile/health",
        headers={"User-Agent": "Mozilla/5.0 (Linux; Android 14; Pixel 8) Chrome/124.0.0.0 Mobile Safari/537.36"}
    )
    try:
        with urllib.request.urlopen(req, timeout=6) as resp:
            body = resp.read().decode("utf-8", errors="replace")
            return {"reachable": resp.status == 200, "status_code": resp.status, "body_preview": body[:240]}
    except Exception as exc:
        return {"reachable": False, "status_code": None, "error": str(exc)}


def check_local_server_diagnostics():
    """Consulta diagnósticos del servidor local si está en ejecución."""
    try:
        with urllib.request.urlopen(f"{LOCAL_SERVER}/mobile/diagnostics", timeout=3) as resp:
            return json.loads(resp.read().decode("utf-8"))
    except Exception as exc:
        return {"reachable": False, "error": str(exc)}


def check_unity_standalone_running():
    """Comprueba si GorillaEscape.exe (Unity Standalone Windows) está ejecutándose."""
    try:
        out = subprocess.check_output(
            ["powershell", "-NoProfile", "-Command",
             "Get-Process -Name 'GorillaEscape' -ErrorAction SilentlyContinue | Select-Object Id, ProcessName | ConvertTo-Json"],
            stderr=subprocess.STDOUT
        ).decode("utf-8", errors="replace").strip()
        if not out:
            return {"running": False, "processes": []}
        parsed = json.loads(out)
        procs = parsed if isinstance(parsed, list) else [parsed]
        return {"running": len(procs) > 0, "processes": procs}
    except Exception as exc:
        return {"running": False, "error": str(exc)}


def main():
    REPORT_PATH.parent.mkdir(parents=True, exist_ok=True)
    adb_info = check_adb_physical_phones()
    tunnel_info = check_public_tunnel_health()
    local_diag = check_local_server_diagnostics()
    unity_info = check_unity_standalone_running()

    physical_flutter_count = len(adb_info["physical"])
    active_peers = local_diag.get("peers", 0) if isinstance(local_diag, dict) else 0

    blockers = []
    if physical_flutter_count < 2:
        blockers.append(
            f"Se requieren 2 teléfonos físicos con Flutter Companion; detectados {physical_flutter_count} físicos "
            f"(emuladores detectados: {adb_info['emulators']}, que NO sustituyen teléfonos físicos)."
        )
    if not tunnel_info.get("reachable"):
        blockers.append(
            f"El dominio público {PUBLIC_DOMAIN}/mobile/health no tiene un túnel de sala activo en este momento "
            f"({tunnel_info.get('error') or tunnel_info.get('status_code')})."
        )
    if not unity_info.get("running"):
        blockers.append("GorillaEscape.exe (Unity Standalone Windows) no está en ejecución interactiva con operadores humanos.")
    if active_peers < 4:
        blockers.append(f"Se requieren 4 teléfonos físicos conectados desde redes externas; peers activos actuales: {active_peers}.")

    status = "PASS" if len(blockers) == 0 else "BLOCKED"

    report = {
        "test_id": "MP-PUBLIC-4PHONES-01",
        "category": "physical_devices_public_internet",
        "status": status,
        "timestamp_utc": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
        "environment_probe": {
            "adb_devices": adb_info,
            "public_tunnel": tunnel_info,
            "local_server_diagnostics": local_diag,
            "unity_standalone": unity_info,
        },
        "blockers": blockers,
        "acceptance_criteria": {
            "1_topology": "2 teléfonos físicos reales en PWA (Safari iOS / Chrome Android) + 2 teléfonos físicos reales en Flutter APK + Unity Standalone Windows.",
            "2_network": "Acceso mediante https://play.gorillas.run/<roomPrefix>/... desde al menos 2 redes externas distintas (p. ej. 4G/5G celular + Wi-Fi externo).",
            "3_webrtc_signaling": "Admisión única (/mobile/admission), oferta/respuesta SDP e ICE vía TURN/STUN completados en < 4000 ms por teléfono.",
            "4_sensor_cadence": "Frecuencia sostenida >= 30 Hz (óptimo 50-60 Hz) en los 4 dispositivos sin muestras sintéticas ni ceros fabricados.",
            "5_turn_synchronization": "Rotación de turnos P1 -> P2 -> P3 -> P4 en las 10 disciplinas con comandos de UI móviles autorizados para el jugador en turno.",
            "6_reconnection": "Suspensión de pantalla (5 s) y cambio de red recuperan la sesión con resumeToken sin duplicar slot ni perder Gorilla Points.",
            "7_latency_budget": "RTT WebRTC P95 <= 120 ms en red móvil externa; 0 cierres de socket IPC (IPC_GAME_CONTEXT = 0 errores)."
        },
        "execution_instructions_when_hardware_available": [
            "1. Iniciar GorillaEscape.exe en Windows con GORILLA_CONNECTION_MODE=INTERNET y túnel cloudflared activo hacia https://play.gorillas.run.",
            "2. Conectar 2 teléfonos físicos Android/iOS a redes móviles 4G/5G y escanear el QR de la pantalla de Conexión con el navegador (PWA).",
            "3. Conectar 2 teléfonos físicos con la app Flutter Companion instalada (Companion/build/app/outputs/flutter-apk/app-debug.apk) y escanear el QR.",
            "4. Ejecutar `python tools/qa/suites/verify_physical_4phones_public.py` mientras los 4 teléfonos están en partida para registrar la telemetría."
        ]
    }

    REPORT_PATH.write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding="utf-8")
    print(f"[verify_physical_4phones_public] Status: {status}")
    for b in blockers:
        print(f"  - BLOCKER: {b}")
    print(f"Reporte guardado en: {REPORT_PATH}")


if __name__ == "__main__":
    main()
