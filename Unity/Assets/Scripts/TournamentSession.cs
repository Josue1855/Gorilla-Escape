using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using GorillaEscape.Gameplay.Input;
using GorillaEscape.Contracts;

public static class TournamentSession
{
    public enum GameMode { Story, Party }
    public static GameMode CurrentMode { get; private set; } = GameMode.Story;

    public static bool IsActive { get; private set; }
    public static GameMode? RequestedMode { get; private set; }
    public static int RequestedPlayers { get; private set; } = 1;
    public static bool RequestedFreePlay { get; private set; }
    public static bool ConnectionPending => RequestedMode.HasValue;
    public static bool RequiresBothDevices => ConnectionPending && !RequestedFreePlay;

    public static void RequestFreePlay(int humanCount = 1) => RequestMode(GameMode.Party, humanCount, true);
    private static void RequestMode(GameMode mode, int humanCount, bool freePlay = false)
    {
        ResetTournament();
        RequestedMode = mode;
        RequestedPlayers = Mathf.Clamp(humanCount, 1, 4);
        RequestedFreePlay = freePlay;
        var session = MotionSession.Instance;
        if (session != null && session.CanReusePreparation(!freePlay, RequestedPlayers))
        {
            session.SetCameraOnly(false);
            CompleteModeRequest(session.Lobby);
            return;
        }
        SceneNavigator.Load(SceneNavigator.Connection);
    }
    public static string ConnectionRequirement(bool cameraReady, LobbySnapshotWire lobby)
    {
        if (!ConnectionPending) return null;
        int ready = lobby?.players == null ? 0 : lobby.players.Count(p => p != null &&
            (p.connectionState == "ACTIVE" || p.connectionState == "CONNECTED") && p.networkReady && p.inputReady && !string.IsNullOrWhiteSpace(p.displayName));
        if (RequestedFreePlay) return cameraReady || ready >= RequestedPlayers ? null : "Comprueba la cámara o conecta el celular y activa sus sensores antes de continuar.";
        if (!cameraReady) return "Enciende la cámara y comprueba que recibes imagen antes de comenzar.";
        return ready < RequestedPlayers ? $"Prepara {RequestedPlayers} celular(es): nombre, sensores activos y control abierto ({ready}/{RequestedPlayers})." : null;
    }
    public static bool BeginRequestedMode(bool cameraReady, LobbySnapshotWire lobby)
    {
        if (!ConnectionPending || ConnectionRequirement(cameraReady, lobby) != null) return false;
        MotionSession.Instance?.RememberPreparation();
        return CompleteModeRequest(lobby);
    }
    private static bool CompleteModeRequest(LobbySnapshotWire lobby)
    {
        var mode = RequestedMode.Value;
        int count = RequestedPlayers;
        bool free = RequestedFreePlay;
        string name = lobby?.players?.FirstOrDefault(p => p.playerId == 1)?.displayName;
        CancelRequestedMode();
        if (free) SceneNavigator.Load(SceneNavigator.MainMenu);
        else StartMode(mode, count, name);
        return true;
    }
    public static void CancelRequestedMode() { RequestedMode = null; RequestedPlayers = 1; RequestedFreePlay = false; }

    public static int CurrentRound { get; private set; } // 1..TotalRounds
    public static bool RoundCompleted { get; private set; }
    public static bool IsTurnTransition { get; private set; }

    public static int HumanPlayerCount { get; private set; } = 1;
    public static int CurrentTurnPlayerIndex { get; private set; } = 0; // 0..HumanPlayerCount-1

    public static bool IsStoryModeCleared => PlayerPrefs.GetInt("StoryModeCleared", 0) == 1;

    public static readonly string[] StoryScenes =
    {
        SceneNavigator.GorillaSmash,
        SceneNavigator.CoconutThrow,
        SceneNavigator.Bowling,
        SceneNavigator.CoconutSlice
    };

    public static readonly string[] StoryDisciplineNames =
    {
        "Gorilla Smash (Mazo Feria)",
        "Lanzamiento de Coco",
        "Coconut Bowling (Boliche)",
        "Jungle Slice (Corte Ninja)"
    };

    public static readonly string[] StoryUnits =
    {
        "pts",
        "m",
        "pts",
        "cortes"
    };

