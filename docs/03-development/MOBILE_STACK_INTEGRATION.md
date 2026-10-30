# Integración de red, PWA, SQLite, Flutter y ejecutable

Fecha: 2026-10-27. Rama `feature/saul-jungle-party-alpha`; revisión en [PR #131](https://github.com/Josue1855/Gorilla-Escape/pull/131). Sin merge a develop ni cambios en main. Recuperación selectiva desde `144da9a` y `fcce8a0` de la rama del tronco; conserva los siete juegos y los recursos de Saúl.

## Componentes

| Bloque | Implementación |
| --- | --- |
| Selección LAN | Selección explícita prioritaria. Con varias interfaces privadas vinculadas, prefiere la dirección elegida por la ruta del sistema. UDP connect sólo consulta la ruta: no envía paquetes ni requiere Internet. Si no hay coincidencia inequívoca exige selección manual. |
| PWA | Wake Lock mientras está visible, reutilización de permisos concedidos, diagnóstico de conexión sin datos sensibles y contexto de juego caducable. Se conservan confirmar/recentrar sobre muestras reales, suspensión y límite de 50 Hz. |
| Información en teléfono | Los seis controladores de los siete juegos publican fase y métricas reales mediante MotionSession/PortableServiceHost. Pausa técnica se refleja como Paused. Presentación v2 a máximo 5 Hz por IPC autenticado y ACK de HEARTBEAT; nunca por cada muestra motion. |
| SQLite | Backend existente empaquetado como Java/backend.jar, ejecutado por el Java técnico con el mismo JRE. Dirección 127.0.0.1 y puerto efímero; comprobación GET de API, arranque máximo 30 s y cierre de hijos propios. No expone PostgreSQL ni añade Docker/Java del sistema al usuario final. |
| Flutter | Sensores nativos, escáner QR, WebRTC fiable/control y no fiable/motion, pantalla despierta, ciclo de vida y contexto de siete juegos. No usa navegador ni transporte alternativo. |
| Ejecutable | Build explícito de las diez escenas, Connection primero, en Unity/Builds/JungleLinux. Marca del build con hashes de scripts/escenas/contratos/configuración impide lanzar un Player antiguo como si fuera actual. Launcher prepara servicios y ejecuta sólo Unity. |

Los cambios en JavaProbeSupervisor se limitan a serializar el DTO Shared de presentación sobre su IPC existente. No se reemplaza su propietario ni se cambia su supervisión. PlayersBackend es un hijo opcional del Java técnico, separado de la adquisición de sensores. Se auditaron la autoridad de peer/HELLO, los límites HTTP/JSON, la limpieza de procesos y la persistencia antes de recuperarlo.

El hijo de persistencia no hereda credenciales GORILLA del IPC/túnel; recibe sólo su directorio de datos. La preparación del backend es asíncrona, con logs acotados y comprobación de API; su fallo no crea un guardado ficticio ni bloquea la mecánica. Tras un fallo del proceso se recupera con el reinicio explícito de la sesión, sin bucle de reinicios. Cierre coordinado dentro del presupuesto existente del supervisor padre.

La fachada POST /mobile/profile exige peer conectado y HELLO confirmado, valida el token y normaliza el nombre. Cada peer puede confirmar un registro; peticiones confirmadas repetidas devuelven el mismo perfil. Un resultado incierto no se repite automáticamente. Un backend todavía indisponible devuelve 503 antes de intentar escribir. No es login, recuperación de cuenta ni persistencia de resultados deportivos. [Contrato de presentación/perfiles](../../Shared/Protocol/mobile/game-presentation.md).

## Persistencia y backend revisado

Se integra con el [repositorio Backend](https://github.com/Josue1855/Gorilla-Escape-Backend), cuyo develop revisado es `c83b8343f1e37c405b127304567e149a1416eddb`. Las fuentes src/pom de la copia local `638c932` son idénticas a ese develop; no se modificó ni se fusionó el otro repositorio. CI compila y prueba el commit fijado y ejecuta el test real del proceso SQLite con ese JAR.

Los datos se conservan fuera del paquete: GORILLA_DATA_DIR explícito o ~/.gorilla-escape/data. El backend crea/migra players.sqlite mediante su configuración existente. Actualizar el juego/JAR no elimina ese directorio. No se ofrecen credenciales ni puerto de base de datos al teléfono; sólo el backend abre el archivo SQLite. Los tests usan directorios temporales, no la base real del usuario.

## Preparación y arranque de desarrollo

Compilar la PWA y el servidor desde este repositorio, y el backend desde su propio repositorio:

```sh
npm --prefix PWA ci
npm --prefix PWA run build
Server/mvnw -B -f Server/pom.xml verify
python3 tools/prepare_players_backend.py --jar /ruta/al/backend/target/backend-0.1.0-SNAPSHOT.jar
./run-game.sh --prepare-only
```

El launcher reutiliza por defecto el paquete FoundationLinux para Java/runtime, Vision, Tools y backend.jar. Se puede indicar --runtime, --package y --backend-jar; no descarga ni instala durante el arranque. Cloudflared conserva AUTO y su alternativa LAN si falta o falla; sólo el modo forzado lo exige. El paquete de desarrollo utiliza enlaces a los runtimes existentes: no es un instalador redistribuible.

En Unity 6000.6, **Gorilla Escape → Build Seven Games Linux** genera el Player actual, las diez escenas y game-build.json. Después ejecutar ./run-game.sh. Sin la marca o con hashes distintos el launcher pide recompilar. El build técnico de FoundationSetup permanece disponible y no se reutiliza como evidencia de los siete juegos.

Al abrir directamente el Player empaquetado, PackagedRuntimeEnvironment configura las rutas locales, AUTO, confianza local y un secreto efímero de operador, respetando valores explícitos. El Editor conserva su opt-in y exclusión de tests. No se inicia hardware desde pruebas de código. El ensamblador de distribución existente admite --players-backend para incluir el JAR; la aceptación de instalador/PC limpia sigue pendiente.

## Validación y límites

[Resultados y evidencia](../evidence/mobile-stack-2026-10-27/README.md). La prueba conjunta utiliza navegador y RTC reales, API real y SQLite, con contexto de Unity suministrado como fixture. No ejecuta Unity ni sensores físicos. Confirma siete contextos por ACK, autoridad del perfil, una fila persistente y el mismo UUID tras reiniciar el backend.

No se recuperan mDNS, PIN de sala, el menú anterior del tronco ni sus vías de entrada sin autorización de InputFusion. La publicación de resultados/rankings al backend sigue fuera del incremento. La compilación completa/importación y pruebas de Player con teléfono/cámara reales, iOS y distribución Windows siguen pendientes. El APK debug generado no constituye una release firmada ni una instalación física.
