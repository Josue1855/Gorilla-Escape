using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Combina un fondo 2D de acuarela con detalles 3D animados que no participan
/// en las físicas del boliche. La pista, el coco y los pinos permanecen intactos.
/// </summary>
public sealed class BowlingHybridEnvironment : MonoBehaviour
{
    private sealed class DriftItem
    {
        public Transform transform;
        public Vector3 origin;
        public float speed;
        public float phase;
    }

    private readonly List<DriftItem> clouds = new List<DriftItem>();
    private readonly List<DriftItem> birds = new List<DriftItem>();
    private readonly List<DriftItem> waterStrips = new List<DriftItem>();
    private GameObject root;
    private Transform backgroundPlate;
    private Camera bowlingCamera;
    private Transform pool;
    private readonly List<Transform> audiencePanels = new List<Transform>();
    private Renderer foregroundGorillaRenderer;
    private Renderer foregroundBlendRenderer;
    private Texture2D foregroundNeutral;
    private Texture2D[] foregroundAngryFrames;
    private Texture2D[] foregroundTauntFrames;
    private Texture2D[] foregroundReactionFrames;
    private float foregroundReactionUntil;
    private float foregroundReactionStarted;
    private float foregroundReactionFrameRate = 5f;
    private int foregroundReactionFrame = -1;
    private float lastBackgroundAspect = -1f;
    private float lastBackgroundFov = -1f;

    private JungleSceneBindings bindings;
    public void Bind(JungleSceneBindings value) => bindings = value;

    private void Start()
    {
        Build();
    }

    public void SetVisible(bool visible)
    {
        if (root != null) root.SetActive(visible);
    }

    public void ShowThrowReaction(bool playerDidWell)
    {
        if (foregroundGorillaRenderer == null) return;
        foregroundReactionFrames = playerDidWell ? foregroundAngryFrames : foregroundTauntFrames;
        foregroundReactionFrameRate = playerDidWell ? 5f : 12f;
        foregroundReactionStarted = Time.time;
        foregroundReactionUntil = Time.time + foregroundReactionFrames.Length / foregroundReactionFrameRate;
        foregroundReactionFrame = -1;
        SetForegroundReactionFrame(0);
    }

    private void SetForegroundReactionFrame(int frame)
    {
        if (foregroundGorillaRenderer == null || foregroundReactionFrames == null || frame == foregroundReactionFrame) return;
        frame = Mathf.Clamp(frame, 0, foregroundReactionFrames.Length - 1);
        foregroundGorillaRenderer.sharedMaterial.mainTexture = foregroundReactionFrames[frame];
        foregroundGorillaRenderer.sharedMaterial.color = Color.white;
        if (foregroundBlendRenderer != null)
            foregroundBlendRenderer.sharedMaterial.color = new Color(1f, 1f, 1f, 0f);
        foregroundReactionFrame = frame;
    }

    private void BlendForegroundReaction(float framePosition)
    {
        if (foregroundGorillaRenderer == null || foregroundBlendRenderer == null || foregroundReactionFrames == null) return;
        int current = Mathf.Clamp(Mathf.FloorToInt(framePosition), 0, foregroundReactionFrames.Length - 1);
        int next = Mathf.Min(current + 1, foregroundReactionFrames.Length - 1);
        float blend = Mathf.SmoothStep(0f, 1f, framePosition - current);
        foregroundGorillaRenderer.sharedMaterial.mainTexture = foregroundReactionFrames[current];
        foregroundBlendRenderer.sharedMaterial.mainTexture = foregroundReactionFrames[next];
        foregroundGorillaRenderer.sharedMaterial.color = new Color(1f, 1f, 1f, 1f - blend);
        foregroundBlendRenderer.sharedMaterial.color = new Color(1f, 1f, 1f, blend);
        foregroundReactionFrame = current;
    }

