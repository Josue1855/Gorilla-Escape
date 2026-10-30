# Transparent connectivity — Phase0

> Actualización de esta rama, 2026-10-27: [tiempos y recuperación Cloudflared](03-development/CLOUDFLARED_RECOVERY.md) amplía el arranque a 30 s (máximo configurable 60 s) y añade recuperación AUTO. Las cifras anteriores de este documento se conservan como historial.

> Follow-up PO policy, 2026-10-15: [T063 transparent AUTO](PHASE0_T063_TRANSPARENT_AUTO.md) supersedes the historical waiting, discovery, forced-protocol and automatic QR replacement behavior below. Historical evidence remains unchanged.

PO instruction accepted 2026-10-14 in DEC-030. T063/#114 is an optional Phase0 enhancement under F41/#68, supporting F05/T019/T062 without adding an academic exit gate. T064/#115 hardens mobile motion; T065/#116 covers Editor development. Phase1 NOT STARTED; JUGAR disabled.

## Shared path and authority

CABAÑA is the canonical local/offline bootstrap: LAN selection → LocalTrust → local HTTPS → PWA/fragment QR → TechnicalSession → direct host-only WebRTC → Java PhoneInputHub → authenticated loopback IPC → Unity. A new phone needs **PREPARAR TELÉFONO** once to trust the local CA; scanning a QR cannot silently install that trust. RNF-GOR-02 still needs physical measurement.

CLOUDFLARED is optional HTTPS/PWA/signaling convenience. The public origin changes; JoinQrPoc and TechnicalSession do not. Motion/control are never HTTP requests through the tunnel. Unity retains official state authority; Java owns session/transport. No public STUN/TURN, relay, remote multiplayer, accounts, Access/OTP, domain provisioning, or cloud dependency is introduced. GOR-07, SRS-GOR-02 and offline acceptance remain unchanged.

## Resolver

`ConnectionBootstrap` owns RequestedMode, ResolvedMode, bootstrap state, public/LAN origin, fallback reason, startup/fallback milliseconds and revision. `Tunnel` is the process boundary and can later accommodate a named/managed implementation without credentials in the repository. Only Quick Tunnel is implemented.

Normal launcher: `./run-qa.sh`, Requested AUTO. Mode can be explicitly set with `GORILLA_CONNECTION_MODE=CABIN` or `CLOUDFLARED`. Invalid mode fails configuration. Cabin never starts the tunnel. AUTO makes one asynchronous attempt, requires a validated temporary HTTPS origin, a registered tunnel connection and HTTP 200, and falls back only after owned-process cleanup. Forced Cloudflared failure is visible and does not silently select Cabin. No infinite resolver retries. Main Menu remains responsive during startup.

`GORILLA_TUNNEL_STARTUP_MS` is configurable from 100 to 15000 ms; default 6000 ms. Process version/discovery/start/HTTPS readiness share that deadline; bounded shutdown may add up to 1500 ms. The real 2026.10.0 registration-only attempt measured 3933 ms; public DNS lookup failed, so public readiness and a successful startup distribution are **not measured**. Six seconds is a provisional tested budget above that observation, not an availability guarantee. Missing binary fallback is measured separately in tests. Exhausted startup remains Cabin for AUTO.

The normal package has no Cloudflared binary. Discovery considers an absolute explicit `GORILLA_CLOUDFLARED_PATH`, then absolute PATH entries. Packaged discovery is intentionally disabled pending a reviewed pin/provenance policy; it is not a trusted arbitrary file search. No launch-time download or installation. An official release binary can be supplied locally for QA; version response is bounded and validated. Quick Tunnel hostname is temporary, memory-only and never saved as permanent configuration.

## Public boundary

`PublicIngress` binds an ephemeral **127.0.0.1-only** listener. It handles requests directly using the same MobileRuntime, and **never proxies Spring Boot**. The tunnel target can only be that exact loopback HTTP origin. Cabin needs no public listener. Fallback/shutdown closes the gateway.

Exact public routes/methods:

| Method | Path | Purpose |
|---|---|---|
| GET | `/`, `/index.html` | Built PWA shell |
| GET | `/manifest.webmanifest`, `/sw.js` | Shell metadata/offline worker |
| GET | `/icons/icon-192.png`, `/icons/icon-512.png` | Existing icons |
| GET | `/assets/[A-Za-z0-9_-]{1,100}.js` or `.css` | Existing classpath PWA build assets only |
| POST | `/mobile/join` | One-use, expiring admission + SDP |
| POST | `/mobile/reconnect` | Existing resume identity/epoch policy |
| POST | `/mobile/disconnect` | Existing authenticated peer close |

Everything else is 404, including `/mobile/admission`, `/mobile/diagnostics`, `/mobile/end`, `/api/health`, `/actuator`, `/ipc`, `/qa/preview`, `/camera/preview`, `/prepare`, `/trust`, `/ca.key`, `/filesystem`, `/metrics`, `/mobile-lab/index.html`, arbitrary debug endpoints, directories, unknown methods, queries, traversal and percent-encoded paths. Neither private CA material nor camera images are served. Hashed assets are read from `static/` only; private server resources are outside the boundary.

Dynamic and shell replies set `Cache-Control: no-store, max-age=0`, `Pragma: no-cache`, no-referrer, nosniff, and frame/object restrictions. Existing service worker precaches only generated shell assets and never signaling/session replies. No admission-specific HTML is generated. Join secret remains `#v=1&sessionId=…&admission=…`, absent from the initial HTTP request; it is never moved into a query. Admission/resume are supplied only by the existing JSON signaling, never operator routes.

