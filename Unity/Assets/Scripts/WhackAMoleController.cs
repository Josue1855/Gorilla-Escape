using System;
using UnityEngine;
using GorillaEscape.Gameplay.Input;

[RequireComponent(typeof(JunglePanel))]
public sealed class WhackAMoleController : MonoBehaviour, IJungleScreen
{
    private enum State { Intro, Countdown, Playing, Results }

    private readonly Vector2[] slots =
    {
        new(.385f, .625f), new(.5f, .635f), new(.615f, .625f),
        new(.38f, .485f),  new(.5f, .495f), new(.62f, .485f),
        new(.372f, .34f),  new(.5f, .35f),  new(.628f, .34f)
    };

    private MotionSession session;
    private State state;
    private GameObject table, hammer, head, hitHead;
    private Transform hammerPivot, hitHeadPivot;
    private GameObject tablePrefab, headPrefab, hitPrefab, hammerPrefab;
    private Material tableMat, headMat, hitMat, hammerMat;
    private Vector2 cursorViewport = new(.5f, .5f), activeSlot;
    private float downSpeed, phonePower, stateAt, endAt, nextHeadAt, headAt, headUntil, hitUntil, lastHit;
    private long inputRevision;
    private bool whackRequested, headActive, hitHeadActive;
    private int score, hits, misses, combo, bestCombo;
    private int cachedScore = -1, cachedCombo = -1, cachedTimerSec = -1, cachedResScore = -1, cachedResHits = -1, cachedResMisses = -1, cachedResCombo = -1;
    private int lastCountdownBeep = -1;
    private string cachedHudText = "", cachedTimerText = "", cachedResultText = "";
    private string message = "";
    private AudioSource sfxSource;
    private AudioClip whackClip, popClip, tickClip, goClip;
    private Vector2 HandViewport => cursorViewport;
    private readonly HandVisualPrediction handVisual = new HandVisualPrediction();
    private Vector2 VisualHand => JungleUITheme.VisualHand(session, handVisual, cursorViewport);

    private void Awake()
    {
        session = MotionSession.Ensure();
        session.ConfigureForScene(SceneNavigator.WhackAMole);
        LoadAssets();
        BuildScene();
        SetupWhackAudio();
        stateAt = Time.time;
    }

    private void SetupWhackAudio()
    {
        sfxSource = JungleAudioBank.Attach2DSource(gameObject);
        whackClip = JungleAudioBank.WhackHit;
        popClip = JungleAudioBank.WhackPop;
        tickClip = JungleAudioBank.CountdownTick;
        goClip = JungleAudioBank.CountdownGo;
    }

    private Material Mat(Texture2D t, string n) => JungleStageBuilder.CreateTexturedMaterial(t, n);

    private void LoadAssets()
    {
        const string r = "Jungle/NewModels/WhackAMole/",
            tr = r + "Meshy_AI_model_20261026_texture_fbx/",
            hr = r + "Meshy_AI_Cheeky_Monkey_20261026_texture_fbx/",
            ir = r + "Meshy_AI_Dizzy_Monkey_20261026_texture_fbx/",
            mr = r + "Meshy_AI_Wooden_War_Drum_20261026_texture_fbx/";
        tablePrefab = JungleAssets.Load<GameObject>(tr + "Meshy_AI_model_20261026_texture");
        headPrefab = JungleAssets.Load<GameObject>(hr + "Meshy_AI_Cheeky_Monkey_20261026_texture");
        hitPrefab = JungleAssets.Load<GameObject>(ir + "Meshy_AI_Dizzy_Monkey_20261026_texture");
        hammerPrefab = JungleAssets.Load<GameObject>(mr + "Meshy_AI_Wooden_War_Drum_20261026_texture");
        tableMat = Mat(JungleAssets.Load<Texture2D>(tr + "Meshy_AI_model_20261026_texture"), "Mesa");
        headMat = Mat(JungleAssets.Load<Texture2D>(hr + "Meshy_AI_Cheeky_Monkey_20261026_texture"), "Cabeza");
        hitMat = Mat(JungleAssets.Load<Texture2D>(ir + "Meshy_AI_Dizzy_Monkey_20261026_texture"), "Golpeado");
        hammerMat = Mat(JungleAssets.Load<Texture2D>(mr + "Meshy_AI_Wooden_War_Drum_20261026_texture"), "Mazo");
    }

