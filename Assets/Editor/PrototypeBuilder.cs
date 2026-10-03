using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PharmaBrawl;

public static class PrototypeBuilder
{
    const string Root="Assets/Resources/Characters";
    public static void Build()
    {
        Directory.CreateDirectory(Root);Directory.CreateDirectory("Assets/Scenes");
        string[] names={"팽재현","구자현","방지욱","박철호","정석호","차동호","김두영","조강희","사공민","이선용"};
        string[] roles={"올라운더","번개 마법사","저격수","탱커","연사 딜러","포병","암살자","소환사","광역 딜러","지속 피해"};
        string[] voices={"팽스닥 좀 사라!","약국에 손님이 없네요","","영미야!","아 철호형!","약국 좀 구해주세요!","오늘밤 증바람 가즈아!","오늘도 맥주잔에 소주 마셨어요!","콩콩이 콩콩콩!","내 티모 맛 좀 봐라!"};
        string[] attacks={"빨간 캡슐 직선 발사","파란 번개 구체","장거리 황금 에너지 화살","근거리 보라 충격파","연두 알약 3발 연사","지정 위치에 파란 알약 낙하","빠른 분홍 알약","빨간 에너지 구체","파란 알약 부채꼴 5발","보라 독성 알약 + 지속 피해"};
        string[] skills={"폭발 캡슐","인접 적 1회 번개 전이","다음 공격 벽 / 적 관통","4초 피해 감소 보호막","4초 공격속도 증가","착탄 지점 위험지역","전방 4m 순간이동","약국 로봇 10초 소환","다음 공격 범위 증가","4초 독 지역"};
        string[] ults={"대형 캡슐 폭발 + 넉백","연쇄 번개","초장거리 관통 황금 광선","돌진 + 적 밀어내기","초고속 알약 난사","넓은 범위 7회 폭격","적 뒤로 순간이동 + 근접 공격","강화 로봇 15초 소환","넓은 부채꼴 알약 폭우","5초 독구름 + 이동속도 감소"};
        Color[] colors={new Color(1,.24f,.25f),new Color(.13f,.67f,1),new Color(1,.72f,.16f),new Color(.57f,.32f,.91f),new Color(.5f,.86f,.19f),new Color(.2f,.46f,1),new Color(1,.34f,.69f),new Color(.93f,.27f,.23f),new Color(.2f,.77f,.94f),new Color(.72f,.28f,.88f)};
        float[] hp={3200,2700,2400,5200,2900,2800,2500,3000,3200,3000};float[] speed={5.2f,5,4.7f,4.2f,5.1f,4.8f,6.6f,5,5,5};
        float[] damage={440,410,780,620,175,600,310,400,160,340};float[] interval={.65f,.7f,1.3f,.85f,.7f,.7f,.38f,.7f,.9f,.75f};float[] range={10,10,15,3.8f,9,8,6,9,8,9};
        var roster=new CharacterDefinition[10];
        for(int i=0;i<10;i++)
        {
            string path=$"{Root}/{i:00}_{(AttackKind)i}.asset";var d=AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path);
            if(!d){d=ScriptableObject.CreateInstance<CharacterDefinition>();AssetDatabase.CreateAsset(d,path);}
            d.displayName=names[i];d.role=roles[i];d.voiceLine=voices[i];d.kind=(AttackKind)i;d.color=colors[i];d.maxHp=hp[i];d.speed=speed[i];d.damage=damage[i];d.attackInterval=interval[i];d.range=range[i];d.projectileSpeed=i==2?26:16;d.skillCooldown=i==6?3:i==9?8:6;d.ultimateRequirement=i==3?2310:i==9?3640:2800;d.attackDescription=attacks[i];d.skillDescription=skills[i];d.ultimateDescription=ults[i];EditorUtility.SetDirty(d);roster[i]=d;
        }
        if(!AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/CombatMaterial.mat"))AssetDatabase.CreateAsset(new Material(Shader.Find("Standard")),"Assets/Resources/CombatMaterial.mat");
        if(!AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/UIBase.mat"))AssetDatabase.CreateAsset(new Material(Shader.Find("UI/Default")),"Assets/Resources/UIBase.mat");
        if(!AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/UIFont.mat"))AssetDatabase.CreateAsset(new Material(Shader.Find("UI/Default Font")),"Assets/Resources/UIFont.mat");
        AssetDatabase.SaveAssets();
        // Dynamic font import includes Hangul; SIL Open Font License shipped beside the font.
        var fi=AssetImporter.GetAtPath("Assets/Resources/Fonts/NotoSansKR.otf") as TrueTypeFontImporter;
        if(fi){fi.fontTextureCase=FontTextureCase.Dynamic;fi.fontSize=24;fi.SaveAndReimport();}
        var titleImporter=AssetImporter.GetAtPath("Assets/Resources/TitleScreen.png") as TextureImporter;
        if(titleImporter){titleImporter.textureType=TextureImporterType.Sprite;titleImporter.spriteImportMode=SpriteImportMode.Single;titleImporter.mipmapEnabled=false;titleImporter.maxTextureSize=2048;titleImporter.textureCompression=TextureImporterCompression.Uncompressed;titleImporter.SaveAndReimport();}
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        for(int i=0;i<10;i++)roster[i]=AssetDatabase.LoadAssetAtPath<CharacterDefinition>($"{Root}/{i:00}_{(AttackKind)i}.asset");
        var g=new GameObject("PHARMA BRAWL / Runtime").AddComponent<PharmaGame>();g.roster=roster;
        // CreatePrimitive's native dependencies are invisible to IL2CPP's static analysis.
        // Inactive scene anchors preserve MeshFilter/Renderer and all three collider types.
        var anchors=new GameObject("Primitive build dependencies (inactive)");
        foreach(var type in new[]{PrimitiveType.Cube,PrimitiveType.Sphere,PrimitiveType.Capsule}){var anchor=GameObject.CreatePrimitive(type);anchor.transform.SetParent(anchors.transform);anchor.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/CombatMaterial.mat");}
        anchors.SetActive(false);
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/Pharmacy.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Pharmacy.unity",true)};
        PlayerSettings.companyName="Pharma Brawl Prototype";PlayerSettings.productName="PHARMA BRAWL";
        PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;PlayerSettings.colorSpace=ColorSpace.Linear;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.template="APPLICATION:Default";PlayerSettings.WebGL.memorySize=256;
        QualitySettings.shadows=ShadowQuality.HardOnly;QualitySettings.shadowResolution=ShadowResolution.Low;QualitySettings.shadowDistance=45;QualitySettings.antiAliasing=2;
        Validate(roster);
        string destination=Path.GetFullPath("../Windows/PharmaBrawl.exe");Directory.CreateDirectory(Path.GetDirectoryName(destination));
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Pharmacy.unity"},locationPathName=destination,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
        if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Windows build failed: "+report.summary.result);
        Debug.Log("PHARMA_BUILD_SUCCESS "+destination);
    }
    public static void BuildWeb()
    {
        PlayerSettings.WebGL.template="PROJECT:Pharma";
        string destination=Path.GetFullPath("../WebGL");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Pharmacy.unity"},locationPathName=destination,target=BuildTarget.WebGL,options=BuildOptions.None});
        if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("WebGL build failed");Debug.Log("PHARMA_WEBGL_SUCCESS");
    }
    static void Require(bool ok,string message){if(!ok)throw new Exception("Validation: "+message);}
    static void Validate(CharacterDefinition[] roster)
    {
        Require(roster.Length==10,"10 definitions");
        for(int k=0;k<10;k++)
        {
            var s=new ArenaSimulation(roster,k);var p=s.fighters[0];p.position=Vector2.zero;p.aim=Vector2.right;s.fighters[3].position=new Vector2(2,0);
            s.Attack(p);Require(p.attackTimer>0,$"attack cooldown {k} hp={p.hp} interval={p.data.attackInterval} timer={p.attackTimer}");s.Skill(p);Require(p.skillTimer>0,"skill cooldown "+k);p.charge=p.data.ultimateRequirement;s.Ultimate(p);Require(p.charge<p.data.ultimateRequirement && s.ultimatesUsed==1,"ultimate consumption "+k);
            for(int i=0;i<180;i++)s.Tick(1/60f,Vector2.zero,Vector2.right,false,false,false);
            Require(s.fighters[3].damageTaken>0,"attack / skill collision "+k);
        }
        var regen=new ArenaSimulation(roster,0);regen.fighters[0].hp=500;regen.fighters[0].position=new Vector2(-16,-11);
        for(int i=0;i<240;i++)regen.Tick(1/60f,Vector2.zero,Vector2.up,false,false,false);
        Require(regen.fighters[0].healing>0,"passive regeneration");
        var respawn=new ArenaSimulation(roster,0);respawn.Damage(3,0,10000);Require(!respawn.fighters[0].Alive && respawn.score[1]==1,"death and score");
        for(int i=0;i<245;i++)respawn.Tick(1/60f,Vector2.zero,Vector2.up,false,false,false);Require(respawn.fighters[0].Alive && respawn.respawns>0,"respawn");
        for(int k=0;k<10;k++)
        {
            var s=new ArenaSimulation(roster,k,42+k);for(int i=0;i<10810 && !s.finished;i++)s.Tick(1/60f,Vector2.zero,Vector2.up,false,false,false,true);
            Require(s.finished,"full match "+k);Require(s.shotsFired>0 && s.skillsUsed>0,"bots attack / skill "+k);Require(s.score[0]+s.score[1]>0,"bots score "+k);
            Debug.Log($"PHARMA_MATCH_TEST hero={k} score={s.score[0]}:{s.score[1]} shots={s.shotsFired} skills={s.skillsUsed} ults={s.ultimatesUsed}");
        }
        Debug.Log("PHARMA_VALIDATION_PASS: attacks/skills/ultimates, regen, death, respawn, ten complete bot matches");
    }
}
