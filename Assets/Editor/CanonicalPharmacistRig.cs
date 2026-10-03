using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Preserve Tripo's textured mesh while repairing incomplete or mislabeled vendor
// joints into a consistent Unity Humanoid skeleton. Original FBX remains intact.
public static class CanonicalPharmacistRig
{
    public static GameObject Create(GameObject source,string folder)
    {
        var original=(GameObject)PrefabUtility.InstantiatePrefab(source);
        var skin=original.GetComponentInChildren<SkinnedMeshRenderer>();
        if(!skin){UnityEngine.Object.DestroyImmediate(original);throw new Exception("Missing skinned Tripo mesh: "+folder);}
        var mesh=new Mesh();skin.BakeMesh(mesh);
        Vector3[] vertices=mesh.vertices,normals=mesh.normals;
        Bounds bounds=new Bounds();
        for(int i=0;i<vertices.Length;i++){vertices[i]=skin.transform.TransformPoint(vertices[i]);if(i==0)bounds=new Bounds(vertices[i],Vector3.zero);else bounds.Encapsulate(vertices[i]);normals[i]=skin.transform.TransformDirection(normals[i]).normalized;}
        float h=bounds.size.y;Vector3 origin=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
        for(int i=0;i<vertices.Length;i++)vertices[i]-=origin;
        mesh.vertices=vertices;
        // Average normals across duplicate UV-seam vertices for soft cartoon faces.
        var sums=new Dictionary<Vector3Int,Vector3>();
        for(int i=0;i<vertices.Length;i++){var key=Vector3Int.RoundToInt(vertices[i]*10000);if(sums.ContainsKey(key))sums[key]+=normals[i];else sums[key]=normals[i];}
        for(int i=0;i<vertices.Length;i++)normals[i]=sums[Vector3Int.RoundToInt(vertices[i]*10000)].normalized;
        mesh.normals=normals;mesh.RecalculateTangents();
        var root=new GameObject("Pharmacist / Unity Humanoid");
        var jointNames=new List<string>();var joints=new List<Transform>();var descriptions=new List<HumanBone>();
        Transform Joint(string name,Transform parent,Vector3 point)
        {
            var t=new GameObject(name).transform;t.SetParent(parent,false);t.position=point*h;
            joints.Add(t);jointNames.Add(name);
            descriptions.Add(new HumanBone{boneName=name,humanName=name,limit=new HumanLimit{useDefaultValues=true}});return t;
        }
        var hips=Joint("Hips",root.transform,new Vector3(0,.22f,0));
        var spine=Joint("Spine",hips,new Vector3(0,.30f,0));
        var chest=Joint("Chest",spine,new Vector3(0,.38f,0));
        var neck=Joint("Neck",chest,new Vector3(0,.46f,0));Joint("Head",neck,new Vector3(0,.60f,0));
        for(int side=-1;side<=1;side+=2)
        {
            string s=side<0?"Left":"Right";
            var shoulder=Joint(s+"Shoulder",chest,new Vector3(side*.08f,.40f,0));
            var upper=Joint(s+"UpperArm",shoulder,new Vector3(side*.135f,.40f,0));
            var lower=Joint(s+"LowerArm",upper,new Vector3(side*.235f,.40f,0));
            Joint(s+"Hand",lower,new Vector3(side*.335f,.40f,0));
            var thigh=Joint(s+"UpperLeg",hips,new Vector3(side*.065f,.215f,0));
            var knee=Joint(s+"LowerLeg",thigh,new Vector3(side*.065f,.12f,0));
            Joint(s+"Foot",knee,new Vector3(side*.065f,.045f,.02f));
        }
        var weights=new BoneWeight[vertices.Length];
        for(int i=0;i<vertices.Length;i++)
        {
            Vector3 p=vertices[i]/h;string side=p.x<0?"Left":"Right";
            string name;
            if(p.y>.47f)name="Head";
            else if(Mathf.Abs(p.x)>.17f && p.y>.29f)name=side+(Mathf.Abs(p.x)>.295f?"Hand":Mathf.Abs(p.x)>.21f?"LowerArm":"UpperArm");
            else if(p.y<.22f)name=side+(p.y<.075f?"Foot":p.y<.145f?"LowerLeg":"UpperLeg");
            else name=p.y>.37f?"Chest":p.y>.29f?"Spine":"Hips";
            int primary=jointNames.IndexOf(name);int secondary=primary;
            float blend=0;
            if(name.EndsWith("LowerArm")){secondary=jointNames.IndexOf(side+"UpperArm");blend=Mathf.Clamp01((.24f-Mathf.Abs(p.x))/.05f)*.4f;}
            else if(name.EndsWith("UpperArm")){secondary=jointNames.IndexOf("Chest");blend=Mathf.Clamp01((.18f-Mathf.Abs(p.x))/.04f)*.4f;}
            else if(name.EndsWith("LowerLeg")){secondary=jointNames.IndexOf(side+"UpperLeg");blend=Mathf.Clamp01((p.y-.11f)/.035f)*.4f;}
            weights[i]=new BoneWeight{boneIndex0=primary,weight0=1-blend,boneIndex1=secondary,weight1=blend};
        }
        var bindposes=new Matrix4x4[joints.Count];for(int i=0;i<joints.Count;i++)bindposes[i]=joints[i].worldToLocalMatrix*root.transform.localToWorldMatrix;
        mesh.boneWeights=weights;mesh.bindposes=bindposes;mesh.RecalculateBounds();
        string meshPath=folder+"/UnityRigMesh.asset";var savedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if(savedMesh){EditorUtility.CopySerialized(mesh,savedMesh);UnityEngine.Object.DestroyImmediate(mesh);mesh=savedMesh;}else AssetDatabase.CreateAsset(mesh,meshPath);
        var renderer=new GameObject("Textured pharmacist mesh").AddComponent<SkinnedMeshRenderer>();renderer.transform.SetParent(root.transform,false);renderer.sharedMesh=mesh;renderer.bones=joints.ToArray();renderer.rootBone=hips;renderer.localBounds=mesh.bounds;renderer.updateWhenOffscreen=true;
        var mats=new Material[skin.sharedMaterials.Length];
        for(int i=0;i<mats.Length;i++)
        {
            string path=folder+$"/CartoonMaterial-{i}.mat";mats[i]=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!mats[i]){mats[i]=new Material(skin.sharedMaterials[i]);AssetDatabase.CreateAsset(mats[i],path);}
            else mats[i].CopyPropertiesFromMaterial(skin.sharedMaterials[i]);
            mats[i].SetFloat("_Metallic",0);mats[i].SetFloat("_Glossiness",.22f);mats[i].DisableKeyword("_METALLICGLOSSMAP");mats[i].DisableKeyword("_NORMALMAP");mats[i].SetTexture("_BumpMap",null);mats[i].SetTexture("_MetallicGlossMap",null);EditorUtility.SetDirty(mats[i]);
        }
        renderer.sharedMaterials=mats;
        var skeleton=new List<SkeletonBone>();
        foreach(var t in root.GetComponentsInChildren<Transform>())skeleton.Add(new SkeletonBone{name=t.name,position=t.localPosition,rotation=t.localRotation,scale=t.localScale});
        var description=new HumanDescription{human=descriptions.ToArray(),skeleton=skeleton.ToArray(),armStretch=.05f,legStretch=.05f,upperArmTwist=.5f,lowerArmTwist=.5f,upperLegTwist=.5f,lowerLegTwist=.5f};
        var avatar=AvatarBuilder.BuildHumanAvatar(root,description);avatar.name="Pharmacist Humanoid";
        if(!avatar.isValid || !avatar.isHuman)throw new Exception("Canonical avatar invalid: "+folder);
        string avatarPath=folder+"/UnityHumanoid.asset";var savedAvatar=AssetDatabase.LoadAssetAtPath<Avatar>(avatarPath);
        if(savedAvatar){EditorUtility.CopySerialized(avatar,savedAvatar);UnityEngine.Object.DestroyImmediate(avatar);avatar=savedAvatar;}else AssetDatabase.CreateAsset(avatar,avatarPath);
        var animator=root.AddComponent<Animator>();animator.avatar=avatar;animator.applyRootMotion=false;
        var prefab=PrefabUtility.SaveAsPrefabAsset(root,folder+"/Pharmacist.prefab");
        Debug.Log($"TRIPO_UNITY_RIG {folder} humanoid={avatar.isHuman} vertices={mesh.vertexCount} triangles={mesh.triangles.Length/3}");
        UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(original);return prefab;
    }
}
