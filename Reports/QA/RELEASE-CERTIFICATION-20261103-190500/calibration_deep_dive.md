# Certificación Técnica: Calibración Personal («Prepárate para jugar»)

**Módulo:** `SplashDirector.State.PersonalPrep`, `PortableServiceHost.cs`, `JungleInputAdapter.cs`  
**Estado:** **PASS** (Lógica, algoritmos de umbral y pruebas de influencia en gameplay verificadas).

---

## 1. Diseño y Accesibilidad («Prepárate para jugar»)

La calibración está diseñada como una experiencia transparente y libre de jerga técnica:
- **Lenguaje Claro:** En lugar de términos como «acelerometría triaxial» o «inferencia de landmarks MediaPipe», la pantalla muestra:
  - *«Ponte frente a la cámara»*
  - *«Levanta los brazos para comprobar tu alcance»*
  - *«Sostén tu teléfono en posición cómoda»*
- **Retroalimentación Visual:** Silueta anatómica ilustrada que valida el encuadre de la persona y la visibilidad de ambas manos.

---

## 2. Calibración de Visión y Landmarks (MediaPipe)

- **Landmarks Evaluados:** Muñecas (`left-wrist`, `right-wrist`), hombros (`left-shoulder`, `right-shoulder`) y caderas (`left-hip`, `right-hip`).
- **Umbral de Calidad:** Visibilidad y presencia mínimas de $0.60$ en coordenadas normalizadas $[0, 1]$.
- **Zona Muerta de Reposo:** Si la muñeca cae por debajo de la línea de la cintura ($y > \text{waistY}$), el cursor de mano se apaga automáticamente, evitando activaciones accidentales con brazos caídos.
- **Detección de Empuje:** Velocidad en $Z < -0.85\text{ m/s}$ calculada en ventanas de $20$ a $250\text{ ms}$.

---

## 3. Calibración de Sensores Móviles (Teléfono)

- **Vector Neutral de Reposo:** Al ingresar al lobby, el cliente móvil muestrea 20 lecturas de aceleración en reposo y calcula el vector de gravedad neutral $\vec{g}_{\text{ref}}$.
- **Prueba de Influencia Activa:**
  1. Calibración con el teléfono vertical a $0^\circ$: El swing de béisbol y bowling registra fuerza $1.0$ con oscilación frontal de $90^\circ$.
  2. Recalibración intencional con el teléfono inclinado a $+25^\circ$: El mismo movimiento de $90^\circ$ produce una trayectoria ajustada en Unity con offset de $+25^\circ$, demostrando que la calibración no es cosmética y altera la física del motor.
