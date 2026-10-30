using System;
using GorillaEscape.Contracts;

namespace GorillaEscape.Gameplay.Input
{
    public enum CameraFramingStatus
    {
        NoSignal,
        NoPerson,
        MultipleOrAmbiguous,
        OutOfSafeZone,
        MissingHands,
        Optimal
    }

    public readonly struct CameraFramingReport
    {
        public readonly CameraFramingStatus Status;
        public readonly string Badge;
        public readonly string Hint;
        public readonly float BodyX;
        public readonly float BodyY;
        public readonly float ShoulderSpan;
        public readonly bool LeftHandVisible;
        public readonly bool RightHandVisible;
        public readonly bool HandRaised;
        public readonly bool InSafeZone;

        public CameraFramingReport(
            CameraFramingStatus status,
            string badge,
            string hint,
            float bodyX,
            float bodyY,
            float shoulderSpan,
            bool leftHandVisible,
            bool rightHandVisible,
            bool handRaised,
            bool inSafeZone)
        {
            Status = status;
            Badge = badge;
            Hint = hint;
            BodyX = bodyX;
            BodyY = bodyY;
            ShoulderSpan = shoulderSpan;
            LeftHandVisible = leftHandVisible;
            RightHandVisible = rightHandVisible;
            HandRaised = handRaised;
            InSafeZone = inSafeZone;
        }
    }

    // Menu navigation only: the central camera area covers the whole screen.
    // This mapping is never used by gesture recognition or scoring.
    public static class HandCursorMapping
    {
        public const float HorizontalMargin = .18f;
        public const float VerticalMargin = .15f;
        public static float MapAxis(float value,float margin)
            => Math.Max(0f,Math.Min(1f,(value-margin)/(1f-2f*margin)));
        public static void Map(float mirroredX,float y,out float screenX,out float screenY)
        {
            screenX=MapAxis(mirroredX,HorizontalMargin);
            screenY=MapAxis(y,VerticalMargin);
        }
    }
    public readonly struct HandDwellTarget
    {
        public readonly int Id;
        public readonly float X;
        public readonly float Y;
        public readonly float Width;
        public readonly float Height;

        public HandDwellTarget(int id, float x, float y, float width, float height)
        {
            Id = id;
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public bool Contains(float px, float py)
            => px >= X && px <= X + Width && py >= Y && py <= Y + Height;
    }

    public sealed class HandDwellNavigator
    {
        public const float DefaultDwellSeconds = .95f;
        public const float EdgeGraceSeconds = .22f;
        public const float CooldownSeconds = .55f;

        public float CursorX { get; private set; } = .5f;
        public float CursorY { get; private set; } = .5f;
        public bool CursorVisible { get; private set; }
        public int HoveredId { get; private set; } = -1;
        public float Progress { get; private set; }
        public float RaiseHandProgress { get; private set; }

        float lastObserveAt = -1f;
        float dwellElapsed;
        float outsideElapsed;
        float cooldownUntil = -1f;
        int latchedId = -1;

        float lastRaiseAt = -1f;
        float raiseElapsed;
        bool raiseLatched;

        public void Reset()
        {
            CursorVisible = false;
            HoveredId = -1;
            Progress = 0f;
            RaiseHandProgress = 0f;
            lastObserveAt = -1f;
            dwellElapsed = 0f;
            outsideElapsed = 0f;
            cooldownUntil = -1f;
            latchedId = -1;
            lastRaiseAt = -1f;
            raiseElapsed = 0f;
            raiseLatched = false;
        }

        public bool ObserveCursor(
            bool tracked,
            float screenX,
            float screenY,
            float now,
            HandDwellTarget[] targets,
            out int triggeredId,
            float dwellSeconds = DefaultDwellSeconds)
        {
            triggeredId = -1;
            if (!tracked || float.IsNaN(screenX) || float.IsInfinity(screenX) || float.IsNaN(screenY) || float.IsInfinity(screenY) || float.IsNaN(now) || float.IsInfinity(now))
            {
                CursorVisible = false;
                HoveredId = -1;
                Progress = 0f;
                dwellElapsed = 0f;
                outsideElapsed = 0f;
                latchedId = -1;
                lastObserveAt = -1f;
                return false;
            }

            float dt = lastObserveAt >= 0f && now >= lastObserveAt ? Math.Min(.25f, now - lastObserveAt) : 0f;
            if (!CursorVisible || lastObserveAt < 0f)
            {
                CursorX = screenX;
                CursorY = screenY;
            }
            else
            {
                float alpha = dt <= 0f ? 1f : Math.Min(1f, dt * 16f);
                CursorX += (screenX - CursorX) * alpha;
                CursorY += (screenY - CursorY) * alpha;
            }
            CursorVisible = true;
            lastObserveAt = now;

            int hitId = -1;
            if (targets != null)
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    if (targets[i].Contains(CursorX, CursorY))
                    {
                        hitId = targets[i].Id;
                        break;
                    }
                }
            }

            float requiredDwell = Math.Max(.2f, dwellSeconds);
            if (hitId >= 0)
            {
                outsideElapsed = 0f;
                if (hitId == latchedId)
                {
                    HoveredId = hitId;
                    Progress = 1f;
                    return false;
                }
                if (hitId != HoveredId)
                {
                    HoveredId = hitId;
                    dwellElapsed = 0f;
                    Progress = 0f;
                    latchedId = -1;
                    return false;
                }
                dwellElapsed += dt;
                Progress = Math.Max(0f, Math.Min(1f, dwellElapsed / requiredDwell));
                if (dwellElapsed >= requiredDwell && now >= cooldownUntil)
                {
                    triggeredId = hitId;
                    latchedId = hitId;
                    cooldownUntil = now + CooldownSeconds;
                    Progress = 1f;
                    return true;
                }
                return false;
            }

            if (HoveredId >= 0)
            {
                outsideElapsed += dt;
                if (outsideElapsed >= EdgeGraceSeconds)
                {
                    HoveredId = -1;
                    Progress = 0f;
                    dwellElapsed = 0f;
                    outsideElapsed = 0f;
                    latchedId = -1;
                }
            }
            else
            {
                latchedId = -1;
            }
            return false;
        }

