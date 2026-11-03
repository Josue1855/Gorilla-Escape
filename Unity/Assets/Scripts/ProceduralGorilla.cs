using UnityEngine;

/// <summary>
/// Gorila low-poly ORIGINAL construido con primitivas de Unity (sin dependencias externas).
/// Proporciona un ciclo de carrera procedimental para el splash cinematográfico y animaciones idle.
/// </summary>
public sealed class ProceduralGorilla : MonoBehaviour
{
    private Transform torso, head, muzzle;
    private Transform shoulderL, shoulderR, elbowL, elbowR;
    private Transform hipL, hipR, kneeL, kneeR;
    private Transform bodyRoot;
    private float baseY;

    private static readonly Color Fur = new Color(0.22f, 0.20f, 0.18f, 1f);
    private static readonly Color FurLight = new Color(0.30f, 0.27f, 0.24f, 1f);
    private static readonly Color FaceColor = new Color(0.13f, 0.12f, 0.11f, 1f);
    private static readonly Color Chest = new Color(0.16f, 0.14f, 0.13f, 1f);

    public static ProceduralGorilla Build(Transform parent, float height)
    {
        var root = new GameObject("Gorila Original (low-poly)");
        root.transform.SetParent(parent, false);
        var g = root.AddComponent<ProceduralGorilla>();
        g.baseY = height * 0.5f;

        g.bodyRoot = new GameObject("Cuerpo").transform;
        g.bodyRoot.SetParent(root.transform, false);
        g.bodyRoot.localPosition = new Vector3(0f, g.baseY, 0f);

        // Torso (levemente inclinado hacia delante, postura natural de gorila)
        g.torso = JungleStageBuilder.CreateLocalPrimitive(PrimitiveType.Capsule, "Torso",
            new Vector3(0f, height * 0.58f, 0f), new Vector3(height * 0.42f, height * 0.34f, height * 0.30f), Fur, g.bodyRoot).transform;
        g.torso.localRotation = Quaternion.Euler(8f, 0f, 0f);

        // Cabeza
        g.head = new GameObject("Cabeza").transform;
        g.head.SetParent(g.torso, false);
        g.head.localPosition = new Vector3(0f, height * 0.40f, height * 0.02f);
        var skull = JungleStageBuilder.CreateLocalPrimitive(PrimitiveType.Sphere, "Cráneo",
            Vector3.zero, Vector3.one * (height * 0.28f), Fur, g.head).transform;
        skull.localPosition = Vector3.zero;
        g.muzzle = JungleStageBuilder.CreateLocalPrimitive(PrimitiveType.Sphere, "Hocico",
            new Vector3(0f, -height * 0.04f, height * 0.13f), Vector3.one * (height * 0.17f), FaceColor, g.head).transform;
        var brow = JungleStageBuilder.CreateLocalPrimitive(PrimitiveType.Sphere, "Cejas",
            new Vector3(0f, height * 0.10f, height * 0.06f), new Vector3(height * 0.24f, height * 0.10f, height * 0.12f), FurLight, g.head).transform;
        brow.localPosition = brow.localPosition;
        
        // Orejas
        JungleStageBuilder.CreateLocalPrimitive(PrimitiveType.Sphere, "OrejaL",
            new Vector3(-height * 0.16f, height * 0.03f, 0f), Vector3.one * (height * 0.08f), Fur, g.head);
        JungleStageBuilder.CreateLocalPrimitive(PrimitiveType.Sphere, "OrejaR",
            new Vector3(height * 0.16f, height * 0.03f, 0f), Vector3.one * (height * 0.08f), Fur, g.head);

        // Pecho
        JungleStageBuilder.CreateLocalPrimitive(PrimitiveType.Sphere, "Pecho",
            new Vector3(0f, -height * 0.06f, height * 0.16f), new Vector3(height * 0.30f, height * 0.26f, height * 0.14f), Chest, g.torso);

        // Hombros
        g.shoulderL = Pivot("HombroL", g.torso, new Vector3(-height * 0.22f, height * 0.12f, 0f));
        g.shoulderR = Pivot("HombroR", g.torso, new Vector3(height * 0.22f, height * 0.12f, 0f));

        // Brazos
        g.elbowL = Arm("BrazoL", g.shoulderL, height, Fur, -1f);
        g.elbowR = Arm("BrazoR", g.shoulderR, height, Fur, 1f);

        // Caderas
        g.hipL = Pivot("CaderaL", g.bodyRoot, new Vector3(-height * 0.11f, height * 0.12f, 0f));
        g.hipR = Pivot("CaderaR", g.bodyRoot, new Vector3(height * 0.11f, height * 0.12f, 0f));

        // Piernas
        g.kneeL = Leg("PiernaL", g.hipL, height, Fur, -1f);
        g.kneeR = Leg("PiernaR", g.hipR, height, Fur, 1f);

        return g;
    }

    private static Transform Pivot(string name, Transform parent, Vector3 localPos)
    {
        var p = new GameObject(name).transform;
        p.SetParent(parent, false);
        p.localPosition = localPos;
        return p;
    }

