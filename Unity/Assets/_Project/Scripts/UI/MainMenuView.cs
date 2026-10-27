using System;
using UnityEngine;

namespace GorillaEscape.UI
{
    /// <summary>
    /// Tropical Jungle Presentation for Gorilla Escape (matching the reference art direction):
    /// Lush island backdrop, 3D chunky golden logo on wooden signboard with leaves,
    /// and stacked carved wooden plank buttons with nail bolts and colorful icon badges.
    /// </summary>
    public sealed class MainMenuView
    {
        readonly MainMenuController controller;

        // Wood Plank Palette
        static readonly Color WoodFace = new Color(0.48f, 0.28f, 0.14f);        // #7a4724 Warm teak plank
        static readonly Color WoodLight = new Color(0.64f, 0.40f, 0.22f);       // #a36638 Sunlit top bevel
        static readonly Color WoodDark = new Color(0.24f, 0.12f, 0.05f);        // #3d1f0d Shadow under plank
        static readonly Color WoodHover = new Color(0.56f, 0.34f, 0.18f);       // Highlighted plank on hover

        // Typography Colors
        static readonly Color GoldText = new Color(.91f, .72f, .37f);         // #ffc733 3D Gold Letters
        static readonly Color GoldShadow = new Color(0.28f, 0.14f, 0.03f);       // #472407 Extruded 3D shadow
        static readonly Color WhiteText = new Color(0.98f, 0.98f, 0.95f);       // Carved white text
        static readonly Color DarkShadow = new Color(0.12f, 0.06f, 0.02f, 0.90f);

        // GUI Styles & Textures
        GUIStyle logoBig, logoSmall, logoTag, plankBtn, plankSub, playerTitle, playerSub, infoLabel;
        Texture2D woodPlankTex, woodHoverTex, woodSmallTex, badgeTex, signBoardTex, leafTex;
        bool ready;

        public MainMenuView(MainMenuController controller)
        {
            this.controller = controller;
        }

        static Texture2D MakeSolid(Color color)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

