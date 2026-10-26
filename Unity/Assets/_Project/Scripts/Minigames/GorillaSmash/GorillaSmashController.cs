using System;
using System.Globalization;
using GorillaEscape.Input;
using GorillaEscape.UI;
using UnityEngine;

namespace GorillaEscape.Minigames.GorillaSmash
{
    /// <summary>
    /// MonoBehaviour controller orchestrating the Gorilla Smash minigame within
    /// the Modular Monolith architecture. Connects phone inputs / keyboard fallback,
    /// domain simulation, procedural visuals, on-screen GUI, and phone telemetry.
    /// </summary>
    public sealed class GorillaSmashController : MonoBehaviour
    {
        [SerializeField] GorillaSmashSettings settings;

        GorillaSmashRules rules;
        GorillaSmashModel model;
        GorillaSmashGestureDetector detector;
        GorillaSmashStage stage;
        MainMenuController menu;

        bool keyboard = true;
        int player = 1;
        bool focused = true;

        public GorillaSmashModel Model => model;
        public int Player => player;
        public bool Keyboard => keyboard;
        public bool KeyboardAvailable => Application.isEditor || Debug.isDebugBuild;

        PhoneInputStore Phones => menu?.Runtime?.java?.PhoneInputs;
        static long Now => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        void Awake()
        {
            if (settings == null)
                settings = Resources.Load<GorillaSmashSettings>("GorillaSmashSettings");

            rules = (settings != null ? settings.rules : new GorillaSmashRules()).Copy();
            rules.Validate();

            model = new GorillaSmashModel(rules);
            detector = new GorillaSmashGestureDetector(rules);
            stage = new GorillaSmashStage(transform, rules);

            // Default to keyboard in editor/standalone debug if no phone connected
            keyboard = KeyboardAvailable;
            model.Begin(player);
        }

        public void Attach(MainMenuController owner)
        {
            menu = owner;
        }

        public void SelectKeyboard(bool value)
        {
            if (value && !KeyboardAvailable) return;
            keyboard = value;
            detector.Reset();
        }

        public void SelectPlayer(int value)
        {
            if (value < 1 || value > 4) return;
            player = value;
            RestartGame();
        }

        public void RestartGame()
        {
            detector.Reset();
            model.Begin(player);
            stage.ResetHammer();
        }

        public void NextAttempt()
        {
            if (model.State == GorillaSmashState.AttemptFinished)
            {
                detector.Reset();
                model.NextAttempt();
                stage.ResetHammer();
            }
        }

        void OnApplicationFocus(bool hasFocus)
        {
            focused = hasFocus;
        }

        void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                if (menu != null)
                {
                    menu.CloseGorillaSmash();
                    return;
                }
            }

            if (UnityEngine.Input.GetKeyDown(KeyCode.R))
            {
                RestartGame();
                return;
            }

            float dt = Mathf.Min(0.1f, Time.unscaledDeltaTime);

            // Handle user strike input
            if (focused && (model.State == GorillaSmashState.Ready || model.State == GorillaSmashState.Armed))
            {
                float power, direction, timing, confidence;
                bool strikeDetected = false;

                if (keyboard)
                {
                    strikeDetected = detector.TryDetectKeyboardStrike(dt, out power, out direction, out timing, out confidence);
                    model.SetArming(detector.IsArmed);
                }
                else
                {
                    strikeDetected = detector.TryDetectPhoneStrike(Phones, player, dt, Now, out power, out direction, out timing, out confidence);
                    model.SetArming(detector.IsArmed);
                }

                if (strikeDetected)
                {
                    if (model.TriggerStrike(power, direction, timing, confidence, Now))
                    {
                        stage.TriggerHammerSwing();
                        stage.ApplyImpactShake(power);
                    }
                }
            }

