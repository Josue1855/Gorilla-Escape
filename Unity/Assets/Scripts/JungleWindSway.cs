using UnityEngine;

/// <summary>
/// Mecido procedural suave por viento para palmeras, arbustos y vegetación 3D.
/// </summary>
public sealed class JungleWindSway : MonoBehaviour
{
    private Quaternion baseLocalRotation;
    private bool initialized;
    private float maxAngle = 3.0f;
    private float speed = 1.6f;
    private float phaseOffset;

    public void Configure(float angle, float swaySpeed)
    {
        if (!initialized)
        {
            baseLocalRotation = transform.localRotation;
            Vector3 p = transform.position;
            phaseOffset = p.x * .73f + p.z * .51f;
            initialized = true;
        }
        maxAngle = angle;
        speed = swaySpeed;
    }

    private void Update()
    {
        if (!initialized) Configure(maxAngle, speed);
        float t = Time.time * speed + phaseOffset;
        float pitch = (Mathf.Sin(t) + .35f * Mathf.Sin(t * 2.1f)) * maxAngle;
        float roll = Mathf.Cos(t * .82f + 1.3f) * (maxAngle * .65f);
        transform.localRotation = baseLocalRotation * Quaternion.Euler(pitch, 0f, roll);
    }
}
