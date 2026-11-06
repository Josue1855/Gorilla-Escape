using System.Collections;
using System.IO;
using GorillaEscape.Gameplay.Input;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Experiencia visual definitiva de Gorilla Escape 1.0:
/// 1. Splash cinematográfico 3D con gorila corriendo hacia la cámara en playa tropical.
/// 2. Menú 3D integrado con letreros físicos de madera, bambú, cuerdas y texto tallado en 3D.
/// 3. Bootstrap asíncrono no bloqueante en segundo plano (Java, IPC, MediaPipe, audio).
/// 4. Modo Historia con historieta de 5 viñetas ilustradas (naufragio, llegada, Kong, desafío, 4 pruebas).
/// 5. Pantalla de preparación personal («Prepárate para jugar») con calibración comprensible.
/// 6. Soporte para captura de evidencia automatizada (GORILLA_AUTO_SCREENSHOT).
/// </summary>
[RequireComponent(typeof(JunglePanel))]
public sealed class SplashDirector : MonoBehaviour, IJungleScreen
{
    public enum State
    {
        Cinematic,
        Menu3D,
        PartyModal,
        ComicStory,
        PersonalPrep,
        Exiting
    }

    [Header("Estado")]
    public State currentState = State.Cinematic;

    private float startedAt;
    private Camera cam;
    private readonly GorillaMascotRig gorillaRig = new GorillaMascotRig();
    private Transform gorillaRoot;
    private Transform signsRoot;
    private readonly SignData[] signs = new SignData[3];
    private int selectedSign = 0;
    private int hoveredSign = -1;

    private AudioSource sfxSource;
    private float lastFootstepTime = -1f;
    private int lastAudibleSign = -1;

    private int comicPanelIndex = 0;
    private int prepStepIndex = 0;

    private PortableServiceHost services;
    private MotionSession session;
    private bool bootstrapped = false;

    private const float CinematicDuration = 5.0f;
    private const float GorillaStartZ = 18f;
    private const float GorillaEndZ = 8.5f;

    // Texturas de ambientación
    private Texture2D texGorillaMenu;
    private Texture2D texGorillaAudience;
    private Texture2D texGorillaTaunt;
    private Texture2D texBambooCard;

    private struct SignData
    {
        public Transform root;
        public Transform board;
        public Renderer boardRenderer;
        public TextMesh textMesh;
        public Color originalColor;
        public string title;
        public string subtitle;
        public string action;
        public Vector3 cameraTargetPos;
    }

    private struct ComicPanel
    {
        public string tag;
        public string title;
        public string narrative;
        public string tip;
    }

    private static readonly ComicPanel[] ComicPanels = new ComicPanel[]
    {
        new ComicPanel
        {
            tag = "VIÑETA 1 DE 5",
            title = "TORMENTA EN ALTAMAR",
            narrative = "Una feroz tormenta tropical destroza tu velero en medio de la noche. Olas gigantescas barren la cubierta y caes al rugiente océano.",
            tip = "El náufrago lucha por mantenerse a flote en aguas oscuras..."
        },
        new ComicPanel
        {
            tag = "VIÑETA 2 DE 5",
            title = "DESPERTAR EN LA ARENA",
            narrative = "Despiertas al amanecer exhausto en la arena cálida de una costa desconocida. A tu espalda se extiende una selva virgen imponente.",
            tip = "Has llegado a la mítica Isla Gorila, un reino oculto de los mapas..."
        },
        new ComicPanel
        {
            tag = "VIÑETA 3 DE 5",
            title = "EL ENCUENTRO",
            narrative = "Decenas de miradas curiosas te observan entre las palmeras. Una multitud de gorilas rodea la playa. En el centro, el colosal Rey Kong se aproxima con paso firme.",
            tip = "Los gorilas examinan al primer humano que pisa su isla en siglos..."
        },
        new ComicPanel
        {
            tag = "VIÑETA 4 DE 5",
            title = "EL DESAFÍO DE KONG",
            narrative = "Kong golpea su pecho y proclama el reto: «Si demuestras tu fuerza y agilidad humana en las pruebas sagradas de las Gorillolimpiadas, mis gorilas construirán una balsa de bambú para tu regreso a casa».",
            tip = "¡El náufrago acepta el reto de las Gorillolimpiadas!"
        },
        new ComicPanel
        {
            tag = "VIÑETA 5 DE 5",
            title = "LAS PRUEBAS SAGRADAS DEL NÁUFRAGO",
            narrative = "1. Gorilla Smash: Mazo de fuerza y reflejos.\n2. Lanzamiento de Coco: Puntería y distancia máxima.\n3. Coconut Bowling: Tiro rodado contra pines tropicales.\n4. Jungle Slice: Reflejos rápidos cortando cocos al vuelo.\n¡Supera las pruebas de Kong para construir tu balsa y escapar!",
            tip = "¡El náufrago debe conquistar la arena de Kong para volver a casa!"
        }
    };

    private static readonly string[] PrepSteps = new string[]
    {
        "1. ENCUADRE DE CÁMARA\nColócate de pie frente a tu cámara web a una distancia aproximada de 1.5 a 2 metros, con buena iluminación frontal.",
        "2. RANGO DE MOVIMIENTO\nLevanta ambos brazos y haz un movimiento de prueba. El sensor de visión registrará tus brazos y torso en tiempo real.",
        "3. TELÉFONO O CONTROL\nSostén tu smartphone en posición vertical o conéctalo escaneando el código QR para controles giroscópicos de máxima precisión.",
        "4. ¡TODO LISTO!\nTu espacio de juego está preparado. Mantén concentración y demuestra tu valor en las Gorillolimpiadas."
    };

    private void Awake()
    {
        if (GetComponent<JunglePanel>() == null) gameObject.AddComponent<JunglePanel>();
        startedAt = -1f;
        LoadAssets();
        EnsureLighting();
        BuildTropicalEnvironment();
        BuildGorillaCharacter();
        Build3DSigns();
        EnsureCinematicCamera();

        // Durante el splash, los letreros permanecen ocultos para que el gorila corra con vista despejada
        if (signsRoot != null)
            signsRoot.gameObject.SetActive(false);

        // Bootstrap asíncrono no bloqueante
        StartCoroutine(AsyncBootstrapRoutine());

        // Hook para automatización de capturas si está configurado
        StartCoroutine(AutomationScreenshotHookRoutine());
    }

    private void OnDestroy()
    {
        gorillaRig.Dispose();
    }

    private void LoadAssets()
    {
        texGorillaMenu = JungleAssets.Load<Texture2D>("Jungle/gorila_menu_hd");
        if (texGorillaMenu == null) texGorillaMenu = JungleAssets.Load<Texture2D>("Jungle/gorila_menu");
        texGorillaAudience = JungleAssets.Load<Texture2D>("Jungle/gorilla_audience");
        texGorillaTaunt = JungleAssets.Load<Texture2D>("Jungle/gorilla_foreground_taunt");
        texBambooCard = JungleUITheme.BambooCardRounded;
    }

    private void EnsureLighting()
    {
        var lights = FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Light mainLight = null;
        if (lights != null && lights.Length > 0)
        {
            foreach (var l in lights)
            {
                if (l.type == LightType.Directional) { mainLight = l; break; }
            }
        }
        if (mainLight == null)
        {
            var lightGo = new GameObject("Luz Tropical Direccional");
            mainLight = lightGo.AddComponent<Light>();
            mainLight.type = LightType.Directional;
        }
        mainLight.intensity = 1.22f;
        mainLight.color = new Color(1f, 0.96f, 0.88f);
        mainLight.transform.rotation = Quaternion.Euler(38f, -32f, 0f);
        mainLight.shadows = LightShadows.Soft;
        mainLight.shadowStrength = 0.65f;
        mainLight.shadowBias = 0.04f;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientLight = new Color(0.55f, 0.65f, 0.75f);
        RenderSettings.ambientSkyColor = new Color(0.55f, 0.70f, 0.90f);
        RenderSettings.ambientEquatorColor = new Color(0.38f, 0.52f, 0.38f);
        RenderSettings.ambientGroundColor = new Color(0.48f, 0.40f, 0.30f);
    }

