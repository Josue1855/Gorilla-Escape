# Gorilla Escape — Decision Log

Este documento registra únicamente decisiones que cambian o aclaran decisiones relevantes del proyecto. La [especificación maestra v4](Gorilla_Escape_Gorilimpiadas_Especificacion_Maestra_v4.md) sigue siendo la fuente principal de verdad.

## Formato

Copiar esta plantilla para cada decisión posterior; reemplazar los campos y asignar un identificador único. No confundir una propuesta con una aprobación.

### DEC-XXX — Nombre

**Fecha:** Pendiente
**Estado:** Proposed / Accepted / Rejected / Superseded
**Decisión anterior:** Pendiente
**Decisión nueva:** Pendiente
**Problema / evidencia:** Pendiente
**Alternativas consideradas:** Pendiente
**Impacto técnico:** Pendiente
**Impacto en alcance:** Pendiente
**Impacto en calendario:** Pendiente
**Impacto en pruebas:** Pendiente
**Responsable:** Pendiente
**Aprobación Product Owner:** Pendiente

## Decisiones registradas

### DEC-001 — Todos los proyectos en el mismo repositorio

**Fecha:** 2026-10-05
**Estado:** Accepted
**Decisión anterior:** La estructura inicial contemplaba Unity/Server/PWA, sin una regla explícita para todos los proyectos propios.
**Decisión nueva:** Todos los proyectos propios, contratos, adaptadores, herramientas, tests y documentos viven en este repositorio. No crear repositorios propios separados ni Git anidado sin una decisión de arquitectura aprobada.
**Problema / evidencia:** Instrucción explícita del usuario: «en el mismo repo se va a tener todos los proyectos».
**Alternativas consideradas:** Repositorios separados; no adoptados por la instrucción directa.
**Impacto técnico:** Organización monorepo, contratos compatibles y validación coordinada. No cambia Unity/ASP.NET/PWA ni el Modular Monolith.
**Impacto en alcance:** Gobierno/documentación; ninguna fase de implementación iniciada.
**Impacto en calendario:** Sin estimación de ahorro comprobada.
**Impacto en pruebas:** Validar consumidores compartidos y builds por componente cuando existan.
**Responsable:** Product Owner y responsables técnicos del proyecto.
**Aprobación Product Owner:** Instrucción directa del usuario en este chat, 2026-10-05.

DEC-001 registra la organización original; el stack de ese momento fue sustituido por DEC-002. Distribución actual implementada y límites: [MONOREPO_PLAN.md](MONOREPO_PLAN.md). Versiones de la base: [TEAM_START.md](TEAM_START.md). Candidatos de cámara, render y empaquetado de la investigación siguen pendientes de pruebas; no se adoptan automáticamente. Bloqueos en [DEVELOPMENT_PROGRESS.md](DEVELOPMENT_PROGRESS.md).

### DEC-002 — Servidor Java y control React

**Fecha:** 2026-10-05. **Estado:** Accepted.
**Decisión anterior:** ASP.NET Core local y PWA sin framework obligatorio.
**Decisión nueva:** Java 21 + Spring Boot 4.1.1 (Web MVC), Maven 3.9.11; PWA React 19.3.0 + Vite 8.3.2.
**Aprobación Product Owner:** solicitud «Java + React; cambiar el servidor a Java» en este chat. Modificación explícita de la arquitectura congelada, reflejada en v4/AGENTS y backlog; no fue una excepción inferida.
**Evidencia:** comp_alumno y base-alumno usan Java 21/Maven/Spring Boot 4.1.1. pwa-alumno no usa React en las ramas revisadas: React se adopta por la instrucción directa del usuario. No se copia su código (licencia de referencia no establecida).
**Motivo:** alineación con herramientas solicitadas para el equipo. Spring Web MVC ofrece hosting local HTTP y una ruta nativa para WebSocket; no se necesita WebFlux para 1–4 controles. Vite compila React a recursos estáticos incluidos en el JAR; no hay Node ni CDN durante gameplay.
**Conservado:** Unity/C#, PC autoritativa, modular monolith, WebSocket/JSON, MediaPipe/OpenCV, sin cuentas/base de datos/cloud obligatorios. Java no puede compartir ensamblados C#; interoperabilidad mediante JSON/fixtures.
**Impacto:** arranque y empaquetado de JVM, compatibilidad Unity ↔ Java y HTTPS/sensores deben validarse durante Spike. No se define aún IPC, propietario del resultado ni cámara nativa. No añade microservicios, JPA, PostgreSQL, Lombok ni Redis.
**Pruebas:** build Java/React y fixtures; tests Unity preparados, pendientes de licencia. No cambia el calendario ni marca Fase 0 completa.

