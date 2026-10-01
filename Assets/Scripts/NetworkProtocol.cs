using System;
using UnityEngine;
namespace PharmaBrawl
{
    [Serializable] public sealed class RoomMember{public string id,nickname;public int slot,hero=-1;public bool host;}
    [Serializable] public sealed class RoomInfo{public string id,name;public int map,teamSize,count;public bool playing;public RoomMember[] members;}
    [Serializable] public sealed class RoomList{public RoomInfo[] rooms;}
    [Serializable] public sealed class RoomJoin{public string roomId,token,playerId;}
    [Serializable] public sealed class NetCommand{public string type,roomId,token;public int hero,map,seq;public float mx,mz,ax,az;public bool attack,skill,ultimate;}
    [Serializable] public sealed class FighterState
    {
        public int hero,kills,deaths;public Vector2 position,aim;
        public float hp,skillTimer,charge,respawn,damageDealt,damageTaken,healing;
        public static FighterState From(ArenaSimulation.Fighter f)=>new FighterState{hero=(int)f.data.kind,kills=f.kills,deaths=f.deaths,position=f.position,aim=f.aim,hp=f.hp,skillTimer=f.skillTimer,charge=f.charge,respawn=f.respawn,damageDealt=f.damageDealt,damageTaken=f.damageTaken,healing=f.healing};
        public void Apply(ArenaSimulation.Fighter f){f.kills=kills;f.deaths=deaths;f.position=position;f.aim=aim;f.hp=hp;f.skillTimer=skillTimer;f.charge=charge;f.respawn=respawn;f.damageDealt=damageDealt;f.damageTaken=damageTaken;f.healing=healing;}
    }
    [Serializable] public sealed class IndexedShot{public int index;public ArenaSimulation.Shot value;}
    [Serializable] public sealed class IndexedZone{public int index;public ArenaSimulation.Zone value;}
    [Serializable] public sealed class IndexedRobot{public int index;public ArenaSimulation.Robot value;}
    [Serializable] public sealed class NetMessage
    {
        public string type,error;public RoomInfo room;public int tick,match;
        public float timeLeft;public int[] score;public bool finished;public int winner;
        public FighterState[] fighters;public IndexedShot[] shots;public IndexedZone[] zones;public IndexedRobot[] robots;public float[] covers;
        public ArenaSimulation.CombatEvent[] events;
    }
}
