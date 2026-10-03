using System;
using System.IO;
using PharmaBrawl;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ResultScreenBuilder
{
    public static void BuildWebAndCapture()
    {
        ReferenceAssetBuilder.Import();
        ArenaMapBuilder.ImportSelectionArtwork();
        var roster=Resources.LoadAll<CharacterDefinition>("Characters");Array.Sort(roster,(a,b)=>a.kind.CompareTo(b.kind));
        Capture(roster,true);Capture(roster,false);
        EditorSceneManager.OpenScene("Assets/Scenes/Pharmacy.unity");
        ArenaMapBuilder.BuildWeb();
    }
    static void Capture(CharacterDefinition[] roster,bool victory)
    {
        ArenaSimulation sim=null;
        for(int seed=1;seed<=50;seed++){
            sim=new ArenaSimulation(roster,seed%10,seed,0);
            for(int t=0;t<10810 && !sim.finished;t++)sim.Tick(1/60f,Vector2.zero,Vector2.up,false,false,false,true);
            if(sim.winner==(victory?0:1))break;
        }
        if(sim.winner!=(victory?0:1))throw new Exception("Could not find real result for capture");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("Result capture camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;
        var target=new RenderTexture(1600,900,24);camera.targetTexture=target;
        var root=new GameObject("Result UI",typeof(RectTransform));var canvas=root.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=5;
        var scaler=root.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
        var music=root.AddComponent<ArenaMusic>();var view=root.AddComponent<ReferenceResultView>();view.Initialize(Resources.Load<Font>("Fonts/NotoSansKR"),music,()=>{},()=>{});view.Show(sim);
        Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
        var pixels=new Texture2D(1600,900,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1600,900),0,0);pixels.Apply();
        string path=Path.GetFullPath("../../outputs/"+(victory?"victory-result.png":"defeat-result.png"));File.WriteAllBytes(path,pixels.EncodeToPNG());
        RenderTexture.active=null;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(target);
        Debug.Log("RESULT_CAPTURE_PASS "+path+" score="+sim.score[0]+":"+sim.score[1]);
    }
}
