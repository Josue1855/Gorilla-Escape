using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using GorillaEscape.Minigames.LogBalance;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
namespace GorillaEscape.Editor
{
    public static class LogBalanceAssetSetup
    {
        public static void InstallAnimationModule()
        {
            var request=Client.Add("com.unity.modules.animation@1.0.0");
            EditorApplication.CallbackFunction poll=null;
            poll=()=>{if(!request.IsCompleted)return;EditorApplication.update-=poll;
                Debug.Log("LOG_BALANCE_ANIMATION_MODULE "+request.Status);
                EditorApplication.Exit(request.Status==StatusCode.Success?0:1);};
            EditorApplication.update+=poll;
        }

        [Serializable] sealed class Manifest { public Source[] assets; }
        [Serializable] sealed class Source {public string name;public Surface[] materials;public string[] models;}
        [Serializable] sealed class Surface {public string name,texture;public float[] color;}
        const string Root="Assets/_Project/Resources/LogBalance";
        static Bounds GeometryBounds(Renderer[] renderers)
        {
            var bounds=renderers[0].bounds;
            foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            return bounds;
        }
        [MenuItem("Gorilla Escape/Log Balance/Rebuild Art Prefabs")]
        public static void CreatePrefabs()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(string folder in new[]{"Materials","Prefabs"})
                if(!AssetDatabase.IsValidFolder(Root+"/"+folder))AssetDatabase.CreateFolder(Root,folder);
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Root+"/asset-manifest.json"));
            foreach(var source in manifest.assets)
            {
                var surfaces=new Dictionary<string,Material>();
                foreach(var surface in source.materials)
                {
                    string path=Root+"/Materials/"+source.name+"_"+surface.name.Replace('/','_')+".mat";
                    var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                    if(material==null){material=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(material,path);}
                    var c=surface.color;material.color=new Color(c[0],c[1],c[2],c[3]);
                    material.SetFloat("_Metallic",0);material.SetFloat("_Glossiness",.05f);
                    if(!string.IsNullOrEmpty(surface.texture))
                    {
                        string texturePath=Root+"/Textures/"+surface.texture;
                        var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);
                        importer.maxTextureSize=1024;importer.sRGBTexture=true;importer.mipmapEnabled=true;importer.SaveAndReimport();
                        material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                    }
                    EditorUtility.SetDirty(material);surfaces.Add(surface.name,material);
                }
                foreach(string file in source.models)
                {
                    string path=Root+"/Models/"+file;
                    var importer=(ModelImporter)AssetImporter.GetAtPath(path);
                    importer.isReadable=source.name=="trunk";
                    importer.importCameras=false;importer.importLights=false;importer.importBlendShapes=false;
                    importer.importAnimation=source.name=="Vagabundo";
                    importer.animationType=source.name=="Vagabundo"?ModelImporterAnimationType.Generic:ModelImporterAnimationType.None;
                    importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
                    importer.materialLocation=ModelImporterMaterialLocation.InPrefab;
                    importer.SaveAndReimport();
                    var prefab=new GameObject(Path.GetFileNameWithoutExtension(file));
                    try
                    {
                        var model=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                        model.transform.SetParent(prefab.transform,false);
                        foreach(var animator in model.GetComponentsInChildren<Animator>())animator.enabled=false;
                        if(source.name=="Vagabundo")
                        {
                            var clip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c=>!c.name.StartsWith("__preview__"));
                            if(clip==null)throw new InvalidOperationException("Castaway authored pose was not imported.");
                            clip.SampleAnimation(model,0);
                        }
                        foreach(var renderer in model.GetComponentsInChildren<Renderer>())
                        {
                            var mapped=renderer.sharedMaterials;
                            for(int i=0;i<mapped.Length;i++)
                            {
                                string name=mapped[i]!=null?mapped[i].name:"";
                                if(!surfaces.TryGetValue(name,out var material))material=surfaces.Values.First();
                                mapped[i]=material;
                            }
                            renderer.sharedMaterials=mapped;
                        }
                        var renderers=model.GetComponentsInChildren<Renderer>();
                        var bounds=GeometryBounds(renderers);
                        float height=source.name=="Vagabundo"?2.35f:source.name=="low_poly_trees"?1:bounds.size.y;
                        model.transform.localScale*=height/Mathf.Max(.001f,bounds.size.y);
                        bounds=GeometryBounds(renderers);
                        model.transform.position-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                        if(source.name=="Vagabundo")
                        {
                            var character=prefab.AddComponent<LogBalanceCharacter>();character.BindRig();
                            if(!character.HasRig)throw new InvalidOperationException("Castaway bones missing.");
                        }
                        PrefabUtility.SaveAsPrefabAsset(prefab,Root+"/Prefabs/"+Path.GetFileNameWithoutExtension(file)+".prefab");
                        Debug.Log("LOG_BALANCE_ART "+file+" bounds="+bounds.size+" renderers="+renderers.Length);
                    }
                    finally{UnityEngine.Object.DestroyImmediate(prefab);}
                }
            }
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        }
    }
}