    private void Build()
    {
        if (root != null) return;
        HidePreviousBackground();
        root = new GameObject("AMBIENTE HÍBRIDO - Cascada y naturaleza");

        Texture2D background = JungleAssets.Load<Texture2D>("Jungle/bowling_waterfall_background");
        if (background != null)
        {
            GameObject plate = new GameObject("Fondo curvo 2D de acuarela");
            plate.transform.SetParent(root.transform);
            backgroundPlate = plate.transform;
            bowlingCamera = Camera.main;
            plate.AddComponent<MeshFilter>();
            plate.AddComponent<MeshRenderer>();
            FitBackgroundToCamera();
            Shader unlit = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
            Material material = new Material(unlit);
            material.mainTexture = background;
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", background);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            // La cascada del arte original está ligeramente a la derecha del centro.
            // Desplazar el muestreo la alinea con el eje de la pista y los pinos.
            material.mainTextureOffset = new Vector2(.03f, 0f);
            plate.GetComponent<Renderer>().sharedMaterial = material;
        }

        CreateNaturalBanks();

        // La cascada ya está pintada en el fondo. El movimiento queda únicamente
        // en su base para evitar trazos flotando sobre el cielo.
        CreateMist(new Vector3(-1.55f, .82f, 13.72f), 0f);
        CreateMist(new Vector3(0f, .70f, 13.70f), 1.7f);
        CreateMist(new Vector3(1.55f, .82f, 13.72f), 3.1f);
        CreateCloud(new Vector3(-4.9f, 8.45f, 13.70f), .16f, 0f);
        CreateCloud(new Vector3(4.0f, 7.95f, 13.68f), .12f, 2.3f);
        CreateBird(new Vector3(-5.6f, 7.2f, 13.50f), .42f, 0f);
        CreateBird(new Vector3(3.1f, 8.0f, 13.48f), .34f, 2.1f);
        CreateBird(new Vector3(5.5f, 6.8f, 13.46f), .38f, 4.0f);
    }

    private void FitBackgroundToCamera()
    {
        if (backgroundPlate == null || bowlingCamera == null) return;
        backgroundPlate.position = bowlingCamera.transform.position;
        backgroundPlate.rotation = bowlingCamera.transform.rotation;
        if (!Mathf.Approximately(lastBackgroundAspect, bowlingCamera.aspect) ||
            !Mathf.Approximately(lastBackgroundFov, bowlingCamera.fieldOfView))
            BuildCurvedBackgroundMesh();
    }

    private void BuildCurvedBackgroundMesh()
    {
        if (backgroundPlate == null || bowlingCamera == null) return;
        MeshFilter filter = backgroundPlate.GetComponent<MeshFilter>();
        if (filter == null) return;

        const int columns = 32;
        const int rows = 18;
        const float radius = 70f;
        float verticalHalf = bowlingCamera.fieldOfView * .5f * Mathf.Deg2Rad * 1.08f;
        float horizontalHalf = Mathf.Atan(Mathf.Tan(verticalHalf / 1.08f) * bowlingCamera.aspect) * 1.08f;
        Vector3[] vertices = new Vector3[(columns + 1) * (rows + 1)];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[columns * rows * 6];

        for (int y = 0; y <= rows; y++)
        {
            float v = y / (float)rows;
            float pitch = Mathf.Lerp(-verticalHalf, verticalHalf, v);
            for (int x = 0; x <= columns; x++)
            {
                float u = x / (float)columns;
                float yaw = Mathf.Lerp(-horizontalHalf, horizontalHalf, u);
                Vector3 direction = new Vector3(Mathf.Sin(yaw) * Mathf.Cos(pitch), Mathf.Sin(pitch), Mathf.Cos(yaw) * Mathf.Cos(pitch));
                int index = y * (columns + 1) + x;
                vertices[index] = direction * radius;
                uv[index] = new Vector2(u, v);
            }
        }

        int triangle = 0;
        for (int y = 0; y < rows; y++)
            for (int x = 0; x < columns; x++)
            {
                int i = y * (columns + 1) + x;
                triangles[triangle++] = i;
                triangles[triangle++] = i + columns + 1;
                triangles[triangle++] = i + 1;
                triangles[triangle++] = i + 1;
                triangles[triangle++] = i + columns + 1;
                triangles[triangle++] = i + columns + 2;
            }

        Mesh mesh = new Mesh { name = "Semiesfera ilustrada del boliche" };
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        filter.sharedMesh = mesh;
        lastBackgroundAspect = bowlingCamera.aspect;
        lastBackgroundFov = bowlingCamera.fieldOfView;
    }

