# Spike propuesto: transporte seguro y sensores físicos

## Criterio vigente de cierre Software/Lab — aprobado por PO, 2026-10-07

Fase 0 puede cerrar como **COMPLETED — SOFTWARE/LAB** únicamente cuando arquitectura base implementada, integrada y todos los gates automatizados/laboratorio requeridos estén PASS. Esta autorización no equivale a declarar el cierre ahora. iPhone 15/Safari y Android físicos, cadencia móvil real, movimiento humano con teléfono/cámara y latencia física quedan **NOT RUN / DEFERRED**, obligatorios antes de aceptación final del MVP. Windows: DEFERRED. Los intentos fallidos de acceso iPhone y toda evidencia anterior se conservan; no se convierten en PASS. No se vuelve a solicitar teléfono para avanzar.

Orden obligatorio: A comparador WebRTC aislado → B DEC-010 y un transporte → C sesión/admission/QR POC → D protocolo común → E input móvil lab/Java/IPC/Unity → F webcam PC/pose/Unity → G asociación y temporal → H infraestructura Fusion/fixtures → I 1–4 clientes → J resiliencia/regresión → K auditoría → L cierre. No gameplay final, reconocimiento facial, cámara móvil, backend cloud ni Fase 1 antes del cierre. PR #94 permanece DRAFT. Sin push/merge autorizado para este trabajo. Secure-context/onboarding sin configuración del jugador sigue como riesgo independiente; resultados lab no demuestran compatibilidad física.

**Estado actual:** cierre IN PROGRESS; A PASS (comparator lab); B DEC-010 selecciona RTC; C PARTIAL (modelo y unit tests); D–L NOT RUN en esta secuencia. Foundation/IPC y resultados de laboratorio históricos conservados. Fase 1 NOT STARTED; sólo será READY TO START después de auditoría PASS e integración requerida.


**2026-10-06, America/Chihuahua. Investigación documental completada; ejecución NOT RUN.** La [decisión](PHASE0_SECURE_MOTION_TRANSPORT_DECISION.md) está Accepted sólo como base de investigación. Ni este plan, ni biblioteca, ni transporte están aceptados para producción. No se instaló, compiló, publicó ni configuró infraestructura. El PO deberá resolver la concesión de preparación online y autorizar los pasos que impliquen publicación/certificados antes de ejecutarlos.

## 1. Evidencia y límites

Distinguir DOCUMENTADO (fuente primaria), PROPUESTO (diseño) y NOT RUN/BLOCKED (prueba pendiente). Fuentes consultadas el 6 de octubre; main de un proyecto y una API disponible no prueban una versión distribuida interoperable. Las mediciones IPC anteriores no son latencia móvil ni medidas de estos Spikes. No hay ranking de rendimiento sin ejecutar ambos bajo el mismo baseline.

**Arquitectura recomendada para probar:** frontend HTTPS público estático preparado → adaptador cifrado LAN en Java 21 → IPC 127.0.0.1 → Unity. Sólo Java backend; cámara PC local → Unity. Un peer navegador PC puede ser referencia de interoperabilidad, nunca backend definitivo ni dependencia obligatoria del jugador.

### Versiones mínimas: no confundir API con producto compatible

| Candidato | Documentación de API | Mínimo de producto / pinning / sensores |
|---|---|---|
| WT | Chrome/Edge 97, Firefox 114 y Safari 26.4 en tabla oficial de Chrome | NOT RUN. Esos números no garantizan serverCertificateHashes, permisos LAN ni interop con el servidor elegido. No afirmar un mínimo específico de pin Chrome sin prueba de esa release. |
| RTC | WebKit documenta WebRTC desde Safari 11/iOS 11; APIs actuales de DataChannel normadas | Antecedente histórico, no compromiso de soportar sistemas obsoletos. Versión Android/Chrome e iPhone/Safari de referencia pendientes. |
| Sensores | W3C DeviceMotion/Orientation, permisos y unidades | Campos, frecuencia, precisión y disponibilidad sólo se confirman con teléfono real. Orientación absoluta puede faltar. |

