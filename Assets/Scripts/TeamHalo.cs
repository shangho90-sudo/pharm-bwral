using UnityEngine;
namespace PharmaBrawl
{
    public sealed class TeamHalo : MonoBehaviour
    {
        Mesh runtimeMesh;Material ringMaterial,discMaterial;
        void OnDestroy(){if(runtimeMesh)Destroy(runtimeMesh);if(ringMaterial)Destroy(ringMaterial);if(discMaterial)Destroy(discMaterial);}
        public void Initialize(Color tint,bool player)
        {
            var mesh=new Mesh();runtimeMesh=mesh;const int n=64;
            var vertices=new Vector3[(n+1)*2];var colors=new Color[vertices.Length];var triangles=new int[n*6];
            for(int i=0;i<=n;i++){
                float a=i*2*Mathf.PI/n;var v=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a));
                vertices[i*2]=v*.72f;vertices[i*2+1]=v*.82f;
                colors[i*2]=new Color(tint.r,tint.g,tint.b,.85f);colors[i*2+1]=new Color(.65f,.95f,1,1);
                if(i<n){int k=i*2,t=i*6;triangles[t]=k;triangles[t+1]=k+2;triangles[t+2]=k+1;triangles[t+3]=k+1;triangles[t+4]=k+2;triangles[t+5]=k+3;}
            }
            mesh.vertices=vertices;mesh.colors=colors;mesh.triangles=triangles;mesh.RecalculateNormals();
            var edge=new GameObject("Luminous team halo",typeof(MeshFilter),typeof(MeshRenderer));edge.transform.SetParent(transform,false);edge.transform.localPosition=Vector3.up*.15f;
            edge.GetComponent<MeshFilter>().sharedMesh=mesh;
            var material=new Material(Resources.Load<Material>("UIBase"));ringMaterial=material;material.color=Color.white;edge.GetComponent<MeshRenderer>().sharedMaterial=material;
            edge.GetComponent<MeshRenderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            var disc=GameObject.CreatePrimitive(PrimitiveType.Cylinder);Destroy(disc.GetComponent<Collider>());disc.name="Smooth team medallion";disc.transform.SetParent(transform,false);disc.transform.localPosition=Vector3.up*.13f;disc.transform.localScale=new Vector3(1.45f,.015f,1.45f);
            var mat=new Material(Shader.Find("Standard"));discMaterial=mat;mat.color=tint;mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",tint*.55f);mat.SetFloat("_Glossiness",.8f);disc.GetComponent<Renderer>().sharedMaterial=mat;disc.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
