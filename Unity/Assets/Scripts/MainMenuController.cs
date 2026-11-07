using GorillaEscape.Gameplay.Input;
using UnityEngine;

[RequireComponent(typeof(JunglePanel))]
public sealed class MainMenuController : MonoBehaviour, IJungleScreen
{
    private static readonly string[] CameraOnlyNames = { "JUNGLE\nSLICE", "JUNGLE\nKEEPER", "GORILLA\nBOXING", "JUNGLE\nBASEBALL", "JUNGLE\nDRUMS" };
    private static readonly string[] CameraOnlyTags = { "CÁMARA · 2 MANOS", "CÁMARA · MANOS Y TORSO", "CÁMARA · 2 PUÑOS", "CÁMARA · BATEO", "CÁMARA · RITMO" };

    private static readonly string[] FullNames =
    {
        "GORILLA\nSMASH", "COCONUT\nTHROW", "COCONUT\nBOWLING", "JUNGLE\nSLICE", "JUNGLE\nBALANCE",
        "GORILLA\nBOXING", "JUNGLE\nBASEBALL", "JUNGLE\nDRUMS", "GORILLA\nWHACK", "JUNGLE\nKEEPER"
    };

    private static readonly string[] FullTags =
    {
        "★ MVP · MAZO FERIA", "★ MVP · LANZAMIENTO", "★ MVP · BOLICHE", "★ MVP · CORTE NINJA", "SOLO CELULAR",
        "CÁMARA · 2 PUÑOS", "CÁMARA · BATEO", "CÁMARA · RITMO", "CELULAR + ALTURA", "CÁMARA · ARQUERO"
    };

    private static readonly string[] CameraOnlyScenes = { SceneNavigator.CoconutSlice, SceneNavigator.JungleKeeper, SceneNavigator.Boxing, SceneNavigator.Baseball, SceneNavigator.Drums };
    private static readonly string[] FullScenes =
    {
        SceneNavigator.GorillaSmash, SceneNavigator.CoconutThrow, SceneNavigator.Bowling, SceneNavigator.CoconutSlice, SceneNavigator.LogBalance,
        SceneNavigator.Boxing, SceneNavigator.Baseball, SceneNavigator.Drums, SceneNavigator.WhackAMole, SceneNavigator.JungleKeeper
    };

    private MotionSession session;
    private readonly HandDwellNavigator handNavigator = new HandDwellNavigator();
    private readonly Rect[] cardRects = new Rect[10];
    private readonly HandDwellTarget[] cameraOnlyTargets = new HandDwellTarget[9];
    private readonly HandDwellTarget[] fullTargets = new HandDwellTarget[16];
    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle cardTitleStyle;
    private GUIStyle cardTitleSelectedStyle;
    private int lastWidth, lastHeight;
    private int cachedDwellPct = -1;
    private string cachedDwellLabel = "";
    private float sceneReadyAt;
    private bool showPartyModal = false;
    private bool showStoryIntroModal = false;
    private int selectedPlayerCount = 1;

    private void Awake()
    {
        sceneReadyAt = Time.unscaledTime;
        session = MotionSession.Ensure();
        session.Connect();
        if (session.PhoneConnected) session.SetCameraOnly(false);
        JungleAudioBank.Attach2DSource(gameObject);
        string autoNav = System.Environment.GetEnvironmentVariable("GORILLA_AUTO_NAV");
        if (autoNav == "PARTY") showPartyModal = true;
        else if (autoNav == "STORY") showStoryIntroModal = true;
    }

    private bool autoMenuShotTaken;
    private void Update()
    {
        if (!autoMenuShotTaken && Time.unscaledTime >= sceneReadyAt + 1.8f)
        {
            autoMenuShotTaken = true;
            string autoShot = System.Environment.GetEnvironmentVariable("GORILLA_AUTO_SCREENSHOT_MENU");
            if (!string.IsNullOrEmpty(autoShot))
            {
                ScreenCapture.CaptureScreenshot(autoShot);
                Debug.Log("CAPTURED_AUTO_SCREENSHOT_MENU: " + autoShot);
            }
        }
        if (System.Environment.GetEnvironmentVariable("GORILLA_AUTO_QUIT") == "1" && Time.unscaledTime >= sceneReadyAt + 4.5f)
        {
            Application.Quit(0);
        }
    }

