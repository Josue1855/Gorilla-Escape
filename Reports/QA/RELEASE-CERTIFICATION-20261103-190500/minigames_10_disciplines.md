# Certificación Técnica: Calibración Personal y 10 Minijuegos

**Estado:** **PASS** (Todas las disciplinas e interactividad demostradas).

---

## 1. Calibración Personal («Prepárate para jugar»)

La pantalla de preparación personal accesible (`evidence_08_personal_preparation.png`) instruye al participante en lenguaje directo y comprensible:
- **Paso 1: Posiciónate frente a la pantalla:** Encuadre de torso y hombros.
- **Paso 2: Rango de Brazos:** Detección de elevación de muñecas por encima de la línea clavicular para habilitar punteros y gestos.
- **Paso 3: Calibración Neutral de Teléfono:** El teléfono captura el vector de aceleración gravitatoria $\vec{g} = (0, 0, -9.81)\text{ m/s}^2$ en posición de reposo, calculando el offset de inclinación neutral para desacoplar el ángulo natural de agarre del jugador.
- **Influencia en Gameplay:** Si el usuario recalibra con el teléfono inclinado a $25^\circ$, la cinemática de lanzamiento (Coconut Bowling / Baseball) toma esa postura como punto cero, demostrando que la calibración modifica activamente los cálculos físicos en Unity.

---

## 2. Certificación Individual de las Diez Disciplinas

Cada minijuego fue auditado verificando la cadena completa:
$$\text{Sensor Móvil / Cámara} \longrightarrow \text{WebRTC DataChannel} \longrightarrow \text{Input Fusion Unity} \longrightarrow \text{Física / Gameplay} \longrightarrow \text{Puntuación Real}$$

1. **Gorilla Smash:** Aceleración brusca descendente del teléfono ($> 18\text{ m/s}^2$) acciona el mazo ceremonial, impactando tambores sagrados y sumando puntos por sincronía.
2. **Coconut Throw:** Arco de elevación e impulso hacia adelante del celular calcula la parábola balística del coco para derribar dianas en la playa.
3. **Coconut Bowling:** Balanceo pendular en el eje sagital calcula velocidad de rodamiento y ángulo de desvío hacia los pines de tótem de madera.
4. **Jungle Slice:** Movimiento cortante transversal rápido activa la estela de navaja tropical en pantalla, rebanando frutas en vuelo.
5. **Log Balance:** Inclinación lateral continua (roll $\pm 30^\circ$) controla el centro de gravedad del náufrago sobre el tronco movedizo.
6. **Gorilla Boxing:** Combinación de puñetazos frontales (aceleración en $Z$) y guardia alta (inclinación en $Y$) para intercambiar golpes con Kong.
7. **Jungle Baseball:** Sincronización precisa de swing con el lanzamiento del coco, calculando distancia y home runs.
8. **Jungle Drums:** Golpes alternados a izquierda y derecha con cadencia rítmica sincronizada con las señales visuales.
9. **Whack-a-Mole:** Impactos rápidos sobre topos de selva que emergen de montículos de arena con penalización por tocar figuras prohibidas.
10. **Jungle Keeper:** Desplazamiento lateral rápido para atajar balones lanzados hacia la portería de bambú.
