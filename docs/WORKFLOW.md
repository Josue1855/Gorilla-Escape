# Gorilla Escape — Ramas, integración y trabajo en equipo

**Fecha:** 2026-10-05.
**Estado:** flujo main/develop activado con remoto público y protecciones. Base de trabajo del equipo: develop, con Java/React y fuentes Unity. Repositorio publicado con una base inicial común.
**Fuente principal:** [Especificación v4](Gorilla_Escape_Gorilimpiadas_Especificacion_Maestra_v4.md), secciones 4, 39, 50 y 51. [Organización del código](MONOREPO_PLAN.md).

## Ramas de desarrollo

Conservar el esquema de la sección 39:

```text
main                          entregas validadas
└── develop                   integración frecuente
    ├── feature/mobile-...    una tarea de móvil/red
    ├── feature/camera-...    una tarea de cámara
    ├── feature/gameplay-...  una tarea de juego
    ├── feature/ui-...        una tarea visual
    ├── fix/...               un bug concreto
    └── docs/...              documentación
```

Se codifica en ramas de tarea creadas desde develop; los PR apuntan a develop. main recibe una promoción de develop tras las pruebas de integración y revisión de entrega. docs/ amplía la nomenclatura para trabajo documental; no cambia las ramas permanentes. Prefijos no son permisos de carpeta: un mismo PR puede tocar PWA/Server/Shared si cumple una tarea de protocolo.

Evitar ramas permanentes por persona, plataforma o minijuego: se separarían durante semanas y la integración llegaría tarde. Cada rama contiene una tarea revisable y limitada a la fase autorizada, con commits explicativos. Objetivo de proceso propuesto: integrar en uno o dos días de trabajo cuando sea viable; dividir tareas mayores en incrementos funcionales, sin convertir el plazo en obligación que comprometa calidad.

Ejemplos de nombres futuros: feature/mobile-phase0-qr-connect, feature/camera-phase0-pose-build, feature/gameplay-phase1-smash-scoring, fix/mobile-reconnect-player-id, docs/repository-workflow. No crear todas estas ramas por anticipado. phase0/phase1 expresa fase de implementación; P0/P1/P2 sigue siendo alcance de producto y Priority sigue siendo urgencia.

