# Gorilla Escape · Companion

Aplicación Flutter para preparar el teléfono y abrir el control de la partida. Comparte la identidad visual de la PWA: selva, arena, río y el náufrago humano.

## Flujo

1. Abre Gorilla Escape en la PC y elige teléfono en el Desafío del Tronco.
2. Escanea el QR desde la aplicación, o pega su enlace en la sección alternativa.
3. El navegador abre la PWA de esa PC. Pulsa Conectar, permite movimiento y vuelve a mirar la PC para calibrar.
4. La PWA recibe de Unity la prueba activa, el turno y la fase. La app nativa no abre una segunda conexión WebRTC ni captura sensores en paralelo.

El QR debe ser HTTPS, versión 1, con sesión y admisión válidas en el fragmento. No se guardan enlaces, admisiones ni perfiles en almacenamiento de la app. El permiso de cámara se solicita al abrir el escáner; denegarlo conserva la alternativa por enlace.

## Desarrollo

Flutter 3.47.6 / Dart 3.13.5 usados en la validación:

```sh
flutter pub get
flutter analyze
flutter test
flutter run -d emulator-5554
flutter build apk --debug
```

APK de desarrollo: `build/app/outputs/flutter-apk/app-debug.apk` (no versionado). Android verificado en API 36. El proyecto iOS incluye permiso de cámara y destino mínimo 15.5; compilar y firmar requiere macOS/Xcode y sigue pendiente. La aplicación es un acompañante con apertura del control web: no constituye un controlador de sensores nativo independiente.
