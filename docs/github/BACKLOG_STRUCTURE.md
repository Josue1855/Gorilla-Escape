# Gorilla Escape — Estructura del Product Backlog

**Fuente principal:** [v4](../Gorilla_Escape_Gorilimpiadas_Especificacion_Maestra_v4.md), no sustituida ni alterada.
**Estado inicial:** todo Backlog; sin assignees, prioridades, puntos ni Iteration asignados. Estos valores los define el equipo durante planificación.

## Descomposición

Epic → Feature → Task/Spike/Documentation. Bugs se crean por fallos observados, no por suposiciones. Hay 88 elementos: 8 Epics, 21 Features, 48 Tasks, 6 Spikes y 5 Documentation. Task/Spike/Documentation suman 59 unidades; no contar Epics/Features como esfuerzo adicional.

Fase 0 concentra HTTPS/WSS, QR, sensores, WebSocket v1, sesión/admisión, contratos, adaptador Unity, cámara, offline, cuatro controles, mediciones, recuperación y build limpia. La salida global de Spike/slice sigue condicionada a la decisión T001 sobre §§42/50; no se marca base técnica como arquitectura validada antes del slice.

Los elementos futuros agrupan candidatos P1/P2 para revisión. No autorizan implementación, release nueva o cambio de stack. Reconocimiento facial está explícitamente prohibido; replays no pueden almacenar webcam. Opcionales comerciales/roadmap no se convierten en requisitos nuevos del MVP.

## Cantidades planificadas por fase

| Fase | Epics | Features | Tasks | Spikes | Documentation | Total |
|---|---:|---:|---:|---:|---:|---:|
| 0 | 1 | 6 | 14 | 4 | 2 | 27 |
| 1 | 1 | 1 | 4 | 0 | 0 | 6 |
| 2 | 1 | 3 | 6 | 0 | 0 | 10 |
| 3 | 1 | 4 | 12 | 0 | 0 | 17 |
| 4 | 1 | 2 | 5 | 0 | 0 | 8 |
| 5 | 1 | 1 | 3 | 0 | 0 | 5 |
| 6 | 1 | 2 | 4 | 0 | 1 | 8 |
| Futuro | 1 | 2 | 0 | 2 | 2 | 7 |

## Trazabilidad de requisitos

| Fuentes v4 | Cobertura |
|---|---|
| §§3–10, 14, 41–43, 57 | F00–F05: decisiones, hardware, versiones, monorepo, host, PWA, protocolo, permisos, red, cámara y evidencia |
| §§11–13, 17, 23, 42 | F10: slice Gorilla Smash con input real y aceptación |
| §§5–16, 34–35, 40 | F20–F22: sesión/jugadores/calibración, motion/fusion, GP/resultados/persistencia |
| §§18–21, 23, 49 | F30–F33: cuatro disciplinas restantes en orden, loop completo y pruebas |
| §§1–2, 5–7, 15, 24–33, 54 | F40–F41: narrativa/torneo, identidad, menús, móvil, arte/audio/accesibilidad/seguridad/privacidad |
| §§14, 22, 26–27, 35–37, 47, 53 | F50: tuning, game feel, rendimiento y comprensión |
| §§37–39, 44, 49, 54–57, Anexo C | F60–F61: QA, aceptación, build, documentación/evidencia y presentación con estimaciones comerciales identificadas |
| §§2, 22, 28, 31, 43–46 | FF1–FF2: opcionales y futuro condicionado |

Prioridades Critical/High/Medium/Low vacías para PO; P0/P1/P2 de alcance queda en Context. La fase de cada Issue es §50, no la prioridad P0. Se reporta la discrepancia del prompt adjunto y se conserva la semántica de la fuente de verdad.

## Dependencias y granularidad

Las dependencias técnicas son blocked-by nativas: versión/hardware antes de build, HTTPS antes de permiso útil, contrato antes de adaptadores, input antes de mecánica, slice antes de generalización, validación de cada disciplina antes de la siguiente y QA antes de entrega. Parent organiza jerarquía, sin bloquear a un hijo por su propio padre.