    private static Transform Arm(string name, Transform pivot, float h, Color c, float side)
    {
        var upper = JungleStageBuilder.CreateLocalPrimitive(PrimitiveType.Capsule, name,
            new Vector3(side * h * 0.16f, -h * 0.24f, 0f), new Vector3(h * 0.12f, h * 0.24f, h * 0.12f), c, pivot).transform;
        upper.localRotation = Quaternion.Euler(0f, 0f, side * -8f);
        var elbow = Pivot("Codo" + (side > 0 ? "R" : "L"), pivot, new Vector3(side * h * 0.16f, -h * 0.44f, 0f));
        var fore = JungleStageBuilder.CreateLocalPrimitive(PrimitiveType.Capsule, "Antebrazo",
            new Vector3(0f, -h * 0.16f, 0f), new Vector3(h * 0.10f, h * 0.17f, h * 0.10f), c, elbow).transform;
        fore.localRotation = Quaternion.Euler(0f, 0f, side * 4f);
        JungleStageBuilder.CreateLocalPrimitive(PrimitiveType.Sphere, "Mano",
            new Vector3(0f, -h * 0.30f, 0f), Vector3.one * (h * 0.10f), c, elbow);
        return elbow;
    }

    private static Transform Leg(string name, Transform pivot, float h, Color c, float side)
    {
        var upper = JungleStageBuilder.CreateLocalPrimitive(PrimitiveType.Capsule, name,
            new Vector3(side * h * 0.06f, -h * 0.14f, 0f), new Vector3(h * 0.15f, h * 0.15f, h * 0.15f), c, pivot).transform;
        var knee = Pivot("Rodilla" + (side > 0 ? "R" : "L"), pivot, new Vector3(side * h * 0.06f, -h * 0.30f, 0f));
        JungleStageBuilder.CreateLocalPrimitive(PrimitiveType.Capsule, "Espinilla",
            new Vector3(0f, -h * 0.13f, 0f), new Vector3(h * 0.12f, h * 0.14f, h * 0.12f), c, knee);
        JungleStageBuilder.CreateLocalPrimitive(PrimitiveType.Sphere, "Pie",
            new Vector3(0f, -h * 0.25f, h * 0.05f), new Vector3(h * 0.13f, h * 0.07f, h * 0.18f), FurLight, knee);
        return knee;
    }

    public void Face(Vector3 worldDir)
    {
        worldDir.y = 0f;
        if (worldDir.sqrMagnitude > 0.0001f)
            bodyRoot.rotation = Quaternion.LookRotation(worldDir.normalized);
    }

    /// <summary>Anima el ciclo de carrera procedimental. phase avanza con el tiempo.</summary>
    public void SetRunCycle(float phase, float speed)
    {
        float swing = Mathf.Sin(phase) * 0.6f;          // balanceo de brazos
        float swing2 = Mathf.Sin(phase + Mathf.PI) * 0.6f;
        float legSwing = Mathf.Sin(phase) * 0.55f;       // zancada de piernas
        float legSwing2 = Mathf.Sin(phase + Mathf.PI) * 0.55f;
        float bob = Mathf.Abs(Mathf.Cos(phase)) * 0.08f; // rebote vertical
        float lean = 0.12f;                              // inclinación al correr

        bodyRoot.localPosition = new Vector3(0f, baseY + bob, 0f);
        torso.localRotation = Quaternion.Euler(lean * Mathf.Rad2Deg, 0f, 0f);

        shoulderL.localRotation = Quaternion.Euler(swing * 48f, 0f, 15f);
        shoulderR.localRotation = Quaternion.Euler(swing2 * 48f, 0f, -15f);
        if (elbowL != null) elbowL.localRotation = Quaternion.Euler(-25f + Mathf.Max(0f, swing) * -35f, 0f, 0f);
        if (elbowR != null) elbowR.localRotation = Quaternion.Euler(-25f + Mathf.Max(0f, swing2) * -35f, 0f, 0f);

        hipL.localRotation = Quaternion.Euler(legSwing * 38f, 0f, 0f);
        hipR.localRotation = Quaternion.Euler(legSwing2 * 38f, 0f, 0f);
        if (kneeL != null) kneeL.localRotation = Quaternion.Euler(Mathf.Max(0f, -legSwing) * 50f, 0f, 0f);
        if (kneeR != null) kneeR.localRotation = Quaternion.Euler(Mathf.Max(0f, -legSwing2) * 50f, 0f, 0f);
    }

    public void SetIdle()
    {
        bodyRoot.localPosition = new Vector3(0f, baseY, 0f);
        torso.localRotation = Quaternion.Euler(6f, 0f, 0f);
        shoulderL.localRotation = Quaternion.Euler(15f, 0f, 18f);
        shoulderR.localRotation = Quaternion.Euler(15f, 0f, -18f);
        if (elbowL != null) elbowL.localRotation = Quaternion.Euler(-30f, 0f, 0f);
        if (elbowR != null) elbowR.localRotation = Quaternion.Euler(-30f, 0f, 0f);
        hipL.localRotation = Quaternion.Euler(-5f, 0f, 0f);
        hipR.localRotation = Quaternion.Euler(-5f, 0f, 0f);
        if (kneeL != null) kneeL.localRotation = Quaternion.Euler(10f, 0f, 0f);
        if (kneeR != null) kneeR.localRotation = Quaternion.Euler(10f, 0f, 0f);
    }
}
