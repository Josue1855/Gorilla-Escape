# Red, contratos Shared y supervisión

Última revisión: 2026-10-16. Fuentes: S1, S2 y S3; identificadores definidos en [inventario de fuentes](../00-product/SOURCE_INVENTORY.md). Las etiquetas describen el alcance indicado, no una certificación global.

S1/S2 conservan la arquitectura de Fase 0; la auditoría no introduce protocolos nuevos.

| Tramo | Contrato actual / evidencia |
|---|---|
| Bootstrap teléfono | HTTPS + POST JOIN/reconnect; admisión de un uso 30 s, identidad técnica asignada por Java; no identidad persistente de BD |
| Input móvil | Gorilla Protocol v1; RTC control ordered/reliable y motion unordered/maxRetransmits=0; host LAN sin STUN/TURN; hasta cuatro peers |
| Java ↔ Unity | TCP IPv4 **127.0.0.1**, puerto efímero Java, uint32 big-endian + JSON UTF-8 1..4096 bytes; token efímero, instanceId/connectionId/sequence |
| Lifecycle | READY no basta: RUNNING exige PONG; I/O en workers; heartbeat/deadlines; stdin EOF y cierre del hijo propio; recuperación manual, sin reinicios automáticos |
| Shared | `PhoneInputWire`, `LobbyWire`, `CameraInputWire` y fixtures versionados; contratos sin motor |

Fuentes normativas de wire: [IPC](../../Shared/Protocol/ipc/README.md) y [móvil](../../Shared/Protocol/mobile/README.md); código `ManagedProbe`, `ProbeCodec`, `JavaProbeSupervisor`; tests IpcFrameCodecTests, IpcLifecycleProcessTests, IpcHardeningProcessTests, ManagedProbeProcessTest y QaHostingContractTest.

La forma vigente es **QaHostingWire.prepare: QaAdmissionWire**, con `modules`, `size`, `state`, `ttlMs`; no campos planos `prepareModules/prepareSize`. El QR de preparación sólo pertenece a recuperación explícita tras fallo local; la experiencia normal ofrece un QR JOIN. Origen congelado tras publicarlo; resolver AUTO prueba HTTPS público con límite antes de fallback CABIN. La pérdida del túnel no debe reiniciar peers/epoch locales. Cloudflared es opcional y supervisado, con versión/hash fijados; S2 incluye un HTTP 200 y otro intento fallido, sin garantizar disponibilidad pública.

Firewall Linux actual: TCP 8443 y UDP 50000–50031 restringidos a interfaz/subred/destino seleccionados. TCP 8081 sólo en recuperación explícita con lease <=180 s/EOF, no apertura permanente para todo RFC1918. Helper de operaciones cerradas y elevación controlada, no Unity completo como root. Recibos de reglas propias, idempotencia y retirada acotada; no se borran reglas ajenas. La primera inspección UFW protegida puede requerir permiso aunque existan reglas equivalentes; el caso físico «sin prompt si ya existen» sigue sin demostrar.

Los puertos IPC/preview y credenciales no se exponen al teléfono ni al ingreso público. No registrar URLs de admisión, módulos QR, tokens, SDP, claves ni imágenes. T019 debe comprobar permisos/cancelación/reintento/segunda ejecución/cambio de red y cierre real. HTTPS/WSS mencionado en diseños antiguos no demuestra un transporte WSS de gameplay vigente: el seleccionado es WebRTC.
