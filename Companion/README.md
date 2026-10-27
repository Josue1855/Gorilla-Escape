# Gorilla Escape — control nativo Flutter

Recuperado del incremento nativo de la rama del tronco y adaptado a los siete juegos de Saúl. Escanea un QR de la PC o pega su enlace, activa los sensores y conserva la app visible. El control usa sensores nativos y WebRTC; no abre un navegador ni una WebView. La PWA conserva su acceso independiente.

Unity decide la mecánica, puntuación y resultados. La app muestra la presentación v2 recibida por ACK del canal fiable, con métricas disponibles y caducidad; no calcula progreso o equilibrio desde sus sensores. Confirmar/recentrar usan touch del contrato oficial. El movimiento es un DataChannel sin orden ni retransmisión, máximo 50 Hz y sólo tras observaciones nuevas. Al suspenderse se limpian muestras y botones; al regresar se requieren muestras frescas y se reanuda mediante el protocolo existente.

El nombre opcional se registra por el peer autenticado del juego. SQLite permanece en la PC, detrás del Java supervisado y el backend privado. No se repite una escritura incierta ni se almacenan admisiones, enlaces o nombres en la app. No hay descubrimiento mDNS ni entrada por PIN en este incremento.

## Desarrollo

Versión del incremento: 1.4.0 (10), posterior a la 1.3.0 (9) de la rama del tronco. Flutter 3.47.6, Dart 3.13.5; dependencias fijadas en pubspec.lock. CI utiliza el commit oficial `5fc346839b5d0eef006ed8404392afb4dfae428d` y ejecuta análisis y pruebas.

```sh
flutter pub get
flutter analyze
flutter test
flutter build apk --debug
```

El APK de desarrollo se genera en `build/app/outputs/flutter-apk/app-debug.apk` y no se versiona. Fuentes Android/iOS, permisos de cámara/movimiento/red local e iconos se conservan. El HTTP de emulador está limitado a debug, loopback y `GORILLA_EMULATOR_LAB=true`; producción exige HTTPS. TLS no se desactiva.

Este incremento verificó análisis, 13 pruebas y compilación APK Android; no instaló la app en un dispositivo ni recompiló/firmó iOS. Las instalaciones y pruebas físicas históricas de la rama original no acreditan estas fuentes nuevas. Falta la aceptación con cámara/QR y sensores reales junto al Player actual de Unity.

[Arquitectura, procedimientos y límites](../docs/03-development/MOBILE_STACK_INTEGRATION.md).
