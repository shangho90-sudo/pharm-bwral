using UnityEngine;
namespace PharmaBrawl
{
    // Shared artillery aim guide; character effects live in their pooled views.
    public sealed class AbilityEffects:MonoBehaviour
    {
        ArenaSimulation model;LineRenderer marker;Material glow;int localPlayer;
        public void Initialize(ArenaSimulation sim,int local=0){model=sim;localPlayer=local;glow=new Material(Shader.Find("Sprites/Default"));var root=new GameObject("Artillery landing aim");root.transform.SetParent(transform,false);marker=root.AddComponent<LineRenderer>();marker.sharedMaterial=glow;marker.positionCount=49;marker.widthMultiplier=.065f;marker.startColor=marker.endColor=new Color(.2f,.8f,1,.8f);marker.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
        public void Emit(ArenaSimulation.CombatEvent e){}
        void Update(){if(model==null)return;var p=model.fighters[localPlayer];marker.enabled=p.Alive&&p.data.kind==AttackKind.Artillery;if(!marker.enabled)return;Vector2 position=p.position+p.aim*Mathf.Min(p.aimDistance,p.data.range);for(int i=0;i<49;i++){float angle=i*Mathf.PI*2/48;marker.SetPosition(i,new Vector3(position.x+Mathf.Cos(angle)*1.65f,.22f,position.y+Mathf.Sin(angle)*1.65f));}}
        public void Clear(){enabled=false;if(marker)marker.enabled=false;}void OnDestroy(){if(glow)Destroy(glow);}
    }
}
