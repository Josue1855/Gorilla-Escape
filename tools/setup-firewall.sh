#!/usr/bin/env bash
# Gorilla Escape — Firewall helper for mobile LAN connections (UFW)
# Ensures ports 8443 (HTTPS PWA/Signaling) and 8081 (Trust Bootstrap) are open when UFW is active.

set -euo pipefail

UFW_CMD=$(command -v ufw 2>/dev/null || echo "/usr/sbin/ufw")

if [ ! -x "$UFW_CMD" ]; then
    # No UFW installed; nothing to configure
    exit 0
fi

# Check if UFW service is active
STATUS_OUT=$(systemctl is-active ufw 2>/dev/null || true)
if [ "$STATUS_OUT" != "active" ]; then
    # Firewall is not active; connections are not filtered by UFW
    exit 0
fi

ACTION="${1:-check-and-allow}"

run_elevated() {
    if [ "$EUID" -eq 0 ]; then
        "$@"
    elif command -v pkexec >/dev/null 2>&1 && [ -n "${DISPLAY:-}${WAYLAND_DISPLAY:-}" ]; then
        pkexec "$@"
    elif command -v sudo >/dev/null 2>&1; then
        sudo "$@"
    else
        echo "Error: Se requieren privilegios de administrador para configurar UFW." >&2
        exit 1
    fi
}

case "$ACTION" in
    --status)
        echo "Comprobando estado del firewall..."
        run_elevated "$UFW_CMD" status verbose
        ;;
    --remove)
        echo "Eliminando reglas de firewall para Gorilla Escape..."
        run_elevated sh -c "
            $UFW_CMD delete allow 8443/tcp 2>/dev/null || true
            $UFW_CMD delete allow 8081/tcp 2>/dev/null || true
            $UFW_CMD delete allow from 10.0.0.0/8 proto udp 2>/dev/null || true
            $UFW_CMD delete allow from 192.168.0.0/16 proto udp 2>/dev/null || true
            $UFW_CMD delete allow from 172.16.0.0/12 proto udp 2>/dev/null || true
        "
        echo "Reglas eliminadas."
        ;;
    check-and-allow|--allow)
        echo "Configurando excepciones en UFW para Gorilla Escape..."
        run_elevated sh -c "
            $UFW_CMD allow 8443/tcp comment 'Gorilla Escape HTTPS'
            $UFW_CMD allow 8081/tcp comment 'Gorilla Escape PREPARE'
            $UFW_CMD allow from 10.0.0.0/8 proto udp comment 'Gorilla Escape WebRTC UDP'
            $UFW_CMD allow from 192.168.0.0/16 proto udp comment 'Gorilla Escape WebRTC UDP'
            $UFW_CMD allow from 172.16.0.0/12 proto udp comment 'Gorilla Escape WebRTC UDP'
        "
        echo "Reglas de firewall configuradas exitosamente."
        ;;
    *)
        echo "Uso: $0 [--status | --allow | --remove]"
        exit 1
        ;;
esac