Resource bounds: 73728 request bytes, 2 MB static resource bound, 4 workers, 4 queued work items, listener backlog16, JVM HTTP connection cap32, 12-second exchange deadline, 60 accepted requests/second globally. Foreign browser Origins are rejected for POST; short-lived admission/resume and session capacity remain the authentication boundary. Public ingress shares the existing native RTC 4-peer maximum and 10-second negotiation deadline. Global limits can be exhausted by an Internet attacker; this is a bounded QA ingress, not a universal security or Internet availability certification.

## Failure and lifecycle

Supervisor states: STOPPED, STARTING, RUNNING, STOPPING, FAILED. Commands use argument arrays, never a shell. Only its own ProcessHandle descendants are stopped; bounded graceful/forced stop and output reader cleanup are checked. Output lines are bounded at 2048 characters, never logged; only exact HTTPS trycloudflare hostnames up to DNS label limits are accepted. Version capture is bounded. Invalid/malicious output, missing executable, timeout, process death, registration loss and HTTPS failure do not create readiness.

After established RTC, tunnel/process registration loss only changes future joins. Snapshot health polling notices process death/explicit registration errors; silent WAN failure can take cloudflared's own failure detection time and is not a universal WAN outage detector. A healthy process alone does not certify continuing WAN reachability. AUTO cleans up and selects Cabin, revokes **all pending admissions**, regenerates a new QR revision, preserves players/PlayerId/epoch/session and Unity state. It does not mutate a displayed admission behind its original QR. The prior QR is invalidated and replaced on the next authenticated IPC update. QA mode switching is rejected while any player slot is retained. Forced Cloudflared stays failed on tunnel loss; established RTC is unaffected by the resolver.

## Observability and evidence

Centro de Pruebas → CONEXIÓN / QA shows Requested/Resolved, bootstrap/tunnel state, public/LAN origin, fallback, startup/fallback ms, WAN observation and **GAMEPLAY PATH: LAN DIRECT / CLOUDFLARED DOES NOT CARRY GAMEPLAY**. No network dashboard is added to Main Menu. Default Editor navigation starts no services. Physical Editor services require `GORILLA_EDITOR_PHYSICAL_QA=1`, an interactive official Bootstrap scene and explicit test-run exclusion; normal tests cannot start them through inherited flags.

Motion now coalesces an event-task burst into one latest sample, sends best-effort on unordered/maxRetransmits0, drops on buffer pressure and never waits for ACK. At most64 sequence/timestamp observations survive; eviction and late/missing ACK count as telemetry loss, not flow control. Reliable control keeps its existing bounded ACK promises/deadline. RTT remains a transport observation, not motion→visible-result latency.

See [overnight report](PHASE0_OVERNIGHT_INTEGRATION_REPORT_2026-10-14.md), [physical runbook](PHASE0_IPHONE15_LIVE_QA_RUNBOOK.md), [T019 acceptance](PHASE0_T019_PHYSICAL_MOBILE_QA.md). Quick Tunnel is temporary development/QA, requires WAN, has no uptime guarantee and is not stable release evidence. [Official Quick Tunnel documentation](https://developers.cloudflare.com/tunnel/get-started/quick-tunnels/), [official run parameters](https://developers.cloudflare.com/cloudflare-one/networks/connectors/cloudflare-tunnel/configure-tunnels/run-parameters/).

### Repeatable LAB commands

Focused software checks: `node --test PWA/tests/motion-flow.test.js` and `Server/mvnw -B -f Server/pom.xml -Dtest=ConnectivityTest test`. Historical stall reproduction is documented in [motion hardening](PHASE0_MOTION_HARDENING.md).

The real ingress/native-RTC lab is explicitly opt-in through `GORILLA_TEST_NODE`, `GORILLA_TEST_PLAYWRIGHT`, `GORILLA_TEST_CHROME` and a new `GORILLA_BOOTSTRAP_LAB_REPORT` path; run `-Dtest=BootstrapRtcLabTest`. It uses a stub **only for the external tunnel**, real PublicIngress, real browser DataChannels and real MobileRuntime; 1/2/4 peers, candidate policy, tunnel-loss survival, ACK loss, reconnect and capacity are measured. Browser hides local candidate addresses; null is UNKNOWN, never a measured private address. Selected pair type/remote private address plus disabled STUN/TURN are recorded; raw addresses are not.

To isolate WAN without touching the host, wrap that command with `python3 tools/t063/run-no-wan.py`. The wrapper creates a private user/network namespace, sets up a virtual Ethernet LAN only inside it and requires a different network-namespace identity before any network mutation. It verifies no default route and `connect_ex(1.1.1.1:443)=ENETUNREACH`. Maven must use prepared offline dependencies. A namespace with no usable LAN interface is not a valid RTC offline test; the first such attempt correctly failed gathering and was superseded by a virtual-LAN attempt. No global Wi-Fi, routes, firewall or DNS are changed.

For external Quick Tunnel testing, opt in with absolute `GORILLA_REAL_TUNNEL` and a new `GORILLA_REAL_TUNNEL_REPORT`, then run `-Dtest=RealQuickTunnelTest`. The official binary used for attempted QA has pinned release/hash in the evidence. Missing WAN/DNS/HTTPS is BLOCKED, not physical acceptance.

QA requests received while bootstrap is STARTING are retained as one bounded pending boolean. Once READY, the existing admission/QR is issued against the resolved origin; no manual extra regeneration is required. A deterministic regression first reproduced the lost-request behavior and then verified fulfillment. The final native-click automation limitation is documented independently of Editor/PlayMode navigation evidence; no native click PASS is inferred from window startup or HTTPS200.
