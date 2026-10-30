using UnityEngine;

public sealed partial class CoconutSliceController
{
    private void SetupSliceAudio()
    {
        sfxSource = JungleAudioBank.Attach2DSource(gameObject);
        sliceClip = JungleAudioBank.CoconutSlice;
        hazardClip = JungleAudioBank.CoconutHazard;
    }

    private Material MakeMaterial(Color color) => JungleStageBuilder.GetSharedMaterial(color, 0f, .20f);

    private void LoadCoconutModels()
    {
        const string wholeRoot = "Jungle/NewModels/Coconut/Meshy_AI_Coconut_Bowling_Ball_20261026_texture_fbx/";
        const string cactusRoot = "Jungle/NewModels/Slice/Cactus/Meshy_AI_Spiked_Melon_Sphere_20261026_texture_fbx/";
        wholeCoconutPrefab = JungleAssets.Load<GameObject>(wholeRoot + "Meshy_AI_Coconut_Bowling_Ball_20261026_texture");
        cactusPrefab = JungleAssets.Load<GameObject>(cactusRoot + "Meshy_AI_Spiked_Melon_Sphere_20261026_texture");
        wholeCoconutModelMaterial = MakeTexturedMaterial(JungleAssets.Load<Texture2D>(wholeRoot + "Coconut_Albedo"), "Coco entero - Slice");
        cactusModelMaterial = MakeTexturedMaterial(JungleAssets.Load<Texture2D>(cactusRoot + "Cactus_Albedo"), "Cactus peligroso - Slice");

        if (keeperMode) return;

        const string halfRoot = "Jungle/NewModels/Slice/HalfCoconut/Meshy_AI_Coconut_Bowl_20261026_texture_fbx/";
        const string halfCactusRoot = "Jungle/NewModels/Slice/HalfCactus/Meshy_AI_Prickly_Cucumber_Slice_20261026_texture_fbx/";
        const string sleepingRoot = "Jungle/NewModels/Slice/GorillaHazard/Meshy_AI_Sleeping_Gorilla_Cub_20261026_texture_fbx/";
        const string hitRoot = "Jungle/NewModels/Slice/GorillaHazard/Meshy_AI_Bumped_Banana_Brute_20261026_texture_fbx/";
        halfCoconutPrefab = JungleAssets.Load<GameObject>(halfRoot + "Meshy_AI_Coconut_Bowl_20261026_texture");
        halfCactusPrefab = JungleAssets.Load<GameObject>(halfCactusRoot + "Meshy_AI_Prickly_Cucumber_Slice_20261026_texture");
        sleepingGorillaPrefab = JungleAssets.Load<GameObject>(sleepingRoot + "Meshy_AI_Sleeping_Gorilla_Cub_20261026_texture");
        hitGorillaPrefab = JungleAssets.Load<GameObject>(hitRoot + "Meshy_AI_Bumped_Banana_Brute_20261026_texture");
        halfCoconutModelMaterial = MakeTexturedMaterial(JungleAssets.Load<Texture2D>(halfRoot + "HalfCoconut_Albedo"), "Coco partido - Slice");
        halfCactusModelMaterial = MakeTexturedMaterial(JungleAssets.Load<Texture2D>(halfCactusRoot + "HalfCactus_Albedo"), "Cactus partido - Slice");
        sleepingGorillaMaterial = MakeTexturedMaterial(JungleAssets.Load<Texture2D>(sleepingRoot + "Meshy_AI_Sleeping_Gorilla_Cub_20261026_texture"), "Gorila hecho bolita");
        hitGorillaMaterial = MakeTexturedMaterial(JungleAssets.Load<Texture2D>(hitRoot + "Meshy_AI_Bumped_Banana_Brute_20261026_texture"), "Gorila golpeado");
        if (wholeCoconutPrefab == null || halfCoconutPrefab == null || cactusPrefab == null || halfCactusPrefab == null)
            Debug.LogWarning("No se encontraron todos los modelos nuevos de Coconut Slice; se usará el respaldo simple cuando sea necesario.");
    }

