using System;
using System.Collections.Generic;
using UnityEngine;
using GorillaEscape.Gameplay.Input;

[RequireComponent(typeof(JunglePanel))]
public sealed class BaseballController : MonoBehaviour, IJungleScreen
{
    private enum PlayState { Ready, Pitching, HitFlight, Result, GameOver }

    private MotionSession session;
    private PlayState state = PlayState.Ready;
    private GameObject ball, bat, pitcher;
    private readonly List<Transform> crowdMembers = new List<Transform>();
    private readonly List<Vector3> crowdRestPositions = new List<Vector3>();
    private Rigidbody ballBody;
    private Vector2 rightHand = new Vector2(.62f,.58f);
    private readonly HandVisualPrediction rightVisual = new HandVisualPrediction();
    private float pitchAt, stateUntil, hitDistance;
    private long inputRevision;
    private float swingSpeed, pitchStartedAt, swingAt = -10f;
    private const float PitchSpeed = 6.4f, ContactZ = -3.2f;
    private Vector3 pitchOrigin;
    private int pitches, strikes, hits, homeRuns, score;
    private int cachedPitches = -1, cachedHits = -1, cachedHomeRuns = -1, cachedScore = -1, cachedFinalScore = -1;
    private string cachedHudText = "", cachedFinalText = "";
    private string message = "PREPÁRATE";
    private GUIStyle titleStyle;
    private int lastScreenHeight;
    private AudioSource sfx;
    private AudioClip pitchTone, hitTone, homeRunTone, strikeTone;

    private void Awake()
    {
        session = MotionSession.Ensure();
        session.ConfigureForScene(SceneNavigator.Baseball);
        BuildField();
        sfx = JungleAudioBank.Attach2DSource(gameObject);
        pitchTone = JungleAudioBank.Pitch;
        hitTone = JungleAudioBank.BatHit;
        homeRunTone = JungleAudioBank.HomeRun;
        strikeTone = JungleAudioBank.Strike;
        SchedulePitch(4f);
    }

    private GameObject Primitive(PrimitiveType type, string name, Vector3 position, Vector3 scale, Color color, bool keepCollider = false) =>
        JungleStageBuilder.CreatePrimitive(type, name, position, scale, color, parent: transform, keepCollider: keepCollider, preferStandard: false);

    private void BuildField()
    {
        Camera camera=Camera.main; camera.transform.position=new Vector3(0,3.1f,-11f); camera.transform.LookAt(new Vector3(0,1.1f,3.5f)); camera.fieldOfView=48f; camera.backgroundColor=new Color(.25f,.65f,.9f);
        Primitive(PrimitiveType.Cube,"Campo",new Vector3(0,-.35f,4),new Vector3(22,.3f,25),new Color(.08f,.43f,.12f), keepCollider: true);
        Primitive(PrimitiveType.Cube,"Tierra",new Vector3(0,-.16f,1.5f),new Vector3(8,.04f,10),new Color(.48f,.25f,.08f)).transform.rotation=Quaternion.Euler(0,45,0);
        Primitive(PrimitiveType.Cube,"Plato",new Vector3(0,-.10f,-3.2f),new Vector3(.8f,.04f,.8f),Color.white).transform.rotation=Quaternion.Euler(0,45,0);
        JungleArenaEnvironment.Build(transform,-.2f,12f,28f);
        crowdMembers.Clear();
        for(int i=-3;i<=3;i++) {
            var spectator=new GorillaMascotRig();
            spectator.LoadMascot(transform,new Vector3(i*2.2f,-.2f,14f),Quaternion.Euler(0,180,0),1.5f,"Público gorila Meshy",runner:true);
            if(spectator.MascotObject!=null){crowdMembers.Add(spectator.MascotObject.transform);crowdRestPositions.Add(spectator.MascotObject.transform.localPosition);}
        }
        var pitcherRig=new GorillaMascotRig();
        pitcherRig.LoadMascot(transform,new Vector3(0,-.2f,6.4f),Quaternion.Euler(0,180,0),3.2f,"Lanzador gorila Meshy",runner:true);
        pitcher=pitcherRig.MascotObject;
        ball=Primitive(PrimitiveType.Sphere,"Pelota",new Vector3(0,1.5f,5.8f),Vector3.one*.32f,Color.white, keepCollider: true);
        ballBody=ball.AddComponent<Rigidbody>(); ballBody.useGravity=false; ballBody.isKinematic=true; ballBody.interpolation=RigidbodyInterpolation.Interpolate;
        bat=new GameObject("Bate de madera");bat.transform.SetParent(transform);bat.transform.position=new Vector3(1.15f,.8f,-3.1f);
        BatPart("Barril",.115f,1.35f,.3f,new Color(.65f,.38f,.14f));
        BatPart("Empuñadura",.05f,.75f,-.73f,new Color(.26f,.12f,.04f));
        BatPart("Tope",.075f,.06f,-1.1f,new Color(.65f,.38f,.14f));
    }

