# Sesión / QR / Gorilla Protocol / WebRTC / PhoneInput — Fase 0

## Estado vigente — FINAL PHASE0 AUDIT, 2026-10-08

**PHASE 0 FINAL AUDIT: PASS — SOFTWARE/LAB.** Fase0 — Base/Spike: **COMPLETED — SOFTWARE/LAB**. Fase1 — Gorilla Smash Vertical Slice: **READY TO START**, no iniciada. Physical validation debt remains open and is required before final MVP acceptance. 3A product/physical onboarding: **DEFERRED / IN PROGRESS**. #94 **OPEN/DRAFT**; candidato local sin commit distinto del PR remoto. Sin nuevo código de producto, commit/push/merge ni fase posterior. [Audit y DoD](PHASE0_FINAL_AUDIT.md), [evidencia](evidence/phase0-final-audit-2026-10-08/gate-summary.json). Los estados previos siguientes son snapshots históricos.

> **Lectura de estado (auditoría 2026-10-08):** las fechas, estados y próximos pasos de este documento son snapshots históricos del incremento descrito. El estado consolidado vigente se registra en [PHASE0_FINAL_AUDIT.md](PHASE0_FINAL_AUDIT.md). Las decisiones de transporte anteriores están **superseded / replaced by DEC-010 Mobile Transport**; se conservan resultados y limitaciones originales.

Estado de cierre: **PASS — SOFTWARE/LAB (Chrome real sintético/replay)**. La variante Android Emulator nueva permanece BLOCKED; no se convierte en PASS Android. Regresiones aplicables PASS con artefacto fijo. Implementado y validado localmente, pendiente de autorización Git/integración. No sustituye evidencias históricas. Alcance aprobado por PO: Software/Lab, sin cámara/Fusion/gameplay/Fase1. PR #94 OPEN/DRAFT; cambios locales, sin commit/push/merge.

## Implementación

Java TechnicalSession asigna PlayerId1–4/deviceSessionId, consume admission one-use, mantiene CONNECTED/ACTIVE/DISCONNECTED, expiración y resume manual con epoch/token rotados. QR PNG generado y decodificado por ZXing antes del join real. Se reutilizan captura PWA, hosting React/health y supervisor/EOF/FileLock existentes.

Adaptador Java webrtc-java0.19.0, dos canales: control reliable/ordered y motion unordered/no-retransmit. Señalización local HTTP del laboratorio, sin pipes como contrato runtime, STUN/TURN ni cloud. SDP sólo data/host local y límites de tamaño/negociación. Opción nueva `gorilla.mobile.rtc-enabled` separada de `gorilla.mobile.enabled`, que ya pertenece al hosting HTTPS: no se activa RTC en regresiones legacy.

Gorilla Protocol v1 estricto, identidad ligada a peer, timestamp añadido por Java, secuencias por dominio de canal, rate/size/depth/duplicados/documento completo. Adaptador PWA conserva null, distingue partial/unavailable, identifica source y preserva timestamp de generación antes de coalescer. SensorCapture permanece separado y sin reescritura.

Mismo IPC loopback: handshake opt-in phoneInputVersion=1, batch latest-only en PONG, mismo prefijo/framing4096/deadlines/EOF. No segundo listener/writer/torneo. Java4 slots acotados; Unity PhoneInputStore consulta latest/age/freshness/state/capabilities/quality/sequence y TryPhoneInput real. Flags de disponibilidad impiden presentar ejes ausentes como ceros válidos. Euler raw se preserva, Quaternion no se inventa.

## Evidencia nueva

Carpeta [mobile-input-runtime-2026-10-07](evidence/mobile-input-runtime-2026-10-07/). Las ejecuciones anteriores y fallidas se conservan; final serial se identifica explícitamente al cerrar.

