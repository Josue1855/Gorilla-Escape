using System;
using System.Collections.Generic;
using UnityEngine;
using GorillaEscape.Gameplay.Input;

public sealed class CoconutSliceController : MonoBehaviour
{
    private enum GameState { Intro, Countdown, Playing, Finale, Results }
    private sealed class Coconut
    {
        public GameObject gameObject;
        public Rigidbody body;
        public bool sliced;
        public bool hazard;
        public bool instantLoss;
        public float bornAt;
    }

    private const float RoundDuration = 45f;
    private readonly List<Coconut> coconuts = new List<Coconut>();
    private readonly List<GameObject> temporaryPieces = new List<GameObject>();
    private MotionSession session;
    private GameState state = GameState.Intro;
    private Vector2 leftHand = new Vector2(.32f, .52f), rightHand = new Vector2(.68f, .52f);
    private Vector2 previousLeft, previousRight;
    private float leftSpeed, rightSpeed, nextSpawn, stateStartedAt, roundEndsAt, lastLeftSliceAt, lastRightSliceAt;
    private int score, combo, bestCombo, missed, sliced, lives = 3;
    private string callout = "";
    private float calloutUntil;
    private Material coconutMaterial, coconutInsideMaterial, darkMaterial, greenMaterial;
    private GameObject wholeCoconutPrefab, halfCoconutPrefab;
    private Material wholeCoconutModelMaterial, halfCoconutModelMaterial;
    private GameObject cactusPrefab, halfCactusPrefab;
    private Material cactusModelMaterial, halfCactusModelMaterial;
    private GameObject sleepingGorillaPrefab, hitGorillaPrefab;
    private Material sleepingGorillaMaterial, hitGorillaMaterial;
    private GameObject runningGorillaPrefab, fallingGorillaPrefab, distractorGorilla;
    private GameObject distractorVisualRoot, distractorAnimatedModel;
    private AnimationClip runningGorillaClip, fallingGorillaClip;
    private Material runningGorillaMaterial, fallingGorillaMaterial;
    private Vector3 distractorStart, distractorEnd;
    private float nextDistractorAt, distractorStartedAt, distractorDuration;
    private bool distractorIsFall;
    private bool hasSpawnedDistractor, previousDistractorWasFall;
    private bool rareGorillaSpawned, lostByGorilla;
    private float rareGorillaAt;
    private int repeatedDistractorType;
    private Texture2D dark, gold, turquoise, orange, red, white;
    private GUIStyle titleStyle, bigStyle, bodyStyle, scoreStyle;

    private void Awake()
    {
        session = MotionSession.Ensure();
        session.Connect();
        BuildStage();
        LoadCoconutModels();
        LoadDistractorGorillas();
        nextDistractorAt = Time.time + UnityEngine.Random.Range(2.5f, 5f);
        previousLeft = leftHand;
        previousRight = rightHand;
        stateStartedAt = Time.time;
    }

    private Material MakeMaterial(Color color)
    {
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        material.color = color;
        return material;
    }

