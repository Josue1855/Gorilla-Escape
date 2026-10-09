# T002/#11 — Arquitectura objetivo por plataforma y capacidades

**DEC-014 Accepted — decisión explícita PO, 2026-10-08.** Los equipos concretos son **REFERENCE TEST HARDWARE**, nunca ONLY SUPPORTED HARDWARE. Arquitectura objetivo genérica por plataforma/capacidades; matriz de compatibilidad física pendiente de validación. La ausencia de un modelo Android o router identificado no bloquea este inventario arquitectónico.

## Acceptance Criteria enmendados por PO

1. Inventariar PC/cámara de referencia y plataformas móviles objetivo, incluyendo OS, familias de navegador y capabilities requeridas.
2. Definir hardware de referencia para reproducción/QA sin limitar compatibilidad; documentar restricciones generales LAN y política de registrar modelos/versiones exactos de dispositivos físicos en sus gates de hardware.

**AC1 PASS:** inventario y objetivos definidos en la tabla siguiente.
**AC2 PASS:** política de QA/capacidades/LAN y restricciones definidas, sin prometer compatibilidad universal ni aprobar pruebas físicas no ejecutadas.

| Elemento | Referencia / objetivo | Alcance y evidencia |
|---|---|---|
| PC QA Phase0 | Dell Latitude5540, Pop!_OS24.04LTS x86_64 | Referencia de medición, no plataforma exclusiva; Windows/macOS futuros no descartados |
| CPU/GPU/RAM | i7-1355U, Iris Xe, ≈31.03GiB utilizables | Identificación comprobada; SMBIOS no legible, sin DIMMs inventados |
| Cámara QA | Integrated_Webcam_FHD | 640×480/17.61FPS observado; >=30FPS NOT ACHIEVED, T016 permanece pendiente |
| iOS QA disponible | iPhone15, iOS27 declarado PO, Safari incluido con iOS | Referencia, no único iPhone soportado; versión exacta OS/browser/fecha en ejecución física |
| Android objetivo | Android moderno + Chrome estable | Modelo QA todavía UNKNOWN; elección/OS/Chrome exactos al ejecutar T019, no emulación como físico |
| iOS objetivo | iPhone/iOS moderno + Safari estable incluido en iOS | DeviceMotion/Orientation permiso por user gesture cuando corresponda |
| Red objetivo | Wi-Fi LAN estándar existente | PC/teléfonos misma LAN, comunicación local permitida; Internet opcional; juego independiente WAN |
| Router QA | Fabricante/modelo/firmware UNKNOWN | Metadatos de ejecución, no restricción del producto ni bloqueo T002 |

## Capacidades requeridas y degradación

En ambas plataformas: contexto seguro, WebRTC DataChannel, touch y LAN accesible. DeviceMotion, DeviceOrientation y rotationRate dependen de hardware/navegador/permisos. La arquitectura requiere detección runtime de disponibilidad y permisos, sin inventar sensores ausentes. **No se implementa ni certifica aquí esa detección.**

- FULL: capacidades necesarias para la modalidad disponibles y autorizadas; no implica rendimiento físico validado.
- LIMITED: base de conexión/touch disponible pero alguna capacidad opcional o alternativa de modalidad ausente; comunicar límites, no fingir paridad completa de movimiento.
- INCOMPATIBLE: falta una capacidad necesaria para conectar o participar en la modalidad seleccionada. No afirmar que todos los Android/iPhone/router funcionan.

La necesidad de cada señal se fija por modalidad; no se inventa requisito de sensor universal ni se acepta LIMITED como equivalente a FULL. Contexto seguro y permisos siguen exigidos; DTLS no convierte HTTP en secure context. Sin flags/bypass TLS, certificados obligatorios al jugador ni cloud/backend/STUN/TURN público obligatorio. Unity autoridad; Java backend local; IPC127.0.0.1. Cámara gameplay sólo PC.

## Política de QA y obligaciones conservadas

