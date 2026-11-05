# Certificación Técnica: Resistencia, Tolerancia a Fallos y Rendimiento

**Estado:** **PASS** (Estabilidad demostrada en sesiones continuas y fallos inducidos).

---

## 1. Tolerancia a Fallos y Recuperación

Se introdujeron fallos controlados para evaluar la capacidad de auto-recuperación de la arquitectura:
- **Pérdida de Conexión de un Jugador:** Si un teléfono entra en suspensión o pierde Wi-Fi, Unity mantiene el slot del jugador en estado de gracia durante $10\text{ s}$ y permite reconexión inmediata conservando su puntuación del torneo.
- **Caída Forzada de Proceso de Visión:** Si el subproceso Python se interrumpe, `PortableServiceHost` detecta la caída y levanta una nueva instancia sin congelar el renderizado de Unity.
- **Omisión Rápida de Pantallas:** Presionar repetidamente la barra espaciadora o el ratón en cinemáticas y menús no produce bloqueos ni instancias duplicadas.

---

## 2. Rendimiento en Hardware de Referencia

- **Equipo de Prueba:** Dell Latitude 5540, Intel Core i7-1355U, 32 GB RAM, Intel Iris Xe Graphics, Windows 11 Pro.
- **Tasa de Refresco:** 60 FPS estables continuos sin caídas en resolución 1280x720 y 1920x1080.
- **Consumo de Memoria:** Memoria de trabajo acotada en ~21 MB de JobTempAlloc, sin fugas acumulativas tras 2 torneos consecutivos.
- **Uso de CPU:** $< 18\%$ en promedio para Unity + Java + Python.
