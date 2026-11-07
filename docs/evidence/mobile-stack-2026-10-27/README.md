# Integración móvil — evidencia del 27 de octubre de 2026

Rama `feature/saul-jungle-party-alpha`, PR [#131](https://github.com/Josue1855/Gorilla-Escape/pull/131). [Arquitectura y reproducción](../../03-development/MOBILE_STACK_INTEGRATION.md).

| Validación ejecutada | Resultado |
| --- | --- |
| PWA | 86/86 pruebas y build Vite aprobados |
| Java técnico completo con backend real | 132 casos: 127 ejecutados sin fallos, cinco omitidos por requisitos de entorno |
| Backend SQLite existente | 17/17 y empaquetado aprobados; fuentes src/pom idénticas al develop fijado en CI |
| Prueba conjunta navegador/RTC/API/SQLite | PASS: siete contextos, perfil autenticado, rechazo de perfil no autorizado/nombre inválido, duplicado confirmado sin otra fila, persistencia del mismo UUID tras reiniciar backend |
| La misma prueba con JRE empaquetado | PASS: Java 21.0.12.1, cuatro pruebas ejecutadas sin omisiones; confirma compatibilidad del backend con el runtime incluido |
| Flutter | Análisis sin incidencias, 13 pruebas aprobadas y APK debug Android generado |
| C# puro | 106/106: incluye contrato v2, copias independientes, configuración empaquetada y preservación de permisos/secretos explícitos |
| Tipos con referencias Unity 6000.6/glTFast | Editor: contratos 7, runtime 38, gameplay 15, herramientas 3+3; Player: 7/38/15. Sin errores de compilación de fuentes |
| Preparación/launcher | Diez pruebas y preparación real aprobadas; rechaza Player sin marca o con fuentes distintas |
| Wiring/metadata | Siete escenas con entrada fusionada y presentación; 421 GUID únicos |
| Unity nativo / Player actual | PENDIENTE: proyecto abierto impidió el primer intento; copia aislada terminó con salida 198 antes de compilar por contexto de licencia |
| Dispositivos físicos / iOS / instalador y Windows | NOT RUN en este incremento |

[Resumen y hashes](validation.json). [Prueba real de RTC y SQLite](rtc-sqlite.json). [Repetición con JRE empaquetado](packaged-jre-rtc-sqlite.json).

El laboratorio usa un navegador Chrome y WebRTC reales, PublicIngress real y backend/SQLite reales con directorio temporal. El contexto que normalmente publica Unity se inyecta como fixture por la API Java validada. La página y señalización del laboratorio usan HTTP loopback explícito; esta prueba no acredita confianza HTTPS, ejecución de Unity, cámara ni movimiento físico. No se fabricaron capturas ni se reinterpretaron las pruebas históricas de la rama del tronco como resultados actuales.

Los logs completos y los intentos de Unity se guardan fuera de Git en el respaldo local `mobile-integration-20261027`. La sesión abierta de Unity se conserva. El ejecutable previo de FoundationLinux no se presenta como el Player actualizado de siete juegos; la preparación nueva usa JungleLinux y exige un build marcado de las fuentes actuales.

Para reproducir los checks sin hardware:

```sh
npm --prefix PWA test
npm --prefix PWA run build
Server/mvnw -B -f Server/pom.xml -Dgorilla.test.playersJar=/ruta/backend.jar verify
dotnet run --project tools/integration/jungle-phase0/JunglePhase0.Checks.csproj --configuration Release
python3 -m unittest discover -s tools/integration -p 'test_*launcher.py' -v
python3 tools/integration/validate_jungle_gameplay.py
python3 tools/validate_foundation.py
python3 tools/github/validate_management.py
```

En Companion ejecutar `flutter analyze`, `flutter test` y `flutter build apk --debug`. Para el laboratorio conjunto, proporcionar GORILLA_TEST_NODE, GORILLA_TEST_PLAYWRIGHT, GORILLA_TEST_CHROME y GORILLA_MOBILE_INTEGRATION_REPORT, y ejecutar MobilePersistenceLabTest con el JAR real. Para repetir con el JRE incluido, añadir `-Djvm=/ruta/paquete/Java/runtime/bin/java` al comando Maven. Son opt-ins de desarrollo, sin instalaciones requeridas al usuario del juego.
