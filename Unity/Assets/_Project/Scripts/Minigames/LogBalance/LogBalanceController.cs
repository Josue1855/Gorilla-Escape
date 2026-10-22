using System;
using GorillaEscape.Input;
using GorillaEscape.UI;
using UnityEngine;
namespace GorillaEscape.Minigames.LogBalance
{
    public sealed class LogBalanceController : MonoBehaviour
    {
        [SerializeField] LogBalanceSettings settings;
        readonly LogBalanceTilt tilt = new LogBalanceTilt();
        LogBalanceRules rules;
        LogBalanceStage stage;
        MainMenuController menu;
        LogBalanceModel model;
        bool keyboard,paused,focused=true,showCredits,pendingStart,pendingResume;
        string creditsText;
        int player=1;
        float fallenTime;
        string notice="Sostén el teléfono en una posición cómoda. El control se prepara automáticamente.";
        Vector2 scroll;
        public LogBalanceModel Attempt => model;
        public bool Paused => paused;
        public bool Keyboard => keyboard;
        public int Player => player;
        public bool KeyboardAvailable => Application.isEditor||Debug.isDebugBuild;
        PhoneInputStore Phones => menu?.Runtime?.java?.PhoneInputs;
        static long Now => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        void Awake()
        {
            if(settings==null)settings=Resources.Load<LogBalanceSettings>("LogBalanceSettings");
            rules=(settings!=null?settings.rules:new LogBalanceRules()).Copy();rules.Validate();
            creditsText=Resources.Load<TextAsset>("LogBalance/CREDITS")?.text??"Créditos en Art/LogBalance/ATTRIBUTION.md";
            model=new LogBalanceModel(rules);stage=new LogBalanceStage(transform,rules.length,settings!=null?settings.visuals:null);
        }
        public void Attach(MainMenuController owner) {menu=owner;}
        public void SelectKeyboard(bool value)
        {
            if(model.State!=LogBalanceState.Ready||value&&!KeyboardAvailable)return;
            keyboard=value;tilt.Reset();pendingStart=pendingResume=false;notice=value?"Teclado de desarrollo: A/D o flechas. Izquierda corrige hacia la izquierda.":"El control se prepara automáticamente.";
        }
        public void SelectPlayer(int value)
        {if(model.State!=LogBalanceState.Ready||value<1||value>4)return;player=value;tilt.Reset();pendingStart=pendingResume=false;}
        bool PlayerAdmitted()
        {
            var lobby=menu?.Runtime?.java?.Lobby?.Snapshot;
            if(lobby?.players==null)return false;
            if(Phones==null||!Phones.TryLatest(player,out var sample))return false;
            foreach(var p in lobby.players)
                if(p.playerId==player&&p.connectionState!="DISCONNECTED"&&sample.sessionId==lobby.sessionId
                    &&sample.deviceSessionId==p.deviceSessionId&&sample.connectionEpoch==p.connectionEpoch)return true;
            return false;
        }
        bool ReadPhone(long now,out float value)
        {
            value=0;if(!PlayerAdmitted())return false;
            if(model.State==LogBalanceState.Ready||paused)tilt.PrepareAutomatically(Phones,player,now,rules);
            return tilt.TryTilt(Phones,player,now,rules,out value);
        }
        public bool BeginAttempt()
        {
            if(model.State!=LogBalanceState.Ready)return false;
            if(!keyboard&&!ReadPhone(Now,out _)){pendingStart=true;notice="Preparando el control… El intento comenzará cuando esté listo.";return false;}
            pendingStart=false;paused=false;fallenTime=0;model.Begin(player);return true;
        }
        public void RestartAttempt()
        {model.Reset();tilt.Reset();pendingStart=pendingResume=false;paused=false;fallenTime=0;notice=keyboard?"Listo para otro intento.":"El control se prepara automáticamente.";}
        public void PauseAttempt(){if(!paused&&(model.State==LogBalanceState.Crossing||model.State==LogBalanceState.Countdown)){paused=true;if(!keyboard)tilt.Reset();}}
        public bool ResumeAttempt()
        {
            if(!paused||!focused)return false;
            if(!keyboard&&!ReadPhone(Now,out _)){pendingResume=true;return false;}
            pendingResume=false;paused=false;return true;
        }
        void OnApplicationFocus(bool hasFocus){focused=hasFocus;if(!hasFocus)PauseAttempt();}
        void OnApplicationPause(bool value){if(value)PauseAttempt();}
        void Update()
        {
            if(UnityEngine.Input.GetKeyDown(KeyCode.Escape)){if(menu!=null)menu.CloseLogBalance();return;}
            if(UnityEngine.Input.GetKeyDown(KeyCode.R))RestartAttempt();
            if(UnityEngine.Input.GetKeyDown(KeyCode.Space)){if(paused)ResumeAttempt();else BeginAttempt();}
            float input=0;
            if(keyboard)
            {if(UnityEngine.Input.GetKey(KeyCode.A)||UnityEngine.Input.GetKey(KeyCode.LeftArrow))input-=1;if(UnityEngine.Input.GetKey(KeyCode.D)||UnityEngine.Input.GetKey(KeyCode.RightArrow))input+=1;}
            bool available=keyboard||ReadPhone(Now,out input);
            if(!available){PauseAttempt();if(!keyboard)notice=PlayerAdmitted()?tilt.Status:"Conecta el teléfono del jugador seleccionado.";}
            if(available&&focused){
                if(pendingStart&&model.State==LogBalanceState.Ready)BeginAttempt();
                if(pendingResume&&paused)ResumeAttempt();
                if(!keyboard&&model.State==LogBalanceState.Ready)notice="Control listo. Pulsa comenzar.";
                else if(!keyboard&&paused)notice="Control listo. Pulsa reanudar.";
            }
            if(!paused&&focused&&available)model.Tick(Mathf.Min(.1f,Time.unscaledDeltaTime),input);
            if(model.State==LogBalanceState.Fell)fallenTime+=Time.unscaledDeltaTime;
            menu?.Runtime?.java?.PresentPhoneGame("log-balance",paused?"Paused":model.State.ToString(),player);
            stage.Present(model.Progress,model.Balance,model.State==LogBalanceState.Fell,fallenTime,model.Elapsed,
                model.State==LogBalanceState.Crossing&&!paused&&focused,model.State==LogBalanceState.Won,model.LogAngle*Mathf.Rad2Deg,model.FootstepPhase);
        }
        void OnGUI()
        {
            // Update owns gameplay shortcuts. Do not also activate the focused IMGUI button with Space.
            if(Event.current.isKey && IsGameplayKey(Event.current.keyCode))Event.current.Use();
            var matrix=GUI.matrix;var color=GUI.color;bool enabledBefore=GUI.enabled;
            try
            {
                float scale=Mathf.Max(.5f,Mathf.Min(Screen.width/1280f,Screen.height/900f));
                GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
                float width=Screen.width/scale,height=Screen.height/scale;
                scroll=GUI.BeginScrollView(new Rect(0,0,width,height),scroll,new Rect(0,0,Mathf.Max(width,1280),Mathf.Max(height,900)));
                Draw(Mathf.Max(width,1280),Mathf.Max(height,900));GUI.EndScrollView();
            }
            finally{GUI.matrix=matrix;GUI.color=color;GUI.enabled=enabledBefore;}
        }
        void Draw(float width,float height)
        {
            var title=new GUIStyle(GUI.skin.label){fontSize=30,fontStyle=FontStyle.Bold};
            var body=new GUIStyle(GUI.skin.label){fontSize=19,wordWrap=true};
            var button=new GUIStyle(GUI.skin.button){fontSize=18};
            GUI.Box(new Rect(20,20,width-40,145),"");
            GUI.Label(new Rect(40,30,800,40),"DESAFÍO DEL TRONCO",title);
            GUI.Label(new Rect(40,75,width-310,60),"Eres el náufrago: cruza el río y supera la prueba de los gorilas.\nCada paso y tu peso hacen girar el tronco. Corrige con suavidad y frena la inercia.",body);
            GUI.Label(new Rect(40,135,width-300,24),"Modelos: gashtan · Aditya Graphical · katarzynaszemla · Anskar · planeta-elefante — CC BY 4.0 (adaptados)",new GUIStyle(body){fontSize=13});
            if(GUI.Button(new Rect(width-220,45,170,50),"VOLVER",button)&&menu!=null)menu.CloseLogBalance();
            if(GUI.Button(new Rect(width-220,105,170,35),showCredits?"CERRAR CRÉDITOS":"CRÉDITOS",button))
            {showCredits=!showCredits;if(showCredits)PauseAttempt();}
            if(showCredits)
            {
                var panel=new Rect(width/2-430,190,860,570);Fill(panel,new Color(.06f,.1f,.07f,.98f));
                GUI.Label(new Rect(panel.x+25,panel.y+20,panel.width-50,panel.height-40),creditsText,new GUIStyle(body){fontSize=17});return;
            }
            float x=width-600,y=height-300;
            GUI.Box(new Rect(x-25,y-10,590,285),"");
            GUI.Label(new Rect(x,y,540,32),"IZQUIERDA  ←                 CENTRO                 →  DERECHA",body);
            float danger=Mathf.InverseLerp(.55f,1,model.Danger);
            Color bar=Color.Lerp(new Color(.2f,.7f,.45f),new Color(.95f,.25f,.12f),danger);
            if(model.State==LogBalanceState.Crossing&&!paused&&danger>0)
            {Fill(new Rect(0,0,7,height),new Color(.9f,.2f,.1f,danger*.7f));Fill(new Rect(width-7,0,7,height),new Color(.9f,.2f,.1f,danger*.7f));}
            Fill(new Rect(x,y+40,540,24),new Color(.25f,.28f,.3f));
            float safe=model.EffectiveFallThreshold/rules.balanceClamp;
            Fill(new Rect(x+270*(1-safe),y+40,540*safe,24),bar);
            float tendency=Mathf.Clamp((model.Balance+model.LateralVelocity*.35f)/rules.balanceClamp,-1,1);
            Fill(new Rect(x+270*(1+tendency)-2,y+40,4,24),new Color(1,.7f,.2f));
            Fill(new Rect(x+270*(1+model.Balance/rules.balanceClamp)-5,y+32,10,40),Color.white);
            GUI.Label(new Rect(x,y+66,540,13),"BLANCO: APOYO ACTUAL · ÁMBAR: TENDENCIA DE MOVIMIENTO",new GUIStyle(body){fontSize=10});
            GUI.Label(new Rect(x,y+78,540,48),$"Progreso {model.Progress:P0} · {model.Distance:F1} / {rules.length:F0} m · {model.Elapsed:F1} s\nGiro {model.LogAngle*Mathf.Rad2Deg:F0}° · Paso {model.ForwardVelocity:F2} m/s · Viento {(Mathf.Abs(model.Wind)<.06f?"suave":model.Wind>0?"→":"←")}",new GUIStyle(body){fontSize=16});
            if(model.Result!=null)
            {
                GUI.Label(new Rect(x,y+120,540,60),$"{(model.Result.Completed?"¡CRUCE COMPLETADO!":"HAS CAÍDO") } · Jugador {model.Result.PlayerId}\n{model.Result.Seconds:F1} s · {model.Result.Distance:F1} m · {model.Result.Progress:P0}",body);
                if(GUI.Button(new Rect(x,y+200,540,48),"OTRO INTENTO (R)",button))RestartAttempt();return;
            }
            if(model.State==LogBalanceState.Ready)
            {
                GUI.Label(new Rect(40,165,500,40),$"Jugador {player} · {(keyboard?"TECLADO DE DESARROLLO":"TELÉFONO")}",body);
                for(int i=1;i<=4;i++)if(GUI.Button(new Rect(40+(i-1)*60,210,50,40),i.ToString(),button))SelectPlayer(i);
                if(KeyboardAvailable&&GUI.Button(new Rect(40,265,310,44),keyboard?"USAR TELÉFONO":"PROBAR CON TECLADO",button))SelectKeyboard(!keyboard);
                if(!keyboard&&GUI.Button(new Rect(40,320,310,44),"CONECTAR TELÉFONO",button)&&menu!=null){menu.OpenPhoneForLogBalance();}
                GUI.Label(new Rect(x,y+120,540,63),notice,body);
                if(GUI.Button(new Rect(x,y+185,540,45),"COMENZAR (ESPACIO)",button))BeginAttempt();
            }
            else if(paused)
            {
                GUI.Label(new Rect(x,y+120,540,60),"PAUSA · "+(focused?notice:"Vuelve a la ventana para continuar."),body);
                if(GUI.Button(new Rect(x,y+195,540,45),"REANUDAR",button))ResumeAttempt();
            }
            else GUI.Label(new Rect(x,y+125,540,70),model.State==LogBalanceState.Countdown?$"Comienza en {Mathf.CeilToInt(model.Countdown)}…":"Frena la inercia antes del borde\nR reinicia",title);
        }
        public static bool IsGameplayKey(KeyCode key) => key==KeyCode.Space||key==KeyCode.R
            ||key==KeyCode.Escape||key==KeyCode.A||key==KeyCode.D||key==KeyCode.LeftArrow||key==KeyCode.RightArrow;
        static void Fill(Rect rect,Color value){GUI.color=value;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=Color.white;}
        void OnDestroy(){menu?.Runtime?.java?.PresentPhoneGame("lobby","Ready",0);stage?.Dispose();}
    }
}
