using System;
using GorillaEscape.Gameplay.Input;
using GorillaEscape.Minigames.LogBalance;
using UnityEngine;

// The original fixed-step log simulation, presented in Saul's jungle and driven only by fused input.
[RequireComponent(typeof(JunglePanel))]
public sealed class LogBalanceGameController : MonoBehaviour, IJungleScreen
{
    [SerializeField] LogBalanceSettings settings;
    [SerializeField] Material trunkMaterial;
    readonly LogBalanceFusedControl control=new LogBalanceFusedControl();
    MotionSession session;
    LogBalanceModel model;
    LogBalanceRules rules;
    LogBalanceStage stage;
    AudioSource feedback;
    AudioClip warning,fall,win;
    LogBalanceState feedbackState;
    float tilt,fallenTime,nextWarning;
    bool manualPause,preparing=true,focused=true,credits;
    string assetError,creditsText;
    GUIStyle titleStyle,bodyStyle,buttonStyle;
    int lastScreenHeight;
    int cachedPct=-1,cachedDistTenths=-1,cachedElapsedTenths=-1,cachedCountdownSec=-1;
    string cachedStatsText="",cachedCountdownText="";
    public LogBalanceModel Attempt => model;

    void Awake()
    {
        session=MotionSession.Ensure();session.ConfigureForScene(SceneNavigator.LogBalance);
        rules=(settings!=null?settings.rules:new LogBalanceRules()).Copy();rules.Validate();
        model=new LogBalanceModel(rules);
        try { stage=new LogBalanceStage(transform,rules.length,settings!=null?settings.visuals:null,trunkMaterial); }
        catch(Exception error) { assetError="No se pudo preparar el escenario del tronco. Revisa sus recursos en Unity.";Debug.LogError("LOG_BALANCE_STAGE_FAILED: "+error.Message); }
        creditsText=JungleAssets.Load<TextAsset>("LogBalance/CREDITS")?.text??"Consulta los créditos del tronco en el repositorio.";
        feedback=JungleAudioBank.Attach2DSource(gameObject);
        warning=JungleAudioBank.LogWarning;fall=JungleAudioBank.LogFall;win=JungleAudioBank.LogWin;
    }
    void Update()
    {
        session.PresentGame(manualPause||credits?"Paused":preparing||model.State==LogBalanceState.Ready?"Ready":model.State==LogBalanceState.Countdown?"Countdown":model.State==LogBalanceState.Won?"Won":model.State==LogBalanceState.Fell?"Lost":"Playing",progress:model.Progress,elapsed:model.Elapsed,balance:model.Balance,distance:model.Distance);
        if(assetError!=null)return;
        if(!session.IsInputReady){control.Reset();preparing=true;tilt=0;return;}
        if(session.TryConsumeFrame(out var frame))
        {
            if(frame.Baseline){control.Reset();preparing=true;tilt=0;}
            bool calibrated=control.Observe(frame,rules,preparing||manualPause||model.State==LogBalanceState.Ready);
            if(calibrated&&session.TryAuthorize(JungleAction.Balance,frame.Revision)){tilt=control.Value;preparing=false;}
            else if(!calibrated)preparing=true;
        }
        bool running=focused&&!credits&&!manualPause&&!preparing&&control.Calibrated;
        if(running)model.Tick(Mathf.Min(.1f,Time.deltaTime),tilt);
        if(running&&model.State==LogBalanceState.Fell)fallenTime+=Time.deltaTime;
        PresentFeedback(running);
        stage.Present(model.Progress,model.Balance,model.State==LogBalanceState.Fell,fallenTime,model.Elapsed,
            running&&model.State==LogBalanceState.Crossing,model.State==LogBalanceState.Won,
            model.LogAngle*Mathf.Rad2Deg,model.FootstepPhase,model.Danger);
    }
    void PresentFeedback(bool running)
    {
        if(running&&model.State==LogBalanceState.Crossing&&model.Danger>=.6f&&Time.time>=nextWarning)
        {feedback.PlayOneShot(warning,model.Danger>=.85f?.85f:.55f);nextWarning=Time.time+(model.Danger>=.85f?.45f:.9f);}
        if(model.State==feedbackState)return;
        if(model.State==LogBalanceState.Fell){feedback.PlayOneShot(fall);JungleStageBuilder.ShakeCamera(.18f,.25f);}
        else if(model.State==LogBalanceState.Won)feedback.PlayOneShot(win);
        feedbackState=model.State;
    }
    public void BeginFromPhone()=>Begin();
    void Begin()
    {
        if(model.State!=LogBalanceState.Ready||!session.IsInputReady||preparing||!control.Calibrated)return;
        model.Begin(1);fallenTime=0;manualPause=false;
    }
    void Restart()
    {
        model.Reset();control.Reset();session.ResetRound();
        preparing=true;manualPause=false;tilt=fallenTime=nextWarning=0;feedbackState=LogBalanceState.Ready;
    }
    void OnApplicationFocus(bool value) { }
    void OnApplicationPause(bool value) { }
    public void RenderUI()
    {
        JungleUITheme.Ensure();float w=Screen.width,h=Screen.height;
        if(titleStyle==null||lastScreenHeight!=Screen.height)
        {
            lastScreenHeight=Screen.height;
            titleStyle=new GUIStyle(JungleUITheme.Title){fontSize=Mathf.RoundToInt(h*.043f)};
            bodyStyle=new GUIStyle(JungleUITheme.Body){fontSize=Mathf.RoundToInt(Mathf.Clamp(h*.024f,14,24))};
            buttonStyle=new GUIStyle(JungleUITheme.Button){fontSize=Mathf.RoundToInt(Mathf.Clamp(h*.025f,15,24))};
        }
        var title=titleStyle;var body=bodyStyle;var button=buttonStyle;
        JungleUITheme.DrawPanel(new Rect(w*.035f,h*.025f,w*.93f,h*.145f));
        JungleUITheme.DrawShadowedLabel(new Rect(w*.06f,h*.035f,w*.56f,h*.06f),"DESAFÍO DEL TRONCO",title,3f);
        JungleUITheme.DrawShadowedLabel(new Rect(w*.06f,h*.095f,w*.56f,h*.05f),"Cruza el río. Inclina suavemente el teléfono y frena la inercia.",body,1.5f);
        if(JungleRetainedUI.Button(new Rect(w*.66f,h*.065f,w*.135f,h*.062f),credits?"CERRAR":"CRÉDITOS",button))credits=!credits;
        if(JungleRetainedUI.Button(new Rect(w*.81f,h*.065f,w*.135f,h*.062f),"MENÚ",button))SceneNavigator.Load(SceneNavigator.MainMenu);
        if(assetError!=null){JungleRetainedUI.Label(new Rect(w*.16f,h*.35f,w*.68f,h*.24f),assetError,body);return;}
        if(credits)
        {
            JungleUITheme.DrawPanel(new Rect(w*.14f,h*.22f,w*.72f,h*.5f));
            JungleRetainedUI.Label(new Rect(w*.18f,h*.25f,w*.64f,h*.43f),creditsText,body);
            return;
        }
        float x=w*.52f,y=h*.66f,pw=w*.43f,ph=h*.30f;
        JungleUITheme.DrawPanel(new Rect(x,y,pw,ph));
        int pct=Mathf.RoundToInt(model.Progress*100f),distT=Mathf.RoundToInt(model.Distance*10f),elapT=Mathf.RoundToInt(model.Elapsed*10f);
        if(pct!=cachedPct||distT!=cachedDistTenths||elapT!=cachedElapsedTenths)
        {
            cachedPct=pct;cachedDistTenths=distT;cachedElapsedTenths=elapT;
            cachedStatsText=$"{model.Progress:P0} · {model.Distance:F1}/{rules.length:F0} m · {model.Elapsed:F1} s";
        }
        JungleRetainedUI.Label(new Rect(x+15,y+10,pw-30,ph*.18f),cachedStatsText,body);
        float bx=x+25,by=y+ph*.25f,bw=pw-50;
        JungleUITheme.DrawSubPanel(new Rect(bx,by,bw,22));
        float position=Mathf.Clamp01(.5f+.5f*model.Balance/rules.balanceClamp);
        Color prior=JungleRetainedUI.color;JungleRetainedUI.color=Color.Lerp(new Color(.2f,.9f,.55f),new Color(1,.25f,.1f),model.Danger);
        JungleRetainedUI.DrawTexture(new Rect(bx+bw*position-4,by-4,8,30),Texture2D.whiteTexture);JungleRetainedUI.color=prior;
        int cdSec=Mathf.CeilToInt(model.Countdown);
        if(model.State==LogBalanceState.Countdown&&cdSec!=cachedCountdownSec){cachedCountdownSec=cdSec;cachedCountdownText=$"PREPÁRATE · {cdSec}";}
        string message=preparing?control.Status:manualPause?"EN PAUSA":model.State==LogBalanceState.Countdown?cachedCountdownText
            :model.State==LogBalanceState.Crossing?"IZQUIERDA ← CORRIGE CON SUAVIDAD → DERECHA"
            :model.State==LogBalanceState.Won?"¡CRUCE COMPLETADO!":model.State==LogBalanceState.Fell?"HAS CAÍDO. PUEDES VOLVER A INTENTARLO.":"Control listo. Pulsa comenzar.";
        JungleRetainedUI.Label(new Rect(x+20,y+ph*.40f,pw-40,ph*.28f),message,body);
        bool enabled=JungleRetainedUI.enabled;
        if(model.State==LogBalanceState.Ready)
        {JungleRetainedUI.enabled=enabled&&session.IsInputReady&&!preparing&&control.Calibrated;if(JungleRetainedUI.Button(new Rect(x+25,y+ph*.74f,pw-50,ph*.18f),"COMENZAR",button))Begin();}
        else if(model.Result!=null)
        {if(JungleRetainedUI.Button(new Rect(x+25,y+ph*.74f,pw-50,ph*.18f),"OTRO INTENTO",button))Restart();}
        else
        {JungleRetainedUI.enabled=enabled&&(!manualPause||control.Calibrated&&!preparing);if(JungleRetainedUI.Button(new Rect(x+25,y+ph*.74f,pw-50,ph*.18f),manualPause?"REANUDAR":"PAUSAR",button))manualPause=!manualPause;}
        JungleRetainedUI.enabled=enabled;JungleUITheme.DrawLeaves();
    }
    void OnDestroy()
    {
        stage?.Dispose();
    }
}