    private void EnsureMenuStyles(float w, float h, float cardWidth)
    {
        int widthInt = Mathf.RoundToInt(w), heightInt = Mathf.RoundToInt(h);
        if (titleStyle != null && lastWidth == widthInt && lastHeight == heightInt) return;
        lastWidth = widthInt;
        lastHeight = heightInt;
        titleStyle = new GUIStyle(JungleUITheme.Title)
        {
            fontSize = Mathf.RoundToInt(h * .078f),
            normal = { textColor = new Color(1f, .84f, .16f) }
        };
        subtitleStyle = new GUIStyle(JungleUITheme.Body)
        {
            fontSize = Mathf.RoundToInt(h * .025f),
            normal = { textColor = new Color(1f, .95f, .80f) }
        };
        int cardFont = Mathf.RoundToInt(Mathf.Min(h * .036f, cardWidth * .102f));
        cardTitleStyle = new GUIStyle(JungleUITheme.Title)
        {
            fontSize = cardFont,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            normal = { textColor = new Color(1f, .96f, .86f) }
        };
        cardTitleSelectedStyle = new GUIStyle(cardTitleStyle)
        {
            fontSize = Mathf.RoundToInt(cardFont * 1.05f),
            normal = { textColor = new Color(1f, .86f, .18f) }
        };
    }

