# Procedimiento de Seguridad, Respaldo y Rotación de Keystore — Gorilla Escape Companion

## 1. Auditoría de Seguridad y Hallazgos Previos

Durante la auditoría adversarial del 1 de noviembre de 2026, se detectaron las siguientes vulnerabilidades en la configuración inicial de firma:
1. **Contraseña en texto claro en `build.gradle.kts`:** La versión preliminar configuraba `storePassword = "gorilla123"` directamente en el script de Gradle bajo control de versiones.
2. **Contraseña débil y expuesta:** La contraseña `gorilla123` fue expuesta en trazas de compilación.
3. **Ausencia de `.gitignore` para archivos de propiedades:** No existía una exclusión formal para `key.properties`.

### Veredicto de Auditoría
El keystore preliminar `gorilla-release.jks` se declara formalmente **COMPROMETIDO Y DESECHO**. No debe utilizarse para distribución pública en Google Play Store ni distribución masiva de producción.

---

## 2. Configuración Segura Implementada (Producción)

Se refactorizó `Companion/android/app/build.gradle.kts` para eliminar cualquier credencial en código:
- Las credenciales se leen exclusivamente desde `Companion/android/key.properties` o desde variables de entorno del sistema (`GORILLA_KEYSTORE_PASSWORD`, etc.).
- Tanto `key.properties` como `*.jks`, `*.keystore` y `*.p12` están estrictamente excluidos en `Companion/.gitignore` y en la raíz del repositorio `.gitignore`.

### Keystore Oficial de Producción
- **Archivo:** `Companion/android/app/gorilla-production-release.jks`
- **Alias:** `gorilla-escape-prod`
- **Algoritmo:** RSA 2048 bits (SHA384withRSA)
- **Validez:** 10,000 días
- **Distinguished Name (DN):** `CN=Gorilla Escape Production, OU=Mobile Release, O=Gorilla Escape Commercial, C=MX`
- **Huella Digital SHA-256 (Firma APK):**
  `9F:F5:56:BF:06:8A:02:2B:E4:FD:1D:48:44:4C:48:ED:D2:D9:15:47:52:12:6B:70:0A:1E:FD:C7:6E:EC:37:56`

---

## 3. Protocolo de Respaldo Seguro y Almacenamiento

El archivo `gorilla-production-release.jks` y `key.properties` NUNCA deben comitearse a Git. Para su preservación antes del lanzamiento comercial:

1. **Bóveda de Secretos Segura:**
   - Cargar `gorilla-production-release.jks` y su contraseña en un gestor de secretos corporativo (AWS Secrets Manager, Google Cloud Secret Manager, 1Password for Teams o HashiCorp Vault).
2. **Respaldo Físico Offline (Cold Storage):**
   - Una copia cifrada (GPG/AES-256) debe almacenarse en un medio extraíble protegido bajo custodia del responsable técnico del proyecto.
3. **CI/CD Seguro (GitHub Actions / GitLab CI):**
   - El keystore debe codificarse en base64 y guardarse en los secretos del repositorio (`ANDROID_KEYSTORE_BASE64`).
   - Las contraseñas deben configurarse como secretos protegidos: `KEYSTORE_PASSWORD`, `KEY_ALIAS`, `KEY_PASSWORD`.

---

## 4. Procedimiento de Firma Reproducible

Para compilar el APK Release idéntico en cualquier máquina autorizada:

```bash
# 1. Asegurarse de tener el archivo key.properties en Companion/android/
# Contenido de key.properties:
# storeFile=gorilla-production-release.jks
# storePassword=<SECRET_PASSPHRASE>
# keyAlias=gorilla-escape-prod
# keyPassword=<SECRET_PASSPHRASE>

# 2. Navegar al módulo móvil y compilar:
cd Companion
flutter clean
flutter pub get
flutter build apk --release

# 3. Verificar la firma con apksigner:
apksigner verify --print-certs build/app/outputs/flutter-apk/app-release.apk
```

---

## 5. Procedimiento de Rotación de Llaves

Si la llave de producción sufriera algún compromiso futuro:
1. Generar un nuevo par de claves RSA 2048/4096 bits con nuevo alias y contraseña segura de 256 bits.
2. Extraer la nueva huella SHA-256 con `keytool -list -v -keystore ...`.
3. Actualizar `tools/cloudflare-worker/src/index.js` agregando la nueva huella a `sha256_cert_fingerprints` en `ASSET_LINKS`.
4. Si la app está en Google Play, utilizar el servicio de rotación de claves de Play App Signing mediante la carga del certificado derivado.
5. Desplegar el Worker en Cloudflare (`npx wrangler deploy`).
