using UnityEngine;
namespace PharmaBrawl
{
    // Only artillery gets landing-point markers; no general direction/range overlays.
    public sealed class AbilityEffects:MonoBehaviour
    {
        ArenaSimulation model;int localPlayer;Material glow;LineRenderer aim;
        readonly LineRenderer[] landings=new LineRenderer[48];
        public void Initialize(ArenaSimulation sim,int local=0){model=sim;localPlayer=local;glow=new Material(Shader.Find("Sprites/Default"));aim=Marker("Artillery aim point");for(int i=0;i<landings.Length;i++)landings[i]=Marker("Artillery landing point "+i);}
        LineRenderer Marker(string name){var g=new GameObject(name);g.transform.SetParent(transform,false);var line=g.AddComponent<LineRenderer>();line.sharedMaterial=glow;line.positionCount=17;line.widthMultiplier=.07f;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.enabled=false;return line;}
        static void Point(LineRenderer line,Vector2 position,Color color){line.enabled=true;line.startColor=line.endColor=color;for(int i=0;i<17;i++){float a=i*Mathf.PI*2/16;line.SetPosition(i,new Vector3(position.x+Mathf.Cos(a)*.38f,.24f,position.y+Mathf.Sin(a)*.38f));}}
        public void Emit(ArenaSimulation.CombatEvent e){}
        void Update(){if(model==null)return;var f=model.fighters[localPlayer];aim.enabled=f.Alive&&f.data.kind==AttackKind.Artillery;if(aim.enabled)Point(aim,f.position+f.aim*Mathf.Min(f.aimDistance,f.data.range),new Color(.15f,.85f,1,.9f));for(int i=0;i<landings.Length;i++){var z=model.zones[i];var line=landings[i];line.enabled=z.active&&z.pending&&model.fighters[z.owner].data.kind==AttackKind.Artillery;if(line.enabled)Point(line,z.position,new Color(1,.65f,.12f,.95f));}}
        public void Clear(){enabled=false;if(aim)aim.enabled=false;foreach(var line in landings)if(line)line.enabled=false;}
        void OnDestroy(){if(glow)Destroy(glow);}
    }
}
