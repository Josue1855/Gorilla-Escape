using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using GorillaEscape.Gameplay.Input;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using GLTFast;

public sealed class PhoneMotionController : MonoBehaviour
{
    private enum GameState { Connecting, Menu, Aim, Rolling, Results, Boxing, BoxingKnockdown, BoxingOver }
    private readonly List<GameObject> pins = new List<GameObject>();
    private readonly Dictionary<GameObject, Vector3> pinRackPositions = new Dictionary<GameObject, Vector3>();
    private readonly Dictionary<GameObject, Quaternion> pinRackRotations = new Dictionary<GameObject, Quaternion>();
    private readonly HashSet<GameObject> pinsFallenThisRoll = new HashSet<GameObject>();
    private readonly List<Vector3> poseTrail = new List<Vector3>();
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
    private Animator bowlingGorillaAnimator;
    private AnimationClip bowlingGorillaIdleClip, bowlingGorillaAngryClip, bowlingGorillaDanceClip;
    private PlayableGraph bowlingGorillaGraph;
    private AnimationClipPlayable bowlingGorillaPlayable;
    private float bowlingGorillaClipStartedAt, bowlingGorillaClipDuration;
    private bool bowlingGorillaUsingImportedAnimation;
    private Material totemPinSharedMaterial;
    private Material bushSharedMaterial, palmSharedMaterial, coconutModelMaterial;
    private GameObject newVegetationRoot;
    private bool bowlingRackTransitioning;
    private string connectionState = "PULSA CONECTAR";
    private string roomCode = "";
    private GameState state = GameState.Connecting;
    private bool polling, calibrated, hasRaw;
    private float lastPacketAt = -10f, centerGamma, centerBeta, latestGamma, latestBeta, aimX, power, resultAt;
    private float ballPassedPinsAt = -1f;
    private float lastPunchAt, aimReadyAt;
    private float poseWristX = .5f, poseBodyX = .5f;
    private Vector2 boxingLeftHand = new Vector2(.36f, .63f), boxingRightHand = new Vector2(.64f, .63f);
    private float boxingLeftSpeed, boxingRightSpeed, playerHealth, gorillaHealth, boxingRoundTime, gorillaActionAt, gorillaAttackResolveAt, boxingMessageUntil, getUpEnergy;
    private int boxingGorillaAction, boxingMessageCode;
    private bool boxingAttackResolved;
    private readonly Vector3 bowlingCameraPosition = new Vector3(0, 6.4f, -12.5f);
    private float swingStartGamma, lastLaunchDirection;
    private int totalScore, lastPins, menuSelection, lastConfirm, lastRecenter;
    private int bowlingFrame, bowlingBall = 1;
    private bool confirmInitialized;
    private bool poseDetected;
    private bool bowlingGameOver, nextNeedsFullRack;
    private bool editableScene;
    private Transform gorillaAnchor;
    private Transform islandAnchor;
    private Vector2 menuCursor = new Vector2(.32f, .56f);
    private Quaternion rawRotation = Quaternion.identity;
    private GUIStyle titleStyle, bigStyle, bodyStyle, buttonStyle;
    private GUIStyle dashHeadingStyle, dashRowStyle, dashRowActiveStyle, dashScoreStyle, dashScoreActiveStyle;
    private Texture2D panelTexture, accentTexture, selectedTexture, optionTexture, darkTexture, boxingBlueTexture, boxingRedTexture, gorillaTexture, leavesBottom, leavesLeft, leavesRight;
    private Texture2D bowlingHudTexture, bowlingCardTexture, bowlingButtonTexture, bowlingButtonHoverTexture;
    private Font hudFont;
    private MotionSession sharedSession;
    private JungleFusedFrame inputFrame;

    private void Awake()
    {
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
        if (SceneManager.GetActiveScene().name == SceneNavigator.Bowling)
        {
            ApplyBowlingSurfaceTextures();
            CreateNewBowlingVegetation();
            ApplyCoconutModel();
        }
        if (SceneManager.GetActiveScene().name == SceneNavigator.Boxing) StartBoxingGame();
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
        GameObject gorillaStage = GameObject.Find("Plataforma del gorila");
        if (gorillaStage != null) gorillaStage.SetActive(false);
        // El jardín lateral está a Y=-.46; hundir apenas los pies evita que la
        // sombra separada haga parecer que el gorila flota.
        if (gorillaAnchor != null) gorillaAnchor.position = new Vector3(-6.25f, -.48f, 4.2f);
        islandAnchor = GameObject.Find("Punto del fondo isla")?.transform;
        pins.Clear(); pinRackPositions.Clear(); pinRackRotations.Clear();
        GameObject rack = GameObject.Find("Pinos editables");
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
        bool boxingScene = SceneManager.GetActiveScene().name == SceneNavigator.Boxing;
        if (boxingScene)
        {
            string path = Path.Combine(Application.streamingAssetsPath, "GorillaLowPoly.glb");
            if (!File.Exists(path)) { Debug.LogWarning("No se encontró GorillaLowPoly.glb"); return; }
            gorillaMascot = new GameObject("Gorila rival de boxeo");
            gorillaImport = new GltfImport();
            bool loaded = await gorillaImport.Load(new Uri(path).AbsoluteUri);
            if (!loaded || gorillaMascot == null) { Debug.LogWarning("No se pudo cargar el gorila GLB"); return; }
            bool instantiated = await gorillaImport.InstantiateMainSceneAsync(gorillaMascot.transform);
            if (!instantiated || gorillaMascot == null) return;
        }
        else
        {
            const string resourcePath = "Jungle/GorillaBowling/Meshy_AI_Low_Poly_Gorilla_Embr_biped/Meshy_AI_Low_Poly_Gorilla_Embr_biped_Meshy_Merged_Animations";
            GameObject prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null) { Debug.LogWarning("No se encontró el gorila bípedo de Bowling en Resources"); return; }
            gorillaMascot = Instantiate(prefab);
            gorillaMascot.name = "Gorila bípedo espectador de Bowling";
            foreach (Animation animation in gorillaMascot.GetComponentsInChildren<Animation>(true)) animation.enabled = false;
            ApplyBowlingGorillaMaterials();
        }

