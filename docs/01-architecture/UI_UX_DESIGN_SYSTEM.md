# Gorilla Escape: Gorilimpiadas — UI/UX Design System

**Estado: PROPUESTA PARA REVISIÓN DEL EQUIPO.** Alcance: Unity (PC/TV) y PWA React (teléfonos). Fuente original del PO: 2026-10-16; revisión documental posterior en esta rama.

**Precedencia:** especificación de dirección visual y criterios candidatos, no adopción de tokens, pantallas o reglas. Los contratos Shared/IPC/WebRTC vigentes prevalecen hasta aprobar y probar cambios. [Revisión de diseño y brechas](../04-testing/UI_UX_DESIGN_REVIEW.md).

## Estado comprobado al incorporar la propuesta

| Alcance | Situación verificable |
|---|---|
| Backend de jugadores | SQLite/JDBC aprobado expresamente por el PO e implementado con GET/POST y migración; [PR backend #14](https://github.com/Josue1855/Gorilla-Escape-Backend/pull/14). El PR #14 ya se integró en develop; el PR #12 de evaluación fue cerrado sin merge. |
| PWA de laboratorio | Fork con Service Worker compilado, snapshot/borrador IndexedDB y GET/POST reales, probados en escritorio; [PR del fork #7 integrado en develop](https://github.com/Josue1855/Dise-o_pwa/pull/7). Sin Background Sync ni idempotencia POST; incorporación upstream pendiente. |
| Diseño de producto completo | Menús/HUD, estados y controles por disciplina descritos abajo son objetivos. Los colores del laboratorio o la demo no constituyen una paleta oficial ratificada. |
| Permisos de repositorio UI/UX | Upstream sólo READ/main; [solicitud de develop y revisión](https://github.com/Quintana-117/Dise-o_pwa/issues/1) abierta. No se modifica main ni se presupone autorización de merge. |

Este documento define una dirección de diseño y criterios de aceptación. No certifica que las pantallas, animaciones o controles ya estén implementados. Los tokens visuales concretos deben aprobarse mediante prototipos y pruebas en dispositivos reales. No reutilizar materiales procedentes de Google Playground.

## 1. Objetivo y principios

Crear una experiencia única, reconocible y accesible para un videojuego social de deportes tropicales, con atletas humanos controlados por los jugadores y gorilas como NPC, rivales, entrenadores, mascotas o espectadores, según cada escena. La apariencia debe ser low-poly estilizada y orgánica, con volúmenes y siluetas legibles, sin depender de bloques y conos evidentes como aspecto final.
Principios: facilidad de uso desde el sofá; acciones importantes visibles; estados explícitos; interfaces rápidas, estables y coherentes; controles adaptados al minijuego; funcionalidad LAN sin Internet; una sola autoridad de gameplay en Unity. La interfaz PWA captura intenciones y presenta información, pero no calcula ni adjudica puntuaciones oficiales.

## 2. Dirección artística propuesta

- Ambiente: isla tropical deportiva, vegetación estilizada, madera, piedra, arena, cocos y escenarios temáticos. No confundir decoración con elementos interactivos.
- Tono: festivo, deportivo y amistoso; interfaz legible para edades diversas, sin saturación visual.
- Geometría: low-poly con proporciones coherentes, bordes y superficies intencionales; personajes humanos claramente diferenciados de gorilas NPC.
- Jerarquía: fondos con contraste controlado, marcador/turno dominantes, elementos decorativos subordinados a objetivos y controles.
- Movimiento: microanimaciones breves para feedback, transiciones consistentes y posibilidad de reducir movimiento.
- Identidad: crear recursos propios o de licencia válida; registrar origen y atribución. Ningún recurso externo descartado forma parte del producto.

### Paleta y tipografía

No existe todavía una paleta oficial ratificada. Propuesta para evaluación visual: verdes selva y turquesas como bases; arena y madera como neutros; naranja/coral para llamadas a la acción; rojo sólo para fallos/advertencias. Validar contraste WCAG para textos y estados; no indicar éxito/error sólo por color.
Definir mediante diseño revisado los tokens semánticos surface, surface-raised, text-primary, text-secondary, action-primary, action-danger, success, warning, focus, player-1..4, espaciado, radios, bordes, elevación, tipografía y escalas. No fijar hexadecimales ni una familia de fuente como hecho aprobado sin mockups.

## 3. Pantallas y navegación de Unity

1. Arranque y preflight: inicio de servicios técnicos, disponibilidad LAN, estados de permisos, fallos recuperables y opción de reintento, sin bloquear innecesariamente el menú por servicios opcionales.
2. Menú principal: Jugar, Pruebas (sólo ambiente/flag QA), Ajustes y Salir. No habilitar Jugar mientras los requisitos de gameplay no estén integrados. No exponer tooling de QA al usuario final.
3. Sala/lobby: código QR verificable, estado real de conectividad por jugador, slots disponibles, nombres, preparado/no preparado, reconexión y transición a juego.
4. Selección deportiva: Coconut Bowling, Gorilla Smash, Coconut Throw y Jungle Slice, con estado de disponibilidad honesto.
5. Instrucciones/calibración: control por disciplina, permisos necesarios, práctica y alternativa accesible autorizada.
6. HUD de partida: turno actual, progreso, marcador, eventos de reglas, desconexión/pausa y feedback legible a distancia.
7. Resultados: clasificación originada en Unity, resumen y estado de persistencia separado (pendiente/guardado/error), revancha/siguiente deporte.
8. Ajustes: audio, accesibilidad, escala de UI, sensibilidad cuando aplique, ayuda y estado de red.

Navegación sin ratón: las funciones de juego deben ser navegables desde celular cuando ya exista un controlador admitido. Definir y probar navegación alternativa para primer inicio/sin celulares y QA, sin prometerlo como implementado.

## 4. Flujo PWA móvil

Máquina de estados propuesta: BOOT → DISCOVERY/JOIN → PERMISSIONS → PLAYER/READY → LOBBY → CONTROLLER → RESULTS; ramas explícitas RECONNECTING, ERROR, OFFLINE_SHELL y DISCONNECTED. Cada estado indica las acciones permitidas, salida y recuperación. El acceso por QR y origen seguro debe seguir los contratos de red existentes.
Pantallas obligatorias previstas:

- Conectar: identidad del producto, lectura/uso de QR y conexión segura; nunca mostrar un «conectado» basado únicamente en navigator.onLine.
- Permisos: solicitud por gesto de usuario, sensores disponibles/no disponibles, consentimiento, instrucciones y fallback aprobado.
- Jugador: nombre/slot/identidad durable sólo al estar integrados los contratos; asociación explícita entre identidad técnica y jugador real.
- Lobby: estado de todos los participantes, preparado, reconexión, mensajes accionables.
- Control por deporte: áreas táctiles y sensores propios de cada juego, nunca un D-pad genérico como sustituto de mecánicas aprobadas.
- Resultados: consulta REST de datos confirmados y estado de guardado; no permitir que PWA establezca puntuaciones oficiales.
- Errores y modo offline: diferenciar Internet WAN caído, LAN no disponible, Java inaccesible y backend de persistencia inaccesible.

## 5. Controles por minijuego

| Deporte | Interfaz móvil prevista | Feedback/validaciones |
| --- | --- | --- |
| Coconut Bowling | Dirección, potencia, efecto y confirmación de lanzamiento | Turno, bloqueo de doble disparo, valor y disponibilidad real de sensores |
| Gorilla Smash / High Striker | Preparación/calibración, gesto o alternativa aprobada y estado de intento | Ventana válida de golpe, tres intentos como requisito a confirmar en reglas, altura/campana calculadas por Unity |
| Coconut Throw / Shot Put | Ángulo, potencia y lanzar, con visualización de los tres intentos | Sector válido, faltas y distancia en metros informados por Unity |
| Jungle Slice | Superficie táctil amplia para trazar cortes | Coordenadas normalizadas, gesto continuo, orientación, combos y penalizaciones resueltas por Unity |

Nunca derivar «fuerza real» en newtons de aceleración sin calibración y modelo validados. Definir fallback cuando el sensor no exista. Todo mensaje input debe mantener versión, identidad de slot, secuencia, antigüedad permitida y rechazo de duplicados según protocolos acordados.

## 6. Componentes reutilizables y tokens de interacción

Unity: FocusIndicator, PrimaryAction, StatusBanner, PlayerSlot, ConnectionIndicator, TurnIndicator, Scoreboard, InstructionPanel, ConfirmationDialog, ServiceHealthPanel (QA), Toast/Feedback; son componentes objetivo, no clases existentes.
PWA: AppShell, ConnectionStatus, PlayerBadge, PermissionCard, ReadyControl, ControllerLayout, TouchSurface, PowerMeter, AimControl, MotionCalibration, PendingSyncBadge, ErrorRecovery, ResultsList. Compartir tokens conceptuales entre Unity y CSS, sin forzar compartir código.
Estados para cada componente: normal, hover/foco (donde aplique), presionado, deshabilitado, esperando respuesta, error, éxito y desconectado. Impedir eventos duplicados durante transiciones críticas.

## 7. Accesibilidad, rendimiento y adaptación

- Objetivo de diseño táctil PWA: áreas activas de al menos 44×44 CSS px y separación adecuada para pulgares; no equiparar CSS px con puntos nativos ni con unidades de Canvas Unity. WCAG 2.2 AA 2.5.8 usa 24×24 CSS px o sus excepciones; 44×44 es una meta más amplia del proyecto, no una certificación de AA. [Referencia W3C](https://www.w3.org/WAI/WCAG22/Understanding/target-size-minimum).
- Textos y botones legibles en TV/proyector a distancia, sin depender de detalles pequeños.
- Criterios propuestos para PWA: contraste de texto normal ≥4.5:1 y texto grande ≥3:1 según WCAG 1.4.3; componentes/indicadores relevantes ≥3:1 según WCAG 1.4.11. Validar sobre cada fondo y estado, sin afirmar ratios hasta medir tokens aprobados. Etiquetas, nombre/rol/estado y foco visible; soporte teclado y lectores de pantalla en menús/formularios. [Contraste W3C](https://www.w3.org/TR/WCAG22/#contrast-minimum).
- Texto ampliado al 200% sin pérdida de acciones esenciales; respetar zoom, safe areas, teclado virtual y cambios de orientación. Unity requiere escala y lectura medidas en TV/proyector, no trasladar tamaños CSS directamente.
- Confirmar acciones de menú al soltar/cancelar cuando corresponda; gestionar pointercancel y pérdida de foco sin dejar controles atascados. Para gestos deportivos esenciales, evaluar la alternativa accesible con reglas/aprobación propia; no sustituirla por un D-pad genérico.
- Señalización redundante con forma/texto/icono, no exclusivamente color, sonido o vibración.
- Vibración opcional en Android compatible; en iOS no asumir APIs hápticas disponibles. Feedback visual y sonoro alternativo con permisos del navegador.
- Animaciones de duración acotada, preferencia de movimiento reducido, presupuesto de rendimiento medido en móviles físicos.
- Evitar bloquear input por llamadas REST; no almacenar frames, giroscopio a alta frecuencia o trazas de dedos en SQLite.

## 8. Offline-first y semántica del estado

La PWA puede cargar recursos cacheados sin WAN, pero offline no implica que el backend local o el WebRTC estén accesibles. Dar tres estados independientes: control conectado al juego, API disponible y persistencia de transacciones.
Cachear sólo recursos aprobados del shell; datos de negocio sujetos a expiración. **Perfil actual verificado:** el laboratorio guarda snapshot y borrador, requiere envío manual al recuperar conectividad y protege un POST incierto incluso tras recargar; no implementa cola transaccional ni reintentos automáticos.

**Perfil futuro propuesto, condicionado a aprobación:** para POST permitidos, diseñar cola durable en IndexedDB con transactionId estable, estado pendiente/enviado/confirmado/error, reintento limitado e idempotencia real en backend. No activar este perfil con el contrato actual de POST /api/players ni añadir Idempotency-Key como si ya estuviera soportado. Los resultados oficiales deben publicarse desde Unity o adaptador autorizado, no desde una pantalla del teléfono.

## 9. Responsabilidades por repositorio

- Unity (Josue1855/Gorilla-Escape): UI de TV, navegación de lobby, HUD, resultados oficiales, feedback de reglas y contrato de entrada.
- PWA UI/UX (Quintana-117/Dise-o_pwa): pantallas móviles, controles, estilos responsivos, accesibilidad, Service Worker, datos REST y cola offline cuando esté aprobada.
- Backend (Josue1855/Gorilla-Escape-Backend): API y almacenamiento de identidades/resultados, validación, idempotencia, métricas y contratos. SQLite/JDBC ya está aprobado para el primer incremento de jugadores e implementado en PR #14; sesiones, resultados, rankings e idempotencia de negocio siguen pendientes. PR #12 conserva la evaluación amplia como documento histórico, cerrado sin merge; PR #14 ya está integrado en develop.

No incorporar código ni activos de Google Playground. No reemplazar el canal Java/WebRTC/IPC por llamadas REST.

## 10. Definición de diseño y aprobación

Etapa A — Inventario (AS-IS): capturas reales de cada pantalla Unity/PWA y matriz de estados, componentes, eventos y flujos; diferenciar prototipos de producción.

Etapa B — UX: mapas de navegación, wireframes por dispositivo, comportamiento sin red, permisos y accesibilidad; revisión por equipo.

Etapa C — UI: moodboard original, paleta y tipografía propuestas, tokens, componentes y prototipos visuales de Unity y PWA; aprobación explícita antes de marcar definitivo.

Etapa D — Implementación: issues por repositorio con criterios de aceptación, ramas desde develop, PRs revisados y pruebas. No trabajar directo en main ni develop.

Etapa E — Evidencia: QA con al menos Android e iPhone físicos, pantalla TV/proyector, navegación sin ratón donde corresponda, pérdida/reconexión, orientación, rendimiento y capturas con commit/SHA.

### Criterios de aceptación transversales

- Recorrido de conexión → lobby → deporte → resultado consistente, sin pantallas huérfanas.
- Cada disciplina dispone de interfaz móvil específica y comunica input real hacia el motor, no sólo cambia estado local.
- Unity conserva autoridad de turnos, físicas y resultados.
- Estado LAN/API/offline es fiel a comprobaciones reales.
- Escala, contraste, enfoque y táctil probados en hardware objetivo.
- Componentes visuales mantienen mismo lenguaje de diseño en TV y celular.
- Manifest/SW/assets PWA presentes en build de producción y comprobados desde DevTools.
- Evidencia de diseño y QA almacenada en rutas docs/ con enlaces desde el índice.

## 11. Backlog inicial recomendado (propuesta, no issues creados)

| ID | Repositorio | Trabajo |
| --- | --- | --- |
| UX-01 | Principal | Auditar pantallas Unity y PWA existentes e identificar divergencias |
| UX-02 | Compartido/documentación principal | Aprobar identidad visual, paleta, tipografía, tokens y prototipos |
| UX-03 | Unity | Menú, lobby y navegación PC/TV sin ratón |
| UX-04 | PWA | Conexión QR, permisos, estados de red y reconexión |
| UX-05 | PWA | Control específico para Coconut Bowling |
| UX-06 | PWA | Control específico para Gorilla Smash |
| UX-07 | PWA | Control específico para Coconut Throw |
| UX-08 | PWA | Control específico para Jungle Slice |
| UX-09 | Unity | HUD, resultados y estado de persistencia |
| UX-10 | PWA | Componentes, accesibilidad y feedback multisensorial |
| UX-11 | PWA | PWA instalable, offline shell y estado de sincronización |
| UX-12 | Integración | QA visual y funcional con dispositivos físicos y evidencia |

Asignar epics, responsables y fechas sólo tras consulta de los GitHub Projects reales. Reutilizar issues existentes y crear enlaces cruzados sin duplicar trabajo.

## 12. Relación con documentación existente

Este documento complementa [PWA UI/UX](PWA_UI_UX.md), [Unity](UNITY.md), [entradas y personajes](../02-gameplay/INPUT_AND_CHARACTERS.md), [plan de integración](../03-development/INTEGRATION_PLAN.md) y [backlog](../05-management/EPICS_AND_BACKLOG.md). Si hay conflicto entre un diseño propuesto y un contrato técnico implementado, prevalece el contrato comprobado hasta que se apruebe y pruebe su modificación.

El inventario UX-01..UX-12 es una propuesta local, no issues creados, asignaciones ni fechas comprometidas. [Trazabilidad con trabajo existente y aprobación](../04-testing/UI_UX_DESIGN_REVIEW.md#trazabilidad-y-siguiente-paso). El perfil offline futuro y los tokens requieren aprobación del equipo; publicar este documento no la concede.