    public static readonly string[] PartyScenes =
    {
        SceneNavigator.GorillaSmash,
        SceneNavigator.CoconutThrow,
        SceneNavigator.Bowling,
        SceneNavigator.CoconutSlice,
        SceneNavigator.LogBalance,
        SceneNavigator.Boxing,
        SceneNavigator.Baseball,
        SceneNavigator.Drums,
        SceneNavigator.WhackAMole,
        SceneNavigator.JungleKeeper
    };

    public static readonly string[] PartyDisciplineNames =
    {
        "Gorilla Smash",
        "Lanzamiento de Coco",
        "Coconut Bowling",
        "Jungle Slice",
        "Desafío del Tronco",
        "Gorilla Boxing",
        "Jungle Baseball",
        "Jungle Drums",
        "Topo de la Selva",
        "Jungle Keeper"
    };

    public static readonly string[] PartyUnits =
    {
        "pts", "m", "pts", "cortes", "%", "pts", "carreras", "pts", "topos", "atajadas"
    };

    public static int TotalRounds => CurrentMode == GameMode.Story ? StoryScenes.Length : PartyScenes.Length;
    public static string[] CurrentScenes => CurrentMode == GameMode.Story ? StoryScenes : PartyScenes;
    public static string[] CurrentNames => CurrentMode == GameMode.Story ? StoryDisciplineNames : PartyDisciplineNames;
    public static string[] CurrentUnits => CurrentMode == GameMode.Story ? StoryUnits : PartyUnits;

    public enum PlayerParticipationState
    {
        Connected,
        Background,
        TemporarilyDisconnected,
        Reconnecting,
        Idle,
        Away,
        Left,
        Forfeited
    }

    public sealed class Competitor
    {
        public int Id;
        public string Name;
        public string Avatar;
        public bool IsHuman;
        public int LastRawScore;
        public int LastGainedGp;
        public int TotalGp;
        public PlayerParticipationState State = PlayerParticipationState.Connected;
        public bool IsForfeited => State == PlayerParticipationState.Forfeited || State == PlayerParticipationState.Left;
        public bool IsAway => State == PlayerParticipationState.Away;
        public bool IsBackground => State == PlayerParticipationState.Background;
        public bool IsDisconnected => State == PlayerParticipationState.TemporarilyDisconnected;
    }

    public struct DisciplineTimerConfig
    {
        public float TurnTransitionDuration;
        public float MaxGameplayDuration;
        public float InactivityTimeout;
        public float AwayOrBackgroundGrace;
        public float DisconnectGraceDuration;

        public DisciplineTimerConfig(float transition, float gameplay, float inactivity, float awayGrace, float disconnectGrace)
        {
            TurnTransitionDuration = transition;
            MaxGameplayDuration = gameplay;
            InactivityTimeout = inactivity;
            AwayOrBackgroundGrace = awayGrace;
            DisconnectGraceDuration = disconnectGrace;
        }
    }

    public static DisciplineTimerConfig GetTimerConfigForScene(string sceneName)
    {
        switch (sceneName)
        {
            case SceneNavigator.GorillaSmash:
                return new DisciplineTimerConfig(25f, 40f, 12f, 10f, 15f);
            case SceneNavigator.CoconutThrow:
                return new DisciplineTimerConfig(25f, 45f, 15f, 10f, 15f);
            case SceneNavigator.Bowling:
                return new DisciplineTimerConfig(25f, 60f, 15f, 10f, 15f);
            case SceneNavigator.CoconutSlice:
                return new DisciplineTimerConfig(25f, 45f, 12f, 10f, 15f);
            case SceneNavigator.LogBalance:
                return new DisciplineTimerConfig(25f, 50f, 15f, 10f, 15f);
            case SceneNavigator.Boxing:
                return new DisciplineTimerConfig(25f, 55f, 12f, 10f, 15f);
            case SceneNavigator.Baseball:
                return new DisciplineTimerConfig(25f, 55f, 15f, 10f, 15f);
            case SceneNavigator.Drums:
                return new DisciplineTimerConfig(25f, 60f, 15f, 10f, 15f);
            case SceneNavigator.WhackAMole:
                return new DisciplineTimerConfig(25f, 45f, 12f, 10f, 15f);
            case SceneNavigator.JungleKeeper:
                return new DisciplineTimerConfig(25f, 50f, 12f, 10f, 15f);
            default:
                return new DisciplineTimerConfig(25f, 45f, 15f, 10f, 15f);
        }
    }

