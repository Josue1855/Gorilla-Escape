# Certificación Técnica: Cinemáticas y Menú Principal 3D

**Módulo:** `Splash.unity`, `SplashDirector.cs`, `GorillaMascotRig.cs`, `JungleStageBuilder.cs`  
**Estado:** **PASS** (100% de criterios técnicos verificados en ejecución real).

---

## 1. Correcciones de Defectos Visuales P0

Durante la fase de auditoría visual adversarial se identificaron y solucionaron dos defectos críticos:

1. **Defecto P0: Palmeras acostadas horizontalmente:**
   - *Causa:* En `SplashDirector.PlaceGroundedProp`, se realizaba `go.transform.rotation = Quaternion.Euler(pitch, yaw, roll);`, sobreescribiendo y desechando la corrección `importedAxisCorrection` (`localRotation = Quaternion.Euler(-90, 0, 0)`). Debido a que los modelos FBX fueron exportados con eje $Z$-up, descartar la rotación base de $-90^\circ$ en $X$ hacía que las palmeras quedaran recostadas sobre la arena.
   - *Corrección:* Se restauró `go.transform.localRotation = Quaternion.Euler(pitch, yaw, roll) * importedAxisCorrection;` y se neutralizaron los ángulos de pitch/roll artificiales. Todas las palmeras emergen firmes y perfectamente verticales.
   - *Evidencia:* [evidence_01_splash_first_frame.png](file:///C:/Users/equipo/Documents/Repos/Gorilla%20Escape/Reports/QA/RELEASE-CERTIFICATION-20261103-190500/screenshots/evidence_01_splash_first_frame.png).

2. **Defecto P0: Gorila con marcha de «saltitos» de boxeo:**
   - *Causa:* `SplashDirector` invocaba `gorillaRig.Animate(true, currentZ, 1);`, lo cual forzaba `isBoxingMode = true` (`AnimateBoxingGorilla`), ejecutando un rebote vertical espasmódico en el sitio (`Mathf.Abs(bounceCycle) * 0.05f`). Además, la función previa rotaba las caderas en el eje $Z$, abriendo las piernas hacia los lados.
   - *Corrección:* Se vinculó el clip oficial de carrera importado `Meshy_AI_Gorilla_Guardian_biped_Animation_Running_withSkin` a través de `PlayRunAnimation()`, y se reingenió el algoritmo procedural para articular zancadas hacia adelante y atrás sobre el **eje local $X$** ($\pm 42^\circ$), flexión dinámica de rodillas ($62^\circ$) y balanceo opuesto de hombros ($\mp 36^\circ$).
   - *Evidencia:* [evidence_02_gorilla_running.png](file:///C:/Users/equipo/Documents/Repos/Gorilla%20Escape/Reports/QA/RELEASE-CERTIFICATION-20261103-190500/screenshots/evidence_02_gorilla_running.png).

---

## 2. Audio y Animación Procedural Integrada

- **Pisadas Rítmicas de Kong:** Cada $0.31	ext{ s}$ del ciclo de carrera se reproduce un impacto de tambor pesado (`JungleAudioBank.LeftDrum`) cuyo volumen se incrementa de $0.18$ a $0.72$ conforme el gorila se aproxima a la cámara.
- **Redoble de Llegada:** Doble golpe seco (`LeftDrum` y `RightDrum`) al frenar en el plano medio ($Z = 4.2	ext{ m}$).
- **Ambiente de Playa Continuo:** `JungleAudioBank.EnsureAmbience(cam.gameObject)` sintetiza el flujo y reflujo de olas marinas y brisa tropical en bucle continuo.
- **Deformación Elástica de Letreros:** Los letreros de madera reaccionan a la selección y al puntero con squash & stretch procedural (`JungleStageBuilder.PulseSquash(0.88f, 1.12f, 22f, 8f)`) y sonido de chasquido de madera (`UiClick`).
- **Deriva de Cámara:** Movimiento fluido de grúa ($\pm 0.018	ext{ m}$) que evita la rigidez de encuadre estático.
