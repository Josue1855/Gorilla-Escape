using JungleParty.Portable;
using UnityEngine;
using UnityEngine.SceneManagement;

// Gameplay facade only. No HTTP polling, process management, room creation or transport authority.
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
    public static MotionSession Ensure() => Instance != null ? Instance : new GameObject("SESION PERSISTENTE - Fase 0").AddComponent<MotionSession>();
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        ConfigureForScene(SceneManager.GetActiveScene().name);
        serviceHost = PortableServiceHost.Ensure();
        Connect();
    }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ConfigureForScene(scene.name);
    public void ConfigureForScene(string sceneName)
    {
        HandMode = sceneName == SceneNavigator.Boxing || sceneName == SceneNavigator.Drums || sceneName == SceneNavigator.CoconutSlice
            ? HandTrackingMode.Both : HandTrackingMode.RightOnly;
    }
    public void Connect() => serviceHost.StartModule();
    public string ConsumeMotion() => serviceHost.ConsumeMotion();
    public string ConsumePose() => serviceHost.ConsumePose();
    public bool TryConsumeMotion(out string json) { json = ConsumeMotion(); return !string.IsNullOrEmpty(json); }
    public bool TryConsumePose(out string json) { json = ConsumePose(); return !string.IsNullOrEmpty(json); }
    void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }
}
