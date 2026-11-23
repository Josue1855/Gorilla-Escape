using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// A presentation-only set: no game controllers, physical scenery or extra sensor sessions.
public sealed class JungleMenuStage : MonoBehaviour
{
    public const string GorillaResource = "Jungle/GorillaBowling/Meshy_AI_Low_Poly_Gorilla_Embr_biped/Meshy_AI_Low_Poly_Gorilla_Embr_biped_Meshy_Merged_Animations";
    private readonly List<Material> materials = new List<Material>();
    private readonly List<Mesh> meshes = new List<Mesh>();
    private readonly List<PlayableGraph> graphs = new List<PlayableGraph>();
    private readonly List<AnimationClipPlayable> animations = new List<AnimationClipPlayable>();
    private readonly List<float> durations = new List<float>();
    private readonly List<Transform> foliage = new List<Transform>();
    private readonly List<Quaternion> foliageRest = new List<Quaternion>();
    private readonly List<GameObject> platforms = new List<GameObject>();
    private GameObject set;
    private Camera cameraView;
    private bool ownCamera;
    private Vector3 oldCameraPosition; private Quaternion oldCameraRotation;
    private CameraClearFlags oldClear; private Color oldBackground; private bool oldOrtho; private float oldSize, oldAspect;
    private Material leaf, trunk, stone, grass;
    public int GorillaCount { get; private set; }
    public int AnimatedCount => animations.Count;
    public Camera PresentationCamera => cameraView;