    private void CreateSlicePalms()
    {
        const string palmRoot = "Jungle/NewModels/Palm/Meshy_AI_Cartoon_Coconut_Palm_20261026_texture_fbx/";
        GameObject palmPrefab = JungleAssets.Load<GameObject>(palmRoot + "Meshy_AI_Cartoon_Coconut_Palm_20261026_texture");
        if (palmPrefab == null) return;
        Material palmMaterial = MakeTexturedMaterial(JungleAssets.Load<Texture2D>(palmRoot + "Palm_Albedo"), "Palmeras de Coconut Slice");
        CreateSlicePalm(palmPrefab, palmMaterial, "Palmera izquierda", new Vector3(-7.65f, -5.42f, 3.55f), 6.3f, 18f);
        CreateSlicePalm(palmPrefab, palmMaterial, "Palmera derecha", new Vector3(7.65f, -5.42f, 3.55f), 6.3f, -18f);
        CreateSlicePalm(palmPrefab, palmMaterial, "Palmera izquierda secundaria", new Vector3(-5.85f, -5.42f, 3.75f), 4.25f, -12f);
        CreateSlicePalm(palmPrefab, palmMaterial, "Palmera derecha secundaria", new Vector3(5.85f, -5.42f, 3.75f), 4.25f, 12f);
    }

    private void CreateSlicePalm(GameObject prefab, Material material, string palmName, Vector3 groundPosition, float desiredHeight, float yaw) =>
        JungleStageBuilder.InstantiateGroundedProp(prefab, transform, material, palmName, groundPosition, desiredHeight, yaw);

    private void LoadDistractorGorillas()
    {
        const string runRoot = "Jungle/GorillaBowling/Meshy_AI_Gorilla_Guardian_biped/";
        const string runAsset = runRoot + "Meshy_AI_Gorilla_Guardian_biped_Animation_Running_withSkin";
        const string fallRoot = "Jungle/NewModels/Slice/GorillaFall/Meshy_AI_Low_Poly_Gorilla_Embr_biped/";
        const string fallAsset = fallRoot + "Meshy_AI_Low_Poly_Gorilla_Embr_biped_Animation_Fall1_withSkin";
        runningGorillaPrefab = JungleAssets.Load<GameObject>(runAsset);
        fallingGorillaPrefab = JungleAssets.Load<GameObject>(fallAsset);
        runningGorillaClip = FirstUsableClip(runAsset);
        fallingGorillaClip = FirstUsableClip(fallAsset);
        runningGorillaMaterial = MakeTexturedMaterial(JungleAssets.Load<Texture2D>(runRoot + "Meshy_AI_Gorilla_Guardian_biped_texture_0"), "Gorila corredor");
        fallingGorillaMaterial = MakeTexturedMaterial(JungleAssets.Load<Texture2D>(fallRoot + "GorillaFall_Albedo"), "Gorila que cae");
        if (runningGorillaPrefab == null || fallingGorillaPrefab == null)
            Debug.LogWarning("Falta uno de los gorilas distractores de Coconut Slice.");
    }

    private static AnimationClip FirstUsableClip(string resourcePath)
    {
        foreach (AnimationClip clip in JungleAssets.LoadAll<AnimationClip>(resourcePath))
            if (clip != null && !clip.name.StartsWith("__preview__")) return clip;
        return null;
    }

    private void UpdateDistractorGorilla()
    {
        if (distractorGorilla == null)
        {
            if (state == GameState.Playing && Time.time >= nextDistractorAt) SpawnDistractorGorilla();
            return;
        }

        float progress = Mathf.Clamp01((Time.time - distractorStartedAt) / Mathf.Max(.01f, distractorDuration));
        AnimationClip activeClip = distractorIsFall ? fallingGorillaClip : runningGorillaClip;
        if (activeClip != null && distractorAnimatedModel != null)
        {
            float elapsed = Mathf.Max(0f, Time.time - distractorStartedAt);
            float sampleTime = distractorIsFall
                ? Mathf.Min(elapsed, Mathf.Max(0f, activeClip.length - .001f))
                : Mathf.Repeat(elapsed * 1.12f, Mathf.Max(.01f, activeClip.length));
            activeClip.SampleAnimation(distractorAnimatedModel, sampleTime);
        }
        float eased = Mathf.SmoothStep(0f, 1f, progress);
        distractorGorilla.transform.position = Vector3.Lerp(distractorStart, distractorEnd, eased);
        if (distractorIsFall)
            distractorGorilla.transform.rotation = Quaternion.Euler(0f, 180f, Mathf.Sin(progress * Mathf.PI) * 9f);
        if (progress >= 1f)
        {
            Destroy(distractorGorilla);
            distractorGorilla = null;
            distractorVisualRoot = null;
            distractorAnimatedModel = null;
            nextDistractorAt = Time.time + UnityEngine.Random.Range(3.5f, 7.5f);
        }
    }

