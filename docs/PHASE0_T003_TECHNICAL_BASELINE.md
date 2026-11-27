# T003/#12 — Phase 0 Technical Baseline

**DEC-015 Accepted**, 2026-10-08, bajo autorización condicional explícita PO: fijar sólo valores sustentados por evidencia existente. Base integrada auditada `65aecf8c3198ed960a43eaa50cfcc495f4562b53`. Rama documental `docs/phase0-t003-baseline`; sin código de producto ni actualización de dependencias.

## Baseline confirmado

| Componente | Versión exacta / identidad | Evidencia |
|---|---|---|
| Unity Editor | 6000.3.23f1, revision09d2ecc7fb28 | [ProjectVersion](../Unity/ProjectSettings/ProjectVersion.txt); audit Edit64/Play16/buildLinux0errores/0warnings retenido |
| Unity UPM Linux | SDK1.1.0, toolchain1.1.0, sysroot1.1.0 | manifest/packages-lock en Unity/Packages; mismo baseline auditado |
| Tests Unity | test-framework1.6.0, ext.nunit2.0.5 | packages-lock; no CI Unity inventado |
| Java | release21; JDK laboratorio Ubuntu OpenJDK21.0.12.1+1-1-24.04.4-Ubuntu | [POM](../Server/pom.xml) java.version21; JDK observado; nuevo build javac release21/classmajor65; CI Temurin21 no implica misma distribución/patch |
| Spring Boot / Spring Framework | Boot4.1.1 / core7.0.9 resuelto por parent | POM y JAR auditado; no override de Spring introducido |
| Maven | 3.9.11; wrapper3.3.2; compilerplugin3.15.0 | [wrapper](../Server/.mvn/wrapper/maven-wrapper.properties); hash distribución configurado; plugin efectivo observado en build nuevo. No Gradle |
| Java WebRTC | dev.onvoid.webrtc:webrtc-java:0.19.0 + classifier linux-x86_64 | POM, JAR auditado y libwebrtc-java-linux-x86_64.so; RTC→Java→IPC→Unity lab PASS |
| Gorilla Protocol | mobilev1 (CURRENT=1), IPCprobev1, CameraInputv1 | [contratos](../Shared/Protocol/README.md), código/fixtures de consumidores; no tres protocolos móviles |
| Adaptador visión LAB | Python3.12.3, MediaPipe Tasks0.10.33, opencv-contrib-python4.13.0.92 (cv2 4.13.0), NumPy2.5.3 | [lock](../tools/spikes/camera/requirements-linux-lab.lock), metadatos instalados e inventario nativo, worker/supervisor actuales |
| Modelo | Pose Landmarker lite float16 v1, pose_landmarker_lite-v1.task,5777746bytes | SHA256 `59929e1d1ee95287735ddd833b19cf4ac46d29bc7afddbbf6753c459690d574a`, archivo privado coincide con inventario histórico |
| Plataforma medida | Dell Latitude5540 / Pop!_OS24.04LTS x86_64, kernel7.1.5-76070105-generic, i7-1355U/IrisXe, Integrated_Webcam_FHD | Referencia QA DEC-014, no único hardware soportado |

Java21 es dependencia de desarrollo (JDK/Maven). En ejecución Java necesita runtime21 adecuado; el producto final no debe exigir SDK/JDK/UnityEditor al usuario: runtime/empaquetado independientes siguen pendientes de T020. Esta decisión fija el laboratorio, **no adopta Python como distribución final ni demuestra instalación limpia**. El runtime debe localizar configuración externa de sus propios componentes como ya documentado.

## Candidatos realmente considerados / evaluados

| Componente | Candidato | Evidencia histórica / resultado | Decisión |
|---|---|---|---|
| Unity | 6000.3.23f1 actual versus Foundation inicial en misma versión | [Foundation](UNITY_FOUNDATION_VALIDATION.md) y [audit final](PHASE0_FINAL_AUDIT.md): builds/tests PASS; no otra versión Unity ensayada demostrada | Mantener versión exacta; no fabricar comparativa de otra release |
| Backend | Java21 actual versus propuesta ASP.NET/.NET anterior | DEC-002 conserva consideración histórica del stack anterior; Java21 tiene build/tests/IPC real, .NET sin build comparativo acreditado | Mantener Java21; contraste de evidencia, no benchmark entre runtimes |
| Transporte | webrtc-java0.19.0 actual versus WT netty-codec-webtransport0.0.1-SNAPSHOT (source b63ebb06b0ab33af73c9bf1f96bedd9e7cb73243, Netty4.2.12.Final) | [RTC comparator](evidence/webrtc-comparator-2026-10-07/run04-reviewed.json) y [WT inventario](evidence/webtransport-spike-2026-10-07/dependencies.json); ambos echo local lab probado; cargas/N distintas | DEC-010 mantiene **WebRTC DataChannel como transporte seleccionado de producto**, implementación/validación aún Software/Lab. WT histórico, no segundo runtime de producto. Sin ranking RTT entre cargas distintas |
| Visión | Python Tasks0.10.33/OpenCV4.13.0.92 versus plugin Unity C#/Java o C++ | [Decisión de adaptador](../tools/spikes/camera/README.md): plugins preexistentes no disponibles; C++ considerado añadiría toolchains/packaging, sin build comparativo probado. Python actual captura/inferencia/pipe/Unity/replay lab PASS | Mantener LAB ADAPTER conocido; alternativas consideradas no se etiquetan FAIL ni medidas. Distribución final pendiente |
| Contrato móvil | mobilev1 estricto actual versus envelope/fixture sensor genérico histórico | [Shared Protocol](../Shared/Protocol/README.md) y corpus/consumidores auditados | v1 actual, no cambiar versión por novedad; fixture histórica no equivale a contrato completo |

