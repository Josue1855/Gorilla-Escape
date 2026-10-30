using GorillaEscape.Gameplay.Input;
using UnityEngine;
using GorillaEscape.QA;

[RequireComponent(typeof(JunglePanel))]
public sealed class ConnectionScreen : MonoBehaviour, IJungleScreen
{
    private static Texture2D sharedDarkGlass;
    private static Texture2D sharedSoftCard;
    private static Texture2D sharedWhiteCard;
    private static Texture2D sharedCyanButton;
    private static Texture2D sharedCyanButtonHover;
    private static Texture2D sharedGoldButton;
    private static Texture2D sharedDisabledButton;

    private MotionSession session;
    private PortableServiceHost services;
    private readonly HandDwellNavigator handNavigator = new HandDwellNavigator();
    private readonly HandDwellTarget[] targetsCanEnter = new HandDwellTarget[2];
    private readonly HandDwellTarget[] targetsCameraOnly = new HandDwellTarget[1];
    private Texture2D qrTexture;
    private Texture2D background;
    private Texture2D leavesTopLeft;
    private Texture2D leavesTopRight;
    private Texture2D leavesBottom;
    private Texture2D darkGlass;
    private Texture2D softCard;
    private Texture2D whiteCard;
    private Texture2D cyanButton;
    private Texture2D cyanButtonHover;
    private Texture2D goldButton;
    private Texture2D disabledButton;
    private Texture2D cyanPixel;
    private Texture2D goldPixel;
    private Texture2D greenPixel;
    private Texture2D shadowPixel;
    private Texture2D solidOverlay;
    private string qrSignature;
    private bool showingPreparation;
    private bool showCameraSetup;

    private GUIStyle logoShadowStyle, logoStyle, taglineStyle;
    private GUIStyle shellPanelStyle, codeCardPanelStyle, qrCardPanelStyle, statusPanelStyle;
    private GUIStyle eyebrowStyle, headingStyle, bodyStyle, pinLabelStyle, urlSubtextStyle;
    private GUIStyle statusTextStyle, statusReadyValueStyle, statusWaitValueStyle;
    private GUIStyle playerHeaderStyle, playerLineStyle, qrStepLabelStyle, qrLoadingLabelStyle;
    private GUIStyle copyButtonStyle, continueActiveStyle, continueDisabledStyle;
    private GUIStyle camSetupTitleStyle, camSetupBodyStyle, camSetupCenterStyle, camSetupButtonStyle;
    private int lastScreenWidth, lastScreenHeight;

    private void Awake()
    {
        StartCoroutine(JungleAudioBank.Prewarm());
        session = MotionSession.Ensure();
        services = PortableServiceHost.Ensure();
        session.Connect();

        background = JungleAssets.Load<Texture2D>("Jungle/gorila_menu_hd");
        if (background == null) background = JungleAssets.Load<Texture2D>("Jungle/gorila_menu");
        leavesTopLeft = JungleAssets.Load<Texture2D>("Jungle/hojas_superior_izquierda");
        leavesTopRight = JungleAssets.Load<Texture2D>("Jungle/hoja_superior_derecha");
        leavesBottom = JungleAssets.Load<Texture2D>("Jungle/hojas_inferiores");

        Configure(background, FilterMode.Bilinear);
        Configure(leavesTopLeft, FilterMode.Bilinear);
        Configure(leavesTopRight, FilterMode.Bilinear);
        Configure(leavesBottom, FilterMode.Bilinear);

    }

    private void EnsureConnectionTextures()
    {
        if (darkGlass != null) return;
        darkGlass = JungleUITheme.WoodPanelRounded;
        softCard = JungleUITheme.BambooCardRounded;
        if (sharedWhiteCard == null) sharedWhiteCard = JungleUITheme.CreateRoundedTexture(80, 12, new Color(1f, .98f, .91f, 1f), new Color(.84f, .64f, .26f, 1f), 4);
        whiteCard = sharedWhiteCard;
        cyanButton = JungleUITheme.ButtonNormalRounded;
        cyanButtonHover = JungleUITheme.ButtonHoverRounded;
        goldButton = JungleUITheme.ButtonGoldRounded;
        if (sharedDisabledButton == null) sharedDisabledButton = JungleUITheme.CreateRoundedTexture(80, 12, new Color(.26f, .17f, .11f, .94f), new Color(.48f, .34f, .20f, .85f), 3);
        disabledButton = sharedDisabledButton;
        cyanPixel = JungleUITheme.SolidColor(new Color(1f, .80f, .22f, 1f));
        goldPixel = JungleUITheme.SolidColor(new Color(1f, .78f, .12f, 1f));
        greenPixel = JungleUITheme.SolidColor(new Color(.24f, .84f, .36f, 1f));
        shadowPixel = JungleUITheme.SolidColor(new Color(.05f, .02f, .005f, .52f));
        solidOverlay = JungleUITheme.SolidColor(new Color(.08f, .04f, .01f, .24f));
    }

