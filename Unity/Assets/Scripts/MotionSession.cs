using JungleParty.Portable;
using GorillaEscape.Gameplay.Input;
using GorillaEscape.Contracts;
using GorillaEscape.Input.Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

// Gameplay facade only. No HTTP polling, process management, room creation or transport authority.
[DefaultExecutionOrder(-10000)]
[RequireComponent(typeof(JunglePanel))]
public sealed class MotionSession : MonoBehaviour, IJungleScreen, IJungleModalScreen, IPortableInputSource
{
    public enum HandTrackingMode { RightOnly, LeftOnly, Both }
    public static MotionSession Instance { get; private set; }
    public string RoomCode => serviceHost != null && !string.IsNullOrEmpty(serviceHost.RoomCode) ? serviceHost.RoomCode : "";
    public string Status => serviceHost != null ? serviceHost.PhoneStatus : "SIN CONEXIÓN";
    public bool IsPolling => serviceHost != null && serviceHost.IsRunning;
    public bool PhoneConnected => serviceHost != null && serviceHost.PhoneConnected;
    public bool CameraConnected => serviceHost != null && serviceHost.CameraConnected;
    public bool CameraOnly => serviceHost != null && serviceHost.CameraOnly;
    public LobbySnapshotWire Lobby => serviceHost?.Lobby;
    public void RememberPreparation() => serviceHost?.RememberPreparation();
    public bool CanReusePreparation(bool bothDevices, int players) => serviceHost != null && serviceHost.CanReusePreparation(bothDevices, players);
    public void SetCameraOnly(bool enabled) => serviceHost.SetCameraOnly(enabled);
    public float LastPhonePacketAt => serviceHost != null ? serviceHost.LastPhonePacketAt : -10;
    public float LastPosePacketAt => serviceHost != null ? serviceHost.LastPosePacketAt : -10;
    public CameraFramingReport CameraFraming => serviceHost != null ? serviceHost.CameraFraming : default;
    public bool TryCameraCursor(out Vector2 screenNormalized, out string handLabel, out bool handRaised)
    {
        screenNormalized = new Vector2(.5f, .5f);
        handLabel = "MANO DER";
        handRaised = false;
        return serviceHost != null && serviceHost.TryCameraCursor(out screenNormalized, out handLabel, out handRaised);
    }
    public bool TryCameraCursor(out Vector2 screenNormalized, out string handLabel, out bool handRaised, out bool handClosed, out bool forwardPush)
    {
        screenNormalized = new Vector2(.5f, .5f);
        handLabel = "MANO DER";
        handRaised = false;
        handClosed = false;
        forwardPush = false;
        return serviceHost != null && serviceHost.TryCameraCursor(out screenNormalized, out handLabel, out handRaised, out handClosed, out forwardPush);
    }
    public bool PhoneTouchTriggered => serviceHost != null && serviceHost.PhoneTouchTriggered;
    public bool PhoneTouchPressed => serviceHost != null && serviceHost.PhoneTouchPressed;
    public string PlayerName => (serviceHost?.Lobby?.players != null && serviceHost.Lobby.players.Length > 0 && !string.IsNullOrWhiteSpace(serviceHost.Lobby.players[0]?.displayName)) ? serviceHost.Lobby.players[0].displayName : "Tú";
    public HandTrackingMode HandMode { get; private set; } = HandTrackingMode.RightOnly;
    public bool UsesLeftHand => HandMode != HandTrackingMode.RightOnly;
    public bool UsesRightHand => HandMode != HandTrackingMode.LeftOnly;
    PortableServiceHost serviceHost;
    readonly GorillaEscape.Contracts.GamePresentationWire presentation=new GorillaEscape.Contracts.GamePresentationWire();
    float nextPresentation;
    bool RoundFinished => presentation.phase == "Finished" || presentation.phase == "Won" || presentation.phase == "Lost";
    public void PresentGame(string phase,double? score=null,float? progress=null,float? elapsed=null,float? balance=null,float? distance=null)
    {
        presentation.phase=phase;var s=presentation.stats;
        s.hasScore=score.HasValue;s.score=score.GetValueOrDefault();
        s.hasProgress=progress.HasValue;s.progress=Mathf.Clamp01(progress.GetValueOrDefault());
        s.hasElapsed=elapsed.HasValue;s.elapsed=Mathf.Clamp(elapsed.GetValueOrDefault(),0,3600);
        s.hasBalance=balance.HasValue;s.balance=Mathf.Clamp(balance.GetValueOrDefault(),-1,1);
        s.hasDistance=distance.HasValue;s.distance=Mathf.Clamp(distance.GetValueOrDefault(),0,10000);
    }
    void LateUpdate()
    {
        ProcessPhoneNavigation();
        if(serviceHost==null||Time.unscaledTime<nextPresentation)return;
        presentation.hand=serviceHost.LeftHanded?"left":"right";
        if (TournamentSession.IsActive)
        {
            presentation.playerId = TournamentSession.CurrentTurnPlayerId;
            if (serviceHost.Lobby?.players != null)
                TournamentSession.UpdatePlayerStates(serviceHost.Lobby.players);
        }
        nextPresentation=Time.unscaledTime+.2f;
        var context=presentation.Copy();
        if(context.gameId!="lobby"&&(InGamePauseActive||!IsInputReady||trackingPause.Paused||Time.timeScale==0)&&context.phase!="Won"&&context.phase!="Lost"&&context.phase!="Finished")context.phase="Paused";
        serviceHost.PresentPhoneGame(context);
    }
    private static readonly (string A, string B)[] SkeletonBonePairs =
    {
        ("left-shoulder", "right-shoulder"),
        ("left-shoulder", "left-elbow"),
        ("left-elbow", "left-wrist"),
        ("left-wrist", "left-hand"),
        ("right-shoulder", "right-elbow"),
        ("right-elbow", "right-wrist"),
        ("right-wrist", "right-hand")
    };

