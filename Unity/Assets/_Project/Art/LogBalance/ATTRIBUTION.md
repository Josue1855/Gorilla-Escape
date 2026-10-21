# Modelos proporcionados para Desafío del Tronco

Los tres originales están en `Sources/`, sin modificaciones. Los nombres, fuentes y licencias siguientes proceden de `asset.extras` dentro de cada GLB. Sus hashes y materiales se conservan en `Resources/LogBalance/asset-manifest.json`.

| Modelo | Autor | Fuente | Licencia incluida |
|---|---|---|---|
| trunk | gashtan | [Sketchfab](https://sketchfab.com/3d-models/trunk-22e47e88a70f4f6e98dd35d3d146b657) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) |
| Low poly trees | Aditya Graphical | [Sketchfab](https://sketchfab.com/3d-models/low-poly-trees-51cae4a194344e8bbfbd0a4cff205f76) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) |
| Castaway / Vagabundo | katarzynaszemla | [Sketchfab](https://sketchfab.com/3d-models/castaway-6a481c17d68a4a138f726ccb76caedff) | [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/) |

Adaptaciones: conversión GLB → FBX con Blender 4.0.2, escala/pivotes, separación del pack de árboles, extracción de texturas, materiales mates y poses procedurales Unity sobre el rig existente de Castaway. No se atribuye a los autores originales la creación o aprobación de estas adaptaciones.

`tools/art/prepare_log_balance.py` reproduce los derivados; **Gorilla Escape → Log Balance → Rebuild Art Prefabs** reconstruye materiales y prefabs mediante el editor. Unity genera sus GUID, referencias y metadatos. Los créditos también se distribuyen como TextAsset y están disponibles desde la pantalla de la prueba.