    private static Competitor[] competitors;
    private static GorillaPointsLedger ledger;
    private static readonly HandDwellNavigator nav = new HandDwellNavigator();
    private static readonly HandDwellTarget[] targets = new HandDwellTarget[3];
    public static float TurnStartedAt { get; private set; }
    public static float TurnTimeoutDuration { get; set; } = 25.0f;

    public static void UpdatePlayerStates(GorillaEscape.Contracts.LobbyPlayerWire[] players)
    {
        if (competitors == null || players == null) return;
        foreach (var p in players)
        {
            var comp = competitors.FirstOrDefault(c => c.Id == p.playerId);
            if (comp != null && comp.IsHuman)
            {
                if (!string.IsNullOrWhiteSpace(p.displayName)) comp.Name = p.displayName;
                string s = p.connectionState ?? "";
                if (s.Equals("FORFEITED", StringComparison.OrdinalIgnoreCase))
                    comp.State = PlayerParticipationState.Forfeited;
                else if (s.Equals("AWAY", StringComparison.OrdinalIgnoreCase))
                    comp.State = PlayerParticipationState.Away;
                else if (s.Equals("BACKGROUND", StringComparison.OrdinalIgnoreCase))
                    comp.State = PlayerParticipationState.Background;
                else if (s.Equals("DISCONNECTED", StringComparison.OrdinalIgnoreCase))
                    comp.State = PlayerParticipationState.TemporarilyDisconnected;
                else
                    comp.State = PlayerParticipationState.Connected;
            }
        }
    }

    public static IReadOnlyList<Competitor> Competitors => competitors;
    public static string CurrentDisciplineName => (CurrentRound >= 1 && CurrentRound <= TotalRounds) ? CurrentNames[CurrentRound - 1] : "";
    public static string NextDisciplineName => (CurrentRound >= 1 && CurrentRound < TotalRounds) ? CurrentNames[CurrentRound] : "";
    public static int CurrentTurnPlayerId => (competitors != null && CurrentTurnPlayerIndex >= 0 && CurrentTurnPlayerIndex < competitors.Length) ? competitors[CurrentTurnPlayerIndex].Id : 1;
    public static string CurrentTurnPlayerName => (competitors != null && CurrentTurnPlayerIndex >= 0 && CurrentTurnPlayerIndex < competitors.Length) ? competitors[CurrentTurnPlayerIndex].Name : "Tú";

    public static void StartTournament(string humanName = null) => StartStoryMode(1, humanName);

    public static void StartStoryMode(int humanCount = 1, string humanName = null)
    {
        RequestMode(GameMode.Story, humanCount);
    }

    public static void StartPartyMode(int humanCount = 1, string humanName = null)
    {
        RequestMode(GameMode.Party, humanCount);
    }

    private static void StartMode(GameMode mode, int humanCount, string humanName)
    {
        IsActive = true;
        CurrentMode = mode;
        HumanPlayerCount = Mathf.Clamp(humanCount, 1, 4);
        CurrentRound = 1;
        CurrentTurnPlayerIndex = 0;
        TurnStartedAt = Time.unscaledTime;
        RoundCompleted = false;
        IsTurnTransition = false;
        ledger = new GorillaPointsLedger(new[] { 1, 2, 3, 4 });

        string p1 = !string.IsNullOrWhiteSpace(humanName) ? humanName.Trim() : (humanCount > 1 ? "Náufrago 1" : "Náufrago");
        var list = new List<Competitor>();
        string[] humanAvatars = { "👤", "🧭", "⚓", "🗺️" };

        for (int i = 0; i < HumanPlayerCount; i++)
        {
            string name = i == 0 ? p1 : $"Náufrago {i + 1}";
            list.Add(new Competitor
            {
                Id = i + 1,
                Name = name,
                Avatar = humanAvatars[i],
                IsHuman = true,
                LastRawScore = 0,
                LastGainedGp = 0,
                TotalGp = 0
            });
        }

        (string name, string avatar)[] gorillas =
        {
            ("Kong (Cacique)", "🦧"),
            ("Chimp (Veloz)", "🐒"),
            ("Babu (Fuerte)", "🦍")
        };

        int gorillaIdx = 0;
        for (int i = HumanPlayerCount; i < 4; i++)
        {
            var g = gorillas[gorillaIdx++];
            list.Add(new Competitor
            {
                Id = i + 1,
                Name = g.name,
                Avatar = g.avatar,
                IsHuman = false,
                LastRawScore = 0,
                LastGainedGp = 0,
                TotalGp = 0
            });
        }

        competitors = list.ToArray();
        SceneNavigator.Load(CurrentScenes[0]);
    }

