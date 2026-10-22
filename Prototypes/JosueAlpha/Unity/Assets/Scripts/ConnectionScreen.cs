using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public sealed class ConnectionScreen : MonoBehaviour
{
    private MotionSession session;
    private PortableServiceHost services;
    private Texture2D qrTexture;
    private string requestedQrUrl;
    private void Awake()
    {
        session = MotionSession.Ensure();
        services = PortableServiceHost.Ensure();
        session.Connect();
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
        string endpoint = "https://api.qrserver.com/v1/create-qr-code/?size=320x320&margin=8&data=" +
            UnityWebRequest.EscapeURL(controllerUrl);
        using UnityWebRequest request = UnityWebRequestTexture.GetTexture(endpoint, true);
        yield return request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.Success)
            qrTexture = DownloadHandlerTexture.GetContent(request);
        else
            Debug.LogWarning("No se pudo generar el QR: " + request.error);
    }
    private void OnGUI()
    {
        JungleUITheme.Ensure();JungleUITheme.DrawBackdrop(true);
        float w = Screen.width, h = Screen.height;
        GUIStyle title = new GUIStyle(JungleUITheme.Title) { fontSize = Mathf.RoundToInt(h * .065f) };
        GUIStyle text = new GUIStyle(JungleUITheme.Heading) { fontSize = Mathf.RoundToInt(h * .032f) };
        JungleUITheme.DrawPanel(new Rect(w*.16f,h*.14f,w*.55f,h*.70f));
        GUI.Label(new Rect(w*.25f,h*.20f,w*.50f,h*.10f), "JUNGLE PARTY", title);
        GUI.Label(new Rect(w*.28f,h*.34f,w*.27f,h*.10f), "CÓDIGO  " + session.RoomCode, title);
        GUI.Label(new Rect(w*.28f,h*.43f,w*.27f,h*.07f), services.Status, text);
        GUI.Label(new Rect(w*.28f,h*.50f,w*.27f,h*.07f), session.Status, text);
        GUI.Label(new Rect(w*.28f,h*.57f,w*.27f,h*.07f), session.CameraConnected ? "CÁMARA LISTA" : "PREPARANDO CÁMARA", text);
        if (qrTexture != null)
        {
            float size = Mathf.Min(w * .15f, h * .27f);
            GUI.DrawTexture(new Rect(w * .55f, h * .35f, size, size), qrTexture, ScaleMode.ScaleToFit, true);
            GUI.Label(new Rect(w * .54f, h * .62f, w * .18f, h * .05f), "ESCANEA PARA CONECTAR", text);
        }
        if (!string.IsNullOrEmpty(services.PublicControllerUrl) && GUI.Button(new Rect(w*.27f,h*.64f,w*.32f,h*.06f), "COPIAR ENLACE DEL CELULAR", JungleUITheme.Button))
            GUIUtility.systemCopyBuffer = services.PublicControllerUrl;
        if (session.PhoneConnected && GUI.Button(new Rect(w*.27f,h*.73f,w*.32f,h*.075f), "CONTINUAR", JungleUITheme.ButtonSelected)) SceneNavigator.Load(SceneNavigator.MainMenu);
    }

    private void OnDestroy()
    {
        if (qrTexture != null) Destroy(qrTexture);
    }
}