    long delivered=-1;
    readonly JunglePauseLease pause=new JunglePauseLease();
    readonly TrackingPauseStability trackingPause=new TrackingPauseStability();
    readonly HandDwellNavigator pauseNavigator=new HandDwellNavigator();
    readonly HandDwellTarget[] reacquireTargets=new HandDwellTarget[1];
    readonly HandDwellTarget[] pauseTargets=new HandDwellTarget[1];
    bool gameplayActive=true;
    bool pipMinimized = true;
    public static bool InGamePauseActive { get; private set; }
    public static void SetInGamePause(bool paused)
    {
        if (InGamePauseActive == paused) return;
        InGamePauseActive = paused;
        if (Instance != null) Instance.RefreshPause();
    }
    public static void ToggleInGamePause() => SetInGamePause(!InGamePauseActive);

    public static string GetGameDisplayName(JungleGame game) => game switch
    {
        JungleGame.Bowling => "BOLICHE SELVÁTICO",
        JungleGame.Boxing => "BOXEO SALVAJE",
        JungleGame.Baseball => "BÉISBOL TROPICAL",
        JungleGame.Drums => "RITMO DE LA SELVA",
        JungleGame.CoconutSlice => SceneNavigator.IsJungleKeeper ? "GUARDIÁN DE LA SELVA" : "CORTE DE COCO",
        JungleGame.Whack => "TOPO DE LA SELVA",
        JungleGame.LogBalance => "DESAFÍO DEL TRONCO",
        JungleGame.GorillaSmash => "GOLPE AL GORILA",
        JungleGame.CoconutThrow => "LANZAMIENTO DE COCO",
        _ => "GORILLA ESCAPE"
    };

    GUIStyle pauseTitleStyle, pauseBodyStyle, pausePreviewWaitStyle, pauseHintStyle;
    GUIStyle pipBadgeStyle, pipToggleStyle, pipWaitStyle, pipHintNormalStyle, pipHintRaisedStyle;
    GUIStyle headerTitleStyle, headerBadgeStyle, headerPauseButtonStyle;
    GUIStyle pauseSectionStyle, pauseItemStyle, pauseMiniButtonStyle, pauseBigButtonStyle, pauseBigButtonSelectedStyle;