            // Progression key after attempt completed
            if (model.State == GorillaSmashState.AttemptFinished)
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.Space) || UnityEngine.Input.GetKeyDown(KeyCode.Return))
                {
                    NextAttempt();
                }
            }

            // Domain simulation tick
            model.Tick(dt);

            if (model.BellRung)
            {
                stage.RingBell();
            }

            // Visual stage update
            stage.Update(dt, model.PuckHeight, model.State);

            // Broadcast game state to connected phone companion
            SendPhoneTelemetry();
        }

        void SendPhoneTelemetry()
        {
            if (menu?.Runtime?.java == null) return;

            string Number(float n) => n.ToString("0.###", CultureInfo.InvariantCulture);
            string stats = "{"
                + "\"attempt\":" + model.CurrentAttemptNumber
                + ",\"maxAttempts\":" + rules.maxAttempts
                + ",\"state\":\"" + model.State.ToString() + "\""
                + ",\"puckHeight\":" + Number(model.PuckHeight)
                + ",\"bestScore\":" + model.BestScore
                + ",\"score\":" + (model.LatestAttempt != null ? model.LatestAttempt.score : 0)
                + ",\"tier\":\"" + (model.LatestAttempt != null ? GorillaSmashModel.GetTierDisplayName(model.LatestAttempt.tier) : "NONE") + "\""
                + ",\"bellHit\":" + (model.LatestAttempt != null && model.LatestAttempt.bellHit ? "true" : "false")
                + "}";

            menu.Runtime.java.PresentPhoneGame("gorilla-smash", model.State.ToString(), player, stats);
        }

        void OnGUI()
        {
            var headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            headerStyle.normal.textColor = new Color(1.0f, 0.85f, 0.2f);

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 17,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            titleStyle.normal.textColor = Color.white;

            var normalStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft
            };
            normalStyle.normal.textColor = new Color(0.9f, 0.9f, 0.92f);

            var btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold
            };

            // Top bar
            GUI.Box(new Rect(15, 15, 520, 360), "");
            GUI.Label(new Rect(30, 25, 490, 35), "GORILLA SMASH · HIGH STRIKER", headerStyle);
            GUI.Label(new Rect(30, 60, 490, 25), "Fase 1 · Minijuego Modular Monolith (PC Autoritativa)", normalStyle);

            // Player selector
            GUI.Label(new Rect(30, 90, 80, 26), "Jugador:", normalStyle);
            for (int i = 1; i <= 4; i++)
            {
                GUI.enabled = model.State == GorillaSmashState.Ready && player != i;
                if (GUI.Button(new Rect(100 + (i - 1) * 45, 90, 40, 26), "P" + i, btnStyle))
                {
                    SelectPlayer(i);
                }
            }
            GUI.enabled = true;

            // Input toggle
            GUI.Label(new Rect(30, 125, 120, 26), "Modo control:", normalStyle);
            if (GUI.Button(new Rect(150, 125, 140, 26), keyboard ? "[X] TECLADO" : "[ ] TECLADO", btnStyle))
            {
                SelectKeyboard(!keyboard);
            }
            GUI.enabled = !keyboard;
            if (GUI.Button(new Rect(300, 125, 140, 26), !keyboard ? "[X] TELÉFONO" : "[ ] TELÉFONO", btnStyle))
            {
                SelectKeyboard(false);
            }
            GUI.enabled = true;

            // Status notice
            string notice = keyboard
                ? "Teclado: Mantén ESPACIO para armar mazo, suelta para golpear (o flecha abajo para golpe rápido)."
                : detector.StatusMessage;

            var noticeStyle = new GUIStyle(normalStyle)
            {
                fontSize = 13,
                wordWrap = true
            };
            noticeStyle.normal.textColor = model.State == GorillaSmashState.Armed ? Color.yellow : Color.cyan;
            GUI.Label(new Rect(30, 160, 480, 40), notice, noticeStyle);

            // Attempt info
            GUI.Label(new Rect(30, 205, 300, 28),
                "Intento: " + Math.Min(rules.maxAttempts, model.CurrentAttemptNumber) + " de " + rules.maxAttempts,
                titleStyle);

            // Latest attempt score breakdown
            if (model.LatestAttempt != null)
            {
                var attempt = model.LatestAttempt;
                Color tierCol = settings != null ? settings.GetTierColor(attempt.tier) : Color.white;

                var tierStyle = new GUIStyle(headerStyle)
                {
                    fontSize = 20,
                    alignment = TextAnchor.MiddleLeft
                };
                tierStyle.normal.textColor = tierCol;

                GUI.Label(new Rect(30, 235, 480, 30),
                    "Puntos: " + attempt.score.ToString("N0") + "  —  " + GorillaSmashModel.GetTierDisplayName(attempt.tier) + (attempt.bellHit ? " 🔔 CAMPANA!" : ""),
                    tierStyle);

                string breakdown = string.Format(
                    "Potencia: {0:P0} | Dirección: {1:P0} | Sincronización: {2:P0} | Confianza: {3:P0}",
                    attempt.power, attempt.directionAccuracy, attempt.timing, attempt.gestureConfidence
                );
                GUI.Label(new Rect(30, 270, 480, 24), breakdown, normalStyle);
            }

            // Bottom control buttons
            if (model.State == GorillaSmashState.AttemptFinished)
            {
                if (GUI.Button(new Rect(30, 310, 230, 40), "SIGUIENTE INTENTO [ESPACIO]", btnStyle))
                {
                    NextAttempt();
                }
            }
            else if (model.State == GorillaSmashState.Completed)
            {
                GUI.Label(new Rect(30, 240, 480, 30), "¡TORNEO COMPLETADO! Mejor Score: " + model.BestScore.ToString("N0"), headerStyle);
                if (GUI.Button(new Rect(30, 310, 230, 40), "REINICIAR TORNEO [R]", btnStyle))
                {
                    RestartGame();
                }
            }

            if (GUI.Button(new Rect(300, 310, 180, 40), "VOLVER AL MENÚ [ESC]", btnStyle))
            {
                if (menu != null) menu.CloseGorillaSmash();
            }

            // Right sidebar: Attempts history
            GUI.Box(new Rect(Screen.width - 290, 15, 275, 220), "");
            GUI.Label(new Rect(Screen.width - 275, 25, 250, 28), "TABLA DE INTENTOS", titleStyle);

            for (int i = 0; i < rules.maxAttempts; i++)
            {
                float rowY = 60 + i * 40;
                if (i < model.Attempts.Count)
                {
                    var att = model.Attempts[i];
                    string attText = string.Format("#{0}: {1,5} pts  ({2})", att.attemptNumber, att.score, GorillaSmashModel.GetTierDisplayName(att.tier));
                    if (att.bellHit) attText += " 🔔";
                    GUI.Label(new Rect(Screen.width - 275, rowY, 250, 24), attText, normalStyle);
                }
                else
                {
                    GUI.Label(new Rect(Screen.width - 275, rowY, 250, 24), "#" + (i + 1) + ":  ---", normalStyle);
                }
            }

            GUI.Label(new Rect(Screen.width - 275, 185, 250, 26),
                "Mejor marca: " + model.BestScore.ToString("N0") + " pts",
                titleStyle);
        }

        void OnDestroy()
        {
            if (stage != null)
            {
                stage.Destroy();
                stage = null;
            }
        }
    }
}
