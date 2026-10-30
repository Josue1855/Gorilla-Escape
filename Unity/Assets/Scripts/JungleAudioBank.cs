using UnityEngine;

/// <summary>
/// Banco estático de efectos de sonido y ambiente procedurales de la selva.
/// Sintetiza cada clip una sola vez por sesión en CPU (combinando armónicos,
/// modulación FM, formantes de madera y ruido filtrado) y lo reutiliza entre escenas.
/// </summary>
public static class JungleAudioBank
{
    public static bool IsPrewarmed { get; private set; }
    private static bool warming;
    public static System.Collections.IEnumerator Prewarm()
    {
        while (warming && !IsPrewarmed) yield return null;
        if (IsPrewarmed) yield break;
        warming = true;
        try
        {
            // Let the connection view paint before synthesizing the long ambience.
            yield return null;
            _ = JungleAmbience; yield return null;
            for (int i = 0; i < BirdCallCount; i++) { _ = BirdCall(i); yield return null; }
            _ = Pitch; yield return null;
            _ = BatHit; yield return null;
            _ = HomeRun; yield return null;
            _ = Strike; yield return null;
            _ = LeftDrum; yield return null;
            _ = RightDrum; yield return null;
            _ = MissDrum; yield return null;
            _ = CoconutSlice; yield return null;
            _ = CoconutHazard; yield return null;
            _ = WhackHit; yield return null;
            _ = WhackPop; yield return null;
            _ = LogWarning; yield return null;
            _ = LogFall; yield return null;
            _ = LogWin; yield return null;
            _ = UiClick; yield return null;
            _ = CountdownTick; yield return null;
            _ = CountdownGo; yield return null;
            _ = BowlingRoll; yield return null;
            _ = BowlingPins; yield return null;
            _ = BowlingStrike; yield return null;
            _ = BoxingPunch; yield return null;
            _ = BoxingBlock; yield return null;
            _ = BoxingBell; yield return null;
            IsPrewarmed = true;
        }
        finally { warming = false; }
    }

    private const int SampleRate = 22050;

    private static AudioClip pitchClip;
    private static AudioClip batHitClip;
    private static AudioClip homeRunClip;
    private static AudioClip strikeClip;
    private static AudioClip leftDrumClip;
    private static AudioClip rightDrumClip;
    private static AudioClip missDrumClip;
    private static AudioClip sliceClip;
    private static AudioClip hazardClip;
    private static AudioClip whackHitClip;
    private static AudioClip whackPopClip;
    private static AudioClip logWarningClip;
    private static AudioClip logFallClip;
    private static AudioClip logWinClip;

    // Nuevos efectos para Boliche, Boxeo, UI y Ambiente
    private static AudioClip uiClickClip;
    private static AudioClip countdownTickClip;
    private static AudioClip countdownGoClip;
    private static AudioClip bowlingRollClip;
    private static AudioClip bowlingPinsClip;
    private static AudioClip bowlingStrikeClip;
    private static AudioClip boxingPunchClip;
    private static AudioClip boxingBlockClip;
    private static AudioClip boxingBellClip;
    private static AudioClip jungleAmbienceClip;

    public const int BirdCallCount = 6;
    private static readonly AudioClip[] birdCalls = new AudioClip[BirdCallCount];
    public static AudioClip BirdCall(int variant)
    {
        if (variant < 0 || variant >= BirdCallCount) throw new System.ArgumentOutOfRangeException(nameof(variant));
        return birdCalls[variant] != null ? birdCalls[variant] : (birdCalls[variant] = CreateBirdPhrase(variant));
    }
    private static AudioSource sharedUiSource;

    public static AudioSource Attach2DSource(GameObject host, bool withAmbience = true)
    {
        AudioSource source = host.GetComponent<AudioSource>();
        if (source == null) source = host.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        sharedUiSource = source;

        if (withAmbience)
            EnsureAmbience(host);

        return source;
    }

