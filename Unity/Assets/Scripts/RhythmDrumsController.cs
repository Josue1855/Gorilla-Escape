using System;
using System.Collections.Generic;
using UnityEngine;
using GorillaEscape.Gameplay.Input;

[RequireComponent(typeof(JunglePanel))]
public sealed class RhythmDrumsController : MonoBehaviour, IJungleScreen
{
    private sealed class Note { public bool left, judged; public float time; }

    private MotionSession session;
    private readonly List<Note> notes=new List<Note>();
    private float startTime,leftFlash,rightFlash,calloutUntil;
    private int score,combo,bestCombo,misses;
    private int cachedScore=-1,cachedCombo=-1,cachedBest=-1,cachedMisses=-1,cachedFinalScore=-1;
    private string cachedHudText="",cachedFinalText="";
    private string callout="";
    private bool finished;
    private long inputRevision;
    private const float Beat=.70f;
    private GUIStyle titleStyle;
    private GUIStyle laneLabelStyle;
    private int lastScreenHeight;
    private GameObject leftDrumBody,rightDrumBody,leftDrumHead,rightDrumHead;
    private readonly List<Transform> stageLights=new List<Transform>();
    private readonly List<Mesh> drumMeshes = new List<Mesh>();
    private void OnDestroy(){foreach(var mesh in drumMeshes)if(mesh!=null)Destroy(mesh);}
    private AudioSource sfx;
    private AudioClip leftDrumClip,rightDrumClip,missClip;

    private void Awake()
    {
        session=MotionSession.Ensure();
        session.ConfigureForScene(SceneNavigator.Drums);
        sfx=JungleAudioBank.Attach2DSource(gameObject);
        leftDrumClip=JungleAudioBank.LeftDrum;
        rightDrumClip=JungleAudioBank.RightDrum;
        missClip=JungleAudioBank.MissDrum;
        BuildStage(); startTime=Time.time+4f; GenerateChart();
    }

    private void BuildStage()
    {
        Camera cam=Camera.main;cam.transform.position=new Vector3(0,3.5f,-9f);cam.transform.LookAt(new Vector3(0,1.1f,1f));cam.backgroundColor=new Color(.018f,.035f,.05f);
        JungleStageBuilder.CreatePrimitive(PrimitiveType.Cube,"Escenario",new Vector3(0,-.45f,2),new Vector3(14,.3f,15),new Color(.04f,.12f,.10f),parent:transform,preferStandard:false);
        leftDrumHead=CreateDrum("Tambor izquierdo",-1.7f,new Color(.05f,.65f,.9f),out leftDrumBody);rightDrumHead=CreateDrum("Tambor derecho",1.7f,new Color(1f,.35f,.08f),out rightDrumBody);
        JungleArenaEnvironment.Build(transform, -.3f, 7f, 20f, night:true);
        stageLights.Clear();
        for(int i=-5;i<=5;i++)
        {
            GameObject lightOrb=JungleStageBuilder.CreatePrimitive(PrimitiveType.Sphere,"Luz rítmica",new Vector3(i*1.25f,3.8f,5.5f),Vector3.one*.32f,i%2==0?new Color(.05f,.7f,1f):new Color(1f,.3f,.06f),parent:transform,preferStandard:false);
            stageLights.Add(lightOrb.transform);
        }
    }

    private GameObject CreateDrum(string name,float x,Color color,out GameObject body)
    {
        body=new GameObject(name);body.transform.SetParent(transform);body.transform.position=new Vector3(x,-.3f,-.2f);
        var profile=new Vector2[]{new(0,.40f),new(.12f,.44f),new(.38f,.29f),new(.68f,.30f),new(.92f,.53f),new(1.3f,.62f)};
        const int sides=24;
        var vertices=new Vector3[profile.Length*(sides+1)];var uv=new Vector2[vertices.Length];
        var triangles=new int[(profile.Length-1)*sides*6];int t=0;
        for(int row=0;row<profile.Length;row++)for(int i=0;i<=sides;i++) {
            float angle=i*Mathf.PI*2/sides;int n=row*(sides+1)+i;
            vertices[n]=new Vector3(Mathf.Cos(angle)*profile[row].y,profile[row].x,Mathf.Sin(angle)*profile[row].y);uv[n]=new Vector2(i/(float)sides,profile[row].x);
            if(row==profile.Length-1||i==sides)continue;
            triangles[t++]=n;triangles[t++]=n+sides+1;triangles[t++]=n+1;
            triangles[t++]=n+1;triangles[t++]=n+sides+1;triangles[t++]=n+sides+2;
        }
        var mesh=new Mesh{name="Cuerpo de djembe procedural",vertices=vertices,triangles=triangles,uv=uv};mesh.RecalculateNormals();mesh.RecalculateBounds();drumMeshes.Add(mesh);
        body.AddComponent<MeshFilter>().sharedMesh=mesh;body.AddComponent<MeshRenderer>().sharedMaterial=JungleStageBuilder.GetSharedMaterial(new Color(.38f,.17f,.055f),smoothness:.2f);
        for(int i=0;i<10;i++) {
            float angle=i*Mathf.PI*2/10;
            var rope=JungleStageBuilder.CreateLocalPrimitive(PrimitiveType.Cylinder,"Tensado de cuerda",new Vector3(Mathf.Cos(angle)*.49f,.95f,Mathf.Sin(angle)*.49f),new Vector3(.025f,.32f,.025f),new Color(.77f,.65f,.39f),body.transform);
            rope.transform.localRotation=Quaternion.Euler(Mathf.Sin(angle)*-15,0,Mathf.Cos(angle)*15);
        }
        return JungleStageBuilder.CreatePrimitive(PrimitiveType.Cylinder,"Parche",new Vector3(x,1.02f,-.2f),new Vector3(1.12f,.055f,1.12f),new Color(.92f,.82f,.58f),parent:transform,preferStandard:false);
    }

