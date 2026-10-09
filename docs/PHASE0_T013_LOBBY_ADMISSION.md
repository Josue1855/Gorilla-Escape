# T013 — Lobby admission and controller association

Date: 2026-10-09. Base: `10b02121bc0d2134ba03f39236d18553cbfdd058` (develop / PR #99 merge).
Branch: `feature/server-t013-lobby-admission`. Acceptance is issue-level Software/Lab, not the Phase 0 final exit certification.

## Initial audit and scope

Existing `TechnicalSession` assigns PlayerId/deviceSessionId, bounds issued admissions plus retained players to four, consumes a 30-second admission once, detects five-second liveness loss, retains resume identity for 30 seconds, rotates resume credentials and increments epoch. `MobileRuntime` already validates local RTC signaling, HELLO and mobile envelopes; the owned loopback IPC already delivers PhoneInput to Unity. None of these previously closed new admission on Unity's official start or established PLAYER_READY.

Only those gaps are implemented. There is one session, one RTC runtime and the existing IPC. No lobby screen, game selection, teams, scoring, calibration, recognizer, result store, T016 implementation or Phase 1 gameplay is added.

## Architecture and authority

Unity's `JavaProbeSupervisor(enableLobby: true).Lobby` is the explicit authority entry point. Its `SetPlayerReadyAsync(playerId, ready)` and `SetPhaseAsync("STARTING" / "STARTED")` send commands on the owned, launch-token-authenticated IPC; the caller must await an accepted receipt. Java replicates the requested state and enforces session identity, epoch, readiness and transition invariants. It never starts a game on HELLO, motion, heartbeat or a timer. The phone has no GAME_START or PLAYER_READY command. PlayerId supplied by the phone does not authorize a slot; RTC identity derives from consumed admission and the gate compares every envelope against that association.

`OPEN → STARTING → STARTED` is monotonic for a session. STARTING immediately closes both issuance and consumption of new admissions and invalidates outstanding admission tokens. STARTED retains that closure. Returning to OPEN requires ending the owned session and an explicit fresh runtime, not automatic recovery. Unity requires one to four retained players, all explicitly ready with fresh prerequisites, at either transition. Repeated requests for the current phase are safe; backward/skipped transitions are rejected.

The opt-in IPC handshake adds `lobbyVersion: 1` beside `phoneInputVersion: 1`. Subsequent PING payloads may contain one `lobbyCommand`; PONG carries a token-free `lobby` snapshot and explicit `hasLobbyResult` plus the command result when present. Extensions execute only after launch token / instance / connection / sequence validation. Legacy IPC remains compatible. A single command can be outstanding in Unity; it is cancelled on lifecycle cleanup. Snapshots are validated and copied; they cannot be mutated by consumers. `hasLobbyResult` is explicit because Unity JsonUtility instantiates missing reference fields. Frame size remains <=4096 bytes: phone batching subtracts snapshot/receipt overhead from the existing bounded budget. No second socket or unbounded queue is introduced.

## Phase 0 READY semantics

- NETWORK_READY: HELLO accepted on the owned RTC identity. Channel establishment alone does not imply PLAYER_READY.
- INPUT_READY in the lobby snapshot: NETWORK_READY plus validated MOTION_SAMPLE with at least one available raw movement group (acceleration, accelerationIncludingGravity, rotationRate or orientation), quality other than unavailable, and server monotonic receive age <2000 ms. TOUCH, heartbeat and screen orientation alone do not establish movement readiness. This is the server's raw-input prerequisite; it does not claim the PWA's independently measured acquisition cadence or sensor permissions, calibration, human motion quality or competitive readiness for a later phase.
- PLAYER_READY: explicit Unity confirmation of that exact session/player/device/epoch while OPEN or STARTING, with those fresh prerequisites. Neither NETWORK_READY nor INPUT_READY sets it automatically.

Stale/unavailable motion, CLIENT_STATE, disconnect or Unity IPC loss revokes PLAYER_READY. Resume retains PlayerId and deviceSessionId, rotates its bearer credential, advances epoch and resets network/input/player readiness. The old peer is retired before the new peer is allocated. A resumed player during STARTING must meet prerequisites and be confirmed again by Unity before STARTED. A disconnect between STARTING and STARTED prevents completion until that recovery; admission remains closed. After STARTED, resume is allowed but never fabricates lobby PLAYER_READY or a gameplay result. The existing 30-second retention window remains; expiry permits slot reuse only while OPEN, never new admission after start.

## Required cases and acceptance

| Original AC / case | Evidence | Result |
|---|---|---|
| AC1: Sesión admite 1–4 jugadores; rechazo de quinto control. | Parameterized 1–4 unit tests; four real Chrome RTC clients observed by Unity; fifth issuance and JOIN rejected | PASS LAB |
| AC2: PlayerId preservado; entrada nueva impedida después de inicio. | Issued-token and new-admission rejection at STARTING/STARTED; real post-start reconnect with same PlayerId/device and epoch+1; expired slot never reopens admission | PASS LAB |
| AC3: Identidad y READY coherentes con autoridad definida. | Explicit Unity READY/start receipts; HELLO without motion fails prerequisites; raw input alone stays unready; spoof, old epoch, stale boundary, suspend and disconnect tests | PASS LAB |
| Duplicate / expired / consumed / wrong-session admission | Deterministic monotonic-clock tests and authenticated IPC/RTC rejection | PASS |
| Wrong identity and resume replay | Wrong player/device/credential/epoch unit tests; real RTC PlayerId spoof and consumed-resume replay | PASS |
| Concurrent JOIN | 16 threads competing for capacity: exactly four unique owners; eight replays of one token: exactly one owner; 30 JOIN-vs-STARTING races | PASS |
| Cleanup and compatibility | Legacy IPC regressions; owned Unity/Java lifecycle tests; no owned Java child after cleanup; command cancellation and token-free snapshots | PASS |

## Reproducibility and evidence

The final evidence summary and sanitized real-client cases are in [evidence](evidence/t013-lobby-admission-2026-10-09/summary.json) and [live clients](evidence/t013-lobby-admission-2026-10-09/live-clients.json). Implementation commit: `b581606e6c9885b8b518a87e19d59ba94a8a801b`. The containing evidence/report commit identifies the same runtime source tree.

Java 21 full verify: 77 cases, zero failures/errors, two unrelated LAN-configuration cases skipped; final focused verify after the boundary/recovery correction: 30/30 PASS, covering 78 distinct Java cases across both runs. PWA 59/59 and build PASS; Unity EditMode 71/71, PlayMode 17/17, real RTC fixture 15/15 cases PASS. Unity Mono Linux development build Succeeded with zero errors/warnings; this is not clean-PC/T020 acceptance. Management PASS (88 records / 530 links), Foundation PASS (65 GUIDs).

Versions: Unity 6000.3.23f1 (09d2ecc7fb28); Java 21.0.12.1; Maven wrapper 3.9.11; Spring Boot 4.1.1; webrtc-java 0.19.0; Node 24.19.0; React 19.3.0; Vite 8.3.2; Chrome version recorded by the real-client harness. No dependency update.

Run `npm --prefix PWA test`, `npm --prefix PWA run build`, `JAVA_HOME=<installed-JDK21> Server/mvnw -B -f Server/pom.xml verify`, `python3 tools/github/validate_management.py` and `python3 tools/validate_foundation.py`.

Unity: use the existing licensed Flatpak launcher and an isolated copy of Assets/Packages/ProjectSettings/Shared, as documented in [foundation validation](UNITY_FOUNDATION_VALIDATION.md). Run EditMode and PlayMode serially, then `GorillaEscape.Editor.FoundationSetup.BuildDevelopmentLinux`. For PlayMode provide GORILLA_TEST_JAVA/JAR/FIXTURES and GORILLA_TEST_CAMERA_PYTHON/SCRIPT/FIXTURE (replay/fault regression only; no physical capture).

The real-lobby PlayMode test additionally requires GORILLA_TEST_NODE, optional GORILLA_TEST_NODE_ARGUMENTS, GORILLA_TEST_LOBBY_CLIENTS pointing to `tools/t013/live_clients.mjs`, GORILLA_TEST_LOBBY_OUTPUT, GORILLA_TEST_PLAYWRIGHT and GORILLA_TEST_CHROME. On this host, flatpak-spawn --host launches the existing bundled Node and host Chrome; explicitly forward the nonsecret fixture paths. The generated operator credential is transferred through stdin in memory, not process arguments or evidence. No new package/global installation or persistent sandbox change.

## Security review and limitations

Admission/resume bearer credentials remain secrets; a valid current credential is the authority to resume, not a client-selected PlayerId. Tests cover consumed/expired tokens, wrong session/identity, replay, malformed commands, invalid IPC authentication and replayed IPC sequence. Diagnostic output and snapshots omit tokens, SDP, admission URLs and native exception details. Future game-to-phone messages can use the existing bidirectional reliable control channel; SCORE_UPDATE/COMBO/ROUND_RESULT/HAPTIC_EVENT are not implemented.

Synthetic desktop RTC input proves the software route and admission semantics, not Android/iPhone physical readiness, sensors or latency. Those obligations remain in T019. No camera performance claim, official-PC packaging claim, Windows validation, physical QA, soak or final Phase 0 exit audit is made.

## Immediate T015 / F03 reevaluation

T015 AC1 (disconnect detected/communicated and PlayerId retained): ALREADY PASS LAB, reinforced by current lobby snapshots and the real Unity/RTC reconnect. AC2 (unvalidated action cancelled and confirmed result retained): NEEDS PRODUCT IMPLEMENTATION. AC3 (duplicate/late data never reproduces official actions): NEEDS PRODUCT IMPLEMENTATION; existing transport sequence/epoch rejection is already PASS but does not establish official-action idempotency. `PlayerInput.Throw/Smash` remain declarations; no official-action lifecycle or confirmed-result store exists.

Once T013 is integrated/closed, #25 and #26 satisfy T015 dependencies; remove blocked while preserving original dependency history. T015 must remain open and F03 cannot close while that child remains incomplete. Do not construct fake gameplay to close either. T016 remains OPEN / Ready independently. E0 remains OPEN; Phase 0 IN PROGRESS; Phase 1 NOT STARTED pending the future fresh integrated FINAL EXIT AUDIT, T019 and T020.
