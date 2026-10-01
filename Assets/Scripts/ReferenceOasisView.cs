using System.Collections.Generic;
using UnityEngine;
namespace PharmaBrawl
{
    public sealed class ReferenceOasisView : MonoBehaviour
    {
        Camera cameraView;
        Transform plate;
        static Transform Quad()
        {
            var g=new GameObject("Reference quad",typeof(MeshFilter),typeof(MeshRenderer));
            var m=new Mesh();m.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)};m.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};m.triangles=new[]{0,2,1,0,3,2};m.RecalculateNormals();g.GetComponent<MeshFilter>().sharedMesh=m;g.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return g.transform;
        }
        static Material MaterialFor(Texture2D texture)
        {
            var mat=new Material(Resources.Load<Material>("ReferenceMap"));mat.mainTexture=texture;return mat;
        }
        public void Build(Camera camera,ArenaSimulation model,List<Transform> views)
        {
            cameraView=camera;
            plate=Quad();plate.name="Clean reference arena background";plate.SetParent(transform,false);
            plate.GetComponent<Renderer>().sharedMaterial=MaterialFor(Resources.Load<Texture2D>(model.map.theme==ArenaTheme.Village?"VillageBackground":model.map.theme==ArenaTheme.Laboratory?"LabBackground":model.map.theme==ArenaTheme.Alpine?"AlpineBackground":"OasisBackground"));
            var atlas=Resources.Load<Texture2D>("GameplayReference");
            foreach(var cover in model.covers){
                var root=new GameObject("Reference collision "+cover.feature.kind).transform;root.SetParent(transform,false);root.position=new Vector3(cover.position.x,0,cover.position.y);views.Add(root);
                if(cover.feature.kind==ArenaProp.Crate){
                    var sprite=Quad();sprite.name="Destructible medical crate";sprite.SetParent(root,false);
                    sprite.rotation=camera.transform.rotation;sprite.position=root.position+camera.transform.up*.62f;sprite.localScale=new Vector3(1.18f,1.28f,1);
                    var mat=MaterialFor(atlas);mat.mainTextureScale=new Vector2(45f/1673,50f/940);mat.mainTextureOffset=new Vector2(659f/1673,1-384f/940);sprite.GetComponent<Renderer>().sharedMaterial=mat;
                }
            }
            LateUpdate();
        }
        void LateUpdate()
        {
            if(!plate || !cameraView)return;
            plate.position=cameraView.transform.position+cameraView.transform.forward*75;plate.rotation=cameraView.transform.rotation;
            plate.localScale=new Vector3(cameraView.orthographicSize*2*cameraView.aspect,cameraView.orthographicSize*2,1);
        }
    }
}