    public void RenderUI()
    {
        JungleUITheme.Ensure();
        JungleUITheme.DrawBackdrop(true);
        float w = Screen.width, h = Screen.height;
        const int columns = 5;
        float gap = w * .015f, cardWidth = (w * .92f - gap * (columns - 1)) / columns;
        EnsureMenuStyles(w, h, cardWidth);

        Rect headerPanel = new Rect(w * .20f, h * .035f, w * .60f, h * .155f);
        JungleUITheme.DrawPanel(headerPanel);
        JungleUITheme.DrawShadowedLabel(new Rect(headerPanel.x, h * .042f, headerPanel.width, h * .095f), "GORILLA ESCAPE", titleStyle, 3.5f);

        string[] names = session.CameraOnly ? CameraOnlyNames : FullNames;
        string[] tags = session.CameraOnly ? CameraOnlyTags : FullTags;
        string[] scenes = session.CameraOnly ? CameraOnlyScenes : FullScenes;

        bool hasHand = session.TryCameraCursor(out Vector2 normCursor, out string handLabel, out bool handRaised, out bool handClosed, out bool forwardPush);
        string subtitle = session.CameraOnly
            ? (hasHand ? "MODO SOLO CÁMARA · MANTÉN TU MANO SOBRE UN DESAFÍO" : "MODO SOLO CÁMARA · MUESTRA TU MANO FRENTE A LA CÁMARA")
            : (hasHand ? "ELIGE TU DESAFÍO · MANTÉN TU MANO SOBRE UNA TARJETA" : "ELIGE TU DESAFÍO EN LA SELVA");
        JungleUITheme.DrawShadowedLabel(new Rect(headerPanel.x + 16f, h * .132f, headerPanel.width - 32f, h * .048f), subtitle, subtitleStyle, 2f);

        for (int i = 0; i < names.Length; i++)
        {
            int row = i / columns, column = i % columns;
            int count = Mathf.Min(columns, names.Length - row * columns);
            float rowWidth = count * cardWidth + (count - 1) * gap;
            cardRects[i] = new Rect((w - rowWidth) / 2f + column * (cardWidth + gap), h * (.225f + row * .265f), cardWidth, h * .225f);
        }

        Rect primaryModeRect = new Rect(w * .15f, h * .725f, w * .34f, h * .065f);
        Rect cameraModeRect = new Rect(w * .51f, h * .725f, w * .34f, h * .065f);
        Rect tournamentBtnRect = new Rect(w * .15f, h * .805f, w * .34f, h * .068f);
        Rect partyBtnRect = new Rect(w * .51f, h * .805f, w * .34f, h * .068f);

        HandDwellTarget[] targets = session.CameraOnly ? cameraOnlyTargets : fullTargets;
        for (int i = 0; i < names.Length; i++)
            targets[i] = new HandDwellTarget(i, cardRects[i].x, cardRects[i].y, cardRects[i].width, cardRects[i].height);
        targets[names.Length] = new HandDwellTarget(100, primaryModeRect.x, primaryModeRect.y, primaryModeRect.width, primaryModeRect.height);
        targets[names.Length + 1] = new HandDwellTarget(101, cameraModeRect.x, cameraModeRect.y, cameraModeRect.width, cameraModeRect.height);
        targets[names.Length + 2] = new HandDwellTarget(102, tournamentBtnRect.x, tournamentBtnRect.y, tournamentBtnRect.width, tournamentBtnRect.height);
        targets[names.Length + 3] = new HandDwellTarget(103, partyBtnRect.x, partyBtnRect.y, partyBtnRect.width, partyBtnRect.height);

        Vector2 cursorPx = new Vector2(normCursor.x * w, normCursor.y * h);
        bool modalOpen = showStoryIntroModal || showPartyModal;
        bool canInteract = !modalOpen && hasHand && (Time.unscaledTime >= sceneReadyAt + 1.2f);
        bool isCharging = session.CameraOnly ? (handClosed || handRaised) : (handClosed || session.PhoneTouchPressed);
        bool actionTrigger = forwardPush || session.PhoneTouchTriggered;
        bool dwellFired = handNavigator.ObserveCursor(canInteract, cursorPx.x, cursorPx.y, Time.unscaledTime, targets, out int triggeredId, 1.6f, actionTrigger, isCharging)
            && triggeredId >= 0;
        int hoveredId = handNavigator.HoveredId;
        float dwellProgress = handNavigator.Progress;
        JungleAudioBank.PlayDwellProgress(dwellProgress);

        for (int i = 0; i < names.Length; i++)
        {
            Rect card = cardRects[i];
            bool isHovered = hoveredId == i;
            JungleUITheme.DrawPanel(card, isHovered);

            Rect innerBtn = new Rect(card.x + 10f, card.y + 10f, card.width - 20f, card.height - 20f);
            if (JungleRetainedUI.Button(innerBtn, GUIContent.none, GUIStyle.none) || (dwellFired && hoveredId == i))
            {
                JungleAudioBank.PlayUiClick();
                handNavigator.Reset();
                if (scenes[i] == SceneNavigator.CoconutSlice || scenes[i] == SceneNavigator.JungleKeeper)
                    session.SetCameraOnly(true);
                else
                    session.SetCameraOnly(false);
                SceneNavigator.Load(scenes[i]);
                return;
            }

            Rect titleRect = new Rect(card.x + 14f, card.y + card.height * .10f, card.width - 28f, card.height * .54f);
            JungleUITheme.DrawShadowedLabel(titleRect, names[i], isHovered ? cardTitleSelectedStyle : cardTitleStyle, 2.5f);

            Rect badgeRect = new Rect(card.x + card.width * .09f, card.yMax - card.height * .30f, card.width * .82f, card.height * .20f);
            JungleUITheme.DrawBadge(badgeRect, tags[i], isHovered);

            if (isHovered && dwellProgress > .02f)
            {
                int pct = Mathf.RoundToInt(dwellProgress * 100f);
                if (pct != cachedDwellPct) { cachedDwellPct = pct; cachedDwellLabel = handClosed ? $"MANTÉN EL PUÑO · {pct}%" : $"CIERRA EL PUÑO · {pct}%"; }
                JungleUITheme.DrawDwellOverlay(card, dwellProgress, cachedDwellLabel);
            }
        }

        bool primaryHovered = hoveredId == 100;
        if (JungleRetainedUI.Button(primaryModeRect, session.CameraOnly ? "JUEGOS CON CELULAR" : "CONEXIÓN Y CALIBRACIÓN", primaryHovered ? JungleUITheme.ButtonSelected : JungleUITheme.Button)
            || (dwellFired && hoveredId == 100))
        {
            JungleAudioBank.PlayUiClick();
            handNavigator.Reset();
            session.SetCameraOnly(false);
            SceneNavigator.Load(SceneNavigator.Connection);
            return;
        }
        if (primaryHovered && dwellProgress > .02f)
            JungleUITheme.DrawDwellOverlay(primaryModeRect, dwellProgress);

        bool cameraHovered = hoveredId == 101;
        if (JungleRetainedUI.Button(cameraModeRect, session.CameraOnly ? "MODO CÁMARA ACTIVO" : "JUEGOS SIN CELULAR", cameraHovered ? JungleUITheme.ButtonSelected : JungleUITheme.Button)
            || (dwellFired && hoveredId == 101))
        {
            JungleAudioBank.PlayUiClick();
            handNavigator.Reset();
            session.SetCameraOnly(!session.CameraOnly);
            return;
        }
        if (cameraHovered && dwellProgress > .02f)
            JungleUITheme.DrawDwellOverlay(cameraModeRect, dwellProgress);

        bool tournamentHovered = hoveredId == 102;
        if (JungleRetainedUI.Button(tournamentBtnRect, " MODO HISTORIA: ESCAPA DE LA ISLA", tournamentHovered ? JungleUITheme.ButtonSelected : JungleUITheme.Button)
            || (dwellFired && hoveredId == 102))
        {
            JungleAudioBank.PlayUiClick(0.6f);
            handNavigator.Reset();
            showStoryIntroModal = true;
            return;
        }
        if (tournamentHovered && dwellProgress > .02f)
            JungleUITheme.DrawDwellOverlay(tournamentBtnRect, dwellProgress, "MODO HISTORIA");

        bool partyHovered = hoveredId == 103;
        string partyBtnLabel = TournamentSession.IsStoryModeCleared ? " MODO FIESTA · DESBLOQUEADO " : " MODO FIESTA · ACCESO LIBRE ";
        if (JungleRetainedUI.Button(partyBtnRect, partyBtnLabel, partyHovered ? JungleUITheme.ButtonSelected : JungleUITheme.Button)
            || (dwellFired && hoveredId == 103))
        {
            JungleAudioBank.PlayUiClick(0.5f);
            handNavigator.Reset();
            showPartyModal = true;
            return;
        }
        if (partyHovered && dwellProgress > .02f)
            JungleUITheme.DrawDwellOverlay(partyBtnRect, dwellProgress, "MODO FIESTA");

        if (showStoryIntroModal)
        {
            float mW = Mathf.Clamp(w * 0.72f, 540f, 860f);
            float mH = Mathf.Clamp(h * 0.75f, 440f, 620f);
            Rect mRect = new Rect((w - mW) * 0.5f, (h - mH) * 0.5f, mW, mH);
            JungleRetainedUI.depth = -130;
            JungleUITheme.DrawPanel(mRect, true);

            GUIStyle mTitle = new GUIStyle(JungleUITheme.Title) { fontSize = Mathf.Clamp(Mathf.RoundToInt(mH * 0.065f), 18, 25), alignment = TextAnchor.MiddleCenter };
            JungleUITheme.DrawShadowedLabel(new Rect(mRect.x + 20f, mRect.y + 14f, mRect.width - 40f, mH * 0.10f), " MODO HISTORIA · ESCAPA DE LA ISLA", mTitle, 2.5f);

            GUIStyle mBody = new GUIStyle(JungleUITheme.Body) { fontSize = Mathf.Clamp(Mathf.RoundToInt(mH * 0.036f), 12, 16), alignment = TextAnchor.MiddleCenter, wordWrap = true };
            string desc = "Tras una tormenta en altamar tu barco naufragó en una isla remota habitada por gorilas pseudointeligentes. Su cacique Kong te reta a sus sagradas 'Gorilimpiadas': si logras vencerlos en las 4 pruebas del MVP (Mazo, Lanzamiento, Boliche y Corte), ¡te construirán una balsa de bambú y podrás escapar en libertad!\n\n¿Cuántos náufragos compiten por turnos?";
            JungleUITheme.DrawShadowedLabel(new Rect(mRect.x + 24f, mRect.y + mH * 0.11f, mRect.width - 48f, mH * 0.38f), desc, mBody, 1.8f);

            // Selector de jugadores humanos (1 a 4)
            float selY = mRect.y + mH * 0.50f;
            float btnW = (mRect.width - 60f) / 4f;
            for (int p = 1; p <= 4; p++)
            {
                Rect pBtn = new Rect(mRect.x + 24f + (p - 1) * (btnW + 4f), selY, btnW, mH * 0.10f);
                bool isSel = selectedPlayerCount == p;
                string pLabel = p == 1 ? "1 NÁUFRAGO" : $"{p} JUGADORES";
                if (JungleRetainedUI.Button(pBtn, pLabel, isSel ? JungleUITheme.ButtonSelected : JungleUITheme.Button))
                {
                    JungleAudioBank.PlayUiClick();
                    selectedPlayerCount = p;
                }
            }

            // Botón Comenzar
            float actY = mRect.yMax - mH * 0.28f;
            Rect startStoryBtn = new Rect(mRect.x + mRect.width * 0.12f, actY, mRect.width * 0.76f, mH * 0.12f);
            if (JungleRetainedUI.Button(startStoryBtn, " ¡ACEPTAR DESAFÍO Y COMENZAR! ", JungleUITheme.ButtonSelected))
            {
                JungleAudioBank.PlayUiClick(0.6f);
                showStoryIntroModal = false;
                handNavigator.Reset();
                TournamentSession.StartStoryMode(selectedPlayerCount, session != null ? session.PlayerName : null);
                return;
            }

            // Botón Cerrar
            Rect closeBtn = new Rect(mRect.x + mRect.width * 0.25f, mRect.yMax - mH * 0.13f, mRect.width * 0.50f, mH * 0.09f);
            if (JungleRetainedUI.Button(closeBtn, "VOLVER AL MENÚ", JungleUITheme.Button))
            {
                JungleAudioBank.PlayUiClick();
                showStoryIntroModal = false;
                handNavigator.Reset();
                return;
            }
        }

        if (showPartyModal)
        {
            float mW = Mathf.Clamp(w * 0.72f, 540f, 860f);
            float mH = Mathf.Clamp(h * 0.75f, 440f, 620f);
            Rect mRect = new Rect((w - mW) * 0.5f, (h - mH) * 0.5f, mW, mH);
            JungleRetainedUI.depth = -130;
            JungleUITheme.DrawPanel(mRect, true);

            GUIStyle mTitle = new GUIStyle(JungleUITheme.Title) { fontSize = Mathf.Clamp(Mathf.RoundToInt(mH * 0.065f), 18, 25), alignment = TextAnchor.MiddleCenter };
            JungleUITheme.DrawShadowedLabel(new Rect(mRect.x + 20f, mRect.y + 14f, mRect.width - 40f, mH * 0.10f), " MODO FIESTA · JUNGLE PARTY", mTitle, 2.5f);

            GUIStyle mBody = new GUIStyle(JungleUITheme.Body) { fontSize = Mathf.Clamp(Mathf.RoundToInt(mH * 0.036f), 12, 16), alignment = TextAnchor.MiddleCenter, wordWrap = true };
            string desc = "¡Celebración en la Selva! Todos los 10 minijuegos están completamente disponibles sin restricciones. Selecciona cuántos jugadores compiten por turnos:";
            JungleUITheme.DrawShadowedLabel(new Rect(mRect.x + 24f, mRect.y + mH * 0.12f, mRect.width - 48f, mH * 0.24f), desc, mBody, 1.8f);

            float selY = mRect.y + mH * 0.38f;
            float btnW = (mRect.width - 60f) / 4f;
            for (int p = 1; p <= 4; p++)
            {
                Rect pBtn = new Rect(mRect.x + 24f + (p - 1) * (btnW + 4f), selY, btnW, mH * 0.09f);
                bool isSel = selectedPlayerCount == p;
                string pLabel = p == 1 ? "1 JUGADOR" : $"{p} JUGADORES";
                if (JungleRetainedUI.Button(pBtn, pLabel, isSel ? JungleUITheme.ButtonSelected : JungleUITheme.Button))
                {
                    JungleAudioBank.PlayUiClick();
                    selectedPlayerCount = p;
                }
            }

            Rect gridPlayBtn = new Rect(mRect.x + mRect.width * 0.10f, mRect.y + mH * 0.52f, mRect.width * 0.80f, mH * 0.11f);
            if (JungleRetainedUI.Button(gridPlayBtn, " ELEGIR MINIJUEGO LIBREMENTE (EN LA CUADRÍCULA)", JungleUITheme.ButtonSelected))
            {
                JungleAudioBank.PlayUiClick();
                showPartyModal = false;
                handNavigator.Reset();
                TournamentSession.RequestFreePlay(selectedPlayerCount);
                return;
            }

            Rect fullTourneyBtn = new Rect(mRect.x + mRect.width * 0.10f, mRect.y + mH * 0.66f, mRect.width * 0.80f, mH * 0.11f);
            if (JungleRetainedUI.Button(fullTourneyBtn, " INICIAR MARATÓN FIESTA (10 JUEGOS)", JungleUITheme.Button))
            {
                JungleAudioBank.PlayUiClick(0.6f);
                showPartyModal = false;
                handNavigator.Reset();
                TournamentSession.StartPartyMode(selectedPlayerCount, session != null ? session.PlayerName : null);
                return;
            }

            Rect closeBtn = new Rect(mRect.x + mRect.width * 0.25f, mRect.yMax - mH * 0.14f, mRect.width * 0.50f, mH * 0.09f);
            if (JungleRetainedUI.Button(closeBtn, "CERRAR", JungleUITheme.Button))
            {
                JungleAudioBank.PlayUiClick();
                showPartyModal = false;
                handNavigator.Reset();
                return;
            }
        }

        JungleUITheme.DrawNavigatorHandCursor(handNavigator, handLabel, handRaised, handClosed);
    }
}