    void EnsureSessionStyles()
    {
        if (pipBadgeStyle != null) return;
        JungleUITheme.Ensure();
        pauseTitleStyle = new GUIStyle(JungleRetainedUI.skin.label) { font = JungleUITheme.TitleFont, fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1f, 0.86f, 0.25f) } };
        pauseBodyStyle = new GUIStyle(JungleRetainedUI.skin.label) { font = JungleUITheme.TextFont, wordWrap = true, alignment = TextAnchor.MiddleCenter, fontSize = 20, normal = { textColor = new Color(0.99f, 0.96f, 0.88f) } };
        pausePreviewWaitStyle = new GUIStyle(JungleRetainedUI.skin.label) { font = JungleUITheme.TextFont, alignment = TextAnchor.MiddleCenter, wordWrap = true, normal = { textColor = new Color(0.95f, 0.90f, 0.76f) } };
        pauseHintStyle = new GUIStyle(JungleRetainedUI.skin.label) { font = JungleUITheme.TextFont, fontSize = 14, alignment = TextAnchor.MiddleCenter, wordWrap = true, normal = { textColor = new Color(0.95f, 0.90f, 0.76f) } };
        pipBadgeStyle = new GUIStyle(JungleRetainedUI.skin.label) { font = JungleUITheme.TextFont, fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, normal = { textColor = new Color(1f, 0.92f, 0.65f) } };
        pipToggleStyle = new GUIStyle(JungleUITheme.Button) { font = JungleUITheme.TextFont, fontSize = 11, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        pipWaitStyle = new GUIStyle(JungleRetainedUI.skin.label) { font = JungleUITheme.TextFont, fontSize = 11, alignment = TextAnchor.MiddleCenter, wordWrap = true, normal = { textColor = new Color(.94f, .88f, .74f) } };
        pipHintNormalStyle = new GUIStyle(JungleRetainedUI.skin.label) { font = JungleUITheme.TextFont, fontSize = 11, alignment = TextAnchor.MiddleCenter, wordWrap = true, normal = { textColor = new Color(.95f, .90f, .76f) } };
        pipHintRaisedStyle = new GUIStyle(JungleRetainedUI.skin.label) { font = JungleUITheme.TextFont, fontSize = 11, alignment = TextAnchor.MiddleCenter, wordWrap = true, normal = { textColor = new Color(1f, .86f, .25f) } };

        headerTitleStyle = new GUIStyle(JungleRetainedUI.skin.label) { font = JungleUITheme.TitleFont, fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, normal = { textColor = new Color(1f, 0.86f, 0.25f) } };
        headerBadgeStyle = new GUIStyle(JungleRetainedUI.skin.label) { font = JungleUITheme.TextFont, fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.98f, 0.95f, 0.88f) } };
        headerPauseButtonStyle = new GUIStyle(JungleUITheme.Button) { font = JungleUITheme.TitleFont, fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };

        pauseSectionStyle = new GUIStyle(JungleRetainedUI.skin.label) { font = JungleUITheme.TitleFont, fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1f, 0.84f, 0.22f) } };
        pauseItemStyle = new GUIStyle(JungleRetainedUI.skin.label) { font = JungleUITheme.TextFont, fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, normal = { textColor = new Color(0.98f, 0.94f, 0.82f) } };
        pauseMiniButtonStyle = new GUIStyle(JungleUITheme.Button) { font = JungleUITheme.TextFont, fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        pauseBigButtonStyle = new GUIStyle(JungleUITheme.Button) { font = JungleUITheme.TitleFont, fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        pauseBigButtonSelectedStyle = new GUIStyle(JungleUITheme.ButtonSelected) { font = JungleUITheme.TitleFont, fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
    }
    public void SetGameplayActive(bool active)
    {
        if(gameplayActive==active)return;
        gameplayActive=active;
        trackingPause.Reset();
        RefreshPause();
    }
    public bool IsInputReady => serviceHost!=null&&serviceHost.IsInputReady;
    public JungleInputStatus InputStatus => serviceHost!=null?serviceHost.InputStatus:JungleInputStatus.Waiting;
    public static MotionSession Ensure() => Instance != null ? Instance : new GameObject("SESION PERSISTENTE - Fase 0").AddComponent<MotionSession>();
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        serviceHost = PortableServiceHost.Ensure();
        ConfigureForScene(SceneManager.GetActiveScene().name);
        Connect();
    }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if(scene!=SceneManager.GetActiveScene())return;
        InGamePauseActive=false;
        ConfigureForScene(scene.name);
        serviceHost.SelectGame(JungleGameplayInput.ForScene(scene.name),true);
        delivered=-1;
        trackingPause.Reset();
        RefreshPause();
    }
    public void ConfigureForScene(string sceneName)
    {
        if(sceneName==SceneNavigator.MainMenu||sceneName==SceneNavigator.Splash){pipMinimized=true;if(serviceHost!=null)serviceHost.PipEnabled=false;}
        if(serviceHost!=null && JungleGameplayInput.ForScene(sceneName)!=JungleGame.None)
            serviceHost.SetCameraOnly(JungleGameplayInput.SupportsCameraOnly(JungleGameplayInput.ForScene(sceneName)));
        gameplayActive=sceneName!=SceneNavigator.CoconutSlice&&sceneName!=SceneNavigator.WhackAMole&&sceneName!=SceneNavigator.GorillaSmash&&sceneName!=SceneNavigator.CoconutThrow;
        presentation.gameId=JungleGameplayInput.ForScene(sceneName) switch {
            JungleGame.Bowling=>"bowling",JungleGame.Boxing=>"boxing",JungleGame.Baseball=>"baseball",JungleGame.Drums=>"drums",
            JungleGame.CoconutSlice=>"coconut-slice",JungleGame.Whack=>"whack",JungleGame.LogBalance=>"log-balance",
            JungleGame.GorillaSmash=>"gorilla-smash",JungleGame.CoconutThrow=>"coconut-throw",_=>"lobby"};
        presentation.playerId=presentation.gameId=="lobby"?0:1;
        if(presentation.gameId=="coconut-slice"&&SceneNavigator.IsJungleKeeper)presentation.gameId="jungle-keeper";
        presentation.screen=presentation.gameId!="lobby"?"game":sceneName==SceneNavigator.Connection?"connection":"menu";
        presentation.phoneRequired=presentation.screen=="connection" || JungleGameplayInput.RequiresPhone(JungleGameplayInput.ForScene(sceneName));
        presentation.contextId=System.Guid.NewGuid().ToString("N");
        PresentGame("Ready");nextPresentation=0;
        HandMode = sceneName == SceneNavigator.Boxing || sceneName == SceneNavigator.Drums || sceneName == SceneNavigator.CoconutSlice || sceneName == SceneNavigator.LogBalance
            ? HandTrackingMode.Both : (sceneName==SceneNavigator.Bowling||sceneName==SceneNavigator.CoconutThrow||sceneName==SceneNavigator.GorillaSmash)&&serviceHost.LeftHanded?HandTrackingMode.LeftOnly:HandTrackingMode.RightOnly;
        serviceHost.SelectGame(JungleGameplayInput.ForScene(sceneName));
        delivered=-1;
    }
    public void Connect() => serviceHost.StartModule();
    void ProcessPhoneNavigation()
    {
        if(serviceHost==null)return;
        while(serviceHost.TakeUiCommand(out var command))
        {
            if(!command.Valid(presentation,serviceHost.Lobby,System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()))continue;
            if(command.action=="start")
            {
                if(!serviceHost.IsInputReady)continue;
                FindAnyObjectByType<CoconutSliceController>()?.BeginFromPhone();
                FindAnyObjectByType<WhackAMoleController>()?.BeginFromPhone();
                FindAnyObjectByType<LogBalanceGameController>()?.BeginFromPhone();
                FindAnyObjectByType<GorillaSmashController>()?.BeginFromPhone();
                FindAnyObjectByType<CoconutThrowController>()?.BeginFromPhone();
                continue;
            }
            if(command.action=="set-hand")
            {
                serviceHost.SetDominantHand(command.target=="left");
                if(presentation.gameId=="bowling"||presentation.gameId=="coconut-throw"||presentation.gameId=="gorilla-smash")HandMode=command.target=="left"?HandTrackingMode.LeftOnly:HandTrackingMode.RightOnly;
                presentation.hand=command.target;delivered=-1;trackingPause.Reset();nextPresentation=0;
                continue;
            }
            if(command.action=="pause"){SetInGamePause(true);continue;}
            if(command.action=="resume"){SetInGamePause(false);continue;}
            if(command.action=="toggle-pause"){ToggleInGamePause();continue;}
            if(command.action=="menu"){SetInGamePause(false);SceneNavigator.Load(SceneNavigator.MainMenu);return;}
            if(command.action=="restart"){SetInGamePause(false);SceneNavigator.Load(presentation.gameId=="jungle-keeper"?SceneNavigator.JungleKeeper:UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);return;}
            string scene=command.target switch {"bowling"=>SceneNavigator.Bowling,"boxing"=>SceneNavigator.Boxing,"baseball"=>SceneNavigator.Baseball,
                "drums"=>SceneNavigator.Drums,"coconut-slice"=>SceneNavigator.CoconutSlice,"jungle-keeper"=>SceneNavigator.JungleKeeper,
                "whack"=>SceneNavigator.WhackAMole,"log-balance"=>SceneNavigator.LogBalance,
                "gorilla-smash"=>SceneNavigator.GorillaSmash,"coconut-throw"=>SceneNavigator.CoconutThrow,_=>null};
            if(scene!=null){SetCameraOnly(command.target=="coconut-slice"||command.target=="jungle-keeper");SceneNavigator.Load(scene);return;}
        }
    }
    public void ResetRound()
    {
        serviceHost.SelectGame(JungleGameplayInput.ForScene(SceneManager.GetActiveScene().name),true);
        delivered=-1;trackingPause.Reset();RefreshPause();
    }
    public bool TryConsumeFrame(out JungleFusedFrame frame)
    {
        RefreshPause();
        frame=null;
        if(trackingPause.Paused||!serviceHost.TryReadFrame(out frame)||frame.Revision==delivered)return false;
        delivered=frame.Revision;return true;
    }
    public bool TryAuthorize(JungleAction action,long revision) => !trackingPause.Paused&&serviceHost.Authorize(action,revision);
    public bool TryPhoneVisualEstimate(out PhoneVisualEstimate estimate)
    {
        estimate=null;
        return serviceHost!=null&&!trackingPause.Paused&&serviceHost.TryPhoneVisualEstimate(out estimate);
    }
    void Update()
    {
        var sceneGame = JungleGameplayInput.ForScene(SceneManager.GetActiveScene().name);
        if (sceneGame != JungleGame.None)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            {
                JungleAudioBank.PlayUiClick();
                ToggleInGamePause();
            }
        }
        RefreshPause();
    }
    void RefreshPause()
    {
        var sceneGame = JungleGameplayInput.ForScene(SceneManager.GetActiveScene().name);
        if (RoundFinished) trackingPause.Reset();
        bool active = !RoundFinished && gameplayActive && serviceHost != null && sceneGame != JungleGame.None && serviceHost.Game == sceneGame;
        float lossGrace = serviceHost != null ? serviceHost.InputLossGraceSeconds : 0.40f;
        bool technicalPause = active && trackingPause.Update(IsInputReady, Time.unscaledTime, lossGrace);
        bool paused = InGamePauseActive || technicalPause;
        float current = Time.timeScale;
        float desired = pause.Apply(paused, current);
        if (desired != current) Time.timeScale = desired;
    }
    public bool BlocksSceneInput
    {
        get
        {
            var game = JungleGameplayInput.ForScene(SceneManager.GetActiveScene().name);
            return game != JungleGame.None && (InGamePauseActive ||
                !RoundFinished && gameplayActive && serviceHost != null && serviceHost.Game == game && trackingPause.ShowNotice(Time.unscaledTime));
        }
    }
    public void RenderUI()
    {
        var activeSceneName = SceneManager.GetActiveScene().name;
        var sceneGame = JungleGameplayInput.ForScene(activeSceneName);
        if (sceneGame != JungleGame.None)
        {
            DrawGlobalHeader(activeSceneName, sceneGame);
        }

        if (InGamePauseActive && sceneGame != JungleGame.None)
        {
            DrawInGamePauseOverlay(activeSceneName, sceneGame);
            return;
        }

        bool showPauseNotice = !RoundFinished && gameplayActive && serviceHost != null && sceneGame != JungleGame.None && serviceHost.Game == sceneGame && trackingPause.ShowNotice(Time.unscaledTime);
        if (!showPauseNotice)
        {
            pauseNavigator.Reset();
            DrawCameraPip(activeSceneName, sceneGame);
            return;
        }

        DrawTechnicalPauseNotice(activeSceneName, sceneGame);
    }

    void DrawGlobalHeader(string activeSceneName, JungleGame sceneGame)
    {
        EnsureSessionStyles();
        int prevDepth = JungleRetainedUI.depth;
        JungleRetainedUI.depth = -95;
        float w = Screen.width;
        float h = JunglePanel.GameplayHeaderHeight;
        float scale = h / 64f;
        float pad = 16f * scale;
        float gap = 10f * scale;
        float buttonWidth = 126f * scale;
        float cameraWidth = 148f * scale;
        float playerWidth = Mathf.Clamp(w * .15f, 130f * scale, 190f * scale);
        float trayX = w - pad - buttonWidth - cameraWidth - playerWidth - gap * 2;

        JungleRetainedUI.DrawTexture(new Rect(0, 0, w, h), JungleUITheme.DarkHud);
        JungleRetainedUI.DrawTexture(new Rect(0, h - 2f, w, 2f), JungleUITheme.Gold);
        headerTitleStyle.fontSize = Mathf.RoundToInt(18f * scale);
        headerBadgeStyle.fontSize = Mathf.RoundToInt(11f * scale);
        headerPauseButtonStyle.fontSize = Mathf.RoundToInt(14f * scale);
        JungleUITheme.DrawShadowedLabel(new Rect(pad, 8f * scale, Mathf.Max(0, trayX - pad - gap), h - 16f * scale),
            GetGameDisplayName(sceneGame)+"\n"+TournamentSession.ControlForScene(activeSceneName), headerTitleStyle, 1.5f);

        string playerName = TournamentSession.IsActive && TournamentSession.HumanPlayerCount > 1
            ? TournamentSession.CurrentTurnPlayerName : PlayerName;
        if (string.IsNullOrWhiteSpace(playerName) || playerName == "Tú")
            playerName = CameraOnly ? "Manos libres" : PhoneConnected ? "Conectado" : "Sin celular";
        if (playerName.Length > 20) playerName = playerName.Substring(0, 19) + "…";
        Rect playerCard = new Rect(trayX, 7f * scale, playerWidth, h - 14f * scale);
        JungleUITheme.DrawSubPanel(playerCard);
        JungleRetainedUI.Label(new Rect(playerCard.x, playerCard.y + 3f * scale, playerCard.width, 15f * scale),
            TournamentSession.IsActive && TournamentSession.HumanPlayerCount > 1 ? "TURNO DE" : "JUGADOR", headerBadgeStyle);
        headerBadgeStyle.fontSize = Mathf.RoundToInt(15f * scale);
        JungleRetainedUI.Label(new Rect(playerCard.x + 6f, playerCard.y + 19f * scale, playerCard.width - 12f, 22f * scale), playerName, headerBadgeStyle);
        headerBadgeStyle.fontSize = Mathf.RoundToInt(11f * scale);

        bool cameraRequired = JungleGameplayInput.RequiresCamera(sceneGame);
        string cameraText = !cameraRequired ? "OPCIONAL" : !CameraConnected ? "SIN SEÑAL"
            : CameraFraming.Status == CameraFramingStatus.Optimal ? "LISTA" : "AJUSTAR ENCUADRE";
        Rect cameraCard = new Rect(playerCard.xMax + gap, playerCard.y, cameraWidth, playerCard.height);
        JungleUITheme.DrawSubPanel(cameraCard);
        JungleRetainedUI.Label(new Rect(cameraCard.x, cameraCard.y + 3f * scale, cameraCard.width, 15f * scale), "CÁMARA", headerBadgeStyle);
        JungleRetainedUI.Label(new Rect(cameraCard.x + 5f, cameraCard.y + 19f * scale, cameraCard.width - 10f, 22f * scale), cameraText, headerBadgeStyle);

        Rect pauseButton = new Rect(cameraCard.xMax + gap, 10f * scale, buttonWidth, h - 20f * scale);
        if (JungleRetainedUI.Button(pauseButton, InGamePauseActive ? "SEGUIR · ESC" : "PAUSA · ESC", headerPauseButtonStyle))
        {
            JungleAudioBank.PlayUiClick();
            ToggleInGamePause();
        }

        JungleRetainedUI.depth = prevDepth;
    }

    void DrawInGamePauseOverlay(string activeSceneName, JungleGame sceneGame)
    {
        EnsureSessionStyles();
        int depth = JungleRetainedUI.depth;
        JungleRetainedUI.depth = -200;

        float sw = Screen.width, sh = Screen.height;

        Color prevColor = JungleRetainedUI.color;
        JungleRetainedUI.color = new Color(0.04f, 0.05f, 0.03f, 0.82f);
        JungleRetainedUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
        JungleRetainedUI.color = prevColor;

        float pw = Mathf.Min(sw - 40, 640);
        float ph = Mathf.Min(sh - 40, 500);
        float px = (sw - pw) / 2;
        float py = (sh - ph) / 2;
        Rect modalRect = new Rect(px, py, pw, ph);

        JungleUITheme.DrawPanel(modalRect, true);

        JungleUITheme.DrawShadowedLabel(new Rect(px + 20, py + 14, pw - 40, 36), " JUEGO EN PAUSA", pauseTitleStyle, 2f);
        JungleUITheme.DrawShadowedLabel(new Rect(px + 20, py + 48, pw - 40, 24), GetGameDisplayName(sceneGame), pauseHintStyle, 1.2f);

        float audioY = py + 76;
        float audioH = 195;
        Rect audioCard = new Rect(px + 24, audioY, pw - 48, audioH);
        JungleUITheme.DrawSubPanel(audioCard);

        JungleUITheme.DrawShadowedLabel(new Rect(audioCard.x, audioCard.y + 8, audioCard.width, 24), "AJUSTES DE AUDIO DE LA SELVA", pauseSectionStyle, 1.5f);

        // Master Volume
        float row1Y = audioCard.y + 38;
        JungleRetainedUI.Label(new Rect(audioCard.x + 16, row1Y, 190, 28), $" General: {Mathf.RoundToInt(JungleAudioBank.MasterVolume * 100)}%", pauseItemStyle);
        if (JungleRetainedUI.Button(new Rect(audioCard.x + 210, row1Y, 40, 28), "-", pauseMiniButtonStyle))
        {
            JungleAudioBank.PlayUiClick();
            JungleAudioBank.SetMasterVolume(Mathf.Max(0f, JungleAudioBank.MasterVolume - 0.1f));
        }
        if (JungleRetainedUI.Button(new Rect(audioCard.x + 256, row1Y, 40, 28), "+", pauseMiniButtonStyle))
        {
            JungleAudioBank.PlayUiClick();
            JungleAudioBank.SetMasterVolume(Mathf.Min(1f, JungleAudioBank.MasterVolume + 0.1f));
        }
        bool masterMuted = JungleAudioBank.MasterVolume <= 0.01f;
        if (JungleRetainedUI.Button(new Rect(audioCard.x + 304, row1Y, audioCard.width - 318, 28), masterMuted ? "ACTIVAR AUDIO" : "SILENCIAR TODO", pauseMiniButtonStyle))
        {
            JungleAudioBank.PlayUiClick();
            JungleAudioBank.SetMasterVolume(masterMuted ? 1.0f : 0f);
        }

        // Music & Ambience Volume
        float row2Y = audioCard.y + 84;
        JungleRetainedUI.Label(new Rect(audioCard.x + 16, row2Y, 190, 28), $" Música: {Mathf.RoundToInt(JungleAudioBank.AmbienceVolume * 100)}%", pauseItemStyle);
        if (JungleRetainedUI.Button(new Rect(audioCard.x + 210, row2Y, 40, 28), "-", pauseMiniButtonStyle))
        {
            JungleAudioBank.PlayUiClick();
            JungleAudioBank.SetAmbienceVolume(Mathf.Max(0f, JungleAudioBank.AmbienceVolume - 0.1f));
        }
        if (JungleRetainedUI.Button(new Rect(audioCard.x + 256, row2Y, 40, 28), "+", pauseMiniButtonStyle))
        {
            JungleAudioBank.PlayUiClick();
            JungleAudioBank.SetAmbienceVolume(Mathf.Min(1f, JungleAudioBank.AmbienceVolume + 0.1f));
        }
        bool ambMuted = JungleAudioBank.AmbienceVolume <= 0.01f;
        if (JungleRetainedUI.Button(new Rect(audioCard.x + 304, row2Y, audioCard.width - 318, 28), ambMuted ? "ACTIVAR MÚSICA" : "MUTEAR MÚSICA", pauseMiniButtonStyle))
        {
            JungleAudioBank.PlayUiClick();
            JungleAudioBank.SetAmbienceVolume(ambMuted ? 0.6f : 0f);
        }

        // SFX Volume
        float row3Y = audioCard.y + 130;
        JungleRetainedUI.Label(new Rect(audioCard.x + 16, row3Y, 190, 28), $" Efectos: {Mathf.RoundToInt(JungleAudioBank.SfxVolume * 100)}%", pauseItemStyle);
        if (JungleRetainedUI.Button(new Rect(audioCard.x + 210, row3Y, 40, 28), "-", pauseMiniButtonStyle))
        {
            JungleAudioBank.PlayUiClick();
            JungleAudioBank.SetSfxVolume(Mathf.Max(0f, JungleAudioBank.SfxVolume - 0.1f));
        }
        if (JungleRetainedUI.Button(new Rect(audioCard.x + 256, row3Y, 40, 28), "+", pauseMiniButtonStyle))
        {
            JungleAudioBank.PlayUiClick();
            JungleAudioBank.SetSfxVolume(Mathf.Min(1f, JungleAudioBank.SfxVolume + 0.1f));
        }
        bool sfxMuted = JungleAudioBank.SfxVolume <= 0.01f;
        if (JungleRetainedUI.Button(new Rect(audioCard.x + 304, row3Y, audioCard.width - 318, 28), sfxMuted ? "ACTIVAR SFX" : "MUTEAR SFX", pauseMiniButtonStyle))
        {
            JungleAudioBank.PlayUiClick();
            JungleAudioBank.SetSfxVolume(sfxMuted ? 0.85f : 0f);
        }

        // Main Navigation Buttons
        float btnY = py + ph - 195;
        float btnW = pw - 60;
        float btnX = px + 30;

        Rect resumeRect = new Rect(btnX, btnY, btnW, 44);
        if (JungleRetainedUI.Button(resumeRect, " REANUDAR PARTIDA (ESC)", pauseBigButtonSelectedStyle))
        {
            JungleAudioBank.PlayUiClick();
            SetInGamePause(false);
        }

        Rect restartRect = new Rect(btnX, btnY + 52, btnW, 42);
        if (JungleRetainedUI.Button(restartRect, " REINICIAR MINIJUEGO", pauseBigButtonStyle))
        {
            JungleAudioBank.PlayUiClick();
            SetInGamePause(false);
            TournamentSession.ResetCurrentTurnForRestart();
            ResetRound();
            SceneNavigator.Load(SceneNavigator.IsJungleKeeper ? SceneNavigator.JungleKeeper : activeSceneName);
        }

        Rect menuRect = new Rect(btnX, btnY + 102, btnW, 42);
        if (JungleRetainedUI.Button(menuRect, " VOLVER AL MENÚ PRINCIPAL", pauseBigButtonStyle))
        {
            JungleAudioBank.PlayUiClick();
            SetInGamePause(false);
            SceneNavigator.Load(SceneNavigator.MainMenu);
        }

        // Hand dwell support on Resume button
        if (TryCameraCursor(out var normCursor, out var handLabel, out var raised))
        {
            Vector2 cursorPx = new Vector2(normCursor.x * Screen.width, normCursor.y * Screen.height);
            pauseTargets[0] = new HandDwellTarget(0, resumeRect.x, resumeRect.y, resumeRect.width, resumeRect.height);
            if (pauseNavigator.ObserveCursor(true, cursorPx.x, cursorPx.y, Time.unscaledTime, pauseTargets, out int hit, 0.95f) && hit == 0)
            {
                JungleAudioBank.PlayUiClick();
                SetInGamePause(false);
            }
            if (pauseNavigator.Progress > 0.02f)
                JungleUITheme.DrawDwellOverlay(resumeRect, pauseNavigator.Progress, "REANUDANDO...");
        }

        JungleRetainedUI.depth = depth;
    }

    void DrawTechnicalPauseNotice(string activeSceneName, JungleGame sceneGame)
    {
        EnsureSessionStyles();
        int depth = JungleRetainedUI.depth;
        JungleRetainedUI.depth = -100;
        float width = Mathf.Min(Screen.width - 40, 760);
        bool canReacquire = JungleGameplayInput.RequiresCamera(serviceHost.Game) && serviceHost.CameraConnected;
        bool showHelp = serviceHost.ShowCameraHelp;
        var framing = serviceHost.CameraFraming;
        float previewHeight = showHelp ? Mathf.Min(260, Mathf.Max(100, Screen.height * .38f)) : 0;
        float height = (canReacquire ? 205 : 140) + (showHelp ? previewHeight + 38 : 0);
        float top = Mathf.Max(5, (Screen.height - height) / 2);

        JungleUITheme.DrawPanel(new Rect((Screen.width - width) / 2, top, width, height), true);
        JungleUITheme.DrawShadowedLabel(new Rect((Screen.width - width) / 2 + 20, top + 8, width - 40, 34), "ESPERANDO SEÑAL", pauseTitleStyle);
        string message = serviceHost.PauseMessage;
        if (string.IsNullOrWhiteSpace(message) || message.Contains("Pausa técnica"))
        {
            message = JungleGameplayInput.RequiresCamera(sceneGame)
                ? "Colócate frente a la cámara dentro del área visible para continuar la partida."
                : "Mantén tu teléfono vertical y quieto para estabilizar la conexión.";
        }
        JungleUITheme.DrawShadowedLabel(new Rect((Screen.width - width) / 2 + 20, top + 43, width - 40, 60), message, pauseBodyStyle);
        if (showHelp)
        {
            float imageWidth = Mathf.Min(width - 40, previewHeight * 4f / 3f);
            Rect picture = new Rect((Screen.width - imageWidth) / 2, top + 108, imageWidth, previewHeight);
            JungleRetainedUI.DrawTexture(picture, Texture2D.blackTexture);
            var texture = serviceHost.CameraHelpTexture;
            if (texture != null) { JungleRetainedUI.DrawTextureWithTexCoords(picture, texture, new Rect(1, 0, -1, 1)); }
            else JungleRetainedUI.Label(picture, "BUSCANDO JUGADOR...", pausePreviewWaitStyle);
            DrawSafeZoneBrackets(picture, framing.Status);
            DrawCameraGuides(picture, serviceHost.CameraHelpFrame);
            string friendlyBadge = GetFriendlyFramingBadge(framing.Status);
            JungleRetainedUI.Label(new Rect((Screen.width - width) / 2 + 20, picture.yMax + 3, width - 40, 25),
                friendlyBadge + " · " + framing.Hint, pauseHintStyle);
        }
        if (canReacquire)
        {
            Rect reacquireRect = new Rect((Screen.width - width) / 2 + width * .18f, top + height - 56, width * .64f, 44);
            bool handTriggered = pauseNavigator.ObserveRaiseHand(framing.HandRaised, Time.unscaledTime, .75f);
            if (TryCameraCursor(out var normCursor, out var handLabel, out var raised))
            {
                Vector2 cursorPx = new Vector2(normCursor.x * Screen.width, normCursor.y * Screen.height);
                reacquireTargets[0] = new HandDwellTarget(0, reacquireRect.x, reacquireRect.y, reacquireRect.width, reacquireRect.height);
                if (pauseNavigator.ObserveCursor(true, cursorPx.x, cursorPx.y, Time.unscaledTime, reacquireTargets, out int hit, .95f) && hit == 0)
                    handTriggered = true;
            }
            if (JungleRetainedUI.Button(reacquireRect, "BUSCAR CÁMARA NUEVAMENTE (O LEVANTA LA MANO)", JungleUITheme.ButtonSelected) || handTriggered)
            {
                if (serviceHost.ReacquireCamera()) { delivered = -1; pauseNavigator.Reset(); }
            }
            float reacquireProgress = Mathf.Max(pauseNavigator.Progress, pauseNavigator.RaiseHandProgress);
            if (reacquireProgress > .02f)
                JungleUITheme.DrawDwellOverlay(reacquireRect, reacquireProgress, "DETECTANDO MANO ARRIBA...");
        }
        JungleRetainedUI.depth = depth;
    }

    static string GetFriendlyFramingBadge(CameraFramingStatus status) => status switch
    {
        CameraFramingStatus.Optimal => "ÓPTIMO",
        CameraFramingStatus.MissingHands => "LEVANTA LAS MANOS",
        CameraFramingStatus.OutOfSafeZone => "CENTRA TU POSICIÓN",
        CameraFramingStatus.NoPerson => "BUSCANDO JUGADOR",
        CameraFramingStatus.NoSignal => "SIN SEÑAL",
        CameraFramingStatus.MultipleOrAmbiguous => "SOLO 1 JUGADOR",
        _ => "CALIBRANDO"
    };

    void DrawCameraPip(string activeSceneName,JungleGame sceneGame)
    {
        if(serviceHost==null||activeSceneName==SceneNavigator.Connection||activeSceneName==SceneNavigator.Splash)return;
        bool relevant=serviceHost.CameraConnected||serviceHost.CameraOnly||activeSceneName==SceneNavigator.MainMenu||JungleGameplayInput.RequiresCamera(sceneGame);
        if(!relevant)return;

        EnsureSessionStyles();
        int prevDepth=JungleRetainedUI.depth;
        JungleRetainedUI.depth=-90;
        float w=Screen.width,h=Screen.height;
        var framing=serviceHost.CameraFraming;
        Color statusColor=framing.Status==CameraFramingStatus.Optimal?new Color(.18f,.95f,.52f)
            :framing.Status==CameraFramingStatus.MissingHands||framing.Status==CameraFramingStatus.OutOfSafeZone?new Color(1f,.78f,.14f)
            :new Color(.96f,.32f,.24f);

        float pipWidth=Mathf.Clamp(w*.185f,210f,310f);
        float headerH=28f;
        float videoH=pipMinimized?0f:Mathf.Round(pipWidth*.72f);
        float footerH=pipMinimized?0f:24f;
        float totalH=headerH+videoH+footerH+(pipMinimized?6f:12f);
        Rect card=new Rect(w-pipWidth-14f,h-totalH-14f,pipWidth,totalH);

        JungleUITheme.DrawPanel(card);
        Color before=JungleRetainedUI.color;
        JungleRetainedUI.color=statusColor;
        JungleRetainedUI.DrawTexture(new Rect(card.x+10f,card.y+9f,10f,10f),Texture2D.whiteTexture);
        JungleRetainedUI.color=before;

        string pipBadge = GetFriendlyFramingBadge(framing.Status);
        JungleUITheme.DrawShadowedLabel(new Rect(card.x+25f,card.y+3f,card.width-66f,22f),(pipMinimized?"CÁMARA · ":"VISTA EN VIVO · ")+pipBadge,pipBadgeStyle);

        if(JungleRetainedUI.Button(new Rect(card.xMax-36f,card.y+4f,30f,20f),pipMinimized?"▲":"▼",pipToggleStyle))
        {
            pipMinimized=!pipMinimized;
            serviceHost.PipEnabled=!pipMinimized;
        }

        if(!pipMinimized)
        {
            Rect picture=new Rect(card.x+6f,card.y+headerH+2f,card.width-12f,videoH);
            JungleRetainedUI.DrawTexture(picture,Texture2D.blackTexture);
            var tex=serviceHost.CameraHelpTexture;
            if(tex!=null)
                JungleRetainedUI.DrawTextureWithTexCoords(picture,tex,new Rect(1,0,-1,1));
            else
                JungleRetainedUI.Label(picture,serviceHost.CameraConnected?"ENCUADRE EN VIVO":"SIN SEÑAL DE CÁMARA",pipWaitStyle);
            DrawSafeZoneBrackets(picture,framing.Status);
            DrawCameraGuides(picture,serviceHost.CameraHelpFrame);

            string caption=framing.HandRaised?"MANO ARRIBA DETECTADA":framing.Hint;
            JungleRetainedUI.Label(new Rect(card.x+6f,picture.yMax+2f,card.width-12f,footerH),caption,framing.HandRaised?pipHintRaisedStyle:pipHintNormalStyle);
        }
        JungleRetainedUI.depth=prevDepth;
    }

    void DrawSafeZoneBrackets(Rect picture,CameraFramingStatus status)
    {
        Color prev=JungleRetainedUI.color;
        JungleRetainedUI.color=status==CameraFramingStatus.Optimal?new Color(.18f,.95f,.52f,.85f)
            :status==CameraFramingStatus.MissingHands||status==CameraFramingStatus.OutOfSafeZone?new Color(1f,.78f,.14f,.85f)
            :new Color(.95f,.32f,.25f,.75f);
        Rect zone=new Rect(picture.x+picture.width*.14f,picture.y+picture.height*.10f,picture.width*.72f,picture.height*.80f);
        float len=Mathf.Min(zone.width,zone.height)*.18f;
        float t=2f;
        // Top-left
        JungleRetainedUI.DrawTexture(new Rect(zone.x,zone.y,len,t),Texture2D.whiteTexture);
        JungleRetainedUI.DrawTexture(new Rect(zone.x,zone.y,t,len),Texture2D.whiteTexture);
        // Top-right
        JungleRetainedUI.DrawTexture(new Rect(zone.xMax-len,zone.y,len,t),Texture2D.whiteTexture);
        JungleRetainedUI.DrawTexture(new Rect(zone.xMax-t,zone.y,t,len),Texture2D.whiteTexture);
        // Bottom-left
        JungleRetainedUI.DrawTexture(new Rect(zone.x,zone.yMax-t,len,t),Texture2D.whiteTexture);
        JungleRetainedUI.DrawTexture(new Rect(zone.x,zone.yMax-len,t,len),Texture2D.whiteTexture);
        // Bottom-right
        JungleRetainedUI.DrawTexture(new Rect(zone.xMax-len,zone.yMax-t,len,t),Texture2D.whiteTexture);
        JungleRetainedUI.DrawTexture(new Rect(zone.xMax-t,zone.yMax-len,t,len),Texture2D.whiteTexture);
        JungleRetainedUI.color=prev;
    }
    static CameraLandmark FindLandmark(CameraLandmark[] landmarks, string name)
    {
        for (int i = 0; i < landmarks.Length; i++)
        {
            var lm = landmarks[i];
            if (lm != null && lm.name == name) return lm;
        }
        return null;
    }

    void DrawCameraGuides(Rect picture,CameraInputWire frame)
    {
        if(frame?.subjects==null)return;
        Color before=JungleRetainedUI.color;
        foreach(var subject in frame.subjects)
        {
            if(subject?.landmarks==null)continue;
            JungleRetainedUI.color=subject.ambiguous?Color.yellow:Color.green;
            foreach(var point in subject.landmarks)
            {
                if(point==null||!point.visible||point.x<0||point.x>1||point.y<0||point.y>1)continue;
                Vector2 p=CameraPoint(picture,subject.subjectId,point);JungleRetainedUI.DrawTexture(new Rect(p.x-3,p.y-3,6,6),Texture2D.whiteTexture);
            }
            for(int i=0;i<SkeletonBonePairs.Length;i++)
            {
                var pair=SkeletonBonePairs[i];
                var a=FindLandmark(subject.landmarks,pair.A);
                var b=FindLandmark(subject.landmarks,pair.B);
                if(a==null||b==null||!a.visible||!b.visible||a.x<0||a.x>1||a.y<0||a.y>1||b.x<0||b.x>1||b.y<0||b.y>1)continue;
                Vector2 start=CameraPoint(picture,subject.subjectId,a),end=CameraPoint(picture,subject.subjectId,b);var matrix=JungleRetainedUI.matrix;
                JungleRetainedUI.RotateAroundPivot(Mathf.Atan2(end.y-start.y,end.x-start.x)*Mathf.Rad2Deg,start);
                JungleRetainedUI.DrawTexture(new Rect(start.x,start.y,Vector2.Distance(start,end),2),Texture2D.whiteTexture);JungleRetainedUI.matrix=matrix;
            }
        }
        JungleRetainedUI.color=before;
    }
    Vector2 CameraPoint(Rect picture,int subject,CameraLandmark point)
    {
        float x=point.x,y=point.y;
        if(serviceHost.TryCameraVisualLandmark(subject,point.name,out float px,out float py)){x=px;y=py;}
        return new Vector2(picture.x+(1-x)*picture.width,picture.y+y*picture.height);
    }
    void OnDestroy()
    {
        if (Instance != this) return;
        Time.timeScale=pause.Apply(false,Time.timeScale);
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }
}
