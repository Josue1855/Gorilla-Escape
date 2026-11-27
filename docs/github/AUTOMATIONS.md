# Gorilla Escape — Automatizaciones CI/CD

## Organización

- Unity/host monorepo: los workflows Foundation y Management existentes siguen en uso.
- PWA independiente: `Josue1855/Dise-o_pwa` (fork de trabajo); el upstream de Hiram
  es `Quintana-117/Dise-o_pwa` y requiere revisión/merge por el mantenedor.
- Backend independiente: `Josue1855/Gorilla-Escape-Backend`, rama base `develop`.

Cada repositorio usa su propia validación y Dependabot para cambios de seguridad.
No se sobrescriben flujos ni ramas de desarrolladores existentes.

## Nuevos flujos

| Workflow | Disparador | Resultado |
| --- | --- | --- |
| cross-repository | Lunes, manual o cambio al contrato/workflow en PR | Compila React/Java, ejecuta tests backend, comprueba OpenAPI y anota SHAs; **no** prueba integración HTTP frontend-real |
| developer-deliverables | Manual o tag `vX.Y.Z` | JAR del host, fuentes Unity y documentos con SHA; **no** compilación Unity Player |
| security | PR, push, semanal | `npm audit` de dependencias de producción |
| issue-triage | issue nuevo/editado/reabierto | Etiqueta sólo títulos con `[bug]`, `[task]`, `[feature]`, `[docs]` |
| Dependabot | Semanal | PRs de actualización Maven, npm y Actions contra develop |

El empaquetado principal compila el JAR con `-DskipTests` para no repetir la suite\nWebRTC dependiente de audio nativo del runner; los tests siguen siendo requisito\nen el workflow **Foundation validation** y deben estar verdes en el commit\nrevisado antes de crear un tag. No considerar el artefacto como certificado\nsi Foundation falla.\n\nVersionado: `vX.Y.Z` para snapshot principal, `backend-vX.Y.Z`
para backend, `pwa-vX.Y.Z` para frontend. Crear tags únicamente a partir de
commits revisados y validados. Nunca publicar un snapshot como ejecutable jugable.

## Limitaciones y seguridad

- Unity 6000.3.23f1 requiere instalar editor y configurar licencia y runner
  para compilar realmente, así como pruebas EditMode/PlayMode. Flutter se
  incorporará a CI cuando el proyecto/ruta de compilación esté verificado.
- Los resultados P50/P95 del backend son reportes informativos de HTTP
  local; no prueban LAN ni latencia Unity↔Java.
- La integración cruzada se ejecuta en horarios/manual o al cambiar contrato
  del monorepo; no está conectada automáticamente a pushes de otros repositorios.
  Para notificación inmediata entre repos se necesita un token o GitHub App de
  permisos mínimos y un evento `repository_dispatch`. No registrar secretos
  ni otorgar acceso `contents:write` a jobs de PR.
- El Project #1 ya tiene automatizaciones nativas del repositorio Unity.
  Agregar reglas Auto-add para cada repositorio de forma independiente desde
  Project Settings requiere autorización del Project; este PR no toca sus
  permisos, asignaciones, estados ni issues existentes.
- Las pruebas estáticas y build NO certifican un juego completo o la conexión
  física de teléfonos. Ejecutar pruebas móviles/Unity antes de release jugable.
- GitHub branch protection y required checks se configuran en Settings
  después de comprobar una ejecución verde. No agregar required checks nuevos
  hasta que la rama base tenga las verificaciones disponibles.
