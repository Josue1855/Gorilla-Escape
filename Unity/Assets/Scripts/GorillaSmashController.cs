using System;
using UnityEngine;
using GorillaEscape.Gameplay.Input;

[RequireComponent(typeof(JunglePanel))]
public sealed class GorillaSmashController : MonoBehaviour, IJungleScreen
{
    private enum State { Intro, Countdown, Playing, Smashing, AttemptResult, Results }

    private MotionSession session;
    private State state = State.Intro;

    // Escenario 3D procedural
    private GameObject towerRoot;
    private GameObject platformPad;
    private GameObject puckIndicator;
    private GameObject bellObject;
    private GameObject hammer;
    private Transform hammerPivot;
    private Light bellGlowLight;

    // Materiales y texturas
    private Material towerMat;
    private Material padMat;
    private Material puckMat;
    private Material bellMat;
    private Material hammerMat;

    // Parámetros de la torre de feria (High Striker)
    private const float BaseY = -0.7f;
    private const float TopY = 6.8f;
    private const float TowerHeight = TopY - BaseY;

    // Estado del juego
    private Vector2 cursorViewport = new Vector2(0.5f, 0.5f);
    private readonly HandVisualPrediction handVisual = new HandVisualPrediction();
    private Vector2 VisualHand => JungleUITheme.VisualHand(session, handVisual, cursorViewport);

    private float downSpeed;
    private float phonePower;
    private long inputRevision;
    private bool smashRequested;
    private float stateAt;
    private float smashStartedAt;
    private float puckCurrentY = BaseY;
    private float puckTargetY = BaseY;
    private bool bellRang;

    // Intentos y puntuación (Spec v4 - Sección 17)
    private const int MaxAttempts = 3;
    private int currentAttempt = 0;
    private readonly int[] attemptScores = new int[MaxAttempts];
    private int bestScore = 0;
    private int currentScore = 0;
    private string currentRankLabel = "";
    private Color currentRankColor = Color.white;

    // Audio y SFX
    private AudioSource sfxSource;
    private AudioClip whackClip;
    private AudioClip popClip;
    private AudioClip tickClip;
    private AudioClip goClip;
    private int lastCountdownBeep = -1;

    // Feedback visual en pantalla
    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle scoreStyle;
    private GUIStyle rankStyle;
    private GUIStyle meterLabelStyle;
    private int lastWidth, lastHeight;

    private void Awake()
    {
        session = MotionSession.Ensure();
        session.ConfigureForScene(SceneNavigator.GorillaSmash);
        SetupAudio();
        BuildHighStrikerScene();
        stateAt = Time.time;
        StartCoroutine(AutoScreenshotRoutine());
    }

    private System.Collections.IEnumerator AutoScreenshotRoutine()
    {
        string screenshotDir = System.Environment.GetEnvironmentVariable("GORILLA_AUTO_SCREENSHOT_DIR");
        string autoFlow = System.Environment.GetEnvironmentVariable("GORILLA_AUTO_FLOW");
        if (string.IsNullOrEmpty(screenshotDir) && string.IsNullOrEmpty(autoFlow))
            yield break;
        if (string.IsNullOrEmpty(screenshotDir))
            screenshotDir = System.IO.Path.Combine(Application.dataPath, "../../Reports/QA/Screenshots");
        System.IO.Directory.CreateDirectory(screenshotDir);

        yield return new WaitForSeconds(0.6f);
        ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(screenshotDir, "evidence_09_transition_gameplay.png"));

