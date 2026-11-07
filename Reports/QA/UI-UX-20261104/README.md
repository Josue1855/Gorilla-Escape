# Revisión UI/UX — Gorilla Escape

Revisión local del 4 de noviembre de 2026. Cambios implementados; no constituye aceptación física completa ni publicación de estas correcciones.

## Flujo integrado

La preparación pertenece a la sala, no al torneo. Historia y Fiesta pasan por conexión la primera vez. Después de terminar, salir al menú o cambiar de modo, se conservan el servidor, la sala y los dispositivos preparados. La preparación se invalida para otra sala, otro teléfono, abandono definitivo o requisitos adicionales de jugadores. Las acciones siempre siguen sujetas a señal vigente: reutilizar la preparación no permite jugar con datos antiguos.

## Correcciones

- Unity: pausa persistente al soltar el clic, navegación por teclado, encabezado separado del HUD y tema de UI Toolkit explícito. El splash usa el mismo gorila Meshy Embr de los juegos y su material compartido, con carrera sobre su propio esqueleto. Véase la revisión posterior en `../Meshy-Splash-20261104/README.md`. Etiquetas de tambores legibles e instrucciones de gestos controlados.
- Unity: la preparación de audio puede continuar aunque se destruya la escena que inició la primera llamada; se eliminó una espera que podía quedarse bloqueada.
- PWA: nombre confirmado por la PC, edición sin guardados prematuros, conexión sin acceso anticipado al menú, pausa según autoridad, estado ausente sin activar sensores y recuperación clara. Ajustes para nombres largos, pantallas pequeñas, horizontal, foco y texto ampliado.
- Flutter: registro del nombre por el endpoint de nombre, activación explícita inicial de sensores, recuperación y cambio a un QR nuevo, confirmación al salir de una partida conectada, pausa según autoridad y distribución adaptable. Una desconexión ya no muestra «Control preparado».

## Comprobaciones

| Área | Resultado | Alcance |
| --- | --- | --- |
| C# compartido/integración | 159/159 | Contratos, conexión, comandos y reutilización de preparación |
| Unity Play Mode | 10/10 | Pausa, eventos de puntero, navegación, continuidad de sala, audio interrumpido, splash y 12 pantallas |
| PWA pruebas | 95/95 | Estado, presentación y control adaptativo |
| PWA navegador | 133 comprobaciones | Diez juegos y cuatro fases, 320×568, 390×844, 844×390, texto ampliado, invitado y recuperación |
| Flutter pruebas | 26/26 | Widgets, diez juegos y fases, texto ampliado, nombre, salida y desconexión |
| Flutter análisis | Sin problemas | Analizador estático |
| Compilación | PWA y APK debug | Compilación local, sin publicar |
| Android emulador | 5 flujos | Inicio, enlace inválido, sala inaccesible, sustitución de QR y regreso al inicio |
| Fundamentos | Correcto | UPM, 714 GUID únicos, contratos y recursos PWA |

Las pruebas de presentación usan estados sintéticos y salas inválidas deliberadas. No equivalen a una sesión física conectada. Los scripts reproducibles son `tools/qa/review_pwa_ui.py` y `tools/qa/review_android_ui.py`. El emulador de Android usa una imagen de datos temporal propia para conservar los datos previos del AVD.

## Evidencia y límites

- [Unity: resultados](unity/results.txt), [capturas de pantallas](unity/screenshots/), [medidas del árbol visual](unity/screenshots/layout.txt).
- [PWA: resultados](pwa/results.json) y capturas en la misma carpeta.
- [Android: resultados](android/results.json) y capturas/XML en la misma carpeta.
- Las capturas programáticas del Editor de Unity muestran un desplazamiento del viewport en esta instalación. El árbol visual cabe dentro de la resolución declarada y también se inspeccionó la ventana real; estas imágenes no certifican el encuadre de una compilación independiente.
- No se certificaron cuatro teléfonos físicos, sensores reales de Android/iOS, Safari, red WAN ni instalador final de Unity. Los avisos de compatibilidad futura de Gradle/flutter_webrtc permanecen; la compilación actual termina.
- Este trabajo no se ha subido a Git ni desplegado.
