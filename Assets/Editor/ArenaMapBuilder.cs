using System;
using System.IO;
using PharmaBrawl;
using UnityEditor;
using UnityEngine;

public static class ArenaMapBuilder
{
    static void Require(bool condition,string message){if(!condition)throw new Exception("Arena validation: "+message);}
    [MenuItem("Pharma Brawl/Validate Four Arenas")]
    public static void Validate()
    {
        var roster=Resources.LoadAll<CharacterDefinition>("Characters");Array.Sort(roster,(a,b)=>a.kind.CompareTo(b.kind));
        Require(roster.Length==10,"roster");
        for(int map=0;map<4;map++)
        {
            var sim=new ArenaSimulation(roster,0,42,map);var nav=new ArenaNavigation(sim);
            for(int id=0;id<6;id++)Require(!sim.Blocked(sim.map.Spawn(id)),"spawn clearance "+map+"/"+id);
            for(int from=0;from<3;from++)for(int to=3;to<6;to++)Require(nav.FindPath(sim.map.Spawn(from),sim.map.Spawn(to)).Length>0,"team connectivity "+map);
            foreach(var feature in sim.map.features)
            {
                if(feature.blocksMovement)Require(sim.Blocked(feature.position,.01f),"solid footprint "+map+" "+feature.kind);
                if(feature.kind==ArenaProp.Bridge || feature.kind==ArenaProp.Ice || feature.kind==ArenaProp.Bush)Require(!sim.Blocked(feature.position),"passable feature "+map+" "+feature.kind);
                if(feature.kind==ArenaProp.Water)Require(!sim.ShotBlocked(feature.position,.01f),"bullets cross water "+map);
            }
            // Direct input walks into the river, but must stop before water; all three bridges cross it.
            if(map==2)
            {
                sim.fighters[0].position=new Vector2(5,-2.2f);
                for(int t=0;t<90;t++)sim.Tick(1/60f,Vector2.up,Vector2.up,false,false,false);
                Require(sim.fighters[0].position.y< -1.5f,"river blocks player");
                foreach(float x in new[]{-10.8f,0,10.8f}){
                    var crossing=new ArenaSimulation(roster,0,42,2);crossing.fighters[0].position=new Vector2(x,-2.2f);
                    for(int t=0;t<60;t++)crossing.Tick(1/60f,Vector2.up,Vector2.up,false,false,false);
                    Require(crossing.fighters[0].position.y>2,"bridge crossing "+x);
                }
            }
            // Destructible art and navigation open the same footprint.
            foreach(var cover in sim.covers)if(cover.destructible){Require(sim.Blocked(cover.position,.01f),"crate initially solid");cover.hp=0;Require(!sim.Blocked(cover.position,.01f),"destroyed crate opens path");break;}
            for(int hero=0;hero<10;hero++)
            {
                var match=new ArenaSimulation(roster,hero,600+hero,map);
                for(int t=0;t<10810 && !match.finished;t++){
                    match.Tick(1/60f,Vector2.zero,Vector2.up,false,false,false,true);
                    foreach(var f in match.fighters)if(f.Alive)Require(!match.Blocked(f.position,.45f),"fighter inside terrain "+map+"/"+hero);
                    foreach(var bot in match.robots)if(bot.active)Require(!match.Blocked(bot.position,.25f),"summon inside terrain "+map+"/"+hero);
                }
                Require(match.finished && match.shotsFired>0 && match.skillsUsed>0 && match.score[0]+match.score[1]>0,"complete combat "+map+"/"+hero);
                Debug.Log($"ARENA_MATCH_PASS map={map} hero={hero} score={match.score[0]}:{match.score[1]} shots={match.shotsFired} respawns={match.respawns}");
            }
            Require(Resources.Load<AudioClip>("Music/"+sim.map.key),"map music "+map);
            Require(Resources.Load<GameObject>("MapProps/"+sim.map.key),"landmark "+map);
        }
        Require(Resources.Load<AudioClip>("Music/menu"),"menu soundtrack");
        Debug.Log("FOUR_ARENAS_VALIDATION_PASS: 40 matches, collision, bridges, ice, foliage, respawns, landmarks, music");
    }
    public static void ImportLandmarks()
    {
        Directory.CreateDirectory("Assets/Resources/MapProps");
        foreach(string key in new[]{"village","lab","desert","alpine","capsule-wall","lab-wall","medical-container","herb-bush"})
        {
            string folder="Assets/Art/Maps/"+key;
            var importer=AssetImporter.GetAtPath(folder+"/model.fbx") as ModelImporter;
            if(!importer){if(Array.IndexOf(ArenaMap.Keys,key)>=0)throw new Exception("Missing FBX: "+key);else continue;}
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation=ModelImporterMaterialLocation.External;
            importer.importCameras=false;importer.importLights=false;importer.importAnimation=false;importer.SaveAndReimport();
            Directory.CreateDirectory(folder+"/Textures");importer.ExtractTextures(folder+"/Textures");AssetDatabase.Refresh();
            // FBX-extracted RGB tangent normals must be packed by Unity as normal maps.
            // Treating these as sRGB color corrupts Standard's normal unpacking and lighting.
            foreach(string file in Directory.GetFiles(folder,"*.PNG",SearchOption.AllDirectories)){
                var textureImporter=AssetImporter.GetAtPath(file.Replace('\\','/')) as TextureImporter;if(!textureImporter)continue;
                bool normal=Path.GetFileName(file).ToLowerInvariant().Contains("normal");
                textureImporter.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
                textureImporter.sRGBTexture=!normal && Path.GetFileName(file).ToLowerInvariant().Contains("basecolor");
                textureImporter.maxTextureSize=2048;textureImporter.anisoLevel=2;textureImporter.SaveAndReimport();
            }
            importer.SearchAndRemapMaterials(ModelImporterMaterialName.BasedOnMaterialName,ModelImporterMaterialSearch.Local);importer.SaveAndReimport();
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(folder+"/model.fbx");
            var instance=UnityEngine.Object.Instantiate(asset);instance.name=key;
            foreach(var renderer in instance.GetComponentsInChildren<Renderer>())foreach(var material in renderer.sharedMaterials)if(material){
                material.shader=Shader.Find("Standard");material.color=Color.white;material.SetFloat("_Glossiness",.2f);material.SetFloat("_BumpScale",.35f);
                material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Textures/tripo_model_basecolor.PNG");
                material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/Textures/tripo_model_normal.PNG"));material.EnableKeyword("_NORMALMAP");EditorUtility.SetDirty(material);
            }
            PrefabUtility.SaveAsPrefabAsset(instance,"Assets/Resources/MapProps/"+key+".prefab");UnityEngine.Object.DestroyImmediate(instance);
        }
        foreach(string key in new[]{"menu","village","lab","desert","alpine"}){
            var importer=AssetImporter.GetAtPath("Assets/Resources/Music/"+key+".mp3") as AudioImporter;
            var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.Streaming;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.8f;importer.defaultSampleSettings=settings;importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();Debug.Log("ARENA_ASSETS_IMPORTED");
    }
    public static void Build()
    {
        ImportLandmarks();Validate();
        string destination=Path.GetFullPath("../../outputs/PharmaBrawl-Windows/PharmaBrawl.exe");Directory.CreateDirectory(Path.GetDirectoryName(destination));
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Pharmacy.unity"},locationPathName=destination,target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
        Require(report.summary.result==UnityEditor.Build.Reporting.BuildResult.Succeeded,"Windows build "+report.summary.result);
        Debug.Log("FOUR_ARENAS_BUILD_SUCCESS "+destination);
    }
    public static void ImportSelectionArtwork()
    {
        var importer=AssetImporter.GetAtPath("Assets/Resources/SelectionScreen.png") as TextureImporter;
        importer.textureType=TextureImporterType.Default;importer.mipmapEnabled=false;
        importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;
        importer.textureCompression=TextureImporterCompression.Uncompressed;importer.wrapMode=TextureWrapMode.Clamp;
        importer.SaveAndReimport();
    }
    public static void BuildWeb()
    {
        ImportSelectionArtwork();Validate();
        PlayerSettings.WebGL.template="PROJECT:Pharma";
        PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback=true;
        PlayerSettings.WebGL.initialMemorySize=512;
        foreach(string key in new[]{"menu","village","lab","desert","alpine"}){
            var importer=AssetImporter.GetAtPath("Assets/Resources/Music/"+key+".mp3") as AudioImporter;
            var settings=importer.defaultSampleSettings;settings.loadType=AudioClipLoadType.CompressedInMemory;
            importer.SetOverrideSampleSettings("WebGL",settings);importer.SaveAndReimport();
        }
        string destination=Path.GetFullPath("../../outputs/PharmaBrawl-Web");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/Pharmacy.unity"},locationPathName=destination,target=BuildTarget.WebGL,options=BuildOptions.None});
        Require(report.summary.result==UnityEditor.Build.Reporting.BuildResult.Succeeded,"Web build "+report.summary.result);
        Debug.Log("FOUR_ARENAS_WEB_SUCCESS "+destination);
    }
}
