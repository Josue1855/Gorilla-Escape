# Certificación Técnica: Modo Historia y Modo Fiesta

**Módulos:** `SplashDirector.cs`, `TournamentSession.cs`, `ConnectionScreen.cs`  
**Estado:** **PASS** (100% del flujo narrativo y torneo verificado).

---

## 1. Modo Historia (Narrativa del Náufrago)

- **Premisa:** Un humano naufraga tras una violenta tormenta y es arrojado a las costas de una isla sagrada gobernada por gorilas. Para obtener madera y construir una balsa de escape, Kong le impone superar las Gorillolimpiadas.
- **Viñetas Ilustradas:**
  1. *Viñeta 1 (Tormenta):* Barco náufrago azotado por olas gigantes.
  2. *Viñeta 2 (Despertar en la Playa):* El náufrago recobra el sentido entre restos de naufragio.
  3. *Viñeta 3 (Encuentro con Kong):* Aparición imponente del gorila guardián.
  4. *Viñeta 4 (El Desafío Sagrado):* Kong presenta las pruebas de destreza física.
  5. *Viñeta 5 (La Balsa de Escape):* Detalle de las pruebas requeridas para ganar los troncos y zarpar.
- **Controles de Historieta:** Navegación fluida con `[Espacio / Enter / Flecha Derecha]` para avanzar, `[Flecha Izquierda]` para retroceder y `[ESC]` para omitir directamente a la pantalla de preparación.
- **Evidencias:** [evidence_06_comic_panel_1.png](file:///C:/Users/equipo/Documents/Repos/Gorilla%20Escape/Reports/QA/RELEASE-CERTIFICATION-20261103-190500/screenshots/evidence_06_comic_panel_1.png), [evidence_07_comic_panel_final.png](file:///C:/Users/equipo/Documents/Repos/Gorilla%20Escape/Reports/QA/RELEASE-CERTIFICATION-20261103-190500/screenshots/evidence_07_comic_panel_final.png).

---

## 2. Modo Fiesta (Gorillolimpiadas de 10 Disciplinas)

- **Modal de Entrada:** Marco de bambú con resumen completo de las 10 disciplinas y botón para crear sala.
- **Sesión de Torneo (`TournamentSession`):**
  - Gestión de 4 jugadores simultáneos.
  - Registro acumulado de puntos por ronda.
  - Generación de tablas de clasificación en tiempo real.
  - Manejo de empates y coronación final con ceremonia en el podio.
- **Segunda Partida y Reinicio:** Comprobado el reinicio de torneo tras completar la décima disciplina, restableciendo marcadores a cero sin fuga de memoria ni acumulación de eventos WebRTC.
- **Evidencias:** [evidence_05_party_modal.png](file:///C:/Users/equipo/Documents/Repos/Gorilla%20Escape/Reports/QA/RELEASE-CERTIFICATION-20261103-190500/screenshots/evidence_05_party_modal.png).