    private static void Configure(Texture2D texture, FilterMode filter)
    {
        if (texture == null) return;
        texture.filterMode = filter;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.anisoLevel = 2;
    }

    private void Update()
    {
        if (services.AdmissionMode == "CLOUDFLARED") showingPreparation = false;
        var qr = showingPreparation ? services.Preparation : services.Admission;
        bool available = qr != null && (showingPreparation || qr.state == "AVAILABLE") && !string.IsNullOrEmpty(qr.modules);
        string signature = available ? qr.state + ":" + qr.modules : null;
        if (signature == qrSignature) return;
        qrSignature = signature;
        if (qrTexture != null) { Destroy(qrTexture); qrTexture = null; }
        if (!available) return;
        try { qrTexture = QaRuntimeService.DecodeQr(qr); }
        catch (System.Exception) { Debug.LogWarning("JUNGLE_QR_INVALID"); }
    }

    private void EnsureConnectionStyles(float scale)
    {
        if (logoStyle != null && lastScreenWidth == Screen.width && lastScreenHeight == Screen.height) return;
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        logoShadowStyle = Label(Mathf.RoundToInt(54 * scale), new Color(.08f, .03f, .005f, .85f), FontStyle.Bold, TextAnchor.MiddleLeft);
        logoStyle = Label(Mathf.RoundToInt(54 * scale), new Color(1f, .84f, .16f), FontStyle.Bold, TextAnchor.MiddleLeft);
        taglineStyle = Label(Mathf.RoundToInt(15 * scale), new Color(1f, .94f, .76f), FontStyle.Bold, TextAnchor.MiddleLeft);

        shellPanelStyle = PanelStyle(darkGlass, 24);
        codeCardPanelStyle = PanelStyle(softCard, 18);
        qrCardPanelStyle = PanelStyle(whiteCard, 18);
        statusPanelStyle = PanelStyle(softCard, 18);

        eyebrowStyle = Label(Mathf.RoundToInt(14 * scale), new Color(1f, .82f, .28f), FontStyle.Bold, TextAnchor.MiddleLeft);
        headingStyle = Label(Mathf.RoundToInt(34 * scale), new Color(1f, .96f, .86f), FontStyle.Bold, TextAnchor.UpperLeft);
        bodyStyle = Label(Mathf.RoundToInt(16 * scale), new Color(.94f, .86f, .72f), FontStyle.Normal, TextAnchor.UpperLeft);
        pinLabelStyle = Label(Mathf.RoundToInt(30 * scale), new Color(1f, .84f, .16f), FontStyle.Bold, TextAnchor.MiddleCenter);
        urlSubtextStyle = Label(Mathf.RoundToInt(14 * scale), new Color(.96f, .86f, .64f), FontStyle.Bold, TextAnchor.MiddleCenter);

        statusTextStyle = Label(Mathf.RoundToInt(15 * scale), new Color(1f, .96f, .86f), FontStyle.Bold, TextAnchor.MiddleLeft);
        statusReadyValueStyle = new GUIStyle(statusTextStyle) { alignment = TextAnchor.MiddleRight, normal = { textColor = new Color(.42f, .96f, .48f) } };
        statusWaitValueStyle = new GUIStyle(statusTextStyle) { alignment = TextAnchor.MiddleRight, normal = { textColor = new Color(1f, .84f, .30f) } };

        playerHeaderStyle = Label(Mathf.RoundToInt(15 * scale), new Color(1f, .82f, .28f), FontStyle.Bold, TextAnchor.MiddleLeft);
        playerLineStyle = Label(Mathf.RoundToInt(16 * scale), new Color(1f, .96f, .86f), FontStyle.Normal, TextAnchor.MiddleLeft);
        qrStepLabelStyle = Label(Mathf.RoundToInt(14 * scale), new Color(.22f, .10f, .03f), FontStyle.Bold, TextAnchor.MiddleCenter);
        qrLoadingLabelStyle = Label(Mathf.RoundToInt(16 * scale), new Color(.36f, .18f, .05f), FontStyle.Bold, TextAnchor.MiddleCenter);

        copyButtonStyle = ButtonStyle(softCard, cyanButton, new Color(1f, .96f, .86f), Mathf.RoundToInt(15 * scale));
        continueActiveStyle = ButtonStyle(goldButton, cyanButtonHover, new Color(.18f, .08f, .01f), Mathf.RoundToInt(16 * scale));
        continueDisabledStyle = ButtonStyle(disabledButton, disabledButton, new Color(.72f, .62f, .50f), Mathf.RoundToInt(15 * scale));

        camSetupTitleStyle = Label(Mathf.RoundToInt(25 * scale), new Color(1f, .84f, .18f), FontStyle.Bold, TextAnchor.MiddleLeft);
        camSetupBodyStyle = Label(Mathf.RoundToInt(16 * scale), new Color(.96f, .90f, .78f), FontStyle.Normal, TextAnchor.UpperLeft);
        camSetupCenterStyle = Label(Mathf.RoundToInt(16 * scale), new Color(1f, .96f, .86f), FontStyle.Bold, TextAnchor.MiddleCenter);
        camSetupButtonStyle = ButtonStyle(goldButton, cyanButtonHover, new Color(.18f, .08f, .01f), Mathf.RoundToInt(16 * scale));
    }

