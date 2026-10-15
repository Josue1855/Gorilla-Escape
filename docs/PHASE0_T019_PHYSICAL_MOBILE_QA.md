# T019 — physical mobile QA and lab preparation

2026-10-13, America/Chihuahua. Current dependencies #22/#27/#30/#32 CLOSED/completed; #33 OPEN/Validation/needs-device-test/performance. Original AC unchanged. DEC-028 transfers physical clocks/Hz/E2E here; DEC-027 human tracking debt is outside this gate, Post-MVP #106 and NOT RUN.

## Available evidence / inventory

[Device inventory](evidence/overnight-2026-10-13/device-inventory.json): ADB zero devices; USB only reference integrated webcam/nonphone peripherals. Historical iPhone15 reference does not prove current access. Real LAN interface on PC available; router identity/current compatibility UNKNOWN. No physical Android/iPhone test executed this run. No network scan or external device access attempted.

[T019 lab evidence](evidence/overnight-2026-10-13/t019-lab.json): five independent Player sessions, four simultaneous host Chrome RTC clients each; 20/20 initial connections and 20/20 reconnects, 100% LAB. Correct player/device association and epoch increment; synthetic motion reaches Unity; clean Player/owned-Java closure. No physical acquisition/emission Hz derived from these synthetic inputs. QR trust/unprepared human duration NOT RUN.

## Matrix

| AC | Requirement/test | Device | Can automate? | Executed? | LAB/PHYSICAL | N | Result | Evidence | Limitations |
|---|---|---|---|---|---|---|---|---|---|
| AC1 | 1 physical control | Phone LAN | Some portions; physical interaction/independent timing require available devices | No physical execution | PHYSICAL required | 0 physical | NOT RUN | Inventory + existing carry-forward | LAB cannot substitute this gate |
| AC1 | 4 simultaneous physical controls | 4 phones LAN; include iPhone Safari and Android Chrome | Some portions; physical interaction/independent timing require available devices | No physical execution | PHYSICAL required | 0 physical | NOT RUN | Inventory + existing carry-forward | LAB cannot substitute this gate |
| AC1 | 20 initial connections >=95% | 4 physical phones, external observation | Some portions; physical interaction/independent timing require available devices | No physical execution | PHYSICAL required | 0 physical | NOT RUN | Inventory + existing carry-forward | LAB cannot substitute this gate |
| AC1 | 20 reconnections >=95% | 4 physical phones, external observation | Some portions; physical interaction/independent timing require available devices | No physical execution | PHYSICAL required | 0 physical | NOT RUN | Inventory + existing carry-forward | LAB cannot substitute this gate |
| RNF-GOR-02 | QR/JOIN on genuinely unprepared phone <60s | Unconfigured phone; full PREPARE+JOIN timing | Some portions; physical interaction/independent timing require available devices | No physical execution | PHYSICAL required | 0 physical | NOT RUN | Inventory + existing carry-forward | LAB cannot substitute this gate |
| T008 | Prepared-device QR-visible→technical READY | Prepared phone; separate external timer | Some portions; physical interaction/independent timing require available devices | No physical execution | PHYSICAL required | 0 physical | NOT RUN | Inventory + existing carry-forward | LAB cannot substitute this gate |
| T008 | Certificate trust / secure-context / exact OS/browser | iPhone + Android actual versions | Some portions; physical interaction/independent timing require available devices | No physical execution | PHYSICAL required | 0 physical | NOT RUN | Inventory + existing carry-forward | LAB cannot substitute this gate |
| T008/T011 | LAN, WAN unavailable, cold-cache first-load | Real router LAN; WAN loss safely isolated for test devices | Some portions; physical interaction/independent timing require available devices | No physical execution | PHYSICAL required | 0 physical | NOT RUN | Inventory + existing carry-forward | LAB cannot substitute this gate |
| T010 | motion/orientation actualMeasuredHz and emissionHz | REST>=15s/GENTLE>=25s on both phone platforms | Some portions; physical interaction/independent timing require available devices | No physical execution | PHYSICAL required | 0 physical | NOT RUN | Inventory + existing carry-forward | LAB cannot substitute this gate |
| T010 | stationary freshness; alpha/beta/gamma; sensor unavailable | Real sensors/permission UI; no injected events | Some portions; physical interaction/independent timing require available devices | No physical execution | PHYSICAL required | 0 physical | NOT RUN | Inventory + existing carry-forward | LAB cannot substitute this gate |
| AC2/T011 | background→foreground short x20 | Physical browser, freshness/recovery | Some portions; physical interaction/independent timing require available devices | No physical execution | PHYSICAL required | 0 physical | NOT RUN | Inventory + existing carry-forward | LAB cannot substitute this gate |
| AC2/T011 | long suspension/throttling | Real OS/browser | Some portions; physical interaction/independent timing require available devices | No physical execution | PHYSICAL required | 0 physical | NOT RUN | Inventory + existing carry-forward | LAB cannot substitute this gate |
| AC2 | lock→unlock | Phone physical lockscreen | Some portions; physical interaction/independent timing require available devices | No physical execution | PHYSICAL required | 0 physical | NOT RUN | Inventory + existing carry-forward | LAB cannot substitute this gate |
| AC2 | permission denied | Real permission prompt | Some portions; physical interaction/independent timing require available devices | No physical execution | PHYSICAL required | 0 physical | NOT RUN | Inventory + existing carry-forward | LAB cannot substitute this gate |
| AC2 | network loss / return | Real Wi-Fi controlled loss | Some portions; physical interaction/independent timing require available devices | No physical execution | PHYSICAL required | 0 physical | NOT RUN | Inventory + existing carry-forward | LAB cannot substitute this gate |
| AC2 | camera loss | Reference webcam; no human tracking | Some portions; physical interaction/independent timing require available devices | No physical execution | PHYSICAL required | 0 physical | NOT RUN | Inventory + existing carry-forward | LAB cannot substitute this gate |
| AC2 | RTC close / reconnect | Physical browser RTC; association/new epoch | Some portions; physical interaction/independent timing require available devices | No physical execution | PHYSICAL required | 0 physical | NOT RUN | Inventory + existing carry-forward | LAB cannot substitute this gate |
| AC2 | invalid packets, old epoch, stale/duplicate input | Existing software corpus; physical transport carry-forward | Some portions; physical interaction/independent timing require available devices | No physical execution | PHYSICAL required | 0 physical | NOT RUN | Inventory + existing carry-forward | LAB cannot substitute this gate |
| AC2 | Java killed / Unity shutdown / no invalid scoring | Technical process lifecycle; full tournament scoring later | Some portions; physical interaction/independent timing require available devices | No physical execution | PHYSICAL required | 0 physical | NOT RUN | Inventory + existing carry-forward | LAB cannot substitute this gate |
| AC3/T018 | phone↔PC offset/drift/uncertainty | Physical phone browser UTC/mono and PC ACK UTC | Some portions; physical interaction/independent timing require available devices | No physical execution | PHYSICAL required | 0 physical | NOT RUN | Inventory + existing carry-forward | LAB cannot substitute this gate |
| AC3/RNF-GOR-01 | action→visible-response P95<150ms | Presentation hardware + independently measured source/visible boundary | Some portions; physical interaction/independent timing require available devices | No physical execution | PHYSICAL required | 0 physical | NOT RUN | Inventory + existing carry-forward | LAB cannot substitute this gate |

