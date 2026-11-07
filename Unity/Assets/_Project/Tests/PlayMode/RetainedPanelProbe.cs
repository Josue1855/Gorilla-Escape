using UnityEngine;
[RequireComponent(typeof(JunglePanel))]
public sealed class RetainedPanelProbe : MonoBehaviour, IJungleScreen
{
    public int Renders, Activations;
    public bool DuplicateFrame, ButtonEnabled = true, ToggleLabel;
    int lastFrame = -1;
    public void RenderUI()
    {
        DuplicateFrame |= lastFrame == Time.frameCount; lastFrame = Time.frameCount; Renders++;
        JungleRetainedUI.Label(new Rect(0,0,240,40), "Retained UI");
        JungleRetainedUI.enabled = ButtonEnabled;
        if (JungleRetainedUI.Button(new Rect(0,50,240,50), ToggleLabel && Activations % 2 == 1 ? "Resume" : "Continue")) Activations++;
    }
}
