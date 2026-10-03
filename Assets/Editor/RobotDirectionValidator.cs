using System;
using System.IO;
using PharmaBrawl;
using UnityEditor;
using UnityEngine;

public static class RobotDirectionValidator
{
    public static void Capture()
    {
        UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
        var roster=Resources.LoadAll<CharacterDefinition>("Characters");Array.Sort(roster,(a,b)=>a.kind.CompareTo(b.kind));
        var sim=new ArenaSimulation(roster,7,42,0,1,new[]{7,7});sim.covers.Clear();sim.fighters[0].position=Vector2.zero;sim.fighters[1].withdrawn=true;
        var light=new GameObject("Robot light").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;light.transform.rotation=Quaternion.Euler(35,-30,0);RenderSettings.ambientLight=Color.white*.7f;
        var camera=new GameObject("Robot front comparison").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=3.3f;camera.aspect=2.5f;camera.transform.position=new Vector3(0,4,-12);camera.transform.LookAt(Vector3.up);
        for(int i=0;i<4;i++){
            var r=sim.robots[i];r.active=true;r.owner=0;r.elite=true;r.hp=r.maxHp=1600;
            sim.fighters[0].aim=ArenaSimulation.Rotate(Vector2.up,i*90);
            r.aim=sim.fighters[0].aim;
            var root=new GameObject("Elite angle "+i*90);root.transform.position=new Vector3((i-1.5f)*3.4f,0,0);var view=root.AddComponent<PharmacyDroneView>();view.Initialize();view.Sync(sim,i,1);
        }
        var target=new RenderTexture(1500,600,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;var pixels=new Texture2D(1500,600,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1500,600),0,0);pixels.Apply();File.WriteAllBytes(Path.GetFullPath("../../outputs/elite-directions-after.png"),pixels.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;UnityEngine.Object.DestroyImmediate(pixels);UnityEngine.Object.DestroyImmediate(target);
    }
    public static void Build()
    {
        var roster=Resources.LoadAll<CharacterDefinition>("Characters");Array.Sort(roster,(a,b)=>a.kind.CompareTo(b.kind));
        var sim=new ArenaSimulation(roster,7,42,0,1,new[]{7,7});sim.covers.Clear();sim.fighters[0].position=Vector2.zero;
        var robot=sim.robots[0];robot.active=true;robot.owner=0;robot.elite=true;robot.hp=robot.maxHp=1600;
        var root=new GameObject("Elite aim QA");var view=root.AddComponent<PharmacyDroneView>();view.Initialize();
        // Conflicting nearby projectiles from the same owner must never override this robot's aim.
        sim.shots[0].active=true;sim.shots[0].owner=0;sim.shots[0].damage=240;sim.shots[0].direction=Vector2.down;
        for(int i=0;i<8;i++){
            robot.aim=ArenaSimulation.Rotate(Vector2.up,i*45);view.Sync(sim,0,.1f);
            var desired=new Vector3(robot.aim.x,0,robot.aim.y);
            if(Vector3.Dot(root.transform.forward,desired)<.999f)throw new Exception("Robot rotation does not follow authoritative aim");
            var body=root.transform.Find("Tripo Elite robot body");
            if(Vector3.Dot(body.TransformDirection(Vector3.left),desired)<.999f)throw new Exception("Imported elite front axis does not match projectile direction");
        }
        UnityEngine.Object.DestroyImmediate(root);
        Debug.Log("ELITE_DIRECTION_PASS eight directions; model front corrected; unrelated projectile ignored; immediate tracking");
        Capture();ReferenceAbilityBuilder.Validate();ArenaMapBuilder.BuildWeb();
    }
}
