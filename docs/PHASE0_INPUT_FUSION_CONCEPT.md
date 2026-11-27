# Contrato conceptual común, cámara PC e Input Fusion

**2026-10-06 — diseño, no implementación.** Requisitos de producto aprobados; schema/wire format y algoritmos siguientes propuestos para prueba. [Decisión de transporte](PHASE0_SECURE_MOTION_TRANSPORT_DECISION.md), [plan de Spike](PHASE0_SECURE_MOTION_SPIKE_PLAN.md). No se implementan sensores, Flutter, webcam, fusión, calibración final ni física deportiva.

## 1. Ownership y fuentes

PWA y Flutter son controles completos de movimiento/touch, condicionados a hardware/permisos reales. Java asigna/admite identidades de sesión técnica, valida/adapta mensajes y lifecycle; no decide estado competitivo. Unity posee gameplay, física, animaciones, scoring, resultados, alineación temporal, calibración aplicada, fusión e interpretación final. No enviar «bola curva», «puntos» ni «ganador» como input confiable.

Sólo webcam integrada/externa **PC** para detección corporal local. Nunca vídeo/cámara móvil para postura, gameplay o fusión. QR del sistema operativo y futuro scanner Flutter son incorporación, no fuentes de gameplay. No pedir cámara móvil para mejorar candidatos ICE. IPC Unity/Java sólo 127.0.0.1.

## 2. Contrato conceptual compartido

El contrato futuro se documentará en Shared/Protocol con fixtures comunes Java/JavaScript/Dart/C#. Codecs distintos, mismas unidades/ausencias/validación/semántica. No ampliar IpcProbeContract hacia parser general ni reciclar PING como mensaje de sensores. Codificación JSON/binaria y DTOs definitivos pendientes de medir y aprobar.

| Grupo | Campos conceptuales | Regla |
|---|---|---|
| Versión/identidad | protocolVersion, sessionId, playerId, connectionEpoch, sequence, clientKind | Java asigna/vincula identidad al canal admitido; IDs declarados no autorizan. Epoch nueva después de recovery. |
| Tiempo | sampleTime, clockDomain, clockEpoch, eventTimeSource, callbackTime; recepción Java/Unity | Monotónico en unidad ms explícita; tiempos de relojes distintos no son comparables directamente. Dominio reinicia al recargar. |
| Capacidades | supported, granted, active; campos disponibles; sensor fuente/API | «Interfaz existe» no significa sensor activo. Feedback sonoro/háptico y permisos separados. |
| Aceleración | linearAccelerationXYZ, accelerationIncludingGravityXYZ | m/s², distinguidos. Null/ausente no es cero ni igualdad entre canales. |
| Gravedad | gravityXYZ, measured/derived, método y calidad | Opcional; no etiquetar accelerationIncludingGravity como vector de gravedad. Derivación sólo con muestras alineadas y método declarado. |
| Velocidad angular | angularVelocityXYZ | rad/s canónicos, fuente declarada; web deg/s requiere conversión. No afirmar raw gyro cuando API ofrece evento sintetizado. |
| Orientación | quaternion XYZW, referenceFrame, absolute/relative, screenRotation | Unitario/tolerancia validada, método versionado. Euler web convertido mediante su convención, no tres rotaciones arbitrarias. |
| Touch | pointers/pad, botones y edges, gestures, touchCancel | Límites de pointers; estados y transiciones sin perder release. Cliente no interpreta resultado deportivo. |
| Calibración | calibrationId, revision, status, neutralReference | Unity valida/aplica revisión. Revisión desconocida bloquea interpretación calibrada. |
| Perfil | user/device/minigame/competitive revision | Gain efectivo aplicado una vez, no sensitivities acumuladas sin declaración. |
| Calidad | missing fields, finite, saturated si conocido, gap, age, sourceConfidence | Confianza desconocida no se inventa como 1. Confianza pose no es precisión física del gyro. |
| Estado/error | foreground/background, connection state, permission denied, sensor unavailable | Cancelar input activo al suspender/perder conexión; no reutilizar muestra vieja como nueva. |

Reglas propuestas: negar NaN/Infinity, tamaños y tipos inválidos; quaternion inválido no normalizar silenciosamente a pose confiable. Límites de valores físicamente plausibles y saturación se fijan por capacidades medibles, no por asumir una precisión universal. Duplicados: no repetir acciones; muestras fuera de orden se descartan para latest-only o entran sólo en ventana de fusión definida. Control fiable ordenado, muestras con frescura; gap observado registrado, no relleno inventado. Epoch antigua y timestamps futuros/no comparables se rechazan o ponen en cuarentena de clock-sync, sin afectar otra plaza.

Un sequence monotónico por flujo/epoch; acordar rango/rollover antes del wire format, sin superar precisión entera segura de JS. Reload/cambio de reloj crea epoch nueva, sin retroceso temporal. Ausencias y calidad viajan explícitas; modalidades accesibles no se presentan como equivalentes totales a movimiento requerido.

### Ejes y transformaciones

Propuesta canónica: frame de dispositivo diestro, x derecha de pantalla en orientación de referencia, y arriba, z hacia el usuario. Mantener screenRotation separada: no reescribir neutral al girar pantalla sin transición/calibración. Mapeo de ejes de plugins Flutter se verificará mediante fixtures físicos, no se asume igual a web.