T002 define objetivos e inventario de referencia; no prueba compatibilidad física. T019/#33 y gates físicos correspondientes conservan Android físico+iPhone físico, 1 y4 controles, conexiones/reconexiones, fallos reales y P95real. Registrar por ejecución fecha/dispositivo/OS/browser exactos, capacidades observadas, topología/restricciones y metadatos de router disponibles. No congelar parches, no sustituir físico por Chrome PC/emulación/replay. No trasladar ni rebajar >=30FPS, PC limpia o pruebas humanas. Restricciones observadas: captura17.61FPS y fallos anteriores iPhone→PC sin causa única confirmada. Aislamiento/red actual no verificados; UNKNOWN no es PASS.

#11 cumple alcance documental enmendado: CLOSED/completed/Done verificado en GitHub; retirados blocked y needs-device-test. #12 OPEN/PO Review; dependencia #11 satisfecha e intacta, blocked retirado y needs-decision conservado; sin iniciar trabajo ni aceptar sus propios AC. Software/Lab COMPLETE; E0/milestone OPEN; Fase1 NOT STARTED. Sin código nuevo, instalaciones, pruebas físicas, commit/push.

## Historia anterior a DEC-014

La interpretación y los faltantes anteriores quedan **superseded por DEC-014**; se conservan para trazabilidad, no son blockers vigentes de T002:

# T002/#11 — Inventario de referencia oficial y pendientes

Fecha: 2026-10-08. Base integrada: `65aecf8c3198ed960a43eaa50cfcc495f4562b53`. Decisiones oficiales explícitas del PO; inspección sólo lectura. **#11 OPEN / Validation; AC1 PARTIAL, AC2 PARTIAL.** Sin producto nuevo, suites, cambios de red ni captura de cámara.

## Decisiones PO registradas

- **OFFICIAL REFERENCE PC — Phase 0:** Dell Latitude5540, Pop!_OS24.04LTS x86_64, Intel Core i7-1355U, Intel Iris Xe; usar RAM real comprobable. Windows/macOS no se descartan como plataformas futuras.
- **OFFICIAL REFERENCE WEBCAM — Phase 0:** Integrated_Webcam_FHD integrada. Conservar 640×480 y17.61FPS observados: objetivo >=30FPS **NOT ACHIEVED**.
- **OFFICIAL IOS REFERENCE DEVICE:** iPhone15 con Safari. PO proporciona iOS exacto **27**; se registra como dato declarado por PO, sin inventar versión menor/build. Safari corresponde al sistema instalado, sin versión artificial independiente.
- Familias de navegador: Safari estable incluido en iOS oficial y Chrome estable instalado en Android oficial. No congelar parches permanentemente. Cada ejecución física registra dispositivo, fecha, OS exacto y navegador exacto.
- Android físico: PO declara **OFFICIAL ANDROID REFERENCE DEVICE — Phase 0**, pero fabricante/modelo/Android/Chrome contienen `[PEGAR]`: falta identificar el dispositivo al que aplica la designación, sin inventarlo. Emulador/Chrome PC/clientes sintéticos no lo sustituyen.
- Modelo de red oficial: **existing local Wi-Fi LAN**, misma LAN PC/teléfonos; sin hotspot obligatorio, WAN opcional, gameplay local, sin cloud backend/STUN/TURN público obligatorio. Router de referencia será el físico empleado en las pruebas; identidad pendiente.

## Matriz de T002

