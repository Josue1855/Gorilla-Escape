export const screens = {
  CONNECT: ['Partida encontrada', 'El enlace de tu QR está listo. Conecta con la PC para continuar.', 'CONECTAR', 'ready'],
  NO_ADMISSION: ['Escanea el QR de la PC', 'Abre Gorilla Escape en la PC y escanea un QR nuevo para incorporarte.', null, 'warning'],
  CONNECTING: ['Conectando con la PC…', 'Mantén el teléfono en la misma Wi-Fi que la PC.', null, 'connecting'],
  PERMISSION_REQUIRED: ['Conectado. Activa tu control', 'Permite el acceso al movimiento y la orientación con el siguiente botón.', 'ACTIVAR CONTROL', 'success'],
  REQUESTING_PERMISSION: ['Esperando tu permiso…', 'Responde a la solicitud del navegador, si aparece.', null, 'connecting'],
  PERMISSION_GRANTED: ['Acceso al movimiento permitido', 'La señal de sensores está pendiente de comprobar. Todavía no has completado la preparación para jugar.', null, 'success'],
  PERMISSION_DENIED: ['Movimiento sin permiso', 'No podemos acceder a una o más APIs de movimiento. Puedes intentarlo de nuevo. Si el navegador recuerda tu respuesta y no muestra la solicitud, revisa los permisos de este sitio en el navegador y vuelve aquí.', 'REINTENTAR PERMISO', 'warning'],
  SENSOR_UNAVAILABLE: ['Movimiento limitado', 'Una o más APIs de movimiento no están disponibles. El acceso que sí existe se conserva; la señal y los modos compatibles están pendientes de verificar. Prueba un navegador o dispositivo con estas capacidades y abre un QR nuevo.', 'VOLVER A COMPROBAR', 'warning'],
  DISCONNECTED: ['Se perdió la conexión', 'Comprueba la Wi-Fi y que Gorilla Escape siga abierto en la PC. Escanea un QR nuevo para volver a conectar.', null, 'error'],
  ERROR: ['No se completó la preparación', 'Comprueba la Wi-Fi, la confianza HTTPS y el QR vigente. Si el problema fue el permiso, puedes repetir la solicitud.', null, 'error'],
};
