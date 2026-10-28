using JungleParty.Portable;
using GorillaEscape.Gameplay.Input;
using GorillaEscape.Contracts;
using UnityEngine;
using UnityEngine.SceneManagement;

// Gameplay facade only. No HTTP polling, process management, room creation or transport authority.
[DefaultExecutionOrder(-10000)]
public sealed class MotionSession : MonoBehaviour, IPortableInputSource
{
    public enum HandTrackingMode { RightOnly, LeftOnly, Both }
    public static MotionSession Instance { get; private set; }
    public string RoomCode => serviceHost != null && !string.IsNullOrEmpty(serviceHost.RoomCode) ? serviceHost.RoomCode : "";
    public string Status => serviceHost != null ? serviceHost.PhoneStatus : "SIN CONEXIÓN";
    public bool IsPolling => serviceHost != null && serviceHost.IsRunning;
    public bool PhoneConnected => serviceHost != null && serviceHost.PhoneConnected;
    public bool CameraConnected => serviceHost != null && serviceHost.CameraConnected;
    public float LastPhonePacketAt => serviceHost != null ? serviceHost.LastPhonePacketAt : -10;
    public float LastPosePacketAt => serviceHost != null ? serviceHost.LastPosePacketAt : -10;
    public HandTrackingMode HandMode { get; private set; } = HandTrackingMode.RightOnly;
    public bool UsesLeftHand => HandMode != HandTrackingMode.RightOnly;
    public bool UsesRightHand => HandMode != HandTrackingMode.LeftOnly;
    PortableServiceHost serviceHost;
    readonly GorillaEscape.Contracts.GamePresentationWire presentation=new GorillaEscape.Contracts.GamePresentationWire();
    float nextPresentation;
    public void PresentGame(string phase,double? score=null,float? progress=null,float? elapsed=null,float? balance=null,float? distance=null)
    {
        presentation.phase=phase;var s=presentation.stats;
        s.hasScore=score.HasValue;s.score=score.GetValueOrDefault();
        s.hasProgress=progress.HasValue;s.progress=Mathf.Clamp01(progress.GetValueOrDefault());
        s.hasElapsed=elapsed.HasValue;s.elapsed=Mathf.Clamp(elapsed.GetValueOrDefault(),0,3600);
        s.hasBalance=balance.HasValue;s.balance=Mathf.Clamp(balance.GetValueOrDefault(),-1,1);
        s.hasDistance=distance.HasValue;s.distance=Mathf.Clamp(distance.GetValueOrDefault(),0,10000);
    }
    void LateUpdate()
    {
        if(serviceHost==null||Time.unscaledTime<nextPresentation)return;
        nextPresentation=Time.unscaledTime+.2f;
        var context=presentation.Copy();
        if(context.gameId!="lobby"&&(!IsInputReady||Time.timeScale==0)&&context.phase!="Won"&&context.phase!="Lost"&&context.phase!="Finished")context.phase="Paused";
        serviceHost.PresentPhoneGame(context);
    }
    long delivered=-1;
    readonly JunglePauseLease pause=new JunglePauseLease();
    public bool IsInputReady => serviceHost!=null&&serviceHost.IsInputReady;
    public JungleInputStatus InputStatus => serviceHost!=null?serviceHost.InputStatus:JungleInputStatus.Waiting;
    public static MotionSession Ensure() => Instance != null ? Instance : new GameObject("SESION PERSISTENTE - Fase 0").AddComponent<MotionSession>();
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        serviceHost = PortableServiceHost.Ensure();
        ConfigureForScene(SceneManager.GetActiveScene().name);
        Connect();
    }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if(scene!=SceneManager.GetActiveScene())return;
        ConfigureForScene(scene.name);
        serviceHost.SelectGame(JungleGameplayInput.ForScene(scene.name),true);
        delivered=-1;
        RefreshPause();
    }
    public void ConfigureForScene(string sceneName)
    {
        presentation.gameId=JungleGameplayInput.ForScene(sceneName) switch {
            JungleGame.Bowling=>"bowling",JungleGame.Boxing=>"boxing",JungleGame.Baseball=>"baseball",JungleGame.Drums=>"drums",
            JungleGame.CoconutSlice=>"coconut-slice",JungleGame.Whack=>"whack",JungleGame.LogBalance=>"log-balance",_=>"lobby"};
        presentation.playerId=presentation.gameId=="lobby"?0:1;
        PresentGame("Ready");nextPresentation=0;
        HandMode = sceneName == SceneNavigator.Boxing || sceneName == SceneNavigator.Drums || sceneName == SceneNavigator.CoconutSlice || sceneName == SceneNavigator.LogBalance
            ? HandTrackingMode.Both : HandTrackingMode.RightOnly;
        serviceHost.SelectGame(JungleGameplayInput.ForScene(sceneName));
        delivered=-1;
    }
    public void Connect() => serviceHost.StartModule();
    public void ResetRound()
    {
        serviceHost.SelectGame(JungleGameplayInput.ForScene(SceneManager.GetActiveScene().name),true);
        delivered=-1;RefreshPause();
    }
    public bool TryConsumeFrame(out JungleFusedFrame frame)
    {
        RefreshPause();
        if(!serviceHost.TryReadFrame(out frame)||frame.Revision==delivered)return false;
        delivered=frame.Revision;return true;
    }
    public bool TryAuthorize(JungleAction action,long revision) => serviceHost.Authorize(action,revision);
    void Update() => RefreshPause();
    void RefreshPause()
    {
        bool paused=serviceHost!=null&&serviceHost.Game!=JungleGame.None&&!IsInputReady;
        float current=Time.timeScale;
        float desired=pause.Apply(paused,current);
        if(desired!=current)Time.timeScale=desired;
    }
    void OnGUI()
    {
        if(serviceHost==null||serviceHost.Game==JungleGame.None||IsInputReady)return;
        int depth=GUI.depth;GUI.depth=-100;
        float width=Mathf.Min(Screen.width-40,760);
        var pauseTitle = new GUIStyle(GUI.skin.label) { font = JungleUITheme.TitleFont, fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        bool canReacquire=JungleGameplayInput.RequiresCamera(serviceHost.Game)&&serviceHost.CameraConnected;
        bool showHelp=serviceHost.ShowCameraHelp;
        float previewHeight=showHelp?Mathf.Min(260,Mathf.Max(100,Screen.height*.38f)):0;
        float height=(canReacquire?190:140)+(showHelp?previewHeight+35:0);
        float top=Mathf.Max(5,(Screen.height-height)/2);
        GUI.Box(new Rect((Screen.width-width)/2,top,width,height),GUIContent.none);
        GUI.Label(new Rect((Screen.width-width)/2+20,top+8,width-40,34),"JUEGO EN PAUSA",pauseTitle);
        string message=serviceHost.PauseMessage;
        GUI.Label(new Rect((Screen.width-width)/2+20,top+43,width-40,60),message,
            new GUIStyle(GUI.skin.label){font=JungleUITheme.TextFont,wordWrap=true,alignment=TextAnchor.MiddleCenter,fontSize=20});
        if(showHelp)
        {
            float imageWidth=Mathf.Min(width-40,previewHeight*4f/3f);
            Rect picture=new Rect((Screen.width-imageWidth)/2,top+108,imageWidth,previewHeight);
            GUI.DrawTexture(picture,Texture2D.blackTexture);
            var texture=serviceHost.CameraHelpTexture;
            if(texture!=null){GUI.DrawTextureWithTexCoords(picture,texture,new Rect(1,0,-1,1));DrawCameraGuides(picture,serviceHost.CameraHelpFrame);}
            else GUI.Label(picture,"PREPARANDO VISTA DE CÁMARA",new GUIStyle(GUI.skin.label){alignment=TextAnchor.MiddleCenter,wordWrap=true});
            GUI.Label(new Rect((Screen.width-width)/2+20,picture.yMax+3,width-40,25),"Colócate en el centro y muestra hombros, brazos y manos.",new GUIStyle(GUI.skin.label){font=JungleUITheme.TextFont,fontSize=14,alignment=TextAnchor.MiddleCenter,wordWrap=true});
        }
        if(canReacquire&&GUI.Button(new Rect((Screen.width-width)/2+width*.22f,top+height-50,width*.56f,42),"VOLVER A DETECTAR JUGADOR"))
        {
            if(serviceHost.ReacquireCamera())delivered=-1;
        }
        GUI.depth=depth;
    }
    static void DrawCameraGuides(Rect picture,CameraInputWire frame)
    {
        if(frame?.subjects==null)return;
        Color before=GUI.color;
        foreach(var subject in frame.subjects)
        {
            if(subject?.landmarks==null)continue;
            GUI.color=subject.ambiguous?Color.yellow:Color.green;
            foreach(var point in subject.landmarks)
            {
                if(point==null||!point.visible||point.x<0||point.x>1||point.y<0||point.y>1)continue;
                Vector2 p=CameraPoint(picture,point);GUI.DrawTexture(new Rect(p.x-3,p.y-3,6,6),Texture2D.whiteTexture);
            }
            foreach(string pair in new[]{"left-shoulder,right-shoulder","left-shoulder,left-elbow","left-elbow,left-wrist","left-wrist,left-hand","right-shoulder,right-elbow","right-elbow,right-wrist","right-wrist,right-hand"})
            {
                var names=pair.Split(',');var a=System.Array.Find(subject.landmarks,p=>p!=null&&p.name==names[0]);var b=System.Array.Find(subject.landmarks,p=>p!=null&&p.name==names[1]);
                if(a==null||b==null||!a.visible||!b.visible||a.x<0||a.x>1||a.y<0||a.y>1||b.x<0||b.x>1||b.y<0||b.y>1)continue;
                Vector2 start=CameraPoint(picture,a),end=CameraPoint(picture,b);var matrix=GUI.matrix;
                GUIUtility.RotateAroundPivot(Mathf.Atan2(end.y-start.y,end.x-start.x)*Mathf.Rad2Deg,start);
                GUI.DrawTexture(new Rect(start.x,start.y,Vector2.Distance(start,end),2),Texture2D.whiteTexture);GUI.matrix=matrix;
            }
        }
        GUI.color=before;
    }
    static Vector2 CameraPoint(Rect picture,CameraLandmark point)=>new Vector2(picture.x+(1-point.x)*picture.width,picture.y+point.y*picture.height);
    void OnDestroy()
    {
        if (Instance != this) return;
        Time.timeScale=pause.Apply(false,Time.timeScale);
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }
}
