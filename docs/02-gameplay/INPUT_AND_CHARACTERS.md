# Entradas, humanos y gorilas

Última revisión: 2026-10-16. Fuentes: S1, S2 y S3; identificadores definidos en [inventario de fuentes](../00-product/SOURCE_INVENTORY.md). Las etiquetas describen el alcance indicado, no una certificación global.

S3 define participantes humanos; gorilas como NPC/rival/entrenador/mascota/espectador según validación de arte. Seleccionar un gorila de la demo S4 no crea un humano persistente ni cambia la autoridad Unity. Inventario de arte S1 tiene carpetas preparadas, pero no se identificó un conjunto artístico final para las cuatro disciplinas.

S1 PhoneValues transporta disponibilidad y valores raw: acceleration, accelerationIncludingGravity, rotationRate, orientation Euler, screenOrientation y touch x/y/pressed. Los flags hasX/hasPressed distinguen cero de ausencia. PhoneInput no calcula Power/Spin ni convierte orientación en quaternion oficial. Calibración, dirección anatómica, filtros y gestos deportivos son trabajo pendiente; un botón o vibración local de S4 no llegan a Unity.

Para Bowling hace falta intención de apuntado/potencia/efecto; Smash golpe y ventana válida; Throw potencia/ángulo/lanzamiento; Slice trayectorias de corte. Estas correspondencias son PLANIFICADO; el diseño del detector y las unidades normalizadas son PROPUESTO. La cámara complementaria no sustituye controles sin una decisión por disciplina.

Proponer contratos de intención con freshness/epoch/secuencia sin enviar histories ilimitadas. Validar permisos por gesto, sensor no disponible, orientación de pantalla, pausa y desconexión. T060/#104 mantiene el control alternativo de usuario sin implementar; no confundir herramientas de QA con fallback de torneo accesible.

Arte/UX deberá aprobar siluetas humanas, tamaños/contraste, controles por dedo y etiquetas coherentes. Los recursos nuevos procederán exclusivamente del equipo y se registrarán con autoría/licencia y revisiones.