        Vector3 intendedPosition = boxingScene ? new Vector3(0f, -.30f, -2.75f) : gorillaAnchor != null ? gorillaAnchor.position : new Vector3(-6.25f, -.48f, 4.2f);
        if (!boxingScene)
        {
            intendedPosition.x = Mathf.Max(intendedPosition.x, -4.35f);
            intendedPosition.z -= 2.35f;
        }
        Vector3 intendedFacing = Camera.main != null ? Camera.main.transform.position - intendedPosition : Vector3.back;
        intendedFacing.y = 0f;
        gorillaMascot.transform.rotation = intendedFacing.sqrMagnitude > .01f ? Quaternion.LookRotation(intendedFacing.normalized) : Quaternion.identity;
        if (!boxingScene && gorillaAnchor != null) gorillaMascot.transform.rotation *= gorillaAnchor.rotation;
        Renderer[] renderers = gorillaMascot.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            float scale = (boxingScene ? 5.6f : 5.75f) / Mathf.Max(.01f, bounds.size.y);
            gorillaMascot.transform.localScale = Vector3.one * scale;
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            gorillaMascot.transform.position += new Vector3(intendedPosition.x - bounds.center.x, intendedPosition.y - bounds.min.y, intendedPosition.z - bounds.center.z);
        }
        foreach (Collider item in gorillaMascot.GetComponentsInChildren<Collider>(true)) item.enabled = false;

        gorillaHead = FindGorillaBone("Head", "Head_M_040");
        gorillaNeck = FindGorillaBone("Neck", "Neck_M_039");
        gorillaSpine = FindGorillaBone("Spine2", "Spine1", "Spine2_M_037");
        gorillaHipLeft = FindGorillaBone("LeftUpLeg", "Hip_L_08");
        gorillaHipRight = FindGorillaBone("RightUpLeg", "Hip_R_022");
        gorillaKneeLeft = FindGorillaBone("LeftLeg", "Knee_L_09");
        gorillaKneeRight = FindGorillaBone("RightLeg", "Knee_R_023");
        gorillaShoulderLeft = FindGorillaBone("LeftArm", "LeftShoulder", "Shoulder_L_043");
        gorillaShoulderRight = FindGorillaBone("RightArm", "RightShoulder", "Shoulder_R_062");
        gorillaElbowLeft = FindGorillaBone("LeftForeArm", "Elbow_L_00");
        gorillaElbowRight = FindGorillaBone("RightForeArm", "Elbow_R_063");
        gorillaWristLeft = FindGorillaBone("LeftHand", "Wrist_L_044");
        gorillaWristRight = FindGorillaBone("RightHand", "Wrist_R_064");
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
        if (!boxingScene) SetupBowlingGorillaAnimations();
    }

    private void ApplyBowlingGorillaMaterials()
    {
        const string textureRoot = "Jungle/GorillaBowling/Meshy_AI_Low_Poly_Gorilla_Embr_biped/";
        Texture2D albedo = Resources.Load<Texture2D>(textureRoot + "Meshy_AI_Low_Poly_Gorilla_Embr_biped_texture_0");
        Texture2D normal = Resources.Load<Texture2D>(textureRoot + "Meshy_AI_Low_Poly_Gorilla_Embr_biped_texture_0_normal");
        if (albedo == null)
        {
            Debug.LogWarning("No se encontró la textura de color del gorila bípedo de Bowling");
            return;
        }

        Material material = MakeMaterial(Color.white, 0f, .22f);
        material.name = "Gorila Bowling - Color Meshy";
        material.mainTexture = albedo;
        material.SetTexture("_MainTex", albedo);
        if (normal != null)
        {
            material.SetTexture("_BumpMap", normal);
            material.SetFloat("_BumpScale", .72f);
            material.EnableKeyword("_NORMALMAP");
        }

        foreach (Renderer renderer in gorillaMascot.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++) materials[i] = material;
            renderer.sharedMaterials = materials;
        }
    }

    private void SetupBowlingGorillaAnimations()
    {
        const string resourcePath = "Jungle/GorillaBowling/Meshy_AI_Low_Poly_Gorilla_Embr_biped/Meshy_AI_Low_Poly_Gorilla_Embr_biped_Meshy_Merged_Animations";
        bowlingGorillaAnimator = gorillaMascot.GetComponentInChildren<Animator>(true);
        if (bowlingGorillaAnimator == null) bowlingGorillaAnimator = gorillaMascot.AddComponent<Animator>();
        foreach (AnimationClip clip in Resources.LoadAll<AnimationClip>(resourcePath))
        {
            if (clip.name == "Alert") bowlingGorillaIdleClip = clip;
            else if (clip.name == "Angry_Ground_Stomp_2") bowlingGorillaAngryClip = clip;
            else if (clip.name == "Angry_Ground_Stomp" && bowlingGorillaAngryClip == null) bowlingGorillaAngryClip = clip;
            else if (clip.name == "All_Night_Dance") bowlingGorillaDanceClip = clip;
        }

        bowlingGorillaUsingImportedAnimation = bowlingGorillaAnimator != null && bowlingGorillaAngryClip != null && bowlingGorillaDanceClip != null;
        if (!bowlingGorillaUsingImportedAnimation)
        {
            Debug.LogWarning("El gorila nuevo no contiene todos los clips esperados; se usarán las reacciones procedurales.");
            return;
        }
        PlayBowlingGorillaClip(bowlingGorillaIdleClip != null ? bowlingGorillaIdleClip : bowlingGorillaDanceClip, 1.15f, true);
    }

    private void PlayBowlingGorillaClip(AnimationClip clip, float speed, bool loop)
    {
        if (clip == null || bowlingGorillaAnimator == null) return;
        if (bowlingGorillaGraph.IsValid()) bowlingGorillaGraph.Destroy();
        bowlingGorillaGraph = PlayableGraph.Create("Animaciones del gorila de Bowling");
        bowlingGorillaGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        AnimationPlayableOutput output = AnimationPlayableOutput.Create(bowlingGorillaGraph, "Gorila", bowlingGorillaAnimator);
        bowlingGorillaPlayable = AnimationClipPlayable.Create(bowlingGorillaGraph, clip);
        bowlingGorillaPlayable.SetApplyFootIK(true);
        bowlingGorillaPlayable.SetSpeed(speed);
        output.SetSourcePlayable(bowlingGorillaPlayable);
        bowlingGorillaGraph.Play();
        bowlingGorillaClipStartedAt = Time.time;
        bowlingGorillaClipDuration = loop ? -clip.length / Mathf.Max(.01f, speed) : clip.length / Mathf.Max(.01f, speed);
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

    private Transform FindGorillaBone(params string[] boneNames)
    {
        if (gorillaMascot == null) return null;
        Transform[] bones = gorillaMascot.GetComponentsInChildren<Transform>(true);
        foreach (string boneName in boneNames)
            foreach (Transform item in bones)
                if (item.name == boneName || item.name.EndsWith(":" + boneName, StringComparison.OrdinalIgnoreCase)) return item;
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
        if (bowlingGorillaUsingImportedAnimation)
        {
            AnimationClip reactionClip = strike ? bowlingGorillaAngryClip : bowlingGorillaDanceClip;
            float speed = strike ? 1.35f : 2.10f;
            PlayBowlingGorillaClip(reactionClip, speed, false);
            gorillaReactionUntil = Time.time + reactionClip.length / speed;
        }
        else gorillaReactionUntil = Time.time + 2.6f;
    }

    private void AnimateGorilla()
    {
        if (gorillaMascot == null) return;
        if (state == GameState.Boxing || state == GameState.BoxingKnockdown || state == GameState.BoxingOver) { AnimateBoxingGorilla(); return; }
        if (bowlingGorillaUsingImportedAnimation)
        {
            gorillaMascot.transform.position = gorillaRestPosition;
            gorillaMascot.transform.rotation = gorillaRestRotation;
            float duration = Mathf.Abs(bowlingGorillaClipDuration);
            if (duration > 0f && Time.time - bowlingGorillaClipStartedAt >= duration)
            {
                if (bowlingGorillaClipDuration < 0f && bowlingGorillaPlayable.IsValid())
                {
                    bowlingGorillaPlayable.SetTime(0d);
                    bowlingGorillaClipStartedAt = Time.time;
                }
                else PlayBowlingGorillaClip(bowlingGorillaIdleClip != null ? bowlingGorillaIdleClip : bowlingGorillaDanceClip, 1.15f, true);
            }
            return;
        }
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
            // Felicidad bípeda: anticipación con rodillas, balanceo y brazos celebrando.
            float joy = SmoothPulse(u, .04f, .16f, .70f, .90f);
            float bounce = (Mathf.Sin(Mathf.Clamp01((u - .10f) / .68f) * Mathf.PI * 4f) * .5f + .5f) * joy;
            float sway = Mathf.Sin(Mathf.Clamp01((u - .12f) / .65f) * Mathf.PI * 3f) * joy;
            float clap = gorillaVariant == 0 ? Mathf.Max(0f, Mathf.Sin(u * Mathf.PI * 8f)) * joy : 0f;
            float raised = gorillaVariant == 1 ? joy : joy * .45f;
            float proud = gorillaVariant == 2 ? joy : 0f;
            gorillaMascot.transform.position = gorillaRestPosition;
            gorillaMascot.transform.rotation = gorillaRestRotation * Quaternion.Euler(0, sway * 4f, 0);
            SetBone(gorillaSpine, gorillaSpineRest, new Vector3(0, sway * 5f, -5f * bounce + 7f * proud), envelope);
            SetBone(gorillaNeck, gorillaNeckRest, new Vector3(0, -sway * 4f, -4f * proud), envelope);
            SetBone(gorillaHead, gorillaHeadRest, new Vector3(0, -sway * 6f, -7f * bounce - 5f * proud), envelope);
            SetBone(gorillaHipLeft, gorillaHipLeftRest, new Vector3(0, 0, -5f * bounce), envelope);
            SetBone(gorillaHipRight, gorillaHipRightRest, new Vector3(0, 0, 5f * bounce), envelope);
            SetBone(gorillaKneeLeft, gorillaKneeLeftRest, new Vector3(0, 0, 13f * bounce), envelope);
            SetBone(gorillaKneeRight, gorillaKneeRightRest, new Vector3(0, 0, -13f * bounce), envelope);
            SetBone(gorillaShoulderLeft, gorillaLeftRest, new Vector3(0, -34f * raised - 18f * clap, 30f * raised + 22f * clap), envelope);
            SetBone(gorillaShoulderRight, gorillaRightRest, new Vector3(0, 34f * raised + 18f * clap, -30f * raised - 22f * clap), envelope);
            SetBone(gorillaElbowLeft, gorillaElbowLeftRest, new Vector3(0, 0, 42f * clap + 18f * raised), envelope);
            SetBone(gorillaElbowRight, gorillaElbowRightRest, new Vector3(0, 0, -42f * clap - 18f * raised), envelope);
            SetBone(gorillaWristLeft, gorillaWristLeftRest, new Vector3(0, sway * 9f, 0), envelope);
            SetBone(gorillaWristRight, gorillaWristRightRest, new Vector3(0, -sway * 9f, 0), envelope);
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
                GameObject pin = new GameObject("Pino");
                pin.name = "Pino";
                pin.transform.position = new Vector3((col - row * .5f) * .78f, .65f, startZ + row * .72f);
                CapsuleCollider pinCollider = pin.AddComponent<CapsuleCollider>();
                pinCollider.direction = 1; pinCollider.center = new Vector3(0f, .20f, 0f); pinCollider.height = 1.70f; pinCollider.radius = .26f;
                ApplyTotemPinVisual(pin);
                Rigidbody body = pin.AddComponent<Rigidbody>(); body.mass = 1.5f; body.linearDamping = .10f; body.angularDamping = .07f;
                body.centerOfMass = new Vector3(0f, -.16f, 0f); body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
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
        GameObject prefab = Resources.Load<GameObject>(modelPath);
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
            Texture2D albedo = Resources.Load<Texture2D>(assetRoot + "Meshy_AI_Banana_Guardian_20261024_albedo");
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
        GameObject bushPrefab = Resources.Load<GameObject>(bushRoot + "Meshy_AI_Emerald_Canopy_20261026_texture");
        GameObject palmPrefab = Resources.Load<GameObject>(palmRoot + "Meshy_AI_Cartoon_Coconut_Palm_20261026_texture");
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
            // Los arbustos cubren la unión entre el piso ilustrado y las paredes 3D.
            // Se mantienen lejos del gorila y completamente fuera de la pista.
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

    private Material CreateSharedPropMaterial(string albedoPath, string materialName)
    {
        Material material = MakeMaterial(Color.white, 0f, .18f);
        material.name = materialName;
        material.enableInstancing = true;
        Texture2D albedo = Resources.Load<Texture2D>(albedoPath);
        if (albedo != null) { material.mainTexture = albedo; material.SetTexture("_MainTex", albedo); }
        else Debug.LogWarning("No se encontró la textura: " + albedoPath);
        return material;
    }

    private void ApplyBowlingSurfaceTextures()
    {
        Texture2D laneTexture = Resources.Load<Texture2D>("Jungle/SurfaceTextures/BowlingLaneBamboo");
        Texture2D sidesTexture = Resources.Load<Texture2D>("Jungle/SurfaceTextures/BowlingSidesJungle");
        if (laneTexture == null || sidesTexture == null)
        {
            Debug.LogWarning("No se encontraron las nuevas texturas selváticas de la pista.");
            return;
        }

        ConfigureSurfaceTexture(laneTexture);
        ConfigureSurfaceTexture(sidesTexture);
        Material laneMaterial = MakeMaterial(Color.white, 0f, .24f);
        laneMaterial.name = "Bambú pintado de la pista";
        laneMaterial.mainTexture = laneTexture;
        laneMaterial.mainTextureScale = new Vector2(1f, 5.5f);

        Material sidesMaterial = MakeMaterial(new Color(.88f, .94f, .88f), 0f, .12f);
        sidesMaterial.name = "Laterales selváticos pintados";
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

    private static void ApplySurfaceMaterial(string objectName, Material material)
    {
        GameObject surface = GameObject.Find(objectName);
        if (surface == null) return;
        Renderer renderer = surface.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
    }

    private void CreateEnvironmentProp(GameObject prefab, Material material, string objectName, Vector3 groundPosition, float desiredHeight, float yaw)
    {
        GameObject prop = Instantiate(prefab, newVegetationRoot.transform);
        prop.name = objectName;
        Quaternion importedAxisCorrection = prop.transform.localRotation;
        prop.transform.position = Vector3.zero;
        prop.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * importedAxisCorrection;
        prop.transform.localScale = Vector3.one;
        foreach (Collider collider in prop.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        Renderer[] renderers = prop.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        prop.transform.localScale = Vector3.one * (desiredHeight / Mathf.Max(.01f, bounds.size.y));
        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        prop.transform.position += new Vector3(groundPosition.x - bounds.center.x, groundPosition.y - bounds.min.y, groundPosition.z - bounds.center.z);
        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++) materials[i] = material;
            renderer.sharedMaterials = materials;
        }
    }

    private void ApplyCoconutModel()
    {
        if (ballVisual == null) return;
        const string assetRoot = "Jungle/NewModels/Coconut/Meshy_AI_Coconut_Bowling_Ball_20261026_texture_fbx/";
        GameObject prefab = Resources.Load<GameObject>(assetRoot + "Meshy_AI_Coconut_Bowling_Ball_20261026_texture");
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
                Renderer rootRenderer = pin.GetComponent<Renderer>();
                bool hasTotemVisual = pin.transform.Find("Visual pino tótem") != null;
                if (rootRenderer != null) rootRenderer.enabled = !hasTotemVisual;
                if (hasTotemVisual)
                    foreach (Renderer visualRenderer in pin.transform.Find("Visual pino tótem").GetComponentsInChildren<Renderer>(true))
                        visualRenderer.enabled = true;
                Collider collider = pin.GetComponent<Collider>(); if (collider != null) collider.enabled = true;
                Rigidbody body = pin.GetComponent<Rigidbody>();
                if (body != null) { StopBody(body, false); body.Sleep(); }
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
        aimX = 0; power = 0; aimReadyAt = Time.time + .8f;
        poseTrail.Clear();
        state = polling ? GameState.Aim : GameState.Connecting;
    }

    private void StartBowlingGame()
    {
        sharedSession.ResetRound();
        SetBowlingVisible(true);
        if (gorillaMascot != null) { gorillaMascot.transform.position = gorillaRestPosition; gorillaMascot.transform.rotation = gorillaRestRotation; }
        for (int i = 0; i < bowlingFrames.Length; i++) bowlingFrames[i].Clear();
        bowlingFrame = 0; bowlingBall = 1; totalScore = 0; lastPins = 0;
        bowlingGameOver = false; nextNeedsFullRack = true;
        ResetRack();
    }

    private void SetBowlingVisible(bool visible)
    {
        if (ballVisual != null) ballVisual.SetActive(visible);
        foreach (GameObject pin in pins) if (pin != null) pin.SetActive(visible);
        string[] groups = { "01 - Pista y arquitectura", "02 - Arboles editables", "03 - Zona del gorila", "04 - Fondo editable" };
        foreach (string groupName in groups) { GameObject group = GameObject.Find(groupName); if (group != null) group.SetActive(visible); }
        if (islandBackground != null) islandBackground.SetActive(visible);
        if (hybridBowlingEnvironment != null) hybridBowlingEnvironment.SetVisible(visible);
        if (newVegetationRoot != null) newVegetationRoot.SetActive(visible);
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
        sharedSession.ResetRound();
        SetBowlingVisible(false);
        playerHealth = 100f; gorillaHealth = 100f; boxingRoundTime = 90f; getUpEnergy = 0f;
        boxingGorillaAction = 0; gorillaActionAt = Time.time + 1.8f; boxingMessageCode = 0; boxingMessageUntil = 0;
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
        if (state != GameState.Boxing || Time.time < lastPunchAt + .34f) return;
        if(inputFrame==null||!sharedSession.TryAuthorize(right?JungleAction.PunchRight:JungleAction.PunchLeft,inputFrame.Revision))return;
        lastPunchAt = Time.time;
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
        if(!sharedSession.IsInputReady)
        {
            poseDetected=hasRaw=calibrated=false;power=boxingLeftSpeed=boxingRightSpeed=0;poseTrail.Clear();
            return;
        }
        if(sharedSession.TryConsumeFrame(out var frame))
        {
            inputFrame=frame;
            if(frame.Baseline){poseTrail.Clear();calibrated=confirmInitialized=false;}
            power=frame.PhonePower;
            ReadPose(frame);
            ReadPhone(frame);
        }
        AnimateGorilla();
        if (state == GameState.Aim)
        {
            transform.position = Vector3.Lerp(transform.position, new Vector3(aimX, .48f, -7.2f), 12f * Time.deltaTime);
            transform.rotation = Quaternion.identity;
        }
        else if (state == GameState.Rolling)
        {
            if (transform.position.z > 11.5f && ballPassedPinsAt < 0f) ballPassedPinsAt = Time.time;
            bool settledAfterPassing = ballPassedPinsAt > 0f && Time.time >= ballPassedPinsAt + 1.65f;
            if (Time.time >= resultAt || settledAfterPassing) FinishRoll();
        }
        else if (state == GameState.Results && Time.time >= resultAt && !bowlingGameOver && !bowlingRackTransitioning)
            StartCoroutine(AnimateBowlingRackTransition());
        else if (state == GameState.Boxing) UpdateBoxing();
        else if (state == GameState.BoxingKnockdown)
        {
            if (getUpEnergy >= 100f) { playerHealth = 45f; boxingGorillaAction = 0; gorillaActionAt = Time.time + 1.5f; state = GameState.Boxing; }
            else if (Time.time >= resultAt) state = GameState.BoxingOver;
        }

    }

    private bool CameraActive => sharedSession.IsInputReady && poseDetected;

    private void ReadPose(JungleFusedFrame frame)
    {
        var pose=frame.Pose;
        bool needsBothHands = sharedSession.HandMode == MotionSession.HandTrackingMode.Both;
        poseDetected = needsBothHands
            ? pose.leftDetected || pose.rightDetected
            : sharedSession.HandMode == MotionSession.HandTrackingMode.LeftOnly ? pose.leftDetected : pose.rightDetected || pose.detected;
        if (!poseDetected) return;
        float now = Time.unscaledTime;

        if (pose.leftDetected || pose.rightDetected)
        {
            Vector2 newLeft = new Vector2(pose.leftWristX, pose.leftWristY);
            Vector2 newRight = new Vector2(pose.rightWristX, pose.rightWristY);
            boxingLeftSpeed = frame.Left.Speed;
            boxingRightSpeed = frame.Right.Speed;
            if (pose.leftDetected) boxingLeftHand = Vector2.Lerp(boxingLeftHand, newLeft, .72f);
            if (pose.rightDetected) boxingRightHand = Vector2.Lerp(boxingRightHand, newRight, .72f);
            if (state == GameState.Boxing && (frame.Actions&JungleAction.PunchLeft)!=0) TryBoxingPunch(false, boxingLeftSpeed, power);
        }
        float activeWristX = sharedSession.HandMode == MotionSession.HandTrackingMode.LeftOnly ? pose.leftWristX : pose.rightWristX;
        float activeWristY = sharedSession.HandMode == MotionSession.HandTrackingMode.LeftOnly ? pose.leftWristY : pose.rightWristY;
        poseWristX = activeWristX;
        poseBodyX = pose.bodyX;
        if(frame.NewPose)poseTrail.Add(new Vector3(activeWristX - pose.bodyX, activeWristY, now));
        while (poseTrail.Count > 0 && now - poseTrail[0].z > .65f) poseTrail.RemoveAt(0);
        Vector2 hand = new Vector2(
            Mathf.Clamp01(.5f + (activeWristX - .5f) * 1.55f),
            Mathf.Clamp01(.5f + (activeWristY - .5f) * 1.35f));
        if (state == GameState.Menu)
        {
            menuCursor = Vector2.Lerp(menuCursor, hand, .72f);
            UpdateMenuSelection();
        }

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

    private void ReadPhone(JungleFusedFrame frame)
    {
        var p=frame.Motion;
        lastPacketAt = Time.unscaledTime; latestGamma = p.gamma; latestBeta = p.beta;
        rawRotation = Quaternion.Euler(p.beta, p.alpha, -p.gamma) * Quaternion.Euler(0, 0, -p.screen); hasRaw = true;
        if (!calibrated) Calibrate();
        if (!confirmInitialized)
        {
            lastConfirm = p.confirm; lastRecenter = p.recenter; confirmInitialized = true;
            string activeScene = SceneManager.GetActiveScene().name;
            if (activeScene == SceneNavigator.Bowling && state == GameState.Connecting) state = GameState.Aim;
            else if (activeScene != SceneNavigator.Boxing && state == GameState.Connecting) state = GameState.Menu;
        }
        if (p.recenter != lastRecenter) { lastRecenter = p.recenter; Calibrate(); menuCursor = new Vector2(.32f, .56f); }
        if (state == GameState.Menu)
        {
            UpdateMenuSelection();
            if (p.confirm != lastConfirm)
            {
                lastConfirm = p.confirm;
                if (menuSelection < 2) SceneNavigator.Load(menuSelection==0?SceneNavigator.Bowling:SceneNavigator.Boxing);
                else { Calibrate(); menuCursor = new Vector2(.32f, .56f); }
            }
            return;
        }

        power=frame.PhonePower;
        if (state == GameState.BoxingKnockdown)
        {
            if ((frame.Actions&JungleAction.Recover)!=0 && sharedSession.TryAuthorize(JungleAction.Recover,frame.Revision)) { getUpEnergy = Mathf.Min(100f, getUpEnergy + Mathf.Clamp((power - 210f) * .12f, 8f, 24f));  }
            return;
        }
        if (state == GameState.Boxing)
        {
            if ((frame.Actions&JungleAction.PunchRight)!=0) { TryBoxingPunch(true, boxingRightSpeed, power);  }
            return;
        }

        if (state == GameState.Aim && Time.time >= aimReadyAt)
        {
            if ((frame.Actions&JungleAction.Throw)!=0)
            {
                swingStartGamma = centerGamma;
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
        if(state!=GameState.Aim||inputFrame==null||!CameraActive||!sharedSession.TryAuthorize(JungleAction.Throw,inputFrame.Revision))return;
        state = GameState.Rolling; ballBody.isKinematic = false;
        ballPassedPinsAt = -1f;
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
        else return;
        lastLaunchDirection = direction;
        ballBody.linearVelocity = new Vector3(direction * 3.1f, 0, speed);
        ballBody.angularVelocity = new Vector3(speed * 1.4f, 0, -direction * 7.5f);
        resultAt = Time.time + 7.0f;
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

    private IEnumerator AnimateBowlingRackTransition()
    {
        bowlingRackTransitioning = true;
        bool replacingFullRack = nextNeedsFullRack;
        var lowering = new List<GameObject>();
        var starts = new Dictionary<GameObject, Vector3>();
        foreach (GameObject pin in pins)
        {
            if (pin == null || !pin.activeSelf) continue;
            if (!replacingFullRack && !pinsFallenThisRoll.Contains(pin)) continue;
            lowering.Add(pin);
            starts[pin] = pin.transform.position;
            Rigidbody body = pin.GetComponent<Rigidbody>();
            if (body != null) StopBody(body, true);
        }

        float elapsed = 0f;
        const float lowerDuration = .48f;
        while (elapsed < lowerDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / lowerDuration));
            foreach (GameObject pin in lowering)
                if (pin != null) pin.transform.position = starts[pin] + Vector3.down * (1.95f * t);
            yield return null;
        }

        AdvanceBowlingTurn();
        GameState readyState = state;
        state = GameState.Results;

        if (replacingFullRack)
        {
            var targets = new Dictionary<GameObject, Vector3>();
            foreach (GameObject pin in pins)
            {
                if (pin == null || !pin.activeSelf) continue;
                targets[pin] = pin.transform.position;
                pin.transform.position = targets[pin] + Vector3.down * 1.95f;
            }
            elapsed = 0f;
            const float raiseDuration = .62f;
            while (elapsed < raiseDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / raiseDuration));
                foreach (KeyValuePair<GameObject, Vector3> item in targets)
                    if (item.Key != null) item.Key.transform.position = item.Value + Vector3.down * (1.95f * (1f - t));
                yield return null;
            }
        }

        state = readyState;
        bowlingRackTransitioning = false;
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
                    if (body != null) { StopBody(body, false); body.Sleep(); }
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
        float verticalShift = Mathf.Abs(position.y - start.y);
        return upright > .88f && verticalShift < .14f && horizontalShift < .30f && Mathf.Abs(position.x) < 3.75f && position.z > 6.4f && position.z < 11f;
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
        hudFont ??= Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial" }, 32);
        // Las referencias sobreviven a una recompilación mientras Play está activo.
        // Descarta las texturas antiguas para que el nuevo HUD se vea de inmediato.
        if (bowlingHudTexture != null && bowlingHudTexture.width < 192) { Destroy(bowlingHudTexture); bowlingHudTexture = null; }
        if (bowlingCardTexture != null && bowlingCardTexture.width < 144) { Destroy(bowlingCardTexture); bowlingCardTexture = null; }
        if (bowlingButtonTexture != null && bowlingButtonTexture.width < 128) { Destroy(bowlingButtonTexture); bowlingButtonTexture = null; }
        if (bowlingButtonHoverTexture != null && bowlingButtonHoverTexture.width < 128) { Destroy(bowlingButtonHoverTexture); bowlingButtonHoverTexture = null; }
        panelTexture ??= Solid(new Color(.025f, .045f, .09f, .94f));
        accentTexture ??= Solid(new Color(.05f, .72f, .92f, 1f));
        selectedTexture ??= Solid(new Color(1f, .78f, .08f, .96f));
        optionTexture ??= Solid(new Color(.08f, .19f, .13f, .96f));
        darkTexture ??= Solid(new Color(.035f, .025f, .018f, 1f));
        boxingBlueTexture ??= Solid(new Color(.12f, .55f, 1f, 1f));
        boxingRedTexture ??= Solid(new Color(1f, .25f, .12f, 1f));
        bowlingHudTexture ??= Rounded(192, 48, new Color(.018f, .055f, .075f, .94f), new Color(.018f, .055f, .075f, .94f));
        bowlingCardTexture ??= Rounded(144, 36, new Color(.035f, .15f, .13f, .96f), new Color(.035f, .15f, .13f, .96f));
        bowlingButtonTexture ??= Rounded(128, 32, new Color(.07f, .67f, .84f, 1f), new Color(.07f, .67f, .84f, 1f));
        bowlingButtonHoverTexture ??= Rounded(128, 32, new Color(.16f, .84f, .94f, 1f), new Color(.16f, .84f, .94f, 1f));
        titleStyle ??= new GUIStyle(GUI.skin.label) { font = hudFont, fontSize = 32, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(.45f, .94f, 1f) } };
        bigStyle ??= new GUIStyle(GUI.skin.label) { font = hudFont, fontSize = 38, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
        bodyStyle ??= new GUIStyle(GUI.skin.label) { font = hudFont, fontSize = 21, alignment = TextAnchor.MiddleCenter, wordWrap = true, normal = { textColor = new Color(.86f, .9f, 1f) } };
        buttonStyle ??= new GUIStyle(GUI.skin.button) { font = hudFont, fontSize = 22, fontStyle = FontStyle.Bold, normal = { background = accentTexture, textColor = new Color(.01f, .06f, .1f) } };
    }

    private Texture2D Rounded(int size, int radius, Color fill, Color border)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = Mathf.Max(0f, Mathf.Max(radius - x, x - (size - radius - 1)));
            float dy = Mathf.Max(0f, Mathf.Max(radius - y, y - (size - radius - 1)));
            float distance = Mathf.Sqrt(dx * dx + dy * dy);
            float alpha = Mathf.Clamp01(radius + .5f - distance);
            float inside = Mathf.Clamp01(radius - 2.5f - distance);
            Color color = Color.Lerp(border, fill, inside);
            color.a *= alpha;
            texture.SetPixel(x, y, color);
        }
        texture.Apply(false, true);
        return texture;
    }

    private void OnGUI()
    {
        PrepareStyles();
        if (hudFont != null) GUI.skin.font = hudFont;
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
        DrawBowlingHud();
        DrawBowlingDashboard();
        DrawCameraStatus();
    }

    private void DrawBowlingHud()
    {
        float w = Screen.width, h = Screen.height;
        float scale = Mathf.Clamp(Mathf.Min(w / 1920f, h / 1080f), .65f, 1.35f);
        Rect hud = new Rect(24 * scale, 20 * scale, Mathf.Min(w * .57f, 820 * scale), 154 * scale);
        GUI.Box(hud, GUIContent.none, RoundedStyle(bowlingHudTexture, 24));

        GUIStyle brand = CrispLabel(Mathf.RoundToInt(18 * scale), new Color(.30f, .95f, .88f), FontStyle.Bold, TextAnchor.MiddleLeft);
        GUIStyle frame = CrispLabel(Mathf.RoundToInt(29 * scale), Color.white, FontStyle.Bold, TextAnchor.MiddleLeft);
        GUIStyle messageStyle = CrispLabel(Mathf.RoundToInt(14 * scale), new Color(.82f, .90f, .89f), FontStyle.Normal, TextAnchor.MiddleLeft);
        GUI.Label(new Rect(hud.x + 22 * scale, hud.y + 10 * scale, hud.width * .42f, 30 * scale), "BANANA BOWLING", brand);

        bool connected = Time.unscaledTime - lastPacketAt < 1f;
        if (!connected)
        {
            GUI.Label(new Rect(hud.x + 22 * scale, hud.y + 42 * scale, hud.width * .52f, 43 * scale), $"CÓDIGO  {roomCode}", frame);
            GUI.Label(new Rect(hud.x + 22 * scale, hud.y + 86 * scale, hud.width * .54f, 28 * scale), connectionState, messageStyle);
            if (!polling && GUI.Button(new Rect(hud.xMax - 205 * scale, hud.y + 52 * scale, 180 * scale, 42 * scale), "CONECTAR", CrispButton(Mathf.RoundToInt(14 * scale)))) Connect();
            return;
        }

        GUI.Label(new Rect(hud.x + 22 * scale, hud.y + 40 * scale, hud.width * .44f, 43 * scale), $"FRAME {Mathf.Min(bowlingFrame + 1, 10)}  ·  BOLA {bowlingBall}", frame);
        string shotLabel = lastLaunchDirection < -.18f ? "DIAGONAL IZQUIERDA" : lastLaunchDirection > .18f ? "DIAGONAL DERECHA" : "TIRO RECTO";
        string message = bowlingGameOver ? $"PARTIDA TERMINADA · TOTAL {GetDisplayedBowlingScore()}" : state == GameState.Aim
            ? "Coloca la mano · Swing fuerte · Traza la diagonal"
            : state == GameState.Rolling ? $"¡Bola en juego! · {shotLabel}" : $"Derribaste {lastPins} pinos";
        GUI.Label(new Rect(hud.x + 22 * scale, hud.y + 80 * scale, hud.width - 44 * scale, 28 * scale), message, messageStyle);

        float buttonY = hud.yMax - 38 * scale;
        float gap = 8 * scale;
        float buttonWidth = (hud.width - 44 * scale - gap * 2) / 3f;
        GUIStyle button = CrispButton(Mathf.RoundToInt(13 * scale));
        if (GUI.Button(new Rect(hud.x + 22 * scale, buttonY, buttonWidth, 29 * scale), "CALIBRAR", button)) Calibrate();
        if (GUI.Button(new Rect(hud.x + 22 * scale + buttonWidth + gap, buttonY, buttonWidth, 29 * scale), "NUEVA PARTIDA", button)) StartBowlingGame();
        if (GUI.Button(new Rect(hud.x + 22 * scale + (buttonWidth + gap) * 2, buttonY, buttonWidth, 29 * scale), "MENÚ", button)) SceneNavigator.Load(SceneNavigator.MainMenu);
    }

    private GUIStyle CrispLabel(int size, Color color, FontStyle weight, TextAnchor alignment)
    {
        var style = new GUIStyle(GUI.skin.label) { font = hudFont, fontSize = Mathf.Max(15, size), fontStyle = weight, alignment = alignment, clipping = TextClipping.Clip };
        style.normal.textColor = color;
        return style;
    }

    private GUIStyle CrispButton(int size)
    {
        var style = new GUIStyle(GUI.skin.button) { font = hudFont, fontSize = Mathf.Max(14, size), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, border = new RectOffset(32, 32, 32, 32) };
        style.normal.background = bowlingButtonTexture;
        style.hover.background = bowlingButtonHoverTexture;
        style.active.background = bowlingButtonHoverTexture;
        style.normal.textColor = new Color(.015f, .08f, .09f);
        style.hover.textColor = style.normal.textColor;
        style.active.textColor = style.normal.textColor;
        return style;
    }

    private static GUIStyle RoundedStyle(Texture2D texture, int border)
    {
        var style = new GUIStyle(GUI.skin.box) { border = new RectOffset(border, border, border, border) };
        style.normal.background = texture;
        return style;
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
        float scale = Mathf.Clamp(Mathf.Min(w / 1920f, h / 1080f), .65f, 1.35f);
        float width = Mathf.Min(w * .205f, 292 * scale);
        float height = 618 * scale;
        float x = w - width - 24 * scale, y = 22 * scale;
        GUI.Box(new Rect(x, y, width, height), GUIContent.none, RoundedStyle(bowlingHudTexture, 24));
        dashHeadingStyle = CrispLabel(Mathf.RoundToInt(18 * scale), new Color(1f, .84f, .18f), FontStyle.Bold, TextAnchor.MiddleCenter);
        dashRowStyle = CrispLabel(Mathf.RoundToInt(12 * scale), new Color(.84f, .91f, .89f), FontStyle.Normal, TextAnchor.MiddleLeft);
        dashRowActiveStyle = CrispLabel(Mathf.RoundToInt(12 * scale), new Color(.06f, .09f, .05f), FontStyle.Bold, TextAnchor.MiddleLeft);
        dashScoreStyle = CrispLabel(Mathf.RoundToInt(14 * scale), Color.white, FontStyle.Bold, TextAnchor.MiddleRight);
        dashScoreActiveStyle = CrispLabel(Mathf.RoundToInt(14 * scale), new Color(.06f, .09f, .05f), FontStyle.Bold, TextAnchor.MiddleRight);
        GUI.Label(new Rect(x + 14 * scale, y + 12 * scale, width - 28 * scale, 28 * scale), "TABLERO DE LA SELVA", dashHeadingStyle);
        GUI.Label(new Rect(x + 18 * scale, y + 44 * scale, width - 36 * scale, 23 * scale), $"ÚLTIMO TIRO: {lastPins} PINOS", dashRowStyle);
        int[] scores = CalculateBowlingScores();
        float rowsY = y + 76 * scale, rowH = 42 * scale;
        for (int i = 0; i < 10; i++)
        {
            bool active = i == Mathf.Min(bowlingFrame, 9) && !bowlingGameOver;
            Rect row = new Rect(x + 14 * scale, rowsY + i * rowH, width - 28 * scale, rowH - 5 * scale);
            GUI.Box(row, GUIContent.none, RoundedStyle(active ? selectedTexture : bowlingCardTexture, active ? 0 : 14));
            GUIStyle frameStyle = active ? dashRowActiveStyle : dashRowStyle;
            GUIStyle frameScore = active ? dashScoreActiveStyle : dashScoreStyle;
            GUI.Label(new Rect(row.x + 10 * scale, row.y, row.width * .18f, row.height), (i + 1).ToString("00"), frameStyle);
            GUI.Label(new Rect(row.x + row.width * .21f, row.y, row.width * .39f, row.height), BowlingRollText(i), frameStyle);
            GUI.Label(new Rect(row.x + row.width * .62f, row.y, row.width * .32f, row.height), scores[i] >= 0 ? scores[i].ToString() : "", frameScore);
        }
        GUI.Label(new Rect(x + 16 * scale, y + height - 66 * scale, width - 32 * scale, 45 * scale), $"TOTAL  {GetDisplayedBowlingScore()}", dashHeadingStyle);
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
        float scale = Mathf.Clamp(Mathf.Min(Screen.width / 1920f, Screen.height / 1080f), .65f, 1.35f);
        GUIStyle status = CrispLabel(Mathf.RoundToInt(13 * scale), CameraActive ? new Color(.3f, 1f, .45f) : new Color(1f, .72f, .22f), FontStyle.Bold, TextAnchor.MiddleCenter);
        status.normal.textColor = CameraActive ? new Color(.3f, 1f, .45f) : new Color(1f, .72f, .22f);
        Rect rect = new Rect(Screen.width - 292 * scale - 24 * scale, Screen.height - 55 * scale, 292 * scale, 38 * scale);
        GUI.Box(rect, GUIContent.none, RoundedStyle(bowlingHudTexture, 18));
        GUI.Label(rect, CameraActive ? "● CÁMARA · MANO DETECTADA" : "○ CÁMARA · ESPERANDO", status);
    }

    private void DrawLeafFrame()
    {
        if (leavesLeft != null) GUI.DrawTexture(new Rect(0, 0, Screen.width * .31f, Screen.height * .2f), leavesLeft, ScaleMode.ScaleToFit, true);
        if (leavesRight != null) GUI.DrawTexture(new Rect(Screen.width * .71f, 0, Screen.width * .29f, Screen.height * .21f), leavesRight, ScaleMode.ScaleToFit, true);
        if (leavesBottom != null) GUI.DrawTexture(new Rect(0, Screen.height * .82f, Screen.width * .38f, Screen.height * .18f), leavesBottom, ScaleMode.ScaleToFit, true);
    }

    private void OnDestroy()
    {
        if (bowlingGorillaGraph.IsValid()) bowlingGorillaGraph.Destroy();
        gorillaImport?.Dispose();
        islandImport?.Dispose();
        if (hudFont != null) Destroy(hudFont);
        if (bowlingHudTexture != null) Destroy(bowlingHudTexture);
        if (bowlingCardTexture != null) Destroy(bowlingCardTexture);
        if (bowlingButtonTexture != null) Destroy(bowlingButtonTexture);
        if (bowlingButtonHoverTexture != null) Destroy(bowlingButtonHoverTexture);
    }
}
