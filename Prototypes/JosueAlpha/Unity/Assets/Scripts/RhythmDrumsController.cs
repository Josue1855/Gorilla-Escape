using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class RhythmDrumsController : MonoBehaviour
{
    [Serializable] private struct MotionPacket { public string type; public float ax,ay,az,rx,ry,rz; }
    [Serializable] private struct PosePacket { public bool leftDetected,rightDetected; public float leftWristX,leftWristY,rightWristX,rightWristY; }
    private sealed class Note { public bool left, judged; public float time; }

    private MotionSession session;
    private readonly List<Note> notes=new List<Note>();
    private Vector2 leftHand=new Vector2(.35f,.50f),rightHand=new Vector2(.65f,.50f),previousLeft,previousRight;
    private float leftDownSpeed,rightDownSpeed,phonePower,nextBeat,startTime,lastLeftHit,lastRightHit,leftFlash,rightFlash;
    private int score,combo,bestCombo,misses,beatIndex;
    private bool rightPowerReady=true,finished;
    private const float Beat=.54545f;
    private Texture2D dark,cyan,orange,white,green,red;

    private void Awake()
    {
        session=MotionSession.Ensure(); previousLeft=leftHand; previousRight=rightHand;
        BuildStage(); startTime=Time.time+2f; nextBeat=startTime; GenerateChart();
    }

    private Material Mat(Color c){Material m=new Material(Shader.Find("Standard"));m.color=c;return m;}
    private void BuildStage()
    {
        Camera cam=Camera.main;cam.transform.position=new Vector3(0,3.5f,-9f);cam.transform.LookAt(new Vector3(0,1.1f,1f));cam.backgroundColor=new Color(.018f,.035f,.05f);
        GameObject floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Escenario";floor.transform.position=new Vector3(0,-.45f,2);floor.transform.localScale=new Vector3(14,.3f,15);floor.GetComponent<Renderer>().material=Mat(new Color(.04f,.12f,.10f));
        CreateDrum("Tambor izquierdo",-1.7f,new Color(.05f,.65f,.9f));CreateDrum("Tambor derecho",1.7f,new Color(1f,.35f,.08f));
        for(int i=-5;i<=5;i++){GameObject torch=GameObject.CreatePrimitive(PrimitiveType.Sphere);torch.name="Luz rítmica";torch.transform.position=new Vector3(i*1.25f,3.8f,5.5f);torch.transform.localScale=Vector3.one*.32f;torch.GetComponent<Renderer>().material=Mat(i%2==0?new Color(.05f,.7f,1f):new Color(1f,.3f,.06f));torch.GetComponent<Collider>().enabled=false;}
    }

    private void CreateDrum(string name,float x,Color color)
    {
        GameObject drum=GameObject.CreatePrimitive(PrimitiveType.Cylinder);drum.name=name;drum.transform.position=new Vector3(x,.35f,-.2f);drum.transform.localScale=new Vector3(1.25f,.65f,1.25f);drum.GetComponent<Renderer>().material=Mat(color);
        GameObject head=GameObject.CreatePrimitive(PrimitiveType.Cylinder);head.name="Parche";head.transform.position=new Vector3(x,1.02f,-.2f);head.transform.localScale=new Vector3(1.12f,.055f,1.12f);head.GetComponent<Renderer>().material=Mat(new Color(.92f,.82f,.58f));head.GetComponent<Collider>().enabled=false;
    }

    private void GenerateChart()
    {
        for(int i=0;i<64;i++)
        {
            bool both=i>8&&i%16==15;
            notes.Add(new Note{left=i%4==0||i%4==3,time=startTime+i*Beat});
            if(both)notes.Add(new Note{left=false,time=startTime+i*Beat});
        }
    }

    private void Update()
    {
        ReadSensors();float now=Time.time;
        if(!finished)
        {
            foreach(Note note in notes)if(!note.judged&&now-note.time>.24f){note.judged=true;misses++;combo=0;}
            if(now>startTime+65*Beat)finished=true;
        }
    }

    private void ReadSensors()
    {
        string poseJson=session.ConsumePose();
        if(!string.IsNullOrEmpty(poseJson))
        {
            PosePacket p=JsonUtility.FromJson<PosePacket>(poseJson);float dt=Mathf.Clamp(Time.deltaTime,.01f,.08f);
            Vector2 nl=new Vector2(p.leftWristX,p.leftWristY),nr=new Vector2(p.rightWristX,p.rightWristY);
            leftDownSpeed=(nl.y-previousLeft.y)/dt;rightDownSpeed=(nr.y-previousRight.y)/dt;
            if(p.leftDetected)leftHand=Vector2.Lerp(leftHand,nl,.72f);if(p.rightDetected)rightHand=Vector2.Lerp(rightHand,nr,.72f);
            previousLeft=nl;previousRight=nr;
            if(p.leftDetected&&leftDownSpeed>.62f&&Time.time-lastLeftHit>.20f){lastLeftHit=Time.time;TryHit(true,leftDownSpeed);}
            if(p.rightDetected&&rightDownSpeed>.62f&&Time.time-lastRightHit>.20f){lastRightHit=Time.time;TryHit(false,rightDownSpeed);}
        }
        string motionJson=session.ConsumeMotion();if(string.IsNullOrEmpty(motionJson))return;
        MotionPacket m=JsonUtility.FromJson<MotionPacket>(motionJson);phonePower=Mathf.Sqrt(m.rx*m.rx+m.ry*m.ry+m.rz*m.rz)+new Vector3(m.ax,m.ay,m.az).magnitude*7f;
        if(phonePower<150f)rightPowerReady=true;
        if(phonePower>225f&&rightPowerReady&&Time.time-lastRightHit>.16f){rightPowerReady=false;lastRightHit=Time.time;TryHit(false,phonePower/180f);}
    }

    private void TryHit(bool left,float strength)
    {
        Note best=null;float error=.26f;
        foreach(Note n in notes)if(!n.judged&&n.left==left&&Mathf.Abs(Time.time-n.time)<error){best=n;error=Mathf.Abs(Time.time-n.time);}
        if(best==null){combo=0;misses++;return;}best.judged=true;combo++;bestCombo=Mathf.Max(bestCombo,combo);
        int gained=error<.075f?1000:error<.15f?650:350;score+=gained+combo*12;
        if(left)leftFlash=Time.time+.12f;else rightFlash=Time.time+.12f;
    }

    private Texture2D Solid(Color c){Texture2D t=new Texture2D(1,1);t.SetPixel(0,0,c);t.Apply();return t;}
    private void OnGUI()
    {
        JungleUITheme.Ensure();
        dark??=Solid(new Color(.015f,.025f,.04f,.94f));cyan??=Solid(new Color(.05f,.7f,1f));orange??=Solid(new Color(1f,.32f,.06f));white??=Solid(Color.white);green??=Solid(new Color(.15f,.9f,.3f));red??=Solid(new Color(.9f,.08f,.06f));
        float w=Screen.width,h=Screen.height;GUIStyle title=new GUIStyle(GUI.skin.label){fontSize=Mathf.RoundToInt(h*.038f),fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter,normal={textColor=Color.white}};
        JungleUITheme.DrawPanel(new Rect(w*.16f,h*.02f,w*.68f,h*.13f));GUI.Label(new Rect(w*.18f,h*.025f,w*.64f,h*.055f),"JUNGLE DRUMS",title);GUI.Label(new Rect(w*.19f,h*.08f,w*.62f,h*.05f),$"PUNTOS {score}    COMBO {combo}    MEJOR {bestCombo}    FALLOS {misses}",title);
        DrawLane(true,w*.25f,w,h,title);DrawLane(false,w*.62f,w,h,title);
        if(finished){GUI.DrawTexture(new Rect(w*.28f,h*.31f,w*.44f,h*.25f),dark);GUI.Label(new Rect(w*.30f,h*.35f,w*.40f,h*.08f),$"CANCIÓN TERMINADA · {score}",title);if(GUI.Button(new Rect(w*.39f,h*.47f,w*.22f,h*.06f),"REPETIR"))SceneNavigator.Load(SceneNavigator.Drums);}
        if(GUI.Button(new Rect(w*.85f,h*.04f,w*.12f,h*.06f),"MENÚ",JungleUITheme.Button))SceneNavigator.Load(SceneNavigator.MainMenu);
        JungleUITheme.DrawLeaves();
    }

    private void DrawLane(bool left,float x,float w,float h,GUIStyle style)
    {
        float laneW=w*.13f,targetY=h*.76f;GUI.DrawTexture(new Rect(x, h*.16f,laneW,h*.66f),dark);GUI.DrawTexture(new Rect(x,targetY,laneW,h*.025f),left?cyan:orange);
        foreach(Note n in notes)if(!n.judged&&n.left==left){float seconds=n.time-Time.time;if(seconds<2.6f&&seconds>-.3f){float y=targetY-seconds*(h*.23f);GUI.DrawTexture(new Rect(x+laneW*.18f,y,laneW*.64f,h*.045f),left?cyan:orange);}}
        bool flash=Time.time<(left?leftFlash:rightFlash);GUI.DrawTexture(new Rect(x-laneW*.05f,h*.80f,laneW*1.1f,h*.08f),flash?white:(left?cyan:orange));GUI.Label(new Rect(x,h*.80f,laneW,h*.08f),left?"IZQUIERDA":"DERECHA",style);
    }
}
