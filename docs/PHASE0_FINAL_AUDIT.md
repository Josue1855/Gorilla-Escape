# FINAL PHASE 0 AUDIT — 2026-10-08

**PHASE 0 FINAL AUDIT: PASS — SOFTWARE/LAB**

**Fase 0 — Base/Spike: COMPLETED — SOFTWARE/LAB**

**Fase 1 — Gorilla Smash Vertical Slice: READY TO START**

Deferred physical/platform validation remains open and is required before final MVP acceptance.
Fase 1 no fue iniciada. El cierre corresponde al candidato local integrado y probado en laboratorio,
no declara integración Git de los cambios nuevos ni aceptación del producto en teléfonos físicos.
**3A product/physical onboarding = DEFERRED / IN PROGRESS. PR #94 = OPEN/DRAFT.**

## Alcance y candidato

Auditoría autorizada por el PO: verificar código/evidencia, repetir regresiones necesarias, clasificar y
cerrar Software/Lab. Ninguna modificación de código de producto, dependencia, threshold, red o infraestructura.
No nuevo transporte, Android fix, pruebas físicas, optimización de cámara ni gameplay.

Rama conservada: `feature/mobile-phase0-lan-https-3a`.
HEAD y remoto #94: `0d940f77f533bb6780bc3f07169dce414ea21e54`.
`origin/develop`, obtenido de nuevo: `ec5880c94304e8c7d587c5f5d8c2cf28dbf5a760`.
**local candidate != remote PR head**: el runtime móvil y cámara/fusión, junto con esta auditoría, siguen
como cambios locales sin commit. No rama nueva, commit, push, merge ni actualización de Issues/Project.
Los dos checks remotos PASS acreditan sólo ese HEAD publicado; las pruebas locales acreditan el candidato actual.

Se conservó un checkpoint privado de los archivos locales previos; se registraron estado Git, hashes
de 145 artefactos históricos y 187 fuentes del candidato anterior. Ningún artefacto histórico cambió.
Las diferencias de esta auditoría son documentación y evidencia; no cambia la semántica ejecutada.
[Git](evidence/phase0-final-audit-2026-10-08/git-state.json),
[GitHub read-only](evidence/phase0-final-audit-2026-10-08/github-readonly.json),
[checks remotos](evidence/phase0-final-audit-2026-10-08/remote-checks.json),
[hashes](evidence/phase0-final-audit-2026-10-08/candidate-hashes.json).

## Definition of Done Software/Lab

PASS significa Software/Lab salvo los alcances históricos MERGED explícitos. Cada fila tiene código
inspeccionado y evidencia; no se infiere aprobación física ni integración remota del nuevo runtime.

| Gate | Estado | Evidencia |
|---|---|---|
| Foundation | PASS / MERGED | PR90, ancestry/tree verificados; foundation validator y EditMode |
| Unity base | PASS | EditMode64/64, PlayMode16/16, Linux build0/0; test-summary.json |
| Java base | PASS | Java48/48; HTTP/TLS/config/contratos/procesos reales; test-summary.json |
| PWA base | PASS | Node18/18, build; 7/7 recursos dist idénticos en JAR; packaged-pwa-verification.json |
| IPC 2A | PASS / MERGED | PR91 ancestry/tree; Player smoke y EOF; ipc-2a-2b-regression.json |
| IPC 2B | PASS / MERGED | PR92 ancestry/tree; 11 grupos Player, lifecycle/timeout/singleton/recovery; ipc-2a-2b-regression.json |
| IPC 2C | PASS / MERGED; RETAINED PREVIOUS EVIDENCE | PR93; corpus71/deadlines/presión repetidos Java/PlayMode; 3×N1000 y601.2234171s retenidos, retention-verification.json |
| WebRTC / DEC-010 | PASS | Accepted for Phase0 architecture; cuatro host/host UDP peers sin STUN/TURN; mobile-runtime.json |
| Session / Admission | PASS | TechnicalSessionTest + admission real, expiración/identidad/replay/resume; test-summary.json y mobile-runtime.json |
| QR | PASS | PNG → ZXing decode → JOIN real; JoinQrPocTest y mobile-runtime.json |
| Gorilla Protocol | PASS | Corpus17 + validación/versiones/tipos/canales/identidad/secuencia/rate; Java tests y18 casos negativos/lifecycle |
| Mobile → Unity | PASS | PWA adapter → RTC → Java → mismo IPC → PhoneInputStore; mobile-runtime.json unityGate PASS |
| 1–4 clients | PASS | IDs1–4 sin crossover, rechazo quinto, desconexión aislada; mobile-runtime.json |
| CameraInput | PASS | Corpus12, child real, buffers/validación, replay Player; Edit/Play y camera-fusion.json |
| Player Lock | PASS | READY/turnos1–4, LOST/recovery, ambigüedad/aislamiento; EditMode y camera-fusion.json |
| Temporal Alignment | PASS | Corpus21 escenarios, cadencias/edad/offset/duplicados; proxy PC receipt/read-complete; EditMode y camera-fusion.json |
| Input Fusion Infra | PASS | Raw/quality/ausencia/freshness, ocho trazas conceptuales sin acción deportiva; EditMode y camera-fusion.json |
| Resilience | PASS | Java/PlayMode/Player11 grupos; mobile18 negativos/lifecycle; vision failure/unavailable previos retenidos |
| Cleanup | PASS | Unity/Java/vision propios0, peers0, test servers0, puertos de prueba liberados; cleanup.json |
| Regression | PASS | Suites nuevas, build, mobile/camera Player; benchmarks2C retenidos con hashes, sin mezcla de corridas |
| Documentation | PASS | v4/decisiones/roadmap/trazabilidad/README reconciliados; management/foundation/diff; manifest/evidence-review |

