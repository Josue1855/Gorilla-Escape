> Enmienda 2026-10-05: el usuario aprobó Java + React (DEC-002). Las recomendaciones ASP.NET/.NET y frontend sin framework de esta investigación son antecedentes sustituidos. La base actual está en [TEAM_START.md](TEAM_START.md); métricas físicas continúan pendientes.

# Gorilla Escape — Investigación técnica y de mercado

**Fecha de consulta:** 2026-10-05.
**Alcance:** investigación y gobierno previo a Fase 0; ninguna dependencia instalada ni proyecto creado.
**Fuente principal:** [Especificación maestra v4](Gorilla_Escape_Gorilimpiadas_Especificacion_Maestra_v4.md).
**Organización autorizada:** todos los proyectos propios en este repositorio; ver [MONOREPO_PLAN.md](MONOREPO_PLAN.md).

## Decisión vigente y lectura del antecedente

La base publicada usa Unity 6000.3.23f1, Java 21/Spring Boot 4.1.1/Maven 3.9.11 y React 19.3.0/Vite 8.3.2 con Node 24. DEC-002 recoge autorización del Product Owner; [TEAM_START](TEAM_START.md) registra versiones y comandos y [MONOREPO_PLAN](MONOREPO_PLAN.md) las fronteras actuales. Java/React tienen build/tests de base aprobados en FND-001; Unity Foundation tiene validación Linux de desarrollo en UNITY-001; PC de presentación pendiente.

El resto de este archivo conserva la investigación anterior a DEC-002: ASP.NET/Kestrel, net10.0, System.Text.Json y frontend sin framework NO son recomendaciones vigentes para el servidor/control actuales. Las consideraciones de .NET dentro de Unity y de wrappers de cámara siguen siendo antecedentes a evaluar, no un motivo para añadir un servidor .NET. HTTPS, red local, hardware, modelos y licencias requieren pruebas de Spike. El bloque P0-R08/P0-R10 antiguo debe leerse con JVM y React/Java/Unity según DEC-002.

## Conclusión original — anterior a DEC-002

La arquitectura prevista sigue siendo una candidata razonable para el MVP. La investigación no demuestra que sea la más rápida en el hardware del equipo ni que las metas estén alcanzadas. Recomiendo empezar con una combinación pequeña, reproducible y compatible: Unity 6.3 LTS como candidato, ASP.NET Core sobre .NET 10 LTS, PWA sencilla, WebSocket y JSON, cámara local y adaptadores separados del gameplay. La selección final requiere Spike y build en la PC oficial.

Los riesgos que dominan la decisión son HTTPS y permisos en teléfonos, compatibilidad nativa de cámara, antigüedad de muestras, empaquetado offline y sincronización temporal. Buscar una biblioteca más rápida no resuelve por sí solo esos riesgos.

## Método y límites

Se hicieron búsquedas por componente, consultas directas a documentación de proveedores, repositorios, releases, licencias, workflows e issues de integración. Se consultó la API pública de GitHub para 12 repositorios: release estable publicada, fecha de actividad, licencia detectada y estado archivado. El resumen verificable queda debajo; no se usó el número de estrellas como evidencia de rendimiento.

Las fuentes técnicas empleadas son primarias: Microsoft, Unity, Google, W3C, MDN, fabricantes y mantenedores. Las afirmaciones de proveedores describen sus productos, no mediciones independientes. Los issues son señales para diseñar pruebas, no demostraciones de que todas las instalaciones fallan.

No es un censo de todo GitHub ni de todo el mercado. No se ejecutaron benchmarks, builds, pruebas de hardware, compras, instalaciones, análisis completo de vulnerabilidades ni auditoría de todas las dependencias transitivas. Las versiones se consultaron en esta fecha y deben revalidarse antes de fijarlas. No hay garantías de perfección, latencia ni compatibilidad universal.

Distinción de estados: **hecho** = respaldado por fuente; **recomendación** = juicio para este proyecto; **pendiente** = necesita evidencia local; **alternativa** = no autoriza un cambio de arquitectura.

## 1. Candidatos por área

