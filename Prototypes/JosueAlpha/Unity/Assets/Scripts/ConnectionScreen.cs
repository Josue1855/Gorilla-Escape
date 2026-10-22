using UnityEngine;

public sealed class ConnectionScreen : MonoBehaviour
{
    private MotionSession session;
    private PortableServiceHost services;
    private void Awake()
    {
        session = MotionSession.Ensure();
        services = PortableServiceHost.Ensure();
        session.Connect();
    }
    private void OnGUI()
    {
        JungleUITheme.Ensure();JungleUITheme.DrawBackdrop(true);
        float w = Screen.width, h = Screen.height;
        GUIStyle title = new GUIStyle(JungleUITheme.Title) { fontSize = Mathf.RoundToInt(h * .065f) };
        GUIStyle text = new GUIStyle(JungleUITheme.Heading) { fontSize = Mathf.RoundToInt(h * .032f) };
        JungleUITheme.DrawPanel(new Rect(w*.16f,h*.14f,w*.55f,h*.70f));
        GUI.Label(new Rect(w*.25f,h*.20f,w*.50f,h*.10f), "JUNGLE PARTY", title);
        GUI.Label(new Rect(w*.28f,h*.34f,w*.44f,h*.10f), "CÓDIGO  " + session.RoomCode, title);
        GUI.Label(new Rect(w*.28f,h*.43f,w*.44f,h*.07f), services.Status, text);
        GUI.Label(new Rect(w*.28f,h*.50f,w*.44f,h*.07f), session.Status, text);
        GUI.Label(new Rect(w*.28f,h*.57f,w*.44f,h*.07f), session.CameraConnected ? "CÁMARA LISTA" : "PREPARANDO CÁMARA", text);
        if (!string.IsNullOrEmpty(services.PublicControllerUrl) && GUI.Button(new Rect(w*.27f,h*.64f,w*.32f,h*.06f), "COPIAR ENLACE DEL CELULAR", JungleUITheme.Button))
            GUIUtility.systemCopyBuffer = services.PublicControllerUrl;
        if (session.PhoneConnected && GUI.Button(new Rect(w*.27f,h*.73f,w*.32f,h*.075f), "CONTINUAR", JungleUITheme.ButtonSelected)) SceneNavigator.Load(SceneNavigator.MainMenu);
    }
}
