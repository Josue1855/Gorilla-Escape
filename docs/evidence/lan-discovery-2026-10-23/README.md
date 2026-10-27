# Evidencia de descubrimiento LAN

`validation.json` conserva los resultados de la ejecución del2026-10-23. `avahi-discovery.sanitized.log` deriva del registro Avahi de esa misma ejecución: se sustituyeron únicamente interfaz, hostname/IP locales y sufijo de sesión por marcadores explícitos. Se conservan eventos de descubrimiento/resolución/retirada, puerto, tipo y TXT. No se volvió a ejecutar esa prueba LAN en la preparación del commit.

El archivo original `avahi-discovery.log` permanece local, excluido de Git. El origen trycloudflare de esta prueba era un dato de anuncio de laboratorio; no se abrió ni se creó ese túnel, como explica el documento de descubrimiento.
