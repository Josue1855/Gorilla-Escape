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
        readonly Dictionary<Color,Material> palette=new Dictionary<Color,Material>();
        readonly Transform human;
        readonly LogBalanceCharacter character;
        readonly float length;
        readonly float[] deck=new float[65];
        public GameObject Root=>root;
        public LogBalanceCharacter Character=>character;
        public LogBalanceStage(Transform parent,float length)
        {
            this.length=length;
            root=new GameObject("Log Balance — low-poly jungle");root.transform.SetParent(parent,false);
            foreach(var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if(camera.enabled){hidden.Add(camera);camera.enabled=false;}
            var view=new GameObject("Log Balance Camera");view.transform.SetParent(root.transform);
            var cam=view.AddComponent<Camera>();cam.transform.position=new Vector3(13,11,-14);
            cam.transform.LookAt(new Vector3(0,1,0));cam.clearFlags=CameraClearFlags.SolidColor;
            cam.backgroundColor=new Color(.12f,.25f,.22f);cam.orthographic=true;cam.orthographicSize=12.5f;
            var sun=new GameObject("Jungle daylight");sun.transform.SetParent(root.transform);
            var light=sun.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.15f;light.color=new Color(1,.94f,.79f);
            sun.transform.rotation=Quaternion.Euler(48,-25,0);
            Shape("River",PrimitiveType.Cube,new Vector3(0,-1.05f,0),new Vector3(80,.15f,80),new Color(.12f,.42f,.39f));
            foreach(int sign in new[]{-1,1})CreateBank(sign);
            var log=Asset("trunk","Supplied crossing trunk",root.transform);
            var bounds=BoundsOf(log);
            log.transform.localScale=new Vector3(1.35f/bounds.size.x,1.15f/bounds.size.y,length/bounds.size.z);
            bounds=BoundsOf(log);log.transform.position-=bounds.center;SampleDeck(log);
            Shape("Goal marker",PrimitiveType.Cube,new Vector3(0,.18f,length/2+.7f),new Vector3(2,.12f,.4f),new Color(.95f,.72f,.22f));
            var figure=Asset("Vagabundo","Vagabundo model",root.transform);
            human=new GameObject("Human castaway — controlled").transform;human.SetParent(root.transform,false);
            figure.transform.SetParent(human,false);character=figure.GetComponent<LogBalanceCharacter>();
            if(character==null||!character.HasRig)throw new System.InvalidOperationException("Castaway rig unavailable.");
            PopulateJungle();CreateSpectators();Present(0,0,false,0,0,false,false);
        }
        void SampleDeck(GameObject log)
        {
            // Sample the actual imported triangles, so decorative branches do not make feet float above the trunk.
            for(int sample=0;sample<deck.Length;sample++)
            {
                float z=Mathf.Lerp(-length/2+.03f,length/2-.03f,sample/(float)(deck.Length-1));
                float height=float.NegativeInfinity;
                foreach(var filter in log.GetComponentsInChildren<MeshFilter>())
                {
                    var vertices=filter.sharedMesh.vertices;var triangles=filter.sharedMesh.triangles;
                    for(int i=0;i<triangles.Length;i+=3)
                    {
                        var a=filter.transform.TransformPoint(vertices[triangles[i]]);var b=filter.transform.TransformPoint(vertices[triangles[i+1]]);var c=filter.transform.TransformPoint(vertices[triangles[i+2]]);
                        float det=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);
                        if(Mathf.Abs(det)<.000001f)continue;
                        float u=((b.z-c.z)*(-c.x)+(c.x-b.x)*(z-c.z))/det;
                        float v=((c.z-a.z)*(-c.x)+(a.x-c.x)*(z-c.z))/det;
                        if(u>=-.001f&&v>=-.001f&&u+v<=1.001f)height=Mathf.Max(height,u*a.y+v*b.y+(1-u-v)*c.y);
                    }
                }
                deck[sample]=float.IsNegativeInfinity(height)?0:height;
            }
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
            Facets("Jungle riverbank "+side,vertices,new Color(.27f,.36f,.14f));
        }
        void PopulateJungle()
        {
            var rng=new System.Random(1855);int[] types={0,2,3,5,7,8};
            foreach(int side in new[]{-1,1})
            {
                for(int i=0;i<32;i++)
                {
                    float x=-23+(float)rng.NextDouble()*46;
                    float z=side*(length/2+3+(float)rng.NextDouble()*17);
                    // The foreground camera corridor and the landing remain readable.
                    if(side<0&&x>-3&&x<12&&Mathf.Abs(z)<21)continue;
                    if(Mathf.Abs(x)<2.8f&&Mathf.Abs(z)<length/2+8)continue;
                    int type=types[rng.Next(types.Length)];
                    var tree=Asset("Tree_"+type.ToString("00"),"Supplied jungle tree",root.transform);
                    float height=4.5f+(float)rng.NextDouble()*3.5f;
                    tree.transform.localScale=Vector3.one*height;tree.transform.localPosition=new Vector3(x,.18f,z);
                    tree.transform.localRotation=Quaternion.Euler(0,(float)rng.NextDouble()*360,0);
                }
                for(int i=0;i<10;i++)
                {
                    float x=(i-4.5f)*3.6f;
                    if(Mathf.Abs(x)<2.5f)continue;
                    var rock=Asset("Tree_12","Supplied river rock",root.transform);
                    rock.transform.localScale=Vector3.one*(.45f+(float)rng.NextDouble()*.6f);
                    rock.transform.localPosition=new Vector3(x,-.05f,side*(length/2+.3f+(float)rng.NextDouble()));
                    rock.transform.localRotation=Quaternion.Euler(0,i*43,0);
                }
                for(int i=0;i<50;i++)
                {
                    float x=-25+(float)rng.NextDouble()*50,z=side*(length/2+.8f+(float)rng.NextDouble()*12);
                    if(Mathf.Abs(x)<2.5f&&Mathf.Abs(z)<length/2+6)continue;
                    var leaves=new List<Vector3>();
                    for(int blade=0;blade<5;blade++)
                    {
                        float angle=blade*Mathf.PI*.4f;var direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                        var across=Vector3.Cross(direction,Vector3.up)*.16f;
                        var tip=direction*.6f+Vector3.up*(.35f+(float)rng.NextDouble()*.45f);
                        leaves.AddRange(new[]{-across,tip,across,across,tip,-across});
                    }
                    Facets("Low-poly jungle undergrowth",leaves,i%2==0?new Color(.13f,.3f,.1f):new Color(.3f,.43f,.12f)).localPosition=new Vector3(x,.2f,z);
                }
            }
        }
        void CreateSpectators()
        {
            foreach(int side in new[]{-1,1})foreach(int bank in new[]{-1,1})
            {
                var npc=new GameObject("Gorilla spectator — NPC").transform;npc.SetParent(root.transform);
                npc.localPosition=new Vector3(side*3.2f,.15f,bank*(length/2+2));var fur=new Color(.19f,.16f,.13f);
                Part(npc,"Broad torso",new Vector3(0,1.05f,0),new Vector3(.95f,1.4f,.6f),fur);
                Part(npc,"Head",new Vector3(0,1.95f,.15f),Vector3.one*.65f,fur);
                Part(npc,"Muzzle",new Vector3(0,1.8f,.4f),new Vector3(.4f,.3f,.3f),new Color(.38f,.31f,.24f));
                foreach(int arm in new[]{-1,1})Part(npc,"Long arm",new Vector3(arm*.65f,.9f,0),new Vector3(.3f,1.3f,.3f),fur);
            }
        }
        void Part(Transform parent,string name,Vector3 position,Vector3 scale,Color color)
        {
            // Faceted octagonal forms keep even provisional NPCs within the low-poly direction.
            var vertices=new List<Vector3>();
            for(int i=0;i<8;i++)
            {
                float a=i*Mathf.PI/4,b=(i+1)*Mathf.PI/4;
                var p=new Vector3(Mathf.Cos(a)*.5f,-.3f,Mathf.Sin(a)*.5f);var q=new Vector3(Mathf.Cos(b)*.5f,-.3f,Mathf.Sin(b)*.5f);
                var r=p+Vector3.up*.6f;var s=q+Vector3.up*.6f;
                vertices.AddRange(new[]{p,r,q,q,r,s,Vector3.up*.5f,s,r,Vector3.down*.5f,p,q});
            }
            var obj=Facets(name,vertices,color,parent);obj.localPosition=position;obj.localScale=scale;
        }
        Transform Shape(string name,PrimitiveType type,Vector3 position,Vector3 scale,Color color)
        {
            var obj=GameObject.CreatePrimitive(type);obj.name=name;obj.transform.SetParent(root.transform,false);
            obj.transform.localPosition=position;obj.transform.localScale=scale;
            if(obj.GetComponent<MeshFilter>()==null)throw new System.InvalidOperationException("Primitive mesh missing.");
            obj.GetComponent<MeshRenderer>().sharedMaterial=Surface(color);return obj.transform;
        }
        public void Present(float progress,float balance,bool fell,float fallSeconds,float elapsed=0,bool walking=false,bool won=false)
        {
            float side=fell?(balance<0?-1:1)*Mathf.Min(2.7f,fallSeconds*3):0;
            float bob=walking?Mathf.Sin(elapsed*Mathf.PI*4.6f)*.025f:0;
            human.localPosition=new Vector3(side,SurfaceHeight(progress)+.035f+bob-(fell?Mathf.Min(3.5f,fallSeconds*4):0),-length/2+length*progress);
            human.localRotation=Quaternion.Euler(fell?Mathf.Min(65,fallSeconds*85):0,0,-balance*22-(fell?side*28:0));
            character.Pose(elapsed,balance,walking,fell,fallSeconds,won);
            if(!fell)human.localPosition+=Vector3.up*character.SupportOffset;
        }
        public void Dispose()
        {
            if(root!=null)root.SetActive(false);
            foreach(var camera in hidden)if(camera!=null)camera.enabled=true;
            foreach(var material in materials)Object.Destroy(material);foreach(var mesh in meshes)Object.Destroy(mesh);
            Object.Destroy(root);
        }
    }
}