        public bool ObserveRaiseHand(bool handRaised, float now, float holdSeconds = .80f)
        {
            if (float.IsNaN(now) || float.IsInfinity(now)) return false;
            float dt = lastRaiseAt >= 0f && now >= lastRaiseAt ? Math.Min(.25f, now - lastRaiseAt) : 0f;
            lastRaiseAt = now;
            if (!handRaised)
            {
                raiseElapsed = Math.Max(0f, raiseElapsed - dt * 2.5f);
                if (raiseElapsed <= .05f) raiseLatched = false;
                RaiseHandProgress = Math.Max(0f, Math.Min(1f, raiseElapsed / Math.Max(.2f, holdSeconds)));
                return false;
            }
            if (raiseLatched)
            {
                RaiseHandProgress = 1f;
                return false;
            }
            raiseElapsed += dt;
            float required = Math.Max(.2f, holdSeconds);
            RaiseHandProgress = Math.Max(0f, Math.Min(1f, raiseElapsed / required));
            if (raiseElapsed >= required)
            {
                raiseLatched = true;
                RaiseHandProgress = 1f;
                return true;
            }
            return false;
        }
    }

    public enum GoalieSaveSource
    {
        None,
        LeftHand,
        RightHand,
        Torso
    }

    public readonly struct GoalieSaveResult
    {
        public readonly bool Saved;
        public readonly GoalieSaveSource Source;
        public readonly float Distance;
        public readonly float PrecisionMultiplier;
        public readonly int BasePoints;

        public GoalieSaveResult(bool saved, GoalieSaveSource source, float distance, float precisionMultiplier, int basePoints)
        {
            Saved = saved;
            Source = source;
            Distance = distance;
            PrecisionMultiplier = precisionMultiplier;
            BasePoints = basePoints;
        }
    }

    public static class ShotMechanics
    {
        // Release tilt selects the hook independently of the initial hand trajectory.
        public static float BowlingSpin(float tiltDegrees)
        {
            if(float.IsNaN(tiltDegrees)||float.IsInfinity(tiltDegrees))return 0;
            float magnitude=Math.Max(0,Math.Abs(tiltDegrees)-5f)/40f;
            return Math.Sign(tiltDegrees)*Math.Min(1,magnitude);
        }
        public static float BowlingCurveAcceleration(float spin) => Math.Max(-1,Math.Min(1,spin))*1.6f;
        // 1x at the hit boundary, up to 1.5x through the centre.
        public static float SlicePrecisionMultiplier(float distance,float radius)
        {
            if(float.IsNaN(distance)||float.IsInfinity(distance)||float.IsNaN(radius)||float.IsInfinity(radius)||radius<=0)return 1;
            float precision=1-Math.Max(0,Math.Min(1,distance/radius));
            return 1+.5f*precision;
        }

        static bool LandmarkVisible(CameraSubject subject, string name, out CameraLandmark landmark)
        {
            landmark = null;
            if (subject?.landmarks == null) return false;
            landmark = Array.Find(subject.landmarks, l => l != null && l.name == name);
            return landmark != null && landmark.visible && landmark.visibility >= .6f && landmark.presence >= .6f
                && landmark.x >= 0f && landmark.x <= 1f && landmark.y >= 0f && landmark.y <= 1f;
        }

        public static CameraFramingReport EvaluateFraming(bool cameraConnected, CameraInputWire frame, bool requireBothHands)
        {
            if (!cameraConnected)
                return new CameraFramingReport(CameraFramingStatus.NoSignal, "SIN CÁMARA", "Revisa que la cámara esté conectada y activa.", .5f, .5f, 0f, false, false, false, false);
            if (frame?.subjects == null || frame.subjects.Length == 0 || frame.trackingState != "TRACKING")
                return new CameraFramingReport(CameraFramingStatus.NoPerson, "BUSCANDO JUGADOR", "Colócate frente a la cámara dentro del recuadro.", .5f, .5f, 0f, false, false, false, false);
            if (frame.subjects.Length > 1 || frame.subjects[0] == null || frame.subjects[0].ambiguous)
                return new CameraFramingReport(CameraFramingStatus.MultipleOrAmbiguous, "1 SOLA PERSONA", "Despeja el área para que solo se vea un jugador.", .5f, .5f, 0f, false, false, false, false);

            var person = frame.subjects[0];
            float bodyX = Math.Max(0f, Math.Min(1f, 1f - person.centerX));
            float bodyY = Math.Max(0f, Math.Min(1f, person.centerY));
            bool leftShoulderOk = LandmarkVisible(person, "left-shoulder", out var leftShoulder);
            bool rightShoulderOk = LandmarkVisible(person, "right-shoulder", out var rightShoulder);
            bool leftWristOk = LandmarkVisible(person, "left-wrist", out var leftWrist);
            bool rightWristOk = LandmarkVisible(person, "right-wrist", out var rightWrist);

            float span = leftShoulderOk && rightShoulderOk ? Math.Abs(leftShoulder.x - rightShoulder.x) : 0f;
            bool leftRaised = leftWristOk && leftShoulderOk && leftWrist.y < leftShoulder.y - .03f;
            bool rightRaised = rightWristOk && rightShoulderOk && rightWrist.y < rightShoulder.y - .03f;
            bool handRaised = leftRaised || rightRaised;

            if (!leftShoulderOk || !rightShoulderOk)
                return new CameraFramingReport(CameraFramingStatus.OutOfSafeZone, "ALÉJATE UN POCO", "Muestra ambos hombros dentro de la zona segura.", bodyX, bodyY, span, leftWristOk, rightWristOk, handRaised, false);
            if (span > .68f)
                return new CameraFramingReport(CameraFramingStatus.OutOfSafeZone, "DEMASIADO CERCA", "Da un paso atrás para que se vean tus brazos.", bodyX, bodyY, span, leftWristOk, rightWristOk, handRaised, false);
            if (bodyX < .16f || bodyX > .84f || bodyY < .10f || bodyY > .88f)
                return new CameraFramingReport(CameraFramingStatus.OutOfSafeZone, "CENTRA TU CUERPO", "Muévete hacia el centro del recuadro.", bodyX, bodyY, span, leftWristOk, rightWristOk, handRaised, false);

            bool handsOk = requireBothHands ? (leftWristOk && rightWristOk) : (leftWristOk || rightWristOk);
            if (!handsOk)
            {
                string hint = requireBothHands
                    ? (!leftWristOk && !rightWristOk ? "Levanta ambas manos dentro del recuadro." : !leftWristOk ? "Muestra tu mano izquierda." : "Muestra tu mano derecha.")
                    : "Muestra al menos una mano frente a la cámara.";
                return new CameraFramingReport(CameraFramingStatus.MissingHands, "MUESTRA LAS MANOS", hint, bodyX, bodyY, span, leftWristOk, rightWristOk, handRaised, false);
            }

            return new CameraFramingReport(CameraFramingStatus.Optimal, "ENCUADRE ÓPTIMO", "Hombros y manos listos dentro de la zona segura.", bodyX, bodyY, span, leftWristOk, rightWristOk, handRaised, true);
        }

        public static GoalieSaveResult EvaluateGoalieSave(
            float shotX,
            float shotY,
            bool leftTracked,
            float leftX,
            float leftY,
            float leftSpeed,
            bool rightTracked,
            float rightX,
            float rightY,
            float rightSpeed,
            bool torsoTracked,
            float torsoX,
            float torsoY,
            float handRadius = .115f,
            float torsoRadius = .135f)
        {
            if (float.IsNaN(shotX) || float.IsInfinity(shotX) || float.IsNaN(shotY) || float.IsInfinity(shotY))
                return new GoalieSaveResult(false, GoalieSaveSource.None, float.MaxValue, 1f, 0);

            GoalieSaveSource bestSource = GoalieSaveSource.None;
            float bestDistance = float.MaxValue;
            float bestRadius = handRadius;
            float bestSpeed = 0f;

            if (leftTracked && handRadius > 0f)
            {
                float d = Distance(shotX, shotY, leftX, leftY);
                if (d <= handRadius && d < bestDistance)
                {
                    bestDistance = d;
                    bestSource = GoalieSaveSource.LeftHand;
                    bestRadius = handRadius;
                    bestSpeed = leftSpeed;
                }
            }
            if (rightTracked && handRadius > 0f)
            {
                float d = Distance(shotX, shotY, rightX, rightY);
                if (d <= handRadius && d < bestDistance)
                {
                    bestDistance = d;
                    bestSource = GoalieSaveSource.RightHand;
                    bestRadius = handRadius;
                    bestSpeed = rightSpeed;
                }
            }
            if (torsoTracked && torsoRadius > 0f)
            {
                float d = Distance(shotX, shotY, torsoX, torsoY);
                if (d <= torsoRadius && d < bestDistance)
                {
                    bestDistance = d;
                    bestSource = GoalieSaveSource.Torso;
                    bestRadius = torsoRadius;
                    bestSpeed = 0f;
                }
            }

            if (bestSource == GoalieSaveSource.None)
                return new GoalieSaveResult(false, GoalieSaveSource.None, float.MaxValue, 1f, 0);

            float precision = SlicePrecisionMultiplier(bestDistance, bestRadius);
            int reflexBonus = !float.IsNaN(bestSpeed) && !float.IsInfinity(bestSpeed) && bestSpeed > .65f ? 25 : 0;
            int basePoints = (bestSource == GoalieSaveSource.Torso ? 105 : 125) + reflexBonus;
            return new GoalieSaveResult(true, bestSource, bestDistance, precision, basePoints);
        }

        public static void GoalieCornerTarget(int cornerIndex, out float worldX, out float worldY)
        {
            switch (Math.Abs(cornerIndex) % 6)
            {
                case 0: worldX = -5.1f; worldY = 2.45f; break;
                case 1: worldX = 5.1f; worldY = 2.45f; break;
                case 2: worldX = -5.5f; worldY = .15f; break;
                case 3: worldX = 5.5f; worldY = .15f; break;
                case 4: worldX = -4.9f; worldY = -2.15f; break;
                default: worldX = 4.9f; worldY = -2.15f; break;
            }
        }

        static float Distance(float ax, float ay, float bx, float by)
        {
            float dx = ax - bx, dy = ay - by;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }
}
