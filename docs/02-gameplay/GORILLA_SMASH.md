# Gorilla Smash / High Striker

Última revisión: 2026-10-16. Fuentes: S1, S2 y S3; identificadores definidos en [inventario de fuentes](../00-product/SOURCE_INVENTORY.md). Las etiquetas describen el alcance indicado, no una certificación global.

Requisito PLANIFICADO: Máquina de feria de golpe con mazo. Implementación Unity: no encontrada en S1; PlayEnabled=false. No hay evidencia de gameplay, física específica ni tests de esta disciplina.

## Flujo y estados

Preparar humano y mazo → calibrar/controlar → ejecutar golpe → impactar plataforma → elevar indicador → medir altura/campana → feedback → siguiente intento → resultado.

Estados candidatos PROPUESTO: preparación, calibración, listo, golpe, impacto, ascenso, resolución, siguiente intento, final, pausa/abortado.

## Entrada, UI y eventos

Intención de golpe desde gesto o control accesible; la magnitud física y el instante válido se calculan/validan en Unity.

Indicador listo/calibrado, potencia normalizada, disparador de intento y contador. PC muestra mazo, plataforma, torre y campana; la pantalla táctil no adjudica puntos.

Eventos de salida propuestos: golpe aceptado, impacto válido, altura máxima, campana alcanzada, intento resuelto, resultado final. Incorporarán partida, participante, secuencia de acción y rulesVersion tras acordar el contrato; no son mensajes existentes de Shared.

## Reglas y puntuación

Tres intentos propuestos, pendientes de confirmación del PO; S3 actual no fija su cantidad. Puntuación basada en fuerza/altura; curva, máximo, bonus de campana y agregación PROPUESTO hasta aprobación. Aceleración del teléfono no es fuerza en newtons sin modelo/calibración.

## Aceptación y pruebas requeridas

Jugador humano golpea la plataforma; indicador sube y campana sólo suena al alcanzar umbral autorizado; no hay combate ni daño a rivales; ausencia de sensor no crea potencia ficticia.

Reglas de altura/puntos en bordes, impacto fuera de ventana, golpes repetidos, saturación, sensor parcial/denegado y revisión física segura; medir incertidumbre de calibración.

Dependencias: [framework](MINIGAME_FRAMEWORK.md), [input y personajes](INPUT_AND_CHARACTERS.md), autoridad Unity, UI/UX por disciplina y [matriz de pruebas](../04-testing/TEST_CASE_MATRIX.md). Las reglas incompletas deben acordarse antes de cerrar la issue; este documento no autoriza una implementación durante la auditoría.
