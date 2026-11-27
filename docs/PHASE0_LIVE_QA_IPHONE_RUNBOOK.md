# Phase0 Live QA — procedimiento histórico

> Sustituido operativamente por [Main Menu → Centro de Pruebas](PHASE0_IPHONE15_LIVE_QA_RUNBOOK.md). Esta versión documenta el candidato local anterior; usar el runbook nuevo.

DEC-029; T062/#111 soporta T019/#33. Superficie técnica, sin gameplay/score. **T019 OPEN / Validation; Final Exit Audit BLOCKED.** Una prueba con un iPhone no acredita cuatro teléfonos ni los gates completos.

## Inicio A–O

A. Abre `/home/josuegalvan/Documentos/GorillaEscape-Phase0-LiveQA-2026-10-14` y ejecuta `./run-qa.sh` desde Terminal. El paquete incluye Player, Java21, Python y Vision. No abras otra build simultáneamente. Tamaño recomendado 1280×900. El launcher comprueba hashes antes de arrancar.

B. Espera **CAMERA READY**. Debe verse tu webcam, sin deformación. Tracking puede decir LOST si no encuentra landmarks válidos; eso no es fallo de captura. No abrir la webcam en otra aplicación. Si aparece CAMERA FAILED, cierra esta build y vuelve a intentar; no cambies exposición/control del pipeline ni mates procesos ajenos. ROI amarillo, hombros/brazos/manos verdes; rojo marca ambigüedad. Los subjectId son etiquetas efímeras, no identidades. Preview y landmarks son latest independientes, no corresponden necesariamente al mismo frame.

C. Espera QR AVAILABLE. Comprueba que el origen indicado sea HTTPS LAN y no localhost. La PC actual tiene Wi-Fi `wlp0s20f3`; se selecciona la LAN física existente. Si cambia, la dirección y el certificado IP se regeneran mediante el mecanismo existente. Si hay varias interfaces activas, antes de ejecutar selecciona la correcta con `GORILLA_LAN_INTERFACE` y `GORILLA_LAN_ADDRESS`. El backend falla de forma explícita si no puede elegir una LAN; no cae a localhost.

D. Pon el iPhone 15 en la misma Wi-Fi. Para la primera preparación abre en Safari la URL **Prepare** mostrada en la PC (HTTP LAN:8081/prepare por defecto). Sólo sirve la CA pública e instrucciones, no PWA/JOIN. Compara la huella SHA256 con la mostrada en la PC antes de instalar. Instala el perfil en Ajustes → General → VPN y gestión de dispositivos; después General → Información → Ajustes de confianza de certificados → activa confianza SSL de esa CA. Abre la página HTTPS `/prepare` para verificar confianza normal. No ignores advertencias TLS. La CA privada permanece en la PC. Retira la confianza del iPhone cuando dejes de confiar en esta instalación.

E. Selecciona **Prepared** sólo después de preparar el dispositivo. Selecciona **Unprepared** únicamente si confirmas que realmente no tiene la CA/cache/permiso preparados y estás midiendo todo el onboarding; no se deduce automáticamente. Para el gate completo sin preparar, inicia además un cronómetro externo **antes de D**; QR→technical-ready por sí solo no acredita PREPARE+JOIN <60s. Marca **I confirm a physical phone** únicamente al usar el iPhone real. Pulsa **Regenerate QR** después de elegir clasificación; así se registra el estado elegido al mostrarlo. QR one-use y TTL30s; regenerar no borra jugadores existentes.

F. Escanea con Cámara de iPhone y abre el enlace en Safari. En la PWA pulsa **CONECTAR**. Si expiró/está usado, vuelve a la PC y regenera. No copies el enlace ni hagas screenshots del QR. SESSION FULL incluye clientes retenidos y admisiones pendientes; no resetea arbitrariamente a los jugadores.

G. Pulsa **ACTIVAR CONTROL** y acepta movimiento/orientación. El permiso debe activarse desde ese gesto real. Si se deniega, sigue el reintento de la PWA; no inventes capacidades. Registra modelo/versión iOS/Safari manualmente en tu resumen de prueba, sin identificadores ni fingerprinting.

H. Comprueba el slot: CONNECTING antes de HELLO, CONNECTED al completar red, ACTIVE al llegar input, DISCONNECTED al perder sesión; EMPTY sin cliente retenido. `N/I/P` significa Network Ready / Input Ready / Player Ready del lobby real. Network e Input pueden ser true con Player Ready false: esta UI no declara preparación oficial ni inicia un juego. QR→technical ready termina con Network+Input para el cliente nuevo de esa admisión.