    private void LoadCoconutModels()
    {
        const string wholeRoot = "Jungle/NewModels/Coconut/Meshy_AI_Coconut_Bowling_Ball_20261026_texture_fbx/";
        const string halfRoot = "Jungle/NewModels/Slice/HalfCoconut/Meshy_AI_Coconut_Bowl_20261026_texture_fbx/";
        const string cactusRoot = "Jungle/NewModels/Slice/Cactus/Meshy_AI_Spiked_Melon_Sphere_20261026_texture_fbx/";
        const string halfCactusRoot = "Jungle/NewModels/Slice/HalfCactus/Meshy_AI_Prickly_Cucumber_Slice_20261026_texture_fbx/";
        const string sleepingRoot = "Jungle/NewModels/Slice/GorillaHazard/Meshy_AI_Sleeping_Gorilla_Cub_20261026_texture_fbx/";
        const string hitRoot = "Jungle/NewModels/Slice/GorillaHazard/Meshy_AI_Bumped_Banana_Brute_20261026_texture_fbx/";
        wholeCoconutPrefab = Resources.Load<GameObject>(wholeRoot + "Meshy_AI_Coconut_Bowling_Ball_20261026_texture");
        halfCoconutPrefab = Resources.Load<GameObject>(halfRoot + "Meshy_AI_Coconut_Bowl_20261026_texture");
        cactusPrefab = Resources.Load<GameObject>(cactusRoot + "Meshy_AI_Spiked_Melon_Sphere_20261026_texture");
        halfCactusPrefab = Resources.Load<GameObject>(halfCactusRoot + "Meshy_AI_Prickly_Cucumber_Slice_20261026_texture");
        sleepingGorillaPrefab = Resources.Load<GameObject>(sleepingRoot + "Meshy_AI_Sleeping_Gorilla_Cub_20261026_texture");
        hitGorillaPrefab = Resources.Load<GameObject>(hitRoot + "Meshy_AI_Bumped_Banana_Brute_20261026_texture");
        wholeCoconutModelMaterial = MakeTexturedMaterial(Resources.Load<Texture2D>(wholeRoot + "Coconut_Albedo"), "Coco entero - Slice");
        halfCoconutModelMaterial = MakeTexturedMaterial(Resources.Load<Texture2D>(halfRoot + "HalfCoconut_Albedo"), "Coco partido - Slice");
        cactusModelMaterial = MakeTexturedMaterial(Resources.Load<Texture2D>(cactusRoot + "Cactus_Albedo"), "Cactus peligroso - Slice");
        halfCactusModelMaterial = MakeTexturedMaterial(Resources.Load<Texture2D>(halfCactusRoot + "HalfCactus_Albedo"), "Cactus partido - Slice");
        sleepingGorillaMaterial = MakeTexturedMaterial(Resources.Load<Texture2D>(sleepingRoot + "Meshy_AI_Sleeping_Gorilla_Cub_20261026_texture"), "Gorila hecho bolita");
        hitGorillaMaterial = MakeTexturedMaterial(Resources.Load<Texture2D>(hitRoot + "Meshy_AI_Bumped_Banana_Brute_20261026_texture"), "Gorila golpeado");
        if (wholeCoconutPrefab == null || halfCoconutPrefab == null || cactusPrefab == null || halfCactusPrefab == null)
            Debug.LogWarning("No se encontraron todos los modelos nuevos de Coconut Slice; se usará el respaldo simple cuando sea necesario.");
    }

    private void CreateSlicePalms()
    {
        const string palmRoot = "Jungle/NewModels/Palm/Meshy_AI_Cartoon_Coconut_Palm_20261026_texture_fbx/";
        GameObject palmPrefab = Resources.Load<GameObject>(palmRoot + "Meshy_AI_Cartoon_Coconut_Palm_20261026_texture");
        if (palmPrefab == null) return;
        Material palmMaterial = MakeTexturedMaterial(Resources.Load<Texture2D>(palmRoot + "Palm_Albedo"), "Palmeras de Coconut Slice");
        CreateSlicePalm(palmPrefab, palmMaterial, "Palmera izquierda", new Vector3(-7.65f, -5.42f, 3.55f), 6.3f, 18f);
        CreateSlicePalm(palmPrefab, palmMaterial, "Palmera derecha", new Vector3(7.65f, -5.42f, 3.55f), 6.3f, -18f);
        CreateSlicePalm(palmPrefab, palmMaterial, "Palmera izquierda secundaria", new Vector3(-5.85f, -5.42f, 3.75f), 4.25f, -12f);
        CreateSlicePalm(palmPrefab, palmMaterial, "Palmera derecha secundaria", new Vector3(5.85f, -5.42f, 3.75f), 4.25f, 12f);
    }

