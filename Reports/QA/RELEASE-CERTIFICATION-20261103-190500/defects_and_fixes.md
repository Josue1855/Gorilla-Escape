# Registro de Defectos Encontrados y Correcciones Aplicadas

**Identificador:** `RELEASE-CERTIFICATION-20261103-190500`

---

### Defecto 1 (P0 Visual): Palmeras acostadas horizontalmente en el Splash
- **Severidad:** P0 (Visual bloqueante).
- **Causa Raíz:** En `SplashDirector.cs`, `PlaceGroundedProp` asignaba `go.transform.rotation = Quaternion.Euler(pitch, yaw, roll);`, ignorando el factor de rotación de $-90^\circ$ en $X$ (`importedAxisCorrection`) requerido por los modelos FBX Z-up de Meshy AI.
- **Corrección:** Se multiplicó la rotación deseada por `importedAxisCorrection` (`Quaternion.Euler(...) * importedAxisCorrection`) y se neutralizaron los ángulos de pitch/roll artificiales.
- **Resultado:** Palmeras 100% erguidas verticalmente en dunas.
- **Estado:** **RESUELTO Y VERIFICADO**.

---

### Defecto 2 (P0 Visual): Gorila con marcha espasmódica de boxeo («saltitos»)
- **Severidad:** P0 (Visual y jugabilidad).
- **Causa Raíz:** `SplashDirector` activaba `isBoxingMode = true`, ejecutando un rebote vertical en el sitio (`AnimateBoxingGorilla`). Además, `AnimateRun` rotaba las caderas en el eje $Z$, abriendo las piernas lateralmente.
- **Corrección:** Se enlazó el clip de animación oficial `Meshy_AI_Gorilla_Guardian_biped_Animation_Running_withSkin` a través de `PlayRunAnimation()`, y se reingenió el algoritmo procedural con zancadas atléticas en el eje local $X$ ($\pm 42^\circ$).
- **Resultado:** Kong avanza al trote heroico hacia la cámara con balanceo coordinado de brazos.
- **Estado:** **RESUELTO Y VERIFICADO**.

---

### Defecto 3 (P1 Backend): Race condition en prueba de integración de Cloudflared
- **Severidad:** P1 (Fallo en suite de calidad automatizada).
- **Causa Raíz:** En `TestCloudflaredFixture.java`, en Windows se anteponía la URL del túnel antes de las líneas de registro, provocando que `supervisor.start()` retornara antes de que el script emitiera los eventos de registro, causando un fallo de expectativa en `ConnectivityRecoveryTest.realReaderKeepsSurvivingConnection`.
- **Corrección:** Se reescribió la generación del script por lotes en Windows para iterar secuencialmente línea por línea sobre el escenario de prueba, preservando el orden estricto de emisión.
- **Resultado:** `mvnw verify` superó el 100% de las 164 pruebas sin fallos (`BUILD SUCCESS`).
- **Estado:** **RESUELTO Y VERIFICADO**.