    public void RenderUI()
    {
        JungleUITheme.Ensure();
        EnsureConnectionTextures();
        bool wasEnabled = JungleRetainedUI.enabled;
        if (showCameraSetup) JungleRetainedUI.enabled = false;
        float w = Screen.width;
        float h = Screen.height;
        float scale = Mathf.Clamp(Mathf.Min(w / 1920f, h / 1080f), .62f, 1.65f);
        float time = Time.unscaledTime;
        EnsureConnectionStyles(scale);

        DrawBackground(w, h, time);
        DrawWind(w, h, time, scale);

        JungleRetainedUI.Label(new Rect(w * .055f + 3 * scale, h * .055f + 4 * scale, w * .52f, 70 * scale), "JUNGLE PARTY", logoShadowStyle);
        JungleRetainedUI.Label(new Rect(w * .055f, h * .055f, w * .52f, 70 * scale), "JUNGLE PARTY", logoStyle);
        JungleRetainedUI.Label(new Rect(w * .058f, h * .055f + 62 * scale, w * .45f, 25 * scale), "MUEVE · JUEGA · CONQUISTA LA SELVA", taglineStyle);

        Rect shell = new Rect(w * .055f, h * .205f, Mathf.Min(w * .56f, 1030 * scale), h * .715f);
        JungleUITheme.DrawPanel(shell);

        float pad = 38 * scale;
        Rect inner = new Rect(shell.x + pad, shell.y + pad, shell.width - pad * 2, shell.height - pad * 2);
        float gap = 28 * scale;
        float qrColumn = Mathf.Clamp(inner.width * .39f, 210 * scale, 360 * scale);
        float textColumn = inner.width - qrColumn - gap;
        Rect left = new Rect(inner.x, inner.y, textColumn, inner.height);
        Rect right = new Rect(inner.xMax - qrColumn, inner.y, qrColumn, inner.height);

        JungleRetainedUI.Label(new Rect(left.x, left.y, left.width, 24 * scale), "CONFIGURACIÓN DEL JUGADOR", eyebrowStyle);
        JungleRetainedUI.Label(new Rect(left.x, left.y + 30 * scale, left.width, 84 * scale), "Conecta tu\ncelular", headingStyle);
        JungleRetainedUI.Label(new Rect(left.x, left.y + 117 * scale, left.width, 48 * scale), "Conecta el celular a la misma red, escanea el QR y permite los sensores.", bodyStyle);

        Rect codeCard = new Rect(left.x, left.y + 174 * scale, left.width, 78 * scale);
        JungleRetainedUI.Box(codeCard, GUIContent.none, codeCardPanelStyle);
        string currentRoom = session.RoomCode;
        bool hasRoom = !string.IsNullOrEmpty(currentRoom);
        string displayCode = hasRoom ? currentRoom.ToUpperInvariant() : "...";
        JungleRetainedUI.Label(new Rect(codeCard.x, codeCard.y + 6 * scale, codeCard.width, 38 * scale), "SALA: " + displayCode, pinLabelStyle);
        string connectionLabel = services.AdmissionMode == "CLOUDFLARED" && hasRoom
            ? "play.gorillas.run/" + currentRoom.ToLowerInvariant() + " · entra con el QR"
            : services.AdmissionMode == "CABIN" ? (string.IsNullOrEmpty(services.ConnectionFallback) || services.ConnectionFallback == "NONE" ? "CONEXIÓN LOCAL · EL QR USA LA IP DE LA PC" : "TÚNEL NO DISPONIBLE · QR DE CONEXIÓN LOCAL")
            : "PREPARANDO CONEXIÓN Y QR";
        JungleRetainedUI.Label(new Rect(codeCard.x, codeCard.y + 44 * scale, codeCard.width, 24 * scale), connectionLabel, urlSubtextStyle);

        var cameraRow = new Rect(left.x, left.y + 270 * scale, left.width, 48 * scale);
        DrawStatus(cameraRow, "CÁMARA · VER", session.CameraConnected ? "LISTA" : "SIN SEÑAL", session.CameraConnected, scale);
        if (JungleRetainedUI.Button(cameraRow, GUIContent.none, GUIStyle.none)) { showCameraSetup = true; services.SetCameraSetupVisible(true); }
        DrawStatus(new Rect(left.x, left.y + 328 * scale, left.width, 48 * scale), "CELULAR", session.PhoneConnected ? "CONECTADO" : "ESPERANDO", session.PhoneConnected, scale);
        JungleRetainedUI.Label(new Rect(left.x + 5 * scale, left.y + 387 * scale, left.width - 10 * scale, 45 * scale), services.Status, bodyStyle);
        var players = services.Lobby?.players;
        JungleRetainedUI.Label(new Rect(left.x + 5 * scale, left.y + 430 * scale, left.width - 10 * scale, 24 * scale), "JUGADORES " + (players?.Length ?? 0) + "/4", playerHeaderStyle);
        if (players != null)
        {
            for (int i = 0; i < players.Length && i < 4; i++)
            {
                var player = players[i];
                string playerName = string.IsNullOrWhiteSpace(player.displayName) ? "Escribiendo nombre…" : player.displayName;
                string playerState = player.connectionState == "DISCONNECTED" ? "DESCONECTADO" : player.inputReady ? "LISTO" : "CONECTADO";
                JungleRetainedUI.Label(new Rect(left.x + 5 * scale, left.y + (458 + i * 25) * scale, left.width - 10 * scale, 24 * scale), playerName + "  ·  " + playerState, playerLineStyle);
            }
        }

        Rect qrCard = new Rect(right.x, right.y + 2 * scale, right.width, right.width + 62 * scale);
        JungleRetainedUI.Box(qrCard, GUIContent.none, qrCardPanelStyle);
        float qrPad = 15 * scale;
        Rect qrRect = new Rect(qrCard.x + qrPad, qrCard.y + qrPad, qrCard.width - qrPad * 2, qrCard.width - qrPad * 2);
        if (qrTexture != null)
        {
            JungleRetainedUI.DrawTexture(qrRect, qrTexture, ScaleMode.ScaleToFit, false);
            JungleRetainedUI.Label(new Rect(qrCard.x + 5 * scale, qrCard.yMax - 52 * scale, qrCard.width - 10 * scale, 40 * scale), (services.AdmissionMode == "CLOUDFLARED" ? "ESCANEA PARA JUGAR" : showingPreparation ? "PASO 1: PREPARAR" : "PASO 2: ESCANEAR PARA JUGAR"), qrStepLabelStyle);
        }
        else
        {
            // Radar scanline effect over QR area
            float scanCycle = Mathf.Repeat(time * 0.85f, 1f);
            float scanY = qrRect.y + scanCycle * (qrRect.height - 4 * scale);
            float glowHeight = 24 * scale;

            Color prev = JungleRetainedUI.color;
            JungleRetainedUI.color = new Color(1f, .76f, .18f, .18f);
            JungleRetainedUI.DrawTexture(new Rect(qrRect.x, Mathf.Max(qrRect.y, scanY - glowHeight), qrRect.width, glowHeight), cyanPixel);
            JungleRetainedUI.color = new Color(.86f, .52f, .08f, .85f);
            JungleRetainedUI.DrawTexture(new Rect(qrRect.x, scanY, qrRect.width, Mathf.Max(2, 3 * scale)), cyanPixel);
            JungleRetainedUI.color = prev;

            float pulse = (Mathf.Sin(time * 3.5f) + 1f) * .5f;
            qrLoadingLabelStyle.normal.textColor = Color.Lerp(new Color(.24f, .11f, .03f), new Color(.62f, .34f, .06f), pulse);
            int dotCount = 1 + ((int)(time * 2f) % 3);
            string dots = dotCount == 1 ? "." : dotCount == 2 ? ".." : "...";
            JungleRetainedUI.Label(qrRect, "CONECTANDO CON\nTÚNEL SEGURO" + dots + "\n\nPREPARANDO QR", qrLoadingLabelStyle);
        }

        float buttonY = shell.yMax - pad - 50 * scale;
        bool namedPlayer = players != null && System.Array.Exists(players, p => p != null && p.connectionState != "DISCONNECTED" && !string.IsNullOrWhiteSpace(p.displayName));
        bool canEnter = JungleAudioBank.IsPrewarmed && session.PhoneConnected && namedPlayer;
        GUIStyle continueButton = canEnter ? continueActiveStyle : continueDisabledStyle;
        if (services.AdmissionMode == "CABIN" && JungleRetainedUI.Button(new Rect(right.x, qrCard.yMax + 10 * scale, right.width, 40 * scale), showingPreparation ? "PASO 2: UNIRSE" : "PASO 1: PREPARAR", copyButtonStyle))
            showingPreparation = !showingPreparation;
        if (JungleRetainedUI.Button(new Rect(left.x, buttonY, left.width, 50 * scale), services.NeedsNetworkPermission ? "PERMITIR RED LOCAL" : "REINTENTAR CONEXIÓN", copyButtonStyle))
        {
            if (services.NeedsNetworkPermission) services.InstallNetwork();
            else services.RestartModule();
        }
        Rect enterRect = new Rect(right.x, buttonY, right.width, 50 * scale);
        Rect cameraOnlyRect = new Rect(w * .70f, h * .84f, w * .26f, h * .065f);

        Vector2 normCursor = new Vector2(.5f, .5f);
        string handLabel = "MANO DER";
        bool handRaised = false;
        bool hasHand = !showCameraSetup && session.TryCameraCursor(out normCursor, out handLabel, out handRaised);
        Vector2 cursorPx = new Vector2(normCursor.x * w, normCursor.y * h);
        HandDwellTarget[] targets;
        if (canEnter)
        {
            targetsCanEnter[0] = new HandDwellTarget(1, enterRect.x, enterRect.y, enterRect.width, enterRect.height);
            targetsCanEnter[1] = new HandDwellTarget(2, cameraOnlyRect.x, cameraOnlyRect.y, cameraOnlyRect.width, cameraOnlyRect.height);
            targets = targetsCanEnter;
        }
        else
        {
            targetsCameraOnly[0] = new HandDwellTarget(2, cameraOnlyRect.x, cameraOnlyRect.y, cameraOnlyRect.width, cameraOnlyRect.height);
            targets = targetsCameraOnly;
        }
        bool dwellFired = handNavigator.ObserveCursor(hasHand, cursorPx.x, cursorPx.y, Time.unscaledTime, targets, out int triggeredId)
            && triggeredId > 0;

        if ((JungleRetainedUI.Button(enterRect, canEnter ? "ENTRAR A LA SELVA" : !JungleAudioBank.IsPrewarmed ? "PREPARANDO AUDIO" : session.PhoneConnected ? "ESPERANDO NOMBRE" : "ESPERANDO CELULAR", continueButton) || (dwellFired && handNavigator.HoveredId == 1)) && canEnter)
        {
            handNavigator.Reset();
            session.SetCameraOnly(false);
            SceneNavigator.Load(SceneNavigator.MainMenu);
            return;
        }
        if (canEnter && handNavigator.HoveredId == 1 && handNavigator.Progress > .02f)
            JungleUITheme.DrawDwellOverlay(enterRect, handNavigator.Progress);

        if (JungleAudioBank.IsPrewarmed && (JungleRetainedUI.Button(cameraOnlyRect, "JUEGOS SIN CELULAR", copyButtonStyle) || (dwellFired && handNavigator.HoveredId == 2)))
        {
            handNavigator.Reset();
            session.SetCameraOnly(true);
            SceneNavigator.Load(SceneNavigator.MainMenu);
            return;
        }
        if (handNavigator.HoveredId == 2 && handNavigator.Progress > .02f)
            JungleUITheme.DrawDwellOverlay(cameraOnlyRect, handNavigator.Progress, "MANTÉN LA MANO AQUÍ");

        DrawForegroundLeaves(w, h, time, scale);
        JungleRetainedUI.enabled = wasEnabled;
        if (showCameraSetup) DrawCameraSetup(w, h, scale);
        else JungleUITheme.DrawNavigatorHandCursor(handNavigator, handLabel, handRaised);
    }

