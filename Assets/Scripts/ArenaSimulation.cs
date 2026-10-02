using System;
using System.Collections.Generic;
using UnityEngine;

namespace PharmaBrawl
{
    // Fixed-step combat model. Presentation subscribes to events; no GameObjects or physics allocations here.
    public sealed class ArenaSimulation
    {
        public const float Width = 18, Height = 13, MatchDuration = 180;
        public const int TargetScore = 20;
        [Serializable] public sealed class Fighter
        {
            public int id, team, kills, deaths;
            public string nickname;public bool human,withdrawn;
            public CharacterDefinition data;
            public Vector2 position, aim = Vector2.up;
            public float aimDistance=7;
            public float hp, attackTimer, skillTimer, charge, respawn, quiet, shield, haste, poison, poisonTick, poisonDamage, boost;
            public int poisonOwner;
            public bool empowered;
            public int burstRemaining;
            public float burstTimer, rapidRemaining;
            public float damageDealt, damageTaken, healing;
            public bool Alive => hp > 0 && !withdrawn;
        }
        [Serializable] public sealed class Shot
        {
            public bool active, piercing;
            public int owner, kind;
            public Vector2 position, direction;
            public float damage, speed, remaining, radius, poison;
            public int hitMask;
        }
        [Serializable] public sealed class Zone
        {
            public bool active, pending;
            public int owner, kind;
            public Vector2 position;
            public float radius, remaining, tick, damage;
        }
        [Serializable] public sealed class Robot
        {
            public bool active;
            public int owner;
            public Vector2 position;
            public float remaining, attackTimer, hp, maxHp;
            public bool elite;
        }
        [Serializable] public sealed class Cover
        {
            public Vector2 position, size;
            public float hp;
            public bool destructible;
            public bool blocksShots = true;
            public ArenaMap.Feature feature;
            public bool Active => !destructible || hp > 0;
        }
        [Serializable] public struct CombatEvent
        {
            public string type;
            public Vector2 position;
            public int actor;
            public float size;
            public CombatEvent(string t, Vector2 p, int a, float s = 1) { type=t; position=p; actor=a; size=s; }
        }
        public readonly Fighter[] fighters;
        public readonly int TeamSize;
        public struct HumanInput { public bool human,attack,skill,ultimate; public Vector2 move,aim; }
        HumanInput[] networkInputs;
        public readonly Shot[] shots = new Shot[256];
        public readonly Zone[] zones = new Zone[48];
        public readonly Robot[] robots = new Robot[12];
        public readonly List<Cover> covers = new List<Cover>();
        public readonly int[] score = new int[2];
        public event Action<CombatEvent> Event;
        public float timeLeft = MatchDuration;
        public bool finished;
        public int winner = -1;
        public int shotsFired, skillsUsed, ultimatesUsed, respawns;
        readonly System.Random rng;
        public readonly ArenaMap map;
        readonly ArenaNavigation navigation;

