using System.Collections;
using GorillaEscape.Gameplay.Input;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Splash cinematográfico + menú de letreros 3D. Construye todo el escenario tropical y el
/// gorila procedimentalmente (sin assets externos), ejecuta el bootstrap en paralelo y
/// conduce a la conexión/juego existentes.
/// </summary>
public sealed class SplashDirector : MonoBehaviour
{
    private enum State { Cinematic, Menu, Comic, Exiting }

    private State state = State.Cinematic;
    private float startedAt;
    private Camera cam;
    private ProceduralGorilla gorilla;
    private Transform gorillaRoot;
    private readonly Sign[] signs = new Sign[3];
    private int hoveredSign = -1;
    private int comicPanel;
    private PortableServiceHost services;
    private MotionSession session;
    private bool bootstrapped;

    private const float CinematicDuration = 7.5f;
    private const float GorillaStartZ = 26f;
    private const float GorillaEndZ = 5f;

    private struct Sign
    {
        public Transform board;
        public string title;
        public string subtitle;
        public string action;
    }

    private static readonly string[] ComicLines =
    {
        "UNA TORMENTA EN ALTAMAR…",
        "TU BARCO NAUFRAGA Y DESPIERTAS EN UNA PLAYA DESCONOCIDA.",
        "UNOS GORILAS TE OBSERVAN DESDE LA SELVA…",
        "SU CACIQUE KONG TE PRESENTA LAS 'GORILIMPIADAS'.",
        "SI VENCES LAS PRUEBAS, TE CONSTRUIRÁN UNA BALSA PARA ESCAPAR.",
        "¡QUE COMIENCEN LAS GORILIMPIADAS!"
    };

    private void Awake()
    {
        startedAt = Time.unscaledTime;
        services = PortableServiceHost.Ensure();
        session = MotionSession.Ensure();
        StartCoroutine(Bootstrap());
        BuildEnvironment();
        BuildGorilla();
        BuildSigns();
        EnsureCamera();
    }

    private IEnumerator Bootstrap()
    {
        yield return JungleAudioBank.Prewarm();
        session.Connect();
        bootstrapped = true;
    }

