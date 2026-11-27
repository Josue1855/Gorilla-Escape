using System;
using UnityEngine;

public sealed class WhackAMoleController : MonoBehaviour
{
    [Serializable] private struct PosePacket { public bool rightDetected; public float rightWristX,rightWristY; }
    [Serializable] private struct MotionPacket { public float beta,gamma,ax,ay,az,rx,ry,rz; public int recenter; }
    private enum State { Intro, Countdown, Playing, Results }
    private readonly Vector2[] slots={new(.385f,.625f),new(.5f,.635f),new(.615f,.625f),new(.38f,.485f),new(.5f,.495f),new(.62f,.485f),new(.372f,.34f),new(.5f,.35f),new(.628f,.34f)};
    private AudioSource sfx;
    private int lastCountdownBeep;
    private MotionSession session; private State state; private GameObject table,hammer,head,hitHead; private Transform tableVisual,hammerPivot;
    private GameObject tablePrefab,headPrefab,hitPrefab,hammerPrefab; private Material tableMat,headMat,hitMat,hammerMat;
    private Vector2 cursorViewport=new(.5f,.5f),previousPose,activeSlot; private float downSpeed,phonePower,stateAt,endAt,nextHeadAt,headAt,headUntil,hitUntil,lastHit,lastPoseAt,centerBeta,centerGamma; private int lastRecenter; private bool phoneCalibrated;
    private int score,hits,misses,combo,bestCombo; private string message=""; private Texture2D dark,gold,red,turquoise,white; private GUIStyle title,body,scoreStyle,big;
    private Vector3 tableRotation=new(252,0,0),headRotation=new(255,180,0),hammerRotation=new(343,260,27); private bool adjustMode; private int adjustTarget; private float adjustSavedUntil; private const string AdjustKey="JungleParty.WhackAdjust.";
    private readonly GameObject[] positionPreviews = new GameObject[9];
    private readonly Vector3[] headOffsets =
    {
        new(.35f, .05f, 3.15f), new(-.05f, 0f, 3.30f), new(-.35f, .05f, 3.15f),
        new(.55f, .55f, 1.35f), new(.10f, .45f, 1.30f), new(-.50f, .50f, 1.25f),
        new(.60f, 1.10f, -.45f), new(0f, 1f, -.50f), new(-.65f, 1.05f, -.55f)
    };
    private Vector3 backgroundOffset;
    private Transform background;
    private int positionTarget, activeHeadIndex;
    private float adjustmentStarted;
    private Vector2 HandViewport=>cursorViewport;
    private Vector3 HeadPosition(int index) => World(slots[index], 10.45f) + headOffsets[index];
    private Vector3 ActiveHeadPosition => HeadPosition(activeHeadIndex);
    private void FitBackground()
    {
        if (background == null) return;
        Camera camera = Camera.main;
        const float distance = 24f;
        background.position = World(new Vector2(.5f, .5f), distance) + backgroundOffset;
        background.rotation = camera.transform.rotation;
        Texture texture = background.GetComponent<Renderer>().sharedMaterial.mainTexture;
        float aspect = texture == null ? 16f / 9f : texture.width / (float)texture.height;
        float height = 2f * distance * Mathf.Tan(camera.fieldOfView * .5f * Mathf.Deg2Rad);
        float width = Mathf.Max(height * camera.aspect, height * aspect);
        background.localScale = new Vector3(width, width / aspect, 1);
    }

