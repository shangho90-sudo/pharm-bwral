using UnityEngine;

namespace PharmaBrawl
{
    // Rendering only: server snapshots own the position, radius and fuse.
    public sealed class PaengAbilityView : MonoBehaviour
    {
        sealed class Bomb {public GameObject root;public Transform capsule;public LineRenderer ring;public ParticleSystem charge;public int zone=-1;}
        sealed class Burst {public GameObject root;public LineRenderer ring;public ParticleSystem sparks,debris,flash,smoke,halo;public bool smoked;public Vector3 position;public float radius,time,duration;}
        readonly Bomb[] bombs=new Bomb[8];readonly Burst[] bursts=new Burst[12];
        ArenaSimulation simulation;Material glow,stone,impactMaterial,smokeMaterial,haloMaterial;Mesh debrisMesh;bool visible=true;int cursor;
        public System.Action<Vector2> ExplosionEnded;
        public int ActiveBombs {get{int n=0;foreach(var b in bombs)if(b!=null&&b.root.activeSelf)n++;return n;}}
        public int ActiveBursts {get{int n=0;foreach(var b in bursts)if(b!=null&&b.time>0)n++;return n;}}
        static Vector3 World(Vector2 p,float y)=>new Vector3(p.x,y,p.y);
        public void Initialize(ArenaSimulation model)
        {
            simulation=model;impactMaterial=MedicalVfx.Material(0);smokeMaterial=MedicalVfx.Material(1,true);haloMaterial=MedicalVfx.Material(3);glow=new Material(Shader.Find("Sprites/Default"));stone=new Material(Shader.Find("Standard")){color=new Color(.48f,.24f,.1f)};
            var cube=GameObject.CreatePrimitive(PrimitiveType.Capsule);debrisMesh=cube.GetComponent<MeshFilter>().sharedMesh;if(Application.isPlaying)Destroy(cube);else DestroyImmediate(cube);
            var prefab=Resources.Load<GameObject>("Projectiles/Capsule");
            for(int i=0;i<bombs.Length;i++){
                var root=new GameObject("Pooled Paeng bomb warning "+i);root.transform.SetParent(transform,false);
                var body=new GameObject("Large Tripo capsule").transform;body.SetParent(root.transform,false);
                var capsule=Instantiate(prefab,body).transform;foreach(var c in capsule.GetComponentsInChildren<Collider>())Destroy(c);
                var bounds=PharmacistModelRig.BoundsOf(body);float length=Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z));float scale=2.6f/length;
                capsule.localPosition=-bounds.center;body.localScale=Vector3.one*scale;
                if(bounds.size.x>bounds.size.y&&bounds.size.x>bounds.size.z)body.localRotation=Quaternion.Euler(0,0,90);
                else if(bounds.size.z>bounds.size.y)body.localRotation=Quaternion.Euler(90,0,0);
                // The medical cross is a separate attachment, never baked scenery.
                foreach(int side in new[]{-1,1})foreach(var size in new[]{new Vector3(.23f,.76f,.08f),new Vector3(.76f,.23f,.08f)}){
                    var cross=GameObject.CreatePrimitive(PrimitiveType.Cube);Destroy(cross.GetComponent<Collider>());cross.name="Cyan medical badge";cross.transform.SetParent(root.transform,false);cross.transform.localScale=size;cross.GetComponent<Renderer>().sharedMaterial=glow;
                    cross.transform.localPosition=new Vector3(0,0,side*.64f);
                    cross.GetComponent<Renderer>().material.color=new Color(0,1,1);
                }
                bombs[i]=new Bomb{root=root,capsule=body,ring=Ring(root,"Exact damage radius"),charge=MedicalVfx.Layer(root,"Charging upward sparks",impactMaterial,.4f,.2f,1.5f,12)};root.SetActive(false);
            }
            for(int i=0;i<bursts.Length;i++){
                var root=new GameObject("Pooled Paeng explosion "+i);root.transform.SetParent(transform,false);
                bursts[i]=new Burst{root=root,ring=Ring(root,"Ground shockwave"),sparks=Particles(root,false),debris=Particles(root,true),flash=MedicalVfx.Layer(root,"White hot impact core",impactMaterial,.16f,2,0,2),smoke=MedicalVfx.Layer(root,"Drifting impact smoke",smokeMaterial,.7f,1.4f,.9f,8),halo=MedicalVfx.Layer(root,"Textured expanding halo",haloMaterial,.35f,3,0,2)};root.SetActive(false);
            }
        }
        LineRenderer Ring(GameObject root,string name){var child=new GameObject(name);child.transform.SetParent(root.transform,false);var line=child.AddComponent<LineRenderer>();line.sharedMaterial=glow;line.useWorldSpace=true;line.positionCount=65;line.widthMultiplier=.08f;line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return line;}
        ParticleSystem Particles(GameObject root,bool rocks){
            var child=new GameObject(rocks?"Cartoon debris":"Orange impact star");child.transform.SetParent(root.transform,false);var ps=child.AddComponent<ParticleSystem>();var main=ps.main;main.playOnAwake=false;main.loop=false;main.simulationSpace=ParticleSystemSimulationSpace.World;main.maxParticles=rocks?18:48;main.startLifetime=rocks?.55f:.35f;main.startSize=rocks?.18f:.25f;main.startSpeed=rocks?5:9;main.gravityModifier=rocks?1.3f:0;
            main.startColor=rocks?new ParticleSystem.MinMaxGradient(new Color(.95f,.1f,.05f),Color.white):new ParticleSystem.MinMaxGradient(new Color(1,.25f,.02f),new Color(1,.95f,.24f));
            var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.18f;
            var size=ps.sizeOverLifetime;size.enabled=true;size.size=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,1,1,0));
            var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=rocks?stone:impactMaterial;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.renderMode=rocks?ParticleSystemRenderMode.Mesh:ParticleSystemRenderMode.Stretch;if(rocks)renderer.mesh=debrisMesh;else {renderer.lengthScale=2.5f;renderer.velocityScale=.05f;}
            ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);return ps;
        }
        static void Circle(LineRenderer ring,Vector3 center,float radius,Color color){ring.enabled=false;}
        public void Emit(ArenaSimulation.CombatEvent e)
        {
            if(!visible||e.type!="explosion"||simulation.fighters[e.actor].data.kind!=AttackKind.Capsule)return;
            var burst=bursts[cursor++%bursts.Length];burst.sparks.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);burst.debris.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            burst.flash.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);burst.smoke.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);burst.halo.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);burst.smoked=false;burst.root.SetActive(true);burst.position=World(e.position,.25f);burst.root.transform.position=burst.position;burst.radius=e.size;burst.duration=e.size>3?.95f:.75f;burst.time=burst.duration;
            var main=burst.sparks.main;main.startSpeed=e.size>3?12:7;main.startSize=e.size>3?.34f:.2f;burst.sparks.Emit(e.size>3?48:24);burst.debris.Emit(e.size>3?18:7);var core=burst.flash.main;core.startSize=e.size>3?6f:2.8f;core.startColor=new Color(1,.9f,.55f);burst.flash.Emit(1);var halo=burst.halo.main;halo.startSize=e.size*1.7f;halo.startColor=new Color(1,.5f,.08f,.65f);burst.halo.Emit(1);DrawBurst(burst);
        }
        void DrawBurst(Burst burst){float age=1-burst.time/burst.duration;if(!burst.smoked&&age>.08f){burst.smoked=true;var smoke=burst.smoke.main;smoke.startSize=burst.radius*.8f;smoke.startColor=new Color(.55f,.34f,.23f,.24f);burst.smoke.Emit(burst.radius>3?7:4);}Circle(burst.ring,burst.position,burst.radius*Mathf.Min(1,age*5+.15f),new Color(1,.65f,.08f,1-age));burst.ring.widthMultiplier=(1-age)*.17f;}
        public void Sync(float dt)
        {
            if(!visible)return;
            foreach(var bomb in bombs)if(bomb.zone>=0){var zone=simulation.zones[bomb.zone];if(!zone.active||!zone.pending||zone.kind!=3||simulation.fighters[zone.owner].data.kind!=AttackKind.Capsule){bomb.zone=-1;bomb.charge.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);bomb.root.SetActive(false);}}
            for(int index=0;index<simulation.zones.Length;index++){
                var zone=simulation.zones[index];if(!zone.active||!zone.pending||zone.kind!=3||simulation.fighters[zone.owner].data.kind!=AttackKind.Capsule)continue;
                Bomb view=null;foreach(var bomb in bombs)if(bomb.zone==index){view=bomb;break;}
                if(view==null)foreach(var bomb in bombs)if(bomb.zone<0){view=bomb;view.zone=index;break;}
                if(view==null)continue;bool first=!view.root.activeSelf;view.root.SetActive(true);view.root.transform.position=World(zone.position,0);
                // Fuse follows authoritative remaining time even after reconnect.
                if(first){var main=view.charge.main;main.loop=true;main.startColor=new Color(1,.7f,.15f,.6f);var emission=view.charge.emission;emission.enabled=true;emission.rateOverTime=12;var shape=view.charge.shape;shape.shapeType=ParticleSystemShapeType.Circle;shape.rotation=new Vector3(90,0,0);shape.radius=zone.radius*.8f;view.charge.Play();}
                float height=1.45f+Mathf.Clamp01(zone.remaining/.9f)*7;view.capsule.localPosition=Vector3.up*height;
                foreach(Transform child in view.root.transform)if(child.name=="Cyan medical badge")child.localPosition=new Vector3(0,height-.38f,child.localPosition.z);
                Circle(view.ring,World(zone.position,.23f),zone.radius,new Color(1,.65f,.07f,.65f+.25f*Mathf.Sin(zone.remaining*28)));
            }
            foreach(var burst in bursts)if(burst.time>0){burst.time=Mathf.Max(0,burst.time-dt);if(burst.time==0){ExplosionEnded?.Invoke(new Vector2(burst.position.x,burst.position.z));burst.root.SetActive(false);burst.sparks.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);burst.debris.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}else DrawBurst(burst);}
        }
        void Update(){Sync(Time.deltaTime);}
        public void SetVisible(bool value){if(visible==value)return;visible=value;if(!visible)Clear();}
        public void Clear(){visible=false;foreach(var bomb in bombs)if(bomb!=null){bomb.zone=-1;bomb.charge.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);bomb.root.SetActive(false);}foreach(var burst in bursts)if(burst!=null){burst.time=0;burst.root.SetActive(false);burst.sparks.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);burst.debris.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}}
        void OnDestroy(){foreach(var bomb in bombs)if(bomb!=null)foreach(var renderer in bomb.root.GetComponentsInChildren<Renderer>())if(renderer.name=="Cyan medical badge")Destroy(renderer.sharedMaterial);if(glow)Destroy(glow);if(stone)Destroy(stone);if(impactMaterial)Destroy(impactMaterial);if(smokeMaterial)Destroy(smokeMaterial);if(haloMaterial)Destroy(haloMaterial);}
    }
}
