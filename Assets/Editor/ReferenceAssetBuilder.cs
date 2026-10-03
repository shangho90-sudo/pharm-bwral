using System.IO;
using UnityEditor;
using UnityEngine;
using PharmaBrawl;
public static class ReferenceAssetBuilder
{
    public static void Import()
    {
        ImportModel("Assets/Art/Characters/Reference01",true);
        ImportModel("Assets/Art/Projectiles/Capsule",false);
        AssetDatabase.SaveAssets();
    }
    static void ImportModel(string folder,bool character)
    {
        string path=folder+"/model.fbx";if(!File.Exists(path))return;
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.animationType=character?ModelImporterAnimationType.Generic:ModelImporterAnimationType.None;
        importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.materialLocation=ModelImporterMaterialLocation.External;importer.isReadable=true;importer.SaveAndReimport();
        Directory.CreateDirectory(folder+"/Textures");importer.ExtractTextures(folder+"/Textures");AssetDatabase.Refresh();
        foreach(string file in Directory.GetFiles(folder+"/Textures")){var ti=AssetImporter.GetAtPath(file.Replace('\\','/')) as TextureImporter;if(!ti)continue;bool normal=file.ToLowerInvariant().Contains("normal");ti.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;ti.sRGBTexture=!normal;ti.maxTextureSize=1024;ti.SaveAndReimport();}
        importer.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName,ModelImporterMaterialSearch.Local);importer.SaveAndReimport();
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        foreach(var renderer in model.GetComponentsInChildren<Renderer>())foreach(var mat in renderer.sharedMaterials)if(mat){mat.shader=Shader.Find("Standard");mat.color=Color.white;mat.SetFloat("_Glossiness",character?.25f:.8f);foreach(string file in Directory.GetFiles(folder+"/Textures"))if(file.ToLowerInvariant().Contains("basecolor") && !file.EndsWith(".meta"))mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(file.Replace('\\','/'));if(!mat.mainTexture)throw new System.Exception("Missing reference texture "+folder);EditorUtility.SetDirty(mat);}
        if(character){var definition=AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/Resources/Characters/00_Capsule.asset");definition.characterPrefab=CanonicalPharmacistRig.Create(model,folder);EditorUtility.SetDirty(definition);}
        else{Directory.CreateDirectory("Assets/Resources/Projectiles");var instance=Object.Instantiate(model);PrefabUtility.SaveAsPrefabAsset(instance,"Assets/Resources/Projectiles/Capsule.prefab");Object.DestroyImmediate(instance);}
    }
}