## Reproduce lab

Build PWA then Server, use actual compiled Linux Player. Supply installed Playwright module and Chrome via GORILLA_TEST_PLAYWRIGHT / GORILLA_TEST_CHROME. `node tools/t019/lab.mjs <new-output.json> <player> <java> <jar>` creates five owned sessions, four RTC clients each; no tokens/SDP/sensor traces persisted. `node --test tools/t019/clock.test.mjs` checks asymmetric-path offset intervals, clock discontinuities, finite inputs and bounds. `python3 tools/t019/inventory.py --adb <adb> --output <new-output.json>` inspects only attached authorized devices without retaining serials.

## Clock algorithm and limits

32 heartbeat ACKs per session at >=50ms spacing (N160 overall); client records Date.now Unixms and performance.now monotonicms immediately around send/ACK; ACK serverReceiveTimestamp is Java UTCms. Let c0/c3 be client UTC bracket and s server receive UTC: offset server−client lies in [s−c3−2ms, s−c0+2ms], assuming clocks stable during bracket and correctly ordered transport. Two ms margin covers integer timestamp quantization; scheduling is inside bracket, path symmetry is not assumed. Wall/mono elapsed disagreement>4ms is rejected, not corrected. Pick narrowest bracket and publish midpoint±halfwidth; sample count max4096. First/last half-run best brackets give a descriptive drift interval and slope uncertainty, not a future clock guarantee. Physical phone protocol freshness/tolerance is not altered; a phone rejected before ACK cannot be synchronized by this method.

Current results are **same-host LAB**; physical phone offset/drift/error **UNKNOWN/NOT RUN**. ACK RTT includes return path; it is not one-way transport, motion→gameplay or response on display. Unity input receipt is the furthest automatic boundary accredited here; no official sport result or visible-response timestamp/optical boundary exists in this Phase0 evidence. <150ms visible RNF is NOT RUN. No latency fabricated by summing segments.

## Physical execution tomorrow

