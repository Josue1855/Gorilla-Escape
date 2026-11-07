# Diagnóstico de descarga del runner — 2026-10-27

La primera ejecución de Foundation del PR #131, [40520408011](https://github.com/Josue1855/Gorilla-Escape/actions/runs/40520408011), se canceló durante la preparación de audio. No llegó a las pruebas Java ni al harness nuevo. Los checks de documentación y dependencias web ya habían pasado.

El log confirma que apt solicitó 55 paquetes (34.1 MB). Las descargas desde azure.archive.ubuntu.com tardaban varios segundos incluso para paquetes pequeños; libcodec2 de 8 998 kB demoró aproximadamente cuatro minutos entre sus registros. La preparación seguía descargando al cancelar, más de siete minutos después de empezar. Esto no acredita un fallo de gameplay ni una prueba aprobada.

La corrección cambia únicamente la preparación del runner efímero: reemplaza esa entrada por el archivo oficial archive.ubuntu.com, conserva las verificaciones de firma de apt, limita reintentos/esperas y añade límites de cinco minutos al paso y quince al job. El arranque de PulseAudio tiene un máximo de veinte segundos. No se omiten pruebas WebRTC ni se instala nada en la PC del usuario.

El código de los juegos y los resultados locales de 71 pruebas permanecen iguales durante este ajuste de CI. Los resultados del nuevo head deben consultarse en los checks del PR; la ejecución cancelada no cuenta como PASS.
