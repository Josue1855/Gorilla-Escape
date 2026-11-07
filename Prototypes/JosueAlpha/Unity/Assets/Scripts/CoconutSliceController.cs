using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class CoconutSliceController : MonoBehaviour
{
    [Serializable] private struct PosePacket
    {
        public bool detected, leftDetected, rightDetected;
        public float leftWristX, leftWristY, rightWristX, rightWristY, bodyX, confidence, time;
    }

    private enum GameState { Intro, Countdown, Playing, Finale, Results }
    private sealed class Coconut
    {
        public GameObject gameObject;
        public Rigidbody body;
        public bool sliced;
        public float bornAt;
    }

    private const float RoundDuration = 45f;
    private const float SliceSpeed = .72f;
    private readonly List<Coconut> coconuts = new List<Coconut>();
    private readonly List<GameObject> temporaryPieces = new List<GameObject>();
    private MotionSession session;
    private GameState state = GameState.Intro;
    private Vector2 leftHand = new Vector2(.32f, .52f), rightHand = new Vector2(.68f, .52f);
    private Vector2 previousLeft, previousRight, mousePrevious;
    private float leftSpeed, rightSpeed, nextSpawn, stateStartedAt, roundEndsAt, lastSliceAt;
    private int score, combo, bestCombo, missed, sliced;
    private string callout = "";
    private float calloutUntil;
    private Transform gorillaBody, gorillaLeftArm, gorillaRightArm;
    private Material coconutMaterial, coconutInsideMaterial, darkMaterial, greenMaterial;
    private Texture2D dark, gold, turquoise, orange, red, white;
    private GUIStyle titleStyle, bigStyle, bodyStyle, scoreStyle;

    private void Awake()
    {
        session = MotionSession.Ensure();
        session.Connect();
        BuildStage();
        previousLeft = leftHand;
        previousRight = rightHand;
        stateStartedAt = Time.time;
    }

    private Material MakeMaterial(Color color)
    {
        Material material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        material.color = color;
        return material;
    }

    private GameObject Primitive(PrimitiveType type, string objectName, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject item = GameObject.CreatePrimitive(type);
        item.name = objectName;
        item.transform.SetParent(parent);
        item.transform.position = position;
        item.transform.localScale = scale;
        item.GetComponent<Renderer>().sharedMaterial = material;
        Collider collider = item.GetComponent<Collider>();
        if (collider != null) collider.enabled = false;
        return item;
    }

    private void BuildStage()
    {
        coconutMaterial = MakeMaterial(new Color(.27f, .105f, .035f));
        coconutInsideMaterial = MakeMaterial(new Color(.94f, .92f, .76f));
        darkMaterial = MakeMaterial(new Color(.035f, .075f, .055f));
        greenMaterial = MakeMaterial(new Color(.035f, .30f, .12f));

        Camera camera = Camera.main;
        camera.orthographic = true;
        camera.orthographicSize = 5.4f;
        camera.transform.position = new Vector3(0, 0, -10f);
        camera.transform.rotation = Quaternion.identity;
        camera.backgroundColor = new Color(.025f, .16f, .12f);

        Primitive(PrimitiveType.Cube, "Fondo selva", transform, new Vector3(0, 0, 4.5f), new Vector3(20, 12, .3f), greenMaterial);
        for (int i = -5; i <= 5; i++)
        {
            float height = 1.2f + Mathf.Abs(i % 3) * .45f;
            Primitive(PrimitiveType.Cylinder, "Palmera " + i, transform, new Vector3(i * 1.85f, -4.25f + height * .5f, 3.8f), new Vector3(.18f, height, .18f), darkMaterial);
            Primitive(PrimitiveType.Sphere, "Copa " + i, transform, new Vector3(i * 1.85f, -3.0f + height, 3.7f), new Vector3(1.55f, .55f, .25f), darkMaterial);
        }

        GameObject gorilla = new GameObject("Gorila anfitrión");
        gorilla.transform.SetParent(transform);
        gorillaBody = Primitive(PrimitiveType.Capsule, "Torso", gorilla.transform, new Vector3(0, -3.48f, 2.5f), new Vector3(1.35f, 1.1f, .65f), darkMaterial).transform;
        Primitive(PrimitiveType.Sphere, "Cabeza", gorilla.transform, new Vector3(0, -2.0f, 2.45f), new Vector3(1.0f, .82f, .55f), darkMaterial);
        Primitive(PrimitiveType.Sphere, "Hocico", gorilla.transform, new Vector3(0, -2.12f, 1.85f), new Vector3(.62f, .42f, .25f), coconutInsideMaterial);
        gorillaLeftArm = Primitive(PrimitiveType.Capsule, "Brazo izquierdo", gorilla.transform, new Vector3(-1.34f, -3.15f, 2.3f), new Vector3(.48f, 1.22f, .48f), darkMaterial).transform;
        gorillaRightArm = Primitive(PrimitiveType.Capsule, "Brazo derecho", gorilla.transform, new Vector3(1.34f, -3.15f, 2.3f), new Vector3(.48f, 1.22f, .48f), darkMaterial).transform;
        gorillaLeftArm.rotation = Quaternion.Euler(0, 0, -24f);
        gorillaRightArm.rotation = Quaternion.Euler(0, 0, 24f);
    }

    private void Update()
    {
        ReadPose();
        AnimateGorilla();

        if (state == GameState.Countdown && Time.time - stateStartedAt >= 3.2f) StartRound();
        else if (state == GameState.Playing)
        {
            if (Time.time >= nextSpawn) SpawnWave();
            if (Time.time >= roundEndsAt) StartFinale();
        }
        else if (state == GameState.Finale && Time.time - stateStartedAt >= 3.4f)
        {
            ClearCoconuts();
            state = GameState.Results;
            stateStartedAt = Time.time;
        }

        UpdateCoconuts();
        UpdateTemporaryPieces();
    }

    private void ReadPose()
    {
        string json = session.ConsumePose();
        if (!string.IsNullOrEmpty(json))
        {
            PosePacket pose = JsonUtility.FromJson<PosePacket>(json);
            float dt = Mathf.Clamp(Time.deltaTime, .01f, .08f);
            Vector2 nextLeft = new Vector2(pose.leftWristX, 1f - pose.leftWristY);
            Vector2 nextRight = new Vector2(pose.rightWristX, 1f - pose.rightWristY);
            leftSpeed = Vector2.Distance(nextLeft, previousLeft) / dt;
            rightSpeed = Vector2.Distance(nextRight, previousRight) / dt;
            if (pose.leftDetected)
            {
                TrySliceSegment(previousLeft, nextLeft, leftSpeed);
                leftHand = Vector2.Lerp(leftHand, nextLeft, .72f);
            }
            if (pose.rightDetected)
            {
                TrySliceSegment(previousRight, nextRight, rightSpeed);
                rightHand = Vector2.Lerp(rightHand, nextRight, .72f);
            }
            previousLeft = nextLeft;
            previousRight = nextRight;
        }

        Vector2 mouse = new Vector2(Input.mousePosition.x / Mathf.Max(1f, Screen.width), Input.mousePosition.y / Mathf.Max(1f, Screen.height));
        if (Input.GetMouseButton(0)) TrySliceSegment(mousePrevious, mouse, Vector2.Distance(mousePrevious, mouse) / Mathf.Max(.01f, Time.deltaTime));
        mousePrevious = mouse;
    }

    private void TrySliceSegment(Vector2 from, Vector2 to, float speed)
    {
        if (state != GameState.Playing || speed < SliceSpeed || Time.time - lastSliceAt < .045f) return;
        for (int i = coconuts.Count - 1; i >= 0; i--)
        {
            Coconut coconut = coconuts[i];
            if (coconut.sliced) continue;
            Vector3 viewport3 = Camera.main.WorldToViewportPoint(coconut.gameObject.transform.position);
            if (viewport3.z <= 0f) continue;
            Vector2 viewport = new Vector2(viewport3.x, viewport3.y);
            if (DistanceToSegment(viewport, from, to) < .075f)
            {
                Slice(coconut, to - from, speed);
                lastSliceAt = Time.time;
                break;
            }
        }
    }

    private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 segment = b - a;
        float lengthSquared = segment.sqrMagnitude;
        if (lengthSquared < .00001f) return Vector2.Distance(point, a);
        float t = Mathf.Clamp01(Vector2.Dot(point - a, segment) / lengthSquared);
        return Vector2.Distance(point, a + segment * t);
    }

    private void SpawnWave()
    {
        int amount = UnityEngine.Random.value < .28f ? 2 : 1;
        for (int i = 0; i < amount; i++) SpawnCoconut(i, amount);
        float progress = 1f - Mathf.Clamp01((roundEndsAt - Time.time) / RoundDuration);
        nextSpawn = Time.time + Mathf.Lerp(1.05f, .52f, progress) + UnityEngine.Random.Range(-.08f, .15f);
    }

    private void SpawnCoconut(int index, int count)
    {
        float x = UnityEngine.Random.Range(-4.6f, 4.6f) + (index - (count - 1) * .5f) * 1.15f;
        GameObject visual = Primitive(PrimitiveType.Sphere, "Coco", transform, new Vector3(x, -5.05f, UnityEngine.Random.Range(-.4f, .45f)), new Vector3(.72f, .68f, .66f), coconutMaterial);
        visual.GetComponent<Collider>().enabled = true;
        Rigidbody body = visual.AddComponent<Rigidbody>();
        body.mass = .65f;
        body.useGravity = true;
        body.linearVelocity = new Vector3(Mathf.Clamp(-x * .30f + UnityEngine.Random.Range(-1.7f, 1.7f), -3.5f, 3.5f), UnityEngine.Random.Range(9.4f, 12.2f), 0f);
        body.angularVelocity = UnityEngine.Random.insideUnitSphere * 8f;
        coconuts.Add(new Coconut { gameObject = visual, body = body, bornAt = Time.time });
    }

    private void Slice(Coconut coconut, Vector2 direction, float speed)
    {
        coconut.sliced = true;
        Vector3 position = coconut.gameObject.transform.position;
        Vector3 velocity = coconut.body.linearVelocity;
        Destroy(coconut.gameObject);
        coconuts.Remove(coconut);

        Vector3 split = new Vector3(direction.y, -direction.x, 0f).normalized;
        for (int side = -1; side <= 1; side += 2)
        {
            GameObject half = Primitive(PrimitiveType.Sphere, side < 0 ? "Medio coco A" : "Medio coco B", transform, position + split * side * .18f, new Vector3(.36f, .66f, .62f), coconutInsideMaterial);
            Rigidbody body = half.AddComponent<Rigidbody>();
            body.linearVelocity = velocity * .72f + split * side * Mathf.Clamp(speed, 1.4f, 4.5f);
            body.angularVelocity = UnityEngine.Random.insideUnitSphere * 12f;
            temporaryPieces.Add(half);
        }

        sliced++;
        combo++;
        bestCombo = Mathf.Max(bestCombo, combo);
        int gained = 100 + Mathf.Min(combo, 20) * 15;
        score += gained;
        callout = combo >= 10 ? "¡GORILA IMPARABLE!  x" + combo : combo >= 5 ? "¡COMBO x" + combo + "!" : "+" + gained;
        calloutUntil = Time.time + .65f;
    }

    private void UpdateCoconuts()
    {
        for (int i = coconuts.Count - 1; i >= 0; i--)
        {
            Coconut coconut = coconuts[i];
            if (coconut.gameObject == null) { coconuts.RemoveAt(i); continue; }
            if (coconut.gameObject.transform.position.y < -6f && Time.time - coconut.bornAt > .7f)
            {
                if (state == GameState.Playing)
                {
                    missed++;
                    combo = 0;
                    callout = "SE ESCAPÓ";
                    calloutUntil = Time.time + .65f;
                }
                Destroy(coconut.gameObject);
                coconuts.RemoveAt(i);
            }
        }
    }

    private void UpdateTemporaryPieces()
    {
        for (int i = temporaryPieces.Count - 1; i >= 0; i--)
        {
            GameObject piece = temporaryPieces[i];
            if (piece == null || piece.transform.position.y < -6.5f)
            {
                if (piece != null) Destroy(piece);
                temporaryPieces.RemoveAt(i);
            }
        }
    }

    private void BeginCountdown()
    {
        ClearCoconuts();
        score = combo = bestCombo = missed = sliced = 0;
        state = GameState.Countdown;
        stateStartedAt = Time.time;
    }

    private void StartRound()
    {
        state = GameState.Playing;
        stateStartedAt = Time.time;
        roundEndsAt = Time.time + RoundDuration;
        nextSpawn = Time.time + .25f;
        callout = "¡CORTA!";
        calloutUntil = Time.time + .8f;
    }

    private void StartFinale()
    {
        state = GameState.Finale;
        stateStartedAt = Time.time;
        callout = "¡TIEMPO!";
        calloutUntil = Time.time + 2f;
    }

    private void ClearCoconuts()
    {
        foreach (Coconut coconut in coconuts) if (coconut.gameObject != null) Destroy(coconut.gameObject);
        coconuts.Clear();
        foreach (GameObject piece in temporaryPieces) if (piece != null) Destroy(piece);
        temporaryPieces.Clear();
    }

    private void AnimateGorilla()
    {
        float t = Time.time - stateStartedAt;
        bool celebration = state == GameState.Intro || state == GameState.Finale || state == GameState.Results;
        float beat = celebration ? Mathf.Abs(Mathf.Sin(t * 5.5f)) : Mathf.Abs(Mathf.Sin(t * 1.8f)) * .25f;
        gorillaLeftArm.localRotation = Quaternion.Euler(0, 0, -24f + beat * 34f);
        gorillaRightArm.localRotation = Quaternion.Euler(0, 0, 24f - beat * 34f);
        gorillaBody.localScale = new Vector3(1.35f + beat * .05f, 1.1f - beat * .03f, .65f);
    }

    private Texture2D Solid(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private void EnsureUI()
    {
        if (dark != null) return;
        dark = Solid(new Color(.01f, .045f, .035f, .94f));
        gold = Solid(new Color(1f, .76f, .08f, .96f));
        turquoise = Solid(new Color(.03f, .82f, .78f, .95f));
        orange = Solid(new Color(1f, .34f, .08f, .95f));
        red = Solid(new Color(.90f, .08f, .04f, .95f));
        white = Solid(new Color(1f, 1f, 1f, .92f));
        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(Screen.height * .052f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1f, .82f, .15f) } };
        bigStyle = new GUIStyle(titleStyle) { fontSize = Mathf.RoundToInt(Screen.height * .105f), normal = { textColor = Color.white } };
        bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(Screen.height * .029f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true, normal = { textColor = Color.white } };
        scoreStyle = new GUIStyle(bodyStyle) { fontSize = Mathf.RoundToInt(Screen.height * .034f) };
    }

    private void OnGUI()
    {
        JungleUITheme.Ensure();
        EnsureUI();
        float w = Screen.width, h = Screen.height;

        GUI.DrawTexture(new Rect(w * .015f, h * .02f, w * .97f, h * .105f), dark);
        GUI.Label(new Rect(w * .05f, h * .025f, w * .44f, h * .085f), "COCONUT SLICE", titleStyle);
        GUI.Label(new Rect(w * .46f, h * .025f, w * .38f, h * .085f), $"PUNTOS {score}   COMBO x{combo}", scoreStyle);
        if (GUI.Button(new Rect(w * .85f, h * .035f, w * .12f, h * .065f), "MENÚ", JungleUITheme.Button)) SceneNavigator.Load(SceneNavigator.MainMenu);

        DrawHand(leftHand, turquoise, "IZQ");
        DrawHand(rightHand, orange, "DER");

        if (state == GameState.Intro) DrawIntro(w, h);
        else if (state == GameState.Countdown)
        {
            int count = Mathf.Clamp(3 - Mathf.FloorToInt(Time.time - stateStartedAt), 1, 3);
            GUI.DrawTexture(new Rect(w * .37f, h * .29f, w * .26f, h * .25f), dark);
            GUI.Label(new Rect(w * .38f, h * .30f, w * .24f, h * .23f), count.ToString(), bigStyle);
            GUI.Label(new Rect(w * .30f, h * .59f, w * .40f, h * .07f), "LEVANTA LAS MANOS", bodyStyle);
        }
        else if (state == GameState.Playing)
        {
            float remaining = Mathf.Max(0f, roundEndsAt - Time.time);
            GUI.DrawTexture(new Rect(w * .42f, h * .135f, w * .16f, h * .07f), remaining < 6f ? red : dark);
            GUI.Label(new Rect(w * .42f, h * .135f, w * .16f, h * .07f), Mathf.CeilToInt(remaining) + " s", scoreStyle);
        }
        else if (state == GameState.Finale) DrawFinale(w, h);
        else if (state == GameState.Results) DrawResults(w, h);

        if (Time.time < calloutUntil)
        {
            float pulse = 1f + Mathf.Sin((calloutUntil - Time.time) * 10f) * .04f;
            Rect box = new Rect(w * (.35f - (pulse - 1f) * .3f), h * .22f, w * .30f * pulse, h * .09f * pulse);
            GUI.DrawTexture(box, callout.Contains("ESCAP") ? red : gold);
            GUI.Label(box, callout, bodyStyle);
        }
        JungleUITheme.DrawLeaves();
    }

    private void DrawIntro(float w, float h)
    {
        float entrance = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((Time.time - stateStartedAt) / .75f));
        Rect panel = new Rect(w * (.50f - .25f * entrance), h * .20f, w * .50f * entrance, h * .57f);
        GUI.DrawTexture(panel, dark);
        if (entrance < .65f) return;
        GUI.Label(new Rect(w * .29f, h * .23f, w * .42f, h * .09f), "¡EL GORILA TIENE HAMBRE!", titleStyle);
        GUI.DrawTexture(new Rect(w * .30f, h * .34f, w * .40f, h * .055f), turquoise);
        GUI.Label(new Rect(w * .29f, h * .325f, w * .42f, h * .09f), "1  MUEVE TUS MANOS FRENTE A LA CÁMARA", bodyStyle);
        GUI.DrawTexture(new Rect(w * .30f, h * .44f, w * .40f, h * .055f), orange);
        GUI.Label(new Rect(w * .29f, h * .425f, w * .42f, h * .09f), "2  HAZ UN CORTE RÁPIDO SOBRE LOS COCOS", bodyStyle);
        GUI.Label(new Rect(w * .30f, h * .535f, w * .40f, h * .07f), "USA AMBAS MANOS · ENCADENA CORTES", bodyStyle);
        if (GUI.Button(new Rect(w * .37f, h * .64f, w * .26f, h * .085f), "¡ESTOY LISTO!", JungleUITheme.Button)) BeginCountdown();
    }

    private void DrawFinale(float w, float h)
    {
        float t = Time.time - stateStartedAt;
        float scale = 1f + Mathf.Sin(t * 8f) * Mathf.Exp(-t * 1.5f) * .16f;
        Rect box = new Rect(w * (.50f - .22f * scale), h * .32f, w * .44f * scale, h * .22f * scale);
        GUI.DrawTexture(box, gold);
        GUI.Label(box, "¡TIEMPO!", bigStyle);
        GUI.Label(new Rect(w * .30f, h * .57f, w * .40f, h * .07f), "EL GORILA CUENTA TUS COCOS...", bodyStyle);
    }

    private void DrawResults(float w, float h)
    {
        string rank = score >= 5000 ? "REY DE LA SELVA" : score >= 3000 ? "NINJA GORILA" : score >= 1600 ? "CORTADOR TROPICAL" : "APRENDIZ DEL COCO";
        GUI.DrawTexture(new Rect(w * .27f, h * .19f, w * .46f, h * .59f), dark);
        GUI.Label(new Rect(w * .31f, h * .22f, w * .38f, h * .08f), "RESULTADO", titleStyle);
        GUI.DrawTexture(new Rect(w * .32f, h * .315f, w * .36f, h * .08f), gold);
        GUI.Label(new Rect(w * .32f, h * .315f, w * .36f, h * .08f), rank, bodyStyle);
        GUI.Label(new Rect(w * .32f, h * .41f, w * .36f, h * .15f), $"{score} PUNTOS\n{sliced} COCOS CORTADOS  ·  MEJOR COMBO x{bestCombo}\n{missed} COCOS ESCAPARON", bodyStyle);
        if (GUI.Button(new Rect(w * .34f, h * .63f, w * .20f, h * .085f), "OTRA RONDA", JungleUITheme.Button)) BeginCountdown();
        if (GUI.Button(new Rect(w * .56f, h * .63f, w * .10f, h * .085f), "MENÚ", JungleUITheme.Button)) SceneNavigator.Load(SceneNavigator.MainMenu);
    }

    private void DrawHand(Vector2 hand, Texture2D color, string label)
    {
        float size = Mathf.Max(34f, Screen.height * .06f);
        Rect rect = new Rect(hand.x * Screen.width - size * .5f, (1f - hand.y) * Screen.height - size * .5f, size, size);
        GUI.DrawTexture(rect, white);
        GUI.DrawTexture(new Rect(rect.x + 5, rect.y + 5, rect.width - 10, rect.height - 10), color);
        GUI.Label(rect, label, new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, normal = { textColor = Color.black } });
    }
}
