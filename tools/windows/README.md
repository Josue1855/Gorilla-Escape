# Gorilla Escape — candidato Windows x64

Este paquete combina un Player de Unity 6000.3.23f1, el host Java 21,
el backend Spring Boot/SQLite y un runtime Java incluido. No es necesario
instalar Unity ni Java para abrir este candidato.

## Uso

1. Extraer TODO el ZIP en una carpeta con permiso de escritura.
2. Ejecutar `Start-GorillaEscape.cmd`.
3. El launcher inicia el backend SQLite sólo en loopback (127.0.0.1:8090),
   expone las rutas absolutas `GORILLA_IPC_JAVA` y `GORILLA_IPC_JAR` a Unity,
   y espera al Player. Unity es responsable de iniciar/detener su host IPC
   administrado cuando el flujo de producto lo solicite.
4. Al cerrar Unity, el launcher detiene el proceso del backend que inició.

Los datos persistentes de SQLite se conservan en el directorio del usuario;
no se incluyen bases reales, tokens, certificados ni datos personales.

## Limitaciones de aceptación

- Paquete generado **únicamente** después de GameCI EditMode/PlayMode,
  build Windows y verificación Java/SQLite. La licencia Unity requiere configuración.
- Es un **candidato**, no una versión jugable certificada; deben validarse
  navegación, unión de teléfono, QR HTTPS, red LAN, sensores y minijuegos en
  Windows, Android e iPhone reales antes de presentar una release jugable.
- La PWA integrada en el host Java procede de `PWA/` del repo Unity;
  puede ser distinta al fork `Dise-o_pwa`. No se mezcla el fork experimental
  automáticamente con el juego sin revisión del mantenedor.
- El backend SQLite expone APIs en loopback; no publicar el puerto 8090
  en Internet, ni modificar las reglas de firewall.
- Se deja instalado el JRE autocontenido del paquete. El acceso físico
  a hardware móvil y la comprobación de funcionamiento requieren pruebas externas.
