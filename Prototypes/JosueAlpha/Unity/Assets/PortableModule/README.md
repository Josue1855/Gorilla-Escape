# Portable Module

Esta carpeta es la frontera copiable hacia otro proyecto Unity.

- `IPortableInputSource` entrega movimiento y pose sin revelar el transporte.
- `IPortableModuleStatus` entrega estado y URL pública.
- `PortableServiceHost` prepara y administra el servicio local y Cloudflare sin ventanas adicionales.
- `MotionSession` implementa `IPortableInputSource` para los juegos actuales.

Para adaptarlo a otro proyecto:

1. Copiar `Assets/PortableModule` conservando los `.meta`.
2. Copiar o adaptar `MotionSession.cs`.
3. Consumir `IPortableInputSource` mediante composición o inyección.
4. Colocar `CameraTracker`, `MotionRelay/index.html` y `PREPARAR_MODULO.ps1` en la raíz indicada por `JUNGLE_PARTY_MODULE_ROOT`.

En el editor de este alpha, la raíz se detecta automáticamente. En un Player se espera una carpeta `PortableModule` junto al ejecutable. La variable de entorno permite otra disposición sin recompilar.