    private void BuildScene()
    {
        Camera c = Camera.main;
        c.orthographic = false;
        c.fieldOfView = 48;
        c.transform.position = new(0, 3.15f, -11.2f);
        c.transform.LookAt(new Vector3(0, -.65f, .25f));
        c.backgroundColor = new(.08f, .18f, .13f);
        var bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bg.name = "01 Fondo";
        bg.transform.SetParent(transform, false);
        bg.transform.position = new(0, 1.2f, 7.2f);
        bg.transform.localScale = new(31, 17.4f, 1);
        if (bg.TryGetComponent<Collider>(out var bgCol)) { bgCol.enabled = false; Destroy(bgCol); }
        var unlitShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
        var bm = new Material(unlitShader);
        var bgTex = JungleAssets.Load<Texture2D>("Jungle/NewModels/WhackAMole/WhackAMoleBackground");
        bm.mainTexture = bgTex;
        if (bm.HasProperty("_BaseMap")) bm.SetTexture("_BaseMap", bgTex);
        if (bm.HasProperty("_BaseColor")) bm.SetColor("_BaseColor", Color.white);
        bg.GetComponent<Renderer>().sharedMaterial = bm;

        table = Model(tablePrefab, tableMat, "02 Mesa cara superior", new Vector3(0, -.75f, .28f), 9.35f, true, Quaternion.Euler(117, 0, 0));
        hammer = Model(hammerPrefab, hammerMat, "04 Mazo", World(HandViewport, 7.7f), 2.35f, false, Quaternion.Euler(8, 180, -38));
        hammerPivot = hammer.transform.Find("Pivote visual");
        head = Model(headPrefab, headMat, "03 Cabeza activa", World(slots[4], 10.45f), 1.42f, false, Quaternion.Euler(180, 180, 0));
        head.SetActive(false);
        hitHead = Model(hitPrefab, hitMat, "03 Cabeza golpeada", World(slots[4], 10.35f), 1.52f, false, Quaternion.Euler(180, 180, 0));
        hitHeadPivot = hitHead.transform.Find("Pivote visual");
        hitHead.SetActive(false);
        var l = FindAnyObjectByType<Light>();
        if (l != null) { l.intensity = 1.5f; l.transform.rotation = Quaternion.Euler(42, -25, 0); }
    }

    private Vector3 World(Vector2 v, float d) => Camera.main.ViewportToWorldPoint(new Vector3(v.x, v.y, d));