- end-to-end01: primera cadena real, antes de ampliación del gate.
- end-to-end02: QR PNG/decoder/join real; cuatro peers/IDs; 30/50/60Hz N60 por cliente/carga; ráfaga500/coalesced498; capture DOM sintético, suspend/resume; 17 casos negativos/lifecycle; reconnect mismoPlayer/epochnuevo; 630 observaciones Unity; sin crossover; STOPPED/Unity0/Java0/residual0.
- end-to-end03: FAIL/deadline sin READY durante ejecución simultánea con la suite sostenida; coherente con singleton ocupado. No es PASS ni reemplaza end-to-end02.
- end-to-end04: **PASS final del runtime**, cuatro host/host UDP pairs, sin STUN/TURN, 18 casos negativos/lifecycle; replay real del fixture, TOUCH fiable, capture DOM sintético, burst500/coalesced498 y presión400 (209 ACK /191 rechazos). Java recibe961 actualizaciones técnicas, forward643/coalesced318, pending0/rejected202; cero peers y Java propios residuales, STOPPED/exit0/no force. Los contadores incluyen presión y control, no sólo motion nominal.
- Android variante nueva: **BLOCKED**, API36/Chrome133, contexto seguro y API RTC/motion presentes, QR decodificado. Los canales RTC no llegaron OPEN antes del deadline. Causa de conectividad/NAT/mDNS no establecida; no se alteró red ni se usaron flags. Player/Java/Emulator/ADB exit0, propios/helpers residuales0. No sustituye histórico16/16.

Los valores anteriores corresponden a sus fuentes de ese momento. Gate final y hashes se publican por separado, sin mezclar corridas ni excluir muestras para obtener PASS.

## Regresión y reproducibilidad

[Comando/entorno](../tools/spikes/webrtc/RUNTIME.md), [contrato/corpus](../Shared/Protocol/mobile/README.md). Java, Node/React/capture/adapter, EditMode, PlayMode real, build Linux, Player2A/2B once grupos y Player2C tres corridas independientes N1000 +>=600s se registran en el manifiesto final. El comparador RTC cerrado **no se reejecuta**: los tests RTC del adaptador runtime son el gate actual.

Los intentos fallidos se conservan: primera suite Java46 con1error por colisión de opción HTTPS/RTC, corregida; PlayMode ruta Java inexistente en Flatpak, después symlinks de seguridad inaccesibles, después singleton ocupado al intentar suites paralelas. No se cambian presupuestos ni tests legacy para obtener PASS. Copia temporal del JDK existente resuelve sólo el entorno de test y suites administradas se ejecutan serialmente.

## Límites y deuda

- Browser Chrome real con cuatro perfiles es evidencia LAB sintética/replay, **no Android físico ni iPhone**. Android Emulator en este nuevo runtime BLOCKED por conexión RTC; background/foreground móvil NOT RUN. Histórico Android16/16 conserva alcance original. Físicos y Windows DEFERRED.
- localhost HTTP es contexto seguro del laboratorio, no solución de producto para teléfono nuevo. Señalización confiable/origen HTTPS sin configuración y autenticación del fingerprint en LAN física permanecen deuda independiente.
- Clock UTC del cliente debe ser compatible (age2000ms/future250ms). Medición software misma PC no demuestra sincronización física/cámara ni human latency/motion-to-photon.
- Native RTC enumera interfaces; no se certifica política de NIC/puertos en router arbitrario, multicast/mDNS/LNA/aislamiento. Ninguna regla de firewall/router/DNS/trust modificada por este incremento.
- Límites de aplicación verificables, sin afirmar ausencia global de fugas/colas nativas. RSS de 2C es observación. Notices JNI/transitivos y Windows requieren auditoría de distribución futura.
- El endpoint operador/admission es sólo loopback; el perfil HTTPS LAN anterior no se convierte automáticamente en onboarding RTC de teléfono. Esa integración necesita origen/señalización confiables y autorización futura; no se declara validada.
- PWA laboratorio/adapter no constituye UX final/onboarding universal. Flutter futuro reutilizará contrato; no implementado.

Siguiente incremento único recomendado al cerrar este gate: PC Webcam → CameraInput → Player Lock → temporal alignment → Input Fusion infrastructure. **No iniciado.** Fase0 IN PROGRESS, Fase1 NOT STARTED.

## Resultado serial del candidato fijo

Java48/48 (LAN explícita,0 SKIP); Node/React/capture/adapter18/18; EditMode23/23; PlayMode12/12 con Java real; build Linux0 errores/0 warnings; Player2A/2B11 grupos PASS. Corpus móvil compartido17 casos + escenarios unitarios secuencia/rate/aislamiento; corpus IPC anterior71 casos conserva sus regresiones. [Componentes](evidence/mobile-input-runtime-2026-10-07/component-tests.json), [hashes126 fuentes y artefactos](evidence/mobile-input-runtime-2026-10-07/candidate-hashes.json).

