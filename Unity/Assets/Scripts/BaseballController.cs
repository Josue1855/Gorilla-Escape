using System;
using UnityEngine;
using GorillaEscape.Gameplay.Input;

public sealed class BaseballController : MonoBehaviour
{
    private enum PlayState { Ready, Pitching, HitFlight, Result, GameOver }

    private MotionSession session;
    private PlayState state = PlayState.Ready;
    private GameObject ball, bat;
    private Rigidbody ballBody;
    private Vector2 rightHand = new Vector2(.62f,.58f);
    private float phonePower, pitchAt, stateUntil, hitDistance;
    private long inputRevision;
    private bool phoneCalibrated;
    private float centerBeta, centerGamma;
    private int lastRecenter;
    private Quaternion batOrientation = Quaternion.Euler(15f, 0f, 0f);
    private int pitches, strikes, hits, homeRuns, score;
    private string message = "PREPÁRATE";
    private Texture2D panel, green, yellow, red, blue;

    private void Awake()
    {
        session = MotionSession.Ensure();
        BuildField();
        SchedulePitch(1.2f);
    }

    private Material Material(Color color)
    {
        Material material = new Material(Shader.Find("Standard")); material.color = color; return material;
    }

    private GameObject Primitive(PrimitiveType type, string name, Vector3 position, Vector3 scale, Color color)
    {
        GameObject item = GameObject.CreatePrimitive(type); item.name=name; item.transform.position=position; item.transform.localScale=scale;
        item.GetComponent<Renderer>().material=Material(color); return item;
    }

    private void BuildField()
    {
        Camera camera=Camera.main; camera.transform.position=new Vector3(0,3.1f,-11f); camera.transform.LookAt(new Vector3(0,1.1f,3.5f)); camera.fieldOfView=48f; camera.backgroundColor=new Color(.25f,.65f,.9f);
        Primitive(PrimitiveType.Cube,"Campo",new Vector3(0,-.35f,4),new Vector3(22,.3f,25),new Color(.08f,.43f,.12f));
        Primitive(PrimitiveType.Cube,"Tierra",new Vector3(0,-.16f,1.5f),new Vector3(8,.04f,10),new Color(.48f,.25f,.08f)).transform.rotation=Quaternion.Euler(0,45,0);
        Primitive(PrimitiveType.Cube,"Plato",new Vector3(0,-.10f,-3.2f),new Vector3(.8f,.04f,.8f),Color.white).transform.rotation=Quaternion.Euler(0,45,0);
        for(int i=-5;i<=5;i++) Primitive(PrimitiveType.Capsule,"Público",new Vector3(i*1.7f,1.1f,13f+Mathf.Abs(i%2)),new Vector3(.65f,1.1f,.65f),new Color(.04f,.08f,.06f));
        Primitive(PrimitiveType.Capsule,"Pitcher gorila provisional",new Vector3(0,1.2f,6.4f),new Vector3(1.6f,2.1f,1.2f),new Color(.16f,.17f,.18f));
        ball=Primitive(PrimitiveType.Sphere,"Pelota",new Vector3(0,1.5f,5.8f),Vector3.one*.32f,Color.white);
        ballBody=ball.AddComponent<Rigidbody>(); ballBody.useGravity=false; ballBody.isKinematic=true;
        bat=Primitive(PrimitiveType.Cube,"Bate provisional",new Vector3(1.15f,.8f,-3.1f),new Vector3(.18f,.18f,2.2f),new Color(.55f,.27f,.07f)); bat.GetComponent<Collider>().enabled=false;
    }

    private void SchedulePitch(float delay)
    {
        state=PlayState.Ready; pitchAt=Time.time+delay; message="PREPÁRATE";
        ballBody.isKinematic=true; ball.transform.position=new Vector3(0,1.5f,5.8f); ball.transform.localScale=Vector3.one*.32f;
    }

