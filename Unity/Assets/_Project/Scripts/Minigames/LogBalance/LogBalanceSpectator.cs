using System;
using System.Collections.Generic;
using UnityEngine;
namespace GorillaEscape.Minigames.LogBalance
{
    /// <summary>In-place spectator gestures on the supplied gorilla's original weighted skeleton.</summary>
    public sealed class LogBalanceSpectator : MonoBehaviour
    {
        struct Rest {public Transform bone;public Quaternion rotation;}
        readonly List<Rest> rest=new List<Rest>();
        Transform chest,head,leftArm,rightArm,leftElbow,rightElbow;
        public bool HasRig {get;private set;}
        bool attempted;
        void Awake()=>BindRig();
        public void BindRig()
        {
            attempted=true;rest.Clear();var bones=GetComponentsInChildren<Transform>(true);
            Transform Find(params string[] names)=>Array.Find(bones,b=>Array.Exists(names,n=>b.name==n||b.name.EndsWith(":"+n,StringComparison.Ordinal)));
            chest=Find("Spine2","Spine1","Chest_M_038");head=Find("Head","Head_M_040");
            leftArm=Find("LeftArm","LeftShoulder","Shoulder_L_043");rightArm=Find("RightArm","RightShoulder","Shoulder_R_062");
            leftElbow=Find("LeftForeArm","Elbow_L_00");rightElbow=Find("RightForeArm","Elbow_R_063");
            HasRig=chest!=null&&head!=null&&leftArm!=null&&rightArm!=null&&leftElbow!=null&&rightElbow!=null;
            foreach(var bone in new[]{chest,head,leftArm,rightArm,leftElbow,rightElbow})
                if(bone!=null)rest.Add(new Rest{bone=bone,rotation=bone.localRotation});
        }
        void Turn(Transform bone,Vector3 axis,float degrees){if(bone!=null)bone.rotation=Quaternion.AngleAxis(degrees,axis)*bone.rotation;}
        public void Pose(float time,float phase,float balance,bool fell,float fallSeconds,bool won)
        {
            if(!attempted)BindRig();if(!HasRig)return;
            foreach(var pose in rest)pose.bone.localRotation=pose.rotation;
            float wave=Mathf.Sin(time*1.7f+phase),breath=Mathf.Sin(time*2.2f+phase);
            float tension=Mathf.InverseLerp(.45f,.85f,Mathf.Abs(balance));
            float surprise=fell?Mathf.Clamp01(1-fallSeconds/2.5f):0;
            float cheer=won?1:0;
            Turn(chest,transform.right,breath*1.2f+tension*5-surprise*10);
            Turn(chest,transform.forward,wave*1.5f);
            Turn(head,transform.up,Mathf.Sin(time*.8f+phase)*5);
            Turn(head,transform.right,breath*2+surprise*12-cheer*8);
            float beat=(Mathf.Sin(time*7+phase)+1)*.5f;
            Turn(leftArm,transform.forward,-(cheer*(35+beat*10)+surprise*22));
            Turn(rightArm,transform.forward,cheer*(35+(1-beat)*10)+surprise*22);
            Turn(leftArm,transform.right,-tension*8-cheer*20);
            Turn(rightArm,transform.right,-tension*8-cheer*20);
            Turn(leftElbow,transform.right,-(cheer*(25+beat*35)+surprise*18));
            Turn(rightElbow,transform.right,-(cheer*(25+(1-beat)*35)+surprise*18));
        }
    }
}
