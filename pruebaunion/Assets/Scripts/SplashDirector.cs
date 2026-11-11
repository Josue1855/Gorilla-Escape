using System.Collections;
using System.IO;
using GorillaEscape.Gameplay.Input;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public sealed class SplashDirector : MonoBehaviour
{
    public enum State
    {
        Cinematic,
        Menu3D,
        PartyModal,
        ComicStory,
        PersonalPrep,
        Exiting,
        StudioLogo
    }

    [Header("Estado")]
    public State currentState = State.Cinematic;

    private float startedAt;
    private float menuLogoStartedAt;
    private static bool enterMenuAfterConnection;
    private PhoneMenuPointer menuPointer;

    public static void OpenConnectedMenu()
    {
        enterMenuAfterConnection = true;
        SceneNavigator.Load(SceneNavigator.Splash);
    }
    private const float StudioLogoDuration = 5.2f;
    private Texture2D studioLogo;
    private Texture2D hangingLogo;
    private Camera cam;
    private sealed class SplashRunner
    {
        public Transform Root;
        public Transform Model;
        public Vector3 RestPosition;
        public Quaternion RestRotation;
        public Vector3 Offset;
        public PlayableGraph Graph;
        public AnimationClipPlayable Clip;
        public SpriteRenderer Dust;
    }

    private readonly System.Collections.Generic.List<SplashRunner> runners = new();
    private Sprite dustSprite;
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

    private const float CinematicDuration = 3.6f;
    private static readonly Vector3 RunnerStart = new Vector3(-10f, 0f, 6.15f);
    private static readonly Vector3 RunnerEnd = new Vector3(16.5f, 0f, 6.15f);

    // Texturas de ambientación
    private Texture2D texGorillaMenu;
    private Texture2D texGorillaAudience;
    private Texture2D texGorillaTaunt;

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
            narrative = "1. Coconut Throw: lanza el coco.\n2. Coconut Slice: corta los cocos al vuelo.\n3. Whack-a-Mole: atrapa a los gorilas traviesos.\n4. Banana Bowling: derriba los pinos.\n¡Supera las pruebas de Kong para construir tu balsa y escapar!",
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
        bool showMenu = enterMenuAfterConnection;
        enterMenuAfterConnection = false;
        currentState = State.StudioLogo;
        startedAt = -1f;
        LoadSplashLogos();
        LoadAssets();
        EnsureLighting();
        BuildTropicalEnvironment();
        BuildGorillaCharacter();
        Build3DSigns();
        EnsureCinematicCamera();
        AudioClip startup = Resources.Load<AudioClip>("Audio/StudioStartup");
        if (showMenu)
        {
            menuPointer = gameObject.AddComponent<PhoneMenuPointer>();
            currentState = State.Cinematic;
        }
        else if (startup != null)
        {
            sfxSource.PlayOneShot(startup, .82f);
        }

        // Durante el splash, los letreros permanecen ocultos para que el gorila corra con vista despejada
        if (signsRoot != null)
            signsRoot.gameObject.SetActive(false);

        // Bootstrap asíncrono no bloqueante
        StartCoroutine(AsyncBootstrapRoutine());

        // Hook para automatización de capturas si está configurado
    }

    private void OnDestroy()
    {
        if (hangingLogo != null)
        {
            Destroy(hangingLogo);
        }
        foreach (SplashRunner runner in runners)
            if (runner.Graph.IsValid()) runner.Graph.Destroy();
        if (dustSprite != null) Destroy(dustSprite);
        if (skyMaterial != null) Destroy(skyMaterial);
    }

    private void LoadSplashLogos()
    {
        const string folder = "Jungle/SplashModels/";
        studioLogo = Resources.Load<Texture2D>(folder + "SplashStudioLogo");
        Texture2D image = Resources.Load<Texture2D>(folder + "SplashGameLogo");
        if (image == null) return;
        if (!image.isReadable)
        {
            Debug.LogWarning("Activa Read/Write en SplashGameLogo para dibujar el letrero.");
            return;
        }

        Color32[] pixels = image.GetPixels32();
        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 pixel = pixels[i];
            int brightest = Mathf.Max(pixel.r, Mathf.Max(pixel.g, pixel.b));
            pixel.a = (byte)Mathf.RoundToInt(pixel.a * Mathf.Clamp01((brightest - 9f) / 17f));
            pixels[i] = pixel;
        }
        hangingLogo = new Texture2D(image.width, image.height, TextureFormat.RGBA32, false)
        {
            name = "Logo colgante con transparencia",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        hangingLogo.SetPixels32(pixels);
        hangingLogo.Apply(false, true);
    }

    private void DrawStudioLogo()
    {
        if (Event.current.type != EventType.Repaint) return;
        Color previous = GUI.color;
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        if (studioLogo != null)
        {
            float elapsed = Time.unscaledTime - startedAt;
            float alpha = Mathf.Min(Mathf.Clamp01(elapsed / .38f),
                Mathf.Clamp01((StudioLogoDuration - elapsed) / .4f));
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(new Rect(Screen.width * .16f, Screen.height * .10f,
                Screen.width * .68f, Screen.height * .80f), studioLogo, ScaleMode.ScaleToFit);
        }
        GUI.color = previous;
    }

    private void DrawHangingLogo(float width, float height)
    {
        if (hangingLogo == null) return;
        float logoHeight = Mathf.Min(height * .69f, width * .48f);
        float logoWidth = logoHeight * hangingLogo.width / hangingLogo.height;
        float elapsed = Mathf.Max(0f, Time.unscaledTime - menuLogoStartedAt);
        float lower = Mathf.Clamp01(elapsed / 1.15f);
        float eased = Mathf.SmoothStep(0f, 1f, lower);
        float settle = Mathf.Max(0f, elapsed - 1.15f);
        float bounce = Mathf.Sin(settle * 16f) * Mathf.Exp(-settle * 6f) * logoHeight * .035f;
        float y = Mathf.Lerp(-logoHeight, height * .305f - logoHeight, eased) + bounce;
        float top = Mathf.Max(0f, y);
        float visibleHeight = y + logoHeight - top;
        if (visibleHeight <= 0f) return;
        Rect destination = new Rect((width - logoWidth) * .5f, top, logoWidth, visibleHeight);
        Rect uv = new Rect(0f, 0f, 1f, visibleHeight / logoHeight);
        GUI.DrawTextureWithTexCoords(destination, hangingLogo, uv, true);
    }

    private void LoadAssets()
    {
        texGorillaMenu = Resources.Load<Texture2D>("Jungle/gorila_menu_hd");
        if (texGorillaMenu == null) texGorillaMenu = Resources.Load<Texture2D>("Jungle/gorila_menu");
        texGorillaAudience = Resources.Load<Texture2D>("Jungle/gorilla_audience");
        texGorillaTaunt = Resources.Load<Texture2D>("Jungle/gorilla_foreground_taunt");
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

    private Transform skyQuad;
    private Material skyMaterial;

    private void BuildSplashSky()
    {
        Texture2D texture = Resources.Load<Texture2D>("Jungle/SplashModels/SplashSky");
        if (texture == null) return;
        var background = GameObject.CreatePrimitive(PrimitiveType.Quad);
        background.name = "Cielo tropical 2D";
        background.transform.SetParent(cam.transform, false);
        Destroy(background.GetComponent<Collider>());
        var renderer = background.GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        skyMaterial = new Material(Shader.Find("Unlit/Texture")) { mainTexture = texture };
        renderer.sharedMaterial = skyMaterial;
        skyQuad = background.transform;
        FitSplashSky();
    }

    private void FitSplashSky()
    {
        if (skyQuad == null) return;
        float distance = cam.farClipPlane * .9f;
        float height = 2f * distance * Mathf.Tan(cam.fieldOfView * .5f * Mathf.Deg2Rad);
        skyQuad.localPosition = Vector3.forward * distance;
        skyQuad.localScale = new Vector3(height * cam.aspect * 1.01f, height * 1.01f, 1f);

        Texture texture = skyMaterial.mainTexture;
        float imageAspect = (float)texture.width / texture.height;
        var crop = new Vector2(Mathf.Min(1f, cam.aspect / imageAspect), Mathf.Min(1f, imageAspect / cam.aspect));
        skyMaterial.mainTextureScale = crop;
        skyMaterial.mainTextureOffset = (Vector2.one - crop) * .5f;
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
        BuildSplashSky();

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
            if (mat != null)
            {
                JungleStageBuilder.ApplySharedMaterial(renderers, mat);
                if (mat.shader.name == "Jungle/Splash Palm Crown")
                {
                    var properties = new MaterialPropertyBlock();
                    properties.SetVector("_PalmBounds", new Vector4(pos.x, pos.y, pos.z, height));
                    foreach (Renderer renderer in renderers) renderer.SetPropertyBlock(properties);
                }
            }
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
        GameObject palmPrefab = Resources.Load<GameObject>(palmRoot + "Meshy_AI_Cartoon_Coconut_Palm_20261026_texture");
        Texture2D palmAlbedo = Resources.Load<Texture2D>(palmRoot + "Palm_Albedo");
        Texture2D palmNormal = Resources.Load<Texture2D>(palmRoot + "Meshy_AI_Cartoon_Coconut_Palm_20261026_texture_normal");
        Material palmMat = MakeMaterialWithTexture(palmAlbedo, Color.white, "Palmera Tropical - Splash", 0.18f, palmNormal, 0.85f);
        Shader crownWind = Resources.Load<Shader>("Jungle/SplashModels/SplashPalmCrown");
        if (crownWind != null) palmMat.shader = crownWind;

        const string bushRoot = "Jungle/NewModels/Bush/Meshy_AI_Emerald_Canopy_20261026_texture_fbx/";
        GameObject bushPrefab = Resources.Load<GameObject>(bushRoot + "Meshy_AI_Emerald_Canopy_20261026_texture");
        Texture2D bushAlbedo = Resources.Load<Texture2D>(bushRoot + "Bush_Albedo");
        Texture2D bushNormal = Resources.Load<Texture2D>(bushRoot + "Meshy_AI_Emerald_Canopy_20261026_texture_normal");
        Material bushMat = MakeMaterialWithTexture(bushAlbedo, Color.white, "Arbusto Selva - Splash", 0.22f, bushNormal, 0.75f);

        const string coconutRoot = "Jungle/NewModels/Coconut/Meshy_AI_Coconut_Bowling_Ball_20261026_texture_fbx/";
        GameObject coconutPrefab = Resources.Load<GameObject>(coconutRoot + "Meshy_AI_Coconut_Bowling_Ball_20261026_texture");
        Texture2D coconutAlbedo = Resources.Load<Texture2D>(coconutRoot + "Coconut_Albedo");
        Texture2D coconutNormal = Resources.Load<Texture2D>(coconutRoot + "Meshy_AI_Coconut_Bowling_Ball_20261026_texture_normal");
        Material coconutMat = MakeMaterialWithTexture(coconutAlbedo, Color.white, "Coco Suelo - Splash", 0.12f, coconutNormal, 0.8f);

        GameObject trunkPrefab = Resources.Load<GameObject>("LogBalance/Models/trunk") ?? Resources.Load<GameObject>("LogBalance/Prefabs/trunk");
        Material trunkMat = Resources.Load<Material>("LogBalance/Materials/trunk_Color_C14") ?? Resources.Load<Material>("LogBalance/Materials/trunk_Color_C09");



        Texture2D texGround = Resources.Load<Texture2D>("Jungle/bowling_jungle_ground") ?? CreateProceduralSandTexture();
        Material sandMat = MakeMaterialWithTexture(texGround, new Color(0.86f, 0.74f, 0.52f), "Arena Dorada - Splash", 0.05f);

        Shader groundShader = Resources.Load<Shader>("Jungle/SplashModels/SplashGround");
        if (groundShader != null)
        {
            sandMat.shader = groundShader;
            sandMat.mainTexture = texGround;
            sandMat.color = Color.white;
        }

        Texture2D texBamboo = Resources.Load<Texture2D>("Jungle/SurfaceTextures/BowlingLaneBamboo");
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


        // 3. Palmeras auténticas del proyecto con composición cinematográfica (Verticales y erguidas)
        // Primer plano: Palmeras de encuadre (Framing)
        PlaceGroundedProp(palmPrefab, root, palmMat, "Palmera_Framing_Izq", new Vector3(-6.2f, 0f, 2.5f), 7.6f, yaw: 35f, swayAngle: 0f);
        PlaceGroundedProp(palmPrefab, root, palmMat, "Palmera_Framing_Der", new Vector3(6.2f, 0f, 2.8f), 7.8f, yaw: -30f, swayAngle: 0f);

        // Arboleda izquierda (Midground & Dunes)
        PlaceGroundedProp(palmPrefab, root, palmMat, "Palmera_Izq_1", new Vector3(-8.2f, 0.05f, 6.5f), 7.0f, yaw: 18f, swayAngle: 0f);
        PlaceGroundedProp(palmPrefab, root, palmMat, "Palmera_Izq_2", new Vector3(-10.4f, 0.20f, 9.5f), 6.0f, yaw: 45f, swayAngle: 0f);
        PlaceGroundedProp(palmPrefab, root, palmMat, "Palmera_Izq_3", new Vector3(-7.6f, 0.10f, 13.8f), 7.4f, yaw: -15f, swayAngle: 0f);
        PlaceGroundedProp(palmPrefab, root, palmMat, "Palmera_Izq_4", new Vector3(-10.8f, 0.30f, 18.2f), 6.5f, yaw: 32f, swayAngle: 0f);
        PlaceGroundedProp(palmPrefab, root, palmMat, "Palmera_Izq_5", new Vector3(-8.6f, 0.15f, 23.5f), 7.5f, yaw: -8f, swayAngle: 0f);

        // Arboleda derecha (Midground & Dunes)
        PlaceGroundedProp(palmPrefab, root, palmMat, "Palmera_Der_1", new Vector3(8.2f, 0.05f, 7.0f), 7.1f, yaw: -18f, swayAngle: 0f);
        PlaceGroundedProp(palmPrefab, root, palmMat, "Palmera_Der_2", new Vector3(10.5f, 0.20f, 10.2f), 6.2f, yaw: -38f, swayAngle: 0f);
        PlaceGroundedProp(palmPrefab, root, palmMat, "Palmera_Der_3", new Vector3(7.8f, 0.10f, 14.8f), 7.3f, yaw: 16f, swayAngle: 0f);
        PlaceGroundedProp(palmPrefab, root, palmMat, "Palmera_Der_4", new Vector3(10.8f, 0.30f, 19.5f), 6.4f, yaw: -28f, swayAngle: 0f);
        PlaceGroundedProp(palmPrefab, root, palmMat, "Palmera_Der_5", new Vector3(8.5f, 0.15f, 24.2f), 7.4f, yaw: 10f, swayAngle: 0f);

        // Palmeras de fondo (Horizonte de selva)
        PlaceGroundedProp(palmPrefab, root, palmMat, "Palmera_Fondo_1", new Vector3(-12.0f, 0.3f, 28.0f), 8.2f, yaw: 20f, swayAngle: 0f);
        PlaceGroundedProp(palmPrefab, root, palmMat, "Palmera_Fondo_2", new Vector3(-5.5f, 0.2f, 31.0f), 7.0f, yaw: -12f, swayAngle: 0f);
        PlaceGroundedProp(palmPrefab, root, palmMat, "Palmera_Fondo_3", new Vector3(5.5f, 0.2f, 31.5f), 7.2f, yaw: 14f, swayAngle: 0f);
        PlaceGroundedProp(palmPrefab, root, palmMat, "Palmera_Fondo_4", new Vector3(12.0f, 0.3f, 28.5f), 8.0f, yaw: -22f, swayAngle: 0f);

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
        AddFoliageLayers(root, bushPrefab, bushMat);
        BuildMountainBackdrop(root);
        DressSplash(root);
    }

    private static void AddFoliageLayers(Transform root, GameObject bushPrefab, Material bushMaterial)
    {
        var foliage = new GameObject("Sotobosque lateral").transform;
        foliage.SetParent(root, false);

        // Frame the clearing with low vegetation and keep the path and signs open.
        for (int side = -1; side <= 1; side += 2)
        {
            PlaceGroundedProp(bushPrefab, foliage, bushMaterial, "Borde bajo del sendero",
                new Vector3(side * 3.8f, 0f, 2.4f), .68f, side * 37f, swayAngle: 0f);
            PlaceGroundedProp(bushPrefab, foliage, bushMaterial, "Follaje de primer plano",
                new Vector3(side * 5.5f, 0f, 3.3f), 1.4f, -side * 52f, swayAngle: 0f);
            PlaceGroundedProp(bushPrefab, foliage, bushMaterial, "Vegetación detrás de los letreros",
                new Vector3(side * 4.5f, 0f, 11.5f), 2.05f, side * 95f, swayAngle: 0f);
            PlaceGroundedProp(bushPrefab, foliage, bushMaterial, "Sotobosque lateral",
                new Vector3(side * 6.9f, .1f, 15.9f), 2.6f, side * 158f, swayAngle: 0f);
        }

    }

    private static void BuildMountainBackdrop(Transform root)
    {
        const string asset = "Jungle/SplashModels/montana";
        GameObject mountain = Resources.Load<GameObject>(asset);
        if (mountain == null)
        {
            Debug.LogWarning("Falta el modelo de montaña del Splash.");
            return;
        }

        Material rock = LoadSplashMaterial("montana");
        var backdrop = new GameObject("Montañas del Splash").transform;
        backdrop.SetParent(root, false);
        var placements = new (float X, float Z, float Height, float Yaw)[]
        {
            (-31f, 19f, 19f, 27f),
            (-24f, 31f, 24f, -42f),
            (-13f, 44f, 22f, 68f),
            (  0f, 50f, 17f, 12f),
            ( 15f, 43f, 25f, -23f),
            ( 25f, 30f, 21f, 72f),
            ( 32f, 19f, 19f, -55f)
        };
        foreach (var placement in placements)
        {
            PlaceGroundedProp(mountain, backdrop, rock, "Montaña",
                new Vector3(placement.X, -.25f, placement.Z), placement.Height,
                placement.Yaw, swayAngle: 0f);
        }
    }

    private static void CreateDune(Transform parent, Material mat, string name, Vector3 pos, Vector3 scale)
    {
        var dune = JungleStageBuilder.CreatePrimitive(PrimitiveType.Sphere, name, pos, scale,
            new Color(0.86f, 0.74f, 0.52f), parent, smoothness: 0.05f);
        dune.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private static Material LoadSplashMaterial(string asset)
    {
        Texture2D texture = Resources.Load<Texture2D>("Jungle/SplashModels/" + asset);
        return MakeMaterialWithTexture(texture, Color.white, "Splash / " + asset, .18f);
    }

    private static GameObject PlaceSplashProp(Transform parent, string asset, string name, Vector3 position, float height, float yaw)
    {
        GameObject prefab = Resources.Load<GameObject>("Jungle/SplashModels/" + asset);
        if (prefab == null)
        {
            Debug.LogWarning("Falta un modelo del Splash: " + asset);
            return null;
        }

        return PlaceGroundedProp(prefab, parent, LoadSplashMaterial(asset), name, position, height, yaw, swayAngle: 0f);
    }

    private static void DressSplash(Transform root)
    {
        var props = new GameObject("Animales y accesorios del Splash").transform;
        props.SetParent(root, false);
        PlaceSplashProp(props, "sombrilla", "Sombrilla de playa", new Vector3(-6.1f, 0f, 8.2f), 3.4f, 16f);
        GameObject crate = PlaceSplashProp(props, "cajaconcocos", "Caja de cocos", new Vector3(3.85f, 0f, 4.35f), 2.18f, -21f);
        PlaceSplashProp(props, "antorcha", "Antorcha junto a Historia", new Vector3(-2.7f, 0f, 7.1f), 2.25f, 18f);
        PlaceSplashProp(props, "antorcha", "Antorcha junto a Juego libre", new Vector3(2.8f, 0f, 7.6f), 2.25f, -18f);
        PlaceSplashProp(props, "arbusto3", "Flores junto al camino", new Vector3(-3.45f, 0f, 3.8f), .94f, 198f);
        PlaceSplashProp(props, "arbusto3", "Flores junto a la sombrilla", new Vector3(-5.1f, 0f, 8.4f), 1.2f, 157f);
        PlaceSplashProp(props, "arbusto3", "Flores junto a la caja", new Vector3(4.6f, 0f, 4.7f), 1.18f, 190f);
        PlaceSplashProp(props, "tucan", "Tucán sobre el tronco", new Vector3(-4.0f, 1.18f, 8.2f), 0.72f, 118f);
        PlaceParrotOnCrate(props, crate);
        PlaceWalkingAnimal(props, "monochiquitocapuchino", "Capuchino", new Vector3(4.8f, 0f, 5.7f), new Vector3(-4.65f, 0f, 5.7f), 1.27f, 1f, facingOffset: -25f);
        PlaceWalkingAnimal(props, "mapache", "Mapache", new Vector3(-4.9f, 0f, 8.65f), new Vector3(5.05f, 0f, 8.65f), 1.04f, 4.4f);
    }

    private static void PlaceParrotOnCrate(Transform parent, GameObject crate)
    {
        if (crate == null) return;
        Renderer[] renderers = crate.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds bounds = JungleStageBuilder.CombinedBounds(renderers);
        Vector3 perch = new Vector3(bounds.center.x, bounds.max.y, Mathf.Lerp(bounds.min.z, bounds.max.z, .30f));
        Ray probe = new Ray(perch + Vector3.up, Vector3.down);
        float highestContact = float.NegativeInfinity;
        foreach (MeshFilter filter in crate.GetComponentsInChildren<MeshFilter>())
        {
            if (filter.sharedMesh == null) continue;
            var surface = filter.gameObject.AddComponent<MeshCollider>();
            surface.sharedMesh = filter.sharedMesh;
            Physics.SyncTransforms();
            if (surface.Raycast(probe, out RaycastHit hit, bounds.size.y + 1f))
                highestContact = Mathf.Max(highestContact, hit.point.y);
            surface.enabled = false;
            Destroy(surface);
        }
        if (!float.IsNegativeInfinity(highestContact)) perch.y = highestContact;

        // The bottom of the imported mesh is the tail, slightly below the feet.
        perch.y -= .11f;
        PlaceSplashProp(parent, "parrot", "Guacamaya sobre la caja", perch, .66f, -115f);
    }

    private static void PlaceWalkingAnimal(Transform parent, string asset, string label, Vector3 start, Vector3 end, float height, float delay, float facingOffset = 0f)
    {
        string path = "Jungle/SplashModels/" + asset;
        var route = new GameObject(label + " / recorrido");
        route.transform.SetParent(parent, false);
        route.transform.position = start;
        var model = PlaceGroundedProp(Resources.Load<GameObject>(path), route.transform,
            LoadSplashMaterial(asset), label, start, height, facingOffset, swayAngle: 0f);
        if (model == null) return;

        AnimationClip clip = System.Array.Find(Resources.LoadAll<AnimationClip>(path),
            candidate => candidate.length > 0f && !candidate.name.StartsWith("__preview__"));
        if (clip == null)
        {
            Debug.LogWarning("Falta la animación de caminar: " + label);
            return;
        }
        const string shrubAsset = "Jungle/SplashModels/arbusto3";
        GameObject shrub = Resources.Load<GameObject>(shrubAsset);
        Material shrubMaterial = LoadSplashMaterial("arbusto3");
        Vector3 coverOffset = Vector3.back * .65f;
        GameObject entrance = PlaceGroundedProp(shrub, parent, shrubMaterial, "Arbusto de salida / " + label,
            start + coverOffset, height + .4f, 215f, swayAngle: 0f);
        GameObject exit = PlaceGroundedProp(shrub, parent, shrubMaterial, "Arbusto de llegada / " + label,
            end + coverOffset, height + .4f, 142f, swayAngle: 0f);
        route.AddComponent<SplashAnimalWalk>().Configure(model.transform, clip, end,
            entrance != null ? entrance.transform : null, exit != null ? exit.transform : null, delay);
    }

    private void BuildGorillaCharacter()
    {
        const string guardian = "Jungle/GorillaBowling/Meshy_AI_Gorilla_Guardian_biped/Meshy_AI_Gorilla_Guardian_biped_";
        const string runPath = guardian + "Animation_Running_withSkin";
        gorillaRoot = new GameObject("Grupo de gorilas del Splash").transform;
        GameObject prefab = Resources.Load<GameObject>(runPath);
        if (prefab == null) return;

        var material = MakeMaterialWithTexture(Resources.Load<Texture2D>(guardian + "texture_0"),
            Color.white, "Gorilas del cruce", .18f, Resources.Load<Texture2D>(guardian + "texture_0_normal"));
        Texture2D dust = Resources.Load<Texture2D>("Jungle/SplashModels/SplashDustTrail");
        if (dust != null)
            dustSprite = Sprite.Create(dust, new Rect(0f, 0f, dust.width, dust.height),
                new Vector2(.10f, .13f), dust.width / 3.65f);

        var formation = new (Vector3 Offset, float Height)[]
        {
            (Vector3.zero, 2.8f),
            (new Vector3(-2.85f, 0f, 1.08f), 2.45f),
            (new Vector3(-5.55f, 0f, -.78f), 2.62f)
        };

        foreach (var slot in formation)
        {
            var root = new GameObject("Cruce lateral").transform;
            root.SetParent(gorillaRoot, false);
            root.position = RunnerStart + slot.Offset;
            GameObject model = PlaceGroundedProp(prefab, root, material,
                "Gorila corredor", root.position, slot.Height, 0f, swayAngle: 0f);
            root.rotation = Quaternion.LookRotation(Vector3.right);

            var runner = new SplashRunner
            {
                Root = root,
                Model = model.transform,
                RestPosition = model.transform.localPosition,
                RestRotation = model.transform.localRotation,
                Offset = slot.Offset
            };
            runner.Graph = CreateSplashLoop(model, runPath, out runner.Clip);
            if (dustSprite != null)
            {
                var dustObject = new GameObject("Estela de polvo");
                dustObject.transform.SetParent(gorillaRoot, false);
                runner.Dust = dustObject.AddComponent<SpriteRenderer>();
                runner.Dust.sprite = dustSprite;
                runner.Dust.flipX = true;
            }
            runners.Add(runner);
        }
    }

    private static PlayableGraph CreateSplashLoop(GameObject model, string path, out AnimationClipPlayable playable)
    {
        playable = default;
        AnimationClip clip = System.Array.Find(Resources.LoadAll<AnimationClip>(path),
            item => item.length > 0f && !item.name.StartsWith("__preview__"));
        if (clip == null)
        {
            Debug.LogWarning("Falta el clip del Splash: " + path);
            return default;
        }

        Animator animator = model.GetComponent<Animator>();
        if (animator == null) animator = model.AddComponent<Animator>();
        animator.applyRootMotion = false;
        var graph = PlayableGraph.Create(model.name);
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        playable = AnimationClipPlayable.Create(graph, clip);
        AnimationPlayableOutput.Create(graph, "Splash", animator).SetSourcePlayable(playable);
        graph.Play();
        return graph;
    }

    private static void SampleSplashLoop(PlayableGraph graph, AnimationClipPlayable playable, Transform model,
        float elapsed, Vector3 position, Quaternion rotation)
    {
        if (!graph.IsValid()) return;
        playable.SetTime(Mathf.Repeat(elapsed, playable.GetAnimationClip().length));
        graph.Evaluate(0f);
        model.SetLocalPositionAndRotation(position, rotation);
    }

    private void Build3DSigns()
    {
        signsRoot = new GameObject("Letreros 3D").transform;

        string[] titles = { "HISTORIA", "JUEGO LIBRE", "SALA Y CONEXIÓN" };
        string[] subs = { "Aventura y Escape con Kong", "Catálogo de minijuegos", "Escanear QR y Celulares" };
        string[] actions = { "STORY", "PARTY", "CONNECTION" };

        Vector3[] positions = new Vector3[]
        {
            new Vector3(-4.0f, 0f, 7.1f), // Modo Historia (Izquierda)
            new Vector3(4.0f, 0f, 7.1f),  // Modo Fiesta (Derecha)
            new Vector3(0f, 0f, 9.5f)  // Conexión QR (Centro al fondo detrás del gorila)
        };

        GameObject signPrefab = Resources.Load<GameObject>("Jungle/SplashModels/letrero");
        Material signMaterial = LoadSplashMaterial("letrero");
        for (int i = 0; i < signs.Length; i++)
        {
            var signObj = new GameObject("Letrero " + titles[i]).transform;
            signObj.SetParent(signsRoot, false);
            signObj.position = positions[i];

            var board = new GameObject("Área de selección");
            board.transform.SetParent(signObj, false);
            board.transform.localPosition = new Vector3(0f, 2.15f, 0f);
            var hitbox = board.AddComponent<BoxCollider>();
            hitbox.size = new Vector3(3.9f, 1.65f, .7f);

            Renderer signRenderer = null;
            float faceDepth = -.31f;
            float faceHeight = 2.15f;
            GameObject visual = PlaceGroundedProp(signPrefab, board.transform, signMaterial,
                "Letrero tropical de madera", positions[i], 3.45f, 0f, swayAngle: 0f);
            if (visual != null)
            {
                Renderer[] renderers = visual.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Bounds bounds = JungleStageBuilder.CombinedBounds(renderers);
                    if (bounds.size.x > 3.9f)
                    {
                        visual.transform.localScale *= 3.9f / bounds.size.x;
                        bounds = JungleStageBuilder.CombinedBounds(renderers);
                        visual.transform.position += new Vector3(positions[i].x - bounds.center.x,
                            positions[i].y - bounds.min.y, positions[i].z - bounds.center.z);
                        bounds = JungleStageBuilder.CombinedBounds(renderers);
                    }
                    faceDepth = bounds.min.z - positions[i].z - .025f;
                    signRenderer = renderers[0];
                    faceHeight = bounds.size.y * .72f;
                    visual.transform.SetParent(signObj, true);
                    board.transform.localPosition = new Vector3(0f, faceHeight, 0f);
                    visual.transform.SetParent(board.transform, true);
                }
            }
            else
            {
                var fallback = JungleStageBuilder.CreateLocalPrimitive(PrimitiveType.Cube, "Tabla de madera",
                    Vector3.zero, new Vector3(3.2f, 1.25f, .18f), new Color(.58f, .40f, .22f), board.transform);
                signRenderer = fallback.GetComponent<Renderer>();
            }

            Color woodColor = visual != null ? Color.white : new Color(.58f, .40f, .22f);

            // Texto tallado en 3D con sombra profunda (anclado a signObj para escala 1:1)
            var shadowTextGo = new GameObject("Texto3D_Sombra");
            shadowTextGo.transform.SetParent(signObj, false);
            shadowTextGo.transform.localPosition = new Vector3(0.015f, faceHeight - .02f, faceDepth + .012f);
            var tmShadow = shadowTextGo.AddComponent<TextMesh>();
            tmShadow.font = JungleUITheme.TitleFont;
            shadowTextGo.GetComponent<MeshRenderer>().sharedMaterial = tmShadow.font.material;
            tmShadow.text = titles[i];
            tmShadow.fontSize = 28;
            tmShadow.characterSize = 0.075f;
            tmShadow.anchor = TextAnchor.MiddleCenter;
            tmShadow.alignment = TextAlignment.Center;
            tmShadow.color = new Color(0.14f, 0.08f, 0.03f, 0.95f);

            var textGo = new GameObject("Texto3D");
            textGo.transform.SetParent(signObj, false);
            textGo.transform.localPosition = new Vector3(0f, faceHeight, faceDepth);
            var tm = textGo.AddComponent<TextMesh>();
            tm.font = JungleUITheme.TitleFont;
            textGo.GetComponent<MeshRenderer>().sharedMaterial = tm.font.material;
            tm.text = titles[i];
            tm.fontSize = 28;
            tm.characterSize = 0.075f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(1f, 0.94f, 0.42f);

            signs[i] = new SignData
            {
                root = signObj,
                board = board.transform,
                boardRenderer = signRenderer,
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
        FitSplashSky();
        if (startedAt < 0f)
        {
            startedAt = Time.unscaledTime;
        }
        float elapsed = Time.unscaledTime - startedAt;

        switch (currentState)
        {
            case State.StudioLogo:
                if (elapsed >= StudioLogoDuration || (elapsed >= 1.1f && AnyInputSkip()))
                {
                    if (sfxSource != null) sfxSource.Stop();
                    LoadTargetScene(SceneNavigator.Connection);
                }
                break;
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

    private static bool IsAutomated => false;

    private static bool CanSkipCinematic(float elapsed)
    {
        if (IsAutomated) return false;
        return elapsed > 0.8f && (Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1));
    }

    private void UpdateCinematic(float elapsed)
    {
        float progress = Mathf.Clamp01(elapsed / CinematicDuration);

        Vector3 lead = Vector3.Lerp(RunnerStart, RunnerEnd, progress);
        for (int i = 0; i < runners.Count; i++)
        {
            SplashRunner runner = runners[i];
            runner.Root.position = lead + runner.Offset;
            float stride = elapsed * (1.18f + i * .04f) + i * .19f;
            SampleSplashLoop(runner.Graph, runner.Clip, runner.Model, stride,
                runner.RestPosition, runner.RestRotation);

            if (runner.Dust != null)
            {
                float pulse = .5f + .5f * Mathf.Sin(stride * 13f);
                runner.Dust.transform.position = runner.Root.position + new Vector3(-.35f, .035f, .18f);
                runner.Dust.transform.localScale = new Vector3(1f + .12f * pulse, .79f + .13f * pulse, 1f);
                runner.Dust.color = new Color(1f, 1f, 1f, .59f + .17f * pulse);
            }
        }

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

        cam.fieldOfView = 58f;
        cam.transform.position = new Vector3(0f, 2.25f, -2.1f);
        cam.transform.rotation = Quaternion.Euler(5.5f, 0f, 0f);

        if (progress >= 1f || CanSkipCinematic(elapsed))
        {
            TransitionToMenu3D();
        }
    }

    private void TransitionToMenu3D()
    {
        cam.fieldOfView = 58f;
        cam.transform.position = new Vector3(0f, 2.2f, -1.65f);
        cam.transform.rotation = Quaternion.Euler(5.5f, 0f, 0f);
        foreach (SplashRunner runner in runners)
            if (runner.Graph.IsValid()) runner.Graph.Stop();
        gorillaRoot.gameObject.SetActive(false);
        if (signsRoot != null)
            signsRoot.gameObject.SetActive(true);

        // Golpe de pecho y redoble de tambores en la llegada
        if (sfxSource != null)
        {
            sfxSource.pitch = 1.0f;
            sfxSource.PlayOneShot(JungleAudioBank.LeftDrum, 0.85f);
            sfxSource.PlayOneShot(JungleAudioBank.RightDrum, 0.90f);
        }

        menuLogoStartedAt = Time.unscaledTime;
        currentState = State.Menu3D;
    }

    private void UpdateMenu3D()
    {

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
            int phoneSign = menuPointer != null && menuPointer.Connected
                ? SignUnderPointer(menuPointer.ScreenPosition) : -1;
            bool phoneConfirmed = menuPointer != null && menuPointer.TakeConfirm();
            if (phoneConfirmed && phoneSign >= 0)
            {
                TriggerSignAction(phoneSign);
                return;
            }

            // Confirmación
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) ||
                (hoveredSign >= 0 && Input.GetMouseButtonDown(0)))
            {
                TriggerSignAction((hoveredSign >= 0) ? hoveredSign : selectedSign);
                return;
            }
            if (phoneSign >= 0) hoveredSign = phoneSign;
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
        Vector3 targetCamPos = new Vector3(0f, 2.2f, -1.65f);
        float camDriftX = Mathf.Sin(Time.unscaledTime * 0.8f) * 0.018f;
        float camDriftY = Mathf.Cos(Time.unscaledTime * 1.2f) * 0.012f;
        Vector3 driftPos = targetCamPos + new Vector3(camDriftX, camDriftY, 0f);
        cam.transform.position = Vector3.Lerp(cam.transform.position, driftPos, Time.unscaledDeltaTime * 4f);
        Quaternion targetCamRot = Quaternion.Euler(5.5f, 0f, 0f);
        cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, targetCamRot, Time.unscaledDeltaTime * 4f);

        // Resaltar el letrero activo con brillo tropical
        for (int i = 0; i < signs.Length; i++)
        {
            Vector3 awayFromCamera = signs[i].root.position - cam.transform.position;
            awayFromCamera.y = 0f;
            signs[i].root.rotation = Quaternion.LookRotation(awayFromCamera);

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

    private bool TryGetSignRect(int index, out Rect rect)
    {
        Vector3 point = cam.WorldToScreenPoint(signs[index].board.position + Vector3.up * .15f);
        float width = Mathf.Clamp(Screen.width * .19f, 190f, 390f);
        rect = new Rect(point.x - width * .5f, Screen.height - point.y - 42f, width, 116f);
        return point.z > 0f;
    }

    private int SignUnderPointer(Vector2 position)
    {
        for (int i = 0; i < signs.Length; i++)
            if (TryGetSignRect(i, out Rect rect) && rect.Contains(position)) return i;
        return -1;
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
                currentState = State.ComicStory;
                break;
            case "PARTY":
                LoadTargetScene(SceneNavigator.MainMenu);
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
            LoadTargetScene(SceneNavigator.MainMenu);
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
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || session?.TakeRemoteAccept() == true)
        {
            if (sfxSource != null) sfxSource.PlayOneShot(JungleAudioBank.Pitch, 0.45f);
            AdvanceComic();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || session?.TakeRemoteBack() == true)
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
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || session?.TakeRemoteAccept() == true)
        {
            AdvancePrep();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || session?.TakeRemoteBack() == true)
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
        JungleStory.Begin();
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
    private void OnGUI()
    {
        if (currentState == State.StudioLogo)
        {
            DrawStudioLogo();
            return;
        }
        if (cam == null) return;
        JungleUITheme.Ensure();

        int sw = Screen.width;
        int sh = Screen.height;

        var titleStyle = new GUIStyle(GUI.skin.label)
        {
            font = JungleUITheme.TitleFont,
            fontSize = Mathf.Clamp(sh / 15, 32, 54),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.88f, 0.20f) }
        };

        var subtitleStyle = new GUIStyle(GUI.skin.label)
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
            float pillW = Mathf.Min(sw * 0.82f, 620f);
            float pillH = 42f;
            Rect pillRect = new Rect((sw - pillW) * 0.5f, sh * 0.88f, pillW, pillH);

            JungleUITheme.DrawSubPanel(pillRect);
            var tipBright = new GUIStyle(tipStyle) { normal = { textColor = new Color(1f, 0.95f, 0.82f) } };
            JungleUITheme.DrawShadowedLabel(new Rect(pillRect.x, pillRect.y + 4f, pillRect.width, 32f), "Preparando la isla sagrada... [Espacio o Clic para omitir]", tipBright, 1.5f);
        }
        else if (currentState == State.Menu3D)
        {
            DrawHangingLogo(sw, sh);

            string netStatus = (services != null && session.CameraConnected)
                ? "CÁMARA Y QR LISTOS"
                : ((services != null && services.IsRunning) ? "PREPARANDO CONEXIÓN" : "SERVICIOS EN SEGUNDO PLANO");
            Rect statusRect = new Rect(sw - 246f, 16f, 230f, 32f);
            JungleUITheme.DrawSubPanel(statusRect);
            var statusStyle = new GUIStyle(tipStyle) { fontSize = 12, normal = { textColor = new Color(0.4f, 1f, 0.5f) } };
            GUI.Label(statusRect, "* " + netStatus, statusStyle);

            for (int i = 0; i < signs.Length; i++)
            {
                if (signs[i].board == null) continue;
                Vector3 sp = cam.WorldToScreenPoint(signs[i].board.position + Vector3.up * 0.15f);
                if (sp.z < 0f) continue;

                sp.y = sh - sp.y;
                bool isFocused = (i == ((hoveredSign >= 0) ? hoveredSign : selectedSign));

                float subW = Mathf.Clamp(sw * .19f, 190f, 390f);
                float subH = 32f;
                Rect subRect = new Rect(sp.x - subW * 0.5f, sp.y + 42f, subW, subH);

                if (isFocused)
                    JungleUITheme.DrawBadge(subRect, signs[i].subtitle, highlighted: true);
                else
                    JungleUITheme.DrawPlankLabel(subRect, signs[i].subtitle);

                if (TryGetSignRect(i, out Rect clickRect) && GUI.Button(clickRect, "", GUIStyle.none))
                {
                    TriggerSignAction(i);
                }
            }

            float navPillW = Mathf.Min(sw * 0.75f, 540f);
            float navPillH = 38f;
            Rect navPillRect = new Rect((sw - navPillW) * 0.5f, sh * 0.915f, navPillW, navPillH);
            JungleUITheme.DrawSubPanel(navPillRect);
            var navTipStyle = new GUIStyle(tipStyle) { fontSize = 15, normal = { textColor = new Color(1f, 0.95f, 0.82f) } };
            JungleUITheme.DrawShadowedLabel(new Rect(navPillRect.x, navPillRect.y + 4f, navPillW, 28f), menuPointer != null && menuPointer.Connected ? "Apunta con el celular y toca ACEPTAR" : "Usa las flechas o haz clic en un letrero", navTipStyle, 1.5f);
            JungleUITheme.DrawLeaves();
            if (menuPointer != null && menuPointer.Connected)
                JungleUITheme.DrawGameHandSquare(menuPointer.Position, JungleUITheme.Gold);
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
        Color prev = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.65f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
        GUI.color = prev;

        float cardW = Mathf.Min(sw * 0.80f, 760f);
        float cardH = Mathf.Min(sh * 0.70f, 480f);
        Rect cardRect = new Rect((sw - cardW) * 0.5f, (sh - cardH) * 0.5f, cardW, cardH);

        JungleUITheme.DrawPanel(cardRect, selected: true);

        var partyTitleStyle = new GUIStyle(titleStyle) { fontSize = 32 };
        JungleUITheme.DrawShadowedLabel(new Rect(cardRect.x, cardRect.y + 25f, cardW, 45f), "JUEGO LIBRE", partyTitleStyle, 2.5f);

        var bodyStyle = new GUIStyle(subtitleStyle)
        {
            fontSize = 19,
            wordWrap = true,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.98f, 0.90f) }
        };
        string partyDesc = "Elige un minijuego después de conectar el celular o la cámara.";
        JungleUITheme.DrawSubPanel(new Rect(cardRect.x + 36f, cardRect.y + 84f, cardW - 72f, cardH * 0.50f));
        GUI.Label(new Rect(cardRect.x + 48f, cardRect.y + 92f, cardW - 96f, cardH * 0.46f), partyDesc, bodyStyle);

        float btnW = 210f;
        float btnH = 48f;
        float btnY = cardRect.y + cardH - 68f;

        if (GUI.Button(new Rect(cardRect.x + 46f, btnY, btnW, btnH), "< Volver (ESC)", JungleUITheme.Button))
        {
            JungleAudioBank.PlayUiClick();
            currentState = State.Menu3D;
        }

        if (GUI.Button(new Rect(cardRect.x + cardW - btnW - 46f, btnY, btnW, btnH), "Crear Sala QR >", JungleUITheme.ButtonSelected))
        {
            JungleAudioBank.PlayUiClick();
            LoadTargetScene(SceneNavigator.MainMenu);
        }
    }

    private void DrawComicModal(int sw, int sh, GUIStyle titleStyle, GUIStyle subtitleStyle, GUIStyle tipStyle)
    {
        Color prev = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.70f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
        GUI.color = prev;

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
        GUI.Label(new Rect(cardRect.x, cardRect.y + 20f, cardW, 25f), p.tag, tagStyle);

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
            GUI.DrawTexture(imgRect, panelTex, ScaleMode.ScaleToFit);
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
        GUI.Label(new Rect(textX, cardRect.y + 105f, textW, cardH * 0.48f), p.narrative, narrativeStyle);

        var contextStyle = new GUIStyle(tipStyle)
        {
            normal = { textColor = new Color(1f, 0.90f, 0.65f) },
            alignment = TextAnchor.MiddleCenter
        };
        GUI.Label(new Rect(cardRect.x + 20f, cardRect.y + cardH - 88f, cardW - 40f, 26f), p.tip, contextStyle);

        float btnW = 175f;
        float btnH = 44f;
        float btnY = cardRect.y + cardH - 56f;

        if (comicPanelIndex > 0)
        {
            if (GUI.Button(new Rect(cardRect.x + 40f, btnY, btnW, btnH), "< Anterior", JungleUITheme.Button))
            {
                JungleAudioBank.PlayUiClick();
                comicPanelIndex--;
            }
        }

        string nextLabel = (comicPanelIndex >= ComicPanels.Length - 1) ? "¡Prepararse! >" : "Siguiente >";
        if (GUI.Button(new Rect(cardRect.x + cardW - btnW - 40f, btnY, btnW, btnH), nextLabel, JungleUITheme.ButtonSelected))
        {
            JungleAudioBank.PlayUiClick();
            AdvanceComic();
        }

        if (GUI.Button(new Rect(cardRect.x + (cardW - 150f) * 0.5f, btnY, 150f, btnH), "Omitir (ESC)", JungleUITheme.Button))
        {
            JungleAudioBank.PlayUiClick();
            SkipComicToPrep();
        }
    }

    private void DrawPrepModal(int sw, int sh, GUIStyle titleStyle, GUIStyle subtitleStyle)
    {
        Color prev = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.70f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
        GUI.color = prev;

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
        GUI.Label(new Rect(cardRect.x, cardRect.y + 70f, cardW, 26f), "Paso " + (prepStepIndex + 1) + " de " + PrepSteps.Length, stepHeaderStyle);

        var bodyStyle = new GUIStyle(subtitleStyle)
        {
            fontSize = 20,
            wordWrap = true,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.98f, 0.92f) }
        };
        JungleUITheme.DrawSubPanel(new Rect(cardRect.x + 42f, cardRect.y + 112f, cardW - 84f, cardH * 0.50f));
        GUI.Label(new Rect(cardRect.x + 54f, cardRect.y + 120f, cardW - 108f, cardH * 0.46f), PrepSteps[prepStepIndex], bodyStyle);

        float btnW = 175f;
        float btnH = 44f;
        float btnY = cardRect.y + cardH - 58f;

        if (prepStepIndex > 0)
        {
            if (GUI.Button(new Rect(cardRect.x + 40f, btnY, btnW, btnH), "< Anterior", JungleUITheme.Button))
            {
                JungleAudioBank.PlayUiClick();
                prepStepIndex--;
            }
        }

        string actionText = (prepStepIndex >= PrepSteps.Length - 1) ? "¡A Jugar! >" : "Siguiente >";
        if (GUI.Button(new Rect(cardRect.x + cardW - btnW - 40f, btnY, btnW, btnH), actionText, JungleUITheme.ButtonSelected))
        {
            JungleAudioBank.PlayUiClick();
            AdvancePrep();
        }

        if (GUI.Button(new Rect(cardRect.x + (cardW - 150f) * 0.5f, btnY, 150f, btnH), "Omitir (ESC)", JungleUITheme.Button))
        {
            JungleAudioBank.PlayUiClick();
            LaunchStoryDiscipline();
        }
    }

    // -------------------------------------------------------------
    // AUTOMACIÓN DE CAPTURA VISUAL PARA LABORATORIO QA (10 EVIDENCIAS)
    // -------------------------------------------------------------
}
