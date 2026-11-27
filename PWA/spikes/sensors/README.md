# Spike aislado de sensores PWA

> Continuidad T010, 2026-10-09: el módulo mantenido se promovió a `PWA/src/input/sensors/capture.js`; este entry lo reexporta. Los tests históricos se incluyen ahora en `npm --prefix PWA test`, junto con pruebas de calidad/captura. La UI React usa la misma adquisición. Los apartados siguientes describen el experimento histórico y no equivalen al estado operativo actual. [Gate vigente](../../../docs/PHASE0_T010_SENSOR_CAPTURE.md).


Implementado el 2026-10-07. No está conectado al shell React productivo, Java, Unity ni transporte móvil. No modifica package.json, lockfiles, Service Worker o protocolo. Se conserva la base React y su build; este experimento usa APIs browser directamente para aislar captura, sin dependencia adicional.

Prueba automatizada reproducible desde raíz:

```sh
node --test PWA/spikes/sensors/capture.test.js
```

Node 24 existente; 8 tests con EventTarget y fuentes/permisos/reloj simulados. Comprueban comportamiento del capturador, no sensores reales ni confianza HTTPS física. No necesita instalar, abrir red, iniciar Java o pedir permisos reales. Regresión existente separada: `npm --prefix PWA test`.

Página manual futura: `PWA/spikes/sensors/index.html`, módulo `capture.js`. En un entorno de desarrollo ya autorizado puede abrirse en la ruta `/spikes/sensors/index.html`; el servidor existente no se inició en esta tarea. Teléfono físico requiere HTTPS top-level confiable y preparación autorizada; HTTP por IP privada no soluciona sensores. `file://` no es procedimiento físico aceptado. Sin hosting publicado, CA móvil ni cambio de red. No afirmar que localhost de PC es localhost de teléfono.

Uso: pulsar «Permitir e iniciar» para invocar ambos requestPermission si existen dentro de la interacción, observar diagnóstico, mover cómodamente sin soltar el teléfono y detener. Permisos granted/denied/error/API ausente distinguidos; método requestPermission ausente NO equivale a sensor demostrado. Sólo campos/eventos efectivamente observados aportan evidencia. Se puede capturar motion u orientation por separado si la otra fuente falla.

Valores nulos preservados, NaN/Infinity/tipos no numéricos descartados y contados; Euler validado por rangos de API y interval no positivo inválido. Aceleración lineal e incluyendo gravedad separadas; rotationRate en grados/s, orientación Euler en grados, sin inventar gravity/quaternion/gyro raw. No thresholds físicos universales ni conversiones de gameplay. [Semántica W3C](https://www.w3.org/TR/orientation-event/).

Timestamps: monotónico de entrada del callback y timeStamp del evento por separado. No demostrar adquisición hardware, latencia real de captura ni pérdida de eventos hardware. Sequence local sólo del capturador. effectiveEventHz = intervalos positivos observados / tiempo entre callbacks en segmentos activos; no promete 50 Hz ni mide frecuencia de datos válidos. Eventos con todos los campos nulos pueden tener cadencia y no demostrar ningún sensor útil.

Diagnóstico de interrupción tras 2000 ms sin eventos en una fuente permitida, timer UI cada 500 ms: parámetros del experimento, no presupuestos de gameplay ni prueba de dispositivo defectuoso. Suspensión al ocultar página retira listeners de sensores; al volver, nueva referencia de intervalos excluye pausa. pagehide/Detener cancela captura/timer/listeners; tras bfcache se inicia manualmente con el botón. Respuestas tardías de permisos tras Stop no reactivan nada. No puede cancelar el diálogo del navegador, sí invalidar la continuación propia.

Memoria: sólo última muestra por fuente y contadores, sin historial de eventos/array/cola; ningún envío, fetch, cámara, localStorage, descarga de datos o log de muestras. Notificación UI periódica evita actualizar DOM por evento. Última muestra conservada es diagnóstico fechado, no input vigente. Orientación de pantalla separada de ejes/ángulos del sensor, sin recalibración silenciosa.

Físico Android/iPhone, frecuencia/precisión real, suspensión del navegador/OS y confianza TLS: NOT RUN. Guardar sólo evidencia sanitizada en prueba futura consentida, no IDs/red/biometría. No modificar los 10 PASS / 2 BLOCKED / 1 SKIP históricos del emulador.


## Regresión encontrada por Android Emulator — 2026-10-07

La validación nativa detectó que los timers por defecto perdían el receptor Window: UI permanecía REQUESTING mientras listeners recibían eventos. Se corrigieron sólo defaults de setInterval/clearInterval para llamar al Window inyectado. Suite actual 9/9 PASS con test del receptor/publicación RUNNING/cleanup; histórico 8/8 anterior conservado. Android Emulator final recibió eventos virtuales, publicó RUNNING y se detuvo en STOPPED. [Fallo conservado, resultados y límites](../../../docs/PHASE0_SECURE_MOTION_TRANSPORT_DECISION.md#16-android-emulator--validación-integrada-de-componentes-2026-10-07). Sin conexión de sensores a Java/WT/Unity ni validación física; no asumir paridad de sensores por valores emulados.
