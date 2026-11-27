# Phase0 connectivity validation

Run from the repository root. Requires the existing prepared Node24/JDK21/Chrome/Playwright/runtime dependencies; no gameplay installer.

- `Server/mvnw -B -f Server/pom.xml -Dtest=ConnectivityTest test`: deterministic supervisor, bounds, HTTP denylist, cleanup, mode/fallback/QR tests.
- Set absolute `GORILLA_TEST_NODE`, `GORILLA_TEST_PLAYWRIGHT`, `GORILLA_TEST_CHROME`, and a new `GORILLA_BOOTSTRAP_LAB_REPORT` JSON output, then run `Server/mvnw -B -f Server/pom.xml -Dtest=BootstrapRtcLabTest test`: real PublicIngress and native/browser RTC, 1/2/4 clients, fake external tunnel death, control, ACK loss and reconnect.
- `python3 tools/t063/run-no-wan.py Server/mvnw -o -B -f Server/pom.xml -Dtest=BootstrapRtcLabTest test` with those prepared variables: private user/network namespace, virtual LAN, no WAN/default route. Retain the wrapper's WAN probe and structured LAB report. If mapping UID0 changes the runtime's home, explicitly set `JAVA_TOOL_OPTIONS=-Duser.home=<prepared-home>` and `-Dmaven.repo.local=<prepared-cache>` for that test invocation. Never run `no-wan.py` directly; it rejects a matching/missing host namespace identity.
- `GORILLA_REAL_TUNNEL=<reviewed-absolute-binary> GORILLA_REAL_TUNNEL_REPORT=<new-output> Server/mvnw -B -f Server/pom.xml -Dtest=RealQuickTunnelTest test`: opt-in external HTTPS QA; DNS/WAN failure is BLOCKED, not physical evidence.
- `node tools/t063/soak.mjs <new-output.json> <absolute-package> 1800` with Playwright/Chrome paths: real Player, Java/IPC, replay Vision, four real RTC clients. Client1 drops all motion ACKs; client2 drops every seventh motion packet. Aggregated metrics/resources only; no sensor histories/images/SDP/tokens persisted. Package must already be built and verified. Closes only owned processes.
- Editor smoke: copy `editor-smoke.cs` temporarily into an isolated Unity project's Editor folder, set `GORILLA_EDITOR_SMOKE_REPORT` to a new absolute JSON output, invoke `EditorProductSmoke.Start` in an interactive editor and remove the temporary harness/meta afterwards. Does not regenerate Bootstrap or alter the user's scene. Keep the existing licensed launcher and use an isolated layout configuration if the environment's saved maximized layout cannot load.

All results are LAB. Physical iPhone/Android/four-phone/T020 clean-PC acceptance remains NOT RUN. No human tracking tests.
