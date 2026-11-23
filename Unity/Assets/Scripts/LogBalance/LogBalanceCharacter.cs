using System;
using System.Collections.Generic;
using UnityEngine;
namespace GorillaEscape.Minigames.LogBalance
{
    /// <summary>Additive, in-place posing of the supplied Castaway skeleton; gameplay owns translation.</summary>
    public sealed class LogBalanceCharacter : MonoBehaviour
    {
        struct RestPose {public Transform bone;public Quaternion rotation;}
        readonly List<RestPose> rest=new List<RestPose>();
        Transform hip,leftThigh,rightThigh,leftKnee,rightKnee,leftFoot,rightFoot,spine,chest,head,leftArm,rightArm,leftElbow,rightElbow;
        bool bound;
        float restAnkleHeight;
        public float SupportOffset {get;private set;}
        public bool HasRig { get { if (!bound) BindRig(); return bound; } }
        public void BindRig()
        {
            rest.Clear();
            var bones=GetComponentsInChildren<Transform>(true);
            Transform Find(string name)=>Array.Find(bones,b=>b.name==name||b.name.EndsWith(":"+name,StringComparison.Ordinal)||b.name.StartsWith(name,StringComparison.OrdinalIgnoreCase)||b.name.IndexOf(name,StringComparison.OrdinalIgnoreCase)>=0);
            hip=Find("CATRigpelvis_00");spine=Find("CATRigSpinespine1_09");chest=Find("CATRigchest_011");head=Find("CATRighead_032");
            leftThigh=Find("CATRigLLeg1_02");rightThigh=Find("CATRigRLeg1_05");leftKnee=Find("CATRigLLeg2_01");rightKnee=Find("CATRigRLeg2_06");
            leftFoot=Find("CATRigLLegLAnkle_03");rightFoot=Find("CATRigRLegRAnkle_07");
            leftArm=Find("CATRigLArm1_013");rightArm=Find("CATRigRArm1_034");leftElbow=Find("CATRigLArm2_014");rightElbow=Find("CATRigRArm2_035");
            foreach(var b in new[]{hip,spine,chest,head,leftThigh,rightThigh,leftKnee,rightKnee,leftFoot,rightFoot,leftArm,rightArm,leftElbow,rightElbow})
                if(b!=null)rest.Add(new RestPose{bone=b,rotation=b.localRotation});
            if(leftFoot!=null&&rightFoot!=null)restAnkleHeight=Mathf.Min(transform.InverseTransformPoint(leftFoot.position).y,transform.InverseTransformPoint(rightFoot.position).y);
            bound=(hip!=null&&leftThigh!=null&&rightThigh!=null&&leftArm!=null&&rightArm!=null)||bones.Length>1;
        }
        void Awake(){BindRig();}
        void Turn(Transform bone,Vector3 axis,float degrees){if(bone!=null)bone.Rotate(axis,degrees,Space.World);}
        public void Pose(float elapsed,float balance,bool walking,bool fell,float fallTime,bool won,float gaitPhase=-1)
        {
            if(!bound)BindRig();
            foreach(var pose in rest)pose.bone.localRotation=pose.rotation;
            var lateral=transform.right;var forward=transform.forward;
            float phase=gaitPhase>=0?gaitPhase:elapsed*2*Mathf.PI*1.15f;
            float stride=walking?Mathf.Sin(phase)*20:0;
            Turn(leftThigh,lateral,stride);Turn(rightThigh,lateral,-stride);
            Turn(leftKnee,lateral,walking?-Mathf.Max(0,-Mathf.Sin(phase))*26:0);
            Turn(rightKnee,lateral,walking?-Mathf.Max(0,Mathf.Sin(phase))*26:0);
            Turn(leftFoot,lateral,-stride*.35f);Turn(rightFoot,lateral,stride*.35f);
            float alarm=Mathf.InverseLerp(.4f,.9f,Mathf.Abs(balance));
            float breath=!walking&&Mathf.Abs(balance)<=.05f?Mathf.Sin(elapsed*2.2f):0;
            Turn(spine,forward,balance*10);Turn(chest,lateral,walking?3:breath*1.5f);Turn(head,forward,balance*8);
            float armSpread=(walking||Mathf.Abs(balance)>.05f?42:18+breath*1.5f)+alarm*18;
            Turn(leftArm,forward,-armSpread-balance*16);Turn(rightArm,forward,armSpread-balance*16);
            float elbowBend=(walking||Mathf.Abs(balance)>.05f?-16:-12)-alarm*10;
            Turn(leftElbow,lateral,elbowBend);Turn(rightElbow,lateral,elbowBend);
            if(fell)
            {
                float tumble=Mathf.Clamp01(fallTime*2);
                Turn(spine,lateral,25*tumble);Turn(leftThigh,lateral,35*tumble);Turn(rightThigh,lateral,-25*tumble);
                Turn(leftKnee,lateral,-45*tumble);Turn(rightKnee,lateral,-30*tumble);
                Turn(leftArm,lateral,-65*tumble);Turn(rightArm,lateral,50*tumble);
            }
            if(won){Turn(leftArm,forward,-65);Turn(rightArm,forward,65);Turn(head,lateral,-8);}
            SupportOffset=leftFoot!=null&&rightFoot!=null?restAnkleHeight-Mathf.Min(transform.InverseTransformPoint(leftFoot.position).y,transform.InverseTransformPoint(rightFoot.position).y):0;
        }
    }
}
