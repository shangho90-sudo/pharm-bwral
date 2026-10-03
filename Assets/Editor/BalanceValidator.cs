using System;
using PharmaBrawl;
using UnityEditor;
using UnityEngine;
public static class BalanceValidator
{
    static void Check(bool ok,string message){if(!ok)throw new Exception("Balance QA: "+message);}
    static CharacterDefinition[] Roster(){var r=Resources.LoadAll<CharacterDefinition>("Characters");Array.Sort(r,(a,b)=>a.kind.CompareTo(b.kind));return r;}
    static ArenaSimulation Setup(CharacterDefinition[] r,int hero=0){var sim=new ArenaSimulation(r,hero,57,0,1,new[]{hero,7});sim.covers.Clear();sim.fighters[0].position=Vector2.zero;sim.fighters[0].aim=Vector2.right;sim.fighters[1].position=new Vector2(12,10);return sim;}
    public static void Validate(){var roster=Roster();for(int i=0;i<10;i++){Check(roster[i].skillCooldown==(i==6?3:i==9?8:6),"cooldown "+i);Check(roster[i].ultimateRequirement==(i==3?2310:i==9?3640:2800),"charge requirement "+i);var s=Setup(roster,i);s.Skill(s.fighters[0]);Check(s.fighters[0].skillTimer==(i==6?3:i==9?8:6),"skill timer "+i);s.fighters[0].charge=roster[i].ultimateRequirement-1;s.Ultimate(s.fighters[0]);Check(s.ultimatesUsed==0,"charge gate "+i);s.fighters[0].charge++;s.Ultimate(s.fighters[0]);Check(s.ultimatesUsed==1,"new charge threshold "+i);}
        var sim=Setup(roster);sim.Skill(sim.fighters[1]);var robot=sim.robots[0];Check(robot.hp==800&&robot.maxHp==800,"support health");robot.position=new Vector2(2,0);sim.Attack(sim.fighters[0]);var input=new[]{new ArenaSimulation.HumanInput{human=true},new ArenaSimulation.HumanInput{human=true}};for(int i=0;i<15;i++)sim.TickNetwork(1f/60,input);Check(robot.hp==800-roster[0].damage,"projectile damages robot once");float hp=robot.hp;sim.DamageRobot(1,0,100);Check(robot.hp==hp,"friendly fire excluded");sim.DamageRobot(0,0,900);Check(!robot.active&&robot.hp==0,"destruction");Check(sim.score[0]==0&&sim.fighters[0].kills==0&&sim.fighters[0].charge==0,"robots grant no score or ultimate charge");
        sim.fighters[1].charge=roster[7].ultimateRequirement;sim.Ultimate(sim.fighters[1]);robot=sim.robots[0];Check(robot.active&&robot.hp==1600&&robot.maxHp==1600,"elite reused pool resets health");robot.position=new Vector2(3,0);sim.fighters[0].charge=roster[0].ultimateRequirement;sim.fighters[0].aimDistance=3;sim.Ultimate(sim.fighters[0]);sim.TickNetwork(.91f,input);Check(!robot.active,"explosion destroys elite");
        sim=Setup(roster,2);sim.Skill(sim.fighters[1]);robot=sim.robots[0];robot.position=new Vector2(2,0);robot.hp=1600;sim.fighters[0].empowered=true;sim.Attack(sim.fighters[0]);for(int i=0;i<15;i++)sim.TickNetwork(1f/60,input);Check(robot.hp==1600-roster[2].damage,"piercing shot cannot damage same robot twice");sim.fighters[0].charge=roster[2].ultimateRequirement;sim.Ultimate(sim.fighters[0]);Check(!robot.active,"laser hits robots");
        sim=Setup(roster,9);sim.Skill(sim.fighters[1]);robot=sim.robots[0];robot.position=new Vector2(3,0);sim.fighters[0].aimDistance=3;sim.Skill(sim.fighters[0]);sim.TickNetwork(.01f,input);Check(robot.hp==630,"poison zone tick damages robot");
        Debug.Log("BALANCE_ROBOT_PASS ten cooldowns and charge thresholds including poison=8/3640, support=800 elite=1600, projectile/piercing/explosion/laser/poison, friendly-fire exclusion, destruction, no score/charge, pooled health reset");
    }
    public static void Build(){Validate();ReferenceAbilityBuilder.Validate();MobileAbilityValidator.Validate();PaengCombatValidator.Validate();ArenaMapBuilder.BuildWeb();}
}