Referencias de la tabla relativas a [evidencia de auditoría](evidence/phase0-final-audit-2026-10-08/).
[Gate estructurado](evidence/phase0-final-audit-2026-10-08/gate-summary.json).

## Arquitectura inspeccionada

```text
PWA/client → WebRTC DataChannel → Java21 → Gorilla Protocol validation
           → IPC 127.0.0.1 → Unity → PhoneInput
PC webcam → OpenCV/MediaPipe local → CameraInput → Unity
PhoneInput + CameraInput → Player Lock → Temporal Alignment → Fusion infrastructure
```

**DEC-010 Mobile Transport: WebRTC DataChannel. Status: Accepted for Phase 0 architecture.**
Señalización local, sin STUN/TURN público obligatorio, signaling cloud ni backend cloud de gameplay.
WebTransport conserva sus Spikes/evidencia histórica; no es un transporte productivo paralelo.
La entrada deportiva anterior con el mismo número DEC-010 queda identificada como uso histórico del ID.

Unity es la única autoridad de gameplay, física, animación, scoring, resultados e interpretación final
del input. Java sólo transporte/sesión técnica/admission/validación/routing/lifecycle.
El mismo child Java y listener IPC loopback se reutilizan. Cámara de gameplay exclusivamente PC,
private pipes del adaptador supervisado por Unity; no vídeo móvil ni segundo backend.

Cuatro controles con slots latest-only; un jugador cámara-activo por turno. Historias16 frames/32 motion
por jugador, no cola de imágenes. Asociación técnica de subject efímero, sin identidad biométrica.
El adaptador Python es opt-in Linux lab, no una decisión definitiva de distribución del producto.
[Inventario de fronteras inspeccionadas](evidence/phase0-final-audit-2026-10-08/architecture-review.json).

No implementados: Gorilla Smash, hammer physics, scoring, Power final, detector deportivo final,
animaciones/feedback final, Gorilla Points, torneo u otros minijuegos. Los campos Power/Smash/Throw de
PlayerMotion/PlayerInput son declaraciones de contrato anteriores sin productor ni consumidor deportivo.
FusionFrame describe asociación/calidad/alineación, nunca confirma una acción jugable.

## Ejecución nueva frente a evidencia retenida

| Ejecución nueva del audit | Resultado |
|---|---|
| Java21 Maven tests, LAN explícita | 48/48 PASS,0 FAIL/ERROR/SKIP |
| PWA/capture/adapter Node | 18/18 PASS,0 FAIL/SKIP |
| Unity6000.3.23f1 EditMode | 64/64 PASS,0 FAIL/SKIP |
| Unity PlayMode, Java/child visión fixtures reales | 16/16 PASS,0 FAIL/SKIP |
| Linux Mono Development build | PASS,0 errores/0 warnings de build |
| PWA build | PASS; 7/7 assets idénticos al JAR existente |
| Player IPC2A/2B | 11 grupos PASS; propios residuales0 |
| Mobile runtime Player/Chrome | PASS; cuatro peers,18 negativos/lifecycle, unityGate PASS |
| Camera/Fusion replay Player | PASS; cuatro turnos/loss/recovery y recuperación manual phone |
| Foundation / management / diff | PASS; evidencia validations.json |

