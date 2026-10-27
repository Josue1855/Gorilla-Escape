# Descubrimiento de partidas en la red local

El servidor del juego anuncia `_gorillaescape._tcp.local.` mediante DNS-SD/mDNS. Flutter usa las APIs nativas de Bonjour (iOS) y NSD (Android) para encontrarlo. No existe un directorio público de salas ni un servidor remoto adicional.

## Flujo

1. El servidor local prepara su conexión y anuncia la partida en la red privada.
2. La app busca durante 12 segundos al abrirse o volver al primer plano. También permite buscar manualmente.
3. Si aparece una PC, abre el campo del PIN. Si aparecen varias, el jugador elige una.
4. La app solicita la admisión al origen HTTPS anunciado, utilizando el PIN mostrado por Unity. Se conservan las mismas admisiones breves de un solo uso, límite de jugadores y control RTC del acceso por QR.

El registro SRV resuelve la IP local y el puerto del servidor. Sus atributos TXT son `v=1`, `game=gorilla-escape` y `origin=<origen HTTPS>`. No se anuncian PINs, admisiones, perfiles ni sensores. La IP local permite identificar el host; la app utiliza `origin` para conectar con HTTPS. Si el modo resuelto es cloudflared, este origen es el del túnel; no se desactiva la validación de certificados para conectarse a una IP descubierta.

El anuncio se actualiza al cambiar el origen y se retira al dejar de estar preparado o cerrar el servidor. Un fallo de publicación no bloquea Unity; se vuelve a intentar después de 30 segundos. La publicación utiliza interfaces IPv4 privadas. Flutter reconoce los tipos de servicio con y sin el punto final que pueden devolver las plataformas y descarta versiones, tipos u orígenes inesperados. Admite orígenes HTTPS privados IPv4 o de `trycloudflare.com`.

## Permisos y límites

- iOS declara `_gorillaescape._tcp` en `NSBonjourServices` y explica el acceso en `NSLocalNetworkUsageDescription`. El sistema puede pedir permiso de red local.
- Android utiliza el plugin NSD nativo. La app declara el permiso de red local y lo solicita cuando lo exige Android 17 con un target SDK 37 o superior. La compilación actual mantiene el target que define Flutter.
- Si no hay permisos, no aparece la PC o la red bloquea multicast, se mantiene el QR y la búsqueda manual.
- Ambos dispositivos deben compartir una red local que permita tráfico entre ellos. El descubrimiento no atraviesa redes de invitados aisladas ni otras subredes de forma automática.
- La búsqueda se detiene al pasar a segundo plano, entrar al control o destruir la pantalla. Los anuncios son descubrimiento, no una prueba criptográfica de identidad de la PC.
- Una PWA convencional no ejecuta la búsqueda Bonjour nativa. Conserva QR y PIN dentro del origen de la PC ya abierto.

## Dependencias

Servidor: JmDNS 3.6.1. Flutter: Bonsoir platform interface y sus plugins Darwin/Android. Se usan los plugins móviles directamente porque el paquete paraguas añade una dependencia Linux de dbus incompatible con el wake lock actual. No se cambió ni se forzó la versión de dbus.

## Validación del 23 de octubre de 2026

- 34 pruebas de servidor enfocadas en conectividad, descubrimiento y admisión: aprobadas.
- 22 pruebas Flutter: aprobadas; analizador sin problemas.
- Android debug e iOS release 1.3.0 (9): compilados.
- Prueba LAN real con el publicador Java y un navegador Avahi independiente: detectó nombre, IP y puerto, leyó los TXT y recibió la retirada al cerrar el publicador. El origen usado en esta prueba era sólo un dato de anuncio; no se abrió ese dominio ni se creó un túnel de prueba.
- El JAR incluido en el paquete Linux contiene el publicador y JmDNS.

Falta la prueba de descubrimiento y entrada con un iPhone/Android físico. La compilación móvil preparada aún debe instalarse. Esta evidencia no demuestra una partida completa entre hardware físico ni el comportamiento de todos los routers.
