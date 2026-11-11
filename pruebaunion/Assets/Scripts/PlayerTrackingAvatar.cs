using UnityEngine;
using UnityEngine.SceneManagement;

internal sealed class PlayerTrackingAvatar : MonoBehaviour
{
    private const int PreviewLayer = 31;
    private const string Asset = "Jungle/PlayerAvatar/Meshy_AI_model_20261111_animated_mixamo_Running";

    private Camera preview;
    private RenderTexture picture;
    private Material material;
    private Arm screenLeft, screenRight;
    private Vector3 up, across;
    private Transform headBone;
    private Quaternion headRest;
    private float hipsViewportY;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= AddToScene;
        SceneManager.sceneLoaded += AddToScene;
    }

    private static void AddToScene(Scene scene, LoadSceneMode mode)
    {
        string name = scene.name;
        if (name != SceneNavigator.Connection && name != SceneNavigator.MainMenu &&
            (!SceneNavigator.IsMinigame(name) || name == SceneNavigator.LogBalance)) return;

        var overlay = new GameObject("Náufrago · seguimiento de manos");
        SceneManager.MoveGameObjectToScene(overlay, scene);
        overlay.AddComponent<PlayerTrackingAvatar>();
    }

    private void Awake()
    {
        var prefab = Resources.Load<GameObject>(Asset);
        if (prefab == null) return;

        transform.position = new Vector3(500f, -500f, 500f);
        var model = Instantiate(prefab, transform);
        foreach (var animator in model.GetComponentsInChildren<Animator>(true))
            animator.enabled = false;
        foreach (var animation in model.GetComponentsInChildren<Animation>(true))
            animation.enabled = false;
        foreach (var node in model.GetComponentsInChildren<Transform>(true))
            node.gameObject.layer = PreviewLayer;

        screenLeft = new Arm(model.transform, "Right");
        screenRight = new Arm(model.transform, "Left");
        var hips = FindBone(model.transform, "Hips");
        var head = FindBone(model.transform, "Head");
        if (!screenLeft.Valid || !screenRight.Valid || hips == null || head == null)
        {
            Debug.LogWarning("El náufrago necesita los huesos de brazos, cadera y cabeza.");
            Destroy(gameObject);
            return;
        }

        headBone = head;
        headRest = head.localRotation;
        Vector3 shoulders = (screenRight.Shoulder - screenLeft.Shoulder).normalized;
        Vector3 front = Vector3.Cross(shoulders, Vector3.up).normalized;
        var leftFoot = FindBone(model.transform, "LeftFoot");
        var leftToe = FindBone(model.transform, "LeftToeBase");
        var rightFoot = FindBone(model.transform, "RightFoot");
        var rightToe = FindBone(model.transform, "RightToeBase");
        if (leftFoot != null && leftToe != null && rightFoot != null && rightToe != null)
        {
            Vector3 toes = leftToe.position - leftFoot.position + rightToe.position - rightFoot.position;
            if (Vector3.Dot(front, toes) < 0f) front = -front;
        }

        Vector3 forward = front;
        up = Vector3.up;
        across = Vector3.Cross(up, forward).normalized;
        if (Vector3.Dot(screenRight.Shoulder - screenLeft.Shoulder, across) < 0f)
            (screenLeft, screenRight) = (screenRight, screenLeft);

        var renderers = model.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;
        var texture = Resources.Load<Texture2D>("Jungle/PlayerAvatar/NaufragoTexture");
        Shader shader = Shader.Find("Unlit/Texture");
        if (texture != null && shader != null)
            material = new Material(shader) { mainTexture = texture };
        Bounds bounds = renderers[0].bounds;
        foreach (var renderer in renderers)
        {
            bounds.Encapsulate(renderer.bounds);
            if (renderer is SkinnedMeshRenderer skin) skin.updateWhenOffscreen = true;
            if (material != null) renderer.sharedMaterial = material;
        }

        float height = bounds.size.y;
        if (height <= .001f) return;
        Vector3 center = Vector3.Lerp(hips.position, head.position, .50f);
        float radius = Mathf.Max(height * .52f, (screenRight.Shoulder - screenLeft.Shoulder).magnitude * 1.6f);
        picture = new RenderTexture(640, 640, 24, RenderTextureFormat.ARGB32);

        var cameraObject = new GameObject("Cámara del avatar");
        cameraObject.transform.SetParent(transform, false);
        preview = cameraObject.AddComponent<Camera>();
        preview.transform.SetPositionAndRotation(center - forward * height * 4f, Quaternion.LookRotation(forward, up));
        preview.clearFlags = CameraClearFlags.SolidColor;
        preview.backgroundColor = Color.clear;
        preview.orthographic = true;
        preview.orthographicSize = radius;
        preview.nearClipPlane = height;
        preview.farClipPlane = height * 8f;
        preview.cullingMask = 1 << PreviewLayer;
        preview.targetTexture = picture;
        preview.allowHDR = false;
        preview.allowMSAA = false;
        hipsViewportY = preview.WorldToViewportPoint(hips.position).y;
    }

    private void LateUpdate()
    {
        if (preview == null) return;

        var pose = MotionSession.Instance?.LatestPose;
        float blend = 1f - Mathf.Exp(-24f * Time.unscaledDeltaTime);
        float bodyX = pose?.bodyX ?? .5f;
        float bodyY = pose?.bodyY ?? .5f;
        Quaternion headTarget = headRest;
        if (pose?.headDetected == true)
        {
            Vector3 tilt = across * Mathf.Clamp(pose.headPitch, -22f, 22f)
                + up * Mathf.Clamp(pose.headYaw, -32f, 32f)
                + preview.transform.forward * Mathf.Clamp(pose.headRoll, -24f, 24f);
            float angle = tilt.magnitude;
            if (angle > .01f)
                headTarget = Quaternion.AngleAxis(angle, headBone.parent.InverseTransformDirection(tilt / angle)) * headRest;
        }
        headBone.localRotation = Quaternion.Slerp(headBone.localRotation, headTarget, blend);
        Vector3 towardPlayer = -preview.transform.forward;
        screenLeft.Follow(pose?.leftDetected == true, (pose?.leftWristX ?? bodyX) - bodyX,
            bodyY - (pose?.leftWristY ?? bodyY), across, up, towardPlayer, -1f, blend);
        screenRight.Follow(pose?.rightDetected == true, (pose?.rightWristX ?? bodyX) - bodyX,
            bodyY - (pose?.rightWristY ?? bodyY), across, up, towardPlayer, 1f, blend);
    }

    private void OnGUI()
    {
        if (JungleStory.BlocksGame || picture == null || Event.current.type != EventType.Repaint) return;
        bool connecting = gameObject.scene.name == SceneNavigator.Connection;
        float size = connecting
            ? Mathf.Min(Screen.height * .24f, Screen.width * .22f)
            : Mathf.Min(Screen.height * .76f, Screen.width * .34f);
        float margin = Screen.height * .018f;
        float bottom = Screen.height + size * hipsViewportY;
        int depth = GUI.depth;
        GUI.depth = -80;
        GUI.DrawTexture(new Rect(margin, bottom - size, size, size), picture);
        GUI.depth = depth;
    }

    private void OnDestroy()
    {
        if (preview != null) preview.targetTexture = null;
        if (picture != null)
        {
            picture.Release();
            Destroy(picture);
        }
        if (material != null) Destroy(material);
    }

    private static Transform FindBone(Transform model, string name)
    {
        foreach (var bone in model.GetComponentsInChildren<Transform>(true))
            if (bone.name == name || bone.name.EndsWith(":" + name, System.StringComparison.Ordinal))
                return bone;
        return null;
    }

    private sealed class Arm
    {
        private readonly Transform upper, forearm, wrist;
        private readonly Quaternion upperRest, forearmRest;
        public bool Valid => upper != null && forearm != null && wrist != null;
        public Vector3 Shoulder => upper.position;

        public Arm(Transform model, string side)
        {
            upper = FindBone(model, side + "Arm");
            forearm = FindBone(model, side + "ForeArm");
            wrist = FindBone(model, side + "Hand");
            if (!Valid) return;
            upperRest = upper.localRotation;
            forearmRest = forearm.localRotation;
        }

        public void Follow(bool tracked, float x, float y, Vector3 across, Vector3 up,
            Vector3 towardPlayer, float side, float blend)
        {
            Quaternion previousUpper = upper.localRotation;
            Quaternion previousForearm = forearm.localRotation;
            upper.localRotation = upperRest;
            forearm.localRotation = forearmRest;

            Vector3 shoulder = upper.position;
            float a = Vector3.Distance(shoulder, forearm.position);
            float b = Vector3.Distance(forearm.position, wrist.position);
            if (Mathf.Min(a, b) <= .0001f) return;
            float reach = a + b;
            Vector3 offset = tracked
                ? (across * (x * 3f - side * .30f) + up * (y * 3f - .07f) + towardPlayer * .23f) * reach
                : (across * side * .15f - up * .85f + towardPlayer * .10f) * reach;
            Vector3 direction = offset.normalized;
            float distance = Mathf.Clamp(offset.magnitude, Mathf.Abs(a - b) + reach * .01f, reach * .985f);
            float projection = (a * a - b * b + distance * distance) / (2f * distance);
            Vector3 bend = Vector3.ProjectOnPlane(-up + across * side * .65f - towardPlayer * .45f, direction).normalized;
            Vector3 elbow = shoulder + direction * projection + bend * Mathf.Sqrt(Mathf.Max(0, a * a - projection * projection));
            Vector3 target = shoulder + direction * distance;

            upper.rotation = Quaternion.FromToRotation(forearm.position - shoulder, elbow - shoulder) * upper.rotation;
            forearm.rotation = Quaternion.FromToRotation(wrist.position - forearm.position, target - forearm.position) * forearm.rotation;
            upper.localRotation = Quaternion.Slerp(previousUpper, upper.localRotation, blend);
            forearm.localRotation = Quaternion.Slerp(previousForearm, forearm.localRotation, blend);
        }
    }
}




