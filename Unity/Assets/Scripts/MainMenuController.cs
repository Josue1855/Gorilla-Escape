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

        string[] names = { "BANANA\nBOWLING", "BOXEO\nCON GORILA", "JUNGLE\nBASEBALL", "JUNGLE\nDRUMS", "COCONUT\nSLICE", "GORILLA\nWHACK", "DESAFÍO DEL\nTRONCO" };
        string[] scenes = { SceneNavigator.Bowling, SceneNavigator.Boxing, SceneNavigator.Baseball, SceneNavigator.Drums, SceneNavigator.CoconutSlice, SceneNavigator.WhackAMole, SceneNavigator.LogBalance };
        for (int i = 0; i < names.Length; i++)
        {
            const int columns=4;
            int row=i/columns,column=i%columns;
            int count=Mathf.Min(columns,names.Length-row*columns);
            float gap=w*.022f,cardWidth=(w*.90f-gap*(columns-1))/columns;
            float rowWidth=count*cardWidth+(count-1)*gap;
            Rect card=new Rect((w-rowWidth)/2+column*(cardWidth+gap),h*(.22f+row*.27f),cardWidth,h*.22f);
            JungleUITheme.DrawPanel(card);
            if (GUI.Button(new Rect(card.x + 12, card.y + 12, card.width - 24, card.height - 24), names[i], new GUIStyle(JungleUITheme.Button){fontSize=Mathf.RoundToInt(Mathf.Min(h*.035f,card.width*.095f))}))
                SceneNavigator.Load(scenes[i]);
        }

        if (GUI.Button(new Rect(w * .38f, h * .77f, w * .24f, h * .075f), "CONEXIÓN Y CALIBRACIÓN", JungleUITheme.Button))
            SceneNavigator.Load(SceneNavigator.Connection);
    }
}
