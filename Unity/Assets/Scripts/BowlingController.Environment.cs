using System;
using System.IO;
using GLTFast;
using UnityEngine;

public sealed partial class BowlingController
{
    private static Texture2D sharedCoconutFiberTexture;
    private static Texture2D sharedBowlingHudTexture;
    private static Texture2D sharedBowlingCardTexture;
    private static Texture2D sharedBowlingButtonTexture;
    private static Texture2D sharedBowlingButtonHoverTexture;

    private Material MakeMaterial(Color color, float metallic = 0f, float smoothness = .35f) =>
        JungleStageBuilder.GetSharedMaterial(color, metallic, smoothness, preferStandard: false);

    private Material MakeCoconutMaterial()
    {
        var material = MakeMaterial(Color.white, 0f, .08f);
        if (sharedCoconutFiberTexture == null)
        {
            var texture = new Texture2D(128, 64, TextureFormat.RGB24, false)
            {
                name = "Fibra de coco",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear
            };
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    float grain = Mathf.PerlinNoise(x * .12f, y * .42f);
                    float fibers = Mathf.Abs(Mathf.Sin(x * .37f + grain * 5f)) > .92f ? -.12f : 0f;
                    float light = Mathf.Lerp(.72f, 1.18f, grain) + fibers;
                    texture.SetPixel(x, y, new Color(.30f * light, .14f * light, .055f * light));
                }
            }
            texture.Apply(false, true);
            sharedCoconutFiberTexture = texture;
        }
        material.mainTexture = sharedCoconutFiberTexture;
        return material;
    }

    private void AddCoconutEye(Vector3 localPosition, float size)
    {
        GameObject eye = JungleStageBuilder.CreatePrimitive(
            PrimitiveType.Sphere,
            "Marca del coco",
            Vector3.zero,
            new Vector3(size, size * .82f, size * .22f),
            new Color(.055f, .022f, .009f),
            ballVisual.transform,
            keepCollider: false,
            metallic: 0f,
            smoothness: .04f,
            preferStandard: false);
        eye.transform.localPosition = localPosition;
    }

    private GameObject bowlingStageRoot;

    private GameObject Box(string name, Vector3 position, Vector3 scale, Color color, Transform parent = null, bool keepCollider = true) =>
        JungleStageBuilder.CreatePrimitive(PrimitiveType.Cube, name, position, scale, color, parent: parent, keepCollider: keepCollider, smoothness: .35f, preferStandard: false);

    private void BuildBowlingAlley()
    {
        transform.localScale = Vector3.one;
        Renderer controllerRenderer = GetComponent<Renderer>();
        if (controllerRenderer != null) controllerRenderer.enabled = false;
        Collider oldCollider = GetComponent<Collider>(); if (oldCollider != null) oldCollider.enabled = false;
        if (bowlingStageRoot == null) bowlingStageRoot = new GameObject("Escenario de Boliche");
        Transform stageParent = bowlingStageRoot.transform;
        Box("Pista", new Vector3(0, -.35f, 2), new Vector3(7.2f, .35f, 21), new Color(.28f, .14f, .045f), stageParent);
        Box("Franja central", new Vector3(0, -.16f, 2), new Vector3(6.4f, .05f, 20.6f), new Color(.68f, .43f, .14f), stageParent);
        Box("Canal izquierdo", new Vector3(-4.15f, -.2f, 2), new Vector3(1.1f, .28f, 21), new Color(.025f, .12f, .07f), stageParent);
        Box("Canal derecho", new Vector3(4.15f, -.2f, 2), new Vector3(1.1f, .28f, 21), new Color(.025f, .12f, .07f), stageParent);
        Box("Pared fondo", new Vector3(0, 2.5f, 12.3f), new Vector3(11, 5.5f, .4f), new Color(.04f, .18f, .09f), stageParent);
        for (int i = -3; i <= 3; i++) Box("Marca visual", new Vector3(i, -.135f, -5.4f), new Vector3(.08f, .015f, .6f), new Color(.95f, .84f, .45f), stageParent, keepCollider: false);
        for (int side = -1; side <= 1; side += 2)
        {
            for (int z = -5; z <= 10; z += 3)
            {
                // El costado izquierdo central queda libre como zona del gorila espectador.
                if (side == -1 && z >= 1 && z <= 7) continue;
                CreateJunglePlant(side * 5.2f, z, stageParent);
            }
        }
        ballVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ballVisual.name = "Coco de boliche";
        ballVisual.transform.SetParent(transform, false);
        ballVisual.transform.localScale = new Vector3(1.08f, 1.03f, 1.1f);
        ballVisual.GetComponent<Renderer>().material = MakeCoconutMaterial();
        AddCoconutEye(new Vector3(-.14f, .11f, -.46f), .15f);
        AddCoconutEye(new Vector3(.14f, .11f, -.46f), .15f);
        AddCoconutEye(new Vector3(0f, -.12f, -.48f), .17f);
        ballBody = gameObject.AddComponent<Rigidbody>();
        ballBody.mass = 4.5f; ballBody.linearDamping = .12f; ballBody.angularDamping = .08f;
        ballBody.interpolation = RigidbodyInterpolation.Interpolate;
        var sphere = gameObject.AddComponent<SphereCollider>(); sphere.radius = .53f;

        Camera camera = Camera.main;
        camera.orthographic = false; camera.fieldOfView = 52f;
        camera.transform.position = new Vector3(0, 6.4f, -12.5f);
        camera.transform.LookAt(new Vector3(0, .5f, 2.8f));
        camera.backgroundColor = new Color(.025f, .13f, .075f);
        Light light = FindAnyObjectByType<Light>();
        if (light != null) { light.intensity = 1.35f; light.transform.rotation = Quaternion.Euler(48, -25, 0); }
    }

    private void BindEditableScene()
    {
        Renderer controllerRenderer = GetComponent<Renderer>();
        if (controllerRenderer != null) controllerRenderer.enabled = false;
        Collider oldCollider = GetComponent<Collider>(); if (oldCollider != null) oldCollider.enabled = false;
        ballVisual = transform.Find("Visual del coco") != null ? transform.Find("Visual del coco").gameObject : gameObject;
        ballBody = GetComponent<Rigidbody>();
        if (ballBody == null) ballBody = gameObject.AddComponent<Rigidbody>();
        ballBody.mass = 4.5f; ballBody.linearDamping = .12f; ballBody.angularDamping = .08f;
        ballBody.interpolation = RigidbodyInterpolation.Interpolate;
        if (GetComponent<SphereCollider>() == null) { var sphere = gameObject.AddComponent<SphereCollider>(); sphere.radius = .53f; }
        gorillaAnchor = SceneBindings?.Get("Punto del gorila")?.transform;
        GameObject gorillaStage = SceneBindings?.Get("Plataforma del gorila");
        if (gorillaStage != null) gorillaStage.SetActive(false);
        // El jardín lateral está a Y=-.46; hundir apenas los pies evita que la
        // sombra separada haga parecer que el gorila flota.
        if (gorillaAnchor != null) gorillaAnchor.position = new Vector3(-6.25f, -.48f, 4.2f);
        islandAnchor = SceneBindings?.Get("Punto del fondo isla")?.transform;
        pins.Clear(); pinRackPositions.Clear(); pinRackRotations.Clear();
        GameObject rack = SceneBindings?.Get("Pinos editables");
        if (rack != null)
        {
            foreach (Transform child in rack.transform)
            {
                if (!child.name.StartsWith("Pino")) continue;
                ApplyTotemPinVisual(child.gameObject);
                pins.Add(child.gameObject);
                pinRackPositions[child.gameObject] = child.position;
                pinRackRotations[child.gameObject] = child.rotation;
            }
        }
    }



    private void CreateJunglePlant(float x, float z, Transform parent = null)
    {
        Box("Tronco selva", new Vector3(x, 1.1f, z), new Vector3(.45f, 2.3f, .45f), new Color(.25f, .11f, .035f), parent, keepCollider: false);
        GameObject crown = JungleStageBuilder.CreatePrimitive(PrimitiveType.Sphere, "Copa selva", new Vector3(x, 2.7f, z), new Vector3(2.1f, 1.5f, 1.6f), new Color(.03f, .35f, .12f), parent: parent, keepCollider: false, smoothness: .35f, preferStandard: false);
        JungleStageBuilder.AttachWindSway(crown, 3.2f, 1.7f);
    }

    private async void LoadIslandBackground()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "EasterIslandLowPoly.glb");
        if (!File.Exists(path)) { Debug.LogWarning("No se encontró el fondo EasterIslandLowPoly.glb"); return; }
        islandBackground = new GameObject("Isla de Pascua alrededor de la pista");
        if (bowlingStageRoot != null) islandBackground.transform.SetParent(bowlingStageRoot.transform, false);
        islandImport = new GltfImport();
        bool loaded = await islandImport.Load(new Uri(path).AbsoluteUri);
        if (!loaded || islandBackground == null) return;
        GameObject left = new GameObject("Isla lateral izquierda"); left.transform.SetParent(islandBackground.transform);
        GameObject right = new GameObject("Isla lateral derecha"); right.transform.SetParent(islandBackground.transform);
        GameObject back = new GameObject("Isla del horizonte"); back.transform.SetParent(islandBackground.transform);
        if (await islandImport.InstantiateMainSceneAsync(left.transform)) ArrangeIslandPiece(left, -1);
        if (await islandImport.InstantiateMainSceneAsync(right.transform)) ArrangeIslandPiece(right, 1);
        if (await islandImport.InstantiateMainSceneAsync(back.transform)) ArrangeIslandPiece(back, 0);
    }

    private void ArrangeIslandPiece(GameObject piece, int side)
    {
        piece.transform.rotation = side == 0
            ? (islandAnchor != null ? islandAnchor.rotation : Quaternion.Euler(0, 180f, 0))
            : Quaternion.Euler(0, side < 0 ? 105f : 255f, 0);
        foreach (Transform item in piece.GetComponentsInChildren<Transform>(true))
            if (item.name == "Agua" || item.name == "Mar_e_moto" || item.name == "Mar_e_moto1")
                foreach (Renderer renderer in item.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;

        Renderer[] renderers = piece.GetComponentsInChildren<Renderer>(true);
        Renderer first = null;
        foreach (Renderer renderer in renderers) if (renderer.enabled) { first = renderer; break; }
        if (first == null) return;
        Bounds bounds = first.bounds;
        foreach (Renderer renderer in renderers) if (renderer.enabled) bounds.Encapsulate(renderer.bounds);
        float desiredWidth = side == 0 ? 24f : 17f;
        piece.transform.localScale = Vector3.one * (desiredWidth / Mathf.Max(.01f, bounds.size.x));
        bounds = first.bounds;
        foreach (Renderer renderer in renderers) if (renderer.enabled) bounds.Encapsulate(renderer.bounds);

        Vector3 offset;
        if (side < 0) offset = new Vector3(-4.85f - bounds.max.x, -1.35f - bounds.min.y, 3f - bounds.center.z);
        else if (side > 0) offset = new Vector3(4.85f - bounds.min.x, -1.35f - bounds.min.y, 3f - bounds.center.z);
        else
        {
            Vector3 anchor = islandAnchor != null ? islandAnchor.position : new Vector3(0, -.65f, 13f);
            offset = new Vector3(anchor.x - bounds.center.x, anchor.y - bounds.min.y, anchor.z - bounds.min.z);
        }
        piece.transform.position += offset;
        foreach (Collider item in piece.GetComponentsInChildren<Collider>(true)) item.enabled = false;
    }

    private void CreatePins()
    {
        GameObject rackRoot = new GameObject("Pinos de Boliche");
        if (bowlingStageRoot != null) rackRoot.transform.SetParent(bowlingStageRoot.transform, false);
        float startZ = 7.2f;
        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col <= row; col++)
            {
                GameObject pin = new GameObject("Pino");
                pin.name = "Pino";
                pin.transform.SetParent(rackRoot.transform, false);
                pin.transform.position = new Vector3((col - row * .5f) * .78f, .65f, startZ + row * .72f);
                CapsuleCollider pinCollider = pin.AddComponent<CapsuleCollider>();
                pinCollider.direction = 1; pinCollider.center = new Vector3(0f, .20f, 0f); pinCollider.height = 1.70f; pinCollider.radius = .26f;
                ApplyTotemPinVisual(pin);
                Rigidbody body = pin.AddComponent<Rigidbody>(); body.mass = 1.5f; body.linearDamping = .10f; body.angularDamping = .07f;
                body.centerOfMass = new Vector3(0f, -.16f, 0f); body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.Sleep();
                pins.Add(pin);
                pinRackPositions[pin] = pin.transform.position;
                pinRackRotations[pin] = pin.transform.rotation;
            }
        }
    }

    private void ApplyTotemPinVisual(GameObject pin)
    {
        if (pin == null) return;
        const string assetRoot = "Jungle/BowlingPins/Meshy_AI_Banana_Guardian_20261024_texture_fbx/";
        const string modelPath = assetRoot + "Meshy_AI_Banana_Guardian_20261024_texture";
        GameObject prefab = JungleAssets.Load<GameObject>(modelPath);
        if (prefab == null)
        {
            Debug.LogWarning("No se encontró el modelo del pino tótem en Resources.");
            return;
        }

        pin.transform.localScale = Vector3.one;
        Renderer oldRenderer = pin.GetComponent<Renderer>();
        if (oldRenderer != null) oldRenderer.enabled = false;
        CapsuleCollider collider = pin.GetComponent<CapsuleCollider>();
        if (collider == null) collider = pin.AddComponent<CapsuleCollider>();
        collider.direction = 1; collider.center = new Vector3(0f, .20f, 0f); collider.height = 1.70f; collider.radius = .26f;
        Rigidbody pinBody = pin.GetComponent<Rigidbody>();
        if (pinBody != null)
        {
            pinBody.mass = 1.5f; pinBody.linearDamping = .10f; pinBody.angularDamping = .07f;
            pinBody.centerOfMass = new Vector3(0f, -.16f, 0f);
            pinBody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            pinBody.interpolation = RigidbodyInterpolation.Interpolate;
        }

        Transform previous = pin.transform.Find("Visual pino tótem");
        if (previous != null) Destroy(previous.gameObject);
        GameObject visual = Instantiate(prefab, pin.transform);
        visual.name = "Visual pino tótem";
        Quaternion importedAxisCorrection = visual.transform.localRotation;
        visual.transform.localPosition = Vector3.zero;
        // Conserva la corrección Z-up del FBX y gira solamente el frente hacia el jugador.
        visual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * importedAxisCorrection;
        visual.transform.localScale = Vector3.one;
        foreach (Collider childCollider in visual.GetComponentsInChildren<Collider>(true)) childCollider.enabled = false;

        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        float scale = 1.82f / Mathf.Max(.01f, bounds.size.y);
        visual.transform.localScale = Vector3.one * scale;
        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        Vector3 targetCenter = pin.transform.position + Vector3.up * .19f;
        visual.transform.position += targetCenter - bounds.center;

        if (totemPinSharedMaterial == null)
        {
            // El albedo tiene un nombre distinto al FBX para evitar rutas duplicadas dentro de Resources.
            Texture2D albedo = JungleAssets.Load<Texture2D>(assetRoot + "Meshy_AI_Banana_Guardian_20261024_albedo");
            totemPinSharedMaterial = MakeMaterial(Color.white, 0f, .16f);
            totemPinSharedMaterial.name = "Pinos tótem - material compartido optimizado";
            totemPinSharedMaterial.enableInstancing = true;
            if (albedo != null)
            {
                totemPinSharedMaterial.mainTexture = albedo;
                totemPinSharedMaterial.SetTexture("_MainTex", albedo);
            }
            else Debug.LogWarning("No se encontró el albedo único de los pinos tótem.");
        }
        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++) materials[i] = totemPinSharedMaterial;
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }
    }

    private void CreateNewBowlingVegetation()
    {
        const string bushRoot = "Jungle/NewModels/Bush/Meshy_AI_Emerald_Canopy_20261026_texture_fbx/";
        const string palmRoot = "Jungle/NewModels/Palm/Meshy_AI_Cartoon_Coconut_Palm_20261026_texture_fbx/";
        GameObject bushPrefab = JungleAssets.Load<GameObject>(bushRoot + "Meshy_AI_Emerald_Canopy_20261026_texture");
        GameObject palmPrefab = JungleAssets.Load<GameObject>(palmRoot + "Meshy_AI_Cartoon_Coconut_Palm_20261026_texture");
        if (bushPrefab == null || palmPrefab == null)
        {
            Debug.LogWarning("No se encontraron los nuevos modelos de vegetación de Bowling.");
            return;
        }

        newVegetationRoot = new GameObject("Vegetación 3D nueva - editable");
        bushSharedMaterial = CreateSharedPropMaterial(bushRoot + "Bush_Albedo", "Arbustos pintados");
        palmSharedMaterial = CreateSharedPropMaterial(palmRoot + "Palm_Albedo", "Palmeras pintadas");

        Vector4[] bushLayout =
        {
            new Vector4(-7.55f, .65f, 2.10f, -18f),
            new Vector4(-7.25f, 3.95f, 1.90f, 12f),
            new Vector4(-5.85f, 7.05f, 1.72f, -8f),
            new Vector4(5.75f, 7.05f, 1.78f, 16f),
            new Vector4(7.35f, 3.10f, 2.05f, -12f)
        };
        for (int i = 0; i < bushLayout.Length; i++)
        {
            Vector4 item = bushLayout[i];
            CreateEnvironmentProp(bushPrefab, bushSharedMaterial, "Arbusto " + (i + 1), new Vector3(item.x, -.10f, item.y), item.z, item.w);
        }

        Vector4[] palmLayout =
        {
            new Vector4(-7.35f, -2.7f, 7.8f, 22f),
            new Vector4(-5.65f, 6.35f, 6.25f, -14f),
            new Vector4(5.65f, 6.45f, 6.45f, 15f),
            new Vector4(7.35f, -2.45f, 7.8f, -24f)
        };
        for (int i = 0; i < palmLayout.Length; i++)
        {
            Vector4 item = palmLayout[i];
            CreateEnvironmentProp(palmPrefab, palmSharedMaterial, "Palmera " + (i + 1), new Vector3(item.x, -.16f, item.y), item.z, item.w);
        }
    }

    private Material CreateSharedPropMaterial(string albedoPath, string materialName) =>
        JungleStageBuilder.CreateTexturedMaterial(JungleAssets.Load<Texture2D>(albedoPath), materialName, .18f, preferStandard: false);

    private void ApplyBowlingSurfaceTextures()
    {
        Texture2D laneTexture = JungleAssets.Load<Texture2D>("Jungle/SurfaceTextures/BowlingLaneBamboo");
        Texture2D sidesTexture = JungleAssets.Load<Texture2D>("Jungle/SurfaceTextures/BowlingSidesJungle");
        if (laneTexture == null || sidesTexture == null)
        {
            Debug.LogWarning("No se encontraron las nuevas texturas selváticas de la pista.");
            return;
        }

        ConfigureSurfaceTexture(laneTexture);
        ConfigureSurfaceTexture(sidesTexture);
        Material laneMaterial = new Material(JungleStageBuilder.LitShader) { name = "Bambú pintado de la pista" };
        laneMaterial.color = Color.white;
        if (laneMaterial.HasProperty("_BaseColor")) laneMaterial.SetColor("_BaseColor", Color.white);
        if (laneMaterial.HasProperty("_BaseMap")) { laneMaterial.SetTexture("_BaseMap", laneTexture); laneMaterial.SetTextureScale("_BaseMap", new Vector2(1f, 5.5f)); }
        if (laneMaterial.HasProperty("_MainTex")) { laneMaterial.SetTexture("_MainTex", laneTexture); laneMaterial.SetTextureScale("_MainTex", new Vector2(1f, 5.5f)); }
        laneMaterial.mainTexture = laneTexture;
        laneMaterial.mainTextureScale = new Vector2(1f, 5.5f);

        Material sidesMaterial = new Material(JungleStageBuilder.LitShader) { name = "Laterales selváticos pintados" };
        sidesMaterial.color = new Color(.88f, .94f, .88f);
        if (sidesMaterial.HasProperty("_BaseColor")) sidesMaterial.SetColor("_BaseColor", new Color(.88f, .94f, .88f));
        if (sidesMaterial.HasProperty("_BaseMap")) { sidesMaterial.SetTexture("_BaseMap", sidesTexture); sidesMaterial.SetTextureScale("_BaseMap", new Vector2(1.25f, 6f)); }
        if (sidesMaterial.HasProperty("_MainTex")) { sidesMaterial.SetTexture("_MainTex", sidesTexture); sidesMaterial.SetTextureScale("_MainTex", new Vector2(1.25f, 6f)); }
        sidesMaterial.mainTexture = sidesTexture;
        sidesMaterial.mainTextureScale = new Vector2(1.25f, 6f);

        ApplySurfaceMaterial("Franja central", laneMaterial);
        ApplySurfaceMaterial("Pista", sidesMaterial);
        ApplySurfaceMaterial("Canal izquierdo", sidesMaterial);
        ApplySurfaceMaterial("Canal derecho", sidesMaterial);
    }

    private static void ConfigureSurfaceTexture(Texture2D texture)
    {
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Trilinear;
        texture.anisoLevel = 8;
    }

    private void ApplySurfaceMaterial(string objectName, Material material)
    {
        GameObject surface = SceneBindings?.Get(objectName);
        if (surface == null) return;
        Renderer renderer = surface.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
    }

    private void CreateEnvironmentProp(GameObject prefab, Material material, string objectName, Vector3 groundPosition, float desiredHeight, float yaw) =>
        JungleStageBuilder.InstantiateGroundedProp(prefab, newVegetationRoot.transform, material, objectName, groundPosition, desiredHeight, yaw);

    private void ApplyCoconutModel()
    {
        if (ballVisual == null) return;
        const string assetRoot = "Jungle/NewModels/Coconut/Meshy_AI_Coconut_Bowling_Ball_20261026_texture_fbx/";
        GameObject prefab = JungleAssets.Load<GameObject>(assetRoot + "Meshy_AI_Coconut_Bowling_Ball_20261026_texture");
        if (prefab == null) { Debug.LogWarning("No se encontró el nuevo coco de Bowling."); return; }
        foreach (Renderer renderer in ballVisual.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
        GameObject visual = Instantiate(prefab, ballVisual.transform);
        visual.name = "Coco Meshy pintado";
        Quaternion importedAxisCorrection = visual.transform.localRotation;
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = importedAxisCorrection;
        visual.transform.localScale = Vector3.one;
        foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        visual.transform.localScale = Vector3.one * (1.08f / Mathf.Max(.01f, Mathf.Max(bounds.size.x, Mathf.Max(bounds.size.y, bounds.size.z))));
        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        visual.transform.position += ballVisual.transform.position - bounds.center;
        coconutModelMaterial = CreateSharedPropMaterial(assetRoot + "Coconut_Albedo", "Coco de boliche pintado");
        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++) materials[i] = coconutModelMaterial;
            renderer.sharedMaterials = materials;
            renderer.enabled = true;
        }
    }
}