    private void BatPart(string name,float radius,float length,float z,Color color)
    {
        var part=JungleStageBuilder.CreateLocalPrimitive(PrimitiveType.Cylinder,name,new Vector3(0,0,z),new Vector3(radius*2,length*.5f,radius*2),color,bat.transform);
        part.transform.localRotation=Quaternion.Euler(90,0,0);
    }

    private void SchedulePitch(float delay)
    {
        hitDistance=0; state=PlayState.Ready; pitchAt=Time.time+delay; message="PREPÁRATE";
        ballBody.isKinematic=true; ball.transform.position=new Vector3(0,1.5f,5.8f); ball.transform.localScale=Vector3.one*.32f;
    }

    private void Update()
    {
        session.PresentGame(state==PlayState.GameOver?"Finished":state==PlayState.Ready?"Ready":"Playing",score,Mathf.Clamp01(pitches/10f),distance:hitDistance>0?hitDistance:(float?)null);
        if(!session.IsInputReady){rightVisual.Reset();return;}
        if(session.TryConsumeFrame(out var frame))ReadSensors(frame);
        Vector2 visualRight = rightVisual.TryPosition(Time.unscaledTime, out float vx, out float vy) ? new Vector2(vx, vy) : rightHand;
        Vector3 handPosition = new Vector3(Mathf.Lerp(-2.45f,2.45f,visualRight.x),Mathf.Lerp(2.75f,.30f,visualRight.y),-3.1f);
        bat.transform.position = Vector3.Lerp(bat.transform.position, handPosition, 22f * Time.deltaTime);
        float swingPhase = Mathf.Clamp01((Time.time-swingAt)/.32f);
        float sweep = Mathf.Sin(swingPhase*Mathf.PI)*105f;
        bat.transform.rotation = Quaternion.Slerp(bat.transform.rotation, Quaternion.Euler(12f, -sweep, -18f), 1f-Mathf.Exp(-22f*Time.deltaTime));
        AnimateStageProps();
        if(state==PlayState.Ready && Time.time>=pitchAt) StartPitch();
        else if(state==PlayState.Pitching)
        {
            ball.transform.position = pitchOrigin + Vector3.back * (PitchSpeed*(Time.time-pitchStartedAt));
            float z=ball.transform.position.z;
            message = Mathf.Abs(z-ContactZ)<=PitchSpeed*.22f ? "¡AHORA! ¡BATEA!" : "SIGUE LA PELOTA";
            if(z < ContactZ-PitchSpeed*.28f) FinishPitch(false,0);
        }
        else if(state==PlayState.HitFlight)
        {
            if(Time.time>=stateUntil) FinishHit();
        }
        else if(state==PlayState.Result && Time.time>=stateUntil)
        {
            if(pitches>=10) state=PlayState.GameOver; else SchedulePitch(1.1f);
        }
    }

    private void StartPitch()
    {
        state=PlayState.Pitching; pitches++; message="¡BATEA!";
        ball.transform.position=new Vector3(UnityEngine.Random.Range(-.45f,.45f),UnityEngine.Random.Range(1.25f,1.85f),5.8f);
        pitchOrigin=ball.transform.position; pitchStartedAt=Time.time;
        if(pitcher!=null){pitcher.transform.rotation=Quaternion.Euler(0f,180f,0f);JungleStageBuilder.PulseSquash(pitcher,1.22f,.84f,22f,8f);}
        JungleStageBuilder.PulseSquash(ball,.75f,1.22f,28f,10f);
        if(sfx!=null&&pitchTone!=null)sfx.PlayOneShot(pitchTone,.55f);
    }