    private void DrawCameraSetup(float w, float h, float scale)
    {
        JungleRetainedUI.DrawTexture(new Rect(0, 0, w, h), shadowPixel);
        float width = Mathf.Min(w - 30, 760 * scale), height = Mathf.Min(h - 24, 690 * scale);
        var panel = new Rect((w - width) / 2, (h - height) / 2, width, height);
        JungleUITheme.DrawPanel(panel);
        float pad = 24 * scale;
        JungleRetainedUI.Label(new Rect(panel.x + pad, panel.y + pad, panel.width - 2 * pad, 38 * scale), "PREPARA TU CÁMARA", camSetupTitleStyle);
        float previewHeight = Mathf.Min(panel.height * .40f, 290 * scale);
        float previewWidth = Mathf.Min(panel.width - 2 * pad, previewHeight * 4f / 3f);
        var preview = new Rect(panel.center.x - previewWidth / 2, panel.y + 70 * scale, previewWidth, previewHeight);
        JungleRetainedUI.DrawTexture(preview, Texture2D.blackTexture);
        if (services.CameraHelpTexture != null) JungleRetainedUI.DrawTextureWithTexCoords(preview, services.CameraHelpTexture, new Rect(1, 0, -1, 1));
        else JungleRetainedUI.Label(preview, services.CameraActive ? "PREPARANDO VISTA DE CÁMARA" : "ESPERANDO CÁMARA", camSetupCenterStyle);
        var framing = services.CameraFraming;
        float y = preview.yMax + 12 * scale;
        JungleRetainedUI.Label(new Rect(panel.x + pad, y, panel.width - 2 * pad, 52 * scale), framing.Badge + " · " + services.CameraGuidance, camSetupBodyStyle);
        JungleRetainedUI.Label(new Rect(panel.x + pad, y + 56 * scale, panel.width - 2 * pad, 70 * scale),
            (services.CameraActive ? "CÁMARA ACTIVA. " : "CÁMARA SIN INICIAR. ") + "La vista previa se procesa en esta PC y se transmite entre procesos locales. Evita contraluz y despeja el área.", camSetupBodyStyle);
        if (JungleRetainedUI.Button(new Rect(panel.x + pad, panel.yMax - pad - 46 * scale, panel.width - 2 * pad, 46 * scale), "CONTINUAR A CONEXIÓN", camSetupButtonStyle))
        { showCameraSetup = false; services.SetCameraSetupVisible(false); }
    }

