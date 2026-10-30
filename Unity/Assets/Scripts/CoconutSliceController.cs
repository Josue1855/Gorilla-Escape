using System;
using System.Collections.Generic;
using UnityEngine;
using GorillaEscape.Gameplay.Input;

[RequireComponent(typeof(JunglePanel))]
public sealed partial class CoconutSliceController : MonoBehaviour, IJungleScreen
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
        public bool isKeeperShot;
        public int cornerIndex;
        public Vector3 keeperStart;
        public Vector3 keeperTarget;
        public float keeperDuration;
    }

    private const float RoundDuration = 45f;
    private readonly List<Coconut> coconuts = new List<Coconut>();
    private readonly List<GameObject> temporaryPieces = new List<GameObject>();
    private readonly Dictionary<int, Stack<GameObject>> objectPools = new Dictionary<int, Stack<GameObject>>();
    private readonly Dictionary<GameObject, int> instancePoolKeys = new Dictionary<GameObject, int>();
    private readonly HandDwellNavigator uiHandNavigator = new HandDwellNavigator();
    private readonly HandDwellTarget[] dwellTargets = new HandDwellTarget[2];
    private MotionSession session;
    private bool keeperMode;
    private GameState state = GameState.Intro;
    private Vector2 leftHand = new Vector2(.32f, .52f), rightHand = new Vector2(.68f, .52f), torsoPos = new Vector2(.50f, .42f);
    private bool leftTracked, rightTracked, torsoTracked;
    private long latestRevision = -1, authorizedSaveRevision = -1;
    private readonly HandVisualPrediction leftVisual = new HandVisualPrediction(), rightVisual = new HandVisualPrediction();
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
    private bool lostByLives;
    private float rareGorillaAt;
    private int repeatedDistractorType;
    private int lastCountdownBeep = -1;
    private AudioSource sfxSource;
    private AudioClip sliceClip, hazardClip;

    private void Awake()
    {
        keeperMode = SceneNavigator.IsJungleKeeper;
        session = MotionSession.Ensure();
        session.SetCameraOnly(true);
        session.Connect();
        BuildStage();
        SetupSliceAudio();
        LoadCoconutModels();
        PrewarmSlicePools();
        if (keeperMode) BuildKeeperGoalFrame();
        else LoadDistractorGorillas();
        nextDistractorAt = Time.time + UnityEngine.Random.Range(2.5f, 5f);
        previousLeft = leftHand;
        previousRight = rightHand;
        stateStartedAt = Time.time;
    }

    private void Update()
    {
        // End the round before consuming a new input frame or spawning a wave.
        EndRoundIfExpired();
        float? roundProgress = state == GameState.Playing ? Mathf.Clamp01(1f - (roundEndsAt - Time.time) / RoundDuration) : (float?)null;
        session.PresentGame(state == GameState.Intro ? "Ready" : state == GameState.Countdown ? "Countdown" : state == GameState.Playing ? "Playing" : "Finished", score, progress: roundProgress);
        bool needsTracking = state == GameState.Countdown || state == GameState.Playing;
        if (needsTracking && !session.IsInputReady)
        {
            leftSpeed = rightSpeed = 0;
            return;
        }
        if (needsTracking && session.TryConsumeFrame(out var frame)) ReadPose(frame);
        if (!keeperMode) UpdateDistractorGorilla();

        if (state == GameState.Countdown)
        {
            int c = Mathf.Clamp(3 - Mathf.FloorToInt(Time.time - stateStartedAt), 1, 3);
            if (c != lastCountdownBeep) { lastCountdownBeep = c; if (sfxSource != null) sfxSource.PlayOneShot(JungleAudioBank.CountdownTick, .6f); }
            if (Time.time - stateStartedAt >= 3.2f) StartRound();
        }
        else if (state == GameState.Playing)
        {
            if (Time.time >= nextSpawn)
            {
                if (keeperMode) SpawnKeeperWave();
                else SpawnWave();
            }
        }
        else if (state == GameState.Finale && Time.time - stateStartedAt >= 3.4f)
        {
            ClearCoconuts();
            state = GameState.Results;
            session.SetGameplayActive(false);
            stateStartedAt = Time.time;
        }

        if (keeperMode) UpdateKeeperShots();
        else UpdateCoconuts();
        UpdateTemporaryPieces();
    }

    private void EndRoundIfExpired()
    {
        if (state == GameState.Playing && Time.time >= roundEndsAt) StartFinale();
    }

    private Vector2 VisualHand(HandVisualPrediction prediction, Vector2 observed) =>
        JungleUITheme.VisualHand(session, prediction, observed);

    private void ReadPose(JungleFusedFrame frame)
    {
        if (!frame.NewPose && !frame.Baseline) return;
        leftTracked = frame.Pose != null && frame.Pose.leftDetected;
        rightTracked = frame.Pose != null && frame.Pose.rightDetected;
        torsoTracked = frame.Pose != null && (leftTracked || rightTracked);
        if (frame.Pose != null)
            torsoPos = new Vector2(Mathf.Clamp(frame.Pose.bodyX, .12f, .88f), Mathf.Clamp(1f - frame.Pose.bodyY, .18f, .78f));
        latestRevision = frame.Revision;
        Vector2 nextLeft = new Vector2(frame.Left.X, 1f - frame.Left.Y);
        Vector2 nextRight = new Vector2(frame.Right.X, 1f - frame.Right.Y);
        leftSpeed = frame.Left.Speed; rightSpeed = frame.Right.Speed;
        if (!frame.Baseline && !keeperMode)
        {
            if (leftTracked && (frame.Actions & JungleAction.SliceLeft) != 0) TrySliceSegment(previousLeft, nextLeft, leftSpeed, JungleAction.SliceLeft, frame.Revision);
            if (rightTracked && (frame.Actions & JungleAction.SliceRight) != 0) TrySliceSegment(previousRight, nextRight, rightSpeed, JungleAction.SliceRight, frame.Revision);
        }
        if (leftTracked) { leftHand = previousLeft = nextLeft; leftVisual.Observe(nextLeft.x, nextLeft.y, frame.Left.VelocityX, -frame.Left.VelocityY, Time.unscaledTime); }
        else leftVisual.Reset();
        if (rightTracked) { rightHand = previousRight = nextRight; rightVisual.Observe(nextRight.x, nextRight.y, frame.Right.VelocityX, -frame.Right.VelocityY, Time.unscaledTime); }
        else rightVisual.Reset();
    }

    private void TrySliceSegment(Vector2 from, Vector2 to, float speed, JungleAction action, long revision)
    {
        float previousSlice = action == JungleAction.SliceLeft ? lastLeftSliceAt : lastRightSliceAt;
        if (state != GameState.Playing || Time.time - previousSlice < .045f) return;
        if (!session.TryAuthorize(action, revision)) return;
        for (int i = coconuts.Count - 1; i >= 0; i--)
        {
            Coconut coconut = coconuts[i];
            if (coconut.sliced) continue;
            Vector3 viewport3 = Camera.main.WorldToViewportPoint(coconut.gameObject.transform.position);
            if (viewport3.z <= 0f) continue;
            Vector2 viewport = new Vector2(viewport3.x, viewport3.y);
            const float hitRadius = .075f;
            float distance = DistanceToSegment(viewport, from, to);
            if (distance < hitRadius)
            {
                Slice(coconut, to - from, speed, ShotMechanics.SlicePrecisionMultiplier(distance, hitRadius));
                if (action == JungleAction.SliceLeft) lastLeftSliceAt = Time.time; else lastRightSliceAt = Time.time;
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
        GameObject visual = AcquireCoconutPhysicsObject(10, "Gorila hecho bolita - NO CORTAR", sleepingGorillaPrefab, sleepingGorillaMaterial, position, 2.15f, false, 0, 1.15f);
        visual.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        Rigidbody body = visual.GetComponent<Rigidbody>();
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
        GameObject visual = AcquireCoconutPhysicsObject(hazard ? 2 : 1, hazard ? "Cactus peligroso" : "Coco entero", hazard ? cactusPrefab : wholeCoconutPrefab, hazard ? cactusModelMaterial : wholeCoconutModelMaterial, position, hazard ? 1.02f : .86f, false, 0, hazard ? .78f : .65f);
        Rigidbody body = visual.GetComponent<Rigidbody>();
        body.linearVelocity = new Vector3(Mathf.Clamp(-x * .30f + UnityEngine.Random.Range(-1.7f, 1.7f), -3.5f, 3.5f), UnityEngine.Random.Range(9.4f, 12.2f), 0f);
        body.angularVelocity = UnityEngine.Random.insideUnitSphere * 8f;
        coconuts.Add(new Coconut { gameObject = visual, body = body, bornAt = Time.time, hazard = hazard });
    }

    private void Slice(Coconut coconut, Vector2 direction, float speed, float precisionMultiplier)
    {
        coconut.sliced = true;
        Vector3 position = coconut.gameObject.transform.position;
        Vector3 velocity = coconut.body.linearVelocity;
        ReleaseCoconutPhysicsObject(coconut.gameObject);
        coconuts.Remove(coconut);

        if (coconut.instantLoss)
        {
            if (sfxSource != null && hazardClip != null) sfxSource.PlayOneShot(hazardClip, .85f);
            JungleStageBuilder.ShakeCamera(.18f, .26f);
            ShowHitGorilla(position, velocity, direction, speed);
            lives = 0;
            combo = 0;
            lostByGorilla = true;
            callout = "¡GOLPEASTE AL GORILA!";
            calloutUntil = Time.time + 2.4f;
            state = GameState.Finale;
            session.SetGameplayActive(false);
            stateStartedAt = Time.time;
            return;
        }

        Vector3 split = new Vector3(direction.y, -direction.x, 0f).normalized;
        for (int side = -1; side <= 1; side += 2)
        {
            int poolKey = coconut.hazard ? (side < 0 ? 5 : 6) : (side < 0 ? 3 : 4);
            GameObject half = AcquireCoconutPhysicsObject(poolKey, coconut.hazard ? "Medio cactus" : (side < 0 ? "Medio coco A" : "Medio coco B"), coconut.hazard ? halfCactusPrefab : halfCoconutPrefab, coconut.hazard ? halfCactusModelMaterial : halfCoconutModelMaterial, position + split * side * .18f, coconut.hazard ? .82f : .72f, true, side, coconut.hazard ? .37f : .31f);
            Rigidbody body = half.GetComponent<Rigidbody>();
            body.linearVelocity = velocity * .72f + split * side * Mathf.Clamp(speed, 1.4f, 4.5f);
            body.angularVelocity = UnityEngine.Random.insideUnitSphere * 12f;
            temporaryPieces.Add(half);
            JungleSquashStretch.Apply(half, 1.25f, 0.75f, 0.22f);
        }

        if (coconut.hazard)
        {
            if (sfxSource != null && hazardClip != null) sfxSource.PlayOneShot(hazardClip, .75f);
            JungleStageBuilder.ShakeCamera(.15f, .20f);
            lives = Mathf.Max(0, lives - 1);
            combo = 0;
            callout = lives > 0 ? "¡OUCH!  VIDA -1" : "¡SIN VIDAS!";
            calloutUntil = Time.time + 1.1f;
            if (lives == 0)
            {
                state = GameState.Finale;
                session.SetGameplayActive(false);
                stateStartedAt = Time.time;
                lostByLives = true;
            }
        }
        else
        {
            if (sfxSource != null && sliceClip != null) sfxSource.PlayOneShot(sliceClip, .65f);
            JungleStageBuilder.ShakeCamera(.045f, .08f);
            sliced++;
            combo++;
            bestCombo = Mathf.Max(bestCombo, combo);
            int gained = Mathf.RoundToInt(FruitPointsForCombo(combo) * precisionMultiplier);
            score += gained;
            callout = "+" + gained + " · PRECISIÓN x" + precisionMultiplier.ToString("F2") + (combo >= 2 ? " · COMBO " + combo : "");
            calloutUntil = Time.time + .65f;
        }
    }

    private static int FruitPointsForCombo(int currentCombo)
    {
        return 100 + Mathf.Clamp(currentCombo, 0, 20) * 15;
    }

    private void PlaySliceSound(bool hazard)
    {
        if (sfxSource == null) return;
        AudioClip clip = hazard ? hazardClip : sliceClip;
        if (clip != null) sfxSource.PlayOneShot(clip, hazard ? .75f : .65f);
    }

    private void ShowHitGorilla(Vector3 position, Vector3 velocity, Vector2 direction, float speed)
    {
        GameObject hit = AcquireCoconutPhysicsObject(11, "Gorila golpeado", hitGorillaPrefab, hitGorillaMaterial, position, 2.05f, false, 0, 1.25f);
        Rigidbody body = hit.GetComponent<Rigidbody>();
        Vector3 push = new Vector3(direction.x, direction.y, 0f).normalized * Mathf.Clamp(speed, 1.2f, 3.4f);
        body.linearVelocity = velocity * .35f + push;
        body.angularVelocity = new Vector3(0f, 0f, -direction.x * 3.2f);
        temporaryPieces.Add(hit);
    }

    private void PrewarmSlicePools()
    {
        for (int i = 0; i < 6; i++)
        {
            ReleaseCoconutPhysicsObject(AcquireCoconutPhysicsObject(1, "Coco entero", wholeCoconutPrefab, wholeCoconutModelMaterial, Vector3.zero, .86f, false, 0, .65f));
            if (!keeperMode)
            {
                ReleaseCoconutPhysicsObject(AcquireCoconutPhysicsObject(3, "Medio coco A", halfCoconutPrefab, halfCoconutModelMaterial, Vector3.zero, .72f, true, -1, .31f));
                ReleaseCoconutPhysicsObject(AcquireCoconutPhysicsObject(4, "Medio coco B", halfCoconutPrefab, halfCoconutModelMaterial, Vector3.zero, .72f, true, 1, .31f));
            }
        }
        for (int i = 0; i < 3; i++)
        {
            ReleaseCoconutPhysicsObject(AcquireCoconutPhysicsObject(2, "Cactus peligroso", cactusPrefab, cactusModelMaterial, Vector3.zero, 1.02f, false, 0, .78f));
            if (!keeperMode)
            {
                ReleaseCoconutPhysicsObject(AcquireCoconutPhysicsObject(5, "Medio cactus", halfCactusPrefab, halfCactusModelMaterial, Vector3.zero, .82f, true, -1, .37f));
                ReleaseCoconutPhysicsObject(AcquireCoconutPhysicsObject(6, "Medio cactus", halfCactusPrefab, halfCactusModelMaterial, Vector3.zero, .82f, true, 1, .37f));
            }
        }
    }

    private GameObject AcquireCoconutPhysicsObject(int poolKey, string objectName, GameObject prefab, Material material, Vector3 position, float desiredSize, bool isHalf, int side, float mass)
    {
        if (objectPools.TryGetValue(poolKey, out Stack<GameObject> pool))
        {
            while (pool.Count > 0)
            {
                GameObject item = pool.Pop();
                if (item == null) continue;
                item.name = objectName;
                item.transform.SetPositionAndRotation(position, Quaternion.identity);
                item.transform.localScale = Vector3.one;
                item.SetActive(true);
                Rigidbody existingBody = item.GetComponent<Rigidbody>();
                if (existingBody != null)
                {
                    existingBody.isKinematic = false;
                    existingBody.mass = mass;
                    existingBody.useGravity = true;
                    existingBody.interpolation = RigidbodyInterpolation.Interpolate;
                    existingBody.linearVelocity = Vector3.zero;
                    existingBody.angularVelocity = Vector3.zero;
                }
                return item;
            }
        }
        GameObject created = CreateCoconutPhysicsObject(objectName, prefab, material, position, desiredSize, isHalf, side);
        if (!created.TryGetComponent<Rigidbody>(out Rigidbody body))
            body = created.AddComponent<Rigidbody>();
        body.isKinematic = false;
        body.mass = mass;
        body.useGravity = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        instancePoolKeys[created] = poolKey;
        return created;
    }

    private void ReleaseCoconutPhysicsObject(GameObject item)
    {
        if (item == null) return;
        if (!instancePoolKeys.TryGetValue(item, out int poolKey))
        {
            Destroy(item);
            return;
        }
        Rigidbody body = item.GetComponent<Rigidbody>();
        if (body != null)
        {
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            body.isKinematic = false;
            body.useGravity = true;
        }
        item.transform.localScale = Vector3.one;
        item.SetActive(false);
        if (!objectPools.TryGetValue(poolKey, out Stack<GameObject> pool))
        {
            pool = new Stack<GameObject>();
            objectPools[poolKey] = pool;
        }
        pool.Push(item);
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
                ReleaseCoconutPhysicsObject(coconut.gameObject);
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
                if (piece != null) ReleaseCoconutPhysicsObject(piece);
                temporaryPieces.RemoveAt(i);
            }
        }
    }

    public void BeginFromPhone(){if(state==GameState.Intro)BeginCountdown();}
    private void BeginCountdown()
    {
        JungleAudioBank.PlayUiClick();
        uiHandNavigator.Reset();
        session.SetGameplayActive(true);
        session.ResetRound();
        ClearCoconuts();
        score = combo = bestCombo = missed = sliced = 0;
        lastCountdownBeep = -1;
        lives = 3;
        lostByGorilla = false;
        lostByLives = false;
        rareGorillaSpawned = false;
        callout = "";
        calloutUntil = 0f;
        lastLeftSliceAt = lastRightSliceAt = 0f;
        authorizedSaveRevision = -1;
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
        callout = keeperMode ? "¡CUBRE EL ARCO!" : "¡CORTA!";
        calloutUntil = Time.time + .8f;
        if (sfxSource != null) sfxSource.PlayOneShot(JungleAudioBank.CountdownGo, .7f);
    }

    private void StartFinale()
    {
        lostByGorilla = false;
        state = GameState.Finale;
        session?.SetGameplayActive(false);
        stateStartedAt = Time.time;
        callout = "¡TIEMPO!";
        calloutUntil = Time.time + 2f;
    }

    private void ClearCoconuts()
    {
        foreach (Coconut coconut in coconuts) if (coconut.gameObject != null) ReleaseCoconutPhysicsObject(coconut.gameObject);
        coconuts.Clear();
        foreach (GameObject piece in temporaryPieces) if (piece != null) ReleaseCoconutPhysicsObject(piece);
        temporaryPieces.Clear();
    }

    private int cachedScore = -1, cachedSliced = -1, cachedCombo = -1, cachedTimerSec = -1;
    private int cachedResultScore = -1, cachedResultSliced = -1, cachedResultCombo = -1, cachedResultMissed = -1;
    private string cachedHudScoreText = "", cachedTimerText = "", cachedResultSummary = "";

    public void RenderUI()
    {
        JungleUITheme.Ensure();
        float w = Screen.width, h = Screen.height;

        if (score != cachedScore || sliced != cachedSliced || combo != cachedCombo)
        {
            cachedScore = score;
            cachedSliced = sliced;
            cachedCombo = combo;
            cachedHudScoreText = keeperMode
                ? $"PUNTOS {score}   ATAJADAS {sliced}   RACHA x{combo}"
                : $"PUNTOS {score}   COMBO x{combo}";
        }

        JungleUITheme.DrawPanel(new Rect(w * .015f, h * .02f, w * .97f, h * .108f));
        JungleUITheme.DrawShadowedLabel(new Rect(w * .045f, h * .025f, w * .42f, h * .085f), keeperMode ? "PORTERO DE LA JUNGLA" : "COCONUT SLICE", JungleUITheme.HudTitle, 3f);
        JungleUITheme.DrawShadowedLabel(new Rect(w * .45f, h * .025f, w * .38f, h * .085f), cachedHudScoreText, JungleUITheme.HudScore, 2f);
        Rect menuRect = new Rect(w * .845f, h * .035f, w * .125f, h * .065f);
        if (JungleRetainedUI.Button(menuRect, "MENÚ", JungleUITheme.Button)) { JungleAudioBank.PlayUiClick(); SceneNavigator.Load(SceneNavigator.MainMenu); }
        DrawLives(w, h);

        if (keeperMode && state == GameState.Playing) DrawKeeperCornerReticles(w, h);

        if (session.IsInputReady)
        {
            Vector2 vLeft = VisualHand(leftVisual, leftHand);
            Vector2 vRight = VisualHand(rightVisual, rightHand);
            if (leftTracked) DrawHand(vLeft, JungleUITheme.Turquoise, keeperMode ? "GUANTE IZQ" : "IZQ");
            if (rightTracked) DrawHand(vRight, JungleUITheme.Orange, keeperMode ? "GUANTE DER" : "DER");
            if (keeperMode && torsoTracked) DrawHand(torsoPos, JungleUITheme.Gold, "PECHO");
        }

        if (state == GameState.Intro) DrawIntro(w, h, menuRect);
        else if (state == GameState.Countdown)
        {
            int count = Mathf.Clamp(3 - Mathf.FloorToInt(Time.time - stateStartedAt), 1, 3);
            JungleUITheme.DrawPanel(new Rect(w * .37f, h * .29f, w * .26f, h * .25f), selected: true);
            JungleUITheme.DrawShadowedLabel(new Rect(w * .38f, h * .30f, w * .24f, h * .23f), count == 3 ? "3" : count == 2 ? "2" : "1", JungleUITheme.HudBig, 3.5f);
            JungleUITheme.DrawShadowedLabel(new Rect(w * .30f, h * .59f, w * .40f, h * .07f), keeperMode ? "PREPARA MANOS Y TORSO" : "LEVANTA LAS MANOS", JungleUITheme.HudBody, 2f);
        }
        else if (state == GameState.Playing)
        {
            float remaining = Mathf.Max(0f, roundEndsAt - Time.time);
            int remSec = Mathf.CeilToInt(remaining);
            if (remSec != cachedTimerSec)
            {
                cachedTimerSec = remSec;
                cachedTimerText = remSec + " s";
            }
            Rect timerRect = new Rect(w * .42f, h * .138f, w * .16f, h * .07f);
            if (remaining < 6f) JungleRetainedUI.DrawTexture(timerRect, JungleUITheme.Red);
            else JungleUITheme.DrawSubPanel(timerRect);
            JungleUITheme.DrawShadowedLabel(timerRect, cachedTimerText, JungleUITheme.HudScore, 2f);
        }
        else if (state == GameState.Finale) DrawFinale(w, h);
        else if (state == GameState.Results) DrawResults(w, h);

        if (Time.time < calloutUntil)
        {
            float pulse = 1f + Mathf.Sin((calloutUntil - Time.time) * 10f) * .04f;
            Rect box = new Rect(w * (.33f - (pulse - 1f) * .3f), h * .22f, w * .34f * pulse, h * .09f * pulse);
            bool neg = callout.Contains("ESCAP") || callout.Contains("VIDA") || callout.Contains("GORILA") || callout.Contains("OUCH");
            if (neg) JungleRetainedUI.DrawTexture(box, JungleUITheme.Red);
            else JungleUITheme.DrawPanel(box, selected: true);
            JungleUITheme.DrawShadowedLabel(box, callout, JungleUITheme.HudBody, 2f);
        }
        JungleUITheme.DrawLeaves();
    }

    private void DrawLives(float w, float h)
    {
        Rect backing = new Rect(w * .025f, h * .138f, w * .235f, h * .062f);
        JungleUITheme.DrawSubPanel(backing);
        JungleUITheme.DrawShadowedLabel(new Rect(w * .034f, h * .143f, w * .082f, h * .05f), "VIDAS", JungleUITheme.HudBody, 1.5f);
        float segmentWidth = w * .035f;
        for (int i = 0; i < 3; i++)
        {
            Rect segment = new Rect(w * .118f + i * (segmentWidth + w * .009f), h * .152f, segmentWidth, h * .032f);
            JungleRetainedUI.DrawTexture(segment, i < lives ? JungleUITheme.Red : JungleUITheme.DarkHud);
            JungleRetainedUI.Box(segment, GUIContent.none);
        }
    }

    private void DrawIntro(float w, float h, Rect menuRect)
    {
        float entrance = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((Time.time - stateStartedAt) / .75f));
        Rect panel = new Rect(w * (.50f - .28f * entrance), h * .185f, w * .56f * entrance, h * .60f);
        JungleUITheme.DrawPanel(panel);
        if (entrance < .65f) return;
        JungleUITheme.DrawShadowedLabel(new Rect(w * .25f, h * .215f, w * .50f, h * .09f), keeperMode ? "¡ATAJA LOS TIROS DEL GORILA!" : "¡EL GORILA TIENE HAMBRE!", JungleUITheme.HudTitle, 3f);
        JungleUITheme.DrawPlankLabel(new Rect(w * .26f, h * .325f, w * .48f, h * .078f), keeperMode ? "1 · USA AMBAS MANOS PARA CUBRIR LAS 6 ESQUINAS" : "1 · MUEVE TUS MANOS FRENTE A LA CÁMARA");
        JungleUITheme.DrawPlankLabel(new Rect(w * .26f, h * .425f, w * .48f, h * .078f), keeperMode ? "2 · USA TU TORSO AL CENTRO Y ESQUIVA CACTUS" : "2 · HAZ UN CORTE RÁPIDO SOBRE LOS COCOS");
        JungleUITheme.DrawShadowedLabel(new Rect(w * .25f, h * .525f, w * .50f, h * .07f), keeperMode ? "CÁMARA DE CUERPO SUPERIOR · MANOS + TORSO" : "USA AMBAS MANOS · EVITA CACTUS Y GORILAS", JungleUITheme.HudBody, 2f);

        Rect readyRect = new Rect(w * .36f, h * .635f, w * .28f, h * .085f);
        bool hasHand = session.TryCameraCursor(out Vector2 normCursor, out string handLabel, out bool handRaised);
        Vector2 cursorPx = new Vector2(normCursor.x * w, normCursor.y * h);
        dwellTargets[0] = new HandDwellTarget(1, readyRect.x, readyRect.y, readyRect.width, readyRect.height);
        dwellTargets[1] = new HandDwellTarget(2, menuRect.x, menuRect.y, menuRect.width, menuRect.height);
        bool dwellFired = uiHandNavigator.ObserveCursor(hasHand, cursorPx.x, cursorPx.y, Time.unscaledTime, dwellTargets, out int triggeredId)
            && triggeredId > 0;

        if (JungleRetainedUI.Button(readyRect, "¡ESTOY LISTO!", uiHandNavigator.HoveredId == 1 ? JungleUITheme.ButtonSelected : JungleUITheme.Button)
            || (dwellFired && uiHandNavigator.HoveredId == 1))
        {
            BeginCountdown();
            return;
        }
        if (uiHandNavigator.HoveredId == 1 && uiHandNavigator.Progress > .02f)
            JungleUITheme.DrawDwellOverlay(readyRect, uiHandNavigator.Progress, "MANTÉN LA MANO AQUÍ");
        if (dwellFired && uiHandNavigator.HoveredId == 2)
        {
            uiHandNavigator.Reset();
            SceneNavigator.Load(SceneNavigator.MainMenu);
            return;
        }
        JungleUITheme.DrawNavigatorHandCursor(uiHandNavigator, handLabel, handRaised);
    }

    private void DrawFinale(float w, float h)
    {
        float t = Time.time - stateStartedAt;
        float scale = 1f + Mathf.Sin(t * 8f) * Mathf.Exp(-t * 1.5f) * .16f;
        Rect box = new Rect(w * (.50f - .22f * scale), h * .32f, w * .44f * scale, h * .22f * scale);
        bool lost = lostByGorilla || lostByLives;
        if (lost) JungleRetainedUI.DrawTexture(box, JungleUITheme.Red);
        else JungleUITheme.DrawPanel(box, selected: true);
        JungleUITheme.DrawShadowedLabel(box, lost ? "¡PERDISTE!" : "¡TIEMPO!", JungleUITheme.HudBig, 3.5f);
        JungleUITheme.DrawShadowedLabel(new Rect(w * .27f, h * .57f, w * .46f, h * .07f), lostByGorilla ? "CORTASTE AL GORILA POR ACCIDENTE" : lostByLives ? "TE QUEDASTE SIN VIDAS" : (keeperMode ? "EL ÁRBITRO CUENTA TUS ATAJADAS..." : "EL GORILA CUENTA TUS COCOS..."), JungleUITheme.HudBody, 2f);
    }

    private void DrawResults(float w, float h)
    {
        string rank = keeperMode
            ? (score >= 4500 ? "MURO DE LA SELVA" : score >= 2800 ? "PORTERO LEGENDARIO" : score >= 1400 ? "GUARDIÁN DEL ARCO" : "APRENDIZ DEL ARCO")
            : (score >= 5000 ? "REY DE LA SELVA" : score >= 3000 ? "NINJA GORILA" : score >= 1600 ? "CORTADOR TROPICAL" : "APRENDIZ DEL COCO");
        JungleUITheme.DrawPanel(new Rect(w * .26f, h * .19f, w * .48f, h * .59f));
        JungleUITheme.DrawShadowedLabel(new Rect(w * .30f, h * .22f, w * .40f, h * .08f), "RESULTADO", JungleUITheme.HudTitle, 3f);
        JungleUITheme.DrawBadge(new Rect(w * .31f, h * .315f, w * .38f, h * .08f), rank, highlighted: true);
        if (score != cachedResultScore || sliced != cachedResultSliced || bestCombo != cachedResultCombo || missed != cachedResultMissed)
        {
            cachedResultScore = score;
            cachedResultSliced = sliced;
            cachedResultCombo = bestCombo;
            cachedResultMissed = missed;
            cachedResultSummary = keeperMode
                ? $"{score} PUNTOS\n{sliced} ATAJADAS  ·  MEJOR RACHA x{bestCombo}\n{missed} GOLES RECIBIDOS"
                : $"{score} PUNTOS\n{sliced} COCOS CORTADOS  ·  MEJOR COMBO x{bestCombo}\n{missed} COCOS ESCAPARON";
        }
        JungleUITheme.DrawShadowedLabel(new Rect(w * .30f, h * .41f, w * .40f, h * .15f), cachedResultSummary, JungleUITheme.HudBody, 2f);

        Rect replayRect = new Rect(w * .31f, h * .63f, w * .22f, h * .085f);
        Rect menuBtnRect = new Rect(w * .55f, h * .63f, w * .14f, h * .085f);
        bool hasHand = session.TryCameraCursor(out Vector2 normCursor, out string handLabel, out bool handRaised);
        Vector2 cursorPx = new Vector2(normCursor.x * w, normCursor.y * h);
        dwellTargets[0] = new HandDwellTarget(1, replayRect.x, replayRect.y, replayRect.width, replayRect.height);
        dwellTargets[1] = new HandDwellTarget(2, menuBtnRect.x, menuBtnRect.y, menuBtnRect.width, menuBtnRect.height);
        bool dwellFired = uiHandNavigator.ObserveCursor(hasHand, cursorPx.x, cursorPx.y, Time.unscaledTime, dwellTargets, out int triggeredId)
            && triggeredId > 0;

        if (JungleRetainedUI.Button(replayRect, "OTRA RONDA", uiHandNavigator.HoveredId == 1 ? JungleUITheme.ButtonSelected : JungleUITheme.Button)
            || (dwellFired && uiHandNavigator.HoveredId == 1))
        {
            BeginCountdown();
            return;
        }
        if (uiHandNavigator.HoveredId == 1 && uiHandNavigator.Progress > .02f)
            JungleUITheme.DrawDwellOverlay(replayRect, uiHandNavigator.Progress);

        if (JungleRetainedUI.Button(menuBtnRect, "MENÚ", uiHandNavigator.HoveredId == 2 ? JungleUITheme.ButtonSelected : JungleUITheme.Button)
            || (dwellFired && uiHandNavigator.HoveredId == 2))
        {
            uiHandNavigator.Reset();
            SceneNavigator.Load(SceneNavigator.MainMenu);
            return;
        }
        if (uiHandNavigator.HoveredId == 2 && uiHandNavigator.Progress > .02f)
            JungleUITheme.DrawDwellOverlay(menuBtnRect, uiHandNavigator.Progress);

        JungleUITheme.DrawNavigatorHandCursor(uiHandNavigator, handLabel, handRaised);
    }

    private void DrawHand(Vector2 hand, Texture2D color, string label) =>
        JungleUITheme.DrawGameHandSquare(hand, color, label, invertY: true);
}
