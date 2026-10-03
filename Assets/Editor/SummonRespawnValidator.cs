using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using PharmaBrawl;
using UnityEditor;
using UnityEngine;

public static class SummonRespawnValidator
{
    static void Check(bool ok,string reason){if(!ok)throw new Exception("Respawn/audio validation: "+reason);}
    public static void Build()
    {
        AssetDatabase.Refresh();var hashes=new HashSet<string>();
        foreach(string family in Enum.GetNames(typeof(AttackKind)))foreach(string suffix in new[]{"Skill","Ultimate"}){
            string key=family+suffix;var clip=Resources.Load<AudioClip>("SFX/"+key);
            Check(clip&&clip.length>.8f&&clip.length<2.3f,key+" imported duration");
            Check(hashes.Add(Convert.ToBase64String(SHA256.Create().ComputeHash(File.ReadAllBytes("Assets/Resources/SFX/"+key+".mp3")))),key+" unique content");
            clip.LoadAudioData();var samples=new float[clip.samples*clip.channels];Check(clip.GetData(samples,0),key+" decoded PCM");
            float peak=0;double square=0;foreach(float s in samples){peak=Mathf.Max(peak,Mathf.Abs(s));square+=s*s;}
            Check(peak>.02f&&peak<1&&square/samples.Length>.00001,key+" audible, no clipping");
            Debug.Log("ABILITY_AUDIO_PASS "+key+" seconds="+clip.length+" peak="+peak+" rms="+Math.Sqrt(square/samples.Length));
        }
        var roster=Resources.LoadAll<CharacterDefinition>("Characters");Array.Sort(roster,(a,b)=>a.kind.CompareTo(b.kind));
        foreach(var data in roster){
            var root=new GameObject("Respawn blink QA "+data.displayName);var rig=root.AddComponent<PharmacistModelRig>();rig.Initialize(data);
            rig.SetRespawnProtection(1.91f);foreach(var r in root.GetComponentsInChildren<Renderer>())if(r.gameObject.activeInHierarchy)Check(!r.enabled,"body and weapon blink off "+data.displayName);
            rig.SetRespawnProtection(1.81f);foreach(var r in root.GetComponentsInChildren<Renderer>())if(r.gameObject.activeInHierarchy)Check(r.enabled,"body and weapon blink on "+data.displayName);
            rig.SetRespawnProtection(1.91f);rig.SetRespawnProtection(0);foreach(var r in root.GetComponentsInChildren<Renderer>())if(r.gameObject.activeInHierarchy)Check(r.enabled,"visibility restored "+data.displayName);
            Check(root.activeSelf,"blink does not deactivate controls or actor");UnityEngine.Object.DestroyImmediate(root);
        }
        Debug.Log("RESPAWN_BLINK_PASS all ten characters and weapons; restoration after protection");
        ReferenceAbilityBuilder.Validate();PaengCombatValidator.Validate();SpectacularAbilityBuilder.Validate();
        ArenaMapBuilder.BuildWeb();
    }
}
