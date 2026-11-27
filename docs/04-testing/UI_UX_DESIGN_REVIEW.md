# Revisión de diseño: propuesta UI/UX Gorilla Escape

Estado: REVISIÓN DOCUMENTAL; no aprobación visual ni certificación de accesibilidad. Alcance: especificación de Unity PC/TV y PWA React. Fuente: propuesta del PO del 2026-10-16 y estado técnico S9/S10. [Propuesta consolidada](../01-architecture/UI_UX_DESIGN_SYSTEM.md).

## Evaluación

La dirección es coherente con un juego social tropical: humanos participantes, gorilas NPC, Unity autoritativa y controles por disciplina. Necesita concreción de estados, alternativas accesibles y pruebas antes de implementar. No se proporcionaron mockups/tokens aprobados para medir contraste o legibilidad; esa parte queda pendiente, no se califica como PASS ni como infracción observada.

La revisión usa apple-design (principios HIG adaptados a React/Unity), referencias de accesibilidad, color, layout, tipografía y onboarding, junto con criterios W3C para la parte web. La identidad visual sigue siendo propuesta. No se generaron pantallas, assets, clases ni contratos nuevos.

## Puntos de prioridad alta antes de implementar

| Qué falta concretar | Por qué | Cambio en la especificación / aceptación |
|---|---|---|
| Perfil offline actual frente a futuro | Confundir borrador con cola automática puede duplicar POST y dar falsa confirmación | Se separan snapshot/borrador manual existente y cola idempotente futura. Estado incierto persiste; no añadir retries con la API actual. |
| Estado de SQLite | La propuesta original lo describía como opción por confirmar | Se registra aprobación expresa e incremento real de jugadores en PR #14; publicación de resultados/idempotencia siguen pendientes. |
| Disponibilidad independiente | navigator.onLine no prueba RTC, Java ni REST | Diseñar control conectado, API accesible y persistencia confirmada como señales distintas. Para cada una: comprobación, fecha/antigüedad, fallo y recuperación. |
| Navegación sin teléfonos/primer inicio | Exigir un controlador admitido para poder admitir el primer controlador crearía un bloqueo | Prototipar ruta alternativa local para PC/QA y demostrar foco/confirmación/volver. Navegación móvil remota requiere contrato aprobado; no declarar implementada. |
| Accesibilidad verificable | Tamaños/contraste genéricos no permiten decidir aceptación | Añadir métricas propuestas para PWA, escala/zoom/foco y medidas físicas para TV. Valores de tokens y resultados de medición pendientes de prototipo. |

Estas brechas son de especificación; no son una auditoría del runtime entero ni defectos visuales medidos en pantallas inexistentes.

## Mejoras de interacción y sistema visual

