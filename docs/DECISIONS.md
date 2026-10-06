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
