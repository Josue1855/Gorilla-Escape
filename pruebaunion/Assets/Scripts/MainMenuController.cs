
using GorillaEscape.Gameplay.Input;
using UnityEngine;

public sealed class MainMenuController : MonoBehaviour
{
    private static readonly (string FirstLine, string SecondLine, string Control, string Scene)[] Games =
    {
        ("COCONUT", "THROW", "CELULAR Y CÁMARA", SceneNavigator.CoconutThrow),
        ("BANANA", "BOWLING", "CELULAR Y CÁMARA", SceneNavigator.Bowling),
        ("COCONUT", "SLICE", "CÁMARA · DOS MANOS", SceneNavigator.CoconutSlice),
        ("LOG", "BALANCE", "CELULAR", SceneNavigator.LogBalance),
        ("GORILLA", "BOXING", "CELULAR Y CÁMARA", SceneNavigator.Boxing),
        ("JUNGLE", "BASEBALL", "CELULAR Y CÁMARA", SceneNavigator.Baseball),
        ("JUNGLE", "DRUMS", "CELULAR Y CÁMARA", SceneNavigator.Drums),
        ("GORILLA", "WHACK", "CÁMARA Y CELULAR", SceneNavigator.WhackAMole),
        ("JUNGLE", "KEEPER", "CÁMARA · MANOS Y TORSO", SceneNavigator.JungleKeeper)
    };

    private static readonly int[] AllGames = { 0, 1, 2, 3, 4, 5, 6, 7, 8 };
    private static readonly int[] CameraGames = { 2, 8 };
    private const int Connection = 100;
    private const int CameraMode = 101;
    private const int Recenter = 102;

    private readonly HandDwellNavigator handNavigator = new HandDwellNavigator();
    private readonly Rect[] cards = new Rect[Games.Length];
    private readonly HandDwellTarget[] fullTargets = new HandDwellTarget[AllGames.Length + 3];
    private readonly HandDwellTarget[] cameraTargets = new HandDwellTarget[CameraGames.Length + 3];
    private MotionSession session;
    private PhoneMenuPointer pointer;
    private GUIStyle cardTitle, selectedTitle, subtitle, footerButton;
    private int styleHeight;
    private bool cameraOnly, loading;
    private float readyAt;

    private void Awake()
    {
        session = MotionSession.Ensure();
        cameraOnly = !session.PhoneConnected;
        JungleAudioBank.Attach2DSource(gameObject);
        StartMenuMusic();
        pointer = gameObject.AddComponent<PhoneMenuPointer>();
        readyAt = Time.unscaledTime + 1.2f;
    }

    private void StartMenuMusic()
    {
        AudioClip clip = Resources.Load<AudioClip>("Audio/JungleBreeze");
        if (clip == null) return;

        var musicObject = new GameObject("Música tropical del menú");
        musicObject.transform.SetParent(transform, false);
        var music = musicObject.AddComponent<AudioSource>();
        music.playOnAwake = false;
        music.spatialBlend = 0f;
        music.loop = true;
        music.volume = .28f * JungleAudioBank.MusicVolume;
        music.clip = clip;
        music.Play();
    }
    private void EnsureStyles(float width, float height)
    {
        if (cardTitle != null && styleHeight == Screen.height) return;
        styleHeight = Screen.height;
        int fontSize = Mathf.Max(12, Mathf.RoundToInt(Mathf.Min(height * .044f, width * .16f)));
        cardTitle = new GUIStyle(JungleUITheme.Title)
        {
            fontSize = fontSize,
            wordWrap = false,
            normal = { textColor = new Color(1f, .96f, .86f) }
        };
        selectedTitle = new GUIStyle(cardTitle) { normal = { textColor = new Color(1f, .86f, .18f) } };
        subtitle = new GUIStyle(JungleUITheme.Body) { fontSize = Mathf.Max(12, Mathf.RoundToInt(height * .025f)) };
        footerButton = new GUIStyle(JungleUITheme.Button) { fontSize = Mathf.Max(12, Mathf.RoundToInt(height * .025f)) };
    }

    private void Activate(int id)
    {
        if (loading || id < 0) return;
        JungleAudioBank.PlayUiClick();
        handNavigator.Reset();
        if (id == Recenter)
        {
            pointer.Recenter();
            return;
        }
        if (id == CameraMode)
        {
            cameraOnly = !cameraOnly;
            readyAt = Time.unscaledTime + .65f;
            return;
        }
        loading = true;
        SceneNavigator.Load(id == Connection ? SceneNavigator.Connection : Games[id].Scene);
    }

