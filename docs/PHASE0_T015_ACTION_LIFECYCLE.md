# T015 — Official action lifecycle

Base: develop `049a0fc169acbd03a56f3139588efba8b17c5fe9`. Rama `feature/network-t015-action-lifecycle`. DEC-024 ACCEPTED. Alcance genérico Phase0; Phase1 NOT STARTED.

## Autoridad y boundary real

`JavaProbeSupervisor.PhoneInputs.Actions` expone `OfficialActionLedger`, puro C# con reloj inyectable. El supervisor alimenta el mismo PhoneInputStore con input validado por IPC y snapshots validados por LobbyAuthorityBridge. No hay otro transporte ni store de input. Snapshot invalida candidatas incluso sin otro sample; SERVER_STATE comunica suspensión/desconexión. Cleanup de IPC termina sesión, cancela candidatas y deja resultados consultables; siguiente lanzamiento limpia PhoneInputStore/ledger. `Clear()` es reset explícito de sesión y elimina toda retención.

Ejemplo para futuro caller Unity (su validación semántica ocurre entre begin/confirm):

```csharp
if (supervisor.PhoneInputs.TryActionSource(playerId, "semantic-key", out var key)
    && supervisor.PhoneInputs.Actions.TryBegin(key, out _) == ActionDecision.CREATED)
{
    // Future gameplay validates semantics; no validation/scoring invented in T015.
    var decision = supervisor.PhoneInputs.Actions.Confirm(key, "accepted", out var record);
    if (decision == ActionDecision.CONFIRMED) { /* apply future effect exactly once */ }
}
```

Ningún input/TOUCH/ACK/READY crea o confirma automáticamente. Caller debe aplicar efectos sólo al retorno CONFIRMED; IDEMPOTENT no es permiso para repetir efectos. El ledger no implementa motor transaccional de efectos externos ni recuperación persistente del proceso.

## Identidad y estados

ActionKey inmutable = sessionId + deviceSessionId + playerId(1–4) + connectionEpoch + sourceSequence + actionKind(1–64 caracteres alfanuméricos/`_-.`). Identidad no secreta; no admission/resume/SDP. Result immutable contiene ActionId, OutcomeCode y ConfirmedAt Unix ms del reloj Unity; ningún score/damage/combo/winner.

CANDIDATE → CONFIRMED o CANCELLED. Ningún terminal regresa a CANDIDATE. TryBegin requiere identidad observada actual, última secuencia aceptada, input usable ACTIVE y frescura estricta <2000ms; si hay lobby, networkReady/inputReady actuales. PLAYER_READY no confirma acciones. La validación semántica pertenece al futuro caller Unity.

Un solo candidate por sourceSequence por jugador/epoch; otro actionKind del mismo sample también se rechaza. Política conservadora explícita: no admite dos acciones diferentes desde una sola muestra. High-water consumido independiente P1–P4. Una nueva muestra no cancela por sí sola una candidata todavía válida.

## Cancelación, reconexión y carreras

SERVER_STATE distinto de ACTIVE, input unavailable, lobby sin networkReady/inputReady, eliminación de jugador, epoch/dispositivo reemplazado, transporte terminado o cancelación explícita cancelan candidatas del jugador afectado. Confirm vuelve a comprobar frescura, por lo que una candidata vencida tampoco confirma entre polls. La observación ocurre en el boundary recibido; no hay reconstrucción de eventos intermedios omitidos por latest-only ni detección de gestos.

Disconnect antes de Confirm → CANCELLED sin resultado; Confirm antes de disconnect → CONFIRMED con el mismo objeto resultado. Nuevo epoch cancela candidatas anteriores, conserva confirmadas y permite secuencia nueva independiente. Epoch antiguo/identidad incorrecta/entrada tardía no crea candidata; key terminal ya conocido devuelve DUPLICATE. Confirm duplicado mismo OutcomeCode → IDEMPOTENT, distinto → CONFLICT, cancelada → TERMINAL. Cancel de confirmada → TERMINAL.

Todas las observaciones y transiciones comparten lock del ledger; PhoneInputStore mantiene orden de observación con su lock, sin callbacks bajo esos locks. Resultado se publica junto al estado confirmado. No existen estados terminales parciales. Confirm repetido concurrente retorna exactamente un CONFIRMED.

## Memoria y retención

Cuatro slots de identidad actuales y máximo256 registros totales por sesión, incluyendo candidatas/canceladas/confirmadas. Cache sin eviction: todos los resultados confirmados sobreviven disconnect/reconnect/rotación de epoch dentro de la sesión. Al alcanzar256 devuelve CAPACITY y exige reset de sesión/round técnica por la autoridad; no borra resultados para aceptar nuevos. High-water y terminales impiden replay; ningún eviction reabre keys. Cadenas identidad <=128 y claves/outcomes <=64. No dictionary/list creciente: la lista temporal de cancelación está acotada por256. Reinicio del proceso no conserva resultados en disco.

## Acreditación

Evidencia software/lab: [summary](evidence/t015-action-lifecycle-2026-10-09/summary.json). PlayMode usa PWA/Chrome reales con input sintético → RTC → Java → IPC → PhoneInputStore → boundary explícito del ledger. Repite T013 1–4/quinto/post-start/READY/resume/epoch y mantiene regresiones T012/T014. QA física integrada final permanece en T019/Final Exit Audit.

AC originales: AC1 “Desconexión detectada y comunicada; PlayerId conservado.” AC2 “Acción sin validar se cancela y resultado confirmado se conserva.” AC3 “Duplicados/atrasados no reproducen acciones oficiales.” Cierre requiere pruebas/build/checks/security e integración normal; Issue-level PASS != Phase0 final certification. E0 OPEN, Phase0 IN PROGRESS; T016 siguiente Ready sin iniciar.

## Reproducir

Usar Unity6000.3.23f1 instalado/licenciado y copia aislada de Assets/Packages/ProjectSettings/Shared. Ejecutar EditMode completo, PlayMode completo y `GorillaEscape.Editor.FoundationSetup.BuildDevelopmentLinux`. Configuración existente de fixtures: GORILLA_TEST_JAVA/JAR/FIXTURES, GORILLA_TEST_CAMERA_PYTHON/SCRIPT/FIXTURE y GORILLA_TEST_NODE/NODE_ARGUMENTS/PLAYWRIGHT/CHROME. T013 usa GORILLA_TEST_LOBBY_CLIENTS/LOBBY_OUTPUT; T015 usa GORILLA_TEST_ACTION_CLIENTS (`tools/t015/live_clients.mjs`)/ACTION_OUTPUT. Credencial efímera por stdin; paths host se reenvían explícitamente desde Flatpak. No QA física ni instalación nueva. Management: `python3 tools/github/validate_management.py`; Foundation: `python3 tools/validate_foundation.py`. Java/PWA sin cambios; Foundation CI repite sus suites.

Implementación y pruebas: commit `939e17a`; hashes exactos en summary. Review de seguridad: identidades sólo no secretas, resultados/records inmutables, locks sin callbacks, límite estricto256, cuatro jugadores, freshness/epoch/identidad actual, autoridad exclusivamente C#, evidencia agregada sin credenciales ni SDP. Revisión directa PASS.