    public static void RecordRoundResult(int humanScore)
    {
        if (!IsActive || RoundCompleted || ledger == null || competitors == null) return;

        int score = Mathf.Max(0, humanScore);
        competitors[CurrentTurnPlayerIndex].LastRawScore = score;

        // If there are more human players who haven't taken their turn in this round:
        if (CurrentTurnPlayerIndex < HumanPlayerCount - 1)
        {
            IsTurnTransition = true;
            TurnStartedAt = Time.unscaledTime;
            return;
        }

        // All human players finished their turn!
        IsTurnTransition = false;

        int humanSum = 0;
        for (int i = 0; i < HumanPlayerCount; i++) humanSum += competitors[i].LastRawScore;
        int humanAvg = Mathf.Max(1, humanSum / HumanPlayerCount);

        string currentScene = CurrentScenes[CurrentRound - 1];

        // Simulate AI gorillas for any remaining competitor slots:
        for (int i = HumanPlayerCount; i < 4; i++)
        {
            competitors[i].LastRawScore = GenerateAiScore(currentScene, i, humanAvg);
        }

        var ordered = competitors.OrderByDescending(c => c.LastRawScore).ThenBy(c => c.Id).Select(c => c.Id).ToList();
        string gameKey = $"round-{CurrentRound}-{currentScene}";
        ledger.RecordPlacements(gameKey, ordered);

        int[] gpTable = { 10, 7, 5, 3 };
        for (int i = 0; i < ordered.Count; i++)
        {
            int pId = ordered[i];
            int gained = gpTable[i];
            var comp = competitors.First(c => c.Id == pId);
            comp.LastGainedGp = gained;
        }

        var standings = ledger.Standings();
        foreach (var st in standings)
        {
            var comp = competitors.First(c => c.Id == st.PlayerId);
            comp.TotalGp = st.Points;
        }

        RoundCompleted = true;

        if (CurrentMode == GameMode.Story && CurrentRound >= TotalRounds)
        {
            var leaders = ledger.LeadingPlayers();
            bool humanWon = competitors.Any(c => c.IsHuman && leaders.Contains(c.Id));
            if (humanWon)
            {
                PlayerPrefs.SetInt("StoryModeCleared", 1);
                PlayerPrefs.Save();
            }
        }
    }

