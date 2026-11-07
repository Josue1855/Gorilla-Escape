using UnityEngine;

public static class JungleUITheme
{
    private static bool ready;
    private static Texture2D deepGreen, jungleGreen, wood, woodLight, gold, turquoise, shadow, leafLeft, leafRight, leafBottom, gorilla;
    public static GUIStyle Title { get; private set; }
    public static GUIStyle Heading { get; private set; }
    public static GUIStyle Body { get; private set; }
    public static GUIStyle Button { get; private set; }
    public static GUIStyle ButtonSelected { get; private set; }

    private static Texture2D Solid(Color color)
    {
        Texture2D texture=new Texture2D(1,1);texture.name="Jungle UI "+color;texture.SetPixel(0,0,color);texture.Apply();return texture;
    }

    public static void Ensure()
    {
        if(ready)return;ready=true;
        deepGreen=Solid(new Color(.018f,.07f,.055f,1f));jungleGreen=Solid(new Color(.025f,.20f,.105f,1f));
        wood=Solid(new Color(.20f,.095f,.035f,.97f));woodLight=Solid(new Color(.36f,.18f,.055f,1f));
        gold=Solid(new Color(1f,.76f,.08f,1f));turquoise=Solid(new Color(.04f,.76f,.82f,1f));shadow=Solid(new Color(.005f,.012f,.01f,.72f));
        leafLeft=Resources.Load<Texture2D>("Jungle/hojas_superior_izquierda");leafRight=Resources.Load<Texture2D>("Jungle/hoja_superior_derecha");leafBottom=Resources.Load<Texture2D>("Jungle/hojas_inferiores");gorilla=Resources.Load<Texture2D>("Jungle/gorila_menu");
        Title=new GUIStyle(GUI.skin.label){fontSize=54,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter,normal={textColor=new Color(1f,.82f,.16f)}};
        Heading=new GUIStyle(Title){fontSize=34,normal={textColor=Color.white}};
        Body=new GUIStyle(GUI.skin.label){fontSize=23,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter,wordWrap=true,normal={textColor=new Color(.92f,.97f,.88f)}};
        Button=new GUIStyle(GUI.skin.button){fontSize=26,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter,border=new RectOffset(8,8,8,8),normal={background=woodLight,textColor=Color.white},hover={background=turquoise,textColor=new Color(.015f,.08f,.055f)},active={background=gold,textColor=new Color(.12f,.06f,.01f)}};
        ButtonSelected=new GUIStyle(Button);ButtonSelected.normal.background=gold;ButtonSelected.normal.textColor=new Color(.12f,.06f,.01f);
    }

    public static void DrawBackdrop(bool showGorilla=false)
    {
        Ensure();float w=Screen.width,h=Screen.height;
        GUI.DrawTexture(new Rect(0,0,w,h),deepGreen);
        GUI.DrawTexture(new Rect(0,h*.22f,w,h*.78f),jungleGreen);
        for(int i=0;i<7;i++)GUI.DrawTexture(new Rect(i*w/6f-w*.08f,h*(.42f+(i%2)*.08f),w*.18f,h*.58f),SolidCached(i%2==0?new Color(.018f,.11f,.055f):new Color(.025f,.15f,.07f)));
        if(showGorilla&&gorilla!=null)GUI.DrawTexture(new Rect(w*.57f,h*.12f,w*.43f,h*.88f),gorilla,ScaleMode.ScaleAndCrop,true);
        GUI.DrawTexture(new Rect(0,0,w,h),shadow,ScaleMode.StretchToFill);
        DrawLeaves();
    }

    private static Texture2D SolidCached(Color color) => color.g>.13f?jungleGreen:deepGreen;

    public static void DrawLeaves()
    {
        float w=Screen.width,h=Screen.height;
        if(leafLeft!=null)GUI.DrawTexture(new Rect(0,0,w*.25f,h*.22f),leafLeft,ScaleMode.ScaleToFit,true);
        if(leafRight!=null)GUI.DrawTexture(new Rect(w*.76f,0,w*.24f,h*.21f),leafRight,ScaleMode.ScaleToFit,true);
        if(leafBottom!=null){GUI.DrawTexture(new Rect(0,h*.82f,w*.28f,h*.18f),leafBottom,ScaleMode.ScaleToFit,true);GUI.DrawTexture(new Rect(w*.72f,h*.82f,w*.28f,h*.18f),leafBottom,ScaleMode.ScaleToFit,true);}
    }

    public static void DrawPanel(Rect rect,bool selected=false)
    {
        Ensure();float edge=Mathf.Max(5f,rect.height*.025f);
        GUI.DrawTexture(new Rect(rect.x+10,rect.y+12,rect.width,rect.height),shadow);
        GUI.DrawTexture(rect,selected?gold:woodLight);
        GUI.DrawTexture(new Rect(rect.x+edge,rect.y+edge,rect.width-edge*2,rect.height-edge*2),wood);
        GUI.DrawTexture(new Rect(rect.x+edge,rect.y+edge,selected?edge*1.8f:edge,rect.height-edge*2),selected?turquoise:gold);
    }
}