    private void Awake(){sfx=JungleAudioBank.Attach2DSource(gameObject);session=MotionSession.Ensure();session.ConfigureForScene(SceneNavigator.WhackAMole);LoadAdjustments();LoadAssets();BuildScene();previousPose=new Vector2(.5f,.5f);stateAt=Time.time;}
    private Material Mat(Texture2D t,string n){var m=new Material(Shader.Find("Universal Render Pipeline/Lit")??Shader.Find("Standard")){name=n,mainTexture=t,color=Color.white};return m;}
    private void LoadAssets(){const string r="Jungle/NewModels/WhackAMole/",tr=r+"Meshy_AI_model_20261026_texture_fbx/",hr=r+"Meshy_AI_Cheeky_Monkey_20261026_texture_fbx/",ir=r+"Meshy_AI_Dizzy_Monkey_20261026_texture_fbx/",mr=r+"Meshy_AI_Wooden_War_Drum_20261026_texture_fbx/";tablePrefab=Resources.Load<GameObject>(tr+"Meshy_AI_model_20261026_texture");headPrefab=Resources.Load<GameObject>(hr+"Meshy_AI_Cheeky_Monkey_20261026_texture");hitPrefab=Resources.Load<GameObject>(ir+"Meshy_AI_Dizzy_Monkey_20261026_texture");hammerPrefab=Resources.Load<GameObject>(mr+"Meshy_AI_Wooden_War_Drum_20261026_texture");tableMat=Mat(Resources.Load<Texture2D>(tr+"Meshy_AI_model_20261026_texture"),"Mesa");headMat=Mat(Resources.Load<Texture2D>(hr+"Meshy_AI_Cheeky_Monkey_20261026_texture"),"Cabeza");hitMat=Mat(Resources.Load<Texture2D>(ir+"Meshy_AI_Dizzy_Monkey_20261026_texture"),"Golpeado");hammerMat=Mat(Resources.Load<Texture2D>(mr+"Meshy_AI_Wooden_War_Drum_20261026_texture"),"Mazo");}

    private void BuildScene(){Camera c=Camera.main;c.orthographic=false;c.fieldOfView=48;c.transform.position=new(0,3.15f,-11.2f);c.transform.LookAt(new Vector3(0,-.65f,.25f));c.backgroundColor=new(.08f,.18f,.13f);var bg=GameObject.CreatePrimitive(PrimitiveType.Quad);bg.name="01 Fondo";background=bg.transform;bg.transform.position=new(0,.25f,7.2f);bg.transform.localScale=new(34,22.5f,1);bg.GetComponent<Collider>().enabled=false;var bm=new Material(Shader.Find("Unlit/Texture")){mainTexture=Resources.Load<Texture2D>("Jungle/NewModels/WhackAMole/WhackAMoleBackground")};bg.GetComponent<Renderer>().sharedMaterial=bm;FitBackground();
        table=Model(tablePrefab,tableMat,"02 Mesa cara superior",new Vector3(0,-.75f,.28f),9.35f,true,Quaternion.Euler(tableRotation));tableVisual=table.transform.Find("Pivote visual/Modelo");hammer=Model(hammerPrefab,hammerMat,"04 Mazo",World(HandViewport,7.7f),2.35f,false,Quaternion.Euler(hammerRotation));hammerPivot=hammer.transform.Find("Pivote visual");var l=FindAnyObjectByType<Light>();if(l!=null){l.intensity=1.5f;l.transform.rotation=Quaternion.Euler(42,-25,0);}}
    private Vector3 World(Vector2 v,float d)=>Camera.main.ViewportToWorldPoint(new Vector3(v.x,v.y,d));
    private GameObject Model(GameObject prefab,Material mat,string n,Vector3 pos,float size,bool width,Quaternion rot){var root=new GameObject(n);root.transform.SetParent(transform);root.transform.position=pos;var pivot=new GameObject("Pivote visual");pivot.transform.SetParent(root.transform,false);if(prefab==null){Debug.LogError("Falta "+n);return root;}var model=Instantiate(prefab,pivot.transform);model.name="Modelo";model.transform.SetLocalPositionAndRotation(Vector3.zero,rot);model.transform.localScale=Vector3.one;foreach(var c in model.GetComponentsInChildren<Collider>(true))c.enabled=false;var rs=model.GetComponentsInChildren<Renderer>(true);if(rs.Length==0)return root;Bounds b=BoundsOf(rs);float measure=width?b.size.x:Mathf.Max(b.size.x,b.size.y);model.transform.localScale=Vector3.one*(size/Mathf.Max(.01f,measure));b=BoundsOf(rs);model.transform.position+=pos-b.center;foreach(var r in rs){r.enabled=true;r.gameObject.layer=0;var ms=r.sharedMaterials;for(int i=0;i<ms.Length;i++)ms[i]=mat;r.sharedMaterials=ms;}return root;}
    private static Bounds BoundsOf(Renderer[] rs){Bounds b=rs[0].bounds;for(int i=1;i<rs.Length;i++)b.Encapsulate(rs[i].bounds);return b;}