    private static int GenerateAiScore(string scene, int gorillaIndex, int humanAvg)
    {
        int variation = (gorillaIndex == 1) ? UnityEngine.Random.Range(-5, 10) :
                        (gorillaIndex == 2) ? UnityEngine.Random.Range(-15, 6) :
                                              UnityEngine.Random.Range(-25, 2);

        if (scene == SceneNavigator.GorillaSmash)
        {
            int baseScore = humanAvg > 100 ? humanAvg : 6500;
            return Mathf.Clamp(baseScore + UnityEngine.Random.Range(-1200, 800) + variation * 40, 2500, 9600);
        }
        if (scene == SceneNavigator.CoconutThrow)
        {
            int baseDist = humanAvg > 0 ? humanAvg : 20;
            return Mathf.Clamp(baseDist + UnityEngine.Random.Range(-4, 3) + (variation > 0 ? 1 : -1), 8, 34);
        }
        if (scene == SceneNavigator.Bowling)
        {
            int basePins = humanAvg > 10 ? humanAvg : 90;
            return Mathf.Clamp(basePins + UnityEngine.Random.Range(-25, 18) + variation * 2, 35, 180);
        }
        if (scene == SceneNavigator.CoconutSlice || scene == SceneNavigator.JungleKeeper)
        {
            int baseCuts = humanAvg > 5 ? humanAvg : 32;
            return Mathf.Clamp(baseCuts + UnityEngine.Random.Range(-8, 6) + variation, 12, 58);
        }
        if (scene == SceneNavigator.LogBalance)
        {
            int baseBal = humanAvg > 10 ? humanAvg : 75;
            return Mathf.Clamp(baseBal + UnityEngine.Random.Range(-15, 10) + variation, 30, 98);
        }
        if (scene == SceneNavigator.Boxing)
        {
            int baseBox = humanAvg > 100 ? humanAvg : 5000;
            return Mathf.Clamp(baseBox + UnityEngine.Random.Range(-1000, 700) + variation * 30, 2000, 8500);
        }
        if (scene == SceneNavigator.Baseball)
        {
            int baseRuns = humanAvg > 0 ? humanAvg : 4;
            return Mathf.Clamp(baseRuns + UnityEngine.Random.Range(-2, 2), 1, 9);
        }
        if (scene == SceneNavigator.Drums)
        {
            int baseDrums = humanAvg > 10 ? humanAvg : 70;
            return Mathf.Clamp(baseDrums + UnityEngine.Random.Range(-15, 10), 25, 110);
        }
        if (scene == SceneNavigator.WhackAMole)
        {
            int baseWhack = humanAvg > 5 ? humanAvg : 22;
            return Mathf.Clamp(baseWhack + UnityEngine.Random.Range(-5, 4), 8, 36);
        }
        return Mathf.Clamp(humanAvg + variation, 10, 100);
    }

    public static void NextRound()
    {
        if (!IsActive) return;
        if (CurrentRound < TotalRounds)
        {
            CurrentRound++;
            CurrentTurnPlayerIndex = 0;
            RoundCompleted = false;
            IsTurnTransition = false;
            SceneNavigator.Load(CurrentScenes[CurrentRound - 1]);
        }
        else
        {
            ResetTournament();
            SceneNavigator.Load(SceneNavigator.MainMenu);
        }
    }

    public static void ResetTournament()
    {
        CancelRequestedMode();
        IsActive = false;
        CurrentRound = 0;
        CurrentTurnPlayerIndex = 0;
        HumanPlayerCount = 1;
        RoundCompleted = false;
        IsTurnTransition = false;
        competitors = null;
        ledger = null;
        nav.Reset();
    }

