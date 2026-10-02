using System;
using PharmaBrawl;
using UnityEngine;
public static class AttackPresentationValidator
{
    static void Check(bool ok,string message){if(!ok)throw new Exception("Attack presentation: "+message);}
    static ArenaSimulation Setup(CharacterDefinition[] r,float distance){var s=new ArenaSimulation(r,3,57,0,1,new[]{3,0});s.covers.Clear();s.fighters[0].position=Vector2.zero;s.fighters[0].aim=Vector2.right;s.fighters[1].position=new Vector2(distance,0);return s;}
    public static void Build(){var r=Resources.LoadAll<CharacterDefinition>("Characters");Array.Sort(r,(a,b)=>a.kind.CompareTo(b.kind));var s=Setup(r,4.4f);float hp=s.fighters[1].hp;var root=new GameObject("Wave QA");var view=root.AddComponent<ReferenceAbilityView>();view.Initialize(s);s.Event+=view.Emit;s.Attack(s.fighters[0]);Check(s.fighters[1].hp==hp-r[3].damage,"extended range hits once");Check(view.ActiveWaves==1,"traveling wave emitted");view.Advance(.4f);Check(view.ActiveWaves==0,"wave lifetime ends");view.Clear();UnityEngine.Object.DestroyImmediate(root);s=Setup(r,5.2f);hp=s.fighters[1].hp;s.Attack(s.fighters[0]);Check(s.fighters[1].hp==hp,"out of range misses");s=Setup(r,4.4f);s.covers.Add(new ArenaSimulation.Cover{position=new Vector2(2,0),size=new Vector2(1,3)});hp=s.fighters[1].hp;s.Attack(s.fighters[0]);Check(s.fighters[1].hp==hp,"wall still blocks attack");Debug.Log("ATTACK_PRESENTATION_PASS extended range, single damage, wall and range exclusions, flying wave lifetime");ArenaMapBuilder.BuildWeb();}
}
