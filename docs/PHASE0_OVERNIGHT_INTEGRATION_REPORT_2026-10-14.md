# Phase0 overnight integration — 2026-10-14

Run started2026-10-14 America/Chihuahua and continues2026-10-15. Final frozen soak is IN PROGRESS; physical/external limitations are explicit. Initial develop `59a32a236f196a5daa6291ce9c7d4934f941fd2a`; Editor PR #113 merged normally at `c35a3e31e3f1437c5bc341910233c887c1815377`. Existing dirty Editor worktree preserved; implementation isolated in the attached `phase0-integration` worktree. DEC-030 accepted by explicit PO instruction. T063/#114 (optional under F41), T064/#115 (T019 software support), T065/#116 (Editor DX) created after auditing all existing issues. Project field readback retained in evidence; no field/view recreation.

Fresh evidence is collected in [integration evidence](evidence/phase0-integration-2026-10-14/). Historical PASS is not substituted for current tests. Full final audit will be appended after completion.

T019 physical NOT COMPLETE; T020 clean physical PC NOT RUN; F05/E0 OPEN; final exit BLOCKED; Phase1 NOT STARTED; JUGAR disabled. No phone/human/secondary-PC tests authorized overnight. [Connectivity architecture](TRANSPARENT_CONNECTIVITY.md).

## Integration checkpoints

- `develop` after motion merge: `beca79863236217ebb1fd39b0e949e44f641c2c3`; PR117 required checks PASS, zero unresolved threads. Post-merge PWA69/69 rerun via its documented package script PASS. An accidental direct-root test invocation failed only because the SW test expects PWA as working directory; that attempt was superseded, not counted PASS.
- Fresh gateway/native RTC LAB and safely isolated no-WAN LAB: CABIN and AUTO with external-tunnel stub each progress1→2→3→4 unique players. Selected candidate pairs host↔host, no configured ICE servers, remote address private; local candidate address hidden by Chrome = UNKNOWN. All4 retain epoch1 and send40 new motion samples each after simulated tunnel loss with all motion ACKs suppressed; reliable HEARTBEAT/CLIENT_STATE succeed. Reconnect retains PlayerId and reaches epoch2; capacity/reused admission denied; public-IP candidates denied. This is software/LAB evidence, not public5G physical testing.
- Fresh Java full run94 cases:93 passed,1 explicitly skipped external Quick Tunnel; includes10 deterministic connectivity cases, real native gateway/browser RTC, explicit LAN HTTPS configuration and process lifecycle. No failures. Python25 tests PASS. Quick Tunnel was separately attempted and blocked, never promoted from skipped to PASS.
- The later PlayMode repetition collided with the deliberately running soak's process singleton:9 attempts correctly returned ALREADY_RUNNING,18 passed. It does **not** supersede the earlier27/27 result as a PASS. A clean full repeat must run after the soak releases its owned backend. No guard bypass or second production backend is introduced to make tests green.
- T018 fresh A–B comparison halted on first arm:physical capture opened/configured,zero frames,all owned resources and controls restored. Wireplumber holds a camera descriptor; causality is not established and that unrelated process was not stopped. Result **BLOCKED / comparison incomplete**, performance evidence must be rerun when capture is available. Existing historical T018 acceptance is not rewritten.

## Frozen final candidate — 2026-10-15

Actual package: `/home/josuegalvan/Documentos/GorillaEscape-Phase0-Integrated-2026-10-15`. Local integrity verifier PASS; runtime hashes are frozen in `final-soak-provenance.json`. JRE21/Python/Vision/models/native dependencies/PWA/Unity/notices/manifest retained from the verified package plus newly built Unity/Java/PWA; optional Cloudflared remains absent and does not invalidate this package. No gameplay startup download. Final runtime is not mutated during its soak.

