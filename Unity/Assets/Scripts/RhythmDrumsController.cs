using System;
using System.Collections.Generic;
using UnityEngine;
using GorillaEscape.Gameplay.Input;

public sealed class RhythmDrumsController : MonoBehaviour
{
    private sealed class Note { public bool left, judged; public float time; }

    private MotionSession session;
    private readonly List<Note> notes=new List<Note>();
    private float startTime,leftFlash,rightFlash;
    private int score,combo,bestCombo,misses;
    private bool finished;
    private long inputRevision;
    private const float Beat=.54545f;
    private Texture2D dark,cyan,orange,white,green,red;

    private void Awake()
    {
        session=MotionSession.Ensure();
        BuildStage(); startTime=Time.time+2f; GenerateChart();
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
        session.PresentGame(finished?"Finished":Time.time<startTime?"Countdown":"Playing",score,Mathf.Clamp01((Time.time-startTime)/(65*Beat)),Mathf.Max(0,Time.time-startTime));
        if(!session.IsInputReady)return;
        if(session.TryConsumeFrame(out var frame))ReadSensors(frame);
        float now=Time.time;
        if(!finished)
        {
            foreach(Note note in notes)if(!note.judged&&now-note.time>.24f){note.judged=true;misses++;combo=0;}
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
