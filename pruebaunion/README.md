# Jungle Party · betaSaul1

Proyecto de Unity en `pruebaunion`. Requiere Windows, Unity **6000.6.0f1**, cámara web, un celular con sensores de movimiento y acceso a Internet.

## Primer arranque

1. Desde PowerShell, en la raíz del repositorio, ejecuta `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\pruebaunion\PREPARAR_MODULO.ps1`. Instala Python, las dependencias de la cámara y Cloudflare en la carpeta del proyecto.
2. En Unity Hub, agrega `pruebaunion` como proyecto. Abre `Assets/Scenes/Splash.unity` y pulsa **Play**.
3. En Conexión, espera a que aparezca el QR. Escanéalo con el celular, toca **ACTIVAR MOVIMIENTO** y concede los permisos del navegador.
4. Pulsa **ACEPTAR** en el celular. Elige **Historia** para recorrer los cuatro retos o **Juego libre** para abrir el catálogo.

Cada sesión genera un enlace HTTPS nuevo. Desde el celular puedes aceptar, volver, abrir el menú y recentrar el puntero.

## Contenido

- **Historia:** Coconut Throw, Coconut Slice, Whack-a-Mole y Banana Bowling, con viñetas, narración, reintentos y dos finales.
- **Juego libre:** los cuatro retos de Historia, Gorilla Boxing, Jungle Baseball, Jungle Drums, Log Balance y Jungle Keeper.
- Los recursos que usa Unity están dentro de `Assets`. Los archivos fuente sueltos de arte, incluidos los modelos comprimidos, están en la carpeta `Modelos e imagenes` en la raíz del repositorio.
