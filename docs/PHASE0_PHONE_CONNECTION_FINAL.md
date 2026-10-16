# Phone connection decision — PO, 2026-10-16

Status: implementation candidate based on remote develop `b1db2f3` (PR #120).
T063 remains closed Software/Lab. T019, T020, F05 and E0 remain open; Phase1
implementation by the team is authorized under the subsequent PO instruction below; JUGAR remains disabled until gameplay exists. This decision supersedes the
immediate-Cabin-QR clarification of DEC-030 and the preventive trust UI.

## Normal experience

Open Gorilla Escape → CONECTAR TELÉFONO → Preparando conexión… → one JOIN QR →
Safari/Chrome → CONECTAR → CONTROL LISTO. The menu stays interactive while
Java prepares local HTTPS, public bootstrap and network checks in parallel.
There are no route selectors, CA/fingerprint, certificate QR or preparation
steps on the normal surface.

AUTO gives the existing bounded public attempt its full 15000 ms budget.
The current real JDK21 measurement was 6986 ms; prior measurements were
6297/6537/6826 ms. This retains the previously bounded budget rather than
choosing a deadline below observed startup. READY still requires pinned SHA,
live owned process, exact public hostname, registered tunnel, and HTTPS GET
`/` == 200. No runtime download, TLS bypass or public relay is introduced.
Failure selects Cabin after cleanup; no WAN is required for established local
RTC or for local HTTPS on an already trusting phone.

An admission freezes its origin. Background readiness/loss cannot replace its
raster or reset session/PlayerId/epoch/Unity state. Only an explicit new QR
selects a new origin. Even a replacement request waits across subsequent IPC
polls while a new AUTO resolution is in progress.

## Recovery, not trust detection

No JOIN after the 30-second local admission window, no public route ready, and
no confirmed joined admission permits the human error surface REINTENTAR /
CONFIGURAR ESTE TELÉFONO. This observes absence of JOIN; it does not classify
trust from IP, cookies, history or a previous download. Recovery appears only
after the explicit configuration choice. Its fingerprint comparison is visible
before installation and its mobile guide retains iPhone and Android steps.
Never ignore TLS errors. Fresh phone + private CA + no WAN cannot provide
zero-setup browser trust silently.

`TrustOnboarding` retains HTTPS `/prepare` and `/gorilla-root.cer`. Its HTTP
bootstrap starts only during explicit recovery, not at normal launch, and stops
when leaving recovery or shutting down. Shared keeps the existing
`QaHostingWire.prepare : QaAdmissionWire`; no duplicate prepareModules/prepareSize.
A shared Java-generated JSON fixture, real authenticated IPC assertion and
Unity decoding test cover that shape and the 4096-byte frame bound.

## PC firewall

Only `tools/setup-firewall.sh` and its fixed Python implementation receive
Polkit elevation. Unity/Java/Vision/cloudflared stay under the ordinary user;
the launcher rejects UID0. No sudo/password transport, dynamic root shell,
caller command, UFW enable, global policy, router/DNS or WAN change exists.
The helper operations are `--check`, `--install`, `--remove`; the only optional
inputs are a validated local private IPv4 and the closed recovery flag.

The helper re-derives interface/subnet from installed `ip` JSON, checks actual
UFW tuples, and installs only missing interface + source subnet + destination
IP rules. TCP8443 is the normal listener. WebRTC now explicitly configures
libwebrtc's minPort/maxPort to UDP50000–50031, with STUN/relay/TCP disabled as
before. The four-browser native RTC test verifies selected Java ports against
that bound (observed 50000–50003), rather than assuming an OS ephemeral range.
The 32-port allocator policy provides four-player/multi-interface/resume
headroom; other RTC applications/ports are not opened.

TCP8081 is a recovery lease only. A root helper holds its own exception for
at most 180 seconds, removing only its own rule on pipe EOF (including parent
exit) or expiry. Pre-existing equivalent rules are never claimed or removed.
Root-owned receipts record rule ownership before changes, verified rules and
UFW file identity/size/mtime/ctime; transactional failure rolls back new rules.
Recovery removal refreshes the verified base-rule receipt, avoiding a new
administrative request on the next normal run. `--remove` can retire this
installation's recorded rules from previous networks too.

Subsequent checks are non-elevated when an unchanged root-owned receipt proves
the rules. A new interface/subnet/destination or changed UFW files invalidates
that proof and requires inspection. Cancellation is a recoverable state; no
QR claims RTC availability until preflight succeeds. Tunnel startup continues
independently. Existing RTC peers are never closed by preflight or resolver.

**Protected-state limitation:** on this Linux host `/etc/ufw/user.rules` is
root-readable only. On the very first run, without a prior helper receipt,
non-elevated inspection returns INSPECTION_REQUIRED even if equivalent rules
might already exist. Polkit is then needed to inspect and verify; install
creates no duplicates and does not claim those existing rules. The stronger
"never ask if pre-existing rules already exist" case is not proven for this
protected-state configuration. A permanently installed privileged read-only
broker/policy would require separate OS installation/design; none is silently
installed here. UFW receipt verification concerns UFW-managed persistent state,
not arbitrary independent nftables changes by an administrator.

## Review findings and acceptance boundary

The original dirty checkout remains intact. In remote develop, nested Shared
prepare, visible fingerprint comparison, test-runner isolation, asynchronous
shutdown and build-relative Vision paths already superseded the old findings.
This candidate adds minimal owned firewall rules/removal without importing the
old broad RFC1918 permissions, Editor reset/Wait, dated runtime fallbacks or
unrelated AI packages.

New tests cover resolver wait/fallback/frozen origins and WAN-loss survival,
closed preflight actions/cancellation, firewall idempotence/rollback/ownership/
network mismatch, recovery-only HTTP and inherited physical-QA Test Runner
isolation. Deterministic helper tests never modify the host firewall or invoke
Polkit. Software test counts and physical gates are recorded separately in the
validation evidence; fixture updates preserve security assertions while the
physical TLS fixture now expects 503 when its uninstalled preflight helper is
unavailable, and verifies that HTTP bootstrap is initially closed.

The iPhone15 gate must exercise the final executable and system permission UI.
Record iOS/Safari, current LAN, resolved route, selected ICE pair, JOIN,
DataChannels, sensor Hz and clean shutdown internally; never persist admission,
operator/resume tokens, raw SDP, camera images or sensor histories. Existing
rules/no prompt, first permission, cancel/retry, second launch, owned removal
and another-network cases require real OS/human evidence before claiming PASS.
Physical results feed T019 and do not reopen T063 without an actual regression.

## Integration and team handoff — subsequent PO instruction, 2026-10-16

The PO explicitly requested normal PR integration into develop and authorized
teammates to begin the remaining game/PWA/Flutter implementation in task branches.
This supersedes the earlier implementation-start restriction, without closing
T019/T020/F05/E0 or modifying physical acceptance. The runtime integration does
not implement games or Flutter, and does not enable JUGAR. See TEAM_START.

Pre-publication review additionally prevents all physical helper checks/elevation
in explicit LAB mode, and requires the ownership comment on the same UFW tuple
before rollback/removal; a comment attached to another rule cannot establish
ownership. Both changes have targeted regression coverage.
