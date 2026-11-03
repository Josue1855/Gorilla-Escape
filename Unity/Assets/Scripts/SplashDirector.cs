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
public sealed class SplashDirector : MonoBehaviour
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
    private ProceduralGorilla gorilla;
    private Transform gorillaRoot;
    private Transform signsRoot;
    private readonly SignData[] signs = new SignData[3];
    private int selectedSign = 0;
    private int hoveredSign = -1;

    private int comicPanelIndex = 0;
    private int prepStepIndex = 0;

    private PortableServiceHost services;
    private MotionSession session;
    private bool bootstrapped = false;

    private const float CinematicDuration = 5.0f;
    private const float GorillaStartZ = 24f;
    private const float GorillaEndZ = 6.2f;

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
            narrative = "Kong golpea su pecho y proclama el reto: «Si demuestras tu fuerza y agilidad humana en nuestras 4 pruebas sagradas, mis gorilas construirán una balsa de bambú para tu regreso a casa».",
            tip = "¡El náufrago acepta el reto de las Gorillolimpiadas!"
        },
        new ComicPanel
        {
            tag = "VIÑETA 5 DE 5",
            title = "LAS 4 PRUEBAS MVP",
            narrative = "1. Gorilla Smash: Fuerza con el mazo de la selva.\n2. Lanzamiento de Coco: Puntería y distancia máxima.\n3. Coconut Bowling: Tiro rodado contra pines tropicales.\n4. Jungle Slice: Reflejos rápidos cortando cocos al vuelo.",
            tip = "¡Supera el torneo para ganar tu boleto de vuelta a casa!"
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
        startedAt = Time.unscaledTime;
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
        mainLight.intensity = 1.45f;
        mainLight.color = new Color(1f, 0.96f, 0.88f);
        mainLight.transform.rotation = Quaternion.Euler(42f, -28f, 0f);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientLight = new Color(0.70f, 0.75f, 0.82f);
        RenderSettings.ambientSkyColor = new Color(0.65f, 0.82f, 0.98f);
        RenderSettings.ambientEquatorColor = new Color(0.58f, 0.65f, 0.65f);
        RenderSettings.ambientGroundColor = new Color(0.48f, 0.42f, 0.34f);
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
        cam.backgroundColor = new Color(0.42f, 0.75f, 0.95f, 1f);
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 500f;
        cam.fieldOfView = 58f;
        // Vista general hacia +Z
        cam.transform.position = new Vector3(0f, 2.0f, -1.0f);
        cam.transform.rotation = Quaternion.Euler(6.5f, 0f, 0f);
    }

    private void BuildTropicalEnvironment()
    {
        var root = new GameObject("Escenario Tropical 3D").transform;

        // Arena dorada
        JungleStageBuilder.CreatePrimitive(PrimitiveType.Cube, "Arena", new Vector3(0f, -0.2f, 22f),
            new Vector3(65f, 0.4f, 75f), new Color(0.88f, 0.78f, 0.58f), root);

        // Sendero central
        JungleStageBuilder.CreatePrimitive(PrimitiveType.Cube, "Sendero", new Vector3(0f, -0.15f, 22f),
            new Vector3(5.5f, 0.1f, 70f), new Color(0.74f, 0.63f, 0.43f), root);

        // Océano al horizonte (+Z)
        JungleStageBuilder.CreatePrimitive(PrimitiveType.Cube, "Oceano", new Vector3(0f, -0.3f, 55f),
            new Vector3(95f, 0.3f, 50f), new Color(0.16f, 0.52f, 0.76f), root);

        // Sol dorado
        JungleStageBuilder.CreatePrimitive(PrimitiveType.Sphere, "Sol Dorado", new Vector3(10f, 20f, 58f),
            Vector3.one * 5.5f, new Color(1f, 0.94f, 0.60f), root);

        // Palmeras a los lados (separadas a +/- 7.5m para despejar la vista del sendero)
        for (int i = 0; i < 10; i++)
        {
            float z = 4f + i * 5.5f;
            float side = (i % 2 == 0) ? -7.8f : 7.8f;
            BuildPalmTree(root, new Vector3(side + Random.Range(-0.5f, 0.5f), 0f, z));
        }

        // Rocas de playa
        for (int i = 0; i < 8; i++)
        {
            float rx = (i % 2 == 0 ? -1f : 1f) * Random.Range(4.5f, 15f);
            float rz = Random.Range(3f, 35f);
            JungleStageBuilder.CreatePrimitive(PrimitiveType.Sphere, "Roca " + i,
                new Vector3(rx, -0.05f, rz),
                new Vector3(Random.Range(0.8f, 1.8f), Random.Range(0.5f, 1.0f), Random.Range(0.8f, 1.8f)),
                new Color(0.48f, 0.44f, 0.40f), root);
        }
    }

    private static void BuildPalmTree(Transform parent, Vector3 pos)
    {
        var palm = new GameObject("Palmera").transform;
        palm.SetParent(parent, false);
        palm.position = pos;

        var trunk = JungleStageBuilder.CreateLocalPrimitive(PrimitiveType.Cylinder, "Tronco",
            new Vector3(0f, 2.5f, 0f), new Vector3(0.42f, 2.5f, 0.42f), new Color(0.52f, 0.36f, 0.20f), palm).transform;
        trunk.localRotation = Quaternion.Euler(Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f));

        var crown = new GameObject("Corona").transform;
        crown.SetParent(palm, false);
        crown.localPosition = new Vector3(0f, 4.9f, 0f);

        for (int f = 0; f < 8; f++)
        {
            float a = f * Mathf.PI * 2f / 8f;
            var frond = JungleStageBuilder.CreateLocalPrimitive(PrimitiveType.Cube, "Hoja",
                new Vector3(Mathf.Cos(a) * 1.6f, 0f, Mathf.Sin(a) * 1.6f),
                new Vector3(0.38f, 0.12f, 2.6f), new Color(0.18f, 0.60f, 0.26f), crown).transform;
            frond.localRotation = Quaternion.Euler(22f, -a * Mathf.Rad2Deg, 0f);
        }
    }

    private void BuildGorillaCharacter()
    {
        gorillaRoot = new GameObject("Gorila Runner").transform;
        gorillaRoot.position = new Vector3(0f, 0f, GorillaStartZ);
        gorilla = ProceduralGorilla.Build(gorillaRoot, 2.3f);
        gorillaRoot.rotation = Quaternion.LookRotation(Vector3.back); // Hacia la cámara
    }

    private void Build3DSigns()
    {
        signsRoot = new GameObject("Letreros 3D").transform;

        string[] titles = { "MODO HISTORIA", "MODO FIESTA", "SALA Y CONEXIÓN" };
        string[] subs = { "Aventura con Kong (4 Pruebas)", "Torneo 1-4 Jugadores", "Escanear QR y Celulares" };
        string[] actions = { "STORY", "PARTY", "CONNECTION" };
        
        // Disposición abierta: izquierda, derecha, y centro ligeramente al fondo
        Vector3[] positions = new Vector3[]
        {
            new Vector3(-3.3f, 0f, 5.8f), // Modo Historia (Izquierda)
            new Vector3(3.3f, 0f, 5.8f),  // Modo Fiesta (Derecha)
            new Vector3(0f, 0.6f, 8.8f)   // Conexión QR (Centro al fondo detrás del gorila)
        };

        for (int i = 0; i < 3; i++)
        {
            var signObj = new GameObject("Sign_" + actions[i]).transform;
            signObj.SetParent(signsRoot, false);
            signObj.position = positions[i];

            // Orientar levemente hacia el centro de la cámara
            float angleY = (i == 0) ? 18f : (i == 1) ? -18f : 0f;
            signObj.rotation = Quaternion.Euler(0f, angleY, 0f);

            // Postes de bambú gemelos
            JungleStageBuilder.CreateLocalPrimitive(PrimitiveType.Cylinder, "Bambu_Izq",
                new Vector3(-1.45f, 1.4f, 0f), new Vector3(0.13f, 1.4f, 0.13f), new Color(0.58f, 0.44f, 0.22f), signObj);
            JungleStageBuilder.CreateLocalPrimitive(PrimitiveType.Cylinder, "Bambu_Der",
                new Vector3(1.45f, 1.4f, 0f), new Vector3(0.13f, 1.4f, 0.13f), new Color(0.58f, 0.44f, 0.22f), signObj);

            // Cuerdas de soporte
            var ropeL = JungleStageBuilder.CreateLocalPrimitive(PrimitiveType.Cylinder, "Cuerda_Izq",
                new Vector3(-1.45f, 2.5f, 0f), new Vector3(0.04f, 0.5f, 0.04f), new Color(0.65f, 0.52f, 0.35f), signObj).transform;
            ropeL.localRotation = Quaternion.Euler(0f, 0f, 26f);
            var ropeR = JungleStageBuilder.CreateLocalPrimitive(PrimitiveType.Cylinder, "Cuerda_Der",
                new Vector3(1.45f, 2.5f, 0f), new Vector3(0.04f, 0.5f, 0.04f), new Color(0.65f, 0.52f, 0.35f), signObj).transform;
            ropeR.localRotation = Quaternion.Euler(0f, 0f, -26f);

            // Tabla de madera principal con collider
            Color woodColor = new Color(0.60f, 0.42f, 0.24f);
            var board = JungleStageBuilder.CreateLocalPrimitive(PrimitiveType.Cube, "TablaMadera",
                new Vector3(0f, 2.3f, 0f), new Vector3(3.0f, 1.20f, 0.16f), woodColor, signObj, keepCollider: true);

            // Borde superior e inferior
            JungleStageBuilder.CreateLocalPrimitive(PrimitiveType.Cube, "MarcoSup",
                new Vector3(0f, 2.92f, 0f), new Vector3(3.1f, 0.10f, 0.20f), new Color(0.46f, 0.34f, 0.16f), signObj);
            JungleStageBuilder.CreateLocalPrimitive(PrimitiveType.Cube, "MarcoInf",
                new Vector3(0f, 1.68f, 0f), new Vector3(3.1f, 0.10f, 0.20f), new Color(0.46f, 0.34f, 0.16f), signObj);

            // Follaje decorativo
            JungleStageBuilder.CreateLocalPrimitive(PrimitiveType.Sphere, "FollajeL",
                new Vector3(-1.60f, 2.8f, 0.05f), new Vector3(0.5f, 0.25f, 0.35f), new Color(0.20f, 0.58f, 0.25f), signObj);
            JungleStageBuilder.CreateLocalPrimitive(PrimitiveType.Sphere, "FollajeR",
                new Vector3(1.60f, 2.8f, 0.05f), new Vector3(0.5f, 0.25f, 0.35f), new Color(0.20f, 0.58f, 0.25f), signObj);

            // Texto tallado en 3D
            var textGo = new GameObject("Texto3D");
            textGo.transform.SetParent(board.transform, false);
            textGo.transform.localPosition = new Vector3(0f, 0.05f, -0.10f);
            textGo.transform.localRotation = Quaternion.identity;
            var tm = textGo.AddComponent<TextMesh>();
            tm.text = titles[i];
            tm.fontSize = 28;
            tm.characterSize = 0.06f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(1f, 0.92f, 0.45f);

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
                cameraTargetPos = new Vector3(positions[i].x * 0.40f, 2.0f, 1.2f)
            };
        }
    }

    private void Update()
    {
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

        // Gorila corre libremente por el sendero hacia la cámara
        float currentZ = Mathf.Lerp(GorillaStartZ, GorillaEndZ, progress);
        gorillaRoot.position = new Vector3(0f, 0f, currentZ);
        gorilla.SetRunCycle(elapsed * 9.5f, 1f);

        // Suave avance de cámara hacia el gorila
        cam.transform.position = Vector3.Lerp(new Vector3(0f, 2.3f, 0.5f), new Vector3(0f, 2.1f, 1.4f), progress);
        cam.transform.rotation = Quaternion.Euler(Mathf.Lerp(7.0f, 5.0f, progress), 0f, 0f);

        if (progress >= 1f || CanSkipCinematic(elapsed))
        {
            TransitionToMenu3D();
        }
    }

    private void TransitionToMenu3D()
    {
        gorilla.SetIdle();
        gorillaRoot.position = new Vector3(0f, 0f, GorillaEndZ);
        if (signsRoot != null)
            signsRoot.gameObject.SetActive(true);
        currentState = State.Menu3D;
    }

    private void UpdateMenu3D()
    {
        if (IsAutomated) return;

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

        int activeIndex = (hoveredSign >= 0) ? hoveredSign : selectedSign;

        // Suave movimiento de cámara hacia el letrero enfocado
        Vector3 targetCamPos = signs[activeIndex].cameraTargetPos;
        cam.transform.position = Vector3.Lerp(cam.transform.position, targetCamPos, Time.unscaledDeltaTime * 4f);
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

        // Confirmación
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) ||
            (hoveredSign >= 0 && Input.GetMouseButtonDown(0)))
        {
            TriggerSignAction(activeIndex);
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
        switch (signs[index].action)
        {
            case "STORY":
                comicPanelIndex = 0;
                currentState = State.ComicStory;
                break;
            case "PARTY":
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
            TournamentSession.StartPartyMode();
            LoadTargetScene(SceneNavigator.Connection);
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            currentState = State.Menu3D;
        }
    }

    private void UpdateComicStory()
    {
        if (IsAutomated) return;
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            AdvanceComic();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            comicPanelIndex = Mathf.Max(0, comicPanelIndex - 1);
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
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
        LoadTargetScene(SceneNavigator.GorillaSmash);
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
    // RENDERIZADO VISUAL CON JUNGLE UI THEME
    // -------------------------------------------------------------
    private void OnGUI()
    {
        if (cam == null) return;

        int sw = Screen.width;
        int sh = Screen.height;

        var titleStyle = new GUIStyle(GUI.skin.label)
        {
            font = JungleUITheme.TitleFont,
            fontSize = Mathf.Clamp(sh / 15, 32, 54),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        titleStyle.normal.textColor = new Color(1f, 0.88f, 0.20f);

        var subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            font = JungleUITheme.TextFont,
            fontSize = Mathf.Clamp(sh / 32, 16, 24),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        subtitleStyle.normal.textColor = Color.white;

        var tipStyle = new GUIStyle(subtitleStyle)
        {
            fontSize = Mathf.Clamp(sh / 38, 14, 18),
            normal = { textColor = new Color(0.9f, 0.9f, 0.8f, 0.9f) }
        };

        if (currentState == State.Cinematic)
        {
            Rect titleRect = new Rect(0, sh * 0.06f, sw, 65);
            Rect shadowRect = new Rect(titleRect.x + 2, titleRect.y + 2, titleRect.width, titleRect.height);
            var shadowStyle = new GUIStyle(titleStyle) { normal = { textColor = new Color(0f, 0f, 0f, 0.7f) } };
            GUI.Label(shadowRect, "GORILLA ESCAPE", shadowStyle);
            GUI.Label(titleRect, "GORILLA ESCAPE", titleStyle);

            GUI.Label(new Rect(0, sh * 0.88f, sw, 32), "Preparando la isla sagrada… [Espacio o Clic para omitir]", tipStyle);
        }
        else if (currentState == State.Menu3D)
        {
            Rect titleRect = new Rect(0, sh * 0.05f, sw, 55);
            GUI.Label(titleRect, "GORILLA ESCAPE", titleStyle);

            for (int i = 0; i < signs.Length; i++)
            {
                if (signs[i].board == null) continue;
                Vector3 sp = cam.WorldToScreenPoint(signs[i].board.position + Vector3.up * 0.2f);
                if (sp.z < 0f) continue;

                sp.y = sh - sp.y;

                bool isFocused = (i == ((hoveredSign >= 0) ? hoveredSign : selectedSign));

                var signHeaderStyle = new GUIStyle(titleStyle)
                {
                    fontSize = isFocused ? 26 : 22,
                    normal = { textColor = isFocused ? new Color(1f, 0.95f, 0.35f) : new Color(0.95f, 0.85f, 0.55f) }
                };

                var signSubStyle = new GUIStyle(subtitleStyle)
                {
                    fontSize = isFocused ? 15 : 13,
                    normal = { textColor = isFocused ? Color.white : new Color(0.85f, 0.85f, 0.85f) }
                };

                Rect clickRect = new Rect(sp.x - 150, sp.y - 30, 300, 75);
                if (GUI.Button(clickRect, GUIContent.none, GUIStyle.none))
                {
                    TriggerSignAction(i);
                }

                GUI.Label(new Rect(sp.x - 160, sp.y + 40, 320, 26), signs[i].subtitle, signSubStyle);
            }

            GUI.Label(new Rect(0, sh * 0.92f, sw, 28), "Usa [←/→] o haz clic en un letrero para entrar", tipStyle);
        }
        else if (currentState == State.PartyModal)
        {
            DrawPartyModal(sw, sh, titleStyle, subtitleStyle, tipStyle);
        }
        else if (currentState == State.ComicStory)
        {
            DrawComicModal(sw, sh, titleStyle, subtitleStyle, tipStyle);
        }
        else if (currentState == State.PersonalPrep)
        {
            DrawPrepModal(sw, sh, titleStyle, subtitleStyle, tipStyle);
        }
    }

    private void DrawPartyModal(int sw, int sh, GUIStyle titleStyle, GUIStyle subtitleStyle, GUIStyle tipStyle)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.65f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float cardW = Mathf.Min(sw * 0.80f, 760f);
        float cardH = Mathf.Min(sh * 0.70f, 480f);
        Rect cardRect = new Rect((sw - cardW) * 0.5f, (sh - cardH) * 0.5f, cardW, cardH);

        if (texBambooCard != null)
            GUI.DrawTexture(cardRect, texBambooCard);
        else
            GUI.Box(cardRect, GUIContent.none);

        var partyTitleStyle = new GUIStyle(titleStyle) { fontSize = 32 };
        GUI.Label(new Rect(cardRect.x, cardRect.y + 25, cardW, 45), "MODO FIESTA — GORILIMPIADAS", partyTitleStyle);

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
        GUI.Label(new Rect(cardRect.x + 40, cardRect.y + 85, cardW - 80, cardH * 0.50f), partyDesc, bodyStyle);

        float btnW = 180f;
        float btnH = 46f;
        float btnY = cardRect.y + cardH - 65f;

        if (GUI.Button(new Rect(cardRect.x + 50, btnY, btnW, btnH), "← Volver (ESC)"))
        {
            currentState = State.Menu3D;
        }

        if (GUI.Button(new Rect(cardRect.x + cardW - btnW - 50, btnY, btnW, btnH), "Crear Sala QR →"))
        {
            TournamentSession.StartPartyMode();
            LoadTargetScene(SceneNavigator.Connection);
        }
    }

    private void DrawComicModal(int sw, int sh, GUIStyle titleStyle, GUIStyle subtitleStyle, GUIStyle tipStyle)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.70f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float cardW = Mathf.Min(sw * 0.85f, 880f);
        float cardH = Mathf.Min(sh * 0.80f, 560f);
        Rect cardRect = new Rect((sw - cardW) * 0.5f, (sh - cardH) * 0.5f, cardW, cardH);

        if (texBambooCard != null)
            GUI.DrawTexture(cardRect, texBambooCard);
        else
            GUI.Box(cardRect, GUIContent.none);

        ComicPanel p = ComicPanels[comicPanelIndex];

        var tagStyle = new GUIStyle(subtitleStyle)
        {
            fontSize = 15,
            normal = { textColor = new Color(1f, 0.85f, 0.35f) }
        };
        GUI.Label(new Rect(cardRect.x, cardRect.y + 20, cardW, 25), p.tag, tagStyle);

        var panelTitleStyle = new GUIStyle(titleStyle)
        {
            fontSize = 32,
            normal = { textColor = new Color(1f, 0.92f, 0.30f) }
        };
        GUI.Label(new Rect(cardRect.x, cardRect.y + 48, cardW, 45), p.title, panelTitleStyle);

        Rect imgRect = new Rect(cardRect.x + 35, cardRect.y + 105, cardW * 0.38f, cardH * 0.50f);
        Texture2D panelTex = (comicPanelIndex >= 2) ? texGorillaAudience : texGorillaMenu;
        if (panelTex != null)
        {
            GUI.DrawTexture(imgRect, panelTex, ScaleMode.ScaleToFit);
        }

        float textX = (panelTex != null) ? (cardRect.x + cardW * 0.42f) : (cardRect.x + 40);
        float textW = (panelTex != null) ? (cardW * 0.52f) : (cardW - 80);
        var narrativeStyle = new GUIStyle(subtitleStyle)
        {
            fontSize = 19,
            wordWrap = true,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(1f, 0.98f, 0.92f) }
        };
        GUI.Label(new Rect(textX, cardRect.y + 105, textW, cardH * 0.48f), p.narrative, narrativeStyle);

        var contextStyle = new GUIStyle(tipStyle)
        {
            normal = { textColor = new Color(1f, 0.90f, 0.65f) },
            alignment = TextAnchor.MiddleCenter
        };
        GUI.Label(new Rect(cardRect.x + 20, cardRect.y + cardH - 85, cardW - 40, 26), p.tip, contextStyle);

        float btnW = 160f;
        float btnH = 44f;
        float btnY = cardRect.y + cardH - 54f;

        if (comicPanelIndex > 0)
        {
            if (GUI.Button(new Rect(cardRect.x + 40, btnY, btnW, btnH), "← Anterior"))
            {
                comicPanelIndex--;
            }
        }

        string nextLabel = (comicPanelIndex >= ComicPanels.Length - 1) ? "¡Prepararse! →" : "Siguiente →";
        if (GUI.Button(new Rect(cardRect.x + cardW - btnW - 40, btnY, btnW, btnH), nextLabel))
        {
            AdvanceComic();
        }

        if (GUI.Button(new Rect(cardRect.x + (cardW - 130) * 0.5f, btnY, 130, btnH), "Omitir (ESC)"))
        {
            SkipComicToPrep();
        }
    }

    private void DrawPrepModal(int sw, int sh, GUIStyle titleStyle, GUIStyle subtitleStyle, GUIStyle tipStyle)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.70f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float cardW = Mathf.Min(sw * 0.85f, 820f);
        float cardH = Mathf.Min(sh * 0.75f, 500f);
        Rect cardRect = new Rect((sw - cardW) * 0.5f, (sh - cardH) * 0.5f, cardW, cardH);

        if (texBambooCard != null)
            GUI.DrawTexture(cardRect, texBambooCard);
        else
            GUI.Box(cardRect, GUIContent.none);

        var prepTitleStyle = new GUIStyle(titleStyle)
        {
            fontSize = 30,
            normal = { textColor = new Color(1f, 0.90f, 0.25f) }
        };
        GUI.Label(new Rect(cardRect.x, cardRect.y + 25, cardW, 40), "PREPARACIÓN PERSONAL", prepTitleStyle);

        var stepHeaderStyle = new GUIStyle(subtitleStyle)
        {
            fontSize = 16,
            normal = { textColor = new Color(1f, 0.85f, 0.35f) }
        };
        GUI.Label(new Rect(cardRect.x, cardRect.y + 70, cardW, 26), "Paso " + (prepStepIndex + 1) + " de " + PrepSteps.Length, stepHeaderStyle);

        var bodyStyle = new GUIStyle(subtitleStyle)
        {
            fontSize = 20,
            wordWrap = true,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.98f, 0.92f) }
        };
        GUI.Label(new Rect(cardRect.x + 50, cardRect.y + 115, cardW - 100, cardH * 0.50f), PrepSteps[prepStepIndex], bodyStyle);

        float btnW = 160f;
        float btnH = 44f;
        float btnY = cardRect.y + cardH - 58f;

        if (prepStepIndex > 0)
        {
            if (GUI.Button(new Rect(cardRect.x + 40, btnY, btnW, btnH), "← Anterior"))
            {
                prepStepIndex--;
            }
        }

        string actionText = (prepStepIndex >= PrepSteps.Length - 1) ? "¡A Jugar! →" : "Siguiente →";
        if (GUI.Button(new Rect(cardRect.x + cardW - btnW - 40, btnY, btnW, btnH), actionText))
        {
            AdvancePrep();
        }

        if (GUI.Button(new Rect(cardRect.x + (cardW - 130) * 0.5f, btnY, 130, btnH), "Omitir (ESC)"))
        {
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

        // Transición suave al Menú 3D
        TransitionToMenu3D();
        selectedSign = 0;
        yield return new WaitForSeconds(0.8f);

        // 3. Evidencia 3: Menú 3D con letreros físicos integrados
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(screenshotDir, "evidence_03_menu_3d_signs.png"));
        yield return null;
        yield return null;

        // 4. Evidencia 4: Enfoque / selección de letrero
        selectedSign = 1; // Modo Fiesta enfocado
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

        // 7. Evidencia 7: Historieta Pantalla Final (Las 4 Pruebas MVP)
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
        SceneNavigator.Load(SceneNavigator.GorillaSmash);
    }
}
