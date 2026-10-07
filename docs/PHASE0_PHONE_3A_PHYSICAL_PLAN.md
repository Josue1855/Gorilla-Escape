# 3A — Datos pendientes, reutilización del router y validación física

**Preparado, no ejecutado.** 3A IN PROGRESS; #94 OPEN/DRAFT. No cambios de infraestructura ni nuevas funciones. El [preflight](PHASE0_PHONE_3A_PREFLIGHT.md) sigue vigente; este documento concreta obtención de datos y ejecución posterior, sin repetir su análisis.

## Datos confirmados e incorporación al candidato

La evidencia de apoyo Android conserva exactamente **10 PASS, 2 BLOCKED y 1 SKIP** en [results.json](evidence/android-emulator-3a-2026-10-06/results.json). No sustituye aceptación física ni HTTPS público. [Guía](PHASE0_PHONE_3A_ANDROID_EMULATOR.md), harness probado y capturas [Chrome/React](evidence/android-emulator-3a-2026-10-06/native-react-health.png), [error real](evidence/android-emulator-3a-2026-10-06/connection-error.png) y [rechazo TLS](evidence/android-emulator-3a-2026-10-06/tls-rejected.png) revisadas: sólo laboratorio loopback, sin cuentas/credenciales. [Manifest](evidence/android-emulator-3a-2026-10-06/manifest.json) conserva hashes, candidato de ejecución e intentos fallidos. Logs brutos, PKI y discos Android excluidos.

Antes de esta tarea esos archivos existían sólo localmente, no en el head remoto `034ab44aa5b27c1812fc1b5f9f3cc8f7593debf1`. Se incorporan en la misma rama, con commit/push normal y verificación posterior del head remoto. El hash histórico del manifest identifica lo probado, no se reemplaza por un SHA posterior de documentación. No se modifica Unity/Java/React/Shared ni se reejecutan suites históricas para afirmar nuevos resultados. La prueba adicional de SDK ausente permanece separada del conteo nominal.

Inspección adicional sólo lectura: PC Wi-Fi conectada, IPv4 `10.1.125.17/22`, gateway `10.1.124.1`, iguales al preflight; Ethernet sin enlace. Inventario USB actual sin Android/iPhone identificable: no prueba que no estén disponibles. No login/scan del gateway, descubrimiento activo de la LAN, lectura de cuentas de dominios ni elevación administrativa. Modelo del AP/router, firmware, propietario y permisos no pueden deducirse de una ruta IP. SDK/API36 no identifica un teléfono físico.

## Siete datos: qué podemos obtener sin modificar

Todos son indispensables para ejecutar/cerrar el gate; pueden obtenerse después por operador/propietario en modo lectura. Hoy ninguno de los siete está suficientemente confirmado. No pedir contraseñas, tokens, claves privadas, IMEI/serial, SSID personal ni backups completos en el chat.

| Dato | Inspección sólo lectura permitida / modo de identificar | Confirmado hoy / bloqueo |
|---|---|---|
| Router: marca, modelo, revisión hardware y firmware | Leer etiqueta sin publicar claves/serial; propietario abre página Status/System autorizada para firmware. Luego contrastar manual oficial de esa revisión | No identificado; gateway no demuestra fabricante ni funciones |
| Acceso administrativo autorizado | Titular confirma propiedad/permiso y que puede abrir panel con su acceso existente; sólo consultar estado, sin guardar cambios | No confirmado; capacidad técnica de login no equivale a autorización |
| Dominio/subdominio controlado | Titular indica FQDN y confirma control en panel de dominio/delegación. Consultar DNS público del nombre una vez suministrado, sin crear registros | No FQDN confirmado; una búsqueda DNS no prueba propiedad |
| Proveedor DNS | Titular consulta nameservers/hosting de zona; con FQDN conocido se puede consultar NS. Identificar quién puede crear TXT/delegación DNS-01 | No confirmado; registrador y operador DNS pueden ser distintos |
| Android físico | Ajustes → Acerca del teléfono: modelo/Android; Chrome → Ajustes → Acerca de Chrome. USB/ADB sólo si ya autorizado y habilitado; no activar debugging como requisito de jugador | Disponibilidad genérica Android mencionada, sin modelo/versión ni equipo conectado |
| iPhone físico | Ajustes → General → Información: modelo/iOS; Safari integrado con iOS, anotar versión del sistema y navegador utilizado | Disponibilidad genérica iOS mencionada, sin equipo/versiones confirmados |
| LAN de laboratorio sin terceros | Propietario confirma titularidad, usuarios afectados, ventana y posibilidad de retirar WAN conservando AP/DHCP/DNS; consulta topología/status sin desconectar | La Wi-Fi actual no acredita permiso, exclusividad ni desconexión segura |

Opcionales para reducir trabajo: router dedicado ya disponible, PC por Ethernet, exportación sanitizada de configuración, segundo equipo para comprobar aislamiento y captura remota consentida. Comprar router, disponer de Android Studio, habilitar ADB en teléfonos, instalar PWA o Flutter no son requisitos. El test remoto de IPC necesita algún segundo dispositivo, pero no exige comprar equipo adicional.

