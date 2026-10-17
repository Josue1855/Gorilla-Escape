# Coconut Bowling

Última revisión: 2026-10-16. Fuentes: S1, S2 y S3; identificadores definidos en [inventario de fuentes](../00-product/SOURCE_INVENTORY.md). Las etiquetas describen el alcance indicado, no una certificación global.

Requisito PLANIFICADO: Boliche de diez frames con cocos. Implementación Unity: no encontrada en S1; PlayEnabled=false. No hay evidencia de gameplay, física específica ni tests de esta disciplina.

## Flujo y estados

Apuntar → ajustar potencia y efecto → lanzar → simular trayectoria/colisiones → contar pinos → resolver bonus → siguiente lanzamiento/frame → resultado final.

Estados candidatos PROPUESTO: preparación, apuntado, lanzamiento, resolución, espera de bonus, fin de frame, fin de partida, pausa/abortado.

## Entrada, UI y eventos

Intención de apuntado, potencia, efecto y confirmar lanzamiento; gestos/calibración por decidir.

Retícula/apuntado, potencia y efecto con valores visibles, confirmar y estado de turno. Bloquear doble lanzamiento; marcador por frame en PC y resumen móvil.

Eventos de salida propuestos: lanzamiento aceptado/rechazado, pinos resueltos, frame actualizado, puntuación provisional/definitiva, partida finalizada. Incorporarán partida, participante, secuencia de acción y rulesVersion tras acordar el contrato; no son mensajes existentes de Shared.

## Reglas y puntuación

Requisito: puntuación oficial de boliche, strikes, spares y décimo frame. Antes de codificar, registrar fuente reglamentaria aprobada y fixtures: partida perfecta 300, todos ceros 0, bonus pendientes, spare/strike final, límites de pinos y bolas extra. No inventar fórmula simplificada.

## Aceptación y pruebas requeridas

Diez frames exactos; no cerrar bonus antes de las bolas necesarias; colisiones no puntúan dos veces el mismo pino; UI coincide con Unity; falta/desconexión no duplican lanzamiento.

Scoring puro de todos los casos límite, integración física/pinos, doble toque, cambio de turno y pérdida de enlace durante la bola. Sector/falta y política de empate requieren aprobación.

Dependencias: [framework](MINIGAME_FRAMEWORK.md), [input y personajes](INPUT_AND_CHARACTERS.md), autoridad Unity, UI/UX por disciplina y [matriz de pruebas](../04-testing/TEST_CASE_MATRIX.md). Las reglas incompletas deben acordarse antes de cerrar la issue; este documento no autoriza una implementación durante la auditoría.
