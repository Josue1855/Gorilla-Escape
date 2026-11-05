using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public interface IJungleScreen { void RenderUI(); }
public interface IJungleModalScreen { bool BlocksSceneInput { get; } }

// Style/rectangle compatibility keeps existing art and layout while rendering entirely
// through UI Toolkit. GUIStyle is read as data; Unity IMGUI is never invoked.
public static class JungleRetainedUI
{
    public sealed class StyleDefaults
    {
        public GUIStyle label = new GUIStyle { normal = { textColor = Color.white } };
        public GUIStyle button = new GUIStyle { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
        public GUIStyle box = new GUIStyle();
        public Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
    public sealed class Node
    {
        public VisualElement element;
        public string kind, identity;
        public bool clicked, hovered, focused;
        public string clickedIdentity;
        public Rect rect, clickedRect;
        public GUIStyle style;
        public Color tint;
        public int lastActivationFrame = -10;
    }
    public static readonly StyleDefaults skin = new StyleDefaults();
    public static Color color = Color.white;
    public static bool enabled = true;
    public static int depth;
    public static Matrix4x4 matrix = Matrix4x4.identity;
    static VisualElement root;
    static List<Node> pool;
    static int index;
    static Node mousePressedNode;
    static string mousePressedIdentity;
    static Rect mousePressedRect;
    static int mousePressFrame = -1;
    public static void Begin(VisualElement target, List<Node> nodes)
    {
        root = target; pool = nodes; index = 0;
        color = Color.white; enabled = true; depth = 0; matrix = Matrix4x4.identity;
        if (Input.GetMouseButtonDown(0) && mousePressFrame != Time.frameCount)
        { mousePressFrame = Time.frameCount; mousePressedNode = null; }
    }
    public static void End()
    {
        for (int i = index; i < pool.Count; i++)
        {
            pool[i].element.style.display = DisplayStyle.None;
            pool[i].clicked = false;
        }
        root = null; pool = null;
    }
    static Node Acquire(string kind, Rect rect, string identity = "")
    {
        if (root == null) throw new InvalidOperationException("UI presentation requires a JunglePanel scope.");
        Node node;
        if (index == pool.Count) { node = new Node(); pool.Add(node); }
        else node = pool[index];
        index++;
        if (node.kind != kind)
        {
            node.element?.RemoveFromHierarchy();
            node.kind = kind; node.clicked = node.hovered = false;
            node.element = kind == "image" ? (VisualElement)new Image() : kind == "button" ? new Button() : new Label();
            node.element.style.position = Position.Absolute;
            node.element.style.marginLeft = node.element.style.marginRight = 0;
            node.element.style.marginTop = node.element.style.marginBottom = 0;
            node.element.style.borderLeftWidth = node.element.style.borderRightWidth = 0;
            node.element.style.borderTopWidth = node.element.style.borderBottomWidth = 0;
            node.element.style.backgroundColor = Color.clear;
            node.element.style.minWidth = node.element.style.minHeight = 0;
            node.element.pickingMode = kind == "button" ? PickingMode.Position : PickingMode.Ignore;
            if (node.element is Button button)
            {
                button.clicked += () => {
                    if (Time.frameCount <= node.lastActivationFrame + 1) return;
                    node.clicked = true; node.clickedIdentity = node.identity; node.clickedRect = node.rect;
                };
                button.RegisterCallback<PointerEnterEvent>(_ => node.hovered = true);
                button.RegisterCallback<PointerLeaveEvent>(_ => node.hovered = false);
                button.RegisterCallback<FocusInEvent>(_ => node.focused = true);
                button.RegisterCallback<FocusOutEvent>(_ => node.focused = false);
            }
            root.Insert(Mathf.Min(index - 1, root.childCount), node.element);
        }
        node.identity = identity; node.rect = rect;
        var element = node.element;
        element.style.display = DisplayStyle.Flex;
        element.SetEnabled(kind != "button" || enabled);
        Vector3 point = matrix.MultiplyPoint3x4(new Vector3(rect.x, rect.y, 0));
        Vector3 axis = matrix.MultiplyVector(Vector3.right);
        element.style.left = point.x; element.style.top = point.y;
        element.style.width = Mathf.Max(0, rect.width * axis.magnitude);
        element.style.height = Mathf.Max(0, rect.height * matrix.MultiplyVector(Vector3.up).magnitude);
        element.style.transformOrigin = new TransformOrigin(0, 0, 0);
        element.style.rotate = new Rotate(new Angle(Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg));
        return node;
    }
    static void Apply(Node node, GUIStyle style)
    {
        style = style ?? skin.label;
        var s = node.element.style;
        var state = (node.hovered || node.focused) && enabled ? style.hover : style.normal;
        // Some imported styles omit hover fields.
        Texture2D background = state.background != null ? state.background : style.normal.background;
        s.backgroundImage = background == null ? StyleKeyword.None : new StyleBackground(background);
        s.unityBackgroundImageTintColor = color;
        s.unitySliceLeft = style.border.left; s.unitySliceRight = style.border.right;
        s.unitySliceTop = style.border.top; s.unitySliceBottom = style.border.bottom;
        s.color = (node.hovered && state.textColor.a <= 0 ? style.normal.textColor : state.textColor) * color;
        s.fontSize = style.fontSize > 0 ? style.fontSize : 16;
        s.unityFont = style.font != null ? style.font : skin.font;
        s.unityFontStyleAndWeight = style.fontStyle;
        s.unityTextAlign = style.alignment;
        s.whiteSpace = style.wordWrap ? WhiteSpace.Normal : WhiteSpace.NoWrap;
        s.overflow = Overflow.Hidden;
        s.paddingLeft = style.padding.left; s.paddingRight = style.padding.right;
        s.paddingTop = style.padding.top; s.paddingBottom = style.padding.bottom;
        node.style = style; node.tint = color;
    }
    public static void Label(Rect rect, string text, GUIStyle style = null)
    {
        var node = Acquire("label", rect); ((Label)node.element).text = text; Apply(node, style);
    }
    public static void Box(Rect rect, GUIContent content, GUIStyle style = null) => Box(rect, content.text, style);
    public static void Box(Rect rect, string text, GUIStyle style = null) => Label(rect, text, style ?? skin.box);
    public static bool Button(Rect rect, GUIContent content, GUIStyle style = null) => Button(rect, content.text, style);
    static int lastMouseReleaseFrame = -1;
    public static bool Button(Rect rect, string text, GUIStyle style = null)
    {
        var node = Acquire("button", rect, text);
        ((Button)node.element).text = text; Apply(node, style ?? skin.button);
        bool clicked = enabled && node.clicked && node.clickedIdentity == text && node.clickedRect == rect;
        node.clicked = false;

        // The Editor's legacy input path also activates on release, never on press.
        // Merge it with Toolkit delivery so one gesture cannot toggle twice.
        Vector2 mouse = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        bool picked = enabled && node.element.panel != null && node.element.panel.Pick(mouse) == node.element;
        if (picked && Input.GetMouseButtonDown(0))
        { mousePressedNode = node; mousePressedIdentity = text; mousePressedRect = rect; }
        if (mousePressedNode == node && Input.GetMouseButtonUp(0))
        {
            if (picked && mousePressedIdentity == text && mousePressedRect == rect && lastMouseReleaseFrame != Time.frameCount)
            { lastMouseReleaseFrame = Time.frameCount; clicked = true; }
            mousePressedNode = null;
        }
        if (clicked)
        {
            if (Time.frameCount <= node.lastActivationFrame + 1) return false;
            node.lastActivationFrame = Time.frameCount;
        }
        return clicked;
    }
    public static void DrawTexture(Rect rect, Texture texture, ScaleMode mode = ScaleMode.StretchToFill, bool alphaBlend = true)
    {
        var node = Acquire("image", rect);
        var image = (Image)node.element;
        image.image = texture; image.scaleMode = mode; image.tintColor = color; image.uv = new Rect(0, 0, 1, 1);
    }
    public static void DrawTextureWithTexCoords(Rect rect, Texture texture, Rect uv, bool alphaBlend = true)
    {
        DrawTexture(rect, texture); ((Image)pool[index - 1].element).uv = uv;
    }
    public static void RotateAroundPivot(float angle, Vector2 pivot)
    {
        matrix = matrix * Matrix4x4.Translate(pivot) * Matrix4x4.Rotate(Quaternion.Euler(0, 0, angle)) * Matrix4x4.Translate(-pivot);
    }
}
