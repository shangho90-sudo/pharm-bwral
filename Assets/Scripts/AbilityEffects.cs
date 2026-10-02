using UnityEngine;
namespace PharmaBrawl
{
    // Fixed pools keep ability presentation independent of simulation and avoid
    // instantiation spikes on mobile. Each family has its own silhouette.
    public sealed class AbilityEffects:MonoBehaviour
    {
        sealed class Effect {public GameObject root;public LineRenderer line;public ParticleSystem sparks;public Transform emblem;public float remaining,duration,radius,emblemScale;public int actor,kind;public bool ultimate,follow;public Vector3 position,aim,emblemOffset;public Color color;}
        readonly Effect[] pool=new Effect[24];Material glow;int cursor;
        ArenaSimulation model;LineRenderer targetMarker;int localPlayer;
        static Vector3 P(Vector2 p,float y=.35f)=>new Vector3(p.x,y,p.y);
        public void Initialize(ArenaSimulation sim,int local=0){model=sim;localPlayer=local;glow=new Material(Shader.Find("Sprites/Default"));var prefab=Resources.Load<GameObject>("Effects/MedicalBurst");
            var marker=new GameObject("Artillery landing aim");marker.transform.SetParent(transform,false);targetMarker=marker.AddComponent<LineRenderer>();targetMarker.sharedMaterial=glow;targetMarker.positionCount=49;targetMarker.widthMultiplier=.065f;targetMarker.startColor=targetMarker.endColor=new Color(.2f,.8f,1,.8f);targetMarker.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            for(int i=0;i<pool.Length;i++){
                var root=new GameObject("Pooled ability "+i);root.transform.SetParent(transform,false);
                var line=root.AddComponent<LineRenderer>();line.sharedMaterial=glow;line.useWorldSpace=true;line.numCapVertices=3;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                var particles=new GameObject("Ability sparks").AddComponent<ParticleSystem>();particles.transform.SetParent(root.transform,false);
                var main=particles.main;main.playOnAwake=false;main.loop=false;main.maxParticles=72;main.simulationSpace=ParticleSystemSimulationSpace.World;main.startLifetime=.65f;main.startSize=.13f;main.startSpeed=4;
                var emission=particles.emission;emission.enabled=false;var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.2f;
                var renderer=particles.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=glow;renderer.renderMode=ParticleSystemRenderMode.Stretch;renderer.lengthScale=2;renderer.velocityScale=.08f;
                Transform emblem=null;if(prefab){emblem=Instantiate(prefab,root.transform).transform;var bounds=PharmacistModelRig.BoundsOf(emblem);float scale=1.5f/Mathf.Max(.01f,bounds.size.magnitude);emblem.localScale=Vector3.one*scale;emblem.localPosition=-bounds.center*scale;}
                pool[i]=new Effect{root=root,line=line,sparks=particles,emblem=emblem,emblemScale=emblem?emblem.localScale.x:1,emblemOffset=emblem?emblem.localPosition:Vector3.zero};root.SetActive(false);
            }
        }
        public void Emit(ArenaSimulation.CombatEvent evt){
            // Capsule has its own reference-based muzzle/impact presentation.
            if(model.fighters[evt.actor].data.kind==AttackKind.Capsule)return;
            bool skill=evt.type=="skill",ultimate=evt.type=="ultimate";
            if(!skill&&!ultimate&&evt.type!="wave"&&evt.type!="lightning"&&evt.type!="explosion"&&evt.type!="blink")return;
            var f=model.fighters[evt.actor];var e=pool[cursor++%pool.Length];e.actor=evt.actor;e.kind=(int)f.data.kind;e.ultimate=ultimate;e.follow=skill&&(e.kind==2||e.kind==3||e.kind==4||e.kind==8);e.duration=ultimate?1.15f:skill?.7f:.4f;e.remaining=e.duration;e.radius=ultimate?3.2f:skill?1.45f:Mathf.Max(1,evt.size);e.position=P(evt.position);e.aim=P(f.aim,0);e.color=Color.Lerp(f.data.color,Color.white,.3f);e.root.transform.position=e.position;e.root.SetActive(true);
            if(e.emblem)e.emblem.gameObject.SetActive(ultimate||skill);
            var main=e.sparks.main;main.startColor=new ParticleSystem.MinMaxGradient(e.color,Color.white);main.startSpeed=ultimate?6:3;main.startSize=ultimate?.28f:.18f;main.startLifetime=ultimate?.8f:.5f;e.sparks.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);e.sparks.Emit(ultimate?60:skill?25:14);
            // Instant hit attacks still need a visible connection from muzzle to target.
            if(evt.type=="lightning"){e.kind=10;e.position=P(f.position,1.2f);e.aim=P(evt.position,1.2f)-e.position;e.radius=0;}
            if(evt.type=="wave"){e.kind=11;e.position=P(f.position,.65f);e.radius=3.8f;}
            UpdateEffect(e,0);
        }
        void Update(){var player=model.fighters[localPlayer];targetMarker.enabled=player.Alive&&player.data.kind==AttackKind.Artillery; if(targetMarker.enabled){Vector3 center=P(player.position+player.aim*Mathf.Min(player.aimDistance,player.data.range),.22f);for(int i=0;i<49;i++){float a=i*Mathf.PI*2/48;targetMarker.SetPosition(i,center+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*1.65f);}}foreach(var e in pool){if(e==null||e.remaining<=0)continue;e.remaining-=Time.deltaTime;if(e.remaining<=0){e.root.SetActive(false);continue;}UpdateEffect(e,1-e.remaining/e.duration);}}
        void UpdateEffect(Effect e,float age){
            if(e.follow)e.position=P(model.fighters[e.actor].position);e.root.transform.position=e.position;
            Color color=e.color;color.a=1-age;e.line.startColor=e.line.endColor=color;e.line.widthMultiplier=(e.ultimate?.16f:.10f)*(1-age*.65f);
            int count=49;e.line.positionCount=count;float radius=e.radius*(.45f+age*.55f);float heading=Mathf.Atan2(e.aim.z,e.aim.x);
            for(int i=0;i<count;i++){
                float t=(float)i/(count-1),a=t*Mathf.PI*2;Vector3 p;
                if(e.kind==10)p=e.position+e.aim*t+Vector3.right*(i%2==0?.10f:-.10f)*(i==0||i==count-1?0:1);
                else if(e.kind==11||e.kind==8){a=heading+Mathf.Lerp(-.95f,.95f,t);p=e.position+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*radius;}
                else if(e.kind==2){p=e.position+e.aim*(t*radius*2)+Vector3.up*Mathf.Sin(t*Mathf.PI)*.3f;}
                else {float shape=e.kind==0?1+Mathf.Cos(a*6)*.16f:e.kind==1?1+Mathf.Sin(a*12+age*15)*.16f:e.kind==4?1+Mathf.Cos(a*8)*.2f:e.kind==5?1+Mathf.Cos(a*4)*.2f:e.kind==6?1+Mathf.Sin(a*3)*.22f:e.kind==7?1+Mathf.Cos(a*4)*.25f:1;p=e.position+new Vector3(Mathf.Cos(a)*radius*shape,e.kind==3?Mathf.Sin(a+age*8)*.2f:e.kind==9?age*.8f:0,Mathf.Sin(a)*radius*shape);}
                e.line.SetPosition(i,p);
            }
            if(e.emblem){float scale=(.5f+Mathf.Sin(age*Mathf.PI)*.7f)*(e.ultimate?1.6f:1);e.emblem.localRotation=Quaternion.Euler(0,age*(e.kind==6?-250:180),0);e.emblem.localPosition=e.emblemOffset*scale+Vector3.up*(2.9f+age*.7f);e.emblem.localScale=Vector3.one*e.emblemScale*scale;}
        }
        public void Clear(){enabled=false;if(targetMarker)targetMarker.enabled=false;foreach(var e in pool)if(e!=null){e.remaining=0;e.root.SetActive(false);}}
        void OnDestroy(){if(glow)Destroy(glow);}
    }
}
