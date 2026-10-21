using System.Collections.Generic;
using UnityEngine;
namespace GorillaEscape.Minigames.LogBalance
{
    // Original, disposable primitive scenery. The controlled figure is always a human castaway.
    public sealed class LogBalanceStage
    {
        readonly GameObject root;
        readonly List<Material> materials = new List<Material>();
        readonly List<Camera> hidden = new List<Camera>();
        readonly Transform human;
        readonly float length;
        public GameObject Root => root;
        public LogBalanceStage(Transform parent,float length)
        {
            this.length=length;
            root=new GameObject("Log Balance — temporary scenery");root.transform.SetParent(parent,false);
            foreach(var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if(camera.enabled){hidden.Add(camera);camera.enabled=false;}
            var view=new GameObject("Log Balance Camera");view.transform.SetParent(root.transform);
            var cam=view.AddComponent<Camera>();cam.transform.position=new Vector3(14,12,-14);
            cam.transform.LookAt(new Vector3(0,0,1));cam.clearFlags=CameraClearFlags.SolidColor;
            cam.backgroundColor=new Color(.13f,.25f,.28f);cam.orthographic=true;cam.orthographicSize=13;
            var sun=new GameObject("Stage light");sun.transform.SetParent(root.transform);
            var light=sun.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.1f;
            sun.transform.rotation=Quaternion.Euler(45,-30,0);
            Shape("River",PrimitiveType.Cube,new Vector3(0,-1,0),new Vector3(26,.25f,length+16),new Color(.12f,.48f,.58f));
            foreach(int sign in new[]{-1,1})Shape("Bank",PrimitiveType.Cube,new Vector3(0,-.45f,sign*(length/2+3)),new Vector3(20,1,6),new Color(.27f,.42f,.19f));
            var log=Shape("Crossing log",PrimitiveType.Cylinder,Vector3.zero,new Vector3(1.2f,length/2,1.2f),new Color(.35f,.19f,.09f));
            log.localRotation=Quaternion.Euler(90,0,0);
            Shape("Goal marker",PrimitiveType.Cube,new Vector3(0,.12f,length/2+.7f),new Vector3(2,.1f,.4f),new Color(.98f,.77f,.24f));
            human=new GameObject("Human castaway — controlled").transform;human.SetParent(root.transform);
            Part(human,"Shirt",PrimitiveType.Capsule,new Vector3(0,1.1f,0),new Vector3(.48f,.5f,.3f),new Color(.83f,.64f,.33f));
            Part(human,"Human head",PrimitiveType.Sphere,new Vector3(0,1.9f,0),Vector3.one*.42f,new Color(.67f,.42f,.28f));
            foreach(int sign in new[]{-1,1})
            {
                Part(human,"Leg",PrimitiveType.Capsule,new Vector3(sign*.16f,.4f,0),new Vector3(.17f,.38f,.2f),new Color(.18f,.25f,.32f));
                var arm=Part(human,"Outstretched arm",PrimitiveType.Capsule,new Vector3(sign*.56f,1.4f,0),new Vector3(.15f,.4f,.15f),new Color(.67f,.42f,.28f));arm.localRotation=Quaternion.Euler(0,0,90);
            }
            foreach(int sign in new[]{-1,1})foreach(int bank in new[]{-1,1})
            {
                var npc=new GameObject("Gorilla spectator — NPC").transform;npc.SetParent(root.transform);
                npc.localPosition=new Vector3(sign*3,.1f,bank*(length/2+2));
                var fur=new Color(.16f,.14f,.13f);
                Part(npc,"Broad torso",PrimitiveType.Capsule,new Vector3(0,1.05f,0),new Vector3(.95f,.7f,.6f),fur);
                Part(npc,"Head",PrimitiveType.Sphere,new Vector3(0,1.95f,.15f),Vector3.one*.65f,fur);
                Part(npc,"Muzzle",PrimitiveType.Sphere,new Vector3(0,1.8f,.4f),new Vector3(.4f,.3f,.3f),new Color(.36f,.3f,.25f));
                foreach(int side in new[]{-1,1})Part(npc,"Long arm",PrimitiveType.Capsule,new Vector3(side*.65f,.9f,0),new Vector3(.3f,.65f,.3f),fur);
            }
            Present(0,0,false,0);
        }
        Transform Shape(string name,PrimitiveType type,Vector3 position,Vector3 scale,Color color)
            => Part(root.transform,name,type,position,scale,color);
        Transform Part(Transform parent,string name,PrimitiveType type,Vector3 position,Vector3 scale,Color color)
        {
            var shape=GameObject.CreatePrimitive(type);shape.name=name;shape.transform.SetParent(parent,false);
            shape.transform.localPosition=position;shape.transform.localScale=scale;
            var template=Resources.Load<Material>("LogBalanceStage");
            var material=template!=null?new Material(template):new Material(Shader.Find("Standard"));
            material.color=color;materials.Add(material);
            // Explicit component references also preserve primitive rendering in stripped Players.
            if(shape.GetComponent<MeshFilter>()==null)throw new System.InvalidOperationException("Primitive mesh missing.");
            shape.GetComponent<MeshRenderer>().sharedMaterial=material;
            return shape.transform;
        }
        public void Present(float progress,float balance,bool fell,float fallSeconds)
        {
            float side=fell?(balance<0?-1:1)*Mathf.Min(2,fallSeconds*3):0;
            human.localPosition=new Vector3(side,.65f-(fell?Mathf.Min(3,fallSeconds*4):0),-length/2+length*progress);
            human.localRotation=Quaternion.Euler(0,0,-balance*28-(fell?side*30:0));
        }
        public void Dispose()
        {
            root.SetActive(false);
            foreach(var camera in hidden)if(camera!=null)camera.enabled=true;
            foreach(var material in materials)Object.Destroy(material);
            Object.Destroy(root);
        }
    }
}