[Casos/suites](evidence/phase0-final-audit-2026-10-08/test-summary.json).
Tests de Java administrado, PlayMode y Players se ejecutaron serialmente; se respetó el FileLock.
Gstack no se encontró en los skill roots instalados; se utilizó auditoría directa proporcional, sin instalar herramientas.
Logs/XML completos quedan privados; reportes publicados contienen resultados sanitizados y hashes.
Diagnósticos conocidos del entorno Unity permanecen separados (#89); build0warnings no equivale a cero fugas.

**RETAINED PREVIOUS EVIDENCE:** Foundation/IPC integrados y benchmark2C largo del candidato móvil anterior.
JAR SHA256 `8cc8f015a749e76548c9251ca6d4431d1bb9a44961b5d7d9c6ff2ed932ec4a22` idéntico;
codec, writer/deadlines, managed lifecycle y PhoneInputStore/PhoneInput sin cambios materiales.
El getter readonly y camera runner opt-in no modifican el transporte. Corpus, presión y procesos se regresaron
de nuevo; no se reconstruyó ni reemplazó el JAR durante Players.
Se retienen también unavailable/failure Player de cámara con fuentes/runtime iguales y exit2 esperado,
smoke webcam físico y Android histórico, sin repetir ensayos físicos ni el comparador RTC cerrado.
[Justificación de retención](evidence/phase0-final-audit-2026-10-08/retention-verification.json).

## Métricas independientes

No hay P95 global, startup P95 de tres launches, física de gameplay, motion-to-photon ni precisión de gesto.
Cada dataset y corrida se conserva por separado en [independent-metrics.json](evidence/phase0-final-audit-2026-10-08/independent-metrics.json).

### IPC2C retenido: tres corridas independientes

| Run | Startup ms | N | sent/received/valid | RTT min/P50/P95/max ms | Duration s | Hz | late ticks | shutdown ms | errores/timeouts |
|---|---:|---:|---|---|---:|---:|---:|---:|---|
| 1 | 2047.0593 | 1000 | 1100/1100/1100 | .4540/1.0951/1.6423/4.8207 | 21.9873806 | 49.9996679 | 203 | 69.1765 | 0/0 |
| 2 | 1532.4179 | 1000 | 1100/1100/1100 | .3852/1.0853/1.6477/3.9184 | 21.9877400 | 49.9999732 | 226 | 92.2208 | 0/0 |
| 3 | 2068.1565 | 1000 | 1100/1100/1100 | .3444/1.0777/1.6583/3.7717 | 21.9846093 | 50.0002720 | 207 | 82.2050 | 0/0 |

100 warmup por corrida. Estabilidad retenida601.2234171s,601 enviados/recibidos/válidos,600 RTT tras handshake,
errores/timeouts0; STOPPED/exit0/shutdown68.9671ms y propios residuales0. RSS observación, managed heap
NOT MEASURED; no certificación global de leaks. La corrida original integrada de 2C601.24s permanece en
su carpeta histórica independiente, no se combina con esta regresión.

### Mobile runtime retenido

end-to-end04: 12 grupos RTC browser→Java a30/50/60Hz, N60 motion por cliente/grupo más heartbeat;
P95 por grupo2.7–5.3ms, máximo individual9.4ms; nominales faltantes/errores0.
UTC de misma PC: N636 motion ACTIVE únicas (incluye replay/presión), Java→Unity P50/P9511/23ms,max29;
generación cliente→Unity13/25ms,max30. Startup5265.13ms de un lanzamiento, no percentil.
No inferir frecuencia hardware ni latencia humana.

### Camera/Fusion: replay y físico separados

Replay04 retenido: 329 producidos/326 store,29.869775FPS; procesamiento fixture P50/P95.137470/.349317ms;
delta N48 P50/P959/15ms,max15; process→Unity N32610/42ms,max210.
Nuevo audit replay: 294 producidos/290 store,29.867482FPS; procesamiento fixture .123781/.350066ms;
40 Eligible, delta3/7ms,max11; process→Unity N2909/34ms,max233.
Overwrites/latest-only y evaluaciones incluyen startup, turnos/plazas inactivas, lost/low-confidence,
desconexión y shutdown: no son errores nominales de transporte ni gestos únicos.

Physical01 retenido exactamente: **capture → MediaPipe → Unity = technical PASS**,640×480 observados,
**17.61FPS observados**, reliable human tracking **NOT RUN**, target30FPS **NOT ACHIEVED in smoke**.
102 frames/100 store; procesamiento39.341077/65.884074ms, read7.655495/36.538060ms;
process→Unity N10010/157ms,max402. Cero frames fiables con threshold .6lab; no validación física de
CameraInput/Player Lock/Fusion. Physical-adapter01 anterior mantiene su timing combinado read+process,
sin reinterpretarlo como inferencia aislada. Exposición y dropped frames hardware desconocidos.

## PHYSICAL / PLATFORM VALIDATION DEBT

| Validación | Estado |
|---|---|
| iPhone physical | DEFERRED |
| Android physical | DEFERRED |
| Android Emulator RTC nuevo | BLOCKED |
| Windows | DEFERRED |
| human motion validation | DEFERRED |
| physical phone + camera fusion | DEFERRED |
| physical clock synchronization | DEFERRED |
| final threshold tuning | DEFERRED |
| product HTTPS/onboarding | DEFERRED |
| distribution packaging/licenses | DEFERRED |
| physical webcam human tracking reliable | NOT RUN |
| physical webcam30FPS target | NOT ACHIEVED in smoke |

**DO NOT BLOCK Phase0 Software/Lab closure. MUST BLOCK final MVP physical acceptance where relevant.**
Android histórico: PASS within historical scope; originales10 PASS/2 BLOCKED/1 SKIP y posterior16 PASS
se conservan sin reinterpretación. El nuevo RTC Android BLOCKED no se corrige ni reemplaza aquí.
No secure-context/onboarding universal acreditado; localhost lab no es HTTPS confiable de un teléfono nuevo.
RTC local de laboratorio no demuestra conectividad en cualquier router ni paridad Safari/Android físicos.
La alineación usa PC receipt/read-complete proxies, no clocks de adquisición sincronizados físicamente.

## Evidencia, cleanup y coherencia documental

145 artefactos históricos hash-idénticos. FAIL previos siguen conservados. e2e-vision-failure01 conservó
PASS de aislamiento pero exit134: su companion attempt-failure01-review acredita **FAIL** de salida;
para acceptance se usa failure03 exit2 esperado. Intentos descartados no acreditan gates.
No mezclar las corridas independientes ni publicar secretos/tokens reutilizables/claves privadas,
vídeo físico o landmarks corporales físicos. Fixtures detallados son sintéticos/replay.
[Revisión](evidence/phase0-final-audit-2026-10-08/evidence-review.json),
[manifest](evidence/phase0-final-audit-2026-10-08/manifest.json).

Cleanup final verificado: Unity/Java/vision propios0, RTC peers0, test servers0; puertos TCP de pruebas
liberados. Cámara no se abrió en este audit; release capture/detector del smoke físico previo retenido.
No se terminó ningún observador OS preexistente. Cierre normal exit0/STOPPED de los recorridos nominales;
kill/death/fallback intencionales de resiliencia quedan separados. Logs/fixtures temporales no secretos
quedan privados/ignorados; no claves/tokens publicados. Sin cambio permanente de red/router/firewall/DNS/trust.
No afirmación global de ausencia de leaks. [Cleanup](evidence/phase0-final-audit-2026-10-08/cleanup.json).

Documentos reconciliados: DEVELOPMENT_PROGRESS, TEST_REPORT (no TEST_REPORT(2) existente), decisiones,
secure motion decision, mobile/camera validation, v4 (roadmap§50/trazabilidad§57), MONOREPO_PLAN,
backlog histórico, reglas y README pertinentes. Snapshots anteriores se etiquetan históricos/superseded;
no se eliminan ni se altera el backlog remoto. Los criterios de producto/MVP físico siguen vigentes.

El siguiente bloque de implementación sería **Fase1 — Gorilla Smash Vertical Slice**, únicamente bajo
nueva autorización del PO y con alcance/pruebas propios. Esta auditoría termina aquí; no crea rama ni código.
