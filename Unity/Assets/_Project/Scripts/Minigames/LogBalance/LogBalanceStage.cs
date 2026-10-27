using System.Collections.Generic;
using UnityEngine;
namespace GorillaEscape.Minigames.LogBalance
{
    /// <summary>Disposable low-poly jungle; supplied art is imported at authoring time, never downloaded at runtime.</summary>
    public sealed class LogBalanceStage
    {
        readonly GameObject root;
        readonly List<Material> materials=new List<Material>();
        readonly List<Mesh> meshes=new List<Mesh>();
        readonly List<Camera> hidden=new List<Camera>();
        readonly List<Light> hiddenDirectionalLights=new List<Light>();
        readonly Dictionary<string,Material> importedMaterials=new Dictionary<string,Material>();
        readonly Dictionary<Color,Material> palette=new Dictionary<Color,Material>();
        readonly Transform human;
        readonly Transform logRoll;
        readonly List<Vector3> logSurface=new List<Vector3>();
        Quaternion logRotation=Quaternion.identity;
        readonly LogBalanceCharacter character;
        readonly List<LogBalanceSpectator> spectators=new List<LogBalanceSpectator>();
        readonly float length;
        readonly LogBalanceAtmosphere atmosphere;
        readonly float[] deck=new float[65];
        public GameObject Root=>root;
        public LogBalanceCharacter Character=>character;
        public LogBalanceStage(Transform parent,float length,LogBalanceVisuals visuals=null,Material trunkMaterial=null)
        {
            this.length=length;
            foreach(string asset in new[]{LogBalanceArtCatalog.Palm,LogBalanceArtCatalog.Bush,LogBalanceArtCatalog.Gorilla,
                LogBalanceArtCatalog.Human,LogBalanceArtCatalog.Trunk})
                if(Resources.Load<GameObject>(asset)==null)throw new System.InvalidOperationException("Log Balance asset missing: "+asset);
            try
            {
            root=new GameObject("Desafío del Tronco — selva de Saúl");root.transform.SetParent(parent,false);
            foreach(var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if(camera.enabled){hidden.Add(camera);camera.enabled=false;}
            // The scene may already contain a sun. Keep a single, controlled daylight source.
            foreach(var existing in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
                if(existing.enabled && existing.type==LightType.Directional){hiddenDirectionalLights.Add(existing);existing.enabled=false;}
            var view=new GameObject("Log Balance Camera");view.transform.SetParent(root.transform);
            var cam=view.AddComponent<Camera>();cam.transform.position=new Vector3(13,11,-14);
            cam.transform.LookAt(new Vector3(0,1,0));cam.clearFlags=CameraClearFlags.SolidColor;
            cam.backgroundColor=new Color(.12f,.25f,.22f);cam.orthographic=true;cam.orthographicSize=12.5f;
            var sun=new GameObject("Jungle daylight");sun.transform.SetParent(root.transform);
            var light=sun.AddComponent<Light>();light.type=LightType.Directional;light.intensity=.85f;light.color=new Color(1,.94f,.85f);
            sun.transform.rotation=Quaternion.Euler(48,-25,0);
            atmosphere=new LogBalanceAtmosphere(root.transform,cam,light,length,visuals??new LogBalanceVisuals());
            Shape("River",PrimitiveType.Cube,new Vector3(0,-1.05f,0),new Vector3(80,.15f,80),new Color(.12f,.42f,.39f));
            foreach(int sign in new[]{-1,1})CreateBank(sign);
            var log=Asset("trunk","Supplied crossing trunk",root.transform);
            if(trunkMaterial!=null)foreach(var renderer in log.GetComponentsInChildren<Renderer>())
            {var slots=renderer.sharedMaterials;for(int i=0;i<slots.Length;i++)slots[i]=trunkMaterial;renderer.sharedMaterials=slots;}
            var bounds=BoundsOf(log);
            log.transform.localScale=new Vector3(1.35f/bounds.size.x,1.15f/bounds.size.y,length/bounds.size.z);
            bounds=BoundsOf(log);log.transform.position-=bounds.center;
            logRoll=new GameObject("Rolling trunk pivot").transform;logRoll.SetParent(root.transform,false);
            log.transform.SetParent(logRoll,true);SampleDeck(log);
            Shape("Goal marker",PrimitiveType.Cube,new Vector3(0,.18f,length/2+.7f),new Vector3(2,.12f,.4f),new Color(.95f,.72f,.22f));
            var figure=Asset("Vagabundo","Vagabundo model",root.transform);
            human=new GameObject("Human castaway — controlled").transform;human.SetParent(root.transform,false);
            figure.transform.SetParent(human,false);character=figure.GetComponent<LogBalanceCharacter>();
            if(character==null||!character.HasRig)throw new System.InvalidOperationException("Castaway rig unavailable.");
            PopulateJungle();PopulateGrass();CreateSpectators();CreateBackdrop(cam);Present(0,0,false,0,0,false,false);
            }
            catch { Dispose(); throw; }
        }
        void SampleDeck(GameObject log)
        {
            foreach(var filter in log.GetComponentsInChildren<MeshFilter>())
            {
                if(!filter.sharedMesh.isReadable)throw new System.InvalidOperationException("Reimport trunk with Read/Write enabled.");
                var vertices=filter.sharedMesh.vertices;var triangles=filter.sharedMesh.triangles;
                foreach(int index in triangles)logSurface.Add(root.transform.InverseTransformPoint(filter.transform.TransformPoint(vertices[index])));
            }
            for(int sample=0;sample<deck.Length;sample++)
                deck[sample]=SampleLogHeight(Mathf.Lerp(-length/2+.03f,length/2-.03f,sample/(float)(deck.Length-1)),Quaternion.identity);
        }
        float SampleLogHeight(float z,Quaternion rotation,float x=0)
        {
            float height=float.NegativeInfinity;
            for(int i=0;i<logSurface.Count;i+=3)
            {
                var a=rotation*logSurface[i];var b=rotation*logSurface[i+1];var c=rotation*logSurface[i+2];
                float det=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);
                if(Mathf.Abs(det)<.000001f)continue;
                float u=((b.z-c.z)*(x-c.x)+(c.x-b.x)*(z-c.z))/det;
                float v=((c.z-a.z)*(x-c.x)+(a.x-c.x)*(z-c.z))/det;
                if(u>=-.001f&&v>=-.001f&&u+v<=1.001f)height=Mathf.Max(height,u*a.y+v*b.y+(1-u-v)*c.y);
            }
            return float.IsNegativeInfinity(height)?0:height;
        }
        public float SurfaceHeight(float progress)
        {float t=Mathf.Clamp01(progress)*(deck.Length-1);int i=Mathf.Min(deck.Length-2,(int)t);return Mathf.Lerp(deck[i],deck[i+1],t-i);}
        static Bounds BoundsOf(GameObject model)
        {
            var renderers=model.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;
            foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);return bounds;
        }
        static GameObject Asset(string file,string name,Transform parent)
        {
            var prefab=Resources.Load<GameObject>("LogBalance/Prefabs/"+file);
            if(prefab==null)throw new System.InvalidOperationException("Log Balance art prefab missing: "+file);
            var obj=Object.Instantiate(prefab,parent,false);obj.name=name;return obj;
        }
        Material Surface(Color color)
        {
            if(palette.TryGetValue(color,out var cached))return cached;
            var template=Resources.Load<Material>("LogBalanceStage");
            var material=template!=null?new Material(template):new Material(Shader.Find("Standard"));
            material.color=color;material.SetFloat("_Glossiness",0);material.SetFloat("_Metallic",0);
            materials.Add(material);palette.Add(color,material);return material;
        }
        Transform Facets(string name,List<Vector3> vertices,Color color,Transform parent=null)
        {
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);var indices=new int[vertices.Count];for(int i=0;i<indices.Length;i++)indices[i]=i;
            var uv=new Vector2[vertices.Count];for(int i=0;i<uv.Length;i++)uv[i]=new Vector2((vertices[i].x+40)/80,(vertices[i].z+45)/90);mesh.uv=uv;
            mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
            var obj=new GameObject(name);obj.transform.SetParent(parent??root.transform,false);
            obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=Surface(color);return obj.transform;
        }
        void CreateBank(int side)
        {
            // Irregular shore with a clear central landing, rather than a rectangular floating platform.
            var vertices=new List<Vector3>();
            for(int i=0;i<20;i++)
            {
                float a=-40+i*4,b=a+4;
                float Shore(float x)=>side*(length/2+.15f+(Mathf.Abs(x)<2?0:Mathf.Sin(x*.7f)*.65f));
                var p=new Vector3(a,.1f,Shore(a));var q=new Vector3(b,.1f,Shore(b));
                var r=new Vector3(a,.3f,side*45);var s=new Vector3(b,.3f,side*45);
                if(side>0){vertices.AddRange(new[]{p,r,q,q,r,s});}else{vertices.AddRange(new[]{p,q,r,q,s,r});}
                var lowP=p-Vector3.up*1.1f;var lowQ=q-Vector3.up*1.1f;
                vertices.AddRange(side>0?new[]{p,q,lowP,q,lowQ,lowP}:new[]{q,p,lowP,lowQ,q,lowP});
            }
            var bank=Facets("Ribera con suelo de Saúl "+side,vertices,Color.white);
            bank.GetComponent<MeshRenderer>().sharedMaterial=ImportedMaterial(LogBalanceArtCatalog.Ground);
        }
        Material ImportedMaterial(string texture)
        {
            if(importedMaterials.TryGetValue(texture,out var cached))return cached;
            var image=Resources.Load<Texture2D>(texture);
            if(image==null)throw new System.InvalidOperationException("Log Balance texture missing: "+texture);
            var material=new Material(Shader.Find("Standard")){name="Tronco / Saúl / "+texture,mainTexture=image,color=Color.white,enableInstancing=true};
            material.SetFloat("_Glossiness",.12f);material.SetFloat("_Metallic",0);
            materials.Add(material);importedMaterials.Add(texture,material);return material;
        }
        GameObject SaulAsset(string path,string texture,string name,Vector3 position,float height,float yaw)
        {
            var prefab=Resources.Load<GameObject>(path);
            if(prefab==null)throw new System.InvalidOperationException("Log Balance asset missing: "+path);
            var container=new GameObject(name);container.transform.SetParent(root.transform,false);
            var model=Object.Instantiate(prefab,container.transform,false);
            foreach(var animator in model.GetComponentsInChildren<Animator>(true))animator.enabled=false;
            foreach(var animation in model.GetComponentsInChildren<Animation>(true))animation.enabled=false;
            foreach(var collider in model.GetComponentsInChildren<Collider>(true))collider.enabled=false;
            if(path==LogBalanceArtCatalog.Gorilla)
            {
                AnimationClip idle=null;
                foreach(var clip in Resources.LoadAll<AnimationClip>(path))
                    if(!clip.name.StartsWith("__preview__")&&(idle==null||clip.name.ToLowerInvariant().Contains("idle")))idle=clip;
                if(idle!=null)idle.SampleAnimation(model,0);
            }
            // Palm FBX carries an import-axis correction on its root; keep it upright.
            if(path!=LogBalanceArtCatalog.Palm)model.transform.localRotation=Quaternion.identity;
            var renderers=model.GetComponentsInChildren<Renderer>(true);
            if(renderers.Length==0)throw new System.InvalidOperationException("Log Balance empty model: "+path);
            var bounds=BoundsOf(model);container.transform.localScale=Vector3.one*(height/Mathf.Max(.01f,bounds.size.y));
            bounds=BoundsOf(model);model.transform.position-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            var material=ImportedMaterial(texture);
            foreach(var renderer in renderers)
            {
                var slots=renderer.sharedMaterials;
                for(int i=0;i<slots.Length;i++)slots[i]=material;
                renderer.sharedMaterials=slots;
            }
            container.transform.localPosition=position;container.transform.localRotation=Quaternion.Euler(0,yaw,0);
            return container;
        }
        void PopulateJungle()
        {
            var random=new System.Random(1855);
            foreach(int bank in new[]{-1,1})for(int i=0;i<9;i++)
            {
                float x=(i-4)*5.5f;
                if(Mathf.Abs(x)<3)continue;
                float z=bank*(length/2+5+(float)random.NextDouble()*9);
                SaulAsset(LogBalanceArtCatalog.Palm,LogBalanceArtCatalog.PalmTexture,"Palmera de Saúl",
                    new Vector3(x,.15f,z),5+(float)random.NextDouble()*2.5f,(float)random.NextDouble()*360);
            }
        }
        void PopulateGrass()
        {
            var random=new System.Random(9123);
            foreach(int bank in new[]{-1,1})for(int i=0;i<12;i++)
            {
                float x=(i-5.5f)*3.7f;
                if(Mathf.Abs(x)<3)continue;
                float z=bank*(length/2+1.5f+(float)random.NextDouble()*7);
                SaulAsset(LogBalanceArtCatalog.Bush,LogBalanceArtCatalog.BushTexture,"Arbusto de Saúl",
                    new Vector3(x,.15f,z),.8f+(float)random.NextDouble()*.6f,(float)random.NextDouble()*360);
            }
        }
        void CreateSpectators()
        {
            foreach(int side in new[]{-1,1})foreach(int bank in new[]{-1,1})
            {
                var npc=SaulAsset(LogBalanceArtCatalog.Gorilla,LogBalanceArtCatalog.GorillaTexture,"Gorila espectador de Saúl",
                    new Vector3(side*3.3f,.15f,bank*(length/2+2)),2,0);
                var spectator=npc.AddComponent<LogBalanceSpectator>();spectator.BindRig();spectators.Add(spectator);
            }
        }
        void CreateBackdrop(Camera view)
        {
            var image=Resources.Load<Texture2D>(LogBalanceArtCatalog.Background);
            if(image==null)throw new System.InvalidOperationException("Log Balance backdrop missing.");
            var backdrop=GameObject.CreatePrimitive(PrimitiveType.Quad);backdrop.name="Fondo de cascada de Saúl";
            backdrop.transform.SetParent(root.transform,false);backdrop.transform.position=view.transform.position+view.transform.forward*60;
            backdrop.transform.rotation=view.transform.rotation;backdrop.transform.localScale=new Vector3(140,80,1);
            backdrop.GetComponent<Collider>().enabled=false;
            var material=new Material(Shader.Find("Unlit/Texture")){mainTexture=image};materials.Add(material);
            backdrop.GetComponent<MeshRenderer>().sharedMaterial=material;
        }
        Transform Shape(string name,PrimitiveType type,Vector3 position,Vector3 scale,Color color)
        {
            var obj=GameObject.CreatePrimitive(type);obj.name=name;obj.transform.SetParent(root.transform,false);
            obj.transform.localPosition=position;obj.transform.localScale=scale;
            if(obj.GetComponent<MeshFilter>()==null)throw new System.InvalidOperationException("Primitive mesh missing.");
            obj.GetComponent<MeshRenderer>().sharedMaterial=Surface(color);return obj.transform;
        }
        public void Present(float progress,float balance,bool fell,float fallSeconds,float elapsed=0,bool walking=false,bool won=false,float rollDegrees=0,float gaitPhase=-1,float danger=0)
        {
            logRotation=Quaternion.Euler(0,0,-rollDegrees);
            logRoll.localRotation=logRotation;
            float footX=Mathf.Clamp(balance,-1,1)*.42f;
            float support=SampleLogHeight(Mathf.Lerp(-length/2+.03f,length/2-.03f,Mathf.Clamp01(progress)),logRotation,footX);
            float side=fell?(balance<0?-1:1)*Mathf.Min(2.7f,fallSeconds*3):0;
            float bob=walking?Mathf.Sin(gaitPhase>=0?gaitPhase*2:elapsed*Mathf.PI*4.6f)*.025f:0;
            human.localPosition=new Vector3(footX+side,support+.035f+bob-(fell?Mathf.Min(3.5f,fallSeconds*4):0),-length/2+length*progress);
            float warningLean=walking?Mathf.Sin(elapsed*11)*Mathf.InverseLerp(.6f,1,danger)*3:0;
            human.localRotation=Quaternion.Euler(fell?Mathf.Min(65,fallSeconds*85):0,0,-balance*22+warningLean-(fell?side*28:0));
            character.Pose(elapsed,balance,walking,fell,fallSeconds,won,gaitPhase);
            if(!fell)human.localPosition+=Vector3.up*character.SupportOffset;
            for(int i=0;i<spectators.Count;i++)
            {
                var npc=spectators[i];var direction=human.position-npc.transform.position;direction.y=0;
                if(direction.sqrMagnitude>.01f)npc.transform.rotation=Quaternion.LookRotation(direction);
                npc.Pose(Time.unscaledTime,i*1.73f,balance,fell,fallSeconds,won);
            }
            atmosphere.Present(Time.unscaledTime,balance,fell,fallSeconds,human.localPosition);
        }
        public void Dispose()
        {
            atmosphere?.Dispose();
            if(root!=null)root.SetActive(false);
            foreach(var camera in hidden)if(camera!=null)camera.enabled=true;
            foreach(var light in hiddenDirectionalLights)if(light!=null)light.enabled=true;
            foreach(var material in materials)Object.Destroy(material);foreach(var mesh in meshes)Object.Destroy(mesh);
            Object.Destroy(root);
        }
    }
}
