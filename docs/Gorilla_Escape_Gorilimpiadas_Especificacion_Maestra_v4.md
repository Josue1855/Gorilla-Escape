# Gorilla Escape: Gorilimpiadas

> **Fuente operativa vigente — 2026-10-08:** GitHub Project/Issues/dependencias/AC. Checkpoint técnico Fase0 **PASS — SOFTWARE/LAB**, conservado; **Project Fase0/E0/milestone siguen OPEN**. Fase1 **BLOCKED / NOT STARTED** por #35→#34; los estados READY del audit anterior no autorizan iniciar según Project. [Reconciliación vigente](PHASE0_PROJECT_RECONCILIATION.md).

> **Precedencia de estado:** las enmiendas vigentes al final de este documento prevalecen sobre los snapshots de implementación y transporte del texto original. WebSocket obligatorio y WT/RTC pendiente están superseded / replaced by DEC-010 Mobile Transport. El cierre Software/Lab se decide en [auditoría final](PHASE0_FINAL_AUDIT.md), separado del MVP físico.
## Especificación maestra v4 — producto, experiencia, implementación y viabilidad

**Estado:** línea base de preproducción para validación del equipo  
**Propietario:** Product Owner  
**Documento base:** `Gorilla_Escape_Gorilimpiadas_Especificacion_Maestra_v3.md`  
**Referencias narrativas:** `Kit de preproducción 01`, `Historia y estructura narrativa`, `Causa, consecuencia y progresión narrativa`, `Cómo encontrar huecos argumentales` y `Construyendo y poniendo a prueba nuestra historia`  
**Objetivo:** reunir en una sola fuente las decisiones de producto, narrativa funcional, mecánicas, UX, arquitectura, calidad, producción y negocio necesarias para construir y evaluar el MVP.  
**Inicio de desarrollo:** semana del 28 de septiembre de 2026  
**Entrega objetivo del MVP:** semana del 9 al 13 de noviembre de 2026  
**Feature Freeze:** 6 de noviembre de 2026  
**Última revisión editorial:** 22 de septiembre de 2026  
**Enmienda técnica:** 5 de octubre de 2026 — DEC-002: servidor Java y PWA React, por instrucción explícita del Product Owner. No modifica fases ni reglas de gameplay.
**Aclaración de autoridad:** 6 de octubre de 2026 — DEC-005 aprobada por el Product Owner: Unity confirma los resultados oficiales; Java sólo transporta. Implementación IPC 2A condicionada a integración normal de Unity Foundation, sin iniciar Fase 1.

> Esta v4 sustituye a la v3 como fuente principal de consulta. Conserva su contenido técnico y de negocio, corrige ambigüedades e incorpora el concepto y el marco narrativo del MVP. Una decisión sólo se considera vigente cuando queda registrada en este documento o en el historial de decisiones del proyecto.

## Resumen ejecutivo

**Gorilla Escape: Gorilimpiadas** es un party game físico local para una a cuatro personas. La PC ejecuta el juego y arbitra los resultados; cada participante usa su teléfono como control de movimiento y una webcam aporta contexto corporal. El MVP propone un torneo de cinco pruebas en el que la potencia, la dirección, el timing, la estabilidad y el giro del movimiento real influyen de forma comprensible en el resultado.

La viabilidad del proyecto depende de demostrar primero una cadena completa con **Gorilla Smash**: conexión por QR, lectura de sensores, tracking, calibración, fusión de señales, acción en Unity, puntuación, feedback y recuperación de errores. Si esa cadena no satisface los criterios de latencia, estabilidad y comprensión, el equipo aplicará los planes de contingencia antes de producir los demás minijuegos.

## Cómo leer este documento

- Las secciones 0–2 definen la intención, el concepto, la narrativa funcional y el alcance.
- Las secciones 3–16 especifican arquitectura, sesión, red, input, calibración y competencia.
- Las secciones 17–31 describen minijuegos, UX, presentación, arte, audio y accesibilidad.
- Las secciones 32–43 establecen seguridad, privacidad, persistencia, calidad, rendimiento y contingencias.
- La sección 44 evalúa la factibilidad y la viabilidad como ejercicio académico.
- Las secciones 45–57 establecen roadmap, métricas, responsabilidades, decisiones y trazabilidad.

## Convenciones normativas

| Término | Significado |
|---|---|
| **Debe** | requisito obligatorio para aceptar el MVP |
| **Debería** | objetivo recomendado; puede ajustarse con evidencia |
| **Puede** | opción permitida, no comprometida |
| **Hipótesis** | afirmación que requiere prueba técnica, de usuario o comercial |
| **Pendiente** | decisión con responsable y fecha límite aún por registrar |

Las cifras de tuning son valores iniciales, no promesas de diseño. Los valores definitivos deben provenir de pruebas y mantenerse en configuración.

---

# 0. Principios de decisión

El proyecto priorizará, en este orden:

1. Llegar a un MVP demostrable.
2. Estabilidad.
3. Baja latencia y respuesta inmediata.
4. Facilidad y velocidad de desarrollo.
5. Experiencia del jugador.
6. Mantenibilidad.
7. Escalabilidad futura.

## Regla de alcance

Toda nueva propuesta se clasifica como:

- **P0 — MVP:** necesaria para completar correctamente la experiencia.
- **P1 — Si hay tiempo:** mejora importante que no debe retrasar el MVP.
- **P2 — Post-MVP:** se diseña para ser posible, pero no se desarrolla ahora.

No se agregará una característica P1 o P2 si pone en riesgo una P0.

---

# 1. Visión definitiva del producto

**Gorilla Escape: Gorilimpiadas** es un party game deportivo multijugador local en el que una PC, una webcam y los teléfonos de los jugadores forman una plataforma de juego físico.

La diferencia central no será solamente detectar que el jugador se movió, sino interpretar **cómo se movió**:

- potencia;
- dirección;
- elevación;
- velocidad angular;
- spin;
- timing;
- estabilidad;
- trayectoria corporal.

## Propuesta de valor

> Convertir dispositivos que los jugadores ya poseen —teléfonos, una PC y una cámara convencional— en una plataforma de videojuegos físicos multijugador donde la técnica del movimiento real afecta directamente el resultado dentro del juego.

## Pitch de concepto

> Gorilla Escape: Gorilimpiadas es un party game deportivo local para familias, amistades y jugadores casuales, donde una a cuatro personas compiten en cinco pruebas usando sus teléfonos y una webcam para lanzar, apuntar, golpear, girar y cortar. El objetivo es coronarse campeón mediante movimientos fáciles de aprender, seguros de ejecutar y suficientemente técnicos para mejorar con la práctica.

## Experiencia deseada

La experiencia debe provocar actividad, competencia amistosa y celebración compartida. Una persona nueva debe comprender rápidamente qué movimiento realizar, percibir una respuesta inmediata y relacionar su técnica con el resultado. Quienes observan deben entender el turno y anticipar el desenlace sin necesitar una explicación técnica.

## Público primario

- grupos de una a cuatro personas en un mismo espacio;
- jugadores casuales o con experiencia mixta;
- sesiones sociales breves frente a una pantalla común;
- hogares, aulas o demostraciones controladas con PC, webcam, red local y teléfonos compatibles.

El producto no se diseña para entrenamiento deportivo, rehabilitación, competición profesional ni sesiones online en el MVP.

## Rol y objetivo del jugador

Cada participante representa a un gorila atleta invitado a las Gorilimpiadas. Su objetivo es completar cinco disciplinas, obtener Gorilla Points según su posición y terminar el torneo con la mayor puntuación acumulada.

Los verbos principales son:

| Verbo | Función jugable |
|---|---|
| **Conectar** | incorporarse a la sesión mediante QR y teléfono |
| **Calibrar** | adaptar la lectura a la persona, el dispositivo y el espacio |
| **Ejecutar** | realizar el gesto físico solicitado en cada prueba |
| **Ajustar** | mejorar dirección, potencia, estabilidad, giro o timing |
| **Competir** | comparar resultados, acumular puntos y buscar el campeonato |

## Premisa narrativa

Atletas de distintos rincones de la jungla se reúnen para disputar las Gorilimpiadas, un torneo de cinco disciplinas que premia control, potencia y precisión. Cada participante elige a su representante, completa las pruebas y asciende en la tabla hasta que el torneo corona a un campeón.

La narrativa funciona como marco de cohesión y motivación, no como campaña cinematográfica. El MVP no requiere diálogos ramificados, escenas largas, antagonista, exploración de mundo ni decisiones argumentales.

## Sinopsis funcional del MVP

El anfitrión crea una Gorilimpiada y los participantes ingresan mediante un código QR. Después de elegir nombre, color o avatar, cada persona calibra su control y aprende las reglas de seguridad. Una ceremonia breve presenta el torneo y conduce a cinco pruebas: Gorilla Smash, Coconut Throw, Jungle Archery, Coconut Bowling y Jungle Slice.

En cada disciplina, el juego explica el objetivo, concede los intentos definidos, traduce el movimiento físico en una acción visible y muestra por qué el resultado fue bueno o mejorable. Al terminar cada prueba se actualiza la tabla. Después de la quinta disciplina, una ceremonia final muestra la clasificación, corona al campeón y permite regresar al menú.

## Cadena causal de la experiencia

```text
Los jugadores entran al torneo
→ por eso conectan sus teléfonos y eligen representante
→ para competir con equidad deben calibrar
→ la calibración habilita movimientos comparables
→ cada prueba transforma la técnica en un resultado
→ cada resultado otorga Gorilla Points
→ la suma de puntos determina la clasificación
→ la quinta prueba cierra el torneo y corona al campeón
```

## Beat sheet del torneo

| Beat | Qué ocurre | Qué cambia |
|---|---|---|
| 1. Convocatoria | se crea la sesión y aparece el QR | el grupo pasa de espectador a participante |
| 2. Registro | cada persona elige identidad visual | la sesión reconoce a todos los competidores |
| 3. Preparación | se explican seguridad y calibración | el sistema queda listo para interpretar movimientos |
| 4. Apertura | inicia la ceremonia breve | el grupo comprende el objetivo global y el orden del torneo |
| 5. Competencia | se juegan cinco disciplinas | cambian resultados, posiciones y expectativas |
| 6. Tensión final | la tabla presenta la situación antes de la última prueba | cada participante entiende qué necesita para ganar |
| 7. Coronación | se calculan desempates y se presenta al campeón | el torneo queda resuelto y la sesión obtiene cierre |

## Conflicto, resistencia y consecuencias

El conflicto del MVP no depende de un villano. Cada participante quiere ganar el torneo, pero no puede hacerlo inmediatamente porque debe dominar cinco gestos distintos, actuar dentro de intentos limitados y superar los resultados de los rivales.

| Pregunta | Respuesta del MVP |
|---|---|
| ¿Qué quiere conseguir el jugador? | terminar el torneo con la mayor cantidad de Gorilla Points |
| ¿Qué se lo impide? | la exigencia técnica de cada prueba, los intentos limitados y el desempeño de los demás |
| ¿Qué está en juego? | la posición en la tabla, la posibilidad de remontar y la coronación final |
| ¿Qué ocurre al fallar? | el intento produce un resultado bajo o inválido, cambia la clasificación y reduce el margen de recuperación |
| ¿Qué empeora si no mejora? | las pruebas restantes ofrecen menos oportunidades para remontar |
| ¿Qué cambia cuando actúa? | obtiene feedback, aprende qué ajustar y modifica su resultado acumulado |

El fallo no elimina al participante ni reinicia toda la sesión. Su consecuencia debe ser visible, proporcional y recuperable mientras queden intentos o disciplinas.

## Cadena causal verificable

| Acontecimiento | Causa | Acción del jugador o sistema | Consecuencia | Qué provoca después |
|---|---|---|---|---|
| se crea la Gorilimpiada | el grupo decide competir | el anfitrión inicia una sesión | aparece un QR y un `SessionId` | los demás pueden incorporarse |
| se registra un participante | escanea el QR | elige nombre y avatar | obtiene identidad y turno | la sesión puede verificar cuándo está completo el grupo |
| se cierra el lobby | todos están listos o el anfitrión decide comenzar | la PC fija participantes y orden | ya no ingresan jugadores de forma accidental | comienza la preparación |
| se calibra | los dispositivos y capacidades físicas varían | cada persona ejecuta gestos guiados | se crea un perfil comparable | el sistema puede interpretar los intentos |
| inicia una disciplina | el torneo sigue su orden establecido | el juego presenta objetivo y gesto | el participante sabe qué debe hacer | se habilita el intento |
| se registra un intento | existe input válido dentro de la ventana | el sistema fusiona señales y ejecuta la física | se calcula un resultado explicable | el jugador recibe feedback y puede ajustar |
| termina una disciplina | se agotaron los intentos de todos | se ordenan resultados y asignan GP | cambia la tabla general | aumenta o disminuye la posibilidad de victoria |
| inicia la prueba final | quedan posiciones por resolver | se muestra la situación de la tabla | el grupo entiende la tensión competitiva | cada resultado puede cerrar o alterar la clasificación |
| termina el torneo | concluyen las cinco pruebas | se aplican desempates y se confirma la tabla | existe un campeón inequívoco | se presenta la coronación y el regreso al menú |

Si una transición no puede explicarse mediante su causa, acción, consecuencia y continuidad, debe marcarse como **HUECO CAUSAL** y no ocultarse con una escena decorativa.

## Progresión narrativa y jugable

La progresión se expresa mediante cambios de estado observables, no mediante escenas extensas:

1. **Expectativa:** el grupo descubre el torneo y se incorpora.
2. **Preparación:** la calibración transforma dispositivos personales en controles válidos.
3. **Aprendizaje:** las primeras pruebas enseñan la relación entre gesto, física y puntuación.
4. **Comparación:** la tabla convierte resultados aislados en competencia acumulada.
5. **Presión:** la reducción de pruebas restantes hace más valioso cada resultado.
6. **Resolución:** el desempate y la coronación cierran la promesa del torneo.

El orden definitivo de los minijuegos debe apoyarse en una curva de comprensión, intensidad y espacio físico. Cambiar el orden es válido si un playtest demuestra una progresión mejor; la transición visual por sí sola no justifica el orden.

## Matriz de información del jugador

El MVP no contiene un misterio central ni un giro argumental. Aun así, debe controlar qué información conoce cada parte para evitar resultados que parezcan arbitrarios.

| Información | Verdad del sistema | Qué sabe el avatar | Qué sabe o cree el jugador | Momento de comunicación | Preparación necesaria |
|---|---|---|---|---|---|
| objetivo del torneo | gana quien obtiene más GP | compite por el campeonato | debe superar a los demás | apertura y tabla | mostrar las cinco disciplinas y el sistema de puntos |
| función de la calibración | normaliza señales y capacidad | queda “preparado” | puede creer que es un tutorial opcional | antes de calibrar | explicar que mejora equidad y lectura, no habilidad del avatar |
| calidad de un intento | depende de variables técnicas del minijuego | expresa éxito o fallo | necesita saber qué variable falló | tutorial, HUD y resultado | anticipar potencia, dirección, estabilidad, giro o timing relevantes |
| asignación de GP | depende de la posición y reglas publicadas | celebra según su puesto | espera que el score bruto determine posición | cierre de cada disciplina | mostrar orden, GP obtenidos y total acumulado |
| posibilidad de remontar | depende de puntos disponibles y desempates | mantiene reacción competitiva | puede sobreestimar o desconocer sus opciones | antes de la prueba final | mostrar diferencia de puntos sin prometer una victoria imposible |
| desempate | sigue la regla de la sección 15 | acepta el resultado final | necesita prever la regla | al explicar el torneo y antes de aplicarla | nunca introducir un criterio nuevo durante la coronación |
| degradación de sensores | puede reducir precisión o cambiar el input disponible | muestra estado neutral o de espera | podría interpretar un fallo técnico como mal desempeño | en cuanto se detecta | diferenciar claramente error, intento inválido y resultado bajo |

Una revelación de reglas sin preparación es un error de UX, no un giro narrativo. La pantalla de resultados nunca debe mostrar una causa que el tutorial y el HUD no permitieron anticipar.