GitHub documenta el trabajo mediante ramas descriptivas y PR revisados. La recomendación de corta duración se aplica a nuestras ramas de tarea, conservando develop por la v4; no se sustituye el flujo del proyecto por GitHub Flow completo. [GitHub flow](https://docs.github.com/en/get-started/using-github/github-flow).

## Secuencia para una tarea

1. Elegir una tarea Ready del alcance actual: problema, responsable, dependencia y aceptación claros.
2. Actualizar develop y crear la rama desde esa base.
3. Implementar un resultado pequeño con pruebas aplicables y documentación.
4. Abrir PR a develop; draft si falta implementación o se necesita revisión temprana.
5. Obtener revisión de otro integrante, resolver comentarios y pasar checks aplicables. Integración de input exige revisión de quien consume el contrato.
6. Integrar y eliminar la rama remota terminada; actualizar copia local sin reescribir historial compartido.
7. Verificar el comportamiento integrado, no sólo la rama aislada.

Mantener main/develop integrables al madurar la base; la base Java/React ya compila; no se promete build Unity o gameplay hasta validarlos. Código incompleto puede integrarse únicamente si queda aislado del flujo jugable, compila y no rompe lo existente. No usar flags para introducir funcionalidades de fases futuras.

Para recuperar un cambio defectuoso, preferir un revert revisable. No force push ni rebase de ramas publicadas bajo las reglas actuales. Para ponerse al día en una rama compartida, integrar develop mediante merge y revisar conflictos. Squash merge es propuesta para PR de tarea cuando conserve un cambio coherente; la promoción develop → main debe conservar la relación entre historiales mediante merge commit, para evitar diffs repetidos en entregas siguientes.

## Protección activa en GitHub

Para main y develop se configuró: PR obligatorio, al menos una aprobación de otro integrante, conversaciones resueltas, checks aplicables aprobados y prohibición de force push/deletion. No exigir historial lineal si se permite el merge de promoción descrito arriba. Restringir promociones a responsables designados; definir permisos reales con los usuarios GitHub del equipo.

GitHub confirmó estas protecciones tras autorizar el usuario la visibilidad pública. Check requerido: management-validation, con rama actualizada; reglas aplicadas también a administradores. [Protecciones GitHub](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches).

CI separa validación automática de aprobación humana: un pipeline pasa/falla tests/build; la revisión sucede en el PR. Para documentación sólo ejecutar validaciones documentales. Al existir código, ejecutar suites del componente y contratos cruzados; en entrega ejecutar build completo e integración. Si un cambio Shared requiere todos los consumidores, no saltarlos sólo por filtros de carpetas.

## Responsabilidad sin silos

| Frente | Principal según v4 | Coordinación necesaria |
|---|---|---|
| Producto / aceptación | Josue | Cambios de alcance y decisiones con evidencia |
| Proceso / backlog | Erik | Dependencias, Ready/Done y riesgos |
| Arte / UI | Saul | Prefabs/escenas y móvil con Arturo/Hiram |
| Unity / gameplay | Arturo | Input normalizado con Austin/Hiram |
| PWA / servidor / red | Hiram | Protocolo con Arturo y UX con Saul |
| Cámara / tracking | Austin | CameraInput y Player Lock con Arturo |

Principal no significa único autorizado ni aprobador universal. Cambios de protocolo requieren acuerdo productor/consumidor; cambios visuales en escenas requieren coordinar quien las edita. CODEOWNERS es una opción futura tras conocer usuarios/rutas reales, no crear nombres GitHub ficticios ni bloquear al equipo con reglas imposibles.

Asignar temporalmente un responsable por escena/prefab durante edición simultánea; comunicar archivos y momento de integración. Mantener .meta junto a sus assets. Usar prefabs y escenas pequeñas para reducir conflictos. Nunca resolver YAML de Unity eligiendo toda una versión sin abrir y comprobar el resultado en el editor.

## Entregas y correcciones

1. Estabilizar develop en el alcance del hito.
2. Ejecutar tests, build e integración y las pruebas físicas requeridas; registrar hardware, resultados y limitaciones.
3. Crear PR develop → main con evidencia; revisar y promover el commit aprobado.
4. Etiquetar ese commit de main y producir/publicar la build correspondiente a ese mismo commit con versiones/modelos identificados.

Propuesta de tags: v0.1.0-spike, v0.2.0-smash y v1.0.0 cuando el MVP cumpla aceptación. No son entregas existentes ni una autorización para marcar Done. Un tag fija un punto del historial; no convierte métricas pendientes en aprobadas.

Antes de tener usuarios/releases, los fixes parten de develop. Si más adelante hay que corregir una entrega de main sin incluir novedades de develop, proponer una rama hotfix desde el tag entregado, validar y promover la corrección a main y después integrarla en develop. No reescribir ni mover tags publicados. No crear rama release permanente hasta que exista necesidad real de mantener versiones en paralelo.

## Arranque actual del equipo

Actualización de setup GitHub: base documental guardada en 4baddbd; main/develop publicados y remoto origin configurado. Rama docs/github-project-setup contiene gestión, formularios y validación para revisión. El usuario autorizó hacerlo público; main/develop ya están protegidas. Ver [PROJECT_SETUP](github/PROJECT_SETUP.md).

Fase 0 ya fue solicitada: la base compartida es develop y está documentada en [TEAM_START](TEAM_START.md). Crear ramas pequeñas desde develop y enviar sus PR a develop. Las tareas siguientes mantienen las revisiones y protecciones configuradas. Continuar con tareas pequeñas del Spike; no acumular Unity, red y cámara sin integración intermedia. Los bloques pueden coordinarse dentro de la misma fase, respetando dependencias y el orden de fases de v4.

## Checks para esta base

Antes de PR: npm --prefix PWA test, npm --prefix PWA run build, Server/mvnw -B -f Server/pom.xml verify, python3 tools/validate_foundation.py y python3 tools/github/validate_management.py. Restaurar con npm ci y JDK 21 según TEAM_START. Unity requiere editor/licencia: ejecutar sus tests y build cuando cambien sus fuentes; si está bloqueado, registrar pendiente y no marcar aceptación completa.

Cambios exclusivamente documentales deben pasar validación de enlaces/estructura y conservar la fuente normativa, salvo cambio explícitamente autorizado. La CI de esta base ejecuta también Java/React sin filtros para evitar omitir consumidores de Shared. No cambiar protecciones ni añadir colaboradores por inferir roles académicos.