    private void Awake() { Initialize(); }
    public void Initialize()
    {
        if(set!=null) return;
        set=new GameObject("MENU - Isla tropical y gorilas");set.transform.SetParent(transform,false);
        cameraView=Camera.main;
        if(cameraView==null) { var go=new GameObject("Menu Camera");go.transform.SetParent(set.transform);go.tag="MainCamera";cameraView=go.AddComponent<Camera>();ownCamera=true; }
        oldCameraPosition=cameraView.transform.position;oldCameraRotation=cameraView.transform.rotation;
        oldClear=cameraView.clearFlags;oldBackground=cameraView.backgroundColor;oldOrtho=cameraView.orthographic;oldSize=cameraView.orthographicSize;oldAspect=cameraView.aspect;
        cameraView.transform.position=new Vector3(0,8,-23);cameraView.transform.LookAt(new Vector3(0,2,1));
        cameraView.orthographic=true;cameraView.orthographicSize=8.1f;cameraView.aspect=1280f/720f;
        cameraView.clearFlags=CameraClearFlags.SolidColor;cameraView.backgroundColor=new Color(.12f,.43f,.40f);
        leaf=Mat(new Color(.12f,.38f,.14f));trunk=Mat(new Color(.25f,.13f,.055f));stone=Mat(new Color(.30f,.33f,.21f));grass=Mat(new Color(.24f,.40f,.16f));
        Shape("Isla",PrimitiveType.Cylinder,new Vector3(0,-.8f,1),new Vector3(32,.8f,19),grass);
        Material water=Mat(new Color(.07f,.56f,.51f));
        Shape("Laguna",PrimitiveType.Cube,new Vector3(0,-1.7f,5),new Vector3(80,.25f,80),water);
        for(int i=0;i<13;i++)
        {
            float x=-26+i*4.3f;float height=5+(i*7%5);
            Peak("Montaña facetada",new Vector3(x,-1,18+i%3*3),new Vector3(5,height,5),i%2==0?stone:grass);
        }
        for(int i=0;i<18;i++)
        {
            float x=-19+i*2.25f;float z=8+(i%3)*2;
            Palm(new Vector3(x,-.1f,z),3.4f+(i%4)*.5f,i*29);
        }
        Palm(new Vector3(-13,0,-1),6,20);Palm(new Vector3(13,0,0),6,-25);
        var lightGo=new GameObject("Luz dorada del menú");lightGo.transform.SetParent(set.transform);lightGo.transform.rotation=Quaternion.Euler(48,-32,0);
        Light light=lightGo.AddComponent<Light>();light.type=LightType.Directional;light.color=new Color(1,.86f,.61f);light.intensity=.9f;
        var fillGo=new GameObject("Relleno turquesa");fillGo.transform.SetParent(set.transform);fillGo.transform.rotation=Quaternion.Euler(32,145,0);
        Light fill=fillGo.AddComponent<Light>();fill.type=LightType.Directional;fill.color=new Color(.5f,.85f,1);fill.intensity=.55f;
        BuildGorillas();
    }
    private Material Mat(Color color, Texture texture=null)
    {
        var material=new Material(Shader.Find("Standard"));material.color=color;material.SetFloat("_Glossiness",.12f);material.mainTexture=texture;materials.Add(material);return material;
    }
    private GameObject Shape(string name,PrimitiveType type,Vector3 pos,Vector3 scale,Material material)
    {
        GameObject go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(set.transform,false);go.transform.localPosition=pos;go.transform.localScale=scale;
        go.GetComponent<Renderer>().sharedMaterial=material;
        Collider collider=go.GetComponent<Collider>();if(collider!=null) { collider.enabled=false;Release(collider); }
        return go;
    }
    private void Peak(string name,Vector3 position,Vector3 scale,Material material)
    {
        // Separate triangle vertices preserve the flat-shaded, low-poly art direction.
        Vector3 top=new Vector3(0,1,0);Vector3[] ring={new Vector3(-1,0,-1),new Vector3(1,0,-1),new Vector3(1,0,1),new Vector3(-1,0,1)};
        var vertices=new Vector3[12];var triangles=new int[12];
        for(int i=0;i<4;i++){vertices[i*3]=ring[i];vertices[i*3+1]=top;vertices[i*3+2]=ring[(i+1)%4];triangles[i*3]=i*3;triangles[i*3+1]=i*3+1;triangles[i*3+2]=i*3+2;}
        var mesh=new Mesh();mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();meshes.Add(mesh);
        var go=new GameObject(name);go.transform.SetParent(set.transform,false);go.transform.localPosition=position;go.transform.localScale=scale;
        go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;
    }
    private void Palm(Vector3 pos,float height,float twist)
    {
        var stem=Shape("Tronco de palma",PrimitiveType.Cylinder,pos+Vector3.up*height*.5f,new Vector3(.25f,height*.5f,.25f),trunk);
        for(int j=0;j<6;j++)
        {
            var frond=Shape("Hoja de palma",PrimitiveType.Cube,pos+Vector3.up*height,new Vector3(.5f,.12f,2.7f),leaf);
            frond.transform.localRotation=Quaternion.Euler(18,twist+j*60,0);
            frond.transform.position+=frond.transform.forward*1.1f;
            foliage.Add(frond.transform);foliageRest.Add(frond.transform.localRotation);
        }
    }
    private void BuildGorillas()
    {
        GameObject prefab=Resources.Load<GameObject>(GorillaResource);
        if(prefab==null) { Debug.LogError("No se encontró el modelo 3D para el menú: "+GorillaResource);return; }
        AnimationClip[] clips=Resources.LoadAll<AnimationClip>(GorillaResource);
        string[] preferred={"Alert","Angry_Ground_Stomp_2","Angry_Ground_Stomp","All_Night_Dance"};
        string textureRoot="Jungle/GorillaBowling/Meshy_AI_Low_Poly_Gorilla_Embr_biped/";
        Texture2D texture=Resources.Load<Texture2D>(textureRoot+"Meshy_AI_Low_Poly_Gorilla_Embr_biped_texture_0");
        Color[] colors={new Color(1,.90f,.67f),new Color(.76f,.82f,.92f),new Color(.9f,.69f,.46f),new Color(.68f,.92f,.78f)};
        for(int i=0;i<4;i++)
        {
            float x=(i-1.5f)*6.8f;
            platforms.Add(Shape("Pedestal "+i,PrimitiveType.Cylinder,new Vector3(x,-.15f,0),new Vector3(5.7f,.25f,4.6f),stone));
            GameObject gorilla=Instantiate(prefab,set.transform);gorilla.name="Gorila "+preferred[i];
            gorilla.transform.rotation=Quaternion.Euler(0,180,0);
            foreach(Animation a in gorilla.GetComponentsInChildren<Animation>(true)) a.enabled=false;
            foreach(Collider c in gorilla.GetComponentsInChildren<Collider>(true)) c.enabled=false;
            Renderer[] renderers=gorilla.GetComponentsInChildren<Renderer>(true);
            if(renderers.Length==0) {Release(gorilla);continue;}
            var material=Mat(colors[i],texture);
            foreach(Renderer renderer in renderers)
            {
                var slots=renderer.sharedMaterials;for(int m=0;m<slots.Length;m++) slots[m]=material;renderer.sharedMaterials=slots;
                if(renderer is SkinnedMeshRenderer skin) skin.updateWhenOffscreen=true;
            }
            Bounds bounds=renderers[0].bounds;foreach(Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
            gorilla.transform.localScale*=4.6f/Mathf.Max(.01f,bounds.size.y);
            bounds=renderers[0].bounds;foreach(Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
            gorilla.transform.position+=new Vector3(x-bounds.center.x,.10f-bounds.min.y,-bounds.center.z);
            GorillaCount++;
            AnimationClip chosen=null;
            foreach(AnimationClip clip in clips) if(clip.name==preferred[i]) {chosen=clip;break;}
            if(chosen==null) foreach(AnimationClip clip in clips) if(!clip.name.StartsWith("__preview__")) {chosen=clip;break;}
            if(chosen==null) {Debug.LogWarning("Gorila sin clip en el menú");continue;}
            Animator animator=gorilla.GetComponentInChildren<Animator>(true);
            if(animator==null) animator=gorilla.AddComponent<Animator>();
            animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var graph=PlayableGraph.Create("Menú - "+chosen.name);graph.SetTimeUpdateMode(DirectorUpdateMode.UnscaledGameTime);
            var playable=AnimationClipPlayable.Create(graph,chosen);playable.SetApplyFootIK(true);playable.SetSpeed(.85+i*.07);
            AnimationPlayableOutput.Create(graph,"Gorila",animator).SetSourcePlayable(playable);
            graph.Play();graphs.Add(graph);animations.Add(playable);durations.Add(Mathf.Max(.1f,chosen.length));
            Debug.Log("MENU_GORILLA "+i+": "+chosen.name);
        }
    }
    private void Update()
    {
        for(int i=0;i<animations.Count;i++) if(animations[i].IsValid()&&animations[i].GetTime()>=durations[i]) animations[i].SetTime(animations[i].GetTime()%durations[i]);
        for(int i=0;i<foliage.Count;i++) foliage[i].localRotation=foliageRest[i]*Quaternion.Euler(Mathf.Sin(Time.unscaledTime*.7f+i*.6f)*2.5f,0,0);
    }
    public void SetSelection(int index)
    {
        for(int i=0;i<platforms.Count;i++) platforms[i].transform.localScale=new Vector3(i==index?6f:5.7f,.25f,4.6f);
    }
    public void SampleAnimations(float seconds)
    {
        for(int i=0;i<graphs.Count;i++) { animations[i].SetTime(seconds); graphs[i].Evaluate(0); }
    }
    private static void Release(UnityEngine.Object item)
    {
        if(item==null) return;
        if(Application.isPlaying) Destroy(item); else DestroyImmediate(item);
    }
    private void OnDestroy() { Shutdown(); }
    public void Shutdown()
    {
        foreach(var graph in graphs) if(graph.IsValid()) graph.Destroy();
        foreach(Material material in materials) Release(material);
        foreach(Mesh mesh in meshes) Release(mesh);
        if(cameraView!=null&&!ownCamera)
        {
            cameraView.transform.SetPositionAndRotation(oldCameraPosition,oldCameraRotation);cameraView.clearFlags=oldClear;
            cameraView.backgroundColor=oldBackground;cameraView.orthographic=oldOrtho;cameraView.orthographicSize=oldSize;cameraView.aspect=oldAspect;
        }
        if(set!=null) Release(set);
        set=null;cameraView=null;
        graphs.Clear();animations.Clear();durations.Clear();materials.Clear();meshes.Clear();
    }
}
