using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

internal sealed class PartyMode : MonoBehaviour
{
    [Serializable] internal sealed class Player { public string id, name; public long seen; }
    [Serializable] private sealed class Lobby { public Player[] players; public string active; }
    private enum View { Choice, Lobby, Catalog, Ready, Playing, Result, Scores }
    private static PartyMode instance;
    private View view = View.Choice;
    private Lobby lobby = new Lobby { players = Array.Empty<Player>() };
    private Player[] roster;
    private float[] scores;
    private int turn;
    private object[] rounds;
    private bool[] completed;
    private Action<object> restoreRound;
    private string game;
    private bool busy, isParty;
    private Texture2D qr;
    private Texture2D backdrop;
    private PhoneMenuPointer pointer;
    private string error;

    internal static bool Blocking => instance != null && instance.isParty &&
        instance.view is View.Ready or View.Result or View.Scores;
    internal static void Ensure()
    {
        if (instance != null) return;
        var host = new GameObject("Party · turnos");
        DontDestroyOnLoad(host);
        instance = host.AddComponent<PartyMode>();
        instance.backdrop = Resources.Load<Texture2D>("Jungle/PartyMenuBackground");
        instance.pointer = host.AddComponent<PhoneMenuPointer>();
        instance.StartCoroutine(instance.RefreshLobby());
    }
    private string Endpoint => "http://127.0.0.1:8788/party?room=" + MotionSession.Instance.RoomCode;

    private IEnumerator RefreshLobby()
    {
        while (true)
        {
            if (view == View.Lobby)
            {
                using var request = UnityWebRequest.Get(Endpoint);
                request.timeout = 3;
                yield return request.SendWebRequest();
                if (request.result == UnityWebRequest.Result.Success)
                    lobby = JsonUtility.FromJson<Lobby>(request.downloadHandler.text);
                if (qr == null)
                {
                    string url = PortableServiceHost.Ensure().ControllerUrlForRoom(MotionSession.Instance.RoomCode);
                    if (!string.IsNullOrEmpty(url))
                    {
                        using var image = UnityWebRequestTexture.GetTexture("http://127.0.0.1:8790/qr.png?url=" + UnityWebRequest.EscapeURL(url));
                        image.timeout = 3;
                        yield return image.SendWebRequest();
                        if (image.result == UnityWebRequest.Result.Success) qr = DownloadHandlerTexture.GetContent(image);
                    }
                }
            }
            yield return new WaitForSecondsRealtime(.5f);
        }
    }

    private IEnumerator Configure(string active, bool locked, bool reset, Action done, bool disable = false)
    {
        busy = true;
        string json = "{\"active\":\"" + active + "\",\"locked\":" + (locked ? "true" : "false") +
            ",\"reset\":" + (reset ? "true" : "false") + ",\"disable\":" + (disable ? "true" : "false") + "}";
        using var request = new UnityWebRequest(Endpoint, "POST");
        request.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = 3;
        yield return request.SendWebRequest();
        busy = false;
        if (request.result != UnityWebRequest.Result.Success) { error = "No se pudo actualizar la sala. Intenta otra vez."; yield break; }
        error = null;
        MotionSession.Instance.ResetPartyPhone();
        Time.timeScale = 1f;
        done();
    }

    internal static bool DrawMenu()
    {
        Ensure();
        if (instance.view is View.Catalog or View.Playing) return false;
        instance.DrawPanel();
        return true;
    }
    internal static void OpenFreePlay()
    {
        Ensure();
        instance.isParty = false;
        instance.view = View.Choice;
        Time.timeScale = 1f;
        SceneNavigator.Load(SceneNavigator.MainMenu);
    }

