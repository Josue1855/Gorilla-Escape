using System;
using System.Linq;
using GorillaEscape.Minigames.LogBalance;
using NUnit.Framework;
using UnityEngine;
public sealed class LogBalanceArtTests
{
    [Test] public void SuppliedModelsAndTexturesAreAvailableWithoutRuntimeGlbLoader()
    {
        foreach(string name in new[]{"trunk","Vagabundo","Tree_00","Tree_02","Tree_12"})
        {
            var prefab=Resources.Load<GameObject>("LogBalance/Prefabs/"+name);Assert.NotNull(prefab,name);
            var renderers=prefab.GetComponentsInChildren<Renderer>();Assert.That(renderers.Length,Is.GreaterThan(0));
            foreach(var renderer in renderers)foreach(var material in renderer.sharedMaterials)
            {Assert.NotNull(material);Assert.That(material.GetFloat("_Metallic"),Is.Zero);if(name!="trunk")Assert.NotNull(material.mainTexture,name+": albedo missing");}
        }
    }
    [Test] public void CastawayRetainsWeightedSkeletonAndProceduralWalkingDeformsMesh()
    {
        var prefab=Resources.Load<GameObject>("LogBalance/Prefabs/Vagabundo");var human=UnityEngine.Object.Instantiate(prefab);
        var bakedA=new Mesh();var bakedB=new Mesh();
        try
        {
            var driver=human.GetComponent<LogBalanceCharacter>();driver.BindRig();Assert.True(driver.HasRig);
            var skins=human.GetComponentsInChildren<SkinnedMeshRenderer>();Assert.That(skins.Length,Is.EqualTo(3));
            Assert.That(skins.Max(s=>s.bones.Length),Is.GreaterThanOrEqualTo(50));
            var skin=skins.OrderByDescending(s=>s.sharedMesh.vertexCount).First();
            driver.Pose(0,0,false,false,0,false);skin.BakeMesh(bakedA);
            driver.Pose(.2f,.6f,true,false,0,false);skin.BakeMesh(bakedB);
            Assert.That(bakedA.vertexCount,Is.EqualTo(bakedB.vertexCount));
            var a=bakedA.vertices;var b=bakedB.vertices;Assert.True(a.Where((v,i)=>(v-b[i]).sqrMagnitude>.000001f).Any(),"Bone poses must deform actual skin, not a detached skeleton.");
            driver.Pose(0,0,false,false,0,false);skin.BakeMesh(bakedB);b=bakedB.vertices;
            Assert.That(a.Where((v,i)=>(v-b[i]).sqrMagnitude>.000001f).Count(),Is.Zero,"Reset must restore the original weighted pose.");
            Assert.That(human.transform.position,Is.EqualTo(Vector3.zero),"Animation must never own forward movement.");
        }
        finally{UnityEngine.Object.DestroyImmediate(human);UnityEngine.Object.DestroyImmediate(bakedA);UnityEngine.Object.DestroyImmediate(bakedB);}
    }
    [Test] public void FallAndCelebrationPoseAreDistinctAndDoNotAccumulateDrift()
    {
        var human=UnityEngine.Object.Instantiate(Resources.Load<GameObject>("LogBalance/Prefabs/Vagabundo"));
        try
        {
            var driver=human.GetComponent<LogBalanceCharacter>();driver.BindRig();
            var bone=human.GetComponentsInChildren<Transform>().First(t=>t.name=="CATRigLArm1_013");
            driver.Pose(0,0,false,true,.5f,false);var fall=bone.localRotation;
            driver.Pose(0,0,false,false,0,true);var win=bone.localRotation;Assert.That(Quaternion.Angle(fall,win),Is.GreaterThan(10));
            for(int i=0;i<100;i++)driver.Pose(0,0,false,false,0,true);
            Assert.That(Quaternion.Angle(win,bone.localRotation),Is.LessThan(.01f));
        }
        finally{UnityEngine.Object.DestroyImmediate(human);}
    }
}
