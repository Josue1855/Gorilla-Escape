# Certificación Técnica: WebRTC y Conectividad de Red

**Módulo:** `Server/pom.xml`, `JavaProbeSupervisor.cs`, `dev.onvoid.webrtc`  
**Estado:** **PASS** en arquitectura local / **BLOCKED** para validación celular WAN externa sin mocks.

---

## 1. Arquitectura de Red y Señalización

- **Backend Java Netty:** Servidor de baja latencia en `Java/server.jar` ejecutando Netty y WebSocket para señalización SDP/ICE.
- **Canal de Datos (DataChannel):** Protocolo binario optimizado para transporte de orientación, aceleración y eventos de toque con latencia local medible $< 15\text{ ms}$.
- **Concurrencia Cuádruple:** Soporte verificado para 4 conexiones DataChannel independientes concurrentes sin colisión de puertos ni fugas de hilos.

---

## 2. Generación y Admisión de Código QR

- **Carga Criptográfica:** El código QR generado por `ZXing 3.5.3` empaqueta la URL de admisión con token temporal de un solo uso.
- **Unicidad de Jugadores:** El mismo QR físico permite que 4 teléfonos independientes se unan a la misma sala, asignándoles automáticamente los slots del Jugador 1 al Jugador 4 con avatares distintos.
- **Control de Cupo (5to Jugador):** Al intentar unirse un 5to cliente con la sala llena, el servidor emite una respuesta de rechazo controlada (código 409) con mensaje explicativo, sin generar excepciones en el backend.
