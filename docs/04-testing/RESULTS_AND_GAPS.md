# Resultados de auditoría y discrepancias

Última revisión: 2026-10-16. Fuentes: S1, S2 y S3; identificadores definidos en [inventario de fuentes](../00-product/SOURCE_INVENTORY.md). Las etiquetas describen el alcance indicado, no una certificación global.

| Discrepancia | Resolución documental / acción pendiente |
|---|---|
| README describe cinco pruebas; encargo actual define cuatro | Índice/alcance actual usa cuatro; v4 y milestones anteriores permanecen como historia |
| Todo debe vivir en monorepo vs tres proyectos | Documentar organización objetivo y tres repositorios reales (backend S7 sólo documental); decisión de empaquetado sigue pendiente |
| UI/UX afirma controlador sincronizado | App.jsx sólo cambia estado local; no hay cliente técnico/RTC allí |
| «Selección de gorila atleta» vs humanos participantes | Requisito humano vigente; revisar copy/modelo UI antes de integración |
| README UI/UX promete PWA offline | Build S5 no contiene SW/iconos; precache fuente referencia JSX no emitido; verificación offline pendiente |
| GET demo interpretado como backend real | JSONPlaceholder es demo; no acredita Spring/SQL ni POST |
| Campos QR planos mencionados en descripción inicial | S1 tiene QaHostingWire.prepare anidado; no crear contrato paralelo |
| Apertura 8081/RFC1918 amplia atribuida al runtime actual | PR #121 usa interfaz/subred/destino concretos y 8081 temporal de recuperación |
| Preparar certificado como paso normal | DEC-031 da un JOIN QR y recuperación explícita; preparación no preventiva |
| Fase0 «completa» de auditoría histórica | Software/Lab acotado; #33/#34/#31/#1 abiertos y evidencia física final faltante |
| PR #94 descrito en algunos documentos como draft | Snapshot actual de PR accesibles muestra merges; verificar SHA/ancestría, no copiar estado antiguo |
| «Spring Boot» interpretado como BD existente | Java técnico no contiene JPA/SQL; backend independiente creado S7, sin implementación |
| Número de issues del template como estado live | S6: 95 issues, 68 abiertos/27 cerrados; Project 95 items. Template local no es estado operativo |

Los documentos históricos se conservan íntegros salvo avisos de lectura en las entradas principales. No se actualizan retroactivamente logs de ejecución ni se ocultan fallos. El [informe](../00-product/AUDIT_REPORT.md) registra alcance y verificaciones editoriales; las preguntas bloqueantes están en [próximos pasos](../05-management/NEXT_STEPS.md).
