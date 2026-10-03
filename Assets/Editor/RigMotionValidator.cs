using System;
using PharmaBrawl;
using UnityEditor;
using UnityEngine;
public static class RigMotionValidator
{
    public static void Validate(){
        var data=AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/Resources/Characters/00_Capsule.asset");
        var actor=new GameObject("Paeng locomotion validation");var rig=actor.AddComponent<PharmacistModelRig>();rig.Initialize(data);
        if(!rig.HasLocomotionAnimator)throw new Exception("Paeng must have an enabled Animator with real Tripo clips");
        var animator=actor.GetComponentInChildren<Animator>();
        foreach(var clip in animator.runtimeAnimatorController.animationClips){
            int boundLegCurves=0;
            foreach(var binding in AnimationUtility.GetCurveBindings(clip))if((binding.path.Contains("Leg")||binding.path.Contains("Foot"))&&animator.transform.Find(binding.path))boundLegCurves++;
            if(boundLegCurves<8)throw new Exception("Locomotion clip does not bind to actual leg bones: "+clip.name);
            Debug.Log("PAENG_LEG_BINDINGS_PASS "+clip.name+" curves="+boundLegCurves);
        }
        var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=new Mesh();skin.BakeMesh(mesh);var rest=mesh.vertices;
        Debug.Log("PAENG_BOUNDS world="+skin.bounds+" baked="+mesh.bounds+" scale="+skin.transform.lossyScale);
        float maximum=0;for(int frame=0;frame<90;frame++){
            actor.transform.position+=Vector3.forward*(5f/60);rig.UpdatePose(1f/60);skin.BakeMesh(mesh);
            var posed=mesh.vertices;float sum=0;for(int i=0;i<rest.Length;i++)sum+=(posed[i]-rest[i]).sqrMagnitude;
            maximum=Mathf.Max(maximum,Mathf.Sqrt(sum/rest.Length));
        }
        if(maximum<.015f)throw new Exception("Paeng mesh does not deform while walking: "+maximum);
        if(animator.GetFloat("Speed")<.9f)throw new Exception("Run blend not reached");
        for(int frame=0;frame<90;frame++)rig.UpdatePose(1f/60);
        if(animator.GetFloat("Speed")>.01f)throw new Exception("Idle blend not restored after stopping");
        Debug.Log("PAENG_WALK_DEFORMATION_PASS maxRms="+maximum+" vertices="+rest.Length+" frames=90");
        UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(actor);
    }
    public static void Inspect(){
        var data=AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/Resources/Characters/00_Capsule.asset");
        var actor=new GameObject("Paeng rig inspection");var rig=actor.AddComponent<PharmacistModelRig>();rig.Initialize(data);
        foreach(var skin in actor.GetComponentsInChildren<SkinnedMeshRenderer>())foreach(var bone in skin.bones)if(bone)Debug.Log("RIG_BONE "+bone.name+" pos="+actor.transform.InverseTransformPoint(bone.position));
        UnityEngine.Object.DestroyImmediate(actor);
    }
}
