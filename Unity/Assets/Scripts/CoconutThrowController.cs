using System;
using UnityEngine;
using GorillaEscape.Gameplay.Input;

[RequireComponent(typeof(JunglePanel))]
public sealed class CoconutThrowController : MonoBehaviour, IJungleScreen
{
    private enum State { Intro, Countdown, Aiming, Flying, Landed, Results }

    private MotionSession session;
    private State state = State.Intro;

    // Escenario y objetos 3D
    private GameObject throwFieldRoot;
    private GameObject circleRing;
    private GameObject coconutBall;
    private GameObject trajectoryMarker;
    private Material groundMat;
    private Material boundaryMat;
    private Material coconutMat;
    private Material markerMat;
    private readonly GorillaMascotRig gorillaJudge = new GorillaMascotRig();

    // Cámara cinemática
    private Camera mainCamera;
    private Vector3 initialCamPos = new Vector3(0f, 2.5f, -6f);
    private Quaternion initialCamRot = Quaternion.Euler(12f, 0f, 0f);

    // Parámetros de física de lanzamiento de coco (Spec v4 - Sección 18)
    private const int MaxAttempts = 3;
    private int currentAttempt = 0;
    private readonly float[] attemptDistances = new float[MaxAttempts];
    private float bestDistance = 0f;
    private float currentDistance = 0f;
    private bool wasFoul = false;
    private string lastFeedbackText = "";

    // Simulación del coco
    private Vector3 ballPosition;
    private Vector3 ballVelocity;
    private Vector3 flightOrigin, initialVelocity;
    private float landingTime, matchedTiming;
    private bool inAir;
    private float flightTime;
    private const float Gravity = -9.81f;

    // Variables de lanzamiento (Spec v4)
    private float elevationAngle = 42f; // Ángulo óptimo 40-45°
    private float lateralAngle = 0f;
    private float stateAt;

    // Input fusion
    private Vector2 cursorViewport = new Vector2(0.5f, 0.5f);
    private readonly HandVisualPrediction handVisual = new HandVisualPrediction();
    private Vector2 VisualHand => JungleUITheme.VisualHand(session, handVisual, cursorViewport);

    private float handSpeed;
    private float phonePower;
    private long inputRevision;
    private bool throwRequested;

    // Audio
    private AudioSource sfxSource;
    private AudioClip whackClip;
    private AudioClip popClip;
    private AudioClip tickClip;
    private AudioClip goClip;
    private int lastCountdownBeep = -1;

    // GUI Styles
    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle scoreStyle;
    private GUIStyle distanceStyle;
    private int lastWidth, lastHeight;

    private void Awake()
    {
        session = MotionSession.Ensure();
        session.ConfigureForScene(SceneNavigator.CoconutThrow);
        SetupAudio();
        BuildShotPutField();
        stateAt = Time.time;
    }

    private void SetupAudio()
    {
        sfxSource = JungleAudioBank.Attach2DSource(gameObject);
        whackClip = JungleAudioBank.WhackHit;
        popClip = JungleAudioBank.WhackPop;
        tickClip = JungleAudioBank.CountdownTick;
        goClip = JungleAudioBank.CountdownGo;
    }

    private void BuildShotPutField()
    {
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.orthographic = false;
            mainCamera.fieldOfView = 55f;
            ResetCamera();
            mainCamera.backgroundColor = new Color(0.08f, 0.18f, 0.12f);
        }

        JungleArenaEnvironment.Build(transform,0f,25f,70f);
        throwFieldRoot = new GameObject("01 Campo Lanzamiento Coco");
        throwFieldRoot.transform.SetParent(transform, false);

        groundMat = JungleStageBuilder.GetSharedMaterial(new Color(0.25f, 0.42f, 0.18f), 0.05f, 0.2f);
        var sandMat = JungleStageBuilder.GetSharedMaterial(new Color(0.78f, 0.68f, 0.45f), 0.05f, 0.15f);
        boundaryMat = JungleStageBuilder.GetSharedMaterial(new Color(0.95f, 0.95f, 0.95f), 0.1f, 0.3f);
        coconutMat = JungleStageBuilder.GetSharedMaterial(new Color(0.38f, 0.22f, 0.10f), 0.1f, 0.25f);
        markerMat = JungleStageBuilder.GetSharedMaterial(new Color(1f, 0.3f, 0.2f), 0.2f, 0.6f);

