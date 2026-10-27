# Verificación del séptimo juego: Desafío del Tronco

Fecha: 2026-10-27. Rama: `feature/saul-jungle-party-alpha`. [Diseño y aceptación pendiente](../../02-gameplay/LOG_BALANCE_SAUL.md).

Resultados realmente ejecutados:

| Comprobación | Resultado |
| --- | --- |
| Harness C# real con fixtures sintéticos | 101/101 PASS; incluye siete perfiles, calibración del tronco, inercia, caída/meta y feedback con 250 ms de retraso |
| Escenas y dependencias de assets | PASS, siete escenas con controladores fusionados; Resources tipados, GUID de prefabs/materiales y Read/Write del tronco resueltos |
| Fundación/metadatos | PASS, 419 GUID únicos, contratos independientes del motor |
| Comprobación de tipos contra DLL Unity 6000.6/glTFast, variante Editor | PASS: contratos 6 fuentes, runtime 37, gameplay 15, herramientas Editor 3+3 |
| Comprobación de tipos, variante Player | PASS: contratos 6 fuentes, runtime 37, gameplay 15 |
| Modelo original del tronco | Copia idéntica al archivo de la rama de equilibrio; origen y SHA-256 en provenance.json |
| Unity nativo, visual y teléfono/cámara | PENDIENTE; no se han fabricado capturas ni resultados físicos |

Los logs del harness y validadores están en este directorio. Los logs de compilador, con sus advertencias, se conservan fuera del repositorio en el respaldo local `saul-log-balance-20261027/type-check-editor` y `type-check-player`. Una comprobación de tipos no ejecuta Unity ni demuestra importación/renderizado correctos.

Reproducción desde la raíz:

```sh
dotnet run --project tools/integration/jungle-phase0/JunglePhase0.Checks.csproj --configuration Release
python3 tools/integration/validate_jungle_gameplay.py
python3 tools/validate_foundation.py
python3 tools/github/validate_management.py
python3 tools/integration/compile_jungle_phase0.py --editor-data /ruta/al/Editor/Data --project-scripts
python3 tools/integration/compile_jungle_phase0.py --editor-data /ruta/al/Editor/Data --project-scripts --player
git diff --check
```

Este incremento añade únicamente la escena del tronco, sus dos modelos/tres texturas originales y reglas/presentación adaptadas. La selva y el material reutilizan los recursos ya existentes de Saúl. No cambia las escenas/arte de los otros seis juegos, los supervisores ni Shared. El código permanece en la rama de trabajo y el PR de revisión; no representa una integración aprobada en develop/main.