The exploratory1054-second run was stopped gracefully to freeze the deliverable; its post-stop disconnected-state assertion failed as expected, exit0/owned residual0/controls restored. It is **SUPERSEDED**, not30min PASS. An earlier package refresh during that trial prevents release provenance claims for it. The separate final30min run starts from the frozen hashes and includes QA heartbeat aggregates as well as real accepted motion/Unity visual response.

Launcher verification reached HTTPS LAN200, MainMenu/JUGAR-disabled and physical capture startup. Automated pointer injection was not reliable: requested local(180,568), actual local(874,842); direct owned-window events reached Unity but did not activate the targeted button. Those click attempts remain **NOT RUN/BLOCKED as reliable native-click proof**, with original FAIL results preserved. They exited cleanly with zero owned Java/Vision residuals and restored controls. No UI failure is hidden and no physical operator interaction was requested. Editor interactive entry/navigation/Exit and fullPlayMode27/27 are separate successful evidence, not a relabeling of those attempts.

The delayed-bootstrap QR race was reproduced (`expected AVAILABLE; actual ORIGIN_CHANGED`), then fixed with one pending-request boolean: QA requests made during STARTING are fulfilled exactly once after READY using the resolved origin.13 focused connectivity/security tests now pass, including this case, real child death, missing binary timing and GET/POST denials. Missing executable observation: startup0ms/fallback0ms in this host's millisecond-resolution test; AUTO READY/CABIN versus forcedCloudflared FAILED. These are observed values, not universal timing guarantees. Default6000ms budget remains configurable and provisional; successful public HTTPS startup was not accredited.

Cadence precision: the6900-sample unit stress uses virtual sample timestamps for50/60/120Hz; it does not establish measured wall-clock cadence. A separate wall-clock nine-run fixture and the real RTC timer-driven30/50/60Hz lab complement that proof. The30min frozen run exercises continuous real-time synthetic emission. None measures physical phone Hz or motion→visible-result E2E.

## Evidence classification

| Classification | Evidence and practical limit |
|---|---|
| FRESH | PWA69/69/build; UnityEdit161/161, clean finalPlay27/27 and Linuxbuild0errors/0warnings; Java97cases=96pass/1external skip; Python25; clock/management/foundation/diff;13 connectivity/security tests; standard mobileRTC lab; gateway1/2/4 and isolated no-WAN lab; decoded existingQR; wall-clock motion stress9runs/1725sends, maxinterval22.05ms, telemetrymax64, errors0 |
| FRESH, IN PROGRESS | Separate30min frozen-runtime soak; completion/cleanup are required before final acceptance of this row |
| RETAINED BUT STILL VALID | Historical decisions and accepted T016/T017/T018/T062 scope; offline requirement and original T019/T020 AC; initial interactiveEditor smoke for unchanged entry/scene/test-isolation code, plus finalPlayMode verification of the QA mode/QR bridge |
| STALE — MUST RERUN | A fresh performance/imperceptibility comparison under the final UI workload: new T018 physical-camera arm captured0frames and cannot replace prior measurements |
| SUPERSEDED | Old stop-and-wait motion implementation; wrong-root PWA invocation; host-direct unlicensed Unity launch; initial sandbox Chrome failure; concurrent singleton-collision PlayMode attempt; exploratory1054s soak stopped for runtime freeze |
| NOT RUN / BLOCKED | T019 physical phones/trueE2E/cross-device clocks; T020 clean physical PC; reliable native-click automation in this compositor; public QuickTunnel HTTPS (registered once but DNS failed; final implementation correctly did not reportREADY); real5G negative test; bundledCloudflared dependency/license policy; new human tests |

A zero-error software suite does not clear the blocked physical/external rows. ACKRTT is not E2E. A host↔host LAB pair with local address hidden is not a measured physical network path. VM/replay/fixtures/public HTTPS never satisfy T019/T020. Final Exit remains BLOCKED, T019/T020/F05/E0 OPEN, Phase1 NOT STARTED and JUGAR disabled.