## Auditoría de huecos argumentales y de experiencia

| Pregunta de auditoría | Hallazgo | Resolución o estado |
|---|---|---|
| ¿Por qué se celebra el torneo? | no se necesita una mitología extensa para justificar una competencia local | establecerlo como encuentro deportivo de la jungla; no ampliar en P0 |
| ¿Por qué existen cinco pruebas? | el número proviene del alcance de producto, no de una causa narrativa | presentarlas como el pentatlón oficial de las Gorilimpiadas |
| ¿Por qué participa cada avatar? | falta motivación individual escrita | motivación compartida: representar a su competidor y buscar el campeonato; biografías son P1 |
| ¿Por qué calibrar antes de jugar? | podría parecer una espera técnica sin función | convertirla en preparación oficial del atleta y explicar su efecto en equidad |
| ¿Por qué no omitir una prueba difícil? | el torneo perdería comparabilidad y cierre | las cinco disciplinas forman el campeonato completo del MVP |
| ¿Qué pasa si el jugador no actúa? | sin regla, la sesión podría bloquearse | aplicar temporizador, intento inválido y avance seguro según cada minijuego |
| ¿Qué pasa si un sensor falla? | podría confundirse con falta de habilidad | activar recuperación o degradación y comunicar que no fue un resultado competitivo normal |
| ¿Cómo se determina el campeón? | existe riesgo de sorpresa si el desempate aparece al final | presentar la regla de GP y desempate antes de iniciar |
| ¿Qué significa “Escape”? | no existe mecánica de escape en el loop | tratarlo como marca de plataforma; validar el nombre antes de comunicación comercial |
| ¿Hace falta un presentador? | puede añadir claridad, pero también arte, voz y animación | P0 usa UI y audio funcionales; personaje presentador completo es P1 |
| ¿Existe una solución más simple que la fusión de sensores? | sí, para algunos gestos puede bastar una fuente | usar Input Fusion sólo donde aporte valor medible; aplicar Plan B si aumenta fallos |

## Prueba de eliminación narrativa

| Elemento | Función | Qué ocurre si se elimina | Decisión |
|---|---|---|---|
| ceremonia de apertura breve | establece objetivo, reglas y tono | el grupo entra sin contexto común | conservar y mantener breve |
| avatares de gorila | identidad, legibilidad y celebración | se debilitan marca y pertenencia, pero no la mecánica | conservar cuatro variantes sin estadísticas |
| biografías individuales | añade personalidad | no cambia el loop ni el resultado | dejar en P1 |
| presentador personificado | guía y cohesiona transiciones | la UI puede cumplir la función | combinar con UI/audio en P0; personaje completo en P1 |
| tabla entre disciplinas | convierte pruebas aisladas en torneo | desaparecen progresión y tensión acumulada | conservar |
| escena narrativa entre cada prueba | puede ambientar | aumenta tiempo y producción sin cambiar el estado | eliminar de P0; usar transición funcional |
| coronación | resuelve el objetivo global | el torneo termina sin cierre | conservar |
| explicación argumental de “Escape” | conectaría la marca con una historia mayor | no afecta el campeonato | excluir del MVP y revisar el naming |

La solución preferida para un hueco es aclarar, combinar, simplificar o eliminar. No se añadirá un personaje, subtrama, escenario o cinemática únicamente para justificar una decisión que puede comunicarse con una regla o transición breve.

## Prueba previa a guion y storyboard

Antes de producir escenas o storyboard, el equipo debe poder responder por escrito:

- cuál es el objetivo del torneo y qué impide alcanzarlo de inmediato;
- qué causa cada transición y qué cambia después;
- qué sabe el jugador antes de actuar;
- cómo se comunica un resultado bajo, un intento inválido y un fallo técnico;
- por qué existe cada personaje o elemento visual importante;
- qué contenido puede eliminarse sin romper el loop;
- qué decisiones siguen pendientes y quién debe resolverlas.

Si la respuesta sólo puede darse verbalmente y no aparece en la especificación, existe un problema de documentación.

## Alcance narrativo

**P0:** identidad de torneo, ceremonia breve de apertura, transiciones entre pruebas, comentarios visuales o sonoros no verbales, progresión de tabla y coronación.  
**P1:** líneas breves de presentador, animaciones adicionales, rivalidad contextual y recapitulación de mejores momentos.  
**P2:** campaña, mundo explorable, escenas extensas, personajes con arcos propios y una explicación argumental de la palabra “Escape”.

El nombre **Gorilla Escape** funciona como marca de plataforma y **Gorilimpiadas** como nombre de esta experiencia. El MVP no debe prometer una mecánica de escape que no existe en el loop jugable.

## Principios de gameplay

1. **Fácil de entender.**
2. **Respuesta inmediata.**
3. **Difícil de dominar.**
4. **La técnica importa más que agitar el teléfono.**
5. **El teléfono nunca se suelta físicamente.**
6. **El jugador debe entender por qué obtuvo su resultado.**
7. **Cada acción importante debe producir feedback audiovisual claro.**
8. **El sistema debe degradarse de forma segura si un sensor falla.**

---

# 2. Alcance definitivo del MVP

## P0 — Obligatorio

- Unity en PC.
- 1 a 4 jugadores.
- Multijugador local.
- Modo Gorilimpiadas con marco narrativo funcional.
- 5 minijuegos:
  1. Gorilla Smash.
  2. Coconut Throw.
  3. Jungle Archery.
  4. Coconut Bowling.
  5. Jungle Slice.
- Teléfono como control mediante PWA.
- QR para conexión.
- WebSocket.
- Acelerómetro.
- Giroscopio.
- Orientación.
- Webcam.
- Tracking corporal.
- ROI.
- Player Lock.
- Calibración.
- Input Fusion cuando corresponda.
- Sistema de puntuación.
- Gorilla Points.
- Tabla general.
- Campeón.
- Reconexión básica.
- Audio y feedback básico.
- Pantalla `Modo Fiesta — Próximamente`.
- Build estable.
- Telemetría local de desarrollo.
- Pruebas funcionales y cuantitativas.

## P1 — Si hay tiempo

