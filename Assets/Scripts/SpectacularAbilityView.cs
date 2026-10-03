using UnityEngine;
namespace PharmaBrawl
{
    // Presentation only. Real damage, timing, targets and summons remain authoritative.
    public sealed class SpectacularAbilityView:MonoBehaviour
    {
        sealed class Effect
        {
            public GameObject root;public Transform[] pieces=new Transform[8];public MeshFilter[] filters=new MeshFilter[8];public Renderer[] renderers=new Renderer[8];
            public LineRenderer[] ribbons=new LineRenderer[3],streaks=new LineRenderer[8];public ParticleSystem sparks,mist,flash;public float time,duration,range,trailTick;public int owner,kind,count;public bool ultimate,follow,impact;public Vector3 start,center,aim;public Color color;
        }
        readonly Effect[] pool=new Effect[24];readonly Mesh[] meshes=new Mesh[4];ArenaSimulation simulation;int cursor;bool visible=true;
        Material ribbonMaterial;Material crystal,flareMaterial,mistMaterial,starMaterial;MaterialPropertyBlock block;
        public int ActiveEffects{get{int n=0;foreach(var e in pool)if(e!=null&&e.time>0)n++;return n;}}
        public bool HasTripoMeshes=>meshes[0]&&meshes[1]&&meshes[2]&&meshes[3];
        static Vector3 P(Vector2 p,float height=1.2f)=>new Vector3(p.x,height,p.y);
        static readonly Color[] Colors={new Color(1,.16f,.05f),new Color(.05f,.7f,1),new Color(1,.7f,.06f),new Color(.65f,.2f,1),new Color(.25f,1,.1f),new Color(.08f,.55f,1),new Color(1,.15f,.65f),new Color(1,.62f,.05f),new Color(.05f,1,.95f),new Color(.7f,.12f,1)};
        public void Initialize(ArenaSimulation model)
        {
            block=new MaterialPropertyBlock();simulation=model;string[] names={"CrossBurst","BoltCrystal","BloomCore","CapsuleGem"};for(int i=0;i<4;i++){var prefab=Resources.Load<GameObject>("Effects/Spectacular/"+names[i]);if(prefab)meshes[i]=prefab.GetComponent<MeshFilter>().sharedMesh;}
            ribbonMaterial=new Material(Resources.Load<Material>("VFX/AbilityRibbon"));crystal=new Material(Resources.Load<Material>("VFX/AbilityCrystal"));flareMaterial=MedicalVfx.Material(0);mistMaterial=MedicalVfx.Material(1,true);starMaterial=MedicalVfx.Material(2);
            for(int i=0;i<pool.Length;i++){
                var root=new GameObject("Pooled spectacular skill "+i);root.transform.SetParent(transform,false);var e=new Effect{root=root};
                for(int j=0;j<8;j++){var piece=new GameObject("Animated Tripo energy "+j);piece.transform.SetParent(root.transform,false);e.pieces[j]=piece.transform;e.filters[j]=piece.AddComponent<MeshFilter>();var renderer=piece.AddComponent<MeshRenderer>();renderer.sharedMaterial=crystal;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;e.renderers[j]=renderer;}
                for(int j=0;j<3;j++)e.ribbons[j]=Ribbon(root,"Spinning luminous energy ribbon "+j,65);for(int j=0;j<8;j++)e.streaks[j]=Ribbon(root,"White hot energy tail "+j,13);e.sparks=MedicalVfx.Layer(root,"Colored comet sparks",starMaterial,.55f,.28f,5,96);e.mist=MedicalVfx.Layer(root,"Layered energy smoke",mistMaterial,.75f,1.4f,.7f,24);e.flash=MedicalVfx.Layer(root,"White hot activation flare",flareMaterial,.22f,2.5f,0,4);pool[i]=e;root.SetActive(false);
            }
        }
        public void Emit(ArenaSimulation.CombatEvent evt)
        {
            if(!visible)return;
            if(evt.type=="death"||evt.type=="withdraw"){ClearActor(evt.actor);return;}
            if(evt.type!="skill"&&evt.type!="ultimate")return;
            var f=simulation.fighters[evt.actor];var e=pool[cursor++%pool.Length];Stop(e);e.owner=evt.actor;e.kind=(int)f.data.kind;e.ultimate=evt.type=="ultimate";e.start=P(evt.position);e.center=e.start;e.aim=P(f.aim,0);e.range=f.data.range;e.color=Colors[e.kind];
            e.duration=e.ultimate?1.65f:e.kind==3||e.kind==4?4:e.kind==2||e.kind==8?2.8f:1.2f;e.time=e.duration;e.trailTick=0;e.impact=false;e.follow=!e.ultimate&&(e.kind==1||e.kind==2||e.kind==3||e.kind==4||e.kind==7||e.kind==8);
            if(e.kind==5||e.kind==9||e.ultimate&&e.kind==0)e.center=P(f.position+f.aim*Mathf.Min(f.aimDistance,e.kind==9&&!e.ultimate?7:8),.8f);
            e.count=e.ultimate?8:e.kind==2||e.kind==5?1:3;int mesh=e.kind==5&&e.ultimate?2:e.kind==1||e.kind==2||e.kind==5?1:e.kind==7&&e.ultimate?0:3;
            e.root.transform.position=e.center;e.root.SetActive(true);for(int i=0;i<8;i++){e.pieces[i].gameObject.SetActive(i<e.count);e.streaks[i].enabled=false;e.filters[i].sharedMesh=meshes[mesh];}
            Configure(e.sparks,e.color,e.ultimate?7:4,e.ultimate?.38f:.25f);Configure(e.mist,new Color(e.color.r,e.color.g,e.color.b,.3f),.8f,e.ultimate?2.3f:1.1f);Configure(e.flash,Color.Lerp(e.color,Color.white,.75f),0,e.ultimate?4:2.3f);
            e.flash.Emit(e.ultimate?2:1);e.sparks.Emit(e.ultimate?45:20);e.mist.Emit(e.ultimate?10:4);Draw(e,0);
        }
        LineRenderer Ribbon(GameObject root,string name,int vertices){var g=new GameObject(name);g.transform.SetParent(root.transform,false);var line=g.AddComponent<LineRenderer>();line.sharedMaterial=ribbonMaterial;line.useWorldSpace=true;line.positionCount=vertices;line.numCapVertices=3;line.widthCurve=new AnimationCurve(new Keyframe(0,.05f),new Keyframe(.2f,1),new Keyframe(.75f,.75f),new Keyframe(1,.05f));line.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return line;}
        static void Configure(ParticleSystem ps,Color color,float speed,float size){var m=ps.main;m.startColor=color;m.startSpeed=speed;m.startSize=size;}
        void Draw(Effect e,float age)
        {
            var fighter=simulation.fighters[e.owner];if(e.follow)e.center=P(fighter.position);e.root.transform.position=e.center;
            float envelope=Mathf.Min(1,age*10+.2f)*Mathf.Min(1,(1-age)*5);float power=e.ultimate?1.4f:1;Vector3 side=Vector3.Cross(e.aim,Vector3.up);float spin=age*Mathf.PI*(e.ultimate?5:3);
            for(int i=0;i<e.count;i++){
                float a=i*Mathf.PI*2/e.count+spin,t=(i+.5f)/e.count;Vector3 offset=Vector3.zero;float size=.8f*power;
                switch(e.kind){
                    case 0: // capsule volley, then an ascending capsule detonation crown
                        offset=e.ultimate?new Vector3(Mathf.Cos(a)*(2.5f-age),age*4+Mathf.Sin(a)*.3f,Mathf.Sin(a)*(2.5f-age)):e.aim*(age*2)+side*((i-1)*1.2f)+Vector3.up*(age*3+.8f);size=e.ultimate?1.3f:.7f;break;
                    case 1: // electric prongs / falling ion crystals
                        offset=e.ultimate?new Vector3(Mathf.Cos(a)*2,(1-age)*4+Mathf.Sin(a)*.3f,Mathf.Sin(a)*2):e.aim*(.5f+age*2)+side*Mathf.Cos(a)*1.3f+Vector3.up*Mathf.Sin(a);size=e.ultimate?1.6f:.95f;break;
                    case 2: // orbiting piercing charge / layered laser lances
                        offset=e.ultimate?e.aim*(age*10+t*2)+side*Mathf.Sin(a)*.4f+Vector3.up*Mathf.Cos(a)*.4f:e.aim*.9f+side*Mathf.Cos(a)*.55f+Vector3.up*Mathf.Sin(a)*.55f;size=e.ultimate?1.8f:.8f;break;
                    case 3: // faceted shield satellites / comet-like charge through actual path
                        offset=e.ultimate?Vector3.Lerp(e.start,P(fighter.position),Mathf.Clamp01(age*3))-e.center+side*Mathf.Cos(a)*.65f+Vector3.up*Mathf.Sin(a)*.6f:new Vector3(Mathf.Cos(a)*1.3f,Mathf.Sin(a*2)*.55f,Mathf.Sin(a)*1.3f);size=e.ultimate?1.3f:.8f;break;
                    case 4: // speed booster sparks / upward blossoming rapid-fire stars
                        offset=new Vector3(Mathf.Cos(a)*(e.ultimate?2:1),age*(e.ultimate?4:3)+.5f,Mathf.Sin(a)*(e.ultimate?2:1));size=e.ultimate?1.2f:.65f;break;
                    case 5: // rain of artillery crystals
                        offset=new Vector3(Mathf.Cos(a)*(e.ultimate?2.1f:1),(1-age)*(e.ultimate?5:3)+i*.15f,Mathf.Sin(a)*(e.ultimate?2.1f:1));size=e.ultimate?1.8f:1;break;
                    case 6: // blink flower fragments / an assassination bloom at the real destination
                        Vector3 destination=P(fighter.position);offset=Vector3.Lerp(e.start,destination,Mathf.Clamp01(age*3))-e.center+new Vector3(Mathf.Cos(a)*(1-age)*1.4f,Mathf.Sin(a)*.6f,Mathf.Sin(a)*(1-age)*1.4f);size=e.ultimate?1.55f:.9f;break;
                    case 7: // medical summoning beacons, reinforcing the existing Tripo robots
                        offset=new Vector3(Mathf.Cos(a)*(e.ultimate?1.5f:.9f),.35f+Mathf.Sin(a)*.6f+age*.8f,Mathf.Sin(a)*(e.ultimate?1.5f:.9f));size=e.ultimate?1.5f:.8f;break;
                    case 8: // green fan jewels / outward fan of luminous petal blades
                        float spread=Mathf.Lerp(-.85f,.85f,t);offset=e.ultimate?(e.aim*Mathf.Cos(spread)+side*Mathf.Sin(spread))*(age*6+.8f):e.aim*.8f+side*Mathf.Lerp(-1,1,t)+Vector3.up*Mathf.Sin(spin)*.4f;size=e.ultimate?1.3f:.8f;break;
                    case 9: // toxic crystalline blossom with rising gas and rotating petals
                        offset=new Vector3(Mathf.Cos(a)*(e.ultimate?2:1.1f),Mathf.Sin(a*2)*.45f+age*(e.ultimate?1.8f:.8f),Mathf.Sin(a)*(e.ultimate?2:1.1f));size=e.ultimate?1.65f:1;break;
                }
                e.pieces[i].localPosition=offset;e.pieces[i].localRotation=Quaternion.Euler(age*180+i*35,age*(e.kind==9?-280:320)+i*60,Mathf.Sin(a)*35);e.pieces[i].localScale=Vector3.one*size*envelope*(e.ultimate?1.45f:1.25f);
                var streak=e.streaks[i];streak.enabled=true;streak.widthMultiplier=(e.ultimate?.16f:.075f)*envelope;streak.startColor=new Color(e.color.r,e.color.g,e.color.b,0);streak.endColor=Color.Lerp(e.color,Color.white,.7f);for(int k=0;k<13;k++){float q=k/12f;Vector3 tail=e.pieces[i].position-Vector3.up*(1-q)*(e.ultimate?3.5f:2.4f)-e.aim*(1-q)*.7f;streak.SetPosition(k,tail);}
                if(e.kind==1||e.kind==2)e.pieces[i].localRotation=Quaternion.Euler(0,age*90+i*45,0);if(e.kind==5&&e.ultimate&&i==0)e.pieces[i].localScale*=2;Color tint=Color.Lerp(e.color,Color.white,.18f+.22f*Mathf.Sin(age*15+i));tint.a=envelope*.75f;block.SetColor("_Color",tint);e.renderers[i].SetPropertyBlock(block);
            }
            for(int j=0;j<3;j++){var line=e.ribbons[j];line.enabled=e.ultimate;line.widthMultiplier=(.3f+j*.1f)*envelope;line.startColor=new Color(e.color.r,e.color.g,e.color.b,0);line.endColor=new Color(e.color.r,e.color.g,e.color.b,.95f*envelope);for(int k=0;k<65;k++){float q=k/64f;float angle=q*Mathf.PI*3+spin*(j%2==0?1:-1)+j*2;float radius=(1.4f+q*2.2f)*(1-.25f*age);Vector3 v=new Vector3(Mathf.Cos(angle)*radius,q*(j==1?2.8f:.7f)+j*.15f,Mathf.Sin(angle)*radius);if(e.kind==1||e.kind==5)v.y+=Mathf.Sin(k*2.3f+j)*.22f;line.SetPosition(k,e.center+v);}}
        }
        public void Advance(float dt)
        {
            if(!visible)return;foreach(var e in pool)if(e!=null&&e.time>0){e.time=Mathf.Max(0,e.time-dt);var f=simulation.fighters[e.owner];if(e.time<=0||!f.Alive){Stop(e);continue;}float age=1-e.time/e.duration;Draw(e,age);
                if(!e.ultimate&&(e.kind==2||e.kind==8)&&age>.12f&&!f.empowered)e.time=Mathf.Min(e.time,.15f);
                if(e.ultimate&&!e.impact&&age>.55f){e.impact=true;e.flash.Emit(2);e.sparks.Emit(28);e.mist.Emit(6);}
                e.trailTick+=dt;if(e.trailTick>=1f/30){e.trailTick%=1f/30;for(int i=0;i<e.count;i++){var trail=new ParticleSystem.EmitParams{position=e.pieces[i].position,startColor=e.color,startSize=e.ultimate?.28f:.18f};e.sparks.Emit(trail,1);}}
            }
        }
        static void Stop(Effect e){if(e==null)return;e.time=0;e.root.SetActive(false);e.sparks.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);e.mist.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);e.flash.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
        public void ClearActor(int actor){foreach(var e in pool)if(e!=null&&e.owner==actor)Stop(e);}
        public void Clear(){foreach(var e in pool)Stop(e);}
        public void SetVisible(bool value){if(visible==value)return;visible=value;if(!visible)Clear();}
        void Update(){Advance(Time.deltaTime);}
        void OnDestroy(){if(ribbonMaterial)Destroy(ribbonMaterial);if(crystal)Destroy(crystal);if(flareMaterial)Destroy(flareMaterial);if(mistMaterial)Destroy(mistMaterial);if(starMaterial)Destroy(starMaterial);}
    }
}