| Área | Candidato inicial | Motivo para evaluarlo | Pendiente antes de adoptarlo |
|---|---|---|---|
| Motor | Unity 6.3 LTS, parche estable compatible | Horizonte de soporte superior a 6.0 en la fecha de entrega | Plugin cámara, build PC y notas del parche |
| Render | URP Forward sencillo | Encaja con low-poly y pocas luces | Presupuesto CPU/GPU en PC oficial |
| Servidor | .NET 10 LTS + ASP.NET Core/Kestrel | Soporte vigente, APIs nativas suficientes | OS objetivo, empaquetado y arranque local |
| Móvil | HTML/CSS/JavaScript modular sin framework inicial | Control pequeño con pocos estados | Safari/Chrome reales, permisos, offline |
| Transporte | WebSocket nativo | Coincide con protocolo congelado | Heartbeat, recuperación, límites y buffers |
| JSON servidor | System.Text.Json | Ya pertenece al stack .NET | Fixtures interoperables y perfil de asignaciones |
| JSON Unity | Adaptador compatible con runtime elegido | No asumir que APIs .NET 10 existen en Unity | DTOs, enums, campos, AOT si se usa |
| Pose | MediaPipeUnityPlugin como candidato de integración | Mantiene MediaPipe local en Unity | CPU Windows, recursos, estabilidad y versión |
| OpenCV | Usar primero lo que necesite el adaptador elegido | Evita cargar dos distribuciones nativas por costumbre | Qué funciones faltan y qué wrapper las ofrece |
| QR | QRCoder evaluado en servidor; imagen PNG | Reduce acoplamiento a un wrapper Unity antiguo | Lectura real desde TV y URL HTTPS válida |
| Async Unity | Awaitable y CancellationToken | Funciones del motor disponibles | Ciclo de vida y thread principal |
| DI | Composición explícita; DI nativa servidor | Menos dependencias para un equipo pequeño | Sólo considerar VContainer si aporta valor real |
| Observabilidad | Logs estructurados y métricas locales | Requisito de diagnóstico y aceptación | Persistencia con buffer, límites y pérdidas |
| CI | GitHub Actions; GameCI sólo al existir Unity | Soporta ruta del proyecto y pruebas del motor | Licencia, runner, binarios nativos y build objetivo |

Las filas son recomendaciones, no decisiones aceptadas de versiones o paquetes. Se conserva la arquitectura v4.

## 2. Versiones y compatibilidad que cambian la elección

### Unity y .NET