    private void CreateNaturalBanks()
    {
        Texture2D groundTexture = JungleAssets.Load<Texture2D>("Jungle/bowling_jungle_ground");
        Shader unlitGround = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
        Material paintedGround = new Material(unlitGround);
        paintedGround.mainTexture = groundTexture;
        if (paintedGround.HasProperty("_BaseMap")) paintedGround.SetTexture("_BaseMap", groundTexture);
        paintedGround.color = Color.white;
        if (paintedGround.HasProperty("_BaseColor")) paintedGround.SetColor("_BaseColor", Color.white);
        paintedGround.mainTextureScale = new Vector2(.8f, 1.45f);
        for (int side = -1; side <= 1; side += 2)
        {
            GameObject bank = new GameObject(side < 0 ? "Jardín lateral izquierdo redondeado" : "Jardín lateral derecho redondeado");
            bank.name = side < 0 ? "Jardín lateral izquierdo" : "Jardín lateral derecho";
            bank.transform.SetParent(root.transform);
            MeshFilter filter = bank.AddComponent<MeshFilter>();
            bank.AddComponent<MeshRenderer>();
            filter.sharedMesh = CreateRoundedBankMesh(side);
            bank.GetComponent<Renderer>().sharedMaterial = paintedGround;
        }
    }

    private Mesh CreateRoundedBankMesh(int side)
    {
        const int arcSteps = 14;
        const float innerX = 4.15f;
        const float outerX = 14.25f;
        const float frontZ = -12.5f;
        const float capCenterZ = 8.7f;
        float centerX = (innerX + outerX) * .5f;
        float radius = (outerX - innerX) * .5f;
        List<Vector3> boundary = new List<Vector3>
        {
            new Vector3(-innerX, -.46f, frontZ),
            new Vector3(-outerX, -.46f, frontZ)
        };
        for (int i = 0; i <= arcSteps; i++)
        {
            float angle = Mathf.Lerp(Mathf.PI, 0f, i);
            boundary.Add(new Vector3(-centerX + Mathf.Cos(angle) * radius, -.46f, capCenterZ + Mathf.Sin(angle) * radius));
        }

        Vector3[] vertices = new Vector3[boundary.Count + 1];
        Vector2[] uv = new Vector2[vertices.Length];
        vertices[0] = new Vector3(-centerX, -.46f, 0f);
        uv[0] = new Vector2(.5f, .5f);
        for (int i = 0; i < boundary.Count; i++)
        {
            Vector3 point = boundary[i];
            if (side > 0) point.x = -point.x;
            vertices[i + 1] = point;
            uv[i + 1] = new Vector2(Mathf.InverseLerp(-outerX, -innerX, boundary[i].x), Mathf.InverseLerp(frontZ, capCenterZ + radius, boundary[i].z));
        }
        if (side > 0) vertices[0].x = -vertices[0].x;

        int[] triangles = new int[boundary.Count * 3];
        for (int i = 0; i < boundary.Count; i++)
        {
            int next = (i + 1) % boundary.Count;
            triangles[i * 3] = 0;
            triangles[i * 3 + (side < 0 ? 1 : 2)] = i + 1;
            triangles[i * 3 + (side < 0 ? 2 : 1)] = next + 1;
        }
        Mesh mesh = new Mesh { name = side < 0 ? "Jardín izquierdo con remate circular" : "Jardín derecho con remate circular" };
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void CreateAudience()
    {
        foregroundAngryFrames = LoadReactionFrames("Jungle/GorillaAngryFrames", 12);
        foregroundTauntFrames = LoadReactionFrames("Jungle/GorillaTauntFrames30V2", 30);
        foregroundNeutral = foregroundTauntFrames != null ? foregroundTauntFrames[0] : null;
        if (foregroundNeutral == null || foregroundAngryFrames == null || foregroundTauntFrames == null || bowlingCamera == null) return;
        GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Quad);
        panel.name = "Gorila en primer plano junto al jugador";
        panel.transform.SetParent(bowlingCamera.transform, false);
        JungleStageBuilder.StripCollider(panel);
        Material material = new Material(Shader.Find("Sprites/Default"));
        material.mainTexture = foregroundNeutral;
        material.color = Color.white;
        foregroundGorillaRenderer = panel.GetComponent<Renderer>();
        foregroundGorillaRenderer.sharedMaterial = material;
        audiencePanels.Add(panel.transform);

        GameObject blendPanel = GameObject.CreatePrimitive(PrimitiveType.Quad);
        blendPanel.name = "Transición suave del gorila";
        blendPanel.transform.SetParent(bowlingCamera.transform, false);
        JungleStageBuilder.StripCollider(blendPanel);
        Material blendMaterial = new Material(Shader.Find("Sprites/Default"));
        blendMaterial.mainTexture = foregroundNeutral;
        blendMaterial.color = new Color(1f, 1f, 1f, 0f);
        foregroundBlendRenderer = blendPanel.GetComponent<Renderer>();
        foregroundBlendRenderer.sharedMaterial = blendMaterial;
        audiencePanels.Add(blendPanel.transform);
        FaceAudienceTowardCamera();
    }

