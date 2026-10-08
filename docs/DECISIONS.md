# Gorilla Escape — Decision Log

> **Fuente operativa vigente — 2026-10-08:** GitHub Project/Issues/dependencias/AC. Checkpoint técnico Fase0 **PASS — SOFTWARE/LAB**, conservado; **Project Fase0/E0/milestone siguen OPEN**. Fase1 **BLOCKED / NOT STARTED** por #35→#34; los estados READY del audit anterior no autorizan iniciar según Project. [Reconciliación vigente](PHASE0_PROJECT_RECONCILIATION.md).

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

### DEC-003 — Unity Test Framework core de la foundation

**Fecha:** 2026-10-06. **Estado:** Accepted.
**Decisión anterior:** TEAM_START/MONOREPO_PLAN/manifest declaraban Test Framework 1.4.2.
**Decisión nueva:** conservar Unity **6000.3.23f1** y declarar Test Framework **1.6.0**, versión core que distribuye y exige ese editor. NUnit permanece transitivo; la resolución core observada es 2.0.5. Lockfile generado por Unity/UPM, sin edición manual.
**Problema / evidencia:** UNITY-001A-AUDIT verificó minimumVersion 1.6.0/mustBeBundled y resolución efectiva 1.6.0; no provino de los paquetes Linux. UNITY-001 valida compilación/tests/build y reimportación del candidato coherente.
**Alternativas consideradas:** conservar una declaración 1.4.2 incongruente; cambiar de editor; no adoptadas. No se actualiza por novedad.
**Impacto técnico:** manifest, lockfile y documentación coherentes con el editor fijado. SDK/toolchain Linux no aceptados como requisito permanente; el build de desarrollo seleccionado usa Mono y se valida sin ellos mediante UPM y opciones locales de desactivación de instalación/migración del editor. No establece plataforma/backend de presentación.
**Impacto en alcance:** exclusivamente Incremento 1 — Unity Foundation. No modifica gameplay, IPC, Java, WebSocket, sensores ni cámara. Fase 0 sigue abierta.
**Impacto en calendario:** sin cambio de fechas ni estimación nueva.
**Impacto en pruebas:** EditMode, PlayMode, Linux development build/Player y reimportación documentados en UNITY-001. PC oficial pendiente.
**Responsable:** Product Owner para versión; responsable técnico para evidencia/configuración candidata.
**Aprobación Product Owner:** instrucción directa del usuario «Continúa exclusivamente con el Incremento 1 — Unity Foundation», que mantiene 6000.3.23f1 y aprueba 1.6.0, 2026-10-06. No se infiere aprobación de toolchains permanentes ni de plataforma final.

### DEC-004 — Aceptación de foundation en desarrollo y separación del entorno

