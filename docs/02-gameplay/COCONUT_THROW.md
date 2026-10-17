# Coconut Throw / Shot Put

Última revisión: 2026-10-16. Fuentes: S1, S2 y S3; identificadores definidos en [inventario de fuentes](../00-product/SOURCE_INVENTORY.md). Las etiquetas describen el alcance indicado, no una certificación global.

Requisito PLANIFICADO: Lanzamiento de coco tipo lanzamiento de bala. Implementación Unity: no encontrada en S1; PlayEnabled=false. No hay evidencia de gameplay, física específica ni tests de esta disciplina.

## Flujo y estados

Preparar sector → ajustar potencia/ángulo → lanzar → resolver vuelo/aterrizaje → medir distancia y falta → siguiente intento → mejor distancia válida.

Estados candidatos PROPUESTO: preparación, apuntado, lanzamiento, vuelo, aterrizaje, validación, siguiente intento, final, pausa/abortado.

## Entrada, UI y eventos

Potencia, ángulo y confirmar; adaptación de gesto/cámara propuesta. No tratarlo como disparar a una diana.

Ángulo/potencia, línea/sector, contador de tres intentos, falta y distancia válida. PC muestra trayectoria y punto de caída.

Eventos de salida propuestos: lanzamiento aceptado, aterrizaje, falta con código, distancia medida, mejor marca, resultado final. Incorporarán partida, participante, secuencia de acción y rulesVersion tras acordar el contrato; no son mensajes existentes de Shared.

## Reglas y puntuación

Tres intentos y mejor distancia válida en metros. Unity decide sector y falta. Todos inválidos deben mostrar sin marca, no victoria por cero ficticio. Precisión, redondeo, desempate y conversión unidades Unity↔metros por aprobar.

## Aceptación y pruebas requeridas

Exactamente tres intentos resueltos; potencia/ángulo afectan trayectoria; sólo sector válido genera marca; UI y BD conservan unidad metros y validez; no existe tiro al blanco.

Límites de sector, falta en línea, vuelo fuera del escenario, distancias conocidas, todos inválidos, empate y desconexión; no asumir ángulo óptimo fijo sin modelo físico.

Dependencias: [framework](MINIGAME_FRAMEWORK.md), [input y personajes](INPUT_AND_CHARACTERS.md), autoridad Unity, UI/UX por disciplina y [matriz de pruebas](../04-testing/TEST_CASE_MATRIX.md). Las reglas incompletas deben acordarse antes de cerrar la issue; este documento no autoriza una implementación durante la auditoría.
