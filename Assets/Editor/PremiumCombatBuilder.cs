using System;
using System.IO;
using PharmaBrawl;
using UnityEditor;
using UnityEngine;
public static class PremiumCombatBuilder
{
    static void Require(bool ok,string message){if(!ok)throw new Exception("Premium combat validation: "+message);}
    public static void BuildWeb(){Import();Validate();RigMotionValidator.Validate();NetworkMatchValidator.BuildWeb();}
    public static void Import(){
        AssetDatabase.Refresh();var image=(TextureImporter)AssetImporter.GetAtPath("Assets/Resources/RoomLobbyScreen.png");image.textureType=TextureImporterType.Default;image.maxTextureSize=2048;image.npotScale=TextureImporterNPOTScale.None;image.alphaSource=TextureImporterAlphaSource.None;image.mipmapEnabled=false;image.textureCompression=TextureImporterCompression.Uncompressed;image.SaveAndReimport();
        string folder="Assets/Art/Effects/MedicalBurst",path=folder+"/model.fbx";var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.materialLocation=ModelImporterMaterialLocation.External;importer.SaveAndReimport();Directory.CreateDirectory(folder+"/Textures");importer.ExtractTextures(folder+"/Textures");AssetDatabase.Refresh();
        foreach(string file in Directory.GetFiles(folder+"/Textures")){var ti=AssetImporter.GetAtPath(file.Replace('\\','/')) as TextureImporter;if(!ti)continue;ti.maxTextureSize=256;ti.SaveAndReimport();}
        importer.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName,ModelImporterMaterialSearch.Local);importer.SaveAndReimport();var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);var instance=UnityEngine.Object.Instantiate(model);
        foreach(var renderer in instance.GetComponentsInChildren<Renderer>()){foreach(var material in renderer.sharedMaterials)if(material){material.shader=Shader.Find("Standard");material.SetFloat("_Metallic",0);material.SetTexture("_BumpMap",null);material.SetTexture("_MetallicGlossMap",null);material.SetTexture("_OcclusionMap",null);material.DisableKeyword("_NORMALMAP");material.DisableKeyword("_METALLICGLOSSMAP");material.EnableKeyword("_EMISSION");material.SetColor("_EmissionColor",new Color(.3f,.5f,.5f));material.SetTexture("_EmissionMap",material.mainTexture);EditorUtility.SetDirty(material);}renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
        foreach(var collider in instance.GetComponentsInChildren<Collider>())UnityEngine.Object.DestroyImmediate(collider);Directory.CreateDirectory("Assets/Resources/Effects");PrefabUtility.SaveAsPrefabAsset(instance,"Assets/Resources/Effects/MedicalBurst.prefab");UnityEngine.Object.DestroyImmediate(instance);
        foreach(string key in Enum.GetNames(typeof(AttackKind)))foreach(string suffix in new[]{"","Skill","Ultimate"}){var audio=(AudioImporter)AssetImporter.GetAtPath("Assets/Resources/SFX/"+key+suffix+".mp3");var settings=audio.defaultSampleSettings;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.8f;audio.defaultSampleSettings=settings;audio.SaveAndReimport();}
        // Canonical rigs already disable these maps. Clearing unused references
        // keeps their source textures in the repo without shipping them to phones.
        foreach(string guid in AssetDatabase.FindAssets("t:Material",new[]{"Assets/Art/Characters"})){string materialPath=AssetDatabase.GUIDToAssetPath(guid);if(!materialPath.Contains("CartoonMaterial-"))continue;var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);material.SetTexture("_BumpMap",null);material.DisableKeyword("_NORMALMAP");material.SetTexture("_MetallicGlossMap",null);material.DisableKeyword("_METALLICGLOSSMAP");EditorUtility.SetDirty(material);}
        AssetDatabase.SaveAssets();
    }
    public static void Validate(){
        var roster=Resources.LoadAll<CharacterDefinition>("Characters");Array.Sort(roster,(a,b)=>a.kind.CompareTo(b.kind));Require(roster.Length==10,"roster");
        for(int hero=0;hero<10;hero++){
            foreach(string suffix in new[]{"","Skill","Ultimate"}){var clip=Resources.Load<AudioClip>("SFX/"+roster[hero].kind+suffix);Require(clip&&clip.length>.2f&&clip.length<2.5f,"cue "+hero+suffix);}
            for(int ability=0;ability<3;ability++){
                var sim=new ArenaSimulation(roster,hero,33,0,1,new[]{hero,(hero+1)%10});sim.covers.Clear();var p=sim.fighters[0];var target=sim.fighters[1];p.position=Vector2.zero;p.aim=Vector2.right;p.aimDistance=2;target.position=Vector2.right*2;
                int events=0;var effectRoot=new GameObject("Ability presentation validation");var effect=effectRoot.AddComponent<AbilityEffects>();effect.Initialize(sim);sim.Event+=e=>{if(e.actor==0)events++;effect.Emit(e);};
                if(ability==0)sim.Attack(p);else if(ability==1)sim.Skill(p);else{p.charge=p.data.ultimateRequirement;sim.Ultimate(p);}
                var inputs=new ArenaSimulation.HumanInput[2];for(int i=0;i<2;i++)inputs[i].human=true;
                if(ability==1&&(hero==2||hero==4||hero==8))sim.Attack(p);
                // Wave skill is a defensive shield, not a projectile.
                if(ability==1&&hero==3){float before=p.hp;sim.Damage(1,0,1000);Require(before-p.hp<500,"shield reduction");}
                for(int tick=0;tick<100;tick++)sim.TickNetwork(1/60f,inputs);
                if(!(ability==1&&(hero==3||hero==6)))Require(p.damageDealt>0,"damage hero="+hero+" ability="+ability);
                if(ability==1&&hero==6)Require(p.position.x>3,"blink displacement");Require(events>0,"presentation events "+hero+"/"+ability);
                foreach(var line in effectRoot.GetComponentsInChildren<LineRenderer>()){for(int i=0;i<line.positionCount;i++){var point=line.GetPosition(i);Require(!float.IsNaN(point.x)&&!float.IsNaN(point.y)&&!float.IsNaN(point.z),"finite effect vertices "+hero+"/"+ability);}}
                UnityEngine.Object.DestroyImmediate(effectRoot);Debug.Log("ISOLATED_ABILITY_PASS hero="+hero+" ability="+ability+" damage="+p.damageDealt+" events="+events);
            }
            var actor=new GameObject("Rig validation "+hero);var rig=actor.AddComponent<PharmacistModelRig>();rig.Initialize(roster[hero]);var skin=actor.GetComponentInChildren<SkinnedMeshRenderer>();Require(skin,"skinned character "+hero);var mesh=new Mesh();skin.BakeMesh(mesh);Vector3 beforeVertex=skin.transform.TransformPoint(mesh.vertices[0]);actor.transform.rotation=Quaternion.Euler(0,90,0);skin.BakeMesh(mesh);Vector3 afterVertex=skin.transform.TransformPoint(mesh.vertices[0]);Require(Vector3.Distance(afterVertex,Quaternion.Euler(0,90,0)*beforeVertex)<.01f,"body follows aim rotation "+hero);
            var visual=actor.transform.GetChild(0);actor.transform.position+=Vector3.right*.1f;rig.ReactToHit();rig.UpdatePose(.1f);Require(Quaternion.Angle(visual.localRotation,Quaternion.identity)>12,"large flinch "+hero);UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(actor);
            Debug.Log("BODY_ROTATION_FLINCH_PASS hero="+hero);
        }
        Require(roster[5].attackInterval<=.7f,"artillery cadence");Require(Resources.Load<GameObject>("Effects/MedicalBurst"),"Tripo ability asset");Require(Resources.Load<Texture2D>("RoomLobbyScreen"),"premium room lobby");Debug.Log("PREMIUM_COMBAT_PASS 30 isolated abilities, 30 audio cues, 10 rotating rigs");
    }
}