    internal static void DrawModeButton()
    {
        if (instance == null) return;
        if (GUI.Button(new Rect(Screen.width*.76f, Screen.height*.025f, Screen.width*.21f, Screen.height*.065f),
            instance.isParty ? "MODO PARTY · JUGADORES" : "CAMBIAR MODO / PARTY", JungleUITheme.Button))
            instance.StartCoroutine(instance.Configure("", false, true, () => {
                instance.isParty=false;
                instance.view=View.Choice;
            }, true));
    }
    internal static bool Launch(string scene)
    {
        if (instance == null || !instance.isParty) return false;
        instance.game = scene;
        instance.turn = 0;
        instance.rounds = new object[instance.roster.Length];
        instance.completed = new bool[instance.roster.Length];
        instance.restoreRound = null;
        instance.scores = new float[instance.roster.Length];
        instance.SelectTurn();
        return true;
    }
    private void SelectTurn()
    {
        view = View.Ready;
        StartCoroutine(Configure(roster[turn].id, true, false, () => {
            if (restoreRound == null) SceneNavigator.Load(game);
            else restoreRound(rounds[turn]);
        }));
    }
    internal static bool Round(float score, object snapshot, bool finished, Action<object> restore)
    {
        if (instance == null || !instance.isParty || instance.view != View.Playing) return false;
        instance.rounds[instance.turn] = snapshot;
        instance.completed[instance.turn] = finished;
        instance.restoreRound = restore;
        return Report(score);
    }

    internal static bool Report(float score)
    {
        if (instance == null || !instance.isParty)
        {
            GameResultRecorder.Record(score);
            return false;
        }
        if (instance.view != View.Playing) return false;
        if (instance.restoreRound == null || instance.completed[instance.turn])
            GameResultRecorder.Record(score, instance.roster[instance.turn].id);
        instance.scores[instance.turn] = score;
        if (instance.restoreRound == null) instance.completed[instance.turn] = true;
        instance.view = View.Result;
        return true;
    }
    internal static bool Exit()
    {
        if (instance == null || !instance.isParty) return false;
        Time.timeScale = 1f;
        instance.view = View.Catalog;
        return false;
    }
    private void Update()
    {
        if (isParty) Time.timeScale = Blocking ? 0f : 1f;
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
        if (qr != null) Destroy(qr);
        if (instance == this) instance = null;
    }

