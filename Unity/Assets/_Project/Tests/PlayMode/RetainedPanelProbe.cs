using UnityEngine;
[RequireComponent(typeof(JunglePanel))]
public sealed class RetainedPanelProbe : MonoBehaviour, IJungleScreen
{
    public int Renders, Activations;
    public bool DuplicateFrame, ButtonEnabled = true;
    int lastFrame = -1;
    public void RenderUI()
    {
        DuplicateFrame |= lastFrame == Time.frameCount; lastFrame = Time.frameCount; Renders++;
        JungleRetainedUI.Label(new Rect(0,0,240,40), "Retained UI");
        JungleRetainedUI.enabled = ButtonEnabled;
        if (JungleRetainedUI.Button(new Rect(0,50,240,50), "Continue")) Activations++;
    }
}