        // 1. Terreno general (césped de la selva)
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Pasto";
        ground.transform.SetParent(throwFieldRoot.transform, false);
        ground.transform.position = new Vector3(0f, -0.5f, 35f);
        ground.transform.localScale = new Vector3(50f, 1f, 90f);
        ground.GetComponent<Renderer>().sharedMaterial = groundMat;

        // 2. Círculo de lanzamiento (Shot Put Ring)
        var circleFloor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        circleFloor.name = "Suelo Circulo Lanzamiento";
        circleFloor.transform.SetParent(throwFieldRoot.transform, false);
        circleFloor.transform.position = new Vector3(0f, 0.01f, 0f);
        circleFloor.transform.localScale = new Vector3(2.5f, 0.02f, 2.5f);
        circleFloor.GetComponent<Renderer>().sharedMaterial = sandMat;

        circleRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        circleRing.name = "Borde Circulo";
        circleRing.transform.SetParent(throwFieldRoot.transform, false);
        circleRing.transform.position = new Vector3(0f, 0.05f, 0f);
        circleRing.transform.localScale = new Vector3(2.6f, 0.08f, 2.6f);
        circleRing.GetComponent<Renderer>().sharedMaterial = boundaryMat;

        // Tabla de contención frontal (Toeboard)
        var toeBoard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        toeBoard.name = "Tabla Contencion";
        toeBoard.transform.SetParent(throwFieldRoot.transform, false);
        toeBoard.transform.position = new Vector3(0f, 0.12f, 1.25f);
        toeBoard.transform.localScale = new Vector3(1.4f, 0.22f, 0.18f);
        toeBoard.GetComponent<Renderer>().sharedMaterial = boundaryMat;

        // 3. Sector de lanzamiento (líneas delimitadoras angulares de 35°)
        float sectorLength = 65f;
        float angleRad = 18f * Mathf.Deg2Rad;
        float halfWidth = Mathf.Sin(angleRad) * sectorLength;

        CreateBoundaryLine("Linea Sector Izq", new Vector3(0f, 0.02f, 0f), new Vector3(-halfWidth, 0.02f, sectorLength));
        CreateBoundaryLine("Linea Sector Der", new Vector3(0f, 0.02f, 0f), new Vector3(halfWidth, 0.02f, sectorLength));

        // 4. Arcos de distancia cada 10 metros (10m, 20m, 30m, 40m, 50m, 60m)
        for (int m = 10; m <= 60; m += 10)
        {
            CreateDistanceArc(m);
        }