    private IEnumerator AsyncBootstrapRoutine()
    {
        yield return JungleAudioBank.Prewarm();
        services = PortableServiceHost.Ensure();
        session = MotionSession.Ensure();
        session.Connect();
        bootstrapped = true;
    }

    private void EnsureCinematicCamera()
    {
        cam = Camera.main;
        if (cam == null)
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam = go.AddComponent<Camera>();
            go.AddComponent<AudioListener>();
        }
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.38f, 0.70f, 0.94f, 1f);
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 500f;
        cam.fieldOfView = 58f;
        cam.transform.position = new Vector3(0f, 2.3f, 0.2f);
        cam.transform.rotation = Quaternion.Euler(6.5f, 0f, 0f);

        sfxSource = cam.gameObject.GetComponent<AudioSource>();
        if (sfxSource == null) sfxSource = cam.gameObject.AddComponent<AudioSource>();
        sfxSource.spatialBlend = 0f;
        sfxSource.playOnAwake = false;
        JungleAudioBank.EnsureAmbience(cam.gameObject);
    }

    private static Material MakeMaterialWithTexture(Texture2D albedo, Color fallbackColor, string name, float smoothness = 0.15f, Texture2D normal = null, float bumpScale = 0.7f)
    {
        if (albedo != null)
        {
            var mat = JungleStageBuilder.CreateTexturedMaterial(albedo, name, smoothness, normal, bumpScale);
            mat.color = fallbackColor;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", fallbackColor);
            return mat;
        }
        return JungleStageBuilder.GetSharedMaterial(fallbackColor, 0f, smoothness);
    }

    private static GameObject PlaceGroundedProp(GameObject prefab, Transform parent, Material mat, string name, Vector3 pos, float height, float yaw, float pitch = 0f, float roll = 0f, float swayAngle = 2.0f, float swaySpeed = 1.4f)
    {
        if (prefab == null) return null;
        var go = Instantiate(prefab, parent);
        go.name = name;
        Quaternion importedAxisCorrection = go.transform.localRotation;
        go.transform.position = Vector3.zero;
        go.transform.localRotation = Quaternion.Euler(pitch, yaw, roll) * importedAxisCorrection;
        go.transform.localScale = Vector3.one;
        JungleStageBuilder.DisableChildColliders(go);
        var renderers = go.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            Bounds b = JungleStageBuilder.CombinedBounds(renderers);
            float scale = height / Mathf.Max(0.01f, b.size.y);
            go.transform.localScale = Vector3.one * scale;
            b = JungleStageBuilder.CombinedBounds(renderers);
            go.transform.position += new Vector3(pos.x - b.center.x, pos.y - b.min.y, pos.z - b.center.z);
            if (mat != null) JungleStageBuilder.ApplySharedMaterial(renderers, mat);
        }
        else
        {
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * height;
        }
        if (swayAngle > 0.05f)
            JungleStageBuilder.AttachWindSway(go, swayAngle, swaySpeed);
        return go;
    }

    private static Texture2D CreateProceduralSandTexture()
    {
        int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGB24, true)
        {
            name = "Arena Dorada Procedural",
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear
        };
        Color sandDark = new Color(0.78f, 0.64f, 0.42f);
        Color sandLight = new Color(0.92f, 0.81f, 0.58f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float n1 = Mathf.PerlinNoise(x * 0.15f, y * 0.15f);
                float n2 = Mathf.PerlinNoise(x * 0.45f + 12f, y * 0.45f + 15f) * 0.35f;
                float blend = Mathf.Clamp01(n1 + n2 - 0.15f);
                tex.SetPixel(x, y, Color.Lerp(sandDark, sandLight, blend));
            }
        }
        tex.Apply(true, true);
        return tex;
    }

    private void BuildTropicalEnvironment()
    {
        var root = new GameObject("Escenario Tropical 3D").transform;

        // 1. Cargar modelos 3D y texturas auténticas del proyecto
        const string palmRoot = "Jungle/NewModels/Palm/Meshy_AI_Cartoon_Coconut_Palm_20261026_texture_fbx/";
        GameObject palmPrefab = JungleAssets.Load<GameObject>(palmRoot + "Meshy_AI_Cartoon_Coconut_Palm_20261026_texture");
        Texture2D palmAlbedo = JungleAssets.Load<Texture2D>(palmRoot + "Palm_Albedo");
        Texture2D palmNormal = JungleAssets.Load<Texture2D>(palmRoot + "Meshy_AI_Cartoon_Coconut_Palm_20261026_texture_normal");
        Material palmMat = MakeMaterialWithTexture(palmAlbedo, Color.white, "Palmera Tropical - Splash", 0.18f, palmNormal, 0.85f);

        const string bushRoot = "Jungle/NewModels/Bush/Meshy_AI_Emerald_Canopy_20261026_texture_fbx/";
        GameObject bushPrefab = JungleAssets.Load<GameObject>(bushRoot + "Meshy_AI_Emerald_Canopy_20261026_texture");
        Texture2D bushAlbedo = JungleAssets.Load<Texture2D>(bushRoot + "Bush_Albedo");
        Texture2D bushNormal = JungleAssets.Load<Texture2D>(bushRoot + "Meshy_AI_Emerald_Canopy_20261026_texture_normal");
        Material bushMat = MakeMaterialWithTexture(bushAlbedo, Color.white, "Arbusto Selva - Splash", 0.22f, bushNormal, 0.75f);

        const string coconutRoot = "Jungle/NewModels/Coconut/Meshy_AI_Coconut_Bowling_Ball_20261026_texture_fbx/";
        GameObject coconutPrefab = JungleAssets.Load<GameObject>(coconutRoot + "Meshy_AI_Coconut_Bowling_Ball_20261026_texture");
        Texture2D coconutAlbedo = JungleAssets.Load<Texture2D>(coconutRoot + "Coconut_Albedo");
        Texture2D coconutNormal = JungleAssets.Load<Texture2D>(coconutRoot + "Meshy_AI_Coconut_Bowling_Ball_20261026_texture_normal");
        Material coconutMat = MakeMaterialWithTexture(coconutAlbedo, Color.white, "Coco Suelo - Splash", 0.12f, coconutNormal, 0.8f);

        GameObject trunkPrefab = JungleAssets.Load<GameObject>("LogBalance/Models/trunk") ?? JungleAssets.Load<GameObject>("LogBalance/Prefabs/trunk");
        Material trunkMat = JungleAssets.Load<Material>("LogBalance/Materials/trunk_Color_C14") ?? JungleAssets.Load<Material>("LogBalance/Materials/trunk_Color_C09");

        const string drumRoot = "Jungle/NewModels/WhackAMole/Meshy_AI_Wooden_War_Drum_20261026_texture_fbx/";
        GameObject drumPrefab = JungleAssets.Load<GameObject>(drumRoot + "Meshy_AI_Wooden_War_Drum_20261026_texture");
        Texture2D drumAlbedo = JungleAssets.Load<Texture2D>(drumRoot + "Meshy_AI_Wooden_War_Drum_20261026_texture");
        Texture2D drumNormal = JungleAssets.Load<Texture2D>(drumRoot + "Meshy_AI_Wooden_War_Drum_20261026_texture_normal");
        Material drumMat = MakeMaterialWithTexture(drumAlbedo, Color.white, "Tambor Tribal - Splash", 0.25f, drumNormal, 0.8f);

        const string guardianRoot = "Jungle/BowlingPins/Meshy_AI_Banana_Guardian_20261024_texture_fbx/";
        GameObject guardianPrefab = JungleAssets.Load<GameObject>(guardianRoot + "Meshy_AI_Banana_Guardian_20261024_texture");
        Texture2D guardianAlbedo = JungleAssets.Load<Texture2D>(guardianRoot + "Meshy_AI_Banana_Guardian_20261024_albedo");
        Texture2D guardianNormal = JungleAssets.Load<Texture2D>(guardianRoot + "Meshy_AI_Banana_Guardian_20261024_texture_normal");
        Material guardianMat = MakeMaterialWithTexture(guardianAlbedo, Color.white, "Totem Guardian - Splash", 0.20f, guardianNormal, 0.8f);

        Texture2D texGround = JungleAssets.Load<Texture2D>("Jungle/bowling_jungle_ground") ?? CreateProceduralSandTexture();
        Material sandMat = MakeMaterialWithTexture(texGround, new Color(0.86f, 0.74f, 0.52f), "Arena Dorada - Splash", 0.05f);

        Texture2D texBamboo = JungleAssets.Load<Texture2D>("Jungle/SurfaceTextures/BowlingLaneBamboo");
        Material bambooMat = MakeMaterialWithTexture(texBamboo, new Color(0.85f, 0.70f, 0.42f), "Bambu Sendero - Splash", 0.25f);

        // 2. Arena y relieve del terreno
        var arena = JungleStageBuilder.CreatePrimitive(PrimitiveType.Cube, "Arena Principal",
            new Vector3(0f, -0.2f, 22f), new Vector3(75f, 0.4f, 80f), new Color(0.86f, 0.74f, 0.52f), root, smoothness: 0.05f);
        arena.GetComponent<Renderer>().sharedMaterial = sandMat;

        // Dunas naturales para romper la planicie
        CreateDune(root, sandMat, "Duna_Izq_1", new Vector3(-8.5f, -0.15f, 7.0f), new Vector3(8f, 0.9f, 15f));
        CreateDune(root, sandMat, "Duna_Izq_2", new Vector3(-10.5f, 0.1f, 19.0f), new Vector3(10f, 1.2f, 20f));
        CreateDune(root, sandMat, "Duna_Der_1", new Vector3(8.5f, -0.15f, 7.5f), new Vector3(8f, 0.9f, 15f));
        CreateDune(root, sandMat, "Duna_Der_2", new Vector3(10.5f, 0.1f, 20.0f), new Vector3(10f, 1.3f, 20f));

        // Sendero central del gorila
        var sendero = JungleStageBuilder.CreatePrimitive(PrimitiveType.Cube, "Sendero del Gorila",
            new Vector3(0f, -0.13f, 18f), new Vector3(5.6f, 0.10f, 44f), new Color(0.74f, 0.60f, 0.40f), root, smoothness: 0.06f);
        sendero.GetComponent<Renderer>().sharedMaterial = bambooMat;

        // Océano al horizonte (+Z)
        var ocean = JungleStageBuilder.CreatePrimitive(PrimitiveType.Cube, "Oceano Tropical",
            new Vector3(0f, -0.30f, 65f), new Vector3(120f, 0.26f, 60f), new Color(0.12f, 0.52f, 0.78f), root, smoothness: 0.68f);

        // Línea de espuma en la orilla
        JungleStageBuilder.CreatePrimitive(PrimitiveType.Cube, "Espuma Orilla",
            new Vector3(0f, -0.16f, 35.5f), new Vector3(80f, 0.04f, 1.4f), new Color(0.94f, 0.98f, 1.0f), root, smoothness: 0.25f);

        // Sol tropical cálido
        JungleStageBuilder.CreatePrimitive(PrimitiveType.Sphere, "Sol Dorado",
            new Vector3(8f, 16f, 68f), Vector3.one * 5.8f, new Color(1f, 0.96f, 0.65f), root);

        // 3. Palmeras auténticas del proyecto con composición cinematográfica (Verticales y erguidas)
        // Primer plano: Palmeras de encuadre (Framing)
        PlaceGroundedProp(palmPrefab, root, palmMat, "Palmera_Framing_Izq", new Vector3(-6.2f, 0f, 2.5f), 7.6f, yaw: 35f, swayAngle: 1.8f);
        PlaceGroundedProp(palmPrefab, root, palmMat, "Palmera_Framing_Der", new Vector3(6.2f, 0f, 2.8f), 7.8f, yaw: -30f, swayAngle: 1.8f);

        // Arboleda izquierda (Midground & Dunes)
        PlaceGroundedProp(palmPrefab, root, palmMat, "Palmera_Izq_1", new Vector3(-8.2f, 0.05f, 6.5f), 7.0f, yaw: 18f);
        PlaceGroundedProp(palmPrefab, root, palmMat, "Palmera_Izq_2", new Vector3(-10.4f, 0.20f, 9.5f), 6.0f, yaw: 45f);
        PlaceGroundedProp(palmPrefab, root, palmMat, "Palmera_Izq_3", new Vector3(-7.6f, 0.10f, 13.8f), 7.4f, yaw: -15f);
        PlaceGroundedProp(palmPrefab, root, palmMat, "Palmera_Izq_4", new Vector3(-10.8f, 0.30f, 18.2f), 6.5f, yaw: 32f);
        PlaceGroundedProp(palmPrefab, root, palmMat, "Palmera_Izq_5", new Vector3(-8.6f, 0.15f, 23.5f), 7.5f, yaw: -8f);

        // Arboleda derecha (Midground & Dunes)
        PlaceGroundedProp(palmPrefab, root, palmMat, "Palmera_Der_1", new Vector3(8.2f, 0.05f, 7.0f), 7.1f, yaw: -18f);
        PlaceGroundedProp(palmPrefab, root, palmMat, "Palmera_Der_2", new Vector3(10.5f, 0.20f, 10.2f), 6.2f, yaw: -38f);
        PlaceGroundedProp(palmPrefab, root, palmMat, "Palmera_Der_3", new Vector3(7.8f, 0.10f, 14.8f), 7.3f, yaw: 16f);
        PlaceGroundedProp(palmPrefab, root, palmMat, "Palmera_Der_4", new Vector3(10.8f, 0.30f, 19.5f), 6.4f, yaw: -28f);
        PlaceGroundedProp(palmPrefab, root, palmMat, "Palmera_Der_5", new Vector3(8.5f, 0.15f, 24.2f), 7.4f, yaw: 10f);

        // Palmeras de fondo (Horizonte de selva)
        PlaceGroundedProp(palmPrefab, root, palmMat, "Palmera_Fondo_1", new Vector3(-12.0f, 0.3f, 28.0f), 8.2f, yaw: 20f);
        PlaceGroundedProp(palmPrefab, root, palmMat, "Palmera_Fondo_2", new Vector3(-5.5f, 0.2f, 31.0f), 7.0f, yaw: -12f);
        PlaceGroundedProp(palmPrefab, root, palmMat, "Palmera_Fondo_3", new Vector3(5.5f, 0.2f, 31.5f), 7.2f, yaw: 14f);
        PlaceGroundedProp(palmPrefab, root, palmMat, "Palmera_Fondo_4", new Vector3(12.0f, 0.3f, 28.5f), 8.0f, yaw: -22f);

        // 4. Arbustos y sotobosque tropical (Emerald Canopy)
        // Costado izquierdo
        PlaceGroundedProp(bushPrefab, root, bushMat, "Arbusto_Izq_1", new Vector3(-5.0f, 0f, 3.2f), 1.6f, 25f);
        PlaceGroundedProp(bushPrefab, root, bushMat, "Arbusto_Izq_2", new Vector3(-6.8f, 0.1f, 6.2f), 1.9f, -40f);
        PlaceGroundedProp(bushPrefab, root, bushMat, "Arbusto_Izq_3", new Vector3(-7.6f, 0.2f, 10.0f), 1.5f, 15f);
        PlaceGroundedProp(bushPrefab, root, bushMat, "Arbusto_Izq_4", new Vector3(-5.2f, 0.1f, 13.5f), 1.8f, -20f);
        PlaceGroundedProp(bushPrefab, root, bushMat, "Arbusto_Izq_5", new Vector3(-7.8f, 0.25f, 18.0f), 2.1f, 35f);

        // Costado derecho
        PlaceGroundedProp(bushPrefab, root, bushMat, "Arbusto_Der_1", new Vector3(5.0f, 0f, 3.5f), 1.7f, -30f);
        PlaceGroundedProp(bushPrefab, root, bushMat, "Arbusto_Der_2", new Vector3(6.8f, 0.1f, 6.6f), 1.8f, 45f);
        PlaceGroundedProp(bushPrefab, root, bushMat, "Arbusto_Der_3", new Vector3(7.8f, 0.2f, 10.8f), 1.6f, -15f);
        PlaceGroundedProp(bushPrefab, root, bushMat, "Arbusto_Der_4", new Vector3(5.4f, 0.1f, 14.5f), 1.9f, 25f);
        PlaceGroundedProp(bushPrefab, root, bushMat, "Arbusto_Der_5", new Vector3(8.0f, 0.25f, 19.0f), 2.0f, -35f);

        // Fondo de letreros
        PlaceGroundedProp(bushPrefab, root, bushMat, "Arbusto_Sign_Story", new Vector3(-4.8f, 0f, 5.8f), 2.3f, 10f);
        PlaceGroundedProp(bushPrefab, root, bushMat, "Arbusto_Sign_Party", new Vector3(4.8f, 0f, 5.8f), 2.3f, -10f);
        PlaceGroundedProp(bushPrefab, root, bushMat, "Arbusto_Sign_Conn", new Vector3(2.5f, 0.5f, 10.2f), 2.2f, 0f);

        // 5. Troncos de playa (Driftwood)
        PlaceGroundedProp(trunkPrefab, root, trunkMat, "Tronco_Playa_Izq", new Vector3(-4.0f, 0.05f, 8.2f), 1.15f, yaw: 32f, pitch: 2f, roll: 8f, swayAngle: 0f);
        PlaceGroundedProp(trunkPrefab, root, trunkMat, "Tronco_Playa_Der", new Vector3(4.3f, 0.05f, 12.6f), 1.25f, yaw: -38f, pitch: -3f, roll: -6f, swayAngle: 0f);

        // 6. Cocos dispersos en la arena
        PlaceGroundedProp(coconutPrefab, root, coconutMat, "Coco_1", new Vector3(-4.1f, 0.06f, 2.9f), 0.42f, 15f, swayAngle: 0f);
        PlaceGroundedProp(coconutPrefab, root, coconutMat, "Coco_2", new Vector3(-4.5f, 0.06f, 3.3f), 0.38f, -40f, swayAngle: 0f);
        PlaceGroundedProp(coconutPrefab, root, coconutMat, "Coco_3", new Vector3(-3.8f, 0.06f, 8.5f), 0.45f, 22f, swayAngle: 0f);
        PlaceGroundedProp(coconutPrefab, root, coconutMat, "Coco_4", new Vector3(4.2f, 0.06f, 3.2f), 0.40f, -25f, swayAngle: 0f);
        PlaceGroundedProp(coconutPrefab, root, coconutMat, "Coco_5", new Vector3(4.6f, 0.06f, 3.6f), 0.44f, 50f, swayAngle: 0f);
        PlaceGroundedProp(coconutPrefab, root, coconutMat, "Coco_6", new Vector3(4.0f, 0.06f, 12.2f), 0.42f, -10f, swayAngle: 0f);

        // 7. Tambores tribales y guardianes
        PlaceGroundedProp(drumPrefab, root, drumMat, "Tambor_Story", new Vector3(-2.3f, 0f, 5.2f), 1.05f, yaw: 15f, swayAngle: 0f);
        PlaceGroundedProp(drumPrefab, root, drumMat, "Tambor_Party", new Vector3(2.3f, 0f, 5.2f), 1.05f, yaw: -15f, swayAngle: 0f);
        PlaceGroundedProp(drumPrefab, root, drumMat, "Tambor_Lateral", new Vector3(-5.8f, 0f, 9.8f), 1.18f, yaw: 0f, swayAngle: 0f);

        PlaceGroundedProp(guardianPrefab, root, guardianMat, "Totem_Guardian_Izq", new Vector3(-5.4f, 0f, 4.8f), 2.3f, yaw: 24f, swayAngle: 0f);
        PlaceGroundedProp(guardianPrefab, root, guardianMat, "Totem_Guardian_Der", new Vector3(5.4f, 0f, 4.8f), 2.3f, yaw: -24f, swayAngle: 0f);

        // 8. Rocas costeras decorativas
        for (int i = 0; i < 8; i++)
        {
            float rx = (i % 2 == 0 ? -1f : 1f) * UnityEngine.Random.Range(4.5f, 14f);
            float rz = UnityEngine.Random.Range(4f, 32f);
            var roca = JungleStageBuilder.CreatePrimitive(PrimitiveType.Sphere, "Roca_Playa_" + i,
                new Vector3(rx, -0.05f, rz),
                new Vector3(UnityEngine.Random.Range(1.1f, 2.2f), UnityEngine.Random.Range(0.6f, 1.2f), UnityEngine.Random.Range(1.1f, 2.2f)),
                new Color(0.46f, 0.42f, 0.38f), root, smoothness: 0.15f);
            roca.transform.rotation = Quaternion.Euler(UnityEngine.Random.Range(0f, 30f), UnityEngine.Random.Range(0f, 180f), 0f);
        }
    }

    private static void CreateDune(Transform parent, Material mat, string name, Vector3 pos, Vector3 scale)
    {
        var dune = JungleStageBuilder.CreatePrimitive(PrimitiveType.Sphere, name, pos, scale,
            new Color(0.86f, 0.74f, 0.52f), parent, smoothness: 0.05f);
        dune.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private void BuildGorillaCharacter()
    {
        gorillaRoot = new GameObject("Gorila Meshy AI Runner").transform;
        gorillaRoot.position = new Vector3(0f, 0f, GorillaStartZ);
        gorillaRoot.rotation = Quaternion.LookRotation(Vector3.back); // Hacia la cámara
        gorillaRig.LoadMascot(gorillaRoot, gorillaRoot.position, gorillaRoot.rotation, 2.8f, "Gorila Meshy - personaje de los juegos", runner: true);
        gorillaRig.PlayRunAnimation(1.15f);
    }

    private void Build3DSigns()
    {
        signsRoot = new GameObject("Letreros 3D").transform;

        string[] titles = { "MODO HISTORIA", "MODO FIESTA", "SALA Y CONEXIÓN" };
        string[] subs = { "Aventura y Escape con Kong", "Torneo 10 Disciplinas", "Escanear QR y Celulares" };
        string[] actions = { "STORY", "PARTY", "CONNECTION" };

        Vector3[] positions = new Vector3[]
        {
            new Vector3(-4.4f, 0f, 6.2f), // Modo Historia (Izquierda)
            new Vector3(4.4f, 0f, 6.2f),  // Modo Fiesta (Derecha)
            new Vector3(0f, 0.85f, 9.2f)  // Conexión QR (Centro al fondo detrás del gorila)
        };

        Texture2D texBamboo = JungleAssets.Load<Texture2D>("Jungle/SurfaceTextures/BowlingLaneBamboo");
        Material bambooMat = MakeMaterialWithTexture(texBamboo, new Color(0.80f, 0.65f, 0.38f), "Bambu Postes", 0.25f);

        for (int i = 0; i < 3; i++)
        {
            var signObj = new GameObject("Sign_" + actions[i]).transform;
            signObj.SetParent(signsRoot, false);
            signObj.position = positions[i];

            // Orientar levemente hacia el centro de la cámara
            float angleY = (i == 0) ? 20f : (i == 1) ? -20f : 0f;
            signObj.rotation = Quaternion.Euler(0f, angleY, 0f);

            // Postes de bambú gemelos
            var p1 = JungleStageBuilder.CreateLocalPrimitive(PrimitiveType.Cylinder, "Bambu_Izq",
                new Vector3(-1.50f, 1.45f, 0f), new Vector3(0.16f, 1.45f, 0.16f), new Color(0.60f, 0.46f, 0.24f), signObj);
            p1.GetComponent<Renderer>().sharedMaterial = bambooMat;

            var p2 = JungleStageBuilder.CreateLocalPrimitive(PrimitiveType.Cylinder, "Bambu_Der",
                new Vector3(1.50f, 1.45f, 0f), new Vector3(0.16f, 1.45f, 0.16f), new Color(0.60f, 0.46f, 0.24f), signObj);
            p2.GetComponent<Renderer>().sharedMaterial = bambooMat;

            // Cuerdas de soporte diagonales
            var ropeL = JungleStageBuilder.CreateLocalPrimitive(PrimitiveType.Cylinder, "Cuerda_Izq",
                new Vector3(-1.50f, 2.55f, 0f), new Vector3(0.045f, 0.52f, 0.045f), new Color(0.68f, 0.54f, 0.36f), signObj).transform;
            ropeL.localRotation = Quaternion.Euler(0f, 0f, 26f);

            var ropeR = JungleStageBuilder.CreateLocalPrimitive(PrimitiveType.Cylinder, "Cuerda_Der",
                new Vector3(1.50f, 2.55f, 0f), new Vector3(0.045f, 0.52f, 0.045f), new Color(0.68f, 0.54f, 0.36f), signObj).transform;
            ropeR.localRotation = Quaternion.Euler(0f, 0f, -26f);

            // Tabla de madera principal con collider
            Color woodColor = new Color(0.58f, 0.40f, 0.22f);
            var board = JungleStageBuilder.CreateLocalPrimitive(PrimitiveType.Cube, "TablaMadera",
                new Vector3(0f, 2.35f, 0f), new Vector3(3.2f, 1.25f, 0.18f), woodColor, signObj, keepCollider: true);

            // Marcos rústicos superior e inferior
            JungleStageBuilder.CreateLocalPrimitive(PrimitiveType.Cube, "MarcoSup",
                new Vector3(0f, 2.98f, 0f), new Vector3(3.3f, 0.12f, 0.22f), new Color(0.42f, 0.30f, 0.14f), signObj);
            JungleStageBuilder.CreateLocalPrimitive(PrimitiveType.Cube, "MarcoInf",
                new Vector3(0f, 1.72f, 0f), new Vector3(3.3f, 0.12f, 0.22f), new Color(0.42f, 0.30f, 0.14f), signObj);

            // Follaje decorativo en las esquinas superiores
            JungleStageBuilder.CreateLocalPrimitive(PrimitiveType.Sphere, "FollajeL",
                new Vector3(-1.65f, 2.9f, 0.05f), new Vector3(0.55f, 0.30f, 0.40f), new Color(0.18f, 0.56f, 0.22f), signObj);
            JungleStageBuilder.CreateLocalPrimitive(PrimitiveType.Sphere, "FollajeR",
                new Vector3(1.65f, 2.9f, 0.05f), new Vector3(0.55f, 0.30f, 0.40f), new Color(0.18f, 0.56f, 0.22f), signObj);

            // Texto tallado en 3D con sombra profunda (anclado a signObj para escala 1:1)
            var shadowTextGo = new GameObject("Texto3D_Sombra");
            shadowTextGo.transform.SetParent(signObj, false);
            shadowTextGo.transform.localPosition = new Vector3(0.015f, 2.37f, -0.12f);
            var tmShadow = shadowTextGo.AddComponent<TextMesh>();
            tmShadow.text = titles[i];
            tmShadow.fontSize = 28;
            tmShadow.characterSize = 0.070f;
            tmShadow.anchor = TextAnchor.MiddleCenter;
            tmShadow.alignment = TextAlignment.Center;
            tmShadow.color = new Color(0.14f, 0.08f, 0.03f, 0.95f);

            var textGo = new GameObject("Texto3D");
            textGo.transform.SetParent(signObj, false);
            textGo.transform.localPosition = new Vector3(0f, 2.39f, -0.14f);
            var tm = textGo.AddComponent<TextMesh>();
            tm.text = titles[i];
            tm.fontSize = 28;
            tm.characterSize = 0.070f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(1f, 0.94f, 0.42f);

            var rend = board.GetComponent<Renderer>();

            signs[i] = new SignData
            {
                root = signObj,
                board = board.transform,
                boardRenderer = rend,
                textMesh = tm,
                originalColor = woodColor,
                title = titles[i],
                subtitle = subs[i],
                action = actions[i],
                cameraTargetPos = new Vector3(positions[i].x * 0.35f, 2.05f, 1.35f)
            };
        }

        // Los letreros inician inactivos durante la cinemática inicial y se revelan en TransitionToMenu3D
        if (signsRoot != null)
            signsRoot.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (startedAt < 0f)
        {
            startedAt = Time.unscaledTime;
        }
        float elapsed = Time.unscaledTime - startedAt;

        switch (currentState)
        {
            case State.Cinematic:
                UpdateCinematic(elapsed);
                break;
            case State.Menu3D:
                UpdateMenu3D();
                break;
            case State.PartyModal:
                UpdatePartyModal();
                break;
            case State.ComicStory:
                UpdateComicStory();
                break;
            case State.PersonalPrep:
                UpdatePersonalPrep();
                break;
        }
    }

    private static bool IsAutomated => !string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("GORILLA_AUTO_FLOW"));

    private static bool CanSkipCinematic(float elapsed)
    {
        if (IsAutomated) return false;
        return elapsed > 0.8f && (Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1));
    }

    private void UpdateCinematic(float elapsed)
    {
        float progress = Mathf.Clamp01(elapsed / CinematicDuration);

        // Gorila Meshy AI corre libremente por el sendero hacia la cámara con zancada heroica
        float currentZ = Mathf.Lerp(GorillaStartZ, GorillaEndZ, progress);
        Vector3 currentPos = new Vector3(0f, 0f, currentZ);
        gorillaRoot.position = currentPos;
        gorillaRig.SetPositionAndRotation(currentPos, Quaternion.LookRotation(Vector3.back));
        gorillaRig.AnimateRun(elapsed, 1.25f);

        // Pasos procedurales rítmicos y pesados de Kong avanzando por la arena
        if (elapsed - lastFootstepTime >= 0.31f)
        {
            lastFootstepTime = elapsed;
            if (sfxSource != null)
            {
                float footstepVol = Mathf.Lerp(0.18f, 0.72f, progress);
                sfxSource.pitch = UnityEngine.Random.Range(0.85f, 1.05f);
                sfxSource.PlayOneShot(JungleAudioBank.LeftDrum, footstepVol);
            }
        }

        // Suave avance de cámara cinematográfica: encuadre heroico y dinámico
        cam.fieldOfView = Mathf.Lerp(48f, 38f, progress);
        cam.transform.position = Vector3.Lerp(new Vector3(3.2f, 2.2f, .2f), new Vector3(2.2f, 1.9f, 1.2f), progress);
        cam.transform.rotation = Quaternion.LookRotation(currentPos + Vector3.up * 1.4f - cam.transform.position);

        if (progress >= 1f || CanSkipCinematic(elapsed))
        {
            TransitionToMenu3D();
        }
    }

    private void TransitionToMenu3D()
    {
        cam.fieldOfView = 58f;
        Vector3 menuPos = new Vector3(0f, 0f, GorillaEndZ);
        gorillaRoot.position = menuPos;
        gorillaRig.SetPositionAndRotation(menuPos, Quaternion.LookRotation(Vector3.back));
        gorillaRig.PlayIdleAnimation();
        if (signsRoot != null)
            signsRoot.gameObject.SetActive(true);

        // Golpe de pecho y redoble de tambores en la llegada
        if (sfxSource != null)
        {
            sfxSource.pitch = 1.0f;
            sfxSource.PlayOneShot(JungleAudioBank.LeftDrum, 0.85f);
            sfxSource.PlayOneShot(JungleAudioBank.RightDrum, 0.90f);
        }

        currentState = State.Menu3D;
    }

    private void UpdateMenu3D()
    {
        gorillaRig.Animate(false, 0, 0);

        if (!IsAutomated)
        {
            // Navegación por teclado (Flechas / A-D)
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                selectedSign = (selectedSign - 1 + signs.Length) % signs.Length;
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                selectedSign = (selectedSign + 1) % signs.Length;
            }

            UpdateMouseRaycast();

            // Confirmación
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) ||
                (hoveredSign >= 0 && Input.GetMouseButtonDown(0)))
            {
                TriggerSignAction((hoveredSign >= 0) ? hoveredSign : selectedSign);
                return;
            }
        }

        int activeIndex = Mathf.Clamp((hoveredSign >= 0) ? hoveredSign : selectedSign, 0, signs.Length - 1);

        // Retroalimentación de audio y animación procedural de squash/stretch al cambiar de letrero
        if (activeIndex != lastAudibleSign)
        {
            if (lastAudibleSign >= 0 && sfxSource != null)
            {
                sfxSource.pitch = 1.25f;
                sfxSource.PlayOneShot(JungleAudioBank.UiClick, 0.50f);
            }
            if (signs[activeIndex].board != null)
            {
                JungleStageBuilder.PulseSquash(signs[activeIndex].board.gameObject, 0.88f, 1.12f, 22f, 8f);
            }
            lastAudibleSign = activeIndex;
        }

        // Animación procedural de escala elástica en el letrero enfocado
        float pulse = 1f + Mathf.Sin(Time.unscaledTime * 3.4f) * 0.035f;
        for (int i = 0; i < signs.Length; i++)
        {
            if (signs[i].root != null)
            {
                float targetScale = (i == activeIndex) ? 1.05f * pulse : 1.0f;
                signs[i].root.localScale = Vector3.Lerp(signs[i].root.localScale, Vector3.one * targetScale, Time.unscaledDeltaTime * 8f);
            }
        }

        // Suave movimiento de cámara hacia el letrero enfocado con oscilación sutil de brisa marina
        Vector3 targetCamPos = signs[activeIndex].cameraTargetPos;
        float camDriftX = Mathf.Sin(Time.unscaledTime * 0.8f) * 0.018f;
        float camDriftY = Mathf.Cos(Time.unscaledTime * 1.2f) * 0.012f;
        Vector3 driftPos = targetCamPos + new Vector3(camDriftX, camDriftY, 0f);
        cam.transform.position = Vector3.Lerp(cam.transform.position, driftPos, Time.unscaledDeltaTime * 4f);
        Quaternion targetCamRot = Quaternion.Euler(5.5f, signs[activeIndex].root.position.x * 2.2f, 0f);
        cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, targetCamRot, Time.unscaledDeltaTime * 4f);

        // Resaltar el letrero activo con brillo tropical
        for (int i = 0; i < signs.Length; i++)
        {
            if (signs[i].boardRenderer != null)
            {
                Color targetCol = (i == activeIndex) ? new Color(0.85f, 0.65f, 0.35f) : signs[i].originalColor;
                signs[i].boardRenderer.material.color = Color.Lerp(signs[i].boardRenderer.material.color, targetCol, Time.unscaledDeltaTime * 8f);
            }
            if (signs[i].textMesh != null)
            {
                Color targetTextCol = (i == activeIndex) ? new Color(1f, 0.98f, 0.30f) : new Color(0.95f, 0.88f, 0.50f);
                signs[i].textMesh.color = targetTextCol;
            }
        }
    }

    private void UpdateMouseRaycast()
    {
        hoveredSign = -1;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 200f))
        {
            for (int i = 0; i < signs.Length; i++)
            {
                if (signs[i].board != null && (hit.collider.transform == signs[i].board || hit.collider.transform.IsChildOf(signs[i].root)))
                {
                    hoveredSign = i;
                    selectedSign = i;
                    break;
                }
            }
        }
    }

    private void TriggerSignAction(int index)
    {
        if (sfxSource != null)
        {
            sfxSource.pitch = 1.0f;
            sfxSource.PlayOneShot(JungleAudioBank.CountdownGo, 0.80f);
            sfxSource.PlayOneShot(JungleAudioBank.BatHit, 0.65f);
        }

        switch (signs[index].action)
        {
            case "STORY":
                comicPanelIndex = 0;
                gorillaRig.PlayAngryStompAnimation();
                currentState = State.ComicStory;
                break;
            case "PARTY":
                gorillaRig.PlayCelebrationAnimation();
                currentState = State.PartyModal;
                break;
            case "CONNECTION":
            default:
                LoadTargetScene(SceneNavigator.Connection);
                break;
        }
    }

    private void UpdatePartyModal()
    {
        if (IsAutomated) return;
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
        {
            if (sfxSource != null) sfxSource.PlayOneShot(JungleAudioBank.LogWin, 0.80f);
            TournamentSession.StartPartyMode();
            currentState = State.Exiting;
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (sfxSource != null) sfxSource.PlayOneShot(JungleAudioBank.UiClick, 0.50f);
            currentState = State.Menu3D;
        }
    }

    private void UpdateComicStory()
    {
        if (IsAutomated) return;
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            if (sfxSource != null) sfxSource.PlayOneShot(JungleAudioBank.Pitch, 0.45f);
            AdvanceComic();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (sfxSource != null) sfxSource.PlayOneShot(JungleAudioBank.Pitch, 0.45f);
            comicPanelIndex = Mathf.Max(0, comicPanelIndex - 1);
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (sfxSource != null) sfxSource.PlayOneShot(JungleAudioBank.UiClick, 0.50f);
            SkipComicToPrep();
        }
    }

    private void AdvanceComic()
    {
        if (comicPanelIndex >= ComicPanels.Length - 1)
        {
            prepStepIndex = 0;
            currentState = State.PersonalPrep;
        }
        else
        {
            comicPanelIndex++;
        }
    }

    private void SkipComicToPrep()
    {
        prepStepIndex = 0;
        currentState = State.PersonalPrep;
    }

    private void UpdatePersonalPrep()
    {
        if (IsAutomated) return;
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            AdvancePrep();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            prepStepIndex = Mathf.Max(0, prepStepIndex - 1);
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            LaunchStoryDiscipline();
        }
    }

    private void AdvancePrep()
    {
        if (prepStepIndex >= PrepSteps.Length - 1)
        {
            LaunchStoryDiscipline();
        }
        else
        {
            prepStepIndex++;
        }
    }

    private void LaunchStoryDiscipline()
    {
        currentState = State.Exiting;
        TournamentSession.StartStoryMode();
    }

    private void LoadTargetScene(string sceneName)
    {
        currentState = State.Exiting;
        SceneNavigator.Load(sceneName);
    }

    private static bool AnyInputSkip()
    {
        return Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);
    }

    // -------------------------------------------------------------
    // RENDERIZADO VISUAL CON JUNGLE UI THEME (UI TOOLKIT)
    // -------------------------------------------------------------
    public void RenderUI()
    {
        if (cam == null) return;
        JungleUITheme.Ensure();

        int sw = Screen.width;
        int sh = Screen.height;

        var titleStyle = new GUIStyle(JungleRetainedUI.skin.label)
        {
            font = JungleUITheme.TitleFont,
            fontSize = Mathf.Clamp(sh / 15, 32, 54),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.88f, 0.20f) }
        };

        var subtitleStyle = new GUIStyle(JungleRetainedUI.skin.label)
        {
            font = JungleUITheme.TextFont,
            fontSize = Mathf.Clamp(sh / 32, 16, 24),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };

        var tipStyle = new GUIStyle(subtitleStyle)
        {
            fontSize = Mathf.Clamp(sh / 38, 14, 18),
            normal = { textColor = new Color(0.95f, 0.92f, 0.82f, 0.95f) }
        };

        if (currentState == State.Cinematic)
        {
            Rect titleRect = new Rect(0, sh * 0.06f, sw, 65);
            JungleUITheme.DrawShadowedLabel(titleRect, "GORILLA ESCAPE", titleStyle, 3f);

            float pillW = Mathf.Min(sw * 0.82f, 620f);
            float pillH = 42f;
            Rect pillRect = new Rect((sw - pillW) * 0.5f, sh * 0.88f, pillW, pillH);

            JungleUITheme.DrawSubPanel(pillRect);
            var tipBright = new GUIStyle(tipStyle) { normal = { textColor = new Color(1f, 0.95f, 0.82f) } };
            JungleUITheme.DrawShadowedLabel(new Rect(pillRect.x, pillRect.y + 4f, pillRect.width, 32f), "Preparando la isla sagrada... [Espacio o Clic para omitir]", tipBright, 1.5f);
        }
        else if (currentState == State.Menu3D)
        {
            Rect titleRect = new Rect(0, sh * 0.05f, sw, 55);
            JungleUITheme.DrawShadowedLabel(titleRect, "GORILLA ESCAPE", titleStyle, 3f);

            string netStatus = (services != null && services.CameraConnected)
                ? "CÁMARA Y QR LISTOS"
                : ((services != null && services.IsRunning) ? "SERVIDOR Y QR EN LÍNEA" : "SERVICIOS EN SEGUNDO PLANO");
            Rect statusRect = new Rect(sw - 246f, 16f, 230f, 32f);
            JungleUITheme.DrawSubPanel(statusRect);
            var statusStyle = new GUIStyle(tipStyle) { fontSize = 12, normal = { textColor = new Color(0.4f, 1f, 0.5f) } };
            JungleRetainedUI.Label(statusRect, "* " + netStatus, statusStyle);

            for (int i = 0; i < signs.Length; i++)
            {
                if (signs[i].board == null) continue;
                Vector3 sp = cam.WorldToScreenPoint(signs[i].board.position + Vector3.up * 0.15f);
                if (sp.z < 0f) continue;

                sp.y = sh - sp.y;
                bool isFocused = (i == ((hoveredSign >= 0) ? hoveredSign : selectedSign));

                float subW = 320f;
                float subH = 32f;
                Rect subRect = new Rect(sp.x - subW * 0.5f, sp.y + 42f, subW, subH);

                if (isFocused)
                    JungleUITheme.DrawBadge(subRect, signs[i].subtitle, highlighted: true);
                else
                    JungleUITheme.DrawPlankLabel(subRect, signs[i].subtitle);

                Rect clickRect = new Rect(sp.x - 160f, sp.y - 35f, 320f, 85f);
                if (JungleRetainedUI.Button(clickRect, "", GUIStyle.none))
                {
                    TriggerSignAction(i);
                }
            }

            float navPillW = Mathf.Min(sw * 0.75f, 540f);
            float navPillH = 38f;
            Rect navPillRect = new Rect((sw - navPillW) * 0.5f, sh * 0.915f, navPillW, navPillH);
            JungleUITheme.DrawSubPanel(navPillRect);
            var navTipStyle = new GUIStyle(tipStyle) { fontSize = 15, normal = { textColor = new Color(1f, 0.95f, 0.82f) } };
            JungleUITheme.DrawShadowedLabel(new Rect(navPillRect.x, navPillRect.y + 4f, navPillW, 28f), "Usa [Izquierda / Derecha] o haz clic en un letrero para entrar", navTipStyle, 1.5f);
            JungleUITheme.DrawLeaves();
        }
        else if (currentState == State.PartyModal)
        {
            DrawPartyModal(sw, sh, titleStyle, subtitleStyle);
            JungleUITheme.DrawLeaves();
        }
        else if (currentState == State.ComicStory)
        {
            DrawComicModal(sw, sh, titleStyle, subtitleStyle, tipStyle);
            JungleUITheme.DrawLeaves();
        }
        else if (currentState == State.PersonalPrep)
        {
            DrawPrepModal(sw, sh, titleStyle, subtitleStyle);
            JungleUITheme.DrawLeaves();
        }
    }

    private void DrawPartyModal(int sw, int sh, GUIStyle titleStyle, GUIStyle subtitleStyle)
    {
        Color prev = JungleRetainedUI.color;
        JungleRetainedUI.color = new Color(0f, 0f, 0f, 0.65f);
        JungleRetainedUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
        JungleRetainedUI.color = prev;

        float cardW = Mathf.Min(sw * 0.80f, 760f);
        float cardH = Mathf.Min(sh * 0.70f, 480f);
        Rect cardRect = new Rect((sw - cardW) * 0.5f, (sh - cardH) * 0.5f, cardW, cardH);

        JungleUITheme.DrawPanel(cardRect, selected: true);

        var partyTitleStyle = new GUIStyle(titleStyle) { fontSize = 32 };
        JungleUITheme.DrawShadowedLabel(new Rect(cardRect.x, cardRect.y + 25f, cardW, 45f), "MODO FIESTA — GORILIMPIADAS", partyTitleStyle, 2.5f);

        var bodyStyle = new GUIStyle(subtitleStyle)
        {
            fontSize = 19,
            wordWrap = true,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.98f, 0.90f) }
        };
        string partyDesc = "Compite de 1 a 4 jugadores en las 10 disciplinas tropicales.\n" +
                           "Cada jugador puede usar su teléfono como control de movimiento\n" +
                           "o jugar frente a la cámara web en pruebas de cuerpo entero.\n\n" +
                           "¡Conecta a tus amigos escaneando el código QR en la siguiente pantalla!";
        JungleUITheme.DrawSubPanel(new Rect(cardRect.x + 36f, cardRect.y + 84f, cardW - 72f, cardH * 0.50f));
        JungleRetainedUI.Label(new Rect(cardRect.x + 48f, cardRect.y + 92f, cardW - 96f, cardH * 0.46f), partyDesc, bodyStyle);

        float btnW = 210f;
        float btnH = 48f;
        float btnY = cardRect.y + cardH - 68f;

        if (JungleRetainedUI.Button(new Rect(cardRect.x + 46f, btnY, btnW, btnH), "< Volver (ESC)", JungleUITheme.Button))
        {
            JungleAudioBank.PlayUiClick();
            currentState = State.Menu3D;
        }

        if (JungleRetainedUI.Button(new Rect(cardRect.x + cardW - btnW - 46f, btnY, btnW, btnH), "Crear Sala QR >", JungleUITheme.ButtonSelected))
        {
            JungleAudioBank.PlayUiClick();
            TournamentSession.StartPartyMode();
            currentState = State.Exiting;
        }
    }

    private void DrawComicModal(int sw, int sh, GUIStyle titleStyle, GUIStyle subtitleStyle, GUIStyle tipStyle)
    {
        Color prev = JungleRetainedUI.color;
        JungleRetainedUI.color = new Color(0f, 0f, 0f, 0.70f);
        JungleRetainedUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
        JungleRetainedUI.color = prev;

        float cardW = Mathf.Min(sw * 0.85f, 880f);
        float cardH = Mathf.Min(sh * 0.80f, 560f);
        Rect cardRect = new Rect((sw - cardW) * 0.5f, (sh - cardH) * 0.5f, cardW, cardH);

        JungleUITheme.DrawPanel(cardRect, selected: true);

        ComicPanel p = ComicPanels[comicPanelIndex];

        var tagStyle = new GUIStyle(subtitleStyle)
        {
            fontSize = 15,
            normal = { textColor = new Color(1f, 0.85f, 0.35f) }
        };
        JungleRetainedUI.Label(new Rect(cardRect.x, cardRect.y + 20f, cardW, 25f), p.tag, tagStyle);

        var panelTitleStyle = new GUIStyle(titleStyle)
        {
            fontSize = 32,
            normal = { textColor = new Color(1f, 0.92f, 0.30f) }
        };
        JungleUITheme.DrawShadowedLabel(new Rect(cardRect.x, cardRect.y + 48f, cardW, 45f), p.title, panelTitleStyle, 2.5f);

        Rect imgRect = new Rect(cardRect.x + 35f, cardRect.y + 105f, cardW * 0.38f, cardH * 0.50f);
        Texture2D panelTex = (comicPanelIndex >= 2) ? texGorillaAudience : texGorillaMenu;
        if (panelTex != null)
        {
            JungleUITheme.DrawSubPanel(new Rect(imgRect.x - 4f, imgRect.y - 4f, imgRect.width + 8f, imgRect.height + 8f));
            JungleRetainedUI.DrawTexture(imgRect, panelTex, ScaleMode.ScaleToFit);
        }

        float textX = (panelTex != null) ? (cardRect.x + cardW * 0.43f) : (cardRect.x + 40f);
        float textW = (panelTex != null) ? (cardW * 0.52f) : (cardW - 80f);
        var narrativeStyle = new GUIStyle(subtitleStyle)
        {
            fontSize = 19,
            wordWrap = true,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(1f, 0.98f, 0.92f) }
        };
        JungleRetainedUI.Label(new Rect(textX, cardRect.y + 105f, textW, cardH * 0.48f), p.narrative, narrativeStyle);

        var contextStyle = new GUIStyle(tipStyle)
        {
            normal = { textColor = new Color(1f, 0.90f, 0.65f) },
            alignment = TextAnchor.MiddleCenter
        };
        JungleRetainedUI.Label(new Rect(cardRect.x + 20f, cardRect.y + cardH - 88f, cardW - 40f, 26f), p.tip, contextStyle);

        float btnW = 175f;
        float btnH = 44f;
        float btnY = cardRect.y + cardH - 56f;

        if (comicPanelIndex > 0)
        {
            if (JungleRetainedUI.Button(new Rect(cardRect.x + 40f, btnY, btnW, btnH), "< Anterior", JungleUITheme.Button))
            {
                JungleAudioBank.PlayUiClick();
                comicPanelIndex--;
            }
        }

        string nextLabel = (comicPanelIndex >= ComicPanels.Length - 1) ? "¡Prepararse! >" : "Siguiente >";
        if (JungleRetainedUI.Button(new Rect(cardRect.x + cardW - btnW - 40f, btnY, btnW, btnH), nextLabel, JungleUITheme.ButtonSelected))
        {
            JungleAudioBank.PlayUiClick();
            AdvanceComic();
        }

        if (JungleRetainedUI.Button(new Rect(cardRect.x + (cardW - 150f) * 0.5f, btnY, 150f, btnH), "Omitir (ESC)", JungleUITheme.Button))
        {
            JungleAudioBank.PlayUiClick();
            SkipComicToPrep();
        }
    }

    private void DrawPrepModal(int sw, int sh, GUIStyle titleStyle, GUIStyle subtitleStyle)
    {
        Color prev = JungleRetainedUI.color;
        JungleRetainedUI.color = new Color(0f, 0f, 0f, 0.70f);
        JungleRetainedUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
        JungleRetainedUI.color = prev;

        float cardW = Mathf.Min(sw * 0.85f, 820f);
        float cardH = Mathf.Min(sh * 0.75f, 500f);
        Rect cardRect = new Rect((sw - cardW) * 0.5f, (sh - cardH) * 0.5f, cardW, cardH);

        JungleUITheme.DrawPanel(cardRect, selected: true);

        var prepTitleStyle = new GUIStyle(titleStyle)
        {
            fontSize = 30,
            normal = { textColor = new Color(1f, 0.90f, 0.25f) }
        };
        JungleUITheme.DrawShadowedLabel(new Rect(cardRect.x, cardRect.y + 25f, cardW, 40f), "PREPARACIÓN PERSONAL", prepTitleStyle, 2.5f);

        var stepHeaderStyle = new GUIStyle(subtitleStyle)
        {
            fontSize = 16,
            normal = { textColor = new Color(1f, 0.85f, 0.35f) }
        };
        JungleRetainedUI.Label(new Rect(cardRect.x, cardRect.y + 70f, cardW, 26f), "Paso " + (prepStepIndex + 1) + " de " + PrepSteps.Length, stepHeaderStyle);

        var bodyStyle = new GUIStyle(subtitleStyle)
        {
            fontSize = 20,
            wordWrap = true,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.98f, 0.92f) }
        };
        JungleUITheme.DrawSubPanel(new Rect(cardRect.x + 42f, cardRect.y + 112f, cardW - 84f, cardH * 0.50f));
        JungleRetainedUI.Label(new Rect(cardRect.x + 54f, cardRect.y + 120f, cardW - 108f, cardH * 0.46f), PrepSteps[prepStepIndex], bodyStyle);

        float btnW = 175f;
        float btnH = 44f;
        float btnY = cardRect.y + cardH - 58f;

        if (prepStepIndex > 0)
        {
            if (JungleRetainedUI.Button(new Rect(cardRect.x + 40f, btnY, btnW, btnH), "< Anterior", JungleUITheme.Button))
            {
                JungleAudioBank.PlayUiClick();
                prepStepIndex--;
            }
        }

        string actionText = (prepStepIndex >= PrepSteps.Length - 1) ? "¡A Jugar! >" : "Siguiente >";
        if (JungleRetainedUI.Button(new Rect(cardRect.x + cardW - btnW - 40f, btnY, btnW, btnH), actionText, JungleUITheme.ButtonSelected))
        {
            JungleAudioBank.PlayUiClick();
            AdvancePrep();
        }

        if (JungleRetainedUI.Button(new Rect(cardRect.x + (cardW - 150f) * 0.5f, btnY, 150f, btnH), "Omitir (ESC)", JungleUITheme.Button))
        {
            JungleAudioBank.PlayUiClick();
            LaunchStoryDiscipline();
        }
    }

    // -------------------------------------------------------------
    // AUTOMACIÓN DE CAPTURA VISUAL PARA LABORATORIO QA (10 EVIDENCIAS)
    // -------------------------------------------------------------
    private IEnumerator AutomationScreenshotHookRoutine()
    {
        string screenshotDir = System.Environment.GetEnvironmentVariable("GORILLA_AUTO_SCREENSHOT_DIR");
        string autoFlow = System.Environment.GetEnvironmentVariable("GORILLA_AUTO_FLOW");

        if (string.IsNullOrEmpty(screenshotDir) && string.IsNullOrEmpty(autoFlow))
            yield break;

        if (string.IsNullOrEmpty(screenshotDir))
            screenshotDir = Path.Combine(Application.dataPath, "../../Reports/QA/Screenshots");

        Directory.CreateDirectory(screenshotDir);

        // 1. Evidencia 1: Primer fotograma del Splash
        yield return new WaitForSeconds(0.4f);
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(screenshotDir, "evidence_01_splash_first_frame.png"));
        yield return null;
        yield return null;

        // 2. Evidencia 2: Gorila corriendo libremente hacia la cámara
        yield return new WaitForSeconds(1.8f);
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(screenshotDir, "evidence_02_gorilla_running.png"));
        yield return null;
        yield return null;

        // Transición suave al Menú 3D (Vista panorámica)
        TransitionToMenu3D();
        selectedSign = 2; // Centro (Sala y Conexión)
        cam.transform.position = new Vector3(0f, 2.1f, 1.2f);
        cam.transform.rotation = Quaternion.Euler(4.5f, 0f, 0f);
        yield return new WaitForSeconds(0.8f);

        // 3. Evidencia 3: Menú 3D con letreros físicos integrados
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(screenshotDir, "evidence_03_menu_3d_signs.png"));
        yield return null;
        yield return null;

        // 4. Evidencia 4: Enfoque / selección de letrero (Dolly hacia Modo Fiesta)
        selectedSign = 1; // Modo Fiesta enfocado
        cam.transform.position = signs[1].cameraTargetPos;
        cam.transform.rotation = Quaternion.Euler(5.5f, signs[1].root.position.x * 2.2f, 0f);
        yield return new WaitForSeconds(0.8f);
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(screenshotDir, "evidence_04_sign_focus.png"));
        yield return null;
        yield return null;

        // 5. Evidencia 5: Selección / entrada Modo Fiesta modal
        currentState = State.PartyModal;
        yield return new WaitForSeconds(0.6f);
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(screenshotDir, "evidence_05_party_modal.png"));
        yield return null;
        yield return null;

        // 6. Evidencia 6: Historieta Pantalla 1 (Naufragio)
        currentState = State.ComicStory;
        comicPanelIndex = 0;
        yield return new WaitForSeconds(0.6f);
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(screenshotDir, "evidence_06_comic_panel_1.png"));
        yield return null;
        yield return null;

        // 7. Evidencia 7: Historieta Pantalla Final (Las Pruebas Sagradas del Náufrago)
        comicPanelIndex = ComicPanels.Length - 1;
        yield return new WaitForSeconds(0.6f);
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(screenshotDir, "evidence_07_comic_panel_final.png"));
        yield return null;
        yield return null;

        // 8. Evidencia 8: Pantalla de Preparación Personal («Prepárate para jugar»)
        currentState = State.PersonalPrep;
        prepStepIndex = 0;
        yield return new WaitForSeconds(0.6f);
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(screenshotDir, "evidence_08_personal_preparation.png"));
        yield return null;
        yield return null;

        // 9. Transición al minijuego 1 (Gorilla Smash)
        TournamentSession.StartStoryMode();
        currentState = State.Exiting;
        yield return new WaitForSeconds(0.4f);
        // The automated story flow must also stop at the connection gate.
    }
}
