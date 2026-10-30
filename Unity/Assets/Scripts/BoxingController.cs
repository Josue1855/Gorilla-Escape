using System;
using UnityEngine;
using GorillaEscape.Gameplay.Input;
[RequireComponent(typeof(JunglePanel))]
public sealed partial class BoxingController : MonoBehaviour, IJungleScreen
{
    private enum GameState { Boxing, BoxingKnockdown, BoxingOver }
    private GameState state;
    private readonly GorillaMascotRig gorillaRig = new GorillaMascotRig();
    private readonly HandVisualPrediction leftVisual = new HandVisualPrediction(), rightVisual = new HandVisualPrediction();
    private MotionSession sharedSession;
    private JungleFusedFrame inputFrame;
    private AudioSource sfxSource;
    private GameObject boxingArena;
    private bool poseDetected, boxingAttackResolved;
    private Vector2 boxingLeftHand = new Vector2(.36f,.63f), boxingRightHand = new Vector2(.64f,.63f);
    private float boxingLeftSpeed, boxingRightSpeed, playerHealth, gorillaHealth, boxingRoundTime, gorillaActionAt, gorillaAttackResolveAt, boxingMessageUntil, getUpEnergy, lastPunchAt, resultAt;
    private float poseBodyX = .5f;
    private int boxingGorillaAction, boxingMessageCode;
    private GUIStyle fightTitleStyle, fightTextStyle, buttonStyle, bodyStyle, bigStyle;
    private Texture2D darkTexture, accentTexture, boxingBlueTexture, boxingRedTexture;
    private bool CameraActive => sharedSession.IsInputReady && poseDetected;
    private void Awake()
    {
        sharedSession = MotionSession.Ensure();
        sfxSource = JungleAudioBank.Attach2DSource(gameObject);
        BuildBoxingArena(); boxingArena.SetActive(true);
        var anchor = new GameObject("Boxing gorilla anchor").transform;
        anchor.SetParent(boxingArena.transform,false);
        gorillaRig.LoadAsync(anchor, MakeMaterial, null);
        var camera = Camera.main;
        if(camera != null) {
            camera.transform.position = new Vector3(0,2.25f,-8f);
            camera.transform.LookAt(new Vector3(0,2.55f,-3f));camera.fieldOfView=42f;
            camera.backgroundColor=new Color(.012f,.018f,.022f);
        }
        StartBoxingGame();
    }
    private Material MakeMaterial(Color color,float metallic=0f,float smoothness=.35f) => JungleStageBuilder.GetSharedMaterial(color,metallic,smoothness,preferStandard:false);
    private GameObject Box(string name,Vector3 position,Vector3 scale,Color color,Transform parent=null,bool keepCollider=true) => JungleStageBuilder.CreatePrimitive(PrimitiveType.Cube,name,position,scale,color,parent,keepCollider:keepCollider,smoothness:.35f,preferStandard:false);
    private void Update()
    {
        sharedSession.PresentGame(state==GameState.BoxingOver ? (gorillaHealth<=0 || playerHealth>0 && playerHealth>=gorillaHealth ? "Won":"Lost") : "Playing",score:Math.Round(100f-gorillaHealth),progress:Mathf.Clamp01(1f-boxingRoundTime/90f),balance:Mathf.Clamp((playerHealth-gorillaHealth)/100f,-1f,1f));
        if(!sharedSession.IsInputReady){poseDetected=false;leftVisual.Reset();rightVisual.Reset();return;}
        if(sharedSession.TryConsumeFrame(out var frame))
        {
            inputFrame=frame;
            if(frame.Baseline){leftVisual.Reset();rightVisual.Reset();}
            var pose=frame.Pose;poseDetected=pose.leftDetected||pose.rightDetected;poseBodyX=pose.bodyX;
            boxingLeftSpeed=frame.Left.Speed;boxingRightSpeed=frame.Right.Speed;
            if(pose.leftDetected){
                boxingLeftHand=Vector2.Lerp(boxingLeftHand,new Vector2(pose.leftWristX,pose.leftWristY),.72f);
                if(frame.NewPose||frame.Baseline)leftVisual.Observe(pose.leftWristX,pose.leftWristY,frame.Left.VelocityX,frame.Left.VelocityY,Time.unscaledTime);
            }else leftVisual.Reset();
            if(pose.rightDetected){
                boxingRightHand=Vector2.Lerp(boxingRightHand,new Vector2(pose.rightWristX,pose.rightWristY),.72f);
                if(frame.NewPose||frame.Baseline)rightVisual.Observe(pose.rightWristX,pose.rightWristY,frame.Right.VelocityX,frame.Right.VelocityY,Time.unscaledTime);
            }else rightVisual.Reset();
            if(state==GameState.Boxing){
                if(pose.leftDetected&&(frame.Actions&JungleAction.PunchLeft)!=0)TryBoxingPunch(false,boxingLeftSpeed,frame.PhonePower);
                if((frame.Actions&JungleAction.PunchRight)!=0)TryBoxingPunch(true,boxingRightSpeed,frame.PhonePower);
            }else if(state==GameState.BoxingKnockdown&&(frame.Actions&JungleAction.Recover)!=0&&sharedSession.TryAuthorize(JungleAction.Recover,frame.Revision))
                getUpEnergy=Mathf.Min(100f,getUpEnergy+Mathf.Clamp((frame.PhonePower-210f)*.12f,8f,24f));
        }
        gorillaRig.Animate(true,boxingGorillaAction,gorillaAttackResolveAt);
        if(state==GameState.Boxing)UpdateBoxing();
        else if(state==GameState.BoxingKnockdown){
            if(getUpEnergy>=100f){playerHealth=45f;boxingGorillaAction=0;gorillaActionAt=Time.time+1.5f;state=GameState.Boxing;}
            else if(Time.time>=resultAt)state=GameState.BoxingOver;
        }
    }
    public void RenderUI()
    {
        JungleUITheme.Ensure();
        if(fightTitleStyle==null){
            fightTitleStyle=new GUIStyle(JungleUITheme.Heading){alignment=TextAnchor.MiddleCenter};
            fightTextStyle=new GUIStyle(JungleUITheme.Body){alignment=TextAnchor.MiddleCenter};
            bodyStyle=new GUIStyle(fightTextStyle);bigStyle=new GUIStyle(fightTitleStyle);buttonStyle=JungleUITheme.Button;
            darkTexture=JungleUITheme.DarkHud;accentTexture=JungleUITheme.Gold;boxingBlueTexture=JungleUITheme.Blue;boxingRedTexture=JungleUITheme.Red;
        }
        DrawBoxing();
    }
    private void OnDestroy()=>gorillaRig.Dispose();
    private void BuildBoxingArena()
    {
        boxingArena = new GameObject("ARENA DE BOXEO");
        GameObject floor = Box("Lona azul del ring", new Vector3(0, -.45f, -1f), new Vector3(14f, .25f, 16f), new Color(.035f, .22f, .48f), keepCollider: false); floor.transform.SetParent(boxingArena.transform);
        GameObject back = Box("Fondo del estadio", new Vector3(0, 3.2f, 5.7f), new Vector3(15f, 7.2f, .35f), new Color(.012f, .016f, .028f), keepCollider: false); back.transform.SetParent(boxingArena.transform);
        GameObject leftWall = Box("Pared izquierda", new Vector3(-7f, 2.5f, -.5f), new Vector3(.3f, 6f, 13f), new Color(.04f, .055f, .06f), keepCollider: false); leftWall.transform.SetParent(boxingArena.transform);
        GameObject rightWall = Box("Pared derecha", new Vector3(7f, 2.5f, -.5f), new Vector3(.3f, 6f, 13f), new Color(.04f, .055f, .06f), keepCollider: false); rightWall.transform.SetParent(boxingArena.transform);
        for (int side = -1; side <= 1; side += 2)
            for (int level = 0; level < 3; level++)
            {
                GameObject rope = Box("Cuerda del ring", new Vector3(side * 5.4f, .9f + level * .62f, -.6f), new Vector3(.09f, .09f, 11.5f), new Color(.82f, .12f, .08f), keepCollider: false);
                rope.transform.SetParent(boxingArena.transform);
            }
        for (int x = -1; x <= 1; x += 2)
        {
            GameObject lamp = Box("Lámpara superior", new Vector3(x * 2.8f, 5.3f, 1.6f), new Vector3(2.2f, .12f, .35f), new Color(.72f, .88f, .9f), keepCollider: false);
            lamp.transform.SetParent(boxingArena.transform);
        }
        for (int level = 0; level < 3; level++)
        {
            GameObject rearRope = Box("Cuerda trasera", new Vector3(0, .9f + level * .62f, 2.15f), new Vector3(11f, .09f, .09f), level == 1 ? Color.white : new Color(.86f, .08f, .06f), keepCollider: false);
            rearRope.transform.SetParent(boxingArena.transform);
        }
        for (int i = -6; i <= 6; i++)
        {
            JungleStageBuilder.CreatePrimitive(
                PrimitiveType.Sphere,
                "Silueta del público",
                new Vector3(i * 1.05f, 2.15f + Mathf.Abs(i % 2) * .28f, 4.9f),
                new Vector3(.72f, 1.15f, .55f),
                new Color(.025f + (i & 1) * .018f, .03f, .045f),
                boxingArena.transform,
                keepCollider: false,
                smoothness: .35f,
                preferStandard: false);
        }
        for (int side = -1; side <= 1; side += 2)
        {
            GameObject lightObject = new GameObject("Reflector del ring"); lightObject.transform.SetParent(boxingArena.transform);
            lightObject.transform.position = new Vector3(side * 3.8f, 5.2f, -3.8f);
            lightObject.transform.LookAt(new Vector3(0, 2.2f, -2.5f));
            Light spot = lightObject.AddComponent<Light>(); spot.type = LightType.Spot; spot.intensity = 5.2f; spot.range = 14f; spot.spotAngle = 48f;
            spot.color = side < 0 ? new Color(.72f, .85f, 1f) : new Color(1f, .78f, .62f);
        }
        boxingArena.SetActive(false);
    }
}
