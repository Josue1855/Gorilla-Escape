using UnityEngine;

public sealed class MainMenuController : MonoBehaviour
{
    private void Awake() { MotionSession.Ensure(); }

    private void OnGUI()
    {
        JungleUITheme.Ensure();
        JungleUITheme.DrawBackdrop(true);
        float w = Screen.width, h = Screen.height;
        GUIStyle title = new GUIStyle(JungleUITheme.Title) { fontSize = Mathf.RoundToInt(h * .075f) };
        GUI.Label(new Rect(w * .10f, h * .055f, w * .68f, h * .12f), "JUNGLE PARTY", title);

        string[] names = { "BANANA\nBOWLING", "BOXEO\nCON GORILA", "JUNGLE\nBASEBALL", "JUNGLE\nDRUMS", "COCONUT\nSLICE", "GORILLA\nWHACK" };
        string[] scenes = { SceneNavigator.Bowling, SceneNavigator.Boxing, SceneNavigator.Baseball, SceneNavigator.Drums, SceneNavigator.CoconutSlice, SceneNavigator.WhackAMole };
        for (int i = 0; i < names.Length; i++)
        {
            int row = i < 3 ? 0 : 1;
            int column = i < 3 ? i : i - 3;
            float startX = w * .125f;
            Rect card = new Rect(startX + column * w * .255f, h * (row == 0 ? .22f : .49f), w * .23f, h * .22f);
            JungleUITheme.DrawPanel(card);
            if (GUI.Button(new Rect(card.x + 12, card.y + 12, card.width - 24, card.height - 24), names[i], JungleUITheme.Button))
                SceneNavigator.Load(scenes[i]);
        }

        if (GUI.Button(new Rect(w * .38f, h * .77f, w * .24f, h * .075f), "CONEXIÓN Y CALIBRACIÓN", JungleUITheme.Button))
            SceneNavigator.Load(SceneNavigator.Connection);
    }
}
