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
    private Rigidbody ballBody;
    private Vector2 rightHand = new Vector2(.62f,.58f);
    private readonly HandVisualPrediction rightVisual = new HandVisualPrediction();
    private float phonePower, pitchAt, stateUntil, hitDistance;
    private long inputRevision;
    private bool phoneCalibrated;
    private float centerBeta, centerGamma;
    private int lastRecenter;
    private Quaternion batOrientation = Quaternion.Euler(15f, 0f, 0f);
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
        BuildField();
        sfx = JungleAudioBank.Attach2DSource(gameObject);
        pitchTone = JungleAudioBank.Pitch;
        hitTone = JungleAudioBank.BatHit;
        homeRunTone = JungleAudioBank.HomeRun;
        strikeTone = JungleAudioBank.Strike;
        SchedulePitch(1.2f);
    }

    private GameObject Primitive(PrimitiveType type, string name, Vector3 position, Vector3 scale, Color color, bool keepCollider = false) =>
        JungleStageBuilder.CreatePrimitive(type, name, position, scale, color, parent: transform, keepCollider: keepCollider, preferStandard: false);

    private void BuildField()
    {
        Camera camera=Camera.main; camera.transform.position=new Vector3(0,3.1f,-11f); camera.transform.LookAt(new Vector3(0,1.1f,3.5f)); camera.fieldOfView=48f; camera.backgroundColor=new Color(.25f,.65f,.9f);
        Primitive(PrimitiveType.Cube,"Campo",new Vector3(0,-.35f,4),new Vector3(22,.3f,25),new Color(.08f,.43f,.12f), keepCollider: true);
        Primitive(PrimitiveType.Cube,"Tierra",new Vector3(0,-.16f,1.5f),new Vector3(8,.04f,10),new Color(.48f,.25f,.08f)).transform.rotation=Quaternion.Euler(0,45,0);
        Primitive(PrimitiveType.Cube,"Plato",new Vector3(0,-.10f,-3.2f),new Vector3(.8f,.04f,.8f),Color.white).transform.rotation=Quaternion.Euler(0,45,0);
        crowdMembers.Clear();
        for(int i=-5;i<=5;i++) crowdMembers.Add(Primitive(PrimitiveType.Capsule,"Público",new Vector3(i*1.7f,1.1f,13f+Mathf.Abs(i%2)),new Vector3(.65f,1.1f,.65f),new Color(.04f,.08f,.06f)).transform);
        pitcher=Primitive(PrimitiveType.Capsule,"Pitcher gorila provisional",new Vector3(0,1.2f,6.4f),new Vector3(1.6f,2.1f,1.2f),new Color(.16f,.17f,.18f));
        ball=Primitive(PrimitiveType.Sphere,"Pelota",new Vector3(0,1.5f,5.8f),Vector3.one*.32f,Color.white, keepCollider: true);
        ballBody=ball.AddComponent<Rigidbody>(); ballBody.useGravity=false; ballBody.isKinematic=true; ballBody.interpolation=RigidbodyInterpolation.Interpolate;
        bat=Primitive(PrimitiveType.Cube,"Bate provisional",new Vector3(1.15f,.8f,-3.1f),new Vector3(.18f,.18f,2.2f),new Color(.55f,.27f,.07f));
    }

    private void SchedulePitch(float delay)
    {
        state=PlayState.Ready; pitchAt=Time.time+delay; message="PREPÁRATE";
        ballBody.isKinematic=true; ball.transform.position=new Vector3(0,1.5f,5.8f); ball.transform.localScale=Vector3.one*.32f;
    }

    private void Update()
    {
        session.PresentGame(state==PlayState.GameOver?"Finished":state==PlayState.Ready?"Ready":"Playing",score,Mathf.Clamp01(pitches/10f),distance:hitDistance>0?hitDistance:(float?)null);
        if(!session.IsInputReady){phonePower=0;phoneCalibrated=false;rightVisual.Reset();return;}
        if(session.TryConsumeFrame(out var frame))ReadSensors(frame);
        Vector2 visualRight = rightVisual.TryPosition(Time.unscaledTime, out float vx, out float vy) ? new Vector2(vx, vy) : rightHand;
        Vector3 handPosition = new Vector3(Mathf.Lerp(-2.45f,2.45f,visualRight.x),Mathf.Lerp(2.75f,.30f,visualRight.y),-3.1f);
        bat.transform.position = Vector3.Lerp(bat.transform.position, handPosition, 22f * Time.deltaTime);
        Quaternion visualOrientation=batOrientation;
        if(phoneCalibrated&&session.TryPhoneVisualEstimate(out var visual)&&visual.Orientation!=null&&visual.Orientation.hasBeta&&visual.Orientation.hasGamma)
        {
            float visualUp=Mathf.Clamp(Mathf.DeltaAngle(centerBeta,visual.Orientation.beta),-70f,70f);
            float visualSide=Mathf.Clamp(Mathf.DeltaAngle(centerGamma,visual.Orientation.gamma),-95f,95f);
            visualOrientation=Quaternion.Euler(12f-visualUp*.95f,visualSide*.90f,-visualSide*.28f);
        }
        bat.transform.rotation = Quaternion.Slerp(bat.transform.rotation, visualOrientation, 22f * Time.deltaTime);
        AnimateStageProps();
        if(state==PlayState.Ready && Time.time>=pitchAt) StartPitch();
        else if(state==PlayState.Pitching)
        {
            ball.transform.position += new Vector3(0,-.05f,-8.2f)*Time.deltaTime;
            float z=ball.transform.position.z;
            if(z<=1.1f&&z>=-3.6f&&message=="¡BATEA!")message="¡AHORA! ¡BATEA!";
            if(z < -4.2f) FinishPitch(false,0);
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
        if(pitcher!=null){pitcher.transform.rotation=Quaternion.identity;JungleStageBuilder.PulseSquash(pitcher,1.22f,.84f,22f,8f);}
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
            pitcher.transform.rotation=Quaternion.Euler(-windup,0f,Mathf.Sin(now*3.5f)*3f);
        }
        bool cheering=state==PlayState.HitFlight||(state==PlayState.Result&&hitDistance>70f);
        for(int i=0;i<crowdMembers.Count;i++)
        {
            Transform member=crowdMembers[i];
            if(member==null)continue;
            int idx=i-5;
            float hop=cheering?Mathf.Abs(Mathf.Sin(now*9.5f-idx*.55f))*(hitDistance>105f?.55f:.28f):Mathf.Sin(now*2.4f+i)*.04f;
            member.localPosition=new Vector3(idx*1.7f,1.1f+hop,13f+Mathf.Abs(idx%2));
        }
    }

    private void ReadSensors(JungleFusedFrame frame)
    {
        inputRevision=frame.Revision;
        if(frame.Baseline){phoneCalibrated=false;rightVisual.Reset();}
        rightHand=new Vector2(frame.Right.X,frame.Right.Y);
        if(frame.NewPose||frame.Baseline)rightVisual.Observe(rightHand.x,rightHand.y,frame.Right.VelocityX,frame.Right.VelocityY,Time.unscaledTime);
        phonePower=frame.PhonePower;
        var motion=frame.Motion;
        if(!phoneCalibrated||motion.recenter!=lastRecenter)
        {centerBeta=motion.beta;centerGamma=motion.gamma;lastRecenter=motion.recenter;phoneCalibrated=true;}
        float tiltUp=Mathf.Clamp(Mathf.DeltaAngle(centerBeta,motion.beta),-70f,70f);
        float tiltSide=Mathf.Clamp(Mathf.DeltaAngle(centerGamma,motion.gamma),-95f,95f);
        batOrientation=Quaternion.Euler(12f-tiltUp*.95f,tiltSide*.90f,-tiltSide*.28f);
        if(state==PlayState.Pitching&&(frame.Actions&JungleAction.Swing)!=0)TrySwing();
    }

    private void TrySwing()
    {
        if(state!=PlayState.Pitching||!session.TryAuthorize(JungleAction.Swing,inputRevision))return;
        float z=ball.transform.position.z;
        if(z>1.1f || z<-3.6f) { message="SWING TEMPRANO"; return; }
        float timing=1f-Mathf.Clamp01(Mathf.Abs(z+1.4f)/2.5f);
        float direction=Mathf.Clamp((rightHand.x-.5f)*2.4f,-1f,1f);
        float strength=Mathf.Clamp((phonePower-210f)/260f,.25f,1f);
        hitDistance=25f+timing*55f+strength*55f;
        hits++; score += hitDistance>105f?4:hitDistance>72f?2:1; if(hitDistance>105f) homeRuns++;
        state=PlayState.HitFlight; message=hitDistance>105f?$"¡JONRÓN! · {hitDistance:F0} m":$"¡HIT! · {hitDistance:F0} m";
        if(sfx!=null)sfx.PlayOneShot(hitDistance>105f?homeRunTone:hitTone,.85f);
        JungleStageBuilder.PulseSquash(bat,.72f,1.26f,30f,10f);
        JungleStageBuilder.PulseSquash(ball,.68f,1.28f,28f,9f);
        JungleStageBuilder.ShakeCamera(hitDistance>105f?.16f:.085f,hitDistance>105f?.24f:.14f);
        ballBody.isKinematic=false; ballBody.useGravity=true; ballBody.linearVelocity=new Vector3(direction*8f,8f+strength*7f,13f+strength*12f); stateUntil=Time.time+2.7f;
    }

    private void FinishHit() { ballBody.isKinematic=true; state=PlayState.Result; stateUntil=Time.time+1.5f; }
    private void FinishPitch(bool hit,float distance) { strikes++; message="STRIKE"; if(sfx!=null&&strikeTone!=null)sfx.PlayOneShot(strikeTone,.65f); state=PlayState.Result; stateUntil=Time.time+1.3f; }

    public void RenderUI()
    {
        JungleUITheme.Ensure();
        float w=Screen.width,h=Screen.height;
        if(titleStyle==null||lastScreenHeight!=Screen.height){lastScreenHeight=Screen.height;titleStyle=new GUIStyle(JungleUITheme.Title){fontSize=Mathf.RoundToInt(h*.045f),normal={textColor=new Color(1f,.96f,.86f)}};}
        GUIStyle title=titleStyle;
        int shownPitch = Mathf.Min(pitches + 1, 10);
        if (shownPitch != cachedPitches || hits != cachedHits || homeRuns != cachedHomeRuns || score != cachedScore)
        {
            cachedPitches = shownPitch; cachedHits = hits; cachedHomeRuns = homeRuns; cachedScore = score;
            cachedHudText = $"LANZAMIENTO {shownPitch}/10    HITS {hits}    JONRONES {homeRuns}    PUNTOS {score}";
        }
        JungleUITheme.DrawPanel(new Rect(w*.12f,h*.03f,w*.76f,h*.16f)); JungleUITheme.DrawShadowedLabel(new Rect(w*.14f,h*.04f,w*.72f,h*.065f),"JUNGLE BASEBALL",JungleUITheme.HudTitle,3f);
        JungleUITheme.DrawShadowedLabel(new Rect(w*.15f,h*.105f,w*.70f,h*.06f),cachedHudText,JungleUITheme.HudScore,2f);
        Rect calloutRect = new Rect(w*.34f,h*.22f,w*.32f,h*.075f);
        if(message.Contains("STRIKE")) JungleRetainedUI.DrawTexture(calloutRect,JungleUITheme.Red);
        else JungleUITheme.DrawPanel(calloutRect, selected: message.Contains("JON")||message.Contains("AHORA")||message.Contains("HIT"));
        JungleUITheme.DrawShadowedLabel(calloutRect,message,title,2.5f);
        if(state==PlayState.GameOver)
        {
            if (score != cachedFinalScore) { cachedFinalScore = score; cachedFinalText = $"PARTIDA TERMINADA · {score} PUNTOS"; }
            JungleUITheme.DrawPanel(new Rect(w*.27f,h*.34f,w*.46f,h*.25f));
            JungleUITheme.DrawShadowedLabel(new Rect(w*.29f,h*.37f,w*.42f,h*.08f),cachedFinalText,title,2.5f);
            if(JungleRetainedUI.Button(new Rect(w*.38f,h*.49f,w*.24f,h*.07f),"OTRA PARTIDA",JungleUITheme.Button)){JungleAudioBank.PlayUiClick();session.ResetRound();pitches=strikes=hits=homeRuns=score=0;SchedulePitch(1f);}
        }
        if(JungleRetainedUI.Button(new Rect(w*.84f,h*.04f,w*.13f,h*.07f),"MENÚ",JungleUITheme.Button)){JungleAudioBank.PlayUiClick();SceneNavigator.Load(SceneNavigator.MainMenu);}
        JungleUITheme.DrawShadowedLabel(new Rect(w*.18f,h*.88f,w*.64f,h*.06f),"SIGUE LA PELOTA · HAZ EL SWING CON EL CELULAR Y LA MANO DERECHA",JungleUITheme.HudBody,2f);
        JungleUITheme.DrawLeaves();
    }
}