**Decisión PO vigente (DEC-013, T001/#10, 2026-10-08):** slow motion P1 fuera del MVP P0; no bloquea Gorilla Smash. Reconsideración sólo con decisión explícita futura del PO; sin implementación ni trabajo técnico autorizado.

- Vibración.
- Replays breves.
- Slow motion contextual.
- Más animaciones de celebración.
- Récord personal local.
- Asistencia adaptativa más avanzada.
- Pantallas móviles enriquecidas.
- Estadísticas finales.
- Accesibilidad ampliada.

## P2 — Post-MVP

- Modo Fiesta funcional.
- Multijugador por Internet.
- Matchmaking.
- Cuentas.
- Nube.
- Ranking global.
- Tienda.
- DLC real.
- Personalización avanzada.
- Tracking simultáneo complejo de varios jugadores.
- Dos cámaras.
- Cámara de profundidad.
- Reconocimiento facial.
- IA.
- Voice control.
- Aplicación móvil nativa salvo necesidad técnica.
- Más de cinco minijuegos.

## Criterio de aceptación del alcance

El MVP no se acepta por cantidad de pantallas o sistemas implementados. Se acepta cuando una persona externa completa la experiencia descrita en la sección 54, los cinco minijuegos tienen un loop cerrado y se cumplen los criterios cuantitativos de las secciones 36 y 37.

Si una P0 no puede terminarse sin comprometer estabilidad o seguridad, el Product Owner debe registrar una de estas decisiones antes del Feature Freeze:

1. simplificar su implementación conservando el resultado para el jugador;
2. activar el Plan B definido en la sección 43;
3. retirar explícitamente la función y actualizar alcance, pruebas, presentación y análisis de viabilidad.

Una característica no puede aparecer simultáneamente como P0 y como “próximamente”. La pantalla de Modo Fiesta sí es P0; el modo jugable es P2.

---

# 3. Arquitectura técnica definitiva

## 3.1 Estrategia

Para el MVP se utilizará una arquitectura **Modular Monolith**.

No se implementarán microservicios distribuidos.

Razones:

- menor complejidad;
- integración más rápida;
- debugging sencillo;
- menor riesgo de red interna;
- despliegue simple;
- adecuada para seis integrantes y siete semanas;
- permite separar responsabilidades sin pagar el costo operativo de microservicios.

## 3.2 Stack

| Componente | Tecnología |
|---|---|
| Juego | Unity + C# |
| Física | Unity Physics |
| Servidor local | Java 21 + Spring Boot |
| Comunicación móvil | WebSocket |
| Control móvil | PWA React + HTML/CSS/JavaScript |
| Datos en red | JSON compacto en MVP |
| Tracking | MediaPipe |
| Procesamiento de cámara | OpenCV |
| Persistencia | JSON local |
| Configuración Unity | ScriptableObjects |
| CI | GitHub Actions |
| Repositorio | GitHub |
| Modelado | Blender |
| UI/UX | Figma |
| Audio | Unity AudioMixer |

## 3.3 Autoridad

La **PC es la autoridad absoluta**.

El teléfono:

- captura input;
- muestra UI personal;
- solicita acciones.

El teléfono **no decide**:

- puntuaciones;
- física;
- ganador;
- posición final;
- resultados oficiales.

La cámara tampoco decide gameplay final. Produce observaciones que son interpretadas por el sistema.

## 3.4 Arquitectura lógica

```text
                        GORILLA ESCAPE PC
                               │
                     ┌─────────┴─────────┐
                     │   SessionManager  │
                     └─────────┬─────────┘
                               │
          ┌────────────────────┼────────────────────┐
          │                    │                    │
   NetworkManager       CameraManager          GameManager
          │                    │                    │
 Spring/WebSocket       MediaPipe/OpenCV           Unity
          │                    │                    │
      PhoneInput            CameraInput             │
          └─────────────┬──────┘                    │
                        │                           │
                   MotionAnalyzer                  │
                        │                           │
                    InputFusion                    │
                        │                           │
                   InputValidator                  │
                        │                           │
                     PlayerInput ──────────────────┘
                        │
                  MinigameManager
                        │
        ┌───────────────┼────────────────┐
        │               │                │
     Physics          Score           Feedback
```

---

# 4. Estructura recomendada de Unity

```text
Assets/
│
├── _Project/
│   ├── Art/
│   │   ├── Characters/
│   │   ├── Environment/
│   │   ├── Props/
│   │   ├── Materials/
│   │   ├── VFX/
│   │   └── UI/
│   │
│   ├── Audio/
│   │   ├── Music/
│   │   ├── SFX/
│   │   ├── UI/
│   │   └── Ambience/
│   │
│   ├── Prefabs/
│   ├── Scenes/
│   │   ├── Bootstrap/
│   │   ├── Menus/
│   │   └── Minigames/
│   │
│   ├── Scripts/
│   │   ├── Core/
│   │   ├── Session/
│   │   ├── Players/
│   │   ├── Network/
│   │   ├── Input/
│   │   ├── Motion/
│   │   ├── CameraTracking/
│   │   ├── Gameplay/
│   │   ├── Scoring/
│   │   ├── UI/
│   │   ├── Audio/
│   │   ├── Persistence/
│   │   └── Minigames/
│   │
│   ├── Settings/
│   └── Tests/
│
└── ThirdParty/
```

## 4.1 Servicios persistentes

Se utilizará una escena `Bootstrap` que inicializa servicios persistentes.

Servicios principales:

```text
AppManager
SessionManager
PlayerManager
NetworkManager
InputManager
CameraTrackingManager
AudioManager
PersistenceManager
TelemetryManager
SceneFlowManager
```

Estos servicios sobreviven a cambios de minijuego.

Los minijuegos no deben conocer directamente WebSocket, MediaPipe ni detalles del teléfono.

Reciben únicamente datos normalizados mediante `PlayerInput`.

---

# 5. Modelo de sesión

```csharp
public class GameSession
{
    public string SessionId;
    public SessionState State;
    public List<PlayerProfile> Players;
    public int CurrentMinigameIndex;
    public int CurrentPlayerIndex;
    public Dictionary<int, TournamentScore> Scores;
}
```

## Estados

```text
BOOT
MAIN_MENU
LOBBY
CONNECTING
CALIBRATION
STORY_INTRO
MINIGAME_INTRO
MINIGAME_PLAYING
MINIGAME_RESULT
NEXT_MINIGAME
FINAL_RESULTS
PARTY_COMING_SOON
MAIN_MENU
```

Estados excepcionales:

```text
PAUSED
TRACKING_LOST
CONNECTION_LOST
RECONNECTING
ERROR_RECOVERABLE
```

## Reglas

- Una sesión admite 1–4 jugadores.
- Los jugadores entran antes de iniciar la primera prueba.
- En el MVP no se permite agregar un nuevo jugador después de comenzar el torneo.
- Un jugador desconectado puede reconectarse.
- La partida conserva su PlayerId durante una reconexión.
- Si un teléfono se desconecta durante una acción, esa acción se cancela si aún no fue validada.
- Si Unity, como autoridad de juego de la PC, ya confirmó el resultado oficial, permanece válido. Java únicamente transporta el resultado y no calcula ni confirma scoring, ganador o estado competitivo por sí mismo.
- El host puede pausar.
- Un jugador puede recalibrarse entre intentos, nunca durante una acción activa.

---

# 6. Modelo de jugador

```csharp
public class PlayerProfile
{
    public int PlayerId;
    public string DisplayName;
    public int AvatarId;
    public int ColorId;

    public bool IsConnected;
    public bool IsReady;
    public bool IsTracked;

    public CalibrationProfile Calibration;
    public TournamentScore Tournament;
}
```

## Identidad

MVP:

- nombre;
- uno de cuatro colores;
- uno de cuatro gorilas visualmente diferenciables.

Los personajes **no tienen estadísticas diferentes**.

No habrá ventajas por avatar.

Esto evita problemas de balance.

---

# 7. Flujo multijugador

```text
Abrir juego
↓
Nueva Gorilimpiada
↓
Crear SessionId
↓
Mostrar QR
↓
Jugador 1 conecta
↓
Nombre + color/avatar
↓
Jugador 2...
↓
READY
↓
Cerrar lobby
↓
Calibración y seguridad
↓
Ceremonia breve de apertura
↓
5 pruebas
↓
Resultados
↓
Campeón
```

Cuando interviene la webcam, el gameplay será principalmente por turnos.

Mientras otro jugador juega, el teléfono muestra:

```text
TURNO DE KONGO

Coconut Throw
Intento 2/3

Tu turno: siguiente
```

---

# 8. Gorilla Protocol v1

## 8.1 Objetivo

Definir una comunicación estable entre teléfono y PC.

## 8.2 Envelope

Todo mensaje usa:

```json
{
  "version": 1,
  "type": "sensor",
  "sessionId": "AB12CD",
  "playerId": 2,
  "sequence": 12834,
  "timestamp": 1789988221,
  "payload": {}
}
```

## 8.3 Tipos de mensaje

### Cliente → servidor

```text
HELLO
JOIN
READY
CALIBRATION_SAMPLE
SENSOR
ACTION_DOWN
ACTION_UP
PAUSE_REQUEST
HEARTBEAT
RECONNECT
```

### Servidor → cliente

```text
WELCOME
JOIN_ACCEPTED
JOIN_REJECTED
PLAYER_STATE
CALIBRATION_STATE
GAME_STATE
YOUR_TURN
WAIT
FEEDBACK
RESULT
PAUSE
RESUME
ERROR
RECONNECT_ACCEPTED
```

## 8.4 Sensor payload

```json
{
  "acceleration": { "x": 0.0, "y": 0.0, "z": 0.0 },
  "gyro": { "x": 0.0, "y": 0.0, "z": 0.0 },
  "orientation": { "x": 0.0, "y": 0.0, "z": 0.0, "w": 1.0 }
}
```

## 8.5 Frecuencia

Objetivo inicial:

**50 Hz**

El servidor podrá solicitar:

```text
60 Hz
50 Hz
30 Hz
20 Hz
```

según rendimiento.

No se enviará más información de la necesaria.

## 8.6 Heartbeat

Cada cliente envía heartbeat periódicamente.

Estados sugeridos:

```text
< 1.5 s sin respuesta = degradado
< 3 s = CONNECTION_LOST
```

Los valores finales deben ajustarse durante pruebas.

## 8.7 Sequence

`sequence` permite detectar:

- paquetes atrasados;
- paquetes duplicados;
- saltos.

Un mensaje sensor viejo no debe modificar un movimiento ya procesado.

---

# 9. Contratos de input

## PhoneInput

```csharp
public struct PhoneInput
{
    public int PlayerId;

    public Vector3 Acceleration;
    public Vector3 AngularVelocity;
    public Quaternion Orientation;

    public bool ActionPressed;
    public bool IsConnected;

    public long Timestamp;
    public long Sequence;
}
```

## CameraInput

```csharp
public struct CameraInput
{
    public int PlayerId;

    public bool PlayerDetected;

    public Vector2 BodyPosition;

    public Vector2 LeftHand;
    public Vector2 RightHand;

    public Vector2 LeftArmDirection;
    public Vector2 RightArmDirection;

    public bool ThrowGesture;
    public bool SmashGesture;

    public float TrackingConfidence;

    public long Timestamp;
}
```

## PlayerMotion

```csharp
public struct PlayerMotion
{
    public float Power;             // 0..1
    public float Direction;         // -1..1
    public float Elevation;         // 0..1
    public float Spin;              // -1..1
    public float AngularVelocity;   // 0..1 normalizado
    public float Stability;         // 0..1
    public float Timing;            // 0..1
    public float GestureConfidence; // 0..1
}
```

## PlayerInput

```csharp
public struct PlayerInput
{
    public int PlayerId;

    public PlayerMotion Motion;

    public Vector2 LeftHand;
    public Vector2 RightHand;

    public bool Throw;
    public bool Smash;
    public bool Swing;

    public bool PlayerTracked;
}
```

---

# 10. Sistema de coordenadas

## Teléfono

Los datos crudos se transforman a un espacio normalizado independiente de la orientación física del dispositivo.

Antes de jugar se captura:

```text
NeutralOrientation
```

Toda orientación posterior se calcula relativamente a ese quaternion.

## Convenciones

```text
Direction
-1 = izquierda
 0 = centro
+1 = derecha

Spin
-1 = giro izquierdo
 0 = sin spin
+1 = giro derecho

Power
0 = sin acción
1 = máximo útil

Elevation
0 = horizontal/bajo
1 = máximo vertical útil
```

## Cámara

```text
X: 0 izquierda → 1 derecha
Y: 0 arriba → 1 abajo
```

La lógica de gameplay debe consumir coordenadas normalizadas, no píxeles.

---

# 11. Motion Analyzer

El `MotionAnalyzer` transforma señales crudas en intención de movimiento.

Pipeline:

```text
Raw Sensors
↓
Timestamp validation
↓
Orientation normalization
↓
Noise filtering
↓
Dead Zone
↓
Calibration normalization
↓
Feature extraction
↓
Power / Direction / Elevation / Spin / Stability
```

## Reglas

- No usar un único frame para decidir una acción.
- Analizar una ventana temporal.
- Ignorar ruido por debajo de DeadZone.
- Saturar valores extremos.
- Un movimiento físicamente absurdo no obtiene potencia ilimitada.
- La potencia útil máxima es `1.0`.

---

# 12. Input Fusion

## Objetivo

Combinar evidencia del teléfono y cámara sin hacer que los minijuegos dependan directamente de hardware específico.

Ejemplo:

```text
Phone:
Power = 0.82
Direction = 0.10
Confidence = 0.94

Camera:
ThrowGesture = true
Confidence = 0.86

Delta = 46 ms

↓
VALID THROW
```

## Reglas P0

Una acción que requiera validación cruzada será válida cuando:

```text
PhoneMotionValid
AND
CameraGestureValid
AND
abs(PhoneTimestamp - CameraTimestamp) <= SynchronizationWindow
AND
CameraConfidence >= MinimumCameraConfidence
AND
GameState == EXPECTING_ACTION
AND
Cooldown == false
```

## Valores iniciales de tuning

No son constantes definitivas:

```text
SynchronizationWindow = 250 ms
MinimumCameraConfidence = 0.60
Cooldown = 500 ms
```

Se almacenan en configuración central y se ajustan mediante testing.

## Degradación

Si la cámara falla repetidamente:

1. pausar;
2. intentar recuperación;
3. informar al jugador;
4. si no recupera, utilizar el Plan B definido para ese minijuego.

Nunca seleccionar automáticamente a otra persona.

---

# 13. Calibración adaptativa

## Objetivo

Normalizar diferencias entre:

- teléfonos;
- sensores;
- fuerza física;
- estatura;
- alcance;
- ruido;
- forma de sostener el dispositivo.

## CalibrationProfile

```csharp
public class CalibrationProfile
{
    public Quaternion NeutralOrientation;

    public float NoiseLevel;
    public float NormalAcceleration;
    public float StrongAcceleration;

    public float MotionRange;
    public float PlayerHeight;
    public float ArmReach;

    public float SensitivityScale;
}
```

## Flujo

```text
1. Entrar a ROI
2. Detectar jugador
3. Levantar brazos
4. Capturar posición corporal
5. Teléfono neutral
6. Movimiento suave
7. Movimiento normal
8. Movimiento fuerte controlado
9. Calcular perfil
10. Prueba visual
```

Objetivo:

**15–30 segundos.**

## Normalización de potencia

Conceptualmente:

```text
raw = PeakAcceleration - NoiseLevel

normalized =
(raw - NormalAcceleration)
/
(StrongAcceleration - NormalAcceleration)
```

Luego:

```text
Power = Clamp01(normalized ajustado)
```

La fórmula final puede incorporar curvas, pero debe mantenerse centralizada.

## Principio de equidad

La calibración evita que el juego sea simplemente:

> la persona físicamente más fuerte gana.

La técnica seguirá importando.

---

# 14. Configuración central

No se hardcodearán valores de tuning dentro de scripts de gameplay.

## MotionSettings

```text
DeadZone
MinimumAcceleration
MaximumAcceleration
MinimumSpin
MaximumSpin
GestureWindow
SynchronizationWindow
Cooldown
MinimumCameraConfidence
SensorRate
```

## NetworkSettings

```text
Port
HeartbeatInterval
DisconnectTimeout
SensorRate
ProtocolVersion
```

## CameraSettings

```text
Resolution
TargetFPS
ROI
MinimumConfidence
LostTrackingTimeout
```

## GameBalanceSettings

Configuración común de:

```text
Feedback thresholds
Tournament points
Tie breakers
Assistance
```

Cada minijuego tendrá su propio `ScriptableObject`.

---

# 15. Sistema competitivo — Gorilla Points

Cada minijuego mantiene su unidad natural.

Ejemplo:

```text
Smash      → puntos
Throw      → metros
Archery    → puntos
Bowling    → puntos
Slice      → puntos
```

No se suman directamente.

## GP por posición

| Posición | Gorilla Points |
|---|---:|
| 1.º | 10 GP |
| 2.º | 7 GP |
| 3.º | 5 GP |
| 4.º | 3 GP |

Para 1 jugador:

- los GP representan rendimiento;
- se muestran récords de sesión;
- no se simulan oponentes falsos.

## Desempate

Orden:

1. mayor cantidad de primeros lugares;
2. mayor cantidad de segundos lugares;
3. mayor cantidad de terceros lugares;
4. si continúa el empate: **Gorilla Smash — Sudden Smash**, un intento por jugador empatado.

---

# 16. Lenguaje global de rendimiento

Los minijuegos pueden utilizar:

```text
MISS
GOOD
GREAT
PERFECT
LEGENDARY
```

Estos niveles no sustituyen el resultado numérico.

Sirven para feedback.

## Recomendación inicial

```text
MISS       < 0.25
GOOD       0.25–0.49
GREAT      0.50–0.74
PERFECT    0.75–0.94
LEGENDARY  >= 0.95
```

Cada minijuego puede ajustar sus umbrales.

---

# 17. Mecánica definitiva — Gorilla Smash

## Objetivo

Ejecutar un golpe descendente potente, recto y correctamente sincronizado.

## Intentos

**3 por jugador.**

## Variables

```text
Power
DirectionAccuracy
Timing
GestureConfidence
```

## Score técnico normalizado

Propuesta inicial:

```text
Technique =
Power             * 0.45 +
DirectionAccuracy * 0.25 +
Timing            * 0.20 +
GestureConfidence * 0.10
```

## Puntuación visible

```text
Score = round(Technique * 10,000)
```

Máximo:

**10,000**

## DirectionAccuracy

```text
1.0 = trayectoria descendente ideal
0.0 = dirección completamente incorrecta
```

## Anti-cheat mecánico

- agitar repetidamente no cuenta;
- requiere estado `ARMED`;
- requiere patrón descendente;
- requiere cooldown;
- valores extremos se saturan;
- la cámara confirma el gesto.

## Feedback

```text
< 2500    MISS / WEAK
2500–4999 GOOD
5000–7499 GREAT
7500–9499 PERFECT
9500+     LEGENDARY
```

---

# 18. Mecánica definitiva — Coconut Throw

## Objetivo

Obtener máxima distancia mediante potencia, elevación y release.

## Intentos

**3 por jugador.**

## Variables

```text
Power
Elevation
Direction
ReleaseTiming
GestureConfidence
```

## Ángulo

Ángulo objetivo inicial:

**aprox. 40–45° virtuales**

No se utilizará una física puramente académica; se utilizará una curva jugable.

## Quality

Propuesta:

```text
AngleQuality   = 1 - normalizedDistanceFromOptimalAngle
ReleaseQuality = Timing
DirectionQuality = 1 - abs(Direction)
```

## Velocidad inicial

```text
LaunchPower =
Power * 0.60 +
AngleQuality * 0.20 +
ReleaseQuality * 0.15 +
DirectionQuality * 0.05
```

La física de Unity transforma `LaunchPower` y `Elevation` en velocidad inicial.

## Reglas

- demasiado alto pierde distancia;
- demasiado bajo pierde distancia;
- movimiento fuerte con mal release pierde rendimiento;
- movimiento controlado técnicamente correcto puede superar uno más fuerte.

## Resultado

Principal:

**metros.**

Bonus visuales:

- Perfect Angle.
- Perfect Release.
- Personal Best.

---

# 19. Mecánica definitiva — Jungle Archery

## Objetivo

Apuntar utilizando orientación del teléfono y disparar manteniendo estabilidad.

## Flechas

**5 por jugador.**

## Control

```text
Orientar teléfono → mover mira
Mantener botón → tensar
Mantener estable
Soltar → disparar
```

## Variables

```text
Yaw
Pitch
Stability
HoldTime
ReleaseTiming
```

## Stability

Se calcula mediante variación angular durante una ventana previa al disparo.

Conceptualmente:

```text
menor variación angular = mayor Stability
```

## Zonas

| Zona | Puntos |
|---|---:|
| Bullseye | 100 |
| Interior | 75 |
| Media | 50 |
| Exterior | 25 |
| Fallo | 0 |

## Técnica

Una estabilidad alta:

- reduce dispersión.

Una estabilidad baja:

- incrementa dispersión.

No se debe mover artificialmente el proyectil después del disparo.

---

# 20. Mecánica definitiva — Coconut Bowling

## Objetivo

Derribar objetivos mediante dirección, potencia y spin.

## MVP

**3 rondas por jugador.**

## Variables

```text
Power
Direction
Elevation
Spin
ReleaseTiming
```

## Conversión conceptual

```text
ForwardVelocity = Power
HorizontalVelocity = Direction
Torque = Spin
```

## Límites

```text
Spin = -1 .. +1
```

Los valores extremos producen hook notable pero controlable.

## Regla de habilidad

Mucho spin no debe ser automáticamente mejor.

Exceso de spin puede:

- sacar el coco de trayectoria;
- reducir control;
- provocar fallo.

## Tipos emergentes

- recto;
- hook izquierdo;
- hook derecho;
- fuerte;
- controlado.

No se seleccionan mediante menú: emergen del movimiento.

## Scoring

MVP:

- objetos derribados;
- Strike;
- Spare.

La lógica debe ser consistente entre jugadores.

---

# 21. Mecánica definitiva — Jungle Slice

## Objetivo

Cortar frutas mediante trayectoria real de manos.

## Duración

**60 segundos.**

## Input

Cámara.

## Variables

```text
LeftHand
RightHand
HandVelocity
Trajectory
TrackingConfidence
```

## Corte válido

Un corte requiere:

```text
TrackingConfidence válido
AND
HandVelocity >= MinimumCutVelocity
AND
segmento de trayectoria intersecta hitbox
```

## Scoring inicial

```text
Fruta normal       +100
Multi-cut          +50 adicional por fruta desde la segunda
Combo              multiplicador progresivo limitado
Objeto negativo    -250
```

## Combo

Ejemplo inicial:

```text
3 cortes consecutivos  x1.2
5 cortes               x1.5
10 cortes              x2.0 máximo
```

Un fallo o golpe a objeto negativo reinicia combo.

## Objetos negativos

- piedra;
- fruta podrida;
- colmena.

La colmena puede producir feedback fuerte, pero no una mecánica secundaria compleja en MVP.

---

# 22. Dificultad y asistencia

## MVP

No habrá menú:

```text
Easy / Normal / Hard
```

Se utilizará una dificultad base común.

## Asistencia adaptativa P1

Puede ajustar discretamente:

- sensibilidad;
- tolerancia angular;
- estabilidad;
- dead zone;
- ventana de timing.

Nunca:

- alterar arbitrariamente el ganador;
- regalar puntos;
- falsificar un resultado.

---

# 23. Tutoriales

Cada minijuego tendrá:

1. explicación visual;
2. demostración;
3. práctica libre breve;
4. confirmación `READY`;
5. competencia.

Objetivo:

**tutorial de 10–20 segundos**, sin contar práctica voluntaria.

## Ejemplo Bowling

```text
1. Mantén presionado para preparar.

2. Realiza el movimiento de boliche
   sin soltar el teléfono.

3. Gira la muñeca para aplicar spin.

4. Suelta el botón para liberar el coco.
```

---

# 24. UX del teléfono

## Estados

```text
CONNECT
PLAYER_SETUP
CALIBRATION
WAITING
YOUR_TURN
GAME_CONTROL
RESULT
PAUSED
CONNECTION_LOST
RECONNECTING
```

## Pantalla WAITING

Debe mostrar:

- jugador actual;
- minijuego;
- intento/ronda;
- posición propia;
- cuándo será su turno.

## YOUR_TURN

Debe ser imposible no notar que corresponde jugar.

Mostrar:

- nombre;
- color;
- instrucción;
- botón principal.

## GAME_CONTROL

Sólo mostrar controles relevantes.

Evitar UI innecesaria mientras se mueve el teléfono.

## RESULT

Mostrar:

- resultado;
- feedback;
- GP si corresponde;
- posición provisional.

---

# 25. UX de errores y recuperación

Todo error recuperable debe responder:

1. qué ocurrió;
2. qué debe hacer el jugador;
3. si la partida está segura.

## Casos obligatorios

### Teléfono desconectado

```text
Conexión perdida

Tu partida está pausada.
Reconectando...
```

### Tracking perdido

```text
No podemos verte

Regresa a la zona marcada
y mira hacia la cámara.
```

### Permiso de sensores

```text
Necesitamos acceso a los sensores
de movimiento para usar este teléfono
como control.

[Permitir movimiento]
```

### Cámara no disponible

No iniciar una prueba que la requiera.

### Jugador incorrecto entra al ROI

Mantener Player Lock.

No transferir control.

---

# 26. Game Feel

Toda acción relevante debe combinar, según intensidad:

```text
SFX
VFX
Animation
Camera
UI
Haptics
Crowd
Score
```

## Intensidad

### GOOD

- SFX;
- score pop.

### GREAT

- SFX;
- partículas;
- reacción leve.

### PERFECT

- SFX especial;
- partículas;
- animación;
- cámara;
- público.

### LEGENDARY

- micro slow-motion;
- impacto visual;
- camera shake controlado;
- partículas especiales;
- crowd reaction;
- animación de celebración;
- resultado destacado.

## Regla

El slow motion no debe romper la sincronización de input ni hacer lenta la sesión.

**Aclaración PO DEC-013:** la mención micro slow-motion de LEGENDARY es una opción P1 futura, no un requisito MVP P0 ni un bloqueo para Gorilla Smash. No implementarlo sin nueva autorización.

---

# 27. Cámara virtual de Unity

## Gorilla Smash

- encuadre lateral/3⁄4;
- pequeño zoom al impacto;
- shake proporcional;
- breve enfoque al resultado.

## Coconut Throw

- inicio mostrando jugador;
- transición al coco;
- seguimiento de trayectoria;
- resultado al aterrizar.

## Jungle Archery

- mira clara;
- zoom breve al impacto;
- bullseye destacado.

## Coconut Bowling

- cámara detrás/lateral;
- seguimiento suave del coco;
- énfasis en impacto final.

## Jungle Slice

- cámara principalmente fija;
- lectura clara de objetos;
- mínimo movimiento de cámara.

---

# 28. Personajes

## MVP

Cuatro gorilas base.

Cada gorila representa visualmente a un participante; no es un personaje autónomo que tome decisiones por él. Su función es:

- identificar de inmediato quién está jugando;
- reflejar preparación, esfuerzo, resultado y celebración;
- mantener la coherencia temática del torneo;
- hacer legible la competencia para quienes observan.

Diferenciación:

- color;
- accesorio;
- silueta secundaria;
- expresiones.

No diferencias de estadísticas.

## Motivación y conocimiento

Los cuatro avatares comparten una motivación funcional: competir en las Gorilimpiadas y buscar el campeonato en representación del jugador. En P0 no necesitan biografías, secretos ni conflictos personales.

El avatar sólo “conoce” lo que el sistema ya comunicó al jugador. Sus reacciones no deben insinuar reglas, resultados o información todavía no presentada. Una animación de celebración o derrota responde al resultado calculado; no lo anticipa.

## Personaje guía o presentador

El MVP no requiere un quinto personaje modelado. Las funciones de guía pueden resolverse mediante UI, voz, texto y audio:

- presentar el objetivo general;
- anunciar turno y disciplina;
- explicar el gesto;
- comunicar error o recuperación;
- actualizar la tabla;
- coronar al campeón.

Un presentador con nombre, modelo, diálogos y animaciones propias es P1. Sólo debe incorporarse si sustituye interfaces existentes sin duplicarlas y no pone en riesgo las animaciones P0 de los competidores.

## Regla de función narrativa

Antes de crear un personaje, accesorio o animación adicional se debe responder qué información comunica, qué cambio representa o qué necesidad jugable resuelve. Si al eliminarlo no cambia comprensión, identidad, feedback o progresión, no pertenece al MVP.

## Animaciones P0

```text
Idle
Ready
Celebrate
Lose
Smash
Throw
Aim
Bowling
Slice/Reaction
HitReaction
```

## P1

```text
Walk
Run
Jump
MultipleCelebrations
Taunts
```

---

# 29. Art Bible

## Dirección

**Low-poly estilizado tropical.**

## Reglas

- formas grandes;
- siluetas claras;
- pocos detalles pequeños;
- lectura desde TV/proyector;
- materiales simples;
- colores diferenciables;
- expresiones exageradas;
- UI grande;
- evitar realismo.

## Entornos

```text
Gorilla Smash      Arena de piedra / tótems
Coconut Throw      Estadio tropical
Jungle Archery     Bosque / ruinas
Coconut Bowling    Pista de bambú
Jungle Slice       Mercado / zona frutal
```

## Pipeline

```text
Concept
↓
Model
↓
UV
↓
Material
↓
Rig
↓
Animation
↓
FBX
↓
Unity Import
↓
Prefab
↓
Validation
```

## Convención de nombres

```text
CHR_Gorilla_01
ENV_Palm_01
PROP_Coconut_01
PROP_BambooPin_01
UI_Icon_Spin
VFX_PerfectImpact
SFX_CoconutImpact_01
```

---

# 30. Audio

## AudioMixer

```text
MASTER
├── MUSIC
├── SFX
├── UI
├── CHARACTER
└── AMBIENCE
```

## Categorías

- música;
- ambiente;
- UI;
- impacto;
- personaje;
- público;
- feedback.

## Reglas

- feedback importante debe distinguirse del ambiente;
- no saturar con efectos simultáneos;
- `Perfect` y `Legendary` deben ser reconocibles por sonido;
- licencias compatibles con el proyecto;
- registrar atribución cuando aplique.

---

# 31. Accesibilidad

## P0

- texto grande;
- no depender sólo de color;
- instrucciones visuales + texto;
- sensibilidad calibrada;
- soporte para diferentes estaturas;
- volumen ajustable;
- UI legible desde distancia.

## P1

- reducir camera shake;
- modo de movimiento reducido;
- soporte zurdo explícito;
- subtítulos ampliados;
- contraste aumentado.

---

# 32. Seguridad física

## Regla global

> Ningún minijuego requiere soltar físicamente el teléfono.

## Pantalla inicial

Debe indicar:

- sostener firmemente el teléfono;
- asegurar espacio libre;
- evitar golpear personas u objetos;
- realizar movimientos controlados;
- usar correa si está disponible.

## Protección por software

- saturación de potencia;
- movimientos absurdamente violentos no dan ventaja adicional;
- pausa si se pierde tracking;
- no incentivar lanzamientos reales del teléfono.

---

# 33. Privacidad

Para el MVP:

- procesamiento de cámara local;
- no reconocimiento facial;
- no identificación biométrica;
- no guardar video;
- no guardar fotografías;
- no enviar video a Internet;
- telemetría de desarrollo local;
- no requiere cuenta personal.

La cámara se usa para posición y movimiento corporal, no identidad.

---

# 34. Persistencia

## MVP

JSON local.

Guardar:

- configuración;
- volumen;
- ajustes técnicos;
- récords locales opcionales;
- última calibración sólo si es seguro reutilizarla durante la sesión.

No guardar datos personales innecesarios.

## Archivo de sesión

Puede almacenar:

```json
{
  "sessionId": "AB12CD",
  "date": "2026-10-15",
  "players": [],
  "results": []
}
```

---

# 35. Telemetría local de desarrollo

## Objetivo

Balancear y diagnosticar.

Registrar:

```text
Timestamp
SessionId
PlayerId
Minigame
Latency
TrackingConfidence
Power
Direction
Elevation
Spin
Timing
Result
Score
ErrorCode
```

Ejemplo:

```json
{
  "minigame": "CoconutThrow",
  "playerId": 2,
  "power": 0.82,
  "elevation": 0.61,
  "release": 0.94,
  "latencyMs": 67,
  "distance": 38.4
}
```

## Regla

La telemetría no debe afectar perceptiblemente el framerate.

Escribir en buffer y persistir de manera controlada.

---

# 36. Rendimiento

## Objetivos

| Métrica | Objetivo |
|---|---:|
| Unity | 60 FPS |
| Webcam | ≥30 FPS |
| Sensores | 50 Hz objetivo |
| Calibración | <30 s |
| Latencia P95 | <150 ms |
| Latencia ideal | <100 ms |
| Crash | 0 por sesión |
| Cambio accidental de jugador | 0 |

## Presupuesto conceptual de latencia

```text
Phone sampling       0–20 ms
Network LAN          5–30 ms
Server processing    <10 ms
Motion/Fusion        <20 ms
Unity response       <17 ms
--------------------------------
Objetivo total       <100–150 ms
```

No son garantías individuales; sirven para diagnóstico.

---

# 37. QA cuantitativo

## Conectividad

- 20 conexiones iniciales consecutivas.
- ≥95% sin intervención técnica.
- 20 reconexiones.
- ≥95% exitosas.

## Sensores

- 100 movimientos pequeños.
- objetivo: ≤2 falsos positivos.

## Acciones

Por gesto crítico:

- 50 intentos válidos.
- objetivo inicial: ≥95% detectados.

## Player Lock

- otra persona entra al frame durante prueba.
- 20 intentos.
- objetivo: 0 cambios accidentales de jugador.

## Sesión

- 10 sesiones completas.
- objetivo: 0 crashes.
- ninguna sesión debe entrar en estado imposible.

## Latencia

Medir P50 y P95.

Objetivo:

```text
P95 < 150 ms
```

## Jugabilidad

Al menos 5 personas que no hayan programado el juego deben intentar:

```text
conectar
calibrar
entender una prueba
jugar
interpretar resultado
```

sin explicación técnica paso a paso.

---

# 38. Estrategia de testing

## Unit Tests

Priorizar:

- normalización;
- scoring;
- GP;
- desempates;
- validación;
- transformación de datos.

## Integration Tests

- WebSocket → PhoneInput;
- CameraInput → InputFusion;
- PlayerInput → minijuego;
- resultados → torneo.

## PlayMode Tests

- flujo de escenas;
- sesión;
- reinicio;
- resultado;
- desconexión.

## Manual

Obligatorio para:

- sensores;
- cámara;
- movimiento físico;
- game feel;
- UX.

---

# 39. CI/CD

## Branches

```text
main
└── develop
    ├── feature/gameplay-...
    ├── feature/mobile-...
    ├── feature/camera-...
    ├── feature/ui-...
    └── fix/...
```

## Pull Request

Debe:

- compilar;
- no introducir errores críticos;
- tener descripción;
- vincular tarea;
- pasar pruebas disponibles;
- ser revisado.

## Pipeline

```text
Push / PR
↓
Restore
↓
Static validation
↓
Tests
↓
Build validation
↓
PR approval
↓
Merge develop
```

## Release

```text
develop
↓
integration test
↓
main
↓
tag
↓
build
```

---

# 40. Logging y errores

## Niveles

```text
INFO
WARNING
ERROR
CRITICAL
```

## Códigos

Ejemplos:

```text
NET_001 PhoneDisconnected
NET_002 ProtocolMismatch
CAM_001 CameraUnavailable
CAM_002 TrackingLost
INPUT_001 InvalidGesture
INPUT_002 SensorTimeout
GAME_001 InvalidState
```

Los errores técnicos se registran.

El jugador recibe mensajes comprensibles, no stack traces.

---

# 41. Hardware de referencia

Debe elegirse antes de cerrar Sprint 1:

1. PC oficial.
2. Webcam oficial.
3. Android de referencia.
4. iPhone de referencia.
5. router/hotspot oficial.
6. TV/proyector objetivo.

## Regla

> Una funcionalidad P0 no está completamente Done hasta probarse en el hardware oficial de presentación cuando dicho hardware sea relevante.

---

# 42. Spike 0 obligatorio

Antes de invertir en contenido completo, validar:

```text
Teléfono
↓
QR
↓
PWA
↓
Permiso sensores
↓
WebSocket
↓
50 Hz
↓
PC
↓
Unity
```

y:

```text
Webcam
↓
MediaPipe
↓
Jugador
↓
Gesture
↓
Unity
```

## Criterio de salida

Construir un **Vertical Slice de Gorilla Smash**:

```text
Abrir juego
↓
Conectar teléfono
↓
Calibrar
↓
Detectar jugador
↓
Preparar golpe
↓
Movimiento teléfono + cámara
↓
Input Fusion
↓
Validación
↓
Golpe Unity
↓
Puntuación
↓
Feedback
↓
Resultado
```

Si esto funciona, la arquitectura base está validada.

---

# 43. Plan B técnico

## PWA no accede correctamente a sensores

Plan:

1. verificar permisos/contexto seguro;
2. simplificar API;
3. probar navegadores objetivo;
4. si sigue bloqueando P0: control nativo mínimo.

No construir app nativa por adelantado.

## MediaPipe demasiado lento

1. reducir resolución;
2. reducir ROI;
3. limitar landmarks;
4. 30 FPS;
5. tracking torso/manos solamente.

## Player Lock inestable

Plan B:

- zona física exclusiva;
- jugador activo único;
- confirmación manual `READY`.

## Spin inconsistente

Plan B:

```text
spin < -threshold = LEFT
abs(spin) <= threshold = STRAIGHT
spin > threshold = RIGHT
```

## Wi-Fi problemático

- router local dedicado;
- hotspot local;
- reducir frecuencia de sensores si es necesario.

## Cinco juegos en riesgo

No eliminar inmediatamente un juego.

Primero reducir:

1. profundidad secundaria;
2. VFX avanzados;
3. animaciones extra;
4. combos;
5. narrativa;
6. replays;
7. asistencia avanzada.

Mantener el loop completo.

---

# 44. Modelo de negocio, factibilidad y viabilidad

## 44.1 Naturaleza y alcance del análisis

Esta sección formaliza a **Gorilla Escape: Gorilimpiadas** como idea de negocio para el segmento de videojuegos y complementa, sin modificar, el alcance técnico y funcional definido en las secciones 0–43.

Las cantidades financieras de esta sección son **estimaciones académicas expresadas en pesos mexicanos (MXN) de 2026**. No son cotizaciones, estados financieros, compromisos de gasto ni datos reales comprobados. Deben sustituirse por cotizaciones y condiciones comerciales vigentes antes de tomar una decisión de inversión o publicación. Las cifras no incluyen impuesto sobre la renta y utilizan un modelo simplificado para fines de evaluación.

Reglas de coherencia:

- el análisis comercial no agrega características al MVP;
- las prioridades P0/P1/P2 y el Feature Freeze del 6 de noviembre de 2026 permanecen vigentes;
- el producto evaluado conserva Unity + C#, PC autoritativa, PWA, WebSocket, Java 21 + Spring Boot, MediaPipe/OpenCV, procesamiento local, 1–4 jugadores, cinco minijuegos y un equipo de seis integrantes;
- las líneas de ingreso post-MVP no se consideran terminadas ni disponibles en v1.0;
- ninguna meta de ventas justifica introducir una función P1 o P2 que ponga en riesgo una P0.

## 44.2 Descripción formal de la idea de negocio

**Gorilla Escape: Gorilimpiadas** es un videojuego premium de fiesta y competencia física para PC, diseñado para sesiones locales de uno a cuatro jugadores. La PC ejecuta y arbitra el juego; una webcam convencional observa gestos corporales; y cada participante utiliza su teléfono, mediante una PWA accesible con QR, como control de movimiento. El sistema combina sensores móviles y visión por computadora para transformar potencia, dirección, elevación, giro, estabilidad y timing en resultados jugables dentro de cinco pruebas.

La idea de negocio consiste en distribuir digitalmente el juego base para uso doméstico y, después de validar el MVP, ampliar la marca mediante contenido adicional y licencias de uso para eventos o espacios recreativos. El producto no vende hardware propietario ni exige una cuenta o conexión a Internet durante el gameplay. Su promesa comercial se apoya en aprovechar dispositivos que el público objetivo potencialmente ya posee.

El MVP comercialmente evaluable es exactamente el definido en P0:

- 1–4 jugadores;
- torneo local con cinco minijuegos;
- control por PWA y conexión mediante QR;
- sensores de teléfono y webcam;
- calibración, tracking, Input Fusion, puntuación y Gorilla Points;
- feedback, resultados, reconexión básica y build estable.

## 44.3 Problema u oportunidad identificada

El mercado de entretenimiento social en casa presenta una oportunidad entre dos extremos:

1. videojuegos convencionales que ofrecen profundidad y pulido, pero cuya interacción suele limitarse a mandos tradicionales; y
2. experiencias de movimiento que pueden exigir hardware dedicado, periféricos descontinuados, consolas concretas o una instalación poco accesible.

También existe fricción en reuniones donde varias personas desean participar, pero no todas poseen un control compatible o experiencia previa. A la vez, los teléfonos con sensores de movimiento y las webcams son recursos extendidos que normalmente no se integran como una sola interfaz de juego físico.

La oportunidad es convertir esos dispositivos de propósito general en una experiencia local compartida, comprensible para público casual y con suficiente lectura técnica del movimiento para que la habilidad importe. El proyecto no presupone que toda persona tenga PC y webcam compatibles; por ello, su mercado objetivo se limita a hogares, grupos o espacios que sí cumplan los requisitos mínimos.

## 44.4 Justificación del proyecto

La propuesta se justifica por cinco razones:

1. **Reutilización de hardware.** Reduce la necesidad de fabricar, distribuir o mantener un periférico propietario.
2. **Valor social.** El formato por turnos y la tabla de torneo favorecen una experiencia compartida frente a una sola pantalla.
3. **Diferenciación técnica con propósito jugable.** La combinación de teléfono y cámara no se plantea como demostración tecnológica, sino como medio para que la técnica real afecte el resultado.
4. **Alcance académico ejecutable.** La arquitectura monolítica modular, la autoridad en PC y el procesamiento local reducen dependencias externas y son proporcionales a seis integrantes y siete semanas.
5. **Potencial de extensión.** Si el núcleo se valida, la misma plataforma puede soportar nuevos packs, modos y usos, todos fuera del MVP y sujetos a evidencia de mercado.

En términos académicos, el proyecto permite demostrar diseño de producto, redes locales, sensores, visión por computadora, UX multiplataforma, física, QA y evaluación financiera dentro de una solución coherente.

## 44.5 Propuesta de valor

> Convertir una PC, una webcam y los teléfonos que el grupo ya posee en una plataforma de videojuegos físicos multijugador, sin periférico propietario y sin Internet durante la partida, donde la técnica del movimiento real modifica de forma comprensible el resultado.

La propuesta combina cuatro beneficios:

- **acceso:** incorporación mediante QR y PWA, sin instalar una aplicación móvil nativa en el MVP;
- **participación:** de uno a cuatro jugadores en una misma sesión local;
- **habilidad:** potencia, dirección, timing, estabilidad y spin tienen consecuencias visibles;
- **privacidad y autonomía:** el video se procesa localmente, no se almacena y la PC conserva la autoridad.

## 44.6 Segmentos de clientes y mercado objetivo

### Mercado objetivo inicial

| Segmento | Contexto de uso | Necesidad principal | Condición de encaje |
|---|---|---|---|
| Familias con PC | convivencia en casa | actividad compartida y fácil de aprender | espacio libre, webcam y teléfonos compatibles |
| Amigos y jóvenes adultos | reuniones o fiestas pequeñas | competencia breve, social y observable | pantalla común y red local estable |
| Jugadores casuales | sesiones esporádicas | baja barrera de entrada | onboarding claro y calibración rápida |
| Aficionados a party games | compra digital para PC | novedad en controles y rejugabilidad social | interés por juego local y movimiento |

El comprador probable es quien posee o administra la PC. Los usuarios son todas las personas que participan en la sesión. Por ello, la comunicación comercial debe explicar tanto el valor para quien compra como la facilidad para quienes se incorporan con su teléfono.

### Segmentos futuros, no incluidos en el MVP

- escuelas;
- centros recreativos;
- fiestas y eventos;
- activaciones de marca;
- torneos locales organizados.

Estos segmentos requerirían validación, términos de licencia, soporte y posiblemente controles operativos adicionales. No deben presentarse como ingresos asegurados del MVP.

## 44.7 Necesidades que resuelve

- disponer de una actividad grupal donde varias personas alternen y observen resultados;
- jugar con movimiento sin comprar un control especializado fabricado por el proyecto;
- facilitar la incorporación de invitados mediante un teléfono y un QR;
- ofrecer feedback que explique la relación entre el movimiento y el resultado;
- mantener datos de cámara y telemetría de desarrollo en el entorno local;
- brindar competencia accesible sin ventajas estadísticas comprables;
- proporcionar una experiencia que pueda demostrarse en aula, hogar o evento controlado.

## 44.8 Diferenciadores y ventajas competitivas

### Diferenciadores del producto

1. **Fusión multimodal:** el teléfono aporta mediciones inerciales y la webcam aporta contexto corporal.
2. **Sin periférico propietario:** se utiliza hardware general ya disponible en el escenario objetivo.
3. **PWA como control:** el ingreso mediante QR reduce fricción frente a una aplicación móvil dedicada.
4. **Procesamiento local:** el gameplay no depende de servicios en nube y no conserva imágenes.
5. **PC autoritativa:** puntuación, física y ganadores se resuelven en una sola autoridad.
6. **Calibración adaptativa:** se normalizan diferencias de dispositivos y capacidad física.
7. **La técnica importa:** agitar el teléfono no sustituye dirección, timing, estabilidad o gesto válido.
8. **Arquitectura reusable:** los minijuegos consumen `PlayerInput` normalizado y no conocen el hardware.

### Ventajas competitivas de Gorilla Escape: Gorilimpiadas

- menor dependencia de ecosistemas cerrados que las experiencias ligadas a una consola concreta;
- barrera de incorporación grupal menor que comprar varios controles convencionales;
- mayor riqueza de señal que una solución basada únicamente en cámara o únicamente en teléfono;
- privacidad más clara por su procesamiento local y ausencia de cuenta en el MVP;
- temática, arte y torneo coherentes alrededor de cinco disciplinas, en vez de una colección sin marco común;
- base técnica que permite reutilizar conexión, calibración, scoring y feedback en contenido futuro.

Estas ventajas son hipótesis a validar con pruebas de usuario. No constituyen por sí solas evidencia de demanda.

## 44.9 Posicionamiento del producto

Posicionamiento propuesto:

> Para familias, amigos y jugadores casuales con una PC que buscan una actividad social activa, Gorilla Escape: Gorilimpiadas es un party game físico local que convierte teléfonos y webcam en controles coordinados. A diferencia de experiencias que requieren periféricos o un ecosistema de consola específico, combina sensores y visión local para que la técnica sea visible, competitiva y fácil de compartir.

El producto se posicionará como:

- **videojuego premium accesible**, no free-to-play;
- **party game físico local**, no simulador deportivo;
- **experiencia tecnológica sencilla para el usuario**, aunque su implementación sea avanzada;
- **complemento para reuniones**, no sustituto de entrenamiento profesional ni dispositivo médico.

## 44.10 Competidores y productos de referencia

La comparación se usa para orientar diseño y posicionamiento; no implica equivalencia exacta ni evaluación de versiones, precios o disponibilidad actuales.

| Referencia | Aprendizaje relevante | Diferencia frente a Gorilimpiadas |
|---|---|---|
| Wii Sports / Nintendo Switch Sports | lectura inmediata de deportes y valor social del movimiento | dependen de hardware y ecosistema específicos; Gorilimpiadas usa PC, webcam y teléfonos |
| Kinect Sports | control corporal visible y atractivo para espectadores | Kinect se apoya en sensor dedicado; Gorilimpiadas utiliza webcam convencional y teléfono |
| Just Dance | onboarding social, movimiento y sesiones compartidas | prioriza coreografía; Gorilimpiadas prioriza cinco pruebas, física, timing y torneo |
| WarioWare: Move It! | instrucciones breves y variedad de gestos | se basa en consola y controles dedicados; el proyecto usa infraestructura web local |
| Jackbox Party Pack | incorporación por teléfono y valor para reuniones | el teléfono suele ser interfaz de selección; aquí también es sensor físico combinado con cámara |
| Juegos independientes de webcam o móvil como control | validan interés en interfaces alternativas | Gorilimpiadas busca una capa unificada de calibración, Input Fusion y competencia |

### Comparación competitiva por atributo

| Atributo | Gorilimpiadas | Consola con motion controllers | Cámara dedicada | Party game con teléfono |
|---|---|---|---|---|
| Hardware propietario del producto | No | Sí o ligado al ecosistema | Frecuente | No |
| Teléfono como sensor de movimiento | Sí | No necesariamente | No | Variable |
| Webcam para contexto corporal | Sí | No necesariamente | Sí | Poco común |
| Fusión teléfono + cámara | Sí | Poco común | No | Poco común |
| Juego local 1–4 | Sí | Frecuente | Frecuente | Frecuente |
| Internet durante gameplay | No requerido | No siempre | No siempre | Frecuente en algunas soluciones |
| Procesamiento visual local | Sí | Depende del sistema | Generalmente local | No aplica o depende |
| Cinco minijuegos en MVP | Sí | Variable | Variable | Variable |

## 44.11 Barreras de entrada y defensibilidad

### Barreras para entrar al mercado

- alcanzar latencia y detección suficientemente consistentes entre distintos teléfonos;
- integrar PWA, red local, Unity y tracking sin convertir la instalación en soporte técnico;
- producir tutoriales y calibración que funcionen para público no técnico;
- lograr contenido audiovisual y game feel competitivos con recursos limitados;
- conseguir visibilidad en tiendas digitales;
- generar confianza sobre seguridad física y privacidad.

### Defensibilidad potencial

- conocimiento acumulado de calibración, normalización e Input Fusion;
- biblioteca de perfiles de tuning y pruebas en hardware diverso;
- arquitectura reutilizable para nuevos minijuegos;
- identidad de marca y lenguaje audiovisual propios;
- telemetría local agregada de pruebas, sin conservar video ni datos innecesarios;
- comunidad y catálogo, sólo si llegan a construirse después del MVP.

El concepto general de usar teléfonos o cámaras no es exclusivo. La defensa depende de calidad de ejecución, contenido, marca y aprendizaje operativo, no de asumir una exclusividad tecnológica.

## 44.12 Factibilidad técnica

### Evaluación

La factibilidad técnica es **condicionada pero razonable** porque cada componente principal dispone de tecnología conocida y el diseño reduce complejidad:

- Unity + C# concentra gameplay, física, escenas y UI de PC;
- Java 21 + Spring Boot y WebSocket resuelven la sesión local;
- la PWA captura sensores y evita desarrollar dos aplicaciones móviles nativas;
- MediaPipe y OpenCV cubren tracking y preprocesamiento;
- la PC es autoridad única;
- el monolito modular evita coordinación de microservicios;
- el procesamiento local elimina dependencia de nube durante la partida;
- el gameplay por turnos reduce la dificultad del tracking simultáneo.

### Evidencia mínima requerida

La factibilidad no se declara demostrada hasta completar Spike 0 y el Vertical Slice de Gorilla Smash descritos en la sección 42. Deben comprobarse:

- permisos de sensores en navegadores objetivo;
- conexión QR/PWA/WebSocket;
- muestreo estable hasta el objetivo de 50 Hz o degradación configurada;
- tracking de cámara a ≥30 FPS en hardware de referencia;
- sincronización teléfono–cámara dentro de la ventana de configuración;
- latencia P95 <150 ms;
- Player Lock sin cambios accidentales en la prueba definida;
- recuperación de desconexión y tracking perdido;
- scoring reproducible y comprensible.

### Restricciones técnicas

- compatibilidad desigual de sensores y permisos entre teléfonos;
- necesidad de contexto seguro para ciertas APIs web;
- iluminación, espacio y posición de cámara;
- variación de rendimiento de PC;
- ruido de red Wi-Fi;
- tuning aún pendiente de pruebas reales.

### Dictamen técnico

**Factible con puerta de validación.** Si el Vertical Slice no cumple los criterios, deben aplicarse los Planes B de la sección 43 antes de producir los otros cuatro minijuegos. No se debe ampliar alcance para compensar una falla del núcleo.

## 44.13 Factibilidad operativa

### Capacidad del equipo

El equipo de seis integrantes tiene responsabilidades complementarias:

| Frente | Responsable principal | Resultado operativo esperado |
|---|---|---|
| Producto y alcance | Josue | prioridades, aceptación y documento maestro |
| Proceso | Erik | tablero, riesgos, ceremonias e impedimentos |
| Arte y UI | Saul | assets, UI PC/móvil, VFX e integración visual |
| Unity y gameplay | Arturo | sistemas de juego, física, scoring y torneo |
| Móvil y red | Hiram | PWA, Java 21 + Spring Boot, protocolo y sensores |
| Cámara | Austin | MediaPipe, OpenCV, ROI, tracking y gestos |

La integración crítica de Hiram, Austin y Arturo se realiza desde el Vertical Slice, no al final.

### Operación de desarrollo

- repositorio y CI centralizados;
- ramas, PR, revisión y Definition of Done definidos;
- hardware oficial seleccionado antes de cerrar Sprint 1;
- pruebas unitarias, de integración, PlayMode y manuales;
- telemetría local para diagnóstico;
- Feature Freeze dedicado a estabilidad, documentación y evidencias.

### Operación del producto

En el uso doméstico, la PC funciona como host. El usuario inicia una sesión, muestra el QR, conecta teléfonos, calibra y juega. No se requiere operar un backend remoto en el MVP. La atención posterior al lanzamiento se concentraría en:

- documentación de compatibilidad;
- guía de red local, espacio e iluminación;
- resolución de incidencias;
- parches de compatibilidad;
- actualización de la matriz de dispositivos probados.

### Dictamen operativo

**Factible para un MVP académico y una distribución inicial controlada**, siempre que se cierre el hardware de referencia, se respete el alcance y se documenten los requisitos. Una operación comercial amplia exigiría después capacidad sostenida de soporte, QA de dispositivos y gestión de comunidad.

## 44.14 Factibilidad financiera: supuestos

### Supuestos académicos base

| Variable | Supuesto |
|---|---:|
| Moneda | MXN de 2026 |
| Duración del desarrollo MVP | 7 semanas |
| Integrantes | 6 |
| Dedicación promedio estimada | 15 h/semana por integrante |
| Horas totales estimadas | 630 h |
| Valor académico de hora de trabajo | $120 |
| Precio público sugerido del juego base | $149 IVA incluido |
| IVA usado para modelar | 16% |
| Comisión conservadora de plataforma | 30% sobre precio sin IVA |
| Ingreso neto estimado por copia | $90 |
| Costo variable de soporte/reserva por copia | $5 |
| Margen de contribución por copia | $85 |
| Operación fija post-lanzamiento, primer año | $12,000 |

El precio y la comisión son supuestos de modelado. Deben verificarse con la tienda, territorio, tipo de cambio y régimen fiscal elegidos. El “valor de hora” representa costo de oportunidad académico; no afirma que el equipo pagará o cobrará esa tarifa.

### Derivación del ingreso unitario

```text
Precio público con IVA                         $149.00
Precio estimado sin IVA = 149 / 1.16          $128.45
Menos comisión de plataforma (30%)             $38.54
Ingreso neto estimado por copia                 $89.91 ≈ $90
Menos reserva variable de soporte                $5.00
Margen de contribución por copia                $84.91 ≈ $85
```

## 44.15 Recursos existentes, recursos por adquirir e inversión inicial estimada

La separación evita registrar como inversión en efectivo todo lo que el equipo ya utiliza.

### Recursos que se asumen disponibles para el plan académico base

| Recurso | Uso | Tratamiento financiero |
|---|---|---|
| Seis computadoras personales | desarrollo por rol | sin desembolso de compra; se reconoce uso económico |
| Teléfonos Android/iPhone del equipo | pruebas iniciales | sin desembolso de compra |
| PC de desarrollo capaz de ejecutar Unity | integración inicial | sin desembolso, sujeto a validar como PC oficial |
| Conexión a Internet y red doméstica | repositorio, dependencias y POC | sólo costo incremental |
| GitHub y herramientas con plan gratuito/educativo | repositorio, CI y diseño | $0 en plan base |
| Unity, Blender, MediaPipe, OpenCV, Java 21 + Spring Boot | producción | $0 en licencias bajo supuestos y términos aplicables |
| Conocimiento y tiempo del equipo | diseño, desarrollo y pruebas | costo de oportunidad, no desembolso inmediato |

Esta disponibilidad es un supuesto académico, no un inventario comprobado. Debe confirmarse en el checklist. Si la PC, teléfonos o red no satisfacen el hardware de referencia, dejan de considerarse disponibles y deben cotizarse antes de continuar.

### Recursos que realmente sería necesario adquirir o reservar

| Concepto | Estimación académica | Motivo |
|---|---:|---|
| Webcam 1080p/30 FPS de referencia | $1,500 | tracking reproducible si no existe una compatible |
| Router local de referencia | $1,200 | estabilidad de sesión y presentación |
| Correas, soportes, cables y señalización de seguridad | $1,400 | pruebas y uso físico seguro |
| Assets/licencias puntuales de audio, tipografía o arte | $1,500 | completar contenido con licencia compatible |
| Cuota/reserva de publicación digital | $2,000 | alta y variación cambiaria; supuesto académico |
| Incentivos y logística de QA externo | $2,500 | pruebas con personas fuera del equipo |
| Respaldo/almacenamiento y consumibles | $600 | builds y evidencia |
| Contingencia de adquisición | $1,700 | variaciones y reemplazos menores |
| **Subtotal de adquisición/publicación** | **$12,400** | |
| Electricidad, Internet y operación incremental durante desarrollo | $3,000 | siete semanas |
| **Desembolso inicial estimado** | **$15,400** | |

No se incluye la compra de seis computadoras, cuatro teléfonos ni una cámara de profundidad. Tampoco se presupuesta infraestructura cloud para gameplay porque contradice la arquitectura local del MVP.

## 44.16 Costos de desarrollo y operación

### Costo de desarrollo

| Categoría | Cálculo | Costo |
|---|---:|---:|
| Trabajo del equipo | 630 h × $120 | $75,600 |
| Uso/depreciación académica de equipo existente | estimación conjunta | $3,600 |
| Adquisición, publicación y QA | tabla anterior | $12,400 |
| Operación incremental de desarrollo | estimación | $3,000 |
| **Costo económico total del MVP** | | **$94,600** |

De los $94,600:

- **$15,400** representan desembolso inicial estimado;
- **$79,200** representan tiempo y uso económico de recursos existentes, sin salida de efectivo equivalente en el plan base.

### Operación post-lanzamiento del primer año

| Concepto | Estimación anual |
|---|---:|
| Parches y soporte programado | $6,000 |
| QA de compatibilidad y dispositivos | $3,000 |
| Comunicación, comunidad y materiales | $2,000 |
| Respaldo, sitio o servicios mínimos | $1,000 |
| **Costo fijo anual estimado** | **$12,000** |

Adicionalmente se reserva **$5 por copia** para soporte variable, promociones, devoluciones u otras desviaciones no incluidas en la liquidación unitaria simplificada.

## 44.17 Estrategia de precio

Se propone un precio público inicial de **$149 MXN IVA incluido** para el juego base, sujeto a investigación de disposición de pago antes del lanzamiento.

Justificación:

- precio de entrada compatible con una compra social de bajo riesgo;
- producto de cinco minijuegos y alcance independiente;
- el grupo entero puede jugar con una sola licencia de PC;
- permite descuentos futuros sin posicionar el producto como gratuito;
- evita depender de monetización agresiva.

Reglas:

- lanzar el MVP como compra única;
- no vender ventajas estadísticas;
- no usar loot boxes necesarias para progresar;
- probar precios de $119, $149 y $179 con encuestas y páginas de intención, sin prometer contenido no construido;
- usar descuentos sólo cuando no destruyan la percepción de valor ni el margen;
- definir precios regionales conforme a reglas de la plataforma.

## 44.18 Fuentes de ingresos

### Fuente incluida en el modelo inicial

- venta digital del juego base para PC.

### Fuentes futuras condicionadas a validación

```text
Juego base
+
Expansiones
+
Packs de minijuegos
+
Cosméticos sin ventajas
+
Licencias para eventos o espacios
```

Estas fuentes corresponden a P1/P2 o versiones posteriores según su naturaleza. No forman parte del cálculo de recuperación del MVP y no deben desarrollarse antes de estabilizar P0.

No se contempla:

- pay-to-win;
- loot boxes necesarias para progresar;
- ventajas estadísticas comprables;
- venta de datos personales o video;
- suscripción obligatoria para jugar el contenido base.

## 44.19 Escenarios de ventas

Los escenarios siguientes son ejercicios académicos, **no pronósticos**. Usan $149 de precio público, $90 de ingreso neto por copia, $5 de costo variable por copia y $12,000 de costo fijo operativo del primer año.

| Escenario | Copias/año | Venta bruta al público | Ingreso neto estimado | Costo variable | Contribución | Menos operación anual | Flujo antes de inversión inicial |
|---|---:|---:|---:|---:|---:|---:|---:|
| Conservador | 500 | $74,500 | $45,000 | $2,500 | $42,500 | $12,000 | $30,500 |
| Medio | 2,000 | $298,000 | $180,000 | $10,000 | $170,000 | $12,000 | $158,000 |
| Favorable | 5,000 | $745,000 | $450,000 | $25,000 | $425,000 | $12,000 | $413,000 |

### Resultado después de inversión

| Escenario | Flujo antes de inversión | Resultado contra desembolso inicial de $15,400 | Resultado contra costo económico total de $94,600 |
|---|---:|---:|---:|
| Conservador | $30,500 | $15,100 | -$64,100 |
| Medio | $158,000 | $142,600 | $63,400 |
| Favorable | $413,000 | $397,600 | $318,400 |

Lectura:

- el escenario conservador recupera el efectivo incremental, pero no remunera por completo el tiempo y uso de recursos;
- el escenario medio supera ambos criterios bajo los supuestos;
- el escenario favorable ofrece margen para reinversión, pero requeriría demanda, calidad y visibilidad que aún no están demostradas.

## 44.20 Punto de equilibrio

Fórmula:

```text
Punto de equilibrio en copias =
Costos fijos a recuperar
/
Margen de contribución por copia
```

### Punto de equilibrio de desembolso inicial

```text
$15,400 / $85 = 181.18
Redondeo hacia arriba: 182 copias
```

### Punto de equilibrio económico del desarrollo

```text
$94,600 / $85 = 1,112.94
Redondeo hacia arriba: 1,113 copias
```

### Punto de equilibrio incluyendo operación del primer año

| Enfoque | Costos a recuperar | Copias requeridas |
|---|---:|---:|
| Caja: desembolso inicial + operación anual | $27,400 | 323 |
| Económico: MVP + operación anual | $106,600 | 1,255 |

Los dos puntos de equilibrio deben comunicarse juntos. Reportar sólo 182 copias ocultaría el costo del tiempo del equipo; reportar sólo 1,113 ocultaría que gran parte del costo no exige efectivo inmediato.

## 44.21 Viabilidad económica

El proyecto es **económicamente viable de forma condicionada**:

- el desembolso inicial estimado es relativamente bajo porque reutiliza hardware y software disponibles;
- 323 copias cubrirían desembolso y operación del primer año en el modelo de caja;
- 1,255 copias cubrirían el costo económico completo y la operación anual;
- el escenario medio de 2,000 copias supera ambos umbrales;
- el escenario conservador no remunera por completo el trabajo valorado del equipo.

La decisión de publicar debe tomarse después de:

1. validar el Vertical Slice;
2. estimar compatibilidad real;
3. probar disposición de pago;
4. obtener cotizaciones;
5. revisar comisión, IVA, devoluciones y obligaciones fiscales;
6. estimar costo real de soporte.

No se debe considerar rentable por el solo hecho de que el desembolso de efectivo sea pequeño.

## 44.22 Viabilidad dentro del segmento de videojuegos

La propuesta tiene encaje potencial en el subsegmento de party games locales y juegos de movimiento porque ofrece:

- una premisa observable en video y demostraciones;
- incorporación grupal con teléfonos;
- sesiones competitivas de lectura rápida;
- diferenciación frente al control tradicional;
- potencial de contenido adicional sin rehacer el núcleo.

Sus límites de mercado son igualmente claros:

- requiere PC, webcam, teléfonos compatibles, red local y espacio físico;
- el multijugador local es más acotado que un producto online;
- la fricción técnica puede afectar reseñas y devoluciones;
- cinco minijuegos limitan profundidad y exigen alto pulido;
- la visibilidad comercial de un videojuego independiente no está garantizada.

Por lo tanto, la viabilidad de segmento es **plausible, no demostrada**. Debe validarse con pruebas de concepto públicas, sesiones observadas, lista de deseos o métricas equivalentes de intención y una matriz de compatibilidad antes de escalar inversión.

## 44.23 Riesgos técnicos, operativos, financieros y comerciales y estrategias de mitigación

| Tipo | Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|---|
| Técnico | sensores no disponibles o bloqueados en navegador | Media | Alta | Spike 0, contexto seguro, navegadores objetivo y app nativa mínima sólo como Plan B |
| Técnico | latencia o pérdida de paquetes | Media | Alta | router de referencia, frecuencia adaptable, sequence, heartbeat y presupuesto P95 |
| Técnico | tracking inestable por iluminación/espacio | Alta | Alta | ROI, Player Lock, guía física, calibración, menor resolución y tracking reducido |
| Técnico | Input Fusion produce falsos positivos | Media | Alta | ventanas temporales, confidence, cooldown, pruebas cuantitativas y tuning central |
| Operativo | integración tardía entre módulos | Media | Alta | Vertical Slice temprano e integración compartida Hiram–Austin–Arturo |
| Operativo | cinco juegos exceden siete semanas | Alta | Alta | reutilizar sistemas, recortar P1/P2 y profundidad secundaria según Plan B, no el loop P0 |
| Operativo | hardware de presentación difiere del probado | Media | Alta | fijar hardware oficial en Sprint 1 y exigirlo en Definition of Done |
| Operativo | dependencia de una persona por módulo | Media | Media | contratos documentados, PR, revisión cruzada y handoff antes de freeze |
| Financiero | costos reales superan estimaciones | Media | Media | cotizaciones, contingencia y aprobación del Product Owner |
| Financiero | ventas inferiores a 323 copias | Media/Alta | Alta | mantener bajo desembolso, validar intención, lanzamiento controlado y no comprometer gasto P2 |
| Financiero | precio neto menor por descuentos/comisiones | Media | Media | análisis de sensibilidad y margen mínimo por campaña |
| Comercial | baja visibilidad en tienda | Alta | Alta | demo, clips de movimiento, página temprana, eventos y comunidad |
| Comercial | usuarios perciben instalación compleja | Media | Alta | onboarding QR, diagnóstico comprensible, tutorial y documentación de red |
| Comercial | comparación desfavorable con productos de mayor presupuesto | Alta | Media | posicionar la fusión multimodal y pulir el núcleo, no competir por volumen de contenido |
| Seguridad | movimiento causa golpe o caída del teléfono | Media | Alta | no soltar teléfono, correas, espacio libre, saturación de potencia y advertencias |
| Privacidad | temor por uso de cámara | Media | Alta | procesamiento local, no grabar, no reconocimiento facial y mensaje explícito |
| Legal | licencias de assets o marcas inadecuadas | Baja/Media | Alta | registro de atribución, revisión de licencias y contenido original |

### Regla de mitigación

Cada riesgo P0 debe tener responsable, señal de alerta y criterio de escalamiento en el tablero. Si una mitigación requiere una función fuera del alcance, se prefiere el Plan B más simple que conserve la experiencia.

## 44.24 Escalabilidad del modelo de negocio

### Escalabilidad técnica

El núcleo puede escalar por reutilización:

```text
Session + Players + PWA + Protocol + Calibration
                        +
Motion Analyzer + Input Fusion + Scoring + Feedback
                        ↓
                 Nuevos minijuegos
```

La escalabilidad no implica microservicios en el MVP. El monolito modular seguirá siendo válido mientras el producto local no requiera servicios online.

### Escalabilidad comercial

1. más ventas del juego base con costo marginal digital bajo;
2. packs de minijuegos que reutilicen infraestructura;
3. cosméticos sin ventajas;
4. licencias para eventos con soporte y precio diferenciados;
5. versiones de plataforma sólo después de comprobar demanda y costo de porting.

### Límites de escalabilidad

- QA crece con la diversidad de teléfonos, cámaras, navegadores y PCs;
- soporte de red local puede crecer más rápido que las ventas;
- contenido nuevo exige arte, diseño, balance y pruebas;
- online, cuentas, nube y matchmaking cambian materialmente costos y arquitectura;
- eventos requieren operación, contratos y seguridad adicionales.

La escalabilidad debe financiarse por hitos. No se inicia un frente futuro únicamente porque aparezca en el roadmap.

## 44.25 Canales, relación con clientes y adquisición

### Canales iniciales

- distribución digital para PC;
- página oficial o landing page;
- redes sociales y clips demostrativos;
- demostraciones académicas;
- eventos locales controlados;
- comunidades de videojuegos independientes y party games.

### Relación

- autoservicio guiado;
- onboarding mediante QR;
- tutorial y calibración;
- documentación de compatibilidad;
- actualizaciones;
- soporte básico;
- comunidad, si existe capacidad para moderarla.

### Mensaje comercial

La comunicación debe mostrar en pocos segundos:

1. el teléfono se conecta;
2. la persona realiza un movimiento;
3. el gorila responde;
4. el resultado cambia por técnica;
5. el grupo compite.

No se debe anunciar online, cuentas, DLC, eventos u otras funciones futuras como si fueran parte del MVP.

## 44.26 Roadmap comercial

El roadmap comercial se acopla al roadmap de producto de la sección 46.

| Etapa | Producto | Objetivo comercial | Evidencia para avanzar |
|---|---|---|---|
| Descubrimiento y Spike 0 | POC técnica | comprobar promesa central | sensores, cámara y latencia funcionando |
| Vertical Slice | Gorilla Smash end-to-end | probar comprensión y atractivo | usuarios externos conectan, juegan e interpretan score |
| Producción MVP | cinco minijuegos P0 | preparar producto demostrable | DoD, QA cuantitativo y sesiones completas |
| Feature Freeze | sólo corrección/polish | reducir riesgo de presentación | cero crashes en 10 sesiones y build candidata |
| Validación precomercial | mismo MVP | medir interés y precio | feedback, intención, compatibilidad y costos verificados |
| Lanzamiento controlado v1.0 | juego base | validar ventas y soporte | conversión, devoluciones, errores y reseñas |
| v1.x | polish y accesibilidad priorizada | mejorar retención y reputación | métricas del lanzamiento |
| v2.0+ | Modo Fiesta, packs u otros | ampliar catálogo e ingreso | núcleo rentable o evidencia clara de demanda |
| Fases posteriores | eventos, cooperativo u online experimental | diversificar segmentos | caso financiero y técnico independiente |

### Puertas comerciales

- no invertir en marketing de lanzamiento antes de validar el Vertical Slice;
- no comprometer publicación antes de confirmar hardware y compatibilidad;
- no producir DLC antes de verificar uso y satisfacción del juego base;
- no construir backend online con ingresos hipotéticos del MVP local.

## 44.27 Indicadores de negocio a validar

Además de las métricas técnicas de la sección 47:

| Indicador | Propósito |
|---|---|
| intención de compra por rango de precio | validar $149 y sensibilidad |
| porcentaje que conecta sin asistencia | estimar fricción y soporte |
| finalización de los cinco minijuegos | evaluar valor del contenido |
| deseo de repetir o invitar a otra persona | estimar potencial social |
| tasa de devolución | detectar incompatibilidad o expectativa incorrecta |
| incidencias por 100 sesiones | estimar costo operativo |
| conversión de demo/lista de deseos a compra | orientar adquisición |
| margen de contribución real por copia | recalcular punto de equilibrio |

No se fijan metas comerciales rígidas antes de obtener una primera línea base. Los objetivos técnicos existentes sí se mantienen.

## 44.28 Conclusión formal de factibilidad y viabilidad

**Gorilla Escape: Gorilimpiadas es una idea de negocio académicamente factible y potencialmente viable, condicionada a validación técnica y comercial.**

La factibilidad técnica se sustenta en un stack disponible, una autoridad local simple, procesamiento en PC, arquitectura monolítica modular y un Vertical Slice que concentra el mayor riesgo. La factibilidad operativa se apoya en seis roles definidos, integración temprana, QA cuantitativo, hardware de referencia, Feature Freeze y planes de contingencia. La factibilidad financiera se favorece por el uso de recursos existentes: el desembolso inicial se estima en $15,400, mientras que el costo económico total, incluyendo tiempo y uso de equipo, se estima en $94,600.

Con un margen académico de $85 por copia, el punto de equilibrio es de 182 copias para recuperar sólo el desembolso, 323 incluyendo la operación del primer año, 1,113 para recuperar el costo económico del MVP y 1,255 incluyendo además un año de operación. El escenario medio de 2,000 copias sería económicamente positivo bajo estos supuestos; el conservador de 500 copias no remuneraría completamente el trabajo del equipo.

Dentro del segmento de videojuegos, su oportunidad reside en unir la accesibilidad del teléfono, la lectura corporal de la webcam y la competencia de un party game sin periférico propietario ni Internet durante la partida. Sus riesgos principales son compatibilidad, tracking, integración, alcance, descubrimiento comercial y soporte.

El dictamen final es:

- **factibilidad técnica:** viable si Spike 0 y Vertical Slice cumplen los criterios;
- **factibilidad operativa:** viable para MVP y lanzamiento controlado con el equipo actual;
- **factibilidad financiera:** viable bajo disciplina de gasto y ventas por encima de los umbrales calculados;
- **viabilidad comercial:** plausible, pero debe demostrarse mediante intención de compra, sesiones externas y datos de lanzamiento;
- **decisión recomendada:** continuar por hitos, preservar el Feature Freeze y no ampliar el MVP para embellecer el caso de negocio.

---

# 45. Arquitectura de producto futura

```text
GORILLA ESCAPE
│
├── GORILIMPIADAS
│   └── deportes y movimiento
│
├── PARTY
│   └── minijuegos sociales
│
├── ADVENTURE
│   └── experiencias cooperativas
│
└── PACKS
    └── contenido temático
```

**Gorilla Escape** funciona como marca/plataforma.

**Gorilimpiadas** es la primera experiencia principal.

Esto es dirección de producto, no alcance del MVP.

---

# 46. Roadmap propuesto

## v1.0 — MVP

- cinco minijuegos;
- 1–4 jugadores;
- PWA;
- cámara;
- sensores;
- torneo;
- resultados.

## v1.x

- polish;
- récords;
- más feedback;
- mejoras de calibración;
- accesibilidad;
- optimización.

## v2.0

- Modo Fiesta;
- nuevos minijuegos;
- selección libre.

## v2.x

- personalización;
- nuevos gorilas;
- estadísticas;
- packs.

## v3.0

- eventos;
- torneos locales;
- modos cooperativos.

## Futuro experimental

- online;
- plataformas adicionales;
- app móvil dedicada;
- nuevas tecnologías de tracking.

---

# 47. Métricas de producto

Además de métricas técnicas, medir en pruebas:

```text
Tiempo hasta conectar teléfono
Tiempo de calibración
Tiempo hasta entender el primer juego
Número de errores de conexión
Número de recalibraciones
Falsos positivos
Acciones no detectadas
Duración total de sesión
Porcentaje de jugadores que entienden su resultado
```

## Objetivos iniciales

```text
Conectar teléfono        < 60 s para usuario nuevo
Calibración              < 30 s
Entender instrucción     < 20 s
Crash                    0
```

---

# 48. Definition of Ready

Una tarea entra a Sprint cuando:

- objetivo claro;
- responsable;
- prioridad;
- dependencias identificadas;
- criterio de aceptación;
- diseño/contrato disponible cuando aplique;
- suficientemente pequeña para ejecutarse.

No comenzar historias ambiguas grandes.

---

# 49. Definition of Done v4

Una funcionalidad P0 está Done cuando:

- implementada;
- compila;
- cumple aceptación;
- probada;
- integrada;
- PR revisado;
- no rompe flujo existente;
- logs razonables;
- errores recuperables tratados;
- funciona en hardware relevante;
- configuración no hardcodeada innecesariamente;
- documentación mínima actualizada.

Un minijuego está Done cuando:

```text
Tutorial
+
Input real
+
Gameplay completo
+
Scoring
+
Feedback
+
Resultado
+
Reinicio
+
Multijugador/turnos
+
Sin crash
```

---

# 50. Orden de implementación recomendado

La secuencia histórica se conserva; cobertura vigente del Spike y siguiente fase en [auditoría final](PHASE0_FINAL_AUDIT.md). Fase 1 no se inicia automáticamente.

## Fase 0

```text
Repo
Unity
Servidor
PWA
WebSocket
Cámara
```

## Fase 1 — Vertical Slice

```text
Gorilla Smash end-to-end
```

## Fase 2 — Sistemas reutilizables

```text
Session
Players
Calibration
Motion
Input Fusion
Scoring
GP
Results
```

## Fase 3 — Minijuegos

```text
Coconut Throw
Jungle Archery
Coconut Bowling
Jungle Slice
```

## Fase 4 — Integración

```text
Secuencia del torneo
Transiciones
Final
Party Coming Soon
```

## Fase 5 — Polish

```text
UX
Audio
VFX
Balance
Latency
Bugs
```

## Fase 6 — Freeze

```text
No features
Only bugs
Testing
Build
Evidence
Presentation
```

---

# 51. Responsabilidades técnicas afinadas

## Josue — Product Owner

- prioridades;
- aceptación;
- decisiones de alcance;
- balance aprobado;
- validación de experiencia;
- mantener Documento Maestro.

## Erik — Scrum Master

- tablero;
- seguimiento;
- impedimentos;
- Sprint ceremonies;
- cumplimiento de Definition of Ready/Done.

## Saul — Art / UI

- Art Bible;
- UI PC;
- UI móvil;
- personajes;
- arenas;
- VFX;
- integración visual.

## Arturo — Unity / Gameplay

- Bootstrap;
- GameManager;
- Session integration;
- minijuegos;
- física;
- scoring;
- torneo;
- resultados.

## Hiram — Mobile / Network

- Java 21 + Spring Boot;
- PWA;
- WebSocket;
- protocolo;
- sensores;
- reconexión;
- Motion Analyzer inicial.

## Austin — Camera

- MediaPipe;
- OpenCV;
- ROI;
- Player Lock;
- tracking;
- gestos;
- CameraInput.

## Integración compartida

```text
Hiram + Austin + Arturo
↓
Input Fusion
↓
Vertical Slice
```

Debe trabajarse conjuntamente; no dejarlo para el final.

---

# 52. Decisiones congeladas para evitar retrabajo

A menos que una prueba técnica demuestre que son inviables:

1. Unity + C#.
2. PC autoritativa.
3. PWA primero.
4. WebSocket.
5. JSON en MVP.
6. Java 21 + Spring Boot local.
7. MediaPipe + OpenCV.
8. Modular Monolith.
9. 1–4 jugadores.
10. cámara principalmente por turnos.
11. cinco minijuegos.
12. Gorilla Points.
13. cuatro avatares sin estadísticas.
14. calibración adaptativa.
15. ScriptableObjects para tuning.
16. JSON local para persistencia.
17. telemetría local.
18. sin Internet requerido durante gameplay.
19. sin reconocimiento facial.
20. sin soltar el teléfono.

---

# 53. Preguntas que sólo deben resolverse con pruebas reales

No conviene inventar estas respuestas antes de medir:

- DeadZone exacta por dispositivo.
- MaximumAcceleration.
- filtro óptimo.
- sensibilidad de orientación.
- SynchronizationWindow definitiva.
- MinimumCameraConfidence definitiva.
- intensidad de spin.
- fricción exacta de Bowling.
- curva final de Coconut Throw.
- hitboxes finales.
- frecuencia óptima entre 30/50/60 Hz.
- resolución óptima de webcam.
- número final de polígonos por asset.
- duración final de feedback.
- tolerancia final de asistencia.

Todos estos valores deben vivir en configuración para permitir tuning rápido.

---

# 54. Criterio de éxito del proyecto

El MVP se considera exitoso cuando una persona externa puede:

```text
Abrir Gorilla Escape
↓
Crear Gorilimpiada
↓
Conectar su teléfono por QR
↓
Elegir jugador
↓
Calibrar
↓
Entender qué debe hacer
↓
Completar los 5 minijuegos
↓
Obtener resultados coherentes con su técnica
↓
Ver tabla final
↓
Ver campeón
↓
Ver Modo Fiesta — Próximamente
↓
Volver al menú
```

sin:

- configuración técnica manual compleja;
- crash;
- cambio accidental de jugador;
- explicación constante de un desarrollador.

---

# 55. Criterio de calidad de la experiencia

El sistema no debe sentirse como:

> una demo de sensores.

Debe sentirse como:

> un videojuego completo que casualmente utiliza cámara y teléfono como una nueva forma de control.

Por ello, cada sistema técnico debe terminar en una experiencia visible:

```text
SENSOR
↓
INTERPRETACIÓN
↓
ACCIÓN
↓
FÍSICA
↓
FEEDBACK
↓
RESULTADO
↓
COMPETENCIA
```

---

# 56. Regla final del MVP

Cuando exista una decisión entre:

```text
más funciones
```

y:

```text
mejor funcionamiento de las funciones existentes
```

se elegirá:

**mejor funcionamiento.**

El diferenciador de Gorilla Escape: Gorilimpiadas no será la cantidad de características.

Será que:

**conectar el teléfono sea sencillo, el movimiento responda bien, la técnica importe y jugar con otras personas sea divertido.**

---

# 57. Trazabilidad y control de cambios

## Matriz mínima de aceptación

Esta matriz es de aceptación del producto físico/MVP, no los gates de cierre Software/Lab. La [Definition of Done del audit](PHASE0_FINAL_AUDIT.md#definition-of-done-softwarelab) vincula cada gate técnico a su evidencia; las deudas físicas siguen obligatorias.

| Objetivo | Requisito relacionado | Evidencia de aceptación | Responsable principal |
|---|---|---|---|
| Incorporación sencilla | QR, PWA, sesión y permisos | una persona nueva conecta en menos de 60 s en hardware objetivo | Hiram |
| Lectura física confiable | sensores, cámara, Player Lock y calibración | pruebas de secciones 37 y 42, con logs reproducibles | Austin + Hiram |
| Respuesta jugable | Motion Analyzer, Input Fusion y Unity | latencia P95 dentro del objetivo y acción visible correcta | Arturo + Hiram + Austin |
| Técnica comprensible | scoring y feedback | la persona identifica por qué obtuvo su resultado | Arturo + Josue |
| Torneo completo | cinco pruebas, GP, desempate y campeón | sesión completa sin crash ni bloqueo | Arturo |
| Presentación coherente | narrativa funcional, UI, arte, audio y transiciones | apertura, progreso y cierre del torneo se entienden sin explicación externa | Saul + Josue |
| Uso seguro | advertencias, límites, espacio y no soltar el teléfono | checklist de seguridad ejecutado antes de cada sesión de prueba | Josue + Erik |
| Entrega reproducible | build, CI, configuración y documentación | build candidata instalada y ejecutada en la PC oficial | Erik + Arturo |

## Registro de decisiones

Toda modificación de una decisión congelada debe registrar:

```text
Fecha
Decisión anterior
Decisión nueva
Evidencia o problema que motivó el cambio
Impacto en alcance, calendario y pruebas
Responsable
Aprobación del Product Owner
```

Los acuerdos verbales no sustituyen la actualización del documento. Una decisión que cambie P0, seguridad, privacidad, arquitectura o fecha de entrega debe reflejarse también en el tablero y en los criterios de prueba afectados.

## Pendientes que requieren cierre explícito

| Pendiente | Evidencia necesaria | Fecha límite recomendada |
|---|---|---|
| versión exacta de Unity y paquetes | proyecto creado y build mínima | antes del inicio de desarrollo |
| PC, webcam, teléfonos y red oficiales | inventario y prueba de referencia | antes de terminar Spike 0 |
| navegadores móviles compatibles | matriz de permisos y sensores | durante Spike 0 |
| orden final de las cinco pruebas | playtest de ritmo y dificultad | antes de producir transiciones finales |
| uso definitivo de la marca “Gorilla Escape” | prueba de comprensión del nombre y decisión de naming | antes de publicar materiales externos |
| identidad del torneo y tono de la apertura | guion funcional de una página | antes del storyboard |
| necesidad de un presentador personificado | prueba de la UI guía sin personaje adicional | antes de modelar o grabar voz |
| nombres y diseño final de los cuatro avatares | Art Bible aprobada | antes de animación de producción |
| valores finales de tuning | sesiones instrumentadas | antes del Feature Freeze |
| precio y canal comercial | validación y condiciones vigentes | después del MVP académico |

---

# Anexo A — Checklist antes del 28 de septiembre

- [ ] Repositorio creado.
- [ ] Versión de Unity fijada.
- [ ] PC oficial definida.
- [ ] Webcam de referencia.
- [ ] Android de referencia.
- [ ] iPhone de referencia.
- [ ] Red local de referencia.
- [ ] Java 21 + Spring Boot elegido.
- [ ] Puerto/configuración definidos.
- [ ] PWA mínima.
- [ ] QR POC.
- [ ] WebSocket POC.
- [ ] DeviceMotion POC.
- [ ] DeviceOrientation POC.
- [ ] MediaPipe POC.
- [ ] Contratos compartidos.
- [ ] MotionSettings creado.
- [ ] GameBalanceSettings creado.
- [ ] Tablero Scrum.
- [ ] GitHub Actions base.
- [ ] Art Bible inicial.
- [ ] Vertical Slice Gorilla Smash planificado.

---

# Anexo B — Checklist Vertical Slice

- [ ] Crear sesión.
- [ ] Mostrar QR.
- [ ] Conectar teléfono.
- [ ] Asignar PlayerId.
- [ ] Leer sensores.
- [ ] Cámara detecta jugador.
- [ ] Player Lock.
- [ ] Calibración.
- [ ] Armar acción.
- [ ] Detectar golpe.
- [ ] Fusionar teléfono + cámara.
- [ ] Validar.
- [ ] Ejecutar golpe en Unity.
- [ ] Calcular score.
- [ ] Mostrar feedback.
- [ ] Mostrar resultado.
- [ ] Reiniciar intento.
- [ ] Reconectar teléfono.
- [ ] Tracking lost/recovery.
- [ ] Log de telemetría.

---

# Anexo C — Checklist de Feature Freeze

A partir del 6 de noviembre:

## Permitido

- [ ] corregir bugs;
- [ ] optimizar;
- [ ] ajustar balance;
- [ ] corregir UI;
- [ ] corregir audio;
- [ ] mejorar estabilidad;
- [ ] completar documentación;
- [ ] generar evidencias;
- [ ] preparar presentación.

## No permitido

- [ ] nuevos minijuegos;
- [ ] nueva arquitectura;
- [ ] nuevas tecnologías;
- [ ] nuevo modo grande;
- [ ] multijugador online;
- [ ] personalización avanzada;
- [ ] rediseño total;
- [ ] funciones experimentales.

---

# Anexo D — Resumen ejecutivo técnico

```text
PC autoritativa
+
Unity
+
Java 21 + Spring Boot local
+
PWA
+
WebSocket
+
Sensores del teléfono
+
MediaPipe/OpenCV
+
Calibración
+
Motion Analyzer
+
Input Fusion
+
5 minijuegos
+
Gorilla Points
+
Feedback
=
GORILLA ESCAPE: GORILIMPIADAS MVP
```

**Objetivo:** construir primero una cadena end-to-end estable y reutilizable, después multiplicarla hacia los cinco minijuegos.

**Prioridad máxima:** que el jugador conecte, se mueva y vea una respuesta correcta, rápida, comprensible y divertida.

---

# Anexo E — Checklist de auditoría narrativa

Este checklist debe completarse antes del storyboard y repetirse después de cualquier cambio que afecte el flujo del torneo.

## Dirección y conflicto

- [ ] La premisa identifica participantes, situación, objetivo y resistencia.
- [ ] Se entiende qué quiere conseguir el jugador.
- [ ] Se entiende por qué no puede conseguirlo inmediatamente.
- [ ] Se entiende qué está en juego y qué ocurre al fallar.
- [ ] El conflicto puede explicarse sin inventar un villano o una subtrama innecesaria.

## Causalidad y progresión

- [ ] Cada beat importante tiene una causa comprensible.
- [ ] Cada beat cambia información, objetivo, clasificación, posibilidad o dirección.
- [ ] Cada transición provoca o habilita la siguiente etapa.
- [ ] El orden de las pruebas tiene una razón jugable comprobable.
- [ ] Ninguna escena existe sólo porque “después toca” otra escena.
- [ ] Los huecos causales están marcados como pendientes en lugar de ocultarse.

## Información y reglas

- [ ] El jugador conoce el objetivo antes de actuar.
- [ ] Las variables relevantes de puntuación se presentan antes del resultado.
- [ ] La tabla y los Gorilla Points se explican antes de que determinen al campeón.
- [ ] El desempate se comunica antes de aplicarse.
- [ ] Se distingue entre resultado bajo, intento inválido y fallo técnico.
- [ ] Ninguna regla cambia únicamente para producir un desenlace conveniente.
- [ ] No existen “revelaciones” de reglas sin preparación previa.

## Personajes y función

- [ ] Cada avatar o personaje comunica identidad, información, feedback o progresión.
- [ ] Las reacciones coinciden con información que el jugador ya conoce.
- [ ] Un personaje guía no duplica funciones que la UI resuelve mejor.
- [ ] Las motivaciones necesarias están escritas y son comprensibles.
- [ ] Eliminar un personaje secundario no deja tareas narrativas sin responsable.

## Alcance y eliminación

- [ ] Cada escena cambia algo relevante.
- [ ] Cada escenario, objeto y animación importante tiene una función identificada.
- [ ] Se intentó eliminar o combinar cada elemento narrativo no esencial.
- [ ] La solución a un hueco no agrega contenido innecesario.
- [ ] La versión P0 puede producirse dentro del calendario.
- [ ] Los elementos P1 y P2 no se presentan como parte terminada del MVP.

## Prueba final de documentación

- [ ] Una persona externa puede explicar el flujo del torneo leyendo únicamente este documento.
- [ ] El equipo no necesita añadir verbalmente información ausente para justificar el orden o las reglas.
- [ ] Los problemas abiertos están registrados con responsable, evidencia requerida y fecha límite.
- [ ] Premisa, sinopsis, cadena causal, beat sheet, matriz de información y auditoría coinciden entre sí.


# Enmienda de producto — dos clientes móviles oficiales (DEC-006, 2026-10-06)

Por instrucción explícita del Product Owner, el producto admite PWA/web como fallback universal sin instalación obligatoria y Flutter Android como cliente nativo opcional, preferido cuando instalado. Instalar Flutter nunca es requisito para participar. Esta enmienda prevalece sobre «app nativa sólo Plan B»/«no construir app nativa por adelantado» en cuanto al roadmap aprobado: Flutter se diseña para4C, no se implementa ahora. No amplía a app iOS.

Ambos comparten sesión, IDs, contratos/Gorilla Protocol móvil, semántica/reglas de conexión y backend Java; preferencia WSS/WebSocket para ambos, sin UDP nativo propio. Java administra transporte/sesión técnica; Unity conserva autoridad exclusiva de gameplay/resultados. No duplicar scoring/torneo ni lógica de negocio Dart/JS.

Un enlace HTTPS y un QR deben funcionar en navegador; App Links/deep linking Android es una mejora futura opcional, nunca reemplaza el fallback web. El jugador no instala CA/certificados/perfiles TLS, cambia DNS/configura navegador ni necesita instalar Flutter. Preparación de red/host corresponde al operador; no Internet requerido durante gameplay. La estrategia técnica HTTPS/DNS/certificado del [rediseño](PHASE0_PHONE_LAN_ONBOARDING_REDESIGN.md) queda Accepted para adaptación de software3A por aprobación posterior explícita del PO. No declarar offline/trust público validados.

Roadmap diseñado:3A LAN+HTTPS sin configuración TLS del teléfono;3B QR+sesión técnica;4A WS/protocolo común;4B sensores PWA reales;4C Flutter Android común; comparación/hardening posterior. No se autoriza implementación de estos bloques por esta enmienda; #94 permanece DRAFT hasta aprobar nueva estrategia. Fase1 NOT STARTED.

**Precisión de ejecución3A:** autorizada adaptación de #94 para FQDN/SAN DNS, cadena TLS, origen público443 y Java8443 sin privilegios. No autoriza compras, emisión pública ni modificaciones router/firewall. IPC/supervisor intactos; procedimientos preparados no equivalen a infraestructura aplicada. 3A IN PROGRESS hasta certificado público real, cero configuración del jugador y gate físico Android/iPhone. #94 DRAFT; 3B/4A/4B/4C y Fase1 NOT STARTED.


# Enmienda de producto — movimiento completo y webcam PC (DEC-007)

Por instrucción explícita del PO, PWA y Flutter Android opcional deben compartir acelerómetro, giroscopio, orientación, touch/gestos, calibración, personalización y feedback según capacidades declaradas. Touch no sustituye completamente movimiento requerido. No instalación obligatoria, dominio propio, router específico, certificados móviles ni DNS manual. Juego sin dependencia WAN durante partida.

Input Fusion combina **webcam integrada/externa PC + sensores móviles + touch**. Cámara del teléfono excluida de postura, vídeo y gameplay; QR del sistema o scanner Flutter futuro es sólo onboarding. Procesamiento visual local/offline; integración MediaPipe/OpenCV y modelos por evaluar, no implementados.

Unity única autoridad de gameplay/física/animaciones/scoring/resultados, sincronización temporal, calibración aplicada, fusión e interpretación de gestos. Java único backend/transporte/sesión técnica e hijo supervisado; IPC sólo 127.0.0.1. Perfiles separados por usuario, dispositivo, minijuego y límites competitivos Unity. Boliche distingue dirección/velocidad/spin/eje/hook; giro de muñeca no equivale automáticamente a curva proporcional.

Esta enmienda prevalece sobre requisitos anteriores de dominio/DNS y WebSocket obligatorio para transporte móvil. **No selecciona WT ni RTC:** [decisión Accepted sólo como base de Spike](PHASE0_SECURE_MOTION_TRANSPORT_DECISION.md). Primera preparación online, hosting y gate offline revisado pendientes del PO; ningún PASS físico nuevo. [Plan de Spike](PHASE0_SECURE_MOTION_SPIKE_PLAN.md) y [contrato conceptual](PHASE0_INPUT_FUSION_CONCEPT.md) son propuestas, no implementación autorizada. #94 DRAFT; 3A IN PROGRESS; posteriores y Fase 1 NOT STARTED.


# Enmienda de producto — calibración inmersiva y justicia competitiva (DEC-008)

Por instrucción explícita del PO, la calibración individual se integrará mediante desafíos jugables breves, animaciones/efectos y feedback inmediato, no tutorial técnico ni asistente largo. Rango cómodo individual, mano, orientación, estabilidad y capacidades sirven para normalizar intención; fuerza absoluta, talla o alcance no determinan automáticamente potencia virtual. No exigir esfuerzo máximo ni movimientos peligrosos; el teléfono permanece sujeto.

Unity conserva límites de potencia, física y reglas competitivas; precisión, timing, técnica y coordinación siguen distinguiendo habilidad. Oportunidades comparables de alcanzar techo virtual no significan resultados idénticos ni equilibrio demostrado. Personalización distingue usuario/dispositivo/minijuego/reglas competitivas; asistencias que afectan resultados son explícitas/versionadas. Parámetros competitivos congelados por ronda; desviaciones o variación aparente de rendimiento sólo motivan propuesta/recalibración controlada entre rondas, sin diagnóstico ni cambios silenciosos.

Input Fusion usa exclusivamente webcam PC, sensores móviles y touch según disponibilidad; degradación explícita, sin pose inventada ni cámara móvil. Sin vídeo ni biometría identificable por defecto. Pruebas físicas diversas con consentimiento/privacidad/seguridad, especialmente menores, necesarias para aceptar equidad; simulación sola no basta. [Contrato y criterios conceptuales](PHASE0_INPUT_FUSION_CONCEPT.md#8-calibración-inmersiva-y-justicia-competitiva--dec-008) y DEC-008 registran requisito Accepted con métodos/targets Proposed. Ejemplos boxeo/boliche/tenis/golf/penales no agregan minijuegos al alcance congelado.

Sólo documentación y dependencias futuras en Fase 0 actual; no reiniciar incrementos ni implementar desafíos, webcam, gameplay o calibración definitiva. #94 OPEN/DRAFT, 3A/Fase 0 IN PROGRESS; Fase 1 NOT STARTED.


# Enmienda de producto — intención y acciones únicas (DEC-009)

Unity reconocerá patrones temporales de preparación, inicio, dirección/trayectoria, aceleración/orientación/giro, ejecución, finalización y recuperación; no disparará acciones por un único umbral de aceleración. Modelo conceptual IDLE → PREPARING → ACTION_CANDIDATE → CONFIRMED → RECOVERY → IDLE, con cancelación/rechazo y suspensión por calidad/identidad. Una sola acción oficial por gesto; no ejecución durante preparación ni redisparo por muestra intensa, replay o dato tardío.

Intención e intensidad separadas: lanzamiento suave/lento coherente puede ser válido; movimiento accidental rápido no lo es por su pico. Umbrales personalizados a ruido/movimiento habitual/rango cómodo/mano y capacidades, limitados competitivamente por Unity y fijos por ronda según DEC-008. Sin fuerza máxima ni thresholds universales no medidos.

Fusión temporal/espacial con webcam exclusivamente PC cuando datos y asociación persona/control sean fiables. Si visión no es fiable, evaluar modalidad sensor-only con criterios específicos; si no hay evidencia suficiente, pausar/rechazar o alternativa explícita, nunca pose/confianza inventadas. Multijugador 1–4 con identidad técnica y track temporal validados; no asignación cruzada por proximidad. Confirmar golpe no confirma impacto/scoring; confirmar lanzamiento antecede a física/spin/hook Unity.

[Contrato, estados y pruebas](PHASE0_INPUT_FUSION_CONCEPT.md#9-reconocimiento-de-intención-y-prevención-de-activaciones-falsas--dec-009): sesiones etiquetadas, falsos positivos/negativos, lentos/rápidos, duplicados/prematuros, atribución cruzada y latencia física, por dispositivo/fuentes/modalidad. Valores definitivos requieren evidencia física y aprobación; ninguna precisión demostrada ahora. Sólo diseño dentro de Fase 0 existente, sin detector/minijuegos/cámara implementados, transporte independiente pendiente, #94 OPEN/DRAFT, Fase 0 IN PROGRESS y Fase 1 NOT STARTED.

## Precisión de cierre Fase 0 aprobada por Product Owner — 2026-10-07

El cierre **COMPLETED — SOFTWARE/LAB** requiere todos los gates de base, transporte único, session/admission/QR, protocolo, pipeline PWA/Java/IPC/Unity, webcam PC, Player Lock, temporal alignment, Input Fusion infrastructure, fixtures, 1–4 clientes, resiliencia/regresión, cleanup e integración PASS. No se implementa el detector/minijuego final de Smash en Fase0. iPhone/Android físicos, cadencia/tuning móviles, movimiento humano fusionado y latencia física son **NOT RUN / DEFERRED**, obligatorios antes de aceptar el MVP y no bloquean el cierre Software/Lab. Linux es el entorno de laboratorio actual; Windows DEFERRED. Fase1 sólo READY TO START después de auditoría final PASS; el estado actual sigue IN PROGRESS/NOT STARTED. Las restricciones históricas sobre implementar Spikes de RTC/cámara/Fusion quedan reemplazadas exclusivamente por el alcance técnico A–L autorizado; no autorizan gameplay final ni servicios cloud ni cambios de infraestructura.

## Enmienda vigente — runtime móvil Software/Lab, 2026-10-07

Por instrucción del PO, DEC-010 Mobile Transport queda ACCEPTED / IMPLEMENTED FOR PHASE 0 LAB: **WebRTC DataChannel**, único transporte móvil seleccionado; comparador CLOSED. Sustituye, para este alcance, WebSocket obligatorio o selección WT/RTC pendiente de enmiendas anteriores. WT permanece como evidencia experimental histórica aislada. Sin signaling cloud, STUN/TURN público ni vídeo móvil.

Session/admission/QR técnico, Gorilla Protocol v1 común, adaptador PWA/Java y extensión opt-in del mismo IPC loopback hacia PhoneInput Unity están implementados y validados en laboratorio con 1–4 clientes Chrome sintéticos/replay. Java sólo administra identidad/sesión técnica/transporte/validación/routing; Unity sigue siendo la única autoridad competitiva. Sin gestos deportivos, scoring, Fusion o cámara nuevos. [Contrato real](../Shared/Protocol/mobile/README.md) y [gate, evidencia y deuda](PHASE0_MOBILE_INPUT_RUNTIME_VALIDATION.md).

El resultado no valida onboarding HTTPS de un teléfono nuevo ni red física. Emulador RTC nuevo BLOCKED; iPhone/Android físicos/Windows DEFERRED. Camera/Input Fusion NOT STARTED; 3A/Fase 0 IN PROGRESS; Fase 1 NOT STARTED. PR #94 permanece OPEN/DRAFT; sin push/merge ni siguiente incremento automático.


## Enmienda vigente — infraestructura cámara/fusión Software/Lab, 2026-10-08 UTC

El incremento autorizado implementa CameraInput raw de PC, asociación temporal/Player Lock por turno,
alineación acotada y FusionFrame con fuentes/calidad/ausencias explícitas, usando el runtime móvil validado.
No es recognizer de deporte, calibración inmersiva final, scoring ni Fase1. DEC-012 limita el adaptador
MediaPipe/OpenCV supervisado a Linux laboratorio; empaquetado distribuido/Windows pendiente. Timestamps
PC receipt/read-complete son proxies, no sincronización física demostrada. [Gate y deuda](PHASE0_CAMERA_FUSION_VALIDATION.md).

Camera/Fusion NOT STARTED de secciones anteriores describe su momento histórico. Resultado vigente se
registra en DEVELOPMENT_PROGRESS/TEST_REPORT del incremento. Fase0/3A IN PROGRESS y Fase1 NOT STARTED;
#94 OPEN/DRAFT. El siguiente bloque será únicamente FINAL PHASE0 AUDIT, no iniciado en esta entrega.

## Enmienda vigente — cierre FINAL PHASE0 AUDIT, 2026-10-08

Por el gate final autorizado por el PO, **PHASE 0 FINAL AUDIT: PASS — SOFTWARE/LAB**.
Fase0 — Base/Spike: **COMPLETED — SOFTWARE/LAB**; Fase1 — Gorilla Smash Vertical Slice: **READY TO START**, no iniciada. La [auditoría](PHASE0_FINAL_AUDIT.md) acredita cada gate técnico y las regresiones del candidato local.
Foundation/2A/2B/2C PASS/MERGED; móvil/cámara/lock/alineación/Fusion infra PASS Software/Lab local, todavía sin integración Git de cambios recientes. #94 OPEN/DRAFT; local candidate != remote PR head. Sin commit/push/merge ni gameplay.
3A product/physical onboarding **DEFERRED / IN PROGRESS**; deuda física/plataforma sigue abierta y obligatoria antes de aceptación final del MVP. No declarar cámara/fusión físicamente validadas, no inferir timing humano de replay ni convertir emulador RTC BLOCKED en PASS. Esta enmienda prevalece sobre estados de avance anteriores; no elimina requisitos de producto ni cambia thresholds.


# Enmienda PO — hardware QA y compatibilidad por capacidades (DEC-014)

T002 define plataformas/capacidades y REFERENCE TEST HARDWARE, no únicos equipos soportados. Android moderno/Chrome estable e iOS moderno/Safari del sistema; secure context/WebRTC DataChannel/touch/LAN, sensores según disponibilidad y permisos normales. Detección runtime requerida con FULL/LIMITED/INCOMPATIBLE sin inventar señales, no implementada por esta decisión. Red Wi-Fi LAN estándar, WAN opcional, gameplay local. Modelos/versiones/firmware se registran en QA física: Android no identificado/router UNKNOWN no bloquean T002. T019 y gates físicos mantienen todas sus obligaciones, sin compatibilidad universal prometida. [DEC-014/inventario](PHASE0_T002_HARDWARE_INVENTORY.md).
