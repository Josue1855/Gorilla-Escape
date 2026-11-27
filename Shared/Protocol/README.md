# Gorilla Protocol v1

Fuente normativa: v4 §8. Fixture sensor.json consumida por tests Java, JavaScript y Unity. Publicar cambios coordinados con consumidores; no cambiar versión ni mayúsculas de mensajes sin acuerdo. Envelope genérico no valida sesiones ni implementa transporte. El timestamp y los tipos se deben precisar antes del handler WebSocket.

## Contratos implementados vigentes

La fixture sensor.json/envelope genérico anterior es histórica; el contrato móvil validado es [mobile v1](mobile/README.md), seleccionado con WebRTC en DEC-010. [IPC](ipc/README.md) y [CameraInput](camera/README.md) conservan fronteras independientes; no son protocolos móviles alternativos. [Auditoría](../../docs/PHASE0_FINAL_AUDIT.md).