### DEC-005 — Ownership, IPC y lifecycle Unity ↔ Java

**Fecha:** 2026-10-06. **Estado:** Accepted.
**Decisión anterior:** DEC-002 separa runtimes sin definir IPC/lifecycle; v4 §5 usa «confirmado por el servidor» de forma ambigua. DEC-003/004 pertenecen a la foundation del PR #90, no integrado en develop en la consulta de esta tarea; se reserva DEC-005 para evitar colisión de identificadores.
**Decisión nueva:** Unity conserva estado jugable/sesión, física, scoring, torneo y resultados oficiales; Java conserva hosting/transporte/conexiones y adaptación de input. TCP loopback persistente, framing length-prefix/JSON mínimo PING/PONG; Unity supervisa un hijo Java con READY por stdout, cierre por EOF stdin, lock de instancia y recovery acotado.
**Problema / evidencia:** estado real de develop e950bd8, health HTTP insuficiente para readiness IPC, DTO sensor sin contrato de lifecycle y ambigüedad de autoridad. [Documento completo](PHASE0_UNITY_JAVA_DECISION.md) compara opciones, propone contrato, límites, pruebas, métricas y manejo de huérfanos.
**Alternativas consideradas:** WebSocket local, named pipes/UDS, stdin/stdout completo; ninguna medida. No se justifica mover autoridad a Java.
**Impacto técnico:** nuevo supervisor y listener separado; dependencias/versiones actuales conservadas. Enmienda v4 §5 aprobada y aplicada documentalmente. Runtime JRE y compatibilidad Unity/Windows requieren validación.
**Impacto en alcance:** diseño de Incremento 2 de Fase 0 aprobado. Implementación autorizada exclusivamente para 2A después de integración normal de #90 y verificación de develop; todavía no iniciada. Recovery avanzado, watchdog, fault injection extensa y performance/hardening diferidos, no eliminados. Sin PWA móvil, QR, sensores, WebSocket móvil, cámara, gameplay ni Fase 1.
**Impacto en calendario:** sin estimaciones o fechas nuevas; gate de merge #90 obligatorio antes de código.
**Impacto en pruebas:** unit lifecycle/codec, integración Java con cliente real simulado y Unity con JVM real, fault injection y métricas startup/RTT/reconnect/shutdown; todavía NOT RUN.
**Responsable:** PO para autoridad, política de recuperación, plataforma y presupuesto; Arturo/Hiram para revisión productor/consumidor. No se asignan usuarios GitHub ficticios.
**Aprobación Product Owner:** instrucción explícita «Como Product Owner, apruebo DEC-005» de 2026-10-06. Acepta ownership, TCP IPv4 loopback, framing UTF-8/uint32 big-endian/4096 bytes, identidades y token efímero, puerto 0 Java, supervisión Unity, READY + PONG, stdin EOF, deadlines/buffers/cancellation y ninguna dependencia adicional. Autoriza sólo 2A después del merge normal de #90; exige reporte previo de SHA/rama/archivos/tests/riesgos/aceptación. No autoriza modificar #90 ni continuar a 2B.