        // 5. El Coco (Coconut Ball con malla 3D oficial Meshy AI)
        coconutBall = new GameObject("Coco Balístico");
        coconutBall.transform.SetParent(throwFieldRoot.transform, false);
        coconutBall.transform.position = new Vector3(0f, 1.2f, 0.2f);
        const string cocoFolder = "Jungle/NewModels/Coconut/Meshy_AI_Coconut_Bowling_Ball_20261026_texture_fbx/";
        var cocoPrefab = JungleAssets.Load<GameObject>(cocoFolder + "Meshy_AI_Coconut_Bowling_Ball_20261026_texture");
        var cocoTex = JungleAssets.Load<Texture2D>(cocoFolder + "Meshy_AI_Coconut_Bowling_Ball_20261026_texture");
        var cocoNormal = JungleAssets.Load<Texture2D>(cocoFolder + "Meshy_AI_Coconut_Bowling_Ball_20261026_texture_normal");
        if (cocoPrefab != null)
        {
            var cocoModel = Instantiate(cocoPrefab, coconutBall.transform, false);
            cocoModel.name = "Modelo Coco 3D";
            JungleStageBuilder.DisableChildColliders(cocoModel);
            var rs = cocoModel.GetComponentsInChildren<Renderer>(true);
            if (rs.Length > 0)
            {
                Bounds b = JungleStageBuilder.CombinedBounds(rs);
                float scale = 0.62f / Mathf.Max(0.01f, Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z)));
                cocoModel.transform.localScale = Vector3.one * scale;
                b = JungleStageBuilder.CombinedBounds(rs);
                cocoModel.transform.position += coconutBall.transform.position - b.center;
                if (cocoTex != null)
                {
                    coconutMat = JungleStageBuilder.CreateTexturedMaterial(cocoTex, "Coco Balístico 3D", 0.18f, cocoNormal, 0.7f);
                    JungleStageBuilder.ApplySharedMaterial(rs, coconutMat);
                }
            }
        }
        else
        {
            var fallbackSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fallbackSphere.transform.SetParent(coconutBall.transform, false);
            fallbackSphere.transform.localScale = new Vector3(0.55f, 0.65f, 0.55f);
            fallbackSphere.GetComponent<Renderer>().sharedMaterial = coconutMat;
            JungleStageBuilder.StripCollider(fallbackSphere);
        }

        // 6. Marcador de impacto en suelo
        trajectoryMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trajectoryMarker.name = "Marcador Impacto";
        trajectoryMarker.transform.SetParent(throwFieldRoot.transform, false);
        trajectoryMarker.transform.position = new Vector3(0f, -10f, 0f);
        trajectoryMarker.transform.localScale = new Vector3(0.6f, 0.05f, 0.6f);
        trajectoryMarker.GetComponent<Renderer>().sharedMaterial = markerMat;
        trajectoryMarker.SetActive(false);

        // 7. Gorila Juez / Espectador 3D al costado del círculo de lanzamiento
        var judgeAnchor = new GameObject("07 Gorila Juez Lanzamiento").transform;
        judgeAnchor.SetParent(throwFieldRoot.transform, false);
        judgeAnchor.position = new Vector3(-3.2f, 0f, 1.8f);
        judgeAnchor.rotation = Quaternion.Euler(0f, 35f, 0f);
        gorillaJudge.LoadMascot(judgeAnchor, judgeAnchor.position, judgeAnchor.rotation, 2.5f, "Gorila Juez Coco");
    }

    private void CreateBoundaryLine(string name, Vector3 start, Vector3 end)
    {
        var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
        line.name = name;
        line.transform.SetParent(throwFieldRoot.transform, false);
        Vector3 mid = (start + end) * 0.5f;
        line.transform.position = mid;
        line.transform.LookAt(end);
        float len = Vector3.Distance(start, end);
        line.transform.localScale = new Vector3(0.12f, 0.04f, len);
        line.GetComponent<Renderer>().sharedMaterial = boundaryMat;
    }

    private void CreateDistanceArc(float meters)
    {
        var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = $"Arco_{meters}m";
        marker.transform.SetParent(throwFieldRoot.transform, false);
        marker.transform.position = new Vector3(0f, 0.03f, meters);
        marker.transform.localScale = new Vector3(meters * 0.72f, 0.04f, 0.25f);
        marker.GetComponent<Renderer>().sharedMaterial = boundaryMat;

        // Postes indicadores en los extremos
        var postL = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        postL.transform.SetParent(throwFieldRoot.transform, false);
        postL.transform.position = new Vector3(-meters * 0.36f, 0.4f, meters);
        postL.transform.localScale = new Vector3(0.15f, 0.4f, 0.15f);
        postL.GetComponent<Renderer>().sharedMaterial = markerMat;

        var postR = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        postR.transform.SetParent(throwFieldRoot.transform, false);
        postR.transform.position = new Vector3(meters * 0.36f, 0.4f, meters);
        postR.transform.localScale = new Vector3(0.15f, 0.4f, 0.15f);
        postR.GetComponent<Renderer>().sharedMaterial = markerMat;
    }

    private void ResetCamera()
    {
        if (mainCamera == null) return;
        mainCamera.transform.position = initialCamPos;
        mainCamera.transform.rotation = initialCamRot;
    }

    private void ResetBall()
    {
        inAir = false;
        ballPosition = new Vector3(0f, 1.2f, 0.2f);
        ballVelocity = Vector3.zero;
        if (coconutBall != null)
        {
            coconutBall.transform.position = ballPosition;
            coconutBall.SetActive(true);
        }
        ResetCamera();
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
            fontSize = Mathf.RoundToInt(h * 0.080f),
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.96f, 0.35f) }
        };

        distanceStyle = new GUIStyle(JungleUITheme.Title)
        {
            fontSize = Mathf.RoundToInt(h * 0.060f),
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold
        };
    }

    private void Update()
    {
        float? progress = state == State.Aiming || state == State.Flying || state == State.Landed
            ? Mathf.Clamp01((float)currentAttempt / MaxAttempts)
            : (float?)null;

        session.PresentGame(
            state == State.Intro ? "Ready" :
            state == State.Countdown ? "Countdown" :
            state == State.Results ? "Finished" : "Playing",
            bestDistance > 0f ? (double?)Mathf.Round(bestDistance * 10f) : null,
            progress: progress,
            distance: bestDistance);

        if (!session.IsInputReady)
        {
            handSpeed = phonePower = 0f;
            throwRequested = false;
            ReadKeyboardAim();
            UpdateVisuals();
            return;
        }

        throwRequested = false;
        if (session.TryConsumeFrame(out var frame))
        {
            ReadInput(frame);
        }

        ReadKeyboardAim();
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
        else if (state == State.Aiming)
        {
            if (throwRequested)
            {
                ExecuteThrow();
            }
        }
        else if (state == State.Flying)
        {
            SimulateFlight();
        }
        else if (state == State.Landed)
        {
            if (Time.time - stateAt >= 3.0f)
            {
                currentAttempt++;
                if (currentAttempt >= MaxAttempts)
                {
                    session.SetGameplayActive(false);
                    state = State.Results;
                    stateAt = Time.time;
                }
                else
                {
                    ResetBall();
                    state = State.Aiming;
                }
            }
        }
    }

    private void ReadInput(JungleFusedFrame frame)
    {
        inputRevision = frame.Revision;
        matchedTiming = CompetitiveMechanics.TimingQuality(frame.PhoneTimestamp,frame.CameraTimestamp);
        handSpeed = frame.Right.Speed;
        phonePower = frame.PhonePower;
        cursorViewport = new Vector2(frame.Right.X, 1f - frame.Right.Y);
        // Score and trajectory use the accepted pose; prediction is only for drawing.
        elevationAngle = Mathf.Clamp(cursorViewport.y*60f+15f,20f,65f);
        lateralAngle = Mathf.Clamp((cursorViewport.x-.5f)*28f,-14f,14f);

        if (frame.NewPose || frame.Baseline)
            handVisual.Observe(cursorViewport.x, cursorViewport.y, frame.Right.VelocityX, -frame.Right.VelocityY, Time.unscaledTime);

        if ((frame.Actions & JungleAction.Throw) != 0)
            throwRequested = true;
    }

    private void ReadKeyboardAim()
    {
        // Apuntado por teclado; el lanzamiento sigue requiriendo una acción física autorizada.
        if (state == State.Aiming)
        {
            if (Input.GetKey(KeyCode.UpArrow)) elevationAngle = Mathf.Min(65f, elevationAngle + 20f * Time.deltaTime);
            if (Input.GetKey(KeyCode.DownArrow)) elevationAngle = Mathf.Max(20f, elevationAngle - 20f * Time.deltaTime);
            if (Input.GetKey(KeyCode.LeftArrow)) lateralAngle = Mathf.Max(-14f, lateralAngle - 15f * Time.deltaTime);
            if (Input.GetKey(KeyCode.RightArrow)) lateralAngle = Mathf.Min(14f, lateralAngle + 15f * Time.deltaTime);
        }
    }

    private void UpdateVisuals()
    {
        // Posición del coco en reposo antes del lanzamiento
        if (state == State.Aiming && coconutBall != null)
        {
            float idleBob = Mathf.Sin(Time.time * 4f) * 0.05f;
            coconutBall.transform.position = new Vector3(0f, 1.2f + idleBob, 0.2f);
        }
        gorillaJudge.Animate(false, 0, 0);
    }

    private void ExecuteThrow()
    {
        if (state != State.Aiming) return;

        bool authorized = session.TryAuthorize(JungleAction.Throw, inputRevision);

        if (!authorized) return;

        if (sfxSource != null && whackClip != null)
            sfxSource.PlayOneShot(whackClip, 0.85f);

        JungleStageBuilder.PulseSquash(coconutBall, 0.72f, 1.25f, 26f, 10f);

        // Fórmula oficial Spec v4 Sección 18:
        // AngleQuality = 1 - normalizedDistanceFromOptimalAngle (óptimo ~43°)
        // ReleaseQuality = Timing
        // DirectionQuality = 1 - abs(Direction)
        // LaunchPower = Power * 0.60 + AngleQuality * 0.20 + ReleaseQuality * 0.15 + DirectionQuality * 0.05
        float launchPower = CompetitiveMechanics.ThrowPower(handSpeed,phonePower,elevationAngle,lateralAngle,matchedTiming);

        // Velocidad inicial física (m/s) entre 12 y 25 m/s
        float v0 = Mathf.Lerp(12f, 25.5f, launchPower);

        float elevRad = elevationAngle * Mathf.Deg2Rad;
        float latRad = lateralAngle * Mathf.Deg2Rad;

        ballVelocity = new Vector3(
            Mathf.Sin(latRad) * Mathf.Cos(elevRad) * v0,
            Mathf.Sin(elevRad) * v0,
            Mathf.Cos(latRad) * Mathf.Cos(elevRad) * v0
        );

        ballPosition = coconutBall.transform.position;
        flightOrigin = ballPosition; initialVelocity = ballVelocity;
        landingTime = (float)CompetitiveMechanics.GroundTime(flightOrigin.y,initialVelocity.y,.25f,Gravity);
        inAir = true;
        flightTime = 0f;
        state = State.Flying;
        stateAt = Time.time;
    }

    private void SimulateFlight()
    {
        if (!inAir) return;

        float dt = Time.deltaTime;
        flightTime = Mathf.Min(landingTime,flightTime+dt);
        ballPosition = new Vector3(
            (float)CompetitiveMechanics.Position(flightOrigin.x,initialVelocity.x,0,flightTime),
            (float)CompetitiveMechanics.Position(flightOrigin.y,initialVelocity.y,Gravity,flightTime),
            (float)CompetitiveMechanics.Position(flightOrigin.z,initialVelocity.z,0,flightTime));

        if (coconutBall != null)
        {
            coconutBall.transform.position = ballPosition;
            coconutBall.transform.Rotate(Vector3.right * (280f * dt) + Vector3.up * (150f * dt));
        }

        // Cámara de seguimiento suave (Spec v4 Sección 27)
        if (mainCamera != null)
        {
            Vector3 camTarget = ballPosition + new Vector3(0f, 2.2f, -5.2f);
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, camTarget, 10f * dt);
            mainCamera.transform.LookAt(ballPosition + Vector3.forward * 2f);
        }

        // Impacto contra el suelo (y <= 0.25f)
        if (flightTime >= landingTime)
        {
            ballPosition.y = 0.25f;
            inAir = false;
            OnBallLanded();
        }
    }

    private void OnBallLanded()
    {
        if (sfxSource != null && popClip != null)
            sfxSource.PlayOneShot(popClip, 0.9f);

        JungleStageBuilder.ShakeCamera(0.25f, 0.20f);

        // Medir distancia en metros desde el origen (0,0,0)
        currentDistance = Mathf.Round(Vector3.Distance(Vector3.zero, new Vector3(ballPosition.x, 0f, ballPosition.z)) * 10f) / 10f;

        // Evaluación de falta / sector (ángulo de sector de 35°)
        float tanAngle = Mathf.Abs(ballPosition.x) / Mathf.Max(0.1f, ballPosition.z);
        wasFoul = tanAngle > 0.325f || currentDistance < 1.5f;

        if (wasFoul)
        {
            currentDistance = 0f;
            lastFeedbackText = "¡FALTA! (FUERA DE SECTOR)";
            gorillaJudge.PlayAngryStompAnimation();
        }
        else
        {
            if (currentDistance > bestDistance)
                bestDistance = currentDistance;

            if (currentDistance > 45f)
                lastFeedbackText = "¡RÉCORD DE LA ISLA! (LEGENDARY)";
            else if (currentDistance > 35f)
                lastFeedbackText = "¡LANZAMIENTO PERFECTO! (PERFECT)";
            else if (currentDistance > 25f)
                lastFeedbackText = "¡GRAN POTENCIA! (GREAT)";
            else
                lastFeedbackText = "¡BUEN LANZAMIENTO! (GOOD)";

            if (currentDistance >= 25f)
                gorillaJudge.PlayCelebrationAnimation();
        }

        attemptDistances[currentAttempt] = currentDistance;

        // Colocar marcador visual en el sitio de impacto
        if (trajectoryMarker != null)
        {
            trajectoryMarker.transform.position = new Vector3(ballPosition.x, 0.05f, ballPosition.z);
            trajectoryMarker.SetActive(true);
            JungleStageBuilder.PulseSquash(trajectoryMarker, 0.65f, 1.35f, 24f, 9f);
        }
        JungleStageBuilder.PulseSquash(coconutBall, 0.68f, 1.28f, 26f, 9f);

        state = State.Landed;
        stateAt = Time.time;
    }

    public void BeginFromPhone()
    {
        if (state == State.Intro)
            StartCountdown();
    }

    private void StartCountdown()
    {
        session.SetGameplayActive(true);
        session.ResetRound();
        state = State.Countdown;
        stateAt = Time.time;
        lastCountdownBeep = -1;
    }

    private void StartRound()
    {
        state = State.Aiming;
        currentAttempt = 0;
        bestDistance = 0f;
        for (int i = 0; i < MaxAttempts; i++) attemptDistances[i] = 0f;
        ResetBall();
    }

    public void RenderUI()
    {
        JungleUITheme.Ensure();
        float w = Screen.width, h = Screen.height;
        EnsureStyles(w, h);

        // Encabezado superior
        Rect headerRect = new Rect(w * 0.15f, h * 0.02f, w * 0.70f, h * 0.12f);
        JungleUITheme.DrawPanel(headerRect);
        JungleUITheme.DrawShadowedLabel(new Rect(headerRect.x, h * 0.025f, headerRect.width, h * 0.065f), "COCONUT THROW", titleStyle, 3f);
        string sub = state == State.Intro ? "LANZAMIENTO DE BALA CON COCO · MÁXIMA DISTANCIA" :
                     state == State.Countdown ? "¡PREPÁRATE PARA EL LANZAMIENTO!" :
                     state == State.Results ? "¡PRUEBA DE ATLETISMO COMPLETADA!" :
                     state == State.Flying ? "¡EL COCO ESTÁ EN EL AIRE!" :
                     $"INTENTO {currentAttempt + 1} DE {MaxAttempts} · AJUSTA ÁNGULO (~43°) E IMPULSA EL COCO";
        JungleUITheme.DrawShadowedLabel(new Rect(headerRect.x, h * 0.088f, headerRect.width, h * 0.04f), sub, subtitleStyle, 2f);

        // Panel de intentos / tabla a la izquierda
        Rect statusRect = new Rect(w * 0.05f, h * 0.20f, w * 0.28f, h * 0.52f);
        JungleUITheme.DrawPanel(statusRect);

        Rect statusTitleRect = new Rect(statusRect.x + 10f, statusRect.y + 12f, statusRect.width - 20f, h * 0.045f);
        JungleUITheme.DrawShadowedLabel(statusTitleRect, "DISTANCIAS VÁLIDAS", subtitleStyle, 2f);

        for (int i = 0; i < MaxAttempts; i++)
        {
            Rect attemptRow = new Rect(statusRect.x + 15f, statusRect.y + h * (0.07f + i * 0.08f), statusRect.width - 30f, h * 0.065f);
            bool isCurrent = (state == State.Aiming || state == State.Flying || state == State.Landed) && currentAttempt == i;
            JungleUITheme.DrawPanel(attemptRow, isCurrent);

            string distTxt = attemptDistances[i] > 0f ? $"{attemptDistances[i]:F1} M" : isCurrent ? "EN VUELO..." : "---";
            string labelTxt = $"INTENTO {i + 1}: {distTxt}";
            JungleUITheme.DrawShadowedLabel(attemptRow, labelTxt, subtitleStyle, 1.5f);
        }

        // Mejor distancia
        Rect bestBox = new Rect(statusRect.x + 15f, statusRect.y + h * 0.34f, statusRect.width - 30f, h * 0.14f);
        JungleUITheme.DrawPanel(bestBox, true);
        JungleUITheme.DrawShadowedLabel(new Rect(bestBox.x, bestBox.y + 6f, bestBox.width, h * 0.035f), "MEJOR MARCA", subtitleStyle, 1.5f);
        JungleUITheme.DrawShadowedLabel(new Rect(bestBox.x, bestBox.y + h * 0.045f, bestBox.width, h * 0.07f), $"{bestDistance:F1} M", scoreStyle, 2.5f);

        // Indicador de ángulo / parámetros durante apuntado
        if (state == State.Aiming)
        {
            Rect aimPanel = new Rect(w * 0.68f, h * 0.20f, w * 0.27f, h * 0.26f);
            JungleUITheme.DrawPanel(aimPanel);
            JungleUITheme.DrawShadowedLabel(new Rect(aimPanel.x, aimPanel.y + 10f, aimPanel.width, h * 0.04f), "PARÁMETROS", subtitleStyle, 2f);
            JungleUITheme.DrawShadowedLabel(new Rect(aimPanel.x, aimPanel.y + h * 0.07f, aimPanel.width, h * 0.045f), $"ELEVACIÓN: {elevationAngle:F0}°", subtitleStyle, 1.8f);
            JungleUITheme.DrawShadowedLabel(new Rect(aimPanel.x, aimPanel.y + h * 0.12f, aimPanel.width, h * 0.045f), $"DIRECCIÓN: {lateralAngle:F0}°", subtitleStyle, 1.8f);

            bool isOptimal = Mathf.Abs(elevationAngle - 43f) <= 4f;
            string tip = isOptimal ? "★ ¡ÁNGULO ÓPTIMO!" : "OBJETIVO: 40° - 45°";
            JungleUITheme.DrawShadowedLabel(new Rect(aimPanel.x, aimPanel.y + h * 0.18f, aimPanel.width, h * 0.045f), tip, subtitleStyle, 1.8f);
        }

        // Feedback al aterrizar
        if (state == State.Landed)
        {
            Rect feedbackPanel = new Rect(w * 0.35f, h * 0.76f, w * 0.32f, h * 0.11f);
            JungleUITheme.DrawPanel(feedbackPanel, true);
            string landTxt = wasFoul ? "¡FALTA EN EL TIRO!" : $"{currentDistance:F1} METROS\n{lastFeedbackText}";
            distanceStyle.normal.textColor = wasFoul ? new Color(1f, 0.3f, 0.3f) : new Color(1f, 0.88f, 0.2f);
            JungleUITheme.DrawShadowedLabel(feedbackPanel, landTxt, distanceStyle, 2.5f);
        }

        // Pantalla de Intro
        if (state == State.Intro)
        {
            Rect introCard = new Rect(w * 0.30f, h * 0.35f, w * 0.40f, h * 0.32f);
            JungleUITheme.DrawPanel(introCard);
            Rect introText = new Rect(introCard.x + 15f, introCard.y + 20f, introCard.width - 30f, h * 0.12f);
            JungleUITheme.DrawShadowedLabel(introText, "Sujeta el teléfono firmemente.\nApunta a ~43° y haz un empuje controlado hacia adelante, sin soltar el teléfono.", subtitleStyle, 2f);

            Rect startBtn = new Rect(introCard.x + introCard.width * 0.15f, introCard.y + h * 0.18f, introCard.width * 0.70f, h * 0.08f);
            if (JungleRetainedUI.Button(startBtn, "¡INICIAR LANZAMIENTO!", JungleUITheme.ButtonSelected))
            {
                JungleAudioBank.PlayUiClick();
                StartCountdown();
            }
        }
        else if (state == State.Results)
        {
            if (TournamentSession.IsActive)
            {
                TournamentSession.RecordRoundResult(Mathf.RoundToInt(bestDistance));
                Vector2 normCursor = new Vector2(0.5f, 0.5f);
            string handLabel = "MANO";
            bool handRaised = false, handClosed = false, forwardPush = false;
            if (session != null) session.TryCameraCursor(out normCursor, out handLabel, out handRaised, out handClosed, out forwardPush);
                TournamentSession.DrawTournamentOverlay(w, h, normCursor, handLabel, handRaised, handClosed, forwardPush, session != null && session.PhoneTouchTriggered);
                return;
            }
            Rect resultsCard = new Rect(w * 0.28f, h * 0.25f, w * 0.44f, h * 0.46f);
            JungleUITheme.DrawPanel(resultsCard, true);

            JungleUITheme.DrawShadowedLabel(new Rect(resultsCard.x, resultsCard.y + 15f, resultsCard.width, h * 0.06f), "RESULTADO FINAL", titleStyle, 2.5f);
            JungleUITheme.DrawShadowedLabel(new Rect(resultsCard.x, resultsCard.y + h * 0.09f, resultsCard.width, h * 0.08f), $"{bestDistance:F1} METROS", scoreStyle, 3f);
            JungleUITheme.DrawShadowedLabel(new Rect(resultsCard.x, resultsCard.y + h * 0.18f, resultsCard.width, h * 0.05f), lastFeedbackText, subtitleStyle, 2f);

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

    private void OnDestroy()
    {
        gorillaJudge.Dispose();
    }
}
