using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public sealed class ConnectionScreen : MonoBehaviour
{
    private MotionSession session;
    private PortableServiceHost services;
    private Texture2D qrTexture;
    private Texture2D background;
    private Texture2D overlay;
    private Texture2D panel;
    private Texture2D card;
    private Texture2D white;
    private Texture2D cyan;
    private Texture2D yellow;
    private Texture2D green;
    private Texture2D muted;
    private string requestedQrUrl;

    private void Awake()
    {
        session = MotionSession.Ensure();
        services = PortableServiceHost.Ensure();
        session.Connect();
        background = Resources.Load<Texture2D>("Jungle/gorila_menu");
        if (background != null) { background.filterMode = FilterMode.Bilinear; background.wrapMode = TextureWrapMode.Clamp; }
        overlay = Solid(new Color(.015f, .045f, .065f, .48f));
        panel = Solid(new Color(.025f, .075f, .09f, .96f));
        card = Solid(new Color(.045f, .12f, .13f, 1f));
        white = Solid(new Color(.98f, .99f, .97f, 1f));
        cyan = Solid(new Color(.08f, .84f, .82f, 1f));
        yellow = Solid(new Color(1f, .79f, .12f, 1f));
        green = Solid(new Color(.18f, .88f, .48f, 1f));
        muted = Solid(new Color(.27f, .39f, .40f, 1f));
    }

    private static Texture2D Solid(Color color)
    {
        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private void Update()
    {
        string controllerUrl = services.PublicControllerUrl;
        if (string.IsNullOrEmpty(controllerUrl) || controllerUrl == requestedQrUrl) return;
        requestedQrUrl = controllerUrl;
        StartCoroutine(DownloadQr(controllerUrl));
    }

    private IEnumerator DownloadQr(string controllerUrl)
    {
        string endpoint = "https://api.qrserver.com/v1/create-qr-code/?size=400x400&margin=8&data=" +
            UnityWebRequest.EscapeURL(controllerUrl);
        using UnityWebRequest request = UnityWebRequestTexture.GetTexture(endpoint, true);
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
        {
            if (qrTexture != null) Destroy(qrTexture);
            qrTexture = DownloadHandlerTexture.GetContent(request);
            qrTexture.filterMode = FilterMode.Point;
            qrTexture.wrapMode = TextureWrapMode.Clamp;
            qrTexture.anisoLevel = 0;
        }
        else Debug.LogWarning("No se pudo generar el QR: " + request.error);
    }

    private void OnGUI()
    {
        float w = Screen.width;
        float h = Screen.height;
        float scale = Mathf.Clamp(h / 1080f, .72f, 1.7f);

        if (background != null) GUI.DrawTexture(new Rect(0, 0, w, h), background, ScaleMode.ScaleAndCrop, true);
        else GUI.DrawTexture(new Rect(0, 0, w, h), panel);
        GUI.DrawTexture(new Rect(0, 0, w, h), overlay);

        Rect shell = new Rect(w * .055f, h * .285f, w * .535f, h * .63f);
        GUI.DrawTexture(new Rect(shell.x + 14 * scale, shell.y + 16 * scale, shell.width, shell.height), overlay);
        GUI.DrawTexture(shell, panel);
        GUI.DrawTexture(new Rect(shell.x, shell.y, 8 * scale, shell.height), yellow);
        GUI.DrawTexture(new Rect(shell.x + 30 * scale, shell.y + 30 * scale, shell.width - 60 * scale, 2 * scale), cyan);

        GUIStyle eyebrow = Label(Mathf.RoundToInt(17 * scale), new Color(.38f, .95f, .91f), FontStyle.Bold, TextAnchor.MiddleLeft);
        GUIStyle heading = Label(Mathf.RoundToInt(36 * scale), Color.white, FontStyle.Bold, TextAnchor.MiddleLeft);
        GUIStyle codeStyle = Label(Mathf.RoundToInt(60 * scale), new Color(1f, .82f, .18f), FontStyle.Bold, TextAnchor.MiddleLeft);
        GUIStyle body = Label(Mathf.RoundToInt(18 * scale), new Color(.78f, .86f, .84f), FontStyle.Normal, TextAnchor.MiddleLeft);
        GUIStyle status = Label(Mathf.RoundToInt(17 * scale), Color.white, FontStyle.Bold, TextAnchor.MiddleLeft);
        GUIStyle qrCaption = Label(Mathf.RoundToInt(17 * scale), new Color(.03f, .10f, .11f), FontStyle.Bold, TextAnchor.MiddleCenter);

        float left = shell.x + 46 * scale;
        float top = shell.y + 47 * scale;
        float columnWidth = shell.width * .49f;
        GUI.Label(new Rect(left, top, columnWidth, 28 * scale), "CONEXIÓN DEL JUGADOR", eyebrow);
        GUI.Label(new Rect(left, top + 30 * scale, columnWidth, 58 * scale), "Conecta tu celular", heading);
        GUI.Label(new Rect(left, top + 91 * scale, columnWidth, 45 * scale), "Escanea el QR y escribe este código:", body);
        GUI.Label(new Rect(left, top + 132 * scale, columnWidth, 80 * scale), session.RoomCode, codeStyle);

        DrawStatus(new Rect(left, top + 230 * scale, columnWidth, 50 * scale), "CÁMARA", session.CameraConnected ? "LISTA" : "PREPARANDO", session.CameraConnected, status, scale);
        DrawStatus(new Rect(left, top + 292 * scale, columnWidth, 50 * scale), "CELULAR", session.PhoneConnected ? "CONECTADO" : "ESPERANDO", session.PhoneConnected, status, scale);
        GUI.Label(new Rect(left, top + 350 * scale, columnWidth, 55 * scale), services.Status, body);

        float qrCardSize = Mathf.Min(shell.height * .63f, shell.width * .35f);
        float qrX = shell.xMax - qrCardSize - 47 * scale;
        float qrY = shell.y + 63 * scale;
        GUI.DrawTexture(new Rect(qrX - 13 * scale, qrY - 13 * scale, qrCardSize + 26 * scale, qrCardSize + 78 * scale), white);
        if (qrTexture != null)
            GUI.DrawTexture(new Rect(qrX, qrY, qrCardSize, qrCardSize), qrTexture, ScaleMode.ScaleToFit, false);
        else
            GUI.Label(new Rect(qrX, qrY, qrCardSize, qrCardSize), "GENERANDO\nENLACE SEGURO…", Label(Mathf.RoundToInt(20 * scale), new Color(.08f, .24f, .24f), FontStyle.Bold, TextAnchor.MiddleCenter));
        GUI.Label(new Rect(qrX - 4 * scale, qrY + qrCardSize + 8 * scale, qrCardSize + 8 * scale, 42 * scale), "ESCANEA CON TU CÁMARA", qrCaption);

        GUIStyle secondaryButton = Button(card, Color.white, Mathf.RoundToInt(18 * scale));
        GUIStyle primaryButton = Button(session.PhoneConnected ? yellow : muted, session.PhoneConnected ? new Color(.07f, .10f, .08f) : new Color(.68f, .73f, .71f), Mathf.RoundToInt(20 * scale));
        float buttonY = shell.yMax - 70 * scale;
        if (!string.IsNullOrEmpty(services.PublicControllerUrl) && GUI.Button(new Rect(left, buttonY, columnWidth, 48 * scale), "COPIAR ENLACE", secondaryButton))
            GUIUtility.systemCopyBuffer = services.PublicControllerUrl;
        if (GUI.Button(new Rect(qrX - 13 * scale, buttonY, qrCardSize + 26 * scale, 48 * scale), session.PhoneConnected ? "CONTINUAR" : "ESPERANDO CELULAR", primaryButton) && session.PhoneConnected)
            SceneNavigator.Load(SceneNavigator.MainMenu);
    }

    private void DrawStatus(Rect rect, string label, string value, bool ready, GUIStyle style, float scale)
    {
        GUI.DrawTexture(rect, card);
        GUI.DrawTexture(new Rect(rect.x + 14 * scale, rect.y + 17 * scale, 16 * scale, 16 * scale), ready ? green : yellow);
        GUI.Label(new Rect(rect.x + 42 * scale, rect.y, rect.width * .48f, rect.height), label, style);
        GUIStyle valueStyle = new GUIStyle(style) { alignment = TextAnchor.MiddleRight };
        valueStyle.normal.textColor = ready ? new Color(.35f, 1f, .63f) : new Color(1f, .84f, .30f);
        GUI.Label(new Rect(rect.x + rect.width * .48f, rect.y, rect.width * .47f, rect.height), value, valueStyle);
    }

    private static GUIStyle Label(int size, Color color, FontStyle weight, TextAnchor alignment)
    {
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = size,
            fontStyle = weight,
            alignment = alignment,
            wordWrap = true,
            clipping = TextClipping.Clip
        };
        style.normal.textColor = color;
        return style;
    }

    private static GUIStyle Button(Texture2D texture, Color textColor, int size)
    {
        var style = new GUIStyle(GUI.skin.button)
        {
            fontSize = size,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            border = new RectOffset(4, 4, 4, 4)
        };
        style.normal.background = texture;
        style.hover.background = texture;
        style.active.background = texture;
        style.normal.textColor = textColor;
        style.hover.textColor = textColor;
        style.active.textColor = textColor;
        return style;
    }

    private void OnDestroy()
    {
        if (qrTexture != null) Destroy(qrTexture);
        Destroy(overlay); Destroy(panel); Destroy(card); Destroy(white);
        Destroy(cyan); Destroy(yellow); Destroy(green); Destroy(muted);
    }
}