    private void Update()
    {
        session.PresentGame(state==PlayState.GameOver?"Finished":state==PlayState.Ready?"Ready":"Playing",score,Mathf.Clamp01(pitches/10f));
        if(!session.IsInputReady){phonePower=0;phoneCalibrated=false;return;}
        if(session.TryConsumeFrame(out var frame))ReadSensors(frame);
        Vector3 handPosition = new Vector3(Mathf.Lerp(-2.45f,2.45f,rightHand.x),Mathf.Lerp(2.75f,.30f,rightHand.y),-3.1f);
        bat.transform.position = Vector3.Lerp(bat.transform.position, handPosition, 18f * Time.deltaTime);
        bat.transform.rotation = Quaternion.Slerp(bat.transform.rotation, batOrientation, 20f * Time.deltaTime);
        if(state==PlayState.Ready && Time.time>=pitchAt) StartPitch();
        else if(state==PlayState.Pitching)
        {
            ball.transform.position += new Vector3(0,-.05f,-8.2f)*Time.deltaTime;
            if(ball.transform.position.z < -4.2f) FinishPitch(false,0);
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
    }

    private void ReadSensors(JungleFusedFrame frame)
    {
        inputRevision=frame.Revision;
        if(frame.Baseline)phoneCalibrated=false;
        rightHand=new Vector2(frame.Right.X,frame.Right.Y);
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
        state=PlayState.HitFlight; message=hitDistance>105f?"¡JONRÓN!":"¡HIT!";
        ballBody.isKinematic=false; ballBody.useGravity=true; ballBody.linearVelocity=new Vector3(direction*8f,8f+strength*7f,13f+strength*12f); stateUntil=Time.time+2.7f;
    }

    private void FinishHit() { ballBody.isKinematic=true; state=PlayState.Result; stateUntil=Time.time+1.5f; }
    private void FinishPitch(bool hit,float distance) { strikes++; message="STRIKE"; state=PlayState.Result; stateUntil=Time.time+1.3f; }

    private Texture2D Solid(Color c){Texture2D t=new Texture2D(1,1);t.SetPixel(0,0,c);t.Apply();return t;}
    private void OnGUI()
    {
        JungleUITheme.Ensure();
        panel??=Solid(new Color(.02f,.04f,.055f,.94f)); green??=Solid(new Color(.08f,.65f,.18f)); yellow??=Solid(new Color(1f,.78f,.08f)); red??=Solid(new Color(.9f,.12f,.08f)); blue??=Solid(new Color(.06f,.55f,.9f));
        float w=Screen.width,h=Screen.height; GUIStyle title=new GUIStyle(JungleUITheme.Title){fontSize=Mathf.RoundToInt(h*.045f),normal={textColor=Color.white}};
        JungleUITheme.DrawPanel(new Rect(w*.12f,h*.03f,w*.76f,h*.16f)); GUI.Label(new Rect(w*.14f,h*.04f,w*.72f,h*.065f),"JUNGLE BASEBALL",title);
        GUI.Label(new Rect(w*.15f,h*.105f,w*.70f,h*.06f),$"LANZAMIENTO {Mathf.Min(pitches+1,10)}/10    HITS {hits}    JONRONES {homeRuns}    PUNTOS {score}",title);
        GUI.DrawTexture(new Rect(w*.34f,h*.22f,w*.32f,h*.075f),message.Contains("JON")?yellow:message.Contains("STRIKE")?red:blue); GUI.Label(new Rect(w*.34f,h*.22f,w*.32f,h*.075f),message,title);
        if(state==PlayState.GameOver){GUI.DrawTexture(new Rect(w*.27f,h*.34f,w*.46f,h*.25f),panel);GUI.Label(new Rect(w*.29f,h*.37f,w*.42f,h*.08f),$"PARTIDA TERMINADA · {score} PUNTOS",title);if(GUI.Button(new Rect(w*.38f,h*.49f,w*.24f,h*.07f),"OTRA PARTIDA")){session.ResetRound();pitches=strikes=hits=homeRuns=score=0;SchedulePitch(1f);}}
        if(GUI.Button(new Rect(w*.84f,h*.04f,w*.13f,h*.07f),"MENÚ",JungleUITheme.Button))SceneNavigator.Load(SceneNavigator.MainMenu);
        GUI.Label(new Rect(w*.18f,h*.88f,w*.64f,h*.06f),"SIGUE LA PELOTA · HAZ EL SWING CON EL CELULAR Y LA MANO DERECHA",title);
        JungleUITheme.DrawLeaves();
    }
}
