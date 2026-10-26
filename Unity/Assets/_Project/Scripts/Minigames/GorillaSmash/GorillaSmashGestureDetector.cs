using System;
using System.Collections.Generic;
using GorillaEscape.Input;
using UnityEngine;

namespace GorillaEscape.Minigames.GorillaSmash
{
    /// <summary>
    /// Analyzes physical phone telemetry and accessibility inputs to detect
    /// complete temporal smash gestures (DEC-008, DEC-009, Spec v4 Section 17).
    /// Prevents false triggers, rejects shaking/agitation, and computes
    /// normalized Power, DirectionAccuracy, Timing, and GestureConfidence.
    /// </summary>
    public sealed class GorillaSmashGestureDetector
    {
        readonly GorillaSmashRules rules;
        readonly Queue<float> recentAccelY = new Queue<float>();
        readonly Queue<long> accelTimestamps = new Queue<long>();

        bool isArmed;
        float armHoldTime;
        float lastPeakAcceleration;
        float swingStartTime;
        int shakeReversals;
        float lastSign;
        long lastSampleTime;

        // Keyboard / debug fallback state
        bool spaceHeld;
        float spaceHoldTime;

        public bool IsArmed => isArmed;
        public float ArmHoldTime => armHoldTime;
        public string StatusMessage { get; private set; } = "Sostén el teléfono levantado para preparar el golpe.";

        public GorillaSmashGestureDetector(GorillaSmashRules rules)
        {
            this.rules = rules ?? new GorillaSmashRules();
        }

        public void Reset()
        {
            isArmed = false;
            armHoldTime = 0f;
            lastPeakAcceleration = 0f;
            swingStartTime = 0f;
            shakeReversals = 0;
            lastSign = 0f;
            recentAccelY.Clear();
            accelTimestamps.Clear();
            spaceHeld = false;
            spaceHoldTime = 0f;
            StatusMessage = "Sostén el teléfono levantado para preparar el golpe.";
        }

        /// <summary>
        /// Analyzes phone sensors from PhoneInputStore.
        /// Returns true if a valid, non-cheat downward smash strike was completed.
        /// </summary>
        public bool TryDetectPhoneStrike(
            PhoneInputStore phoneStore,
            int playerId,
            float deltaTime,
            long nowUnixMs,
            out float power,
            out float directionAccuracy,
            out float timing,
            out float confidence)
        {
            power = 0f;
            directionAccuracy = 0f;
            timing = 0f;
            confidence = 0f;

            if (phoneStore == null || !phoneStore.TryPhoneInput(playerId, out var phone) || !phone.IsConnected)
            {
                StatusMessage = "Esperando conexión del teléfono del jugador " + playerId;
                return false;
            }

            if (!phone.HasAcceleration)
            {
                StatusMessage = "Esperando lecturas del acelerómetro...";
                return false;
            }

            // Clean sample age check
            long sampleAge = phoneStore.AgeMilliseconds(playerId, nowUnixMs);
            if (sampleAge > 350)
            {
                StatusMessage = "Señal del sensor retrasada (" + sampleAge + " ms).";
                return false;
            }

            Vector3 acc = phone.Acceleration;
            float downwardAccel = -acc.y; // Dominant downward movement along phone body
            float transverseMag = Mathf.Sqrt(acc.x * acc.x + acc.z * acc.z);

            // Maintain sliding window for anti-shake / jitter detection
            recentAccelY.Enqueue(downwardAccel);
            accelTimestamps.Enqueue(nowUnixMs);
            while (accelTimestamps.Count > 0 && nowUnixMs - accelTimestamps.Peek() > 400)
            {
                recentAccelY.Dequeue();
                accelTimestamps.Dequeue();
            }

            // Anti-shake check: Detect frequent sign oscillations
            float currentSign = Mathf.Sign(downwardAccel);
            if (Mathf.Abs(downwardAccel) > 0.8f && currentSign != lastSign)
            {
                shakeReversals++;
                lastSign = currentSign;
            }

            // Reset shake reversals if time elapsed
            if (accelTimestamps.Count <= 2)
            {
                shakeReversals = 0;
            }

            if (shakeReversals > rules.maxShakeReversals)
            {
                StatusMessage = "¡Agitar no cuenta! Mantén el control firme y golpea recto.";
                isArmed = false;
                armHoldTime = 0f;
                return false;
            }

            // Arming phase detection (stable overhead / raised posture)
            bool postureArmed = acc.y > 0.4f || (Mathf.Abs(acc.z) > 0.7f && Mathf.Abs(downwardAccel) < 0.6f);
            if (postureArmed && !isArmed)
            {
                armHoldTime += deltaTime;
                StatusMessage = "Preparando mazo... (" + (int)(armHoldTime * 100) + "%)";
                if (armHoldTime >= rules.armingHoldSeconds)
                {
                    isArmed = true;
                    swingStartTime = Time.time;
                    StatusMessage = "¡MAZO LISTO! ¡GOLPEA CON FUERZA HACIA ABAJO!";
                }
            }
            else if (!postureArmed && !isArmed)
            {
                armHoldTime = Mathf.Max(0f, armHoldTime - deltaTime * 2f);
                StatusMessage = "Levanta el teléfono para armar el golpe.";
            }

            // Strike detection: Must be armed and deliver downward acceleration
            if (isArmed && downwardAccel >= rules.minStrikeAcceleration)
            {
                float swingDuration = Time.time - swingStartTime;

                // DEC-008: Calibrated normalized power (comfortable strike ~1.5G to 4.0G)
                power = Mathf.Clamp01((downwardAccel - rules.minStrikeAcceleration) / 3.0f + 0.4f);

                // Direction accuracy: penalize lateral transverse drift
                float driftRatio = transverseMag / Mathf.Max(0.1f, downwardAccel + transverseMag);
                directionAccuracy = Mathf.Clamp01(1.0f - (driftRatio / rules.maxTransverseTolerance));

                // Timing score: optimal swing duration around sweetSpotTimingWindow
                float timingError = Mathf.Abs(swingDuration - rules.sweetSpotTimingWindow);
                timing = Mathf.Clamp01(1.0f - (timingError / rules.sweetSpotTimingWindow));

                // Confidence: sensor quality + freshness
                confidence = sampleAge < 100 ? 1.0f : Mathf.Clamp01(1.0f - (sampleAge - 100f) / 250f);

                // Reset detector state
                isArmed = false;
                armHoldTime = 0f;
                shakeReversals = 0;
                StatusMessage = "¡GOLPE REGISTRADO!";
                return true;
            }

            return false;
        }

