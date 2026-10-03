namespace UnityEngine
{
    public struct Vector2
    {
        public float x,y;
        public Vector2(float x,float y){this.x=x;this.y=y;}
        public static Vector2 zero=>new(0,0);public static Vector2 up=>new(0,1);
        public float sqrMagnitude=>x*x+y*y;public float magnitude=>MathF.Sqrt(sqrMagnitude);
        public Vector2 normalized=>magnitude>1e-5f?this/magnitude:zero;
        public void Normalize(){this=normalized;}
        public static Vector2 operator +(Vector2 a,Vector2 b)=>new(a.x+b.x,a.y+b.y);
        public static Vector2 operator -(Vector2 a,Vector2 b)=>new(a.x-b.x,a.y-b.y);
        public static Vector2 operator -(Vector2 a)=>new(-a.x,-a.y);
        public static Vector2 operator *(Vector2 a,float s)=>new(a.x*s,a.y*s);
        public static Vector2 operator *(float s,Vector2 a)=>a*s;
        public static Vector2 operator /(Vector2 a,float s)=>new(a.x/s,a.y/s);
        public static float Dot(Vector2 a,Vector2 b)=>a.x*b.x+a.y*b.y;
        public static float Distance(Vector2 a,Vector2 b)=>(a-b).magnitude;
        public static Vector2 Lerp(Vector2 a,Vector2 b,float t)=>a+(b-a)*Math.Clamp(t,0,1);
        public static Vector2 ClampMagnitude(Vector2 a,float max)=>a.magnitude>max?a.normalized*max:a;
        public static Vector2 Perpendicular(Vector2 a)=>new(-a.y,a.x);
        public static float Angle(Vector2 a,Vector2 b){float d=a.magnitude*b.magnitude;return d<1e-8f?0:MathF.Acos(Math.Clamp(Dot(a,b)/d,-1,1))*180/MathF.PI;}
    }
    public struct Vector4{public float x,y,z,w;public Vector4(float x,float y,float z,float w){this.x=x;this.y=y;this.z=z;this.w=w;}}
    public static class Mathf
    {
        public const float PI=MathF.PI,Deg2Rad=MathF.PI/180;
        public static float Abs(float x)=>MathF.Abs(x);public static float Sin(float x)=>MathF.Sin(x);public static float Cos(float x)=>MathF.Cos(x);
        public static float Min(float a,float b)=>MathF.Min(a,b);public static float Max(float a,float b)=>MathF.Max(a,b);
        public static int Max(int a,int b)=>Math.Max(a,b);
        public static float Clamp(float x,float a,float b)=>Math.Clamp(x,a,b);public static int Clamp(int x,int a,int b)=>Math.Clamp(x,a,b);
        public static int CeilToInt(float x)=>(int)MathF.Ceiling(x);public static int RoundToInt(float x)=>(int)MathF.Round(x);
    }
}
namespace PharmaBrawl
{
    public enum AttackKind{Capsule,Lightning,Arrow,Wave,Burst,Artillery,Assassin,Summoner,Fan,Poison}
    public sealed class CharacterDefinition
    {
        public AttackKind kind;
        public float maxHp,speed,damage,attackInterval,range,projectileSpeed,skillCooldown,ultimateRequirement;
    }
}
