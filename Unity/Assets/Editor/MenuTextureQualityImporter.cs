using UnityEditor;

public sealed class MenuTextureQualityImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.EndsWith("gorila_menu_hd.png")) return;
        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Default;
        importer.maxTextureSize = 4096;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = UnityEngine.FilterMode.Bilinear;
        importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
        importer.sRGBTexture = true;
    }
}