        yield return new WaitForSeconds(0.6f);
        ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(screenshotDir, "evidence_10_reopen_verification.png"));

        if (System.Environment.GetEnvironmentVariable("GORILLA_AUTO_QUIT") == "1")
        {
            yield return new WaitForSeconds(0.5f);
            Application.Quit();
        }
    }

    private void SetupAudio()
    {
        sfxSource = JungleAudioBank.Attach2DSource(gameObject);
        whackClip = JungleAudioBank.WhackHit;
        popClip = JungleAudioBank.WhackPop;
        tickClip = JungleAudioBank.CountdownTick;
        goClip = JungleAudioBank.CountdownGo;
    }

    private void BuildHighStrikerScene()
    {
        Camera c = Camera.main;
        if (c != null)
        {
            c.orthographic = false;
            c.fieldOfView = 50f;
            c.transform.position = new Vector3(0f, 2.8f, -9.5f);
            c.transform.LookAt(new Vector3(0f, 2.5f, 0f));
            c.backgroundColor = new Color(0.06f, 0.14f, 0.10f);
        }

        // 1. Fondo de selva
        var bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bg.name = "01 Fondo Selva";
        bg.transform.SetParent(transform, false);
        bg.transform.position = new Vector3(0f, 3.2f, 8.5f);
        bg.transform.localScale = new Vector3(32f, 18f, 1f);
        if (bg.TryGetComponent<Collider>(out var bgCol)) Destroy(bgCol);

        var unlitShader = JungleStageBuilder.UnlitShader;
        var bgMat = new Material(unlitShader);
        var bgTex = JungleAssets.Load<Texture2D>("Jungle/NewModels/WhackAMole/WhackAMoleBackground");
        if (bgTex != null)
        {
            bgMat.mainTexture = bgTex;
            if (bgMat.HasProperty("_BaseMap")) bgMat.SetTexture("_BaseMap", bgTex);
        }
        else
        {
            bgMat.color = new Color(0.12f, 0.28f, 0.18f);
        }
        bg.GetComponent<Renderer>().sharedMaterial = bgMat;

        // 2. Iluminación adicional local
        var lightObj = new GameObject("02 Luz Foco Striker");
        lightObj.transform.SetParent(transform, false);
        lightObj.transform.position = new Vector3(0f, 4f, -4f);
        var spot = lightObj.AddComponent<Light>();
        spot.type = LightType.Point;
        spot.color = new Color(1f, 0.95f, 0.85f);
        spot.range = 15f;
        spot.intensity = 1.4f;

        // 3. Torre vertical (High Striker)
        towerRoot = new GameObject("03 Torre High Striker");
        towerRoot.transform.SetParent(transform, false);

        towerMat = JungleStageBuilder.GetSharedMaterial(new Color(0.35f, 0.22f, 0.12f), 0.1f, 0.4f);
        var metalMat = JungleStageBuilder.GetSharedMaterial(new Color(0.75f, 0.78f, 0.82f), 0.6f, 0.7f);
        padMat = JungleStageBuilder.GetSharedMaterial(new Color(0.85f, 0.35f, 0.15f), 0.2f, 0.5f);
        puckMat = JungleStageBuilder.GetSharedMaterial(new Color(0.95f, 0.82f, 0.15f), 0.7f, 0.85f);
        bellMat = JungleStageBuilder.GetSharedMaterial(new Color(1f, 0.88f, 0.22f), 0.85f, 0.9f);

        // Columnas guía laterales de la torre
        var leftCol = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        leftCol.name = "Pilar Izquierdo";
        leftCol.transform.SetParent(towerRoot.transform, false);
        leftCol.transform.position = new Vector3(-0.65f, 3.0f, 0.5f);
        leftCol.transform.localScale = new Vector3(0.18f, 4.0f, 0.18f);
        leftCol.GetComponent<Renderer>().sharedMaterial = towerMat;

        var rightCol = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rightCol.name = "Pilar Derecho";
        rightCol.transform.SetParent(towerRoot.transform, false);
        rightCol.transform.position = new Vector3(0.65f, 3.0f, 0.5f);
        rightCol.transform.localScale = new Vector3(0.18f, 4.0f, 0.18f);
        rightCol.GetComponent<Renderer>().sharedMaterial = towerMat;

        // Riel central
        var centerRail = GameObject.CreatePrimitive(PrimitiveType.Cube);
        centerRail.name = "Riel Central";
        centerRail.transform.SetParent(towerRoot.transform, false);
        centerRail.transform.position = new Vector3(0f, 3.0f, 0.5f);
        centerRail.transform.localScale = new Vector3(0.22f, 8.0f, 0.12f);
        centerRail.GetComponent<Renderer>().sharedMaterial = metalMat;

        // Travesaños decorativos
        for (int i = 0; i <= 8; i++)
        {
            float rungY = BaseY + i * (TowerHeight / 8f);
            var rung = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rung.name = $"Travesaño_{i}";
            rung.transform.SetParent(towerRoot.transform, false);
            rung.transform.position = new Vector3(0f, rungY, 0.52f);
            rung.transform.localScale = new Vector3(1.35f, 0.08f, 0.08f);
            rung.GetComponent<Renderer>().sharedMaterial = metalMat;
        }

        // 4. Base y plataforma de impacto
        var baseBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseBlock.name = "Base Madera";
        baseBlock.transform.SetParent(towerRoot.transform, false);
        baseBlock.transform.position = new Vector3(0f, -1.25f, 0.3f);
        baseBlock.transform.localScale = new Vector3(2.4f, 0.7f, 2.2f);
        baseBlock.GetComponent<Renderer>().sharedMaterial = towerMat;

        platformPad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        platformPad.name = "Plataforma Impacto";
        platformPad.transform.SetParent(towerRoot.transform, false);
        platformPad.transform.position = new Vector3(0f, -0.85f, 0.3f);
        platformPad.transform.localScale = new Vector3(1.2f, 0.15f, 1.2f);
        platformPad.GetComponent<Renderer>().sharedMaterial = padMat;

        // 5. Puck / Indicador ascendente
        puckIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        puckIndicator.name = "Puck Indicador";
        puckIndicator.transform.SetParent(towerRoot.transform, false);
        puckIndicator.transform.position = new Vector3(0f, BaseY, 0.5f);
        puckIndicator.transform.localScale = new Vector3(0.55f, 0.14f, 0.55f);
        puckIndicator.GetComponent<Renderer>().sharedMaterial = puckMat;

        // 6. Campana superior
        bellObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bellObject.name = "Campana Dorada";
        bellObject.transform.SetParent(towerRoot.transform, false);
        bellObject.transform.position = new Vector3(0f, TopY, 0.5f);
        bellObject.transform.localScale = new Vector3(1.0f, 0.9f, 1.0f);
        bellObject.GetComponent<Renderer>().sharedMaterial = bellMat;

        var bellRim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bellRim.name = "Borde Campana";
        bellRim.transform.SetParent(bellObject.transform, false);
        bellRim.transform.position = new Vector3(0f, -0.3f, 0f);
        bellRim.transform.localScale = new Vector3(1.25f, 0.12f, 1.25f);
        bellRim.GetComponent<Renderer>().sharedMaterial = bellMat;

        var glow = new GameObject("Glow Campana");
        glow.transform.SetParent(bellObject.transform, false);
        bellGlowLight = glow.AddComponent<Light>();
        bellGlowLight.type = LightType.Point;
        bellGlowLight.color = new Color(1f, 0.9f, 0.3f);
        bellGlowLight.range = 4f;
        bellGlowLight.intensity = 0f;

        // 7. Mazo 3D
        CreateHammer();
    }

    private void CreateHammer()
    {
        hammer = new GameObject("04 Mazo del Gorila");
        hammer.transform.SetParent(transform, false);
        hammer.transform.position = new Vector3(1.5f, 0f, -1f);

        var pivotObj = new GameObject("Pivote Visual");
        pivotObj.transform.SetParent(hammer.transform, false);
        hammerPivot = pivotObj.transform;

        hammerMat = JungleStageBuilder.GetSharedMaterial(new Color(0.45f, 0.28f, 0.14f), 0.1f, 0.3f);
        var metalBandMat = JungleStageBuilder.GetSharedMaterial(new Color(0.6f, 0.6f, 0.65f), 0.7f, 0.8f);

        // Mango de madera
        var handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        handle.name = "Mango";
        handle.transform.SetParent(hammerPivot, false);
        handle.transform.localPosition = new Vector3(0f, 0.65f, 0f);
        handle.transform.localScale = new Vector3(0.10f, 0.75f, 0.10f);
        handle.GetComponent<Renderer>().sharedMaterial = hammerMat;

        // Cabeza cilíndrica del mazo
        var head = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        head.name = "Cabeza Mazo";
        head.transform.SetParent(hammerPivot, false);
        head.transform.localPosition = new Vector3(0f, 1.35f, 0f);
        head.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        head.transform.localScale = new Vector3(0.45f, 0.55f, 0.45f);
        head.GetComponent<Renderer>().sharedMaterial = hammerMat;

        // Aros de refuerzo
        var ring1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring1.transform.SetParent(head.transform, false);
        ring1.transform.localPosition = new Vector3(0f, 0.38f, 0f);
        ring1.transform.localScale = new Vector3(1.04f, 0.08f, 1.04f);
        ring1.GetComponent<Renderer>().sharedMaterial = metalBandMat;

        var ring2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring2.transform.SetParent(head.transform, false);
        ring2.transform.localPosition = new Vector3(0f, -0.38f, 0f);
        ring2.transform.localScale = new Vector3(1.04f, 0.08f, 1.04f);
        ring2.GetComponent<Renderer>().sharedMaterial = metalBandMat;
    }

    private void EnsureStyles(float w, float h)
    {
        int wi = Mathf.RoundToInt(w), hi = Mathf.RoundToInt(h);
        if (titleStyle != null && lastWidth == wi && lastHeight == hi) return;
        lastWidth = wi;
        lastHeight = hi;

        titleStyle = new GUIStyle(JungleUITheme.Title)
        {
            fontSize = Mathf.RoundToInt(h * 0.065f),
            normal = { textColor = new Color(1f, 0.86f, 0.18f) }
        };

        subtitleStyle = new GUIStyle(JungleUITheme.Body)
        {
            fontSize = Mathf.RoundToInt(h * 0.026f),
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.95f, 0.85f) }
        };

        scoreStyle = new GUIStyle(JungleUITheme.Title)
        {
            fontSize = Mathf.RoundToInt(h * 0.085f),
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.96f, 0.35f) }
        };

        rankStyle = new GUIStyle(JungleUITheme.Title)
        {
            fontSize = Mathf.RoundToInt(h * 0.052f),
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };

        meterLabelStyle = new GUIStyle(JungleUITheme.Body)
        {
            fontSize = Mathf.RoundToInt(h * 0.022f),
            alignment = TextAnchor.MiddleRight,
            normal = { textColor = new Color(1f, 0.85f, 0.4f) }
        };
    }

    private void Update()
    {
        float? progress = state == State.Playing || state == State.Smashing
            ? Mathf.Clamp01((float)currentAttempt / MaxAttempts)
            : (float?)null;

        session.PresentGame(
            state == State.Intro ? "Ready" :
            state == State.Countdown ? "Countdown" :
            state == State.Results ? "Finished" : "Playing",
            bestScore > 0 ? (double?)bestScore : null,
            progress: progress);

        if (!session.IsInputReady)
        {
            downSpeed = phonePower = 0f;
            smashRequested = false;
            // Permitir avance accesible con teclado para pruebas
            CheckKeyboardFallback();
            UpdateVisuals();
            return;
        }

        smashRequested = false;
        if (session.TryConsumeFrame(out var frame))
        {
            ReadInput(frame);
        }

        CheckKeyboardFallback();
        UpdateVisuals();

        if (state == State.Countdown)
        {
            int c = Mathf.Clamp(3 - Mathf.FloorToInt(Time.time - stateAt), 1, 3);
            if (c != lastCountdownBeep)
            {
                lastCountdownBeep = c;
                if (sfxSource != null && tickClip != null) sfxSource.PlayOneShot(tickClip, 0.6f);
            }
            if (Time.time - stateAt >= 3f)
            {
                if (sfxSource != null && goClip != null) sfxSource.PlayOneShot(goClip, 0.8f);
                StartRound();
            }
        }
        else if (state == State.Playing)
        {
            if (smashRequested)
            {
                ExecuteSmash();
            }
        }
        else if (state == State.Smashing)
        {
            float elapsed = Time.time - smashStartedAt;
            float duration = 1.25f;
            float t = Mathf.Clamp01(elapsed / duration);

            // Subida con desaceleración elástica
            float eased = Mathf.Sin(t * Mathf.PI * 0.5f);
            puckCurrentY = Mathf.Lerp(BaseY, puckTargetY, eased);

            if (puckIndicator != null)
                puckIndicator.transform.position = new Vector3(0f, puckCurrentY, 0.5f);

            // Al llegar arriba, evaluar campana
            if (t >= 0.95f && !bellRang && currentScore >= 9500)
            {
                bellRang = true;
                RingBell();
            }

            if (elapsed >= duration + 0.6f)
            {
                state = State.AttemptResult;
                stateAt = Time.time;
            }
        }
        else if (state == State.AttemptResult)
        {
            if (Time.time - stateAt >= 2.2f)
            {
                currentAttempt++;
                if (currentAttempt >= MaxAttempts)
                {
                    state = State.Results;
                    stateAt = Time.time;
                }
                else
                {
                    ResetPuck();
                    state = State.Playing;
                }
            }
        }
    }

    private void ReadInput(JungleFusedFrame frame)
    {
        inputRevision = frame.Revision;
        downSpeed = frame.Right.VelocityY;
        phonePower = frame.PhonePower;
        cursorViewport = new Vector2(frame.Right.X, 1f - frame.Right.Y);

        if (frame.NewPose || frame.Baseline)
            handVisual.Observe(cursorViewport.x, cursorViewport.y, frame.Right.VelocityX, -frame.Right.VelocityY, Time.unscaledTime);

        // Detectar acción Smash o Whack autorizada
        if ((frame.Actions & (JungleAction.Smash | JungleAction.Whack)) != 0)
            smashRequested = true;
    }

    private void CheckKeyboardFallback()
    {
        // Fallback accesible sin celular/cámara (Espacio o Enter)
        if (state == State.Playing && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
        {
            downSpeed = UnityEngine.Random.Range(1.8f, 3.2f);
            phonePower = UnityEngine.Random.Range(320f, 650f);
            smashRequested = true;
        }
    }

    private void UpdateVisuals()
    {
        // Mover mazo hacia la posición de la mano
        if (hammer != null)
        {
            Vector3 targetHandPos = Camera.main.ViewportToWorldPoint(new Vector3(VisualHand.x, VisualHand.y, 7.5f));
            targetHandPos.z = 0f;
            hammer.transform.position = Vector3.Lerp(hammer.transform.position, targetHandPos, 22f * Time.deltaTime);

            if (hammerPivot != null)
            {
                float strength = Mathf.Clamp01(Mathf.Max(downSpeed / 2.5f, phonePower / 300f));
                float swingAngle = Mathf.Lerp(-25f, 75f, strength);
                hammerPivot.localRotation = Quaternion.Slerp(hammerPivot.localRotation, Quaternion.Euler(swingAngle, 0f, 0f), 24f * Time.deltaTime);
            }
        }

        // Parpadeo de luz de campana si sonó
        if (bellGlowLight != null && bellGlowLight.intensity > 0f)
        {
            bellGlowLight.intensity = Mathf.Max(0f, bellGlowLight.intensity - 2.5f * Time.deltaTime);
        }
    }

    private void ExecuteSmash()
    {
        if (state != State.Playing) return;

        // Intentar autorizar con el supervisor oficial
        bool authorized = session.TryAuthorize(JungleAction.Smash, inputRevision)
                       || session.TryAuthorize(JungleAction.Whack, inputRevision);

        // Si no hay hardware conectado pero se presionó teclado fallback, permitir
        if (!authorized && !session.IsInputReady)
        {
            authorized = true;
        }

        if (!authorized) return;

        // Efecto de impacto en plataforma
        if (sfxSource != null && whackClip != null)
            sfxSource.PlayOneShot(whackClip, 0.9f);

        JungleStageBuilder.PulseSquash(platformPad, 1.35f, 0.7f, 25f, 10f);
        JungleStageBuilder.ShakeCamera(0.22f, 0.18f);

        // Cálculo de técnica según Spec v4 Sección 17:
        // Technique = Power * 0.45 + DirectionAccuracy * 0.25 + Timing * 0.20 + GestureConfidence * 0.10
        float powerNorm = Mathf.Clamp01(Mathf.Max(downSpeed / 2.6f, phonePower / 420f));
        float dirAcc = Mathf.Clamp01(1f - Mathf.Abs(VisualHand.x - 0.5f) * 1.5f);
        float timing = UnityEngine.Random.Range(0.85f, 1.0f);
        float gestureConf = session.CameraConnected ? 0.95f : 0.80f;

        float technique = powerNorm * 0.45f + dirAcc * 0.25f + timing * 0.20f + gestureConf * 0.10f;
        currentScore = Mathf.Clamp(Mathf.RoundToInt(technique * 10000f), 200, 10000);

        if (currentScore > bestScore)
            bestScore = currentScore;

        attemptScores[currentAttempt] = currentScore;

        // Clasificación de feedback oficial
        if (currentScore < 2500)
        {
            currentRankLabel = "¡GOLPE SUAVE! (WEAK)";
            currentRankColor = new Color(0.7f, 0.7f, 0.7f);
        }
        else if (currentScore < 5000)
        {
            currentRankLabel = "¡BUEN IMPACTO! (GOOD)";
            currentRankColor = new Color(0.3f, 0.85f, 0.4f);
        }
        else if (currentScore < 7500)
        {
            currentRankLabel = "¡GRAN FUERZA! (GREAT)";
            currentRankColor = new Color(0.2f, 0.8f, 1f);
        }
        else if (currentScore < 9500)
        {
            currentRankLabel = "¡FUERZA COLOSAL! (PERFECT)";
            currentRankColor = new Color(1f, 0.8f, 0.15f);
        }
        else
        {
            currentRankLabel = "¡¡¡CAMPANAZO LEGENDARIO!!!";
            currentRankColor = new Color(1f, 0.25f, 0.25f);
        }

        // Altura objetivo del puck
        float scoreRatio = currentScore / 10000f;
        puckTargetY = BaseY + scoreRatio * TowerHeight;

        bellRang = false;
        smashStartedAt = Time.time;
        state = State.Smashing;
    }

    private void RingBell()
    {
        if (sfxSource != null && popClip != null)
            sfxSource.PlayOneShot(popClip, 1.0f);

        JungleStageBuilder.ShakeCamera(0.35f, 0.28f);
        JungleStageBuilder.PulseSquash(bellObject, 1.3f, 0.85f, 28f, 12f);

        if (bellGlowLight != null)
            bellGlowLight.intensity = 3.5f;
    }

    private void ResetPuck()
    {
        puckCurrentY = BaseY;
        puckTargetY = BaseY;
        if (puckIndicator != null)
            puckIndicator.transform.position = new Vector3(0f, BaseY, 0.5f);
    }

    public void BeginFromPhone()
    {
        if (state == State.Intro)
            StartCountdown();
    }

    private void StartCountdown()
    {
        state = State.Countdown;
        stateAt = Time.time;
        lastCountdownBeep = -1;
    }

    private void StartRound()
    {
        state = State.Playing;
        currentAttempt = 0;
        bestScore = 0;
        for (int i = 0; i < MaxAttempts; i++) attemptScores[i] = 0;
        ResetPuck();
    }

    public void RenderUI()
    {
        JungleUITheme.Ensure();
        float w = Screen.width, h = Screen.height;
        EnsureStyles(w, h);

        // Encabezado superior
        Rect headerRect = new Rect(w * 0.15f, h * 0.02f, w * 0.70f, h * 0.12f);
        JungleUITheme.DrawPanel(headerRect);
        JungleUITheme.DrawShadowedLabel(new Rect(headerRect.x, h * 0.025f, headerRect.width, h * 0.065f), "GORILLA SMASH", titleStyle, 3f);
        string sub = state == State.Intro ? "MÁQUINA DE FUERZA DE FERIA · PREPARA TU GOLPE" :
                     state == State.Countdown ? "¡PREPÁRATE PARA GOLPEAR!" :
                     state == State.Results ? "¡TORNEO DE FUERZA COMPLETADO!" :
                     $"INTENTO {currentAttempt + 1} DE {MaxAttempts} · ASES TA UN GOLPE CON FUERZA";
        JungleUITheme.DrawShadowedLabel(new Rect(headerRect.x, h * 0.088f, headerRect.width, h * 0.04f), sub, subtitleStyle, 2f);

        // Marcadores de la torre a la derecha
        Rect meterRect = new Rect(w * 0.55f, h * 0.18f, w * 0.18f, h * 0.55f);
        DrawTowerGraduations(meterRect);

        // Panel de información / intentos a la izquierda
        Rect statusRect = new Rect(w * 0.05f, h * 0.22f, w * 0.28f, h * 0.50f);
        JungleUITheme.DrawPanel(statusRect);

        Rect statusTitleRect = new Rect(statusRect.x + 10f, statusRect.y + 12f, statusRect.width - 20f, h * 0.045f);
        JungleUITheme.DrawShadowedLabel(statusTitleRect, "TABLA DE INTENTOS", subtitleStyle, 2f);

        for (int i = 0; i < MaxAttempts; i++)
        {
            Rect attemptRow = new Rect(statusRect.x + 15f, statusRect.y + h * (0.07f + i * 0.08f), statusRect.width - 30f, h * 0.065f);
            bool isCurrent = (state == State.Playing || state == State.Smashing || state == State.AttemptResult) && currentAttempt == i;
            JungleUITheme.DrawPanel(attemptRow, isCurrent);

            string scoreTxt = attemptScores[i] > 0 ? $"{attemptScores[i]:N0} PTS" : isCurrent ? "EN CURSO..." : "---";
            string labelTxt = $"INTENTO {i + 1}: {scoreTxt}";
            JungleUITheme.DrawShadowedLabel(attemptRow, labelTxt, subtitleStyle, 1.5f);
        }

        // Mejor marca
        Rect bestBox = new Rect(statusRect.x + 15f, statusRect.y + h * 0.33f, statusRect.width - 30f, h * 0.12f);
        JungleUITheme.DrawPanel(bestBox, true);
        JungleUITheme.DrawShadowedLabel(new Rect(bestBox.x, bestBox.y + 6f, bestBox.width, h * 0.035f), "MEJOR MARCA", subtitleStyle, 1.5f);
        JungleUITheme.DrawShadowedLabel(new Rect(bestBox.x, bestBox.y + h * 0.04f, bestBox.width, h * 0.07f), $"{bestScore:N0}", scoreStyle, 2.5f);

        // Feedback del golpe actual
        if (state == State.Smashing || state == State.AttemptResult)
        {
            Rect feedbackPanel = new Rect(w * 0.35f, h * 0.76f, w * 0.30f, h * 0.10f);
            JungleUITheme.DrawPanel(feedbackPanel, true);
            rankStyle.normal.textColor = currentRankColor;
            JungleUITheme.DrawShadowedLabel(feedbackPanel, currentRankLabel, rankStyle, 2.5f);
        }

        // Pantalla de Intro / Inicio
        if (state == State.Intro)
        {
            Rect introCard = new Rect(w * 0.32f, h * 0.35f, w * 0.36f, h * 0.30f);
            JungleUITheme.DrawPanel(introCard);
            Rect introText = new Rect(introCard.x + 15f, introCard.y + 20f, introCard.width - 30f, h * 0.12f);
            JungleUITheme.DrawShadowedLabel(introText, "Sujeta el teléfono firmemente.\nLevanta la mano y asesta un golpe rápido hacia abajo contra la plataforma.", subtitleStyle, 2f);

            Rect startBtn = new Rect(introCard.x + introCard.width * 0.15f, introCard.y + h * 0.18f, introCard.width * 0.70f, h * 0.08f);
            if (JungleRetainedUI.Button(startBtn, "¡INICIAR PRUEBA!", JungleUITheme.ButtonSelected))
            {
                JungleAudioBank.PlayUiClick();
                StartCountdown();
            }
        }
        else if (state == State.Results)
        {
            if (TournamentSession.IsActive)
            {
                TournamentSession.RecordRoundResult(bestScore);
                Vector2 normCursor = new Vector2(0.5f, 0.5f);
            string handLabel = "MANO";
            bool handRaised = false, handClosed = false, forwardPush = false;
            if (session != null) session.TryCameraCursor(out normCursor, out handLabel, out handRaised, out handClosed, out forwardPush);
                TournamentSession.DrawTournamentOverlay(w, h, normCursor, handLabel, handRaised, handClosed, forwardPush, session != null && session.PhoneTouchTriggered);
                return;
            }
            Rect resultsCard = new Rect(w * 0.30f, h * 0.25f, w * 0.40f, h * 0.45f);
            JungleUITheme.DrawPanel(resultsCard, true);

            JungleUITheme.DrawShadowedLabel(new Rect(resultsCard.x, resultsCard.y + 15f, resultsCard.width, h * 0.06f), "RESULTADO FINAL", titleStyle, 2.5f);
            JungleUITheme.DrawShadowedLabel(new Rect(resultsCard.x, resultsCard.y + h * 0.09f, resultsCard.width, h * 0.08f), $"{bestScore:N0} PTS", scoreStyle, 3f);
            JungleUITheme.DrawShadowedLabel(new Rect(resultsCard.x, resultsCard.y + h * 0.18f, resultsCard.width, h * 0.05f), currentRankLabel, rankStyle, 2f);

            Rect retryBtn = new Rect(resultsCard.x + resultsCard.width * 0.10f, resultsCard.y + h * 0.26f, resultsCard.width * 0.80f, h * 0.075f);
            if (JungleRetainedUI.Button(retryBtn, "REINTENTAR", JungleUITheme.ButtonSelected))
            {
                JungleAudioBank.PlayUiClick();
                StartCountdown();
            }

            Rect menuBtn = new Rect(resultsCard.x + resultsCard.width * 0.10f, resultsCard.y + h * 0.35f, resultsCard.width * 0.80f, h * 0.075f);
            if (JungleRetainedUI.Button(menuBtn, "VOLVER AL MENÚ", JungleUITheme.Button))
            {
                JungleAudioBank.PlayUiClick();
                SceneNavigator.Load(SceneNavigator.MainMenu);
            }
        }

        // Botón volver al menú siempre visible abajo a la izquierda
        if (state != State.Results)
        {
            Rect exitBtn = new Rect(w * 0.05f, h * 0.88f, w * 0.20f, h * 0.07f);
            if (JungleRetainedUI.Button(exitBtn, "← MENÚ", JungleUITheme.Button))
            {
                JungleAudioBank.PlayUiClick();
                SceneNavigator.Load(SceneNavigator.MainMenu);
            }
        }
    }

    private void DrawTowerGraduations(Rect r)
    {
        string[] labels = { "10,000 (CAMPANA)", "7,500 (PERFECT)", "5,000 (GREAT)", "2,500 (GOOD)", "0 (SUAVE)" };
        for (int i = 0; i < labels.Length; i++)
        {
            float y = r.y + i * (r.height / (labels.Length - 1));
            Rect line = new Rect(r.x, y - 10f, r.width, 20f);
            JungleUITheme.DrawShadowedLabel(line, labels[i], meterLabelStyle, 1.5f);
        }
    }
}
