using System.Collections.Generic;
using UnityEngine;

// Visual dressing for Coconut Throw, outside the marked throwing sector.
internal sealed class CoconutThrowScenery : MonoBehaviour
{
    private readonly List<Material> materials = new();

    internal static void Build(Transform parent, Renderer field)
    {
        var root = new GameObject("02 Vegetación y público");
        root.transform.SetParent(parent, false);
        var scenery = root.AddComponent<CoconutThrowScenery>();
        var grass = scenery.Material("Jungle/CoconutThrow/TropicalGrass", "Césped tropical pintado");
        grass.mainTexture.wrapMode = TextureWrapMode.Repeat;
        grass.mainTextureScale = new Vector2(10, 18);
        field.sharedMaterial = grass;

        for (int side = -1; side <= 1; side += 2)
        {
            for (int i = 0; i < 10; i++)
            {
                float z = 2f + i * 7f;
                float edge = Mathf.Max(5.8f, z * .325f + 4.5f);
                scenery.Place("arbusto3", new Vector3(side * edge, 0, z), 1.4f + i % 3 * .25f, side * 25f);
                if (i % 2 == 0)
                    scenery.Place("arbusto3", new Vector3(side * (edge + 2.2f), 0, z + 2.8f), 2.3f, 180f + i * 37f);
                if (i % 3 == 0)
                    scenery.Place("antorcha", new Vector3(side * (edge - 1.2f), 0, z + 1.5f), 2f, 0);
            }
            for (int i = 0; i < 3; i++)
                scenery.Place("montana", new Vector3(side * (43f + i * 9f), -.5f, 28f + i * 26f), 22f + i * 5f, side * (40f + i * 50f));
        }
        for (int i = -2; i <= 2; i++)
            scenery.Place("montana", new Vector3(i * 23f, -.5f, 112f + Mathf.Abs(i) * 5f), 28f + Mathf.Abs(i) * 3f, i * 43f);

        scenery.Place("sombrilla", new Vector3(-8, 0, 5), 4f, 15f);
        scenery.Place("cajaconcocos", new Vector3(-5, 0, 1.5f), 1.6f, 150f);
        scenery.Place("cajaconcocos", new Vector3(7, 0, 13), 1.4f, 205f);
        scenery.Place("gorilarojo", new Vector3(6.5f, 0, 4), 3.1f, -135f);
        const string blue = "Jungle/CoconutThrow/BlueGorilla/Meshy_AI_Frostback_Guardian_20261111_animated_mixamo_Chair_Sit_Idle_M";
        scenery.PlaceModel(blue, new Vector3(-7, 0, 8), 2.7f, 135f, "Gorila azul espectador");
    }

    private Material Material(string texturePath, string label)
    {
        var material = JungleStageBuilder.CreateTexturedMaterial(Resources.Load<Texture2D>(texturePath), label, .08f);
        materials.Add(material);
        return material;
    }

    private void Place(string asset, Vector3 position, float height, float yaw)
    {
        PlaceModel("Jungle/SplashModels/" + asset, position, height, yaw, asset);
    }

    private void PlaceModel(string path, Vector3 position, float height, float yaw, string label)
    {
        var prefab = Resources.Load<GameObject>(path);
        if (prefab == null) return;
        var model = Instantiate(prefab, transform);
        model.name = label;
        foreach (var animator in model.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
        foreach (var animation in model.GetComponentsInChildren<Animation>(true)) animation.enabled = false;
        model.transform.localRotation = Quaternion.Euler(0, yaw, 0) * prefab.transform.localRotation;
        model.transform.localScale = Vector3.one;
        JungleStageBuilder.DisableChildColliders(model);
        var renderers = model.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;
        var bounds = JungleStageBuilder.CombinedBounds(renderers);
        model.transform.localScale *= height / Mathf.Max(.01f, bounds.size.y);
        bounds = JungleStageBuilder.CombinedBounds(renderers);
        model.transform.position += position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        var texture = Resources.Load<Texture2D>(path);
        if (texture != null)
            JungleStageBuilder.ApplySharedMaterial(renderers, Material(path, label + " pintado"));
    }

    private void OnDestroy()
    {
        foreach (var material in materials) Destroy(material);
    }
}
