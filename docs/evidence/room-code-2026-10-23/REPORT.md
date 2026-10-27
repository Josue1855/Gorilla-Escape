# Salas locales por código — 23 de octubre de 2026

Esta revisión sustituye el diseño anterior de un directorio público. Todo el juego y la admisión de salas se ejecutan en la PC. La única conexión intermediada prevista es el túnel cloudflared ya utilizado por el proyecto. No se publicó ningún servicio externo.

El código alfanumérico de cuatro caracteres pertenece a la sesión local y genera la misma admisión breve de un solo uso que el QR. Ambos accesos conviven, respetando el límite de cuatro jugadores y los límites de intentos. Unity muestra el código cuando la conexión está preparada.

La PWA solicita el PIN únicamente al mismo origen desde el que se abrió. Flutter utiliza el origen obtenido del último QR durante la sesión de la app; si aún no conoce la PC, pide escanear su QR. No consulta directorios públicos ni acepta certificados arbitrarios. Con un túnel temporal nuevo será necesario volver a compartir su dirección mediante QR. Cuatro caracteres por sí solos no permiten descubrir una dirección desconocida de Internet.

Se eliminaron el servicio RoomDirectory, su configuración de despliegue y sus adaptadores. No se necesita servidor contratado, dominio ni base de datos pública para este flujo. Las bases de datos existentes del proyecto no se migraron a la nube.

La PWA se verificó sin scroll en 393×852, 320×568, 568×320 y 320×300, con el campo del PIN y el botón visibles. Las evidencias de pantalla siguen siendo válidas. El teclado real de iOS y la instalación en un iPhone físico siguen pendientes.

No se hizo merge, push ni PR. Se preservaron los cambios previos del proyecto. Consulte validation.json para las comprobaciones de esta revisión local.

## Descubrimiento local posterior

Se añadió descubrimiento Bonjour/NSD para encontrar la PC sin escanear primero el QR en Flutter. El flujo y sus límites están documentados en `docs/01-architecture/LOCAL_DISCOVERY.md`. La PWA conserva el acceso por QR y PIN en su origen conocido.