    public static void DrawTournamentOverlay(float w, float h, Vector2 normCursor, string handLabel, bool handRaised, bool handClosed, bool forwardPush, bool phoneTouch)
    {
        if (!IsActive || competitors == null) return;

        // CASE 1: Turn-based transition card between human players
        if (IsTurnTransition)
        {
            DrawTurnTransitionCard(w, h, normCursor, handLabel, handRaised, handClosed, forwardPush, phoneTouch);
            return;
        }

        // CASE 2: Official Round Leaderboard
        if (!RoundCompleted || ledger == null) return;

        bool isFinal = CurrentRound >= TotalRounds;
        var standings = ledger.Standings();
        var leaders = ledger.LeadingPlayers();
        bool humanWon = competitors.Any(c => c.IsHuman && leaders.Contains(c.Id));

        float panelW = Mathf.Clamp(w * 0.78f, 620f, 980f);
        float panelH = Mathf.Clamp(h * 0.84f, 490f, 740f);
        Rect panel = new Rect((w - panelW) * 0.5f, (h - panelH) * 0.5f, panelW, panelH);

        JungleRetainedUI.depth = -100;
        JungleUITheme.DrawPanel(panel, false);

        string title;
        if (CurrentMode == GameMode.Story)
        {
            title = isFinal
                ? (humanWon ? "🏆 ¡VICTORIA! ¡HAS ESCAPADO DE LA ISLA! ⛵🏝️" : "🍌 ¡KONG Y LOS GORILAS HAN GANADO! 🦍")
                : $"🏝️ MODO HISTORIA · PRUEBA {CurrentRound} DE {TotalRounds} (MVP)";
        }
        else
        {
            title = isFinal
                ? "🏆 ¡GRAN FINAL DEL MODO FIESTA! 🎉"
                : $"🎉 MODO FIESTA · RONDA {CurrentRound} DE {TotalRounds}";
        }

        GUIStyle titleStyle = new GUIStyle(JungleUITheme.Title)
        {
            fontSize = Mathf.Clamp(Mathf.RoundToInt(panelH * 0.052f), 17, 26),
            alignment = TextAnchor.MiddleCenter
        };
        JungleUITheme.DrawShadowedLabel(new Rect(panel.x + 20f, panel.y + 14f, panel.width - 40f, panelH * 0.08f), title, titleStyle, 2.5f);

        string sub;
        if (isFinal)
        {
            if (CurrentMode == GameMode.Story)
            {
                sub = humanWon
                    ? "¡Los gorilas te construyeron una gran balsa de bambú! ¡Modo Fiesta Desbloqueado! 👑"
                    : "No lograste la balsa de la libertad. ¡Vuelve a intentarlo para escapar de la isla!";
            }
            else
            {
                sub = humanWon ? "¡Felicidades! Eres el Rey indiscutible de la Selva 👑" : "¡Gran fiesta en la selva! Gracias por jugar 🌴";
            }
        }
        else
        {
            sub = $"TABLA DE PUNTOS ACUMULADA · SIGUIENTE PRUEBA: {NextDisciplineName.ToUpperInvariant()}";
        }

        GUIStyle subStyle = new GUIStyle(JungleUITheme.Heading)
        {
            fontSize = Mathf.Clamp(Mathf.RoundToInt(panelH * 0.032f), 12, 16),
            alignment = TextAnchor.MiddleCenter
        };
        JungleUITheme.DrawShadowedLabel(new Rect(panel.x + 20f, panel.y + panelH * 0.088f, panel.width - 40f, panelH * 0.05f), sub, subStyle, 1.8f);

        // Competitor rows
        float rowStartY = panel.y + panelH * 0.165f;
        float rowHeight = panelH * 0.12f;
        float rowGap = panelH * 0.02f;

        for (int i = 0; i < standings.Length; i++)
        {
            var st = standings[i];
            var comp = competitors.First(c => c.Id == st.PlayerId);
            Rect rowRect = new Rect(panel.x + panelW * 0.05f, rowStartY + i * (rowHeight + rowGap), panelW * 0.90f, rowHeight);

            JungleUITheme.DrawPanel(rowRect, comp.IsHuman);

            string medal = i == 0 ? "🥇" : i == 1 ? "🥈" : i == 2 ? "🥉" : "  4º";
            string roleTag = comp.IsHuman ? " (NÁUFRAGO)" : " (GORILA)";
            string nameTag = $"{medal} {comp.Avatar} {comp.Name}{roleTag}";
            string unit = CurrentUnits[CurrentRound - 1];
            string stats = $"Ronda: {comp.LastRawScore} {unit} (+{comp.LastGainedGp} GP)  |  TOTAL: {comp.TotalGp} GP";

            GUIStyle nameStyle = new GUIStyle(JungleUITheme.Body)
            {
                fontSize = Mathf.Clamp(Mathf.RoundToInt(rowHeight * 0.38f), 13, 21),
                alignment = TextAnchor.MiddleLeft
            };
            if (comp.IsHuman) nameStyle.normal.textColor = Color.yellow;
            JungleUITheme.DrawShadowedLabel(new Rect(rowRect.x + 18f, rowRect.y + 6f, rowRect.width * 0.46f, rowHeight - 12f), nameTag, nameStyle, 1.8f);

            GUIStyle statStyle = new GUIStyle(JungleUITheme.HudScore)
            {
                fontSize = Mathf.Clamp(Mathf.RoundToInt(rowHeight * 0.35f), 12, 19),
                alignment = TextAnchor.MiddleRight
            };
            JungleUITheme.DrawShadowedLabel(new Rect(rowRect.xMax - rowRect.width * 0.52f - 18f, rowRect.y + 6f, rowRect.width * 0.52f, rowHeight - 12f), stats, statStyle, 1.8f);
        }

        // Action Buttons
        float btnY = panel.yMax - panelH * 0.16f;
        float btnH = panelH * 0.11f;
        Rect continueBtn = new Rect(panel.x + panelW * 0.05f, btnY, panelW * 0.60f, btnH);
        Rect exitBtn = new Rect(panel.x + panelW * 0.68f, btnY, panelW * 0.27f, btnH);

        targets[0] = new HandDwellTarget(10, continueBtn.x, continueBtn.y, continueBtn.width, continueBtn.height);
        targets[1] = new HandDwellTarget(11, exitBtn.x, exitBtn.y, exitBtn.width, exitBtn.height);

        Vector2 cursorPx = new Vector2(normCursor.x * w, normCursor.y * h);
        bool actionTrigger = forwardPush || phoneTouch;
        bool isCharging = handClosed || phoneTouch;
        bool dwellFired = nav.ObserveCursor(true, cursorPx.x, cursorPx.y, Time.unscaledTime, targets, out int hitId, 1.6f, actionTrigger, isCharging) && hitId > 0;
        int hoverId = nav.HoveredId;

        string continueLabel = isFinal ? "🏆 VOLVER AL MENÚ PRINCIPAL" : $"CONTINUAR A: {NextDisciplineName.ToUpperInvariant()} ➔";
        if (JungleRetainedUI.Button(continueBtn, continueLabel, hoverId == 10 ? JungleUITheme.ButtonSelected : JungleUITheme.Button) || (dwellFired && hoverId == 10))
        {
            JungleAudioBank.PlayUiClick(0.6f);
            nav.Reset();
            NextRound();
            return;
        }
        if (hoverId == 10 && nav.Progress > 0.02f)
            JungleUITheme.DrawDwellOverlay(continueBtn, nav.Progress, handClosed ? $"MANTÉN EL PUÑO · {Mathf.RoundToInt(nav.Progress * 100)}%" : "CIERRA EL PUÑO");

        if (JungleRetainedUI.Button(exitBtn, "SALIR DEL TORNEO", hoverId == 11 ? JungleUITheme.ButtonSelected : JungleUITheme.Button) || (dwellFired && hoverId == 11))
        {
            JungleAudioBank.PlayUiClick(0.5f);
            ResetTournament();
            SceneNavigator.Load(SceneNavigator.MainMenu);
            return;
        }
        if (hoverId == 11 && nav.Progress > 0.02f)
            JungleUITheme.DrawDwellOverlay(exitBtn, nav.Progress);

        JungleUITheme.DrawNavigatorHandCursor(nav, handLabel, handRaised, handClosed);
    }