    private void OnGUI()
    {
        if (SceneManager.GetActiveScene().name == SceneNavigator.MainMenu) return;
        if (Blocking) DrawPanel();
        else if (isParty && view == View.Playing)
        {
            JungleUITheme.Ensure();
            GUI.Label(new Rect(Screen.width*.36f, Screen.height*.15f, Screen.width*.28f, Screen.height*.055f),
                roster[turn].name + " · TURNO " + (turn + 1) + "/" + roster.Length, JungleUITheme.Body);
        }
    }
    private void DrawPanel()
    {
        JungleUITheme.Ensure();
        float w = Screen.width, h = Screen.height;
        GUI.depth = -120;
        if (backdrop != null) GUI.DrawTexture(new Rect(0,0,w,h), backdrop, ScaleMode.ScaleAndCrop);
        else JungleUITheme.DrawBackdrop(true);
        JungleUITheme.DrawPanel(view == View.Choice ? new Rect(w*.23f,h*.19f,w*.54f,h*.60f) : new Rect(w*.12f,h*.08f,w*.76f,h*.84f));
        var title = new GUIStyle(JungleUITheme.Title) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(h*.05f) };
        var body = new GUIStyle(JungleUITheme.Body) { alignment = TextAnchor.MiddleCenter, wordWrap = true, fontSize = Mathf.RoundToInt(h*.03f) };
        string heading = view switch
        {
            View.Choice => "JUEGO LIBRE",
            View.Lobby => "PARTY · HASTA 4 JUGADORES",
            View.Ready => "TURNO DE " + roster[turn].name.ToUpperInvariant(),
            View.Result => roster[turn].name + " · " + scores[turn].ToString("0.##"),
            _ => "RESULTADOS DE LA COMPETICIÓN"
        };
        GUI.Label(new Rect(w*.18f,h*.12f,w*.64f,h*.1f),heading,title);
        GUI.enabled = !busy;
        if (view == View.Choice)
        {
            bool confirm = Event.current.type == EventType.Repaint && pointer.TakeConfirm();
            Rect soloButton = new Rect(w*.3f,h*.36f,w*.4f,h*.12f);
            Rect partyButton = new Rect(w*.3f,h*.54f,w*.4f,h*.12f);
            bool soloSelected = soloButton.Contains(pointer.ScreenPosition);
            bool partySelected = partyButton.Contains(pointer.ScreenPosition);
            if ((confirm && soloSelected) || GUI.Button(soloButton,"UN JUGADOR",soloSelected ? JungleUITheme.ButtonSelected : JungleUITheme.Button))
                StartCoroutine(Configure("",false,true,()=> { isParty=false; view=View.Catalog; }, true));
            if ((confirm && partySelected) || GUI.Button(partyButton,"MODO PARTY",partySelected ? JungleUITheme.ButtonSelected : JungleUITheme.Button))
                StartCoroutine(Configure("",false,true,()=> { isParty=true; view=View.Lobby; }));
        }
        else if (view == View.Lobby)
        {
            if (qr != null) GUI.DrawTexture(new Rect(w*.2f,h*.29f,h*.36f,h*.36f),qr,ScaleMode.ScaleToFit);
            string names = "Todos escaneen este QR y activen el movimiento.\n";
            foreach (var player in lobby.players) names += "\n" + player.name;
            GUI.Label(new Rect(w*.49f,h*.27f,w*.31f,h*.4f),names,body);
            GUI.enabled = !busy && lobby.players.Length >= 2;
            if (MotionSession.PrimaryButton(new Rect(w*.3f,h*.74f,w*.4f,h*.09f),"ELEGIR JUEGO",JungleUITheme.Button))
            {
                roster = lobby.players;
                StartCoroutine(Configure(roster[0].id,true,false,()=>view=View.Catalog));
            }
        }
        else if (view == View.Ready)
        {
            GUI.Label(new Rect(w*.23f,h*.33f,w*.54f,h*.22f),
                "Colócate frente a la cámara. Los demás esperan fuera del encuadre.\nPulsa ACEPTAR en tu celular cuando estés listo.",body);
            GUI.enabled = !busy && MotionSession.Instance.PhoneConnected;
            if (MotionSession.PrimaryButton(new Rect(w*.3f,h*.68f,w*.4f,h*.1f),"ESTOY LISTO",JungleUITheme.Button)) view=View.Playing;
        }
        else if (view == View.Result)
        {
            GUI.Label(new Rect(w*.23f,h*.33f,w*.54f,h*.2f),"Puntuación guardada",body);
            if (MotionSession.PrimaryButton(new Rect(w*.3f,h*.65f,w*.4f,h*.1f),"CONTINUAR",JungleUITheme.Button))
            {
                if (Array.TrueForAll(completed, done => done)) view=View.Scores;
                else
                {
                    do { turn = (turn + 1) % roster.Length; } while (completed[turn]);
                    SelectTurn();
                }
            }
        }
        else if (view == View.Scores)
        {
            var order = new int[roster.Length];
            for (int i=0;i<order.Length;i++) order[i]=i;
            Array.Sort(order,(a,b)=>scores[b].CompareTo(scores[a]));
            string text="";
            foreach(int i in order) text += (scores[i]==scores[order[0]] ? "★ " : "") + roster[i].name + "    " + scores[i].ToString("0.##") + "\n";
            GUI.Label(new Rect(w*.23f,h*.29f,w*.54f,h*.32f),text,body);
            if (MotionSession.PrimaryButton(new Rect(w*.3f,h*.7f,w*.4f,h*.1f),"ELEGIR OTRO JUEGO",JungleUITheme.Button))
            { view=View.Catalog; SceneNavigator.Load(SceneNavigator.MainMenu); }
        }
        GUI.enabled = !busy;
        if (view != View.Choice && GUI.Button(new Rect(w*.16f,h*.83f,w*.24f,h*.055f),"SALIR DE PARTY",JungleUITheme.Button))
            StartCoroutine(Configure("",false,true,()=> {isParty=false; view=View.Choice; SceneNavigator.Load(SceneNavigator.MainMenu);}, true));
        GUI.enabled=true;
        if (error != null) GUI.Label(new Rect(w*.42f,h*.83f,w*.4f,h*.06f),error,body);
        if (view == View.Choice) JungleUITheme.DrawGameHandSquare(pointer.Position, JungleUITheme.Gold);
    }
}