1. Provide/connect four physical phones on the presentation LAN, including an iPhone/Safari and Android/Chrome. Record actual model/OS/browser; preserve one genuinely unprepared phone for RNF-GOR-02. Historical reference names are not current hardware availability.
2. Follow the existing T008 local trust/onboarding guide with installation-private CA key retained on PC; never distribute the private key. Time complete unprepared PREPARE+QR/JOIN separately from prepared QR→READY. No TLS bypass or preventive firewall range change.
3. Repeat20 connections/20 reconnects; record successes/attempts/errors; >=19/20 required per category (>=95%). Verify1/4 simultaneous association. Execute failure and sensor/lifecycle rows above; no human PlayerLock tests. Store aggregate counts/rates/times only.
4. Measure physical source→visible response with an independent method and record clock uncertainty/boundaries; do not treat ACK/IPC receipt as display. If the desired gate specifically requires a sport result, Phase0 has no gameplay and Phase1 remains prohibited until E0: request PO clarification of that boundary; do not weaken/close T019 automatically.
5. Commit reproducible aggregate evidence through a reviewed/check-passing PR; close T019 only if original AC and physical carry-forward really pass. Then run T020 on second clean physical PC.

The no-physical-device limitation does not prevent software failure validation: full Unity PlayMode20/20 and Java76 executed/2historical skips cover owned Java death, IPC loss, retry bounds, duplicate READY/PONG, stale/duplicate/reordered inputs, action cancellation/idempotency and cleanup. PWA59/59 covers permissions/capture quality/lifecycle using fixtures. These do not certify physical OS lockscreen, Wi-Fi or invalid tournament scoring in games that do not yet exist.

Clock precision clarification: the ±2ms margin is integer quantization only, a conditional model assumption for these same-host LAB brackets. It does not bound browser privacy coarsening, UTC system accuracy, future slew or physical total error. Before physical interpretation, record actual clock precision/coarsening and uncertainty; unknown precision remains UNKNOWN.

Stable-candidate reference camera/Unity/Java soak>=30min PASS; IPC-only>=30min +20startup/shutdown PASS. These do not accredit physical phone sensor Hz/clock/response latency/QR. Full actual metrics and prior startup failures retained in Final Exit Report. Emulator reattempt reached first/second cold-cache local loads and pagehide cleanup, then failed short-suspension browser deadline; no20emulator-cycle PASS claimed.

## Final emulator/lifecycle result — 2026-10-14

[Final actual emulator report](evidence/overnight-2026-10-13/android-offline-final.json): **PASS**, disposable AndroidAPI36/Chrome133.0.6943.137; cold-cache first/second local load with WAN unavailable, pagehide listeners/timers/transport cleanup, **20/20 actual emulator HOME→Chrome cycles**, zero hidden emission in each cycle, exactly one listener/channel on return, fresh callbacks before INPUT_READY, long-suspension/RTC-loss safe DISCONNECTED and no automatic reconnect. Guest networking restored, own emulator/ADB/Java/Player residuals0, no physical input observed. ADB reverse localhost is LAB/EMULATOR, not physical LAN or certificate trust.

Earlier Chrome-first-run BLOCKED and two subsequent QA timeouts are preserved; final PASS does not erase them. Private HOME/cache/data profile added to owned test Player to isolate concurrent reference runs; loop now records target/attempt/success, and last requested UI control for failure diagnosis. Existing deterministic PWA repeated hide/show regression increased8→20cycles; suite59/59PASS, no product runtime change. [Validation/source hashes](evidence/overnight-2026-10-13/final-lifecycle-validation.json).

Physical mobile sensorHz/clock/E2E/QR<60s and cleanPC remain NOT RUN. T019/T020/F05/E0 OPEN. No ownership assignment or gameplay.


## T062 — Main Menu y Centro de Pruebas, 2026-10-14

DEC-029: fundación real Bootstrap→MainMenu→PRUEBAS, JUGAR deshabilitado; preview QA local/memoria desde captura T016 única, QR productivo, sensores/slots/readiness, indicador técnico y agregados. [Arquitectura/evidencia](PHASE0_MAIN_MENU_QA_CENTER.md) y [runbook iPhone15 sin Editor](PHASE0_IPHONE15_LIVE_QA_RUNBOOK.md). No gameplay ni cierre F41. **T019 OPEN / Validation; T020/F05/E0 OPEN; Final Exit BLOCKED; Phase1 NOT STARTED.** Physical iPhone/four-phone evidence NOT RUN por esta implementación. Resultados técnicos se registran en la documentación de T062; ningún mock acredita PASS físico.

### Preparación de software overnight 2026-10-14

T064/#115 elimina stop-and-wait de motion (reproducción0→2000ms con ACK perdido; bounded latest-only). T065/#116 permite navegación desde Bootstrap en Editor sin procesos físicos por defecto. T063/#114 agrega AUTO/Cabaña/Cloudflared opcional, con Cabaña canónica para completar íntegramente este protocolo. Ningún AC de T019 cambia: cuatro teléfonos,20 connects,20 reconnects,>=95%,P95,fallos y medición física motion→respuesta visual siguen pendientes. [Integración y clasificación de evidencia](PHASE0_OVERNIGHT_INTEGRATION_REPORT_2026-10-14.md). LAB con clientes sintéticos y caídas de túnel no es evidencia física ni sustituye T020.