    public static void EnsureAmbience(GameObject host)
    {
        if (host == null) return;
        Transform existing = host.transform.Find("Ambiente Procedural Selva");
        AudioSource ambSource;
        if (existing != null)
        {
            ambSource = existing.GetComponent<AudioSource>();
        }
        else
        {
            GameObject ambObj = new GameObject("Ambiente Procedural Selva");
            ambObj.transform.SetParent(host.transform, false);
            ambSource = ambObj.AddComponent<AudioSource>();
        }
        if (ambSource == null) return;
        ambSource.spatialBlend = 0f;
        ambSource.loop = true;
        ambSource.volume = 0.12f;
        ambSource.clip = JungleAmbience;
        if (!ambSource.isPlaying)
            ambSource.Play();
        if (ambSource.GetComponent<JungleBirdAmbience>() == null)
            ambSource.gameObject.AddComponent<JungleBirdAmbience>();
    }

    public static void PlayUiClick(float volume = 0.45f)
    {
        if (sharedUiSource != null && UiClick != null)
            sharedUiSource.PlayOneShot(UiClick, volume);
    }



    // --- Béisbol ---
    public static AudioClip Pitch => pitchClip != null ? pitchClip : (pitchClip = CreateWhooshTone("Pitch", 290f, 560f, .16f, .24f));
    public static AudioClip BatHit => batHitClip != null ? batHitClip : (batHitClip = CreateWoodCrackTone("Hit", 520f, .18f, .32f));
    public static AudioClip HomeRun => homeRunClip != null ? homeRunClip : (homeRunClip = CreateFanfareTone("HomeRun", 523.25f, .52f, .28f));
    public static AudioClip Strike => strikeClip != null ? strikeClip : (strikeClip = CreateHollowBuzzerTone("Strike", 240f, 115f, .26f, .24f));

    // --- Tambores ---
    public static AudioClip LeftDrum => leftDrumClip != null ? leftDrumClip : (leftDrumClip = CreatePercussionDrumTone("LeftDrum", 152f, 64f, .19f, .32f, false));
    public static AudioClip RightDrum => rightDrumClip != null ? rightDrumClip : (rightDrumClip = CreatePercussionDrumTone("RightDrum", 268f, 122f, .16f, .30f, true));
    public static AudioClip MissDrum => missDrumClip != null ? missDrumClip : (missDrumClip = CreateWoodTapTone("MissDrum", 135f, 82f, .11f, .16f));

    // --- Coconut Slice & Portero ---
    public static AudioClip CoconutSlice => sliceClip != null ? sliceClip : (sliceClip = CreateMacheteSliceTone("CoconutSliceTone", .14f, .30f));
    public static AudioClip CoconutHazard => hazardClip != null ? hazardClip : (hazardClip = CreateHazardImpactTone("CoconutHazardTone", 215f, 88f, .26f, .34f));

    // --- Whack-A-Mole ---
    public static AudioClip WhackHit => whackHitClip != null ? whackHitClip : (whackHitClip = CreateMalletBonkTone("WhackHit", 390f, 148f, .15f, .32f));
    public static AudioClip WhackPop => whackPopClip != null ? whackPopClip : (whackPopClip = CreateBubblePopTone("WhackPop", 250f, 560f, .09f, .22f));

    // --- Tronco ---
    public static AudioClip LogWarning => logWarningClip != null ? logWarningClip : (logWarningClip = CreateWoodTapTone("Tronco / aviso", 420f, 510f, .12f, .20f));
    public static AudioClip LogFall => logFallClip != null ? logFallClip : (logFallClip = CreateSplashFallTone("Tronco / caída", .52f, .26f));
    public static AudioClip LogWin => logWinClip != null ? logWinClip : (logWinClip = CreateFanfareTone("Tronco / meta", 440f, .46f, .24f));

