using System.Reflection;
using System.Text.Json;
using PharmaBrawl;
using UnityEngine;

static class CombatRegression
{
    static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);}
    public static void Run()
    {
        var roster=JsonSerializer.Deserialize<CharacterDefinition[]>(File.ReadAllText("characters.json"),new JsonSerializerOptions{IncludeFields=true});
        ArenaSimulation Setup(int map=0){var s=new ArenaSimulation(roster,7,42,map,1,new[]{7,7});s.covers.Clear();s.fighters[0].position=new Vector2(-8,0);s.fighters[1].position=new Vector2(8,0);return s;}
        var sim=Setup();var owner=sim.fighters[0];
        for(int i=0;i<3;i++){owner.skillTimer=0;sim.Skill(owner);}
        Check(sim.robots.Count(r=>r.active&&r.owner==0)==3,"three simultaneous support robots");
        sim.robots[0].remaining=1;owner.charge=owner.data.ultimateRequirement;sim.Ultimate(owner);
        Check(sim.robots.Count(r=>r.active&&r.owner==0)==3&&sim.robots[0].elite&&sim.robots[0].hp==1600,"fourth summon refreshes oldest; combined cap three");
        sim.Skill(sim.fighters[1]);Check(sim.robots.Count(r=>r.active&&r.owner==1)==1&&sim.robots.Count(r=>r.active&&r.owner==0)==3,"per-owner cap leaves other robots intact");
        sim.DamageRobot(1,0,2000);owner.skillTimer=0;sim.Skill(owner);
        Check(sim.robots.Count(r=>r.active&&r.owner==0)==3,"destroyed slot can be summoned again");
        var inputs=new[]{new ArenaSimulation.HumanInput{human=true},new ArenaSimulation.HumanInput{human=true}};
        for(int map=0;map<4;map++){
            sim=Setup(map);owner=sim.fighters[0];var target=sim.fighters[1];int hits=0;sim.Event+=e=>{if(e.type=="hit")hits++;};
            sim.Damage(0,1,10000);Check(!target.Alive&&target.respawn==4,"death schedules respawn");
            sim.TickNetwork(4.01f,inputs);Check(target.Alive&&target.respawnProtection==2,"respawn grants exactly two seconds");
            var state=FighterState.From(target);var copy=new ArenaSimulation.Fighter();state.Apply(copy);Check(copy.respawnProtection==2,"network snapshot preserves invulnerability");
            float hp=target.hp,dealt=owner.damageDealt,charge=owner.charge;int beforeHits=hits;
            sim.Damage(0,1,700);Check(target.hp==hp&&owner.damageDealt==dealt&&owner.charge==charge&&hits==beforeHits,"protected damage gives no HP loss, stats, charge or hit event");
            var before=target.position;
            typeof(ArenaSimulation).GetMethod("Area",BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(sim,new object[]{owner,target.position+new Vector2(.1f,0),2f,600f,true});
            Check(Vector2.Distance(before,target.position)<.001f&&target.hp==hp,"protected explosion cannot knock back");
            var shot=sim.shots[0];shot.active=true;shot.owner=0;shot.position=target.position;shot.direction=Vector2.up;shot.speed=0;shot.remaining=2;shot.damage=300;shot.poison=100;
            sim.TickNetwork(.01f,inputs);Check(target.poison==0&&target.hp==hp,"protected projectile cannot apply poison");
            for(int i=0;i<58;i++)sim.TickNetwork(1f/30,inputs);
            Check(target.respawnProtection>0,"protection persists until two seconds");sim.Damage(0,1,500);Check(target.hp==hp,"protected near expiry");
            for(int i=0;i<3;i++)sim.TickNetwork(1f/30,inputs);
            Check(target.respawnProtection==0,"protection expires");sim.Damage(0,1,500);Check(target.hp==hp-500,"full damage returns after expiry");
        }
        sim=Setup();owner=sim.fighters[0];owner.position=new Vector2(-15,0);sim.fighters[1].position=new Vector2(6,0);
        var elite=sim.robots[0];elite.active=true;elite.elite=true;elite.owner=0;elite.position=Vector2.zero;elite.remaining=15;elite.attackTimer=0;
        sim.TickNetwork(1f/30,inputs);
        Check(MathF.Abs(elite.position.magnitude-.15f)<.002f,"elite moves at 4.5, targeting from robot position instead of distant owner");
        Check(elite.aim.x>.99f&&sim.shots.Any(s=>s.active&&Vector2.Dot(s.direction,elite.aim)>.999f),"authoritative aim equals actual projectile direction");
        foreach(var shot in sim.shots)shot.active=false;sim.fighters[1].position=new Vector2(0,6);elite.attackTimer=0;sim.TickNetwork(1f/30,inputs);
        Check(elite.aim.y>.99f&&sim.shots.Any(s=>s.active&&Vector2.Dot(s.direction,elite.aim)>.999f),"robot immediately tracks changed target direction");
        sim=new ArenaSimulation(roster,9,42,0,1,new[]{9,0});sim.covers.Clear();owner=sim.fighters[0];sim.fighters[1].withdrawn=true;
        sim.Skill(owner);var zone=sim.zones.First(z=>z.active);Check(zone.remaining==4&&zone.damage==170,"poison skill lasts four seconds, damage unchanged");
        sim.TickNetwork(3.99f,inputs);Check(zone.active,"skill remains before four seconds");sim.TickNetwork(.02f,inputs);Check(!zone.active,"skill expires at four seconds");
        owner.charge=owner.data.ultimateRequirement;sim.Ultimate(owner);zone=sim.zones.First(z=>z.active);Check(zone.remaining==5&&zone.damage==220,"poison ultimate lasts five seconds, damage unchanged");
        sim.TickNetwork(4.99f,inputs);Check(zone.active,"ultimate remains before five seconds");sim.TickNetwork(.02f,inputs);Check(!zone.active,"ultimate expires at five seconds");
        Console.WriteLine("COMBAT_REGRESSION_PASS robot cap/respawn; elite direction, targeting and speed 4.5; poison expiry skill=4 ultimate=5");
    }
}