    private Texture2D[] LoadReactionFrames(string folder, int count)
    {
        Texture2D[] frames = new Texture2D[count];
        for (int i = 0; i < frames.Length; i++)
        {
            frames[i] = JungleAssets.Load<Texture2D>(folder + "/frame_" + i.ToString("00"));
            if (frames[i] == null) return null;
        }
        return frames;
    }

    private void FaceAudienceTowardCamera()
    {
        if (bowlingCamera == null || audiencePanels.Count == 0 || audiencePanels[0] == null) return;
        const float distance = 4.8f;
        const float height = 2.45f;
        float width = height * 1.105f;
        float halfHeight = Mathf.Tan(bowlingCamera.fieldOfView * .5f * Mathf.Deg2Rad) * distance;
        float halfWidth = halfHeight * bowlingCamera.aspect;
        Transform panel = audiencePanels[0];
        panel.localPosition = new Vector3(-halfWidth + width * .28f, -halfHeight + height * .42f, distance);
        panel.localRotation = Quaternion.identity;
        panel.localScale = new Vector3(width, height, 1f);
        if (audiencePanels.Count > 1 && audiencePanels[1] != null)
        {
            Transform blendPanel = audiencePanels[1];
            blendPanel.localPosition = panel.localPosition + new Vector3(0f, 0f, -.002f);
            blendPanel.localRotation = Quaternion.identity;
            blendPanel.localScale = panel.localScale;
        }
    }

    private void HidePreviousBackground()
    {
        string[] groups = { "02 - Arboles editables", "04 - Fondo editable" };
        foreach (string groupName in groups)
        {
            GameObject group = bindings?.Get(groupName);
            if (group != null) group.SetActive(false);
        }

        if (bindings != null) foreach (var entry in bindings.Entries)
        {
            if (entry.target != null && (entry.key == "Pared fondo" || entry.key == "Tronco selva" || entry.key == "Copa selva")) entry.target.SetActive(false);
        }

        GameObject stage = bindings?.Get("Plataforma del gorila");
        if (stage != null)
        {
            Renderer renderer = stage.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = OpaqueMaterial(new Color(.20f, .30f, .22f));
        }
    }

    private void CreateMist(Vector3 position, float phase)
    {
        GameObject mist = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        mist.name = "Neblina móvil";
        mist.transform.SetParent(root.transform);
        mist.transform.position = position;
        mist.transform.localScale = new Vector3(2.7f, .52f, .28f);
        JungleStageBuilder.StripCollider(mist);
        mist.GetComponent<Renderer>().sharedMaterial = TransparentMaterial(new Color(.88f, .97f, 1f, .16f));
        waterStrips.Add(new DriftItem { transform = mist.transform, origin = position, speed = .28f, phase = phase });
    }

    private void CreateCloud(Vector3 position, float speed, float phase)
    {
        GameObject cloud = new GameObject("Nube 3D suave");
        cloud.transform.SetParent(root.transform);
        cloud.transform.position = position;
        Material cloudMaterial = TransparentMaterial(new Color(1f, 1f, 1f, .30f));
        for (int i = 0; i < 3; i++)
        {
            GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            puff.name = "Mancha de nube";
            puff.transform.SetParent(cloud.transform, false);
            puff.transform.localPosition = new Vector3((i - 1) * .65f, i == 1 ? .20f : 0f, 0f);
            puff.transform.localScale = new Vector3(1.45f, .68f, .22f);
            JungleStageBuilder.StripCollider(puff);
            puff.GetComponent<Renderer>().sharedMaterial = cloudMaterial;
        }
        clouds.Add(new DriftItem { transform = cloud.transform, origin = position, speed = speed, phase = phase });
    }