    private void SpawnDistractorGorilla()
    {
        // La primera aparición siempre cruza la pantalla para presentar la idea.
        // Después se mantiene variedad aleatoria, sin permitir tres eventos iguales.
        bool randomFall = UnityEngine.Random.value < .42f;
        if (!hasSpawnedDistractor) distractorIsFall = false;
        else if (repeatedDistractorType >= 2) distractorIsFall = !previousDistractorWasFall;
        else distractorIsFall = randomFall;
        repeatedDistractorType = hasSpawnedDistractor && distractorIsFall == previousDistractorWasFall ? repeatedDistractorType + 1 : 1;
        previousDistractorWasFall = distractorIsFall;
        hasSpawnedDistractor = true;
        GameObject prefab = distractorIsFall ? fallingGorillaPrefab : runningGorillaPrefab;
        if (prefab == null) { nextDistractorAt = Time.time + 4f; return; }
        bool fromLeft = UnityEngine.Random.value < .5f;
        distractorStart = distractorIsFall
            ? new Vector3(UnityEngine.Random.Range(-4.6f, 4.6f), 7.2f, -2.5f)
            : new Vector3(fromLeft ? -13f : 13f, -6.25f, -4.1f);
        distractorEnd = distractorIsFall
            ? new Vector3(distractorStart.x + UnityEngine.Random.Range(-.8f, .8f), -10.5f, -2.5f)
            : new Vector3(fromLeft ? 13f : -13f, -6.25f, -4.1f);
        distractorDuration = distractorIsFall ? 2.35f : 3.05f;
        distractorStartedAt = Time.time;

        distractorGorilla = new GameObject(distractorIsFall ? "Gorila distractor cayendo" : "Gorila distractor corriendo");
        distractorGorilla.transform.SetParent(transform, false);
        distractorGorilla.name = distractorIsFall ? "Gorila distractor cayendo" : "Gorila distractor corriendo";
        distractorGorilla.transform.position = distractorStart;
        distractorGorilla.transform.rotation = Quaternion.identity;

        distractorVisualRoot = new GameObject("Orientación y escala del gorila");
        distractorVisualRoot.transform.SetParent(distractorGorilla.transform, false);
        distractorVisualRoot.transform.localRotation = Quaternion.Euler(0f, distractorIsFall ? 180f : (fromLeft ? 90f : -90f), 0f);
        distractorAnimatedModel = Instantiate(prefab, distractorVisualRoot.transform);
        distractorAnimatedModel.name = "Modelo animado";
        foreach (Collider collider in distractorAnimatedModel.GetComponentsInChildren<Collider>(true)) collider.enabled = false;

        AnimationClip activeClip = distractorIsFall ? fallingGorillaClip : runningGorillaClip;
        if (activeClip != null) activeClip.SampleAnimation(distractorAnimatedModel, 0f);
        Renderer[] renderers = distractorAnimatedModel.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            Bounds bounds = CombinedBounds(renderers);
            float desiredHeight = distractorIsFall ? 3.6f : 5.15f;
            distractorVisualRoot.transform.localScale = Vector3.one * (desiredHeight / Mathf.Max(.01f, bounds.size.y));
            bounds = CombinedBounds(renderers);
            distractorVisualRoot.transform.position += distractorGorilla.transform.position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            ApplySharedMaterial(renderers, distractorIsFall ? fallingGorillaMaterial : runningGorillaMaterial);
        }
    }

    private static Bounds CombinedBounds(Renderer[] renderers) => JungleStageBuilder.CombinedBounds(renderers);

    private static void ApplySharedMaterial(Renderer[] renderers, Material material) => JungleStageBuilder.ApplySharedMaterial(renderers, material);

    private Material MakeTexturedMaterial(Texture2D texture, string materialName) =>
        JungleStageBuilder.CreateTexturedMaterial(texture, materialName, .12f);

    private GameObject Primitive(PrimitiveType type, string objectName, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject item = GameObject.CreatePrimitive(type);
        item.name = objectName;
        item.transform.SetParent(parent);
        item.transform.position = position;
        item.transform.localScale = scale;
        item.GetComponent<Renderer>().sharedMaterial = material;
        JungleStageBuilder.StripCollider(item);
        return item;
    }

    private void BuildStage()
    {
        coconutMaterial = MakeMaterial(new Color(.27f, .105f, .035f));
        coconutInsideMaterial = MakeMaterial(new Color(.94f, .92f, .76f));
        darkMaterial = MakeMaterial(new Color(.035f, .075f, .055f));
        greenMaterial = MakeMaterial(new Color(.035f, .30f, .12f));

        Camera camera = Camera.main;
        camera.orthographic = true;
        camera.orthographicSize = 5.4f;
        camera.transform.position = new Vector3(0, 0, -10f);
        camera.transform.rotation = Quaternion.identity;
        camera.backgroundColor = new Color(.025f, .16f, .12f);

        Texture2D jungleBackground = JungleAssets.Load<Texture2D>("Jungle/CoconutSlice/JungleSkyBackground");
        Material backgroundMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture") ?? Shader.Find("Universal Render Pipeline/Lit"));
        if (backgroundMaterial.HasProperty("_BaseMap")) backgroundMaterial.SetTexture("_BaseMap", jungleBackground);
        if (backgroundMaterial.HasProperty("_BaseColor")) backgroundMaterial.SetColor("_BaseColor", Color.white);
        backgroundMaterial.name = "Fondo pintado de Coconut Slice";
        backgroundMaterial.mainTexture = jungleBackground;
        backgroundMaterial.mainTextureScale = new Vector2(1f, -1f);
        backgroundMaterial.mainTextureOffset = new Vector2(0f, 1f);
        backgroundMaterial.color = Color.white;
        Primitive(PrimitiveType.Cube, "Fondo selvático pintado", transform, new Vector3(0, 0, 4.5f), new Vector3(19.4f, 10.95f, .3f), backgroundMaterial);

        CreateSlicePalms();
    }

    private GameObject CreateCoconutPhysicsObject(string objectName, GameObject prefab, Material material, Vector3 position, float desiredSize, bool isHalf, int side)
    {
        GameObject root = new GameObject(objectName);
        root.transform.SetParent(transform);
        root.transform.position = position;

        if (isHalf)
        {
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(.58f, .42f, .56f);
        }
        else
        {
            SphereCollider collider = root.AddComponent<SphereCollider>();
            collider.radius = .42f;
        }

        if (prefab == null)
        {
            GameObject fallback = Primitive(PrimitiveType.Sphere, "Visual provisional", root.transform, position, isHalf ? new Vector3(.36f, .66f, .62f) : new Vector3(.72f, .68f, .66f), isHalf ? coconutInsideMaterial : coconutMaterial);
            fallback.transform.localPosition = Vector3.zero;
            return root;
        }

        GameObject model = Instantiate(prefab, root.transform);
        model.name = isHalf ? "Modelo de medio coco" : "Modelo de coco entero";
        Quaternion importedRotation = model.transform.localRotation;
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = (isHalf && side > 0 ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity) * importedRotation;
        model.transform.localScale = Vector3.one;
        foreach (Collider childCollider in model.GetComponentsInChildren<Collider>(true)) childCollider.enabled = false;

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return root;
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        model.transform.localScale = Vector3.one * (desiredSize / Mathf.Max(.01f, Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z))));
        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        model.transform.position += root.transform.position - bounds.center;
        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++) materials[i] = material;
            renderer.sharedMaterials = materials;
        }
        return root;
    }
}
