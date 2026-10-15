using UnityEngine;
namespace GorillaEscape.UI
{
    /// <summary>Reusable product presentation, made only from original geometry and the engine font.</summary>
    public sealed class MainMenuView
    {
        readonly MainMenuController controller;
        GUIStyle brand, subtitle, button, caption;
        static readonly Color Ink = new Color(.025f,.085f,.075f);
        static readonly Color Leaf = new Color(.09f,.24f,.17f);
        static readonly Color Sun = new Color(.95f,.75f,.3f);
        public MainMenuView(MainMenuController controller) { this.controller = controller; }
        static void Fill(Rect rect, Color color) { GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=Color.white; }
        public void Draw(float width,float height)
        {
            if(brand==null)
            {
                brand=new GUIStyle(GUI.skin.label){fontSize=66,fontStyle=FontStyle.Bold};
                subtitle=new GUIStyle(GUI.skin.label){fontSize=22};
                caption=new GUIStyle(GUI.skin.label){fontSize=16};
                button=new GUIStyle(GUI.skin.button){fontSize=23,fontStyle=FontStyle.Bold};
            }
            Fill(new Rect(0,0,width,height),Ink);
            // Layered island ridges, deliberately provisional; no downloaded art or asset licences.
            Fill(new Rect(width*.66f,0,width*.34f,height),new Color(.055f,.15f,.12f));
            for(int i=0;i<6;i++)
            {
                var saved=GUI.matrix;
                GUIUtility.RotateAroundPivot(-28+i*9,new Vector2(width*.82f,height*.7f));
                Fill(new Rect(width*.65f+i*15,height*.55f,145,height),i%2==0?Leaf:new Color(.12f,.3f,.21f));
                GUI.matrix=saved;
            }
            Fill(new Rect(80,95,52,6),Sun);
            GUI.Label(new Rect(80,124,720,170),"GORILLA\nESCAPE",brand);
            GUI.Label(new Rect(84,300,650,40),"LA ISLA ES SÓLO EL COMIENZO",subtitle);
            GUI.Label(new Rect(84,347,650,28),"Identidad visual provisional · Logo tipográfico",caption);
            float y=Mathf.Max(410,height*.48f);
            GUI.enabled=false;GUI.Button(new Rect(84,y,370,66),"JUGAR",button);GUI.enabled=true;
            GUI.Label(new Rect(475,y+21,220,30),"En desarrollo",caption);
            if(MainMenuController.QaVisible && GUI.Button(new Rect(84,y+84,370,66),"PRUEBAS",button))controller.OpenTests();
            if(GUI.Button(new Rect(84,y+168,370,60),"SALIR",button))controller.Exit();
            GUI.Label(new Rect(84,height-65,900,34),"GORILLA ESCAPE  /  Base técnica Phase0",caption);
            GUI.Label(new Rect(width-290,55,260,40),"01  /  ISLAND FOUNDATION",caption);
        }
    }
}
