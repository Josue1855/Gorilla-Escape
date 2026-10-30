using GorillaEscape.Gameplay.Input;
using UnityEngine;

[RequireComponent(typeof(JunglePanel))]
public sealed class MainMenuController : MonoBehaviour, IJungleScreen
{
    private static readonly string[] CameraOnlyNames = { "COCONUT\nSLICE", "PORTERO DE\nLA JUNGLA" };
    private static readonly string[] CameraOnlyTags = { "CÁMARA · 2 MANOS", "CÁMARA · MANOS Y TORSO" };

    private static readonly string[] FullNames =
    {
        "BANANA\nBOWLING", "BOXEO\nCON GORILA", "JUNGLE\nBASEBALL", "JUNGLE\nDRUMS",
        "COCONUT\nSLICE", "PORTERO DE\nLA JUNGLA", "GORILLA\nWHACK", "DESAFÍO DEL\nTRONCO"
    };

    private static readonly string[] FullTags =
    {
        "CELULAR + MANO", "CELULAR + CUERPO", "CELULAR + MANO", "CELULAR + 2 MANOS",
        "SOLO CÁMARA", "CÁMARA · ARQUERO", "CELULAR + MANO", "SOLO CELULAR"
    };

    private static readonly string[] CameraOnlyScenes = { SceneNavigator.CoconutSlice, SceneNavigator.JungleKeeper };
    private static readonly string[] FullScenes =
    {
        SceneNavigator.Bowling, SceneNavigator.Boxing, SceneNavigator.Baseball, SceneNavigator.Drums,
        SceneNavigator.CoconutSlice, SceneNavigator.JungleKeeper, SceneNavigator.WhackAMole, SceneNavigator.LogBalance
    };

    private MotionSession session;
    private readonly HandDwellNavigator handNavigator = new HandDwellNavigator();
    private readonly Rect[] cardRects = new Rect[8];
    private readonly HandDwellTarget[] cameraOnlyTargets = new HandDwellTarget[3];
    private readonly HandDwellTarget[] fullTargets = new HandDwellTarget[10];
    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle cardTitleStyle;
    private GUIStyle cardTitleSelectedStyle;
    private int lastWidth, lastHeight;
    private int cachedDwellPct = -1;
    private string cachedDwellLabel = "";

    private void Awake()
    {
        session = MotionSession.Ensure();
        session.Connect();
        if (session.PhoneConnected) session.SetCameraOnly(false);
        JungleAudioBank.Attach2DSource(gameObject);
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
        const int columns = 4;
        float gap = w * .022f, cardWidth = (w * .90f - gap * (columns - 1)) / columns;
        EnsureMenuStyles(w, h, cardWidth);

        Rect headerPanel = new Rect(w * .20f, h * .035f, w * .60f, h * .155f);
        JungleUITheme.DrawPanel(headerPanel);
        JungleUITheme.DrawShadowedLabel(new Rect(headerPanel.x, h * .042f, headerPanel.width, h * .095f), "JUNGLE PARTY", titleStyle, 3.5f);

        string[] names = session.CameraOnly ? CameraOnlyNames : FullNames;
        string[] tags = session.CameraOnly ? CameraOnlyTags : FullTags;
        string[] scenes = session.CameraOnly ? CameraOnlyScenes : FullScenes;

        bool hasHand = session.TryCameraCursor(out Vector2 normCursor, out string handLabel, out bool handRaised);
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

        Rect primaryModeRect = new Rect(w * .22f, h * .775f, w * .27f, h * .075f);
        Rect cameraModeRect = new Rect(w * .51f, h * .775f, w * .27f, h * .075f);

        HandDwellTarget[] targets = session.CameraOnly ? cameraOnlyTargets : fullTargets;
        for (int i = 0; i < names.Length; i++)
            targets[i] = new HandDwellTarget(i, cardRects[i].x, cardRects[i].y, cardRects[i].width, cardRects[i].height);
        targets[names.Length] = new HandDwellTarget(100, primaryModeRect.x, primaryModeRect.y, primaryModeRect.width, primaryModeRect.height);
        if (!session.CameraOnly)
            targets[names.Length + 1] = new HandDwellTarget(101, cameraModeRect.x, cameraModeRect.y, cameraModeRect.width, cameraModeRect.height);

        Vector2 cursorPx = new Vector2(normCursor.x * w, normCursor.y * h);
        bool dwellFired = handNavigator.ObserveCursor(hasHand, cursorPx.x, cursorPx.y, Time.unscaledTime, targets, out int triggeredId)
            && triggeredId >= 0;
        int hoveredId = handNavigator.HoveredId;
        float dwellProgress = handNavigator.Progress;

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
                if (pct != cachedDwellPct) { cachedDwellPct = pct; cachedDwellLabel = $"MANTÉN LA MANO · {pct}%"; }
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

        if (!session.CameraOnly)
        {
            bool cameraHovered = hoveredId == 101;
            if (JungleRetainedUI.Button(cameraModeRect, "JUEGOS SIN CELULAR", cameraHovered ? JungleUITheme.ButtonSelected : JungleUITheme.Button)
                || (dwellFired && hoveredId == 101))
            {
                JungleAudioBank.PlayUiClick();
                handNavigator.Reset();
                session.SetCameraOnly(true);
                return;
            }
            if (cameraHovered && dwellProgress > .02f)
                JungleUITheme.DrawDwellOverlay(cameraModeRect, dwellProgress);
        }

        JungleUITheme.DrawNavigatorHandCursor(handNavigator, handLabel, handRaised);
    }
}
