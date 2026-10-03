using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using PharmaBrawl;
using UnityEditor;
using UnityEngine;

public static class CombatAudioValidator
{
    public static void Validate()
    {
        var hashes=new HashSet<string>();
        foreach(string key in Enum.GetNames(typeof(AttackKind)))Check(key,hashes);
        Check("Hit",hashes);
        Debug.Log("COMBAT_AUDIO_PASS 10 distinct weapon cues + hit cue imported and decoded");
    }
    static void Check(string key,HashSet<string> hashes)
    {
        string path="Assets/Resources/SFX/"+key+".mp3";
        var clip=Resources.Load<AudioClip>("SFX/"+key);
        if(!clip || clip.length<.4f || clip.length>1.2f)throw new Exception("Missing/invalid SFX "+key);
        if(!hashes.Add(Convert.ToBase64String(SHA256.Create().ComputeHash(File.ReadAllBytes(path)))))throw new Exception("Duplicate SFX "+key);
        var importer=(AudioImporter)AssetImporter.GetAtPath(path);
        if(!importer)throw new Exception("Missing audio importer "+key);
        Debug.Log("WEAPON_AUDIO_PASS "+key+" duration="+clip.length+" samples="+clip.samples);
    }
}