        static Texture2D MakeRoundedBadge(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float rSq = radius * radius;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int cx = x < radius ? radius : (x >= size - radius ? size - radius - 1 : x);
                    int cy = y < radius ? radius : (y >= size - radius ? size - radius - 1 : y);
                    float dSq = (x - cx) * (x - cx) + (y - cy) * (y - cy);
                    if (dSq > rSq)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    // Top bevel highlight and bottom bevel shadow
                    float topHighlight = y >= size - 4 ? 0.22f : 0f;
                    float bottomShadow = y < 4 ? 0.28f : 0f;
                    float v = Mathf.Clamp01(1f + topHighlight - bottomShadow);
                    tex.SetPixel(x, y, new Color(v, v, v, 1f));
                }
            }
            tex.Apply();
            return tex;
        }

        static Texture2D MakeWoodPlank(int w, int h, Color face, Color topLight, Color bottomShadow)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            int bevel = 5;
            int cornerR = 9;
            float cornerRSq = cornerR * cornerR;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    // Rounded corner mask
                    int cx = x < cornerR ? cornerR : (x >= w - cornerR ? w - cornerR - 1 : x);
                    int cy = y < cornerR ? cornerR : (y >= h - cornerR ? h - cornerR - 1 : y);
                    float dSq = (x - cx) * (x - cx) + (y - cy) * (y - cy);
                    if (dSq > cornerRSq)
                    {
                        tex.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    // Natural wood grain variation
                    float grain = Mathf.Sin(y * 0.4f) * 0.025f + Mathf.Cos(x * 0.12f + y * 0.08f) * 0.02f;
                    Color c = new Color(face.r + grain, face.g + grain * 0.8f, face.b + grain * 0.5f, 1f);

                    // Top sun highlight bevel
                    if (y >= h - bevel) c = Color.Lerp(c, topLight, 0.75f);
                    // Bottom carved shadow bevel
                    else if (y < bevel) c = Color.Lerp(c, bottomShadow, 0.85f);
                    // Side bevels
                    else if (x < bevel) c = Color.Lerp(c, topLight, 0.35f);
                    else if (x >= w - bevel) c = Color.Lerp(c, bottomShadow, 0.5f);

                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return tex;
        }

        void InitStyles()
        {
            if (ready) return;

            woodPlankTex = MakeWoodPlank(256, 64, WoodFace, WoodLight, WoodDark);
            woodHoverTex = MakeWoodPlank(256, 64, WoodHover, new Color(0.74f, 0.46f, 0.25f), WoodDark);
            woodSmallTex = MakeWoodPlank(128, 48, new Color(0.38f, 0.21f, 0.10f), WoodLight, WoodDark);
            signBoardTex = MakeWoodPlank(256, 128, new Color(0.44f, 0.25f, 0.12f), WoodLight, WoodDark);
            badgeTex = MakeRoundedBadge(64, 12);
            leafTex = MakeSolid(new Color(0.28f, 0.65f, 0.22f));

            logoBig = new GUIStyle(GUI.skin.label)
            {
                fontSize = 58,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            logoSmall = new GUIStyle(GUI.skin.label)
            {
                fontSize = 38,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            logoTag = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(1f, 0.92f, 0.7f) }
            };

            plankBtn = new GUIStyle(GUI.skin.button)
            {
                fontSize = 21,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(75, 20, 0, 0),
                normal = { background = woodPlankTex, textColor = WhiteText },
                hover = { background = woodHoverTex, textColor = GoldText },
                active = { background = woodHoverTex, textColor = GoldText }
            };

            plankSub = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.92f, 0.82f, 0.65f) }
            };

            playerTitle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = WhiteText }
            };

            playerSub = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = GoldText }
            };

            infoLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.2f, 0.12f, 0.05f) }
            };

            var titleFont=Resources.Load<Font>("Brand/LilitaOne");
            var textFont=Resources.Load<Font>("Brand/NunitoSans");
            foreach(var style in new[]{logoBig,logoSmall,plankBtn})if(titleFont!=null){style.font=titleFont;style.fontStyle=FontStyle.Normal;}
            foreach(var style in new[]{logoTag,plankSub,playerTitle,playerSub,infoLabel})if(textFont!=null)style.font=textFont;
            ready = true;
        }

        static void DrawShadowLabel(Rect rect, string text, GUIStyle style, Color front, Color shadow, int offset = 2)
        {
            var oldColor = style.normal.textColor;
            style.normal.textColor = shadow;
            GUI.Label(new Rect(rect.x + offset, rect.y + offset, rect.width, rect.height), text, style);
            style.normal.textColor = front;
            GUI.Label(rect, text, style);
            style.normal.textColor = oldColor;
        }

        void DrawBadge(Rect rect, Color bg, string symbol, Color symColor)
        {
            GUI.color = bg;
            GUI.DrawTexture(rect, badgeTex);
            GUI.color = Color.white;
            var s = new GUIStyle(GUI.skin.label)
            {
                fontSize = (int)(rect.height * 0.52f),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = symColor }
            };
            var seal=Resources.Load<Texture2D>("Brand/island-seal");
            if(symbol=="🦍"&&seal!=null){GUI.DrawTexture(rect,seal,ScaleMode.ScaleToFit);return;}
            // Draw utility marks geometrically so they never depend on emoji fonts.
            if(symbol=="📱"){
                GUI.color=symColor;GUI.DrawTexture(new Rect(rect.x+14,rect.y+7,16,30),Texture2D.whiteTexture);
                GUI.color=bg;GUI.DrawTexture(new Rect(rect.x+17,rect.y+10,10,21),Texture2D.whiteTexture);GUI.color=Color.white;return;
            }
            if(symbol=="👥"){
                GUI.color=symColor;GUI.DrawTexture(new Rect(rect.x+11,rect.y+9,9,10),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(rect.x+25,rect.y+9,9,10),Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(rect.x+8,rect.y+22,15,14),Texture2D.whiteTexture);GUI.DrawTexture(new Rect(rect.x+23,rect.y+22,15,14),Texture2D.whiteTexture);GUI.color=Color.white;return;
            }
            GUI.Label(rect,symbol=="⚙"?"+":symbol,s);
        }

        public void Draw(float width, float height)
        {
            InitStyles();

            // The background is rendered in real-time 3D by MainMenuBackdrop (Gorilla, Trees, Sun, Sand).
            // This 2D GUI renders only the diegetic wooden signs and interactive planks as an overlay.

            // ==================== TOP-LEFT: 3D LOGO SIGNBOARD ====================
            float logoX = 60f;
            float logoY = 40f;
            float signW = 420f;
            float signH = 175f;

            // Wooden Plaque Sign
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(logoX, logoY, signW, signH), signBoardTex);
            var seal=Resources.Load<Texture2D>("Brand/island-seal");
            if(seal!=null)GUI.DrawTexture(new Rect(logoX+15,logoY+12,32,32),seal,ScaleMode.ScaleToFit);

            // Eyebrow banner
            DrawShadowLabel(new Rect(logoX, logoY + 14, signW, 22), "GORILIMPIADAS", logoTag, new Color(1f, 0.92f, 0.6f), DarkShadow, 1);

            // Big 3D Chunky Logo
            DrawShadowLabel(new Rect(logoX, logoY + 36, signW, 64), "GORILLA", logoBig, GoldText, GoldShadow, 3);
            DrawShadowLabel(new Rect(logoX, logoY + 96, signW, 46), "ESCAPE", logoSmall, WhiteText, DarkShadow, 2);

            // Subtitle ribbon under sign
            GUI.color = new Color(0.32f, 0.17f, 0.08f, 0.95f);
            GUI.DrawTexture(new Rect(logoX + 25, logoY + signH + 6, signW - 50, 28), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(logoX + 25, logoY + signH + 8, signW - 50, 24), "SUPERA SUS PRUEBAS. ENCUENTRA TU SALIDA.", logoTag);

            // ==================== TOP-RIGHT: PLAYER PROFILE BANNER ====================
            float profileW = 280f;
            float profileH = 68f;
            float profileX = width - profileW - 60f;
            float profileY = 40f;

            // Player wooden card
            GUI.DrawTexture(new Rect(profileX, profileY, profileW, profileH), woodSmallTex);

            // Gorilla profile badge
            DrawBadge(new Rect(profileX + 12, profileY + 12, 44, 44), new Color(0.24f, 0.12f, 0.06f), "🦍", Color.white);

            bool netReady = controller.Runtime?.java?.State == GorillaEscape.Network.IpcState.RUNNING;
            string pName = "NÁUFRAGO · EXPEDICIÓN";
            string netStatus = netReady ? "Conexión disponible" : "Preparar conexión al teléfono";

            DrawShadowLabel(new Rect(profileX + 66, profileY + 12, 200, 22), pName, playerTitle, WhiteText, DarkShadow, 1);
            GUI.Label(new Rect(profileX + 66, profileY + 34, 200, 20), netStatus,
                new GUIStyle(playerSub) { normal = { textColor = netReady ? new Color(0.4f, 0.95f, 0.5f) : new Color(0.9f, 0.75f, 0.5f) } });

            // ==================== RIGHT SIDE: WOODEN PLANK MENU STACK ====================
            float menuW = 380f;
            float plankH = 72f;
            float plankGap = 16f;
            float menuX = width - menuW - 60f;
            float menuY = profileY + profileH + 32f;

            // PLANK 1: JUGAR (HERO - Desafío del Tronco)
            Rect p1Rect = new Rect(menuX, menuY, menuW, plankH);
            if (GUI.Button(p1Rect, "JUGAR TRONCO", plankBtn))
            {
                controller.OpenLogBalance();
            }
            DrawBadge(new Rect(menuX + 14, menuY + 14, 44, 44), new Color(0.20f, 0.72f, 0.28f), "▶", Color.white);
            GUI.Label(new Rect(menuX + 78, menuY + 44, 280, 18), "Desafío de equilibrio · 1-4 náufragos", plankSub);

            // PLANK 2: CONECTAR TELÉFONO
            float p2Y = menuY + plankH + plankGap;
            Rect p2Rect = new Rect(menuX, p2Y, menuW, plankH);
            if (GUI.Button(p2Rect, "CONECTAR TELÉFONO", plankBtn))
            {
                controller.OpenPhone();
            }
            DrawBadge(new Rect(menuX + 14, p2Y + 14, 44, 44), new Color(0.95f, 0.65f, 0.15f), "📱", Color.white);
            GUI.Label(new Rect(menuX + 78, p2Y + 44, 280, 18), "Escanea con tu celular para jugar", plankSub);

            // PLANK 3: SALA DE JUGADORES
            float p3Y = p2Y + plankH + plankGap;
            Rect p3Rect = new Rect(menuX, p3Y, menuW, plankH);
            var lobby = controller.Runtime?.java?.Lobby?.Snapshot;
            int connectedCount = 0;
            if (lobby?.players != null)
            {
                foreach (var p in lobby.players)
                    if (p.connectionState != "DISCONNECTED") connectedCount++;
            }
            string lobbyTitle = connectedCount > 0 ? "SALA (" + connectedCount + " CONECTADOS)" : "SALA DE JUGADORES";

            if (GUI.Button(p3Rect, lobbyTitle, plankBtn))
            {
                controller.OpenPhone();
            }
            DrawBadge(new Rect(menuX + 14, p3Y + 14, 44, 44), new Color(0.22f, 0.55f, 0.88f), "👥", Color.white);
            GUI.Label(new Rect(menuX + 78, p3Y + 44, 280, 18), "Revisa los náufragos listos", plankSub);

            // PLANK 4: DIAGNÓSTICO / PRUEBAS (si QA Visible)
            float nextY = p3Y + plankH + plankGap;
            if (MainMenuController.QaVisible)
            {
                Rect p4Rect = new Rect(menuX, nextY, menuW, plankH);
                if (GUI.Button(p4Rect, "PRUEBAS TÉCNICAS", plankBtn))
                {
                    controller.OpenTests();
                }
                DrawBadge(new Rect(menuX + 14, nextY + 14, 44, 44), new Color(0.48f, 0.38f, 0.62f), "⚙", Color.white);
                GUI.Label(new Rect(menuX + 78, nextY + 44, 280, 18), "Centro de pruebas y diagnóstico", plankSub);
                nextY += plankH + plankGap;
            }

            // PLANK 5: SALIR
            Rect p5Rect = new Rect(menuX, nextY, menuW, plankH);
            if (GUI.Button(p5Rect, "SALIR DEL JUEGO", plankBtn))
            {
                controller.Exit();
            }
            DrawBadge(new Rect(menuX + 14, nextY + 14, 44, 44), new Color(0.72f, 0.22f, 0.18f), "✕", Color.white);

            // ==================== BOTTOM-LEFT: TUTORIALES / AYUDA ====================
            float tutW = 200f;
            float tutH = 48f;
            float tutX = 60f;
            float tutY = height - tutH - 45f;

            if (GUI.Button(new Rect(tutX, tutY, tutW, tutH), "CÓMO JUGAR",
                new GUIStyle(plankBtn) { fontSize = 16, padding = new RectOffset(20, 0, 0, 0) }))
            {
                controller.OpenLogBalance();
            }

            // Bottom credits banner
            GUI.Label(new Rect(tutX + tutW + 20, tutY + 14, 400, 24),
                "Gorilla Escape · Aventura y Minijuegos Multijugador", infoLabel);
        }
    }
}