    private GameObject Model(GameObject prefab, Material mat, string n, Vector3 pos, float size, bool width, Quaternion rot)
    {
        var root = new GameObject(n);
        root.transform.SetParent(transform);
        root.transform.position = pos;
        var pivot = new GameObject("Pivote visual");
        pivot.transform.SetParent(root.transform, false);
        if (prefab == null) { Debug.LogError("Falta " + n); return root; }
        var model = Instantiate(prefab, pivot.transform);
        model.name = "Modelo";
        model.transform.SetLocalPositionAndRotation(Vector3.zero, rot);
        model.transform.localScale = Vector3.one;
        JungleStageBuilder.DisableChildColliders(model);
        var rs = model.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) return root;
        Bounds b = JungleStageBuilder.CombinedBounds(rs);
        float measure = width ? b.size.x : Mathf.Max(b.size.x, b.size.y);
        model.transform.localScale = Vector3.one * (size / Mathf.Max(.01f, measure));
        b = JungleStageBuilder.CombinedBounds(rs);
        model.transform.position += pos - b.center;
        for (int i = 0; i < rs.Length; i++) rs[i].gameObject.layer = 0;
        JungleStageBuilder.ApplySharedMaterial(rs, mat);
        return root;
    }

    private void Update()
    {
        float? roundProgress = state == State.Playing ? Mathf.Clamp01(1f - (endAt - Time.time) / 45f) : (float?)null;
        session.PresentGame(state == State.Intro ? "Ready" : state == State.Countdown ? "Countdown" : state == State.Playing ? "Playing" : "Finished", score, progress: roundProgress);
        if (!session.IsInputReady) { downSpeed = phonePower = 0; whackRequested = false; return; }
        whackRequested = false;
        if (session.TryConsumeFrame(out var frame)) ReadInput(frame);
        MoveHammer();
        if (state == State.Countdown)
        {
            int c = Mathf.Clamp(3 - Mathf.FloorToInt(Time.time - stateAt), 1, 3);
            if (c != lastCountdownBeep) { lastCountdownBeep = c; if (sfxSource != null && tickClip != null) sfxSource.PlayOneShot(tickClip, .6f); }
            if (Time.time - stateAt >= 3) StartRound();
        }
        else if (state == State.Playing)
        {
            if (Time.time >= endAt) Finish();
            else UpdateHead();
        }
    }

    private void ReadInput(JungleFusedFrame frame)
    {
        inputRevision = frame.Revision;
        downSpeed = frame.Right.VelocityY;
        phonePower = frame.PhonePower;
        cursorViewport = new Vector2(frame.Right.X, 1f - frame.Right.Y);
        if (frame.NewPose || frame.Baseline) handVisual.Observe(cursorViewport.x, cursorViewport.y, frame.Right.VelocityX, -frame.Right.VelocityY, Time.unscaledTime);
        whackRequested = (frame.Actions & JungleAction.Whack) != 0;
    }

    private void MoveHammer()
    {
        if (hammer == null) return;
        hammer.transform.position = Vector3.Lerp(hammer.transform.position, World(VisualHand, 7.7f), 22 * Time.deltaTime);
        float strength = Mathf.Clamp01(Mathf.Max(downSpeed / 2.2f, phonePower / 260));
        if (hammerPivot != null)
            hammerPivot.localRotation = Quaternion.Slerp(hammerPivot.localRotation, Quaternion.Euler(0, 0, Mathf.Lerp(0, 68, strength)), 20 * Time.deltaTime);
        if (state == State.Playing && headActive && Time.time - lastHit > .22f && whackRequested) TryHit();
    }

    private void SpawnHead()
    {
        activeSlot = slots[UnityEngine.Random.Range(0, slots.Length)];
        head.transform.position = World(activeSlot + Vector2.down * .075f, 10.45f);
        head.SetActive(true);
        JungleStageBuilder.PulseSquash(head, 1.18f, .86f, 22f, 9f);
        headActive = true;
        headAt = Time.time;
        headUntil = Time.time + 1.25f;
        message = "¡AHORA!";
        if (sfxSource != null && popClip != null) sfxSource.PlayOneShot(popClip, .55f);
    }

    private void UpdateHead()
    {
        if (hitHeadActive && Time.time >= hitUntil)
        {
            hitHead.SetActive(false);
            hitHeadActive = false;
            nextHeadAt = Time.time + UnityEngine.Random.Range(.18f, .48f);
        }
        if (!headActive && !hitHeadActive && Time.time >= nextHeadAt) SpawnHead();
        if (!headActive) return;
        float age = Time.time - headAt, remaining = headUntil - Time.time;
        float pop = Mathf.SmoothStep(0, 1, Mathf.Clamp01(age / .18f));
        if (remaining < .18f) pop = Mathf.SmoothStep(0, 1, Mathf.Clamp01(remaining / .18f));
        head.transform.position = World(activeSlot + Vector2.down * Mathf.Lerp(.075f, 0, pop), 10.45f);
        if (Time.time >= headUntil)
        {
            head.SetActive(false);
            headActive = false;
            misses++;
            combo = 0;
            message = "¡SE ESCAPÓ!";
            nextHeadAt = Time.time + .28f;
        }
    }

    private void TryHit()
    {
        if (state != State.Playing || !headActive || Time.time - headAt < .14f || Vector2.Distance(HandViewport, activeSlot) > .105f) return;
        if (!session.TryAuthorize(JungleAction.Whack, inputRevision)) return;
        if (sfxSource != null && whackClip != null) sfxSource.PlayOneShot(whackClip, .75f);
        lastHit = Time.time;
        head.SetActive(false);
        headActive = false;
        hitHead.transform.position = World(activeSlot, 10.35f);
        if (hitHeadPivot != null) hitHeadPivot.localRotation = Quaternion.Euler(0, 0, UnityEngine.Random.Range(-7f, 7f));
        hitHead.SetActive(true);
        JungleStageBuilder.PulseSquash(hitHead, .62f, 1.32f, 28f, 9f);
        JungleStageBuilder.PulseSquash(hammer, .76f, 1.20f, 30f, 10f);
        JungleStageBuilder.PulseSquash(table, .96f, 1.03f, 24f, 11f);
        JungleStageBuilder.ShakeCamera(.085f, .13f);
        hitHeadActive = true;
        hitUntil = Time.time + .42f;
        hits++;
        combo++;
        bestCombo = Mathf.Max(bestCombo, combo);
        int gain = 100 + Mathf.Min(combo, 12) * 20;
        score += gain;
        message = combo >= 4 ? $"¡COMBO x{combo}" : $"+{gain}";
    }

    public void BeginFromPhone(){if(state==State.Intro)Begin();}    private void Begin()
    {
        JungleAudioBank.PlayUiClick();
        session.SetGameplayActive(true);
        session.ResetRound();
        score = hits = misses = combo = bestCombo = 0;
        lastCountdownBeep = -1;
        if (head != null) head.SetActive(false);
        if (hitHead != null) hitHead.SetActive(false);
        headActive = hitHeadActive = false;
        cursorViewport = new Vector2(.5f, .5f);
        state = State.Countdown;
        stateAt = Time.time;
    }

    private void StartRound()
    {
        state = State.Playing;
        endAt = Time.time + 45;
        nextHeadAt = Time.time + .35f;
        message = "¡A JUGAR!";
        if (sfxSource != null && goClip != null) sfxSource.PlayOneShot(goClip, .7f);
    }

    private void Finish()
    {
        if (head != null) head.SetActive(false);
        if (hitHead != null) hitHead.SetActive(false);
        headActive = hitHeadActive = false;
        state = State.Results;
        session.SetGameplayActive(false);
        message = "¡TIEMPO!";
    }

    public void RenderUI()
    {
        JungleUITheme.Ensure();
        float w = Screen.width, h = Screen.height;
        if (score != cachedScore || combo != cachedCombo)
        {
            cachedScore = score;
            cachedCombo = combo;
            cachedHudText = $"PUNTOS {score}   COMBO x{combo}";
        }
        JungleUITheme.DrawPanel(new Rect(w * .02f, h * .02f, w * .96f, h * .108f));
        JungleUITheme.DrawShadowedLabel(new Rect(w * .04f, h * .025f, w * .34f, h * .085f), "GORILLA WHACK", JungleUITheme.HudTitle, 3f);
        JungleUITheme.DrawShadowedLabel(new Rect(w * .39f, h * .028f, w * .38f, h * .075f), cachedHudText, JungleUITheme.HudScore, 2f);
        if (JungleRetainedUI.Button(new Rect(w * .84f, h * .035f, w * .125f, h * .065f), "MENÚ", JungleUITheme.Button)) { JungleAudioBank.PlayUiClick(); SceneNavigator.Load(SceneNavigator.MainMenu); }

        if (state == State.Intro)
        {
            JungleUITheme.DrawPanel(new Rect(w * .24f, h * .22f, w * .52f, h * .49f));
            JungleUITheme.DrawShadowedLabel(new Rect(w * .28f, h * .26f, w * .44f, h * .09f), "¡ATRAPA AL GORILA!", JungleUITheme.HudTitle, 3f);
            JungleUITheme.DrawPlankLabel(new Rect(w * .28f, h * .375f, w * .44f, h * .14f), "MUEVE LA MANO DERECHA PARA APUNTAR\nY BAJA EL CELULAR PARA GOLPEAR");
            if (JungleRetainedUI.Button(new Rect(w * .36f, h * .575f, w * .28f, h * .08f), "CENTRAR Y JUGAR", JungleUITheme.Button)) Begin();
        }
        else if (state == State.Countdown)
        {
            int c = Mathf.Clamp(3 - Mathf.FloorToInt(Time.time - stateAt), 1, 3);
            JungleUITheme.DrawPanel(new Rect(w * .42f, h * .31f, w * .16f, h * .2f), selected: true);
            JungleUITheme.DrawShadowedLabel(new Rect(w * .42f, h * .31f, w * .16f, h * .2f), c == 3 ? "3" : c == 2 ? "2" : "1", JungleUITheme.HudBig, 3.5f);
        }
        else if (state == State.Playing)
        {
            int remSec = Mathf.CeilToInt(Mathf.Max(0, endAt - Time.time));
            if (remSec != cachedTimerSec)
            {
                cachedTimerSec = remSec;
                cachedTimerText = remSec + " s";
            }
            Rect timerRect = new Rect(w * .43f, h * .14f, w * .14f, h * .065f);
            if (endAt - Time.time < 6) JungleRetainedUI.DrawTexture(timerRect, JungleUITheme.Red);
            else JungleUITheme.DrawSubPanel(timerRect);
            JungleUITheme.DrawShadowedLabel(timerRect, cachedTimerText, JungleUITheme.HudScore, 2f);
        }
        else
        {
            if (score != cachedResScore || hits != cachedResHits || misses != cachedResMisses || bestCombo != cachedResCombo)
            {
                cachedResScore = score; cachedResHits = hits; cachedResMisses = misses; cachedResCombo = bestCombo;
                cachedResultText = $"{score} PUNTOS\n{hits} GOLPES · {misses} ESCAPES · MEJOR COMBO x{bestCombo}";
            }
            JungleUITheme.DrawPanel(new Rect(w * .27f, h * .24f, w * .46f, h * .44f));
            JungleUITheme.DrawShadowedLabel(new Rect(w * .31f, h * .28f, w * .38f, h * .08f), "RESULTADO", JungleUITheme.HudTitle, 3f);
            JungleUITheme.DrawPlankLabel(new Rect(w * .31f, h * .38f, w * .38f, h * .15f), cachedResultText);
            if (JungleRetainedUI.Button(new Rect(w * .38f, h * .565f, w * .24f, h * .075f), "OTRA RONDA", JungleUITheme.Button)) Begin();
        }
        if (state == State.Playing) JungleUITheme.DrawShadowedLabel(new Rect(w * .36f, h * .2f, w * .28f, h * .065f), message, JungleUITheme.HudBody, 2f);
        JungleUITheme.DrawGameHandSquare(VisualHand, JungleUITheme.Turquoise, invertY: true);
        JungleUITheme.DrawLeaves();
    }
}