    private void CreateSlicePalm(GameObject prefab, Material material, string palmName, Vector3 groundPosition, float desiredHeight, float yaw)
    {
        GameObject palm = Instantiate(prefab, transform);
        palm.name = palmName;
        Quaternion importedRotation = palm.transform.localRotation;
        palm.transform.position = Vector3.zero;
        palm.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * importedRotation;
        palm.transform.localScale = Vector3.one;
        foreach (Collider collider in palm.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        Renderer[] renderers = palm.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;
        Bounds bounds = CombinedBounds(renderers);
        palm.transform.localScale = Vector3.one * (desiredHeight / Mathf.Max(.01f, bounds.size.y));
        bounds = CombinedBounds(renderers);
        palm.transform.position += new Vector3(groundPosition.x - bounds.center.x, groundPosition.y - bounds.min.y, groundPosition.z - bounds.center.z);
        ApplySharedMaterial(renderers, material);
    }

    private void LoadDistractorGorillas()
    {
        const string runRoot = "Jungle/GorillaBowling/Meshy_AI_Gorilla_Guardian_biped/";
        const string runAsset = runRoot + "Meshy_AI_Gorilla_Guardian_biped_Animation_Running_withSkin";
        const string fallRoot = "Jungle/NewModels/Slice/GorillaFall/Meshy_AI_Low_Poly_Gorilla_Embr_biped/";
        const string fallAsset = fallRoot + "Meshy_AI_Low_Poly_Gorilla_Embr_biped_Animation_Fall1_withSkin";
        runningGorillaPrefab = Resources.Load<GameObject>(runAsset);
        fallingGorillaPrefab = Resources.Load<GameObject>(fallAsset);
        runningGorillaClip = FirstUsableClip(runAsset);
        fallingGorillaClip = FirstUsableClip(fallAsset);
        runningGorillaMaterial = MakeTexturedMaterial(Resources.Load<Texture2D>(runRoot + "Meshy_AI_Gorilla_Guardian_biped_texture_0"), "Gorila corredor");
        fallingGorillaMaterial = MakeTexturedMaterial(Resources.Load<Texture2D>(fallRoot + "GorillaFall_Albedo"), "Gorila que cae");
        if (runningGorillaPrefab == null || fallingGorillaPrefab == null)
            Debug.LogWarning("Falta uno de los gorilas distractores de Coconut Slice.");
    }

    private static AnimationClip FirstUsableClip(string resourcePath)
    {
        foreach (AnimationClip clip in Resources.LoadAll<AnimationClip>(resourcePath))
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

    private static Bounds CombinedBounds(Renderer[] renderers)
    {
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static void ApplySharedMaterial(Renderer[] renderers, Material material)
    {
        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++) materials[i] = material;
            renderer.sharedMaterials = materials;
        }
    }

    private Material MakeTexturedMaterial(Texture2D texture, string materialName)
    {
        Material material = MakeMaterial(Color.white);
        material.name = materialName;
        material.mainTexture = texture;
        material.SetFloat("_Smoothness", .12f);
        material.SetFloat("_Glossiness", .12f);
        material.enableInstancing = true;
        return material;
    }

    private GameObject Primitive(PrimitiveType type, string objectName, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject item = GameObject.CreatePrimitive(type);
        item.name = objectName;
        item.transform.SetParent(parent);
        item.transform.position = position;
        item.transform.localScale = scale;
        item.GetComponent<Renderer>().sharedMaterial = material;
        Collider collider = item.GetComponent<Collider>();
        if (collider != null) collider.enabled = false;
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

        Texture2D jungleBackground = Resources.Load<Texture2D>("Jungle/CoconutSlice/JungleSkyBackground");
        Material backgroundMaterial = new Material(Shader.Find("Unlit/Texture") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard"));
        backgroundMaterial.name = "Fondo pintado de Coconut Slice";
        backgroundMaterial.mainTexture = jungleBackground;
        backgroundMaterial.mainTextureScale = new Vector2(1f, -1f);
        backgroundMaterial.mainTextureOffset = new Vector2(0f, 1f);
        backgroundMaterial.color = Color.white;
        Primitive(PrimitiveType.Cube, "Fondo selvático pintado", transform, new Vector3(0, 0, 4.5f), new Vector3(19.4f, 10.95f, .3f), backgroundMaterial);

        CreateSlicePalms();
    }

    private void Update()
    {
        session.PresentGame(state==GameState.Intro?"Ready":state==GameState.Countdown?"Countdown":state==GameState.Playing?"Playing":"Finished",score);
        if(!session.IsInputReady){leftSpeed=rightSpeed=0;return;}
        if(session.TryConsumeFrame(out var frame))ReadPose(frame);
        UpdateDistractorGorilla();

        if (state == GameState.Countdown && Time.time - stateStartedAt >= 3.2f) StartRound();
        else if (state == GameState.Playing)
        {
            if (Time.time >= nextSpawn) SpawnWave();
            if (Time.time >= roundEndsAt) StartFinale();
        }
        else if (state == GameState.Finale && Time.time - stateStartedAt >= 3.4f)
        {
            ClearCoconuts();
            state = GameState.Results;
            stateStartedAt = Time.time;
        }

        UpdateCoconuts();
        UpdateTemporaryPieces();
    }

    private void ReadPose(JungleFusedFrame frame)
    {
        if(!frame.NewPose&&!frame.Baseline)return;
        Vector2 nextLeft=new Vector2(frame.Left.X,1f-frame.Left.Y);
        Vector2 nextRight=new Vector2(frame.Right.X,1f-frame.Right.Y);
        leftSpeed=frame.Left.Speed;rightSpeed=frame.Right.Speed;
        if(!frame.Baseline)
        {
            if((frame.Actions&JungleAction.SliceLeft)!=0)TrySliceSegment(previousLeft,nextLeft,leftSpeed,JungleAction.SliceLeft,frame.Revision);
            if((frame.Actions&JungleAction.SliceRight)!=0)TrySliceSegment(previousRight,nextRight,rightSpeed,JungleAction.SliceRight,frame.Revision);
        }
        leftHand=nextLeft;rightHand=nextRight;
        previousLeft=nextLeft;previousRight=nextRight;
    }

    private void TrySliceSegment(Vector2 from, Vector2 to, float speed, JungleAction action, long revision)
    {
        float previousSlice=action==JungleAction.SliceLeft?lastLeftSliceAt:lastRightSliceAt;
        if (state != GameState.Playing || Time.time - previousSlice < .045f) return;
        if(!session.TryAuthorize(action,revision))return;
        for (int i = coconuts.Count - 1; i >= 0; i--)
        {
            Coconut coconut = coconuts[i];
            if (coconut.sliced) continue;
            Vector3 viewport3 = Camera.main.WorldToViewportPoint(coconut.gameObject.transform.position);
            if (viewport3.z <= 0f) continue;
            Vector2 viewport = new Vector2(viewport3.x, viewport3.y);
            if (DistanceToSegment(viewport, from, to) < .075f)
            {
                Slice(coconut, to - from, speed);
                if(action==JungleAction.SliceLeft)lastLeftSliceAt=Time.time;else lastRightSliceAt=Time.time;
                break;
            }
        }
    }

    private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 segment = b - a;
        float lengthSquared = segment.sqrMagnitude;
        if (lengthSquared < .00001f) return Vector2.Distance(point, a);
        float t = Mathf.Clamp01(Vector2.Dot(point - a, segment) / lengthSquared);
        return Vector2.Distance(point, a + segment * t);
    }

    private void SpawnWave()
    {
        if (!rareGorillaSpawned && Time.time >= rareGorillaAt && sleepingGorillaPrefab != null)
        {
            SpawnRareGorilla();
            rareGorillaSpawned = true;
            nextSpawn = Time.time + 1.15f;
            return;
        }
        int amount = UnityEngine.Random.value < .28f ? 2 : 1;
        for (int i = 0; i < amount; i++) SpawnCoconut(i, amount, UnityEngine.Random.value < .20f);
        float progress = 1f - Mathf.Clamp01((roundEndsAt - Time.time) / RoundDuration);
        nextSpawn = Time.time + Mathf.Lerp(1.05f, .52f, progress) + UnityEngine.Random.Range(-.08f, .15f);
    }

    private void SpawnRareGorilla()
    {
        float x = UnityEngine.Random.Range(-.75f, .75f);
        Vector3 position = new Vector3(x, -5.25f, -1.15f);
        GameObject visual = CreateCoconutPhysicsObject("Gorila hecho bolita - NO CORTAR", sleepingGorillaPrefab, sleepingGorillaMaterial, position, 2.15f, false, 0);
        visual.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        Rigidbody body = visual.AddComponent<Rigidbody>();
        body.mass = 1.15f;
        body.useGravity = true;
        body.linearVelocity = new Vector3(-x * .16f + UnityEngine.Random.Range(-.18f, .18f), UnityEngine.Random.Range(13.3f, 14.1f), 0f);
        body.angularVelocity = new Vector3(0f, 0f, UnityEngine.Random.Range(-.45f, .45f));
        coconuts.Add(new Coconut { gameObject = visual, body = body, bornAt = Time.time, hazard = true, instantLoss = true });
        callout = "¡NO CORTES AL GORILA!";
        calloutUntil = Time.time + 1.8f;
    }

    private void SpawnCoconut(int index, int count, bool hazard)
    {
        float x = UnityEngine.Random.Range(-4.6f, 4.6f) + (index - (count - 1) * .5f) * 1.15f;
        Vector3 position = new Vector3(x, -5.05f, UnityEngine.Random.Range(-.4f, .45f));
        GameObject visual = CreateCoconutPhysicsObject(hazard ? "Cactus peligroso" : "Coco entero", hazard ? cactusPrefab : wholeCoconutPrefab, hazard ? cactusModelMaterial : wholeCoconutModelMaterial, position, hazard ? 1.02f : .86f, false, 0);
        Rigidbody body = visual.AddComponent<Rigidbody>();
        body.mass = hazard ? .78f : .65f;
        body.useGravity = true;
        body.linearVelocity = new Vector3(Mathf.Clamp(-x * .30f + UnityEngine.Random.Range(-1.7f, 1.7f), -3.5f, 3.5f), UnityEngine.Random.Range(9.4f, 12.2f), 0f);
        body.angularVelocity = UnityEngine.Random.insideUnitSphere * 8f;
        coconuts.Add(new Coconut { gameObject = visual, body = body, bornAt = Time.time, hazard = hazard });
    }

    private void Slice(Coconut coconut, Vector2 direction, float speed)
    {
        coconut.sliced = true;
        Vector3 position = coconut.gameObject.transform.position;
        Vector3 velocity = coconut.body.linearVelocity;
        Destroy(coconut.gameObject);
        coconuts.Remove(coconut);

        if (coconut.instantLoss)
        {
            ShowHitGorilla(position, velocity, direction, speed);
            lives = 0;
            combo = 0;
            lostByGorilla = true;
            callout = "¡GOLPEASTE AL GORILA!";
            calloutUntil = Time.time + 2.4f;
            state = GameState.Finale;
            stateStartedAt = Time.time;
            return;
        }

        Vector3 split = new Vector3(direction.y, -direction.x, 0f).normalized;
        for (int side = -1; side <= 1; side += 2)
        {
            GameObject half = CreateCoconutPhysicsObject(coconut.hazard ? "Medio cactus" : (side < 0 ? "Medio coco A" : "Medio coco B"), coconut.hazard ? halfCactusPrefab : halfCoconutPrefab, coconut.hazard ? halfCactusModelMaterial : halfCoconutModelMaterial, position + split * side * .18f, coconut.hazard ? .82f : .72f, true, side);
            Rigidbody body = half.AddComponent<Rigidbody>();
            body.mass = coconut.hazard ? .37f : .31f;
            body.linearVelocity = velocity * .72f + split * side * Mathf.Clamp(speed, 1.4f, 4.5f);
            body.angularVelocity = UnityEngine.Random.insideUnitSphere * 12f;
            temporaryPieces.Add(half);
        }

        if (coconut.hazard)
        {
            lives = Mathf.Max(0, lives - 1);
            combo = 0;
            callout = lives > 0 ? "¡OUCH!  VIDA -1" : "¡SIN VIDAS!";
            calloutUntil = Time.time + 1.1f;
            if (lives == 0)
            {
                state = GameState.Finale;
                stateStartedAt = Time.time;
            }
        }
        else
        {
            sliced++;
            combo++;
            bestCombo = Mathf.Max(bestCombo, combo);
            int gained = 100 + Mathf.Min(combo, 20) * 15;
            score += gained;
            callout = combo >= 10 ? "¡GORILA IMPARABLE!  x" + combo : combo >= 5 ? "¡COMBO x" + combo + "!" : "+" + gained;
            calloutUntil = Time.time + .65f;
        }
    }

    private void ShowHitGorilla(Vector3 position, Vector3 velocity, Vector2 direction, float speed)
    {
        GameObject hit = CreateCoconutPhysicsObject("Gorila golpeado", hitGorillaPrefab, hitGorillaMaterial, position, 2.05f, false, 0);
        Rigidbody body = hit.AddComponent<Rigidbody>();
        body.mass = 1.25f;
        body.useGravity = true;
        Vector3 push = new Vector3(direction.x, direction.y, 0f).normalized * Mathf.Clamp(speed, 1.2f, 3.4f);
        body.linearVelocity = velocity * .35f + push;
        body.angularVelocity = new Vector3(0f, 0f, -direction.x * 3.2f);
        temporaryPieces.Add(hit);
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

    private void UpdateCoconuts()
    {
        for (int i = coconuts.Count - 1; i >= 0; i--)
        {
            Coconut coconut = coconuts[i];
            if (coconut.gameObject == null) { coconuts.RemoveAt(i); continue; }
            if (coconut.gameObject.transform.position.y < -6f && Time.time - coconut.bornAt > .7f)
            {
                if (state == GameState.Playing && !coconut.hazard)
                {
                    missed++;
                    combo = 0;
                    callout = "SE ESCAPÓ";
                    calloutUntil = Time.time + .65f;
                }
                Destroy(coconut.gameObject);
                coconuts.RemoveAt(i);
            }
        }
    }

    private void UpdateTemporaryPieces()
    {
        for (int i = temporaryPieces.Count - 1; i >= 0; i--)
        {
            GameObject piece = temporaryPieces[i];
            if (piece == null || piece.transform.position.y < -6.5f)
            {
                if (piece != null) Destroy(piece);
                temporaryPieces.RemoveAt(i);
            }
        }
    }

    private void BeginCountdown()
    {
        session.ResetRound();
        ClearCoconuts();
        score = combo = bestCombo = missed = sliced = 0;
        lives = 3;
        lostByGorilla = false;
        rareGorillaSpawned = false;
        state = GameState.Countdown;
        stateStartedAt = Time.time;
    }

    private void StartRound()
    {
        state = GameState.Playing;
        stateStartedAt = Time.time;
        roundEndsAt = Time.time + RoundDuration;
        rareGorillaAt = Time.time + UnityEngine.Random.Range(15f, 28f);
        nextSpawn = Time.time + .25f;
        callout = "¡CORTA!";
        calloutUntil = Time.time + .8f;
    }

    private void StartFinale()
    {
        lostByGorilla = false;
        state = GameState.Finale;
        stateStartedAt = Time.time;
        callout = "¡TIEMPO!";
        calloutUntil = Time.time + 2f;
    }

    private void ClearCoconuts()
    {
        foreach (Coconut coconut in coconuts) if (coconut.gameObject != null) Destroy(coconut.gameObject);
        coconuts.Clear();
        foreach (GameObject piece in temporaryPieces) if (piece != null) Destroy(piece);
        temporaryPieces.Clear();
    }

    private Texture2D Solid(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private void EnsureUI()
    {
        if (dark != null) return;
        dark = Solid(new Color(.01f, .045f, .035f, .94f));
        gold = Solid(new Color(1f, .76f, .08f, .96f));
        turquoise = Solid(new Color(.03f, .82f, .78f, .95f));
        orange = Solid(new Color(1f, .34f, .08f, .95f));
        red = Solid(new Color(.90f, .08f, .04f, .95f));
        white = Solid(new Color(1f, 1f, 1f, .92f));
        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(Screen.height * .052f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1f, .82f, .15f) } };
        bigStyle = new GUIStyle(titleStyle) { fontSize = Mathf.RoundToInt(Screen.height * .105f), normal = { textColor = Color.white } };
        bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(Screen.height * .029f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true, normal = { textColor = Color.white } };
        scoreStyle = new GUIStyle(bodyStyle) { fontSize = Mathf.RoundToInt(Screen.height * .034f) };
    }

    private void OnGUI()
    {
        JungleUITheme.Ensure();
        EnsureUI();
        float w = Screen.width, h = Screen.height;

        GUI.DrawTexture(new Rect(w * .015f, h * .02f, w * .97f, h * .105f), dark);
        GUI.Label(new Rect(w * .05f, h * .025f, w * .44f, h * .085f), "COCONUT SLICE", titleStyle);
        GUI.Label(new Rect(w * .46f, h * .025f, w * .38f, h * .085f), $"PUNTOS {score}   COMBO x{combo}", scoreStyle);
        if (GUI.Button(new Rect(w * .85f, h * .035f, w * .12f, h * .065f), "MENÚ", JungleUITheme.Button)) SceneNavigator.Load(SceneNavigator.MainMenu);
        DrawLives(w, h);

        DrawHand(leftHand, turquoise, "IZQ");
        DrawHand(rightHand, orange, "DER");

        if (state == GameState.Intro) DrawIntro(w, h);
        else if (state == GameState.Countdown)
        {
            int count = Mathf.Clamp(3 - Mathf.FloorToInt(Time.time - stateStartedAt), 1, 3);
            GUI.DrawTexture(new Rect(w * .37f, h * .29f, w * .26f, h * .25f), dark);
            GUI.Label(new Rect(w * .38f, h * .30f, w * .24f, h * .23f), count.ToString(), bigStyle);
            GUI.Label(new Rect(w * .30f, h * .59f, w * .40f, h * .07f), "LEVANTA LAS MANOS", bodyStyle);
        }
        else if (state == GameState.Playing)
        {
            float remaining = Mathf.Max(0f, roundEndsAt - Time.time);
            GUI.DrawTexture(new Rect(w * .42f, h * .135f, w * .16f, h * .07f), remaining < 6f ? red : dark);
            GUI.Label(new Rect(w * .42f, h * .135f, w * .16f, h * .07f), Mathf.CeilToInt(remaining) + " s", scoreStyle);
        }
        else if (state == GameState.Finale) DrawFinale(w, h);
        else if (state == GameState.Results) DrawResults(w, h);

        if (Time.time < calloutUntil)
        {
            float pulse = 1f + Mathf.Sin((calloutUntil - Time.time) * 10f) * .04f;
            Rect box = new Rect(w * (.35f - (pulse - 1f) * .3f), h * .22f, w * .30f * pulse, h * .09f * pulse);
            GUI.DrawTexture(box, callout.Contains("ESCAP") || callout.Contains("VIDA") || callout.Contains("GORILA") ? red : gold);
            GUI.Label(box, callout, bodyStyle);
        }
        JungleUITheme.DrawLeaves();
    }

    private void DrawLives(float w, float h)
    {
        Rect backing = new Rect(w * .025f, h * .135f, w * .215f, h * .060f);
        GUI.DrawTexture(backing, dark);
        GUI.Label(new Rect(w * .032f, h * .140f, w * .065f, h * .05f), "VIDAS", bodyStyle);
        float segmentWidth = w * .036f;
        for (int i = 0; i < 3; i++)
        {
            Rect segment = new Rect(w * .101f + i * (segmentWidth + w * .009f), h * .149f, segmentWidth, h * .032f);
            GUI.DrawTexture(segment, i < lives ? red : dark);
            GUI.Box(segment, GUIContent.none);
        }
    }

    private void DrawIntro(float w, float h)
    {
        float entrance = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((Time.time - stateStartedAt) / .75f));
        Rect panel = new Rect(w * (.50f - .25f * entrance), h * .20f, w * .50f * entrance, h * .57f);
        GUI.DrawTexture(panel, dark);
        if (entrance < .65f) return;
        GUI.Label(new Rect(w * .29f, h * .23f, w * .42f, h * .09f), "¡EL GORILA TIENE HAMBRE!", titleStyle);
        GUI.DrawTexture(new Rect(w * .30f, h * .34f, w * .40f, h * .055f), turquoise);
        GUI.Label(new Rect(w * .29f, h * .325f, w * .42f, h * .09f), "1  MUEVE TUS MANOS FRENTE A LA CÁMARA", bodyStyle);
        GUI.DrawTexture(new Rect(w * .30f, h * .44f, w * .40f, h * .055f), orange);
        GUI.Label(new Rect(w * .29f, h * .425f, w * .42f, h * .09f), "2  HAZ UN CORTE RÁPIDO SOBRE LOS COCOS", bodyStyle);
        GUI.Label(new Rect(w * .27f, h * .535f, w * .46f, h * .07f), "USA AMBAS MANOS · EVITA CACTUS Y GORILAS", bodyStyle);
        if (GUI.Button(new Rect(w * .37f, h * .64f, w * .26f, h * .085f), "¡ESTOY LISTO!", JungleUITheme.Button)) BeginCountdown();
    }