        /// <summary>
        /// Keyboard / accessibility fallback (DEC-009 & DEC-014 testing baseline).
        /// Spacebar: Hold to Arm, Release to Strike.
        /// S / Down Arrow: Instant test swing.
        /// </summary>
        public bool TryDetectKeyboardStrike(
            float deltaTime,
            out float power,
            out float directionAccuracy,
            out float timing,
            out float confidence)
        {
            power = 0f;
            directionAccuracy = 0f;
            timing = 0f;
            confidence = 0f;

            // Instant test strike with Down Arrow or S
            if (UnityEngine.Input.GetKeyDown(KeyCode.DownArrow) || UnityEngine.Input.GetKeyDown(KeyCode.S))
            {
                power = 0.88f;
                directionAccuracy = 0.95f;
                timing = 0.90f;
                confidence = 1.0f;
                isArmed = false;
                armHoldTime = 0f;
                StatusMessage = "¡Golpe con teclado!";
                return true;
            }

            // Spacebar hold-and-release mechanic
            if (UnityEngine.Input.GetKey(KeyCode.Space))
            {
                spaceHeld = true;
                spaceHoldTime += deltaTime;
                if (spaceHoldTime >= rules.armingHoldSeconds)
                {
                    isArmed = true;
                    StatusMessage = "¡Mazo cargado! Suelta ESPACIO para golpear.";
                }
                else
                {
                    StatusMessage = "Cargando mazo... Mantén ESPACIO";
                }
                return false;
            }

            if (spaceHeld && !UnityEngine.Input.GetKey(KeyCode.Space))
            {
                spaceHeld = false;
                if (isArmed)
                {
                    // Power corresponds to hold duration sweet spot (~0.6s)
                    float holdDiff = Mathf.Abs(spaceHoldTime - 0.6f);
                    power = Mathf.Clamp01(1.0f - holdDiff);
                    directionAccuracy = 1.0f; // Pure keyboard is straight
                    timing = Mathf.Clamp01(1.0f - (holdDiff * 0.8f));
                    confidence = 1.0f;

                    isArmed = false;
                    armHoldTime = 0f;
                    spaceHoldTime = 0f;
                    StatusMessage = "¡Golpe ejecutado!";
                    return true;
                }
                else
                {
                    StatusMessage = "Soltaste muy pronto. Mantén presionado hasta armar.";
                    isArmed = false;
                    armHoldTime = 0f;
                    spaceHoldTime = 0f;
                    return false;
                }
            }

            return false;
        }
    }
}
