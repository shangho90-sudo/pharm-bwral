using UnityEngine;

namespace PharmaBrawl
{
    // Ordered server events define every hop. No client target selection or damage.
    public sealed class LightningAbilityView : MonoBehaviour
    {
        sealed class Arc {public GameObject root;public LineRenderer glow,core,aura;public ParticleSystem sparks;public Vector3 from,to;public float time,duration;public int owner,target;public bool ultimate;}
        readonly Arc[] pool=new Arc[32];
        Vector2[] previous;bool[] chaining,ultimate;
        ArenaSimulation simulation;Material material,sparkMaterial;int cursor;bool visible=true;
        public System.Action<Vector2,bool> Impact;
        public System.Action<Vector2> Ended;
        public int ActiveArcs {get{int count=0;foreach(var arc in pool)if(arc!=null&&arc.time>0)count++;return count;}}
        public Vector3 LastFrom {get;private set;}public Vector3 LastTo {get;private set;}
        static Vector3 World(Vector2 p,float y=1.25f)=>new Vector3(p.x,y,p.y);
        public void Initialize(ArenaSimulation model)
        {
            simulation=model;previous=new Vector2[model.fighters.Length];chaining=new bool[previous.Length];ultimate=new bool[previous.Length];
            material=new Material(Shader.Find("Sprites/Default"));sparkMaterial=MedicalVfx.Material(0);
            for(int i=0;i<pool.Length;i++){
                var root=new GameObject("Pooled chain lightning "+i);root.transform.SetParent(transform,false);
                var arc=new Arc{root=root,glow=Line(root,"Cyan electrical envelope",25),core=Line(root,"White lightning core",25),aura=Line(root,"Target electrical impact",33)};
                var particles=new GameObject("Electrical sparks");particles.transform.SetParent(root.transform,false);arc.sparks=particles.AddComponent<ParticleSystem>();
                var main=arc.sparks.main;main.playOnAwake=false;main.loop=false;main.maxParticles=18;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startLifetime=.24f;main.startSpeed=4;main.startSize=.13f;main.startColor=new ParticleSystem.MinMaxGradient(new Color(.1f,.75f,1),Color.white);
                var emission=arc.sparks.emission;emission.enabled=false;var shape=arc.sparks.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.35f;
                var size=arc.sparks.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,0));
                var renderer=arc.sparks.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=sparkMaterial;renderer.renderMode=ParticleSystemRenderMode.Stretch;renderer.lengthScale=2;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                arc.sparks.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);pool[i]=arc;root.SetActive(false);
            }
        }
        LineRenderer Line(GameObject root,string name,int count){var child=new GameObject(name);child.transform.SetParent(root.transform,false);var line=child.AddComponent<LineRenderer>();line.sharedMaterial=material;line.useWorldSpace=true;line.positionCount=count;line.numCapVertices=2;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return line;}
        public void Emit(ArenaSimulation.CombatEvent e)
        {
            if(!visible)return;
            if(e.type=="death"){ClearActor(e.actor);return;}
            if(simulation.fighters[e.actor].data.kind!=AttackKind.Lightning)return;
            if(e.type=="skill"||e.type=="ultimate"){
                previous[e.actor]=e.position;chaining[e.actor]=true;ultimate[e.actor]=e.type=="ultimate";return;
            }
            if(e.type!="lightning")return;
            bool chain=e.size>1.5f&&chaining[e.actor];
            var arc=pool[cursor++%pool.Length];arc.sparks.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            arc.from=World(chain?previous[e.actor]:simulation.fighters[e.actor].position);arc.to=World(e.position);arc.owner=e.actor;arc.ultimate=chain&&ultimate[e.actor];arc.duration=arc.ultimate?.55f:chain?.36f:.22f;arc.time=arc.duration;
            arc.target=-1;float nearest=.6f;foreach(var target in simulation.fighters){float d=Vector2.Distance(target.position,e.position);if(target.team!=simulation.fighters[e.actor].team&&d<nearest){arc.target=target.id;nearest=d;}}
            arc.root.transform.position=arc.to;arc.root.SetActive(true);arc.sparks.Emit(arc.ultimate?18:10);Draw(arc,0);
            previous[e.actor]=e.position;LastFrom=arc.from;LastTo=arc.to;Impact?.Invoke(e.position,arc.ultimate);
        }
        void Draw(Arc arc,float age)
        {
            float alpha=1-age;var tint=arc.ultimate?new Color(.4f,.55f,1,alpha):new Color(0,.85f,1,alpha);
            arc.glow.startColor=arc.glow.endColor=tint;arc.core.startColor=arc.core.endColor=new Color(.85f,1,1,alpha);
            arc.glow.widthMultiplier=(arc.ultimate?.23f:.13f)*alpha;arc.core.widthMultiplier=(arc.ultimate?.085f:.05f)*alpha;
            Vector3 delta=arc.to-arc.from,side=Vector3.Cross(delta.normalized,Vector3.up);int flicker=Mathf.FloorToInt(age*24);
            for(int i=0;i<25;i++){
                float t=i/24f;float offset=i==0||i==24?0:Mathf.Sin(i*19.17f+flicker*4.9f+arc.owner)*(.14f+(arc.ultimate?.12f:0));
                Vector3 p=Vector3.Lerp(arc.from,arc.to,t)+side*offset+Vector3.up*offset*.65f;arc.glow.SetPosition(i,p);arc.core.SetPosition(i,p);
            }
            // Short electrical impact, not a stun, knockback or area-damage marker.
            Vector3 center=arc.to;if(arc.target>=0&&simulation.fighters[arc.target].Alive)center=World(simulation.fighters[arc.target].position);
            arc.aura.startColor=arc.aura.endColor=arc.ultimate?new Color(.65f,.35f,1,alpha):tint;arc.aura.widthMultiplier=.075f*alpha;
            for(int i=0;i<33;i++){float angle=i*Mathf.PI*2/32;float radius=.6f+Mathf.Sin(i*7+flicker)*.12f;arc.aura.SetPosition(i,center+new Vector3(Mathf.Cos(angle)*radius,Mathf.Sin(angle*3)*.5f,Mathf.Sin(angle)*radius));}
        }
        public void Advance(float dt){if(!visible)return;foreach(var arc in pool)if(arc.time>0){arc.time=Mathf.Max(0,arc.time-dt);if(arc.time==0){arc.root.SetActive(false);arc.sparks.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);Ended?.Invoke(new Vector2(arc.to.x,arc.to.z));}else Draw(arc,1-arc.time/arc.duration);}}
        void Update(){Advance(Time.deltaTime);}
        public void ClearActor(int id){if(previous==null)return;chaining[id]=false;foreach(var arc in pool)if(arc.owner==id||arc.target==id){arc.time=0;arc.root.SetActive(false);arc.sparks.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}}
        public void SetVisible(bool value){if(visible==value)return;if(!value)Clear();else visible=true;}
        public void Clear(){visible=false;if(chaining!=null)System.Array.Clear(chaining,0,chaining.Length);foreach(var arc in pool)if(arc!=null){arc.time=0;arc.root.SetActive(false);arc.sparks.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}}
        void OnDestroy(){if(material)Destroy(material);if(sparkMaterial)Destroy(sparkMaterial);}
    }
}