    private static void DrawTurnTransitionCard(float w, float h, Vector2 normCursor, string handLabel, bool handRaised, bool handClosed, bool forwardPush, bool phoneTouch)
    {
        float cW = Mathf.Clamp(w * 0.65f, 520f, 820f);
        float cH = Mathf.Clamp(h * 0.60f, 380f, 520f);
        Rect card = new Rect((w - cW) * 0.5f, (h - cH) * 0.5f, cW, cH);

        JungleRetainedUI.depth = -120;
        JungleUITheme.DrawPanel(card, true);

        var currentComp = competitors[CurrentTurnPlayerIndex];
        var nextComp = competitors[CurrentTurnPlayerIndex + 1];

        GUIStyle headStyle = new GUIStyle(JungleUITheme.Title) { fontSize = Mathf.Clamp(Mathf.RoundToInt(cH * 0.075f), 20, 28), alignment = TextAnchor.MiddleCenter };
        JungleUITheme.DrawShadowedLabel(new Rect(card.x + 20, card.y + 20, card.width - 40, cH * 0.12f), "¡TURNO FINALIZADO!", headStyle, 2.5f);

        GUIStyle compStyle = new GUIStyle(JungleUITheme.Heading) { fontSize = Mathf.Clamp(Mathf.RoundToInt(cH * 0.048f), 14, 20), alignment = TextAnchor.MiddleCenter };
        string scoreInfo = $"{currentComp.Avatar} {currentComp.Name} registró: {currentComp.LastRawScore} {CurrentUnits[CurrentRound - 1]}";
        JungleUITheme.DrawShadowedLabel(new Rect(card.x + 20, card.y + cH * 0.18f, card.width - 40, cH * 0.10f), scoreInfo, compStyle, 1.8f);

        GUIStyle nextStyle = new GUIStyle(JungleUITheme.Title) { fontSize = Mathf.Clamp(Mathf.RoundToInt(cH * 0.065f), 18, 24), alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.yellow } };
        string nextText = $"👉 ¡SIGUIENTE TURNO: {nextComp.Avatar} {nextComp.Name.ToUpperInvariant()}!";
        JungleUITheme.DrawShadowedLabel(new Rect(card.x + 20, card.y + cH * 0.35f, card.width - 40, cH * 0.12f), nextText, nextStyle, 2.2f);

