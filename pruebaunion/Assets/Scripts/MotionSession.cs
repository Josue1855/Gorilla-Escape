using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using JungleParty.Portable;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed partial class MotionSession : MonoBehaviour, IPortableInputSource
{
    public enum HandTrackingMode { RightOnly, LeftOnly, Both }
    public static MotionSession Instance { get; private set; }
    public string RoomCode { get; private set; }
    public string Status { get; private set; } = "LISTO PARA CONECTAR";
    public bool IsPolling { get; private set; }
    public bool PhoneConnected => Time.unscaledTime - LastPhonePacketAt < 1.2f;
    public bool CameraConnected => Time.unscaledTime - LastPosePacketAt < 1f;
    internal PoseSample LatestPose => CameraConnected ? latestPose : null;
    private PoseSample latestPose;
    public float LastPhonePacketAt { get; private set; } = -10f;
    public float LastPosePacketAt { get; private set; } = -10f;
    public HandTrackingMode HandMode { get; private set; } = HandTrackingMode.RightOnly;
    public bool UsesLeftHand => HandMode != HandTrackingMode.RightOnly;
    public bool UsesRightHand => HandMode != HandTrackingMode.LeftOnly;

    private readonly HttpClient http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
    private readonly object sync = new object();
    private CancellationTokenSource cancellation;
    private string motionJson, poseJson;
    private PortableServiceHost serviceHost;
    private RemoteButtons lastRemoteButtons;
    private float acceptUntil, backUntil;
    private bool remoteMenuPending;

    [Serializable]
    private sealed class RemoteButtons
    {
        public long time;
        public int confirm, back, menu;
    }

    private void ReadRemoteButtons(string json)
    {
        RemoteButtons current = JsonUtility.FromJson<RemoteButtons>(json);
        if (current == null || current.time <= 0 || current.time == lastRemoteButtons?.time) return;
        RemoteButtons previous = lastRemoteButtons;
        lastRemoteButtons = current;
        if (previous == null || current.time < previous.time) return;
        float deadline = Time.unscaledTime + .22f;
        if (current.confirm > previous.confirm) acceptUntil = deadline;
        if (current.back > previous.back) backUntil = deadline;
        if (current.menu > previous.menu) remoteMenuPending = true;
    }

    public bool TakeRemoteAccept()
    {
        if (Time.unscaledTime >= acceptUntil) return false;
        acceptUntil = 0f;
        return true;
    }

    public bool TakeRemoteBack()
    {
        if (Time.unscaledTime >= backUntil) return false;
        backUntil = 0f;
        return true;
    }

    public static bool PrimaryButton(Rect rect, string label, GUIStyle style)
    {
        return GUI.Button(rect, label, style) ||
            (GUI.enabled && Event.current.type == EventType.Repaint && Instance != null && Instance.TakeRemoteAccept());
    }

    public static MotionSession Ensure()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("SESION PERSISTENTE - Sensores");
        return go.AddComponent<MotionSession>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        ConfigureForScene(SceneManager.GetActiveScene().name);
        serviceHost = PortableServiceHost.Ensure();
        RoomCode = RandomRoom();
        Connect();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        acceptUntil = backUntil = 0f;
        remoteMenuPending = false;
        ResetImportedInput();
        ConfigureForScene(scene.name);
    }

    public void ConfigureForScene(string sceneName)
    {
        HandMode = sceneName == SceneNavigator.Boxing || sceneName == SceneNavigator.Drums || sceneName == SceneNavigator.CoconutSlice || sceneName == SceneNavigator.JungleKeeper
            ? HandTrackingMode.Both
            : HandTrackingMode.RightOnly;
        Debug.Log($"Rastreo corporal para {sceneName}: {(HandMode == HandTrackingMode.Both ? "dos manos" : "mano derecha")}");
    }

    public void Connect()
    {
        if (IsPolling) return;
        serviceHost.StartModule();
        cancellation = new CancellationTokenSource(); IsPolling = true; Status = "ESPERANDO EL CELULAR";
        _ = PollPhone(cancellation.Token); _ = PollPose(cancellation.Token);
    }

    public string ConsumeMotion()
    {
        lock (sync) { string value = motionJson; motionJson = null; return value; }
    }

    public string ConsumePose()
    {
        lock (sync) { string value = poseJson; poseJson = null; return value; }
    }

    public bool TryConsumeMotion(out string json)
    {
        json = ConsumeMotion();
        return !string.IsNullOrEmpty(json);
    }

    public bool TryConsumePose(out string json)
    {
        json = ConsumePose();
        return !string.IsNullOrEmpty(json);
    }

    private void Update()
    {
        serviceHost?.ReportReadyState(PhoneConnected, CameraConnected);
        string scene = SceneManager.GetActiveScene().name;
        bool leaveGame = SceneNavigator.IsMinigame(scene) && (remoteMenuPending || TakeRemoteBack());
        bool openCatalog = remoteMenuPending && scene == SceneNavigator.Splash;
        remoteMenuPending = false;
        if (leaveGame || openCatalog) SceneNavigator.Load(SceneNavigator.MainMenu);
        else if (scene == SceneNavigator.MainMenu && TakeRemoteBack()) SplashDirector.OpenConnectedMenu();
    }

    private async Task PollPhone(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                using HttpResponseMessage response = await http.GetAsync(serviceHost.MotionEndpoint + RoomCode, token);
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent) Status = "ESPERANDO EL CELULAR";
                else
                {
                    response.EnsureSuccessStatusCode(); string json = await response.Content.ReadAsStringAsync();
                    ReadRemoteButtons(json);
                    lock (sync) motionJson = json; LastPhonePacketAt = Time.unscaledTime; Status = "CELULAR CONECTADO";
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { break; }
            catch { Status = "RECONECTANDO"; }
            try { await Task.Delay(10, token); } catch { break; }
        }
    }

    private async Task PollPose(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                using HttpResponseMessage response = await http.GetAsync(serviceHost.PoseEndpoint, token);
                response.EnsureSuccessStatusCode(); string json = await response.Content.ReadAsStringAsync();
                var sample = JsonUtility.FromJson<PoseSample>(json);
                if (sample != null && sample.time > 0 && sample.time != latestPose?.time)
                {
                    latestPose = sample;
                    lock (sync) poseJson = json;
                    LastPosePacketAt = Time.unscaledTime;
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { break; }
            catch (OperationCanceledException) { }
            catch (HttpRequestException) { }
            catch (ArgumentException exception) { Debug.LogWarning("Paquete de cámara inválido: " + exception.Message); }
            try { await Task.Delay(12, token); } catch { break; }
        }
    }

    private static string RandomRoom()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; char[] result = new char[4];
        for (int i = 0; i < result.Length; i++) result[i] = chars[UnityEngine.Random.Range(0, chars.Length)];
        return new string(result);
    }

    private void OnDestroy() { if (Instance == this) { SceneManager.sceneLoaded -= OnSceneLoaded; cancellation?.Cancel(); http.Dispose(); Instance = null; } }
}
