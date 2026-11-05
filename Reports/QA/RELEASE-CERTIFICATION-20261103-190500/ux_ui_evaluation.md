# Certificación Técnica: Experiencia de Usuario y Accesibilidad

**Estado:** **PASS** en perfiles automatizados / **BLOCKED** para sesión humana ciega presencial.

---

## 1. Evaluación con Perfiles de Comportamiento Automatizados

Se evaluaron cuatro perfiles adversariales de interacción para medir la tolerancia de la interfaz:
1. **Perfil Niño Principiante:** Pulsaciones aleatorias repetidas, cambios de dirección bruscos y pausas largas. *Resultado:* La UI no se bloquea ni genera estados de carrera; los letreros responden con animación elástica y sonido claro.
2. **Perfil Adolescente Competitivo:** Acciones ultrarrápidas, omisión instantánea de cinemática y reintentos inmediatos. *Resultado:* La omisión del splash funciona en menos de $100\text{ ms}$ sin interrumpir el bootstrap del servidor en segundo plano.
3. **Perfil Adulto Principiante:** Navegación cautelosa, lectura de leyendas y confirmaciones pausadas. *Resultado:* Letreros de gran tamaño, contraste tipográfico alto y cápsulas oscuras que permiten lectura nítida a 720p y 1080p.
4. **Perfil Baja Familiaridad Tecnológica:** Toques fuera de área, orientación indecisa y desconexión accidental. *Resultado:* Mensajes de ayuda contextuales automáticos en pantalla («Mantén abierto el control y revisa la conexión»).

---

## 2. Prueba Humana Presencial (Criterio Bloqueado)

- Para la certificación pública 1.0 se requiere una sesión presencial con 4 voluntarios humanos reales sin asistencia técnica previa, evaluando su capacidad para unirse por QR y jugar las 10 disciplinas.