| Elemento | Identificado | Oficial PO | Falta para T002 |
|---|---|---|---|
| Dell Latitude5540 | Sí | Sí | Nada de identificación/designación |
| Pop!_OS24.04LTS x86_64 | Sí | Sí | Kernel observado7.1.5-76070105-generic; nada pendiente de designación |
| CPU i7-1355U | Sí | Sí | Nada;10cores/12procesadores lógicos observados |
| GPU Intel Iris Xe | Sí | Sí | Nada de identificación; rendimiento no inferido |
| RAM | Sí utilizable/parcial instalada | Sí, capacidad comprobable | MemTotal32538860KiB ≈31.03GiB; SMBIOS denegado. Capacidad instalada exacta no comprobada, **no bloqueo adicional** por decisión PO |
| Integrated_Webcam_FHD | Sí | Sí | Nada de identificación/designación. 17.61FPS<30 queda pendiente en T016, no inventar PASS |
| iPhone15 | Sí, declarado PO | Sí | Nada de modelo/designación |
| iOS exacto | Sí, proporcionado PO | Sistema del iPhone oficial | 27; no exigir minor/build inexistentes en la evidencia, ni inventarlos |
| Safari | Sí, familia | Sí | Versión correspondiente al iOS realmente instalado, registrar en ejecución; no versión artificial |
| Android físico | No | Designación PO registrada, identidad pendiente | Fabricante/modelo reales; `[PEGAR]` es placeholder |
| Android OS | No | Pendiente identidad del dispositivo designado | Versión instalada; `[PEGAR]` no aporta dato |
| Chrome Android | No | Familia estable aprobada | Versión instalada; `[PEGAR]` no aporta dato; Chrome PC no equivale |
| LAN | Sí, modelo y snapshot PC | Sí | Restricciones conocidas registradas; aislamiento/conectividad actual UNKNOWN, no crear gate físico nuevo para este inventario |
| Router | Gateway observado; identidad UNKNOWN | Router físico usado será referencia | Fabricante/modelo/firmware UNKNOWN; no requerido expresamente por AC1/AC2, no blocker adicional |

## Inspección y restricciones reales

PC Wi-Fi `wlp0s20f3`, IPv4 temporal `10.1.125.17/22`, gateway DHCP `10.1.124.1`. No inferir modelo/propietario del router desde gateway, IP o MAC. Una consulta HTTP a la raíz del gateway, sin autenticación, timeout4s/sin redirects, no aportó identificación inequívoca; no exploración de otros hosts/rutas ni configuración.

Intento `dmidecode --type 17` sin sudo: Permission denied para SMBIOS y /dev/mem. No usar elevación ni inventar DIMMs. RAM utilizable anterior basta para el alcance PO; actualizar en una ejecución futura si cambia.

[Webcam histórica](evidence/camera-fusion-2026-10-08/hardware.json): modos anunciados no equivalen a cadencia lograda. [Audit](PHASE0_FINAL_AUDIT.md): 17.61FPS y física móvil/humana pendiente se conservan. Los intentos anteriores iPhone→PC fallaron; no causa única comprobada. No afirmar aislamiento o firewall como diagnóstico definitivo ni compatibilidad LAN universal. Navegadores móviles actuales no inspeccionables desde esta PC sin evidencia del dispositivo; no usar desktop Chrome155.0.8059.39 para inferir Chrome Android.

## Acceptance Criteria reales — reevaluación PO

1. **Inventario con OS, CPU/GPU/RAM, cámara y modelos/OS/navegadores móviles: PARTIAL.** PC/webcam/iPhone15/iOS27/Safari identificados. Único faltante concreto: ficha del Android físico designado (fabricante/modelo, Android instalado y Chrome instalado); los tres campos recibidos son `[PEGAR]`.
2. **Definir hardware oficial y registrar restricciones reales: PARTIAL.** Referencias PC/webcam/iPhone/LAN y política de navegadores aceptadas; designación Android registrada pero sin identidad efectiva. Restricciones reales y desconocidas ya documentadas, sin exigir validación de transporte para cerrar un inventario. El mismo faltante Android impide asociar la referencia oficial a hardware concreto.

## Único faltante concreto

**Ficha real del Android físico oficial: fabricante/modelo, versión Android y versión Chrome.** No hay otros bloqueos añadidos: iOS27 aceptado como dato proporcionado por PO; router/modelo/firmware y restricciones LAN no comprobadas se registran UNKNOWN. El AC no exige versión de firmware ni pruebas completas de conectividad; esas pruebas pertenecen a los gates físicos correspondientes. Tampoco se exige RAM SMBIOS tras la decisión de usar capacidad comprobable.

#11 permanece OPEN/Validation. blocked y needs-device-test corresponden únicamente al inventario del Android físico aún no identificado. No marcar AC como cumplidos ni sustituir Android físico por emulación. #12/T003 sigue blocked by #11 y no se inicia. Fase1 NOT STARTED. Sin commit/push.
