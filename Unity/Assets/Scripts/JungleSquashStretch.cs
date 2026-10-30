using UnityEngine;

/// <summary>
/// Rebote elástico de impacto (Squash & Stretch) con resorte subamortiguado.
/// </summary>
public sealed class JungleSquashStretch : MonoBehaviour
{
    private Vector3 baseScale;
    private bool initialized;
    private float triggeredAt = -10f;
    private float targetSquashY = .74f;
    private float targetStretchXZ = 1.20f;
    private float freq = 24f;
    private float damp = 9f;

    private void EnsureBase()
    {
        if (initialized) return;
        baseScale = transform.localScale;
        initialized = true;
    }

    public static void Apply(GameObject target, float squashY = .74f, float stretchXZ = 1.20f, float frequency = 24f, float damping = 9f)
    {
        if (target == null) return;
        if (!target.TryGetComponent<JungleSquashStretch>(out var comp))
            comp = target.AddComponent<JungleSquashStretch>();
        if (frequency > 0f && frequency < 2f)
        {
            float duration = frequency;
            frequency = Mathf.PI / Mathf.Max(0.05f, duration);
            damping = 6f / Mathf.Max(0.05f, duration);
        }
        comp.Trigger(squashY, stretchXZ, frequency, damping);
    }

    public void Trigger(float squashY, float stretchXZ, float frequency, float damping)
    {
        EnsureBase();
        targetSquashY = squashY;
        targetStretchXZ = stretchXZ;
        freq = frequency;
        damp = damping;
        triggeredAt = Time.time;
        enabled = true;
    }

    private void Update()
    {
        float dt = Time.time - triggeredAt;
        if (dt > .55f)
        {
            transform.localScale = baseScale;
            enabled = false;
            return;
        }
        float wave = Mathf.Cos(dt * freq) * Mathf.Exp(-dt * damp);
        float sy = Mathf.LerpUnclamped(1f, targetSquashY, wave);
        float sxz = Mathf.LerpUnclamped(1f, targetStretchXZ, wave);
        transform.localScale = new Vector3(baseScale.x * sxz, baseScale.y * sy, baseScale.z * sxz);
    }
}
