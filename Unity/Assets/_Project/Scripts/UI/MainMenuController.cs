using System;
using GorillaEscape.QA;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GorillaEscape.UI
{
    /// <summary>Product navigation. Technical owners survive view transitions.</summary>
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
        public bool InPhone { get; private set; }
        public bool PlayEnabled => false; // Full tournament remains unavailable; Log Balance has a separate prototype entry.
        public GorillaEscape.Minigames.LogBalance.LogBalanceController LogBalance { get; private set; }
        MainMenuBackdrop backdrop;
        public void OpenLogBalance()
        {
            if(InPhone||InTests||LogBalance!=null)return;
            if(backdrop!=null)backdrop.SetVisible(false);
            var host=new GameObject("Desafío del Tronco");host.transform.SetParent(transform,false);
            LogBalance=host.AddComponent<GorillaEscape.Minigames.LogBalance.LogBalanceController>();LogBalance.Attach(this);
        }
        public void CloseLogBalance(){if(LogBalance==null)return;LogBalance.gameObject.SetActive(false);Destroy(LogBalance.gameObject);LogBalance=null;if(backdrop!=null)backdrop.SetVisible(true);}
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
            backdrop = gameObject.AddComponent<MainMenuBackdrop>();
            // Negotiation remains opt-in within the existing authenticated IPC; no new session authority.
            Runtime = gameObject.AddComponent<QaRuntimeService>();
            if(Runtime.ServicesEnabled)Environment.SetEnvironmentVariable("GORILLA_QA_OVERLAY", "1");
            mainView = new MainMenuView(this);
            tests = new TestsMenuController(this, Runtime);
        }
        void Start()
        {
            if (Environment.GetEnvironmentVariable("GORILLA_QA_AUTOSTART_TESTS") == "1") OpenTests();
            else if(Environment.GetEnvironmentVariable("GORILLA_QA_AUTOSTART_PHONE")=="1")OpenPhone();
            else if(Debug.isDebugBuild&&Environment.GetEnvironmentVariable("GORILLA_QA_AUTOSTART_LOG_BALANCE")=="1"){OpenLogBalance();OpenPhoneForLogBalance();}
        }
        public void OpenDiagnostic(){if(InPhone){Runtime.LeavePhone();InPhone=false;}if(!InTests){InTests=true;tests.Enter();}}
        public void OpenPhoneForLogBalance(){returnToLogBalance=true;CloseLogBalance();OpenPhone();}
        public void OpenPhone(){if(InTests||InPhone)return;preparePhone=prepareDetails=false;if(prepareTexture!=null){Destroy(prepareTexture);prepareTexture=null;}InPhone=true;if(backdrop!=null)backdrop.SetVisible(false);Runtime.EnterPhone();}
        public void OpenTests() { if (!QaVisible || InTests) return; if(InPhone){Runtime.LeavePhone();InPhone=false;} InTests = true; if(backdrop!=null)backdrop.SetVisible(false); tests.Enter(); }
        public void Back() { if(InPhone){Runtime.LeavePhone();InPhone=false;returnToLogBalance=false;if(backdrop!=null)backdrop.SetVisible(true);return;} if (AutomatedMeasurement || !InTests) return; tests.Leave(); InTests = false; if(backdrop!=null)backdrop.SetVisible(true); }
        public void Exit() { Runtime.RequestExit(); }
        public bool CompletePhoneConnection()
        {
            if(!InPhone||!Runtime.PhoneReady)return false;
            int player=Runtime.ReadyPhonePlayer;
            bool returnToGame=returnToLogBalance;returnToLogBalance=false;
            Back();
            if(returnToGame){OpenLogBalance();LogBalance.SelectKeyboard(false);LogBalance.SelectPlayer(player);}
            return true;
        }
        void Update() { if(InPhone&&returnToLogBalance&&Runtime.PhoneReady){CompletePhoneConnection();return;} if (!AutomatedMeasurement && (InTests||InPhone) && UnityEngine.Input.GetKeyDown(KeyCode.Escape)) Back(); }
        bool preparePhone,prepareDetails,returnToLogBalance;
        Texture2D prepareTexture;
        void DrawPhone(float width,float height)
        {
            // Deep background overlay
            GUI.color = new Color(0.039f, 0.11f, 0.086f, 0.98f);
            GUI.DrawTexture(new Rect(0, 0, width, height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            var title = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1.0f, 0.95f, 0.83f) } };
            var subtitle = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(1.0f, 0.706f, 0.368f) } };
            var body = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter, wordWrap = true, normal = { textColor = new Color(0.8f, 0.88f, 0.84f) } };
            var action = new GUIStyle(GUI.skin.button) { fontSize = 19, fontStyle = FontStyle.Bold };

            // Modal Card Container
            float cardW = Mathf.Min(640, width - 80);
            float cardH = 740;
            float cardX = (width - cardW) / 2;
            float cardY = 40;

            // Card background & border
            GUI.color = new Color(0.078f, 0.20f, 0.16f, 0.95f);
            GUI.DrawTexture(new Rect(cardX, cardY, cardW, cardH), Texture2D.whiteTexture);
            GUI.color = new Color(0.56f, 0.87f, 0.68f, 0.3f);
            GUI.DrawTexture(new Rect(cardX, cardY, cardW, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cardX, cardY + cardH - 2, cardW, 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cardX, cardY, 2, cardH), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cardX + cardW - 2, cardY, 2, cardH), Texture2D.whiteTexture);
            GUI.color = Color.white;

            if (GUI.Button(new Rect(cardX + 24, cardY + 24, 130, 42), "← VOLVER", action))
            {
                preparePhone = false;
                if (prepareTexture != null) { Destroy(prepareTexture); prepareTexture = null; }
                Back();
            }

            GUI.Label(new Rect(cardX, cardY + 28, cardW, 26), "GORILLA ESCAPE · MULTIJUGADOR", subtitle);
            GUI.Label(new Rect(cardX, cardY + 54, cardW, 44), "CONECTAR TELÉFONO", title);

            if (!Runtime.ServicesEnabled)
            {
                GUI.Label(new Rect(cardX + 40, cardY + 220, cardW - 80, 140),
                    "Los servicios del teléfono no están activos. Detén Play y usa Gorilla Escape → Play Tests with Phone. Si faltan recursos, prepara el build local.", body);
                return;
            }

            if (Runtime.PhoneBackendFailed || Runtime.PhoneTunnelFailed)
            {
                GUI.Label(new Rect(cardX + 40, cardY + 200, cardW - 80, 90),
                    Runtime.PhoneTunnelFailed ? "Cloudflare no pudo publicar la conexión. Reintenta o revisa el diagnóstico." : "No se pudo iniciar el servicio de conexión.", body);
                if (GUI.Button(new Rect(cardX + 40, cardY + 340, (cardW - 100) / 2, 54), "REINTENTAR", action)) Runtime.RetryPhone();
                if (GUI.Button(new Rect(cardX + 60 + (cardW - 100) / 2, cardY + 340, (cardW - 100) / 2, 54), "DETALLES", action)) OpenDiagnostic();
                return;
            }

            if (Runtime.FirewallState == "CHECKING" || Runtime.FirewallState == "AUTHORIZING")
            {
                GUI.Label(new Rect(cardX + 40, cardY + 240, cardW - 80, 70), "Preparando conexión local segura…", body);
                return;
            }

            if (Runtime.NetworkPermissionRequired)
            {
                GUI.Label(new Rect(cardX + 40, cardY + 200, cardW - 80, 110),
                    "Gorilla Escape necesita permiso para permitir la conexión de teléfonos en esta red.", body);
                if (GUI.Button(new Rect(cardX + (cardW - 280) / 2, cardY + 340, 280, 54), "CONTINUAR", action)) Runtime.InstallNetwork(preparePhone);
                return;
            }

            if (Runtime.NetworkFailed)
            {
                GUI.Label(new Rect(cardX + 40, cardY + 200, cardW - 80, 90), "No pudimos habilitar la conexión local.", body);
                if (GUI.Button(new Rect(cardX + 40, cardY + 340, (cardW - 100) / 2, 54), "REINTENTAR", action)) Runtime.InstallNetwork(preparePhone);
                if (GUI.Button(new Rect(cardX + 60 + (cardW - 100) / 2, cardY + 340, (cardW - 100) / 2, 54), "DETALLES", action)) OpenDiagnostic();
                return;
            }

            if (preparePhone)
            {
                GUI.Label(new Rect(cardX, cardY + 115, cardW, 36), "CONFIGURAR ESTE TELÉFONO", title);
                GUI.Label(new Rect(cardX + 40, cardY + 155, cardW - 80, 70), "Verifica la huella con esta PC antes de instalar el certificado. No ignores advertencias de conexión segura.", body);
                var raster = Runtime.java?.QaHosting?.prepare;
                if (prepareTexture == null && Runtime.java?.QaConnection?.recoveryRequested == true && raster?.size > 0) prepareTexture = QaRuntimeService.DecodeQr(raster);
                if (prepareTexture != null)
                {
                    GUI.color = Color.white;
                    GUI.DrawTexture(new Rect((width - 290) / 2, cardY + 235, 290, 290), Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect((width - 270) / 2, cardY + 245, 270, 270), prepareTexture, ScaleMode.ScaleToFit);
                }
                GUI.Label(new Rect(cardX + 20, cardY + 540, cardW - 40, 70), "Compara la huella antes de instalar:\n" + (Runtime.java?.QaHosting?.fingerprint ?? "Preparando configuración…"), new GUIStyle(body) { fontSize = 15 });
                if (GUI.Button(new Rect(cardX + 30, cardY + 625, (cardW - 80) / 2, 48), "VOLVER A CONECTAR", action))
                {
                    preparePhone = false; Runtime.EndPhoneRecovery();
                    if (prepareTexture != null) { Destroy(prepareTexture); prepareTexture = null; }
                    Runtime.RegenerateQr();
                }
                if (GUI.Button(new Rect(cardX + 50 + (cardW - 80) / 2, cardY + 625, (cardW - 80) / 2, 48), "DETALLES", action)) prepareDetails = !prepareDetails;
                if (prepareDetails) GUI.Label(new Rect(cardX + 20, cardY + 685, cardW - 40, 50), Runtime.java?.QaHosting?.fingerprint ?? "", new GUIStyle(body) { fontSize = 13 });
                return;
            }

            if (Runtime.PhoneReady)
            {
                GUI.Label(new Rect(cardX, cardY + 220, cardW, 55), "¡CONTROL LISTO!", title);
                GUI.Label(new Rect(cardX + 40, cardY + 275, cardW - 80, 60), "Tu teléfono se ha emparejado exitosamente con la sala de juego.", body);
                if (GUI.Button(new Rect(cardX + (cardW - 320) / 2, cardY + 360, 320, 60), "IR AL TRONCO 🌴", action))
                {
                    returnToLogBalance = true;
                    CompletePhoneConnection();
                }
                return;
            }

            if (Runtime.PhoneFailed)
            {
                GUI.Label(new Rect(cardX + 40, cardY + 200, cardW - 80, 90), "No pudimos conectar este teléfono.", body);
                if (GUI.Button(new Rect(cardX + 40, cardY + 340, (cardW - 100) / 2, 54), "REINTENTAR", action)) Runtime.RetryPhone();
                if (GUI.Button(new Rect(cardX + 60 + (cardW - 100) / 2, cardY + 340, (cardW - 100) / 2, 54), "DIAGNÓSTICO", action)) OpenDiagnostic();
                return;
            }

            if (Runtime.CanOfferPhoneRecovery)
            {
                GUI.Label(new Rect(cardX + 40, cardY + 200, cardW - 80, 90), "No pudimos conectar este teléfono.", body);
                if (GUI.Button(new Rect(cardX + 40, cardY + 340, (cardW - 100) / 2, 54), "REINTENTAR", action)) Runtime.RetryPhone();
                if (GUI.Button(new Rect(cardX + 60 + (cardW - 100) / 2, cardY + 340, (cardW - 100) / 2, 54), "CONFIGURAR TELÉFONO", new GUIStyle(action) { fontSize = 16 }))
                {
                    preparePhone = true;
                    Runtime.ConfigurePhone();
                }
                return;
            }

            if (Runtime.qrTexture == null)
            {
                GUI.Label(new Rect(cardX + 40, cardY + 240, cardW - 80, 70), "Generando código de sala…", body);
                if (Runtime.qrState == "EXPIRED" || Runtime.qrState == "SCANNED/JOINED")
                    if (GUI.Button(new Rect(cardX + (cardW - 280) / 2, cardY + 340, 280, 54), "NUEVO QR", action)) Runtime.RegenerateQr();
                return;
            }

            // High contrast white backing frame for QR code
            float qrSize = 250;
            float qrBoxSize = qrSize + 20;
            float qrBoxX = (width - qrBoxSize) / 2;
            float qrBoxY = cardY + 115;

            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(qrBoxX, qrBoxY, qrBoxSize, qrBoxSize), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(qrBoxX + 10, qrBoxY + 10, qrSize, qrSize), Runtime.qrTexture, ScaleMode.ScaleToFit);

            if (Event.current.type == EventType.Repaint && Runtime.qrVisible == 0)
                Runtime.qrVisible = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            if (!string.IsNullOrEmpty(Runtime.roomCode))
            {
                var roomStyle = new GUIStyle(title) { fontSize = 32, alignment = TextAnchor.MiddleCenter, normal = { textColor = new Color(0.96f, 0.78f, 0.24f) } };
                GUI.Label(new Rect(cardX + 20, qrBoxY + qrBoxSize + 15, cardW - 40, 40), "SALA: " + Runtime.roomCode, roomStyle);
                GUI.Label(new Rect(cardX + 20, qrBoxY + qrBoxSize + 55, cardW - 40, 36), "Escanea el QR o ingresa con este código en la PWA / App", body);
            }
            else
            {
                GUI.Label(new Rect(cardX + 20, qrBoxY + qrBoxSize + 25, cardW - 40, 48),
                    "Apunta la cámara de tu teléfono al código para ingresar", body);
            }

            if (GUI.Button(new Rect(cardX + (cardW - 260) / 2, qrBoxY + qrBoxSize + 105, 260, 48), "NUEVO CÓDIGO / QR", action))
            {
                Runtime.RegenerateQr();
            }
        }
        void OnDestroy(){if(prepareTexture!=null)Destroy(prepareTexture);}
        void OnGUI()
        {
            if(LogBalance!=null)return;
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
                if(InPhone)DrawPhone(Screen.width/scale,Screen.height/scale);
                else if (InTests) tests.Draw(Screen.width / scale, Screen.height / scale);
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
