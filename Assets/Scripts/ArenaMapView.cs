using System.Collections.Generic;
using UnityEngine;

namespace PharmaBrawl
{
    public sealed class ArenaMapView
    {
        readonly Transform world;
        readonly ArenaMap map;
        static readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        readonly System.Random random = new System.Random(71);
        readonly Color white = new Color(.95f,.97f,.94f), teal = new Color(.08f,.63f,.65f), coral = new Color(.93f,.36f,.33f), purple = new Color(.32f,.22f,.58f), sand = new Color(.8f,.43f,.22f);
        public ArenaMapView(Transform root, ArenaMap data) { world=root; map=data; }
        Material Mat(Color c, bool glow=false)
        {
            // Emissive strips use a separate material, keeping opaque floor colors intact.
            Color key=glow ? new Color(c.r,c.g,c.b,.5f) : c;
            if(materials.TryGetValue(key,out var m))return m;
            m=new Material(Resources.Load<Material>("CombatMaterial"));m.color=c;m.SetFloat("_Glossiness",.22f);
            if(glow){m.EnableKeyword("_EMISSION");m.SetColor("_EmissionColor",c*1.7f);}
            materials[key]=m;return m;
        }
        Transform Shape(string name,PrimitiveType type,Vector3 p,Vector3 size,Color color,Transform parent=null,bool glow=false)
        {
            var g=GameObject.CreatePrimitive(type);g.name=name;Object.Destroy(g.GetComponent<Collider>());
            g.transform.SetParent(parent ? parent : world,false);g.transform.localPosition=p;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=Mat(color,glow);return g.transform;
        }
        Transform Box(string n,Vector3 p,Vector3 s,Color c,Transform parent=null,bool glow=false)=>Shape(n,PrimitiveType.Cube,p,s,c,parent,glow);
        Transform Ball(string n,Vector3 p,Vector3 s,Color c,Transform parent=null,bool glow=false)=>Shape(n,PrimitiveType.Sphere,p,s,c,parent,glow);
        float Rand(float min,float max)=>(float)random.NextDouble()*(max-min)+min;
        void Cross(Vector3 p,Color color,Transform parent,bool front=false,float scale=1)
        {
            Box("Medical cross",p,front?new Vector3(.7f,.2f,.025f)*scale:new Vector3(.7f,.025f,.2f)*scale,color,parent);
            Box("Medical cross",p,front?new Vector3(.2f,.7f,.025f)*scale:new Vector3(.2f,.025f,.7f)*scale,color,parent);
        }
        public void Build(ArenaSimulation sim,List<Transform> covers)
        {
            Color floor=map.theme==ArenaTheme.Village?new Color(.92f,.8f,.58f):map.theme==ArenaTheme.Laboratory?new Color(.14f,.12f,.28f):map.theme==ArenaTheme.Desert?new Color(.95f,.66f,.3f):new Color(.72f,.84f,.96f);
            Box("Arena foundation",new Vector3(0,-.5f,0),new Vector3(41,.9f,31),floor*.63f);
            Box("Walkable ground",new Vector3(0,-.04f,0),new Vector3(36,.12f,26),floor);
            for(int x=-17;x<=17;x+=2)for(int z=-12;z<=12;z+=2)
            {
                Color c=Color.Lerp(floor,white,Rand(0,.12f));
                var tile=Box("Walkable paving",new Vector3(x,.025f,z),new Vector3(1.97f,.025f,1.97f),c);
                if(map.theme==ArenaTheme.Desert){tile.localScale=new Vector3(1.99f,.025f,1.99f);}
            }
            foreach(var f in map.features)
            {
                var root=new GameObject(f.kind+ (f.blocksMovement?" / BLOCKED":" / WALKABLE")).transform;root.SetParent(world,false);root.localPosition=new Vector3(f.position.x,0,f.position.y);
                Feature(f,root);
                if(f.blocksMovement) covers.Add(root);
            }
            Boundary();SpawnPads();Landmarks();
            if(map.theme==ArenaTheme.Village)
            {
                Shape("Central capsule plaza",PrimitiveType.Cylinder,new Vector3(0,.06f,0),new Vector3(5.3f,.025f,5.3f),new Color(1,.87f,.66f));
                for(int s=-1;s<=1;s+=2){var p=Shape("Capsule mosaic",PrimitiveType.Capsule,new Vector3(s*.4f,.13f,s*.35f),new Vector3(.85f,.45f,.03f),s<0?teal:coral);p.localRotation=Quaternion.Euler(90,0,-45);}
            }
            if(map.theme==ArenaTheme.Laboratory)
            {
                var p=Box("Open central diamond",new Vector3(0,.06f,0),new Vector3(5.5f,.025f,5.5f),purple*.8f);p.localRotation=Quaternion.Euler(0,45,0);
                foreach(int s in new[]{-1,1})foreach(int t in new[]{-1,1})Box("Neon floor marker",new Vector3(s*2.5f,.09f,t*2.5f),new Vector3(.7f,.025f,.1f),new Color(.4f,.7f,1),null,true);
            }
        }
        void Feature(ArenaMap.Feature f,Transform r)
        {
            float w=f.size.x,d=f.size.y;
            string key=f.kind==ArenaProp.CapsuleWall?"capsule-wall":f.kind==ArenaProp.LabWall?"lab-wall":f.kind==ArenaProp.Container?"medical-container":f.kind==ArenaProp.Bush?"herb-bush":null;
            if(key!=null && ImportedFeature(key,f,r))return;
            switch(f.kind)
            {
                case ArenaProp.CapsuleWall:
                    Box("Cream plinth",new Vector3(0,.13f,0),new Vector3(w,.26f,d),white,r);
                    int n=Mathf.Max(1,Mathf.CeilToInt(Mathf.Max(w,d)/1.15f));
                    for(int i=0;i<n;i++){
                        Vector3 p=w>d?new Vector3((i+.5f)*w/n-w/2,.77f,0):new Vector3(0,.77f,(i+.5f)*d/n-d/2);
                        Box("Rounded capsule barricade",p,new Vector3(w>d?w/n-.04f:w,1.18f,w>d?d:d/n-.04f),i%2==0?teal:coral,r);
                        Ball("Soft capsule top",p+Vector3.up*.55f,new Vector3(w>d?w/n-.03f:w,.28f,w>d?d:d/n-.03f),i%2==0?teal:coral,r);
                    }
                    Bottle(new Vector3(-w/2+.25f,1.3f,-d/2+.25f),teal,r,.65f);break;
                case ArenaProp.LabWall:
                    Box("Purple steel bulkhead",new Vector3(0,.7f,0),new Vector3(w,1.4f,d),purple,r);
                    Box("Steel cap",new Vector3(0,1.45f,0),new Vector3(w,.13f,d),new Color(.6f,.44f,.87f),r);
                    Box("Cyan strip",new Vector3(0,.35f,-d/2-.006f),new Vector3(w*.65f,.12f,.025f),new Color(.1f,.85f,1),r,true);
                    Box("Yellow safety stripe",new Vector3(-w/2+.08f,.8f,0),new Vector3(.14f,1.25f,d+.02f),new Color(1,.75f,.1f),r);break;
                case ArenaProp.Sandstone:
                    int columns=Mathf.Max(1,Mathf.CeilToInt(w/.7f)), rows=Mathf.Max(1,Mathf.CeilToInt(d/.7f));
                    for(int x=0;x<columns;x++)for(int z=0;z<rows;z++)for(int y=0;y<3;y++){
                        Box("Sandstone masonry",new Vector3((x+.5f)*w/columns-w/2,.22f+y*.4f,(z+.5f)*d/rows-d/2),new Vector3(w/columns-.035f,.37f,d/rows-.035f),Color.Lerp(sand,white,Rand(0,.16f)),r);
                    }
                    for(int i=0;i<3;i++)Ball("Vine",new Vector3(Rand(-w*.4f,w*.4f),Rand(.5f,1.1f),-d/2-.03f),new Vector3(.25f,.3f,.1f),new Color(.25f,.42f,.13f),r);break;
                case ArenaProp.Container:
                    Color c=(f.position.x*f.position.y>0)?teal:Mathf.Abs(f.position.x)>7?new Color(.98f,.43f,.1f):coral;
                    Box("Shipping container",new Vector3(0,.83f,0),new Vector3(w,1.66f,d),c,r);
                    for(float x=-w/2+.13f;x<w/2;x+=.3f)Box("Corrugated front",new Vector3(x,.83f,-d/2-.02f),new Vector3(.045f,1.47f,.045f),c*.72f,r);
                    Box("Snow cap",new Vector3(0,1.7f,0),new Vector3(w,.13f,d),white,r);
                    Cross(new Vector3(0,.91f,-d/2-.045f),white,r,true,.85f);
                    Cross(new Vector3(0,1.79f,0),coral,r,false,.7f);break;
                case ArenaProp.Crate:
                    Box("Breakable supply crate",new Vector3(0,.48f,0),new Vector3(w,.96f,d),new Color(.64f,.43f,.24f),r);
                    foreach(int s in new[]{-1,1}){
                        Box("Crate bracing",new Vector3(s*w*.4f,.48f,-d/2-.01f),new Vector3(.1f,.94f,.06f),new Color(.91f,.72f,.43f),r);
                        Box("Crate bracing",new Vector3(0,.48f+s*.38f,-d/2-.015f),new Vector3(w,.1f,.06f),new Color(.91f,.72f,.43f),r);
                    }
                    Box("Medicine label",new Vector3(0,.5f,-d/2-.04f),new Vector3(.48f,.48f,.03f),white,r);Cross(new Vector3(0,.5f,-d/2-.065f),coral,r,true,.42f);
                    if(map.theme==ArenaTheme.Alpine)Box("Snow",new Vector3(0,1,0),new Vector3(w,.09f,d),white,r);break;
                case ArenaProp.Rock:
                    // Entire irregular silhouette is contained inside its blocked footprint.
                    for(float x=-w/2+.23f;x<w/2;x+=.55f)for(float z=-d/2+.23f;z<d/2;z+=.5f){Ball("Rock",new Vector3(x,.46f,z),new Vector3(.52f,.88f,.47f),new Color(.37f,.46f,.58f),r);Ball("Snow",new Vector3(x,.86f,z),new Vector3(.52f,.16f,.46f),white,r);}break;
                case ArenaProp.Water:
                    Box("Blocked water",new Vector3(0,.04f,0),new Vector3(w,.04f,d),map.theme==ArenaTheme.Laboratory?new Color(.02f,.7f,.62f):new Color(.04f,.57f,.76f),r,map.theme==ArenaTheme.Laboratory);
                    for(int i=0;i<Mathf.CeilToInt(w*d);i++){
                        Vector3 p=new Vector3(Rand(-w*.45f,w*.45f),.08f,Rand(-d*.43f,d*.43f));
                        if(map.theme==ArenaTheme.Laboratory)Shape("Glowing bubble",PrimitiveType.Cylinder,p,new Vector3(.27f,.014f,.27f),new Color(.25f,1,.73f),r,true);
                        else Box("Water ripple",p,new Vector3(Rand(.13f,.45f),.015f,.035f),new Color(.39f,.89f,.92f),r);
                    }
                    if(map.theme==ArenaTheme.Village){for(int s=-1;s<=1;s+=2)Box("Low canal rim",new Vector3(s*(w/2-.07f),.12f,0),new Vector3(.14f,.2f,d),white,r);}
                    break;
                case ArenaProp.Bridge:
                    bool desert=map.theme==ArenaTheme.Desert;
                    Box("Walkable bridge deck",new Vector3(0,.09f,0),new Vector3(w,.13f,d),desert?new Color(.9f,.65f,.39f):new Color(.6f,.36f,.17f),r);
                    for(float z=-d/2+.12f;z<d/2;z+=.3f)Box("Bridge paving",new Vector3(0,.17f,z),new Vector3(w-.08f,.03f,.26f),desert?new Color(.96f,.75f,.49f):new Color(.76f,.49f,.24f),r);
                    // No rails protrude into the actual passage.
                    break;
                case ArenaProp.Ice:
                    Shape("Walkable frozen lake",PrimitiveType.Cylinder,new Vector3(0,.052f,0),new Vector3(w,.017f,d),new Color(.31f,.69f,.9f),r);
                    for(int i=0;i<20;i++){var crack=Box("Ice cracks",new Vector3(Rand(-w*.33f,w*.33f),.082f,Rand(-d*.33f,d*.33f)),new Vector3(.7f,.012f,.025f),new Color(.67f,.87f,.96f),r);crack.localRotation=Quaternion.Euler(0,Rand(0,180),0);}break;
                case ArenaProp.Bush:
                    // Leaves are low enough to read the paths; these plants do not have collision.
                    Color leaf=map.theme==ArenaTheme.Laboratory?new Color(.28f,.9f,.13f):map.theme==ArenaTheme.Alpine?new Color(.09f,.29f,.23f):map.theme==ArenaTheme.Desert?new Color(.27f,.44f,.16f):new Color(.22f,.63f,.08f);
                    for(float x=-w/2+.22f;x<w/2;x+=.45f)for(float z=-d/2+.22f;z<d/2;z+=.44f){
                        float h=Rand(.45f,.8f);Ball("Passable foliage",new Vector3(x+Rand(-.06f,.06f),h*.5f,z),new Vector3(.54f,h,.52f),Color.Lerp(leaf,new Color(.51f,.79f,.18f),Rand(0,.2f)),r,map.theme==ArenaTheme.Laboratory);
                        if(map.theme==ArenaTheme.Alpine)Ball("Snow on foliage",new Vector3(x,h*.86f,z),new Vector3(.28f,.1f,.25f),white,r);
                        else if(random.NextDouble()<.2)Ball("Herbal flower",new Vector3(x,h*.9f,z),Vector3.one*.13f,map.theme==ArenaTheme.Desert?new Color(.7f,.43f,.81f):white,r);
                    }
                    break;
            }
        }
        bool ImportedFeature(string key,ArenaMap.Feature feature,Transform root)
        {
            var prefab=Resources.Load<GameObject>("MapProps/"+key);if(!prefab)return false;
            var fit=new GameObject("Footprint fit").transform;fit.SetParent(root,false);
            var instance=Object.Instantiate(prefab,fit);
            // Preserve FBX's imported Z-up to Y-up transform. Scale an outer wrapper in arena axes.
            if(feature.size.y>feature.size.x && key!="herb-bush")fit.localRotation=Quaternion.Euler(0,90,0);
            var renderers=instance.GetComponentsInChildren<Renderer>();if(renderers.Length==0){Object.Destroy(instance);return false;}
            Bounds b=renderers[0].bounds;foreach(var renderer in renderers)b.Encapsulate(renderer.bounds);
            float height=feature.kind==ArenaProp.Bush?.72f:feature.kind==ArenaProp.Container?1.75f:1.35f;
            fit.localScale=feature.size.y>feature.size.x && key!="herb-bush"?
                new Vector3(feature.size.y/b.size.z,height/b.size.y,feature.size.x/b.size.x):new Vector3(feature.size.x/b.size.x,height/b.size.y,feature.size.y/b.size.z);
            b=renderers[0].bounds;foreach(var renderer in renderers)b.Encapsulate(renderer.bounds);
            instance.transform.position+=new Vector3(root.position.x-b.center.x,root.position.y-b.min.y,root.position.z-b.center.z);
            foreach(var collider in instance.GetComponentsInChildren<Collider>())Object.Destroy(collider);
            if(feature.kind==ArenaProp.Bush){
                Color leaves=map.theme==ArenaTheme.Alpine?new Color(.1f,.32f,.21f):map.theme==ArenaTheme.Desert?new Color(.3f,.49f,.18f):map.theme==ArenaTheme.Laboratory?new Color(.2f,.65f,.04f):new Color(.25f,.62f,.06f);
                for(float x=-feature.size.x/2+.23f;x<feature.size.x/2;x+=.48f)for(float z=-feature.size.y/2+.22f;z<feature.size.y/2;z+=.48f)
                    Ball("Dense passable undergrowth",new Vector3(x,.21f,z),new Vector3(.5f,.42f,.5f),leaves,root);
            }
            if(feature.kind==ArenaProp.Bush && map.theme==ArenaTheme.Alpine){
                for(int i=0;i<10;i++)Ball("Snow on passable herbs",new Vector3(Rand(-feature.size.x*.35f,feature.size.x*.35f),.62f,Rand(-feature.size.y*.35f,feature.size.y*.35f)),new Vector3(.21f,.08f,.2f),white,root);
            }
            if(feature.kind==ArenaProp.Bush && map.theme==ArenaTheme.Laboratory){
                for(int i=0;i<8;i++)Ball("Glowing medicinal pod",new Vector3(Rand(-feature.size.x*.35f,feature.size.x*.35f),.45f,Rand(-feature.size.y*.35f,feature.size.y*.35f)),new Vector3(.28f,.55f,.28f),new Color(.27f,.94f,.04f),root,true);
            }
            if(feature.kind==ArenaProp.LabWall)Box("Active neon strip",new Vector3(0,.36f,-feature.size.y/2-.008f),new Vector3(feature.size.x*.6f,.08f,.015f),new Color(.12f,.8f,1),root,true);
            return true;
        }
        void Bottle(Vector3 p,Color c,Transform root,float scale=1)
        {
            Shape("Medicine bottle",PrimitiveType.Cylinder,p,new Vector3(.32f,.24f,.32f)*scale,c,root);
            Shape("White cap",PrimitiveType.Cylinder,p+Vector3.up*.27f*scale,new Vector3(.33f,.05f,.33f)*scale,white,root);
        }
        void SpawnPads()
        {
            for(int team=0;team<2;team++){
                Color c=team==0?new Color(.11f,.64f,.97f):coral;float z=team==0?-11.1f:11.1f;
                Box("Walkable team spawn pad",new Vector3(0,.065f,z),new Vector3(8.4f,.045f,2.1f),c*.8f);
                Box("Team spawn edge",new Vector3(0,.093f,z+(team==0?-.9f:.9f)),new Vector3(8.3f,.018f,.06f),c,null,map.theme==ArenaTheme.Laboratory);
                Cross(new Vector3(0,.1f,z),white,world,false,1.2f);
            }
        }
        void Boundary()
        {
            Color c=map.theme==ArenaTheme.Laboratory?purple:map.theme==ArenaTheme.Desert?sand:map.theme==ArenaTheme.Alpine?new Color(.35f,.48f,.57f):white;
            // The scene's background is outside simulation bounds; front walls stay low.
            Box("Back border",new Vector3(0,.43f,13.6f),new Vector3(39,.86f,.8f),c);
            Box("Front border",new Vector3(0,.22f,-13.6f),new Vector3(39,.44f,.8f),c);
            for(int s=-1;s<=1;s+=2)Box("Side border",new Vector3(s*18.6f,.38f,0),new Vector3(.8f,.76f,27),c);
            for(int s=-1;s<=1;s+=2)for(int t=-1;t<=1;t+=2){
                if(map.theme==ArenaTheme.Alpine)Tree(new Vector3(s*18.7f,0,t*8),true);
                if(map.theme==ArenaTheme.Desert)Palm(new Vector3(s*18.8f,0,t*8));
                if(map.theme==ArenaTheme.Village)Tree(new Vector3(s*18.8f,0,t*7),false);
            }
        }
        void Tree(Vector3 p,bool snowy)
        {
            Shape("Tree trunk",PrimitiveType.Cylinder,p+Vector3.up, new Vector3(.4f,1,.4f),new Color(.35f,.23f,.12f));
            for(int i=0;i<3;i++){float w=2.3f-i*.48f;Shape("Tree crown",PrimitiveType.Sphere,p+Vector3.up*(1.5f+i*.72f),new Vector3(w,1.2f,w),new Color(.13f,.35f,.2f));if(snowy)Ball("Snow crown",p+Vector3.up*(2+i*.72f),new Vector3(w,.3f,w),white);}
        }
        void Palm(Vector3 p)
        {
            Shape("Palm trunk",PrimitiveType.Cylinder,p+Vector3.up*1.5f,new Vector3(.4f,1.5f,.4f),sand*.7f);
            for(int i=0;i<7;i++){var leaf=Ball("Palm frond",p+Vector3.up*3,new Vector3(.5f,.2f,3.6f),new Color(.25f,.44f,.14f));leaf.localRotation=Quaternion.Euler(12,i*51,0);}
        }
        void Landmarks()
        {
            var prefab=Resources.Load<GameObject>("MapProps/"+map.key);
            for(int s=-1;s<=1;s+=2)for(int t=-1;t<=1;t+=2)
            {
                if(prefab){
                    var root=new GameObject("Tripo landmark / "+map.key).transform;root.SetParent(world,false);
                    var g=Object.Instantiate(prefab,root);
                    var rs=g.GetComponentsInChildren<Renderer>();if(rs.Length==0)continue;Bounds b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
                    float scale=4.4f/Mathf.Max(b.size.x,b.size.z);g.transform.localScale*=scale;
                    b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);g.transform.position+=new Vector3(-b.center.x,-b.min.y,-b.center.z);
                    root.localPosition=new Vector3(s*15.4f,0,t*15.2f);root.localRotation=Quaternion.Euler(0,t>0?s*25:180-s*25,0);
                    foreach(var collider in g.GetComponentsInChildren<Collider>())Object.Destroy(collider);
                }
                else {
                    // Readable background while imported landmarks are being prepared.
                    Vector3 p=new Vector3(s*15.4f,0,t*15.2f);Box("Backdrop building",p+Vector3.up*1.6f,new Vector3(4.3f,3.2f,3.1f),map.theme==ArenaTheme.Laboratory?purple:map.theme==ArenaTheme.Desert?sand:white);Cross(p+new Vector3(0,2,-1.56f),teal,world,true,1.5f);
                }
            }
            if(map.theme==ArenaTheme.Laboratory)for(int s=-1;s<=1;s+=2)for(int i=-2;i<=2;i++){
                Vector3 p=new Vector3(s*19.7f,1.3f,i*4.8f);Shape("Outer experiment tank",PrimitiveType.Cylinder,p,new Vector3(2.2f,1.3f,2.2f),new Color(.04f,.68f,.61f),null,true);
                Shape("Tank lid",PrimitiveType.Cylinder,p+Vector3.up*1.4f,new Vector3(2.4f,.18f,2.4f),purple);
            }
        }
    }
}
