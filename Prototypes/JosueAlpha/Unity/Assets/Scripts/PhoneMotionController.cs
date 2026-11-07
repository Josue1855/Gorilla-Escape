using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using GLTFast;

public sealed class PhoneMotionController : MonoBehaviour
{
    [Serializable] private struct MotionPacket
    {
        public string type;
        public long time;
        public float alpha, beta, gamma, screen, ax, ay, az, rx, ry, rz;
        public int confirm, recenter;
    }

    [Serializable] private struct PosePacket
    {
        public bool detected;
        public bool leftDetected, rightDetected;
        public string hand;
        public float wristX, wristY, bodyX, confidence;
        public float leftWristX, leftWristY, rightWristX, rightWristY;
        public long time;
    }

    private enum GameState { Connecting, Menu, Aim, Rolling, Results, TargetGame, BalanceGame, KarateGame, MiniResults, Boxing, BoxingKnockdown, BoxingOver }
    private sealed class KarateItem
    {
        public float x;
        public bool cactus;
    }
    private readonly List<GameObject> pins = new List<GameObject>();
    private readonly Dictionary<GameObject, Vector3> pinRackPositions = new Dictionary<GameObject, Vector3>();
    private readonly Dictionary<GameObject, Quaternion> pinRackRotations = new Dictionary<GameObject, Quaternion>();
    private readonly HashSet<GameObject> pinsFallenThisRoll = new HashSet<GameObject>();
    private readonly List<Vector3> poseTrail = new List<Vector3>();
    private readonly List<KarateItem> karateItems = new List<KarateItem>();
    private readonly List<int>[] bowlingFrames = new List<int>[10];
    private readonly int[] bowlingScoreCache = new int[10];
    private readonly int[] bowlingRollCache = new int[21];
    private Rigidbody ballBody;
    private GameObject ballVisual;
    private GameObject gorillaMascot;
    private GltfImport gorillaImport;
    private GameObject islandBackground;
    private BowlingHybridEnvironment hybridBowlingEnvironment;
    private GameObject boxingArena;
    private GltfImport islandImport;
    private Transform gorillaHead, gorillaNeck, gorillaSpine, gorillaHipLeft, gorillaHipRight, gorillaKneeLeft, gorillaKneeRight;
    private Transform gorillaShoulderLeft, gorillaShoulderRight, gorillaElbowLeft, gorillaElbowRight, gorillaWristLeft, gorillaWristRight;
    private Quaternion gorillaHeadRest, gorillaNeckRest, gorillaSpineRest, gorillaHipLeftRest, gorillaHipRightRest, gorillaKneeLeftRest, gorillaKneeRightRest;
    private Quaternion gorillaLeftRest, gorillaRightRest, gorillaElbowLeftRest, gorillaElbowRightRest, gorillaWristLeftRest, gorillaWristRightRest;
    private Vector3 gorillaRestPosition;
    private Quaternion gorillaRestRotation;
    private float gorillaReactionUntil;
    private int gorillaReaction;
    private int gorillaVariant, previousGorillaVariant = -1;
    private string connectionState = "PULSA CONECTAR";
    private string roomCode = "";
    private GameState state = GameState.Connecting;
    private bool polling, calibrated, hasRaw;
    private float lastPacketAt = -10f, centerGamma, centerBeta, latestGamma, latestBeta, aimX, power, resultAt;
    private float miniTime, lastMiniAction, balancePoints, aimReadyAt;
    private float poseWristX = .5f, poseBodyX = .5f, poseWristVelocityX, poseSampleAt;
    private Vector2 boxingLeftHand = new Vector2(.36f, .63f), boxingRightHand = new Vector2(.64f, .63f);
    private Vector2 previousBoxingLeft, previousBoxingRight;
    private float boxingLeftSpeed, boxingRightSpeed, playerHealth, gorillaHealth, boxingRoundTime, gorillaActionAt, gorillaAttackResolveAt, boxingMessageUntil, getUpEnergy;
    private int boxingGorillaAction, boxingMessageCode;
    private bool boxingAttackResolved, boxingPowerReady = true;
    private readonly Vector3 bowlingCameraPosition = new Vector3(0, 6.4f, -12.5f);
    private float swingStartedAt, swingPeakPower, swingStartGamma, lastLaunchDirection;
    private float karateSpawnAt, karateFlashUntil;
    private int round = 1, totalScore, lastPins, menuSelection, lastConfirm, lastRecenter, miniScore, miniMisses;
    private int bowlingFrame, bowlingBall = 1;
    private bool confirmInitialized;
    private bool poseDetected;
    private bool swingPending;
    private bool karateLost;
    private bool karateMotionHigh;
    private bool bowlingGameOver, nextNeedsFullRack;
    private bool editableScene;
    private Transform gorillaAnchor;
    private Transform islandAnchor;
    private float lastPoseAt = -10f;
    private Vector2 menuCursor = new Vector2(.32f, .56f);
    private Vector2 gameCursor = new Vector2(.5f, .55f), targetPoint = new Vector2(.65f, .42f), balanceTarget = new Vector2(.5f, .55f);
    private Quaternion rawRotation = Quaternion.identity;
    private GUIStyle titleStyle, bigStyle, bodyStyle, buttonStyle;
    private GUIStyle dashHeadingStyle, dashRowStyle, dashRowActiveStyle, dashScoreStyle, dashScoreActiveStyle;
    private Texture2D panelTexture, accentTexture, selectedTexture, optionTexture, coconutTexture, cactusTexture, darkTexture, boxingBlueTexture, boxingRedTexture, gorillaTexture, leavesBottom, leavesLeft, leavesRight;
    private MotionSession sharedSession;

    private void Awake()
    {
        Application.runInBackground = true;
        for (int i = 0; i < bowlingFrames.Length; i++) bowlingFrames[i] = new List<int>();
        sharedSession = MotionSession.Ensure();
        roomCode = sharedSession.RoomCode;
        polling = sharedSession.IsPolling;
        gorillaTexture = Resources.Load<Texture2D>("Jungle/gorila_menu");
        leavesBottom = Resources.Load<Texture2D>("Jungle/hojas_inferiores");
        leavesLeft = Resources.Load<Texture2D>("Jungle/hojas_superior_izquierda");
        leavesRight = Resources.Load<Texture2D>("Jungle/hoja_superior_derecha");
        editableScene = GameObject.Find("ESCENARIO EDITABLE") != null;
        if (editableScene) BindEditableScene(); else BuildBowlingAlley();
        BuildBoxingArena();
        LoadGorillaMascot();
        hybridBowlingEnvironment = gameObject.AddComponent<BowlingHybridEnvironment>();
        ResetRack();
        if (SceneManager.GetActiveScene().name == SceneNavigator.Boxing) StartBoxingGame();
    }

