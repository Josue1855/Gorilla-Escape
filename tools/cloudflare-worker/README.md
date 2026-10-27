# Enrutador de salas

El Worker conecta cada sala con su Quick Tunnel. El teléfono y la PC deben estar en la misma LAN para WebRTC. El QR contiene una admisión de un solo uso; abrir solo la URL de la sala no concede acceso.

Antes de publicar, configurar un secreto aleatorio independiente y de al menos 32 bytes como `ROOM_REGISTRATION_SECRET` en el Worker y proporcionar el mismo valor mediante `GORILLA_ROOM_REGISTRATION_SECRET` o el archivo privado `~/.config/gorilla-escape/room-registration-secret` en la PC del juego. Nunca guardarlo en el repositorio ni distribuirlo dentro de la aplicación. La configuración de Wrangler incluye la migración del Durable Object, necesario para registrar salas de manera consistente. La publicación requiere acceso a la cuenta y zona Cloudflare. La versión publicada el 27 de octubre de 2026 usa Durable Objects y el secreto configurado en esta PC.

Sin secreto, o con el Worker anterior, el registro falla y AUTO vuelve a Cabaña. El juego registra y verifica la ruta antes de emitir el QR público; renueva cada 30 minutos y cada reserva caduca tras tres horas sin renovación. Otro proceso no puede reclamar una sala activa. En caso de pérdida del túnel se puede renovar la misma sala desde el mismo proceso. Al reiniciar, un código idéntico puede permanecer reservado hasta su caducidad.

El secreto compartido autoriza a registrar salas y requiere distribución segura a cada PC autorizada; no es un sistema de credenciales individuales. El Worker limita los destinos a Quick Tunnels HTTPS y las rutas públicas a los recursos y operaciones permitidas. El túnel con nombre no es compatible con este enrutador. Para operar a escala o distribuir a terceros hace falta un servicio de aprovisionamiento de credenciales por dispositivo.

Pruebas locales: `node --test tools/cloudflare-worker/tests/router.test.mjs`. Las pruebas no sustituyen una publicación verificada ni una prueba con teléfono físico.
