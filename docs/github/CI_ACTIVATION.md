# Activación de CI, Project y distribución Windows

Estado: workflows creados y con pruebas Java/PWA, no se afirma una versión
jugable hasta que GameCI y el QA con dispositivos hayan ejecutado y pasado.

## 1. Flujo de revisión

- Unity: `Josue1855/Gorilla-Escape`; `develop` para integración; `main` para promociones aprobadas.
- Backend: `Josue1855/Gorilla-Escape-Backend`; rama predeterminada `develop`.
- PWA fork: `Josue1855/Dise-o_pwa`; rama predeterminada `develop`.
- UI/UX original: `Quintana-117/Dise-o_pwa`; este usuario dispone de READ
  y no se ha fusionado código en el repositorio del mantenedor.

**No omitir el revisor en el PR #128.** `main` ya exige aprobación de otra
persona con WRITE; se solicitó revisión a `Quintana-117`.
Esta condición no se puede dar por satisfecha desde otro workflow.

## 2. Activar branch rulesets sin bloquear accidentalmente

Después de fusionar PR #128 a `main`:

1. GitHub → Gorilla-Escape → Settings → Secrets and variables → Actions → Secrets.
2. Crear `GORILLA_ADMIN_TOKEN` para una GitHub App o PAT de alcance mínimo con
   `Administration: write` sobre los **tres repositorios**. El token no se
   agrega a archivos ni a respuestas de PR. Mantener vigencia limitada.
3. Actions → `Preview or configure Gorilla Escape branch rulesets` →
   Run workflow → `apply=false`. Descargar `ruleset-preview.json` y validar.
4. Confirmar estos contexts REALES de CI que ya se han ejecutado:
   - Unity: `java-react-foundation`, `management-validation`.
   - Backend: `backend (ubuntu-latest)`, `backend (windows-latest)`.
   - PWA: `pwa`.
5. Run workflow → `apply=true`. El script crea reglas por repo para
   `main` y `develop` con **1 aprobación externa**, hilos resueltos, no
   force push/deletion y checks requeridos, sin actores de bypass.
6. Si una regla con el mismo nombre ya existe pero difiere, el script falla
   en lugar de modificarla silenciosamente. Revisar Settings → Rules → Rulesets.

No añadir aún GameCI como check obligatorio: hasta configurar licencia se
ejecuta sólo manualmente y quedaría como contexto esperado sin eventos de PR.

## 3. Activar sincronización del Project privado #1

1. Settings → Secrets and variables → Actions → Secrets: crear en cada repo
   `GORILLA_PROJECT_TOKEN` con acceso al Project personal privado #1 y
   permisos suficientes para agregar items. Para PAT clásico, scope
   `project`; para otro tipo de token validar acceso Projects V2.
2. Settings → Variables: `GORILLA_PROJECT_SYNC_ENABLED=true` en cada repo.
3. El workflow `project-sync.yml` incorpora **nuevos** issues/PR mediante URL,
   sin establecer estado, prioridad, responsable o iteración.
4. En el repo Unity (desde su rama predeterminada), Actions →
   `Backfill Gorilla Escape Project 1` → `apply=false` para inventariar
   abiertos. Revisar el listado; luego `apply=true` para incorporar los que
   aún no pertenecen al Project.
5. Revisar si el Project tiene reglas nativas Auto-add activas para las mismas
   fuentes y evitar trabajo redundante; desactivar las redundantes sólo
   después de verificar la cobertura, sin vaciar ni reemplazar items.

**Limitaciones:** esto no crea permisos de acceso para Hiram ni altera su
repositorio original; la sincronización afecta tres repositorios bajo Josue1855.

## 4. Activar Unity Windows GameCI

1. Fijar editor **Unity 6000.3.23f1** y módulos de Windows x64.
2. Settings → Secrets and variables → Actions → Secrets: configurar
   `UNITY_LICENSE` para Unity Personal con el archivo de activación válido,
   y `UNITY_EMAIL`/`UNITY_PASSWORD` según GameCI. Para licencias de pago,
   usar `UNITY_SERIAL` con credenciales autorizadas. Nunca publicar licencias.
3. Settings → Variables: `UNITY_CI_ENABLED=true` en repo Unity.
4. Actions → `Unity Windows candidate build (licensed)` →
   Run workflow (en `main`) → `package_candidate=true`.
5. El pipeline ejecuta EditMode/PlayMode, construye Unity para Windows,
   compila React embebido y Java, prueba SQLite en Windows, crea un runtime
   Java 21 con jlink, empaqueta Unity + host + backend y corre smoke real
   Unity→Java IPC→PWA HTTP + Backend SQLite. Un fallo NO publica ZIP.
6. Descargar el artefacto `GorillaEscape-Windows64-candidate`. ZIP y hash
   SHA-256 se conservan 30 días. En la carpeta extraída ejecutar
   `Start-GorillaEscape.cmd`. No se requiere Java/Unity instalado.
7. Validar manualmente Windows y teléfonos Android/iPhone reales,
   HTTPS/WSS, QR, sensor motion, desconexión, red aislada y flujo jugable.
   Guardar evidencia antes de marcar el release como jugable.

**No se publican releases jugables automáticamente por crear un tag.**
El paquete anterior se etiqueta **candidate**. GitHub Release final requiere
QA real, revisión, versión aprobada y enlace a la evidencia.

## 5. Evidencias y límites

- Backend E2E: Chromium → React lab → POST/GET Java → UUID persistido en SQLite,
  ejecutado y aprobado mediante PR #23.
- Unity cross-repo static integration: tests Java/PWA y formas OpenAPI, NO
  prueba de teléfonos ni interacción jugable.
- Unity Windows smoke: configurado pero NO ejecutado hasta disponer de licencia.
- CodeQL analiza Java y se añade al CI. El escaneo de dependencias npm
  y Dependabot complementan pero no sustituyen validación de amenazas.
- No insertar certs, credenciales, datos de jugadores reales o archivos SQLite
  de producción en ZIP ni artifacts.
