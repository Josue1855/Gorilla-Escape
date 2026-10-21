# Integración móvil y validación · 21 de octubre de 2026

Trabajo en `feature/gameplay-desafio-del-tronco`, dentro del proyecto principal. No se hicieron merges ni PR. El backend separado conserva su rama y sus fuentes originales.

## Implementado

- Unity comunica la prueba activa y su fase por el IPC autenticado existente. Java entrega esa presentación al mismo teléfono mediante ACK de heartbeat, sin aumentar la frecuencia de muestras de sensores.
- La PWA muestra lobby, preparación, cuenta regresiva, cruce, pausa, victoria, caída o espera de turno. Los estados caducan si dejan de actualizarse. La simulación y el resultado siguen perteneciendo a Unity.
- Registro opcional de nombre desde la PWA hacia el backend SQLite real mediante una fachada autorizada por peer. El backend permanece en loopback y arranca como proceso hijo desde `Java/backend.jar`. No hay reintentos automáticos de creación ante una respuesta incierta.
- App Flutter Companion con diseño de selva, escáner QR, alternativa por enlace, permiso de cámara, mensajes accesibles, icono propio y respuesta háptica al abrir el control. El navegador ejecuta la misma PWA y conserva la única conexión de sensores. Hay proyecto Android y fuentes iOS; iOS no se compiló aquí.
- Transiciones de la PWA y respeto a reducción de movimiento. El dibujo de inclinación es una vista local del teléfono, no una medición del equilibrio del personaje.
- Corrección del arranque de laboratorio: un emulador explícito no debe activar la configuración de confianza LAN del ejecutable normal.

## Validación automática

| Capa | Resultado |
| --- | --- |
| Unity EditMode | 186 aprobadas |
| Unity PlayMode, flujo del tronco | 5 aprobadas |
| PWA | 79 aprobadas |
| Java técnico | 115 casos: 111 aprobados, 4 opcionales omitidos; después se ejecutó Cloudflare real por separado |
| Backend jugadores/SQLite | 17 aprobadas; 3 comprobaciones adicionales de fachada/registro aprobadas |
| Flutter | 2 pruebas aprobadas; análisis sin incidencias; APK debug generado; dos emuladores API 36 con pantalla grande/pequeña, orientación vertical/horizontal y permisos de cámara |
| Cloudflare real | HTTPS público 200, rutas privadas 404, admisión inválida 400 y cierre correcto |
| Android, recorrido completo | PASS: SQLite, sensores, contexto Unity, segundo plano y pantalla sin admisión; 483 muestras aceptadas por Unity y cierre de Java correcto |
| Unity Linux | Compilación de desarrollo generada y copiada al paquete del proyecto principal |

Las pruebas de Unity se ejecutaron en una copia temporal de QA del mismo código para respetar el Editor que estaba abierto. El desarrollo y la entrega permanecen en el proyecto principal. No se modificaron escenas sin guardar del Editor.

En ambos arranques fríos hubo una alerta de lentitud del System UI del emulador; se recuperó con Esperar. No fue un cierre de Flutter. No se certifica rapidez de arranque a partir de estos dispositivos virtuales.

Los archivos de evidencia están en `docs/evidence/integration-2026-10-21/`. El flujo Android incluye conexión real, registro en SQLite, señal virtual y cambio de contexto desde Unity. Cuando la caché de accesibilidad nativa de Chrome omite botones web, el script obtiene el botón de su árbol de accesibilidad web y envía un toque real sobre sus límites; no usa coordenadas de capturas ni sustituye permisos. El resultado final del recorrido Android se documenta en `android-results.json`; sus capturas excluyen el fragmento de admisión. Sensores virtuales no equivalen a una prueba física.

## Arranque y reproducción

Escena oficial: `Unity/Assets/_Project/Scenes/Bootstrap/Bootstrap.unity`. En el Editor, menú **Gorilla Escape → Play Gorilla Escape**. El prototipo del tronco utiliza el mismo menú, runtime y servidor del proyecto.

Preparar el backend separado después de compilarlo:

```sh
python3 tools/prepare_players_backend.py --jar '/ruta/al/backend/target/backend-0.1.0-SNAPSHOT.jar'
```

La copia de desarrollo de esta máquina ya está preparada en `Unity/Builds/FoundationLinux/Java/backend.jar`. El ejecutable actualizado está en `Unity/Builds/FoundationLinux/GorillaEscape.x86_64`. El APK está en `Companion/build/app/outputs/flutter-apk/app-debug.apk`.

Repetir QA con un emulador Android dedicado ya iniciado y una pantalla de pruebas X disponible en `:97`:

```sh
python3 tools/integration/android_companion.py docs/evidence/nueva-ejecucion
```

No debe haber otra sesión del servidor del juego activa durante esa prueba. El script crea datos temporales, etiqueta la señal como `emulator`, usa ADB reverse y cierra sus procesos y conexiones al terminar. No modifica certificados ni firewall.

## Límites que no deben confundirse con aceptación

- Falta confirmar el recorrido en el iPhone físico, sus permisos y confianza HTTPS. Android emulado no certifica Safari/Chrome de iOS ni una red Wi-Fi física.
- El túnel real pasó una comprobación externa en esta ejecución. Su disponibilidad futura depende de Internet y Cloudflare; esto no certifica WebRTC desde un teléfono físico ni el funcionamiento sin Internet.
- El backend vigente guarda jugadores. No existe todavía contrato de resultados, clasificación o torneo persistente; esta entrega no inventa uno. Registrar un UUID tampoco es iniciar sesión ni recuperar una cuenta existente.
- Flutter abre el control web: no implementa un segundo controlador nativo de sensores. La cámara virtual prueba permisos y navegación; una lectura óptica de QR en cámara física sigue pendiente.
- La prueba gráfica de Unity corrió en una pantalla virtual con renderizado por software y emuladores simultáneos; su frecuencia observada fue baja. No certifica rendimiento ni una partida física completa calibrada. Las pruebas de reglas y flujo son independientes de esa medición.
- El torneo completo continúa fuera de la disponibilidad del prototipo del tronco. No se declara que todo el juego esté “perfecto”.

## Actualización: instalación física iOS con XCross

Después de este reporte se compiló la Companion en modo release/AOT con XCross 2.0.5 y Swift 6.4.1 en Linux. La instalación por USB en el iPhone 15 se confirmó consultando el registro de aplicaciones: Gorilla Escape, versión 1.0.0, compilación 1. No se hicieron merges. Esto verifica compilación, firma e instalación; no reemplaza la prueba de cámara, sensores y partida completa en el teléfono. El lanzamiento automático fue rechazado por el servicio de procesos de iOS; abrir desde el icono y comprobar cualquier aviso del sistema sigue pendiente.