W3C proporciona m/s², rotationRate alpha/beta/gamma en deg/s y convención de orientación Z-X′-Y″. Alpha giro z, beta x, gamma y; convertir según frame/API, no tomar esos campos como quaternion. Yaw absoluto puede no estar disponible y derivación relativa puede drift. [Especificación W3C](https://www.w3.org/TR/orientation-event/).

Unity usa convenciones distintas. Fijar matriz S entre frames y fixture de seis orientaciones/giros: aceleración (vector polar) a′=Sa; orientación R′=SRS⁻¹. Velocidad angular es vector axial: para cambio con reflexión ω′=det(S)Sω; invertirla igual que aceleración puede invertir el spin. Derivar quaternion de la matriz válida y conservar convención de composición. No trasladar directamente Euler móvil a transform Unity ni invertir ejes por mano dominante sin calibrar grip.

## 3. Relojes, sincronización y calidad temporal

Fuentes: móvil performance/event timestamps, Java nanoTime, Unity monotónico y cámara con timestamp disponible. No comparten epoch ni necesariamente frecuencia. El cliente anuncia origen temporal; Unity mantiene el mapping y decide ventanas de fusión. Java puede transportar mensajes de clock-sync, nunca decidir la acción de juego.

Propuesta de intercambio de cuatro timestamps: t1 PC envía, t2 móvil recibe, t3 móvil devuelve, t4 PC recibe. Offset móvil−PC estimado = ((t2−t1)+(t3−t4))/2; RTT corregido = (t4−t1)−(t3−t2). Convertir móvil→PC restando offset. Sólo con unidades/dominios compatibles y timestamps monotónicos dentro de una epoch. RTT negativo o salto invalida estimación. Separar mapping Unity↔Java si las marcas PC se toman en Java; no asumir que nanoTime y reloj Unity coinciden.

Múltiples intercambios acotados permiten estimar offset/drift y dispersión; registrar incertidumbre de asimetría, colas y timestamp. RTT/2 es aproximación bajo simetría, no latencia one-way demostrada. Seleccionar muestras de sync de menor cola no autoriza excluir outliers del reporte de transporte. Revalidar al reanudar/recargar o cambiar epoch; nunca usar wall clock del teléfono como tiempo de acción.

Para cámara: marcar adquisición cuando API lo permita, recepción de frame e inicio/fin de inferencia. Si sólo existe tiempo de lectura, timestamp de exposición/latencia cámara es desconocido. No alinear pose por fin de inferencia fingiendo adquisición inmediata. Probar movimiento físico observable simultáneo con teléfono/PC para estimar desfase, no atribuir toda diferencia a red.

Unity fusiona muestras en ventanas por minijuego y calidad, con límites de edad y de espera. Paquete tardío no vuelve a ejecutar acción resuelta; duplicado/replay descartado. Botón release/touchCancel debe neutralizar estado incluso si faltan sensores. Ausencia de cámara, gyro, orientación o permiso exige estado degradado explícito y política accesible del minijuego; touch solo no acredita full-motion. No inventar posición corporal o spin cuando falta la fuente.

## 4. Webcam PC y procesamiento offline

Pipeline conceptual: webcam PC → frame timestamp/calibración → pose/manos y confianza → frame cámara/jugador → señales normalizadas → Unity. Resolver active player/turn y vínculo playerId por admisión/calibración; no reconocer identidad facial. Cámara principalmente por turnos; cuatro controles conectados no implican tracking fiable de cuatro cuerpos simultáneos.

[MediaPipe Pose Landmarker](https://developers.google.com/edge/mediapipe/solutions/vision/pose_landmarker) es candidato de pose; documentación de Python/Web/Android no prueba una integración desktop C#/Java lista. Evaluar adaptador nativo C++/plugin Unity y licencia/binarios Windows/Linux antes de elegir. No introducir un servidor Python adicional sólo porque exista ejemplo. [OpenCV](https://github.com/opencv/opencv) es candidato de captura/calibración/operaciones de imagen, no detector de pose completo por sí mismo.

Código [MediaPipe Apache-2.0](https://github.com/google-ai-edge/mediapipe/blob/master/LICENSE) y [OpenCV 4.x Apache-2.0](https://github.com/opencv/opencv/blob/4.x/LICENSE); verificar licencias independientes de modelos/pesos, transitivos y codecs de distribución. Modelo y assets se preparan antes de la partida, hashes/versiones registrados, ninguna descarga/inferencia cloud durante juego. No instalar ni descargar ahora.

Hardware detectado previamente no es webcam física validada para tracking. Faltan FOV/resolución/FPS, luz/oclusiones, distancias, handedness, confidence, carga e integración. Landmarks/world estimates no son ground truth métrico; una muñeca localizada no mide por sí sola torsión anatómica. No almacenar vídeo corporal por defecto; evidencia de pose/timings sanitizada y consentimiento para capturas identificables.

## 5. Boliche: zurdo, lanzamiento y giro a la derecha

El jugador declara mano izquierda y calibra su agarre/neutral. Webcam PC verifica lado/pose del brazo activo y trayectoria; no inferir mano dominante sólo por signo del gyro. Teléfono mide aceleración, orientación y velocidad angular **del dispositivo**, que incluye movimiento de todo el brazo. Estimar torsión de muñeca requiere grip y relación con orientación/pose del brazo; no es medida anatómica directa.

1. **Preparación:** jugador/plaza, mano izquierda, pose neutral, screenRotation, frame de cámara y referencia del teléfono calibrados; permisos/capacidades y calidad suficientes.
2. **Trayectoria/dirección:** desplazamiento del brazo/cuerpo en cámara durante ventana de lanzamiento y orientación calibrada definen dirección inicial estimada. Movimiento lateral cambia dirección; no crea spin por definición.
3. **Potencia/velocidad:** características temporales de trayectoria y aceleración calibrada ayudan a estimar intención de lanzamiento. Integrar acelerómetro libremente deriva; no afirmar velocidad lineal absoluta desde dos muestras ni utilizar gravity como potencia.
4. **Rotación:** componente angular calibrada respecto del eje de suelta y agarre determina intención de spin/eje. En zurdo «girar derecha» debe mapearse físicamente, no por un signo fijo de alpha/gamma.
5. **Liberación virtual:** ventana/gesto o touch de clutch acordado por Unity; el usuario siempre conserva el teléfono en la mano. No implementar ese recognizer ahora.
6. **Resolución:** Unity transforma dirección, velocidad inicial estimada y spin/eje en estado físico de bola dentro de límites del minijuego. Hook depende de eje, velocidad, contacto/fricción y superficie configurada. No fórmula universal giro→curva proporcional.

| Ensayo conceptual futuro | Resultado a verificar, no PASS actual |
|---|---|
| Recto, sin giro intencional | Dirección aproximadamente recta, spin mínimo según neutral/ruido |
| Giro leve/intenso con misma trayectoria | Spin diferente dentro de límites; curva determinada por física, no escala automática |
| Giro derecha/izquierda de zurdo | Mapeo calibrado permite spin con signo apropiado y posible hook derecha/izquierda |
| Movimiento lateral sin giro | Dirección inicial distinta sin añadir efecto por defecto |
| Lanzamiento rápido y giro | Velocidad y spin separados, ambos combinados en física Unity |
| Giro estando quieto, sin lanzamiento | No inventar lanzamiento por gyro alto |
| Diestro versus zurdo | Misma intención equivalente tras calibración, sin ventaja por sensibilidad/latencia |
| Cámara o gyro ausente / muestra tardía | Capacidad insuficiente/degradada explícita; no resultado inventado |

Prueba física posterior debe distinguir dirección inicial, velocidad lineal estimada, velocidad angular, eje/spin y trayectoria final. React/Flutter envían señales, nunca trayectoria/hook precalculados ni scoring. Estas pruebas de interpretación/física no forman parte del Spike de transporte actual.

## 6. Personalización completa, cuatro responsabilidades

| Configuración | Contenido | Autoridad/aplicación |
|---|---|---|
| Usuario | playerId, profileId, mano dominante, sensibilidad accel/gyro/orientación, inversión de ejes, zonas muertas, suavizado, preferencias visuales/auditivas, intensidad háptica, accesibilidad, posición/tamaño de botones, gestos/touch personalizados | Preferencias declaradas; cliente presenta UI/feedback compatible. Unity valida parámetros que afectan acciones. |
| Dispositivo | Capacidades/permisos, fuente/intervalos reales, screen frame, neutral quaternion, bias si medible, grip, límites/saturación conocidos, clock epoch y calidad | Calibración específica, revisionada y aceptada por Unity. Ausente/desconocido no sustituido por valores inventados. |
| Minijuego | Sensibilidad lanzamiento, intensidad de giro, ventanas temporales, interpretación de gestos, asistencia al movimiento, modalidades alternativas y requisitos de fuentes | Unity define semántica y calibración aplicada; no cliente-calculador de física. |
| Competitiva | Límites de gain/deadzone/smoothing/asistencia/spin/potencia y reglas de accesibilidad/equidad | Sólo Unity aplica restricciones; no ajustes cliente que amplíen resultados. |

Perfiles por jugador/dispositivo/minijuego con revisión y regla efectiva competitiva. Un cambio incompatible con la tirada activa se difiere o cancela según política Unity, no se aplica a mitad silenciosamente. No transmitir asistencia como resultado ya corregido ni aplicar sensibilidad dos veces. Feedback audio/haptics local no constituye confirmación oficial de resultado. Paridad funcional significa mismo modelo y semántica; hardware/API pueden ofrecer distinto feedback o cadencia, declarados con claridad.

## 7. Validación posterior y límites actuales

Fixtures de conversión/unidades/ausencias y replay/epoch son automatizables. La prueba de seis ejes/grip/zurdo-diestro, timestamps reales, neutral, cámara/oclusiones y gyro lineal versus rotación requiere hardware físico. Equivalencia JS/Dart/Java/C# se prueba con corpus compartido; no se asegura porque los nombres de campos coincidan.

Estado: conceptos documentados; schema definitivo, sensor físico, pose, clock-sync implementado, fusión, recognizers, personalización UI y física de boliche NOT RUN/NOT IMPLEMENTED. No se crean pipelines ni dependencias en esta tarea. Fase 0 IN PROGRESS; Fase 1 NOT STARTED.


## 8. Calibración inmersiva y justicia competitiva — DEC-008

**Requisito Accepted por el PO; mecanismos y cifras siguientes propuestos, no implementados ni validados.** La calibración individual debe sentirse como juego: desafíos breves, animación, efectos y feedback inmediato, sin asistente técnico ni tutorial largo. La normalización ofrece oportunidades comparables a niños/adolescentes/adultos y jugadores de distinta estatura, fuerza, alcance o movilidad; no garantiza resultados iguales ni equidad ya demostrada.

### Experiencia jugable propuesta

Entrada por una acción natural: «Prueba tu movimiento» en el entorno deportivo, avatar muestra una acción corta y el jugador la replica dentro de su rango cómodo. Un instante de reposo captura neutral; unas pocas repeticiones suaves/medias/cómodamente enérgicas estiman rango y consistencia. Nunca pedir esfuerzo máximo. El feedback celebra control/dirección y confirma «Listo», no evalúa fuerza corporal ni asigna desventaja por tamaño. Si faltan datos, repetir sólo el gesto necesario o ofrecer modalidad compatible, sin bucle interminable.

La práctica no suma puntos oficiales ni consume intentos competitivos. Durante calibración, el feedback de potencia es provisional y claramente de práctica; Unity confirma perfil antes de la ronda. Ofrecer pausa, omitir temporalmente y repetir como «Otro calentamiento»; omitir no acredita calibración válida y puede bloquear sólo la modalidad que la requiere. Una indicación breve mantiene el teléfono sujeto y espacio libre; no esconder información esencial en nombre de inmersión. Alternativa de explicación corta disponible, sin penalizar al jugador por necesitarla.

| Minijuego oficial v4 | Calentamiento inmersivo propuesto (sin consumir intentos oficiales) |
|---|---|
| Gorilla Smash — Mazo | Golpear objetivos de entrenamiento con movimiento descendente cómodo; preparación/retorno, dirección, timing y potencia relativa. Levantar brazo no confirma golpe. |
| Coconut Throw — Lanzamiento de bala | Coco de práctica virtual, preparación/impulso/ángulo/release; mano/grip y rangos cómodos. Nunca soltar físicamente teléfono. |
| Coconut Bowling | Bola de práctica para dirección y giro separados; zurdo/diestro, balanceo, liberación y spin. |
| Jungle Archery | Apuntar a blancos de entrenamiento, neutral yaw/pitch, mantener botón/tensión, estabilidad y release/cancel. |
| Jungle Slice | Frutas de calentamiento con manos vía webcam PC, trayectoria/velocidad/intersección, jugador activo y confianza. No exigir teléfono si no aporta. |

**Corrección vigente (2026-10-07):** boxeo, tenis, golf y penales usados antes fueron ejemplos ilustrativos ajenos al MVP; no son disciplinas oficiales ni alcance de implementación. Se conserva su origen en DEC-008 como registro histórico. Las cinco disciplinas de v4 son las únicas obligatorias; prácticas finales requieren gameplay autorizado. Fase 0 prepara contratos, dependencias y evidencia, no implementa estos desafíos.

### Modelo individual: intención relativa, no fuerza absoluta

Pipeline propuesto, bajo autoridad Unity: calidad/tiempo → frame/grip/neutral → características separadas → rango cómodo individual → intención normalizada → límites competitivos → física del minijuego. Java transporta; PWA/Flutter no generan potencia oficial ni autodeclaran un perfil autoritativo.

Características: rango de aceleración lineal, velocidad angular, amplitud cómoda, orientación/dirección, estabilidad, timing, coordinación y precisión. Webcam PC aporta posición relativa/trayectoria y amplitud articular **observable**, con confianza; sin cámara no inventar esa medida. No medir fuerza muscular, diagnosticar movilidad/fatiga ni derivar estatura/edad como multiplicadores de potencia. No almacenar medidas corporales identificables como requisito del perfil.

Candidato matemático auditable por característica de intención: estimar neutral/bias y ruido, y referencia alta de movimiento cómodo a partir de varias repeticiones válidas, sin usar máximo aislado. Mapear `u = clamp((feature - neutral) / spanAccepted, 0, 1)` con respuesta monotónica y saturación; dirección y signo/eje de spin viajan separados. Es sólo modelo de ensayo: la combinación de features y curva por deporte siguen pendientes. No aplicar una sola norma de aceleración a toda potencia ni convertir reacción/precisión en potencia artificialmente mejorada.

`spanAccepted` necesita margen sobre ruido, rango medible y gain máximo de la modalidad; nunca división por cero, salto por una única muestra ni amplificación ilimitada de un perfil débil. Perfiles con rango menor a lo resoluble se declaran insuficientes para esa modalidad y ofrecen alternativa accesible/recalibración; no obligar a superar un mínimo físico absoluto que excluya movilidad limitada. Mínimos/máximos y curvas dependen de sensores/modos y se aprueban tras evidencia física; no fijar números universales por edad o talla.

Separar neutral y drift del rango de intención, y no integrar aceleración para afirmar velocidad absoluta. En boliche, normalizar intención de potencia/spin puede facilitar techo virtual comparable, mientras dirección, release/timing y técnica siguen determinando la acción. Unity conserva física, superficies, eje/spin y scoring.

### Justicia y calibración artificialmente débil

Todos compiten bajo el mismo techo virtual y reglas declaradas para su modalidad; una práctica débil no debe dar gain ilimitado, más potencia máxima ni tolerancia extra de precisión. Verificar varias repeticiones, consistencia/ruido, límites de gain y estabilidad del perfil. Observar discrepancias sostenidas entre rango calibrado y actividad posterior y proponer verificación entre rondas, sin etiquetar automáticamente fraude: también puede ser cambio natural, grip o accesibilidad.

No pedir fuerza máxima para demostrar honestidad. Una calibración deliberadamente subestimada puede ser difícil de distinguir de limitación real sólo con estas señales; registrar ese riesgo, no declarar anti-trampas resuelto ni sancionar por apariencia corporal. No inferir una expectativa de fuerza de webcam, talla o edad. Reglas de torneo decidirán cómo pausar/verificar ante discrepancia, evitando cambios retroactivos de score o handicap secreto.

Potencia normalizada, precisión, timing, técnica, coordinación y resultados se reportan separadamente. Suavizado, deadzone, sensibilidad y asistencia no pueden ampliar silenciosamente ventanas de acierto o borrar errores. Preferencias de feedback/layout son personales; asistencia que afecte resultados es regla competitiva explícita, con límites/versiones y modalidad visible. Si dos modalidades no tienen equivalencia física validada, no afirmar ranking comparable ni mezclarlas sin regla aprobada. No ajustar resultados para forzar tasas de victoria iguales.

### Lifecycle y calibración dinámica

Estados conceptuales: UNCALIBRATED → PRACTICE → VALIDATING → READY; muestras insuficientes devuelven a práctica acotada o UNAVAILABLE. Al comenzar ronda: READY → LOCKED con snapshot de perfil/reglas. QUALITY_LOST suspende interpretación afectada, neutraliza input activo y conserva resultados ya resueltos; termina en revisión entre rondas, no en recalculación silenciosa. PROPOSAL_PENDING → confirmación → READY con revisión nueva; rechazar conserva perfil sólo si sigue válido. Son estados de calibración, no reemplazo de IpcLifecycle.

En juego se pueden **detectar**, no aplicar silenciosamente: drift, cambio de grip/orientación física, pose distinta, variación sostenida de amplitud/rendimiento o calidad. Variación compatible con cansancio no es diagnóstico ni activa bonus. Screen rotation de interfaz distinta de cambio de agarre; transformación de presentación determinista no cambia gain competitivo. Sesión/ronda, mano, dispositivo, minijuego, fuentes y revisión de reglas vinculan el perfil. Cambiar teléfono/mano/referencia puede exigir nueva práctica antes de competir.

Ajustes graduales candidatos sólo entre rondas: pequeños pasos con límite de cambio, evidencia y explicación breve («¿Probamos otro calentamiento?»); Unity valida y el jugador confirma. Registrar revisión anterior/nueva, causa y effectiveFromRound. En mitad de gesto/ronda no mutar sensibilidad, amplitud de referencia, asistencia, spin ni scoring. Recalibración invalidada por calidad debe pausar o cancelar intento según política deportiva futura, sin consumir oportunidad injustamente ni permitir repetir una acción resuelta a voluntad.

### Contratos conceptuales adicionales

| Grupo | Conceptos / autoridad |
|---|---|
| Referencia de calibración | calibrationId/revision, playerId, deviceProfileId, minigameId, hand/grip/frame; Unity acepta vínculo, no confiar en perfil libre enviado. |
| Práctica | challengeId/algorithmVersion, sampleWindow, pruebas válidas/rechazadas y reason; samples acotadas y temporales, no vídeo persistente. |
| Parámetros | neutral, comfortable spans por característica, ruido observado, unidades, responseCurveVersion, deadzones/gain bounds y quality; estimaciones agregadas, no fuerza muscular. |
| Fuentes/degradación | availableSources, requiredSources, confidence desconocida explícita, calibrationMode, fallbackPolicyId; distintas modalidades no equivalencia asumida. |
| Snapshot competitivo | ruleSetId/version, calibrationRevision, roundId, maxVirtualPower, assistance policy y effectiveFromRound; Unity único propietario. |
| Ajuste | proposalId, cause, boundedDelta, expectedRevision, acceptance/rejection, effectiveRound; operación fiable/idempotente, revisión vieja rechazada. |
| Preferencias | Layout, inversión declarada, feedback/audio/haptics, accesibilidad y mano; separadas del snapshot que afecta equilibrio. |

No se agrega DTO ni wire format ahora. Codecs futuros JS/Dart comparten la misma semántica; no normalización oficial distinta en Flutter. Persistencia opt-in de preferencias y rangos funcionales mínimos; perfil local puede revelar patrones personales, no considerarlo anónimo por quitar nombre. No vídeo, plantillas biométricas identificables, embeddings de identidad ni historial corporal bruto por defecto; no cloud. Borrado/reset de perfil y retención de evidencia con finalidad/consentimiento explícitos, sin diagnosticar salud.

### Pérdida de cámara y otras fuentes

Webcam PC ausente, oclusión/confianza insuficiente o asociación corporal ambigua: marcar fuente no disponible; no cambiar jugador por landmark más cercano sin confirmación. Modalidad sensor-only sólo si esa acción puede interpretarse y se valida con perfil/reglas propios; informar al jugador y decidir antes de ronda. Si el deporte requiere trayectoria corporal, pausar/recalibrar o ofrecer otra modalidad, no inventar pose. En boliche, gyro-only no demuestra trayectoria ni separa de forma fiable toda torsión de brazo/muñeca. Reaparición de cámara requiere comprobar vínculo/referencia; no mezclar calibraciones incompatibles a mitad de tirada. Sin gyro/sensores/permiso, alternativa touch no acredita paridad completa de movimiento.

### Seguridad física y pruebas futuras

Desafíos cortos y cómodos, movimientos controlados, espacio libre visible y teléfono siempre sujeto. No competir por fuerza real, exigir amplitud extrema, lanzar teléfono ni golpear objetos/personas. Permitir pausa, gesto de menor amplitud y alternativa sentada/adaptada; recomendaciones de sujeción no convierten accesorios en obligación. Detener prueba si participante indica molestia/inseguridad; no interpretar eso como derrota o diagnóstico.

Antes de implementar cada deporte, acordar estudio físico con diversidad de edad, talla, rango cómodo y lateralidad, sin seleccionar sólo jugadores que ya funcionan. Usar categorías amplias/autodeclaradas sólo si necesarias para evaluar producto y consentidas, nunca reglas de potencia. Menores: autorización del responsable, asentimiento apropiado y supervisión adulta; participación voluntaria, posibilidad de retirarse y privacidad. No guardar vídeo/identidad/medidas corporales por defecto ni publicar evidencia reconocible; condiciones de estudio y datos recogidos requieren aprobación específica.

| Nivel | Pruebas propuestas | Resultado exigido |
|---|---|---|
| Unit/fixtures | span cero/ruido/outliers, monotonía/saturación, unidad/ejes, perfiles débiles, revisiones y LOCKED | Sin valores inválidos/gain ilimitado; límites idénticos entre clientes; ninguna mutación competitiva durante ronda. |
| Integración | Java transporte y Unity perfil/snapshot, mensajes duplicados/viejos, cancelación/pérdida de fuente | Una sola revisión efectiva, neutralización y causa observable; no segundo cálculo autoritativo. |
| Emulador | Presentación, layout/feedback compatible, confirmación/rechazo y errores | QA de UX/contrato; no amplitud, equidad o diversión físicas PASS. |
| Físico por fuente | PWA/Flutter cuando implementados, webcam PC, zurdo/diestro, movimientos lineales/giro, sensor/cámara ausente | Señales y degradación reales, incertidumbre/errores publicados por dispositivo y modalidad. |
| Participantes | Práctica/repetición/recalibración, distintos rangos físicos, gesto cómodo equivalente | Comprensión, duración, disfrute, consistencia y oportunidad de potencia medida; no equivalencia competitiva sólo simulada. |
| Adversarial/controlado | Práctica artificialmente débil, spike fuerte, grip/orientación cambiados, reducción sostenida de rendimiento | Techo/gain acotados; revisión explícita; no sanción ni recalibración automática por cambio de calidad. |

**Targets de investigación propuestos, no gates Accepted numéricos:** tiempo total mediano ≤45 s y P95 ≤90 s por deporte; ≥80% completan sin explicación técnica prolongada; satisfacción/disfrute mediano ≥4/5; tras instrucción de intención cómodamente alta, ≥90% de participantes pueden alcanzar ≥90% de potencia virtual sin esfuerzo extremo. Reportar denominadores, abandonos, ayuda, repeticiones, modalidad/dispositivo y diferencias por grupo; si falta tamaño de muestra suficiente, descriptivo/inconcluso, sin pretensión estadística. El PO aprobará protocolo/tolerancias antes del estudio; no forzar esfuerzos para obtener esos targets.

Consistencia propuesta: tres calibraciones separadas por participante/modo, acciones de intención similar, diferencia de potencia normalizada y dirección/spin por separado; target inicial de variación ≤0.10 de escala [0,1], sujeto a protocolo físico aprobado. Precisión de detección: acuerdo con referencia anotada (consentida), falsos gestos/misses, incertidumbre y oclusiones; no presentar confianza del modelo como accuracy. Timing/reacción no se evalúan sin controlar latencia de estímulo, teléfono y transporte.

Equilibrio: medir oportunidad de techo de potencia **y** precisión/timing/coordinación, con crossover/modos y orden balanceados cuando sea viable; investigar asociación de resultados con características físicas y asistencia, sin exigir idéntico score/tasa de victorias ni borrar habilidad. Evaluar comparabilidad con un margen práctico aprobado antes del estudio; ausencia de significación estadística no demuestra justicia. Facilidad de recalibrar: tiempo, ayuda necesaria, comprensión de cambio, revisión efectiva sólo siguiente ronda y seguridad. Todos estos resultados hoy **NOT RUN**.

### Dependencias y condición para avanzar

Transportes/sensores reales y relojes → fuentes PC/capacidades/degradación → contrato/revisión de perfil y snapshots Unity → experimento de normalización → desafíos jugables por deporte autorizados → pruebas con participantes → ajustes competitivos explícitos. Se conserva la misma Fase 0 y sus incrementos cerrados. Fase 0 actual sólo prepara conceptos/gates; no construir minijuegos, captura, normalización definitiva ni UI final por esta enmienda. Incertidumbres: rango cómodo vs subestimación deliberada, calidad de fuentes, accesibilidad/modalidades, número de participantes y tolerancias físicas. Nada garantiza justicia competitiva antes de evidencia humana real.


## 9. Reconocimiento de intención y prevención de activaciones falsas — DEC-009

**Requisito Accepted; detector, ventanas y umbrales Proposed; precisión física NOT RUN.** Inferir una acción deportiva a partir de evidencia temporal contextual, no medir intención mental ni garantizar reconocerla siempre. Unity posee el detector y confirmación oficial; Java transporta entradas y los clientes capturan señales, sin emitir acciones competitivas autoritativas. Un único umbral de aceleración o una muestra intensa no confirma un gesto.

### Máquina de estados por jugador y gesto

La instancia se vincula a jugador admitido, ronda/turno, connectionEpoch, perfil/reglas y fuentes disponibles. Es independiente de IpcLifecycle y de los estados de calibración; esos sistemas aportan condiciones de habilitación. Buffers/ventanas limitados, sin buscar indefinidamente un gesto ni procesar muestras viejas como actuales.

| Estado / transición | Evidencia y efecto propuestos |
|---|---|
| IDLE → PREPARING | Acción habilitada por Unity y patrón de preparación compatible con modalidad/perfil. Ruido, caminar o reajustar agarre no bastan. |
| PREPARING → ACTION_CANDIDATE | Inicio coherente de trayectoria/dirección, orientación y evolución de señales; abre una ventana y candidateId único. Todavía no hay lanzamiento/golpe oficial. |
| ACTION_CANDIDATE → CONFIRMED | Patrón suficiente y causal de ejecución/liberación, calidad/identidad/revisión válidas, coherencia de fuentes utilizables. Unity emite una sola acción con actionId. |
| CONFIRMED → RECOVERY | Bloquear nuevas emisiones del mismo gesto; observar finalización/retorno o política de recuperación acotada por deporte. |
| RECOVERY → IDLE | Nueva preparación permitida tras finalización, rearme/histeresis y cooldown configurado. Una señal alta persistente no rearma por sí sola. |
| PREPARING o ACTION_CANDIDATE → RECOVERY/IDLE | Cancelación explícita, patrón incoherente, ventana expirada o candidato rechazado; razón registrada, cero acción. Rearme evita crear otro candidato con la cola del mismo movimiento. |
| Cualquier estado → suspensión de detección | Pérdida de identidad/fuente imprescindible, desconexión, reloj/calibración inválidos o pausa Unity. Vaciar candidato y neutralizar entradas continuas; reanudar sólo tras referencia válida y nueva preparación. |

La confirmación requiere evidencia mínima suficiente **hasta el momento de ejecución**. No esperar necesariamente todo el follow-through para lanzar, ni ejecutar prematuramente para luego intentar retirar puntos. Finalización/recuperación permiten validar continuidad y rearme; no son segundo disparo. Si un deporte realmente necesita evidencia posterior, su espera deberá ser acotada y presupuestada. Animación de preparación puede ser provisional; física/scoring oficiales sólo después de CONFIRMED. Una desconexión posterior no deshace una acción ya resuelta ni genera otra al reconectar.

Histeresis de entrada/salida, duración mínima válida, ventanas máximas, recovery y cooldown por deporte/perfil: todos medidos y limitados por reglas competitivas. Ventanas suficientemente amplias para gestos lentos accesibles, sin aceptar una caminata prolongada por acumular muestras. No imponer un mínimo universal de velocidad/fuerza como sustituto de secuencia; para modalidades que lo requieran, armar mediante touch/gesto explícito puede desambiguar, con regla visible y paridad PWA/Flutter. No obligar touch a todos ni presentarlo como paridad completa de movimiento.

### Umbrales personalizados e independencia de intensidad

Calibración inmersiva incluye reposo/ruido, movimiento habitual, preparación válida y acciones cómodas de distinta intensidad, con repeticiones. Separar características para reconocer **patrón de acción** de características para estimar **intensidad normalizada**. Mano/grip, orientación, amplitud, variaciones naturales y capacidades del dispositivo influyen en la referencia. Niño/adulto o movilidad limitada no precisan la misma aceleración absoluta. No exigir máximo esfuerzo ni fijar g universales.

| Caso | Decisión objetivo |
|---|---|
| Movimiento accidental lento / caminar / ajustar postura | Rechazado por secuencia/contexto incompatibles, no por lentitud. |
| Lanzamiento suave intencional | Confirmado si secuencia coherente y fuentes suficientes; intensidad baja respecto del rango calibrado de esa ejecución. |
| Movimiento rápido accidental / vibración / sacudida sin preparación | Rechazado aunque haya pico grande. |
| Lanzamiento intencional de intensidad alta cómoda | Confirmado por patrón, potencia según normalización y límites Unity. |
| Gesto lento pero amplio y claramente intencional | Puede ser válido; su potencia no se deduce sólo de rapidez, sino del modelo calibrado del deporte. |

Un perfil artificialmente débil no reduce ilimitadamente umbrales de intención ni aumenta gain/techo. Reglas acotadas, calidad suficiente y revisión LOCKED por ronda según DEC-008; cambios se proponen entre rondas, nunca adaptar thresholds silenciosamente para hacer acertar al jugador. Evitar penalizar limitación real como fraude. Reconocer gesto no equivale a eliminar errores de precisión/timing ni regalar éxito.

### Fusión y pérdida de cámara

Comparar sensores y observación **del jugador asociado** en frames/tiempos alineados: preparación del brazo, trayectoria/extensión/dirección, gyro/orientación/aceleración y calidad. Congruencia espacial/temporal no se presume por timestamps parecidos; considerar incertidumbre de sync y frame de cámara. Caminar con teléfono puede producir señales periódicas; distinguir por contexto, preparación, patrón y referencia, sin prometer separabilidad perfecta.

Cámara ausente/ocluida, baja confianza o asociación ambigua: excluir sus observaciones del candidato y evaluar con detector sensor-only específico, reglas/calibración/ventanas propias. No exigir cámara para toda acción reconocible por sensores; tampoco conservar el mismo score de confianza simulando que la fuente sigue presente. Cuando sensor-only no puede desambiguar suficientemente ese gesto, pausar/rechazar con motivo o usar modalidad accesible explícita; no activar con falsa certeza. Un cambio de modalidad competitiva requiere política declarada, no ventaja oculta durante acción.

Fuentes móviles perdidas/late: no rellenar gyro/pose con cero ni extrapolar indefinidamente; datos fuera de ventana se descartan y gaps se registran. Muestra tardía después de rechazo/CONFIRMED no reabre candidato ni produce liberación retroactiva. Si llega información insuficiente para cumplir el deadline causal, señalar calidad insuficiente en vez de esperar sin límite.

### Boliche y mazo: semántica conceptual vigente

Boliche: preparación/neutral → balanceo coherente → candidato de lanzamiento → dirección y momento virtual de liberación → CONFIRMED una vez → finalización/recovery. Giro de muñeca previo no libera bola por sí mismo; movimiento lateral sin lanzamiento completo no dispara. Lanzamiento suave coherente sí se admite. Spin/eje se estiman sólo desde rotación calibrada y temporalmente vinculada a la liberación, no de una sacudida posterior; Unity aplica física/hook después de confirmar el lanzamiento. Trayectoria lateral, aceleración y rotación permanecen separadas. No transmitir «hook confirmado» como input móvil.

Gorilla Smash aplica el principio de gesto descendente intencional: preparación/levantar brazo no es golpe; confirmar gesto no confirma impacto ni puntos. Unity resuelve acción, timing, objetivo e impacto. El ejemplo anterior de boxeo queda fuera del MVP; no se implementan guardias/jabs/ganchos. Las otras disciplinas se detallan en §10.

### Asociación multijugador 1–4 y contratos

Java vincula sesión técnica/connectionEpoch/playerId al canal admitido. Unity vincula plaza/avatar/turno al control y, cuando disponible, a track corporal PC mediante emparejamiento/calibración explícitos. trackId es observación temporal, no identidad biométrica. Entrada/salida, cruce/oclusiones, cambio de brazo o reaparición requieren revalidación; no reasignar por cercanía ni asociar a otro cuerpo porque su movimiento coincide casualmente. Cámara por turnos sigue baseline; cuatro teléfonos no prueban tracking simultáneo de cuatro personas.

Si track ambiguo, no usarlo para confirmar acciones de ninguna plaza con falsa certeza. El sensor pertenece al canal admitido, pero por sí solo no prueba quién sostiene físicamente el teléfono; sensor-only evalúa el gesto de ese control con su modalidad válida, no identifica visualmente al portador. Transferir control exige acción explícita/recalibración según reglas. No reconocimiento facial ni cámara móvil.

| Contrato interno conceptual Unity | Contenido y regla |
|---|---|
| Candidato | candidateId, gestureType, session/player/round/epoch, detectorVersion, calibrationRevision/ruleSetVersion, state, inicio y ventana temporal. |
| Evidencia | Fuentes realmente utilizadas, sensorQuality, poseQuality si existe, associationStatus, temporalUncertainty y evidenceScore. Score heurístico no es probabilidad calibrada; confidence desconocida explícita. |
| Interpretación | executionTime/frame, direction, normalizedIntensity y rotation features cuando válidos; ausencias/limitaciones declaradas. |
| Decisión | confirmed/rejected/cancelled/qualityInsufficient, reasonCode, actionId sólo al confirmar; log mínimo sanitizado. |
| Consumo | ActionId consume como máximo una vez dentro de sesión/ronda; candidato/epoch viejos no reabren acción. Control fiable y dedup del transporte no sustituyen dedup del gesto. |

Reason codes propuestos: NOT_ARMED, INCOMPLETE_PATTERN, DIRECTION_INCOHERENT, AMBIGUOUS_PLAYER, QUALITY_INSUFFICIENT, STALE_SAMPLE, CALIBRATION_CHANGED, WINDOW_EXPIRED, CONNECTION_LOST, DUPLICATE_ACTION, CANCELLED. No distribuir estas inferencias como DTO móvil definitivo ni duplicar detector en JS/Dart/Java. Buffer temporal y registro de actionIds acotados por sesión/ronda; fuera de ventana, rechazo por contexto/epoch, no cache global ilimitada.

### Criterios futuros, métricas y evidencia reproducible

Corpus de sesiones etiquetadas: reposo/vibraciones, caminar, ajuste de postura/grip, lateral sin lanzamiento, preparación abortada, gesto suave/lento completo, gesto fuerte cómodo, giros aislados, acciones sucesivas, otros jugadores, cruces/oclusiones y pérdida de señal. Repetir por jugador/dispositivo/mano/modalidad; orden balanceado y prueba final con sesiones no utilizadas para ajustar detector. Una etiqueta incluye inicio/preparación, intención declarada para la tarea, ejecución esperada, finalización y condiciones, sin afirmar leer intención mental.

Referencia mediante anotación en vivo y timestamps sincronizados; si hace falta vídeo de referencia, autorización/consentimiento, retención/borrado y sanitización específicos, nunca grabación por defecto. Incertidumbre/interobservador y casos ambiguos reportados, no eliminados para mejorar precisión. Seguridad/privacidad y participación de menores según §8.

| Nivel | Pruebas / aceptación propuesta |
|---|---|
| Unit/fixtures | Transiciones válidas/rechazo/cancelación, emisión máxima una por candidateId, sin acción en PREPARING, rearme/histeresis, lento coherente vs pico aislado, límites/buffers. |
| Integración Unity/Java futura | Duplicate/reorder/late/epoch vieja, disconnect antes/después de confirmación, revisiones LOCKED y aislamiento de jugadores. Cero acciones duplicadas, retroactivas o de otra plaza en estos casos deterministas. |
| Emulador | UI/contrato/estados con fixtures; nunca precisión o latencia física del detector. |
| Físico Android/iPhone y Flutter futuro | Gestos etiquetados y negativos, cámaras válidas/ausentes/ambiguas, sensores y manos distintos; resultados por dispositivo/modo/grupo. |
| Multijugador | Movimiento sólo de A mientras B está quieto, cruces y reasignación; no atribución automática a B. Sensor-only bajo ambigüedad se prueba y reporta aparte. |
| Accesibilidad/equilibrio | Acciones lentas/suaves y movilidad diversa reconocibles; no ventaja artificial por bajar thresholds; asistencia explícita y mismo techo competitivo. |

Métricas separadas: falsos positivos por minuto de actividad negativa y por episodio; falsos negativos por gesto intencional etiquetado; duplicados por gesto; acciones prematuras anteriores a liberación esperada; tasa de confirmación lento/suave y rápido; rechazos/cancelaciones/quality-insufficient por causa y cobertura de cada modalidad. No llamar precision a accuracy global con mucho reposo; reportar confusion matrix/counts y denominadores. Medir cross-player misattribution separado de FPR general.

Latencia: tiempo físico anotado de ejecución → CONFIRMED Unity → respuesta visible, distribuciones por sesión/dispositivo/modalidad. Documentar incertidumbre de clocks/cámara/render; RTT no mide reconocimiento ni respuesta visible. Registrar cuánto cuesta ventana de decisión/fusión. Proponer presupuesto tras baseline físico y revisarlo antes de gate final; no agregar espera ilimitada buscando confirmación perfecta ni excluir lentos porque tardan más en **completar** gesto. Tiempo de preparación/completado se mide aparte de demora posterior a ejecución.

Valores de hysteresis, ventana/duración mínima, cooldown, score y objetivos FPR/FNR/latencia **por determinar con mediciones**, con validación separada y aprobación previa al ensayo de aceptación. No declarar PASS del reconocimiento por tener máquina de estados ni elegir thresholds universales ahora. Invariantes deterministas (sin duplicados/prematuros/replay/cross-player en fixtures definidos) sí deben pasar; su PASS futuro no demuestra tasas físicas cero. Evaluación física actual NOT RUN.

### Dependencias y roadmap conservado

Calibración inmersiva/rango cómodo y ruido → fuentes/sync/identidad fiables → contrato candidato/estado y snapshot Unity → pruebas de patrones/negativos/degradación → validación física independiente → incorporación al deporte autorizado. Reutilizar timestamps, perfiles y calidad existentes conceptualmente; no segundo pipeline/estado competitivo ni rediseño del lifecycle. La decisión WT/RTC sigue independiente y pendiente; esta enmienda no cambia próximo Spike de transporte ni reabre foundation/2A/2B/2C. Sólo diseño actual; sin detector definitivo, cámara, gameplay, infra, nuevas dependencias o UI final.


## 10. Cinco disciplinas oficiales y Spike de captura — corrección 2026-10-07

Cotejadas con v4 §§17–21 sin cambiar cantidades, scoring, orden de torneo ni física. Ninguna mecánica de esta tabla está implementada en el capturador. `CANDIDATE` del requisito y `ACTION_CANDIDATE` de §9 son el mismo estado conceptual, no dos detectores.

| Disciplina / regla conservada | Fuentes e intención / práctica | Negativos físicos y aceptación futura |
|---|---|---|
| Gorilla Smash — Mazo, **3 intentos/jugador** (§17) | Motion/orientación y webcam PC cuando válida: preparación → trayectoria descendente → ejecución candidata → gesto confirmado → recuperación. Objetivos de entrenamiento estiman ruido/rango cómodo/dirección/timing. Potencia normalizada y confianza separadas del impacto Unity. | Levantar brazo, caminar, vibración, movimiento ajeno y preparación abortada no golpean. Descenso intencional suave válido con menor potencia según perfil; un gesto no consume varios intentos ni produce impactos fantasma. |
| Coconut Throw — Lanzamiento de bala, **3 intentos/jugador** (§18) | Preparación/impulso de brazo, aceleración relativa, dirección/elevación y release virtual único; práctica con coco virtual. Unity convierte features a ángulo/velocidad/potencia y distancia. | Levantar teléfono/caminar/sacudida sin secuencia no lanzan; movimiento suave coherente válido. Release ni prematuro ni duplicado; teléfono siempre sujeto. |
| Coconut Bowling, **3 rondas/jugador** (§20) | Mano/grip, balanceo/trayectoria, aceleración, gyro/orientación y liberación. Zurdo con giro derecha: estimar spin calibrado separado de dirección lateral; Unity genera curva sólo si física justifica. | Movimiento lateral incompleto/giro aislado no lanzan. Gesto suave completo puede confirmar. Máximo un lanzamiento por gesto; giro post-release no cambia retroactivamente spin. No asumir una sola bola por ronda ni cambiar reglas de rondas sin fuente normativa. |
| Jungle Archery, **5 flechas/jugador** (§19) | Orientación yaw/pitch relativos a neutral, estabilidad, botón mantenido/tensión y release explícito. Calentamiento con blancos y tensión cómoda; sin fuerza corporal para potencia. | Temblores/giros no disparan. Sólo soltar tras tensión válida confirma; touchCancel, desconexión, pausa o pérdida de fuente cancelan tensión y no equivalen a release voluntario. Evento repetido no consume otra flecha. Yaw absoluto no garantizado; referencia/drift declarados. |
| Jungle Slice, **60 s/jugador** (§21) | Webcam PC principal: manos del jugador activo, segmento de trayectoria, velocidad/dirección y confianza; intersección con hitbox Unity. Calentamiento calibrado visual. Teléfono no obligatorio para detección si no aporta valor. | Mano quieta apareciendo sobre fruta no corta; espectador/track ambiguo no puntúa. Conservar tracking válido AND velocidad ≥ MinimumCutVelocity AND segmento intersecta hitbox. Umbral por medir dentro de reglas, no borrar requisito para admitir toda lentitud. Un objeto no puntúa dos veces por muestras del mismo corte; un barrido válido puede cortar varios objetos conforme combo v4. Sin cámara fiable no afirmar Slice completo sensor-only: pausar o modalidad explícita futura, sin frutas fantasma. |

La intención lenta puede ser reconocida aunque no cumpla velocidad de corte exigida por Slice: distinguir intención reconocida, elegibilidad de acción, contacto e impacto/scoring. No todos los deportes usan simultáneamente todas las fuentes. Asociación ambigua de cámara excluye esa fuente; sensor-only viable según disciplina y evidencia, no fallback universal que falsee reglas. Timing, técnica y precisión no se sustituyen por agitar violentamente el teléfono; normalización/umbrales siguen DEC-008/009, límites y snapshot Unity.

**Banana Catch — propuesta post-MVP únicamente:** inclinar para mover canasta y atrapar plátanos podría probar orientación/acelerómetro con menor dependencia visual. Investigar neutral/deadzone/límites de desplazamiento y drift, no aceleración como posición absoluta. No sexto juego del torneo, no compromiso de implementación, sin cambios a v4/backlog/MVP. Cualquier incorporación requiere aprobación separada.

### Spike actual implementado: únicamente adquisición/diagnóstico local

[Capturador y página aislada](../PWA/spikes/sensors/README.md): permisos DeviceMotion/Orientation, valores reales disponibles, timestamps/Hz observable, validación, orientación de pantalla, interrupción, suspensión/reanudación y cleanup. No reconoce intención, normaliza potencia, calcula spin, usa cámara ni envía input a gameplay. No convierte latest sample en PlayerInput; no nuevo protocolo ni decisión de WT/RTC. Ocho tests automatizados y regresión PWA 5/5 PASS; hardware Android/iPhone NOT RUN. Cadencia sintética de tests no es frecuencia física validada.

Dependencias futuras: validar sensores físicos/origen seguro y tiempo → transporte/mensajes comunes → fuentes PC y calibración de cada disciplina → recognizers por deporte con corpus positivo/negativo → pruebas físicas de intención/normalización/latencia → gameplay autorizado. El capturador no reabre foundation/IPC ni consume intentos, flechas/rondas/tiempo de competición. §8/9 siguen conceptuales; no se implementa gameplay/calibración/fusión final.


## Infraestructura implementada — 2026-10-08 UTC

La restricción histórica de sólo diseño queda reemplazada únicamente para CameraInput, Player Lock,
alineación y FusionFrame Software/Lab por el incremento autorizado. [Implementación, corpus, evidencia y
límites](PHASE0_CAMERA_FUSION_VALIDATION.md), DEC-012. No se implementa interpretación deportiva, calibración
jugable final, boliche ni scoring. DEC-008/009 mantienen sus gates físicos y futuros desafíos; ningún PASS
lab acredita equidad/precisión o intención humana. Procesamiento de cámara local PC, sin vídeo móvil.
