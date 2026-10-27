# Dirección visual de Gorilla Escape

Investigación y propuesta: 2026-10-22. Estado: propuesta para orientar el diseño; no es una implementación ni aceptación visual final.

## Dirección recomendada

Aventura tropical artesanal, con geometría low poly y objetos construidos con materiales de la isla. El humano náufrago intenta escapar superando pruebas impuestas por gorilas. El tono combina aventura, humor y peligro comprensible. Esta recomendación es una interpretación de las referencias y de la historia del proyecto; no existe un estilo objetivamente mejor para cualquier juego.

La identidad nace de tres motivos propios: restos del naufragio, marcas de manos de los gorilas y una ruta de escape. Repetir estos motivos de forma consistente aportará más identidad que añadir vegetación o madera a todos los controles.

## Lo que ya existe

- Unity: escenario tropical, tablones de madera, título dorado y follaje. El código de MainMenuView ya declara esa dirección. Conviene refinarla y sustituir el acabado repetitivo por formas propias.
- PWA: paleta selvática, madera, arena e iconos Phosphor. Es una base aprovechable; las pruebas necesitan símbolos dibujados para el juego y una jerarquía más clara.
- Flutter: colores compatibles, pero aún predominan componentes Material y un personaje representado por un icono estándar. Aquí se nota más la diferencia entre una app de utilidad y el mundo del juego.
- El CSS actual de PWA bloquea el desplazamiento global. La implementación visual debe permitir acceder a ayuda, permisos y detalles con pantallas pequeñas y texto ampliado. El mando principal puede ser compacto sin esconder contenido necesario.

## Referencias y aplicación

| Referencia | Qué aporta a esta propuesta | Aplicación en Gorilla Escape |
|---|---|---|
| Donkey Kong Bananza, material oficial Nintendo | Siluetas expresivas, entornos vegetales y energía de aventura | Personajes legibles, vegetación con volumen y acciones principales contundentes; mantener la identidad del náufrago y los gorilas del proyecto |
| A Short Hike, sitio oficial | Exploración y protagonismo del paisaje con una presentación sencilla | Geometría sencilla, lectura clara del escenario y espacio libre alrededor de la interfaz |
| Visual Adventures on Sea of Thieves, charla de Ryan Stevenson en GDC 2018 | Referencia de un proceso de desarrollo visual para una aventura | Materiales artesanales y formas con personalidad. La ficha fue localizada, pero el video no se pudo revisar porque GDC Vault estaba en mantenimiento; no se atribuyen reglas específicas a la charla |
| Game Accessibility Guidelines | Contraste entre interfaz y fondo, especialmente con imágenes en movimiento | Texto sobre superficies tranquilas y sólidas, peligro indicado con palabras y forma además de color |

Las decisiones de color, composición y materiales aquí propuestas son criterio de diseño para este proyecto. Las referencias no autorizan reutilizar sus logotipos, personajes o recursos.

Fuentes consultadas:

- https://www.nintendo.com/us/store/products/donkey-kong-bananza-switch-2/
- https://ashorthike.com/
- https://www.gdcvault.com/play/1025015/Visual-Adventures-on-Sea-of/1000
- https://gameaccessibilityguidelines.com/provide-high-contrast-between-text-ui-and-background/
- https://fonts.google.com/specimen/Lilita+One
- https://fonts.google.com/specimen/Nunito+Sans

## Lenguaje visual compartido

| Elemento | Propuesta |
|---|---|
| Selva profunda | `#102B25`: fondo y superficies de lectura |
| Arena | `#F4E6C4`: texto y paneles claros |
| Madera | `#6A4028`: marcos y acciones del menú |
| Ámbar | `#E8B75F`: acción principal y progreso destacado |
| Agua | `#3F9C9A`: río y acentos ambientales |
| Peligro | `#E56A4F`: aviso acompañado de texto e icono |

Los colores son roles propuestos: cada pareja real se comprobará para contraste antes de aplicarla. Ámbar no debe significar simultáneamente acción principal, selección y peligro en el mismo contexto.

Títulos: probar Lilita One, por su peso y carácter compacto. Cuerpo y números: probar Nunito Sans, con pesos 500–700 y números tabulares donde estén disponibles. Son candidatos para una prueba visual con tildes, ñ y nombres largos, no una selección tipográfica cerrada. El logotipo necesita un dibujo propio a partir de esa dirección; una fuente por sí sola no crea una marca. Integrar fuentes localmente tras verificar su licencia y conservarla, para mantener la presentación sin conexión.

Formas: esquinas cortadas, biseles cortos y bordes algo irregulares. Texturas de baja frecuencia y contraste moderado, sin vetas detrás de los textos. La madera sirve como marco y señal, con superficies de lectura más calmadas. Una misma silueta debe funcionar en Unity, SVG y Flutter.