    private void GenerateChart()
    {
        for(int i=0;i<64;i++)
        {
            // Start with a predictable left/right pulse; introduce pairs only after practice.
            bool left=i%2==0;
            notes.Add(new Note{left=left,time=startTime+i*Beat});
            if(i>=32 && i%8==7)notes.Add(new Note{left=!left,time=startTime+i*Beat});
        }
    }

    private void Update()
    {
        session.PresentGame(finished?"Finished":Time.time<startTime?"Countdown":"Playing",score,Mathf.Clamp01((Time.time-startTime)/(65*Beat)),Mathf.Max(0,Time.time-startTime));
        if(!session.IsInputReady)return;
        if(session.TryConsumeFrame(out var frame))ReadSensors(frame);
        float now=Time.time;
        if(leftDrumHead!=null)leftDrumHead.transform.position=new Vector3(-1.7f,now<leftFlash?0.97f:1.02f,-.2f);
        if(rightDrumHead!=null)rightDrumHead.transform.position=new Vector3(1.7f,now<rightFlash?0.97f:1.02f,-.2f);
        float beatPhase=(now-startTime)/Beat;
        float beatPulse=1f+Mathf.Pow(Mathf.Max(0f,Mathf.Cos(beatPhase*Mathf.PI*2f)),4f)*(.18f+Mathf.Min(combo,15)*.01f);
        for(int i=0;i<stageLights.Count;i++)
        {
            Transform orb=stageLights[i];
            if(orb==null)continue;
            float wave=Mathf.Sin(now*4.2f+i*.55f)*.12f;
            orb.localPosition=new Vector3((i-5)*1.25f,3.8f+wave,5.5f);
            orb.localScale=Vector3.one*(.32f*beatPulse);
        }
        if(!finished)
        {
            foreach(Note note in notes)if(!note.judged&&now-note.time>CompetitiveMechanics.DrumHitWindow){note.judged=true;misses++;combo=0;callout="SE ESCAPÓ";calloutUntil=now+.45f;}
            if(now>startTime+65*Beat)finished=true;
        }
    }

    private void ReadSensors(JungleFusedFrame frame)
    {
        inputRevision=frame.Revision;
        if((frame.Actions&JungleAction.DrumLeft)!=0)TryHit(true);
        if((frame.Actions&JungleAction.DrumRight)!=0)TryHit(false);
    }

    private void TryHit(bool left)
    {
        if(finished||Time.time<startTime||!session.TryAuthorize(left?JungleAction.DrumLeft:JungleAction.DrumRight,inputRevision))return;
        if(sfx!=null)sfx.PlayOneShot(left?leftDrumClip:rightDrumClip,.75f);
        if(left)leftFlash=Time.time+.14f;else rightFlash=Time.time+.14f;
        Note best=null;float error=CompetitiveMechanics.DrumHitWindow;
        foreach(Note n in notes)if(!n.judged&&n.left==left&&Mathf.Abs(Time.time-n.time)<=error){best=n;error=Mathf.Abs(Time.time-n.time);}
        if(best==null){callout="¡DESTIEMPO!";calloutUntil=Time.time+.45f;return;}
        best.judged=true;combo++;bestCombo=Mathf.Max(bestCombo,combo);
        int gained=error<.075f?1000:error<.15f?650:350;score+=gained+combo*12;
        callout=(error<.075f?"¡PERFECTO! ":error<.15f?"¡BIEN! ":"¡OK! ")+(combo>=3?$"· COMBO x{combo}":$"+{gained}");
        calloutUntil=Time.time+.55f;
        JungleStageBuilder.PulseSquash(left?leftDrumBody:rightDrumBody,.76f,1.22f,28f,10f);
        JungleStageBuilder.PulseSquash(left?leftDrumHead:rightDrumHead,.65f,1.18f,30f,11f);
        if(error<.075f)JungleStageBuilder.ShakeCamera(.055f,.10f);
        if(left)leftFlash=Time.time+.14f;else rightFlash=Time.time+.14f;
    }

