# Revisión de portabilidad del Editor Windows — 2026-11-06

Se revisó la rama `feature/saul-jungle-party-alpha` para compartir los cambios pendientes de controles y permitir preparar el túnel en otra PC sin copiar credenciales del desarrollador.

## Cambios

- `tools/prepare-editor-windows.ps1`: requiere Git/LFS, Node 24 y JDK; descarga los recursos LFS, prepara PWA/Server y obtiene el binario oficial de Cloudflared 2026.10.0, comprobando su SHA-256 antes de ejecutarlo. `-VerifyTunnel` activa una prueba de HTTPS real con limpieza del proceso. Los archivos generados siguen fuera de Git.
- El script ejecuta npm con el Node seleccionado para evitar que `npm.cmd` use otra versión instalada junto a él. Esto se detectó al probar con Node 24 en PATH y Node 26 instalado globalmente.
- Cada PC utiliza su propio Quick Tunnel por defecto. No se busca un archivo arbitrario en `.cloudflared`; los túneles con nombre se seleccionan explícitamente. No se publicaron ni distribuyeron credenciales.
- El menú del Editor respeta un presupuesto de arranque explícito; por defecto usa 30 segundos en vez de imponer 15.
- Instrucciones de primera preparación en [Unity/README.md](../../Unity/README.md#desarrollo-local).

## Verificación

- PWA: 97/97 pruebas y build aprobada.
- C# independiente del motor: 180/180.
- Flutter: analizador sin incidencias y 26/26 pruebas.
- Unity 6000.6.0f1: importación/compilación batch terminada con código 0. La licencia permitió ejecutar esta revisión. EditMode seleccionado: **28/28** (`CompetitiveMechanicsTests`, `CameraGameplayControlsTests`, `JunglePresentationTests`). No acredita una build Windows ni una partida física.
- PlayMode: **1/1**, `DisciplinesSwitchDevicesAutomaticallyAndCoconutFitsItsVisibleSize`; recorre siete escenas, comprueba perfiles de dispositivos, mazo compartido y tamaño/centrado del coco. Sin servicios físicos, sin aceptación de red/cámara/teléfono.
- Server antes del ajuste de portabilidad: `verify` aprobado, 165 casos, 0 fallos/errores, 15 omitidos por sus requisitos/opt-in.
- Después del ajuste: 41/41 casos de `ConnectivityTest`, `ConnectivityRecoveryTest` y `PortableTunnelTest` aprobados. Incluyen una PC sin credenciales, credenciales ajenas ignoradas y selección explícita compatible.
- Preparación Windows sin `-VerifyTunnel`: ejecutada completa con Node 24.22.1. LFS, integridad y ejecución de Cloudflared, `npm ci`, build PWA y `Server verify` aprobados. El estado final del servidor contabiliza 168 casos, 0 fallos/errores y 15 omitidos. El binario ya estaba presente; la rama de descarga no se ejecutó en esta comprobación.
- Prueba externa `RealQuickTunnelTest`: **NO APROBADA**. El arranque y su comprobación inicial HTTPS terminaron; la solicitud pública independiente posterior falló con `ConnectException` causada por `UnresolvedAddressException`, incluso con reintentos DNS durante 55 segundos. No se considera acceso público estable ni aceptación del túnel.

## Alcance para otra PC

Clonar no instala Java, Node, Python ni los paquetes de cámara. La preparación documentada cubre PWA, Server, LFS y Cloudflared; el runtime de cámara se prepara aparte. El QR puede usar directamente el dominio temporal sin secreto del enrutador de salas. Cada PC y red debe ejecutar `-VerifyTunnel` y probar un teléfono físico. La conectividad WebRTC, sensores, firewall, aislamiento Wi-Fi, DNS y disponibilidad Cloudflare no quedan acreditados por las pruebas locales. Esta revisión no garantiza funcionamiento perfecto en una computadora o red no probadas ni modifica la configuración DNS/firewall del sistema.