    private void CreateBird(Vector3 position, float speed, float phase)
    {
        GameObject bird = new GameObject("Ave lejana");
        bird.transform.SetParent(root.transform);
        bird.transform.position = position;
        LineRenderer line = bird.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.positionCount = 3;
        line.SetPosition(0, new Vector3(-.24f, 0f, 0f));
        line.SetPosition(1, new Vector3(0f, .13f, 0f));
        line.SetPosition(2, new Vector3(.24f, 0f, 0f));
        line.startWidth = line.endWidth = .035f;
        line.numCapVertices = 2;
        line.sharedMaterial = OpaqueMaterial(new Color(.06f, .13f, .17f));
        birds.Add(new DriftItem { transform = bird.transform, origin = position, speed = speed, phase = phase });
    }

    private Material OpaqueMaterial(Color color)
    {
        Shader unlitColor = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Universal Render Pipeline/Lit");
        Material material = new Material(unlitColor);
        material.color = color;
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        return material;
    }

    private Material TransparentMaterial(Color color)
    {
        Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Transparent");
        Material material = new Material(shader);
        material.color = color;
        return material;
    }

    private void Update()
    {
        if (root == null || !root.activeSelf) return;
        float time = Time.time;
        if (foregroundReactionUntil > 0f && time < foregroundReactionUntil)
        {
            int frame = Mathf.Min(foregroundReactionFrames.Length - 1, Mathf.FloorToInt((time - foregroundReactionStarted) * foregroundReactionFrameRate));
            SetForegroundReactionFrame(frame);
        }
        else if (foregroundReactionUntil > 0f && foregroundGorillaRenderer != null)
        {
            foregroundGorillaRenderer.sharedMaterial.mainTexture = foregroundNeutral;
            foregroundGorillaRenderer.sharedMaterial.color = Color.white;
            if (foregroundBlendRenderer != null)
            {
                foregroundBlendRenderer.sharedMaterial.mainTexture = foregroundNeutral;
                foregroundBlendRenderer.sharedMaterial.color = new Color(1f, 1f, 1f, 0f);
            }
            foregroundReactionUntil = 0f;
            foregroundReactionFrame = -1;
            foregroundReactionFrames = null;
        }
        foreach (DriftItem strip in waterStrips)
        {
            Vector3 position = strip.origin;
            position.y += Mathf.Sin(time * strip.speed * 2.2f + strip.phase) * .10f;
            position.x += Mathf.Sin(time * strip.speed + strip.phase) * .055f;
            strip.transform.position = position;
        }
        if (pool != null)
        {
            float pulse = Mathf.Sin(time * 1.25f) * .055f;
            pool.localScale = new Vector3(7.8f + pulse, .035f, 2.2f + pulse * 2f);
        }
        foreach (DriftItem cloud in clouds)
        {
            Vector3 position = cloud.transform.position;
            position.x += cloud.speed * Time.deltaTime;
            position.y = cloud.origin.y + Mathf.Sin(time * .25f + cloud.phase) * .08f;
            if (position.x > 7.5f) position.x = -7.5f;
            cloud.transform.position = position;
        }
        foreach (DriftItem bird in birds)
        {
            Vector3 position = bird.transform.position;
            position.x += bird.speed * Time.deltaTime;
            position.y = bird.origin.y + Mathf.Sin(time * 1.6f + bird.phase) * .16f;
            if (position.x > 7.2f) position.x = -7.2f;
            bird.transform.position = position;
            bird.transform.localScale = new Vector3(1f, .72f + Mathf.Sin(time * 5f + bird.phase) * .22f, 1f);
        }
    }

    private void LateUpdate()
    {
        // Conserva cobertura total si cambia la relación de aspecto del Game View.
        FitBackgroundToCamera();
        FaceAudienceTowardCamera();
    }
}