    // --- Boliche, Boxeo, Menús y Ambiente ---
    public static AudioClip UiClick => uiClickClip != null ? uiClickClip : (uiClickClip = CreateWoodTapTone("UiClick", 680f, 920f, .055f, .22f));
    public static AudioClip CountdownTick => countdownTickClip != null ? countdownTickClip : (countdownTickClip = CreateWoodTapTone("CountdownTick", 520f, 540f, .085f, .24f));
    public static AudioClip CountdownGo => countdownGoClip != null ? countdownGoClip : (countdownGoClip = CreateWoodTapTone("CountdownGo", 784f, 1046.5f, .24f, .28f));
    public static AudioClip BowlingRoll => bowlingRollClip != null ? bowlingRollClip : (bowlingRollClip = CreateBowlingRollTone("BowlingRoll", .55f, .22f));
    public static AudioClip BowlingPins => bowlingPinsClip != null ? bowlingPinsClip : (bowlingPinsClip = CreateTotemPinsCrashTone("BowlingPins", false, .42f, .32f));
    public static AudioClip BowlingStrike => bowlingStrikeClip != null ? bowlingStrikeClip : (bowlingStrikeClip = CreateTotemPinsCrashTone("BowlingStrike", true, .62f, .36f));
    public static AudioClip BoxingPunch => boxingPunchClip != null ? boxingPunchClip : (boxingPunchClip = CreateLeatherPunchTone("BoxingPunch", 145f, 44f, .16f, .34f));
    public static AudioClip BoxingBlock => boxingBlockClip != null ? boxingBlockClip : (boxingBlockClip = CreateLeatherPunchTone("BoxingBlock", 210f, 92f, .10f, .22f));
    public static AudioClip BoxingBell => boxingBellClip != null ? boxingBellClip : (boxingBellClip = CreateRingBellTone("BoxingBell", 880f, .55f, .28f));
    public static AudioClip JungleAmbience => jungleAmbienceClip != null ? jungleAmbienceClip : (jungleAmbienceClip = CreateJungleAmbienceLoop("JungleAmbience", 6.0f, .18f));

    private static float NextNoise(ref uint seed)
    {
        seed = seed * 1664525u + 1013904223u;
        return ((seed >> 8) & 0xFFFF) / 32767.5f - 1f;
    }

    private static AudioClip BuildClip(string name, float[] samples)
    {
        AudioClip clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreateWhooshTone(string name, float fromHz, float toHz, float duration, float amplitude)
    {
        int count = Mathf.Max(64, Mathf.RoundToInt(SampleRate * duration));
        float[] samples = new float[count];
        float phase = 0f, lp = 0f;
        uint seed = 0x13579BDFu;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / count;
            float env = Mathf.Sin(Mathf.PI * t);
            float hz = Mathf.Lerp(fromHz, toHz, t * t);
            phase += 2f * Mathf.PI * hz / SampleRate;
            float n = NextNoise(ref seed);
            lp += (n - lp) * Mathf.Lerp(.08f, .28f, t);
            float air = (n - lp) * .55f;
            float tone = (Mathf.Sin(phase) * .65f + Mathf.Sin(phase * 1.5f) * .20f + air);
            samples[i] = Mathf.Clamp(tone * env * amplitude, -1f, 1f);
        }
        return BuildClip(name, samples);
    }