    private void AnimateStageProps()
    {
        float now=Time.time;
        if(pitcher!=null&&state==PlayState.Ready)
        {
            float rem=Mathf.Clamp01(1f-(pitchAt-now)/.85f);
            float windup=Mathf.Sin(rem*Mathf.PI)*18f;
            pitcher.transform.rotation=Quaternion.Euler(-windup,180f,Mathf.Sin(now*3.5f)*3f);
        }
        bool cheering=state==PlayState.HitFlight||(state==PlayState.Result&&hitDistance>70f);
        for(int i=0;i<crowdMembers.Count;i++)
        {
            Transform member=crowdMembers[i];
            if(member==null)continue;
            int idx=i-5;
            float hop=cheering?Mathf.Abs(Mathf.Sin(now*9.5f-idx*.55f))*(hitDistance>105f?.55f:.28f):Mathf.Sin(now*2.4f+i)*.04f;
            member.localPosition=crowdRestPositions[i]+Vector3.up*hop;
        }
    }

    private void ReadSensors(JungleFusedFrame frame)
    {
        inputRevision=frame.Revision;
        if(frame.Baseline){rightVisual.Reset();}
        rightHand=new Vector2(frame.Right.X,frame.Right.Y);
        if(frame.NewPose||frame.Baseline)rightVisual.Observe(rightHand.x,rightHand.y,frame.Right.VelocityX,frame.Right.VelocityY,Time.unscaledTime);
        swingSpeed=frame.Right.Speed;
        if(state==PlayState.Pitching&&(frame.Actions&JungleAction.Swing)!=0)TrySwing();
    }

    private void TrySwing()
    {
        if(state!=PlayState.Pitching||!session.TryAuthorize(JungleAction.Swing,inputRevision))return;
        float z=ball.transform.position.z;
        swingAt=Time.time;
        float errorSeconds=Mathf.Abs(z-ContactZ)/PitchSpeed;
        if(errorSeconds>.24f) { FinishPitch(false,0); message=z>ContactZ ? "MUY PRONTO · ESPERA AL PLATO" : "MUY TARDE"; return; }
        float timing=CompetitiveMechanics.BaseballTiming(z,ContactZ,PitchSpeed);
        float direction=Mathf.Clamp((rightHand.x-.5f)*2.4f,-1f,1f);
        hitDistance=CompetitiveMechanics.BaseballDistance(timing,swingSpeed);
        hits++; score += hitDistance>105f?4:hitDistance>72f?2:1; if(hitDistance>105f) homeRuns++;
        state=PlayState.HitFlight; message=hitDistance>105f?$"¡JONRÓN! · {hitDistance:F0} m":$"¡HIT! · {hitDistance:F0} m";
        if(sfx!=null)sfx.PlayOneShot(hitDistance>105f?homeRunTone:hitTone,.85f);
        JungleStageBuilder.PulseSquash(bat,.72f,1.26f,30f,10f);
        JungleStageBuilder.PulseSquash(ball,.68f,1.28f,28f,9f);
        JungleStageBuilder.ShakeCamera(hitDistance>105f?.16f:.085f,hitDistance>105f?.24f:.14f);
        ballBody.isKinematic=false; ballBody.useGravity=true; const float flightSeconds=2.7f;
        var horizontal=new Vector2(direction*.32f,1f).normalized*(hitDistance/flightSeconds);
        float vertical=CompetitiveMechanics.VerticalVelocityForLanding(ball.transform.position.y,-.04f,Physics.gravity.y,flightSeconds);
        ballBody.linearVelocity=new Vector3(horizontal.x,vertical,horizontal.y); stateUntil=Time.time+flightSeconds;
    }

    private void FinishHit() { ballBody.isKinematic=true; state=PlayState.Result; stateUntil=Time.time+1.5f; }
    private void FinishPitch(bool hit,float distance) { strikes++; message="STRIKE"; if(sfx!=null&&strikeTone!=null)sfx.PlayOneShot(strikeTone,.65f); state=PlayState.Result; stateUntil=Time.time+1.3f; }

