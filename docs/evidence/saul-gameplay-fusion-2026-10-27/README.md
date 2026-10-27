# Mecánicas con InputFusion — 2026-10-27

[Arquitectura, matriz de los seis juegos y ensayo pendiente](../../02-gameplay/JUNGLE_FUSED_INPUT.md).

- [Resultados](results.json): 71/71 pruebas C#, 10 000 muestras de ejecución prolongada, enlace de seis escenas y compilación de fuentes contra referencias reales de Unity 6000.6.
- [Salida real del harness](gameplay-tests.log): escenarios ejecutados, sin capturas fabricadas.
- [Hashes de las fuentes](source-sha256.json): recibo de runtime y controladores comprobados.

Los escenarios usan los algoritmos reales de InputFusion/asociación/gestos con fixtures sintéticos. No ejecutan física, animaciones, callbacks de Unity ni hardware. El intento nativo de EditMode desde terminal terminó antes de ejecutar pruebas, con salida 198 por licencia no reconocida en ese contexto; el usuario sí había mostrado Unity Student en Hub. No se declara aceptación física ni licencia inexistente en Hub.

Se preservan contratos Shared, supervisores, Java, PWA, escenas y recursos visuales. Las comprobaciones de fuentes Editor/Player no sustituyen importación completa, un nuevo Player ni el ensayo de teléfono/cámara.
