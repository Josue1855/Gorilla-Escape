using UnityEngine;
using UnityEngine.SceneManagement;

internal sealed class JungleStory : MonoBehaviour
{
    private enum Page { Playing, Challenge, Retry, Defeat, Victory }

    private static readonly string[] Scenes =
    {
        SceneNavigator.CoconutThrow, SceneNavigator.CoconutSlice,
        SceneNavigator.WhackAMole, SceneNavigator.Bowling
    };

    private static readonly string[] Titles =
    {
        "Coconut Throw", "Coconut Slice", "Whack-a-Mole", "Banana Bowling"
    };

    private static readonly string[] Captions =
    {
        "La tormenta se llevó la balsa. El gorila rojo señala una cuerda al otro lado del arroyo: un buen lanzamiento la puede traer de vuelta.",
        "Con la cuerda recuperada, el gorila azul trae cocos para el viaje. El pequeño se durmió entre ellos. ¡Déjalo dormir!",
        "Falta la clavija del timón. Los gorilas pequeños se la llevaron a sus escondites y la devolverán si les ganas.",
        "Kong ya tiene la balsa lista. Antes de botarla, te invita a la pista junto a la cascada. Quiere ver tu mejor partida."
    };

    private static readonly string[] Goals =
    {
        "Apunta con la mano y lanza con el celular. Llega a 25 metros en un tiro válido.",
        "Corta los cocos con ambas manos. Suma 1 600 puntos y evita los cactus y al gorila dormido.",
        "Apunta con la mano derecha y baja el celular al asomarse un gorila. Consigue 10 golpes.",
        "Haz un swing con el celular para rodar el coco. Suma 80 puntos en diez frames."
    };

    private static int chapter = -1;
    private static Page page;
    private static string result;
    private Texture2D atlas;
    private AudioSource narrator;
    private AudioSource calypso;
    private Page narratedPage = Page.Playing;

