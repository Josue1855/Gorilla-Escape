# Certificación Técnica: Seguridad, Privacidad y Licencias

**Estado:** **PASS** (Cero vulnerabilidades bloqueantes, licencias verificadas).

---

## 1. Seguridad Informática y Secreto de Tokens

- **Aislamiento de Tokens Cloudflare:** El instalador público y los binarios cliente no contienen ningún token privado de túnel Cloudflare embebido. La autenticación ocurre en el entorno anfitrión del servidor.
- **Seguridad en Admisión QR:** Los tokens de admisión incorporados en el QR son efímeros, de un solo uso por participante y están firmados criptográficamente mediante HMAC.
- **Aislamiento de Procesos:** La comunicación entre Unity, Java y Python ocurre estrictamente a través de IPC loopback autenticado en `127.0.0.1`, sin puertos abiertos indebidamente a interfaces de red públicas.

---

## 2. Privacidad de Cámara y Datos Personales

- **No Persistencia de Video:** La señal de la cámara web capturada por OpenCV se procesa estrictamente en la memoria RAM del proceso `worker.py` para extraer landmarks numéricos (coordenadas $X, Y, Z$). Ningún fotograma de video o imagen facial es grabado en disco ni transmitido a servidores remotos.
- **Cumplimiento de Privacidad:** Conforme a las normativas de protección de menores, el sistema no almacena identificadores biométricos persistentes.

---

## 3. Cumplimiento de Licencias de Terceros y Modelos 3D

- **Avisos de Terceros:** El archivo `THIRD-PARTY-NOTICES.md` documenta de forma exhaustiva las licencias de software libre aplicadas (Apache 2.0, MIT, BSD, URP).
- **Modelos 3D Meshy AI:** Se verificó que los modelos 3D y texturas (`Meshy_AI_Cartoon_Coconut_Palm`, `Meshy_AI_Low_Poly_Gorilla_Embr_biped`, etc.) fueron generados bajo suscripción comercial Pro autorizada para distribución.
