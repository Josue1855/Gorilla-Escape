# Índice de evidencias

Última revisión: 2026-10-16. Fuentes: S1, S2, S4, S5 y S6; identificadores definidos en [inventario de fuentes](../00-product/SOURCE_INVENTORY.md). Las etiquetas describen el alcance indicado, no una certificación global.

| Fuente | Qué acredita | Qué no acredita |
|---|---|---|
| [S2 validación final 2026-10-16](../evidence/phone-connection-final-2026-10-16/validation.json) | Java 112: 111 pasan/1 omitido externo; PWA 69/69; Unity EditMode 162/162 y PlayMode 29/29; build Linux 0 errores/0 warnings; Vision 23, preview 2 y firewall 11 | Teléfono físico final, PC limpia, juegos o persistencia |
| [S2 decisión de conexión](../PHASE0_PHONE_CONNECTION_FINAL.md) | Alcance QR/firewall, límites y autorización de inicio paralelo | Garantía universal de onboarding sin permisos ni red |
| [Histórico IPC 2C](../PHASE0_IPC_2C_VALIDATION.md) | Corpus/deadlines, procedimientos y mediciones locales delimitadas | Latencia sensor→respuesta visible |
| [Histórico cierre](../PHASE0_FINAL_EXIT_REPORT.md) | Gate físico pendiente y evidencias previas | Cierre E0/F05/T019/T020 |
| S5 build UI/UX de esta auditoría | `npm ci --ignore-scripts` exit 0; `npm run build` exit 0 en S4; 27 módulos; dist sin sw.js ni iconos | Runtime/browser, GET real Spring, instalación/offline o POST SQL |
| S6 consultas GitHub | Repositorios/permisos/issues/PR/milestones y Project existentes | Propiedad o recursos no consultables; no es ejecución remota de cambios |

S5 fue ejecutado en copia temporal limpia de S4; git status posterior sin cambios rastreados. Artefactos dist no se incorporan. Dependencias resueltas se registran en [inventario](../00-product/SOURCE_INVENTORY.md). El instalador reportó 2 vulnerabilidades (1 moderada, 1 alta), sin aplicar fixes ni revisar aquí su explotabilidad; requiere tarea de análisis en S4.

La evidencia previa es un registro persistido, no una nueva ejecución del encargo. Los sourceHashes S2 permiten contrastar archivos funcionales con S1; los documentos históricos se verifican contra su revisión original porque esta tarea añade enlaces/avisos. No publicar reportes brutos con credenciales para aumentar el volumen de evidencia. Las nuevas ejecuciones académicas deben añadir evidencia auténtica referenciada aquí y conservar los fallos.
