using System;
using UnityEngine;

// Consumes only the existing MotionSession. No camera or extra HTTP poller is needed.
public sealed class PhoneMenuPointer : MonoBehaviour
{
    [Serializable] public struct Packet
    {
        public double time;
        public float alpha, beta, gamma, screen;
        public int confirm, recenter;
    }
    public Vector2 Position { get; private set; } = new Vector2(.5f, .5f);
    public bool Connected => session != null && session.PhoneConnected && Time.unscaledTime - receivedAt < 1.2f;
    private MotionSession session;
    private Packet latest;
    private Vector2 target = new Vector2(.5f, .5f);
    private float centerYaw, centerPitch, centerRoll, screenAngle, receivedAt = -10f;
    private int lastConfirm, lastRecenter;
    private double lastTime;
    private bool calibrated, pendingConfirm;

    private void Awake() { session = MotionSession.Ensure(); }
    private void Update()
    {
        ReadPacket();
        if (Connected)
            Position = Vector2.Lerp(Position, target, 1f - Mathf.Exp(-36f * Time.unscaledDeltaTime));
    }

    private void ReadPacket()
    {
        string json = session.LatestMotionJson;
        if (string.IsNullOrEmpty(json)) return;
        Packet packet;
        try { packet = JsonUtility.FromJson<Packet>(json); }
        catch (ArgumentException) { return; }
        if (packet.time <= 0 || double.IsNaN(packet.time) || double.IsInfinity(packet.time) ||
            !Finite(packet.alpha) || !Finite(packet.beta) || !Finite(packet.gamma) || !Finite(packet.screen)) return;
        // The relay retains its last packet. Do not treat repeated/stale samples as movement.
        if (packet.time == lastTime) return;
        bool reconnect = Time.unscaledTime - receivedAt > 1.2f || packet.time < lastTime;
        lastTime = packet.time;
        receivedAt = Time.unscaledTime;
        latest = packet;
        if (!calibrated || reconnect)
        {
            lastConfirm = packet.confirm;
            lastRecenter = packet.recenter;
            pendingConfirm = false;
            Recenter();
        }
        else
        {
            // Monotonic counters allow a held button or repeated packet to activate only once.
            if (packet.confirm > lastConfirm) pendingConfirm = true;
            lastConfirm = packet.confirm;
            if (packet.recenter != lastRecenter || Mathf.Abs(Mathf.DeltaAngle(screenAngle, packet.screen)) > 45f)
                Recenter();
            lastRecenter = packet.recenter;
        }
        target = Project(packet, centerYaw, centerPitch, centerRoll);
    }
    public static Vector2 Project(Packet packet, float yaw, float pitch, float roll)
    {
        Vector2 offset = new Vector2(-Mathf.DeltaAngle(yaw, packet.alpha), Mathf.DeltaAngle(pitch, packet.beta));
        if (Mathf.Abs(offset.x) < .15f) offset.x = Mathf.DeltaAngle(roll, packet.gamma);
        offset = (Vector2)(Quaternion.Euler(0, 0, -packet.screen) * offset);
        return new Vector2(Mathf.Clamp01(.5f + DeadZone(offset.x) / 65f), Mathf.Clamp01(.5f - DeadZone(offset.y) / 55f));
    }
    private static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);
    private static float DeadZone(float n) => Mathf.Sign(n) * Mathf.Max(0, Mathf.Abs(n) - .8f);
    public void Recenter()
    {
        centerYaw = latest.alpha; centerPitch = latest.beta; centerRoll = latest.gamma;
        screenAngle = latest.screen; calibrated = true; target = Position = new Vector2(.5f, .5f);
    }
    public bool TakeConfirm()
    {
        bool result = pendingConfirm && Connected;
        pendingConfirm = false;
        return result;
    }
    public Vector2 ScreenPosition => new Vector2(Position.x * Screen.width, Position.y * Screen.height);
}

