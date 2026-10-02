using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.SceneManagement;
using PharmaBrawl;
public static class PaengQualityCapture {
 public static void Video()=>Capture("video");
 public static void Before()=>Capture("before"); public static void After()=>Capture("after");
 static void Capture(string label){
  EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
  var roster=Resources.LoadAll<CharacterDefinition>("Characters");Array.Sort(roster,(a,b)=>a.kind.CompareTo(b.kind));
  var sim=new ArenaSimulation(roster,0,57,0,1,new[]{0,1});var p=sim.fighters[0];p.position=new Vector2(0,-2);p.aim=Vector2.up;p.aimDistance=6;sim.fighters[1].position=new Vector2(.2f,4);
  var cam=new GameObject("Actual gameplay camera").AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=12.35f;cam.aspect=16f/9;cam.transform.position=new Vector3(0,28,-33.3691f);cam.transform.rotation=Quaternion.Euler(40,0,0);
  new GameObject("Existing village arena").AddComponent<ReferenceOasisView>().Build(cam,sim,new List<Transform>());
  var light=new GameObject("Battle light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;light.transform.rotation=Quaternion.Euler(45,-30,0);RenderSettings.ambientLight=Color.white*.75f;
  var actors=new List<Transform>();foreach(var f in sim.fighters){var actor=new GameObject(f.data.displayName);actor.transform.position=new Vector3(f.position.x,0,f.position.y);actor.transform.rotation=Quaternion.LookRotation(new Vector3(f.aim.x,0,f.aim.y));actor.AddComponent<PharmacistModelRig>().Initialize(f.data);actors.Add(actor.transform);}
  var root=new GameObject("Authoritative capsule effects");Action<float> sync;if(label=="before"){var view=root.AddComponent<PaengBaselineCaptureView>();view.Initialize(sim);sim.Event+=view.Emit;sync=view.Sync;}else{var view=root.AddComponent<PaengAbilityView>();view.Initialize(sim);sim.Event+=view.Emit;sync=view.Sync;}
  p.charge=p.data.ultimateRequirement;sim.Ultimate(p);var input=new[]{new ArenaSimulation.HumanInput{human=true},new ArenaSimulation.HumanInput{human=true}};
  if(label=="video"){for(int i=0;i<90;i++){sim.TickNetwork(1f/30,input);sync(1f/30);foreach(var ps in root.GetComponentsInChildren<ParticleSystem>())if(ps.gameObject.activeInHierarchy)ps.Simulate(1f/30,false,false);for(int j=0;j<actors.Count;j++){actors[j].position=new Vector3(sim.fighters[j].position.x,0,sim.fighters[j].position.y);actors[j].GetComponent<PharmacistModelRig>().UpdatePose(1f/30);}Save(cam,"video-"+i.ToString("000"));}Debug.Log("PAENG_UNITY_VIDEO_CAPTURE_PASS 90 authoritative simulation frames");return;}
  sim.TickNetwork(.45f,input);sync(0);Save(cam,label+"-warning");sim.TickNetwork(.46f,input);
  float prior=0;foreach(float t in new[]{.04f,.14f,.3f,.6f}){sync(t-prior);foreach(var ps in root.GetComponentsInChildren<ParticleSystem>())ps.Simulate(t-prior,false,false);Save(cam,label+"-impact-"+Mathf.RoundToInt(t*100));prior=t;}
  Debug.Log("PAENG_GAME_CAMERA_CAPTURE_PASS "+label+" same camera, map, positions and authoritative timestamps");
 }
 static void Save(Camera cam,string name){var rt=new RenderTexture(1600,900,24);cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;var tex=new Texture2D(1600,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,900),0,0);tex.Apply();File.WriteAllBytes(Path.GetFullPath("../../outputs/paeng-"+name+".png"),tex.EncodeToPNG());RenderTexture.active=null;cam.targetTexture=null;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);}
}