Unity publica 6.3 LTS con soporte hasta diciembre de 2027; 6.0 LTS llega a octubre de 2026 bajo el soporte estándar indicado. Unity también considera sus Update releases aptas para producción. **Recomendación:** por la cercanía de la entrega, evaluar 6.3 LTS y fijar el parche que pase la integración; no perseguir cada Update. [Política Unity](https://unity.com/releases/unity-6/support).

.NET 10 es LTS con soporte hasta el 14 de noviembre de 2028. .NET 8 y 9 terminan soporte el 10 de noviembre de 2026, dentro de la ventana objetivo del MVP. **Recomendación:** candidato .NET 10 para el servidor nuevo, no .NET 8 por inercia. No fijar un parche sólo desde este informe. [Política .NET](https://dotnet.microsoft.com/en-us/platform/support/policy).

Unity 6.3 ofrece perfiles .NET Standard 2.1 y .NET Framework; no admite ensamblados destinados a .NET Core como plugins administrados en esos perfiles. **Consecuencia:** compartir repositorio no permite referenciar una DLL net10.0 desde Unity. Los contratos comunes deben usar un subconjunto compatible y no depender de UnityEngine ni ASP.NET. [Compatibilidad Unity 6.3](https://docs.unity3d.com/6000.3/Documentation/Manual/dotnet-profile-support.html).

**Pendiente arquitectónico:** definir cómo se inicia y comunica el servidor local con Unity. Un ejecutable ASP.NET coempaquetado es una opción a estudiar; supone varios procesos del mismo producto y debe aclararse frente al Modular Monolith. No proponer incrustar Kestrel net10.0 como DLL Unity sin evidencia, ni convertirlo en microservicios o cloud.

### El riesgo de elegir latest

El plugin homuler v0.16.3 declara MediaPipe 0.10.22 y Unity >=2022.3. La release upstream MediaPipe consultada es v1.0.0. No se puede sustituir el binario interno por la última release y asumir compatibilidad de ABI, API, grafo o modelos. [README fijado a v0.16.3](https://github.com/homuler/MediaPipeUnityPlugin/blob/v0.16.3/README.md), [releases upstream](https://github.com/google-ai-edge/mediapipe/releases).

OpenCvSharp5 exige .NET 8+ y elimina .NET Standard; su guía indica mantener OpenCvSharp4 para Unity. Para servidor .NET moderno puede ser candidato 5.x, pero eso no lo vuelve válido dentro de Unity. [Migración del mantenedor](https://github.com/shimat/opencvsharp/blob/main/docs/migration-4-to-5.md).

## 3. PWA local: puerta principal de viabilidad

DeviceMotion requestPermission requiere contexto seguro y activación del usuario; hay que detectar su disponibilidad y solicitar permiso desde una interacción. La ausencia del método no significa ausencia de sensores: según navegador puede no requerir esa llamada. Debe verificarse también la llegada de datos útiles. [MDN permisos](https://developer.mozilla.org/en-US/docs/Web/API/DeviceMotionEvent/requestPermission_static).

Una IP LAN por HTTP no hereda la excepción de localhost; en el teléfono localhost apunta al propio teléfono. Los service workers también necesitan contexto seguro. **Recomendación:** evaluar HTTPS/WSS desde el mismo origen local y verificar isSecureContext antes de habilitar gameplay. [Contextos seguros](https://developer.mozilla.org/en-US/docs/Web/Security/Defenses/Secure_Contexts).

El certificado de desarrollo de .NET se confía en la máquina local y sus SANs no cubren cualquier IP privada. mkcert puede servir para pruebas controladas, pero requiere instalar la CA en dispositivos, se declara de desarrollo y no debe convertirse en la solución de onboarding comercial. No compartir claves privadas ni incluirlas en el repo. [dotnet dev-certs](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-dev-certs), [mkcert](https://github.com/FiloSottile/mkcert).

| Ruta a investigar | Ventaja | Coste o incompatibilidad |
|---|---|---|
| HTTPS con CA de desarrollo en equipos de referencia | Permite probar sin cloud durante la sesión | Fricción de instalación; no demuestra QR <60 s para usuario nuevo |
| Nombre DNS y certificado confiable con resolución local | Puede reducir instalación de certificados | Gestión de DNS, renovaciones y conectividad de preparación; aún sin diseño validado |
| Hosting externo o túnel permanente | Simplifica algunos certificados | Contradice independencia de Internet si gameplay lo necesita; no adoptar |
| App mínima Plan B | Ruta de contingencia prevista en v4 | Sólo tras fallo reproducible de PWA; no desarrollar por adelantado |

No hay una solución comercial de HTTPS local aprobada. El Spike debe distinguir demostración en dispositivos preparados de incorporación de una persona nueva sin configuración técnica compleja.

Chrome documenta permisos de Local Network Access, con evolución por tipo de solicitud. La nota inicial tiene limitaciones específicas para WebSockets; no generalizarla a todas las versiones actuales. Evaluar fetch, conexión WS y navegador/versión exactos; favorecer mismo origen para reducir cruces innecesarios. [Chrome LNA](https://developer.chrome.com/blog/local-network-access).

Mantener todo el shell, fuentes, modelos y assets necesarios disponibles localmente. Un service worker ayuda al offline, pero no crea la red LAN ni un certificado confiable. Versionar el cache y detectar incompatibilidad entre PWA cacheada y protocolo del servidor. [PWA offline](https://developer.mozilla.org/en-US/docs/Web/Progressive_web_apps/Guides/Offline_and_background_operation).

Wake Lock puede evitar que se apague la pantalla, pero puede denegarse o revocarse. Reaccionar a visibilidad, suspensión y regreso a primer plano con pausa/recuperación; no prometer muestreo continuo en background. [Wake Lock](https://developer.mozilla.org/en-US/docs/Web/API/Screen_Wake_Lock_API).

## 4. Sensores, protocolo y latencia

La especificación W3C define aceleración, aceleración con gravedad, giro en grados/s e intervalo en ms; los valores pueden faltar. **Recomendación:** declarar unidades y transformación de ejes, separar grados de radianes, validar null/no finitos y convertir orientación relativa a neutral. No usar aceleración con gravedad como potencia sin tratamiento explícito. [W3C](https://www.w3.org/TR/orientation-event/).

50 Hz es una meta de datos útiles, no el resultado de programar un temporizador cada 20 ms. Registrar frecuencia efectiva de muestras únicas, intervalo, pérdidas y edad del input; repetir una muestra 50 veces no equivale a 50 mediciones.

La API WebSocket del navegador no administra backpressure. **Recomendación:** observar bufferedAmount, limitar tamaño y tasa, evitar cola infinita y registrar congestión. Usar buffer temporal acotado suficiente para detectar gestos: descartar indiscriminadamente todo salvo el último sensor puede perder el pico del golpe. [WebSocket](https://developer.mozilla.org/en-US/docs/Web/API/WebSocket).

En .NET se admite un envío y una recepción simultáneos por ClientWebSocket; serializar escritores. Los Channels acotados permiten diferentes políticas al llenarse. **Recomendación:** diferenciar muestras de alta tasa de eventos discretos como ACTION_UP, READY y resultados; estos últimos no deben perderse por una política pensada para snapshots. [SendAsync](https://learn.microsoft.com/en-us/dotnet/api/system.net.websockets.clientwebsocket.sendasync?view=net-10.0), [Channels](https://learn.microsoft.com/en-us/dotnet/core/extensions/channels).

ASP.NET Core soporta WebSocket, orígenes permitidos y keepalive; CORS no protege WS y Origin no sustituye identidad de sesión. Microsoft recomienda SignalR para muchas aplicaciones y no afirma una desventaja importante general de rendimiento. **Recomendación local:** WS nativo por simplicidad del protocolo v1 congelado; evaluar SignalR sólo con necesidad concreta y decisión explícita, sin afirmar que sea más lento por definición. [ASP.NET WS](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/websockets?view=aspnetcore-10.0).

System.Text.Json tiene generación de metadata y optimización de serialización; ésta no ofrece fast-path deserialization. **Recomendación:** servidor con DTOs y opciones reutilizadas; source generation sólo si resulta conveniente y medido. En Unity probar el serializador elegido con fixtures idénticos. No cambiar JSON por MessagePack sin evidencia de que serialización o ancho de banda bloquean P0. [Microsoft JSON](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation-modes).

**Propuesta de medición, pendiente de implementación:**

- Definir timestamp monotónico, unidad y origen. La comparación directa de reloj teléfono/PC requiere estimar offset y su error; Unix timestamp por sí solo no sincroniza.
- Conservar sample timestamp, recepción PC, análisis, validación y consumo Unity. Asociar trace/sequence al resultado.
- Reportar RTT separado de latencia de una vía. RTT/2 sólo es estimación y supone simetría.
- Medir P50/P95 y edad de muestras con número de observaciones, duración, dispositivos y carga; incluir warm-up por separado.
- No sumar P95 por etapa para fabricar el P95 end-to-end; calcularlo por trazas correlacionadas.
- Para movimiento → respuesta visible, usar un método externo sincronizado. Grabación de la pantalla/dispositivo por un tester no debe introducir almacenamiento de video de webcam del juego, prohibido por v4.

## 5. Cámara y alternativas evaluadas

| Opción | Encaje | Riesgo | Recomendación |
|---|---|---|---|
| homuler MediaPipeUnityPlugin | MediaPipe dentro de Unity; MIT y terceros | Windows CPU, fallos nativos y modelo/ABI fijados | Primera candidata a spike; no declarada aprobada |
| OpenCV for Unity de Enox | Wrapper comercial multiplataforma | Coste, licencia Extension Asset y trabajo de integración | Evaluar sólo si ahorra un bloqueo concreto |
| OpenCvSharp4 | Wrapper .NET con perfil compatible candidato | Runtime nativo/arquitectura y soporte Unity no automático | Usar sólo tras confirmar necesidad y build |
| MediaPipe Python + OpenCV local | API de Tasks documentada y prototipado | Empaquetado, proceso adicional, IPC y nueva dependencia | Comparador o contingencia propuesta; no stack autorizado |
| BlazePose ONNX con Unity Sentis | Muestra oficial de inferencia en Unity | Conversión/modelo y pipeline distinto de MediaPipe | Alternativa documentada si falla ruta congelada |
| Ultralytics pose | Ecosistema de pose alternativo | Cambia stack; AGPL/Enterprise y modelo diferente | No candidato inicial del MVP |

El plugin homuler advierte que fallos de código nativo pueden cerrar el proceso; los binarios distribuidos para Windows/macOS usan CPU. Sus recursos deben configurarse para build mediante StreamingAssets. Un sample funcionando en Editor no es evidencia de distribución estable. [Mantenedor](https://github.com/homuler/MediaPipeUnityPlugin).

La guía MediaPipe Tasks documenta tracking en modos vídeo/live y descarte de frames si una llamada async llega mientras procesa. **Recomendación:** comparar Lite/Full, sin segmentación cuando no sea necesaria, captura medida >=30 FPS y latencia de inferencia separada; no confundir FPS de captura con resultados por segundo. La API Python no demuestra que un wrapper Unity exponga exactamente las mismas opciones. [MediaPipe Tasks](https://developers.google.com/edge/mediapipe/solutions/vision/pose_landmarker/python).

Player Lock sigue necesitando lógica del producto: detectar una pose no garantiza mantener el jugador autorizado. Probar personas que cruzan, oclusión y recuperación sin reasignación automática. Para Jungle Slice estudiar si las muñecas de Pose bastan; Hand Landmarker completo sólo si aporta precisión necesaria. Son hipótesis de implementación, no reducción de requisitos.

Enox declara soporte PC y otros destinos; la ficha consultada muestra 3.0.4, 26 de septiembre de 2026, tipo Extension Asset. No se pudo verificar precio en la página extraída; no se registra una cotización inventada. [Soporte Enox](https://enoxsoftware.com/opencvforunity/documentation/), [ficha Asset Store](https://assetstore.unity.com/packages/tools/integration/opencv-for-unity-21088).

OpenCvSharp necesita biblioteca administrada y runtime nativo; un build puede compilar y fallar al cargarlo. Sus perfiles slim omiten videoio, por lo que elegir el paquete más pequeño podría impedir captura. [Selección del mantenedor](https://github.com/shimat/opencvsharp/blob/main/docs/docfx/articles/getting-started/package-selection.md).

La muestra oficial Unity Sentis incluye variantes Lite/Full/Heavy convertidas de TFLite a ONNX. Es una alternativa técnica para investigar, no prueba de equivalencia ni cambio aprobado de MediaPipe. [Muestra Unity](https://github.com/Unity-Technologies/sentis-samples/blob/main/BlazeDetectionSample/Pose/README.md).

Ultralytics ofrece AGPL y licencia Enterprise. Se registra como coste y alcance adicional, sin afirmar que toda utilización comercial requiera automáticamente pagar. Si se reconsidera, revisar términos del uso concreto. [Licencias del proveedor](https://www.ultralytics.com/license).

## 6. Filtrado, rendimiento y librerías opcionales

One Euro es un filtro adaptable que busca equilibrar jitter y lag; los autores aportan procedimiento de tuning e implementaciones con licencias distintas. **Recomendación:** compararlo con un filtro simple en movimientos quietos y rápidos. No elegir coeficientes definitivos antes de medir, ni filtrar quaternions como cuatro escalares sin estudiar normalización y continuidad. [Autores](https://gery.casiez.net/1euro/).

Unity proporciona Awaitable, con pooling y retorno explícito al main thread; una instancia no debe esperarse varias veces. UniTask aporta APIs adicionales, pero eso no justifica agregarlo automáticamente. [Unity Awaitable](https://docs.unity.com/en-us/engine/6000.3/manual/scripting/programming-distribute-work-threads/async-await-support/async-awaitable-introduction), [UniTask](https://github.com/Cysharp/UniTask).

Unity recomienda reducir asignaciones en rutas frecuentes y reutilizar objetos. **Recomendación:** buffers de sensores/cámara reutilizados, escrituras de telemetría fuera del frame crítico, no Debug.Log por muestra y pooling de objetos de aparición repetida. No deshabilitar GC ni introducir ECS/Burst por costumbre. [Memoria Unity](https://docs.unity3d.com/cn/2022.3/Manual/performance-garbage-collection-best-practices.html).

URP Forward está recomendado por Unity para pocas luces o hardware modesto. **Inferencia de proyecto:** candidato adecuado al estilo low-poly, con luces/sombras limitadas y efectos medidos; no es un benchmark contra HDRP en nuestra escena. [Rutas URP](https://docs.unity.com/en-us/engine/6000.7/manual/render-pipelines/universal-render-pipeline/introduction/urp-concepts/rendering-paths/comparison). Esta página es de una versión posterior a 6.3: validar disponibilidad concreta en el editor fijado.

VContainer tiene DI y publica benchmarks propios; Serilog.Async puede escribir fuera del hilo principal y descartar eventos si se llena el buffer. **Recomendación:** no incorporar DI adicional sin necesidad; si se elige logging async registrar pérdidas y flush al salir. No confundir claims de benchmarks del mantenedor con ahorro total del gameplay. [VContainer](https://github.com/hadashiA/VContainer), [Serilog.Async](https://github.com/serilog/serilog-sinks-async).

## 7. Snapshot GitHub

Última release estable retornada por /releases/latest durante la consulta. Estas etiquetas no constituyen una lista de dependencias aprobadas. Todos los repositorios consultados retornaron archived=false. La actividad de un repositorio no garantiza compatibilidad.

| Repositorio | Release observada | Fecha UTC | Licencia detectada API | Uso en propuesta |
|---|---|---|---|---|
| [homuler/MediaPipeUnityPlugin](https://github.com/homuler/MediaPipeUnityPlugin) | [v0.16.3](https://github.com/homuler/MediaPipeUnityPlugin/releases/tag/v0.16.3) | 2025-11-08 | MIT | Candidato cámara |
| [shimat/opencvsharp](https://github.com/shimat/opencvsharp) | [5.0.0.20261004](https://github.com/shimat/opencvsharp/releases/tag/5.0.0.20261004) | 2026-10-04 | Apache-2.0 | 4.x para evaluar Unity; 5.x no intercambiable |
| [endel/NativeWebSocket](https://github.com/endel/NativeWebSocket) | [2.0.7](https://github.com/endel/NativeWebSocket/releases/tag/2.0.7) | 2026-08-07 | NOASSERTION | Reserva si ClientWebSocket no basta |
| [Cysharp/UniTask](https://github.com/Cysharp/UniTask) | [2.5.11](https://github.com/Cysharp/UniTask/releases/tag/2.5.11) | 2026-05-19 | MIT | Opcional, no agregar inicialmente |
| [Shane32/QRCoder](https://github.com/Shane32/QRCoder) | [v1.8.0](https://github.com/Shane32/QRCoder/releases/tag/v1.8.0) | 2026-04-04 | MIT | Candidato QR servidor |
| [FiloSottile/mkcert](https://github.com/FiloSottile/mkcert) | [v1.4.4](https://github.com/FiloSottile/mkcert/releases/tag/v1.4.4) | 2022-04-26 | BSD-3-Clause | Sólo laboratorio |
| [game-ci/unity-test-runner](https://github.com/game-ci/unity-test-runner) | [v4.3.2](https://github.com/game-ci/unity-test-runner/releases/tag/v4.3.2) | 2026-09-09 | MIT | CI futura |
| [google-ai-edge/mediapipe](https://github.com/google-ai-edge/mediapipe) | [v1.0.0](https://github.com/google-ai-edge/mediapipe/releases/tag/v1.0.0) | 2026-07-28 | Apache-2.0 | Upstream; respetar versión del wrapper |
| [serilog/serilog](https://github.com/serilog/serilog) | [v4.4.0](https://github.com/serilog/serilog/releases/tag/v4.4.0) | 2026-07-10 | Apache-2.0 | Opcional logging local |
| [MessagePack-CSharp/MessagePack-CSharp](https://github.com/MessagePack-CSharp/MessagePack-CSharp) | [v3.1.11](https://github.com/MessagePack-CSharp/MessagePack-CSharp/releases/tag/v3.1.11) | 2026-10-03 | NOASSERTION | Aplazado; protocolo MVP JSON |
| [hadashiA/VContainer](https://github.com/hadashiA/VContainer) | [1.19.0](https://github.com/hadashiA/VContainer/releases/tag/1.19.0) | 2026-07-01 | MIT | Opcional DI |
| [dotnet/runtime](https://github.com/dotnet/runtime) | [v10.0.12](https://github.com/dotnet/runtime/releases/tag/v10.0.12) | 2026-09-08 | MIT | Base servidor |

NOASSERTION significa que el detector de GitHub no resolvió la licencia; no implica ausencia de licencia. NativeWebSocket 2.0.7 contiene Apache-2.0 en LICENSE. MessagePack contiene MIT y avisos de terceros. Revisar cada versión y modelos/binarios redistribuidos, no sólo el badge. [NativeWebSocket LICENSE](https://github.com/endel/NativeWebSocket/blob/2.0.7/LICENSE), [MessagePack LICENSE](https://github.com/MessagePack-CSharp/MessagePack-CSharp/blob/master/LICENSE).

## 8. Mercado y referencias de experiencia

| Referencia | Hecho comprobado | Aprendizaje para Gorilimpiadas | Encaje como tecnología del proyecto |
|---|---|---|---|
| Jackbox | Control en navegador con código; sus servidores requieren Internet | Identidad, espera, incorporación y una copia del host | Referencia UX; no sustituye el servidor local |
| AirConsole | Plataforma aloja/despliega servidores y emparejamiento; plugin Unity | Pantallas personales y control con teléfono | No candidata para requisito sin cloud obligatorio |
| Nintendo Switch Sports | Joy-Con con movimientos, spin y multijugador | Gesto comprensible, feedback y seguridad con correa | Hardware/ecosistema distintos; referencia de game feel |
| Nex Playground | Procesamiento de movimiento local; juegos descargados pueden jugarse offline | Cámara, privacidad, setup y competencia familiar | Hardware propietario, no dependencia de nuestro MVP |

Fuentes: [Jackbox incorporación](https://support.jackboxgames.com/hc/en-us/articles/15794771245975-How-do-I-get-started-playing-Jackbox-Games), [Jackbox Internet](https://www.jackboxgames.com/blog/how-to-bring-jackbox-with-you-wherever-you-go), [AirConsole](https://documentation.airconsole.com/), [Nintendo](https://www.nintendo.com/us/store/products/nintendo-switch-sports-114528/), [Nex FAQ](https://www.nexplayground.com/en-gb/blog/nex-playground-uk-ireland-faq).

**Inferencia:** teléfono como control, juego de movimiento y procesamiento local ya existen por separado en el mercado. No vender ninguno como novedad absoluta. La propuesta a validar es su combinación accesible en PC, técnica comprensible y torneo seguro con hardware existente. Esta investigación no prueba demanda, precio de venta óptimo ni tamaño del mercado.

Just Dance aparece como referencia en la v4, pero las búsquedas devolvieron principalmente documentación antigua para controller. No se atribuyen requisitos actuales de 2026 a un comunicado de 2015. Hace falta revisión por edición y plataforma si se quiere incluir una comparación comercial precisa.

Kenney publica assets de sus páginas de assets bajo CC0; Poly Haven también, aunque muchos de sus recursos son de orientación realista. **Recomendación:** Kenney como fuente a evaluar para placeholders/UI; recursos Poly Haven sólo si encajan y se optimizan. Mantener un manifiesto de origen, licencia, versión y modificaciones de cada asset adoptado. No se descargaron assets. [Kenney](https://kenney.nl/support), [Poly Haven](https://polyhaven.com/license).

Unity Personal tiene umbral financiero de USD 200,000 según su política consultada; Runtime Fee cancelado. La elegibilidad depende de las condiciones del equipo/organización, no sólo de ventas de este juego. No se elige licencia del producto ni se afirma coste cero para todas las herramientas del equipo. [Elegibilidad Unity](https://unity.com/pages/license-compliance), [Runtime Fee](https://unity.com/products/pricing-updates).

## 9. Hardware: investigar antes de comprar

No hay inventario de PC/teléfonos del equipo, así que no se recomienda comprar una configuración específica. Empezar midiendo lo disponible y elegir la PC oficial antes de cerrar la prueba.

- PC: registrar CPU/GPU/RAM/OS y arquitectura. Probar render e inferencia juntos, no dos benchmarks aislados.
- Webcam: medir FPS entregados, exposición, USB, campo visual, distancia e iluminación. Una ficha 30 FPS no demuestra inferencia a 30 FPS.
- Como referencia de mercado, Logitech BRIO documenta 1080p a 30/60 FPS; eso no demuestra que sea la compra óptima ni que un modelo distinto con nombre BRIO tenga idénticos modos. [Ficha del fabricante](https://www.logitech.com/content/dam/logitech/vc/eu/pdf/brio-datasheet.pdf).
- Red: evaluar PC cableada al router local cuando esté disponible, teléfonos en Wi-Fi y WAN desconectada. Comprobar aislamiento entre clientes y firewall antes de culpar al protocolo.
- Teléfonos: Android e iPhone de referencia más un dispositivo de menor rendimiento; registrar modelos y versiones exactas.
- TV/proyector: medir también demora de presentación, si tiene modo juego y si añade procesamiento de imagen.

Estas son condiciones de prueba propuestas. No se inventaron FPS, latencia, costes ni resultados de compatibilidad.

## 10. Pruebas que deben decidir la selección

Todas están **NOT TESTED**. Son propuestas para autorizar y delimitar en la fase correspondiente; no forman una nueva DoD aprobada ni adelantan el gameplay de Fase 1.

| ID | Prueba propuesta | Evidencia requerida | Puerta de decisión |
|---|---|---|---|
| P0-R01 | QR → HTTPS → permisos en Android/iPhone | Modelo, OS, navegador, isSecureContext, muestras válidas y pasos | Viabilidad PWA; medir conexión <60 s para persona nueva |
| P0-R02 | WAN desconectada, teléfono sin cache previo | Conectar, servir PWA/assets, resolver nombre y mantener sesión | Cero dependencia de Internet durante gameplay |
| P0-R03 | 1 y 4 conexiones con sensores | Hz efectivos, muestras únicas, pérdidas, bufferedAmount | 50 Hz objetivo y degradación documentada |
| P0-R04 | Pose en build PC limpia | Recursos/modelos offline, FPS captura/inferencia, memoria y fallos | Webcam >=30 FPS, viabilidad del adaptador |
| P0-R05 | Persona extra entra, oclusión y retorno | 20 cruces, identidad mantenida y recuperación | Cero cambio accidental de Player Lock |
| P0-R06 | Latencia completa con trazas y método externo | P50/P95, definición de inicio/fin, tamaño muestral, incertidumbre | P95 <150 ms; ideal <100 ms |
| P0-R07 | Bloquear teléfono, negar permiso y perder red/cámara | Mensajes, pausa, reconexión, cancelación segura y logs | Sin estados imposibles ni acción falsa |
| P0-R08 | Build fuera de máquina de desarrollo | PC sin Unity/.NET SDK; recursos locales y cierre limpio | Entrega reproducible; decidir empaquetado |
| P0-R09 | Comparar filtros y modelos | Mismos gestos, jitter, lag, detección y carga | Elegir por evidencia, no por popularidad |
| P0-R10 | Contratos cruzados JS/.NET/Unity | Fixtures, unidades, null, duplicados, rangos y enums | Interoperabilidad y autoridad PC |

Las pruebas de juego, cinco disciplinas y diez sesiones completas corresponden a las fases posteriores definidas por v4. Este informe no las marca como hechas.

## 11. Qué conservar, aplazar y resolver

**Conservar:** stack congelado, offline durante partida, 1–4 jugadores, cámara por turnos, autoridad PC, JSON y monorepo. La investigación no prueba inviabilidad suficiente para cambiarlo.

**Aplazar:** framework frontend grande, MessagePack, ECS/DOTS, DI adicional, cloud, backend online, Docker obligatorio para jugador y app móvil nativa. Son decisiones de coste/alcance, no afirmaciones universales de rendimiento.

**Resolver antes de elegir dependencias:** versión/OS Unity, mecanismo HTTPS local, ruta de integración cámara, procesos y empaquetado servidor, compatibilidad de contratos y hardware oficial. Registrar propuestas y evidencia en [DECISIONS.md](DECISIONS.md).

**Próximo trabajo lógico:** delimitar Fase 0 alrededor de estas pruebas, conservando la separación con Fase 1 y aclarando secciones 42/50. No iniciada automáticamente.