## Matriz de viabilidad: reutilizar primero

Evaluar capacidades del **modelo y firmware reales**, no sólo publicidad genérica. Cada fila está PENDIENTE. Leer panel autorizado/manual oficial puede demostrar que una función existe; su comportamiento offline exige ensayo posterior.

| Criterio | Evidencia mínima del router existente | Si no cumple |
|---|---|---|
| Propiedad/admin y red controlada | Permiso del titular, administración local accesible, ventana sin terceros | No intervenir; buscar otra red/equipo existente autorizado |
| DHCP + dirección estable PC | Reserva DHCP disponible o asignación estable administrada, sin conflictos | Evaluar router disponible que sí lo permita; no fijar IP a ciegas |
| DNS local exacto FQDN → IPv4 PC | Registro A persistente, servido a clientes por DHCP, sin necesitar upstream/WAN | No configurar DNS en teléfonos; evaluar otro router existente |
| Resolución offline | AP/DHCP/DNS permanecen activos sin WAN, resolución fría del FQDN comprobada | No aceptar como red de juego offline |
| Teléfono → PC | Segmento permitido, aislamiento/captive portal no impiden HTTPS,443 accesible | Excepción específica autorizada o red alternativa; no desactivar seguridad global |
| DNS rebinding | Registro local/posible excepción sólo del FQDN, conservando protección general | Investigar modelo; no deshabilitar rebinding globalmente |
| Exposición acotada | Sin forward WAN/DMZ/UPnP que exponga Java; gestión protegida | Cambios mínimos sólo con permiso, o red alternativa |
| Reproducibilidad/rollback | Valores originales documentables y restaurables; persistencia tras reinicio autorizable | No declarar instalación reproducible |
| Android/iOS stock | Confianza pública y DNS/LAN reales sin cambios obligatorios al teléfono | Registrar fallo/limitación; nunca CA/manual DNS como arreglo del producto |

Orden: A router actual conforme → B otro router existente administrable → C hotspot PC como fallback condicionado al ensayo autorizado ya descrito en preflight. No activar ninguno ahora ni recomendar compra sin demostrar ausencia de recursos reutilizables. VPN/DNS privado estricto/DoH pueden alterar rutas/resolución: no prometer compatibilidad universal ni resolver fallos imponiendo ajustes al jugador.

## Autorizaciones del propietario antes de intervención

| Acción futura | Quién autoriza / límite |
|---|---|
| Entrar al panel para leer estado | Propietario/admin de la red; no credenciales en chat ni login por defecto/bruteforce |
| Leer UFW/nft privilegiados | Administrador de la PC o resumen sanitizado producido por él; lectura no autoriza escritura |
| Reserva DHCP, DNS local, aislamiento/captive portal o excepción rebinding | Propietario del router; valores exactos, backup/rollback y alcance revisados; no sustituir configuración global |
| Retirar/restaurar WAN o reiniciar router | Propietario de la red, ventana sin terceros; no desconectar red compartida |
| DNS público TXT/delegación y emisión/renovación pública | Titular del dominio/zona y responsable TLS; autorización separada antes de emitir; claves privadas sólo almacenamiento local protegido |
| Regla temporal443→8443 y excepción firewall mínima | Administrador PC; adaptar procedimiento a reglas efectivas, preparar rollback; no root Java/setcap global/flush general |
| Preparar estado limpio del navegador, desconectar Wi-Fi/celular para ensayo de ruta | Dueño de cada teléfono de laboratorio, consentimiento sobre acciones concretas; no borrar datos personales ni convertirlo en paso de onboarding |
| Comprar, instalar paquetes o persistir cambios | Autorización separada; no necesario ni realizado en este informe |

Datos a proporcionar: marca/modelo/revisión/firmware, confirmación de permiso administrativo, FQDN y control confirmado, proveedor DNS, Android/modelo/OS/Chrome, iPhone/modelo/iOS, red/ventana de ensayo autorizada. Respuestas «desconocido» son válidas; no se infieren permisos a partir de que la PC está conectada.

## Procedimiento físico Android/Chrome e iPhone/Safari

Estado de **todos los casos físicos: NOT RUN / BLOCKED por prerrequisitos**. Misma matriz para ambas plataformas; resultados individuales PASS/FAIL/SKIP/BLOCKED, nunca trasladar PASS del emulador. Reutilizar [validación vigente](PHASE0_PHONE_3A_VALIDATION.md) y [Linux443](PHASE0_PHONE_3A_LINUX_443.md); no ejecutar configuración como parte de este informe.

