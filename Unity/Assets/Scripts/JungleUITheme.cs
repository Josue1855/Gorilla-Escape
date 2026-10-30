using System.Collections.Generic;
using GorillaEscape.Gameplay.Input;
using UnityEngine;

public static class JungleUITheme
{
    private static bool ready;
    private static int lastHudScreenHeight = -1;
    private static readonly Dictionary<Color32, Texture2D> solidCache = new Dictionary<Color32, Texture2D>();
    private static Texture2D deepGreen, jungleGreen, wood, woodLight, gold, turquoise, shadow, leafLeft, leafRight, leafBottom, gorilla, jungleBackdrop, bambooSurface;
    private static Texture2D darkHud, orange, red, green, white, blue;
    private static Texture2D woodPanelRounded, woodPanelSelectedRounded, bambooCardRounded, buttonNormalRounded, buttonHoverRounded, buttonGoldRounded, badgeRounded, shadowRounded;
    private static Font titleFont, textFont;
    private static GUIStyle dwellCaptionStyle, cursorLabelStyle, badgeLabelStyle, plankLabelStyle;
    private static GUIStyle woodPanelBoxStyle, woodPanelSelectedBoxStyle, bambooCardBoxStyle, badgeBoxStyle, shadowBoxStyle;

    public static Font TitleFont => titleFont != null ? titleFont : (titleFont = JungleAssets.Load<Font>("Brand/LilitaOne"));
    public static Font TextFont => textFont != null ? textFont : (textFont = JungleAssets.Load<Font>("Brand/NunitoSans"));
    public static GUIStyle Title { get; private set; }
    public static GUIStyle Heading { get; private set; }
    public static GUIStyle Body { get; private set; }
    public static GUIStyle Button { get; private set; }
    public static GUIStyle ButtonSelected { get; private set; }
    public static GUIStyle ButtonGold => ButtonSelected;

    public static GUIStyle HudTitle { get; private set; }
    public static GUIStyle HudBig { get; private set; }
    public static GUIStyle HudBody { get; private set; }
    public static GUIStyle HudScore { get; private set; }
    public static GUIStyle HudSmallTag { get; private set; }
    public static GUIStyle HudHandLabel { get; private set; }

    public static Texture2D DarkHud { get { Ensure(); return darkHud; } }
    public static Texture2D Gold { get { Ensure(); return gold; } }
    public static Texture2D Turquoise { get { Ensure(); return turquoise; } }
    public static Texture2D Orange { get { Ensure(); return orange; } }
    public static Texture2D Red { get { Ensure(); return red; } }
    public static Texture2D Green { get { Ensure(); return green; } }
    public static Texture2D White { get { Ensure(); return white; } }
    public static Texture2D Blue { get { Ensure(); return blue; } }
    public static Texture2D Shadow { get { Ensure(); return shadow; } }

    public static Texture2D WoodPanelRounded { get { Ensure(); return woodPanelRounded; } }
    public static Texture2D WoodPanelSelectedRounded { get { Ensure(); return woodPanelSelectedRounded; } }
    public static Texture2D BambooCardRounded { get { Ensure(); return bambooCardRounded; } }
    public static Texture2D ButtonNormalRounded { get { Ensure(); return buttonNormalRounded; } }
    public static Texture2D ButtonHoverRounded { get { Ensure(); return buttonHoverRounded; } }
    public static Texture2D ButtonGoldRounded { get { Ensure(); return buttonGoldRounded; } }