    private void OnGUI()
    {
        JungleUITheme.Ensure();
        JungleUITheme.DrawBackdrop(true);
        float w = Screen.width, h = Screen.height;
        const int columns = 5;
        float gap = w * .015f;
        float cardWidth = (w * .92f - gap * (columns - 1)) / columns;
        EnsureStyles(cardWidth, h);

        int[] visible = cameraOnly ? CameraGames : AllGames;
        HandDwellTarget[] targets = cameraOnly ? cameraTargets : fullTargets;
        for (int i = 0; i < visible.Length; i++)
        {
            int count = Mathf.Min(columns, visible.Length - i / columns * columns);
            float rowWidth = count * cardWidth + (count - 1) * gap;
            Rect card = new Rect((w - rowWidth) * .5f + i % columns * (cardWidth + gap),
                h * (.225f + i / columns * .265f), cardWidth, h * .225f);
            cards[visible[i]] = card;
            targets[i] = Target(visible[i], card);
        }
        Rect connection = new Rect(w * .15f, h * .755f, w * .34f, h * .07f);
        Rect mode = new Rect(w * .51f, h * .755f, w * .34f, h * .07f);
        Rect recenter = new Rect(w * .33f, h * .845f, w * .34f, h * .07f);
        targets[visible.Length] = Target(Connection, connection);
        targets[visible.Length + 1] = Target(CameraMode, mode);
        targets[visible.Length + 2] = Target(Recenter, recenter);

        bool usePhone = pointer.Connected && !cameraOnly;
        var pose = session.LatestPose;
        bool right = pose?.rightDetected == true;
        bool hasHand = right || pose?.leftDetected == true;
        bool useHand = !usePhone && hasHand;
        bool raised = false;
        int fired = -1;
        if (Event.current.type == EventType.Repaint)
        {
            if (useHand)
            {
                float handX = right ? pose.rightWristX : pose.leftWristX;
                float handY = right ? pose.rightWristY : pose.leftWristY;
                HandCursorMapping.Map(handX, handY, out float cursorX, out float cursorY);
                raised = handY < pose.bodyY;
                handNavigator.ObserveCursor(Time.unscaledTime >= readyAt, cursorX * w, cursorY * h,
                    Time.unscaledTime, targets, out fired, HandDwellNavigator.DefaultDwellSeconds, chargeActive: raised);
                JungleAudioBank.PlayDwellProgress(handNavigator.Progress);
            }
            else
            {
                handNavigator.Reset();
                JungleAudioBank.PlayDwellProgress(0f);
            }
        }

        Vector2 aim = usePhone ? pointer.ScreenPosition : Event.current.mousePosition;
        int selected = useHand ? handNavigator.HoveredId : FindTarget(targets, aim);

        Rect header = new Rect(w * .20f, h * .035f, w * .60f, h * .155f);
        JungleUITheme.DrawPanel(header);
        JungleUITheme.DrawFittedLabel(new Rect(header.x + 12f, h * .042f, header.width - 24f, h * .095f),
            "GORILLA ESCAPE", JungleUITheme.Title);
        string hint = usePhone ? "APUNTA CON EL CELULAR Y TOCA ACEPTAR"
            : useHand ? "LEVANTA LA MANO Y MANTÉNLA SOBRE UN DESAFÍO"
            : "ELIGE TU DESAFÍO EN LA SELVA";
        JungleUITheme.DrawFittedLabel(new Rect(header.x + 16f, h * .132f, header.width - 32f, h * .048f), hint, subtitle);

        foreach (int id in visible)
        {
            Rect card = cards[id];
            bool hovered = selected == id;
            JungleUITheme.DrawPanel(card, hovered);
            float lineHeight = card.height * .25f;
            GUIStyle title = hovered ? selectedTitle : cardTitle;
            JungleUITheme.DrawFittedLabel(new Rect(card.x + 12f, card.y + card.height * .12f, card.width - 24f, lineHeight), Games[id].FirstLine, title);
            JungleUITheme.DrawFittedLabel(new Rect(card.x + 12f, card.y + card.height * .37f, card.width - 24f, lineHeight), Games[id].SecondLine, title);
            JungleUITheme.DrawBadge(new Rect(card.x + card.width * .09f, card.yMax - card.height * .30f,
                card.width * .82f, card.height * .20f), Games[id].Control, hovered);
            DrawProgress(card, id);
        }

        DrawFooter(connection, "CONEXIÓN Y CALIBRACIÓN", Connection, selected);
        DrawFooter(mode, cameraOnly ? "VER TODOS LOS JUEGOS" : "JUEGOS SIN CELULAR", CameraMode, selected);
        DrawFooter(recenter, "RECENTRAR CONTROL", Recenter, selected);

        if (usePhone) JungleUITheme.DrawGameHandSquare(pointer.Position, JungleUITheme.Gold);
        else if (useHand) JungleUITheme.DrawNavigatorHandCursor(handNavigator, right ? "DER" : "IZQ", raised);

        if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
        {
            int clicked = FindTarget(targets, Event.current.mousePosition);
            if (clicked < 0) return;
            Event.current.Use();
            Activate(clicked);
        }
        else if (Event.current.type == EventType.Repaint)
        {
            bool confirmed = pointer.TakeConfirm();
            if (usePhone && confirmed) Activate(selected);
            else if (fired >= 0) Activate(fired);
        }
    }

    private void DrawFooter(Rect rect, string label, int id, int selected)
    {
        JungleUITheme.DrawPanel(rect, id == selected);
        JungleUITheme.DrawFittedLabel(rect, label, footerButton);
        DrawProgress(rect, id);
    }

    private void DrawProgress(Rect rect, int id)
    {
        if (handNavigator.HoveredId == id && handNavigator.Progress > .02f)
            JungleUITheme.DrawDwellOverlay(rect, handNavigator.Progress);
    }

    private static HandDwellTarget Target(int id, Rect rect) => new HandDwellTarget(id, rect.x, rect.y, rect.width, rect.height);

    private static int FindTarget(HandDwellTarget[] targets, Vector2 point)
    {
        foreach (HandDwellTarget target in targets)
            if (target.Contains(point.x, point.y)) return target.Id;
        return -1;
    }
}