El marker gorilla-backlog:KEY es identificador estable para importar sin duplicados. Cada Issue contiene los diez apartados pedidos y referencia de sección. La semilla completa está en [backlog.json](backlog.json); IDs/URLs y cantidades creadas se verifican en [publish_receipt.json](publish_receipt.json). Si un import se interrumpe, reanudar reutilizando los mismos markers.

## Índice

- **E0 · Epic · Fase 0** — Base técnica y viabilidad (v4 §50, §2, §49).
- **E1 · Epic · Fase 1** — Vertical Slice Gorilla Smash (v4 §50, §2, §49).
- **E2 · Epic · Fase 2** — Sistemas reutilizables (v4 §50, §2, §49).
- **E3 · Epic · Fase 3** — Cuatro minijuegos restantes (v4 §50, §2, §49).
- **E4 · Epic · Fase 4** — Integración del torneo (v4 §50, §2, §49).
- **E5 · Epic · Fase 5** — Polish y balance (v4 §50, §2, §49).
- **E6 · Epic · Fase 6** — Freeze, QA y entrega (v4 §50, §2, §49).
- **EFuturo · Epic · Fase Futuro** — Opcionales y roadmap fuera del MVP (v4 §50, §2, §49).
- **F00 · Feature · Fase 0** — Decisiones de viabilidad y hardware (v4 §3, §41, §42, §43, §50, §57).
- **T001 · Documentation · Fase 0** — Aclarar frontera Spike / Vertical Slice y autoridad de resultados (v4 §3.3, §5, §42, §50, §57).
- **T002 · Documentation · Fase 0** — Inventariar PC, webcam, teléfonos, red y navegadores oficiales (v4 §41, §57).
- **T003 · Spike · Fase 0** — Validar versiones Unity/Java y ruta de integración nativa (v4 §3, §42, §57).
- **F01 · Feature · Fase 0** — Monorepo y base ejecutable (v4 §3, §4, §39, §50).
- **T004 · Task · Fase 0** — Crear base Unity con Bootstrap y separación inicial (v4 §3, §4, §50).
- **T005 · Task · Fase 0** — Preparar host Java/Spring Boot local y ciclo de vida (v4 §3, §14, §25, §40, §50).
- **T006 · Task · Fase 0** — Configurar contratos React/Java/Unity y fixtures JSON v1 (v4 §8, §9, §10).
- **T007 · Task · Fase 0** — Configurar CI por componentes y build reproducible inicial (v4 §38, §39, §49).
- **F02 · Feature · Fase 0** — Control móvil seguro y offline (v4 §7, §8, §24, §33, §42, §47).
- **T008 · Spike · Fase 0** — Resolver HTTPS/WSS local e incorporación por QR (v4 §3, §7, §24, §42, §47).
- **T009 · Task · Fase 0** — Crear shell PWA y flujo de permisos (v4 §24, §25, §32, §42).
- **T010 · Task · Fase 0** — Capturar aceleración, giro y orientación con frecuencia efectiva (v4 §8.4, §8.5, §10, §14, §36, §37).
- **T011 · Spike · Fase 0** — Validar PWA/red sin WAN y recuperación de visibilidad (v4 §3, §24, §25, §33, §42).
- **F03 · Feature · Fase 0** — Red local y adaptador Unity (v4 §5, §6, §7, §8, §9, §25).
- **T012 · Task · Fase 0** — Implementar WebSocket v1 y validación de entrada (v4 §8, §14, §40).
- **T013 · Task · Fase 0** — Implementar admisión y asociación de controles al lobby (v4 §5, §6, §7, §8).
- **T014 · Task · Fase 0** — Conectar adaptador Unity a PhoneInput (v4 §3.4, §9, §42).
- **T015 · Task · Fase 0** — Implementar heartbeat, reconexión y cancelación segura (v4 §5, §8.6, §8.7, §25).
- **F04 · Feature · Fase 0** — Cámara local y viabilidad de tracking (v4 §3, §9, §12, §33, §42, §43).
- **T016 · Task · Fase 0** — Integrar webcam, MediaPipe/OpenCV y CameraInput (v4 §3, §9, §14, §33, §36, §42).
- **T017 · Task · Fase 0** — Validar ROI, Player Lock y recuperación de tracking (v4 §9, §12, §25, §37, §43).
- **F05 · Feature · Fase 0** — Evidencia de viabilidad técnica end-to-end (v4 §36, §37, §38, §41, §42, §43).
- **T018 · Task · Fase 0** — Instrumentar latencia, Hz, FPS y errores locales (v4 §35, §36, §40, §47).
- **T019 · Task · Fase 0** — Ejecutar validación base con cuatro teléfonos y fallos (v4 §25, §36, §37, §42).
- **T020 · Spike · Fase 0** — Validar build mínima en PC limpia y dictamen de viabilidad (v4 §39, §41, §42, §43, §57).
- **F10 · Feature · Fase 1** — Gorilla Smash de extremo a extremo (v4 §17, §23, §26, §27, §42, §49).
- **T021 · Task · Fase 1** — Construir gesto Smash con calibración y validación mínima (v4 §11, §12, §13, §17, §32).
- **T022 · Task · Fase 1** — Implementar Smash: intentos, scoring y loop de resultado (v4 §17, §49).
- **T023 · Task · Fase 1** — Integrar tutorial, escena y feedback del slice (v4 §1, §17, §23, §26, §27, §32).
- **T024 · Task · Fase 1** — Validar slice Smash con hardware y jugadores externos (v4 §36, §37, §42, §49, §54).
- **F20 · Feature · Fase 2** — Sesión, jugadores y calibración reutilizables (v4 §5, §6, §7, §13, §14).
- **T025 · Task · Fase 2** — Generalizar sesión y jugadores con estado único (v4 §5, §6, §7).
- **T026 · Task · Fase 2** — Generalizar calibración adaptativa y configuración central (v4 §13, §14, §53).
- **F21 · Feature · Fase 2** — Motion Analyzer y Input Fusion reutilizables (v4 §9, §10, §11, §12, §14).
- **T027 · Task · Fase 2** — Generalizar análisis temporal, filtros y coordenadas (v4 §9, §10, §11, §53).
- **T028 · Task · Fase 2** — Generalizar fusión, InputValidator y recuperación (v4 §9, §12, §25).
- **F22 · Feature · Fase 2** — Scoring, GP, resultados y persistencia comunes (v4 §15, §16, §34, §35, §40).
- **T029 · Task · Fase 2** — Implementar Gorilla Points y desempates comunes (v4 §15, §16).
- **T030 · Task · Fase 2** — Generalizar resultados, JSON local y telemetría (v4 §16, §24, §34, §35, §40).
- **F30 · Feature · Fase 3** — Coconut Throw completo (v4 §18, §23, §49).
- **T031 · Task · Fase 3** — Implementar mecánica y scoring de Coconut Throw (v4 §18, §14, §32).
- **T032 · Task · Fase 3** — Integrar tutorial, feedback, resultado y turnos de Coconut Throw (v4 §18, §23, §24, §26, §27, §49).
- **T033 · Task · Fase 3** — Validar Coconut Throw con input real y recuperación (v4 §18, §25, §37, §38, §49).
- **F31 · Feature · Fase 3** — Jungle Archery completo (v4 §19, §23, §49).
- **T034 · Task · Fase 3** — Implementar mecánica y scoring de Jungle Archery (v4 §19, §14, §32).
- **T035 · Task · Fase 3** — Integrar tutorial, feedback, resultado y turnos de Jungle Archery (v4 §19, §23, §24, §26, §27, §49).
- **T036 · Task · Fase 3** — Validar Jungle Archery con input real y recuperación (v4 §19, §25, §37, §38, §49).
- **F32 · Feature · Fase 3** — Coconut Bowling completo (v4 §20, §23, §49).
- **T037 · Task · Fase 3** — Implementar mecánica y scoring de Coconut Bowling (v4 §20, §14, §32).
- **T038 · Task · Fase 3** — Integrar tutorial, feedback, resultado y turnos de Coconut Bowling (v4 §20, §23, §24, §26, §27, §49).
- **T039 · Task · Fase 3** — Validar Coconut Bowling con input real y recuperación (v4 §20, §25, §37, §38, §49).
- **F33 · Feature · Fase 3** — Jungle Slice completo (v4 §21, §23, §49).
- **T040 · Task · Fase 3** — Implementar mecánica y scoring de Jungle Slice (v4 §21, §14, §32).
- **T041 · Task · Fase 3** — Integrar tutorial, feedback, resultado y turnos de Jungle Slice (v4 §21, §23, §24, §26, §27, §49).
- **T042 · Task · Fase 3** — Validar Jungle Slice con input real y recuperación (v4 §21, §25, §37, §38, §49).
- **F40 · Feature · Fase 4** — Torneo completo y flujo de producto (v4 §1, §5, §7, §15, §54).
- **T043 · Task · Fase 4** — Integrar secuencia del torneo, transiciones y final (v4 §1, §5, §7, §15, §54).
- **T044 · Task · Fase 4** — Integrar narrativa funcional, menús y Modo Fiesta Próximamente (v4 §1, §2, §28, §54, §57).
- **F41 · Feature · Fase 4** — Presentación común, móvil y accesibilidad P0 (v4 §24, §25, §28, §29, §30, §31, §32).
- **T045 · Task · Fase 4** — Producir cuatro avatares y assets/animaciones P0 (v4 §28, §29, §57).
- **T046 · Task · Fase 4** — Integrar UI móvil de turnos, resultados y errores (v4 §24, §25, §31).
- **T047 · Task · Fase 4** — Integrar audio, accesibilidad básica y seguridad (v4 §30, §31, §32, §33).
- **F50 · Feature · Fase 5** — Balance, game feel y rendimiento del MVP (v4 §14, §22, §26, §27, §36, §47, §53).
- **T048 · Task · Fase 5** — Ajustar curvas, filtros y balance de cinco disciplinas (v4 §14, §17, §18, §19, §20, §21, §22, §53).
- **T049 · Task · Fase 5** — Pulir feedback audiovisual y cámaras virtuales (v4 §16, §26, §27, §31).
- **T050 · Task · Fase 5** — Optimizar rendimiento y experiencia de incorporación (v4 §35, §36, §37, §47).
- **F60 · Feature · Fase 6** — QA de aceptación y cierre de defectos (v4 §37, §38, §49, §54, §Anexo C).
- **T051 · Task · Fase 6** — Ejecutar matriz cuantitativa y pruebas de regresión (v4 §37, §38, §49).
- **T052 · Task · Fase 6** — Validar experiencia externa, seguridad y privacidad (v4 §31, §32, §33, §37, §54, §55).
- **T053 · Task · Fase 6** — Cerrar bugs reales y verificar aceptación P0 (v4 §2, §38, §49, §56).
- **F61 · Feature · Fase 6** — Build final, evidencia y presentación (v4 §39, §41, §49, §54, §57).
- **T054 · Task · Fase 6** — Preparar build final y documentación de uso/compatibilidad (v4 §39, §41, §49, §57).
- **T055 · Documentation · Fase 6** — Preparar evidencia y presentación del MVP para aceptación (v4 §44, §49, §54, §57).
- **FF1 · Feature · Fase Futuro** — Opcionales P1 — sólo tras completar P0 (v4 §2, §22, §28, §31, §46).
- **T056 · Spike · Fase Futuro** — Evaluar feedback extra, animaciones y móvil enriquecido (v4 §2, §26, §28).
- **T057 · Spike · Fase Futuro** — Evaluar récords, estadísticas, asistencia y accesibilidad ampliada (v4 §2, §22, §31, §34, §46).
- **FF2 · Feature · Fase Futuro** — Roadmap P2 condicionado fuera del MVP (v4 §2, §43, §44, §45, §46).
- **T058 · Documentation · Fase Futuro** — Catalogar Fiesta, contenido, personalización y eventos futuros (v4 §2, §44, §45, §46).
- **T059 · Documentation · Fase Futuro** — Catalogar opciones de plataforma y red bajo revisión de alcance (v4 §2, §33, §43, §45, §46, §52).

DEC-002 (2026-10-05): stack enmendado por el usuario a Java + React. Issues #12, #15 y #16 actualizadas conservando IDs, jerarquía, dependencias y estado abierto. La base aporta parte de su aceptación; no se cierran sin validaciones restantes.
