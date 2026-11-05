using UnityEngine;
using UnityEngine.Rendering;

// Shared tropical perimeter for arenas. Decoration stays outside the playing area.
public sealed class JungleArenaEnvironment : MonoBehaviour
{
    Material palmMaterial, bushMaterial;
    public static void Build(Transform parent, float groundY, float halfWidth, float depth, bool night = false)
    {
        var root=new GameObject("Perímetro tropical");root.transform.SetParent(parent,false);
        var environment=root.AddComponent<JungleArenaEnvironment>();
        var sky=night?new Color(.035f,.12f,.16f):new Color(.38f,.7f,.88f);
        if(Camera.main!=null){Camera.main.clearFlags=CameraClearFlags.SolidColor;Camera.main.backgroundColor=sky;}
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=night?new Color(.24f,.36f,.43f):new Color(.55f,.67f,.72f);
        RenderSettings.ambientEquatorColor=new Color(.28f,.36f,.25f);
        RenderSettings.ambientGroundColor=new Color(.17f,.22f,.14f);
        RenderSettings.fog=true;RenderSettings.fogColor=sky;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.009f;
        JungleStageBuilder.CreatePrimitive(PrimitiveType.Cube,"Terreno continuo",new Vector3(0,groundY-.3f,depth*.4f),new Vector3(180,.25f,200),new Color(.21f,.37f,.13f),parent:root.transform);
        const string palm="Jungle/NewModels/Palm/Meshy_AI_Cartoon_Coconut_Palm_20261026_texture_fbx/";
        const string bush="Jungle/NewModels/Bush/Meshy_AI_Emerald_Canopy_20261026_texture_fbx/";
        environment.palmMaterial=JungleStageBuilder.CreateTexturedMaterial(JungleAssets.Load<Texture2D>(palm+"Palm_Albedo"),"Palmeras del estadio",.15f,JungleAssets.Load<Texture2D>(palm+"Meshy_AI_Cartoon_Coconut_Palm_20261026_texture_normal"),.6f);
        environment.bushMaterial=JungleStageBuilder.CreateTexturedMaterial(JungleAssets.Load<Texture2D>(bush+"Bush_Albedo"),"Arbustos del estadio",.12f);
        var palmPrefab=JungleAssets.Load<GameObject>(palm+"Meshy_AI_Cartoon_Coconut_Palm_20261026_texture");
        var bushPrefab=JungleAssets.Load<GameObject>(bush+"Meshy_AI_Emerald_Canopy_20261026_texture");
        for(int side=-1;side<=1;side+=2)for(int i=0;i<5;i++) {
            float z=-1f+i*depth*.24f;
            JungleStageBuilder.InstantiateGroundedProp(palmPrefab,root.transform,environment.palmMaterial,"Palmera de borde",new Vector3(side*(halfWidth+2f+i%2),groundY,z),7f+i%3,side*(15+i*31));
            JungleStageBuilder.InstantiateGroundedProp(bushPrefab,root.transform,environment.bushMaterial,"Vegetación de borde",new Vector3(side*(halfWidth+1f),groundY,z+2.5f),1.5f+i%2*.4f,i*53);
        }
    }
    void OnDestroy(){if(palmMaterial!=null)Destroy(palmMaterial);if(bushMaterial!=null)Destroy(bushMaterial);}
}