    public void RenderUI()
    {
        JungleUITheme.Ensure();
        float w=Screen.width,h=Screen.height;
        if(titleStyle==null||lastScreenHeight!=Screen.height){lastScreenHeight=Screen.height;titleStyle=new GUIStyle(JungleUITheme.Title){fontSize=Mathf.RoundToInt(h*.045f),normal={textColor=new Color(1f,.96f,.86f)}};}
        GUIStyle title=titleStyle;
        int shownPitch = Mathf.Clamp(state==PlayState.Ready?pitches+1:pitches,1,10);
        if (shownPitch != cachedPitches || hits != cachedHits || homeRuns != cachedHomeRuns || score != cachedScore)
        {
            cachedPitches = shownPitch; cachedHits = hits; cachedHomeRuns = homeRuns; cachedScore = score;
            cachedHudText = $"LANZAMIENTO {shownPitch}/10    HITS {hits}    JONRONES {homeRuns}    PUNTOS {score}";
        }
        JungleUITheme.DrawPanel(new Rect(w*.12f,h*.03f,w*.76f,h*.16f)); JungleUITheme.DrawShadowedLabel(new Rect(w*.14f,h*.04f,w*.72f,h*.065f),"JUNGLE BASEBALL",JungleUITheme.HudTitle,3f);
        JungleUITheme.DrawShadowedLabel(new Rect(w*.15f,h*.105f,w*.70f,h*.06f),cachedHudText,JungleUITheme.HudScore,2f);
        float cue=Mathf.Clamp01((Time.time-pitchStartedAt)*PitchSpeed/(5.8f-ContactZ));
        if(state==PlayState.Pitching) {
            JungleUITheme.DrawSubPanel(new Rect(w*.30f,h*.77f,w*.40f,h*.055f));
            JungleRetainedUI.DrawTexture(new Rect(w*.30f+w*.40f*.84f,h*.77f,w*.40f*.16f,h*.055f),JungleUITheme.Gold);
            JungleRetainedUI.DrawTexture(new Rect(w*.30f+w*.40f*cue-4,h*.77f,8,h*.055f),JungleUITheme.Turquoise);
        }
        Rect calloutRect = new Rect(w*.34f,h*.22f,w*.32f,h*.075f);
        if(message.Contains("STRIKE")) JungleRetainedUI.DrawTexture(calloutRect,JungleUITheme.Red);
        else JungleUITheme.DrawPanel(calloutRect, selected: message.Contains("JON")||message.Contains("AHORA")||message.Contains("HIT"));
        JungleUITheme.DrawShadowedLabel(calloutRect,message,title,2.5f);
        if(state==PlayState.GameOver)
        {
            if (TournamentSession.IsActive)
            {
                TournamentSession.RecordRoundResult(score);
                Vector2 normCursor = new Vector2(0.5f, 0.5f);
                string handLabel = "MANO";
                bool handRaised = false, handClosed = false, forwardPush = false;
                if (session != null) session.TryCameraCursor(out normCursor, out handLabel, out handRaised, out handClosed, out forwardPush);
                TournamentSession.DrawTournamentOverlay(w, h, normCursor, handLabel, handRaised, handClosed, forwardPush, session != null && session.PhoneTouchTriggered);
                return;
            }
            if (score != cachedFinalScore) { cachedFinalScore = score; cachedFinalText = $"PARTIDA TERMINADA · {score} PUNTOS"; }
            JungleUITheme.DrawPanel(new Rect(w*.27f,h*.34f,w*.46f,h*.25f));
            JungleUITheme.DrawShadowedLabel(new Rect(w*.29f,h*.37f,w*.42f,h*.08f),cachedFinalText,title,2.5f);
            if(JungleRetainedUI.Button(new Rect(w*.38f,h*.49f,w*.24f,h*.07f),"OTRA PARTIDA",JungleUITheme.Button)){JungleAudioBank.PlayUiClick();session.ResetRound();pitches=strikes=hits=homeRuns=score=0;SchedulePitch(1f);}
        }
        if(JungleRetainedUI.Button(new Rect(w*.84f,h*.04f,w*.13f,h*.07f),"MENÚ",JungleUITheme.Button)){JungleAudioBank.PlayUiClick();SceneNavigator.Load(SceneNavigator.MainMenu);}
        JungleUITheme.DrawShadowedLabel(new Rect(w*.18f,h*.88f,w*.64f,h*.06f),"SIN CELULAR · ESPERA «AHORA» Y CRUZA LA MANO DERECHA DE LADO A LADO",JungleUITheme.HudBody,2f);
        JungleUITheme.DrawLeaves();
    }
}
