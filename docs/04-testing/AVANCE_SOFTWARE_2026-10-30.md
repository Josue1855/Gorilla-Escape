# Avance de software — 30 de octubre de 2026

Base de trabajo: rama `feature/saul-jungle-party-alpha`, commit `fd350056019b505dcd368efef007c5f1feb494d0`, más los cambios locales de este avance. No representa una integración en `develop` ni aceptación física.

## Trabajo realizado

- #47: `GorillaPointsLedger` recibe posiciones ya resueltas para 1–4 jugadores, asigna 10/7/5/3 GP, impide duplicar una disciplina y ordena por puntos y cantidad de primeros, segundos y terceros lugares. Un empate persistente devuelve todos los líderes: no inventa un campeón ni ejecuta Sudden Smash. Todavía no está conectado al torneo, no decide cómo resolver empates de una disciplina y no cierra #47.
- #58: cinco pruebas EditMode ejercitan el controlador real de Bowling con partida perfecta, ceros, strike/spare pendientes y bolas extra del décimo frame. Describen los diez frames existentes y no deciden entre ese formato y las tres rondas de #58.
- #127: `JoinDiagnosticTest` usa un módulo de audio dummy mediante un constructor de prueba; producción sigue usando su fábrica normal. La prueba IPC registra categorías limitadas de fallos del proceso hijo ante EOF prematuro. Otras pruebas y procesos aún usan la configuración normal; faltan las ejecuciones repetidas en Ubuntu y la investigación del EOF de CI. No cerrar #127.
- Cámara: una prueba de aviso de pérdida esperaba que transcurrieran 0,3 s después de sólo 0,11 s de una nueva pérdida. Se corrigió la expectativa y se conservó la prueba de aviso transcurridos 0,31 s.
- Navegación: la prueba PlayMode que necesita paquete y script de cámara informa `NOT RUN` si faltan. Si están definidos, continúa ejecutando el recorrido de vista previa y sus aserciones.
- README y guía de equipo distinguen esta rama alpha de la base histórica de `develop`. La [ficha de revisión de reglas](REVISION_REGLAS_JUEGOS.md) deja explícitas las decisiones que el usuario desea revisar y el catálogo aún no definido.

## Verificación

| Comprobación | Resultado |
| --- | --- |
| Unity 6000.6.0f1, EditMode, filtro `BowlingScoringTests` | 5/5 |
| Unity 6000.6.0f1, EditMode completo, antes de añadir el ledger | Primer intento: 208/209, con el fallo de expectativa de cámara anterior. Tras corregirlo: 209/209 |
| Unity 6000.6.0f1, PlayMode `MainMenuNavigationTests` | Primer intento: 7 aprobadas y 1 fallida por falta de paquete de cámara. Después de distinguir la precondición: 7 aprobadas y 1 omitida; preview sin ejecutar |
| Harness C# `dotnet run --project tools/integration/jungle-phase0/JunglePhase0.Checks.csproj` | 126/126, incluida lógica pura del ledger; entradas sintéticas |
| Servidor `mvnw.cmd -q -Dtest=JoinDiagnosticTest,LobbyIpcProcessTest test` | 4/4 en Windows, Java 27; no acredita JDK 21 ni Ubuntu |
| PWA `npm test` | 93/93 en Node 26; Node 24 sigue siendo la versión soportada |
| `python tools/validate_foundation.py` y `python tools/github/validate_management.py` | PASS, 702 GUID únicos y 1087 enlaces locales en ese momento |

Estos resultados no acreditan dispositivos físicos, un build distribuible ni un torneo completo. La prueba de preview necesita un paquete de cámara configurado y evidencia separada. El ledger aún necesita integración de UI/sesión, reglas para resultados de cada disciplina y una decisión para el desempate final.
