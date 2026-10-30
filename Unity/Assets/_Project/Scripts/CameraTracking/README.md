Antecedente histórico (superseded; estado vigente a continuación):

Austin: adaptador MediaPipe/OpenCV local pendiente del Spike; produce CameraInput, sin video persistido ni reconocimiento facial.

Estado vigente: CameraInputStore y VisionSupervisor local están implementados/validados Software/Lab; PC OpenCV/MediaPipe opt-in de laboratorio. El adaptador selecciona V4L2 en Linux y DirectShow con un índice de dispositivo (`GORILLA_CAMERA_DEVICE`, `0` predeterminado) en Windows. Windows no modifica controles globales de la cámara y usa tiempo de lectura completa cuando el backend no ofrece el timestamp V4L2. Player Lock, alineación y Fusion infrastructure están en Scripts/Input/Fusion. Seguimiento humano físico fiable, imagen no negra y distribución Windows siguen NOT RUN hasta preparar el bundle nativo y probar la cámara real. [Auditoría](../../../../../docs/PHASE0_FINAL_AUDIT.md).