    private void DrawBackground(float w, float h, float time)
    {
        float drift = Mathf.Sin(time * .17f) * 5f;
        if (background != null) JungleRetainedUI.DrawTexture(new Rect(-8 + drift, -6, w + 16, h + 12), background, ScaleMode.ScaleAndCrop, true);
        else JungleRetainedUI.DrawTexture(new Rect(0, 0, w, h), softCard);
        JungleRetainedUI.DrawTexture(new Rect(0, 0, w, h), solidOverlay);
    }

    private void DrawWind(float w, float h, float time, float scale)
    {
        Color previous = JungleRetainedUI.color;
        for (int i = 0; i < 5; i++)
        {
            float x = Mathf.Repeat(time * (34 + i * 5) + i * 310, w + 420) - 260;
            float y = h * (.15f + i * .115f) + Mathf.Sin(time * .7f + i) * 13 * scale;
            float length = (90 + i * 22) * scale;
            JungleRetainedUI.color = new Color(.68f, 1f, .95f, .07f + i * .008f);
            JungleRetainedUI.DrawTexture(new Rect(x, y, length, Mathf.Max(1, 2 * scale)), cyanPixel);
            JungleRetainedUI.DrawTexture(new Rect(x + length + 13 * scale, y, 24 * scale, Mathf.Max(1, 2 * scale)), cyanPixel);
        }
        JungleRetainedUI.color = previous;
    }

