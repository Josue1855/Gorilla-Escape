using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using JungleParty.Portable;
using UnityEngine;

public sealed class MotionSession : MonoBehaviour, IPortableInputSource
{
    public static MotionSession Instance { get; private set; }
    public string RoomCode { get; private set; }
    public string Status { get; private set; } = "LISTO PARA CONECTAR";
    public bool IsPolling { get; private set; }
    public bool PhoneConnected => Time.unscaledTime - LastPhonePacketAt < 1.2f;
    public bool CameraConnected => Time.unscaledTime - LastPosePacketAt < .7f;
    public float LastPhonePacketAt { get; private set; } = -10f;
    public float LastPosePacketAt { get; private set; } = -10f;

    private readonly HttpClient http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
    private readonly object sync = new object();
    private CancellationTokenSource cancellation;
    private string motionJson, poseJson;
    private PortableServiceHost serviceHost;

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
        serviceHost = PortableServiceHost.Ensure();
        RoomCode = RandomRoom();
        Connect();
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
                    lock (sync) motionJson = json; LastPhonePacketAt = Time.unscaledTime; Status = "CELULAR CONECTADO";
                }
            }
            catch (OperationCanceledException) { break; }
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
                lock (sync) poseJson = json; LastPosePacketAt = Time.unscaledTime;
            }
            catch (OperationCanceledException) { break; }
            catch { }
            try { await Task.Delay(12, token); } catch { break; }
        }
    }

    private static string RandomRoom()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; char[] result = new char[4];
        for (int i = 0; i < result.Length; i++) result[i] = chars[UnityEngine.Random.Range(0, chars.Length)];
        return new string(result);
    }

    private void OnDestroy() { if (Instance == this) { cancellation?.Cancel(); http.Dispose(); Instance = null; } }
}
