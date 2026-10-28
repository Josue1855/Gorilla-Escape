using UnityEngine;
using GorillaEscape.QA;

public sealed class ConnectionScreen : MonoBehaviour
{
    private MotionSession session;
    private PortableServiceHost services;
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
    private string qrSignature;
    private bool showingPreparation;

    private void Awake()
    {
        session = MotionSession.Ensure();
        services = PortableServiceHost.Ensure();
        session.Connect();

        background = Resources.Load<Texture2D>("Jungle/gorila_menu_hd");
        if (background == null) background = Resources.Load<Texture2D>("Jungle/gorila_menu");
        leavesTopLeft = Resources.Load<Texture2D>("Jungle/hojas_superior_izquierda");
        leavesTopRight = Resources.Load<Texture2D>("Jungle/hoja_superior_derecha");
        leavesBottom = Resources.Load<Texture2D>("Jungle/hojas_inferiores");

        Configure(background, FilterMode.Bilinear);
        Configure(leavesTopLeft, FilterMode.Bilinear);
        Configure(leavesTopRight, FilterMode.Bilinear);
        Configure(leavesBottom, FilterMode.Bilinear);

        darkGlass = RoundedTexture(96, 22, new Color(.018f, .075f, .082f, .94f), new Color(.18f, .92f, .84f, .25f), 2);
        softCard = RoundedTexture(72, 18, new Color(.035f, .145f, .145f, .90f), new Color(.24f, .82f, .72f, .16f), 1);
        whiteCard = RoundedTexture(72, 18, new Color(.98f, .99f, .96f, 1f), new Color(1f, 1f, 1f, 1f), 1);
        cyanButton = RoundedTexture(72, 18, new Color(.10f, .86f, .78f, 1f), new Color(.36f, 1f, .91f, 1f), 1);
        cyanButtonHover = RoundedTexture(72, 18, new Color(.24f, .98f, .86f, 1f), Color.white, 1);
        goldButton = RoundedTexture(72, 18, new Color(1f, .79f, .12f, 1f), new Color(1f, .92f, .45f, 1f), 1);
        disabledButton = RoundedTexture(72, 18, new Color(.22f, .35f, .36f, .96f), new Color(.35f, .48f, .48f, .8f), 1);
        cyanPixel = Solid(new Color(.16f, .94f, .84f, 1f));
        goldPixel = Solid(new Color(1f, .79f, .12f, 1f));
        greenPixel = Solid(new Color(.20f, .94f, .52f, 1f));
        shadowPixel = Solid(new Color(.005f, .02f, .025f, .45f));
    }

    private static void Configure(Texture2D texture, FilterMode filter)
    {
        if (texture == null) return;
        texture.filterMode = filter;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.anisoLevel = 2;
    }

