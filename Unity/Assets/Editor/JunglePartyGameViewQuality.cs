using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class JunglePartyGameViewQuality
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static double enforceUntil;

    static JunglePartyGameViewQuality()
    {
        StartEnforcing(15d);
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode || state == PlayModeStateChange.EnteredPlayMode)
            StartEnforcing(5d);
    }

    private static void StartEnforcing(double seconds)
    {
        enforceUntil = EditorApplication.timeSinceStartup + seconds;
        EditorApplication.update -= EnforceNativeGameView;
        EditorApplication.update += EnforceNativeGameView;
    }

    private static void EnforceNativeGameView()
    {
        Type gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        if (gameViewType != null)
        {
            UnityEngine.Object[] views = Resources.FindObjectsOfTypeAll(gameViewType);
            foreach (UnityEngine.Object view in views)
            {
                object zoomArea = gameViewType.GetField("m_ZoomArea", Flags)?.GetValue(view);
                if (zoomArea == null) continue;

                Type zoomType = zoomArea.GetType();
                zoomType.GetField("m_ScaleWithWindow", Flags)?.SetValue(zoomArea, true);

                PropertyInfo scaleProperty = zoomType.GetProperty("scale", Flags);
                if (scaleProperty != null && scaleProperty.CanWrite)
                    scaleProperty.SetValue(zoomArea, Vector2.one);
                else
                    zoomType.GetField("m_Scale", Flags)?.SetValue(zoomArea, Vector2.one);

                if (view is EditorWindow window) window.Repaint();
            }
        }

        if (EditorApplication.timeSinceStartup >= enforceUntil)
            EditorApplication.update -= EnforceNativeGameView;
    }
}
