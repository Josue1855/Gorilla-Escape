using JungleParty.Portable;
using GorillaEscape.Gameplay.Input;
using UnityEngine;
using UnityEngine.SceneManagement;

// Gameplay facade only. No HTTP polling, process management, room creation or transport authority.
[DefaultExecutionOrder(-10000)]
public sealed class MotionSession : MonoBehaviour, IPortableInputSource
{
    public enum HandTrackingMode { RightOnly, LeftOnly, Both }
    public static MotionSession Instance { get; private set; }
    public string RoomCode => serviceHost != null && !string.IsNullOrEmpty(serviceHost.RoomCode) ? serviceHost.RoomCode : "";
    public string Status => serviceHost != null ? serviceHost.PhoneStatus : "SIN CONEXIÓN";
    public bool IsPolling => serviceHost != null && serviceHost.IsRunning;
    public bool PhoneConnected => serviceHost != null && serviceHost.PhoneConnected;
    public bool CameraConnected => serviceHost != null && serviceHost.CameraConnected;
    public float LastPhonePacketAt => serviceHost != null ? serviceHost.LastPhonePacketAt : -10;
    public float LastPosePacketAt => serviceHost != null ? serviceHost.LastPosePacketAt : -10;
    public HandTrackingMode HandMode { get; private set; } = HandTrackingMode.RightOnly;
    public bool UsesLeftHand => HandMode != HandTrackingMode.RightOnly;
    public bool UsesRightHand => HandMode != HandTrackingMode.LeftOnly;
    PortableServiceHost serviceHost;
    readonly GorillaEscape.Contracts.GamePresentationWire presentation=new GorillaEscape.Contracts.GamePresentationWire();
    float nextPresentation;
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
        if(serviceHost==null||Time.unscaledTime<nextPresentation)return;
        nextPresentation=Time.unscaledTime+.2f;
        var context=presentation.Copy();
        if(context.gameId!="lobby"&&(!IsInputReady||Time.timeScale==0)&&context.phase!="Won"&&context.phase!="Lost"&&context.phase!="Finished")context.phase="Paused";
        serviceHost.PresentPhoneGame(context);
    }
    long delivered=-1;
    readonly JunglePauseLease pause=new JunglePauseLease();
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
        ConfigureForScene(scene.name);
        serviceHost.SelectGame(JungleGameplayInput.ForScene(scene.name),true);
        delivered=-1;
        RefreshPause();
    }
    public void ConfigureForScene(string sceneName)
    {
        presentation.gameId=JungleGameplayInput.ForScene(sceneName) switch {
            JungleGame.Bowling=>"bowling",JungleGame.Boxing=>"boxing",JungleGame.Baseball=>"baseball",JungleGame.Drums=>"drums",
            JungleGame.CoconutSlice=>"coconut-slice",JungleGame.Whack=>"whack",JungleGame.LogBalance=>"log-balance",_=>"lobby"};
        presentation.playerId=presentation.gameId=="lobby"?0:1;
        PresentGame("Ready");nextPresentation=0;
        HandMode = sceneName == SceneNavigator.Boxing || sceneName == SceneNavigator.Drums || sceneName == SceneNavigator.CoconutSlice || sceneName == SceneNavigator.LogBalance
            ? HandTrackingMode.Both : HandTrackingMode.RightOnly;
        serviceHost.SelectGame(JungleGameplayInput.ForScene(sceneName));
        delivered=-1;
    }
    public void Connect() => serviceHost.StartModule();
    public void ResetRound()
    {
        serviceHost.SelectGame(JungleGameplayInput.ForScene(SceneManager.GetActiveScene().name),true);
        delivered=-1;RefreshPause();
    }
    public bool TryConsumeFrame(out JungleFusedFrame frame)
    {
        RefreshPause();
        if(!serviceHost.TryReadFrame(out frame)||frame.Revision==delivered)return false;
        delivered=frame.Revision;return true;
    }
    public bool TryAuthorize(JungleAction action,long revision) => serviceHost.Authorize(action,revision);
    void Update() => RefreshPause();
    void RefreshPause()
    {
        bool paused=serviceHost!=null&&serviceHost.Game!=JungleGame.None&&!IsInputReady;
        float current=Time.timeScale;
        float desired=pause.Apply(paused,current);
        if(desired!=current)Time.timeScale=desired;
    }
    void OnGUI()
    {
        if(serviceHost==null||serviceHost.Game==JungleGame.None||IsInputReady)return;
        int depth=GUI.depth;GUI.depth=-100;
        float width=Mathf.Min(Screen.width-40,760);
        GUI.Box(new Rect((Screen.width-width)/2,Screen.height*.38f,width,140),"JUEGO EN PAUSA");
        string message=serviceHost.Game==JungleGame.LogBalance?"Revisa la conexión y los sensores del teléfono."
            :InputStatus==JungleInputStatus.PartialInput?"Revisa los permisos y sensores del teléfono."
            :"Colócate frente a la cámara y mantén visibles las manos. Revisa la conexión del teléfono.";
        GUI.Label(new Rect((Screen.width-width)/2+20,Screen.height*.38f+35,width-40,85),message,
            new GUIStyle(GUI.skin.label){wordWrap=true,alignment=TextAnchor.MiddleCenter,fontSize=20});
        GUI.depth=depth;
    }
    void OnDestroy()
    {
        if (Instance != this) return;
        Time.timeScale=pause.Apply(false,Time.timeScale);
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }
}