        public ArenaSimulation(CharacterDefinition[] roster, int selected, int seed = 42, int mapIndex = 0, int teamSize = 3, int[] heroPicks = null)
        {
            TeamSize=Mathf.Clamp(teamSize,1,4);fighters=new Fighter[TeamSize*2];
            rng = new System.Random(seed);
            map = new ArenaMap(mapIndex);
            int[] picks = heroPicks ?? new[]{selected,(selected+3)%10,(selected+6)%10,(selected+1)%10,(selected+4)%10,(selected+8)%10,(selected+2)%10,(selected+5)%10};
            for(int i=0;i<fighters.Length;i++) fighters[i] = new Fighter {id=i, team=i<TeamSize?0:1, data=roster[picks[i]], hp=roster[picks[i]].maxHp, position=map.Spawn(i,TeamSize)};
            for(int i=0;i<shots.Length;i++) shots[i]=new Shot();
            for(int i=0;i<zones.Length;i++) zones[i]=new Zone();
            for(int i=0;i<robots.Length;i++) robots[i]=new Robot();
            foreach(var f in map.features) if(f.blocksMovement) covers.Add(new Cover{position=f.position,size=f.size,destructible=f.breakable,hp=1800,blocksShots=f.blocksShots,feature=f});
            navigation = new ArenaNavigation(this);
        }
        public static Vector2 Spawn(int id) => new Vector2((id%3-1)*3.2f, id<3?-11.1f:11.1f);
        void Emit(string t, Vector2 p, int actor, float size=1) => Event?.Invoke(new CombatEvent(t,p,actor,size));
        public void Tick(float dt, Vector2 move, Vector2 aim, bool attack, bool skill, bool ultimate, bool allBots=false)
        {
            if(finished) return;
            timeLeft=Mathf.Max(0,timeLeft-dt);
            for(int i=0;i<fighters.Length;i++)
            {
                var f=fighters[i];
                if(f.withdrawn)continue;
                if(!f.Alive) { f.respawn-=dt; if(f.respawn<=0) {f.hp=f.data.maxHp;f.position=map.Spawn(i,TeamSize);f.quiet=0;f.poison=0;f.shield=1;respawns++;Emit("respawn",f.position,i);} continue; }
                f.attackTimer-=dt; f.skillTimer-=dt; f.shield-=dt; f.haste-=dt; f.boost-=dt; f.quiet+=dt;
                if(f.poison>0) {f.poison-=dt;f.poisonTick-=dt;if(f.poisonTick<=0){f.poisonTick=.5f;Damage(f.poisonOwner,i,f.poisonDamage,false);}}
                if(!f.Alive) continue;
                if(f.burstRemaining>0){f.burstTimer-=dt;if(f.burstTimer<=0){Fire(f,f.aim,f.data.damage,f.data.range,0);f.burstRemaining--;f.burstTimer=.09f;Emit("shoot",f.position,f.id,.3f);}}
                if(f.rapidRemaining>0){f.rapidRemaining-=dt;Attack(f);}
                if(f.quiet>3.5f && f.hp<f.data.maxHp) {float healed=Mathf.Min(f.data.maxHp-f.hp, f.data.maxHp*.13f*dt);f.hp+=healed;f.healing+=healed;}
                if(networkInputs!=null && networkInputs[i].human){var input=networkInputs[i];Move(f,input.move,dt);if(input.aim.sqrMagnitude>.01f){f.aim=input.aim.normalized;f.aimDistance=Mathf.Clamp(input.aim.magnitude,.8f,f.data.range);}if(input.attack)Attack(f);if(input.skill)Skill(f);if(input.ultimate)Ultimate(f);}
                else if(i==0 && !allBots) { Move(f,move,dt); if(aim.sqrMagnitude>.01f) {f.aim=aim.normalized;f.aimDistance=Mathf.Clamp(aim.magnitude,.8f,f.data.range);} if(attack) Attack(f); if(skill) Skill(f); if(ultimate) Ultimate(f); }
                else Bot(f,dt);
            }
            UpdateShots(dt); UpdateZones(dt); UpdateRobots(dt);
            if(timeLeft<=0 || score[0]>=TargetScore || score[1]>=TargetScore) { finished=true;winner=score[0]==score[1]?-1:(score[0]>score[1]?0:1);Emit("finish",Vector2.zero,0); }
        }
        public void TickNetwork(float dt,HumanInput[] inputs){networkInputs=inputs;try{Tick(dt,Vector2.zero,Vector2.up,false,false,false,true);}finally{networkInputs=null;}}
        public void Withdraw(int id){var f=fighters[id];f.withdrawn=true;f.hp=0;f.burstRemaining=0;f.rapidRemaining=f.poison=f.shield=f.haste=0;foreach(var s in shots)if(s.owner==id)s.active=false;foreach(var z in zones)if(z.owner==id)z.active=false;foreach(var r in robots)if(r.owner==id)r.active=false;Emit("withdraw",f.position,id);}
        public bool Blocked(Vector2 p,float radius=.48f)
        {
            if(Mathf.Abs(p.x)>Width-radius || Mathf.Abs(p.y)>Height-radius) return true;
            foreach(var c in covers) if(c.Active && Mathf.Abs(p.x-c.position.x)<c.size.x*.5f+radius && Mathf.Abs(p.y-c.position.y)<c.size.y*.5f+radius) return true;
            return false;
        }
        void Move(Fighter f,Vector2 direction,float dt)
        {
            float slow=1;
            foreach(var z in zones) if(z.active && !z.pending && z.kind==2 && fighters[z.owner].team!=f.team && Vector2.Distance(z.position,f.position)<z.radius) slow=.65f;
            Vector2 d=Vector2.ClampMagnitude(direction,1)*f.data.speed*slow*dt;
            int steps=Mathf.Max(1,Mathf.CeilToInt(d.magnitude/.2f));d/=steps;
            for(int step=0;step<steps;step++){
                var next=f.position+new Vector2(d.x,0);if(!Blocked(next))f.position=next;
                next=f.position+new Vector2(0,d.y);if(!Blocked(next))f.position=next;
            }
        }
        public int NearestEnemy(Fighter f,float range=100)
        {
            int best=-1;float distance=range;
            for(int i=0;i<fighters.Length;i++) {var e=fighters[i];if(!e.Alive || e.team==f.team)continue;float d=Vector2.Distance(f.position,e.position);if(d<distance){best=i;distance=d;}}
            return best;
        }
        bool LineClear(Vector2 a,Vector2 b)
        {
            float length=Vector2.Distance(a,b); int count=Mathf.CeilToInt(length/.35f);
            for(int i=1;i<count;i++) if(ShotBlocked(Vector2.Lerp(a,b,(float)i/count),.08f))return false;
            return true;
        }
        public bool ShotBlocked(Vector2 p,float radius=.08f)
        {
            foreach(var c in covers)if(c.Active && c.blocksShots && Mathf.Abs(p.x-c.position.x)<c.size.x*.5f+radius && Mathf.Abs(p.y-c.position.y)<c.size.y*.5f+radius)return true;
            return false;
        }
        public Vector2 Navigate(Vector2 start,Vector2 goal,int id,float dt) => navigation.Direction(start,goal,id,dt);
        void Bot(Fighter f,float dt)
        {
            int target=NearestEnemy(f); if(target<0)return;
            var e=fighters[target]; Vector2 delta=e.position-f.position; float distance=delta.magnitude;
            f.aim=(delta+e.aim*.2f).normalized;f.aimDistance=Mathf.Min(distance,f.data.range);
            float desired=Mathf.Clamp(f.data.range*.65f,2,7);
            Vector2 movement=distance>desired?delta.normalized:distance<desired-1?-delta.normalized:Vector2.Perpendicular(delta).normalized*Mathf.Sin(timeLeft+f.id);
            if(f.hp<f.data.maxHp*.28f)movement=-delta.normalized;
            foreach(var z in zones)if(z.active && fighters[z.owner].team!=f.team && Vector2.Distance(f.position,z.position)<z.radius+1)movement=(f.position-z.position).normalized;
            if(Blocked(f.position+movement*.8f))
            {
                // Choose a local detour with deterministic side preference; diagonal alternatives prevent cover deadlocks.
                Vector2 left=Vector2.Perpendicular(movement),right=-left;
                if(!Blocked(f.position+left*.8f))movement=left;
                else if(!Blocked(f.position+right*.8f))movement=right;
                else movement=(new Vector2((float)rng.NextDouble()-.5f,(float)rng.NextDouble()-.5f)).normalized;
            }
            // Route to enemies through open corridors and bridges, even when a river is still distant.
            if(distance>desired || !LineClear(f.position,e.position)) movement=Navigate(f.position,e.position,f.id,dt);
            Move(f,movement,dt);
            bool clear=f.data.kind==AttackKind.Artillery || LineClear(f.position,e.position);
            if(distance<f.data.range && clear)Attack(f);
            if(distance<f.data.range+1 && f.skillTimer<=0 && clear)Skill(f);
            if(f.charge>=f.data.ultimateRequirement && distance<f.data.range+2)Ultimate(f);
        }
        Vector2 TargetPoint(Fighter f,float range) => f.position+f.aim*Mathf.Min(range,f.aimDistance);
        public void Attack(Fighter f)
        {
            if(!f.Alive || f.attackTimer>0 || finished)return;
            f.attackTimer=f.data.attackInterval*(f.haste>0?.45f:1);f.quiet=0;
            float dmg=f.data.damage*(f.boost>0?1.5f:1);
            switch(f.data.kind)
            {
                case AttackKind.Artillery: MakeZone(f,TargetPoint(f,f.data.range),1.65f,.3f,dmg,0,true); break;
                case AttackKind.Wave: Cone(f,3.8f,55,dmg);break;
                case AttackKind.Burst: Fire(f,f.aim,dmg,f.data.range,0);f.burstRemaining=2;f.burstTimer=.09f;break;
                case AttackKind.Fan: for(int i=-2;i<=2;i++)Fire(f,Rotate(f.aim,i*(f.empowered?16:9)),dmg,f.empowered?f.data.range*1.4f:f.data.range,0);f.empowered=false;break;
                default: Fire(f,f.aim,dmg,f.data.range,f.data.kind==AttackKind.Poison?100:0,f.empowered);f.empowered=false;break;
            }
            Emit("shoot",f.position,f.id,.3f);
        }
        public void Skill(Fighter f)
        {
            if(!f.Alive || f.skillTimer>0 || finished)return;
            f.skillTimer=f.data.skillCooldown;skillsUsed++;Emit("skill",f.position,f.id,1);
            switch(f.data.kind)
            {
                case AttackKind.Capsule: Fire(f,f.aim,600,9,0,false,1);break;
                case AttackKind.Lightning: Chain(f,2,500,10);break;
                case AttackKind.Arrow: f.empowered=true;break;
                case AttackKind.Wave: f.shield=4;break;
                case AttackKind.Burst: f.haste=4;break;
                case AttackKind.Artillery: MakeZone(f,TargetPoint(f,8),2,5,150,1);break;
                case AttackKind.Assassin: Teleport(f,f.position+f.aim*4);break;
                case AttackKind.Summoner: Summon(f,false);break;
                case AttackKind.Fan: f.empowered=true;break;
                case AttackKind.Poison: MakeZone(f,TargetPoint(f,7),2.5f,5,170,2);break;
            }
        }
        public void Ultimate(Fighter f)
        {
            if(!f.Alive || f.charge<f.data.ultimateRequirement || finished)return;
            f.charge=0;ultimatesUsed++;Emit("ultimate",f.position,f.id,3);
            switch(f.data.kind)
            {
                case AttackKind.Capsule: MakeZone(f,TargetPoint(f,8),3.5f,.9f,1700,3,true);break;
                case AttackKind.Lightning: Chain(f,6,850,12);break;
                case AttackKind.Arrow: Beam(f);break;
                case AttackKind.Wave:
                    for(int j=0;j<16;j++){Vector2 p=f.position+f.aim*.45f;if(Blocked(p))break;f.position=p;Area(f,p,1.5f,180,true);}break;
                case AttackKind.Burst: f.haste=3.5f;f.rapidRemaining=3.5f;f.attackTimer=0;for(int j=-6;j<=6;j++)Fire(f,Rotate(f.aim,j*3),180,12,0);break;
                case AttackKind.Artillery:
                    Vector2 center=TargetPoint(f,8);for(int j=0;j<7;j++)MakeZone(f,center+Rotate(Vector2.up,j*51)*((j%3)*1.7f),1.9f,.4f+j*.18f,650,0,true);break;
                case AttackKind.Assassin:
                    int target=NearestEnemy(f,11);if(target>=0){Teleport(f,fighters[target].position-fighters[target].aim*1.1f);Area(f,f.position,2,1800,false);}break;
                case AttackKind.Summoner: Summon(f,true);break;
                case AttackKind.Fan: for(int j=-8;j<=8;j++)Fire(f,Rotate(f.aim,j*5),300,12,0);break;
                case AttackKind.Poison: MakeZone(f,TargetPoint(f,8),4.5f,8,220,2);break;
            }
        }
        void Teleport(Fighter f,Vector2 p) {for(int j=10;j>0;j--){var n=Vector2.Lerp(f.position,p,j/10f);if(!Blocked(n)){Emit("blink",f.position,f.id);f.position=n;Emit("blink",n,f.id);break;}}}
        void Summon(Fighter f,bool elite){foreach(var r in robots)if(!r.active){r.active=true;r.owner=f.id;r.position=f.position;for(int j=0;j<8;j++){var p=f.position+Rotate(Vector2.up,j*45)*.8f;if(!Blocked(p,.3f)){r.position=p;break;}}r.remaining=elite?15:10;r.elite=elite;r.hp=r.maxHp=elite?1600:800;r.attackTimer=.3f;return;}}
        public void DamageRobot(int owner,int index,float amount)
        {
            var r=robots[index];if(!r.active||fighters[owner].team==fighters[r.owner].team)return;
            r.hp=Mathf.Max(0,r.hp-amount);Emit("hit",r.position,r.owner,.6f);
            if(r.hp<=0){r.active=false;Emit("robotDestroyed",r.position,r.owner,r.elite?1.2f:.8f);}
        }
        public static Vector2 Rotate(Vector2 p,float degrees){float r=degrees*Mathf.Deg2Rad;return new Vector2(p.x*Mathf.Cos(r)-p.y*Mathf.Sin(r),p.x*Mathf.Sin(r)+p.y*Mathf.Cos(r));}
        void Fire(Fighter f,Vector2 direction,float damage,float range,float poison,bool piercing=false,int kind=0)
        {
            foreach(var s in shots)if(!s.active){s.active=true;s.owner=f.id;s.position=f.position+direction*.6f;s.direction=direction;s.damage=damage;s.speed=f.data.projectileSpeed;s.remaining=range;s.poison=poison;s.piercing=piercing;s.kind=kind;s.radius=kind==4?.45f:.23f;s.hitMask=0;shotsFired++;return;}
        }
        void MakeZone(Fighter f,Vector2 p,float radius,float duration,float damage,int kind,bool pending=false)
        {
            foreach(var z in zones)if(!z.active){z.active=true;z.owner=f.id;z.position=p;z.radius=radius;z.remaining=duration;z.damage=damage;z.kind=kind;z.pending=pending;z.tick=0;return;}
        }
        void Cone(Fighter f,float range,float angle,float damage){for(int i=0;i<fighters.Length;i++){var e=fighters[i];if(e.Alive && e.team!=f.team && Vector2.Distance(f.position,e.position)<range && Vector2.Angle(f.aim,e.position-f.position)<angle && LineClear(f.position,e.position))Damage(f.id,i,damage);}for(int i=0;i<robots.Length;i++){var r=robots[i];if(r.active&&Vector2.Distance(f.position,r.position)<range&&Vector2.Angle(f.aim,r.position-f.position)<angle&&LineClear(f.position,r.position))DamageRobot(f.id,i,damage);}Emit("wave",f.position+f.aim*1.6f,f.id,2);}
        void Beam(Fighter f){for(int i=0;i<fighters.Length;i++){var e=fighters[i];Vector2 delta=e.position-f.position;float forward=Vector2.Dot(delta,f.aim);if(e.Alive && e.team!=f.team && forward>0 && forward<35 && Mathf.Abs(Vector2.Dot(delta,Vector2.Perpendicular(f.aim)))<.8f)Damage(f.id,i,1900);}for(int i=0;i<robots.Length;i++){var r=robots[i];Vector2 delta=r.position-f.position;float forward=Vector2.Dot(delta,f.aim);if(r.active&&forward>0&&forward<35&&Mathf.Abs(Vector2.Dot(delta,Vector2.Perpendicular(f.aim)))<.8f)DamageRobot(f.id,i,1900);}Emit("beam",f.position,f.id,35);}
        void Chain(Fighter f,int jumps,float damage,float range)
        {
            Vector2 point=f.position;int mask=0;
            for(int j=0;j<jumps;j++){int target=-1;float closest=range;for(int i=0;i<fighters.Length;i++){var e=fighters[i];float d=Vector2.Distance(point,e.position);if(e.Alive && e.team!=f.team && (mask&(1<<i))==0 && d<closest){target=i;closest=d;}}
                if(target<0){int robot=-1;for(int i=0;i<robots.Length;i++){var r=robots[i];float d=Vector2.Distance(point,r.position);if(r.active&&fighters[r.owner].team!=f.team&&(mask&(1<<(8+i)))==0&&d<closest){robot=i;closest=d;}}if(robot<0)break;mask|=1<<(8+robot);point=robots[robot].position;DamageRobot(f.id,robot,damage);}
                else{mask|=1<<target;Damage(f.id,target,damage);point=fighters[target].position;}Emit("lightning",point,f.id,1.7f);range=6;}
        }
        void Area(Fighter f,Vector2 p,float radius,float damage,bool knockback)
        {
            for(int i=0;i<fighters.Length;i++){var e=fighters[i];if(e.Alive && e.team!=f.team && Vector2.Distance(e.position,p)<radius){Damage(f.id,i,damage);if(knockback){Vector2 n=e.position+(e.position-p).normalized*1.2f;if(!Blocked(n))e.position=n;}}}
            foreach(var c in covers)if(c.destructible && c.Active && Vector2.Distance(c.position,p)<radius+1)c.hp-=damage;
            for(int i=0;i<robots.Length;i++)if(robots[i].active&&Vector2.Distance(robots[i].position,p)<radius)DamageRobot(f.id,i,damage);
        }
        public void Damage(int owner,int target,float amount,bool charge=true)
        {
            var f=fighters[owner];var e=fighters[target];if(!e.Alive || f.team==e.team)return;
            amount=Mathf.Min(e.hp,amount*(e.shield>0?.45f:1));e.hp-=amount;e.quiet=0;e.damageTaken+=amount;f.damageDealt+=amount;
            if(charge)f.charge=Mathf.Min(f.data.ultimateRequirement,f.charge+amount);
            Emit("hit",e.position,target,.6f);
            if(e.hp<=0){e.hp=0;e.deaths++;f.kills++;e.respawn=4;e.poison=0;e.shield=0;e.haste=0;e.boost=0;e.empowered=false;e.burstRemaining=0;e.rapidRemaining=0;score[f.team]++;Emit("death",e.position,target,1.7f);}
        }
        void UpdateShots(float dt)
        {
            foreach(var s in shots)if(s.active)
            {
                // Substeps avoid tunnelling at low render rates; simulation itself runs at 60 Hz.
                int steps=Mathf.Max(1,Mathf.CeilToInt(s.speed*dt/.25f));float step=s.speed*dt/steps;
                for(int j=0;j<steps && s.active;j++)
                {
                    s.position+=s.direction*step;s.remaining-=step;
                    if(s.remaining<=0 || Mathf.Abs(s.position.x)>Width || Mathf.Abs(s.position.y)>Height){s.active=false;break;}
                    if(!s.piercing)foreach(var c in covers)if(c.Active && c.blocksShots && Mathf.Abs(s.position.x-c.position.x)<c.size.x*.5f+s.radius && Mathf.Abs(s.position.y-c.position.y)<c.size.y*.5f+s.radius){if(c.destructible)c.hp-=s.damage;Impact(s);break;}
                    if(!s.active)break;
                    for(int i=0;i<robots.Length&&s.active;i++){var r=robots[i];int bit=1<<(8+i);if(r.active&&fighters[r.owner].team!=fighters[s.owner].team&&(s.hitMask&bit)==0&&Vector2.Distance(r.position,s.position)<(r.elite?.65f:.45f)+s.radius){s.hitMask|=bit;DamageRobot(s.owner,i,s.damage);if(!s.piercing)Impact(s);}}
                    if(!s.active)break;
                    for(int i=0;i<fighters.Length;i++){var e=fighters[i];if(e.Alive && e.team!=fighters[s.owner].team && (s.hitMask&(1<<i))==0 && Vector2.Distance(e.position,s.position)<.5f+s.radius){s.hitMask|=1<<i;Damage(s.owner,i,s.damage);
                        if(s.poison>0 && e.Alive){e.poison=2.5f;e.poisonTick=.5f;e.poisonDamage=s.poison;e.poisonOwner=s.owner;}
                        if(fighters[s.owner].data.kind==AttackKind.Lightning && s.kind==0){Emit("lightning",e.position,s.owner);}
                        if(!s.piercing){Impact(s);break;}}
                    }
                }
            }
        }
        void Impact(Shot s){s.active=false;if(s.kind==1){Area(fighters[s.owner],s.position,1.6f,300,false);Emit("explosion",s.position,s.owner,1.6f);}}
        void UpdateZones(float dt)
        {
            foreach(var z in zones)if(z.active){z.remaining-=dt;if(z.pending){if(z.remaining<=0){Area(fighters[z.owner],z.position,z.radius,z.damage,z.kind==3);Emit("explosion",z.position,z.owner,z.radius);z.active=false;}}else {z.tick-=dt;if(z.tick<=0){z.tick=.5f;Area(fighters[z.owner],z.position,z.radius,z.damage,false);}if(z.remaining<=0)z.active=false;}}
        }
        void UpdateRobots(float dt)
        {
            foreach(var r in robots)if(r.active){r.remaining-=dt;if(r.remaining<=0){r.active=false;continue;}var f=fighters[r.owner];int t=NearestEnemy(f,15);if(t<0)continue;var delta=fighters[t].position-r.position;
                if(delta.magnitude>5){Vector2 direction=Navigate(r.position,fighters[t].position,fighters.Length+System.Array.IndexOf(robots,r),dt);Vector2 p=r.position+direction*3*dt;if(!Blocked(p,.3f))r.position=p;}r.attackTimer-=dt;
                if(r.attackTimer<=0 && delta.magnitude<9 && LineClear(r.position,fighters[t].position)){r.attackTimer=r.elite?.35f:.8f;Vector2 old=f.position;f.position=r.position;Fire(f,delta.normalized,r.elite?240:160,9,0);f.position=old;Emit("shoot",r.position,f.id);}}
        }
    }
}