[Tramos medidos](evidence/mobile-input-runtime-2026-10-07/segment-metrics.json): 643 observaciones Unity,636 muestras ACTIVE únicas con aceleración presente (incluye replay/presión). Java→Unity P50=11/P95=23/max29ms; generación cliente→Unity P50=13/P95=25/max30ms. UTC de misma PC, resolución1ms; no se mide latencia humana ni motion-to-photon. RTT browser→Java mantiene12 grupos independientes a30/50/60Hz, cada uno con60 MOTION_SAMPLE y recibos adicionales de heartbeat (conteos reales por grupo en JSON); P95 de cada grupo entre2.7–5.3ms, máximo individual9.4ms, recibos nominales faltantes0/errores0. No se calcula percentil combinando grupos.

Startup hasta HTTP_READY desde lanzamiento Player:5265.13ms en un lanzamiento, no percentil de startup ni tiempo total de incorporación. Cadencias son cargas sintéticas, no requisitos de hardware. Sobrescrituras latest-only son intencionales, no estimación de pérdida de paquetes.

Primera regresión2C: tres N1000 PASS, pero cierre de estabilidad FAIL/forzado137 tras≥600s; evidencia [attempt01](evidence/mobile-input-runtime-2026-10-07/ipc-regression-attempt01.json). Se había reconstruido el JAR en esa corrida. La repetición con JAR fijo pasó: **601.2234171s**,601 enviados/recibidos/válidos,600 muestras después del primer intercambio,0 errores/timeouts, cierre68.9671ms,STOPPED/exit0/no force/propios residuales0. Fallo anterior conservado, no reproducido con artefacto fijo; causa definitiva no establecida.

### Regresión2C final — corridas independientes

| Corrida | Startup ms | N | sent/received/valid | RTT min /P50 /P95 /max ms | Duration s | effective Hz | late ticks | shutdown ms | errores/timeouts |
|---|---:|---:|---|---|---:|---:|---:|---:|---|
| 1 | 2047.0593 | 1000 | 1100/1100/1100 | 0.4540/1.0951/1.6423/4.8207 | 21.9873806 | 49.9996679 | 203 | 69.1765 | 0/0 |
| 2 | 1532.4179 | 1000 | 1100/1100/1100 | 0.3852/1.0853/1.6477/3.9184 | 21.9877400 | 49.9999732 | 226 | 92.2208 | 0/0 |
| 3 | 2068.1565 | 1000 | 1100/1100/1100 | 0.3444/1.0777/1.6583/3.7717 | 21.9846093 | 50.0002720 | 207 | 82.2050 | 0/0 |

100 warmup por corrida, publicados separadamente, sin combinar percentiles ni declarar startup P95 con tres lanzamientos. Presupuestos IPC existentes satisfechos, sin modificar umbrales/excluir muestras. RSS observado sin señal de investigación al cierre, sin afirmar ausencia de fugas globales. [Resultados completos](evidence/mobile-input-runtime-2026-10-07/ipc-2c-regression-final.json).

## Estado al detenerse

DEC-010 WebRTC ACCEPTED / IMPLEMENTED FOR PHASE0 LAB. Session/Admission PASS; QR POC PASS; Gorilla Protocol PASS; input móvil de laboratorio→Unity PASS; integración1–4 clientes PASS. Camera NOT STARTED; Input Fusion NOT STARTED. Foundation y2A/2B/2C conservan PASS/MERGED históricos. Fase0 IN PROGRESS;3A IN PROGRESS;Fase1 NOT STARTED. iPhone/Android físicos/Windows DEFERRED. Emulador RTC nuevo BLOCKED, background/foreground móvil NOT RUN.

Git: rama existente feature/mobile-phase0-lan-https-3a,HEAD0d940f77f533bb6780bc3f07169dce414ea21e54. PR94 OPEN/DRAFT verificado, base develop ec5880c94304e8c7d587c5f5d8c2cf28dbf5a760. Sin rama nueva,commit,push,merge ni cambios remotos. Cambios locales previos conservados y checkpoint privado previo a implementación.