**Fecha:** 2026-10-06. **Estado:** Accepted.
**Decisión anterior:** el cierre operativo trataba «log completamente limpio» como criterio adicional de aceptación de foundation.
**Decisión nueva:** Unity Foundation de desarrollo **PASS** con import/compilación/paquetes/escena/settings, EditMode 2/2, PlayMode 1/1, build Linux/Mono, Player/cierre y reimportación reproducible aprobados. Los diagnósticos conocidos se clasifican **ENVIRONMENT / REQUIRES FOLLOW-UP**, sin atribuirlos al código sin evidencia ni bloquear integración por sí solos. Seguimiento separado: [#89](https://github.com/Josue1855/Gorilla-Escape/issues/89).
**Problema / evidencia:** UNITY-001 y 76 hashes fuente coincidentes; tests/build/ejecución reales correctos con diagnósticos del launcher/editor y registro nativo de memoria pendientes de investigar.
**Alternativas consideradas:** exigir un log vacío; rechazada explícitamente por el PO. No ocultar mensajes ni afirmar cero leaks.
**Impacto técnico/pruebas:** conservar Unity 6000.3.23f1/Test Framework 1.6.0 y lockfile de cinco paquetes; ningún cambio funcional nuevo. PC/plataforma oficial **BLOCKED / NOT RUN**; FPS real, frame time, latencia, memoria/leaks, sensores, webcam y Player Lock **NOT RUN**.
**Impacto en alcance/calendario:** cierre del trabajo actual de foundation de desarrollo, commit/PR a develop y revisión normal; sin cierre de Fase 0, sin Incremento 2/Fase 1 ni cambio de calendario.
**Responsable / aprobación Product Owner:** instrucción directa «Cierra el trabajo actual de Unity Foundation de desarrollo» en este chat, 2026-10-06. Autoriza commit y PR, no merge forzado ni siguiente incremento.

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

**Aprobación posterior de Incremento 2B (2026-10-06):** el PO aprueba [PHASE0_IPC_2B_DESIGN](PHASE0_IPC_2B_DESIGN.md) con precisiones explícitas. Autoriza implementar exclusivamente lifecycle y fallos básicos desde develop `9fbb566e5f24af8f4e92fa3c139968161e832d46`, estados sin RECONNECTING, PING/PONG a 1 Hz en RUNNING, recuperación manual hasta tres lanzamientos sólo tras cleanup completo, generaciones aisladas, singleton FileChannel.tryLock y presupuestos 15/2/10+2/readers2 s conservados. Abrir PR sin merge automático. Resiliencia avanzada permanece diferida; no autoriza 2C ni Fase 1.

### DEC-006 — PWA y Flutter Android como clientes oficiales del mismo producto

**Fecha:** 2026-10-06. **Estado del requisito de producto:** Accepted por instrucción explícita del PO. **Estado de estrategia técnica HTTPS/onboarding:** Accepted para adaptación software3A; infraestructura pendiente.
**Decisión:** PWA sin instalación obligatoria y Flutter Android opcional/preferido cuando instalado; una sesión/IDs/protocolo/semántica/backend Java, WSS común y autoridad Unity. Un enlace HTTPS/QR con fallback navegador y asociación Android futura; ningún jugador instala CA/perfil, cambia DNS/browser o necesita Flutter. Sustituye limitación nativo sólo Plan B y la aceptación del setup CA móvil de3A; no autoriza implementar Flutter ni ampliar a iOS nativo.
**Impacto:** enmienda v4 y reglas coherentes; [rediseño](PHASE0_PHONE_LAN_ONBOARDING_REDESIGN.md) propone certificado público/FQDN/resolución LAN preparada por operador, con renovación previa online y gameplay local offline sujeto a gate físico. Dominio/router/DNS/443/App Links aún requieren aprobación/pruebas. #94 DRAFT, CA anterior no satisface nuevo gate. Sin dependencias, código, compras, trust/red/firewall ni implementación de otros incrementos.
**Pruebas:** futuras físicas Android/Chrome+iPhone/Safari sin configuración del jugador, primera carga sinWAN, QR web y futura paridad JS/Dart/Java; resultados existentes3A son históricos, no PASS del nuevo gate.

**Precisión posterior del PO — software3A:** autorizada adaptación exclusivamente en #94 DRAFT desde head `50e48418ff286dcd3096c92800fd47f602ad27f2`: FQDN/SAN DNS exacto, cadena/vigencia, IPv4 LAN separada, origen HTTPS443 y escucha Java8443, configuración externa privada. Se conserva supervisor/READY/IPC/EOF/singleton/límites2B. Sin compras, emisión pública, cambios router/firewall, QR/sesiones/WS/Flutter. Procedimiento Linux preparado, no aplicado; tests efímeros no sustituyen confianza pública ni Android/iPhone físicos. 3A IN PROGRESS hasta gate físico completo.


### DEC-007 — Movimiento completo, cámara PC y Spike de transporte móvil

**Fecha:** 2026-10-06. **Estado:** Accepted para requisitos de producto y base de Spike; transporte definitivo pendiente.
**Aprobación:** instrucción del PO en «Investigación técnica de transporte seguro, sensores, cámara PC e Input Fusion». No autoriza código de producto, despliegue ni infraestructura.
**Decisión:** PWA y Flutter Android opcional comparten movimiento/touch, sesión y contratos; Unity posee fusión, calibración aplicada, física y resultados. Java único backend supervisado, IPC exclusivamente 127.0.0.1. Webcam exclusivamente PC para detección corporal; cámara móvil sólo QR, no gameplay/vídeo. Personalización separada usuario/dispositivo/minijuego/reglas competitivas Unity.
**Sustitución limitada de DEC-006:** dominio propio, router específico y configuración manual DNS no son requisitos aceptables del jugador/operador para la arquitectura objetivo portable. WSS no queda impuesto como transporte móvil definitivo: investigar WT y RTC, sin elegir ninguno ni invalidar el IPC Accepted. Preservar pruebas y diseño HTTPS/DNS anteriores como evidencia histórica, no aceptación de solución final nueva.
**Concesión pendiente:** primera carga/preparación online de PWA; la aprobación de investigación NO acepta cambiar el gate de un teléfono nuevo sin WAN. Hosting estático gratuito, bibliotecas, certificados efímeros y Spikes ejecutables requieren autorización de sus pasos.
**Impacto:** sólo documentos de investigación y fuente normativa; ningún runtime/dependencia/rama/PR modificado. #94 OPEN/DRAFT, 3A IN PROGRESS; posteriores NOT STARTED, Fase 0 IN PROGRESS, Fase 1 NOT STARTED.
**Evidencia:** [decisión](PHASE0_SECURE_MOTION_TRANSPORT_DECISION.md), [comparación y gates](PHASE0_SECURE_MOTION_SPIKE_PLAN.md), [contrato y fusión conceptual](PHASE0_INPUT_FUSION_CONCEPT.md). Pruebas físicas de esta arquitectura NOT RUN; no reinterpretar emulador ni métricas IPC.


### DEC-008 — Calibración inmersiva y oportunidades competitivas comparables

**Fecha:** 2026-10-06. **Estado:** Accepted para requisito de producto; algoritmos, thresholds y validación futura Proposed / NOT RUN.
**Aprobación Product Owner:** solicitud explícita «Calibración inmersiva, personalización y justicia competitiva» en este chat.
**Decisión:** calibración individual mediante desafíos breves jugables, no asistente técnico; normalizar intención respecto del rango cómodo para que fuerza, talla y alcance no determinen automáticamente potencia virtual. Separar potencia de precisión/timing/técnica/coordinación. No igualar resultados ni afirmar equilibrio probado.
**Autoridad:** Unity acepta/aplica perfil, límites y snapshot competitivo. Java transporta; PWA/Flutter misma semántica. Webcam sólo PC; sin diagnóstico de fuerza/fatiga ni vídeo/biometría identificable por defecto. Preferencias separadas de reglas de asistencia/equilibrio.
**Dinámica:** observar desviaciones, proponer cambios acotados/recalibración entre rondas con confirmación/revisión; no mutación competitiva silenciosa durante acción/ronda. Límites ante perfiles artificialmente débiles sin excluir movilidad limitada ni prometer prevención total de engaño.
**Impacto y alternativas:** no normalización puramente por aceleración absoluta; no exigir fuerza máxima ni retocar scoring para igualar victorias. Ejemplos deportivos del PO son ilustrativos, no ampliación automática de los minijuegos del MVP. Se conserva foundation/IPC/lifecycle/3A y transporte indeciso.
**Pruebas/alcance:** [diseño, contratos y pruebas futuras](PHASE0_INPUT_FUSION_CONCEPT.md#8-calibración-inmersiva-y-justicia-competitiva--dec-008). Equidad exige participantes físicos diversos, privacidad/consentimiento y seguridad, especialmente menores; simulación no basta. Sólo actualización documental, sin cámara/minijuegos/calibración definitiva ni infraestructura. #94 OPEN/DRAFT, 3A/Fase 0 IN PROGRESS, Fase 1 NOT STARTED.


### DEC-009 — Reconocimiento de intención y supresión de activaciones falsas

**Fecha:** 2026-10-06. **Estado:** Accepted para requisito de producto; detector/umbrales/ventanas Proposed, validación física NOT RUN.
**Aprobación PO:** solicitud explícita «Reconocimiento de intención y prevención de movimientos falsos» en este chat.
**Decisión:** gestos temporales completos y contextualizados, no pico de aceleración aislado; intención distinta de intensidad. Acciones lentas/suaves coherentes válidas, accidentales rápidos rechazables. Unity confirma una acción por gesto, luego recovery/rearme; preparación no ejecuta, duplicados/muestras tardías no redisparan.
**Ownership/fuentes:** Unity detector y física/scoring; Java sólo transporte/sesión técnica. Fuentes móviles y exclusivamente webcam PC, asociación jugador/control/track validada. Sensor-only con criterios apropiados cuando visión no fiable; incertidumbre/ausencia explícitas, no atribuir observación ambigua ni inventar pose.
**Calibración/equidad:** thresholds relativos a ruido/rango cómodo y capacidades, límites competitivos y snapshot fijo por ronda (DEC-008). No fuerza máxima ni valores universales sin medir; touch accesible sin falsa equivalencia de movimiento ni bonus oculto.
**Alternativas/riesgos:** umbral simple, cámara siempre confiable y cooldown universal rechazados como supuestos. Intención observable no es certeza mental; distinguir sensor-only suficiente de gestos inherentemente ambiguos. Boxeo/boliche son ejemplos, no ampliación del MVP.
**Pruebas/alcance:** [estados, contratos y corpus físico futuro](PHASE0_INPUT_FUSION_CONCEPT.md#9-reconocimiento-de-intención-y-prevención-de-activaciones-falsas--dec-009). FPR/FNR, duplicados, prematuros, latencia física y atribución por jugador/dispositivo/modo, sin PASS de emulador como detector físico. Sólo documentación; transporte WT/RTC pendiente, foundation/IPC intactos; #94 OPEN/DRAFT, 3A/Fase 0 IN PROGRESS, Fase 1 NOT STARTED.


### DEC-010 — Alcance deportivo v4 y captura PWA aislada de sensores (identificador histórico)

**Fecha:** 2026-10-07. **Estado:** Accepted para corrección de alcance y ejecución del Spike de adquisición exclusivamente.
**Aprobación PO:** instrucción «Fase 0 — Spike de sensores, calibración inmersiva y reconocimiento de intención».
**Decisión:** cinco juegos oficiales v4 §§17–21: Gorilla Smash (3 intentos), Coconut Throw (3 intentos), Jungle Archery (5 flechas), Coconut Bowling (3 rondas) y Jungle Slice (60 s), por jugador. Sustituir ejemplos operativos boxeo/tenis/golf/penales de diseños anteriores; conservarlos como contexto histórico, no ampliar MVP. Banana Catch sólo propuesta post-MVP, sin modificación de v4 ni torneo.
**Implementación autorizada:** captura/diagnóstico PWA aislados, permisos, campos, timestamps/cadencia/validación y cleanup; sin gameplay/envío, transporte, cámara, Flutter, infraestructura o publicación. [Código y pruebas](../PWA/spikes/sensors/README.md).
**Conservado:** Java único backend, Unity autoridad competitiva, IPC intacto y webcam exclusivamente PC futura. Slice mantiene regla velocidad/trayectoria/confianza e intersección, sin exigir teléfono; Archery exige release de botón, cancel no dispara. Gestos personalizados completos, sin fuerza máxima ni pico aislado como confirmación.
**Evidencia:** capturador 8/8 tests unitarios con eventos/permisos simulados PASS; regresión PWA 5/5 PASS. Sensores/Android/iPhone físicos y recognizers NOT RUN. [Diseño oficial corregido](PHASE0_INPUT_FUSION_CONCEPT.md#10-cinco-disciplinas-oficiales-y-spike-de-captura--corrección-2026-10-07). #94 OPEN/DRAFT, 3A/Fase 0 IN PROGRESS, Fase 1 NOT STARTED; sin commit/push/merge automático.


### DEC-011 — Spike WebTransport 3A-T aislado, no selección definitiva

Estado: Accepted únicamente para experimento, autorización PO 2026-10-07. Java 21/Chrome real, pin efímero, AUTH separada, ecos fiables y datagramas; límites/cleanup y métricas en laboratorio. Área tools/spikes/webtransport, sin dependencias del producto ni cambios al IPC. Biblioteca experimental Netty con nativos; Linux validado, móviles/Windows/offline pendientes. WT definitivo, hosting, preparación inicial y QR permanecen pendientes. [Resultados y límites](PHASE0_SECURE_MOTION_TRANSPORT_DECISION.md#15-spike-ejecutable-3a-t--2026-10-07). No autoriza WebRTC, sensores, cámara ni gameplay.


## Criterio vigente de cierre Software/Lab — aprobado por PO, 2026-10-07

Fase 0 puede cerrar como **COMPLETED — SOFTWARE/LAB** únicamente cuando arquitectura base implementada, integrada y todos los gates automatizados/laboratorio requeridos estén PASS. Esta autorización no equivale a declarar el cierre ahora. iPhone 15/Safari y Android físicos, cadencia móvil real, movimiento humano con teléfono/cámara y latencia física quedan **NOT RUN / DEFERRED**, obligatorios antes de aceptación final del MVP. Windows: DEFERRED. Los intentos fallidos de acceso iPhone y toda evidencia anterior se conservan; no se convierten en PASS. No se vuelve a solicitar teléfono para avanzar.

Orden obligatorio: A comparador WebRTC aislado → B DEC-010 y un transporte → C sesión/admission/QR POC → D protocolo común → E input móvil lab/Java/IPC/Unity → F webcam PC/pose/Unity → G asociación y temporal → H infraestructura Fusion/fixtures → I 1–4 clientes → J resiliencia/regresión → K auditoría → L cierre. No gameplay final, reconocimiento facial, cámara móvil, backend cloud ni Fase 1 antes del cierre. PR #94 permanece DRAFT. Sin push/merge autorizado para este trabajo. Secure-context/onboarding sin configuración del jugador sigue como riesgo independiente; resultados lab no demuestran compatibilidad física.

**Estado actual:** cierre IN PROGRESS; A IN PROGRESS; B–L NOT RUN en esta secuencia. Foundation/IPC y resultados de laboratorio históricos conservados. Fase 1 NOT STARTED; sólo será READY TO START después de auditoría PASS e integración requerida.

## DEC-010 — Mobile Transport — decisión Software/Lab

**Status: Accepted for Phase 0 architecture. Mobile Transport: WebRTC DataChannel.**
Señalización local; no STUN/TURN público obligatorio ni cloud gameplay backend. WebTransport conserva evidencia experimental histórica, sin transporte productivo paralelo.
La entrada anterior «DEC-010 — Alcance deportivo» conserva su identificador histórico; la referencia vigente DEC-010 de transporte designa exclusivamente esta sección.


Estado: **ACCEPTED / IMPLEMENTED FOR PHASE 0 LAB**, 2026-10-07, por aprobación explícita del PO. **WebRTC DataChannel** es el único adaptador móvil de este incremento Software/Lab. Session/QR/protocolo/Java/IPC/Unity y 1–4 clientes PASS en Chrome real sintético/replay; Android Emulator nuevo BLOCKED y físicos/Windows DEFERRED. [Validación del runtime](PHASE0_MOBILE_INPUT_RUNTIME_VALIDATION.md). WT se conserva como Spike histórico aislado; no se elimina evidencia ni se incorpora un segundo transporte productivo. El runtime está integrado localmente en laboratorio; no equivale a integración Git, onboarding resuelto ni PASS iPhone físico.

Motivo: ambos candidatos tienen echo cifrado local demostrado; RTC permite mantener un modelo común compatible con las APIs WebRTC documentadas en Safari/Chrome y un futuro Flutter, sin depender de disponibilidad/pinning WebTransport en Safari. La dependencia JNI y la señalización adicional son concesiones explícitas. No se selecciona por ranking de RTT: las corridas WT anteriores tienen N=20/mensajes pequeños y RTC N=100/padding32/1024, sin igualdad de carga. El comparador queda CLOSED por decisión PO; no se vuelve a comparar salvo regresión demostrada. Físicos diferidos; no se declara superioridad de rendimiento.

Tabla histórica del comparador cerrado; el resultado runtime posterior está en la validación enlazada.

| Criterio | WT existente | RTC comparator |
|---|---|---|
| Browser lab | Chrome/Android emulado histórico PASS | Chromium/Java real PASS; Android RTC NOT RUN |
| Safari físico | NOT RUN / DEFERRED | NOT RUN / DEFERRED; API documentada, no interoperabilidad ensayada |
| Java | Netty/QUIC, código experimental fijado | webrtc-java 0.19.0/JNI Linux, artefactos fijados |
| Señalización | Endpoint + pin + admission metadata | SDP/ICE/fingerprint por intercambio autenticado, implementación productiva pendiente |
| Seguridad | Pin endpoint no crea secure context de página | DTLS transport + auth canal lab; secure context/señalización productivos pendientes |
| LAN | Loopback/misma PC + emulador, física diferida | Candidatos host UDP, sin STUN/TURN configurado, misma PC lab |
| 1–4 clientes | 1 cliente lab documentado; 4 NOT RUN | 1 y 4 peers concurrentes PASS, IDs aislados en comparador |
| Recovery | Manual, evidencia histórica | Manual con peer/credential nueva PASS; automático no implementado |
| Mensajes | Stream fiable + datagrams | Fiable ordered + unordered maxRetransmits=0 PASS |
| Loss nominal | Resultados individuales existentes conservados | 0/2400 en corrida lab, sin extrapolar a presión/Wi-Fi |
| Lifecycle | EOF/cleanup histórico PASS | EOF/exit0, cero peers y peticiones pendientes PASS |
| QR/onboarding | Metadata pin compacta; origen seguro pendiente | SDP no debe meterse indiscriminadamente en QR; retorno autenticado pendiente |
| Mantenimiento | HTTP/3/QUIC/pinning en biblioteca experimental | JNI/libwebrtc y signaling aumentan complejidad; wrapper publicado |
| Binarios/licencias | Inventario WT existente | Apache wrapper + avisos native incluidos; auditoría distribución pendiente |

Fuentes primarias: [WebKit WebRTC](https://webkit.org/blog/7763/a-closer-look-into-webrtc/), [webrtc-java DataChannels](https://jrtc.dev/guide/data/data-channels), [webrtc-java plataforma/dependencias](https://jrtc.dev/guide/get-started). Evidencia: `docs/evidence/webrtc-comparator-2026-10-07/`. Límites y comando repetible: `tools/spikes/webrtc/README.md`. Las concesiones de origen HTTPS seguro/distribución y confianza de signaling siguen abiertas y no se ocultan mediante HTTP LAN o bypass.


## DEC-012 — PC camera / association / temporal / Fusion, implementación Linux Software/Lab

2026-10-08 UTC. **IMPLEMENTED FOR PHASE 0 LAB**, dentro del incremento explícitamente autorizado por PO.
Unity conserva CameraInputStore, Player Lock, alineación y FusionFrame. Java/PWA/IPC/PhoneInput existentes
no cambian contrato ni ownership. Un jugador cámara-activo por turno, cuatro controles conectados.

Adaptador opt-in local MediaPipe Python Tasks0.10.33/OpenCV4.13.0.92 en venv aislado, supervisado por Unity,
private stdin/stdout, EOF/timeout/buffers/cleanup. No SDK C#/Java de visión ya instalado; construir ahora un
plugin C++ ampliaría toolchains y packaging antes del gate. Se justifica únicamente el adaptador Linux de
laboratorio; **Python como runtime distribuido de producto no queda adoptado definitivamente**. Inventario
versiones/licencias/native/model hashes, límites y alternativas de distribución en
[README](../tools/spikes/camera/README.md) y [validación](PHASE0_CAMERA_FUSION_VALIDATION.md).

Landmarks raw no métricos, visibilidad/confianza y fuente explícitas; manos como proxies de pose, sin full
hand detector ni quaternion inventado. Camera/Phone histories bounded, lock conservador sin biometría.
Receipt PC UTC versus camera read-complete PC UTC es proxy de alineación lab; sync físico sigue DEFERRED.
DEC-009 recognizer y DEC-008 calibración deportiva siguen conceptuales, sin umbrales/física/scoring nuevos.
Fuentes/confianza insuficientes degradan, no disparan gesto. Distribución nativa/Windows/human motion quedan
pendientes; ninguna cifra de gesture accuracy/switches físicos se atribuye a los fixtures.

## Registro de cierre audit — 2026-10-08

[FINAL PHASE0 AUDIT](PHASE0_FINAL_AUDIT.md): **PASS — SOFTWARE/LAB**; Fase0 **COMPLETED — SOFTWARE/LAB** y Fase1 **READY TO START**, no iniciada. No nueva decisión de arquitectura ni implementación de gameplay. DEC-010 WebRTC Accepted for Phase0 architecture; WT sólo histórico experimental. 3A físico/onboarding DEFERRED / IN PROGRESS. Deuda física/plataforma abierta, obligatoria antes del MVP. Candidato integrado localmente, no remoto; #94 OPEN/DRAFT, sin commit/push/merge.
