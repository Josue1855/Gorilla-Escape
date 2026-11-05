# Splash con el gorila Meshy de los juegos

Se usa `JungleStageBuilder.LoadBowlingGorillaPrefab()` tanto en los juegos como en el splash: la misma malla Low Poly Gorilla Embr y el mismo material compartido. Se retiró del splash la sustitución por Guardian.

La carrera se anima sobre los ocho segmentos principales del esqueleto propio, calculando las rotaciones desde sus direcciones de reposo. Incluye alternancia de brazos y piernas, rodillas flexionadas, inclinación del cuerpo, plantas niveladas y corrección del apoyo. La medición inicial de las plantas usa la geometría real para evitar la holgura de los límites de animación importados. El personaje mantiene la escala al correr y detenerse.

La prueba de Play Mode compara por referencia la malla con el prefab de los juegos, verifica la textura Embr, movimiento de huesos, 32 muestras de apoyo, tamaño y permanencia dentro del encuadre. Resultado en [results.txt](results.txt). Capturas: [inicio de carrera](run-1.80.png), [zancada alterna](run-2.05.png), [aproximación](run-4.20.png).

Cambios locales, sin publicar.
