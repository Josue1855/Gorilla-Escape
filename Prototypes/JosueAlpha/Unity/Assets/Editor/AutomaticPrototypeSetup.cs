using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class AutomaticPrototypeSetup
{
    private const string ScenePath = "Assets/Scenes/PhoneMotionTest.unity";

    static AutomaticPrototypeSetup()
    {
        EditorApplication.delayCall += EnsureEditableScene;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += EnsureEditableScene;
    }

    private static void EnsureEditableScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!System.IO.File.Exists("Assets/Scenes/Connection.unity") || !System.IO.File.Exists("Assets/Scenes/JungleBaseball.unity") || !System.IO.File.Exists("Assets/Scenes/JungleDrums.unity")) { BuildMultiSceneArchitecture(); return; }
        if (!System.IO.File.Exists("Assets/Scenes/CoconutSlice.unity")) BuildCoconutSliceScene();
        EnsureMultiSceneBuildSettings();
        if (EditorSceneManager.GetActiveScene().path != ScenePath) return;
        if (!System.IO.File.Exists(ScenePath)) { BuildEditableBowlingScene(); return; }
        GameObject root = GameObject.Find("ESCENARIO EDITABLE");
        if (root == null) { BuildEditableBowlingScene(); return; }
        if (GameObject.Find("Punto del fondo isla") == null)
        {
            GameObject background = new GameObject("04 - Fondo editable"); background.transform.SetParent(root.transform);
            GameObject islandAnchor = new GameObject("Punto del fondo isla"); islandAnchor.transform.SetParent(background.transform);
            islandAnchor.transform.position = new Vector3(0, -.42f, 13.2f); islandAnchor.transform.rotation = Quaternion.Euler(0, 180f, 0);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }
        GameObject existingIslandPoint = GameObject.Find("Punto del fondo isla");
        if (existingIslandPoint != null && existingIslandPoint.transform.position.z > 14f)
        {
            existingIslandPoint.transform.position = new Vector3(0, -.42f, 13.2f);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }
        GameObject gorillaPoint = GameObject.Find("Punto del gorila");
        if (gorillaPoint != null && gorillaPoint.transform.position.x < -4.45f)
        {
            gorillaPoint.transform.position = new Vector3(-4.3f, -.02f, 3.8f);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }
    }

    private static void EnsureMultiSceneBuildSettings()
    {
        string[] paths = {
            "Assets/Scenes/Connection.unity",
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/BananaBowling.unity",
            "Assets/Scenes/GorillaBoxing.unity",
            "Assets/Scenes/JungleBaseball.unity",
            "Assets/Scenes/JungleDrums.unity",
            "Assets/Scenes/CoconutSlice.unity"
        };
        foreach (string path in paths) if (!System.IO.File.Exists(path)) return;
        bool correct = EditorBuildSettings.scenes.Length == paths.Length;
        if (correct) for (int i = 0; i < paths.Length; i++) correct &= EditorBuildSettings.scenes[i].enabled && EditorBuildSettings.scenes[i].path == paths[i];
        if (!correct)
        {
            var scenes = new EditorBuildSettingsScene[paths.Length];
            for (int i = 0; i < paths.Length; i++) scenes[i] = new EditorBuildSettingsScene(paths[i], true);
            EditorBuildSettings.scenes = scenes;
            Debug.Log("Lista multi-escena reparada en Build Settings.");
        }
    }

    [MenuItem("Tools/Recreate Phone Motion Test")]
    public static void CreateIfNeeded()
    {
        if (System.IO.File.Exists(ScenePath)) return;

        System.IO.Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        GameObject controller = GameObject.CreatePrimitive(PrimitiveType.Cube);
        controller.name = "Phone Controlled Object";
        controller.transform.localScale = new Vector3(2.4f, 0.35f, 1.1f);
        controller.AddComponent<PhoneMotionController>();

        var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        material.color = new Color(0.05f, 0.55f, 1f);
        controller.GetComponent<Renderer>().sharedMaterial = material;

        Camera.main.transform.position = new Vector3(0, 1.4f, -6f);
        Camera.main.transform.LookAt(Vector3.zero);
        Camera.main.backgroundColor = new Color(0.025f, 0.04f, 0.07f);

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        EditorSceneManager.OpenScene(ScenePath);
        Debug.Log("Prueba de movimiento creada correctamente.");
    }

    [MenuItem("Tools/Construir Banana Bowling editable")]
    public static void BuildEditableBowlingScene()
    {
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        System.IO.Directory.CreateDirectory("Assets/Materials/BananaBowling");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        GameObject root = new GameObject("ESCENARIO EDITABLE");
        GameObject architecture = new GameObject("01 - Pista y arquitectura"); architecture.transform.SetParent(root.transform);
        CreateBox("Pista", architecture.transform, new Vector3(0, -.35f, 2), new Vector3(7.2f, .35f, 21), new Color(.28f, .14f, .045f));
        CreateBox("Franja central", architecture.transform, new Vector3(0, -.16f, 2), new Vector3(6.4f, .05f, 20.6f), new Color(.68f, .43f, .14f));
        CreateBox("Canal izquierdo", architecture.transform, new Vector3(-4.15f, -.2f, 2), new Vector3(1.1f, .28f, 21), new Color(.025f, .12f, .07f));
        CreateBox("Canal derecho", architecture.transform, new Vector3(4.15f, -.2f, 2), new Vector3(1.1f, .28f, 21), new Color(.025f, .12f, .07f));
        CreateBox("Pared fondo", architecture.transform, new Vector3(0, 2.5f, 12.3f), new Vector3(11, 5.5f, .4f), new Color(.04f, .18f, .09f));
        for (int i = -3; i <= 3; i++) { GameObject mark = CreateBox("Marca " + (i + 4), architecture.transform, new Vector3(i, -.135f, -5.4f), new Vector3(.08f, .015f, .6f), new Color(.95f, .84f, .45f)); mark.GetComponent<Collider>().enabled = false; }

        GameObject jungle = new GameObject("02 - Arboles editables"); jungle.transform.SetParent(root.transform);
        for (int side = -1; side <= 1; side += 2)
            for (int z = -5; z <= 10; z += 3)
            {
                if (side == -1 && z >= 1 && z <= 7) continue;
                CreateTree(jungle.transform, side * 5.2f, z);
            }
        GameObject spectator = new GameObject("03 - Zona del gorila"); spectator.transform.SetParent(root.transform);
        GameObject stage = CreateBox("Plataforma del gorila", spectator.transform, new Vector3(-5f, -.18f, 4.2f), new Vector3(2.8f, .28f, 4.5f), new Color(.18f, .09f, .035f)); stage.GetComponent<Collider>().enabled = false;
        GameObject anchor = new GameObject("Punto del gorila"); anchor.transform.SetParent(spectator.transform); anchor.transform.position = new Vector3(-4.3f, -.02f, 3.8f);
        GameObject background = new GameObject("04 - Fondo editable"); background.transform.SetParent(root.transform);
        GameObject islandAnchor = new GameObject("Punto del fondo isla"); islandAnchor.transform.SetParent(background.transform); islandAnchor.transform.position = new Vector3(0, -.42f, 13.2f); islandAnchor.transform.rotation = Quaternion.Euler(0, 180f, 0);

        GameObject rack = new GameObject("Pinos editables"); rack.transform.SetParent(root.transform);
        float startZ = 7.2f; int number = 1;
        for (int row = 0; row < 4; row++)
            for (int col = 0; col <= row; col++)
            {
                GameObject pin = GameObject.CreatePrimitive(PrimitiveType.Capsule); pin.name = "Pino " + number++;
                pin.transform.SetParent(rack.transform); pin.transform.position = new Vector3((col - row * .5f) * .78f, .65f, startZ + row * .72f);
                pin.transform.localScale = new Vector3(.43f, .72f, .43f); pin.GetComponent<Renderer>().sharedMaterial = MaterialFor("Pinos", new Color(.96f, .96f, .9f));
                Rigidbody body = pin.AddComponent<Rigidbody>(); body.mass = 1.25f; body.linearDamping = .08f; body.angularDamping = .05f;
            }

        GameObject controller = GameObject.CreatePrimitive(PrimitiveType.Cube); controller.name = "Coco y controlador del juego";
        controller.transform.SetParent(root.transform); controller.transform.position = new Vector3(0, .48f, -7.2f);
        controller.GetComponent<Renderer>().enabled = false; controller.GetComponent<Collider>().enabled = false;
        controller.AddComponent<PhoneMotionController>();
        GameObject coconut = GameObject.CreatePrimitive(PrimitiveType.Sphere); coconut.name = "Visual del coco"; coconut.transform.SetParent(controller.transform, false);
        coconut.transform.localScale = new Vector3(1.08f, 1.03f, 1.1f); coconut.GetComponent<Renderer>().sharedMaterial = MaterialFor("Coco", new Color(.30f, .14f, .055f)); coconut.GetComponent<Collider>().enabled = false;

        Camera.main.transform.position = new Vector3(0, 6.4f, -12.5f); Camera.main.transform.LookAt(new Vector3(0, .5f, 2.8f)); Camera.main.fieldOfView = 52f;
        Light light = Object.FindFirstObjectByType<Light>(); if (light != null) { light.intensity = 1.35f; light.transform.rotation = Quaternion.Euler(48, -25, 0); }
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        Selection.activeGameObject = root;
        Debug.Log("Banana Bowling editable construido. Abre ESCENARIO EDITABLE en la jerarquía.");
    }

    [MenuItem("Tools/Construir arquitectura multi-escena")]
    public static void BuildMultiSceneArchitecture()
    {
        const string connectionPath = "Assets/Scenes/Connection.unity";
        const string menuPath = "Assets/Scenes/MainMenu.unity";
        const string bowlingPath = "Assets/Scenes/BananaBowling.unity";
        const string boxingPath = "Assets/Scenes/GorillaBoxing.unity";
        const string baseballPath = "Assets/Scenes/JungleBaseball.unity";
        const string drumsPath = "Assets/Scenes/JungleDrums.unity";
        const string coconutSlicePath = "Assets/Scenes/CoconutSlice.unity";
        System.IO.Directory.CreateDirectory("Assets/Scenes");

        if (!System.IO.File.Exists(bowlingPath)) AssetDatabase.CopyAsset(ScenePath, bowlingPath);
        CreateSimpleScene(connectionPath, "Connection Controller", typeof(ConnectionScreen), new Color(.015f, .06f, .055f));
        CreateSimpleScene(menuPath, "Main Menu Controller", typeof(MainMenuController), new Color(.018f, .075f, .055f));
        CreateSimpleScene(boxingPath, "Boxing Controller", typeof(PhoneMotionController), new Color(.012f, .018f, .022f));
        CreateSimpleScene(baseballPath, "Baseball Controller", typeof(BaseballController), new Color(.22f, .58f, .88f));
        CreateSimpleScene(drumsPath, "Rhythm Drums Controller", typeof(RhythmDrumsController), new Color(.018f, .035f, .05f));
        CreateSimpleScene(coconutSlicePath, "Coconut Slice Controller", typeof(CoconutSliceController), new Color(.015f, .075f, .055f));

        EditorBuildSettings.scenes = new[] {
            new EditorBuildSettingsScene(connectionPath, true),
            new EditorBuildSettingsScene(menuPath, true),
            new EditorBuildSettingsScene(bowlingPath, true),
            new EditorBuildSettingsScene(boxingPath, true),
            new EditorBuildSettingsScene(baseballPath, true),
            new EditorBuildSettingsScene(drumsPath, true),
            new EditorBuildSettingsScene(coconutSlicePath, true)
        };
        EditorSceneManager.OpenScene(connectionPath);
        Debug.Log("Arquitectura creada: Connection, MainMenu, BananaBowling y GorillaBoxing.");
    }

    [MenuItem("Tools/Construir Coconut Slice")]
    public static void BuildCoconutSliceScene()
    {
        const string path = "Assets/Scenes/CoconutSlice.unity";
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        if (!System.IO.File.Exists(path))
            CreateSimpleScene(path, "Coconut Slice Controller", typeof(CoconutSliceController), new Color(.015f, .075f, .055f));
        EnsureMultiSceneBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("Coconut Slice creado y registrado en Build Settings.");
    }
    private static void CreateSimpleScene(string path, string controllerName, System.Type controllerType, Color background)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        GameObject controller = new GameObject(controllerName); controller.AddComponent(controllerType);
        Camera.main.backgroundColor = background;
        EditorSceneManager.SaveScene(scene, path);
    }

    private static GameObject CreateBox(string name, Transform parent, Vector3 position, Vector3 scale, Color color)
    {
        GameObject item = GameObject.CreatePrimitive(PrimitiveType.Cube); item.name = name; item.transform.SetParent(parent);
        item.transform.position = position; item.transform.localScale = scale; item.GetComponent<Renderer>().sharedMaterial = MaterialFor(name, color); return item;
    }

    private static void CreateTree(Transform parent, float x, float z)
    {
        GameObject tree = new GameObject("Arbol " + (x < 0 ? "izquierdo " : "derecho ") + z); tree.transform.SetParent(parent);
        GameObject trunk = CreateBox("Tronco", tree.transform, new Vector3(x, 1.1f, z), new Vector3(.45f, 2.3f, .45f), new Color(.25f, .11f, .035f)); trunk.GetComponent<Collider>().enabled = false;
        GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere); crown.name = "Copa"; crown.transform.SetParent(tree.transform); crown.transform.position = new Vector3(x, 2.7f, z); crown.transform.localScale = new Vector3(2.1f, 1.5f, 1.6f); crown.GetComponent<Renderer>().sharedMaterial = MaterialFor("Hojas", new Color(.03f, .35f, .12f)); crown.GetComponent<Collider>().enabled = false;
    }

    private static Material MaterialFor(string name, Color color)
    {
        string safe = name.Replace(" ", "_").Replace("-", "_"); string path = "Assets/Materials/BananaBowling/" + safe + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null) { material = new Material(Shader.Find("Standard")); material.color = color; AssetDatabase.CreateAsset(material, path); }
        return material;
    }
}
