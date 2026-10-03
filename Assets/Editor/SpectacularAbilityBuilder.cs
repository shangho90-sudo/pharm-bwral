using System;
using System.IO;
using System.Collections.Generic;
using PharmaBrawl;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class SpectacularAbilityBuilder
{
    static void Require(bool ok,string message){if(!ok)throw new Exception("Spectacular ability: "+message);}
    public static void Import()
    {
        AssetDatabase.Refresh();Directory.CreateDirectory("Assets/Resources/Effects/Spectacular");
        string ribbonPath="Assets/Resources/VFX/AbilityRibbon.mat";var ribbon=AssetDatabase.LoadAssetAtPath<Material>(ribbonPath);if(!ribbon){ribbon=new Material(Shader.Find("Pharma/AbilityRibbon"));AssetDatabase.CreateAsset(ribbon,ribbonPath);}ribbon.shader=Shader.Find("Pharma/AbilityRibbon");
        string matPath="Assets/Resources/VFX/AbilityCrystal.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(!material){material=new Material(Shader.Find("Pharma/AbilityCrystal"));AssetDatabase.CreateAsset(material,matPath);}material.shader=Shader.Find("Pharma/AbilityCrystal");
        string[] names={"CrossBurst","BoltCrystal","BloomCore","CapsuleGem"};
        for(int i=0;i<4;i++){
            string path=i<3?"Assets/Art/Effects/Spectacular/"+names[i]+".fbx":"Assets/Resources/Projectiles/Capsule.prefab";
            if(i<3){var importer=(ModelImporter)AssetImporter.GetAtPath(path);Require(importer!=null,"model "+path);importer.importAnimation=false;importer.importCameras=false;importer.importLights=false;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.SaveAndReimport();}
            var model=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));var combines=new List<CombineInstance>();foreach(var filter in model.GetComponentsInChildren<MeshFilter>())if(filter.sharedMesh){for(int sub=0;sub<filter.sharedMesh.subMeshCount;sub++)combines.Add(new CombineInstance{mesh=filter.sharedMesh,subMeshIndex=sub,transform=model.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix});}
            Require(combines.Count>0,"mesh import "+names[i]);var mesh=new Mesh{indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.CombineMeshes(combines.ToArray(),true,true);mesh.RecalculateBounds();Vector3 center=mesh.bounds.center;float length=Mathf.Max(mesh.bounds.size.x,Mathf.Max(mesh.bounds.size.y,mesh.bounds.size.z));var vertices=mesh.vertices;for(int j=0;j<vertices.Length;j++)vertices[j]=(vertices[j]-center)/Mathf.Max(.001f,length);mesh.vertices=vertices;mesh.RecalculateBounds();mesh.name=names[i];string meshPath="Assets/Resources/Effects/Spectacular/"+names[i]+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(existing){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}else AssetDatabase.CreateAsset(mesh,meshPath);
            var root=new GameObject(names[i]);root.AddComponent<MeshFilter>().sharedMesh=mesh;root.AddComponent<MeshRenderer>().sharedMaterial=material;PrefabUtility.SaveAsPrefabAsset(root,"Assets/Resources/Effects/Spectacular/"+names[i]+".prefab");UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(model);
        }
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
    }
    static CharacterDefinition[] Roster(){var r=Resources.LoadAll<CharacterDefinition>("Characters");Array.Sort(r,(a,b)=>a.kind.CompareTo(b.kind));return r;}
    static ArenaSimulation Setup(CharacterDefinition[] r,int hero){var s=new ArenaSimulation(r,hero,57,0,1,new[]{hero,(hero+1)%10});s.covers.Clear();s.fighters[0].position=Vector2.zero;s.fighters[0].aim=Vector2.right;s.fighters[0].aimDistance=3;s.fighters[1].position=new Vector2(3,0);return s;}
    public static void Validate()
    {
        var r=Roster();for(int hero=0;hero<10;hero++)for(int ability=0;ability<2;ability++){
            var s=Setup(r,hero);var baseline=Setup(r,hero);var root=new GameObject("20 ability presentation QA");var view=root.AddComponent<SpectacularAbilityView>();view.Initialize(s);Require(view.HasTripoMeshes,"four imported Tripo meshes");s.Event+=view.Emit;
            if(ability==0){s.Skill(s.fighters[0]);baseline.Skill(baseline.fighters[0]);}else{s.fighters[0].charge=s.fighters[0].data.ultimateRequirement;baseline.fighters[0].charge=baseline.fighters[0].data.ultimateRequirement;s.Ultimate(s.fighters[0]);baseline.Ultimate(baseline.fighters[0]);}
            Require(view.ActiveEffects==1,"effect for "+hero+"/"+ability);view.Advance(.2f);int activeMeshes=0;foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>()){if(!renderer.gameObject.activeInHierarchy)continue;activeMeshes++;Vector3 p=renderer.transform.position;Require(!float.IsNaN(p.x)&&!float.IsNaN(p.y)&&!float.IsNaN(p.z),"finite transforms");}Require(activeMeshes>=(ability==0?1:8),"layered models");Require(s.fighters[0].position==baseline.fighters[0].position&&s.fighters[1].hp==baseline.fighters[1].hp&&s.skillsUsed==baseline.skillsUsed&&s.ultimatesUsed==baseline.ultimatesUsed,"presentation never changes combat");view.Advance(10);for(int reuse=0;reuse<25;reuse++)view.Emit(new ArenaSimulation.CombatEvent{type=reuse<24?"ultimate":"skill",actor=0,position=Vector2.zero});int tails=0;foreach(var line in root.GetComponentsInChildren<LineRenderer>())if(line.name.StartsWith("White hot energy tail")&&line.enabled)tails++;Require(tails==24*8-(8-(hero==2||hero==5?1:3)),"pooled tails match current ability");view.Advance(10);Require(view.ActiveEffects==0,"expiration");view.Emit(new ArenaSimulation.CombatEvent{type=ability==0?"skill":"ultimate",actor=0,position=Vector2.zero});view.SetVisible(false);Require(view.ActiveEffects==0,"pause cleanup");view.SetVisible(true);view.Emit(new ArenaSimulation.CombatEvent{type="ultimate",actor=0,position=Vector2.zero});view.Emit(new ArenaSimulation.CombatEvent{type="withdraw",actor=0,position=Vector2.zero});Require(view.ActiveEffects==0,"leave cleanup");UnityEngine.Object.DestroyImmediate(root);
        }
        Debug.Log("SPECTACULAR_20_ABILITIES_PASS all ten skills and ultimates, imported meshes, layered transforms, no combat mutation, expiry/pause/withdraw cleanup");
    }
    public static void Capture()
    {
        var r=Roster();string dir=Path.GetFullPath("../../outputs/spectacular-abilities");Directory.CreateDirectory(dir);
        for(int hero=0;hero<10;hero++)for(int ability=0;ability<2;ability++){
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);var s=Setup(r,hero);var p=s.fighters[0];p.position=new Vector2(0,-2);p.aim=Vector2.up;p.aimDistance=4;s.fighters[1].position=new Vector2(0,5);
            var camera=new GameObject("Ability preview camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.orthographic=true;camera.orthographicSize=7;camera.aspect=16f/9;camera.transform.position=new Vector3(0,17,-20);camera.transform.rotation=Quaternion.Euler(40,0,0);camera.backgroundColor=new Color(.035f,.07f,.16f);
            var light=new GameObject("Ability key light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;light.transform.rotation=Quaternion.Euler(45,-30,0);RenderSettings.ambientLight=Color.white*.75f;
            var actor=new GameObject(p.data.displayName);actor.transform.position=new Vector3(0,0,-2);actor.transform.rotation=Quaternion.identity;actor.AddComponent<PharmacistModelRig>().Initialize(p.data);
            var root=new GameObject("Rendered skill layers");var view=root.AddComponent<SpectacularAbilityView>();view.Initialize(s);s.Event+=view.Emit;
            if(ability==0)s.Skill(p);else{p.charge=p.data.ultimateRequirement;s.Ultimate(p);}view.Advance(ability==0?.45f:.65f);foreach(var ps in root.GetComponentsInChildren<ParticleSystem>())if(ps.gameObject.activeInHierarchy)ps.Simulate(.15f,false,false);
            var rt=new RenderTexture(960,540,24);camera.targetTexture=rt;camera.Render();var previous=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(960,540,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,960,540),0,0);image.Apply();File.WriteAllBytes(Path.Combine(dir,hero.ToString("00")+(ability==0?"-skill":"-ultimate")+".png"),image.EncodeToPNG());RenderTexture.active=previous;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);
        }
        EditorSceneManager.OpenScene("Assets/Scenes/Pharmacy.unity");Debug.Log("SPECTACULAR_20_CAPTURE_PASS");
    }
    public static void Prepare(){Import();Validate();Capture();}
    public static void Build(){Prepare();ReferenceAbilityBuilder.Validate();PaengCombatValidator.Validate();ArenaMapBuilder.BuildWeb();}
}
