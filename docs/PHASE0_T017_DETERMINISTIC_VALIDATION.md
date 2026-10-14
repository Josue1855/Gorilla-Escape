# T017 — deterministic acceptance under DEC-027

> **Estado integrado — 2026-10-14:** #30 CLOSED/completed/Project Done tras PR #107 y regresión post-merge61/61. AC humanos DEFERRED/NOT RUN en #106/P1. Las frases condicionales de preparación más abajo conservan su fecha; salida actual autorizada por DEC-027.

2026-10-13, America/Chihuahua. Base `3cf27bd8c382ad38ba97855dd44f87e56ee2e2d7`, branch `codex/t017-deterministic-exit`. Source tests CameraFusionTests; corpus CameraInput v1 synthetic numeric fixtures. No persons/images/video.

## Historical AC

| Original AC | Evidence | Disposition |
|---|---|---|
| 20 entradas de otra persona; cero cambios accidentales | NOT RUN | DEFERRED Post-MVP T061/#106 |
| Oclusión/tracking perdido pausa; retorno recuperable físico | NOT RUN | DEFERRED Post-MVP T061/#106 |
| Nunca seleccionar automáticamente otro jugador físico | NOT RUN | DEFERRED Post-MVP T061/#106 |

## Amended Phase0 gate

20 parameterized scenarios replace locked subject1 with distinct foreign subjects101–120 inside ROI, across players1–4, twice per scenario. Assert zero promotion, original player/subject retained, LOST and fusion ineligible; explicit tracking-lost frame remains safe. Recovery first original-subject frame RECOVERING/ineligible; second frame LOCKED/eligible; session/device/player/epoch unchanged. Existing crossing, ambiguity, low confidence, spatial jump, stale, duplicate, player mismatch and phone reconnect tests retained.

LAB/SYNTHETIC only. Recovery identifies wire subject ID, not real human identity. Scene/gameplay pause is not implemented here; eligibility false is the Phase0 technical safe gate. Test results recorded after actual execution in evidence; closure only after merged DEC/test evidence and post-merge checks.

## Executed results

[Validation receipt](evidence/overnight-2026-10-13/technical-exit-validation.json): 20/20 deterministic replacements PASS, unauthorized switches0. Full Unity EditMode121/121, PlayMode20/20, Linuxbuild Succeeded/errors0/warnings0. Component23/23, PWA59/59/build PASS; Java78 tests/0failures/0errors/2existing skips (76 executed PASS). Management/Foundation/diff PASS. XML hashes and source hashes identify actual tested sources; raw logs remain private local cache. Initial direct Editor license exit198 preserved; supported Flatpak retry succeeded, no plan change. All amended gates PASS LAB, human gates DEFERRED/NOT RUN.
