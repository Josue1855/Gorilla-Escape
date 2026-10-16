# T063 — transparent AUTO follow-up, 2026-10-15

Baseline: merged #118, `4c9bea4663a7c8a2544390db30dc0a83a43ce4d8`.
The original checkout at `08920c8` contains five pre-existing local changes;
this follow-up uses an isolated managed worktree and preserves them.

Normal flow: Main Menu → CONECTAR TELÉFONO → Preparando conexión… →
QR / Escanea con tu teléfono → existing phone CONECTAR → CONTROL LISTO.
No transport selector, origin, certificate fingerprint, failure code or process
output appears on this surface. JUGAR stays disabled. PRUEBAS remains optional;
DIAGNÓSTICO is an explicit escape from the human error message.

AUTO is the default, including run-qa.sh without variables and the existing
Editor physical opt-in. Cabin HTTPS is initialized first. An optional public
bootstrap starts on a worker, with a maximum 15000 ms technical budget. AUTO
can issue a Cabin admission during STARTING. The worker's completion cannot
revoke or reissue that admission. Only a request for NUEVO QR chooses again.
The same freeze applies after tunnel loss; an expired/unreachable QR needs an
explicit replacement. No session, PlayerId, epoch, score or Unity authority is
reset by the resolver. A player who already has LAN RTC keeps that transport.

A public origin requires the owned live process, an exact trycloudflare.com
hostname, registered connection and a public HTTPS GET `/` returning 200.
Hostname/registration are never sufficient. Early exceptions/non-200 replies
retry, with request bounds and pauses inside the shared deadline. No
`--protocol` option is supplied: cloudflared selects its official automatic
transport. A background bounded health check also detects continued public
HTTPS failure, without blocking IPC/UI or closing RTC peers.

The exact official Linux amd64 2026.10.0 binary is bundled as
`Tools/cloudflared`, SHA256
`d33ff2d14475178d2012c2c56beba87389ac5ded27649519f198a7d3134a99db`.
Java checks that pin before *any* execution, including `--version`. Missing,
invalid or unusable binaries safely select Cabin in AUTO; no runtime download
or request to install tools exists. The build-time packaging helper requires
the reviewed binary and copies its license, integration notice and provenance.
The optional binary is intentionally excluded from the mandatory runtime file
manifest; its separate pinned integrity check permits Cabin after removal or
corruption. All mandatory runtime files remain subject to package verification.

Official release and digest:
https://github.com/cloudflare/cloudflared/releases/tag/2026.10.0
Official Apache-2.0 license:
https://github.com/cloudflare/cloudflared/blob/2026.10.0/LICENSE
The included NOTICE is explicitly a Gorilla Escape integration notice: the
pinned upstream NOTICE path returned 404. It does not claim upstream authorship.

Internal preflight observes LAN, LocalTrust, bound PublicIngress, Java, native
RTC initialization, executable integrity, tunnel startup, and DNS/HTTPS through
the actual public request. Unconfirmed checks remain NOT_CONFIRMED; no network
success is fabricated. `ConnectivityService.preflight()` exposes this internal
snapshot, without adding a technical checklist to the normal screen or wasting
the bounded per-frame IPC payload. QA retains Requested/Resolved/active-admission
modes, tunnel state, origins, timing, fallback reason and LAN DIRECT path.

A new offline phone can choose PREPARAR TELÉFONO, see the one-time explanation
and scan a separate QR to the existing local setup guide. Fingerprint and
architecture are in Detalles. There is no silent certificate installation and
no TLS bypass. A prepared phone uses the join QR directly. Public READY needs
no local trust preparation. The PC cannot automatically detect trust in an
unseen phone; the preparation action is explicit.

`Tools/diagnose-cloudflared.py` is an explicit QA-only entry to the official
`cloudflared tunnel diag`, with hash verification and a 15-second bound. It is
never run on startup; reports remain in the local QA summaries directory.
When the temporary metrics port cannot be discovered, the official command may
report no instance; this optional diagnostic is not a readiness requirement.

## Gates and retained limits

Evidence: `docs/evidence/t063-transparent-auto-2026-10-15/`.
Public HTTPS readiness is an independent gate. Failed real attempts are retained
as BLOCKED and cannot be converted to PASS by hostname or registration.
Physical AUTO on iPhone Safari / Android Chrome is NOT RUN. After software,
scan/connect/activate on the same LAN, record the internal ResolvedMode and
browser/OS, then separately test forced CABIN/CLOUDFLARED for QA. Verify offline
preparation on a new phone and preserve active controls during WAN loss.

T063/#114 remains OPEN until its public HTTPS gate passes and the normal
integration checks complete. T019, T020, F05, E0 remain OPEN. Phase1 is NOT
STARTED. No public STUN/TURN, remote multiplayer or cloud gameplay is added.

## DNS/readiness correction — 2026-10-15

The previous BLOCKED network observations are retained as historical evidence.
The controlled comparison isolated two contributors: the active network's DNS
kept missing a newly issued name after an independent HTTPS DNS query could
resolve it, and Java cached a negative lookup for10s. In the same-JVM diagnostic,
normal system DNS/HTTPS recovered earlier but Java could not GET200 until10698ms.

`DnsRetryPolicy` sets `networkaddress.cache.negative.ttl=1` at the application
entrypoint, before InetAddress initialization. This is a Java security property,
not a `-Dnetworkaddress...` system property. The change is confined to the game's
JVM; positive DNS caching, TLS trust/hostname verification, the15s deadline and
Cloudflare protocol selection are unchanged. A forked-JVM regression first
caches a missing hostname, publishes it in an isolated hosts fixture, and proves
recovery inside3s. It requires no external DNS or network configuration.

On the reference developer PC, DNS was corrected only on the current Wi-Fi
profile to1.1.1.1/1.0.0.1. The original configuration and rollback are kept
privately outside Git. Testing the original DNS again with the Java fix still
failed within15s; the corrected DNS plus Java policy passed repeatedly. No
firewall rule was changed, no port opened and no TLS check bypassed. The package
does not change a player's network settings; AUTO still uses Cabin when an
external network cannot provide usable public bootstrap.

Real PWA HTTPS200 now passes (6297ms,6826ms; explicit JDK21 run6537ms), including
private-route404s, invalid-admission400 and owned-process cleanup. The actual
packaged JRE21/server entrypoint also resolves AUTO→CLOUDFLARED and returns the
real PWA through HTTPS200 with zero owned residual processes. These are software/
external-network gates; iPhone Safari/Android Chrome physical joins remain NOT
RUN under T019. This follow-up supplies the previously missing T063 public gate;
normal PR integration is still required before task closure.

Evidence: [DNS diagnosis](evidence/t063-dns-readiness-2026-10-15/diagnosis.json),
[packaged public bootstrap](evidence/t063-dns-readiness-2026-10-15/package-public.json).
Java's property contract: [OpenJDK/Oracle Java21 networking properties](https://docs.oracle.com/en/java/javase/21/docs/api/java.base/java/net/doc-files/net-properties.html).