    private void DrawFinale(float w, float h)
    {
        float t = Time.time - stateStartedAt;
        float scale = 1f + Mathf.Sin(t * 8f) * Mathf.Exp(-t * 1.5f) * .16f;
        Rect box = new Rect(w * (.50f - .22f * scale), h * .32f, w * .44f * scale, h * .22f * scale);
        GUI.DrawTexture(box, lostByGorilla ? red : gold);
        GUI.Label(box, lostByGorilla ? "¡PERDISTE!" : "¡TIEMPO!", bigStyle);
        GUI.Label(new Rect(w * .27f, h * .57f, w * .46f, h * .07f), lostByGorilla ? "CORTASTE AL GORILA POR ACCIDENTE" : "EL GORILA CUENTA TUS COCOS...", bodyStyle);
    }

    private void DrawResults(float w, float h)
    {
        string rank = score >= 5000 ? "REY DE LA SELVA" : score >= 3000 ? "NINJA GORILA" : score >= 1600 ? "CORTADOR TROPICAL" : "APRENDIZ DEL COCO";
        GUI.DrawTexture(new Rect(w * .27f, h * .19f, w * .46f, h * .59f), dark);
        GUI.Label(new Rect(w * .31f, h * .22f, w * .38f, h * .08f), "RESULTADO", titleStyle);
        GUI.DrawTexture(new Rect(w * .32f, h * .315f, w * .36f, h * .08f), gold);
        GUI.Label(new Rect(w * .32f, h * .315f, w * .36f, h * .08f), rank, bodyStyle);
        GUI.Label(new Rect(w * .32f, h * .41f, w * .36f, h * .15f), $"{score} PUNTOS\n{sliced} COCOS CORTADOS  ·  MEJOR COMBO x{bestCombo}\n{missed} COCOS ESCAPARON", bodyStyle);
        if (GUI.Button(new Rect(w * .34f, h * .63f, w * .20f, h * .085f), "OTRA RONDA", JungleUITheme.Button)) BeginCountdown();
        if (GUI.Button(new Rect(w * .56f, h * .63f, w * .10f, h * .085f), "MENÚ", JungleUITheme.Button)) SceneNavigator.Load(SceneNavigator.MainMenu);
    }

    private void DrawHand(Vector2 hand, Texture2D color, string label)
    {
        float size = Mathf.Max(34f, Screen.height * .06f);
        Rect rect = new Rect(hand.x * Screen.width - size * .5f, (1f - hand.y) * Screen.height - size * .5f, size, size);
        GUI.DrawTexture(rect, white);
        GUI.DrawTexture(new Rect(rect.x + 5, rect.y + 5, rect.width - 10, rect.height - 10), color);
        GUI.Label(rect, label, new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, normal = { textColor = Color.black } });
    }
}
