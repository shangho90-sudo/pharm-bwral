using System;
using PharmaBrawl;
using UnityEngine;
public static class LeaveMatchValidator
{
    public static void Build(){var roster=Resources.LoadAll<CharacterDefinition>("Characters");Array.Sort(roster,(a,b)=>a.kind.CompareTo(b.kind));var sim=new ArenaSimulation(roster,7,57,0,2,new[]{7,0,1,2});sim.Skill(sim.fighters[0]);sim.Attack(sim.fighters[0]);sim.Withdraw(0);var input=new ArenaSimulation.HumanInput[4];for(int i=0;i<4;i++)input[i].human=true;for(int i=0;i<300;i++)sim.TickNetwork(1f/60,input);if(sim.finished||sim.fighters[0].Alive||!sim.fighters[0].withdrawn||sim.fighters[0].deaths!=0||sim.score[1]!=0||Array.Exists(sim.robots,r=>r.active&&r.owner==0)||Array.Exists(sim.shots,s=>s.active&&s.owner==0))throw new Exception("Leave match cleanup/continuation failed");var state=FighterState.From(sim.fighters[0]);state.Apply(sim.fighters[1]);if(!sim.fighters[1].withdrawn)throw new Exception("Withdraw snapshot failed");Debug.Log("LEAVE_MATCH_PASS no forced finish, respawn, score, AI or lingering summons; withdrawn snapshot");ArenaMapBuilder.BuildWeb();}
}