I. Selecciona **Player 1** (o su slot real), mueve suavemente el teléfono. Observa acceleration XYZ, includingGravity, rotationRate αβγ, orientation αβγ y screen orientation. Un eje disponible con valor0 se muestra0; uno ausente dice NOT MEASURED. INPUT_LIMITED representa calidad degraded existente; NO_SENSOR_INPUT no afirma permiso denegado.

J. El panel **VISIBLE INPUT RESPONSE** muestra un punto con acceleration XY aceptada, escala ±20m/s² y saturación visual reproducible. Se desactiva sin XY frescos (250ms) o desconectado. No identifica gestos, no aplica física deportiva ni otorga puntos. La PC debe ver cambios de seq/epoch y del indicador.

K. Comprueba FPS capture/inference/preview y Unity Update. Preview es máximo5FPS; no es tracking30FPS. La UI marca Hz motion/orientation/emission y phone RTT **NOT MEASURED** porque esos informes del navegador no viajan en PhoneInput actual. En la PWA usa las mediciones existentes REST ≥15s/GENTLE ≥25s y su resumen diagnóstico para obtener `actualMeasuredHz`/emission/RTT; conserva esos resúmenes junto al de Unity. No calcules Hz a partir del refresco de pantalla, secuencias coalescidas o FPS. Platform/browser tampoco llegan al contrato actual; registra versiones manualmente.

L. **Start measurement**, pon Safari en background y vuelve. Observa pausa/input freshness, estados y regreso de datos nuevos; repite según matriz T019. Las cifras de Start/Stop delimitan la sesión QA, no cambian el protocolo ni la sesión de juego.

M. Bloquea/desbloquea el iPhone y observa si hubo suspensión corta o cierre del RTC. Registra duración, resultado y versión real. No atribuyas una recuperación corta de mismo canal a un reconnect nuevo.

N. Desconecta/reconecta Wi-Fi de ese teléfono. Cuando la sesión es recuperable, el transporte existente incrementa epoch; la PWA actual no añade aquí un botón nuevo de resume. Si Safari cerró el contexto/admisión, usa QR nuevo y registra **nuevo JOIN**, no reconnect exitoso. Clientes desconectados se retienen hasta TTL30s; espera su liberación si la sesión está llena. No recargues el QR consumido ni borres jugadores desde esta UI. Los contadores de conexión cuentan JOIN HTTP intentado y HELLO validado; los de reconnect cuentan endpoint resume y su HELLO, no cada cambio de visibility.

O. **Stop measurement** y **Export summary only**. Guarda JSON en `/home/josuegalvan/GorillaEscape-QA-Summaries`. Es una lista explícita de métricas/estados/timestamps, sin QR, tokens, IDs completos, imágenes, video, landmarks o sensor traces. **Reset QA counters** cambia sólo baseline QA y temporizadores; no borra sesiones/jugadores. Cierra la ventana normalmente y espera cierre de Java/Vision; no debe quedar listener QA ni cámara ocupada por esta build. No hagas screenshots ni grabación de la sesión.

## Límites y problemas que requieren registro

El teléfono reporta UTC source event; Java y Unity registran receipt/accept UTC y Unity apply/end-of-frame. Java→Unity y procesamiento local son segmentos de PC; source→Java requiere medir offset y incertidumbre. End-of-frame es un marcador del Player, **no scanout/photon**. No sumar segmentos para acreditar P95 físico <150ms. El resumen no contiene un PASS automático de T019.

Para una PC con firewall, permite únicamente los endpoints locales necesarios conforme al runbook T008 existente: HTTPS8443, preparación pública8081 y RTC local; no abrir rangos preventivos ni desactivar firewall. Aislamiento de clientes del router puede bloquear PC↔iPhone aunque compartan SSID. Registra el bloqueo y usa una LAN autorizada que permita comunicación. La pantalla no demuestra por sí sola accesibilidad del iPhone.

El preview usa sólo 127.0.0.1 y nunca está disponible en teléfono. El paquete `run-base.sh` conserva modo sin QA; `run-qa.sh` activa explícitamente la superficie. Windows/macOS y otras webcams no están acreditados por el paquete Linux de referencia.
