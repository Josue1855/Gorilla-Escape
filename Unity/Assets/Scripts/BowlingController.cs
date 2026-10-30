using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GorillaEscape.Gameplay.Input;
using UnityEngine.SceneManagement;
using GLTFast;

[RequireComponent(typeof(JunglePanel))]
public sealed partial class BowlingController : MonoBehaviour, IJungleScreen
{
    private JungleSceneBindings SceneBindings => GetComponent<JungleSceneBindings>();
    private enum GameState { Connecting, Aim, Rolling, Results }
    private readonly List<GameObject> pins = new List<GameObject>();
    private readonly Dictionary<GameObject, Vector3> pinRackPositions = new Dictionary<GameObject, Vector3>();
    private readonly Dictionary<GameObject, Quaternion> pinRackRotations = new Dictionary<GameObject, Quaternion>();
    private readonly HashSet<GameObject> pinsFallenThisRoll = new HashSet<GameObject>();
    private readonly List<Vector3> poseTrail = new List<Vector3>();
    private readonly List<int>[] bowlingFrames = new List<int>[10];
    private readonly int[] bowlingScoreCache = new int[10];
    private readonly int[] bowlingRollCache = new int[21];
    private readonly GorillaMascotRig gorillaRig = new GorillaMascotRig();
    private Rigidbody ballBody;
    private GameObject ballVisual;
    private GameObject islandBackground;
    private BowlingHybridEnvironment hybridBowlingEnvironment;
    private GltfImport islandImport;
    private Material totemPinSharedMaterial;
    private Material bushSharedMaterial, palmSharedMaterial, coconutModelMaterial;
    private GameObject newVegetationRoot;
    private bool bowlingRackTransitioning;
    private string connectionState = "PULSA CONECTAR";
    private string roomCode = "";
    private GameState state = GameState.Connecting;
    private bool polling, calibrated, hasRaw;
    private float lastPacketAt = -10f, centerGamma, centerBeta, latestGamma, latestBeta, aimX, power, resultAt;
    private float aimReadyAt;
    private float ballPassedPinsAt = -1f;
    private float poseWristX = .5f, poseBodyX = .5f;
    private readonly HandVisualPrediction leftVisual = new HandVisualPrediction(), rightVisual = new HandVisualPrediction();
    private readonly Vector3 bowlingCameraPosition = new Vector3(0, 6.4f, -12.5f);
    private float lastLaunchDirection, bowlingSpin;
    private int totalScore, lastPins, lastConfirm, lastRecenter;
    private int bowlingFrame, bowlingBall = 1;
    private bool confirmInitialized;
    private bool poseDetected;
    private bool bowlingGameOver, nextNeedsFullRack;
    private bool editableScene;
    private Transform gorillaAnchor;
    private Transform islandAnchor;
    private Quaternion rawRotation = Quaternion.identity;
    private GUIStyle titleStyle, bigStyle, bodyStyle, buttonStyle;
    private GUIStyle dashHeadingStyle, dashRowStyle, dashRowActiveStyle, dashScoreStyle, dashScoreActiveStyle;
    private GUIStyle hudBrandStyle, hudFrameStyle, hudMessageStyle, hudButtonStyle, hudConnectButtonStyle;
    private GUIStyle hudBoxStyle, cardBoxStyle, selectedBoxStyle, statusBoxStyle;
    private GUIStyle pointerStyle, cameraStatusStyle, gorillaReactionStyle;
    private int lastScreenWidth, lastScreenHeight;
    private Texture2D panelTexture, accentTexture, selectedTexture, optionTexture, darkTexture;
    private Texture2D bowlingHudTexture, bowlingCardTexture, bowlingButtonTexture, bowlingButtonHoverTexture;
    private Font hudFont;
    private AudioSource sfxSource;
    private MotionSession sharedSession;
    private JungleFusedFrame inputFrame;

    private void Awake()
    {
        for(int i=0;i<bowlingFrames.Length;i++)bowlingFrames[i]=new List<int>();
        sharedSession=MotionSession.Ensure();sfxSource=JungleAudioBank.Attach2DSource(gameObject);
        roomCode=sharedSession.RoomCode;polling=sharedSession.IsPolling;
        editableScene=SceneBindings!=null&&SceneBindings.EditableRoot!=null;
        if(editableScene)BindEditableScene();else BuildBowlingAlley();
        hybridBowlingEnvironment=gameObject.AddComponent<BowlingHybridEnvironment>();
        hybridBowlingEnvironment.Bind(SceneBindings);ResetRack();
        ApplyBowlingSurfaceTextures();CreateNewBowlingVegetation();ApplyCoconutModel();
        gorillaRig.LoadAsync(gorillaAnchor,MakeMaterial,hybridBowlingEnvironment);
    }

