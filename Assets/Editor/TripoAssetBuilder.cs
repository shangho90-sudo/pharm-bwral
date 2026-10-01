using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using PharmaBrawl;

public static class TripoAssetBuilder
{
    [Serializable] class Result { public string model_file, source_task_id; }
    public static void BuildAvailableWindows(){ImportAvailable();PrototypeBuilder.Build();}
    public static void ImportAvailable()
    {
        string workspace=Path.GetFullPath("../..");
        for(int i=0;i<10;i++)
        {
            Import(workspace,"pharmacist", "Characters",i,true);
            Import(workspace,"medical-weapon", "Weapons",i,false);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("TRIPO_IMPORT_DONE");
    }
    static void Import(string workspace,string prefix,string category,int i,bool humanoid)
    {
        string resultPath=Path.Combine(workspace,"work",$"{prefix}-{i:00}.result.json");
        if(!File.Exists(resultPath) || new FileInfo(resultPath).Length==0)return;
        var result=JsonUtility.FromJson<Result>(File.ReadAllText(resultPath));
        if(result==null || string.IsNullOrEmpty(result.model_file))return;
        string source=Path.Combine(workspace,result.model_file);
        string folder=$"Assets/Art/{category}/{i:00}";
        Directory.CreateDirectory(folder);
        string path=folder+"/model.fbx";
        if(!File.Exists(path))File.Copy(source,path);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(ModelImporter)AssetImporter.GetAtPath(path);
        importer.animationType=humanoid?ModelImporterAnimationType.Generic:ModelImporterAnimationType.None;
        importer.importAnimation=false;
        importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
        importer.isReadable=true;
        importer.SaveAndReimport();
        string textureFolder=folder+"/Textures";
        Directory.CreateDirectory(textureFolder);
        importer.ExtractTextures(textureFolder);
        AssetDatabase.Refresh();
        importer.SaveAndReimport();
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(model);
        var renderers=instance.GetComponentsInChildren<Renderer>();
        Bounds bounds=new Bounds();bool first=true;
        foreach(var renderer in renderers){if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds);}
        var animator=instance.GetComponent<Animator>();
        Debug.Log($"TRIPO_ASSET {category}/{i:00} bounds={bounds} renderers={renderers.Length} humanoid={(animator && animator.isHuman)} avatar={(animator && animator.avatar && animator.avatar.isValid)}");
        if(humanoid)foreach(var skin in instance.GetComponentsInChildren<SkinnedMeshRenderer>())foreach(var bone in skin.bones)if(bone)Debug.Log($"TRIPO_SKINBONE {i:00} {bone.name} parent={bone.parent.name} position={bone.position}");
        if(humanoid && animator && animator.isHuman)
        {
            foreach(var bone in new[]{HumanBodyBones.Hips,HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,HumanBodyBones.LeftHand,HumanBodyBones.Head})
            {var t=animator.GetBoneTransform(bone);if(t)Debug.Log($"TRIPO_BONE {i:00} {bone} {t.name} pos={t.position}");}
        }
        foreach(var renderer in renderers)foreach(var mat in renderer.sharedMaterials)if(mat)Debug.Log($"TRIPO_MATERIAL {i:00} {mat.name} shader={mat.shader.name} texture={(mat.mainTexture?mat.mainTexture.name:"none")}");
        UnityEngine.Object.DestroyImmediate(instance);
        if(humanoid)model=CanonicalPharmacistRig.Create(model,folder);
        string definitionPath=$"Assets/Resources/Characters/{i:00}_{(AttackKind)i}.asset";
        var definition=AssetDatabase.LoadAssetAtPath<CharacterDefinition>(definitionPath);
        if(definition){if(humanoid)definition.characterPrefab=model;else definition.weaponPrefab=model;EditorUtility.SetDirty(definition);}
    }
}
