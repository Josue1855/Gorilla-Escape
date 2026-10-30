using UnityEngine;

// Irregular, sparse wildlife independent of the looping wind and gameplay RNG.
public sealed class JungleBirdAmbience : MonoBehaviour
{
    readonly System.Random random = new System.Random();
    AudioSource source;
    float nextCall;
    int previous = -1;
    void Awake()
    {
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0;
    }
    void OnEnable() => nextCall = Time.unscaledTime + Range(8, 18);
    void Update()
    {
        if (Time.unscaledTime < nextCall || source.isPlaying) return;
        int variant = random.Next(JungleAudioBank.BirdCallCount - 1);
        if (variant >= previous && previous >= 0) variant++;
        previous = variant;
        source.clip = JungleAudioBank.BirdCall(variant);
        source.pitch = Range(.88f, 1.12f);
        source.panStereo = Range(-.65f, .65f);
        source.volume = Range(.025f, .045f);
        source.Play();
        nextCall = Time.unscaledTime + source.clip.length / source.pitch
            + (random.NextDouble() < .25 ? Range(35, 55) : Range(12, 28));
    }
    void OnDisable() { if (source != null) source.Stop(); }
    void OnDestroy() { if (source != null) Destroy(source); }
    float Range(float low, float high) => Mathf.Lerp(low, high, (float)random.NextDouble());
}