    private void Update(){if(PartyMode.Blocking)return;if(JungleStory.BlocksGame)return;FitBackground();if(adjustMode){EnsureAdjustmentPreview();ApplyAdjustmentPreview();return;}ReadInput();MoveHammer();if(state==State.Countdown){float elapsed=Time.time-stateAt;if(elapsed>=3)StartRound();else PlayCountdownTick(elapsed);}else if(state==State.Playing){if(Time.time>=endAt)Finish();else UpdateHead();}}
    private void PlayCountdownTick(float elapsed)
    {
        int count = Mathf.Clamp(3 - Mathf.FloorToInt(elapsed), 1, 3);
        if (count == lastCountdownBeep) return;
        lastCountdownBeep = count;
        sfx.PlayOneShot(JungleAudioBank.CountdownTick, .6f);
    }

    private static float DeadZone(float value,float radius){float a=Mathf.Abs(value);return a<=radius?0:Mathf.Sign(value)*(a-radius);}
    private void ReadInput(){if(session.TryConsumePose(out string p)){var pose=JsonUtility.FromJson<PosePacket>(p);if(pose.rightDetected){Vector2 next=new(pose.rightWristX,pose.rightWristY);float dt=Mathf.Max(.01f,Time.deltaTime);downSpeed=Mathf.Lerp(downSpeed,(next.y-previousPose.y)/dt,.4f);previousPose=next;lastPoseAt=Time.time;Vector2 cameraTarget=new(next.x,1f-next.y);cursorViewport=Vector2.Lerp(cursorViewport,cameraTarget,Mathf.Clamp01(14f*Time.deltaTime));}}if(session.TryConsumeMotion(out string m)){var q=JsonUtility.FromJson<MotionPacket>(m);phonePower=Mathf.Max(0,(new Vector3(q.ax,q.ay,q.az).magnitude-9.81f)*18f)+new Vector3(q.rx,q.ry,q.rz).magnitude;if(!phoneCalibrated||q.recenter!=lastRecenter){centerBeta=q.beta;centerGamma=q.gamma;lastRecenter=q.recenter;phoneCalibrated=true;}if(Time.time-lastPoseAt>.3f){float side=DeadZone(Mathf.DeltaAngle(centerGamma,q.gamma),2.2f);float vertical=DeadZone(Mathf.DeltaAngle(centerBeta,q.beta),2.2f);Vector2 phoneTarget=new(Mathf.Clamp01(.5f+side/70f),Mathf.Clamp01(.5f-vertical/56f));cursorViewport=Vector2.Lerp(cursorViewport,phoneTarget,Mathf.Clamp01(9f*Time.deltaTime));}}}
    private void MoveHammer(){if(hammer==null)return;hammer.transform.position=Vector3.Lerp(hammer.transform.position,World(HandViewport,7.7f),22*Time.deltaTime);float s=Mathf.Clamp01(Mathf.Max(downSpeed/2.2f,phonePower/260));if(hammerPivot!=null)hammerPivot.localRotation=Quaternion.Slerp(hammerPivot.localRotation,Quaternion.Euler(0,0,Mathf.Lerp(0,68,s)),20*Time.deltaTime);if(state==State.Playing&&head!=null&&Time.time-lastHit>.22f&&(downSpeed>.72f||phonePower>235))TryHit();}
    private void SpawnHead(){sfx.PlayOneShot(JungleAudioBank.WhackPop,.55f);activeHeadIndex=UnityEngine.Random.Range(0,slots.Length);activeSlot=Camera.main.WorldToViewportPoint(ActiveHeadPosition);head=Model(headPrefab,headMat,"03 Cabeza activa",ActiveHeadPosition + Vector3.down * .65f,1.42f,false,Quaternion.Euler(headRotation));headAt=Time.time;headUntil=Time.time+1.25f;message="¡AHORA!";}
    private void UpdateHead(){if(hitHead!=null&&Time.time>=hitUntil){Destroy(hitHead);hitHead=null;nextHeadAt=Time.time+UnityEngine.Random.Range(.18f,.48f);}if(head==null&&hitHead==null&&Time.time>=nextHeadAt)SpawnHead();if(head==null)return;float age=Time.time-headAt,remaining=headUntil-Time.time,pop=Mathf.SmoothStep(0,1,Mathf.Clamp01(age/.18f));if(remaining<.18f)pop=Mathf.SmoothStep(0,1,Mathf.Clamp01(remaining/.18f));head.transform.position=ActiveHeadPosition + Vector3.down * Mathf.Lerp(.65f,0,pop);if(Time.time>=headUntil){Destroy(head);head=null;misses++;combo=0;message="¡SE ESCAPÓ!";nextHeadAt=Time.time+.28f;}}
    private void TryHit(){if(Time.time-headAt<.14f||Vector2.Distance(HandViewport,activeSlot)>.105f)return;sfx.PlayOneShot(JungleAudioBank.WhackHit,.75f);lastHit=Time.time;Destroy(head);head=null;hitHead=Model(hitPrefab,hitMat,"03 Cabeza golpeada",ActiveHeadPosition - Camera.main.transform.forward * .1f,1.52f,false,Quaternion.Euler(headRotation+new Vector3(0,0,UnityEngine.Random.Range(-7,7))));hitUntil=Time.time+.42f;hits++;combo++;bestCombo=Mathf.Max(bestCombo,combo);int gain=100+Mathf.Min(combo,12)*20;score+=gain;message=combo>=4?$"¡COMBO x{combo}!":$"+{gain}";}
    internal void Begin(){GameResultRecorder.Reset();lastCountdownBeep=0;score=hits=misses=combo=bestCombo=0;phoneCalibrated=false;cursorViewport=new Vector2(.5f,.5f);state=State.Countdown;stateAt=Time.time;}
    private void StartRound(){sfx.PlayOneShot(JungleAudioBank.CountdownGo,.7f);state=State.Playing;endAt=Time.time+45;nextHeadAt=Time.time+.35f;message="¡A JUGAR!";}
    private void Finish(){if(head!=null)Destroy(head);if(hitHead!=null)Destroy(hitHead);head=hitHead=null;state=State.Results;message="¡TIEMPO!";JungleStory.Finish(SceneNavigator.WhackAMole,hits>=10,$"{hits} golpes");}

