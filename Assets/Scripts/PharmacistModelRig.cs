using System.Collections.Generic;
using UnityEngine;

namespace PharmaBrawl
{
    // Works with the actual skinned bones returned by Tripo, including rigs whose
    // joint names differ from Mixamo. No paid retarget clips are required.
    public sealed class PharmacistModelRig : MonoBehaviour
    {
        readonly List<Material> runtimeMaterials=new List<Material>();
        void OnDestroy(){foreach(var material in runtimeMaterials)if(material)Destroy(material);}
        readonly List<Transform> bones=new List<Transform>();
        readonly List<Quaternion> restRotations=new List<Quaternion>();
        Transform visual, rightHand, leftHand, rightHip, leftHip, weaponSocket, chest;
        Quaternion weaponRotation;
        Vector3 previousPosition;
        float gait, speed, hurtRemaining;
        Vector3 visualRestPosition;
        float previousYaw,turnLean;
        public void ReactToHit(){hurtRemaining=.42f;}
        public bool HasHandSocket => rightHand && weaponSocket;

        public void Initialize(CharacterDefinition data)
        {
            visual=new GameObject("Tripo / "+data.displayName).transform;
            visual.SetParent(transform,false);
            Instantiate(data.characterPrefab,visual);
            foreach(var renderer in visual.GetComponentsInChildren<Renderer>())foreach(var material in renderer.materials){
                runtimeMaterials.Add(material);material.SetFloat("_Metallic",0);material.SetFloat("_Glossiness",.15f);material.DisableKeyword("_METALLICGLOSSMAP");
                material.EnableKeyword("_EMISSION");material.SetTexture("_EmissionMap",material.mainTexture);material.SetColor("_EmissionColor",new Color(.3f,.3f,.3f));
            }
            foreach(var animator in visual.GetComponentsInChildren<Animator>())animator.enabled=false;
            foreach(var collider in visual.GetComponentsInChildren<Collider>())Destroy(collider);
            Bounds bounds=BoundsOf(visual);
            float scale=2.8f/Mathf.Max(.01f,bounds.size.y);
            visual.localScale=Vector3.one*scale;
            visual.localPosition=new Vector3(-bounds.center.x*scale,-bounds.min.y*scale,-bounds.center.z*scale);
            visualRestPosition=visual.localPosition;
            var unique=new HashSet<Transform>();
            foreach(var skin in visual.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                skin.updateWhenOffscreen=true;
                foreach(var bone in skin.bones)if(bone && unique.Add(bone))bones.Add(bone);
            }
            foreach(var bone in bones){restRotations.Add(bone.localRotation);if(bone.name=="Chest")chest=bone;}
            // Locate hands geometrically rather than depending on vendor bone names.
            foreach(var bone in bones)
            {
                Vector3 p=transform.InverseTransformPoint(bone.position);
                if(p.y>.55f && p.y<1.65f)
                {
                    if(p.x>.24f && (!rightHand || p.x>transform.InverseTransformPoint(rightHand.position).x))rightHand=bone;
                    if(p.x<-.24f && (!leftHand || p.x<transform.InverseTransformPoint(leftHand.position).x))leftHand=bone;
                }
                if(p.y>.28f && p.y<.7f && Mathf.Abs(p.x)>.07f && Mathf.Abs(p.x)<.35f)
                {
                    if(p.x>0 && (!rightHip || p.y>transform.InverseTransformPoint(rightHip.position).y))rightHip=bone;
                    if(p.x<0 && (!leftHip || p.y>transform.InverseTransformPoint(leftHip.position).y))leftHip=bone;
                }
            }
            if(!rightHand){Debug.LogError("TRIPO_MISSING_HAND "+data.displayName);return;}
            rightHand=Palm(rightHand);
            leftHand=Palm(leftHand);
            weaponSocket=new GameObject("Weapon grip / Right hand").transform;
            weaponSocket.SetParent(rightHand,false);
            weaponSocket.localPosition=data.weaponGripPosition;
            Vector3 parentScale=rightHand.lossyScale;
            weaponSocket.localScale=new Vector3(1/parentScale.x,1/parentScale.y,1/parentScale.z);
            weaponSocket.rotation=transform.rotation;
            if(data.weaponPrefab)
            {
                var weapon=new GameObject("Tripo / Medical weapon").transform;
                weapon.SetParent(weaponSocket,false);
                Instantiate(data.weaponPrefab,weapon);
                Bounds gunBounds=BoundsOf(weapon);
                bool barrelX=gunBounds.size.x>gunBounds.size.z;
                weaponRotation=Quaternion.Euler(0,barrelX?-90:0,0)*Quaternion.Euler(data.weaponGripEuler);
                float gunScale=data.weaponLength/Mathf.Max(.01f,Mathf.Max(gunBounds.size.x,Mathf.Max(gunBounds.size.y,gunBounds.size.z)));
                weapon.localScale=Vector3.one*gunScale;
                // A handle near the lower rear quarter of each reference weapon.
                Vector3 grip=gunBounds.center+new Vector3(barrelX?-gunBounds.size.x*.19f:0,-gunBounds.size.y*.30f,barrelX?0:-gunBounds.size.z*.19f);
                weapon.localPosition=-grip*gunScale;
                foreach(var collider in weapon.GetComponentsInChildren<Collider>())Destroy(collider);
            }
            previousPosition=transform.position;previousYaw=transform.eulerAngles.y;
            Pose(0);
        }
        static Transform Palm(Transform tip)
        {
            if(!tip)return null;
            while(tip.parent && Vector3.Distance(tip.position,tip.parent.position)<.14f)tip=tip.parent;
            return tip;
        }
        public static Bounds BoundsOf(Transform root)
        {
            var renderers=root.GetComponentsInChildren<Renderer>();
            Bounds b=new Bounds();bool first=true;
            foreach(var renderer in renderers)
            {
                // Skinned bounds are evaluated using the root bone's imported axes.
                // World bounds capture those axes correctly; localBounds alone does not.
                Bounds local=renderer.bounds;
                for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
                {
                    Vector3 corner=local.center+Vector3.Scale(local.extents,new Vector3(x,y,z));
                    Vector3 p=root.InverseTransformPoint(corner);
                    if(first){b=new Bounds(p,Vector3.zero);first=false;}else b.Encapsulate(p);
                }
            }
            return b;
        }
        void LateUpdate()
        {
            UpdatePose(Time.deltaTime);
        }
        public void UpdatePose(float deltaTime)
        {
            float dt=Mathf.Max(.0001f,deltaTime);
            speed=Mathf.Lerp(speed,Mathf.Clamp01(Vector3.Distance(transform.position,previousPosition)/(dt*5)),1-Mathf.Exp(-dt*12));
            previousPosition=transform.position;gait+=dt*9*speed;
            float yaw=transform.eulerAngles.y;turnLean=Mathf.Lerp(turnLean,Mathf.Clamp(Mathf.DeltaAngle(previousYaw,yaw)/dt*.04f,-18,18),1-Mathf.Exp(-dt*10));previousYaw=yaw;
            Pose(Mathf.Sin(gait)*speed);
            hurtRemaining=Mathf.Max(0,hurtRemaining-dt);
            float flinch=Mathf.Sin(Mathf.Clamp01(hurtRemaining/.42f)*Mathf.PI);
            if(visual){visual.localRotation=Quaternion.Euler(-28*flinch+speed*3,0,12*flinch-turnLean*.3f);visual.localPosition=visualRestPosition+new Vector3(0,(.06f*Mathf.Abs(Mathf.Sin(gait))*speed+.10f*flinch),-.22f*flinch);}
        }
        void Pose(float walk)
        {
            for(int i=0;i<bones.Count;i++)bones[i].localRotation=restRotations[i];
            if(chest)chest.localRotation*=Quaternion.Euler(0,turnLean+walk*7,-walk*3);
            if(rightHip)rightHip.rotation=Quaternion.AngleAxis(walk*22,transform.right)*rightHip.rotation;
            if(leftHip)leftHip.rotation=Quaternion.AngleAxis(-walk*22,transform.right)*leftHip.rotation;
            AimArm(rightHand,new Vector3(.36f,1.02f,.48f),Vector3.right);
            AimArm(leftHand,new Vector3(.08f,1.02f,.66f),Vector3.left);
            if(weaponSocket)weaponSocket.rotation=transform.rotation*weaponRotation;
        }
        void AimArm(Transform hand,Vector3 targetLocal,Vector3 poleLocal)
        {
            if(!hand || !hand.parent || !hand.parent.parent)return;
            Transform elbow=hand.parent,shoulder=elbow.parent;
            Vector3 start=shoulder.position,target=transform.TransformPoint(targetLocal);
            float upper=Vector3.Distance(start,elbow.position),lower=Vector3.Distance(elbow.position,hand.position);
            if(upper<.02f || lower<.02f)return;
            Vector3 direction=target-start;float distance=Mathf.Clamp(direction.magnitude,Mathf.Abs(upper-lower)+.002f,upper+lower-.002f);
            direction.Normalize();target=start+direction*distance;
            Vector3 pole=Vector3.ProjectOnPlane(transform.TransformDirection(poleLocal-Vector3.up),direction).normalized;
            float along=(upper*upper-lower*lower+distance*distance)/(2*distance);
            float height=Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
            Vector3 elbowTarget=start+direction*along+pole*height;
            shoulder.rotation=Quaternion.FromToRotation(elbow.position-start,elbowTarget-start)*shoulder.rotation;
            elbow.rotation=Quaternion.FromToRotation(hand.position-elbow.position,target-elbow.position)*elbow.rotation;
        }
    }
}
