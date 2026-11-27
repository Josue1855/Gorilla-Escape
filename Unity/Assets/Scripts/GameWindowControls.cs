using UnityEngine;

public sealed class GameWindowControls : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        Resolution desktop = Screen.currentResolution;
        Screen.SetResolution(desktop.width, desktop.height, FullScreenMode.FullScreenWindow);
#endif
    }

    private static Rect ExitButtonRect()
    {
        float width = Mathf.Clamp(Screen.width * .18f, 160f, 280f);
        float height = Mathf.Clamp(Screen.height * .065f, 42f, 64f);
        return new Rect(Screen.width - width - 18f, Screen.height - height - 18f, width, height);
    }

    public static bool ExitHovered(PhoneMenuPointer pointer)
    {
        if (pointer == null || !pointer.Connected) return false;
        Vector2 position = new Vector2(pointer.Position.x * Screen.width, (1f - pointer.Position.y) * Screen.height);
        return ExitButtonRect().Contains(position);
    }

    public static void DrawExitButton(PhoneMenuPointer pointer)
    {
        GUIStyle style = ExitHovered(pointer) ? JungleUITheme.ButtonSelected : JungleUITheme.Button;
        if (GUI.Button(ExitButtonRect(), "SALIR DEL JUEGO", style)) QuitGame();
    }

    public static void QuitGame()
    {
        if (PortableServiceHost.Instance != null) PortableServiceHost.Instance.StopModule();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