    private void LoadAdjustments(){for(int i=0;i<headOffsets.Length;i++)if(PlayerPrefs.HasKey(AdjustKey+"slot"+i+"X"))headOffsets[i]=LoadVector("slot"+i);backgroundOffset=LoadVector("background");if(!PlayerPrefs.HasKey(AdjustKey+"tableX"))return;tableRotation=LoadVector("table");headRotation=LoadVector("head");hammerRotation=LoadVector("hammer");}
    private Vector3 LoadVector(string n)=>new(PlayerPrefs.GetFloat(AdjustKey+n+"X"),PlayerPrefs.GetFloat(AdjustKey+n+"Y"),PlayerPrefs.GetFloat(AdjustKey+n+"Z"));
    private void SaveVector(string n,Vector3 v){PlayerPrefs.SetFloat(AdjustKey+n+"X",v.x);PlayerPrefs.SetFloat(AdjustKey+n+"Y",v.y);PlayerPrefs.SetFloat(AdjustKey+n+"Z",v.z);}
    private void SaveAdjustments(){for(int i=0;i<headOffsets.Length;i++)SaveVector("slot"+i,headOffsets[i]);SaveVector("background",backgroundOffset);SaveVector("table",tableRotation);SaveVector("head",headRotation);SaveVector("hammer",hammerRotation);PlayerPrefs.Save();adjustSavedUntil=Time.unscaledTime+2;}
    private void ResetAdjustments(){tableRotation=new Vector3(252,0,0);headRotation=new Vector3(255,180,0);hammerRotation=new Vector3(343,260,27);ApplyAdjustmentPreview();}
    private void EnsureAdjustmentPreview()
    {
        for (int i = 0; i < positionPreviews.Length; i++)
            if (positionPreviews[i] == null)
                positionPreviews[i] = Model(headPrefab, headMat, "Cabeza " + (i + 1) + " - posicion visible",
                    HeadPosition(i), 1.42f, false, Quaternion.Euler(headRotation));
    }
    private void ApplyAdjustmentPreview()
    {
        if (tableVisual != null) tableVisual.localRotation = Quaternion.Euler(tableRotation);
        Transform hammerModel = hammer.transform.Find("Pivote visual/Modelo");
        if (hammerModel != null) hammerModel.localRotation = Quaternion.Euler(hammerRotation);
        for (int i = 0; i < positionPreviews.Length; i++)
        {
            if (positionPreviews[i] == null) continue;
            positionPreviews[i].transform.position = HeadPosition(i);
            Transform model = positionPreviews[i].transform.Find("Pivote visual/Modelo");
            if (model != null) model.localRotation = Quaternion.Euler(headRotation);
        }
        FitBackground();
    }
    private void ToggleAdjustment()
    {
        adjustMode = !adjustMode;
        if (adjustMode)
        {
            adjustmentStarted = Time.time;
            if (head != null) Destroy(head);
            if (hitHead != null) Destroy(hitHead);
            head = hitHead = null;
        }
        else
        {
            float paused = Time.time - adjustmentStarted;
            stateAt += paused;
            endAt += paused;
            nextHeadAt = Time.time + .35f;
            foreach (GameObject preview in positionPreviews)
                if (preview != null) Destroy(preview);
        }
    }
    private void PositionPanel(float w, float h)
    {
        float x = w * .71f, y = h * .145f, pw = w * .275f, ph = h * .75f;
        JungleUITheme.DrawPanel(new Rect(x, y, pw, ph));
        GUI.Label(new Rect(x, y, pw, ph * .09f), "POSICIONES", scoreStyle);
        for (int i = 0; i < 9; i++)
        {
            int column = i % 3, row = i / 3;
            if (GUI.Button(new Rect(x + pw * (.03f + column * .32f), y + ph * (.1f + row * .075f),
                pw * .3f, ph * .065f), (positionTarget == i ? "> " : "") + "Cabeza " + (i + 1), JungleUITheme.Button))
                positionTarget = i;
            Vector3 point = Camera.main.WorldToViewportPoint(HeadPosition(i));
            GUI.Label(new Rect(point.x * w - 22, (1 - point.y) * h - 45, 44, 35), (i + 1).ToString(), scoreStyle);
        }
        if (GUI.Button(new Rect(x + pw * .03f, y + ph * .34f, pw * .94f, ph * .065f), "FONDO", JungleUITheme.Button))
            positionTarget = 9;
        Vector3 value = positionTarget == 9 ? backgroundOffset : headOffsets[positionTarget];
        string name = positionTarget == 9 ? "FONDO" : "CABEZA " + (positionTarget + 1);
        GUI.Label(new Rect(x + pw * .02f, y + ph * .415f, pw * .96f, ph * .12f),
            name + "\nX " + value.x.ToString("F2") + "  Y " + value.y.ToString("F2") + "  Z " + value.z.ToString("F2"), body);
        string[] axes = { "X", "Y", "Z" };
        for (int axis = 0; axis < 3; axis++)
        {
            float rowY = y + ph * (.55f + axis * .085f);
            GUI.Label(new Rect(x, rowY, pw * .17f, ph * .075f), axes[axis], body);
            if (GUI.Button(new Rect(x + pw * .18f, rowY, pw * .37f, ph * .075f), "- 0.05", JungleUITheme.Button))
                value[axis] -= .05f;
            if (GUI.Button(new Rect(x + pw * .58f, rowY, pw * .37f, ph * .075f), "+ 0.05", JungleUITheme.Button))
                value[axis] += .05f;
        }
        if (positionTarget == 9) backgroundOffset = value;
        else headOffsets[positionTarget] = value;
        if (GUI.Button(new Rect(x + pw * .03f, y + ph * .82f, pw * .94f, ph * .075f), "CENTRAR SELECCION", JungleUITheme.Button))
        {
            if (positionTarget == 9) backgroundOffset = Vector3.zero;
            else headOffsets[positionTarget] = Vector3.zero;
        }
        GUI.Label(new Rect(x, y + ph * .91f, pw, ph * .07f), "GUARDAR conserva las 9 posiciones", body);
    }
    private Vector3 CurrentAdjustment(){return adjustTarget==0?tableRotation:adjustTarget==1?headRotation:hammerRotation;}
    private void SetCurrentAdjustment(Vector3 v){v=new Vector3(Mathf.Repeat(v.x,360),Mathf.Repeat(v.y,360),Mathf.Repeat(v.z,360));if(adjustTarget==0)tableRotation=v;else if(adjustTarget==1)headRotation=v;else hammerRotation=v;ApplyAdjustmentPreview();}
    private void AdjustmentPanel(float w,float h){float x=w*.018f,y=h*.145f,pw=w*.27f,ph=h*.58f;JungleUITheme.DrawPanel(new Rect(x,y,pw,ph));GUI.Label(new Rect(x+pw*.06f,y+ph*.025f,pw*.88f,ph*.1f),"AJUSTE 3D",scoreStyle);string[] names={"MESA","GORILA","MAZO"};for(int i=0;i<3;i++)if(GUI.Button(new Rect(x+pw*(.055f+i*.305f),y+ph*.14f,pw*.285f,ph*.09f),names[i],JungleUITheme.Button))adjustTarget=i;Vector3 v=CurrentAdjustment();GUI.Label(new Rect(x+pw*.06f,y+ph*.25f,pw*.88f,ph*.13f),$"{names[adjustTarget]}\nX {v.x:0}°   Y {v.y:0}°   Z {v.z:0}°",body);float row=y+ph*.41f;string[] axes={"X","Y","Z"};for(int i=0;i<3;i++){GUI.Label(new Rect(x+pw*.07f,row+i*ph*.11f,pw*.12f,ph*.08f),axes[i],body);if(GUI.Button(new Rect(x+pw*.21f,row+i*ph*.11f,pw*.2f,ph*.08f),"− 5°",JungleUITheme.Button)){v=CurrentAdjustment();v[i]-=5;SetCurrentAdjustment(v);}if(GUI.Button(new Rect(x+pw*.44f,row+i*ph*.11f,pw*.2f,ph*.08f),"+ 5°",JungleUITheme.Button)){v=CurrentAdjustment();v[i]+=5;SetCurrentAdjustment(v);}if(GUI.Button(new Rect(x+pw*.68f,row+i*ph*.11f,pw*.25f,ph*.08f),"+ 1°",JungleUITheme.Button)){v=CurrentAdjustment();v[i]+=1;SetCurrentAdjustment(v);}}if(GUI.Button(new Rect(x+pw*.06f,y+ph*.77f,pw*.42f,ph*.09f),"GUARDAR",JungleUITheme.Button))SaveAdjustments();if(GUI.Button(new Rect(x+pw*.52f,y+ph*.77f,pw*.42f,ph*.09f),"RESTAURAR",JungleUITheme.Button))ResetAdjustments();GUI.Label(new Rect(x+pw*.06f,y+ph*.88f,pw*.88f,ph*.08f),Time.unscaledTime<adjustSavedUntil?"VALORES GUARDADOS":"TOMA CAPTURA DE ESTOS VALORES",body);}

