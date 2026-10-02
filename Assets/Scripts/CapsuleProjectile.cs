using UnityEngine;
namespace PharmaBrawl
{
    public sealed class CapsuleProjectile : MonoBehaviour
    {
        Transform visual,electricOrb;Renderer[] parts;Material[] materials;TrailRenderer trail;Material orbMaterial;
        public void Initialize()
        {
            visual=new GameObject("Tripo capsule mesh").transform;visual.SetParent(transform,false);
            Instantiate(Resources.Load<GameObject>("Projectiles/Capsule"),visual);
            foreach(var c in visual.GetComponentsInChildren<Collider>())Destroy(c);
            var b=PharmacistModelRig.BoundsOf(visual);float length=Mathf.Max(b.size.x,Mathf.Max(b.size.y,b.size.z));float s=.65f/length;
            visual.localScale=Vector3.one*s;visual.localPosition=-b.center*s;
            if(b.size.x>b.size.y && b.size.x>b.size.z)visual.localRotation=Quaternion.Euler(0,0,90);
            else if(b.size.z>b.size.y)visual.localRotation=Quaternion.Euler(90,0,0);
            parts=visual.GetComponentsInChildren<Renderer>();materials=new Material[parts.Length];
            for(int i=0;i<parts.Length;i++){materials[i]=parts[i].material;materials[i].shader=Shader.Find("Pharma/MedicalPalette");materials[i].EnableKeyword("_EMISSION");}
            trail=gameObject.AddComponent<TrailRenderer>();trail.time=.09f;trail.startWidth=.12f;trail.endWidth=0;trail.minVertexDistance=.08f;trail.material=MedicalVfx.Material(0);trail.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            var orb=GameObject.CreatePrimitive(PrimitiveType.Sphere);Destroy(orb.GetComponent<Collider>());orb.name="Pooled lightning energy orb";electricOrb=orb.transform;electricOrb.SetParent(transform,false);electricOrb.localScale=Vector3.one*.42f;orbMaterial=new Material(Shader.Find("Sprites/Default")){color=new Color(.35f,.92f,1)};orb.GetComponent<Renderer>().sharedMaterial=orbMaterial;orb.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;orb.SetActive(false);
        }
        public void Paint(Color color,int kind,AttackKind family=AttackKind.Capsule)
        {
            bool energy=family==AttackKind.Lightning||family==AttackKind.Arrow;visual.gameObject.SetActive(!energy);electricOrb.gameObject.SetActive(energy);electricOrb.localScale=family==AttackKind.Arrow?new Vector3(.13f,safeLength(kind),.13f):Vector3.one*.42f;orbMaterial.color=family==AttackKind.Arrow?new Color(1,.86f,.25f):new Color(.35f,.92f,1);
            foreach(var m in materials){m.color=color;m.SetColor("_EmissionColor",color*.25f);}
            Color tracer=kind==1?new Color(1,.72f,.08f):color;
            trail.startColor=new Color(tracer.r,tracer.g,tracer.b,.85f);trail.endColor=new Color(tracer.r,tracer.g,tracer.b,0);trail.startWidth=kind==1?.23f:.12f;
            transform.localScale=Vector3.one*(kind==4?1.6f:kind==1?1.25f:1);
        }
        static float safeLength(int kind)=>kind==4?1.1f:.75f;
        void OnDestroy(){if(materials!=null)foreach(var material in materials)if(material)Destroy(material);if(trail && trail.sharedMaterial)Destroy(trail.sharedMaterial);if(orbMaterial)Destroy(orbMaterial);}
        void OnEnable(){if(trail)trail.Clear();}
    }
}
