using System;
using GorillaEscape.QA;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GorillaEscape.UI
{
    /// <summary>Product navigation. Technical owners survive the transition between these two views.</summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        public const string BootstrapScenePath = "Assets/_Project/Scenes/Bootstrap/Bootstrap.unity";
#if UNITY_EDITOR
        // Editor assembly supplies test-runner isolation without a player dependency on UnityEditor.
        public static Func<bool> EditorLaunchPermission { private get; set; }
#endif
        public static bool ShouldAutoLaunch(bool editor, bool playing, bool batch, bool testRun, string scenePath, bool productFlow)
            => playing && (editor ? !batch && !testRun && scenePath == BootstrapScenePath : productFlow);

        public static bool QaVisible => Environment.GetEnvironmentVariable("GORILLA_QA_MENU") != "0";
        public static bool ProductFlow => Environment.GetEnvironmentVariable("GORILLA_MAIN_MENU") == "1"
            || Environment.GetEnvironmentVariable("GORILLA_QA_OVERLAY") == "1"
            || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GORILLA_IPC_JAVA"))
                && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GORILLA_IPC_JAR"))
                && Environment.GetEnvironmentVariable("GORILLA_CAMERA_ENABLED") != "1"
                && Environment.GetEnvironmentVariable("GORILLA_CAMERA_LAB") != "1";
        bool AutomatedMeasurement => Environment.GetEnvironmentVariable("GORILLA_QA_AUTOMATED_MEASUREMENT")=="1"&&Runtime!=null&&Runtime.MeasuringFrames;
        public bool InTests { get; private set; }
        public bool PlayEnabled => false; // E0 is still open; no gameplay scene exists.
        public QaRuntimeService Runtime { get; private set; }
        int reportedWidth,reportedHeight;
        Vector2 mainScroll;
        MainMenuView mainView;
        TestsMenuController tests;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            TryLaunchForCurrentScene();
        }

        public static void TryLaunchForCurrentScene()
        {
#if UNITY_EDITOR
            if (EditorLaunchPermission?.Invoke() != true) return;
#endif
            if (!ShouldAutoLaunch(Application.isEditor, Application.isPlaying, Application.isBatchMode,
                false, SceneManager.GetActiveScene().path, ProductFlow)) return;
            if (FindFirstObjectByType<MainMenuController>(FindObjectsInactive.Include) != null) return;
            var host = new GameObject("MainMenu");
            DontDestroyOnLoad(host);
            host.AddComponent<MainMenuController>();
        }

        void Awake()
        {
            // Negotiation remains opt-in within the existing authenticated IPC; no new session authority.
            if (!Application.isEditor && QaVisible) Environment.SetEnvironmentVariable("GORILLA_QA_OVERLAY", "1");
            Runtime = gameObject.AddComponent<QaRuntimeService>();
            mainView = new MainMenuView(this);
            tests = new TestsMenuController(this, Runtime);
        }
        void Start()
        {
            if (Environment.GetEnvironmentVariable("GORILLA_QA_AUTOSTART_TESTS") == "1") OpenTests();
        }
        public void OpenTests() { if (!QaVisible || InTests) return; InTests = true; tests.Enter(); }
        public void Back() { if (AutomatedMeasurement || !InTests) return; tests.Leave(); InTests = false; }
        public void Exit() { Runtime.RequestExit(); }
        void Update() { if (!AutomatedMeasurement && InTests && UnityEngine.Input.GetKeyDown(KeyCode.Escape)) Back(); }
        void OnGUI()
        {
            if(Event.current.type==EventType.Repaint&&(reportedWidth!=Screen.width||reportedHeight!=Screen.height)) {
                reportedWidth=Screen.width;reportedHeight=Screen.height;
                Debug.Log("MAIN_MENU_LAYOUT "+reportedWidth+" "+reportedHeight);
            }
            if(Environment.GetEnvironmentVariable("GORILLA_QA_UI_PROBE")=="1"&&Event.current.type==EventType.MouseDown)
                Debug.Log("MAIN_MENU_POINTER "+Event.current.mousePosition.x+" "+Event.current.mousePosition.y);
            if(AutomatedMeasurement&&(Event.current.isMouse||Event.current.isKey))Event.current.Use();
            var matrix = GUI.matrix;
            var color = GUI.color;
            bool enabledBefore = GUI.enabled;
            try
            {
                // Uniform scaling retains camera geometry. Narrow windows scroll instead of squeezing type.
                float scale = Mathf.Max(.75f, Mathf.Min(Screen.width / 1280f, Screen.height / 940f));
                GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
                if (InTests) tests.Draw(Screen.width / scale, Screen.height / scale);
                else {
                    float width=Screen.width/scale,height=Screen.height/scale;
                    mainScroll=GUI.BeginScrollView(new Rect(0,0,width,height),mainScroll,new Rect(0,0,Mathf.Max(1280,width),Mathf.Max(940,height)));
                    mainView.Draw(Mathf.Max(1280,width),Mathf.Max(940,height));
                    GUI.EndScrollView();
                }
            }
            finally { GUI.matrix = matrix; GUI.color = color; GUI.enabled = enabledBefore; }
        }
    }
}