    private Texture2D Solid(Color c){var t=new Texture2D(1,1);t.SetPixel(0,0,c);t.Apply();return t;}
    private void UI(){if(dark!=null&&title!=null&&body!=null&&scoreStyle!=null&&big!=null)return;dark=Solid(new(.01f,.045f,.035f,.94f));gold=Solid(new(1,.75f,.08f,.96f));red=Solid(new(.9f,.08f,.03f,.95f));turquoise=Solid(new(.03f,.82f,.78f,.95f));white=Solid(Color.white);title=new GUIStyle(JungleUITheme.Title){fontSize=Mathf.RoundToInt(Screen.height*.055f),fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter,normal={textColor=new(1,.82f,.15f)}};body=new GUIStyle(JungleUITheme.Body){fontSize=Mathf.RoundToInt(Screen.height*.028f),fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter,wordWrap=true,normal={textColor=Color.white}};scoreStyle=new GUIStyle(body){fontSize=Mathf.RoundToInt(Screen.height*.037f)};big=new GUIStyle(title){fontSize=Mathf.RoundToInt(Screen.height*.1f),normal={textColor=Color.white}};}
    private void OnGUI(){if(PartyMode.Blocking)return;if(state==State.Results && PartyMode.Report(score))return;if(JungleStory.BlocksGame)return;JungleUITheme.Ensure();UI();float w=Screen.width,h=Screen.height;JungleUITheme.DrawPanel(new Rect(w*.02f,h*.02f,w*.96f,h*.105f));GUI.Label(new Rect(w*.04f,h*.025f,w*.34f,h*.085f),"GORILLA WHACK",title);GUI.Label(new Rect(w*.39f,h*.028f,w*.30f,h*.075f),$"PUNTOS {score}   COMBO x{combo}",scoreStyle);if(GUI.Button(new Rect(w*.71f,h*.035f,w*.11f,h*.065f),adjustMode?"CERRAR AJUSTE":"AJUSTAR 3D",JungleUITheme.Button)){ToggleAdjustment();}if(GUI.Button(new Rect(w*.84f,h*.035f,w*.12f,h*.065f),"MENÚ",JungleUITheme.Button))SceneNavigator.Load(SceneNavigator.MainMenu);
        if(!adjustMode){if(state==State.Intro){JungleUITheme.DrawPanel(new Rect(w*.25f,h*.23f,w*.5f,h*.48f));GUI.Label(new Rect(w*.29f,h*.27f,w*.42f,h*.09f),"¡ATRAPA AL GORILA!",title);GUI.Label(new Rect(w*.3f,h*.38f,w*.4f,h*.13f),"MUEVE LA MANO DERECHA PARA APUNTAR\nY BAJA EL CELULAR PARA GOLPEAR",body);if(MotionSession.PrimaryButton(new Rect(w*.38f,h*.58f,w*.24f,h*.08f),"CENTRAR Y JUGAR",JungleUITheme.Button))Begin();}
        else if(state==State.Countdown){int c=Mathf.Clamp(3-Mathf.FloorToInt(Time.time-stateAt),1,3);JungleUITheme.DrawPanel(new Rect(w*.42f,h*.31f,w*.16f,h*.2f));GUI.Label(new Rect(w*.42f,h*.31f,w*.16f,h*.2f),c.ToString(),big);}else if(state==State.Playing){GUI.DrawTexture(new Rect(w*.43f,h*.14f,w*.14f,h*.065f),endAt-Time.time<6?red:dark);GUI.Label(new Rect(w*.43f,h*.14f,w*.14f,h*.065f),Mathf.CeilToInt(Mathf.Max(0,endAt-Time.time))+" s",scoreStyle);}else{JungleUITheme.DrawPanel(new Rect(w*.28f,h*.25f,w*.44f,h*.42f));GUI.Label(new Rect(w*.32f,h*.29f,w*.36f,h*.08f),"RESULTADO",title);GUI.Label(new Rect(w*.32f,h*.39f,w*.36f,h*.14f),$"{score} PUNTOS\n{hits} GOLPES · {misses} ESCAPES · MEJOR COMBO x{bestCombo}",body);if(MotionSession.PrimaryButton(new Rect(w*.36f,h*.56f,w*.18f,h*.07f),"OTRA RONDA",JungleUITheme.Button))Begin();}
        }if(state==State.Playing&&!adjustMode)GUI.Label(new Rect(w*.36f,h*.2f,w*.28f,h*.065f),message,body);if(adjustMode){AdjustmentPanel(w,h);PositionPanel(w,h);}float s=Mathf.Max(28,h*.045f);Vector2 p=HandViewport;Rect cursor=new(p.x*w-s*.5f,(1-p.y)*h-s*.5f,s,s);GUI.DrawTexture(cursor,white);GUI.DrawTexture(new Rect(cursor.x+5,cursor.y+5,cursor.width-10,cursor.height-10),turquoise);JungleUITheme.DrawLeaves();}
}


