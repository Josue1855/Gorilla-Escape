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
        bool keyboard,paused,focused=true,showCredits;
        string creditsText;
        int player=1;
        float fallenTime;
        string notice="Sostén el teléfono vertical y calibra su posición cómoda.";
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
            keyboard=value;tilt.Reset();notice=value?"Teclado de desarrollo: A/D o flechas. Izquierda corrige hacia la izquierda.":"Calibra el teléfono antes de comenzar.";
        }
        public void SelectPlayer(int value)
        {if(model.State!=LogBalanceState.Ready||value<1||value>4)return;player=value;tilt.Reset();}
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
        {value=0;return PlayerAdmitted()&&tilt.TryTilt(Phones,player,now,rules,out value);}
        public bool Calibrate()
        {
            bool ok=PlayerAdmitted()&&tilt.Calibrate(Phones,player,Now,rules);
            notice=ok?"Calibrado. Pulsa comenzar o reanudar.":"No se pudo calibrar. "+(PlayerAdmitted()?tilt.Status:"Conecta el teléfono del jugador seleccionado.");
            return ok;
        }
        public bool BeginAttempt()
        {
            if(model.State!=LogBalanceState.Ready)return false;
            if(!keyboard&&!ReadPhone(Now,out _)){notice="Conecta y calibra el teléfono antes de comenzar.";return false;}
            paused=false;fallenTime=0;model.Begin(player);return true;
        }
        public void RestartAttempt()
        {model.Reset();tilt.Reset();paused=false;fallenTime=0;notice=keyboard?"Listo para otro intento.":"Calibra el teléfono para el nuevo intento.";}
        public void PauseAttempt(){if(model.State==LogBalanceState.Crossing||model.State==LogBalanceState.Countdown)paused=true;}
        public bool ResumeAttempt()
        {
            if(!paused||!focused||!keyboard&&!ReadPhone(Now,out _))return false;
            paused=false;return true;
        }
        void OnApplicationFocus(bool hasFocus){focused=hasFocus;if(!hasFocus)PauseAttempt();}
        void OnApplicationPause(bool value){if(value)PauseAttempt();}
        void Update()
        {
            if(UnityEngine.Input.GetKeyDown(KeyCode.Escape)){if(menu!=null)menu.CloseLogBalance();return;}
            if(UnityEngine.Input.GetKeyDown(KeyCode.R))RestartAttempt();
            if(!keyboard&&UnityEngine.Input.GetKeyDown(KeyCode.C))Calibrate();
            if(UnityEngine.Input.GetKeyDown(KeyCode.Space)){if(paused)ResumeAttempt();else BeginAttempt();}
            float input=0;
            if(keyboard)
            {if(UnityEngine.Input.GetKey(KeyCode.A)||UnityEngine.Input.GetKey(KeyCode.LeftArrow))input-=1;if(UnityEngine.Input.GetKey(KeyCode.D)||UnityEngine.Input.GetKey(KeyCode.RightArrow))input+=1;}
            bool available=keyboard||ReadPhone(Now,out input);
            if(!available){PauseAttempt();if(!keyboard)notice=PlayerAdmitted()?tilt.Status:"Conecta el teléfono del jugador seleccionado.";}
            if(!paused&&focused&&available)model.Tick(Mathf.Min(.1f,Time.unscaledDeltaTime),input);
            if(model.State==LogBalanceState.Fell)fallenTime+=Time.unscaledDeltaTime;
            menu?.Runtime?.java?.PresentPhoneGame("log-balance",paused?"Paused":model.State.ToString(),player);
            stage.Present(model.Progress,model.Balance,model.State==LogBalanceState.Fell,fallenTime,model.Elapsed,
                model.State==LogBalanceState.Crossing&&!paused&&focused,model.State==LogBalanceState.Won);
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
            GUI.Label(new Rect(40,75,width-310,60),"Eres el náufrago: cruza el río y supera la prueba de los gorilas.\nAvanzas automáticamente. Inclina el teléfono para corregir el equilibrio.",body);
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
            float danger=Mathf.InverseLerp(rules.fallThreshold*.55f,rules.fallThreshold,Mathf.Abs(model.Balance));
            Color bar=Color.Lerp(new Color(.2f,.7f,.45f),new Color(.95f,.25f,.12f),danger);
            if(model.State==LogBalanceState.Crossing&&!paused&&danger>0)
            {Fill(new Rect(0,0,7,height),new Color(.9f,.2f,.1f,danger*.7f));Fill(new Rect(width-7,0,7,height),new Color(.9f,.2f,.1f,danger*.7f));}
            Fill(new Rect(x,y+40,540,24),new Color(.25f,.28f,.3f));
            float safe=rules.fallThreshold/rules.balanceClamp;
            Fill(new Rect(x+270*(1-safe),y+40,540*safe,24),bar);
            Fill(new Rect(x+270*(1+model.Balance/rules.balanceClamp)-5,y+32,10,40),Color.white);
            GUI.Label(new Rect(x,y+83,540,32),$"Progreso {model.Progress:P0} · {model.Distance:F1} / {rules.length:F0} m · {model.Elapsed:F1} s",body);
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
                if(!keyboard&&GUI.Button(new Rect(x,y+185,260,45),"CALIBRAR (C)",button))Calibrate();
                if(GUI.Button(new Rect(x+(keyboard?0:280),y+185,keyboard?540:260,45),"COMENZAR (ESPACIO)",button))BeginAttempt();
            }
            else if(paused)
            {
                GUI.Label(new Rect(x,y+120,540,60),"PAUSA · "+(focused?notice:"Vuelve a la ventana para continuar."),body);
                if(!keyboard&&GUI.Button(new Rect(x,y+195,260,45),"CALIBRAR (C)",button))Calibrate();
                if(GUI.Button(new Rect(x+(keyboard?0:280),y+195,keyboard?540:260,45),"REANUDAR",button))ResumeAttempt();
            }
            else GUI.Label(new Rect(x,y+125,540,70),model.State==LogBalanceState.Countdown?$"Comienza en {Mathf.CeilToInt(model.Countdown)}…":"Corrige hacia el centro · R reinicia",title);
        }
        public static bool IsGameplayKey(KeyCode key) => key==KeyCode.Space||key==KeyCode.R||key==KeyCode.C
            ||key==KeyCode.Escape||key==KeyCode.A||key==KeyCode.D||key==KeyCode.LeftArrow||key==KeyCode.RightArrow;
        static void Fill(Rect rect,Color value){GUI.color=value;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=Color.white;}
        void OnDestroy(){menu?.Runtime?.java?.PresentPhoneGame("lobby","Ready",0);stage?.Dispose();}
    }
}
