using System;
using PharmaBrawl;
using UnityEditor;
using UnityEngine;
public static class NetworkMatchValidator
{
    public static void BuildWeb()
    {
        CombatAudioValidator.Validate();
        var roster=Resources.LoadAll<CharacterDefinition>("Characters");Array.Sort(roster,(a,b)=>a.kind.CompareTo(b.kind));
        for(int map=0;map<4;map++)for(int hero=0;hero<10;hero++){
            var sim=new ArenaSimulation(roster,hero,hero+19,map,4);
            foreach(var f in sim.fighters)if(sim.Blocked(f.position))throw new Exception("Blocked 4v4 spawn");
            for(int tick=0;tick<5400&&!sim.finished;tick++){
                sim.Tick(1/30f,Vector2.zero,Vector2.up,false,false,false,true);
                foreach(var f in sim.fighters)if(sim.Blocked(f.position,.47f))throw new Exception("4v4 fighter left walkable area map="+map+" hero="+hero+" id="+f.id);
            }
            Debug.Log("FOUR_V_FOUR_MATCH_PASS map="+map+" hero="+hero+" score="+sim.score[0]+":"+sim.score[1]);
        }
        ArenaMapBuilder.BuildWeb();
    }
}
