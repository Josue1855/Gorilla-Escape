using System;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// Constructor compartido de escenarios 3D, materiales con GPU Instancing,
/// limpieza de colliders decorativos y utilidades del modelo 3D del gorila.
/// </summary>
public static class JungleStageBuilder
{
    public const string BowlingGorillaFolder = "Jungle/GorillaBowling/Meshy_AI_Low_Poly_Gorilla_Embr_biped/";
    public const string BowlingGorillaAsset = BowlingGorillaFolder + "Meshy_AI_Low_Poly_Gorilla_Embr_biped_Meshy_Merged_Animations";
    private readonly struct MaterialKey : IEquatable<MaterialKey>
    {
        public readonly Color32 Color;
        public readonly byte Metallic;
        public readonly byte Smoothness;
        public readonly bool StandardOnly;
        public MaterialKey(Color color, float metallic, float smoothness, bool standardOnly)
        {
            Color = color;
            Metallic = (byte)Mathf.Clamp(Mathf.RoundToInt(metallic * 255f), 0, 255);
            Smoothness = (byte)Mathf.Clamp(Mathf.RoundToInt(smoothness * 255f), 0, 255);
            StandardOnly = standardOnly;
        }
        public bool Equals(MaterialKey other) =>
            Color.r == other.Color.r &&
            Color.g == other.Color.g &&
            Color.b == other.Color.b &&
            Color.a == other.Color.a &&
            Metallic == other.Metallic &&
            Smoothness == other.Smoothness &&
            StandardOnly == other.StandardOnly;
        public override bool Equals(object obj) => obj is MaterialKey other && Equals(other);
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (Color.r << 24) | (Color.g << 16) | (Color.b << 8) | Color.a;
                hash = (hash * 397) ^ Metallic;
                hash = (hash * 397) ^ Smoothness;
                return (hash * 397) ^ (StandardOnly ? 1 : 0);
            }
        }
    }
    private static readonly Dictionary<MaterialKey, Material> sharedMaterials = new Dictionary<MaterialKey, Material>();
    private static Shader cachedLitShader;
    private static Shader cachedStandardShader;
    private static Material cachedBowlingGorillaMaterial;
    private static GameObject cachedBowlingGorillaPrefab;
    private static AnimationClip cachedGorillaIdleClip;
    private static AnimationClip cachedGorillaAngryClip;
    private static AnimationClip cachedGorillaDanceClip;
    private static bool gorillaClipsLoaded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetShadersDomain()
    {
        sharedMaterials.Clear();
        cachedLitShader = null;
        cachedStandardShader = null;
        cachedBowlingGorillaMaterial = null;
        gorillaClipsLoaded = false;
    }
    public static bool IsUniversalRenderPipeline => true;

    public static Shader LitShader =>
        cachedLitShader != null
            ? cachedLitShader
            : (cachedLitShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse"));

    public static Shader StandardShader => LitShader;
    public static Material GetSharedMaterial(Color color, float metallic = 0f, float smoothness = .25f, bool preferStandard = false)
    {
        MaterialKey key = new MaterialKey(color, metallic, smoothness, preferStandard);
        if (sharedMaterials.TryGetValue(key, out Material cached) && cached != null)
        {
            if (cached.shader == LitShader) return cached;
            sharedMaterials.Remove(key);
        }
        Shader shader = preferStandard ? StandardShader : LitShader;
        Material material = new Material(shader)
        {
            name = $"JungleShared_{key.Color.r}_{key.Color.g}_{key.Color.b}",
            color = color,
            enableInstancing = true
        };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        sharedMaterials[key] = material;
        return material;
    }
    public static Material CreateTexturedMaterial(
        Texture2D albedo,
        string materialName,
        float smoothness = .16f,
        Texture2D normalMap = null,
        float bumpScale = .72f,
        bool preferStandard = false)
    {
        Shader shader = preferStandard ? StandardShader : LitShader;
        Material material = new Material(shader)
        {
            name = materialName,
            color = Color.white,
            mainTexture = albedo,
            enableInstancing = true
        };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
        if (albedo != null)
        {
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", albedo);
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", albedo);
        }
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
        if (normalMap != null)
        {
            if (material.HasProperty("_BumpMap")) material.SetTexture("_BumpMap", normalMap);
            if (material.HasProperty("_BumpScale")) material.SetFloat("_BumpScale", bumpScale);
            material.EnableKeyword("_NORMALMAP");
        }
        return material;
    }
    public static void StripCollider(GameObject go)
    {
        if (go == null) return;
        if (go.TryGetComponent<Collider>(out Collider col))
        {
            col.enabled = false;
            UnityEngine.Object.Destroy(col);
        }
    }
    public static void DisableChildColliders(GameObject root)
    {
        if (root == null) return;
        foreach (Collider col in root.GetComponentsInChildren<Collider>(true))
            col.enabled = false;
    }
    public static GameObject CreatePrimitive(
        PrimitiveType type,
        string name,
        Vector3 position,
        Vector3 scale,
        Color color,
        Transform parent = null,
        bool keepCollider = false,
        float metallic = 0f,
        float smoothness = .25f,
        bool preferStandard = false)
    {
        GameObject item = GameObject.CreatePrimitive(type);
        item.name = name;
        if (parent != null) item.transform.SetParent(parent);
        item.transform.position = position;
        item.transform.localScale = scale;
        item.GetComponent<Renderer>().sharedMaterial = GetSharedMaterial(color, metallic, smoothness, preferStandard);
        if (!keepCollider) StripCollider(item);
        return item;
    }
    public static Bounds CombinedBounds(Renderer[] renderers)
    {
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }
    public static void ApplySharedMaterial(Renderer[] renderers, Material material)
    {
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = true;
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++) materials[i] = material;
            renderer.sharedMaterials = materials;
        }
    }
    public static GameObject InstantiateGroundedProp(
        GameObject prefab,
        Transform parent,
        Material material,
        string objectName,
        Vector3 groundPosition,
        float desiredHeight,
        float yaw)
    {
        if (prefab == null) return null;
        GameObject prop = UnityEngine.Object.Instantiate(prefab, parent);
        prop.name = objectName;
        Quaternion importedAxisCorrection = prop.transform.localRotation;
        prop.transform.position = Vector3.zero;
        prop.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * importedAxisCorrection;
        prop.transform.localScale = Vector3.one;
        DisableChildColliders(prop);
        Renderer[] renderers = prop.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return prop;
        Bounds bounds = CombinedBounds(renderers);
        prop.transform.localScale = Vector3.one * (desiredHeight / Mathf.Max(.01f, bounds.size.y));
        bounds = CombinedBounds(renderers);
        prop.transform.position += new Vector3(groundPosition.x - bounds.center.x, groundPosition.y - bounds.min.y, groundPosition.z - bounds.center.z);
        if (material != null) ApplySharedMaterial(renderers, material);
        AttachWindSway(prop, desiredHeight > 4f ? 2.6f : 3.4f, desiredHeight > 4f ? 1.35f : 1.85f);
        return prop;
    }
    public static void PulseSquash(GameObject target, float squashY = .74f, float stretchXZ = 1.20f, float frequency = 24f, float damping = 9f)
    {
        if (target == null) return;
        if (!target.TryGetComponent<JungleSquashStretch>(out var comp))
            comp = target.AddComponent<JungleSquashStretch>();
        comp.Trigger(squashY, stretchXZ, frequency, damping);
    }
    public static void AttachWindSway(GameObject target, float maxAngle = 3.0f, float speed = 1.6f)
    {
        if (target == null) return;
        if (!target.TryGetComponent<JungleWindSway>(out var sway))
            sway = target.AddComponent<JungleWindSway>();
        sway.Configure(maxAngle, speed);
    }
    public static void ShakeCamera(float intensity = .14f, float duration = .18f)
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        if (!cam.gameObject.TryGetComponent<JungleCameraShake>(out var shake))
            shake = cam.gameObject.AddComponent<JungleCameraShake>();
        shake.Trigger(intensity, duration);
    }
    public static GameObject LoadBowlingGorillaPrefab()
    {
        if (cachedBowlingGorillaPrefab == null)
            cachedBowlingGorillaPrefab = JungleAssets.Load<GameObject>(BowlingGorillaAsset);
        return cachedBowlingGorillaPrefab;
    }
    public static Material GetBowlingGorillaSharedMaterial(bool preferStandard = false)
    {
        if (cachedBowlingGorillaMaterial != null && cachedBowlingGorillaMaterial.shader == LitShader) return cachedBowlingGorillaMaterial;
        Texture2D albedo = JungleAssets.Load<Texture2D>(BowlingGorillaFolder + "Meshy_AI_Low_Poly_Gorilla_Embr_biped_texture_0");
        Texture2D normal = JungleAssets.Load<Texture2D>(BowlingGorillaFolder + "Meshy_AI_Low_Poly_Gorilla_Embr_biped_texture_0_normal");
        cachedBowlingGorillaMaterial = CreateTexturedMaterial(albedo, "Gorila Bowling - Compartido", .22f, normal, .72f, preferStandard);
        return cachedBowlingGorillaMaterial;
    }
    public static void LoadBowlingGorillaClips(out AnimationClip idle, out AnimationClip angry, out AnimationClip dance)
    {
        if (!gorillaClipsLoaded || (cachedGorillaIdleClip == null && cachedGorillaAngryClip == null && cachedGorillaDanceClip == null))
        {
            gorillaClipsLoaded = true;
            foreach (AnimationClip clip in JungleAssets.LoadAll<AnimationClip>(BowlingGorillaAsset))
            {
                if (clip == null) continue;
                if (clip.name == "Alert") cachedGorillaIdleClip = clip;
                else if (clip.name == "Angry_Ground_Stomp_2") cachedGorillaAngryClip = clip;
                else if (clip.name == "Angry_Ground_Stomp" && cachedGorillaAngryClip == null) cachedGorillaAngryClip = clip;
                else if (clip.name == "All_Night_Dance") cachedGorillaDanceClip = clip;
            }
        }
        idle = cachedGorillaIdleClip;
        angry = cachedGorillaAngryClip;
        dance = cachedGorillaDanceClip;
    }
    public static Transform FindBone(Transform root, params string[] names)
    {
        if (root == null || names == null) return null;
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int n = 0; n < names.Length; n++)
        {
            string target = names[n];
            for (int i = 0; i < all.Length; i++)
            {
                string boneName = all[i].name;
                if (boneName == target || boneName.EndsWith(":" + target, StringComparison.OrdinalIgnoreCase))
                    return all[i];
            }
        }
        return null;
    }
    public static void SolveTwoBoneArmIK(Transform upperArm, Transform foreArm, Transform hand, Vector3 targetWorld, Vector3 elbowHint)
    {
        if (upperArm == null || foreArm == null || hand == null) return;
        Vector3 shoulderPos = upperArm.position;
        Vector3 elbowPos = foreArm.position;
        Vector3 wristPos = hand.position;
        float upperLen = Vector3.Distance(shoulderPos, elbowPos);
        float foreLen = Vector3.Distance(elbowPos, wristPos);
        if (upperLen < .01f || foreLen < .01f) return;
        Vector3 toTarget = targetWorld - shoulderPos;
        toTarget.z = Mathf.Clamp(toTarget.z, -.65f, .35f);
        float totalReach = upperLen + foreLen;
        float dist = Mathf.Clamp(toTarget.magnitude, Mathf.Abs(upperLen - foreLen) + .05f, totalReach * .97f);
        Vector3 dir = toTarget.sqrMagnitude > .0001f ? toTarget.normalized : Vector3.up;
        float cosAngle = Mathf.Clamp((upperLen * upperLen + dist * dist - foreLen * foreLen) / (2f * upperLen * dist), -1f, 1f);
        float sinAngle = Mathf.Sqrt(Mathf.Max(0f, 1f - cosAngle * cosAngle));
        Vector3 bendNormal = Vector3.ProjectOnPlane(elbowHint, dir);
        if (bendNormal.sqrMagnitude < .001f) bendNormal = Vector3.down;
        else bendNormal.Normalize();
        Vector3 desiredUpperDir = (dir * cosAngle + bendNormal * sinAngle).normalized;
        Vector3 currentUpperDir = (foreArm.position - upperArm.position).normalized;
        if (currentUpperDir.sqrMagnitude > .0001f && desiredUpperDir.sqrMagnitude > .0001f)
            upperArm.rotation = Quaternion.FromToRotation(currentUpperDir, desiredUpperDir) * upperArm.rotation;
        Vector3 currentForeDir = (hand.position - foreArm.position).normalized;
        Vector3 desiredForeDir = (targetWorld - foreArm.position).normalized;
        if (currentForeDir.sqrMagnitude > .0001f && desiredForeDir.sqrMagnitude > .0001f)
            foreArm.rotation = Quaternion.FromToRotation(currentForeDir, desiredForeDir) * foreArm.rotation;
    }
}