    private static Texture2D Solid(Color color)
    {
        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, color);
        texture.Apply(false, true);
        return texture;
    }

    private static Texture2D RoundedTexture(int size, int radius, Color fill, Color stroke, int strokeWidth)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        float outerRadius = radius;
        float innerRadius = Mathf.Max(0, radius - strokeWidth);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(0, Mathf.Max(radius - x, x - (size - radius - 1)));
                float dy = Mathf.Max(0, Mathf.Max(radius - y, y - (size - radius - 1)));
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float outer = Mathf.Clamp01(outerRadius + .5f - distance);
                float inner = Mathf.Clamp01(innerRadius + .5f - distance);
                Color color = Color.Lerp(stroke, fill, inner);
                color.a *= outer;
                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply(false, true);
        return texture;
    }

    private void Update()
    {
        if(services.AdmissionMode == "CLOUDFLARED") showingPreparation = false;
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

    private void OnGUI()
    {
        float w = Screen.width;
        float h = Screen.height;
        float scale = Mathf.Clamp(Mathf.Min(w / 1920f, h / 1080f), .62f, 1.65f);
        float time = Time.unscaledTime;

        DrawBackground(w, h, time);
        DrawWind(w, h, time, scale);

        GUIStyle logoShadow = Label(Mathf.RoundToInt(54 * scale), new Color(0f, 0f, 0f, .34f), FontStyle.Bold, TextAnchor.MiddleLeft);
        GUIStyle logo = Label(Mathf.RoundToInt(54 * scale), new Color(1f, .82f, .12f), FontStyle.Bold, TextAnchor.MiddleLeft);
        GUIStyle tagline = Label(Mathf.RoundToInt(15 * scale), new Color(.79f, 1f, .95f), FontStyle.Bold, TextAnchor.MiddleLeft);
        GUI.Label(new Rect(w * .055f + 3 * scale, h * .055f + 4 * scale, w * .52f, 70 * scale), "JUNGLE PARTY", logoShadow);
        GUI.Label(new Rect(w * .055f, h * .055f, w * .52f, 70 * scale), "JUNGLE PARTY", logo);
        GUI.Label(new Rect(w * .058f, h * .055f + 62 * scale, w * .45f, 25 * scale), "MUEVE · JUEGA · CONQUISTA LA SELVA", tagline);

        Rect shell = new Rect(w * .055f, h * .205f, Mathf.Min(w * .56f, 1030 * scale), h * .715f);
        GUI.DrawTexture(new Rect(shell.x + 16 * scale, shell.y + 18 * scale, shell.width, shell.height), shadowPixel);
        GUI.Box(shell, GUIContent.none, PanelStyle(darkGlass, 28));

        float pad = 38 * scale;
        Rect inner = new Rect(shell.x + pad, shell.y + pad, shell.width - pad * 2, shell.height - pad * 2);
        float gap = 28 * scale;
        float qrColumn = Mathf.Clamp(inner.width * .39f, 210 * scale, 360 * scale);
        float textColumn = inner.width - qrColumn - gap;
        Rect left = new Rect(inner.x, inner.y, textColumn, inner.height);
        Rect right = new Rect(inner.xMax - qrColumn, inner.y, qrColumn, inner.height);

        GUIStyle eyebrow = Label(Mathf.RoundToInt(14 * scale), new Color(.29f, .96f, .87f), FontStyle.Bold, TextAnchor.MiddleLeft);
        GUIStyle heading = Label(Mathf.RoundToInt(34 * scale), Color.white, FontStyle.Bold, TextAnchor.UpperLeft);
        GUIStyle body = Label(Mathf.RoundToInt(16 * scale), new Color(.76f, .88f, .85f), FontStyle.Normal, TextAnchor.UpperLeft);
        GUIStyle code = Label(Mathf.RoundToInt(28 * scale), new Color(1f, .82f, .15f), FontStyle.Bold, TextAnchor.MiddleCenter);

        GUI.Label(new Rect(left.x, left.y, left.width, 24 * scale), "CONFIGURACIÓN DEL JUGADOR", eyebrow);
        GUI.Label(new Rect(left.x, left.y + 30 * scale, left.width, 84 * scale), "Conecta tu\ncelular", heading);
        GUI.Label(new Rect(left.x, left.y + 117 * scale, left.width, 48 * scale), "Conecta el celular a la misma red, escanea el QR y permite los sensores.", body);

        Rect codeCard = new Rect(left.x, left.y + 174 * scale, left.width, 78 * scale);
        GUI.Box(codeCard, GUIContent.none, PanelStyle(softCard, 20));
        string currentRoom = session.RoomCode;
        bool hasRoom = !string.IsNullOrEmpty(currentRoom);
        string displayCode = hasRoom ? currentRoom.ToUpperInvariant() : "...";
        GUIStyle pinLabel = Label(Mathf.RoundToInt(30 * scale), new Color(1f, .82f, .15f), FontStyle.Bold, TextAnchor.MiddleCenter);
        GUIStyle urlSubtext = Label(Mathf.RoundToInt(14 * scale), new Color(.65f, .95f, .90f), FontStyle.Bold, TextAnchor.MiddleCenter);
        GUI.Label(new Rect(codeCard.x, codeCard.y + 6 * scale, codeCard.width, 38 * scale), "SALA: " + displayCode, pinLabel);
        string connectionLabel = services.AdmissionMode == "CLOUDFLARED" && hasRoom
            ? "play.gorillas.run/" + currentRoom.ToLowerInvariant() + " · entra con el QR"
            : services.AdmissionMode == "CABIN" ? (string.IsNullOrEmpty(services.ConnectionFallback) || services.ConnectionFallback == "NONE" ? "CONEXIÓN LOCAL · EL QR USA LA IP DE LA PC" : "TÚNEL NO DISPONIBLE · QR DE CONEXIÓN LOCAL")
            : "PREPARANDO CONEXIÓN Y QR";
        GUI.Label(new Rect(codeCard.x, codeCard.y + 44 * scale, codeCard.width, 24 * scale), connectionLabel, urlSubtext);

        GUIStyle statusText = Label(Mathf.RoundToInt(15 * scale), Color.white, FontStyle.Bold, TextAnchor.MiddleLeft);
        DrawStatus(new Rect(left.x, left.y + 270 * scale, left.width, 48 * scale), "CÁMARA", session.CameraConnected ? "LISTA" : "SIN SEÑAL", session.CameraConnected, statusText, scale);
        DrawStatus(new Rect(left.x, left.y + 328 * scale, left.width, 48 * scale), "CELULAR", session.PhoneConnected ? "CONECTADO" : "ESPERANDO", session.PhoneConnected, statusText, scale);
        GUI.Label(new Rect(left.x + 5 * scale, left.y + 387 * scale, left.width - 10 * scale, 45 * scale), services.Status, body);
        var players = services.Lobby?.players;
        GUIStyle playerHeader = Label(Mathf.RoundToInt(15 * scale), new Color(.29f,.96f,.87f), FontStyle.Bold, TextAnchor.MiddleLeft);
        GUIStyle playerLine = Label(Mathf.RoundToInt(16 * scale), Color.white, FontStyle.Normal, TextAnchor.MiddleLeft);
        GUI.Label(new Rect(left.x + 5 * scale,left.y + 430 * scale,left.width - 10 * scale,24 * scale), "JUGADORES " + (players?.Length ?? 0) + "/4", playerHeader);
        if(players!=null)for(int i=0;i<players.Length && i<4;i++){
            var player=players[i];
            string playerName=string.IsNullOrWhiteSpace(player.displayName)?"Escribiendo nombre…":player.displayName;
            string playerState=player.connectionState=="DISCONNECTED"?"DESCONECTADO":player.inputReady?"LISTO":"CONECTADO";
            GUI.Label(new Rect(left.x + 5 * scale,left.y + (458 + i*25) * scale,left.width - 10 * scale,24 * scale), playerName + "  ·  " + playerState, playerLine);
        }

        Rect qrCard = new Rect(right.x, right.y + 2 * scale, right.width, right.width + 62 * scale);
        GUI.Box(qrCard, GUIContent.none, PanelStyle(whiteCard, 22));
        float qrPad = 15 * scale;
        Rect qrRect = new Rect(qrCard.x + qrPad, qrCard.y + qrPad, qrCard.width - qrPad * 2, qrCard.width - qrPad * 2);
        if (qrTexture != null)
            GUI.DrawTexture(qrRect, qrTexture, ScaleMode.ScaleToFit, false);
        else
        {
            // Radar scanline effect over QR area
            float scanCycle = Mathf.Repeat(time * 0.85f, 1f);
            float scanY = qrRect.y + scanCycle * (qrRect.height - 4 * scale);
            float glowHeight = 24 * scale;

            Color prev = GUI.color;
            GUI.color = new Color(.16f, .94f, .84f, .15f);
            GUI.DrawTexture(new Rect(qrRect.x, Mathf.Max(qrRect.y, scanY - glowHeight), qrRect.width, glowHeight), cyanPixel);
            GUI.color = new Color(.16f, .94f, .84f, .85f);
            GUI.DrawTexture(new Rect(qrRect.x, scanY, qrRect.width, Mathf.Max(2, 3 * scale)), cyanPixel);
            GUI.color = prev;

            float pulse = (Mathf.Sin(time * 3.5f) + 1f) * .5f;
            Color loadingColor = Color.Lerp(new Color(.05f, .22f, .22f), new Color(.12f, .55f, .50f), pulse);
            string dots = new string('.', 1 + ((int)(time * 2f) % 3));
            GUI.Label(qrRect, "CONECTANDO CON\nLA SELVA" + dots + "\n\nPREPARANDO QR", Label(Mathf.RoundToInt(16 * scale), loadingColor, FontStyle.Bold, TextAnchor.MiddleCenter));
        }
        GUI.Label(new Rect(qrCard.x + 5 * scale, qrCard.yMax - 52 * scale, qrCard.width - 10 * scale, 40 * scale), (services.AdmissionMode == "CLOUDFLARED" ? "ESCANEA PARA JUGAR" : showingPreparation ? "PASO 1: PREPARAR" : "PASO 2: ESCANEAR PARA JUGAR"), Label(Mathf.RoundToInt(14 * scale), new Color(.04f, .16f, .16f), FontStyle.Bold, TextAnchor.MiddleCenter));

        float buttonY = shell.yMax - pad - 50 * scale;
        GUIStyle copyButton = ButtonStyle(softCard, softCard, Color.white, Mathf.RoundToInt(15 * scale));
        bool namedPlayer = players!=null && System.Array.Exists(players, p=>p!=null && p.connectionState!="DISCONNECTED" && !string.IsNullOrWhiteSpace(p.displayName));
        bool canEnter = session.PhoneConnected && namedPlayer;
        GUIStyle continueButton = canEnter
            ? ButtonStyle(goldButton, cyanButtonHover, new Color(.035f, .12f, .11f), Mathf.RoundToInt(16 * scale))
            : ButtonStyle(disabledButton, disabledButton, new Color(.68f, .76f, .74f), Mathf.RoundToInt(15 * scale));
        if (services.AdmissionMode == "CABIN" && GUI.Button(new Rect(right.x, qrCard.yMax + 10 * scale, right.width, 40 * scale), showingPreparation ? "PASO 2: UNIRSE" : "PASO 1: PREPARAR", copyButton))
            showingPreparation = !showingPreparation;
        if (GUI.Button(new Rect(left.x, buttonY, left.width, 50 * scale), services.NeedsNetworkPermission ? "PERMITIR RED LOCAL" : "REINTENTAR CONEXIÓN", copyButton))
        {
            if (services.NeedsNetworkPermission) services.InstallNetwork();
            else services.RestartModule();
        }
        if (GUI.Button(new Rect(right.x, buttonY, right.width, 50 * scale), canEnter ? "ENTRAR A LA SELVA" : session.PhoneConnected ? "ESPERANDO NOMBRE" : "ESPERANDO CELULAR", continueButton) && canEnter)
            SceneNavigator.Load(SceneNavigator.MainMenu);

        DrawForegroundLeaves(w, h, time, scale);
    }

    private void DrawBackground(float w, float h, float time)
    {
        float drift = Mathf.Sin(time * .17f) * 5f;
        if (background != null) GUI.DrawTexture(new Rect(-8 + drift, -6, w + 16, h + 12), background, ScaleMode.ScaleAndCrop, true);
        else GUI.DrawTexture(new Rect(0, 0, w, h), softCard);
        GUI.DrawTexture(new Rect(0, 0, w, h), SolidOverlay);
    }

    private Texture2D SolidOverlay
    {
        get
        {
            if (_solidOverlay == null) _solidOverlay = Solid(new Color(.01f, .055f, .065f, .20f));
            return _solidOverlay;
        }
    }
    private Texture2D _solidOverlay;

    private void DrawWind(float w, float h, float time, float scale)
    {
        Color previous = GUI.color;
        for (int i = 0; i < 5; i++)
        {
            float x = Mathf.Repeat(time * (34 + i * 5) + i * 310, w + 420) - 260;
            float y = h * (.15f + i * .115f) + Mathf.Sin(time * .7f + i) * 13 * scale;
            float length = (90 + i * 22) * scale;
            GUI.color = new Color(.68f, 1f, .95f, .07f + i * .008f);
            GUI.DrawTexture(new Rect(x, y, length, Mathf.Max(1, 2 * scale)), cyanPixel);
            GUI.DrawTexture(new Rect(x + length + 13 * scale, y, 24 * scale, Mathf.Max(1, 2 * scale)), cyanPixel);
        }
        GUI.color = previous;
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
        Matrix4x4 previous = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, pivot);
        GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true);
        GUI.matrix = previous;
    }

    private void DrawStatus(Rect rect, string label, string value, bool ready, GUIStyle style, float scale)
    {
        GUI.Box(rect, GUIContent.none, PanelStyle(softCard, 18));
        GUI.DrawTexture(new Rect(rect.x + 15 * scale, rect.y + 16 * scale, 16 * scale, 16 * scale), ready ? greenPixel : goldPixel);
        GUI.Label(new Rect(rect.x + 43 * scale, rect.y, rect.width * .45f, rect.height), label, style);
        GUIStyle valueStyle = new GUIStyle(style) { alignment = TextAnchor.MiddleRight };
        valueStyle.normal.textColor = ready ? new Color(.35f, 1f, .63f) : new Color(1f, .84f, .30f);
        GUI.Label(new Rect(rect.x + rect.width * .44f, rect.y, rect.width * .51f, rect.height), value, valueStyle);
    }

    private static GUIStyle PanelStyle(Texture2D texture, int border)
    {
        var style = new GUIStyle(GUI.skin.box);
        style.normal.background = texture;
        style.border = new RectOffset(border, border, border, border);
        return style;
    }

    private static GUIStyle Label(int size, Color color, FontStyle weight, TextAnchor alignment)
    {
        var style = new GUIStyle(GUI.skin.label)
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
        var style = new GUIStyle(GUI.skin.button)
        {
            font = JungleUITheme.TextFont,
            fontSize = size,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            border = new RectOffset(18, 18, 18, 18)
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
        if (qrTexture != null) Destroy(qrTexture);
        Destroy(darkGlass); Destroy(softCard); Destroy(whiteCard);
        Destroy(cyanButton); Destroy(cyanButtonHover); Destroy(goldButton); Destroy(disabledButton);
        Destroy(cyanPixel); Destroy(goldPixel); Destroy(greenPixel); Destroy(shadowPixel);
        if (_solidOverlay != null) Destroy(_solidOverlay);
    }
}