        GUIStyle tipStyle = new GUIStyle(JungleUITheme.Body) { fontSize = Mathf.Clamp(Mathf.RoundToInt(cH * 0.038f), 12, 16), alignment = TextAnchor.MiddleCenter, wordWrap = true };
        JungleUITheme.DrawShadowedLabel(new Rect(card.x + 30, card.y + cH * 0.50f, card.width - 60, cH * 0.12f), "Pásale el control del teléfono o prepárate para colocarte frente a la cámara.", tipStyle, 1.5f);

        Rect startNextBtn = new Rect(card.x + card.width * 0.12f, card.yMax - cH * 0.22f, card.width * 0.76f, cH * 0.15f);
        targets[2] = new HandDwellTarget(20, startNextBtn.x, startNextBtn.y, startNextBtn.width, startNextBtn.height);

        Vector2 cursorPx = new Vector2(normCursor.x * w, normCursor.y * h);
        bool actionTrigger = forwardPush || phoneTouch;
        bool isCharging = handClosed || phoneTouch;
        bool dwellFired = nav.ObserveCursor(true, cursorPx.x, cursorPx.y, Time.unscaledTime, targets, out int hitId, 1.6f, actionTrigger, isCharging) && hitId == 20;

        if (nextComp.IsForfeited)
        {
            CurrentTurnPlayerIndex++;
            TurnStartedAt = Time.unscaledTime;
            if (CurrentTurnPlayerIndex < HumanPlayerCount - 1)
            {
                SceneNavigator.Load(CurrentScenes[CurrentRound - 1]);
            }
            else
            {
                RecordRoundResult(0);
            }
            return;
        }

        string currentScene = (CurrentRound >= 1 && CurrentRound <= TotalRounds) ? CurrentScenes[CurrentRound - 1] : "";
        var timerConfig = GetTimerConfigForScene(currentScene);
        float timeout = nextComp.IsForfeited ? 0f : (nextComp.IsAway || nextComp.IsBackground) ? timerConfig.AwayOrBackgroundGrace : nextComp.IsDisconnected ? timerConfig.DisconnectGraceDuration : timerConfig.TurnTransitionDuration;
        float elapsed = Time.unscaledTime - TurnStartedAt;
        float remaining = Mathf.Max(0, timeout - elapsed);
        string countdownNote = remaining < 10f ? $" (Auto en {Mathf.CeilToInt(remaining)}s)" : "";
        string stateWarning = nextComp.IsAway ? " ⏸️ (Ausente)" : nextComp.IsBackground ? " 📱 (Segundo plano)" : nextComp.IsDisconnected ? " ⚠️ (Desconectado)" : "";
        string btnText = $"▶️ COMENZAR TURNO: {nextComp.Name.ToUpperInvariant()}{stateWarning}{countdownNote}";
        if (JungleRetainedUI.Button(startNextBtn, btnText, JungleUITheme.ButtonSelected) || dwellFired || (remaining <= 0f && elapsed > 2f))
        {
            JungleAudioBank.PlayUiClick(0.6f);
            nav.Reset();
            CurrentTurnPlayerIndex++;
            TurnStartedAt = Time.unscaledTime;
            IsTurnTransition = false;
            SceneNavigator.Load(CurrentScenes[CurrentRound - 1]);
            return;
        }

        if (nav.HoveredId == 20 && nav.Progress > 0.02f)
            JungleUITheme.DrawDwellOverlay(startNextBtn, nav.Progress, "COMENZANDO TURNO...");

        JungleUITheme.DrawNavigatorHandCursor(nav, handLabel, handRaised, handClosed);
    }
}
