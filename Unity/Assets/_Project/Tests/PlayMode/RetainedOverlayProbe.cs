using UnityEngine;
[RequireComponent(typeof(JunglePanel))]
public sealed class RetainedOverlayProbe : MonoBehaviour, IJungleScreen, IJungleModalScreen
{
    public bool Modal;
    public bool BlocksSceneInput => Modal;
    public void RenderUI() { JungleRetainedUI.Label(new Rect(300,0,100,40), "Camera"); }
}
