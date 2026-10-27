using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace GorillaEscape.Minigames.LogBalance
{
    [Serializable]
    public sealed class LogBalanceVisuals
    {
        public bool shadows=true, haze=true, flowingRiver=true, splash=true;
        [Range(0,1)] public float shadowStrength=.72f;
        [Range(.5f,2)] public float sunlight=.85f;
        [Range(20,90)] public float shadowDistance=55;
        [Range(0,3)] public float riverSpeed=1.2f;
        [Range(0,2)] public float cameraRoll=.8f;
    }
    /// <summary>Presentation only. Owns and restores scene-wide lighting; never changes sporting input or rules.</summary>
    public sealed class LogBalanceAtmosphere : IDisposable
    {
        readonly AmbientMode oldAmbientMode=RenderSettings.ambientMode;
        readonly Color oldSky=RenderSettings.ambientSkyColor,oldEquator=RenderSettings.ambientEquatorColor,oldGround=RenderSettings.ambientGroundColor,oldFogColor=RenderSettings.fogColor;
        readonly float oldAmbientIntensity=RenderSettings.ambientIntensity,oldFogStart=RenderSettings.fogStartDistance,oldFogEnd=RenderSettings.fogEndDistance,oldShadowDistance=QualitySettings.shadowDistance;
        readonly bool oldFog=RenderSettings.fog;
        readonly FogMode oldFogMode=RenderSettings.fogMode;
        readonly ShadowQuality oldShadows=QualitySettings.shadows;
        readonly ShadowResolution oldResolution=QualitySettings.shadowResolution;
        readonly int oldCascades=QualitySettings.shadowCascades;
        readonly Light oldSun=RenderSettings.sun;
        readonly Camera camera;
        readonly Quaternion baseCameraRotation;
        readonly LogBalanceVisuals options;
        readonly GameObject root,splashRoot;
        readonly List<Material> materials=new List<Material>();
        readonly List<Transform> streaks=new List<Transform>(),drops=new List<Transform>();
        readonly List<Vector3> streakStarts=new List<Vector3>();
        readonly Transform ripple;
        readonly Mesh rippleMesh;
        bool disposed;
        public LogBalanceAtmosphere(Transform stage,Camera camera,Light sun,float length,LogBalanceVisuals options)
        {
            this.options=options;this.camera=camera;baseCameraRotation=camera.transform.rotation;
            root=new GameObject("River and atmosphere effects");root.transform.SetParent(stage,false);
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.38f,.45f,.46f);
            RenderSettings.ambientEquatorColor=new Color(.25f,.30f,.23f);
            RenderSettings.ambientGroundColor=new Color(.16f,.18f,.13f);RenderSettings.ambientIntensity=.7f;
            RenderSettings.sun=sun;sun.intensity=options.sunlight;
            sun.shadows=options.shadows?LightShadows.Soft:LightShadows.None;
            sun.shadowStrength=options.shadowStrength;sun.shadowBias=.035f;sun.shadowNormalBias=.15f;
            sun.shadowNearPlane=.1f;
            if(options.shadows){QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowResolution=ShadowResolution.High;QualitySettings.shadowCascades=2;QualitySettings.shadowDistance=options.shadowDistance;}
            RenderSettings.fog=options.haze;RenderSettings.fogMode=FogMode.Linear;
            RenderSettings.fogColor=new Color(.29f,.43f,.35f);RenderSettings.fogStartDistance=28;RenderSettings.fogEndDistance=75;
            var flow=Material(new Color(.17f,.49f,.46f));
            var random=new System.Random(401);
            if(options.flowingRiver)for(int i=0;i<26;i++)
            {
                var start=new Vector3(-34+(float)random.NextDouble()*68,-.967f,-length/2+1+(float)random.NextDouble()*(length-2));
                var streak=Cube("Low-poly current highlight",root.transform,flow);
                streak.localPosition=start;streak.localScale=new Vector3(.6f+(float)random.NextDouble()*1.3f,.008f,.035f+(float)random.NextDouble()*.07f);
                streaks.Add(streak);streakStarts.Add(start);
            }
            splashRoot=new GameObject("Fall splash");splashRoot.transform.SetParent(root.transform,false);
            var foam=Material(new Color(.65f,.85f,.76f));
            for(int i=0;i<10;i++){var drop=Cube("Low-poly water droplet",splashRoot.transform,foam);drops.Add(drop);}
            var ring=new GameObject("Water impact ripple");ring.transform.SetParent(splashRoot.transform,false);ripple=ring.transform;
            rippleMesh=new Mesh{name="Low-poly ripple"};var vertices=new List<Vector3>();
            for(int i=0;i<20;i++)
            {
                float a=i*Mathf.PI*.1f,b=(i+1)*Mathf.PI*.1f;
                var p=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));var q=new Vector3(Mathf.Cos(b),0,Mathf.Sin(b));
                vertices.AddRange(new[]{p*.88f,q,p,q*.88f,q,p*.88f});
            }
            rippleMesh.SetVertices(vertices);var indices=new int[vertices.Count];for(int i=0;i<indices.Length;i++)indices[i]=i;
            rippleMesh.SetTriangles(indices,0);rippleMesh.RecalculateNormals();rippleMesh.RecalculateBounds();
            ring.AddComponent<MeshFilter>().sharedMesh=rippleMesh;var renderer=ring.AddComponent<MeshRenderer>();renderer.sharedMaterial=foam;renderer.shadowCastingMode=ShadowCastingMode.Off;
            splashRoot.SetActive(false);
        }
        Material Material(Color color)
        {
            var template=Resources.Load<Material>("LogBalanceStage");
            var material=template!=null?new Material(template):new Material(Shader.Find("Standard"));material.color=color;
            material.SetFloat("_Glossiness",.15f);material.SetFloat("_Metallic",0);materials.Add(material);return material;
        }
        static Transform Cube(string name,Transform parent,Material material)
        {
            var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.name=name;cube.transform.SetParent(parent,false);
            var renderer=cube.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
            // Preserve the mesh component through Player stripping.
            if(cube.GetComponent<MeshFilter>()==null)throw new InvalidOperationException("Effect mesh missing.");
            return cube.transform;
        }
        public void Present(float time,float balance,bool fell,float fallTime,Vector3 position)
        {
            for(int i=0;i<streaks.Count;i++)
            {var p=streakStarts[i];p.x=Mathf.Repeat(p.x+34+time*options.riverSpeed,68)-34;streaks[i].localPosition=p;}
            camera.transform.rotation=baseCameraRotation*Quaternion.Euler(0,0,-balance*options.cameraRoll);
            float impact=fallTime-.35f;
            bool visible=options.splash&&fell&&impact>=0&&impact<.95f;
            splashRoot.SetActive(visible);if(!visible)return;
            // Freeze the impact point; presentation does not drag the splash along the falling body.
            float side=balance<0?-1:1;splashRoot.transform.localPosition=new Vector3(side*1.05f,-.955f,position.z);
            for(int i=0;i<drops.Count;i++)
            {
                float angle=i*Mathf.PI*.2f;float radius=impact*(1.7f+i%3*.25f);
                drops[i].localPosition=new Vector3(Mathf.Cos(angle)*radius,Mathf.Max(0,impact*3.4f-impact*impact*5),Mathf.Sin(angle)*radius);
                drops[i].localScale=Vector3.one*Mathf.Lerp(.13f,.03f,impact/.95f);
                drops[i].localRotation=Quaternion.Euler(i*29+impact*80,i*43,impact*110);
            }
            ripple.localScale=Vector3.one*Mathf.Lerp(.25f,2,impact/.95f);
        }
        public void Dispose()
        {
            if(disposed)return;disposed=true;
            RenderSettings.ambientMode=oldAmbientMode;RenderSettings.ambientSkyColor=oldSky;RenderSettings.ambientEquatorColor=oldEquator;RenderSettings.ambientGroundColor=oldGround;RenderSettings.ambientIntensity=oldAmbientIntensity;
            RenderSettings.fog=oldFog;RenderSettings.fogMode=oldFogMode;RenderSettings.fogColor=oldFogColor;RenderSettings.fogStartDistance=oldFogStart;RenderSettings.fogEndDistance=oldFogEnd;RenderSettings.sun=oldSun;
            QualitySettings.shadows=oldShadows;QualitySettings.shadowResolution=oldResolution;QualitySettings.shadowCascades=oldCascades;QualitySettings.shadowDistance=oldShadowDistance;
            if(camera!=null)camera.transform.rotation=baseCameraRotation;
            foreach(var material in materials)UnityEngine.Object.Destroy(material);
            UnityEngine.Object.Destroy(rippleMesh);UnityEngine.Object.Destroy(root);
        }
    }
}
