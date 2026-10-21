# Alcance y precedencia

Última revisión: 2026-10-16. Fuentes: S1, S2 y S3; identificadores definidos en [inventario de fuentes](../00-product/SOURCE_INVENTORY.md). Las etiquetas describen el alcance indicado, no una certificación global.

Alcance PLANIFICADO por el encargo PO S3: Coconut Bowling, Gorilla Smash/High Striker, Coconut Throw/Shot Put y Jungle Slice; UI móvil por disciplina; integración de resultados y laboratorio de persistencia. Cada especificación está en [gameplay](../02-gameplay/MINIGAME_FRAMEWORK.md). Archery y otras pruebas históricas quedan fuera de estos cuatro minijuegos.

Las reglas High Striker y Shot Put del encargo actual sustituyen interpretaciones de combate/tiro al blanco. No se altera el documento histórico v4 para simular una aprobación antigua. El runtime actual prevalece al describir lo implementado; el encargo prevalece al definir este alcance futuro. Persistencia, autoridad del adaptador y fórmulas aún no acordadas se señalan como PROPUESTO.

Actualización 2026-10-20: el PO autorizó el vertical slice [Desafío del Tronco](../02-gameplay/LOG_BALANCE.md) como quinta prueba, con humano náufrago controlable y gorilas NPC. Se desarrolla exclusivamente en `feature/gameplay-desafio-del-tronco`, sin merge a develop/main en esta iteración.

Exclusiones del encargo documental original del 2026-10-16: código funcional, paquetes, escenas, tests, CI, cambios del firewall del equipo, certificados, despliegues y merges. Estas exclusiones describen aquel encargo; no bloquean el prototipo autorizado posteriormente. Los tres proyectos oficiales usan trabajo propio del equipo; no se consultan ni migran prototipos descartados. Flutter figura en autorizaciones previas, pero no se encontró implementación ni constituye un cuarto proyecto dentro de este encargo.

Los gate de aceptación física existentes siguen vigentes. La fecha de revisión no es una fecha de entrega ni un Sprint asignado.