    public void RenderUI()
    {
        JungleUITheme.Ensure();
        float w=Screen.width,h=Screen.height;
        if(titleStyle==null||lastScreenHeight!=Screen.height){lastScreenHeight=Screen.height;titleStyle=new GUIStyle(JungleUITheme.Title){fontSize=Mathf.RoundToInt(h*.038f),normal={textColor=new Color(1f,.96f,.86f)}};}
        GUIStyle title=titleStyle;
        if(score!=cachedScore||combo!=cachedCombo||bestCombo!=cachedBest||misses!=cachedMisses)
        {
            cachedScore=score;cachedCombo=combo;cachedBest=bestCombo;cachedMisses=misses;
            cachedHudText=$"PUNTOS {score}    COMBO {combo}    MEJOR {bestCombo}    FALLOS {misses}";
        }
        JungleUITheme.DrawPanel(new Rect(w*.16f,h*.02f,w*.68f,h*.135f));JungleUITheme.DrawShadowedLabel(new Rect(w*.18f,h*.025f,w*.64f,h*.055f),"JUNGLE DRUMS",JungleUITheme.HudTitle,3f);JungleUITheme.DrawShadowedLabel(new Rect(w*.19f,h*.08f,w*.62f,h*.05f),cachedHudText,JungleUITheme.HudScore,2f);
        JungleUITheme.DrawShadowedLabel(new Rect(w*.12f,h*.90f,w*.76f,h*.055f),Time.time<startTime ? "SIN CELULAR · BAJA LA MANO DEL COLOR CUANDO LA NOTA TOQUE LA LÍNEA" : "IZQUIERDA AZUL · DERECHA NARANJA · SUBE ENTRE GOLPES",JungleUITheme.HudBody,2f);
        DrawLane(true,w*.25f,w,h,title);DrawLane(false,w*.62f,w,h,title);
        if(Time.time<calloutUntil&&!string.IsNullOrEmpty(callout))
        {
            Rect cRect = new Rect(w*.39f,h*.17f,w*.22f,h*.065f);
            if(callout.Contains("DESTIEMPO")||callout.Contains("ESCAP")) JungleRetainedUI.DrawTexture(cRect,JungleUITheme.Red);
            else JungleUITheme.DrawPanel(cRect, selected: true);
            JungleUITheme.DrawShadowedLabel(cRect,callout,title,2f);
        }
        if(finished)
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
            if(score!=cachedFinalScore){cachedFinalScore=score;cachedFinalText=$"CANCIÓN TERMINADA · {score}";}
            JungleUITheme.DrawPanel(new Rect(w*.28f,h*.31f,w*.44f,h*.25f));JungleUITheme.DrawShadowedLabel(new Rect(w*.30f,h*.35f,w*.40f,h*.08f),cachedFinalText,title,2.5f);if(JungleRetainedUI.Button(new Rect(w*.39f,h*.47f,w*.22f,h*.065f),"REPETIR",JungleUITheme.Button)){JungleAudioBank.PlayUiClick();SceneNavigator.Load(SceneNavigator.Drums);}
        }
        if(JungleRetainedUI.Button(new Rect(w*.85f,h*.04f,w*.12f,h*.06f),"MENÚ",JungleUITheme.Button)){JungleAudioBank.PlayUiClick();SceneNavigator.Load(SceneNavigator.MainMenu);}
        JungleUITheme.DrawLeaves();
    }

    private void DrawLane(bool left,float x,float w,float h,GUIStyle style)
    {
        Texture2D accent = left ? JungleUITheme.Turquoise : JungleUITheme.Orange;
        float laneW=w*.13f,targetY=h*.76f;
        if(laneLabelStyle==null)laneLabelStyle=new GUIStyle(style){wordWrap=false};
        laneLabelStyle.fontSize=Mathf.Min(style.fontSize,Mathf.RoundToInt(laneW*.13f));JungleUITheme.DrawSubPanel(new Rect(x, h*.16f,laneW,h*.66f));JungleRetainedUI.DrawTexture(new Rect(x+4f,targetY,laneW-8f,h*.025f),accent);
        foreach(Note n in notes)if(!n.judged&&n.left==left){float seconds=n.time-Time.time;if(seconds<2.6f&&seconds>-.3f){float y=targetY-seconds*(h*.23f);JungleRetainedUI.DrawTexture(new Rect(x+laneW*.18f,y-h*.0225f,laneW*.64f,h*.045f),accent);}}
        bool flash=Time.time<(left?leftFlash:rightFlash);JungleUITheme.DrawPanel(new Rect(x-laneW*.05f,h*.80f,laneW*1.1f,h*.08f), selected: flash);JungleUITheme.DrawShadowedLabel(new Rect(x,h*.80f,laneW,h*.08f),left?"IZQUIERDA":"DERECHA",laneLabelStyle,2f);
    }
}