    private void EnsureCamera()
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
        cam.backgroundColor = new Color(0.45f, 0.78f, 0.95f, 1f);
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 400f;
        cam.fieldOfView = 60f;
        cam.transform.position = new Vector3(0f, 2.0f, 0f);
        cam.transform.rotation = Quaternion.Euler(6f, 180f, 0f);
    }

    private void BuildEnvironment()
    {
        var root = new GameObject("Escenario Tropical").transform;
        // Arena
        JungleStageBuilder.CreatePrimitive(PrimitiveType.Cube, "Arena", new Vector3(0f, -0.15f, 12f),
            new Vector3(48f, 0.3f, 60f), new Color(0.85f, 0.76f, 0.55f), root);
        // Sendero (arena más oscura)
        JungleStageBuilder.CreatePrimitive(PrimitiveType.Cube, "Sendero", new Vector3(0f, -0.1f, 12f),
            new Vector3(4.5f, 0.05f, 50f), new Color(0.72f, 0.62f, 0.42f), root);
        // Mar al fondo
        JungleStageBuilder.CreatePrimitive(PrimitiveType.Cube, "Mar", new Vector3(0f, -0.28f, 34f),
            new Vector3(60f, 0.24f, 40f), new Color(0.18f, 0.52f, 0.75f), root);
        // Sol (esfera dorada lejana)
        JungleStageBuilder.CreatePrimitive(PrimitiveType.Sphere, "Sol", new Vector3(6f, 14f, 46f),
            Vector3.one * 3.2f, new Color(1f, 0.92f, 0.55f), root);

        // Palmeras a los lados del sendero
        for (int i = 0; i < 8; i++)
        {
            float z = 2f + i * 6.5f;
            float side = (i % 2 == 0) ? -6.5f : 6.5f;
            BuildPalm(root, new Vector3(side + Random.Range(-1.2f, 1.2f), 0f, z));
        }
        // Rocas
        for (int i = 0; i < 5; i++)
            JungleStageBuilder.CreatePrimitive(PrimitiveType.Sphere, "Roca",
                new Vector3(Random.Range(-14f, 14f), -0.1f, Random.Range(3f, 30f)),
                new Vector3(Random.Range(0.6f, 1.6f), Random.Range(0.4f, 0.9f), Random.Range(0.6f, 1.6f)),
                new Color(0.45f, 0.42f, 0.38f), root);
    }

    private static void BuildPalm(Transform parent, Vector3 pos)
    {
        var palm = new GameObject("Palmera").transform;
        palm.SetParent(parent, false);
        palm.position = pos;
        var trunk = JungleStageBuilder.CreatePrimitive(PrimitiveType.Cylinder, "Tronco",
            new Vector3(0f, 2.2f, 0f), new Vector3(0.45f, 2.2f, 0.45f), new Color(0.55f, 0.38f, 0.22f), palm).transform;
        trunk.localRotation = Quaternion.Euler(Random.Range(-6f, 6f), 0f, Random.Range(-6f, 6f));
        var crown = new GameObject("Corona").transform;
        crown.SetParent(palm, false);
        crown.localPosition = new Vector3(0f, 4.3f, 0f);
        for (int f = 0; f < 7; f++)
        {
            float a = f * Mathf.PI * 2f / 7f;
            var frond = JungleStageBuilder.CreatePrimitive(PrimitiveType.Cube, "Hoja",
                new Vector3(Mathf.Cos(a) * 1.4f, 0f, Mathf.Sin(a) * 1.4f),
                new Vector3(0.35f, 0.12f, 2.2f), new Color(0.20f, 0.62f, 0.28f), crown).transform;
            frond.localRotation = Quaternion.Euler(18f, -a * Mathf.Rad2Deg, 0f);
        }
    }

    private void BuildGorilla()
    {
        gorillaRoot = new GameObject("Gorila Runner").transform;
        gorillaRoot.position = new Vector3(0f, 0f, GorillaStartZ);
        gorilla = ProceduralGorilla.Build(gorillaRoot, 2.2f);
        gorillaRoot.rotation = Quaternion.LookRotation(Vector3.back); // corre hacia la cámara (-Z)
    }

    private void BuildSigns()
    {
        var root = new GameObject("Letreros").transform;
        string[] titles = { "MODO FIESTA", "MODO HISTORIA", "CONFIGURACIÓN" };
        string[] subs = { "10 desafíos", "Escapa de la isla", "Cámara y celular" };
        string[] actions = { "PARTY", "STORY", "CONFIG" };
        float[] xs = { -4.6f, 0f, 4.6f };
        for (int i = 0; i < 3; i++)
        {
            var signRoot = new GameObject("Letrero " + titles[i]).transform;
            signRoot.SetParent(root, false);
            signRoot.position = new Vector3(xs[i], 0f, 11f);

            // Postes de bambú
            JungleStageBuilder.CreatePrimitive(PrimitiveType.Cylinder, "PosteL",
                new Vector3(-1.35f, 1.4f, 0f), new Vector3(0.12f, 1.4f, 0.12f), new Color(0.55f, 0.42f, 0.20f), signRoot);
            JungleStageBuilder.CreatePrimitive(PrimitiveType.Cylinder, "PosteR",
                new Vector3(1.35f, 1.4f, 0f), new Vector3(0.12f, 1.4f, 0.12f), new Color(0.55f, 0.42f, 0.20f), signRoot);
            // Cuerdas (cilindros finos diagonales)
            var ropeL = JungleStageBuilder.CreatePrimitive(PrimitiveType.Cylinder, "CuerdaL",
                new Vector3(-1.35f, 2.6f, 0f), new Vector3(0.04f, 0.6f, 0.04f), new Color(0.62f, 0.50f, 0.32f), signRoot).transform;
            ropeL.localRotation = Quaternion.Euler(0f, 0f, 32f);
            var ropeR = JungleStageBuilder.CreatePrimitive(PrimitiveType.Cylinder, "CuerdaR",
                new Vector3(1.35f, 2.6f, 0f), new Vector3(0.04f, 0.6f, 0.04f), new Color(0.62f, 0.50f, 0.32f), signRoot).transform;
            ropeR.localRotation = Quaternion.Euler(0f, 0f, -32f);
            // Tabla de madera (con collider para raycast)
            var board = JungleStageBuilder.CreatePrimitive(PrimitiveType.Cube, "Tabla",
                new Vector3(0f, 2.35f, 0f), new Vector3(3.0f, 1.1f, 0.18f), new Color(0.62f, 0.44f, 0.26f), signRoot, keepCollider: true);
            board.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            // Borde de bambú
            JungleStageBuilder.CreatePrimitive(PrimitiveType.Cube, "Borde",
                new Vector3(0f, 2.35f, 0.11f), new Vector3(3.1f, 1.2f, 0.05f), new Color(0.50f, 0.38f, 0.18f), signRoot);
            // Hojas decorativas
            JungleStageBuilder.CreatePrimitive(PrimitiveType.Sphere, "HojaI",
                new Vector3(-1.7f, 2.75f, 0f), new Vector3(0.5f, 0.25f, 0.4f), new Color(0.22f, 0.6f, 0.28f), signRoot);

            signs[i] = new Sign { board = board.transform, title = titles[i], subtitle = subs[i], action = actions[i] };
        }
    }

    private void Update()
    {
        float t = Time.unscaledTime - startedAt;

        if (state == State.Cinematic)
        {
            float u = Mathf.Clamp01(t / CinematicDuration);
            // Gorila corre hacia la cámara
            float z = Mathf.Lerp(GorillaStartZ, GorillaEndZ, u);
            gorillaRoot.position = new Vector3(0f, 0f, z);
            gorilla.SetRunCycle(t * 8f, 1f);
            // Cámara cinematográfica: leve dolly-in y panorámica
            cam.transform.position = Vector3.Lerp(new Vector3(0f, 2.0f, 0f), new Vector3(0f, 1.7f, -1.2f), u);
            cam.transform.rotation = Quaternion.Euler(Mathf.Lerp(6f, 4f, u), 180f, 0f);

            if (u >= 1f || AnySkip())
            {
                gorilla.SetIdle();
                state = State.Menu;
            }
        }
        else if (state == State.Menu)
        {
            UpdateSignRaycast();
            if (hoveredSign >= 0 && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Return)))
                ActivateSign(hoveredSign);
        }
        else if (state == State.Comic)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.Space))
            {
                if (comicPanel >= ComicLines.Length - 1) { TournamentSession.StartStoryMode(); LoadConnection(); }
                else comicPanel++;
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow)) comicPanel = Mathf.Max(comicPanel - 1, 0);
            if (Input.GetKeyDown(KeyCode.Escape)) LoadConnection();
        }

        if (Input.GetKeyDown(KeyCode.Escape) && state == State.Menu)
        {
            // Esc en el menú: re-inicia el splash (no bloquea el bootstrap)
            state = State.Cinematic;
            startedAt = Time.unscaledTime;
        }
    }

    private static bool AnySkip()
    {
        return Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1);
    }

    private void UpdateSignRaycast()
    {
        hoveredSign = -1;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 200f))
        {
            for (int i = 0; i < signs.Length; i++)
                if (signs[i].board != null && hit.collider.transform.IsChildOf(signs[i].board) || (signs[i].board != null && hit.collider.transform == signs[i].board))
                    hoveredSign = i;
        }
    }

    private void ActivateSign(int index)
    {
        switch (signs[index].action)
        {
            case "STORY": state = State.Comic; comicPanel = 0; break;
            case "PARTY": TournamentSession.StartPartyMode(); LoadConnection(); break;
            default: LoadConnection(); break;
        }
    }

    private void LoadConnection()
    {
        state = State.Exiting;
        SceneNavigator.Load(SceneNavigator.Connection);
    }

    private void OnGUI()
    {
        if (cam == null) return;
        var title = new GUIStyle(GUI.skin.label)
        {
            font = JungleUITheme.TitleFont,
            fontSize = 44,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        title.normal.textColor = new Color(1f, 0.86f, 0.18f);
        var body = new GUIStyle(GUI.skin.label)
        {
            font = JungleUITheme.TextFont,
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        body.normal.textColor = Color.white;

        if (state == State.Cinematic)
        {
            GUI.Label(new Rect(0, Screen.height * 0.06f, Screen.width, 60), "GORILLA ESCAPE", title);
            GUI.Label(new Rect(0, Screen.height * 0.88f, Screen.width, 30), "Preparando la isla… (clic para omitir)", body);
        }
        else if (state == State.Menu)
        {
            GUI.Label(new Rect(0, Screen.height * 0.05f, Screen.width, 52), "GORILLA ESCAPE", title);
            for (int i = 0; i < signs.Length; i++)
            {
                if (signs[i].board == null) continue;
                Vector3 sp = cam.WorldToScreenPoint(signs[i].board.position + Vector3.up * 0.3f);
                if (sp.z < 0f) continue; // detrás de la cámara
                sp.y = Screen.height - sp.y;
                var signTitle = new GUIStyle(title) { fontSize = 26 };
                signTitle.normal.textColor = i == hoveredSign ? new Color(1f, 0.95f, 0.4f) : new Color(1f, 0.9f, 0.55f);
                GUI.Label(new Rect(sp.x - 140, sp.y - 20, 280, 36), signs[i].title, signTitle);
                var signSub = new GUIStyle(body) { fontSize = 15 };
                GUI.Label(new Rect(sp.x - 140, sp.y + 16, 280, 24), signs[i].subtitle, signSub);
            }
            GUI.Label(new Rect(0, Screen.height * 0.92f, Screen.width, 26), "Apunta y haz clic en un letrero", body);
        }
        else if (state == State.Comic)
        {
            GUI.Box(new Rect(Screen.width * 0.08f, Screen.height * 0.12f, Screen.width * 0.84f, Screen.height * 0.72f), GUIContent.none);
            GUI.Label(new Rect(Screen.width * 0.12f, Screen.height * 0.2f, Screen.width * 0.76f, 120), "HISTORIA", title);
            var line = new GUIStyle(body) { fontSize = 26, wordWrap = true };
            GUI.Label(new Rect(Screen.width * 0.12f, Screen.height * 0.4f, Screen.width * 0.76f, Screen.height * 0.3f), ComicLines[comicPanel], line);
            GUI.Label(new Rect(0, Screen.height * 0.82f, Screen.width, 30), "→ siguiente · ← anterior · ESC omitir", body);
        }
    }
}
