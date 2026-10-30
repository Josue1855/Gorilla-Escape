using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

// Each screen owns a persistent visual tree. Presentation runs once per frame;
// Toolkit owns painting, batching, pointer dispatch and keyboard navigation.
[DefaultExecutionOrder(20000)]
public sealed class JunglePanel : MonoBehaviour
{
    UIDocument document;
    PanelSettings settings;
    static PanelSettings sharedSettings;
    static int panelUsers;
    readonly List<MonoBehaviour> screens = new List<MonoBehaviour>();
    readonly List<JungleRetainedUI.Node> nodes = new List<JungleRetainedUI.Node>();
    void OnEnable()
    {
        if (sharedSettings == null)
        {
            sharedSettings = ScriptableObject.CreateInstance<PanelSettings>();
            sharedSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
        }
        settings = sharedSettings;
        panelUsers++;
        document = GetComponent<UIDocument>();
        if (document == null) document = gameObject.AddComponent<UIDocument>();
        document.panelSettings = settings;
        document.enabled = true;
        document.rootVisualElement.Clear();
        document.rootVisualElement.pickingMode = PickingMode.Ignore;
        document.rootVisualElement.style.position = Position.Absolute;
        document.rootVisualElement.style.left = 0;
        document.rootVisualElement.style.top = 0;
        document.rootVisualElement.style.width = Length.Percent(100);
        document.rootVisualElement.style.height = Length.Percent(100);
        GetComponents(screens);
        // Session overlays must remain above scene HUDs, including after scene changes.
        foreach (var screen in screens)
            if (screen.GetType().Name == "MotionSession") document.sortingOrder = 100;
    }
    void LateUpdate()
    {
        if (document == null) return;
        GetComponents(screens);
        bool modal = false;
        bool overlayPanel = false;
        foreach (var screen in screens) if (screen is IJungleModalScreen overlay) { overlayPanel = true; if (screen.isActiveAndEnabled && overlay.BlocksSceneInput) modal = true; }
        document.sortingOrder = overlayPanel ? 100 : 0;
        // HUD and session overlays share a panel: an empty fullscreen overlay must
        // not win panel picking and swallow input intended for scene buttons.
        for (var ancestor = document.rootVisualElement.parent; ancestor != null; ancestor = ancestor.parent)
            ancestor.pickingMode = PickingMode.Ignore;
        document.rootVisualElement.pickingMode = modal ? PickingMode.Position : PickingMode.Ignore;
        JungleRetainedUI.Begin(document.rootVisualElement, nodes);
        try
        {
            foreach (var screen in screens)
                if (screen != null && screen.isActiveAndEnabled && screen is IJungleScreen view) view.RenderUI();
        }
        finally { JungleRetainedUI.End(); }
    }
    void OnDisable()
    {
        nodes.Clear();
        // Keep the component through disable/re-enable and assembly reload. Destroy is
        // deferred, so adding its replacement in OnEnable can otherwise fail.
        if (document != null)
        {
            document.rootVisualElement?.Clear();
            document.enabled = false;
            document.panelSettings = null;
        }
        if (settings != null && --panelUsers == 0)
        {
            Destroy(sharedSettings);
            sharedSettings = null;
        }
        settings = null;
    }
}
