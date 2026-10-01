using UnityEngine;
using System.Collections.Generic;
namespace PharmaBrawl
{
    public sealed class PharmacyDroneView:MonoBehaviour
    {
        readonly List<Material> runtimeMaterials=new List<Material>();
        void OnDestroy(){foreach(var material in runtimeMaterials)if(material)Destroy(material);}
        void Part(Transform root,string name,PrimitiveType shape,Vector3 position,Vector3 scale,Color color)
        {
            var g=GameObject.CreatePrimitive(shape);g.name=name;g.transform.SetParent(root,false);g.transform.localPosition=position;g.transform.localScale=scale;Destroy(g.GetComponent<Collider>());
            var mat=new Material(Resources.Load<Material>("CombatMaterial"));runtimeMaterials.Add(mat);mat.color=color;mat.SetFloat("_Metallic",0);mat.SetFloat("_Glossiness",.35f);g.GetComponent<Renderer>().sharedMaterial=mat;
        }
        public void Initialize()
        {
            Part(transform,"Teal medical drone",PrimitiveType.Sphere,new Vector3(0,.55f,0),new Vector3(.65f,.6f,.55f),new Color(.02f,.5f,.57f));
            Part(transform,"Navy face",PrimitiveType.Cube,new Vector3(0,.63f,.26f),new Vector3(.47f,.24f,.05f),new Color(.025f,.05f,.13f));
            Part(transform,"Cyan eyes",PrimitiveType.Cube,new Vector3(0,.65f,.3f),new Vector3(.27f,.065f,.025f),Color.cyan);
            Part(transform,"Medic cross vertical",PrimitiveType.Cube,new Vector3(0,.42f,.28f),new Vector3(.07f,.16f,.035f),new Color(1,.2f,.3f));
            Part(transform,"Medic cross horizontal",PrimitiveType.Cube,new Vector3(0,.42f,.29f),new Vector3(.16f,.065f,.035f),new Color(1,.2f,.3f));
            Part(transform,"Compact tracks",PrimitiveType.Cube,new Vector3(0,.2f,0),new Vector3(.72f,.15f,.46f),new Color(.04f,.08f,.15f));
            Part(transform,"Amber antenna",PrimitiveType.Sphere,new Vector3(0,.96f,0),Vector3.one*.12f,new Color(1,.68f,.05f));
        }
    }
}