Iconos: símbolos originales para tronco, naufragio, gorilas y disciplinas. Mantener Phosphor o equivalentes sencillos sólo para cámara, ayuda, sonido y conexión. No mezclar estilos de iconos en las acciones principales.

## Cada pantalla tiene una función

### Menú de Unity

Una escena de la isla que cuente la historia: náufrago visible, un resto del barco, gorilas observando y una salida lejana sugerida. Reutilizar los modelos actuales; no hace falta un escenario nuevo para cada opción. Menú apoyado en un letrero de madera y acciones grandes: Jugar, Conectar teléfono y Ajustes. Las funciones de prueba permanecen en su acceso de desarrollo. El título y la acción principal se leen antes que la decoración.

### Inicio y conexión móvil, tanto PWA como Flutter

La misma composición básica: marca, frase breve, una ilustración sencilla de la isla y una acción principal. Flutter ofrece Escanear QR; la PWA abierta desde el QR ofrece Conectar. El estado real de conexión aparece junto a esa acción. Un recuadro inspirado en tela o papel de expedición puede agrupar los pasos de preparación. Evitar convertir la pantalla en una lista de tarjetas de configuración.

### Mando durante la partida

La interfaz ocupa poco espacio: nombre de la prueba, equilibrio, corrección direccional y avance. El borde y un símbolo identifican la prueba. El tronco puede inspirar el indicador sin dificultar su lectura. El humano sigue siendo el jugador; los gorilas funcionan como jueces o espectadores. Los datos técnicos y la conexión se despliegan bajo Detalles y Ayuda.

Cada prueba puede cambiar su símbolo y un acento de color conservando posiciones, tipografía y gestos. La misma prueba debe verse reconociblemente igual en PWA y Flutter.

## Movimiento y respuesta

Presión: el tablón se hunde unos 2–3 px durante 100–140 ms y vuelve con una respuesta corta. Selección: contorno claro y un desplazamiento leve. Fondo: hojas, agua y respiración discretas; durante el control no hay movimiento decorativo que compita con el equilibrio. Resultado: sello propio de la prueba, reacción del personaje y transición breve. Respetar movimiento reducido y permitir desactivar vibración y sonido.

## Orden de trabajo

1. Diseñar juntos tres estados representativos: menú principal, conexión móvil y mando del tronco.
2. Cerrar paleta, tipografía, logo, símbolos y componentes con esos estados reales.
3. Implementar la misma biblioteca visual en Unity, CSS/SVG y Flutter, usando assets y valores compartidos cuando corresponda.
4. Completar pausa, caída, victoria, espera y error con esa identidad.
5. Revisar a 320 px de ancho, iPhone 15, texto al 200 %, ratón/teclado y pantalla de PC; comprobar contraste, acceso a detalles y ausencia de cortes.

La primera entrega visual debería permitir evaluar las tres superficies juntas. El objetivo es reconocer Gorilla Escape por sus formas y motivos, incluso cuando el logotipo no esté visible.


## Primera implementación — 2026-10-23

Menú Unity: fuentes locales, sello de los gorilas, náufrago animado y mástil/vela del naufragio, controles de madera conservados y estados de conexión ligados al backend real. Los iconos de teléfono y jugadores se dibujan geométricamente para evitar dependencia de emojis.

PWA: ilustración de ruta y sello compartidos, fuentes locales, colores y contenedores consistentes, acciones con respuesta corta y foco visible. Ayuda y detalles se pueden desplegar sin el bloqueo global de desplazamiento anterior.

Flutter: tema compartido IslandStyle, banner/sello originales, tipografía local y botones con esquinas cortadas. Se conserva navegación atrás, escáner, permisos, datos del mando y vibración. Versión preparada 1.2.0 (6); instalación física pendiente.

Capturas y límites de validación: [evidencia de diseño](../evidence/island-design-2026-10-23/results.json). El PNG de Flutter es un render de widgets; la captura del mando PWA utiliza datos ilustrativos rotulados, no una partida física.


## Pantallas fijas de PWA — 2026-10-23

Por requisito de producto, la PWA no se desplaza en ningún eje. Control/Mando, Ayuda y Datos son vistas independientes con navegación fija. Los textos largos y el diagnóstico se paginan según el espacio medido, conservando todo el contenido. El nombre opcional y la recuperación de conexión tienen sus propias vistas. La ilustración se omite cuando falta altura; el equilibrio, la acción principal y la navegación mantienen prioridad. El marco se ajusta a VisualViewport para responder al teclado y al área visible del navegador.

Validación: tamaños 393×852, 320×568, 568×320 y 320×300; medición del documento, paneles y límites de botones; ayuda y diagnóstico mediante paginación sin desplazamiento. Es una revisión de navegador, no una certificación de Safari físico. La solicitud de texto ampliado anterior se implementa mediante reflujo y páginas en vez de desplazamiento.
