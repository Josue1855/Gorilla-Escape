using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class JungleMenuValidation
{
    public static void Validate()
    {
        try
        {
            if(EditorApplication.isPlaying) throw new Exception("Unity sigue en Play");
            EditorSceneManager.OpenScene("Assets/Scenes/"+SceneNavigator.MainMenu+".unity");
            if(UnityEngine.Object.FindAnyObjectByType<MainMenuController>()==null) throw new Exception("Falta el controlador del menú");
            var clips=Resources.LoadAll<AnimationClip>(JungleMenuStage.GorillaResource);
            if(Resources.Load<GameObject>(JungleMenuStage.GorillaResource)==null) throw new Exception("Falta el modelo");
            foreach(var clip in clips) Debug.Log("MENU_CLIP "+clip.name+" "+clip.length);
            if(clips.Length<4) throw new Exception("Faltan animaciones distintas");
            var packet=new PhoneMenuPointer.Packet {alpha=359,beta=30,gamma=0};
            Vector2 center=PhoneMenuPointer.Project(packet,359,30,0);
            if((center-new Vector2(.5f,.5f)).sqrMagnitude>.0001f) throw new Exception("Puntero sin recentrar");
            packet.alpha=1;
            Vector2 wrap=PhoneMenuPointer.Project(packet,359,30,0);
            if(wrap.x<.4f||wrap.x>.6f) throw new Exception("Salto del puntero al cruzar 360 grados");
            packet.alpha=179;packet.beta=170;
            Vector2 edge=PhoneMenuPointer.Project(packet,359,30,0);
            if(edge.x<0||edge.x>1||edge.y<0||edge.y>1) throw new Exception("Puntero fuera de pantalla");
            packet=new PhoneMenuPointer.Packet {alpha=0,beta=0,gamma=30};
            if(PhoneMenuPointer.Project(packet,0,0,0).x<=.8f) throw new Exception("El control por inclinación no responde");
            Debug.Log("MENU_POINTER_PASS: recentrado, cruce de 360, límites e inclinación");
            // Exercise stage construction and animation graphs without starting camera/network services.
            var probe=new GameObject("Menu validation probe");
            var stage=probe.AddComponent<JungleMenuStage>();
            stage.Initialize();
            stage.SampleAnimations(.7f);
            if(stage.GorillaCount!=4||stage.AnimatedCount!=4) throw new Exception("Se esperaban 4 gorilas animados");
            string log=Path.Combine(Application.dataPath,"../Logs/MenuValidation.txt");
            File.WriteAllText(log,"PASS: 4 gorilas, 4 animaciones, escena MainMenu, recursos y proyección del puntero. Validación sin renderizar.\n");
            stage.Shutdown();
            UnityEngine.Object.DestroyImmediate(probe);
            Debug.Log("MENU_VALIDATION_PASS");
            EditorApplication.Exit(0);
        }
        catch(Exception e) {Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