    internal static bool BlocksGame => chapter >= 0 && page != Page.Playing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        chapter = -1;
        SceneManager.sceneLoaded -= AddPage;
        SceneManager.sceneLoaded += AddPage;
    }

    internal static void Begin()
    {
        chapter = 0;
        page = Page.Challenge;
        SceneNavigator.Load(Scenes[chapter]);
    }

    private static void AddPage(Scene scene, LoadSceneMode mode)
    {
        if (chapter < 0 || scene.name != Scenes[chapter]) return;
        var panel = new GameObject("Viñetas de Historia");
        SceneManager.MoveGameObjectToScene(panel, scene);
        panel.AddComponent<JungleStory>();
    }

    private void Awake()
    {
        atlas = Resources.Load<Texture2D>("Jungle/Story/StoryPanels");
        narrator = gameObject.AddComponent<AudioSource>();
        narrator.playOnAwake = false;
        narrator.spatialBlend = 0f;
        narrator.volume = .9f * JungleAudioBank.CharacterVolume;
        calypso = gameObject.AddComponent<AudioSource>();
        calypso.playOnAwake = false;
        calypso.spatialBlend = 0f;
        calypso.loop = true;
        calypso.clip = Resources.Load<AudioClip>("Audio/Story/Calypso");
    }

    internal static bool RequestExit()
    {
        if (chapter < 0) return false;
        if (page == Page.Defeat || page == Page.Victory) ReturnToMenu();
        else page = Page.Defeat;
        return true;
    }

    internal static void Finish(string scene, bool passed, string summary)
    {
        if (chapter < 0 || page != Page.Playing || scene != Scenes[chapter]) return;
        result = summary;
        if (!passed) page = Page.Retry;
        else if (chapter == Scenes.Length - 1) page = Page.Victory;
        else
        {
            chapter++;
            page = Page.Challenge;
            SceneNavigator.Load(Scenes[chapter]);
        }
    }

    private static void ReturnToMenu()
    {
        chapter = -1;
        SplashDirector.OpenConnectedMenu();
    }

    private void Update()
    {
        if (narratedPage != page)
        {
            narratedPage = page;
            narrator.Stop();
            string clipName = page switch
            {
                Page.Challenge => Scenes[chapter],
                Page.Retry => "Retry",
                Page.Defeat => "Defeat",
                Page.Victory => "Victory",
                _ => null
            };
            narrator.clip = clipName == null ? null : Resources.Load<AudioClip>("Audio/Story/" + clipName);
            if (narrator.clip != null) narrator.Play();
        }
        if (BlocksGame && calypso.clip != null)
        {
            if (!calypso.isPlaying) calypso.Play();
            calypso.volume = JungleAudioBank.MusicVolume * (narrator.isPlaying ? .24f : .34f);
        }
        else if (calypso.isPlaying) calypso.Stop();
        JungleAudioBank.SetStoryNarrationActive(BlocksGame && (calypso.isPlaying || narrator.isPlaying));
    }
    private void OnGUI()
    {
        if (!BlocksGame) return;
        JungleUITheme.Ensure();
        float w = Screen.width, h = Screen.height;
        GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.blackTexture);
        JungleUITheme.DrawPanel(new Rect(w*.055f, h*.045f, w*.89f, h*.91f));
        Rect imageRect = new Rect(w*.078f, h*.10f, w*.47f, h*.80f);
        int imageIndex = page == Page.Victory ? 4 : page == Page.Defeat ? 5 : chapter;
        if (atlas != null)
        {
            float aspect = atlas.width / (float)atlas.height * 2f / 3f;
            float width = Mathf.Min(imageRect.width, imageRect.height * aspect);
            float height = width / aspect;
            var fitted = new Rect(imageRect.center.x - width*.5f, imageRect.center.y - height*.5f, width, height);
            GUI.DrawTextureWithTexCoords(fitted, atlas,
                new Rect(imageIndex % 3 / 3f + .002f, imageIndex < 3 ? .502f : .002f, 1f/3f - .004f, .496f));
        }

        float x = w*.565f, widthText = w*.345f;
        var title = new GUIStyle(JungleUITheme.Title)
        {
            fontSize = Mathf.RoundToInt(Mathf.Min(h*.055f, widthText*.15f)),
            wordWrap = true, alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(1f, .82f, .19f) }
        };
        var body = new GUIStyle(JungleUITheme.Body)
        {
            fontSize = Mathf.RoundToInt(Mathf.Min(h*.032f, widthText*.075f)),
            wordWrap = true, alignment = TextAnchor.UpperLeft,
            normal = { textColor = Color.white }
        };

        string heading = page switch
        {
            Page.Victory => "¡Victoria!",
            Page.Defeat => "Derrota",
            Page.Retry => "Reto no superado",
            _ => $"{chapter + 1} DE {Scenes.Length}\n{Titles[chapter]}"
        };
        string copy = page switch
        {
            Page.Victory => result + "\n\nKong empuja la balsa al agua. Desde la orilla, todos los gorilas despiden al náufrago. Cuando ya está lejos, descubre que le dejaron un coco para el camino.",
            Page.Defeat => "La balsa se queda en la playa. Los gorilas le hacen sitio al náufrago junto a la fogata. El viaje tendrá que esperar.",
            Page.Retry => result + "\n\n" + Goals[chapter],
            _ => Captions[chapter] + "\n\n" + Goals[chapter]
        };

        GUI.Label(new Rect(x, h*.14f, widthText, h*.16f), heading, title);
        GUI.Label(new Rect(x, h*.34f, widthText, h*.37f), copy, body);
        string primary = page == Page.Challenge ? "JUGAR" : page == Page.Retry ? "REINTENTAR" : "VOLVER AL MENÚ";
        if (MotionSession.PrimaryButton(new Rect(x, h*.74f, widthText, h*.075f), primary, JungleUITheme.ButtonSelected))
        {
            if (page == Page.Challenge)
            {
                page = Page.Playing;
                switch (Scenes[chapter])
                {
                    case SceneNavigator.CoconutThrow:
                        FindFirstObjectByType<CoconutThrowController>()?.StartCountdown();
                        break;
                    case SceneNavigator.CoconutSlice:
                        FindFirstObjectByType<CoconutSliceController>()?.BeginCountdown();
                        break;
                    case SceneNavigator.WhackAMole:
                        FindFirstObjectByType<WhackAMoleController>()?.Begin();
                        break;
                }
            }
            else if (page == Page.Retry)
            {
                page = Page.Challenge;
                SceneNavigator.Load(Scenes[chapter]);
            }
            else ReturnToMenu();
            return;
        }
        if (page is Page.Retry or Page.Challenge &&
            GUI.Button(new Rect(x, h*.825f, widthText, h*.065f), "DEJAR HISTORIA", JungleUITheme.Button))
            RequestExit();
    }
}