    private static AudioClip CreateWoodCrackTone(string name, float baseHz, float duration, float amplitude)
    {
        int count = Mathf.Max(64, Mathf.RoundToInt(SampleRate * duration));
        float[] samples = new float[count];
        float p1 = 0f, p2 = 0f, p3 = 0f, lp = 0f;
        uint seed = 0x2468ACE0u;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / count;
            float crackEnv = Mathf.Exp(-t * 28f);
            float woodEnv = Mathf.Exp(-t * 11f) * Mathf.Sin(Mathf.Min(1f, t * 24f) * Mathf.PI * .5f);
            float hz = Mathf.Lerp(baseHz * 1.18f, baseHz * .62f, t);
            p1 += 2f * Mathf.PI * hz / SampleRate;
            p2 += 2f * Mathf.PI * (hz * 1.86f) / SampleRate;
            p3 += 2f * Mathf.PI * (hz * 2.92f) / SampleRate;
            lp += (NextNoise(ref seed) - lp) * .35f;
            float sample = (Mathf.Sin(p1) * .55f + Mathf.Sin(p2) * .28f + Mathf.Sin(p3) * .17f) * woodEnv + lp * crackEnv * .75f;
            samples[i] = Mathf.Clamp(sample * amplitude, -1f, 1f);
        }
        return BuildClip(name, samples);
    }

    private static AudioClip CreateFanfareTone(string name, float rootHz, float duration, float amplitude)
    {
        int count = Mathf.Max(64, Mathf.RoundToInt(SampleRate * duration));
        float[] samples = new float[count];
        float[] multipliers = { 1f, 1.25f, 1.5f, 2f };
        float phase = 0f, phase2 = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / count;
            int step = Mathf.Min(3, (int)(t * 4.2f));
            float localT = (t * 4.2f) - step;
            float noteEnv = Mathf.Exp(-localT * 3.2f) * Mathf.Sin(Mathf.Min(1f, localT * 12f) * Mathf.PI * .5f);
            if (step == 3) noteEnv = Mathf.Exp(- Mathf.Max(0f, t - .7f) * 4.5f);
            float hz = rootHz * multipliers[step];
            phase += 2f * Mathf.PI * hz / SampleRate;
            phase2 += 2f * Mathf.PI * (hz * 2f) / SampleRate;
            float marimba = (Mathf.Sin(phase) * .72f + Mathf.Sin(phase2) * .22f + Mathf.Sin(phase * 3f) * .06f) * noteEnv;
            samples[i] = Mathf.Clamp(marimba * amplitude, -1f, 1f);
        }
        return BuildClip(name, samples);
    }

    private static AudioClip CreateHollowBuzzerTone(string name, float fromHz, float toHz, float duration, float amplitude)
    {
        int count = Mathf.Max(64, Mathf.RoundToInt(SampleRate * duration));
        float[] samples = new float[count];
        float p1 = 0f, p2 = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / count;
            float hz = Mathf.Lerp(fromHz, toHz, t);
            p1 += 2f * Mathf.PI * hz / SampleRate;
            p2 += 2f * Mathf.PI * (hz * 1.48f) / SampleRate;
            float env = Mathf.Sin(Mathf.PI * t) * Mathf.Exp(-t * 2.2f);
            float wave = (Mathf.Sin(p1) * .68f + Mathf.Sin(p2) * .32f);
            samples[i] = Mathf.Clamp(wave * env * amplitude, -1f, 1f);
        }
        return BuildClip(name, samples);
    }

    private static AudioClip CreatePercussionDrumTone(string name, float fromHz, float toHz, float duration, float amplitude, bool brightRim)
    {
        int count = Mathf.Max(64, Mathf.RoundToInt(SampleRate * duration));
        float[] samples = new float[count];
        float p1 = 0f, p2 = 0f, lp = 0f;
        uint seed = brightRim ? 0x9E3779B9u : 0x85EBCA6Bu;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / count;
            float bodyEnv = Mathf.Exp(-t * 9.5f) * Mathf.Sin(Mathf.PI * Mathf.Min(1f, t * 18f) * .5f);
            float slapEnv = Mathf.Exp(-t * 36f);
            float hz = Mathf.Lerp(fromHz, toHz, Mathf.Sqrt(t));
            p1 += 2f * Mathf.PI * hz / SampleRate;
            p2 += 2f * Mathf.PI * (hz * (brightRim ? 2.14f : 1.58f)) / SampleRate;
            lp += (NextNoise(ref seed) - lp) * (brightRim ? .42f : .22f);
            float drum = (Mathf.Sin(p1) * .72f + Mathf.Sin(p2) * .28f) * bodyEnv + lp * slapEnv * .48f;
            samples[i] = Mathf.Clamp(drum * amplitude, -1f, 1f);
        }
        return BuildClip(name, samples);
    }

    private static AudioClip CreateWoodTapTone(string name, float fromHz, float toHz, float duration, float amplitude)
    {
        int count = Mathf.Max(64, Mathf.RoundToInt(SampleRate * duration));
        float[] samples = new float[count];
        float p1 = 0f, p2 = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / count;
            float env = Mathf.Exp(-t * 14f) * Mathf.Sin(Mathf.Min(1f, t * 22f) * Mathf.PI * .5f);
            float hz = Mathf.Lerp(fromHz, toHz, t);
            p1 += 2f * Mathf.PI * hz / SampleRate;
            p2 += 2f * Mathf.PI * (hz * 2.75f) / SampleRate;
            float sample = (Mathf.Sin(p1) * .78f + Mathf.Sin(p2) * .22f * Mathf.Exp(-t * 25f)) * env;
            samples[i] = Mathf.Clamp(sample * amplitude, -1f, 1f);
        }
        return BuildClip(name, samples);
    }

    private static AudioClip CreateMacheteSliceTone(string name, float duration, float amplitude)
    {
        int count = Mathf.Max(64, Mathf.RoundToInt(SampleRate * duration));
        float[] samples = new float[count];
        float p1 = 0f, p2 = 0f, lp = 0f;
        uint seed = 0xCAFEBABEu;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / count;
            float swooshEnv = Mathf.Sin(Mathf.PI * t) * Mathf.Exp(-t * 4f);
            float crackEnv = Mathf.Exp(-Mathf.Abs(t - .28f) * 32f);
            float hz = Mathf.Lerp(480f, 940f, t);
            p1 += 2f * Mathf.PI * hz / SampleRate;
            p2 += 2f * Mathf.PI * 360f / SampleRate;
            float n = NextNoise(ref seed);
            lp += (n - lp) * .26f;
            float bladeAir = (n - lp) * swooshEnv * .75f;
            float shellCrack = (Mathf.Sin(p1) * .55f + Mathf.Sin(p2) * .45f) * (swooshEnv * .55f + crackEnv * .65f);
            samples[i] = Mathf.Clamp((bladeAir + shellCrack) * amplitude, -1f, 1f);
        }
        return BuildClip(name, samples);
    }

    private static AudioClip CreateHazardImpactTone(string name, float startHz, float endHz, float duration, float amplitude)
    {
        int count = Mathf.Max(64, Mathf.RoundToInt(SampleRate * duration));
        float[] samples = new float[count];
        float p1 = 0f, p2 = 0f, lp = 0f;
        uint seed = 0xDEADBEEFu;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)(count - 1);
            float hz = Mathf.Lerp(startHz, endHz, t);
            p1 += 2f * Mathf.PI * hz / SampleRate;
            p2 += 2f * Mathf.PI * (hz * 1.37f) / SampleRate;
            lp += (NextNoise(ref seed) - lp) * .28f;
            float env = Mathf.Sin(Mathf.Min(1f, t * 9f) * Mathf.PI * .5f) * (1f - t);
            float wave = (Mathf.Sin(p1) * .55f + Mathf.Sin(p2) * .25f + lp * .35f) * env;
            samples[i] = Mathf.Clamp(wave * amplitude, -1f, 1f);
        }
        return BuildClip(name, samples);
    }

    private static AudioClip CreateMalletBonkTone(string name, float fromHz, float toHz, float duration, float amplitude)
    {
        int count = Mathf.Max(64, Mathf.RoundToInt(SampleRate * duration));
        float[] samples = new float[count];
        float p1 = 0f, p2 = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)(count - 1);
            float hz = Mathf.Lerp(fromHz, toHz, Mathf.Pow(t, .6f)) + Mathf.Sin(t * 45f) * 18f * (1f - t);
            p1 += 2f * Mathf.PI * hz / SampleRate;
            p2 += 2f * Mathf.PI * (hz * 2.4f) / SampleRate;
            float env = Mathf.Exp(-t * 9.5f) * Mathf.Sin(Mathf.Min(1f, t * 20f) * Mathf.PI * .5f);
            float sample = (Mathf.Sin(p1) * .72f + Mathf.Sin(p2) * .28f) * env;
            samples[i] = Mathf.Clamp(sample * amplitude, -1f, 1f);
        }
        return BuildClip(name, samples);
    }

    private static AudioClip CreateBubblePopTone(string name, float fromHz, float toHz, float duration, float amplitude)
    {
        int count = Mathf.Max(64, Mathf.RoundToInt(SampleRate * duration));
        float[] samples = new float[count];
        float phase = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = i / (float)(count - 1);
            float hz = Mathf.Lerp(fromHz, toHz, t * t);
            phase += 2f * Mathf.PI * hz / SampleRate;
            float env = Mathf.Sin(Mathf.PI * t) * (1f - t * .35f);
            samples[i] = Mathf.Sin(phase) * env * amplitude;
        }
        return BuildClip(name, samples);
    }

    private static AudioClip CreateSplashFallTone(string name, float duration, float amplitude)
    {
        int count = Mathf.Max(64, Mathf.RoundToInt(SampleRate * duration));
        float[] samples = new float[count];
        float phase = 0f, lp = 0f;
        uint seed = 0x5A5A5A5Au;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / count;
            float hz = Mathf.Lerp(260f, 68f, t);
            phase += 2f * Mathf.PI * hz / SampleRate;
            lp += (NextNoise(ref seed) - lp) * Mathf.Lerp(.35f, .08f, t);
            float splashEnv = t > .22f ? Mathf.Exp(-(t - .22f) * 6.5f) : 0f;
            float toneEnv = Mathf.Exp(-t * 4.2f);
            float sample = Mathf.Sin(phase) * toneEnv * .55f + lp * splashEnv * .75f;
            samples[i] = Mathf.Clamp(sample * amplitude, -1f, 1f);
        }
        return BuildClip(name, samples);
    }

    private static AudioClip CreateBowlingRollTone(string name, float duration, float amplitude)
    {
        int count = Mathf.Max(64, Mathf.RoundToInt(SampleRate * duration));
        float[] samples = new float[count];
        float phase = 0f, lp = 0f;
        uint seed = 0x11223344u;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / count;
            float env = Mathf.Sin(Mathf.PI * t) * (1f - t * .4f);
            float rumbleMod = .75f + .25f * Mathf.Sin(t * Mathf.PI * 16f);
            phase += 2f * Mathf.PI * Mathf.Lerp(115f, 88f, t) / SampleRate;
            lp += (NextNoise(ref seed) - lp) * .12f;
            float sample = (Mathf.Sin(phase) * .5f + lp * .7f) * rumbleMod * env;
            samples[i] = Mathf.Clamp(sample * amplitude, -1f, 1f);
        }
        return BuildClip(name, samples);
    }

    private static AudioClip CreateTotemPinsCrashTone(string name, bool isStrike, float duration, float amplitude)
    {
        int count = Mathf.Max(64, Mathf.RoundToInt(SampleRate * duration));
        float[] samples = new float[count];
        float[] hitOffsets = isStrike ? new[] { 0f, .035f, .075f, .12f, .17f, .23f } : new[] { 0f, .045f, .10f, .16f };
        float[] hitFreqs = { 410f, 520f, 360f, 610f, 465f, 580f };
        uint seed = 0x778899AAu;
        float lp = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / SampleRate;
            float sum = 0f;
            for (int h = 0; h < hitOffsets.Length; h++)
            {
                float dt = t - hitOffsets[h];
                if (dt < 0f || dt > .22f) continue;
                float env = Mathf.Exp(-dt * 22f);
                float f = hitFreqs[h];
                sum += (Mathf.Sin(2f * Mathf.PI * f * dt) * .6f + Mathf.Sin(2f * Mathf.PI * f * 2.3f * dt) * .4f) * env;
            }
            if (isStrike && t > .18f)
            {
                float ft = t - .18f;
                float chordEnv = Mathf.Exp(-ft * 5.5f);
                sum += (Mathf.Sin(2f * Mathf.PI * 523.25f * ft) + Mathf.Sin(2f * Mathf.PI * 659.25f * ft) + Mathf.Sin(2f * Mathf.PI * 783.99f * ft)) * .22f * chordEnv;
            }
            lp += (NextNoise(ref seed) - lp) * .28f;
            float noiseEnv = Mathf.Exp(-t * 14f);
            samples[i] = Mathf.Clamp((sum * .65f + lp * noiseEnv * .45f) * amplitude, -1f, 1f);
        }
        return BuildClip(name, samples);
    }

    private static AudioClip CreateLeatherPunchTone(string name, float fromHz, float toHz, float duration, float amplitude)
    {
        int count = Mathf.Max(64, Mathf.RoundToInt(SampleRate * duration));
        float[] samples = new float[count];
        float phase = 0f, lp = 0f;
        uint seed = 0x31415926u;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / count;
            float hz = Mathf.Lerp(fromHz, toHz, Mathf.Sqrt(t));
            phase += 2f * Mathf.PI * hz / SampleRate;
            lp += (NextNoise(ref seed) - lp) * .24f;
            float bodyEnv = Mathf.Exp(-t * 12f);
            float snapEnv = Mathf.Exp(-t * 38f);
            float sample = Mathf.Sin(phase) * bodyEnv * .75f + lp * snapEnv * .65f;
            samples[i] = Mathf.Clamp(sample * amplitude, -1f, 1f);
        }
        return BuildClip(name, samples);
    }

    private static AudioClip CreateRingBellTone(string name, float baseHz, float duration, float amplitude)
    {
        int count = Mathf.Max(64, Mathf.RoundToInt(SampleRate * duration));
        float[] samples = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / SampleRate;
            float env = Mathf.Exp(-t * 6.5f);
            float bell = Mathf.Sin(2f * Mathf.PI * baseHz * t) * .50f
                       + Mathf.Sin(2f * Mathf.PI * (baseHz * 1.51f) * t) * .30f
                       + Mathf.Sin(2f * Mathf.PI * (baseHz * 2.42f) * t) * .20f * Mathf.Exp(-t * 14f);
            samples[i] = Mathf.Clamp(bell * env * amplitude, -1f, 1f);
        }
        return BuildClip(name, samples);
    }

    private static AudioClip CreateJungleAmbienceLoop(string name, float duration, float amplitude)
    {
        int count = Mathf.RoundToInt(SampleRate * duration);
        float[] samples = new float[count];
        uint seed = 0xABCDEF01u;
        float lp1 = 0f, lp2 = 0f;
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / duration / SampleRate; // 0..1 loop normalized
            float sec = (float)i / SampleRate;

            // Brisa tropical suave (ruido rosa filtrado modulado con ciclo cerrado 2*PI*t)
            float n = NextNoise(ref seed);
            float windMod = .045f + .025f * Mathf.Sin(2f * Mathf.PI * t * 2f);
            lp1 += (n - lp1) * windMod;
            lp2 += (lp1 - lp2) * .04f;
            float breeze = lp2 * .42f;

            // Fade en extremos para loop perfecto sin clic
            float edge = Mathf.Min(1f, Mathf.Min(sec / .12f, (duration - sec) / .12f));
            samples[i] = Mathf.Clamp(breeze * amplitude * edge, -1f, 1f);
        }
        return BuildClip(name, samples);
    }

    private static AudioClip CreateBirdPhrase(int variant)
    {
        // Each phrase has its own contour and spacing. Scheduling and pitch vary
        // independently, so birds are never baked into the repeating wind bed.
        int chirps = 2 + variant % 3;
        float chirpLength = .11f + variant * .015f;
        float spacing = .18f + (variant % 3) * .065f;
        float duration = spacing * (chirps - 1) + chirpLength + .08f;
        float[] samples = new float[Mathf.CeilToInt(SampleRate * duration)];
        for (int call = 0; call < chirps; call++)
        {
            float phase = 0;
            float from = 1380f + variant * 155f + call * 75f;
            float to = from + (variant % 2 == 0 ? 470f : -330f);
            int start = Mathf.RoundToInt(call * spacing * SampleRate);
            int length = Mathf.RoundToInt(chirpLength * SampleRate);
            for (int i = 0; i < length; i++)
            {
                float u = (float)i / length;
                float hz = Mathf.Lerp(from, to, u) + Mathf.Sin(u * Mathf.PI * (4 + variant)) * 65f;
                phase += 2 * Mathf.PI * hz / SampleRate;
                float env = Mathf.Sin(Mathf.PI * u);
                samples[start + i] = (Mathf.Sin(phase) + .10f * Mathf.Sin(phase * 2)) * env * env * .10f;
            }
        }
        return BuildClip("Jungle bird " + variant, samples);
    }
}
