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
    public string RoomCode => "JUGADOR 1";
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
        string message=InputStatus==JungleInputStatus.PartialInput?"Revisa los permisos y sensores del teléfono."
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