No se compararon versiones actuales de Internet ni se inventaron benchmarks de versiones nunca probadas. Build mínimo nuevo Java y build mínimo Unity histórico con hashes idénticos sustentan combinación actual; no repetición de benchmarks largos.

## Compatibilidad mínima

| Componente | Linux referencia | Windows | macOS | Observación |
|---|---|---|---|---|
| Unity6000.3.23f1 Player/IPC | VALIDATED | NOT VALIDATED | NOT VALIDATED | Linux development MonoPlayer, build/tests y cierre; no PC limpia |
| Java21/Boot4.1.1 | VALIDATED | NOT VALIDATED | NOT VALIDATED | Portabilidad Java no equivale a ejecución ensayada en otros SO |
| webrtc-java0.19.0/JNI | VALIDATED | NOT VALIDATED | NOT VALIDATED | Sólo native linux-x86_64 observado y usado; no extrapolar ARM |
| MediaPipe/OpenCV/Python LAB | VALIDATED | NOT VALIDATED | NOT VALIDATED | Python3.12.3/venv CPU Linux; packaging/licencias/modelo final pendientes |
| Protocolv1 JS/Java/C# | VALIDATED | NOT VALIDATED | NOT VALIDATED | Corpus integrado en referencia, sin ensayo de Players adicionales |

Safari/iPhone/Chrome Android son objetivos DEC-014, no compatibilidad física certificada por este cuadro. Selección Android/versiones exactas y matriz física permanecen T019/gates correspondientes. Secure-context/onboarding/LAN arbitraria tampoco quedan resueltos aquí.

## Restricciones y ruta nativa

Unity supervisa Java por IPC127.0.0.1; Java carga JNI RTC de su classifier, no plugin RTC dentro de Unity. Visión va webcamPC→OpenCV/MediaPipeCPU→child privado Python→CameraInput Unity; lab opt-in, pipes/EOF/cleanup, sin webcam móvil. Los108 hashes nativos y el modelo coinciden con la evidencia. Inventario licencia/familia Apache-2.0 no resuelve automáticamente avisos transitivos/redistribución: distribución final y modelo/nativos offline deben validarse en sus gates. Física de cámara640×480/17.61FPS sigue **<30FPS; T016 NO PASS**; tracking humano y control físico no se acreditan por replay.

## Política de upgrades

Congelar esta combinación reproducible Phase0. No actualizar por novedad. Cambios posteriores Unity/Java/plugin/modelo requieren necesidad concreta, evaluación de compatibilidad, regresión aplicable y decisión registrada PO. No cambiar mayores silenciosamente. JDK patch/distribución aquí registra la ejecución exacta; CI Temurin21 es evidencia separada, no patch ficticio. Una variación de patch también debe quedar registrada y evaluada, sin promesa de reproducibilidad binaria bit a bit entre distribuciones.

## Validación de T003

- **NUEVA:** config/version consistency;173 fuentes no documentales coinciden con audit;4 artefactos históricos exactos;108 nativos visión/modelo idénticos; release21 classmajor65 nuevo.
- **NUEVA:** mínimo Java compile/package offline en copia aislada, tests omitidos explícitamente; Maven BUILD SUCCESS. OriginalJAR no reemplazado.
- **NUEVA:** management validation y foundation validation; repetir tras documentación final y CI de PR.
- **RETENIDA:** audit Java48/48, Node18/18, UnityEdit64/64, Play16/16, Linux0errores/0warnings y RTC4Chrome/18negativos. Hashes/config sin cambios; no presentar como ejecución nueva.
- **NO EJECUTADA:** nueva Unity suite/build, físico móvil/humano, Windows/macOS, PC limpia; sin justificación para repetir suite costosa inalterada.

**AC1 PASS:** candidatos históricos versus combinación/build mínimo, con evidencia y límites distinguidos. **AC2 PASS:** versiones confirmadas y compatibilidad plugin/modelo/ruta nativa documentadas. DEC-015 Accepted bajo autorización PO; publicación documental vía PR hacia develop, no push directo ni merge automático. T003 sólo se marca completed tras verificaciones finales del candidato; no cerrar otros Issues ni iniciar T004/T005/T006/T007.

[Evidencia nueva sanitizada](evidence/t003-baseline-2026-10-08/consistency.json), [visión](evidence/t003-baseline-2026-10-08/vision-consistency.json), [JNI](evidence/t003-baseline-2026-10-08/rtc-native.json), [build aislado](evidence/t003-baseline-2026-10-08/minimum-build-result.json).
