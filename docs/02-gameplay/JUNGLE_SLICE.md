# Jungle Slice

Última revisión: 2026-10-16. Fuentes: S1, S2 y S3; identificadores definidos en [inventario de fuentes](../00-product/SOURCE_INVENTORY.md). Las etiquetas describen el alcance indicado, no una certificación global.

Requisito PLANIFICADO: Arcade de corte de frutas con identidad artística propia. Implementación Unity: no encontrada en S1; PlayEnabled=false. No hay evidencia de gameplay, física específica ni tests de esta disciplina.

## Flujo y estados

Explicar control → lanzar frutas/obstáculos → capturar trayectoria del dedo → validar cortes Unity → combo/penalización → terminar ronda → resultado.

Estados candidatos PROPUESTO: preparación, listo, ronda activa, resolución de corte, combo, penalización, fin, pausa/abortado.

## Entrada, UI y eventos

Trayectoria táctil, inicio/fin de gesto y coordenadas normalizadas. El wire actual contiene x/y/pressed, pero no implementa reconocimiento ni contrato deportivo de trayectorias/combo.

Área de gesto sin controles superpuestos, feedback de trayectoria/objetivo, combo/puntos y error. Acordar mapeo entre pantalla móvil y playfield Unity antes de programar.

Eventos de salida propuestos: gesto aceptado/rechazado, fruta cortada/perdida, obstáculo tocado, combo actualizado, puntuación final. Incorporarán partida, participante, secuencia de acción y rulesVersion tras acordar el contrato; no son mensajes existentes de Shared.

## Reglas y puntuación

Puntos por corte, ventana de combo, penalización por fallos/obstáculos y duración/vidas PROPUESTO hasta aprobar valores. Un fruto se resuelve una sola vez; no puntuar una repetición de mensaje.

## Aceptación y pruebas requeridas

Frutas al vuelo y corte gestual verificable; no funciona como botón de ataque; identidad visual propia; no se otorga combo por paquetes duplicados ni por gestos de otro slot.

Intersección segmento/fruta, coordenadas y rotación de pantalla, baja frecuencia táctil, discontinuidades, multi-touch (política pendiente), duplicados, obstáculos y desconexión.

Dependencias: [framework](MINIGAME_FRAMEWORK.md), [input y personajes](INPUT_AND_CHARACTERS.md), autoridad Unity, UI/UX por disciplina y [matriz de pruebas](../04-testing/TEST_CASE_MATRIX.md). Las reglas incompletas deben acordarse antes de cerrar la issue; este documento no autoriza una implementación durante la auditoría.