    private static string RandomRoom()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var r = new char[4];
        for (int i = 0; i < 4; i++) r[i] = chars[UnityEngine.Random.Range(0, chars.Length)];
        return new string(r);
    }

    private Material MakeMaterial(Color color, float metallic = 0f, float smoothness = .35f)
    {
        var material = new Material(Shader.Find("Standard"));
        material.color = color;
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Glossiness", smoothness);
        return material;
    }

    private Material MakeCoconutMaterial()
    {
        var material = MakeMaterial(Color.white, 0f, .08f);
        var texture = new Texture2D(128, 64, TextureFormat.RGB24, false);
        texture.name = "Fibra de coco";
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;
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
        texture.Apply();
        material.mainTexture = texture;
        return material;
    }

    private void AddCoconutEye(Vector3 localPosition, float size)
    {
        GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eye.name = "Marca del coco";
        eye.transform.SetParent(ballVisual.transform, false);
        eye.transform.localPosition = localPosition;
        eye.transform.localScale = new Vector3(size, size * .82f, size * .22f);
        eye.GetComponent<Renderer>().material = MakeMaterial(new Color(.055f, .022f, .009f), 0f, .04f);
        eye.GetComponent<Collider>().enabled = false;
    }

    private GameObject Box(string name, Vector3 position, Vector3 scale, Color color)
    {
        GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube);
        item.name = name; item.transform.position = position; item.transform.localScale = scale;
        item.GetComponent<Renderer>().material = MakeMaterial(color);
        return item;
    }

    private void BuildBowlingAlley()
    {
        transform.localScale = Vector3.one;
        Renderer controllerRenderer = GetComponent<Renderer>();
        if (controllerRenderer != null) controllerRenderer.enabled = false;
        Collider oldCollider = GetComponent<Collider>(); if (oldCollider != null) oldCollider.enabled = false;
        Box("Pista", new Vector3(0, -.35f, 2), new Vector3(7.2f, .35f, 21), new Color(.28f, .14f, .045f));
        Box("Franja central", new Vector3(0, -.16f, 2), new Vector3(6.4f, .05f, 20.6f), new Color(.68f, .43f, .14f));
        Box("Canal izquierdo", new Vector3(-4.15f, -.2f, 2), new Vector3(1.1f, .28f, 21), new Color(.025f, .12f, .07f));
        Box("Canal derecho", new Vector3(4.15f, -.2f, 2), new Vector3(1.1f, .28f, 21), new Color(.025f, .12f, .07f));
        Box("Pared fondo", new Vector3(0, 2.5f, 12.3f), new Vector3(11, 5.5f, .4f), new Color(.04f, .18f, .09f));
        for (int i = -3; i <= 3; i++) { GameObject mark = Box("Marca visual", new Vector3(i, -.135f, -5.4f), new Vector3(.08f, .015f, .6f), new Color(.95f, .84f, .45f)); mark.GetComponent<Collider>().enabled = false; }
        for (int side = -1; side <= 1; side += 2)
        {
            for (int z = -5; z <= 10; z += 3)
            {
                // El costado izquierdo central queda libre como zona del gorila espectador.
                if (side == -1 && z >= 1 && z <= 7) continue;
                CreateJunglePlant(side * 5.2f, z);
            }
        }
        GameObject gorillaStage = Box("Plataforma del gorila", new Vector3(-5.0f, -.18f, 4.2f), new Vector3(2.8f, .28f, 4.5f), new Color(.18f, .09f, .035f));
        gorillaStage.GetComponent<Collider>().enabled = false;

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
        var sphere = gameObject.AddComponent<SphereCollider>(); sphere.radius = .53f;

        Camera camera = Camera.main;
        camera.orthographic = false; camera.fieldOfView = 52f;
        camera.transform.position = new Vector3(0, 6.4f, -12.5f);
        camera.transform.LookAt(new Vector3(0, .5f, 2.8f));
        camera.backgroundColor = new Color(.025f, .13f, .075f);
        Light light = FindFirstObjectByType<Light>();
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
        if (GetComponent<SphereCollider>() == null) { var sphere = gameObject.AddComponent<SphereCollider>(); sphere.radius = .53f; }
        gorillaAnchor = GameObject.Find("Punto del gorila")?.transform;
        islandAnchor = GameObject.Find("Punto del fondo isla")?.transform;
        pins.Clear(); pinRackPositions.Clear(); pinRackRotations.Clear();
        GameObject rack = GameObject.Find("Pinos editables");
        if (rack != null)
        {
            foreach (Transform child in rack.transform)
            {
                if (!child.name.StartsWith("Pino")) continue;
                pins.Add(child.gameObject);
                pinRackPositions[child.gameObject] = child.position;
                pinRackRotations[child.gameObject] = child.rotation;
            }
        }
    }

    private void BuildBoxingArena()
    {
        boxingArena = new GameObject("ARENA DE BOXEO");
        GameObject floor = Box("Lona azul del ring", new Vector3(0, -.45f, -1f), new Vector3(14f, .25f, 16f), new Color(.035f, .22f, .48f)); floor.transform.SetParent(boxingArena.transform);
        GameObject back = Box("Fondo del estadio", new Vector3(0, 3.2f, 5.7f), new Vector3(15f, 7.2f, .35f), new Color(.012f, .016f, .028f)); back.transform.SetParent(boxingArena.transform);
        GameObject leftWall = Box("Pared izquierda", new Vector3(-7f, 2.5f, -.5f), new Vector3(.3f, 6f, 13f), new Color(.04f, .055f, .06f)); leftWall.transform.SetParent(boxingArena.transform);
        GameObject rightWall = Box("Pared derecha", new Vector3(7f, 2.5f, -.5f), new Vector3(.3f, 6f, 13f), new Color(.04f, .055f, .06f)); rightWall.transform.SetParent(boxingArena.transform);
        for (int side = -1; side <= 1; side += 2)
            for (int level = 0; level < 3; level++)
            {
                GameObject rope = Box("Cuerda del ring", new Vector3(side * 5.4f, .9f + level * .62f, -.6f), new Vector3(.09f, .09f, 11.5f), new Color(.82f, .12f, .08f));
                rope.transform.SetParent(boxingArena.transform); rope.GetComponent<Collider>().enabled = false;
            }
        for (int x = -1; x <= 1; x += 2)
        {
            GameObject lamp = Box("Lámpara superior", new Vector3(x * 2.8f, 5.3f, 1.6f), new Vector3(2.2f, .12f, .35f), new Color(.72f, .88f, .9f));
            lamp.transform.SetParent(boxingArena.transform); lamp.GetComponent<Collider>().enabled = false;
        }
        for (int level = 0; level < 3; level++)
        {
            GameObject rearRope = Box("Cuerda trasera", new Vector3(0, .9f + level * .62f, 2.15f), new Vector3(11f, .09f, .09f), level == 1 ? Color.white : new Color(.86f, .08f, .06f));
            rearRope.transform.SetParent(boxingArena.transform); rearRope.GetComponent<Collider>().enabled = false;
        }
        for (int i = -6; i <= 6; i++)
        {
            GameObject spectator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            spectator.name = "Silueta del público"; spectator.transform.SetParent(boxingArena.transform);
            spectator.transform.position = new Vector3(i * 1.05f, 2.15f + Mathf.Abs(i % 2) * .28f, 4.9f);
            spectator.transform.localScale = new Vector3(.72f, 1.15f, .55f);
            spectator.GetComponent<Renderer>().material = MakeMaterial(new Color(.025f + (i & 1) * .018f, .03f, .045f));
            spectator.GetComponent<Collider>().enabled = false;
        }
        for (int side = -1; side <= 1; side += 2)
        {
            GameObject lightObject = new GameObject("Reflector del ring"); lightObject.transform.SetParent(boxingArena.transform);
            lightObject.transform.position = new Vector3(side * 3.8f, 5.2f, -3.8f);
            lightObject.transform.LookAt(new Vector3(0, 2.2f, -2.5f));
            Light spot = lightObject.AddComponent<Light>(); spot.type = LightType.Spot; spot.intensity = 5.2f; spot.range = 14f; spot.spotAngle = 48f;
            spot.color = side < 0 ? new Color(.72f, .85f, 1f) : new Color(1f, .78f, .62f);
        }
        boxingArena.SetActive(false);
    }

    private void CreateJunglePlant(float x, float z)
    {
        GameObject trunk = Box("Tronco selva", new Vector3(x, 1.1f, z), new Vector3(.45f, 2.3f, .45f), new Color(.25f, .11f, .035f));
        trunk.GetComponent<Collider>().enabled = false;
        GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere); crown.name = "Copa selva"; crown.transform.position = new Vector3(x, 2.7f, z); crown.transform.localScale = new Vector3(2.1f, 1.5f, 1.6f); crown.GetComponent<Renderer>().material = MakeMaterial(new Color(.03f, .35f, .12f)); crown.GetComponent<Collider>().enabled = false;
    }

    private async void LoadGorillaMascot()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "GorillaLowPoly.glb");
        if (!File.Exists(path)) { Debug.LogWarning("No se encontró GorillaLowPoly.glb"); return; }
        gorillaMascot = new GameObject("Gorila espectador");
        gorillaImport = new GltfImport();
        bool loaded = await gorillaImport.Load(new Uri(path).AbsoluteUri);
        if (!loaded || gorillaMascot == null) { Debug.LogWarning("No se pudo cargar el gorila GLB"); return; }
        bool instantiated = await gorillaImport.InstantiateMainSceneAsync(gorillaMascot.transform);
        if (!instantiated || gorillaMascot == null) return;

        bool boxingScene = SceneManager.GetActiveScene().name == SceneNavigator.Boxing;
        Vector3 intendedPosition = boxingScene ? new Vector3(0f, -.30f, -2.75f) : gorillaAnchor != null ? gorillaAnchor.position : new Vector3(-4.3f, -.02f, 3.8f);
        if (!boxingScene) intendedPosition.x = Mathf.Max(intendedPosition.x, -4.45f);
        Vector3 intendedFacing = Camera.main != null ? Camera.main.transform.position - intendedPosition : Vector3.back;
        intendedFacing.y = 0f;
        gorillaMascot.transform.rotation = intendedFacing.sqrMagnitude > .01f ? Quaternion.LookRotation(intendedFacing.normalized) : Quaternion.identity;
        if (!boxingScene && gorillaAnchor != null) gorillaMascot.transform.rotation *= gorillaAnchor.rotation;
        Renderer[] renderers = gorillaMascot.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            float scale = (boxingScene ? 5.6f : 4.25f) / Mathf.Max(.01f, bounds.size.y);
            gorillaMascot.transform.localScale = Vector3.one * scale;
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            gorillaMascot.transform.position += new Vector3(intendedPosition.x - bounds.center.x, intendedPosition.y - bounds.min.y, intendedPosition.z - bounds.center.z);
        }
        foreach (Collider item in gorillaMascot.GetComponentsInChildren<Collider>(true)) item.enabled = false;

        gorillaHead = FindGorillaBone("Head_M_040");
        gorillaNeck = FindGorillaBone("Neck_M_039");
        gorillaSpine = FindGorillaBone("Spine2_M_037");
        gorillaHipLeft = FindGorillaBone("Hip_L_08");
        gorillaHipRight = FindGorillaBone("Hip_R_022");
        gorillaKneeLeft = FindGorillaBone("Knee_L_09");
        gorillaKneeRight = FindGorillaBone("Knee_R_023");
        gorillaShoulderLeft = FindGorillaBone("Shoulder_L_043");
        gorillaShoulderRight = FindGorillaBone("Shoulder_R_062");
        gorillaElbowLeft = FindGorillaBone("Elbow_L_00");
        gorillaElbowRight = FindGorillaBone("Elbow_R_063");
        gorillaWristLeft = FindGorillaBone("Wrist_L_044");
        gorillaWristRight = FindGorillaBone("Wrist_R_064");
        if (gorillaHead != null) gorillaHeadRest = gorillaHead.localRotation;
        if (gorillaNeck != null) gorillaNeckRest = gorillaNeck.localRotation;
        if (gorillaSpine != null) gorillaSpineRest = gorillaSpine.localRotation;
        if (gorillaHipLeft != null) gorillaHipLeftRest = gorillaHipLeft.localRotation;
        if (gorillaHipRight != null) gorillaHipRightRest = gorillaHipRight.localRotation;
        if (gorillaKneeLeft != null) gorillaKneeLeftRest = gorillaKneeLeft.localRotation;
        if (gorillaKneeRight != null) gorillaKneeRightRest = gorillaKneeRight.localRotation;
        if (gorillaShoulderLeft != null) gorillaLeftRest = gorillaShoulderLeft.localRotation;
        if (gorillaShoulderRight != null) gorillaRightRest = gorillaShoulderRight.localRotation;
        if (gorillaElbowLeft != null) gorillaElbowLeftRest = gorillaElbowLeft.localRotation;
        if (gorillaElbowRight != null) gorillaElbowRightRest = gorillaElbowRight.localRotation;
        if (gorillaWristLeft != null) gorillaWristLeftRest = gorillaWristLeft.localRotation;
        if (gorillaWristRight != null) gorillaWristRightRest = gorillaWristRight.localRotation;
        gorillaRestPosition = gorillaMascot.transform.position;
        gorillaRestRotation = gorillaMascot.transform.rotation;
    }

    private async void LoadIslandBackground()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "EasterIslandLowPoly.glb");
        if (!File.Exists(path)) { Debug.LogWarning("No se encontró el fondo EasterIslandLowPoly.glb"); return; }
        islandBackground = new GameObject("Isla de Pascua alrededor de la pista");
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

    private Transform FindGorillaBone(string boneName)
    {
        if (gorillaMascot == null) return null;
        foreach (Transform item in gorillaMascot.GetComponentsInChildren<Transform>(true))
            if (item.name == boneName) return item;
        return null;
    }

    private void TriggerGorillaReaction(bool strike)
    {
        if (hybridBowlingEnvironment != null) hybridBowlingEnvironment.ShowThrowReaction(strike);
        if (gorillaMascot == null) return;
        gorillaReaction = strike ? 1 : 2;
        gorillaVariant = UnityEngine.Random.Range(0, 3);
        if (gorillaVariant == previousGorillaVariant) gorillaVariant = (gorillaVariant + UnityEngine.Random.Range(1, 3)) % 3;
        previousGorillaVariant = gorillaVariant;
        gorillaReactionUntil = Time.time + 2.6f;
    }

    private void AnimateGorilla()
    {
        if (gorillaMascot == null) return;
        if (state == GameState.Boxing || state == GameState.BoxingKnockdown || state == GameState.BoxingOver) { AnimateBoxingGorilla(); return; }
        bool reacting = Time.time < gorillaReactionUntil;
        float elapsed = Mathf.Max(0f, 2.6f - (gorillaReactionUntil - Time.time));
        float u = Mathf.Clamp01(elapsed / 2.6f);
        float envelope = reacting ? SmoothPulse(u, 0f, .13f, .84f, 1f) : 0f;
        if (reacting && gorillaReaction == 1)
        {
            // Variantes: golpe territorial alternado, rugido erguido y doble golpe pesado.
            float crouch = SmoothPulse(u, .02f, .15f, .30f, .48f);
            float beat1 = SmoothPulse(u, .20f, .27f, .33f, .40f);
            float beat2 = SmoothPulse(u, .36f, .43f, .49f, .56f);
            float beat3 = SmoothPulse(u, .52f, .59f, .65f, .73f);
            float leftBeat = gorillaVariant == 1 ? 0f : Mathf.Max(beat1, beat3);
            float rightBeat = gorillaVariant == 1 ? 0f : gorillaVariant == 2 ? Mathf.Max(beat1, beat3) : beat2;
            float impact = Mathf.Max(leftBeat, rightBeat);
            float roar = gorillaVariant == 1 ? SmoothPulse(u, .18f, .31f, .65f, .83f) : 0f;
            gorillaMascot.transform.position = gorillaRestPosition;
            gorillaMascot.transform.rotation = gorillaRestRotation;
            SetBone(gorillaSpine, gorillaSpineRest, new Vector3(0, impact * 2f, -10f * crouch + 5f * impact + 10f * roar), envelope);
            SetBone(gorillaNeck, gorillaNeckRest, new Vector3(0, -impact * 2f, 7f * crouch - 4f * impact - 9f * roar), envelope);
            SetBone(gorillaHead, gorillaHeadRest, new Vector3(0, (rightBeat - leftBeat) * 3f, -5f * impact - 12f * roar), envelope);
            SetBone(gorillaHipLeft, gorillaHipLeftRest, new Vector3(0, 0, -8f * crouch), envelope);
            SetBone(gorillaHipRight, gorillaHipRightRest, new Vector3(0, 0, 8f * crouch), envelope);
            SetBone(gorillaKneeLeft, gorillaKneeLeftRest, new Vector3(0, 0, 15f * crouch), envelope);
            SetBone(gorillaKneeRight, gorillaKneeRightRest, new Vector3(0, 0, -15f * crouch), envelope);
            SetBone(gorillaShoulderLeft, gorillaLeftRest, new Vector3(0, -20f * leftBeat - 24f * roar, 14f * crouch + 24f * leftBeat + 28f * roar), envelope);
            SetBone(gorillaShoulderRight, gorillaRightRest, new Vector3(0, 20f * rightBeat + 24f * roar, -14f * crouch - 24f * rightBeat - 28f * roar), envelope);
            SetBone(gorillaElbowLeft, gorillaElbowLeftRest, new Vector3(0, 0, 48f * leftBeat - 18f * roar), envelope);
            SetBone(gorillaElbowRight, gorillaElbowRightRest, new Vector3(0, 0, -48f * rightBeat + 18f * roar), envelope);
            SetBone(gorillaWristLeft, gorillaWristLeftRest, new Vector3(0, 0, 10f * leftBeat), envelope);
            SetBone(gorillaWristRight, gorillaWristRightRest, new Vector3(0, 0, -10f * rightBeat), envelope);
        }
        else if (reacting)
        {
            // Variantes: señala y niega, se cubre el rostro o encoge los hombros.
            float lean = SmoothPulse(u, .04f, .17f, .62f, .82f);
            float point = SmoothPulse(u, .18f, .31f, .57f, .74f);
            float shake = Mathf.Sin(Mathf.Clamp01((u - .23f) / .47f) * Mathf.PI * 4f) * point;
            float facePalm = gorillaVariant == 1 ? point : 0f;
            float shrug = gorillaVariant == 2 ? point : 0f;
            if (gorillaVariant != 0) shake *= .25f;
            gorillaMascot.transform.position = gorillaRestPosition;
            gorillaMascot.transform.rotation = gorillaRestRotation * Quaternion.Euler(0, -4f * lean, 0);
            SetBone(gorillaSpine, gorillaSpineRest, new Vector3(0, -5f * lean, -9f * lean), envelope);
            SetBone(gorillaNeck, gorillaNeckRest, new Vector3(0, shake * 11f, 6f * lean), envelope);
            SetBone(gorillaHead, gorillaHeadRest, new Vector3(0, shake * 15f, -3f * lean), envelope);
            SetBone(gorillaHipLeft, gorillaHipLeftRest, new Vector3(0, 0, -5f * lean), envelope);
            SetBone(gorillaHipRight, gorillaHipRightRest, new Vector3(0, 0, 5f * lean), envelope);
            SetBone(gorillaKneeLeft, gorillaKneeLeftRest, new Vector3(0, 0, 8f * lean), envelope);
            SetBone(gorillaKneeRight, gorillaKneeRightRest, new Vector3(0, 0, -8f * lean), envelope);
            SetBone(gorillaShoulderLeft, gorillaLeftRest, new Vector3(0, -18f * shrug, 16f * shrug), envelope);
            SetBone(gorillaShoulderRight, gorillaRightRest, new Vector3(0, 16f * point + 18f * shrug, -20f * point - 16f * shrug), envelope);
            SetBone(gorillaElbowRight, gorillaElbowRightRest, new Vector3(0, 0, -24f * point - 32f * facePalm), envelope);
            SetBone(gorillaWristRight, gorillaWristRightRest, new Vector3(0, 18f * facePalm, -14f * facePalm), envelope);
        }
        else
        {
            gorillaMascot.transform.position = Vector3.Lerp(gorillaMascot.transform.position, gorillaRestPosition, Time.deltaTime * 8f);
            gorillaMascot.transform.rotation = Quaternion.Slerp(gorillaMascot.transform.rotation, gorillaRestRotation, Time.deltaTime * 8f);
            if (gorillaHead != null) gorillaHead.localRotation = Quaternion.Slerp(gorillaHead.localRotation, gorillaHeadRest, Time.deltaTime * 8f);
            if (gorillaSpine != null) gorillaSpine.localRotation = Quaternion.Slerp(gorillaSpine.localRotation, gorillaSpineRest, Time.deltaTime * 8f);
            if (gorillaShoulderLeft != null) gorillaShoulderLeft.localRotation = Quaternion.Slerp(gorillaShoulderLeft.localRotation, gorillaLeftRest, Time.deltaTime * 8f);
            if (gorillaShoulderRight != null) gorillaShoulderRight.localRotation = Quaternion.Slerp(gorillaShoulderRight.localRotation, gorillaRightRest, Time.deltaTime * 8f);
            RestoreGorillaBones();
        }
    }

    private void AnimateBoxingGorilla()
    {
        float pulse = Mathf.Sin(Time.time * 2.2f) * .5f + .5f;
        float attack = boxingGorillaAction == 1 ? Mathf.Clamp01(1f - Mathf.Abs(Time.time - gorillaAttackResolveAt) * 3.2f) : 0f;
        float guard = boxingGorillaAction == 2 ? 1f : .34f;
        float territory = boxingGorillaAction == 3 ? Mathf.Max(0f, Mathf.Sin(Time.time * 10f)) : 0f;
        float hit = Time.time < gorillaReactionUntil ? Mathf.Sin((gorillaReactionUntil - Time.time) * 18f) : 0f;
        gorillaMascot.transform.position = gorillaRestPosition + new Vector3(hit * .10f, 0f, 0f);
        gorillaMascot.transform.rotation = gorillaRestRotation * Quaternion.Euler(0, hit * 5f, 0);
        SetBone(gorillaSpine, gorillaSpineRest, new Vector3(0, hit * 4f, -3f - pulse * 2f + attack * 12f), 1f);
        SetBone(gorillaHead, gorillaHeadRest, new Vector3(0, -hit * 6f, attack * -8f), 1f);
        SetBone(gorillaKneeLeft, gorillaKneeLeftRest, new Vector3(0, 0, 7f + pulse * 2f), 1f);
        SetBone(gorillaKneeRight, gorillaKneeRightRest, new Vector3(0, 0, -7f - pulse * 2f), 1f);
        float leftChest = territory;
        float rightChest = boxingGorillaAction == 3 ? Mathf.Max(0f, Mathf.Sin(Time.time * 10f + Mathf.PI)) : 0f;
        SetBone(gorillaShoulderLeft, gorillaLeftRest, new Vector3(0, -24f * guard - 18f * leftChest, 28f * guard + 24f * leftChest), 1f);
        SetBone(gorillaShoulderRight, gorillaRightRest, new Vector3(0, 24f * guard + 18f * rightChest + attack * 28f, -28f * guard - 24f * rightChest - attack * 38f), 1f);
        SetBone(gorillaElbowLeft, gorillaElbowLeftRest, new Vector3(0, 0, 35f * guard + 48f * leftChest), 1f);
        SetBone(gorillaElbowRight, gorillaElbowRightRest, new Vector3(0, 0, -35f * guard - 48f * rightChest + attack * 18f), 1f);
    }

    private static void SetBone(Transform bone, Quaternion rest, Vector3 euler, float weight)
    {
        if (bone != null) bone.localRotation = Quaternion.Slerp(rest, rest * Quaternion.Euler(euler), weight);
    }

    private static float SmoothPulse(float value, float start, float peakIn, float peakOut, float end)
    {
        if (value <= start || value >= end) return 0f;
        if (value < peakIn) return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, peakIn, value));
        if (value <= peakOut) return 1f;
        return Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(peakOut, end, value));
    }

    private void RestoreGorillaBones()
    {
        float t = Time.deltaTime * 8f;
        if (gorillaNeck != null) gorillaNeck.localRotation = Quaternion.Slerp(gorillaNeck.localRotation, gorillaNeckRest, t);
        if (gorillaHipLeft != null) gorillaHipLeft.localRotation = Quaternion.Slerp(gorillaHipLeft.localRotation, gorillaHipLeftRest, t);
        if (gorillaHipRight != null) gorillaHipRight.localRotation = Quaternion.Slerp(gorillaHipRight.localRotation, gorillaHipRightRest, t);
        if (gorillaKneeLeft != null) gorillaKneeLeft.localRotation = Quaternion.Slerp(gorillaKneeLeft.localRotation, gorillaKneeLeftRest, t);
        if (gorillaKneeRight != null) gorillaKneeRight.localRotation = Quaternion.Slerp(gorillaKneeRight.localRotation, gorillaKneeRightRest, t);
        if (gorillaElbowLeft != null) gorillaElbowLeft.localRotation = Quaternion.Slerp(gorillaElbowLeft.localRotation, gorillaElbowLeftRest, t);
        if (gorillaElbowRight != null) gorillaElbowRight.localRotation = Quaternion.Slerp(gorillaElbowRight.localRotation, gorillaElbowRightRest, t);
        if (gorillaWristLeft != null) gorillaWristLeft.localRotation = Quaternion.Slerp(gorillaWristLeft.localRotation, gorillaWristLeftRest, t);
        if (gorillaWristRight != null) gorillaWristRight.localRotation = Quaternion.Slerp(gorillaWristRight.localRotation, gorillaWristRightRest, t);
    }

    private void CreatePins()
    {
        float startZ = 7.2f;
        for (int row = 0; row < 4; row++)
        {
            for (int col = 0; col <= row; col++)
            {
                GameObject pin = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                pin.name = "Pino";
                pin.transform.position = new Vector3((col - row * .5f) * .78f, .65f, startZ + row * .72f);
                pin.transform.localScale = new Vector3(.43f, .72f, .43f);
                pin.GetComponent<Renderer>().material = MakeMaterial(new Color(.96f, .96f, .9f), 0f, .55f);
                Rigidbody body = pin.AddComponent<Rigidbody>(); body.mass = 1.25f; body.linearDamping = .08f; body.angularDamping = .05f;
                pins.Add(pin);
                pinRackPositions[pin] = pin.transform.position;
                pinRackRotations[pin] = pin.transform.rotation;
            }
        }
    }

    private void ResetRack()
    {
        pinsFallenThisRoll.Clear();
        if (editableScene && pins.Count > 0)
        {
            foreach (GameObject pin in pins)
            {
                if (pin == null || !pinRackPositions.TryGetValue(pin, out Vector3 position)) continue;
                pin.SetActive(true); pin.transform.position = position;
                pin.transform.rotation = pinRackRotations[pin];
                Renderer renderer = pin.GetComponent<Renderer>(); if (renderer != null) renderer.enabled = true;
                Collider collider = pin.GetComponent<Collider>(); if (collider != null) collider.enabled = true;
                Rigidbody body = pin.GetComponent<Rigidbody>();
                if (body != null) StopBody(body, false);
            }
            ResetBallOnly();
            return;
        }
        foreach (GameObject pin in pins) if (pin != null) Destroy(pin);
        pins.Clear(); pinRackPositions.Clear(); pinRackRotations.Clear(); CreatePins();
        ResetBallOnly();
    }

    private void ResetBallOnly()
    {
        StopBody(ballBody, true);
        transform.position = new Vector3(0, .48f, -7.2f);
        transform.rotation = Quaternion.identity;
        aimX = 0; power = 0; aimReadyAt = Time.time + .8f; poseWristVelocityX = 0; swingPending = false;
        poseTrail.Clear();
        state = polling ? GameState.Aim : GameState.Connecting;
    }

    private void StartBowlingGame()
    {
        SetBowlingVisible(true);
        if (gorillaMascot != null) { gorillaMascot.transform.position = gorillaRestPosition; gorillaMascot.transform.rotation = gorillaRestRotation; }
        for (int i = 0; i < bowlingFrames.Length; i++) bowlingFrames[i].Clear();
        bowlingFrame = 0; bowlingBall = 1; totalScore = 0; lastPins = 0;
        bowlingGameOver = false; nextNeedsFullRack = true;
        ResetRack();
    }

    private void StartMiniGame(int game)
    {
        Calibrate(); miniScore = 0; miniMisses = 0; balancePoints = 0; miniTime = 30f; lastMiniAction = -10f; karateLost = false;
        gameCursor = new Vector2(.5f, .55f);
        if (game == 0)
        {
            StartBowlingGame(); state = GameState.Aim;
        }
        else if (game == 1) StartBoxingGame();
        else if (game == 2) state = GameState.BalanceGame;
        else
        {
            karateItems.Clear(); karateLost = false; karateMotionHigh = false; miniTime = 35f;
            karateSpawnAt = Time.time + .35f; karateFlashUntil = 0;
            state = GameState.KarateGame;
        }
    }

    private Vector2 NewTarget()
    {
        return new Vector2(UnityEngine.Random.Range(.2f, .8f), UnityEngine.Random.Range(.34f, .74f));
    }

    private void FinishMiniGame()
    {
        state = GameState.MiniResults; resultAt = Time.time + 3.5f;
    }

    private void SetBowlingVisible(bool visible)
    {
        if (ballVisual != null) ballVisual.SetActive(visible);
        foreach (GameObject pin in pins) if (pin != null) pin.SetActive(visible);
        string[] groups = { "01 - Pista y arquitectura", "02 - Arboles editables", "03 - Zona del gorila", "04 - Fondo editable" };
        foreach (string groupName in groups) { GameObject group = GameObject.Find(groupName); if (group != null) group.SetActive(visible); }
        if (islandBackground != null) islandBackground.SetActive(visible);
        if (hybridBowlingEnvironment != null) hybridBowlingEnvironment.SetVisible(visible);
        if (boxingArena != null) boxingArena.SetActive(!visible);
        Camera camera = Camera.main;
        if (camera == null) return;
        if (visible)
        {
            camera.transform.position = bowlingCameraPosition;
            camera.transform.LookAt(new Vector3(0, .5f, 2.8f)); camera.fieldOfView = 52f;
            camera.backgroundColor = new Color(.025f, .13f, .075f);
        }
        else
        {
            camera.transform.position = new Vector3(0, 2.25f, -8f);
            camera.transform.LookAt(new Vector3(0, 2.55f, -3f)); camera.fieldOfView = 42f;
            camera.backgroundColor = new Color(.012f, .018f, .022f);
        }
    }

    private void StartBoxingGame()
    {
        SetBowlingVisible(false);
        playerHealth = 100f; gorillaHealth = 100f; boxingRoundTime = 90f; getUpEnergy = 0f;
        boxingGorillaAction = 0; gorillaActionAt = Time.time + 1.8f; boxingMessageCode = 0; boxingMessageUntil = 0;
        previousBoxingLeft = boxingLeftHand; previousBoxingRight = boxingRightHand;
        if (gorillaMascot != null)
        {
            gorillaMascot.transform.position = SceneManager.GetActiveScene().name == SceneNavigator.Boxing ? gorillaRestPosition : gorillaRestPosition + new Vector3(4.3f, 0f, -6.8f);
            gorillaMascot.transform.rotation = gorillaRestRotation;
        }
        state = GameState.Boxing;
    }

    private void UpdateBoxing()
    {
        boxingRoundTime -= Time.deltaTime;
        if (boxingRoundTime <= 0f) { state = GameState.BoxingOver; return; }
        if (Time.time >= gorillaActionAt && boxingGorillaAction == 0)
        {
            float roll = UnityEngine.Random.value;
            boxingGorillaAction = roll < .58f ? 1 : roll < .82f ? 2 : 3; // golpe, guardia, gesto territorial
            boxingAttackResolved = false;
            gorillaAttackResolveAt = Time.time + (boxingGorillaAction == 1 ? .62f : 1.05f);
            gorillaActionAt = Time.time + UnityEngine.Random.Range(1.7f, 2.6f);
        }
        if (boxingGorillaAction == 1 && !boxingAttackResolved && Time.time >= gorillaAttackResolveAt)
        {
            boxingAttackResolved = true;
            bool covering = boxingLeftHand.y < .49f && boxingRightHand.y < .49f;
            bool dodging = Mathf.Abs(poseBodyX - .5f) > .13f;
            float damage = dodging ? 0f : covering ? 5f : UnityEngine.Random.Range(14f, 22f);
            playerHealth = Mathf.Max(0f, playerHealth - damage);
            boxingMessageCode = dodging ? 4 : covering ? 3 : 2; boxingMessageUntil = Time.time + .8f;
            if (playerHealth <= 0f) { state = GameState.BoxingKnockdown; getUpEnergy = 0f; resultAt = Time.time + 10f; }
        }
        if (Time.time >= gorillaActionAt) boxingGorillaAction = 0;
    }

    private void TryBoxingPunch(bool right, float cameraSpeed, float phonePower)
    {
        if (state != GameState.Boxing || Time.time < lastMiniAction + .34f || cameraSpeed < .72f) return;
        if (right && phonePower < 215f) return;
        lastMiniAction = Time.time;
        bool gorillaBlocking = boxingGorillaAction == 2;
        float baseDamage = right ? Mathf.Clamp((phonePower - 180f) / 28f, 5f, 20f) : Mathf.Clamp(cameraSpeed * 4.2f, 3f, 10f);
        float damage = gorillaBlocking ? baseDamage * .18f : baseDamage;
        gorillaHealth = Mathf.Max(0f, gorillaHealth - damage);
        boxingMessageCode = gorillaBlocking ? 5 : 1; boxingMessageUntil = Time.time + .55f;
        gorillaReaction = 2; gorillaReactionUntil = Time.time + .38f;
        if (gorillaHealth <= 0f) state = GameState.BoxingOver;
    }

    private void Connect()
    {
        sharedSession.Connect();
        polling = true;
        connectionState = sharedSession.Status;
    }

    private void Update()
    {
        ReadPhone();
        ReadPose();
        AnimateGorilla();
        if (state == GameState.Aim)
        {
            transform.position = Vector3.Lerp(transform.position, new Vector3(aimX, .48f, -7.2f), 12f * Time.deltaTime);
            transform.rotation = Quaternion.identity;
        }
        else if (state == GameState.Rolling && (Time.time >= resultAt || transform.position.z > 11.5f)) FinishRoll();
        else if (state == GameState.Results && Time.time >= resultAt && !bowlingGameOver) AdvanceBowlingTurn();
        else if (state == GameState.Boxing) UpdateBoxing();
        else if (state == GameState.BoxingKnockdown)
        {
            if (getUpEnergy >= 100f) { playerHealth = 45f; boxingGorillaAction = 0; gorillaActionAt = Time.time + 1.5f; state = GameState.Boxing; }
            else if (Time.time >= resultAt) state = GameState.BoxingOver;
        }
        else if (state == GameState.TargetGame || state == GameState.BalanceGame || state == GameState.KarateGame)
        {
            miniTime -= Time.deltaTime;
            if (state == GameState.BalanceGame)
            {
                balanceTarget = new Vector2(.5f + Mathf.Sin(Time.time * .83f) * .22f, .55f + Mathf.Cos(Time.time * 1.17f) * .15f);
                if (Vector2.Distance(gameCursor, balanceTarget) < .115f) balancePoints += Time.deltaTime * 10f;
                miniScore = Mathf.FloorToInt(balancePoints);
            }
            else if (state == GameState.KarateGame) UpdateKarateRow();
            if (miniTime <= 0) FinishMiniGame();
        }
        else if (state == GameState.MiniResults && Time.time >= resultAt) state = GameState.Menu;
    }

    private void UpdateKarateRow()
    {
        for (int i = karateItems.Count - 1; i >= 0; i--)
        {
            karateItems[i].x -= Time.deltaTime * .25f;
            if (karateItems[i].x < -.1f) karateItems.RemoveAt(i);
        }
        if (Time.time >= karateSpawnAt)
        {
            karateItems.Add(new KarateItem { x = 1.08f, cactus = UnityEngine.Random.value < .32f });
            karateSpawnAt = Time.time + UnityEngine.Random.Range(.82f, 1.08f);
        }
    }

    private void TryKarateHit()
    {
        if (Time.time - lastMiniAction < .48f) return;
        float downwardTravel = 0f;
        if (poseTrail.Count >= 2) downwardTravel = poseTrail[poseTrail.Count - 1].y - poseTrail[0].y;
        if (CameraActive && downwardTravel < .035f) return;
        lastMiniAction = Time.time;
        int nearest = -1; float distance = .14f;
        for (int i = 0; i < karateItems.Count; i++)
        {
            float candidate = Mathf.Abs(karateItems[i].x - .34f);
            if (candidate < distance) { distance = candidate; nearest = i; }
        }
        if (nearest < 0) { karateFlashUntil = Time.time + .12f; return; }
        if (karateItems[nearest].cactus)
        {
            karateLost = true; karateItems.Clear(); FinishMiniGame();
        }
        else
        {
            karateItems.RemoveAt(nearest); miniScore++; karateFlashUntil = Time.time + .18f;
        }
    }

    private bool CameraActive => poseDetected && Time.unscaledTime - lastPoseAt < .45f;

    private void ReadPose()
    {
        if (!sharedSession.TryConsumePose(out string json)) return;
        PosePacket pose = JsonUtility.FromJson<PosePacket>(json);
        poseDetected = pose.detected || pose.leftDetected || pose.rightDetected;
        if (!poseDetected) return;
        float now = Time.unscaledTime;
        float dt = poseSampleAt > 0 ? Mathf.Clamp(now - poseSampleAt, .008f, .12f) : .033f;
        if (pose.leftDetected || pose.rightDetected)
        {
            Vector2 newLeft = new Vector2(pose.leftWristX, pose.leftWristY);
            Vector2 newRight = new Vector2(pose.rightWristX, pose.rightWristY);
            boxingLeftSpeed = Vector2.Distance(newLeft, previousBoxingLeft) / dt;
            boxingRightSpeed = Vector2.Distance(newRight, previousBoxingRight) / dt;
            if (pose.leftDetected) boxingLeftHand = Vector2.Lerp(boxingLeftHand, newLeft, .72f);
            if (pose.rightDetected) boxingRightHand = Vector2.Lerp(boxingRightHand, newRight, .72f);
            previousBoxingLeft = newLeft; previousBoxingRight = newRight;
            if (state == GameState.Boxing) TryBoxingPunch(false, boxingLeftSpeed, power);
        }
        float measuredVelocity = (pose.wristX - poseWristX) / dt;
        poseWristVelocityX = Mathf.Lerp(poseWristVelocityX, measuredVelocity, .38f);
        poseWristX = pose.wristX;
        poseBodyX = pose.bodyX;
        poseSampleAt = now;
        poseTrail.Add(new Vector3(pose.wristX - pose.bodyX, pose.wristY, now));
        while (poseTrail.Count > 0 && now - poseTrail[0].z > .65f) poseTrail.RemoveAt(0);
        lastPoseAt = Time.unscaledTime;
        Vector2 hand = new Vector2(
            Mathf.Clamp01(.5f + (pose.wristX - .5f) * 1.55f),
            Mathf.Clamp01(.5f + (pose.wristY - .5f) * 1.35f));
        if (state == GameState.Menu)
        {
            menuCursor = Vector2.Lerp(menuCursor, hand, .72f);
            UpdateMenuSelection();
        }
        else if (state == GameState.TargetGame || state == GameState.BalanceGame)
            gameCursor = Vector2.Lerp(gameCursor, hand, .76f);
        else if (state == GameState.Aim)
            aimX = Mathf.Lerp(-3.05f, 3.05f, Mathf.InverseLerp(.08f, .92f, hand.x));
    }

    private void UpdateMenuSelection()
    {
        Vector2[] choices = { new Vector2(.25f, .48f), new Vector2(.55f, .48f), new Vector2(.76f, .48f) };
        float nearest = float.MaxValue;
        for (int i = 0; i < choices.Length; i++)
        {
            float distance = Vector2.SqrMagnitude(menuCursor - choices[i]);
            if (distance < nearest) { nearest = distance; menuSelection = i; }
        }
    }

    private void ReadPhone()
    {
        if (!sharedSession.TryConsumeMotion(out string json)) return;
        MotionPacket p = JsonUtility.FromJson<MotionPacket>(json); if (p.type != "motion") return;
        lastPacketAt = Time.unscaledTime; latestGamma = p.gamma; latestBeta = p.beta;
        rawRotation = Quaternion.Euler(p.beta, p.alpha, -p.gamma) * Quaternion.Euler(0, 0, -p.screen); hasRaw = true;
        if (!calibrated) Calibrate();
        if (!confirmInitialized)
        {
            lastConfirm = p.confirm; lastRecenter = p.recenter; confirmInitialized = true;
            string activeScene = SceneManager.GetActiveScene().name;
            if (activeScene == SceneNavigator.Bowling) state = GameState.Aim;
            else if (activeScene != SceneNavigator.Boxing) state = GameState.Menu;
        }
        if (p.recenter != lastRecenter) { lastRecenter = p.recenter; Calibrate(); menuCursor = new Vector2(.32f, .56f); }
        if (state == GameState.Menu)
        {
            float dx = Mathf.DeltaAngle(centerGamma, p.gamma);
            float dy = Mathf.DeltaAngle(centerBeta, p.beta);
            Vector2 targetCursor = new Vector2(Mathf.Clamp01(.32f + dx / 75f), Mathf.Clamp01(.56f - dy / 65f));
            if (!CameraActive) menuCursor = Vector2.Lerp(menuCursor, targetCursor, .28f);
            UpdateMenuSelection();
            if (p.confirm != lastConfirm)
            {
                lastConfirm = p.confirm;
                if (menuSelection < 2) StartMiniGame(menuSelection);
                else { Calibrate(); menuCursor = new Vector2(.32f, .56f); }
            }
            return;
        }

        power = Mathf.Sqrt(p.rx * p.rx + p.ry * p.ry + p.rz * p.rz) + new Vector3(p.ax, p.ay, p.az).magnitude * 7f;
        if (state == GameState.BoxingKnockdown)
        {
            if (power > 245f && boxingPowerReady) { getUpEnergy = Mathf.Min(100f, getUpEnergy + Mathf.Clamp((power - 210f) * .12f, 8f, 24f)); boxingPowerReady = false; }
            if (power < 155f) boxingPowerReady = true;
            return;
        }
        if (state == GameState.Boxing)
        {
            if (power > 215f && boxingPowerReady) { TryBoxingPunch(true, boxingRightSpeed, power); boxingPowerReady = false; }
            if (power < 150f) boxingPowerReady = true;
            return;
        }

        if (state == GameState.TargetGame || state == GameState.BalanceGame)
        {
            float dx = Mathf.DeltaAngle(centerGamma, p.gamma);
            float dy = Mathf.DeltaAngle(centerBeta, p.beta);
            Vector2 desired = new Vector2(Mathf.Clamp01(.5f + dx / 70f), Mathf.Clamp01(.55f - dy / 60f));
            if (!CameraActive) gameCursor = Vector2.Lerp(gameCursor, desired, .35f);
            power = Mathf.Sqrt(p.rx * p.rx + p.ry * p.ry + p.rz * p.rz) + new Vector3(p.ax, p.ay, p.az).magnitude * 7f;
            if (state == GameState.TargetGame && power > 285f && Time.time - lastMiniAction > .65f)
            {
                lastMiniAction = Time.time;
                if (Vector2.Distance(gameCursor, targetPoint) < .14f) { miniScore++; targetPoint = NewTarget(); }
                else miniMisses++;
            }
            return;
        }
        if (state == GameState.KarateGame)
        {
            power = Mathf.Sqrt(p.rx * p.rx + p.ry * p.ry + p.rz * p.rz) + new Vector3(p.ax, p.ay, p.az).magnitude * 7f;
            if (power < 185f) karateMotionHigh = false;
            if (power > 310f && !karateMotionHigh)
            {
                karateMotionHigh = true;
                TryKarateHit();
            }
            return;
        }
        if (!CameraActive) aimX = Mathf.Clamp(Mathf.DeltaAngle(centerGamma, p.gamma) / 13f, -3.05f, 3.05f);
        power = Mathf.Sqrt(p.rx * p.rx + p.ry * p.ry + p.rz * p.rz) + new Vector3(p.ax, p.ay, p.az).magnitude * 7f;
        if (state == GameState.Aim && Time.time >= aimReadyAt)
        {
            if (power > 330f)
            {
                swingStartedAt = Time.time;
                swingPeakPower = power;
                swingStartGamma = centerGamma;
                swingPending = false;
                LaunchBall();
            }
        }
    }

    private void Calibrate()
    {
        if (!hasRaw) return;
        centerGamma = latestGamma; centerBeta = latestBeta; calibrated = true; aimX = 0;
    }

    private void LaunchBall()
    {
        state = GameState.Rolling; ballBody.isKinematic = false;
        float speed = Mathf.Clamp(8.3f + (power - 330f) * .022f, 8.3f, 15.8f);
        float direction;
        if (CameraActive)
        {
            float trajectoryX = 0f;
            float handSide = poseWristX - poseBodyX;
            if (poseTrail.Count >= 2)
            {
                Vector3 beginning = poseTrail[0];
                Vector3 ending = poseTrail[poseTrail.Count - 1];
                trajectoryX = ending.x - beginning.x;
                handSide = ending.x;
            }
            float phoneTurn = Mathf.DeltaAngle(swingStartGamma, latestGamma) / 55f;
            direction = Mathf.Clamp(trajectoryX * 6.2f + handSide * .55f + phoneTurn * .3f, -1f, 1f);
        }
        else direction = Mathf.Clamp(Mathf.DeltaAngle(centerGamma, latestGamma) / 28f, -1f, 1f);
        lastLaunchDirection = direction;
        ballBody.linearVelocity = new Vector3(direction * 3.1f, 0, speed);
        ballBody.angularVelocity = new Vector3(speed * 1.4f, 0, -direction * 7.5f);
        resultAt = Time.time + 5.2f;
    }

    private void FinishRoll()
    {
        if (state != GameState.Rolling) return;
        pinsFallenThisRoll.Clear();
        int fallen = 0;
        int activePins = 0;
        foreach (GameObject pin in pins)
        {
            if (pin == null || !pin.activeSelf) continue;
            activePins++;
            if (!IsPinStanding(pin))
            {
                fallen++;
                pinsFallenThisRoll.Add(pin);
                StabilizeFallenPin(pin);
            }
            else FreezeStandingPin(pin);
        }
        bool rackCleared = activePins > 0 && fallen >= activePins;
        lastPins = fallen;
        TriggerGorillaReaction(rackCleared);
        RegisterBowlingRoll(fallen);
        totalScore = CalculateBowlingScores()[Mathf.Clamp(bowlingFrame, 0, 9)];
        if (totalScore < 0)
        {
            totalScore = 0;
            foreach (List<int> frameRolls in bowlingFrames) foreach (int pinsHit in frameRolls) totalScore += pinsHit;
        }
        state = GameState.Results; resultAt = Time.time + 2.7f;
        StopBody(ballBody, true);
    }

    private void RegisterBowlingRoll(int fallen)
    {
        List<int> current = bowlingFrames[bowlingFrame];
        current.Add(fallen);
        if (bowlingFrame < 9)
        {
            if (current.Count == 1 && fallen == 10)
            {
                bowlingFrame++; bowlingBall = 1; nextNeedsFullRack = true;
            }
            else if (current.Count == 1)
            {
                bowlingBall = 2; nextNeedsFullRack = false;
            }
            else
            {
                bowlingFrame++; bowlingBall = 1; nextNeedsFullRack = true;
            }
            return;
        }

        if (current.Count == 1)
        {
            bowlingBall = 2; nextNeedsFullRack = fallen == 10;
        }
        else if (current.Count == 2)
        {
            bool firstStrike = current[0] == 10;
            bool spare = !firstStrike && current[0] + current[1] == 10;
            if (firstStrike || spare)
            {
                bowlingBall = 3;
                nextNeedsFullRack = spare || current[1] == 10;
            }
            else bowlingGameOver = true;
        }
        else bowlingGameOver = true;
    }

    private void AdvanceBowlingTurn()
    {
        if (nextNeedsFullRack) ResetRack();
        else
        {
            for (int i = pins.Count - 1; i >= 0; i--)
            {
                GameObject pin = pins[i];
                if (pin != null && pinsFallenThisRoll.Contains(pin))
                {
                    if (editableScene)
                    {
                        if (pin != null) pin.SetActive(false);
                    }
                    else
                    {
                        if (pin != null) Destroy(pin);
                        if (pin != null) { pinRackPositions.Remove(pin); pinRackRotations.Remove(pin); }
                        pins.RemoveAt(i);
                    }
                }
                else if (pin != null && pin.activeSelf)
                {
                    Rigidbody body = pin.GetComponent<Rigidbody>();
                    if (body != null) StopBody(body, false);
                }
            }
            pinsFallenThisRoll.Clear();
            ResetBallOnly();
        }
    }

    private static void FreezeStandingPin(GameObject pin)
    {
        Rigidbody body = pin != null ? pin.GetComponent<Rigidbody>() : null;
        if (body == null) return;
        StopBody(body, true);
    }

    private bool IsPinStanding(GameObject pin)
    {
        if (pin == null) return false;
        if (!pinRackPositions.TryGetValue(pin, out Vector3 start)) return false;
        Vector3 position = pin.transform.position;
        float horizontalShift = Vector2.Distance(new Vector2(position.x, position.z), new Vector2(start.x, start.z));
        float upright = Vector3.Dot(pin.transform.up, Vector3.up);
        return upright > .80f && position.y > .32f && position.y < 1.35f && horizontalShift < .62f && Mathf.Abs(position.x) < 3.75f && position.z > 6.4f && position.z < 11f;
    }

    private void StabilizeFallenPin(GameObject pin)
    {
        if (pin == null) return;
        Rigidbody body = pin.GetComponent<Rigidbody>();
        if (body != null)
        {
            StopBody(body, true);
        }
        // Si salió despedido o quedó suspendido, se retira visualmente de inmediato.
        if (pin.transform.position.y > .82f || Mathf.Abs(pin.transform.position.x) > 3.7f || pin.transform.position.z < 6.2f || pin.transform.position.z > 11.2f)
        {
            Renderer renderer = pin.GetComponent<Renderer>(); if (renderer != null) renderer.enabled = false;
            Collider collider = pin.GetComponent<Collider>(); if (collider != null) collider.enabled = false;
        }
    }

    private static void StopBody(Rigidbody body, bool makeKinematic)
    {
        if (body == null) return;
        // Unity 6 no permite escribir velocidades mientras el cuerpo es cinemático.
        if (body.isKinematic) body.isKinematic = false;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.isKinematic = makeKinematic;
    }

    private int[] CalculateBowlingScores()
    {
        for (int i = 0; i < bowlingScoreCache.Length; i++) bowlingScoreCache[i] = -1;
        int rollCount = 0;
        foreach (List<int> frameRolls in bowlingFrames)
            foreach (int roll in frameRolls)
                if (rollCount < bowlingRollCache.Length) bowlingRollCache[rollCount++] = roll;
        int index = 0, cumulative = 0;
        for (int frame = 0; frame < 9; frame++)
        {
            if (index >= rollCount) break;
            if (bowlingRollCache[index] == 10)
            {
                if (index + 2 >= rollCount) break;
                cumulative += 10 + bowlingRollCache[index + 1] + bowlingRollCache[index + 2]; index++;
            }
            else
            {
                if (index + 1 >= rollCount) break;
                int framePins = bowlingRollCache[index] + bowlingRollCache[index + 1];
                if (framePins == 10)
                {
                    if (index + 2 >= rollCount) break;
                    cumulative += 10 + bowlingRollCache[index + 2];
                }
                else cumulative += framePins;
                index += 2;
            }
            bowlingScoreCache[frame] = cumulative;
        }
        if (bowlingGameOver)
        {
            int tenth = 0; foreach (int pinsHit in bowlingFrames[9]) tenth += pinsHit;
            cumulative += tenth; bowlingScoreCache[9] = cumulative;
        }
        return bowlingScoreCache;
    }

    private Texture2D Solid(Color color)
    {
        var texture = new Texture2D(1, 1); texture.SetPixel(0, 0, color); texture.Apply(); return texture;
    }

    private void PrepareStyles()
    {
        panelTexture ??= Solid(new Color(.025f, .045f, .09f, .94f));
        accentTexture ??= Solid(new Color(.05f, .72f, .92f, 1f));
        selectedTexture ??= Solid(new Color(1f, .78f, .08f, .96f));
        optionTexture ??= Solid(new Color(.08f, .19f, .13f, .96f));
        coconutTexture ??= Solid(new Color(.34f, .15f, .055f, 1f));
        cactusTexture ??= Solid(new Color(.08f, .62f, .22f, 1f));
        darkTexture ??= Solid(new Color(.035f, .025f, .018f, 1f));
        boxingBlueTexture ??= Solid(new Color(.12f, .55f, 1f, 1f));
        boxingRedTexture ??= Solid(new Color(1f, .25f, .12f, 1f));
        titleStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 32, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(.45f, .94f, 1f) } };
        bigStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 38, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
        bodyStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 21, alignment = TextAnchor.MiddleCenter, wordWrap = true, normal = { textColor = new Color(.86f, .9f, 1f) } };
        buttonStyle ??= new GUIStyle(GUI.skin.button) { fontSize = 22, fontStyle = FontStyle.Bold, normal = { background = accentTexture, textColor = new Color(.01f, .06f, .1f) } };
    }

    private void OnGUI()
    {
        PrepareStyles();
        if (state == GameState.Menu)
        {
            if (gorillaTexture != null) GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), gorillaTexture, ScaleMode.ScaleAndCrop);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Solid(new Color(.01f, .04f, .05f, .24f)));
            GUIStyle menuTitle = new GUIStyle(titleStyle) { fontSize = Mathf.RoundToInt(Screen.height * .065f), normal = { textColor = new Color(1f, .82f, .12f) } };
            GUI.Label(new Rect(Screen.width * .04f, Screen.height * .08f, Screen.width * .62f, Screen.height * .11f), "JUNGLE PARTY", menuTitle);
            GUIStyle subtitle = new GUIStyle(bodyStyle) { fontSize = Mathf.RoundToInt(Screen.height * .027f), alignment = TextAnchor.MiddleLeft };
            GUI.Label(new Rect(Screen.width * .09f, Screen.height * .18f, Screen.width * .55f, Screen.height * .07f), "ELIGE TU DESAFÍO", subtitle);
            DrawMenuChoice(0, "BANANA\nBOWLING", Screen.width * .10f, Screen.height * .35f, Screen.width * .28f, Screen.height * .24f, -4f);
            DrawMenuChoice(1, "BOXEO\nCON GORILA", Screen.width * .41f, Screen.height * .34f, Screen.width * .29f, Screen.height * .25f, 4f);
            DrawMenuChoice(2, "RECENTRAR", Screen.width * .70f, Screen.height * .39f, Screen.width * .17f, Screen.height * .15f, 2f);
            GUIStyle help = new GUIStyle(bodyStyle) { fontSize = Mathf.RoundToInt(Screen.height * .022f) };
            GUI.DrawTexture(new Rect(Screen.width * .14f, Screen.height * .84f, Screen.width * .48f, Screen.height * .075f), panelTexture);
            GUI.Label(new Rect(Screen.width * .15f, Screen.height * .84f, Screen.width * .46f, Screen.height * .075f), "MUEVE EL CELULAR · PULSA CONFIRMAR", help);
            DrawPointer(menuCursor.x * Screen.width, menuCursor.y * Screen.height);
            DrawCameraStatus();
            DrawLeafFrame();
            return;
        }
        if (state == GameState.Boxing || state == GameState.BoxingKnockdown || state == GameState.BoxingOver)
        {
            DrawBoxing(); DrawCameraStatus(); DrawLeafFrame(); return;
        }
        if (state == GameState.TargetGame || state == GameState.BalanceGame || state == GameState.KarateGame || state == GameState.MiniResults)
        {
            DrawMiniGame();
            DrawCameraStatus();
            DrawLeafFrame();
            return;
        }
        float width = Mathf.Min(Screen.width * .67f, 920f);
        float hudX = Screen.width * .025f;
        GUI.DrawTexture(new Rect(hudX, 14, width, 230), panelTexture);
        GUILayout.BeginArea(new Rect(hudX + 18, 22, width - 36, 215));
        GUILayout.Label("BANANA BOWLING", titleStyle);
        bool connected = Time.unscaledTime - lastPacketAt < 1f;
        if (!connected)
        {
            GUILayout.Label($"CÓDIGO  {roomCode}", bigStyle);
            GUILayout.Label(connectionState, bodyStyle);
            if (!polling && GUILayout.Button("CONECTAR JUEGO", buttonStyle, GUILayout.Height(52))) Connect();
        }
        else
        {
            GUILayout.Label($"FRAME {Mathf.Min(bowlingFrame + 1, 10)}     BOLA {bowlingBall}", bigStyle);
            string shotLabel = lastLaunchDirection < -.18f ? "DIAGONAL IZQUIERDA" : lastLaunchDirection > .18f ? "DIAGONAL DERECHA" : "TIRO RECTO";
            string message = bowlingGameOver ? $"PARTIDA TERMINADA · TOTAL {GetDisplayedBowlingScore()}" : state == GameState.Aim
                ? (swingPending ? "LEYENDO LA TRAYECTORIA COMPLETA…" : "Coloca la mano · Swing fuerte · Traza la diagonal")
                : state == GameState.Rolling ? $"¡BOLA EN JUEGO!  {shotLabel}" : $"DERRIBASTE {lastPins} PINOS";
            GUILayout.Label(message, bodyStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("CALIBRAR CENTRO", buttonStyle, GUILayout.Height(48))) Calibrate();
            if (GUILayout.Button("NUEVA PARTIDA", buttonStyle, GUILayout.Height(48))) StartBowlingGame();
            if (GUILayout.Button("MENÚ", buttonStyle, GUILayout.Height(48))) SceneNavigator.Load(SceneNavigator.MainMenu);
            GUILayout.EndHorizontal();
        }
        GUILayout.EndArea();
        DrawBowlingDashboard();
        DrawCameraStatus();
        DrawLeafFrame();
    }

    private void DrawBoxing()
    {
        float w = Screen.width, h = Screen.height;
        GUIStyle fightTitle = new GUIStyle(bigStyle) { fontSize = Mathf.RoundToInt(h * .038f) };
        GUIStyle fightText = new GUIStyle(bodyStyle) { fontSize = Mathf.RoundToInt(h * .025f), fontStyle = FontStyle.Bold };
        GUI.DrawTexture(new Rect(w * .16f, h * .025f, w * .68f, h * .15f), panelTexture);
        GUI.Label(new Rect(w * .35f, h * .035f, w * .30f, h * .055f), "BOXEO CON GORILA", fightTitle);
        DrawHealthBar(new Rect(w * .19f, h * .105f, w * .26f, h * .035f), playerHealth, new Color(.1f, .75f, 1f), "TÚ");
        DrawHealthBar(new Rect(w * .55f, h * .105f, w * .26f, h * .035f), gorillaHealth, new Color(1f, .25f, .1f), "GORILA");
        GUI.Label(new Rect(w * .46f, h * .095f, w * .08f, h * .05f), Mathf.CeilToInt(boxingRoundTime).ToString(), fightTitle);

        if (state == GameState.BoxingKnockdown)
        {
            GUI.DrawTexture(new Rect(w * .25f, h * .28f, w * .50f, h * .32f), panelTexture);
            GUI.Label(new Rect(w * .28f, h * .31f, w * .44f, h * .10f), $"¡LEVÁNTATE!  {Mathf.Max(0, Mathf.CeilToInt(resultAt - Time.time))}", fightTitle);
            GUI.Label(new Rect(w * .29f, h * .41f, w * .42f, h * .07f), "AGITA EL CELULAR REPETIDAMENTE", fightText);
            GUI.DrawTexture(new Rect(w * .31f, h * .51f, w * .38f, h * .035f), darkTexture);
            GUI.DrawTexture(new Rect(w * .31f, h * .51f, w * .38f * getUpEnergy / 100f, h * .035f), accentTexture);
        }
        else if (state == GameState.BoxingOver)
        {
            GUI.DrawTexture(new Rect(w * .26f, h * .30f, w * .48f, h * .25f), panelTexture);
            string result = gorillaHealth <= 0f ? "¡GANASTE!" : playerHealth <= 0f ? "K.O. DEL GORILA" : playerHealth >= gorillaHealth ? "¡GANASTE POR PUNTOS!" : "EL GORILA GANA";
            GUI.Label(new Rect(w * .29f, h * .34f, w * .42f, h * .09f), result, fightTitle);
            if (GUI.Button(new Rect(w * .38f, h * .45f, w * .24f, h * .065f), "REVANCHA", buttonStyle)) StartBoxingGame();
        }
        else
        {
            string action = boxingGorillaAction == 1 ? "¡ESQUIVA O CÚBRETE!" : boxingGorillaAction == 2 ? "EL GORILA SE CUBRE" : boxingGorillaAction == 3 ? "¡GOLPE TERRITORIAL!" : "MANOS ARRIBA";
            if (Time.time < boxingMessageUntil) action = boxingMessageCode == 1 ? "¡BUEN GOLPE!" : boxingMessageCode == 2 ? "¡TE GOLPEÓ!" : boxingMessageCode == 3 ? "BLOQUEO" : boxingMessageCode == 4 ? "¡ESQUIVA!" : "GUARDIA DEL GORILA";
            GUI.Label(new Rect(w * .30f, h * .18f, w * .40f, h * .065f), action, fightText);
        }

        DrawBoxingGlove(boxingLeftHand, new Color(.12f, .55f, 1f), "L");
        DrawBoxingGlove(boxingRightHand, new Color(1f, .25f, .12f), "R");
        GUI.Label(new Rect(w * .19f, h * .83f, w * .62f, h * .065f), "AMBAS MANOS ARRIBA = CUBRIRSE  ·  MUEVE EL CUERPO = ESQUIVAR  ·  CELULAR = PUÑO DERECHO", fightText);
        if (GUI.Button(new Rect(w * .84f, h * .04f, w * .13f, h * .065f), "MENÚ", buttonStyle))
        {
            if (gorillaMascot != null) gorillaMascot.transform.position = gorillaRestPosition;
            SceneNavigator.Load(SceneNavigator.MainMenu);
        }
    }

    private void DrawHealthBar(Rect rect, float health, Color color, string label)
    {
        GUI.DrawTexture(rect, darkTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(health / 100f), rect.height), color.b > color.r ? boxingBlueTexture : boxingRedTexture);
        GUI.Label(new Rect(rect.x, rect.y - rect.height * 1.1f, rect.width, rect.height), label + "  " + Mathf.CeilToInt(health), bodyStyle);
    }

    private void DrawBoxingGlove(Vector2 hand, Color color, string label)
    {
        float w = Screen.width, h = Screen.height;
        float x = Mathf.Lerp(w * .13f, w * .87f, hand.x), y = Mathf.Lerp(h * .24f, h * .78f, hand.y);
        Rect wrist = new Rect(x - w * .018f, y + h * .022f, w * .036f, h * .075f);
        Rect glove = new Rect(x - w * .035f, y - h * .025f, w * .070f, h * .070f);
        Texture2D texture = color.b > color.r ? boxingBlueTexture : boxingRedTexture;
        GUI.DrawTexture(wrist, texture); GUI.DrawTexture(glove, texture);
        GUI.Label(glove, label, bigStyle);
    }

    private void DrawMenuChoice(int index, string label, float x, float y, float width, float height, float angle)
    {
        Matrix4x4 previous = GUI.matrix;
        Rect card = new Rect(x, y, width, height);
        GUIUtility.RotateAroundPivot(angle, card.center);
        bool selected = menuSelection == index;
        if (selected) GUI.DrawTexture(new Rect(x - 7, y - 7, width + 14, height + 14), accentTexture);
        GUI.DrawTexture(card, selected ? selectedTexture : optionTexture);
        GUI.DrawTexture(new Rect(x, y, width * .08f, height), selected ? accentTexture : panelTexture);
        GUIStyle style = new GUIStyle(bigStyle) { fontSize = Mathf.RoundToInt(height * (selected ? .28f : .25f)) };
        style.normal.textColor = menuSelection == index ? new Color(.08f, .08f, .03f) : Color.white;
        GUI.Label(card, label, style);
        GUI.matrix = previous;
    }

    private int GetDisplayedBowlingScore()
    {
        int[] scores = CalculateBowlingScores();
        for (int i = scores.Length - 1; i >= 0; i--) if (scores[i] >= 0) return scores[i];
        int pinsHit = 0; foreach (List<int> frameRolls in bowlingFrames) foreach (int roll in frameRolls) pinsHit += roll;
        return pinsHit;
    }

    private void DrawGorillaReaction()
    {
        if (Time.time >= gorillaReactionUntil || gorillaReaction == 0) return;
        float w = Screen.width, h = Screen.height;
        GUIStyle reaction = new GUIStyle(bigStyle) { fontSize = Mathf.RoundToInt(h * .029f), wordWrap = true };
        reaction.normal.textColor = gorillaReaction == 1 ? new Color(1f, .28f, .12f) : new Color(1f, .84f, .16f);
        GUI.DrawTexture(new Rect(w * .025f, h * .67f, w * .24f, h * .13f), panelTexture);
        GUI.Label(new Rect(w * .035f, h * .68f, w * .22f, h * .11f), gorillaReaction == 1 ? "¡GRRR!\n¡UNA CHUZA!" : "¡JA, JA!\n¡TE FALTARON PINOS!", reaction);
    }

    private string BowlingRollText(int frame)
    {
        List<int> rolls = bowlingFrames[frame];
        if (rolls.Count == 0) return "·  ·";
        if (frame < 9)
        {
            if (rolls[0] == 10) return "   X";
            string first = rolls[0] == 0 ? "-" : rolls[0].ToString();
            if (rolls.Count == 1) return first + "  ·";
            string second = rolls[0] + rolls[1] == 10 ? "/" : rolls[1] == 0 ? "-" : rolls[1].ToString();
            return first + "  " + second;
        }
        string text = "";
        for (int i = 0; i < rolls.Count; i++)
        {
            string symbol;
            if (rolls[i] == 10) symbol = "X";
            else if (i > 0 && rolls[i - 1] != 10 && rolls[i - 1] + rolls[i] == 10) symbol = "/";
            else symbol = rolls[i] == 0 ? "-" : rolls[i].ToString();
            text += (i == 0 ? "" : " ") + symbol;
        }
        while (text.Length < 5) text += " ·";
        return text;
    }

    private void DrawBowlingDashboard()
    {
        float w = Screen.width, h = Screen.height;
        float x = w * .72f, y = h * .145f, width = w * .255f, height = h * .70f;
        GUI.DrawTexture(new Rect(x, y, width, height), panelTexture);
        GUI.DrawTexture(new Rect(x, y, width, h * .012f), selectedTexture);
        dashHeadingStyle ??= new GUIStyle(titleStyle) { normal = { textColor = new Color(1f, .84f, .18f) } };
        dashRowStyle ??= new GUIStyle(bodyStyle) { alignment = TextAnchor.MiddleLeft };
        dashRowActiveStyle ??= new GUIStyle(bodyStyle) { alignment = TextAnchor.MiddleLeft, normal = { textColor = new Color(.08f, .08f, .03f) } };
        dashScoreStyle ??= new GUIStyle(bigStyle) { alignment = TextAnchor.MiddleRight };
        dashScoreActiveStyle ??= new GUIStyle(bigStyle) { alignment = TextAnchor.MiddleRight, normal = { textColor = new Color(.08f, .08f, .03f) } };
        dashHeadingStyle.fontSize = Mathf.RoundToInt(h * .031f);
        dashRowStyle.fontSize = dashRowActiveStyle.fontSize = Mathf.RoundToInt(h * .021f);
        dashScoreStyle.fontSize = dashScoreActiveStyle.fontSize = Mathf.RoundToInt(h * .027f);
        GUI.Label(new Rect(x + width * .08f, y + h * .018f, width * .84f, h * .055f), "TABLERO DE LA SELVA", dashHeadingStyle);
        GUI.Label(new Rect(x + width * .08f, y + h * .073f, width * .84f, h * .045f), $"ÚLTIMO TIRO: {lastPins} PINOS", dashRowStyle);
        int[] scores = CalculateBowlingScores();
        float rowsY = y + h * .125f, rowH = h * .047f;
        for (int i = 0; i < 10; i++)
        {
            bool active = i == Mathf.Min(bowlingFrame, 9) && !bowlingGameOver;
            Rect row = new Rect(x + width * .07f, rowsY + i * rowH, width * .86f, rowH - 3);
            GUI.DrawTexture(row, active ? selectedTexture : optionTexture);
            GUIStyle frameStyle = active ? dashRowActiveStyle : dashRowStyle;
            GUIStyle frameScore = active ? dashScoreActiveStyle : dashScoreStyle;
            GUI.Label(new Rect(row.x + 10, row.y, row.width * .18f, row.height), (i + 1).ToString("00"), frameStyle);
            GUI.Label(new Rect(row.x + row.width * .21f, row.y, row.width * .39f, row.height), BowlingRollText(i), frameStyle);
            GUI.Label(new Rect(row.x + row.width * .62f, row.y, row.width * .32f, row.height), scores[i] >= 0 ? scores[i].ToString() : "", frameScore);
        }
        GUI.Label(new Rect(x + width * .09f, y + height - h * .085f, width * .82f, h * .065f), $"TOTAL  {GetDisplayedBowlingScore()}", dashHeadingStyle);
        if (leavesRight != null) GUI.DrawTexture(new Rect(x + width * .70f, y - h * .025f, width * .34f, h * .13f), leavesRight, ScaleMode.ScaleToFit, true);
        if (leavesBottom != null) GUI.DrawTexture(new Rect(x - width * .05f, y + height - h * .09f, width * .52f, h * .12f), leavesBottom, ScaleMode.ScaleToFit, true);
    }

    private void DrawMiniGame()
    {
        float w = Screen.width, h = Screen.height;
        GUI.DrawTexture(new Rect(w * .19f, 18, w * .62f, h * .16f), panelTexture);
        GUIStyle miniTitle = new GUIStyle(titleStyle) { fontSize = Mathf.RoundToInt(h * .042f) };
        GUIStyle scoreStyle = new GUIStyle(bigStyle) { fontSize = Mathf.RoundToInt(h * .038f) };
        if (state == GameState.MiniResults)
        {
            GUI.DrawTexture(new Rect(w * .22f, h * .32f, w * .56f, h * .34f), panelTexture);
            GUI.Label(new Rect(w * .24f, h * .36f, w * .52f, h * .10f), karateLost ? "¡AY! GOLPEASTE UN CACTUS" : "¡DESAFÍO TERMINADO!", miniTitle);
            GUI.Label(new Rect(w * .24f, h * .47f, w * .52f, h * .10f), $"PUNTOS  {miniScore}", scoreStyle);
            GUI.Label(new Rect(w * .24f, h * .56f, w * .52f, h * .07f), "VOLVIENDO AL MENÚ…", bodyStyle);
            return;
        }
        string title = state == GameState.TargetGame ? "GOLPEA TÓTEMS" : state == GameState.BalanceGame ? "EQUILIBRIO SELVA" : "KARATE COCO";
        GUI.Label(new Rect(w * .21f, 24, w * .58f, h * .07f), title, miniTitle);
        GUI.Label(new Rect(w * .21f, h * .085f, w * .58f, h * .07f), $"PUNTOS {miniScore}     TIEMPO {Mathf.CeilToInt(miniTime)}", scoreStyle);
        if (state == GameState.TargetGame)
        {
            float tx = targetPoint.x * w, ty = targetPoint.y * h;
            float size = Mathf.Min(w, h) * .15f;
            GUI.DrawTexture(new Rect(tx - size * .58f, ty - size * .58f, size * 1.16f, size * 1.16f), selectedTexture);
            GUI.DrawTexture(new Rect(tx - size * .38f, ty - size * .38f, size * .76f, size * .76f), optionTexture);
            GUI.DrawTexture(new Rect(tx - size * .16f, ty - size * .16f, size * .32f, size * .32f), accentTexture);
            GUI.Label(new Rect(w * .26f, h * .82f, w * .48f, h * .08f), "APUNTA AL TÓTEM Y HAZ UN SWING", bodyStyle);
        }
        else if (state == GameState.BalanceGame)
        {
            float bx = balanceTarget.x * w, by = balanceTarget.y * h;
            float size = Mathf.Min(w, h) * .22f;
            GUI.DrawTexture(new Rect(bx - size / 2, by - size / 2, size, size), accentTexture);
            GUI.DrawTexture(new Rect(bx - size * .38f, by - size * .38f, size * .76f, size * .76f), selectedTexture);
            GUI.Label(new Rect(w * .22f, h * .82f, w * .56f, h * .08f), "MANTÉN LA MIRA DENTRO DE LA ZONA", bodyStyle);
        }
        else DrawKarateRow(w, h);
        if (state != GameState.KarateGame) DrawPointer(gameCursor.x * w, gameCursor.y * h);
        if (GUI.Button(new Rect(w * .82f, h * .04f, w * .14f, h * .07f), "MENÚ", buttonStyle)) state = GameState.Menu;
    }

    private void DrawKarateRow(float w, float h)
    {
        float laneY = h * .58f;
        GUI.DrawTexture(new Rect(0, laneY - h * .09f, w, h * .19f), panelTexture);
        GUI.DrawTexture(new Rect(w * .325f, laneY - h * .14f, w * .03f, h * .28f), Time.time < karateFlashUntil ? selectedTexture : accentTexture);
        GUIStyle zone = new GUIStyle(bodyStyle) { fontSize = Mathf.RoundToInt(h * .022f), normal = { textColor = new Color(1f, .85f, .2f) } };
        GUI.Label(new Rect(w * .25f, laneY + h * .105f, w * .18f, h * .06f), "ZONA DE GOLPE", zone);
        float size = Mathf.Min(w, h) * .15f;
        foreach (KarateItem item in karateItems)
        {
            float x = item.x * w;
            if (item.cactus) DrawCactus(x, laneY, size);
            else DrawCoconut(x, laneY, size);
        }
        GUI.Label(new Rect(w * .19f, h * .80f, w * .62f, h * .09f), "GOLPEA HACIA ABAJO SOLO CUANDO PASE UN COCO", bodyStyle);
    }

    private void DrawCoconut(float x, float y, float size)
    {
        GUI.DrawTexture(new Rect(x - size * .48f, y - size * .48f, size * .96f, size * .96f), coconutTexture);
        float eye = size * .12f;
        GUI.DrawTexture(new Rect(x - size * .22f, y - size * .13f, eye, eye), darkTexture);
        GUI.DrawTexture(new Rect(x + size * .10f, y - size * .13f, eye, eye), darkTexture);
        GUI.DrawTexture(new Rect(x - eye * .5f, y + size * .13f, eye, eye), darkTexture);
    }

    private void DrawCactus(float x, float y, float size)
    {
        GUI.DrawTexture(new Rect(x - size * .18f, y - size * .52f, size * .36f, size * 1.04f), cactusTexture);
        GUI.DrawTexture(new Rect(x - size * .42f, y - size * .12f, size * .30f, size * .20f), cactusTexture);
        GUI.DrawTexture(new Rect(x + size * .12f, y - size * .30f, size * .30f, size * .20f), cactusTexture);
        GUI.DrawTexture(new Rect(x - size * .42f, y - size * .30f, size * .13f, size * .25f), cactusTexture);
        GUI.DrawTexture(new Rect(x + size * .29f, y - size * .46f, size * .13f, size * .27f), cactusTexture);
    }

    private void DrawPointer(float x, float y)
    {
        float pulse = 1f + Mathf.Sin(Time.unscaledTime * 7f) * .08f;
        float size = 64f * pulse;
        GUI.DrawTexture(new Rect(x - size / 2, y - size / 2, size, size), accentTexture);
        GUIStyle pointer = new GUIStyle(bigStyle) { fontSize = 40, normal = { textColor = new Color(.05f, .1f, .08f) } };
        GUI.Label(new Rect(x - size / 2, y - size / 2 - 2, size, size), "+", pointer);
    }

    private void DrawCameraStatus()
    {
        GUIStyle status = new GUIStyle(bodyStyle) { fontSize = Mathf.Max(16, Mathf.RoundToInt(Screen.height * .021f)), alignment = TextAnchor.MiddleCenter };
        status.normal.textColor = CameraActive ? new Color(.3f, 1f, .45f) : new Color(1f, .72f, .22f);
        GUI.DrawTexture(new Rect(Screen.width * .77f, Screen.height * .88f, Screen.width * .21f, Screen.height * .075f), panelTexture);
        GUI.Label(new Rect(Screen.width * .775f, Screen.height * .885f, Screen.width * .20f, Screen.height * .065f), CameraActive ? "● CÁMARA: MANO DETECTADA" : "○ CÁMARA: ESPERANDO", status);
    }

    private void DrawLeafFrame()
    {
        if (leavesLeft != null) GUI.DrawTexture(new Rect(0, 0, Screen.width * .31f, Screen.height * .2f), leavesLeft, ScaleMode.ScaleToFit, true);
        if (leavesRight != null) GUI.DrawTexture(new Rect(Screen.width * .71f, 0, Screen.width * .29f, Screen.height * .21f), leavesRight, ScaleMode.ScaleToFit, true);
        if (leavesBottom != null) GUI.DrawTexture(new Rect(0, Screen.height * .82f, Screen.width * .38f, Screen.height * .18f), leavesBottom, ScaleMode.ScaleToFit, true);
    }

    private void OnDestroy() { gorillaImport?.Dispose(); islandImport?.Dispose(); }
}
