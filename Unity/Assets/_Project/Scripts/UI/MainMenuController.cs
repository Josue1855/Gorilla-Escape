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
        public void OpenLogBalance()
        {
            if(InPhone||InTests||LogBalance!=null)return;
            var host=new GameObject("Desafío del Tronco");host.transform.SetParent(transform,false);
            LogBalance=host.AddComponent<GorillaEscape.Minigames.LogBalance.LogBalanceController>();LogBalance.Attach(this);
        }
        public void CloseLogBalance(){if(LogBalance==null)return;LogBalance.gameObject.SetActive(false);Destroy(LogBalance.gameObject);LogBalance=null;}
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
            if(Camera.main==null&&FindFirstObjectByType<Camera>()==null){
                var view=new GameObject("Menu Camera");view.transform.SetParent(transform);
                var camera=view.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;
                camera.backgroundColor=new Color(.06f,.08f,.11f);camera.cullingMask=0;
            }
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
        }
        public void OpenDiagnostic(){if(InPhone){Runtime.LeavePhone();InPhone=false;}if(!InTests){InTests=true;tests.Enter();}}
        public void OpenPhone(){if(InTests||InPhone)return;preparePhone=prepareDetails=false;if(prepareTexture!=null){Destroy(prepareTexture);prepareTexture=null;}InPhone=true;Runtime.EnterPhone();}
        public void OpenTests() { if (!QaVisible || InTests) return; if(InPhone){Runtime.LeavePhone();InPhone=false;} InTests = true; tests.Enter(); }
        public void Back() { if(InPhone){Runtime.LeavePhone();InPhone=false;return;} if (AutomatedMeasurement || !InTests) return; tests.Leave(); InTests = false; }
        public void Exit() { Runtime.RequestExit(); }
        void Update() { if (!AutomatedMeasurement && (InTests||InPhone) && UnityEngine.Input.GetKeyDown(KeyCode.Escape)) Back(); }
        bool preparePhone,prepareDetails;
        Texture2D prepareTexture;
        void DrawPhone(float width,float height)
        {
            GUI.Box(new Rect(0,0,width,height),"");
            var title=new GUIStyle(GUI.skin.label){fontSize=32,alignment=TextAnchor.MiddleCenter};
            var body=new GUIStyle(GUI.skin.label){fontSize=22,alignment=TextAnchor.MiddleCenter,wordWrap=true};
            var action=new GUIStyle(GUI.skin.button){fontSize=22};
            float x=(width-600)/2;
            GUI.Label(new Rect(x,55,600,55),"CONECTAR TELÉFONO",title);
            if(GUI.Button(new Rect(40,35,160,45),"VOLVER",action)){preparePhone=false;if(prepareTexture!=null){Destroy(prepareTexture);prepareTexture=null;}Back();}
            if(!Runtime.ServicesEnabled){
                GUI.Label(new Rect(x,180,600,140),"Los servicios del teléfono no están activos. Detén Play y usa Gorilla Escape → Play Tests with Phone. Si faltan recursos, prepara el build local.",body);return;
            }
            if(Runtime.PhoneBackendFailed||Runtime.PhoneTunnelFailed){
                GUI.Label(new Rect(x,180,600,90),Runtime.PhoneTunnelFailed?"Cloudflare no pudo publicar la conexión. Reintenta o revisa el diagnóstico.":"No se pudo iniciar el servicio de conexión.",body);
                if(GUI.Button(new Rect(x,300,280,55),"REINTENTAR",action))Runtime.RetryPhone();
                if(GUI.Button(new Rect(x+300,300,280,55),"DETALLES",action))OpenDiagnostic();
                return;
            }
            if(Runtime.FirewallState=="CHECKING"||Runtime.FirewallState=="AUTHORIZING"){
                GUI.Label(new Rect(x,180,600,70),"Preparando conexión…",body);return;
            }
            if(Runtime.NetworkPermissionRequired){
                GUI.Label(new Rect(x,180,600,110),"Gorilla Escape necesita permiso para permitir la conexión de teléfonos en esta red.",body);
                if(GUI.Button(new Rect(x+150,320,300,55),"CONTINUAR",action))Runtime.InstallNetwork(preparePhone);
                return;
            }
            if(Runtime.NetworkFailed){
                GUI.Label(new Rect(x,180,600,90),"No pudimos habilitar la conexión local.",body);
                if(GUI.Button(new Rect(x,300,280,55),"REINTENTAR",action))Runtime.InstallNetwork(preparePhone);
                if(GUI.Button(new Rect(x+300,300,280,55),"DETALLES",action))OpenDiagnostic();
                return;
            }
            if(preparePhone){
                GUI.Label(new Rect(x,120,600,55),"CONFIGURAR ESTE TELÉFONO",title);
                GUI.Label(new Rect(x,180,600,90),"Verifica la huella con esta PC antes de instalar el certificado. No ignores advertencias de conexión segura.",body);
                var raster=Runtime.java?.QaHosting?.prepare;
                if(prepareTexture==null&&Runtime.java?.QaConnection?.recoveryRequested==true&&raster?.size>0)prepareTexture=QaRuntimeService.DecodeQr(raster);
                if(prepareTexture!=null)GUI.DrawTexture(new Rect((width-280)/2,280,280,280),prepareTexture,ScaleMode.ScaleToFit);
                GUI.Label(new Rect(x,570,600,90),"Compara la huella antes de instalar:\n"+(Runtime.java?.QaHosting?.fingerprint??"Preparando configuración…"),new GUIStyle(body){fontSize=16});
                if(GUI.Button(new Rect(x,665,285,48),"VOLVER A CONECTAR",action)){preparePhone=false;Runtime.EndPhoneRecovery();if(prepareTexture!=null){Destroy(prepareTexture);prepareTexture=null;}Runtime.RegenerateQr();}
                if(GUI.Button(new Rect(x+300,665,285,48),"DETALLES",action))prepareDetails=!prepareDetails;
                if(prepareDetails)GUI.Label(new Rect(x,735,600,120),"Compara este código con el teléfono antes de aceptar:\n"+(Runtime.java?.QaHosting?.fingerprint??""),new GUIStyle(body){fontSize=16});
                return;
            }
            if(Runtime.PhoneReady){GUI.Label(new Rect(x,180,600,80),"CONTROL LISTO",title);return;}
            if(Runtime.PhoneFailed){
                GUI.Label(new Rect(x,180,600,90),"No pudimos conectar este teléfono.",body);
                if(GUI.Button(new Rect(x,300,280,55),"REINTENTAR",action))Runtime.RetryPhone();
                if(GUI.Button(new Rect(x+300,300,280,55),"DIAGNÓSTICO",action))OpenDiagnostic();
                return;
            }
            if(Runtime.CanOfferPhoneRecovery){
                GUI.Label(new Rect(x,180,600,90),"No pudimos conectar este teléfono.",body);
                if(GUI.Button(new Rect(x,300,280,55),"REINTENTAR",action))Runtime.RetryPhone();
                if(GUI.Button(new Rect(x+300,300,280,55),"CONFIGURAR ESTE TELÉFONO",new GUIStyle(action){fontSize=17})){preparePhone=true;Runtime.ConfigurePhone();}
                return;
            }
            if(Runtime.qrTexture==null){
                GUI.Label(new Rect(x,180,600,70),"Preparando conexión…",body);
                if(Runtime.qrState=="EXPIRED"||Runtime.qrState=="SCANNED/JOINED")
                    if(GUI.Button(new Rect(x+150,300,300,55),"NUEVO QR",action))Runtime.RegenerateQr();
                return;
            }
            GUI.DrawTexture(new Rect((width-320)/2,150,320,320),Runtime.qrTexture,ScaleMode.ScaleToFit);
            if(Event.current.type==EventType.Repaint&&Runtime.qrVisible==0)Runtime.qrVisible=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            GUI.Label(new Rect(x,485,600,50),"Escanea con la cámara de tu teléfono",body);
            if(GUI.Button(new Rect(x+150,550,300,48),"NUEVO QR",action))Runtime.RegenerateQr();


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
