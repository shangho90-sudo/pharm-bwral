using UnityEngine;
namespace PharmaBrawl
{
    // Beam events are instantaneous server attacks; fading light adds no damage ticks.
    public sealed class GoldenAbilityView:MonoBehaviour
    {
        sealed class Beam{public GameObject root;public LineRenderer outer,core,left,right;public float time;public int owner;public Vector3 start,end;}
        readonly Beam[] beams=new Beam[8];LineRenderer[] charges;ArenaSimulation simulation;Material gold;int cursor;bool visible=true;
        public int ActiveBeams{get{int n=0;foreach(var beam in beams)if(beam!=null&&beam.time>0)n++;return n;}}
        public float LastLength{get;private set;}public float LastHalfWidth=>.8f;
        public void Initialize(ArenaSimulation model){simulation=model;gold=new Material(Shader.Find("Sprites/Default"));charges=new LineRenderer[model.fighters.Length];for(int i=0;i<charges.Length;i++)charges[i]=Line(gameObject,"Piercing round ready "+i,33);
            for(int i=0;i<beams.Length;i++){var root=new GameObject("Pooled golden laser "+i);root.transform.SetParent(transform,false);beams[i]=new Beam{root=root,outer=Line(root,"Exact 1.6 wide laser",2),core=Line(root,"White gold core",2),left=Line(root,"Left damage boundary",2),right=Line(root,"Right damage boundary",2)};root.SetActive(false);}}
        LineRenderer Line(GameObject root,string name,int count){var child=new GameObject(name);child.transform.SetParent(root.transform,false);var line=child.AddComponent<LineRenderer>();line.sharedMaterial=gold;line.useWorldSpace=true;line.positionCount=count;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;line.enabled=false;return line;}
        public void Emit(ArenaSimulation.CombatEvent e){if(!visible)return;if(e.type=="death"){foreach(var oldBeam in beams)if(oldBeam.owner==e.actor){oldBeam.time=0;oldBeam.root.SetActive(false);}charges[e.actor].enabled=false;return;}if(e.type!="beam"||simulation.fighters[e.actor].data.kind!=AttackKind.Arrow)return;
            var beam=beams[cursor++%beams.Length];beam.owner=e.actor;beam.time=.45f;beam.start=new Vector3(e.position.x,1.2f,e.position.y);var direction=simulation.fighters[e.actor].aim;beam.end=beam.start+new Vector3(direction.x,0,direction.y)*e.size;LastLength=e.size;beam.root.SetActive(true);Draw(beam);}
        void Draw(Beam beam){float alpha=beam.time/.45f;Vector3 side=Vector3.Cross((beam.end-beam.start).normalized,Vector3.up)*.8f;
            Set(beam.outer,beam.start,beam.end,1.6f,new Color(1,.65f,.05f,.4f*alpha));Set(beam.core,beam.start,beam.end,.18f*alpha,new Color(1,1,.8f,alpha));
            beam.left.enabled=beam.right.enabled=false;}
        static void Set(LineRenderer line,Vector3 a,Vector3 b,float width,Color color){line.enabled=true;line.SetPosition(0,a);line.SetPosition(1,b);line.widthMultiplier=width;line.startColor=line.endColor=color;}
        public void Advance(float dt){if(!visible)return;foreach(var beam in beams)if(beam.time>0){beam.time=Mathf.Max(0,beam.time-dt);if(beam.time==0)beam.root.SetActive(false);else Draw(beam);}for(int i=0;i<charges.Length;i++){
                var fighter=simulation.fighters[i];var ring=charges[i];ring.enabled=false;if(!ring.enabled)continue;
                Vector3 forward=new Vector3(fighter.aim.x,0,fighter.aim.y),side=Vector3.Cross(forward,Vector3.up);Vector3 center=new Vector3(fighter.position.x,1.35f,fighter.position.y)+forward*.85f;
                ring.widthMultiplier=.055f;ring.startColor=ring.endColor=new Color(1,.85f,.2f,.85f);for(int j=0;j<33;j++){float angle=j*Mathf.PI*2/32;ring.SetPosition(j,center+(side*Mathf.Cos(angle)+Vector3.up*Mathf.Sin(angle))*.28f);}
            }}
        void Update(){Advance(Time.deltaTime);}public void SetVisible(bool value){if(visible==value)return;if(!value)Clear();else visible=true;}
        public void Clear(){visible=false;foreach(var beam in beams)if(beam!=null){beam.time=0;beam.root.SetActive(false);}if(charges!=null)foreach(var ring in charges)ring.enabled=false;}
        void OnDestroy(){if(gold)Destroy(gold);}
    }
}