    public static Texture2D SolidColor(Color color)
    {
        Color32 key = color;
        if (solidCache.TryGetValue(key, out Texture2D cached) && cached != null) return cached;
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "Jungle UI " + key };
        texture.SetPixel(0, 0, color);
        texture.Apply(false, true);
        solidCache[key] = texture;
        return texture;
    }

    private static Texture2D Solid(Color color) => SolidColor(color);

    private static float SampleWoodGrain(float u, float v, float seed)
    {
        // Veta horizontal recta y cepillada (sin ondas de mármol que se deformen en 9-slice)
        float warp = (Mathf.PerlinNoise(u * 1.2f + seed, v * 3.5f + seed * 1.7f) - .5f) * .22f;
        float streaks = Mathf.Sin((v * 34f + warp) * Mathf.PI) * .5f + .5f;
        float fibers = Mathf.PerlinNoise(u * 1.8f + seed * 3.1f, v * 52f + seed);
        float finePores = Mathf.PerlinNoise(u * 8f + seed, v * 95f + seed * .7f);
        return Mathf.Clamp01(streaks * .35f + fibers * .45f + finePores * .20f);
    }

    public static Texture2D CreateRoundedShadowTexture(int size, int radius, Color shadowColor)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(0f, Mathf.Max(radius - x, x - (size - radius - 1)));
                float dy = Mathf.Max(0f, Mathf.Max(radius - y, y - (size - radius - 1)));
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((radius + 1.5f - distance) / 4.5f));
                Color c = shadowColor;
                c.a *= alpha;
                texture.SetPixel(x, y, c);
            }
        }
        texture.Apply(false, true);
        return texture;
    }

    public static Texture2D CreateCarvedWoodTexture(
        int width,
        int height,
        int radius,
        Color woodDark,
        Color woodMid,
        Color woodLight,
        Color frameLight,
        Color frameDark,
        int rimWidth = 6,
        int plankCount = 3,
        bool cornerPegs = false)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        float outerRadius = radius;
        float grooveWidth = 2f;
        Color barkEdge = new Color(.11f, .045f, .012f, 1f);
        Color grooveDark = new Color(woodDark.r * .52f, woodDark.g * .48f, woodDark.b * .42f, 1f);

        for (int y = 0; y < height; y++)
        {
            float v = y / (float)Mathf.Max(1, height - 1);
            for (int x = 0; x < width; x++)
            {
                float u = x / (float)Mathf.Max(1, width - 1);
                float dx = Mathf.Max(0f, Mathf.Max(radius - x, x - (width - radius - 1)));
                float dy = Mathf.Max(0f, Mathf.Max(radius - y, y - (height - radius - 1)));
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float outerAlpha = Mathf.Clamp01(outerRadius + .65f - distance);

                if (outerAlpha <= 0f)
                {
                    texture.SetPixel(x, y, Color.clear);
                    continue;
                }

                // Distancia exacta al borde exterior redondeado en todo el perímetro
                float distRight = width - 1 - x;
                float distTop = height - 1 - y;
                float distRectEdge = Mathf.Min(Mathf.Min(x, distRight), Mathf.Min(y, distTop));
                float distCornerEdge = outerRadius - distance;
                float distFromEdge = (dx > 0f && dy > 0f) ? distCornerEdge : distRectEdge;

                Color pixel;
                if (distFromEdge < 1.8f)
                {
                    pixel = barkEdge;
                }
                else if (distFromEdge < rimWidth)
                {
                    float rimT = (distFromEdge - 1.8f) / Mathf.Max(1f, rimWidth - 1.8f);
                    float bevelArch = Mathf.Sin(rimT * Mathf.PI);
                    float lightBias = Mathf.Clamp01(v * .65f + (1f - u) * .35f);
                    Color baseFrame = Color.Lerp(frameDark, frameLight, lightBias);
                    pixel = Color.Lerp(baseFrame * .84f, baseFrame * 1.10f, bevelArch);
                    pixel.a = 1f;
                }
                else if (distFromEdge < rimWidth + grooveWidth)
                {
                    float gT = (distFromEdge - rimWidth) / grooveWidth;
                    pixel = Color.Lerp(grooveDark, woodDark * .88f, gT);
                    pixel.a = 1f;
                }
                else
                {
                    int plankIdx = plankCount > 1 ? Mathf.Clamp(Mathf.FloorToInt(v * plankCount), 0, plankCount - 1) : 0;
                    float seed = 2.1f + plankIdx * 3.7f;
                    float grain = SampleWoodGrain(u, v, seed);

                    // Contraste suave y cálido de madera cepillada
                    Color woodColor = grain < .5f
                        ? Color.Lerp(woodDark, woodMid, grain * 2f)
                        : Color.Lerp(woodMid, woodLight, (grain - .5f) * 2f);

                    float innerBevel = Mathf.Clamp01((distFromEdge - (rimWidth + grooveWidth)) / 6f);
                    float verticalShade = Mathf.Lerp(.93f, 1.06f, v);
                    woodColor *= Mathf.Lerp(.82f, 1f, innerBevel) * verticalShade;

                    if (plankCount > 1)
                    {
                        for (int p = 1; p < plankCount; p++)
                        {
                            float seamY = (height * p) / (float)plankCount;
                            float dySeam = y - seamY;
                            if (Mathf.Abs(dySeam) < 1.1f)
                                woodColor = Color.Lerp(woodColor, grooveDark, .65f);
                            else if (dySeam >= -2.5f && dySeam < -1.1f)
                                woodColor = Color.Lerp(woodColor, woodLight * 1.12f, .32f);
                        }
                    }

                    woodColor.a = woodMid.a;
                    pixel = woodColor;
                }

                pixel.a *= outerAlpha;
                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply(false, true);
        return texture;
    }

    public static Texture2D CreateRoundedTexture(int size, int radius, Color fill, Color stroke, int strokeWidth = 3)
    {
        int safeRadius = Mathf.Clamp(radius, 4, Mathf.Max(6, size / 4 - 2));
        bool isLightCard = fill.r > .78f && fill.g > .75f && fill.b > .68f;
        if (isLightCard)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            Color topFill = new Color(Mathf.Min(1f, fill.r * 1.02f), Mathf.Min(1f, fill.g * 1.01f), fill.b, fill.a);
            Color botFill = new Color(fill.r * .94f, fill.g * .91f, fill.b * .86f, fill.a);
            for (int y = 0; y < size; y++)
            {
                float v = y / (float)Mathf.Max(1, size - 1);
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0f, Mathf.Max(safeRadius - x, x - (size - safeRadius - 1)));
                    float dy = Mathf.Max(0f, Mathf.Max(safeRadius - y, y - (size - safeRadius - 1)));
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float outer = Mathf.Clamp01(safeRadius + .6f - distance);
                    float inner = Mathf.Clamp01(safeRadius - strokeWidth + .6f - distance);
                    Color body = Color.Lerp(botFill, topFill, v);
                    Color c = Color.Lerp(stroke, body, inner);
                    c.a *= outer;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply(false, true);
            return tex;
        }

        Color dark = new Color(Mathf.Max(0f, fill.r * .84f), Mathf.Max(0f, fill.g * .82f), Mathf.Max(0f, fill.b * .78f), fill.a);
        Color mid = new Color(Mathf.Min(1f, fill.r * 1.04f + .02f), Mathf.Min(1f, fill.g * 1.02f + .01f), fill.b, fill.a);
        Color light = new Color(Mathf.Min(1f, fill.r * 1.22f + .05f), Mathf.Min(1f, fill.g * 1.18f + .03f), Mathf.Min(1f, fill.b * 1.12f + .01f), fill.a);
        Color frameDark = new Color(stroke.r * .65f, stroke.g * .60f, stroke.b * .52f, 1f);
        int rim = Mathf.Clamp(strokeWidth + 2, 4, 7);
        return CreateCarvedWoodTexture(size, size, safeRadius, dark, mid, light, stroke, frameDark, rim, plankCount: 1, cornerPegs: false);
    }

    public static void Ensure()
    {
        if (!ready || deepGreen == null || woodPanelRounded == null)
        {
            ready = true;
            // Paleta "Madera Tallada, Bambú y Oro Tropical"
            deepGreen = Solid(new Color(.045f, .11f, .055f, 1f));
            jungleGreen = Solid(new Color(.075f, .21f, .095f, 1f));
            wood = Solid(new Color(.34f, .17f, .065f, .98f));
            woodLight = Solid(new Color(.52f, .27f, .10f, 1f));
            gold = Solid(new Color(1f, .78f, .12f, 1f));
            turquoise = Solid(new Color(.20f, .74f, .36f, 1f)); // Verde Jade / Hoja de Palma
            shadow = Solid(new Color(.04f, .015f, .002f, .58f));
            darkHud = Solid(new Color(.22f, .105f, .04f, .96f)); // Madera corteza cálida
            orange = Solid(new Color(.96f, .44f, .10f, .96f));
            red = Solid(new Color(.86f, .18f, .11f, .96f));
            green = Solid(new Color(.22f, .78f, .32f, .96f));
            white = Solid(new Color(1f, .96f, .86f, .96f)); // Crema pulpa de coco
            blue = Solid(new Color(.18f, .58f, .42f, .96f)); // Verde esmeralda de río

            shadowRounded = CreateRoundedShadowTexture(96, 16, new Color(.03f, .01f, 0f, .60f));

            // Tablón principal de caoba y teca cepillada (radius 16 < border 24 para esquinas 100% limpias)
            woodPanelRounded = CreateCarvedWoodTexture(
                96, 96, 16,
                new Color(.28f, .135f, .048f, .98f),
                new Color(.38f, .19f, .07f, .98f),
                new Color(.48f, .25f, .095f, .98f),
                new Color(.88f, .64f, .24f, 1f),
                new Color(.48f, .26f, .08f, 1f),
                rimWidth: 6, plankCount: 3, cornerPegs: false);

            // Tablón seleccionado iluminado con marco de oro solar
            woodPanelSelectedRounded = CreateCarvedWoodTexture(
                96, 96, 16,
                new Color(.38f, .19f, .062f, .99f),
                new Color(.50f, .26f, .092f, .99f),
                new Color(.62f, .34f, .125f, .99f),
                new Color(1f, .86f, .22f, 1f),
                new Color(.68f, .42f, .08f, 1f),
                rimWidth: 7, plankCount: 3, cornerPegs: false);

            // Sub-tablón / placa de cedro y bambú tallado (radius 12 < border 18)
            bambooCardRounded = CreateCarvedWoodTexture(
                80, 80, 12,
                new Color(.32f, .155f, .055f, .98f),
                new Color(.43f, .215f, .08f, .98f),
                new Color(.54f, .285f, .11f, .98f),
                new Color(.82f, .58f, .22f, 1f),
                new Color(.44f, .23f, .075f, 1f),
                rimWidth: 5, plankCount: 1, cornerPegs: false);

            // Botón de madera tallada (radius 12 < border 18)
            buttonNormalRounded = CreateCarvedWoodTexture(
                80, 80, 12,
                new Color(.40f, .195f, .068f, 1f),
                new Color(.52f, .265f, .098f, 1f),
                new Color(.64f, .345f, .135f, 1f),
                new Color(.94f, .70f, .28f, 1f),
                new Color(.50f, .26f, .085f, 1f),
                rimWidth: 5, plankCount: 1, cornerPegs: false);

            // Botón hoja de selva / esmeralda (hover)
            buttonHoverRounded = CreateCarvedWoodTexture(
                80, 80, 12,
                new Color(.15f, .48f, .20f, 1f),
                new Color(.22f, .64f, .28f, 1f),
                new Color(.32f, .76f, .36f, 1f),
                new Color(.82f, .98f, .48f, 1f),
                new Color(.15f, .42f, .18f, 1f),
                rimWidth: 5, plankCount: 1, cornerPegs: false);

            // Botón dorado tropical (seleccionado / activo)
            buttonGoldRounded = CreateCarvedWoodTexture(
                80, 80, 12,
                new Color(.80f, .54f, .07f, 1f),
                new Color(.94f, .70f, .12f, 1f),
                new Color(1f, .85f, .26f, 1f),
                new Color(1f, .96f, .58f, 1f),
                new Color(.66f, .42f, .06f, 1f),
                rimWidth: 5, plankCount: 1, cornerPegs: false);

            // Insignia de corteza / bambú (radius 9 < border 14)
            badgeRounded = CreateCarvedWoodTexture(
                64, 64, 9,
                new Color(.21f, .10f, .035f, .96f),
                new Color(.29f, .145f, .052f, .96f),
                new Color(.37f, .19f, .07f, .96f),
                new Color(.82f, .60f, .24f, 1f),
                new Color(.38f, .20f, .06f, 1f),
                rimWidth: 4, plankCount: 1, cornerPegs: false);

            shadowBoxStyle = new GUIStyle(JungleRetainedUI.skin.box) { normal = { background = shadowRounded }, border = new RectOffset(24, 24, 24, 24) };
            woodPanelBoxStyle = new GUIStyle(JungleRetainedUI.skin.box) { normal = { background = woodPanelRounded }, border = new RectOffset(24, 24, 24, 24) };
            woodPanelSelectedBoxStyle = new GUIStyle(JungleRetainedUI.skin.box) { normal = { background = woodPanelSelectedRounded }, border = new RectOffset(24, 24, 24, 24) };
            bambooCardBoxStyle = new GUIStyle(JungleRetainedUI.skin.box) { normal = { background = bambooCardRounded }, border = new RectOffset(18, 18, 18, 18) };
            badgeBoxStyle = new GUIStyle(JungleRetainedUI.skin.box) { normal = { background = badgeRounded }, border = new RectOffset(14, 14, 14, 14) };

            leafLeft = JungleAssets.Load<Texture2D>("Jungle/hojas_superior_izquierda");
            leafRight = JungleAssets.Load<Texture2D>("Jungle/hoja_superior_derecha");
            leafBottom = JungleAssets.Load<Texture2D>("Jungle/hojas_inferiores");
            gorilla = JungleAssets.Load<Texture2D>("Jungle/gorila_menu_hd");
            if (gorilla == null) gorilla = JungleAssets.Load<Texture2D>("Jungle/gorila_menu");
            jungleBackdrop = JungleAssets.Load<Texture2D>("Jungle/bowling_waterfall_background");
            if (jungleBackdrop == null) jungleBackdrop = JungleAssets.Load<Texture2D>("Jungle/CoconutSlice/JungleSkyBackground");
            bambooSurface = JungleAssets.Load<Texture2D>("Jungle/SurfaceTextures/BowlingLaneBamboo");
            lastHudScreenHeight = -1;
        }

        if (HudTitle == null || Title == null || lastHudScreenHeight != Screen.height)
        {
            lastHudScreenHeight = Screen.height;
            float h = Mathf.Max(480f, Screen.height);
            int titleSize = Mathf.Clamp(Mathf.RoundToInt(h * .044f), 22, 52);
            int headingSize = Mathf.Clamp(Mathf.RoundToInt(h * .030f), 18, 34);
            int bodySize = Mathf.Clamp(Mathf.RoundToInt(h * .021f), 13, 22);
            int buttonSize = Mathf.Clamp(Mathf.RoundToInt(h * .021f), 13, 22);
            int scoreSize = Mathf.Clamp(Mathf.RoundToInt(h * .028f), 15, 30);
            int bigSize = Mathf.Clamp(Mathf.RoundToInt(h * .088f), 36, 92);
            int smallFont = Mathf.Clamp(Mathf.RoundToInt(h * .016f), 11, 16);

            Title = new GUIStyle(JungleRetainedUI.skin.label)
            {
                font = TitleFont,
                fontSize = titleSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                padding = new RectOffset(10, 10, 4, 4),
                normal = { textColor = new Color(1f, .84f, .18f) }
            };
            Heading = new GUIStyle(Title)
            {
                fontSize = headingSize,
                normal = { textColor = new Color(1f, .96f, .86f) }
            };
            Body = new GUIStyle(JungleRetainedUI.skin.label)
            {
                font = TextFont,
                fontSize = bodySize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                padding = new RectOffset(12, 12, 4, 4),
                normal = { textColor = new Color(1f, .95f, .84f) }
            };
            Button = new GUIStyle(JungleRetainedUI.skin.button)
            {
                font = TextFont,
                fontSize = buttonSize,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false,
                clipping = TextClipping.Clip,
                border = new RectOffset(18, 18, 18, 18),
                padding = new RectOffset(14, 14, 6, 6),
                normal = { background = buttonNormalRounded, textColor = new Color(1f, .96f, .86f) },
                hover = { background = buttonHoverRounded, textColor = new Color(1f, .99f, .90f) },
                active = { background = buttonGoldRounded, textColor = new Color(.18f, .08f, .01f) }
            };
            ButtonSelected = new GUIStyle(Button)
            {
                normal = { background = buttonGoldRounded, textColor = new Color(.18f, .08f, .01f) },
                hover = { background = buttonGoldRounded, textColor = new Color(.18f, .08f, .01f) }
            };

            dwellCaptionStyle = new GUIStyle(Body) { fontSize = 12, padding = new RectOffset(4, 4, 1, 1), normal = { textColor = new Color(1f, .96f, .86f) } };
            cursorLabelStyle = new GUIStyle(Body) { fontSize = 12, padding = new RectOffset(2, 2, 2, 2), normal = { textColor = new Color(1f, .96f, .86f) } };
            badgeLabelStyle = new GUIStyle(Body) { fontSize = 13, wordWrap = false, clipping = TextClipping.Clip, padding = new RectOffset(8, 8, 2, 2), normal = { textColor = new Color(1f, .88f, .46f) } };
            plankLabelStyle = new GUIStyle(Body) { fontSize = bodySize, wordWrap = true, padding = new RectOffset(16, 16, 6, 6), normal = { textColor = new Color(1f, .96f, .86f) } };

            HudTitle = new GUIStyle(Title) { fontSize = titleSize, normal = { textColor = new Color(1f, .84f, .18f) } };
            HudBig = new GUIStyle(HudTitle) { fontSize = bigSize, normal = { textColor = new Color(1f, .96f, .86f) } };
            HudBody = new GUIStyle(Body) { fontSize = bodySize, normal = { textColor = new Color(1f, .96f, .86f) } };
            HudScore = new GUIStyle(HudBody) { fontSize = scoreSize, normal = { textColor = new Color(1f, .92f, .68f) } };
            HudSmallTag = new GUIStyle(HudBody) { fontSize = smallFont, normal = { textColor = new Color(1f, .88f, .46f) } };
            HudHandLabel = new GUIStyle(JungleRetainedUI.skin.label) { font = TextFont, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, normal = { textColor = new Color(.18f, .08f, .01f) } };
        }
    }

    public static void DrawShadowedLabel(Rect rect, string text, GUIStyle style, float offset = 2.5f)
    {
        if (string.IsNullOrEmpty(text) || style == null) return;
        Color prev = style.normal.textColor;
        style.normal.textColor = new Color(.08f, .03f, .005f, .90f);
        JungleRetainedUI.Label(new Rect(rect.x + offset, rect.y + offset, rect.width, rect.height), text, style);
        style.normal.textColor = prev;
        JungleRetainedUI.Label(rect, text, style);
    }

    public static void DrawPlankLabel(Rect rect, string text, bool highlighted = false)
    {
        Ensure();
        DrawSubPanel(rect);
        if (string.IsNullOrEmpty(text)) return;
        int len = Mathf.Max(10, text.Length);
        int widthFit = Mathf.RoundToInt((rect.width - 36f) / (len * .56f));
        int heightFit = Mathf.RoundToInt((rect.height - 14f) * .42f);
        plankLabelStyle.fontSize = Mathf.Clamp(Mathf.Min(heightFit, Mathf.Max(widthFit, Mathf.RoundToInt((rect.height - 14f) * .30f))), 12, 22);
        plankLabelStyle.normal.textColor = highlighted ? new Color(1f, .90f, .36f) : new Color(1f, .96f, .86f);
        DrawShadowedLabel(rect, text, plankLabelStyle, 1.8f);
    }

    public static void DrawBadge(Rect rect, string text, bool highlighted = false)
    {
        Ensure();
        JungleRetainedUI.Box(rect, GUIContent.none, highlighted ? woodPanelSelectedBoxStyle : badgeBoxStyle);
        int len = Mathf.Max(8, text != null ? text.Length : 8);
        int maxByWidth = Mathf.RoundToInt((rect.width - 18f) / (len * .58f));
        int maxByHeight = Mathf.RoundToInt((rect.height - 6f) * .48f);
        badgeLabelStyle.fontSize = Mathf.Clamp(Mathf.Min(maxByWidth, maxByHeight), 10, 16);
        badgeLabelStyle.normal.textColor = highlighted ? new Color(1f, .94f, .52f) : new Color(.96f, .84f, .56f);
        DrawShadowedLabel(rect, text, badgeLabelStyle, 1.2f);
    }

    public static Vector2 VisualHand(MotionSession session, HandVisualPrediction prediction, Vector2 observed)
    {
        if (session != null && session.IsInputReady && prediction != null && prediction.TryPosition(Time.unscaledTime, out float x, out float y))
            return new Vector2(x, y);
        return observed;
    }

    public static void DrawGameHandSquare(Vector2 viewportPos, Texture2D fill, string label = null, bool invertY = false)
    {
        Ensure();
        float size = Mathf.Max(34f, Screen.height * .052f);
        float screenY = invertY ? (1f - viewportPos.y) * Screen.height : viewportPos.y * Screen.height;
        Rect rect = new Rect(viewportPos.x * Screen.width - size * .5f, screenY - size * .5f, size, size);
        JungleRetainedUI.Box(new Rect(rect.x + 3f, rect.y + 4f, rect.width, rect.height), GUIContent.none, shadowBoxStyle);
        JungleRetainedUI.Box(rect, GUIContent.none, fill == orange ? woodPanelSelectedBoxStyle : bambooCardBoxStyle);
        Rect inner = new Rect(rect.x + 7f, rect.y + 7f, rect.width - 14f, rect.height - 14f);
        JungleRetainedUI.DrawTexture(inner, fill != null ? fill : turquoise);
        if (!string.IsNullOrEmpty(label))
        {
            HudHandLabel.fontSize = Mathf.Clamp(Mathf.RoundToInt(size * .24f), 10, 15);
            JungleRetainedUI.Label(rect, label, HudHandLabel);
        }
    }

    public static void DrawBackdrop(bool showGorilla = false)
    {
        Ensure();
        float w = Screen.width, h = Screen.height;
        float time = Time.unscaledTime;
        float drift = Mathf.Sin(time * .18f) * 6f;
        JungleRetainedUI.DrawTexture(new Rect(0, 0, w, h), deepGreen);
        if (jungleBackdrop != null)
            JungleRetainedUI.DrawTexture(new Rect(-10f + drift, -6f, w + 20f, h + 12f), jungleBackdrop, ScaleMode.ScaleAndCrop, true);
        else
            JungleRetainedUI.DrawTexture(new Rect(0, h * .18f, w, h * .82f), jungleGreen);

        if (showGorilla && gorilla != null)
            JungleRetainedUI.DrawTexture(new Rect(-8f - drift * .6f, -6f, w + 16f, h + 12f), gorilla, ScaleMode.ScaleAndCrop, true);

        Color prev = JungleRetainedUI.color;
        JungleRetainedUI.color = new Color(.10f, .05f, .015f, showGorilla ? .36f : .48f);
        JungleRetainedUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
        JungleRetainedUI.color = prev;
        DrawLeaves();
    }

    public static void DrawLeaves()
    {
        Ensure();
        float w = Screen.width, h = Screen.height;
        float time = Time.unscaledTime;
        if (leafLeft != null)
            DrawSway(leafLeft, new Rect(-w * .015f, -h * .018f, w * .26f, h * .23f), Mathf.Sin(time * .65f) * 2.2f, new Vector2(0f, 0f));
        if (leafRight != null)
            DrawSway(leafRight, new Rect(w * .755f, -h * .016f, w * .255f, h * .22f), Mathf.Sin(time * .58f + 1.3f) * 2.6f, new Vector2(w, 0f));
        if (leafBottom != null)
        {
            float bob = Mathf.Sin(time * .78f) * (h * .004f);
            DrawSway(leafBottom, new Rect(-w * .015f, h * .82f + bob, w * .29f, h * .19f), Mathf.Sin(time * .52f) * 1.8f, new Vector2(0f, h));
            DrawSway(leafBottom, new Rect(w * .725f, h * .82f - bob, w * .29f, h * .19f), -Mathf.Sin(time * .55f + .8f) * 1.8f, new Vector2(w, h));
        }
    }

    private static void DrawSway(Texture texture, Rect rect, float angle, Vector2 pivot)
    {
        Matrix4x4 previous = JungleRetainedUI.matrix;
        JungleRetainedUI.RotateAroundPivot(angle, pivot);
        JungleRetainedUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true);
        JungleRetainedUI.matrix = previous;
    }

    public static void DrawPanel(Rect rect, bool selected = false)
    {
        Ensure();
        JungleRetainedUI.Box(new Rect(rect.x + 7f, rect.y + 9f, rect.width, rect.height), GUIContent.none, shadowBoxStyle);
        JungleRetainedUI.Box(rect, GUIContent.none, selected ? woodPanelSelectedBoxStyle : woodPanelBoxStyle);
    }

    public static void DrawSubPanel(Rect rect)
    {
        Ensure();
        JungleRetainedUI.Box(rect, GUIContent.none, bambooCardBoxStyle);
    }

    public static void DrawDwellOverlay(Rect rect, float progress, string caption = null)
    {
        Ensure();
        float p = Mathf.Clamp01(progress);
        if (p <= 0f) return;
        float barHeight = Mathf.Clamp(rect.height * .13f, 10f, 22f);
        Rect track = new Rect(rect.x + 12f, rect.yMax - barHeight - 8f, Mathf.Max(16f, rect.width - 24f), barHeight);
        JungleRetainedUI.DrawTexture(track, shadow);
        JungleRetainedUI.DrawTexture(new Rect(track.x + 2f, track.y + 2f, (track.width - 4f) * p, track.height - 4f), p > .72f ? gold : green);
        if (!string.IsNullOrEmpty(caption) && track.height >= 14f)
        {
            dwellCaptionStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(barHeight * .72f), 10, 15);
            DrawShadowedLabel(track, caption, dwellCaptionStyle, 1.5f);
        }
    }

    public static void DrawHandCursor(Vector2 screenPos, string handLabel, float dwellProgress, bool raised = false)
    {
        Ensure();
        float size = Mathf.Clamp(Screen.height * .064f, 40f, 66f);
        float pulse = 1f + Mathf.Sin(Time.unscaledTime * 7f) * .05f + Mathf.Clamp01(dwellProgress) * .18f;
        float finalSize = size * pulse;
        Rect outer = new Rect(screenPos.x - finalSize * .5f, screenPos.y - finalSize * .5f, finalSize, finalSize);
        JungleRetainedUI.Box(new Rect(outer.x + 3f, outer.y + 5f, outer.width, outer.height), GUIContent.none, shadowBoxStyle);
        JungleRetainedUI.Box(outer, GUIContent.none, raised || dwellProgress > .05f ? woodPanelSelectedBoxStyle : woodPanelBoxStyle);
        if (dwellProgress > .01f)
        {
            Rect inner = new Rect(outer.x + 6f, outer.y + 6f, outer.width - 12f, outer.height - 12f);
            float fillH = inner.height * Mathf.Clamp01(dwellProgress);
            JungleRetainedUI.DrawTexture(new Rect(inner.x, inner.yMax - fillH, inner.width, fillH), dwellProgress > .72f ? gold : green);
        }
        cursorLabelStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(size * .24f), 10, 15);
        DrawShadowedLabel(outer, string.IsNullOrEmpty(handLabel) ? "MANO" : handLabel.Replace("MANO ", ""), cursorLabelStyle, 1.5f);
    }

    public static void DrawNavigatorHandCursor(HandDwellNavigator navigator, string handLabel, bool handRaised)
    {
        if (navigator == null || !navigator.CursorVisible) return;
        int prevDepth = JungleRetainedUI.depth;
        JungleRetainedUI.depth = -150;
        DrawHandCursor(new Vector2(navigator.CursorX, navigator.CursorY), handLabel, navigator.Progress, handRaised);
        JungleRetainedUI.depth = prevDepth;
    }
}
