# Gorilla Escape · Companion

Aplicación Flutter con control nativo: escáner, sensores del teléfono y canales WebRTC hacia Unity, sin abrir el navegador. La PWA sigue disponible para quienes escanean desde la cámara del sistema.

## Flujo

1. Abre Gorilla Escape en la PC y elige teléfono en el Desafío del Tronco.
2. Escanea el QR dentro de la aplicación, o pega su enlace en la sección alternativa.
3. Pulsa ACTIVAR CONTROL y permite movimiento. Calibra desde la PC.
4. Unity comunica la prueba, el turno y la fase al control nativo. Al abandonar la app se suspende el envío; las muestras antiguas no se reutilizan al regresar.

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

APK de desarrollo: `build/app/outputs/flutter-apk/app-debug.apk` (no versionado). Android verificado en API 36. El proyecto iOS incluye permiso de cámara y destino mínimo 15.5. Se compiló y firmó desde Linux con XCross 2.0.5 y se instaló por USB en un iPhone 15. Para repetirlo, usar `xcross flutter run --release --usb` con el Swift 6.4.1 del SDK de XCross en PATH y una sesión de firma configurada. La partida física completa y sus permisos de movimiento siguen pendientes de validación. Se verificó la instalación y apertura de la versión nativa 1.1.0 (2). La partida física sigue pendiente; las pruebas anteriores de la PWA no acreditan el control nativo.

El icono iOS se exporta del mismo vector de gorila que Android, con fondo opaco y todos los tamaños del catálogo. Para regenerarlo desde la raíz: `/usr/bin/python3 tools/art/build_companion_icons.py`.

Para XCross, los PNG principales también figuran explícitamente en Copy Bundle Resources y en `CFBundleIcons`: su ensamblador omite los catálogos `.xcassets`. Se verificó el resultado leyendo el icono de esta aplicación desde SpringBoard en el iPhone, sin capturar la pantalla completa.