- **Color semántico:** mantener variantes por superficie/estado, texto/icono además del color para slots1–4 y no reutilizar el color de acción para decoración indistinguible. Medir pares de contraste una vez exista paleta. Principio: [HIG Color, buenas prácticas](https://developer.apple.com/design/human-interface-guidelines/color).
- **Jerarquía y texto:** turno, objetivo y acción primaria deben conservar prioridad al aumentar texto; evitar truncar nombre/estado y no confundir rótulos decorativos con controles. Registrar fuente/tamaños/pesos por función, pendientes de prototipo. Principios: [HIG Typography](https://developer.apple.com/design/human-interface-guidelines/typography) y [Layout](https://developer.apple.com/design/human-interface-guidelines/layout).
- **Permisos contextuales:** explicar y solicitar sensores por gesto al entrar en una disciplina que los necesite, conservar salida/reintento/alternativa si se deniegan. Servicios opcionales no bloquean el menú completo. Principio: [HIG Onboarding](https://developer.apple.com/design/human-interface-guidelines/onboarding).
- **Foco, movimiento y feedback:** especificar orden/retorno de foco al cerrar paneles, mensajes que el lector de pantalla anuncie sin repetir cada muestra de sensores y feedback persistente para errores importantes. Respetar movimiento reducido y no depender de vibración/sonido. Principio: [HIG Accessibility](https://developer.apple.com/design/human-interface-guidelines/accessibility).
- **Estados y solicitudes:** la máquina propuesta debe definir eventos de entrada/salida, acciones habilitadas, dueño del estado y recuperación. Rechazo de permiso, sensor ausente, API caída, RTC caído y envío incierto necesitan transiciones propias; no un ERROR único que reinicie todo.
- **Cancelación y gestos:** los menús deben poder cancelar una pulsación; una superficie deportiva debe gestionar pointercancel, orientación y pérdida de foco sin inventar un golpe/corte. Definir alternativa aprobada para controles donde corresponda; evaluar esencialidad del gesto con las reglas del juego. Referencias: [W3C2.5.1](https://www.w3.org/WAI/WCAG22/Understanding/pointer-gestures.html) y [W3C2.5.7](https://www.w3.org/WAI/WCAG22/Understanding/dragging-movements).

No se requiere adoptar navegación, fuentes o apariencia nativa de Apple para un juego Unity/React; se aplican los principios de legibilidad, adaptación y control por el usuario. La preferencia de 44×44 CSS px no es equivalente a 44pt nativos ni a unidades de Canvas. No se exige un modo claro/oscuro ya implementado; variantes, alto contraste y su alcance deben decidirse en la etapa visual.

## Qué conservar de la propuesta

Autoridad única de Unity, separación REST/control, estados de guardado explícitos, controles por deporte, permisos por gesto y distinción humanos/NPC. Low-poly orgánico y recursos propios/licenciados son dirección artística propuesta; ni colores actuales del laboratorio ni primitivas de prototipos se convierten por ello en arte final.

## Criterios de accesibilidad propuestos

| Área | Verificación por realizar |
|---|---|
| Texto PWA | Contraste normal 4.5:1; grande 3:1 (≥24 CSS px regular o ≈18.67 CSS px negrita); zoom 200% sin perder acciones esenciales; [WCAG1.4.3](https://www.w3.org/TR/WCAG22/#contrast-minimum), [1.4.4](https://www.w3.org/TR/WCAG22/#resize-text). |
| Indicadores y foco | Contraste no textual 3:1 cuando aplique, foco visible/no oculto y operación de menús/formularios por teclado; [WCAG1.4.11](https://www.w3.org/TR/WCAG22/#non-text-contrast). |
| Táctil | Meta del producto 44×44 CSS px, separación y uso con pulgar. Distinguir del criterio AA 24×24 y sus excepciones: [WCAG2.5.8](https://www.w3.org/WAI/WCAG22/Understanding/target-size-minimum). |
| TV/Unity | Registrar resolución, distancia observada, tamaño de pantalla y escala; leer marcador/turno e identificar foco sin ratón. No asignar umbral físico universal sin pruebas. |
| Movimiento y sensores | Repetir recorrido con movimiento reducido/sensor denegado; feedback visual independiente de háptica. Alternativa deportiva requiere reglas validadas. |
| Dispositivos | Probar Android e iPhone reales, teclado virtual, safe areas, orientación y red; registrar modelo/SO/navegador/SHA y resultado por caso. |

La matriz define objetivos de aceptación, no conformidad WCAG/HIG ya obtenida. Se aplican los criterios web a la PWA; la interfaz Unity usa verificaciones de accesibilidad de juego/TV apropiadas, sin adjudicarle una certificación web.

## Trazabilidad y siguiente paso

Se consultaron los Projects [Unity #1](https://github.com/users/Josue1855/projects/1) y [Backend #2](https://github.com/users/Josue1855/projects/2), y títulos de issues abiertas; no se modificaron campos, estados, responsables ni fechas. Los enlaces siguientes son trabajo relacionado para revisar, no equivalencias automáticas ni criterios nuevos insertados en sus issues:

| Propuesta UX | Trabajo existente relacionado |
|---|---|
| UX-03/09: menús, HUD, resultados | Unity [#67](https://github.com/Josue1855/Gorilla-Escape/issues/67) y [#70](https://github.com/Josue1855/Gorilla-Escape/issues/70); revisar alcance antes de añadir tareas. |
| UX-05 Bowling | Unity tutorial/feedback [#59](https://github.com/Josue1855/Gorilla-Escape/issues/59) y validación [#60](https://github.com/Josue1855/Gorilla-Escape/issues/60). |
| UX-06 Smash | Unity tutorial/escena/feedback [#38](https://github.com/Josue1855/Gorilla-Escape/issues/38); intentos/curva siguen pendientes de aprobación. |
| UX-07 Throw | Unity tutorial/feedback [#51](https://github.com/Josue1855/Gorilla-Escape/issues/51) y validación [#52](https://github.com/Josue1855/Gorilla-Escape/issues/52). |
| UX-08 Slice | Unity tutorial/feedback [#63](https://github.com/Josue1855/Gorilla-Escape/issues/63) y validación [#64](https://github.com/Josue1855/Gorilla-Escape/issues/64). |
| UX-12: hardware y evidencia | Unity [#33](https://github.com/Josue1855/Gorilla-Escape/issues/33) y [#34](https://github.com/Josue1855/Gorilla-Escape/issues/34); no cerrar QA física con capturas de escritorio. |
| Persistencia del laboratorio | Backend [#13](https://github.com/Josue1855/Gorilla-Escape-Backend/issues/13) y laboratorio [#10](https://github.com/Josue1855/Gorilla-Escape-Backend/issues/10); no son la outbox idempotente futura. |
| Incorporación PWA upstream | [Solicitud de develop/revisión #1](https://github.com/Quintana-117/Dise-o_pwa/issues/1); seguir trabajando en el fork hasta disponer de base de integración autorizada. |

Próxima etapa: inventario real de pantallas y estados (UX-01), wireframes del recorrido y recuperación, prototipos de tokens/componentes y aprobación explícita del equipo/PO antes de declararlos definitivos. Crear sólo los issues que no estén cubiertos una vez se revise la correspondencia. No se crearon UX-01..12 como issues remotos ni se implementó arte/UI en esta actualización.
