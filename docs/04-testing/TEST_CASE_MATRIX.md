# Matriz de casos requeridos

Última revisión: 2026-10-16. Fuentes: S1, S2 y S3; identificadores definidos en [inventario de fuentes](../00-product/SOURCE_INVENTORY.md). Las etiquetas describen el alcance indicado, no una certificación global.

| ID | Acción / condición | Resultado esperado | Estado de ejecución / fuente |
|---|---|---|---|
| NET-01 | Codec válido/inválido, framing y correlación IPC | Aceptar válido; cerrar conexión inválida sin filtrar datos | Pruebas históricas S1/S2; no repetidas aquí |
| NET-02 | EOF/cancelación/fallo de proceso propio | Cleanup y recuperación manual acotados | Pruebas históricas S1/S2; no prueba global de todo SO |
| NET-03 | Cuatro RTC nativos y pérdida bootstrap externo | Identidades preservadas y host pairs LAN | S2 Software/Lab; no teléfonos físicos |
| NET-04 | iPhone/cuatro teléfonos, permisos, JOIN, sensores, suspensión y fallos | Criterios T019: 20 connects/20 reconnects, >=95%, latencias/frecuencias físicas y cleanup | PENDIENTE DE EJECUCIÓN completa; #33 |
| NET-05 | Firewall existente/nuevo, cancelar/reintentar, cambio LAN, segunda ejecución/retirada | Sólo reglas propias acotadas, no falsa disponibilidad ni proceso privilegiado residual | Unitarias S2; matriz física PENDIENTE DE EJECUCIÓN |
| NET-06 | PC física separada sin SDK/Editor | Paquete relativo inicia y cierra con LAN/WAN según T020 | PENDIENTE DE EJECUCIÓN; #34 |
| UI-01 | UI/UX pulsa control con transporte conectado | Intención llega una vez al slot Unity, feedback del estado real | PENDIENTE DE EJECUCIÓN; integración no implementada |
| UI-02 | Build S4 y revisar dist | Build reproducible; SW/iconos/source precache válidos | S5 upstream detectó faltantes; S10 fork build/SW/assets PASS, pendiente incorporar upstream |
| UI-03 | Recargas offline de build compilada | Shell y assets coherentes; snapshot etiquetado y borrador persistente | S10 EJECUTADO en Chrome con red emulada; móvil/físico pendiente |
| GAME-01 | Casos de scoring Bowling y partida diez frames | Bonus/décimo frame y resultados según reglas aprobadas | PENDIENTE DE EJECUCIÓN; sin juego |
| GAME-02 | Golpe High Striker, altura/campana y repetición | Impacto válido único, puntos según curva aprobada | PENDIENTE DE EJECUCIÓN; sin juego |
| GAME-03 | Tres lanzamientos Shot Put, faltas y sector | Mejor distancia válida metros y sin marca si todos inválidos | PENDIENTE DE EJECUCIÓN; sin juego |
| GAME-04 | Trayectorias Slice/combos/obstáculos/replay | Cortes válidos únicos y penalización aprobada | PENDIENTE DE EJECUCIÓN; sin juego |
| DB-01 | Participante ajeno a sesión o resultado no autorizado | Rechazo sin filas parciales | PENDIENTE DE EJECUCIÓN; diseño propuesto |
| DB-02 | Misma clave concurrente; misma clave distinto payload | Una escritura o conflicto; rollback íntegro | PENDIENTE DE EJECUCIÓN; diseño propuesto |
| OFF-01 | WAN caída, LAN/backend local activos | Servicios locales probados independientemente de Internet | PENDIENTE DE EJECUCIÓN física/académica completa |
| OFF-02 | Backend inaccesible, LAN activa y posterior regreso | Error/pending visible y sin duplicar tras confirmar | S10 emulado: snapshot/borrador, intento incierto protegido y envío manual; físico pendiente, sin cola automática |
| LAB-01..10 | Procedimientos del laboratorio | Evidencia auténtica trazable por requisito | [Estado por requisito](PWA_TEST_LAB.md): SQL/GET/POST/UI ejecutados, tres capturas DevTools pendientes |

Los valores de aceptación física exactos se toman de [T019](../PHASE0_T019_PHYSICAL_MOBILE_QA.md), distinguiendo RTT técnico de latencia hasta respuesta visual. Cada caso requiere resultado observado, N y archivo/evidencia del commit ejecutado antes de clasificarse como verificado.
