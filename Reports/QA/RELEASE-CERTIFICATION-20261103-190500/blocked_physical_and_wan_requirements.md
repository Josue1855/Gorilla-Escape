# Requisitos Físicos y de Red Bloqueados (Lista de Bloqueos para GO Final)

**Identificador:** `RELEASE-CERTIFICATION-20261103-190500`  
**Total de Requisitos Bloqueados:** 11 de 48 ($22.9\%$)  
**Principio de Honestidad:** El comité se niega estrictamente a simular hardware físico ausente o inventar resultados en redes celulares externas.

---

## 1. Desglose Detallado de los 11 Requisitos Bloqueados

### A. Validación de Cámara Física en Vivo (1 Requisito: CAM-02)
- **Causa del Bloqueo:** La máquina de desarrollo no cuenta en esta sesión con una persona física ubicada en un espacio iluminado frente a una webcam USB ejecutando la sesión de calibración en vivo.
- **Protocolo de Desbloqueo:** Conectar cámara web USB compatible con OpenCV / UVC, colocar a un voluntario a $1.8\text{ m}$ de distancia, y verificar el cursor visual y tracking de brazos en pantalla.

### B. Validación con Cuatro Smartphones Físicos en WAN Externa (4 Requisitos: TEL-03, PWA-03, UX-02, NET-04)
- **Causa del Bloqueo:** La homologación final exige 4 teléfonos celulares físicos reales (mezcla de Android y iPhone) conectados a través de redes celulares 4G/5G externas, sin usar `localhost`, sin ADB reverse y sin DNS manuales.
- **Protocolo de Desbloqueo:**
  1. Conectar 4 teléfonos a datos móviles (red celular telcel/att/movistar).
  2. Abrir la cámara nativa del teléfono y escanear el código QR mostrado en la pantalla de Unity.
  3. Verificar que 2 teléfonos abran la PWA en Chrome/Safari y 2 teléfonos abran la app Flutter vía Android App Links.
  4. Comprobar que los 4 teléfonos aparezcan en el lobby de Unity como Jugador 1, 2, 3 y 4.
  5. Intentar escanear con un 5to teléfono y verificar el mensaje amigable de sala llena.

### C. Túnel Cloudflare Público en Vivo (`https://play.gorillas.run`) (2 Requisitos: NET-04, CAN-01)
- **Causa del Bloqueo:** Requiere que el token de túnel nombrado `gorilla-escape` esté activo en el anfitrión y enrutando tráfico público hacia `play.gorillas.run` sin bloqueos de firewall corporativo.
- **Protocolo de Desbloqueo:** Iniciar `cloudflared tunnel run gorilla-escape`, verificar resolución DNS pública en `play.gorillas.run/health` y validar tráfico de señalización WebRTC.

### D. Instalación en Segunda Computadora Limpia (2 Requisitos: INS-03, CAN-01)
- **Causa del Bloqueo:** Se dispone de una sola estación de trabajo anfitriona.
- **Protocolo de Desbloqueo:** Copiar `GorillaEscape-Setup-1.0.exe` a una laptop secundaria con Windows 10/11 sin software de desarrollo, ejecutar el instalador y verificar que el juego inicie con su Java y Python propios sin errores.

### E. Evaluación UX con Usuarios Humanos Inexpertos (2 Requisitos: UX-02, CAN-01)
- **Causa del Bloqueo:** Requiere una sesión de juego presencial con 4 personas reales (niño, adulto mayor, adolescente y novato tecnológico) observando su comportamiento sin asistencia técnica.

---

## 2. Plan de Desbloqueo para Aprobación GO

Una vez que el equipo cuente con los 4 teléfonos físicos y el acceso a red externa, la sesión de homologación física tomará aproximadamente 45 minutos. Hasta que dicha sesión sea documentada con capturas de los teléfonos reales, el veredicto oficial debe permanecer en **NO-GO**.