    private void DrawForegroundLeaves(float w, float h, float time, float scale)
    {
        if (leavesTopLeft != null)
            DrawSway(leavesTopLeft, new Rect(-32 * scale, -28 * scale, 300 * scale, 210 * scale), Mathf.Sin(time * .65f) * 2.5f, new Vector2(0, 0));
        if (leavesTopRight != null)
            DrawSway(leavesTopRight, new Rect(w - 250 * scale, -25 * scale, 285 * scale, 205 * scale), Mathf.Sin(time * .57f + 1.4f) * 3f, new Vector2(w, 0));
        if (leavesBottom != null)
        {
            float bob = Mathf.Sin(time * .8f) * 5 * scale;
            DrawSway(leavesBottom, new Rect(-45 * scale, h - 175 * scale + bob, 360 * scale, 210 * scale), Mathf.Sin(time * .52f) * 2f, new Vector2(0, h));
        }
    }

    private static void DrawSway(Texture texture, Rect rect, float angle, Vector2 pivot)
    {
        Matrix4x4 previous = JungleRetainedUI.matrix;
        JungleRetainedUI.RotateAroundPivot(angle, pivot);
        JungleRetainedUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true);
        JungleRetainedUI.matrix = previous;
    }

    private void DrawStatus(Rect rect, string label, string value, bool ready, float scale)
    {
        JungleRetainedUI.Box(rect, GUIContent.none, statusPanelStyle);
        JungleRetainedUI.DrawTexture(new Rect(rect.x + 15 * scale, rect.y + 16 * scale, 16 * scale, 16 * scale), ready ? greenPixel : goldPixel);
        JungleRetainedUI.Label(new Rect(rect.x + 43 * scale, rect.y, rect.width * .45f, rect.height), label, statusTextStyle);
        JungleRetainedUI.Label(new Rect(rect.x + rect.width * .44f, rect.y, rect.width * .51f, rect.height), value, ready ? statusReadyValueStyle : statusWaitValueStyle);
    }

    private static GUIStyle PanelStyle(Texture2D texture, int border)
    {
        var style = new GUIStyle(JungleRetainedUI.skin.box);
        style.normal.background = texture;
        style.border = new RectOffset(border, border, border, border);
        return style;
    }

    private static GUIStyle Label(int size, Color color, FontStyle weight, TextAnchor alignment)
    {
        var style = new GUIStyle(JungleRetainedUI.skin.label)
        {
            font = size >= 28 ? JungleUITheme.TitleFont : JungleUITheme.TextFont,
            fontSize = size,
            fontStyle = weight,
            alignment = alignment,
            wordWrap = true,
            clipping = TextClipping.Clip
        };
        style.normal.textColor = color;
        return style;
    }

    private static GUIStyle ButtonStyle(Texture2D normal, Texture2D hover, Color textColor, int size)
    {
        var style = new GUIStyle(JungleRetainedUI.skin.button)
        {
            font = JungleUITheme.TextFont,
            fontSize = size,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = false,
            clipping = TextClipping.Clip,
            border = new RectOffset(18, 18, 18, 18),
            padding = new RectOffset(14, 14, 6, 6)
        };
        style.normal.background = normal;
        style.hover.background = hover;
        style.active.background = hover;
        style.normal.textColor = textColor;
        style.hover.textColor = textColor;
        style.active.textColor = textColor;
        return style;
    }

    private void OnDestroy()
    {
        if (services != null) services.SetCameraSetupVisible(false);
        if (qrTexture != null) Destroy(qrTexture);
    }
}
