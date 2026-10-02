using UnityEngine;
namespace PharmaBrawl
{
    public sealed class CapsuleProjectile : MonoBehaviour
    {
        Transform visual;Renderer[] parts;Material[] materials;TrailRenderer trail;
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
            for(int i=0;i<parts.Length;i++){materials[i]=parts[i].material;materials[i].EnableKeyword("_EMISSION");}
            trail=gameObject.AddComponent<TrailRenderer>();trail.time=.09f;trail.startWidth=.12f;trail.endWidth=0;trail.minVertexDistance=.08f;trail.material=new Material(Shader.Find("Sprites/Default"));trail.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        public void Paint(Color color,int kind)
        {
            foreach(var m in materials){m.color=Color.Lerp(Color.white,color,.3f);m.SetColor("_EmissionColor",color*.25f);}
            Color tracer=kind==1?new Color(1,.72f,.08f):color;
            trail.startColor=new Color(tracer.r,tracer.g,tracer.b,.85f);trail.endColor=new Color(tracer.r,tracer.g,tracer.b,0);trail.startWidth=kind==1?.23f:.12f;
            transform.localScale=Vector3.one*(kind==4?1.6f:kind==1?1.25f:1);
        }
        void OnDestroy(){if(materials!=null)foreach(var material in materials)if(material)Destroy(material);if(trail && trail.sharedMaterial)Destroy(trail.sharedMaterial);}
        void OnEnable(){if(trail)trail.Clear();}
    }
}