    private void TriggerGorillaReaction(bool strike) => gorillaRig.TriggerBowlingReaction(strike, hybridBowlingEnvironment);

    private void AnimateGorilla() => gorillaRig.Animate(false,0,0);

    private void ResetRack()
    {
        pinsFallenThisRoll.Clear();
        if (pins.Count > 0)
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
        CreatePins();
        ResetBallOnly();
    }

    private void ResetBallOnly()
    {
        bowlingSpin = 0;
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
        gorillaRig.ResetToRest(Vector3.zero);
        for (int i = 0; i < bowlingFrames.Length; i++) bowlingFrames[i].Clear();
        bowlingFrame = 0; bowlingBall = 1; totalScore = 0; lastPins = 0;
        bowlingGameOver = false; nextNeedsFullRack = true;
        ResetRack();
    }



    private void Connect()
    {
        sharedSession.Connect();
        polling = true;
        connectionState = sharedSession.Status;
    }

    private void Update()
    {
        float liveSpin=state==GameState.Aim&&calibrated?ShotMechanics.BowlingSpin(Mathf.DeltaAngle(centerGamma,latestGamma)):bowlingSpin;
        sharedSession.PresentGame(bowlingGameOver?"Finished":state==GameState.Connecting?"Ready":"Playing",score:GetDisplayedBowlingScore(),progress:Mathf.Clamp01(bowlingFrame/10f),balance:liveSpin);
        if(!sharedSession.IsInputReady)
        {
            poseDetected=hasRaw=calibrated=false;power=0;poseTrail.Clear();
            leftVisual.Reset();rightVisual.Reset();
            return;
        }
        if(sharedSession.TryConsumeFrame(out var frame))
        {
            inputFrame=frame;
            if(frame.Baseline){poseTrail.Clear();calibrated=confirmInitialized=false;leftVisual.Reset();rightVisual.Reset();}
            power=frame.PhonePower;
            ReadPose(frame);
            ReadPhone(frame);
        }
        AnimateGorilla();
        if (state == GameState.Aim)
        {
            HandVisualPrediction activeVisual = sharedSession.HandMode == MotionSession.HandTrackingMode.LeftOnly ? leftVisual : rightVisual;
            if (CameraActive && activeVisual.TryPosition(Time.unscaledTime, out float visualWristX, out _))
            {
                float handX = Mathf.Clamp01(.5f + (visualWristX - .5f) * 1.55f);
                aimX = Mathf.Lerp(-3.05f, 3.05f, Mathf.InverseLerp(.08f, .92f, handX));
            }
            transform.position = Vector3.Lerp(transform.position, new Vector3(aimX, .48f, -7.2f), 16f * Time.deltaTime);
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
            if (pose.leftDetected)
            {
                if (frame.NewPose || frame.Baseline) leftVisual.Observe(newLeft.x, newLeft.y, frame.Left.VelocityX, frame.Left.VelocityY, now);
            }
            else leftVisual.Reset();
            if (pose.rightDetected)
            {
                if (frame.NewPose || frame.Baseline) rightVisual.Observe(newRight.x, newRight.y, frame.Right.VelocityX, frame.Right.VelocityY, now);
            }
            else rightVisual.Reset();
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
        if (state == GameState.Aim)
            aimX = Mathf.Lerp(-3.05f, 3.05f, Mathf.InverseLerp(.08f, .92f, hand.x));
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
            if(state==GameState.Connecting)state=GameState.Aim;
        }
        if(p.recenter!=lastRecenter){lastRecenter=p.recenter;Calibrate();}
        power=frame.PhonePower;
        if (state == GameState.Aim && Time.time >= aimReadyAt)
        {
            if ((frame.Actions&JungleAction.Throw)!=0)
            {
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
            direction = Mathf.Clamp(trajectoryX * 6.2f + handSide * .55f, -1f, 1f);
        }
        else return;
        lastLaunchDirection = direction;
        bowlingSpin=ShotMechanics.BowlingSpin(Mathf.DeltaAngle(centerGamma,latestGamma));
        ballBody.linearVelocity = new Vector3(direction * 3.1f, 0, speed);
        ballBody.angularVelocity = new Vector3(speed * 1.4f, bowlingSpin*5f, -direction * 7.5f);
        if (sfxSource != null) sfxSource.PlayOneShot(JungleAudioBank.BowlingRoll, .70f);
        if (ballVisual != null && ballVisual != gameObject) JungleStageBuilder.PulseSquash(ballVisual, .80f, 1.18f, 26f, 9f);
        resultAt = Time.time + 7.0f;
    }

    private void FixedUpdate()
    {
        if(state!=GameState.Rolling||ballBody==null||ballBody.isKinematic||transform.position.y>1.2f||transform.position.z>10.7f)return;
        ballBody.AddForce(Vector3.right*ShotMechanics.BowlingCurveAcceleration(bowlingSpin),ForceMode.Acceleration);
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
        if (sfxSource != null && fallen > 0) sfxSource.PlayOneShot(rackCleared ? JungleAudioBank.BowlingStrike : JungleAudioBank.BowlingPins, rackCleared ? .90f : .75f);
        if (fallen > 0) JungleStageBuilder.ShakeCamera(rackCleared ? .16f : .085f, rackCleared ? .24f : .14f);
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
                    pin.SetActive(false);
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

    private Texture2D Solid(Color color) => JungleUITheme.SolidColor(color);

    private void PrepareStyles()
    {
        JungleUITheme.Ensure();
        if (hudFont == null) hudFont = JungleUITheme.TextFont != null ? JungleUITheme.TextFont : Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial" }, 32);
        if (panelTexture == null) panelTexture = JungleUITheme.WoodPanelRounded;
        if (accentTexture == null) accentTexture = JungleUITheme.ButtonGoldRounded;
        if (selectedTexture == null) selectedTexture = JungleUITheme.ButtonGoldRounded;
        if (optionTexture == null) optionTexture = JungleUITheme.BambooCardRounded;
        if (darkTexture == null) darkTexture = Solid(new Color(.12f, .055f, .02f, 1f));
        if (sharedBowlingHudTexture == null) sharedBowlingHudTexture = JungleUITheme.WoodPanelRounded;
        if (sharedBowlingCardTexture == null) sharedBowlingCardTexture = JungleUITheme.BambooCardRounded;
        if (sharedBowlingButtonTexture == null) sharedBowlingButtonTexture = JungleUITheme.ButtonNormalRounded;
        if (sharedBowlingButtonHoverTexture == null) sharedBowlingButtonHoverTexture = JungleUITheme.ButtonHoverRounded;
        bowlingHudTexture = sharedBowlingHudTexture;
        bowlingCardTexture = sharedBowlingCardTexture;
        bowlingButtonTexture = sharedBowlingButtonTexture;
        bowlingButtonHoverTexture = sharedBowlingButtonHoverTexture;

        if (titleStyle != null && lastScreenWidth == Screen.width && lastScreenHeight == Screen.height) return;
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        float w = Screen.width, h = Screen.height;
        float scale = Mathf.Clamp(Mathf.Min(w / 1920f, h / 1080f), .65f, 1.35f);

        titleStyle = new GUIStyle(JungleRetainedUI.skin.label) { font = JungleUITheme.TitleFont ?? hudFont, fontSize = 32, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1f, .84f, .18f) } };
        bigStyle = new GUIStyle(JungleRetainedUI.skin.label) { font = JungleUITheme.TitleFont ?? hudFont, fontSize = 38, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1f, .96f, .86f) } };
        bodyStyle = new GUIStyle(JungleRetainedUI.skin.label) { font = JungleUITheme.TextFont ?? hudFont, fontSize = 21, alignment = TextAnchor.MiddleCenter, wordWrap = true, normal = { textColor = new Color(1f, .95f, .82f) } };
        buttonStyle = new GUIStyle(JungleUITheme.Button) { fontSize = 22 };

        hudBoxStyle = RoundedStyle(bowlingHudTexture, 24);
        cardBoxStyle = RoundedStyle(bowlingCardTexture, 18);
        selectedBoxStyle = RoundedStyle(selectedTexture, 18);
        statusBoxStyle = RoundedStyle(bowlingHudTexture, 18);

        hudBrandStyle = CrispLabel(Mathf.RoundToInt(18 * scale), new Color(1f, .84f, .18f), FontStyle.Bold, TextAnchor.MiddleLeft);
        hudFrameStyle = CrispLabel(Mathf.RoundToInt(29 * scale), new Color(1f, .96f, .86f), FontStyle.Bold, TextAnchor.MiddleLeft);
        hudMessageStyle = CrispLabel(Mathf.RoundToInt(14 * scale), new Color(.96f, .88f, .72f), FontStyle.Normal, TextAnchor.MiddleLeft);
        hudButtonStyle = CrispButton(Mathf.RoundToInt(13 * scale));
        hudConnectButtonStyle = CrispButton(Mathf.RoundToInt(14 * scale));

        dashHeadingStyle = CrispLabel(Mathf.RoundToInt(18 * scale), new Color(1f, .84f, .18f), FontStyle.Bold, TextAnchor.MiddleCenter);
        dashRowStyle = CrispLabel(Mathf.RoundToInt(12 * scale), new Color(.96f, .90f, .78f), FontStyle.Normal, TextAnchor.MiddleLeft);
        dashRowActiveStyle = CrispLabel(Mathf.RoundToInt(12 * scale), new Color(.18f, .08f, .01f), FontStyle.Bold, TextAnchor.MiddleLeft);
        dashScoreStyle = CrispLabel(Mathf.RoundToInt(14 * scale), new Color(1f, .96f, .86f), FontStyle.Bold, TextAnchor.MiddleRight);
        dashScoreActiveStyle = CrispLabel(Mathf.RoundToInt(14 * scale), new Color(.18f, .08f, .01f), FontStyle.Bold, TextAnchor.MiddleRight);

        pointerStyle = new GUIStyle(bigStyle) { fontSize = 40, normal = { textColor = new Color(.18f, .08f, .01f) } };
        cameraStatusStyle = CrispLabel(Mathf.RoundToInt(13 * scale), new Color(.42f, .96f, .48f), FontStyle.Bold, TextAnchor.MiddleCenter);
        gorillaReactionStyle = new GUIStyle(bigStyle) { fontSize = Mathf.RoundToInt(h * .029f), wordWrap = true };
    }

    public void RenderUI()
    {
        PrepareStyles();
        if (hudFont != null) JungleRetainedUI.skin.font = hudFont;
        DrawBowlingHud();
        DrawBowlingDashboard();
        DrawCameraStatus();
    }

    private static readonly string[] FrameNumberLabels = { "01", "02", "03", "04", "05", "06", "07", "08", "09", "10" };
    private string cachedRoomCode = null, cachedRoomCodeText = "";
    private int cachedBowlingFrame = -1, cachedBowlingBall = -1, cachedLastPins = -1, cachedTotalScore = -1;
    private string cachedBowlingFrameText = "", cachedLastPinsText = "", cachedTotalScoreText = "";

    private void DrawBowlingHud()
    {
        float w = Screen.width, h = Screen.height;
        float scale = Mathf.Clamp(Mathf.Min(w / 1920f, h / 1080f), .65f, 1.35f);
        Rect hud = new Rect(24 * scale, 20 * scale, Mathf.Min(w * .57f, 820 * scale), 162 * scale);
        JungleUITheme.DrawPanel(hud);
        JungleRetainedUI.Label(new Rect(hud.x + 22 * scale, hud.y + 10 * scale, hud.width * .42f, 30 * scale), "BANANA BOWLING", hudBrandStyle);

        bool connected = Time.unscaledTime - lastPacketAt < 1f;
        if (!connected)
        {
            if (roomCode != cachedRoomCode) { cachedRoomCode = roomCode; cachedRoomCodeText = $"CÓDIGO  {roomCode}"; }
            JungleRetainedUI.Label(new Rect(hud.x + 22 * scale, hud.y + 42 * scale, hud.width * .52f, 43 * scale), cachedRoomCodeText, hudFrameStyle);
            JungleRetainedUI.Label(new Rect(hud.x + 22 * scale, hud.y + 86 * scale, hud.width * .54f, 28 * scale), connectionState, hudMessageStyle);
            if (!polling && JungleRetainedUI.Button(new Rect(hud.xMax - 205 * scale, hud.y + 52 * scale, 180 * scale, 42 * scale), "CONECTAR", hudConnectButtonStyle)) Connect();
            return;
        }

        int shownFrame = Mathf.Min(bowlingFrame + 1, 10);
        if (shownFrame != cachedBowlingFrame || bowlingBall != cachedBowlingBall)
        {
            cachedBowlingFrame = shownFrame;
            cachedBowlingBall = bowlingBall;
            cachedBowlingFrameText = $"FRAME {shownFrame}  ·  BOLA {bowlingBall}";
        }
        JungleRetainedUI.Label(new Rect(hud.x + 22 * scale, hud.y + 40 * scale, hud.width * .44f, 43 * scale), cachedBowlingFrameText, hudFrameStyle);
        string shotLabel = lastLaunchDirection < -.18f ? "DIAGONAL IZQUIERDA" : lastLaunchDirection > .18f ? "DIAGONAL DERECHA" : "TIRO RECTO";
        float previewSpin = calibrated ? ShotMechanics.BowlingSpin(Mathf.DeltaAngle(centerGamma, latestGamma)) : 0f;
        string previewSpinLabel = previewSpin < -.1f ? "CURVA IZQ" : previewSpin > .1f ? "CURVA DER" : "RECTO";
        string message = bowlingGameOver ? $"PARTIDA TERMINADA · TOTAL {GetDisplayedBowlingScore()}" : state == GameState.Aim
            ? $"Apunta con la mano · Efecto: {previewSpinLabel} · Lanza"
            : state == GameState.Rolling ? $"¡Bola en juego! · {shotLabel} · EFECTO {(bowlingSpin<-.1f?"IZQ":bowlingSpin>.1f?"DER":"RECTO")}" : $"Derribaste {lastPins} pinos";
        JungleRetainedUI.Label(new Rect(hud.x + 22 * scale, hud.y + 80 * scale, hud.width - 44 * scale, 28 * scale), message, hudMessageStyle);

        float buttonY = hud.yMax - 44 * scale;
        float gap = 10 * scale;
        float buttonWidth = (hud.width - 44 * scale - gap * 2) / 3f;
        if (JungleRetainedUI.Button(new Rect(hud.x + 22 * scale, buttonY, buttonWidth, 34 * scale), "CALIBRAR", hudButtonStyle)) { JungleAudioBank.PlayUiClick(); Calibrate(); }
        if (JungleRetainedUI.Button(new Rect(hud.x + 22 * scale + buttonWidth + gap, buttonY, buttonWidth, 34 * scale), "NUEVA PARTIDA", hudButtonStyle)) { JungleAudioBank.PlayUiClick(); StartBowlingGame(); }
        if (JungleRetainedUI.Button(new Rect(hud.x + 22 * scale + (buttonWidth + gap) * 2, buttonY, buttonWidth, 34 * scale), "MENÚ", hudButtonStyle)) { JungleAudioBank.PlayUiClick(); SceneNavigator.Load(SceneNavigator.MainMenu); }
    }

    private GUIStyle CrispLabel(int size, Color color, FontStyle weight, TextAnchor alignment)
    {
        var font = size >= 28 ? (JungleUITheme.TitleFont ?? hudFont) : (JungleUITheme.TextFont ?? hudFont);
        var style = new GUIStyle(JungleRetainedUI.skin.label) { font = font, fontSize = Mathf.Max(15, size), fontStyle = weight, alignment = alignment, clipping = TextClipping.Clip };
        style.normal.textColor = color;
        return style;
    }

    private GUIStyle CrispButton(int size)
    {
        var style = new GUIStyle(JungleRetainedUI.skin.button) { font = JungleUITheme.TextFont ?? hudFont, fontSize = Mathf.Max(13, size), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = false, clipping = TextClipping.Clip, border = new RectOffset(18, 18, 18, 18), padding = new RectOffset(10, 10, 4, 4) };
        style.normal.background = bowlingButtonTexture;
        style.hover.background = bowlingButtonHoverTexture;
        style.active.background = JungleUITheme.ButtonGoldRounded;
        style.normal.textColor = new Color(1f, .96f, .86f);
        style.hover.textColor = new Color(1f, .99f, .90f);
        style.active.textColor = new Color(.18f, .08f, .01f);
        return style;
    }

    private static GUIStyle RoundedStyle(Texture2D texture, int border)
    {
        var style = new GUIStyle(JungleRetainedUI.skin.box) { border = new RectOffset(border, border, border, border) };
        style.normal.background = texture;
        return style;
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
        if (Time.time >= gorillaRig.ReactionUntil || gorillaRig.CurrentReaction == 0) return;
        float w = Screen.width, h = Screen.height;
        gorillaReactionStyle.normal.textColor = gorillaRig.CurrentReaction == 1 ? new Color(1f, .28f, .12f) : new Color(1f, .84f, .16f);
        JungleRetainedUI.DrawTexture(new Rect(w * .025f, h * .67f, w * .24f, h * .13f), panelTexture);
        JungleRetainedUI.Label(new Rect(w * .035f, h * .68f, w * .22f, h * .11f), gorillaRig.CurrentReaction == 1 ? "¡GRRR!\n¡UNA CHUZA!" : "¡JA, JA!\n¡TE FALTARON PINOS!", gorillaReactionStyle);
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
        JungleRetainedUI.Box(new Rect(x, y, width, height), GUIContent.none, hudBoxStyle);
        JungleRetainedUI.Label(new Rect(x + 14 * scale, y + 12 * scale, width - 28 * scale, 28 * scale), "TABLERO DE LA SELVA", dashHeadingStyle);
        if (lastPins != cachedLastPins) { cachedLastPins = lastPins; cachedLastPinsText = $"ÚLTIMO TIRO: {lastPins} PINOS"; }
        JungleRetainedUI.Label(new Rect(x + 18 * scale, y + 44 * scale, width - 36 * scale, 23 * scale), cachedLastPinsText, dashRowStyle);
        int[] scores = CalculateBowlingScores();
        float rowsY = y + 76 * scale, rowH = 42 * scale;
        for (int i = 0; i < 10; i++)
        {
            bool active = i == Mathf.Min(bowlingFrame, 9) && !bowlingGameOver;
            Rect row = new Rect(x + 14 * scale, rowsY + i * rowH, width - 28 * scale, rowH - 5 * scale);
            JungleRetainedUI.Box(row, GUIContent.none, active ? selectedBoxStyle : cardBoxStyle);
            GUIStyle frameStyle = active ? dashRowActiveStyle : dashRowStyle;
            GUIStyle frameScore = active ? dashScoreActiveStyle : dashScoreStyle;
            JungleRetainedUI.Label(new Rect(row.x + 10 * scale, row.y, row.width * .18f, row.height), FrameNumberLabels[i], frameStyle);
            JungleRetainedUI.Label(new Rect(row.x + row.width * .21f, row.y, row.width * .39f, row.height), BowlingRollText(i), frameStyle);
            JungleRetainedUI.Label(new Rect(row.x + row.width * .62f, row.y, row.width * .32f, row.height), scores[i] >= 0 ? scores[i].ToString() : "", frameScore);
        }
        int totalScore = GetDisplayedBowlingScore();
        if (totalScore != cachedTotalScore) { cachedTotalScore = totalScore; cachedTotalScoreText = $"TOTAL  {totalScore}"; }
        JungleRetainedUI.Label(new Rect(x + 16 * scale, y + height - 66 * scale, width - 32 * scale, 45 * scale), cachedTotalScoreText, dashHeadingStyle);
    }



    private void DrawCameraStatus()
    {
        float scale = Mathf.Clamp(Mathf.Min(Screen.width / 1920f, Screen.height / 1080f), .65f, 1.35f);
        cameraStatusStyle.normal.textColor = CameraActive ? new Color(.3f, 1f, .45f) : new Color(1f, .72f, .22f);
        Rect rect = new Rect(Screen.width - 292 * scale - 24 * scale, Screen.height - 55 * scale, 292 * scale, 38 * scale);
        JungleRetainedUI.Box(rect, GUIContent.none, statusBoxStyle);
        string cameraLabel = CameraActive ? "● CÁMARA · MANO DETECTADA"
            : !sharedSession.CameraConnected ? "○ CÁMARA · SIN SEÑAL"
            : !sharedSession.PhoneConnected ? "○ CÁMARA LISTA · ESPERA CELULAR"
            : "○ CÁMARA LISTA · CONTROL EN ESPERA";
        JungleRetainedUI.Label(rect, cameraLabel, cameraStatusStyle);
    }



    private void OnDestroy()
    {
        gorillaRig.Dispose();
        islandImport?.Dispose();
    }
}
