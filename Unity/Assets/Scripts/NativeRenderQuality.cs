using UnityEngine;

public static class NativeRenderQuality
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
        // Evita que Unity dibuje a una resolución interna menor y luego amplíe el frame.
        ScalableBufferManager.ResizeBuffers(1f, 1f);
        QualitySettings.antiAliasing = 8;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        QualitySettings.globalTextureMipmapLimit = 0;
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = 60;
        Camera.onPreCull -= DisableDynamicResolution;
        Camera.onPreCull += DisableDynamicResolution;
    }

    private static void DisableDynamicResolution(Camera camera)
    {
        if (camera != null) camera.allowDynamicResolution = false;
    }
}
