# Validación del vertical slice de Desafío del Tronco

Fecha local: 2026-10-20. Rama exclusiva: `feature/gameplay-desafio-del-tronco`. Base: `1039cfe` (develop tras PR #124). No se integró a develop/main ni se abrió un PR para esta iteración.

Actualización posterior de arte: las capturas y descripciones geométricas de este informe corresponden a la primera iteración. El escenario con los GLB suministrados y el rig del náufrago se registra en [arte y animación](LOG_BALANCE_ART.md).

## Reutilización y alcance

Se inspeccionaron `Unity/Assets/_Project`, Shared, captura PWA y recepción Java antes de implementar. Se reutilizan Bootstrap, menú, `QaRuntimeService` y su supervisor Java, admisión de `LobbyAuthorityBridge`, `PhoneInputStore` y `PhoneInputWire`. El tilt consume gamma disponible, identifica sesión/dispositivo/epoch, comprueba freshness y guarda una posición neutra explícita. El quaternion del adaptador general sigue declarado ausente; no se inventa una orientación nueva.

La infraestructura de Fusion existente asocia teléfono/cámara, pero no interpreta gestos ni lateralidad para juegos. Este prototipo necesita sólo inclinación lateral y no exige un lock de webcam para calcular equilibrio; conserva el lifecycle existente. No se modificaron PWA, Java, contratos Shared, paquetes ni configuración del proyecto. No existía un framework deportivo/resultados implementado; el resultado local no suplanta al ledger de acciones técnicas ni añade backend/persistencia.

El protagonista es humano náufrago. Los gorilas son espectadores NPC. Al no existir modelos/animaciones reutilizables en esta rama, el escenario usa primitives originales y un material generado con Unity; no se importaron recursos externos.

## Pruebas ejecutadas

| Comprobación | Resultado y límite |
|---|---|
| Unity 6000.3.23f1, importación/compilación | PASS; nuevos `.meta` y ScriptableObject/material generados por Unity |
| EditMode completo | 180/180 PASS, 0 skipped; incluye 18 casos del minijuego/adaptador |
| PlayMode completo inicial | 30/30 PASS, 0 skipped; incluye navegación y flujo del prototipo, procesos Java/IPC, navegador RTC y cámara existentes |
| PlayMode específico final | 2/2 PASS después de añadir pausa/reconexión y corregir atajos; cubre dos pruebas de flujo, una ya incluida en la suite anterior |
| Linux Development Build final | Succeeded; 0 errores, 0 advertencias |
| PWA | 69/69 PASS y build de producción PASS; no se cambió su código |
| Maven verify | BUILD SUCCESS; 112 casos, 0 fallos, 0 errores y 4 omitidos por condiciones de entorno |
| Helper firewall | 11/11 PASS, sin elevación ni modificación de reglas del host |
| Cámara: pruebas Python | 23/23 PASS |
| Preview: pruebas Python | 2/2 PASS |
| Foundation y gestión | PASS; GUIDs, contratos, metadatos, enlaces y backlog conservados |
| Higiene del cambio | `git diff --check` PASS |

Las cuatro omisiones Maven son dos casos `MobileHttpsTest` que exigen `gorilla.test.lanAddress`, `RealQuickTunnelTest` que exige `GORILLA_REAL_TUNNEL` y `BootstrapRtcLabTest` que exige `GORILLA_TEST_NODE`. No se reclasifican como aprobados. La suite PlayMode sí configuró sus fixtures Java/Node/Chrome y verificó las regresiones RTC existentes. Esas pruebas no acreditan sensores físicos del nuevo minijuego.

Los casos nuevos cubren input neutro con perturbación desactivada, direcciones, zona muerta, smoothing, clamp, tiempo fuera del límite, recuperación, victoria, caída, exclusión de resultados terminales, countdown, reinicio, identidad, freshness, orientación inválida, recalibración tras nuevo epoch y control corrector capaz de terminar con los valores predeterminados a distintas cadencias.

En PlayMode se prueba además la admisión de un slot y muestras sintéticas en el store real usado por el controller: calibrar, avanzar, inclinar, perder datos, pausar sin avanzar, reconectar con otro epoch y exigir calibración antes de reanudar. Es un replay de la frontera Unity, no un teléfono físico.

## Comprobación gráfica

Se construyó y abrió el Player Linux con OpenGL y se inspeccionó la presentación del escenario: humano diferenciado, cuatro gorilas NPC, tronco, río, orillas, meta, barra de balance, progreso y controles. Se corrigió la interacción entre Espacio y el botón IMGUI con foco para que un atajo de partida no active también navegación.

La comprobación automatizada de teclado se ejecutó en una pantalla virtual aislada con el Player final y entrada X11 real: reinicio con R, inicio con Espacio, countdown, correcciones A/D hasta ganar en 24,0 s y 18,0 m (100 %), nuevo intento y D sostenida hasta caer en 1,5 s y 1,2 m (6 %). El controlador corrector de prueba mantuvo el balance observado entre −0,186 y +0,261 durante el cruce. No se inyectó una victoria al modelo para estas capturas.

Capturas reales del ejecutable: [cuenta regresiva](../evidence/log-balance-2026-10-20/countdown.png), [victoria](../evidence/log-balance-2026-10-20/victory.png), [caída](../evidence/log-balance-2026-10-20/fall.png). El teclado y las pruebas sintéticas no sustituyen la validación física del teléfono.

## Cómo repetir

Seguir [la guía del minijuego](LOG_BALANCE.md) para abrir Bootstrap, usar teclado, conectar y calibrar el teléfono y ajustar el asset de reglas. No hay montaje manual de una escena nueva. Editor y Player Development permiten teclado por elección explícita. Un build sin los runtimes técnicos sirve para teclado; el teléfono necesita la preparación de Java/conectividad del producto integrado.

Para pruebas automatizadas, usar Test Runner EditMode/PlayMode. Las suites de proceso existentes requieren los fixtures Java, Node/Chrome y cámara indicados en sus runbooks; no omitirlos para declarar aprobación completa. El test específico es `LogBalanceFlowTests`; los casos puros son `LogBalanceTests`.

## Limitaciones pendientes

- Sensación, dirección y estabilidad del control con iPhone/Android físico: NOT RUN para este juego; calibrar y ajustar con hardware real antes de aceptarlo.
- Se admite teléfono vertical con parte superior arriba; orientación horizontal/invertida bloquea input y pausa.
- Resultado sólo en memoria del intento; sin GP, torneo, publicación al backend ni persistencia.
- Arte geométrico provisional y sin reacciones animadas de los espectadores.
- No se validaron builds Windows/macOS ni una PC limpia. T019/T020/E0 conservan su aceptación independiente.

## Archivos de esta iteración

La lista siguiente incluye las fuentes, recursos, pruebas, documentación y metadatos generados, con rutas relativas al repositorio.

### Modificados

- `README.md`
- `Unity/Assets/_Project/Scripts/UI/MainMenuController.cs`
- `Unity/Assets/_Project/Scripts/UI/MainMenuView.cs`
- `docs/00-product/DEVELOPMENT_STATUS.md`
- `docs/00-product/SCOPE.md`
- `docs/02-gameplay/MINIGAME_FRAMEWORK.md`
- `docs/README.md`
- `docs/TEAM_START.md`

### Creados

- `Unity/Assets/_Project/Editor/LogBalanceSetup.cs`
- `Unity/Assets/_Project/Editor/LogBalanceSetup.cs.meta`
- `Unity/Assets/_Project/Resources.meta`
- `Unity/Assets/_Project/Resources/LogBalanceSettings.asset`
- `Unity/Assets/_Project/Resources/LogBalanceSettings.asset.meta`
- `Unity/Assets/_Project/Resources/LogBalanceStage.mat`
- `Unity/Assets/_Project/Resources/LogBalanceStage.mat.meta`
- `Unity/Assets/_Project/Scripts/Minigames.meta`
- `Unity/Assets/_Project/Scripts/Minigames/LogBalance.meta`
- `Unity/Assets/_Project/Scripts/Minigames/LogBalance/LogBalanceController.cs`
- `Unity/Assets/_Project/Scripts/Minigames/LogBalance/LogBalanceController.cs.meta`
- `Unity/Assets/_Project/Scripts/Minigames/LogBalance/LogBalanceModel.cs`
- `Unity/Assets/_Project/Scripts/Minigames/LogBalance/LogBalanceModel.cs.meta`
- `Unity/Assets/_Project/Scripts/Minigames/LogBalance/LogBalanceSettings.cs`
- `Unity/Assets/_Project/Scripts/Minigames/LogBalance/LogBalanceSettings.cs.meta`
- `Unity/Assets/_Project/Scripts/Minigames/LogBalance/LogBalanceStage.cs`
- `Unity/Assets/_Project/Scripts/Minigames/LogBalance/LogBalanceStage.cs.meta`
- `Unity/Assets/_Project/Scripts/Minigames/LogBalance/LogBalanceTilt.cs`
- `Unity/Assets/_Project/Scripts/Minigames/LogBalance/LogBalanceTilt.cs.meta`
- `Unity/Assets/_Project/Tests/EditMode/LogBalanceTests.cs`
- `Unity/Assets/_Project/Tests/EditMode/LogBalanceTests.cs.meta`
- `Unity/Assets/_Project/Tests/PlayMode/LogBalanceFlowTests.cs`
- `Unity/Assets/_Project/Tests/PlayMode/LogBalanceFlowTests.cs.meta`
- `docs/02-gameplay/LOG_BALANCE.md`
- `docs/02-gameplay/LOG_BALANCE_VALIDATION.md`
- `docs/evidence/log-balance-2026-10-20/countdown.png`
- `docs/evidence/log-balance-2026-10-20/fall.png`
- `docs/evidence/log-balance-2026-10-20/victory.png`
