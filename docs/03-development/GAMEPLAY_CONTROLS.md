# Controles y mecánicas — criterio del producto (2026-11-05)

Instrucción del usuario: elegir con o sin celular según lo que resulte mejor para cada juego. No exigir un dispositivo por uniformidad. Cada prueba debe explicar objetivo, gesto, momento de actuar y resultado; el modo historia debe cambiar automáticamente de perfil y anunciar qué dispositivo se usa a continuación.

| Juego | Control elegido | Regla comprensible / revisión |
|---|---|---|
| Gorilla Smash | Celular + cámara | Cámara = posición real del mazo y precisión sobre el blanco naranja; celular = potencia. Mazo original compartido con Whack. El impacto se resuelve al cruzar la altura de la plataforma, con barrido entre muestras. Puntaje: 45 % potencia, 35 % precisión y 20 % sincronía; fuera del blanco da cero. |
| Coconut Throw | Celular + cámara | Apuntar, impulsar sin soltar el teléfono y observar el alcance. Modelo de coco real, textura Coconut_Albedo y escala corregida; vuelo analítico existente. |
| Coconut Bowling | Celular + cámara | Apuntar y lanzar suavemente con la mano dominante. Se conservan física a paso fijo, curva según posición de la bola y reintento. |
| Jungle Slice | Solo cámara | Cortar con una o ambas manos y evitar peligros. Se conservan barrido entre muestras y recuperación individual de manos. |
| Jungle Balance | Solo celular | Inclinar suavemente y compensar la inercia; no exige cámara. Se mantienen subpasos, caída y final deterministas. |
| Gorilla Boxing | Solo cámara | Ambas manos tienen el mismo umbral y potencia de golpe. Manos arriba bloquean; cuerpo a un lado esquiva. Aviso de ataque de 0.85 s y extensión del puño sincronizada con daño; modelo animado según direcciones de sus huesos. Recuperarse subiendo/bajando la mano derecha cuatro veces. |
| Jungle Baseball | Solo cámara | Esperar «Ahora» y cruzar la mano derecha horizontalmente. Un swing por intento; contacto alrededor del plato con ventana de ±0.24 s, calidad según timing y velocidad de la mano. Lanzamiento analítico y barra de llegada. |
| Jungle Drums | Solo cámara | Bajar la mano del color al llegar la nota al centro de la línea; levantar entre golpes. Inicio de cuatro segundos, patrón alternado a ritmo más lento, dobles después de practicar. Toda pulsación suena; un golpe fuera de tiempo no añade un segundo fallo a una nota que todavía puede acertarse. |
| Gorilla Whack | Celular + cámara | Tres alturas en una columna. Subir y bajar la mano derecha, sin profundidad ni apuntado horizontal. Cámara para la altura; celular para la potencia. Mazo existente compartido con Smash. Barrido vertical para no saltar el objetivo entre muestras; hay que volver a subir después de un acierto. Acierto: 100 puntos + hasta 40 de precisión + hasta 60 de potencia + hasta 60 de combo. La potencia se satura para que no convenga agitar cada vez más fuerte. |
| Jungle Keeper | Solo cámara | Interceptar tiros con manos y torso. Se conservan zonas visibles, caducidad y resolución única de cada tiro. |

Los juegos de cámara se ofrecen en el catálogo sin celular y también en el catálogo completo. El perfil se aplica al entrar en cada escena, incluidos historia y fiesta. Los controles combinados anteriores se conservan en la frontera de entrada para compatibilidad, pero box, béisbol y tambores ya no los seleccionan ni los exigen.

Al bloquear el teléfono, se suspenden sensores y se descartan acciones pendientes. Al desbloquear, se comunica el estado activo; si se perdió el transporte, se usa la credencial de reanudación del mismo jugador. Los permisos ya concedidos y la captura se recuperan sin esperar otra visita al QR ni al primer contexto del juego. La pausa técnica debe terminar al recibir muestras nuevas y estables; una pausa manual sigue siendo manual.

La autorización del usuario sustituye la obligación histórica de usar teléfono y cámara en todas las pruebas. No altera la autoridad de Unity ni los requisitos de aceptación física. Los umbrales y el ritmo elegidos necesitan una partida con personas y hardware real antes de considerarse afinados definitivamente.

Corrección explícita del usuario (2026-11-06): Whack sí requiere celular para potencia, conservando la columna vertical de cámara. Smash separa precisión de cámara y potencia móvil, con el mismo mazo ya existente. El coco se reutiliza del catálogo actual, sin crear otro asset.
