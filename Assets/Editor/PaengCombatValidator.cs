using System;
using System.IO;
using PharmaBrawl;
using UnityEditor;
using UnityEngine;

public static class PaengCombatValidator
{
    static void Require(bool condition,string message){if(!condition)throw new Exception("Paeng ability validation: "+message);}
    public static void Capture()
    {
        Validate();
        UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
        var roster=Resources.LoadAll<CharacterDefinition>("Characters");Array.Sort(roster,(a,b)=>a.kind.CompareTo(b.kind));
        var sim=new ArenaSimulation(roster,0,57,0,1,new[]{0,1});sim.covers.Clear();var p=sim.fighters[0];p.position=new Vector2(0,4);p.aim=Vector2.down;p.aimDistance=7;sim.fighters[1].position=new Vector2(.2f,-3);
        var light=new GameObject("Battle light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;light.transform.rotation=Quaternion.Euler(45,-30,0);RenderSettings.ambientLight=Color.white*.75f;
        var camera=new GameObject("Battle capture").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=7;camera.backgroundColor=new Color(.06f,.1f,.17f);camera.transform.position=new Vector3(0,13,-15);camera.transform.LookAt(Vector3.up*1.4f);
        var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.name="Capture floor";ground.transform.position=new Vector3(0,-.25f,0);ground.transform.localScale=new Vector3(20,.5f,20);ground.GetComponent<Renderer>().sharedMaterial=new Material(Shader.Find("Standard")){color=new Color(.27f,.5f,.45f)};
        foreach(var f in sim.fighters){var actor=new GameObject(f.data.displayName);actor.transform.position=new Vector3(f.position.x,0,f.position.y);actor.transform.rotation=Quaternion.LookRotation(new Vector3(f.aim.x,0,f.aim.y));var rig=actor.AddComponent<PharmacistModelRig>();rig.Initialize(f.data);}
        var effects=new GameObject("Real-time ability capture").AddComponent<PaengAbilityView>();effects.Initialize(sim);sim.Event+=effects.Emit;
        p.charge=p.data.ultimateRequirement;sim.Ultimate(p);var input=new[]{new ArenaSimulation.HumanInput{human=true},new ArenaSimulation.HumanInput{human=true}};sim.TickNetwork(.45f,input);effects.Sync(0);Save(camera,"paeng-ultimate-warning.png");
        sim.TickNetwork(.46f,input);effects.Sync(.15f);foreach(var ps in effects.GetComponentsInChildren<ParticleSystem>())ps.Simulate(.15f,false,false);Save(camera,"paeng-ultimate-impact.png");
        Debug.Log("PAENG_REALTIME_CAPTURE_PASS actual pending zone and server simulation explosion");
    }
    static void Save(Camera camera,string name){var target=new RenderTexture(1400,900,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;var pixels=new Texture2D(1400,900,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1400,900),0,0);pixels.Apply();File.WriteAllBytes(Path.GetFullPath("../../outputs/"+name),pixels.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(target);}
    public static void Validate(){
        var roster=Resources.LoadAll<CharacterDefinition>("Characters");Array.Sort(roster,(a,b)=>a.kind.CompareTo(b.kind));
        var inputs=new ArenaSimulation.HumanInput[4];for(int i=0;i<inputs.Length;i++)inputs[i].human=true;
        var sim=new ArenaSimulation(roster,0,57,0,2,new[]{0,1,2,3});sim.covers.Clear();
        var p=sim.fighters[0];p.position=Vector2.zero;p.aim=Vector2.right;sim.fighters[1].position=new Vector2(-8,8);sim.fighters[2].position=new Vector2(2,0);sim.fighters[3].position=new Vector2(2,1.1f);
        sim.Skill(p);Require(p.skillTimer==9,"9 second cooldown");Require(sim.shotsFired==1,"one skill capsule");sim.Skill(p);Require(sim.shotsFired==1,"cooldown prevents repeat");
        for(int i=0;i<30;i++)sim.TickNetwork(1f/60,inputs);
        Require(sim.fighters[2].damageTaken==900,"600 direct plus 300 splash");Require(sim.fighters[3].damageTaken==300,"300 nearby splash");Require(p.charge==1200,"damage charges ultimate using existing rules");
        sim=new ArenaSimulation(roster,0,57,0,2,new[]{0,1,2,3});sim.covers.Clear();p=sim.fighters[0];p.position=Vector2.zero;p.aim=Vector2.right;p.aimDistance=7;
        sim.fighters[1].position=new Vector2(-8,8);sim.fighters[2].position=new Vector2(7,.3f);sim.fighters[3].position=new Vector2(10.7f,0);
        var root=new GameObject("Paeng ability presentation validation");var view=root.AddComponent<PaengAbilityView>();view.Initialize(sim);sim.Event+=view.Emit;
        sim.Ultimate(p);Require(sim.ultimatesUsed==0,"cannot cast without required charge");p.charge=p.data.ultimateRequirement;sim.Ultimate(p);Require(p.charge==0,"charge consumed");view.Sync(0);Require(view.ActiveBombs==1,"one falling bomb");
        var zone=Array.Find(sim.zones,z=>z.active);Require(zone.radius==3.5f&&zone.damage==1700&&zone.remaining==.9f,"original ultimate values");
        foreach(var line in root.GetComponentsInChildren<LineRenderer>())if(line.name=="Exact damage radius"&&line.gameObject.activeInHierarchy)for(int i=0;i<line.positionCount;i++){var point=line.GetPosition(i);Require(Mathf.Abs(Vector2.Distance(new Vector2(point.x,point.z),zone.position)-zone.radius)<.001f,"floor marker equals server radius");}
        sim.TickNetwork(.89f,inputs);Require(sim.fighters[2].damageTaken==0,"no early explosion");view.Sync(0);Require(view.ActiveBombs==1,"warning persists before fuse end");
        var before=sim.fighters[2].position;sim.TickNetwork(.02f,inputs);view.Sync(0);Require(sim.fighters[2].damageTaken==1700,"ultimate damage");Require(Mathf.Abs(Vector2.Distance(before,sim.fighters[2].position)-1.2f)<.001f,"existing knockback");Require(sim.fighters[3].damageTaken==0,"outside radius is unharmed");Require(view.ActiveBombs==0&&view.ActiveBursts==1,"bomb removed and one impact plays");
        view.Sync(1);Require(view.ActiveBursts==0,"impact expires");p.charge=p.data.ultimateRequirement;sim.Ultimate(p);view.Sync(0);view.SetVisible(false);Require(view.ActiveBombs==0&&view.ActiveBursts==0,"pause clears effects");view.SetVisible(true);view.Sync(0);Require(view.ActiveBombs==1,"resume reconstructs only authoritative pending zone");view.Clear();view.Sync(0);Require(view.ActiveBombs==0&&view.ActiveBursts==0,"match/leave cleanup remains cleared on next frame");UnityEngine.Object.DestroyImmediate(root);
        var mapSim=new ArenaSimulation(roster,0,57,0,1,new[]{0,0});
        File.WriteAllText(Path.GetFullPath("../../work/paeng-test-map.json"),JsonUtility.ToJson(new MapGeometry{covers=mapSim.covers.ToArray()}));
        Debug.Log("PAENG_ABILITIES_PASS direct=600 splash=300 cooldown=9 fuse=.9 radius=3.5 damage=1700 knockback=1.2 charge gating, exact marker, pooled expiry, pause and match cleanup");
    }
    [Serializable]public sealed class MapGeometry{public ArenaSimulation.Cover[] covers;}
}