1. **Precondición autorizada:** router conforme con LAN independiente, reserva PC/DNS local preparados, FQDN controlado y certificado público previo vigente con cadena completa/SAN correcto. Origen `https://<FQDN>/` en443; Unity inicia Java normal8443 e IPC sólo127.0.0.1. Confirmar artefactos, reglas de operador/reversión y hora correctas. Sin emisión ACME ni Internet necesario durante prueba. Si falta algo, BLOCKED sin instalar CA o degradar HTTP.
2. **Inventario de laboratorio:** registrar modelos/OS/browser/router/firmware, versiones/commit/hash JAR/Player y topología. Verificar que no se preparó confianza personalizada para ese FQDN. No borrar trust de un teléfono personal: usar equipo de referencia limpio o registrar limitación. No certificados/perfiles instalados para participar.
3. **Primera carga realmente sin WAN:** antes de la primera visita al FQDN, el operador retira WAN en red autorizada, manteniendo Wi-Fi/DHCP/DNS. Usar navegador/origen sin visitas/cache/SW anteriores; si no puede acreditarse ese estado, BLOCKED para primera carga. Si se necesita limpieza específica del origen, sólo dispositivo de laboratorio consentido; no borrar historial global ni exigir limpieza al jugador. No precargar sitio/instalar PWA con Internet.
4. **Separar rutas:** ensayo inicial aislado sin salida celular, sólo en equipo de laboratorio autorizado, para probar ruta LAN y no confundir Internet celular con offline. Repetir después con configuración stock y datos móviles activos; conservar ambos resultados. Desactivar datos es condición de instrumentación del primer ensayo, **no requisito de producto**. No usar DevTools Offline, ADB reverse, hosts override, DNS manual, VPN de test ni proxy para obtener PASS físico.
5. **Abrir URL manual:** conectar a Wi-Fi de laboratorio y escribir `https://<FQDN>/` en Chrome/Safari. QR aún fuera3A. Esperado: URL443, shell/assets locales, contexto seguro, HTTPS sí, cero warnings/excepciones/configuración del browser. Una advertencia TLS es FAIL; no avanzar mediante Proceed. Acreditar DNS local frío y conexión al Java LAN con evidencia del operador/servidor, no sólo captura de página o cache.
6. **Backend real por plataforma:** ≥10 health mediante botón, misma instancia que Java y contador creciente/no-store; registrar tiempos y fallos por petición. ≥5 recargas/reaperturas, incluyendo cerrar/abrir navegador. Evidencia del endpoint y llegada real a Java; no declaración de latencia gameplay. Registrar SW y ausencia de dependencias externas necesarias con WAN ausente.
7. **Fallos básicos:** pérdida/vuelta de Wi-Fi consentida, recuperación manual por botón/recarga. Cierre Unity → EOF → Java exit0: al solicitar health debe verse error ≤5s, nunca éxito de API cacheado. Relanzar sólo mediante lifecycle existente, sin nuevo recovery automático. Logs y listeners confirman LAN8443 + IPC loopback, sin HTTP móvil extra; desde segundo dispositivo443 accesible y TCP IPC inaccesible.
8. **Compatibilidad stock:** repetir primera carga/ruta y health con datos móviles activos; anotar selección Wi-Fi/celular y cualquier fallo. Registrar DNS privado/VPN/DoH ya existentes; perfiles no disponibles SKIP como variantes, sin convertirlos en PASS ni alterar teléfono del jugador. No instalar/desactivar VPN ni cambiar DNS para disimular incompatibilidad. Un fallo en baseline soportado impide cerrar3A; variante especial requiere limitación/decisión explícita.
9. **Cierre:** Unity exit0, Java exit0, procesos propios residuales0. Operador restaura sólo WAN/reglas/configuración que cambió, mediante backup/handles propios; no flush general ni cambios ajenos. Sanitizar capturas/logs, no incluir SSID/serials, cuentas, claves, tokens, passwords ni PKCS12. Publicar resultados separados Android/iPhone con fechas/evidencias; si hay fallo, preservar intento y repetir sólo casos afectados después de corregir causa autorizada.

Plantilla por caso: plataforma/modelo/OS/browser; caso; estado; WAN/celular; navegador frío acreditado; HTTPS/FQDN; instancia/contador; duración/error; evidencia sanitizada; causa/bloqueo. Firmware/topología/artefactos comunes en cabecera. Capturas aisladas sin primera carga fría, resolución offline y health real no cierran el gate.

## Estado y bloqueos restantes

Dominio/proveedor/control DNS-01; certificado público y renovación previa; router/modelo/firmware/admin/DNS offline/reserva/aislamiento; red de pruebas autorizada; reglas efectivas PC y autorización443; Android/iPhone físicos/versiones. No compras/instalaciones/emisión/configuración ejecutadas. Las observaciones de PC no resuelven esos bloqueos.

**Emulador: 10 PASS / 2 BLOCKED / 1 SKIP conservados. #94 DRAFT; 3A IN PROGRESS; 3B/4A/4B/4C NOT STARTED; Fase0 IN PROGRESS; Fase1 NOT STARTED.** Detenerse tras informe; no iniciar siguiente bloque.
