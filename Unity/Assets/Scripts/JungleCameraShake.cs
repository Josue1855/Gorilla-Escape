using UnityEngine;

/// <summary>
/// Micro-Shake amortiguado de cámara para impactos fuertes sin alterar la posición base.
/// </summary>
public sealed class JungleCameraShake : MonoBehaviour
{
    private Vector3 lastOffset;
    private float shakeUntil;
    private float shakeDuration = .18f;
    private float shakeIntensity = .14f;

    public void Trigger(float intensity, float duration)
    {
        if (lastOffset != Vector3.zero)
        {
            transform.localPosition -= lastOffset;
            lastOffset = Vector3.zero;
        }
        shakeIntensity = Mathf.Max(intensity, shakeIntensity * .5f);
        shakeDuration = Mathf.Max(.05f, duration);
        shakeUntil = Time.time + shakeDuration;
        enabled = true;
    }

    private void LateUpdate()
    {
        if (lastOffset != Vector3.zero)
        {
            transform.localPosition -= lastOffset;
            lastOffset = Vector3.zero;
        }
        float remaining = shakeUntil - Time.time;
        if (remaining <= 0f)
        {
            shakeIntensity = 0f;
            enabled = false;
            return;
        }
        float decay = remaining / shakeDuration;
        float t = Time.time * 58f;
        lastOffset = new Vector3(
            Mathf.Sin(t * 1.13f) * shakeIntensity * decay,
            Mathf.Cos(t * 1.47f) * shakeIntensity * decay * .85f,
            0f);
        transform.localPosition += lastOffset;
    }

    private void OnDisable()
    {
        if (lastOffset != Vector3.zero)
        {
            transform.localPosition -= lastOffset;
            lastOffset = Vector3.zero;
        }
    }
}
