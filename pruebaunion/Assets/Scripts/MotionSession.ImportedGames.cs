
using System;
using GorillaEscape.Gameplay.Input;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed partial class MotionSession
{
    [Serializable]
    internal sealed class MotionSample
    {
        public long time;
        public float gamma, screen, ax, ay, az, rx, ry, rz;
        public bool hasScreen => true;
    }

    [Serializable]
    internal sealed class PoseSample
    {
        public long time;
        public bool leftDetected, rightDetected, headDetected;
        public float headYaw, headPitch, headRoll;
        public float leftWristX, leftWristY, rightWristX, rightWristY, bodyX, bodyY = .5f;
    }

    private MotionSample importedMotion;
    private PoseSample importedPose;
    private long inputRevision;
    private float lastThrowAt;
    private bool throwArmed;
    private JungleHand left, right;
    private JungleAction availableActions;
    private bool activeRound;

    private static bool IsBalance => SceneManager.GetActiveScene().name == SceneNavigator.LogBalance;
    private static bool IsKeeper => SceneManager.GetActiveScene().name == SceneNavigator.JungleKeeper;
    private bool Tracked => CameraConnected && Time.unscaledTime - lastTrackedAt < .35f;
    private float lastTrackedAt = -10f;

    public bool IsInputReady => IsBalance ? PhoneConnected
        : IsKeeper ? Tracked && (importedPose.leftDetected || importedPose.rightDetected)
        : PhoneConnected && Tracked && importedPose.rightDetected;

    public bool TryConsumeFrame(out JungleFusedFrame frame)
    {
        frame = null;
        bool newMotion = false, newPose = false;
        if (!IsKeeper && TryConsumeMotion(out string motion))
        {
            var sample = JsonUtility.FromJson<MotionSample>(motion);
            if (sample.time > 0 && sample.time != importedMotion?.time)
            {
                importedMotion = sample;
                newMotion = true;
            }
        }

        if (!IsBalance && TryConsumePose(out string pose))
        {
            var sample = JsonUtility.FromJson<PoseSample>(pose);
            if (sample.time > 0 && sample.time != importedPose?.time)
            {
                float seconds = importedPose == null ? 0 : (sample.time - importedPose.time) * .001f;
                left = ReadHand(sample.leftWristX, sample.leftWristY, left,
                    sample.leftDetected && importedPose?.leftDetected == true, seconds);
                right = ReadHand(sample.rightWristX, sample.rightWristY, right,
                    sample.rightDetected && importedPose?.rightDetected == true, seconds);
                importedPose = sample;
                if (sample.leftDetected || sample.rightDetected) lastTrackedAt = Time.unscaledTime;
                newPose = true;
            }
        }

        if (!IsInputReady || (!newPose && !newMotion)) return false;

        float power = importedMotion == null ? 0
            : new Vector3(importedMotion.rx, importedMotion.ry, importedMotion.rz).magnitude
            + 7f * new Vector3(importedMotion.ax, importedMotion.ay, importedMotion.az).magnitude;
        availableActions = IsBalance ? JungleAction.Balance
            : IsKeeper ? JungleAction.Save : JungleAction.None;
        if (power < 160f) throwArmed = true;
        if (!IsBalance && !IsKeeper && newMotion && activeRound && throwArmed &&
            power > 280f && right.Speed > .35f && Time.unscaledTime - lastThrowAt > .5f)
        {
            availableActions |= JungleAction.Throw;
            lastThrowAt = Time.unscaledTime;
            throwArmed = false;
        }

        frame = new JungleFusedFrame
        {
            Revision = ++inputRevision,
            PhoneSequence = importedMotion?.time ?? 0,
            PhoneTimestamp = importedMotion?.time ?? 0,
            CameraTimestamp = importedPose?.time ?? 0,
            NewPose = newPose,
            Motion = importedMotion,
            Pose = importedPose,
            Left = left,
            Right = right,
            PhonePower = power,
            Actions = availableActions
        };
        return true;
    }

    private static JungleHand ReadHand(float x, float y, JungleHand previous, bool continuous, float seconds)
    {
        if (!continuous || seconds <= 0 || seconds > .2f) return new JungleHand(x, y, 0, 0);
        Vector2 velocity = Vector2.ClampMagnitude(new Vector2(x - previous.X, y - previous.Y) / seconds, 12f);
        return new JungleHand(x, y, velocity.x, velocity.y);
    }

    public bool TryAuthorize(JungleAction action, long revision)
    {
        if (!IsInputReady || revision != inputRevision || (availableActions & action) == 0) return false;
        availableActions &= ~action;
        return true;
    }

    public void SetGameplayActive(bool value) => activeRound = value;

    public void ResetRound()
    {
        availableActions = JungleAction.None;
        throwArmed = false;
    }

    private void ResetImportedInput()
    {
        ResetRound();
        activeRound = false;
        importedMotion = null;
        importedPose = null;
        lastTrackedAt = -10f;
        left = right = default;
    }

    public bool TryCameraCursor(out Vector2 position, out string handLabel, out bool raised,
        out bool closed, out bool forwardPush)
    {
        handLabel = "MANO";
        closed = forwardPush = raised = false;
        position = new Vector2(.5f, .5f);
        if (!Tracked) return false;

        bool useRight = importedPose.rightDetected;
        if (!useRight && !importedPose.leftDetected) return false;
        JungleHand hand = useRight ? right : left;
        HandCursorMapping.Map(hand.X, hand.Y, out position.x, out position.y);
        raised = hand.Y < importedPose.bodyY;
        return true;
    }
}

namespace GorillaEscape.Gameplay.Input
{
    [Flags]
    public enum JungleAction { None = 0, Throw = 1, Balance = 2, Save = 4, SliceLeft = 8, SliceRight = 16 }

    public readonly struct JungleHand
    {
        public readonly float X, Y, VelocityX, VelocityY;
        public float Speed => Mathf.Sqrt(VelocityX * VelocityX + VelocityY * VelocityY);
        public JungleHand(float x, float y, float velocityX, float velocityY)
        {
            X = x;
            Y = y;
            VelocityX = velocityX;
            VelocityY = velocityY;
        }
    }

    public sealed class JungleFusedFrame
    {
        public long Revision, PhoneSequence, PhoneTimestamp, CameraTimestamp;
        public bool Baseline, NewPose;
        internal MotionSession.MotionSample Motion;
        internal MotionSession.PoseSample Pose;
        public JungleHand Left, Right;
        public float PhonePower;
        public JungleAction Actions;
    }
}

