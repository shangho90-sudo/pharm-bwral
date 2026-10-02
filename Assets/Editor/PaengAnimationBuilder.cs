using System;
using System.IO;
using System.Linq;
using PharmaBrawl;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class PaengAnimationBuilder
{
    const string Folder="Assets/Art/Characters/PaengAnimated";
    public static void BuildWeb(){Import();PaengCombatValidator.Validate();PremiumCombatBuilder.BuildWeb();}
    public static void InspectImport(){
        var raw=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/Pharmacist.prefab"));
        foreach(var t in raw.GetComponentsInChildren<Transform>())Debug.Log("IMPORT_TRANSFORM "+t.name+" local="+t.localPosition+" scale="+t.localScale+" world="+t.position+" lossy="+t.lossyScale);
        var skin=raw.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=new Mesh();skin.BakeMesh(mesh);Debug.Log("IMPORT_MESH shared="+skin.sharedMesh.bounds+" baked="+mesh.bounds+" world="+skin.bounds);
        var animator=raw.GetComponent<Animator>();animator.Rebind();animator.Update(0);skin.BakeMesh(mesh);Debug.Log("IMPORT_ANIMATED baked="+mesh.bounds+" world="+skin.bounds+" lossy="+skin.transform.lossyScale);
        UnityEngine.Object.DestroyImmediate(mesh);UnityEngine.Object.DestroyImmediate(raw);
    }
    public static void Capture()
    {
        Import();
        UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
        var light=new GameObject("Preview light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.3f;light.transform.rotation=Quaternion.Euler(45,-30,0);RenderSettings.ambientLight=Color.white*.7f;
        var camera=new GameObject("Preview camera").AddComponent<Camera>();camera.backgroundColor=new Color(.06f,.10f,.18f);camera.clearFlags=CameraClearFlags.SolidColor;camera.orthographic=true;camera.orthographicSize=2.3f;camera.transform.position=new Vector3(0,3.8f,8);camera.transform.LookAt(new Vector3(0,1.4f,0));
        var data=AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/Resources/Characters/00_Capsule.asset");
        for(int i=0;i<3;i++){
            var actor=new GameObject(new[]{"Idle","Walk","Run"}[i]);var rig=actor.AddComponent<PharmacistModelRig>();rig.Initialize(data);
            float velocity=new[]{0,2.25f,5f}[i];for(int frame=0;frame<103;frame++){actor.transform.position+=Vector3.forward*(velocity/60);rig.UpdatePose(1f/60);}
            actor.transform.position=new Vector3((i-1)*3,0,0);
        }
        var target=new RenderTexture(1400,600,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;
        var pixels=new Texture2D(1400,600,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1400,600),0,0);pixels.Apply();
        File.WriteAllBytes(Path.GetFullPath("../../outputs/paeng-real-animation-preview.png"),pixels.EncodeToPNG());
        RenderTexture.active=null;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(target);
        Debug.Log("PAENG_ANIMATION_CAPTURE_PASS");
    }
    public static void Import()
    {
        AssetDatabase.Refresh();
        string modelPath=Folder+"/model.fbx";
        Configure(modelPath,false);
        var importer=(ModelImporter)AssetImporter.GetAtPath(modelPath);
        Directory.CreateDirectory(Folder+"/Textures");
        if(!File.Exists(Folder+"/Textures/PaengTexture0.png"))importer.ExtractTextures(Folder+"/Textures");AssetDatabase.Refresh();
        foreach(string file in Directory.GetFiles(Folder+"/Textures")){
            var texture=AssetImporter.GetAtPath(file.Replace('\\','/')) as TextureImporter;
            if(!texture)continue;texture.maxTextureSize=1024;texture.mipmapEnabled=true;texture.SaveAndReimport();
        }
        importer.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName,ModelImporterMaterialSearch.Local);importer.SaveAndReimport();
        var source=AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        var clips=new AnimationClip[3];string[] names={"Idle","Walk","Run"};
        for(int i=0;i<names.Length;i++){
            clips[i]=CreateClip(source,names[i],i);
            var settings=AnimationUtility.GetAnimationClipSettings(clips[i]);settings.loopTime=true;settings.loopBlend=true;AnimationUtility.SetAnimationClipSettings(clips[i],settings);
            string clipPath=Folder+"/"+names[i]+".anim";var saved=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if(saved){EditorUtility.CopySerialized(clips[i],saved);UnityEngine.Object.DestroyImmediate(clips[i]);clips[i]=saved;}else AssetDatabase.CreateAsset(clips[i],clipPath);
            Debug.Log("PAENG_CLIP_IMPORTED "+names[i]+" seconds="+clips[i].length+" curves="+AnimationUtility.GetCurveBindings(clips[i]).Length);
        }
        string controllerPath=Folder+"/Locomotion.controller";
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if(!controller){
            controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);controller.AddParameter("Speed",AnimatorControllerParameterType.Float);
            var state=controller.layers[0].stateMachine.AddState("Locomotion");controller.layers[0].stateMachine.defaultState=state;
            var tree=new BlendTree{name="Idle Walk Run",blendType=BlendTreeType.Simple1D,blendParameter="Speed",useAutomaticThresholds=false};
            AssetDatabase.AddObjectToAsset(tree,controller);state.motion=tree;
        }
        var blend=(BlendTree)controller.layers[0].stateMachine.defaultState.motion;
        blend.children=new[]{new ChildMotion{motion=clips[0],threshold=0,timeScale=1},new ChildMotion{motion=clips[1],threshold=.45f,timeScale=1},new ChildMotion{motion=clips[2],threshold=1,timeScale=1}};
        EditorUtility.SetDirty(controller);EditorUtility.SetDirty(blend);
        var root=(GameObject)PrefabUtility.InstantiatePrefab(source);root.name="Paeng / Tripo animated";root.transform.localRotation=Quaternion.Euler(0,90,0);
        var animator=root.GetComponent<Animator>();if(!animator)animator=root.AddComponent<Animator>();
        animator.avatar=AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Avatar>().FirstOrDefault();
        animator.runtimeAnimatorController=controller;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        foreach(var renderer in root.GetComponentsInChildren<Renderer>())foreach(var mat in renderer.sharedMaterials)if(mat){
            mat.shader=Shader.Find("Standard");mat.SetFloat("_Metallic",0);mat.SetFloat("_Glossiness",.2f);mat.SetTexture("_BumpMap",null);mat.SetTexture("_MetallicGlossMap",null);mat.DisableKeyword("_NORMALMAP");mat.DisableKeyword("_METALLICGLOSSMAP");EditorUtility.SetDirty(mat);
            mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/Textures/PaengTexture0.png");if(!mat.mainTexture)throw new Exception("Missing Paeng base color texture");
        }
        if(!root.GetComponentInChildren<SkinnedMeshRenderer>())throw new Exception("Tripo FBX has no skinned mesh");
        var prefab=PrefabUtility.SaveAsPrefabAsset(root,Folder+"/Pharmacist.prefab");UnityEngine.Object.DestroyImmediate(root);
        var data=AssetDatabase.LoadAssetAtPath<CharacterDefinition>("Assets/Resources/Characters/00_Capsule.asset");data.characterPrefab=prefab;
        var weapon=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Weapons/00/model.fbx"));
        string weaponMaterialPath=Folder+"/MedicalWeapon.mat";var weaponMaterial=AssetDatabase.LoadAssetAtPath<Material>(weaponMaterialPath);
        if(!weaponMaterial){weaponMaterial=new Material(Shader.Find("Standard"));AssetDatabase.CreateAsset(weaponMaterial,weaponMaterialPath);}
        weaponMaterial.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Weapons/00/Textures/tripo_model_basecolor.JPEG");weaponMaterial.SetFloat("_Metallic",0);weaponMaterial.SetFloat("_Glossiness",.3f);EditorUtility.SetDirty(weaponMaterial);
        foreach(var renderer in weapon.GetComponentsInChildren<Renderer>())renderer.sharedMaterial=weaponMaterial;
        data.weaponPrefab=PrefabUtility.SaveAsPrefabAsset(weapon,Folder+"/MedicalWeapon.prefab");UnityEngine.Object.DestroyImmediate(weapon);
        EditorUtility.SetDirty(data);AssetDatabase.SaveAssets();
        RigMotionValidator.Validate();
    }
    static AnimationClip CreateClip(GameObject source,string name,int motion)
    {
        var clip=new AnimationClip{name=name,frameRate=60,legacy=false};float duration=motion==0?2:motion==1?.85f:.58f;
        foreach(var bone in source.GetComponentsInChildren<Transform>()){
            bool thigh=bone.name.EndsWith("UpLeg"),knee=bone.name.EndsWith("Leg")&&!thigh,foot=bone.name.EndsWith("Foot"),chest=bone.name.EndsWith("Spine2"),head=bone.name.EndsWith("Head");
            if(!thigh&&!knee&&!foot&&!chest&&!head)continue;
            string path=AnimationUtility.CalculateTransformPath(bone,source.transform);var curves=new[]{new AnimationCurve(),new AnimationCurve(),new AnimationCurve(),new AnimationCurve()};
            Vector3 axis=bone.parent.InverseTransformDirection(source.transform.forward).normalized;
            for(int sample=0;sample<=64;sample++){
                float phase=sample*Mathf.PI*2/64,sign=bone.name.Contains("Left")?1:-1,wave=Mathf.Sin(phase)*sign;
                float angle=motion==0?(chest?Mathf.Sin(phase)*1.6f:head?Mathf.Sin(phase)*.8f:0):thigh?wave*(motion==1?25:42):knee?Mathf.Max(0,-wave)*(motion==1?35:65):foot?-wave*12:chest?Mathf.Sin(phase)*3:Mathf.Sin(phase)*1.5f;
                Quaternion q=Quaternion.AngleAxis(angle,axis)*bone.localRotation;float time=duration*sample/64;
                curves[0].AddKey(time,q.x);curves[1].AddKey(time,q.y);curves[2].AddKey(time,q.z);curves[3].AddKey(time,q.w);
            }
            string[] components={"x","y","z","w"};for(int component=0;component<4;component++)AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,typeof(Transform),"m_LocalRotation."+components[component]),curves[component]);
        }
        clip.EnsureQuaternionContinuity();return clip;
    }
    static void Configure(string path,bool animation)
    {
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(ModelImporter)AssetImporter.GetAtPath(path);
        importer.animationType=ModelImporterAnimationType.Generic;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
        importer.importAnimation=animation;importer.importCameras=false;importer.importLights=false;importer.isReadable=true;
        importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;importer.materialLocation=ModelImporterMaterialLocation.External;importer.SaveAndReimport();
    }
}