Fuentes: [Chrome WebTransport](https://developer.chrome.com/docs/capabilities/web-apis/webtransport), [Safari 26.4](https://webkit.org/blog/17862/webkit-features-for-safari-26-4/), [WebKit WebRTC](https://webkit.org/blog/7763/a-closer-look-into-webrtc/), [W3C sensores](https://www.w3.org/TR/orientation-event/).

Riesgo concreto: [WebKit 312697](https://bugs.webkit.org/show_bug.cgi?id=312697) reporta bloqueo de stream bidireccional en Safari 26.4 **macOS**, aun con datagramas funcionales, y remite a frameworks del sistema. RESOLVED INVALID no demuestra corrección. No extrapolar ese reporte a todos los iPhone; probar control fiable y datagramas por separado en cada release física. No prometer fallback HTTP/2 interoperable.

## 2. Dependencias Java y licencias preliminares

Inspección de README, licencias, código/POM y metadatos públicos, sin descargar ni ejecutar paquetes. Antes de un Spike ejecutable: fijar artifact/SHA, hashes de binarios, árbol transitivo y LICENSE/NOTICE correspondientes; comprobar que ejemplos/main no requieren funcionalidades ausentes en la release. Esta revisión no equivale a autorización de integración ni dictamen legal de distribución.

| Biblioteca | Alcance y licencia declarada | Versión/actividad observada | Portabilidad y riesgo |
|---|---|---|---|
| [webrtc-java](https://github.com/devopvoid/webrtc-java) | Wrapper JNI de Google WebRTC; Apache-2.0 para wrapper. Auditar notices/licencias nativas y transitivas. | Maven 0.19.0, metadata actualizado 2026-09-27; main `4d205e91bb7f65d8bc4e70f7356f72d06a06c7bb`, commit 2026-10-05. | [Classifiers](https://jrtc.dev/guide/get-started) Windows/Linux/macOS y varias arquitecturas. Java 21, carga JNI, GLIBC, callbacks y cleanup NOT RUN. Preferir DataChannel-only, no módulos de captura/FFmpeg innecesarios. |
| [Flupke](https://github.com/ptrd/flupke) + [Kwik](https://github.com/ptrd/kwik) | HTTP/3/QUIC Java puro; WT experimental. README y cabeceras declaran LGPL, cabeceras LGPL-3.0-or-later; confirmar artifact y avisos antes de distribución. La detección GPL de GitHub no reemplaza esa revisión. | Maven Flupke 0.9.4 / Kwik 0.11. Main Flupke `545cc771b8af751e71ace6a3c2df873dae84d662` (2026-03-11), Kwik `505227c0085e14c7d9488d86f046e290acae79f5` (2026-10-06). | Flupke declara Java 11+. HTTP/3 puro no basta: WT main anuncia draft-13 experimental. Release WT y dependencia Kwik compatibles por confirmar; no combinar automáticamente últimas versiones. Windows/Linux/macOS sobre Java son candidatos, no PASS. Kwik no declara migración de conexión de servidor soportada. |
| [webtransport4j](https://github.com/webtransport4j/webtransport4j) | Apache-2.0; Netty HTTP/3, transitivos QUIC/crypto y módulos opcionales por revisar. | POM 0.1.0-SNAPSHOT; main `d959d35e467892764d98f1ded5150ddf37942c08` (2026-10-06). | Alternativa temprana; matriz CI no equivale a nuestra validación. Verificar classifiers nativos y Java 21. No añadir Spring/Boot sólo por el ejemplo. |
| [netty-webtransport](https://github.com/suboptimal-solutions/netty-webtransport) | Apache-2.0, servidor Java/Netty. | Main `b63ebb06b0ab33af73c9bf1f96bedd9e7cb73243` (2026-05-04). | Alternativa exploratoria; release, mantenimiento y binarios Win/Linux/macOS pendientes. Netty HTTP/3 solo no constituye implementación WT completa. |

Metadatos reproducibles: [RTC Maven](https://repo.maven.apache.org/maven2/dev/onvoid/webrtc/webrtc-java/maven-metadata.xml), [Flupke Maven](https://repo.maven.apache.org/maven2/tech/kwik/flupke/maven-metadata.xml), [Kwik Maven](https://repo.maven.apache.org/maven2/tech/kwik/kwik/maven-metadata.xml). Licencias: [RTC](https://raw.githubusercontent.com/devopvoid/webrtc-java/main/LICENSE), [Flupke LGPL](https://raw.githubusercontent.com/ptrd/flupke/master/LICENSE-LESSER.txt), [cabecera Kwik](https://raw.githubusercontent.com/ptrd/kwik/master/core/src/main/java/tech/kwik/core/QuicConnection.java), [WT4J POM](https://raw.githubusercontent.com/webtransport4j/webtransport4j/main/pom.xml).

**Candidatos de Spike:** webrtc-java para RTC; Flupke/Kwik para WT sujeto a confirmar artifact interoperable y revisión LGPL. Las alternativas Netty sólo se investigan si existe un bloqueo concreto. No construir un stack QUIC/ICE propio, no adoptar Rust/Go/Node como segundo backend. Actividad reciente no garantiza continuidad ni seguridad; mantenimiento de dependencias nativas y drafts es coste real.

## 3. Matriz obligatoria

| Criterio | WebRTC DataChannel | WebTransport |
|---|---|---|
| Android Chrome | API ampliamente documentada; DataChannel-only/ICE privado físicos pendientes | API base documentada; pin, LNA y QUIC físicos pendientes |
| iPhone Safari | Soporte histórico; candidatos, permisos y señalización físicos pendientes | Desde Safari 26.4 API; pin y streams fiables físicos pendientes |
| Navegadores antiguos | Mayor cobertura potencial, sin soporte de producto comprometido | Menor cobertura; sin WT compatible no hay motion transport completo por esta ruta |
| Primera conexión | Cargar PWA, permisos, offer/answer autenticados y ICE/DTLS | Cargar PWA, permisos, endpoint/fingerprint, pin y admisión |
| QR único | No demostrado sin canal bidireccional local autenticado adicional | Hipótesis razonable para metadata pública y pin; no autentica cliente por sí sola |
| Señalización | Obligatoria; SDP/ICE sensible, lifecycle y tamaño propios | No SDP; endpoint, identidad y admisión siguen necesarios |
| Internet inicial | Shell seguro/precache, no STUN/TURN públicos en baseline | Shell seguro/precache; certificado efímero local no exige emisión CA pública |
| Juego sin WAN | Propuesto con cache y candidatos host alcanzables | Propuesto con cache, pin compatible y UDP alcanzable |
| Seguridad | DTLS + fingerprints autenticados en señalización; cliente/plaza admitidos | TLS/QUIC + pin fuera de banda; cliente/plaza admitidos por separado |
| Latencia | NO MEDIDA; fiabilidad/configuración SCTP pueden afectar frescura | NO MEDIDA; streams y datagramas tienen políticas distintas |
| Frecuencia mensajes | No equivale a Hz del sensor; buffers y edad limitados | No equivale a Hz del sensor; buffers/datagramas limitados |
| Cuatro jugadores | Cuatro peers y sus recursos aislados; capacidad física NOT RUN | Cuatro sesiones aisladas; streams/recursos acotados, NOT RUN |
| Dependencias Java | JNI WebRTC y binarios por plataforma | Flupke/Kwik experimental Java puro o Netty/QUIC con transitivos |
| Integración Unity | Adaptador Java → IPC existente; sin autoridad adicional | Igual; no reemplaza el IPC ni HTTPS8443 actual por magia |
| Windows/Linux | Classifiers disponibles, lifecycle real por demostrar | Java puro favorece distribución; UDP/interop real por demostrar |
| Hotspot | Host candidates/mDNS/UDP dependen de ruta y driver | IP directa/QUIC depende de ruta, driver y permisos |
| Flutter futuro | Adaptador RTC nativo/Dart y misma semántica por evaluar | Adaptador QUIC/WT Dart/nativo con pin por evaluar; API web no basta |
| Mantenimiento | Librería nativa, parches y señalización local | Drafts/interop, certificación efímera y evolución de bibliotecas |

No puntuar superioridad antes de gates. Preferencia de ensayo WT por simplicidad potencial, no selección final. Si ambos pasan, elegir menor complejidad comprobada y mejor UX para Android/iPhone y Java; si ninguno pasa, registrar bloqueo/concesión requerida sin convertir touch en sustituto full-motion.

## 4. Contexto seguro, offline y red

GitHub Pages es candidato de distribución estática gratuita, no backend público. Sólo bundle compilado destinado al jugador: no repo privado, mapas fuente internos, secretos, APIs administrativas o configuración sensible. Verificar condiciones de plan y autorización de publicar artifacts en repositorio separado; no habilitar Pages ahora. [HTTPS Pages](https://docs.github.com/en/pages/getting-started-with-github-pages/securing-your-github-pages-site-with-https).

Propuesta de bundle: shell, JS/CSS, chunks, fuentes/iconos y recursos obligatorios, manifest de integridad/buildId y protocolo compatible. Verificación mediante prueba offline de todas las rutas, no sólo SW registrado. Cache atómica, sin actualización a mitad de sesión; versión no compatible bloquea admisión antes de enviar input. SW controla futuras navegaciones del mismo origen, no convierte HTTP LAN en seguro. Ausencia/incompletitud de cache necesita preparación online; la PC debe comunicarlo cuando el shell ni abre.

Probar primera visita online; reabrir sin WAN; cerrar Safari/Chrome; reiniciar teléfono; SW actualizado/bundle incompatible; cache eliminada; presión de almacenamiento. Una visita no garantiza persistencia. [Service Workers](https://github.com/w3c/ServiceWorker/blob/main/explainer.md), [WebKit storage](https://webkit.org/blog/14403/updates-to-storage-policy/).

**Concesión mínima pendiente:** preparación inicial online para un teléfono nuevo. Sin WAN, app, CA ni origen local confiable no existe flujo portable de navegador stock que descargue por primera vez esta PWA segura; QR/WT/RTC no resuelven el bootstrap. No sustituir silenciosamente el antiguo gate físico por otro.

Router existente sólo necesita DHCP/ruta y comunicación PC↔teléfonos; no DNS administrado. Redes aisladas o UDP bloqueado quedan BLOCKED. VPN, DNS privado/DoH, datos móviles y selección automática de red se registran: no exigir desactivarlos como requisito de producto. Comparar prueba controlada sin WAN con configuración stock; confirmar que no hubo señalización/tráfico público. No eludir permisos con flags.

Hotspot Windows/Linux necesita soporte AP, interfaz/driver, disponibilidad sin upstream y autorización futura para activarlo; macOS compartiendo Ethernet por Wi-Fi es caso diferente, no promesa de AP simultáneo universal. Fuentes: [Windows hotspot](https://support.microsoft.com/en-us/windows/experience/connectivity-networking/use-your-windows-device-as-a-mobile-hotspot), [NetworkManager](https://networkmanager.pages.freedesktop.org/NetworkManager/NetworkManager/nmcli.html), [macOS sharing](https://support.apple.com/en-sa/guide/mac-help/mchlp1540/mac). Ninguna configuración realizada.

Chrome documenta Local Network Access y cambios de permisos; el borrador no garantiza una ruta uniforme para Safari/WT/RTC. Registrar cada prompt, origen y release, no trasladar excepciones fetch a WS/WT. [Chrome LNA](https://developer.chrome.com/blog/local-network-access), [WICG](https://wicg.github.io/local-network-access/).

## 5. Seguridad de emparejamiento

### WT propuesto

Certificado por lanzamiento ECDSA P-256; clave privada local, nunca QR/log/argumentos. SHA-256 completo del certificado entregado físicamente junto a IP/puerto, instanceId, versión, sesión y caducidad. Conexión dedicada sin pooling; vigencia ≤14 días y reloj válido; rechazo de hash incorrecto, certificado expirado/futuro, algoritmo inválido y endpoint/instancia distintos. Renovar antes de caducidad mediante preparación local, sin reemplazar identidad de una sesión activa silenciosamente. Generación sólo tras autorización del Spike; no emisión pública ni certificados del sistema. [W3C WT](https://www.w3.org/TR/webtransport/).

Pin identifica al servidor, no autoriza jugadores. QR público no contiene bearer/token IPC ni secretos. En canal ya autenticado: challenge/nonce, petición de plaza, confirmación del operador, credencial de conexión efímera entregada sólo dentro del canal, límite de cuatro admitidos, revocación al cerrar. Origin allowlist es defensa auxiliar, no autenticación. Restringir recursos preadmisión; no aceptar input antes de admisión. No implementar autenticación criptográfica casera ni confiar en nombres/IDs enviados. Deshabilitar 0-RTT para operaciones de admisión que no sean seguras frente a replay.

### RTC propuesto

Peer Java DataChannel-only, iceServers vacío, gathering con deadline y candidatos de interfaces autorizadas. DTLS fingerprint de ambos peers ligado a oferta/respuesta y sesión mediante intercambio físico autenticado. SDP contiene credenciales ICE: **no publicarlo en QR universal, hosting, logs ni capturas**. QR de signaling privado/efímero entre operador y jugador no equivale al enlace público de incorporación.

Dos QR (PC oferta, teléfono respuesta, webcam PC lectura) son referencia de laboratorio, no UX aprobada. Medir tamaño efectivo, scans fallidos, pasos/tiempo y necesidad de segmentación antes de recomendarlo. Si no cabe un intercambio acotado legible, gate UX falla. Un único QR exige resolver el retorno autenticado; HTTP LAN sin autenticar no lo resuelve. No añadir servidor cloud de signaling ni WT como segunda pila sólo para salvar RTC sin decisión de complejidad. [RFC 8827](https://www.rfc-editor.org/rfc/rfc8827.html), [QR capacity](https://www.qrcode.com/en/about/version.html).

Ambos: identidad del QR depende de que el operador muestre la pantalla correcta; no protege contra observador físico autorizado a leer todo. Aislar playerId/connectionEpoch, no aceptar sesiones antiguas, duplicados o replay; tamaño/frecuencia/buffers finitos y logs sanitizados. No trust-all, no CA móvil, no cámara móvil para alterar ICE. Socket/interfaz realmente restringidos: filtrar candidates no prueba que una biblioteca no escuche en interfaces adicionales.

## 6. Propuestas mínimas aisladas

No se crean estos archivos ni ramas ahora. Ubicación futura: `tools/spikes/secure-motion/`, fixtures compartidos específicos del Spike, frontend experimental separado y servidor Java standalone que no modifica React/Java/Unity productivos. Sin Spring extra si no es necesario. Recursos del Spike sólo se cierran por su supervisor; no matar procesos ajenos.

| Spike | Mínimo | Gate específico |
|---|---|---|
| WT | Java 21 Flupke/Kwik compatible, certificado efímero aprobado, PWA secure shell preparado, QR de metadata, control stream fiable + datagramas echo numerados | Pin positivo/negativo en Chrome y Safari, control bidireccional y datagrama reales, un QR y admisión segura, cero dependencia WAN |
| RTC | Java 21 webrtc-java, DataChannel control ordenado fiable + muestras con retransmisión limitada; signaling local privado autenticado | ICE host sin STUN/TURN público, fingerprints verificados, sin permiso cámara móvil, oferta/respuesta y UX medidas |
| Sensores | Logger web mínimo sobre origen HTTPS autorizado; no gameplay/Flutter/pose | Campos físicos, permisos, cadencia y lifecycle registrados en ambos teléfonos, sin inventar raw sensors |

Preparación propuesta: validar artefactos/licencias → fixture de contrato/negativos → smoke Java/browser automatizado → Android/iPhone físicos → comparación en misma red → recomendación al PO. No integrar ganador automáticamente. Si dependencia exige parche amplio QUIC/WebRTC, detener ese candidato y documentar bloqueo, no ampliar scope.

Límites de laboratorio propuestos, no Gorilla Protocol definitivo: máximo 4 conexiones admitidas, control ≤4096 B, muestra ≤1024 B sin fragmentación propia (además respetar máximo datagrama negociado), cola control ≤16 mensajes/64 KiB por cliente; muestra latest-only, máximo una pendiente. Un in-flight writer por canal; callback no bloqueante ni thread por mensaje. Pre-admisión máximo 4 conexiones durante ventana de operador. Deadlines absolutos propuestos: gathering 10 s, transporte 10 s, admisión 30 s, echo 2 s, cierre 5 s. Timeouts de IPC existentes no se modifican. Cualquier ajuste requiere resultado anotado y misma configuración en comparación.

Recuperación sólo manual con conexión/epoch nueva; revocar credenciales anteriores, limpiar antes de otro intento, máximo 3 intentos por sesión de prueba. Sin reconnect infinito, migración transparente prometida ni restart automático. Cambiar IP/red puede exigir nuevo QR. Java hijo de Unity se valida en una etapa futura explícita después del Spike standalone; no afirmar integración de transportes por existir el supervisor IPC.

## 7. Hardware y registro de sensores

PC Pop!_OS/COSMIC disponible; foundation/IPC/QA Android anteriores conservados. Webcam integrada detectada en inspección previa no demuestra captura/pose/rate. Android e iPhone mencionados por el usuario, **modelos, versiones y sensores no confirmados**. No hay baseline Windows/macOS ni cuatro teléfonos confirmados. No cambiar red para descubrirlos. Emulador apoya UI/fixtures, no sensores físicos, Wi-Fi real ni pin Safari.

Por teléfono: modelo/OS/browser exactos, buildId, estado cache, red autorizada, modo página/PWA, permisos, visibilidad, screen orientation, intervalos de eventos, campos null/finite, aceleración lineal/incluyendo gravedad, alpha/beta/gamma y rotationRate. Omitir MAC/SSID/IDs personales de evidencia pública.

Guion reproducible propuesto, tres repeticiones por teléfono: 30 s neutral; 60 s movimiento lineal sin giro intencional; 60 s giro sin traslación intencional; 30 s combinado; 60 s touch/gestos y cambio de orientación; 30 s background; 30 s bloqueado; retorno y 30 s activo. Sujetar el teléfono, nunca lanzarlo físicamente. Separar preparación/permisos de captura; no emitir datos ausentes como cero.

Registrar timestamp del evento si existe y dominio, entrada/salida de callback monotónicas, sequence asignada por logger, interval/campos, enviado/recibido y estado de visibilidad. Calcular Hz efectivos, distribución de intervalos/jitter, máximos gaps, suspensión/recuperación y valores inválidos. 50 Hz es target experimental, no condición cumplida por una interfaz presente.

**Latencia de captura:** callback time menos event timestamp sólo si origen temporal y semántica compatibles; llamar atraso observable de entrega, no latencia hardware demostrada. Si no hay timestamp de adquisición/ground truth, captura real NOT MEASURABLE en este montaje. **Pérdida:** DeviceMotion no expone sequence hardware; gaps y muestras faltantes estimadas no son conteo real de eventos perdidos. Sequence del logger mide pérdida posterior de transporte, no del sensor. No fabricar confidence/precisión ni cuantificar giroscopio raw desde un evento sintetizado.

## 8. Pruebas, métricas y evidencia

| Nivel | Casos | Qué no demuestra |
|---|---|---|
| Unit/fixtures | Unidades/ejes, campos ausentes/NaN, timestamps, duplicate/epoch, límites, permisos simulados | Hardware, Wi-Fi, confianza de release Safari |
| Java real + cliente simulado | Handshake, control/echo/datagrama, identidad incorrecta, sesión cruzada, timeout, backpressure, cierre | Permisos y sensores web reales |
| Browser automatizado/emulador | Shell/cache, versiones, UI error, reload, fixtures, logs y cierre | Sensor físico, cuatro jugadores físicos, iPhone |
| Android Chrome / iPhone Safari físicos | Secure context, sensores/permissions, pin/ICE, offline/reapertura/reinicio, background/lock, QR, routing | Compatibilidad universal ni versiones nunca ensayadas |
| Host por plataforma | Java 21, native load si aplica, sockets, EOF/cleanup/port libre | Win/macOS si sólo se prueba Linux |

Baseline documentado antes de medir: modelo/OS/browser, Java/artifact, host/CPU/power mode, red/interfaz, WAN/celular, 1/4 clientes, cargas activas y tamaño mensaje. Tres corridas independientes de echo sintético 60 s a 10/25/50 Hz por candidato y número de clientes; no combinar diferencias. Medir sensores a su cadencia real por separado, sin interpolar para presumir adquisición 50 Hz. Si faltan cuatro teléfonos, simulación de cuatro conexiones se etiqueta capacidad sintética; físico cuatro BLOCKED.

Por corrida: startup del host, tiempo de QR/gathering/handshake/admisión, enviados/recibidos/válidos, pérdida por sequence de transporte, duplicados/reordenados/stale, errores/timeouts, RTT mínimo/P50/P95/máximo, duración/cadencia efectiva/ticks tardíos, bytes/cola máxima/drop intencional, CPU/RSS observacionales y shutdown. RTT usa mismo reloj emisor; one-way sólo con mapping e incertidumbre, nunca dividir RTT por dos como medida real. No extrapolar al presupuesto gameplay/Input Fusion.

Propuesta de gate de selección, pendiente de aceptar para ejecución: nominal control entregado y válido 100%, errores/timeouts nominales 0; RTT P50 ≤10 ms, P95 ≤20 ms, máximo ≤100 ms en baseline LAN definido; pérdida de muestras recibidas respecto de enviadas ≤1% sin drops de presión, publicados por corrida. Son umbrales de Spike, no modificación de DEC-005 ni garantía gameplay. Sensor no necesita 50 Hz para informar su capacidad; producto full-motion necesita acordar mínimos por minijuego posteriormente. No excluir outliers. Si falla: publicar datos e investigar, no mover umbral para aprobar. Finalista: continuidad ≥600 s con heartbeat real, suspensión/reanudación y cierre.

Presión determinista corta: exceder cola/tamaño/frecuencia configurados, control no perdido silenciosamente, muestras descartadas contabilizadas, ningún crecimiento no acotado. No fuzzing ilimitado ni benchmark exhaustivo. Seguridad negativa: hash/fingerprint cambiado, replay, sesión/epoch ajeno, conexión no admitida, certificado inválido y señalización alterada deben rechazarse sin afectar otra plaza. No usar captura de red con secretos en repo.

Evidencia futura: manifiesto de versiones/hashes y configuración sanitizada; resultados JSON/CSV por corrida; pasos/comando local reproducibles, timestamps y razón de PASS/FAIL/SKIP/BLOCKED; capturas sólo de UI sin QR/SDP/token; logs sin claves, bearer, sensores personales sin consentimiento ni vídeo corporal. Limitar duración/volumen y borrar recursos temporales propios. Todos los procesos iniciados por harness deben cerrar y dejar cero residuales propios. No declarar ausencia global de fugas por RSS estable.

## 9. Gates para avanzar y roadmap

| Bloque futuro | Alcance / dependencias | Aceptación y evidencia / riesgo / condición siguiente |
|---|---|---|
| Contexto seguro (3A-S propuesto) | Autorización hosting y concesión online; SW/versionado | Android/iPhone offline reapertura y cache ausente explícito; evicción sigue riesgo. Sin gate no sensores seguros. |
| Sensores físicos | Shell verificado y teléfonos | Campos/cadencia/permisos/lock y recuperación medidos; datos ausentes claros. Autorizar transporte después de inventario. |
| Transporte comparado | Dependencia/licencia, certificado efímero autorizado, red autorizada | Ambos Spikes con métricas/negativos, Java 21 y platform gates; informe PO selecciona transporte. No merge automático. |
| QR/admisión (3B propuesto) | Transporte seleccionado | Pasos/tiempos físicos, identidad/plazas y replay; un QR objetivo, concesión multiscans sólo PO. |
| Sesión/protocolo común (4A revisado) | Identidades y capacidades verificadas | Fixtures JS/Java/C#/Dart conceptualmente equivalentes, límites/versiones y cierre. Sin backend duplicado. |
| Touch/motion PWA (4B) | Protocolo y sensores físicos | Datos/gestos/feedback reales, permisos y accesibilidad; touch no sustituto full-motion. |
| Flutter Android (4C) | Adaptador seleccionado | Misma semántica/sesión, paridad fixtures y sensores; APIs Dart/nativas/licencias por revisar. Opcional. |
| Webcam PC | Hardware/modelo/licencia/integración offline | Pose/calidad/oclusiones y timestamps reales; no cámara teléfono ni cloud. |
| Input Fusion | Fuentes/relojes/identidad medidos, perfiles calibrados | Temporalidad/stale/ausencia, asociación jugador/control y patrones de intención DEC-009: lento válido, pico accidental rechazado, acción única/recovery. Detector final sólo en bloque autorizado; Unity única autoridad. |
| Calibración/perfiles | Señales, relojes y calidad calibrables; DEC-008 | Contrato de práctica/rangos cómodos y snapshots Unity, cuatro clases de perfil y ajustes explícitos entre rondas; desafíos finales y equidad humana en bloque de gameplay autorizado. Sin doble gain ni fuerza máxima exigida. |
| Minijuegos deportivos | Aprobación de fase y fusión | Física/gestos/boliche reproducibles y accesibles; no iniciado por este roadmap. |
| E2E/optimización | Cadena completa implementada y autorizada | Captura→acción y frame time por corrida, hardware/plataforma oficiales; presupuestos medidos, no IPC extrapolado. |

Aceptación de un candidato exige todos sus gates físicos obligatorios y limpieza; BLOCKED no se transforma en PASS. Si WT no pasa Safari/pin/control, no esconderlo con datagramas Android. Si RTC requiere UX de QR inaceptable o permiso cámara móvil, no considerarlo ganador sólo por compatibilidad histórica. Si ninguna ruta satisface los requisitos simultáneos, volver al PO con concesión mínima explícita.

Decisiones pendientes: preparación inicial online; hosting sólo artifacts y su publicación; permisos/licencias de dependencia concreta; ejecución de certificados efímeros locales; modelos/red/ventana de pruebas; presupuesto y UX de Spike; transporte definitivo sólo después de datos. Sin compras, cambios DNS/router/firewall, certificados públicos, instalaciones globales, gameplay, Flutter, pose o Input Fusion ahora. #94 OPEN/DRAFT; 3A IN PROGRESS; Fase 0 IN PROGRESS; Fase 1 NOT STARTED.


## 10. Próximo Spike mínimo dentro de 3A, sin reiniciar Fase 0

El plan completo anterior es una escalera de validación; no ejecutar todo como primer experimento. El próximo alcance más pequeño debe resolver la incertidumbre crítica **Java 21 ↔ Chrome Android/iPhone Safari con pin WT y control fiable**, sin gameplay, sensores productivos, Unity nuevo, cuatro jugadores ni benchmarking sostenido. RTC tiene experimento de contraste separado, no se instala otra pila en #94.

**Paso previo ejecutable tras autorización específica:** resolver artifact Flupke/Kwik compatible desde POM/release, licencia/transitivos y soporte real del draft/browser objetivo. No basta la versión Maven citada para afirmar que contiene WT main. Si no se puede fijar una combinación reproducible, registrar BLOCKED antes de prototipo; investigar una alternativa acotada, no construir QUIC propio. Para RTC fijar webrtc-java/classifier y validar carga JNI Java 21. No incorporar dependencias al runtime principal.

**Experimento mínimo propuesto:** un host Java standalone Linux, una página experimental HTTPS autorizada y un cliente a la vez. WT: certificado local efímero autorizado, pin, metadata por un QR, admisión explícita de operador, 20 echoes de control + 20 datagramas secuenciados y cierre. Repetir por Android Chrome e iPhone Safari de versiones documentadas. No emitir certificados ni publicar página para ejecutar esta propuesta sin aprobación. Sin contexto seguro disponible, pruebas físicas BLOCKED. Preparación online y posterior reapertura sin WAN se prueban por separado; nuevo teléfono offline permanece bloqueado hasta concesión PO.

Contraste RTC mínimo: peer Java DataChannel-only, iceServers vacío, control/echo y muestras, fingerprints autenticados, gathering/SDP local acotados. QR bidireccional puede servir sólo como montaje privado de laboratorio; medir pasos/tamaño/reintentos y no aprobar esa UX como producto automáticamente. No permiso de cámara móvil, signaling cloud ni HTTP LAN con secretos.

| Gate mínimo | PASS objetivo | FAIL / BLOCKED |
|---|---|---|
| Dependencia reproducible | Artifact/SHA/hash/licencias y Java 21 registrados; componente aislado | Release/draft no compatible o licencia no resuelta: BLOCKED |
| Contexto seguro | HTTPS top-level verificado, preparación y bundle completos; no flags/CA móvil | Hosting/origen no autorizado o inexistente: BLOCKED; `isSecureContext` solo no acredita sensor |
| Identidad WT | Pin correcto conecta; incorrecto/expirado/instancia distinta rechazados | Trust-all, sólo caso positivo o resultado por API detectada: FAIL/NOT RUN |
| Transporte en ambos teléfonos | Stream fiable real y datagramas recibidos/validación por corrida; 20/20 control y conteo datagramas publicado | Android-only no acredita iPhone; pérdidas datagrama se publican y se investigan, no se borran |
| Sesión/admisión | Cliente no admitido no envía input aceptado; sesión/epoch ajena/replay rechazados | QR público bearer/IDs autodeclarados autorizando input: FAIL |
| Offline | Shell previamente preparado y transporte LAN operan sin WAN ni backend remoto | Nunca preparado/cache ausente: BLOCKED esperado, no FAIL oculto ni PASS de offline general |
| Cierre | Exit 0, socket liberado, cero procesos propios residuales | Residual/stream/timer vivo: FAIL |
| Evidencia | Versiones, config sanitizada, tiempos/counts por dispositivo y pasos QR; sin secretos | Emulador presentado como físico o métricas mezcladas: FAIL documental |

RTT de 20 muestras se reporta sólo como smoke; P95 descriptivo no prueba estabilidad ni gate sostenido. Startup/inicio de pairing separados, no percentil significativo con pocos lanzamientos. Los umbrales de la sección 8 son **propuestos** para etapas comparativas posteriores, no aceptación automática de este smoke. Sensores se miden en el logger físico separado cuando exista contexto seguro; no inferirlos del echo ni exigir 50 Hz sin hardware. Después del smoke, PO revisa evidencia y autoriza comparación/carga/sensores/interop Windows pertinentes; no integración automática.

Bloqueos concretos pendientes: modelos/OS/browser de Android+iPhone; red y ventana físicas autorizadas; permiso para preparación/publicación experimental y certificado efímero; concesión online; combinación dependency/draft verificable. Router/firmware/dominio propio de la estrategia anterior ya no son bloqueos universales del diseño portable: confirmar conectividad/ruta, no exigir recursos descartados. Sin prueba, hotspots y cuatro dispositivos permanecen NOT RUN.

Las capacidades Flutter, webcam, fusión y perfiles continúan conceptuales. El roadmap no autoriza minijuegos ni física antes del gate y autorización de Fase 1. Los labels 3A-S/3A-T sólo designan tareas del 3A actual, no nuevos incrementos ni una nueva fase. Todos los intentos requieren permiso de su alcance y terminan con revisión del PO.


**Dependencia futura DEC-008:** calibración inmersiva y normalización justa descritas en [Input Fusion §8](PHASE0_INPUT_FUSION_CONCEPT.md#8-calibración-inmersiva-y-justicia-competitiva--dec-008). Requieren captura/sincronización válidas, revisión de perfiles competitivos Unity y estudios físicos consentidos. No forman parte del Spike de transporte actual ni autorizan cámara, minijuegos, UI o algoritmo definitivo. No imponer 50 Hz ni potencia física absoluta como prueba de accesibilidad/equidad.


**Dependencia futura DEC-009:** [reconocimiento de intención](PHASE0_INPUT_FUSION_CONCEPT.md#9-reconocimiento-de-intención-y-prevención-de-activaciones-falsas--dec-009) con estados/candidatos Unity, asociación corporal, modalidad sensor-only y pruebas etiquetadas de activaciones falsas/duplicadas/prematuras. No afecta elección WT/RTC ni autoriza detector en el Spike actual; necesita evidencia física y presupuesto de demora propio, nunca RTT como precisión de gesto.


## 11. Ejecución autorizada limitada — captura local PWA (2026-10-07)

DEC-010 autoriza un paso aislado dentro del trabajo actual de 3A: [PWA/spikes/sensors](../PWA/spikes/sensors/README.md). No ejecutar los prototipos RTC/WT por esa autorización. Captura DeviceMotion/Orientation, permisos, validación, diagnóstico de cadencia y cleanup implementados con tests; ninguna integración de transporte o gameplay. No duplicar shell/SW ni reemplazar componentes validados. La página experimental permanece separada del build/flujo React productivo.

Automatizado: 8 tests del capturador y 5 regresión PWA PASS. APIs/permisos/reloj de prueba no son Android/iPhone físicos. Uso físico futuro requiere origen seguro autorizado; no se publicó hosting ni configuró HTTPS/CA/DNS/red. Nueva actividad no cambia emulador histórico 10 PASS / 2 BLOCKED / 1 SKIP. Captura humana/frecuencia/precisión NOT RUN.

Próximo paso recomendado: autorización del contexto HTTPS experimental/preparación y teléfonos de referencia, seguido del guion físico de sensores §7 y registro por dispositivo; manteniendo pendiente concesión online/hosting y transporte. En paralelo documental, validar artifact/draft/licencia de WT propuesto. No iniciar automáticamente pruebas físicas ni otro incremento. Cinco juegos cotejados y escenarios específicos futuros en [Input Fusion §10](PHASE0_INPUT_FUSION_CONCEPT.md#10-cinco-disciplinas-oficiales-y-spike-de-captura--corrección-2026-10-07); Banana Catch sólo post-MVP.


## Ejecución autorizada 3A-T — 2026-10-07

PO autorizó únicamente prototipo WT aislado después del plan inicial. [Implementación/instrucciones](../tools/spikes/webtransport/README.md) y [resultados reales](PHASE0_SECURE_MOTION_TRANSPORT_DECISION.md#15-spike-ejecutable-3a-t--2026-10-07): Chrome desktop/Java21 streams+datagramas y negativos PASS; endpoint loopback/IP LAN propia, no LAN física. netty-webtransport elegido para este laboratorio por API datagramas ausente en Session Flupke inspeccionada; snapshot/nativos no aceptados para producto por este resultado. Certificado efímero local permitido sólo para laboratorio, sin emisión pública/instalación/trust bypass.

Comparación RTC, físicos Android/Safari, multiusuario y carga offline pendientes. Concesión de preparación inicial online no aprobada; no despliegue Pages. Próximo ensayo físico requiere origen seguro permitido e infraestructura/teléfonos autorizados; no correr automáticamente. No modificar gates ni reabrir incrementos completados.

### Incremento aislado A/B y base C — 2026-10-07

Comparador RTC Chromium ↔ Java21 real: **PASS**, `webrtc-java 0.19.0`/JNI Linux fijados sólo en Spike. Corrida final `run04-reviewed.json`: 2400/2400 mensajes medidos, 0 pérdidas/timeouts/errores nominales; 1 peer, reconexión nueva y 4 peers activos. Padding32/1024, reliable ordered/unordered maxRetransmits0. Candidatos host UDP, sin iceServers/STUN/TURN. Negativos7 PASS: SDP inválido, oferta con ID duplicado aislada, payload inválido, quinto peer rechazado, desconexión aislada, credential incorrecta, oversized. Revisión detectó que rechazar una oferta con ID repetido podía cerrar el peer existente: corrección limitada al peer asignado por ese request y prueba de regresión real añadida; corridas anteriores conservadas.

Java startup observado264.52ms (un lanzamiento, no P95). RTT por escenario, sin combinar muestras: single N400, mayor P95 de sus subgrupos0.60ms/max1.50ms; recovery N400, mayor P950.90ms/max1.70ms; four N1600, mayor P951.00ms/max2.00ms. Subgrupos/tamaños/canales y métricas completas permanecen separados en JSON; esos máximos de percentiles no son un percentil combinado. No se comparan como benchmark equivalente con WT ni se extrapolan a teléfono/gameplay. Java exit0, peers residuales0, comandos pendientes0, navegador cerrado.

DEC-010 selecciona **WebRTC DataChannel** como dirección Software/Lab; no transporte productivo integrado aún. WT histórico permanece aislado. Sesión técnica Java: siete unit tests PASS para token one-use, cuatro players, expiración, liveness, desconexión/identity y reconnect con epoch. Regresión Java37 total:35 PASS/2 SKIP de variantes LAN,0 fallos/errores; no se cambian resultados históricos. Foundation wiring PASS; diff check PASS. No QR/endpoint de admisión implementado todavía; C PARTIAL, pipeline móvil/Unity, webcam/Fusion, multicliente productivo y auditoría final pendientes.

Evidencia: `docs/evidence/webrtc-comparator-2026-10-07/`; comando `tools/spikes/webrtc/run.mjs`; restricciones/dependencias `tools/spikes/webrtc/README.md`. #94 verificado OPEN/DRAFT, head0d940f77; sin commit/push/merge. Fase0 IN PROGRESS y Fase1 NOT STARTED. Físicos DEFERRED por decisión PO; ya no bloquean gates Software/Lab.

**Actualización del mismo incremento:** POC QR implementado con ZXing core3.5.3/Apache-2.0 (única nueva dependencia del servidor para QR), QR384×384 codificado y decodificado realmente en test; URL HTTPS con admission efímero en fragment, sin token IPC/clave/credencial permanente. Dominios `.invalid` son fixtures, no hosting desplegado ni concesión de infraestructura. Rechaza HTTP, userinfo, query/fragment previos; token consumido sólo una vez. Dos tests QR PASS. Credential de resume rota al reconectar y epoch anterior queda inválido; siete tests sesión PASS. Regresión final con interfaz LAN explícita: **Java39/39 PASS,0 SKIP,0 errores/fallos** (`technical-session-qr-java-final.json`). La corrida previa35PASS/2SKIP sigue conservada separadamente. C permanece PARTIAL: faltan endpoints/integración de admission/liveness con el transporte real/PWA; QR POC componente PASS. No pipeline móvil→Unity, webcam, asociación/Fusion ni auditoría final aún. No se declara Fase0 COMPLETED ni Fase1 READY.
