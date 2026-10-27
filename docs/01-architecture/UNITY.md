# Unity y autoridad del juego

Última revisión: 2026-10-16. Fuentes: S1, S2 y S3; identificadores definidos en [inventario de fuentes](../00-product/SOURCE_INVENTORY.md). Las etiquetas describen el alcance indicado, no una certificación global.

S1 fija Unity **6000.3.23f1** en `Unity/ProjectSettings/ProjectVersion.txt`. `Scripts/Core/Bootstrap.cs` y UI dan la entrada; `MainMenuController.PlayEnabled` es false. No hay minijuegos detectados en las fuentes ni una escena de torneo.

`Network/JavaProbeSupervisor.cs` posee el proceso Java hijo, pipes, handshake, deadlines, cancelación y cleanup. `IpcLifecycle` modela sus estados. `LobbyAuthorityBridge` conserva la admisión competitiva en Unity; `PhoneInputStore` consume snapshots sin calcular potencia/efecto. `OfficialActionLedger` no es un sistema de puntuación de Bowling ni prueba de gameplay. `CameraTracking/VisionSupervisor.cs`, `PlayerLock` e `InputFusion` son infraestructura; la asociación física definitiva mantiene sus límites documentados.

Shared/CSharp es una biblioteca de contratos sin referencias UnityEngine. Los DTO wire no deben convertirse en MonoBehaviour ni introducir dependencias Java/React al assembly del motor. [IPC](NETWORK_AND_IPC.md) conserva versión y límites.

S2 registra 162/162 EditMode, 29/29 PlayMode y build Linux sin errores/advertencias. Son pruebas previas del candidato integrado, no nuevas ejecuciones del encargo documental ni evidencia de PC limpia. Para implementar juegos, usar [framework propuesto](../02-gameplay/MINIGAME_FRAMEWORK.md), coordinar escenas/prefabs y conservar .meta. Ninguna lógica del prototipo UI se adopta como regla oficial.

## Propuesta UI/UX para revisión

El [Design System Unity/PWA](UI_UX_DESIGN_SYSTEM.md) define dirección artística, pantallas, estados, componentes y backlog UX-01..12 como propuesta. No aprueba tokens, reglas, cola idempotente ni clases existentes. La revisión y trazabilidad se enlazan desde ese documento; no se duplican issues ni se cambia el transporte por este registro.
