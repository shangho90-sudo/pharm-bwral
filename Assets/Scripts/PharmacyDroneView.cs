using UnityEngine;
namespace PharmaBrawl
{
    public sealed class PharmacyDroneView:MonoBehaviour
    {
        Transform support,elite,flash;LineRenderer summon;Material glow,flashMaterial;float spawnTime,recoil;bool empowered;
        public bool HasTripoModels=>support&&elite;
        public void Initialize(){support=Model("Support",1.5f);elite=Model("Elite",2.5f);glow=new Material(Shader.Find("Sprites/Default"));
            var light=MedicalVfx.Quad();flashMaterial=MedicalVfx.Material(0);flash=light.transform;flash.SetParent(transform,false);flash.localPosition=new Vector3(0,.9f,.9f);light.GetComponent<Renderer>().sharedMaterial=flashMaterial;light.SetActive(false);
            var circle=new GameObject("Pooled robot summon portal");circle.transform.SetParent(transform,false);summon=circle.AddComponent<LineRenderer>();summon.sharedMaterial=glow;summon.positionCount=49;summon.useWorldSpace=false;summon.widthMultiplier=.08f;summon.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;for(int i=0;i<49;i++){float a=i*Mathf.PI*2/48;summon.SetPosition(i,new Vector3(Mathf.Cos(a)*.85f,.2f,Mathf.Sin(a)*.85f));}}
        Transform Model(string name,float height){var prefab=Resources.Load<GameObject>("Robots/"+name);if(!prefab){Debug.LogError("Missing Tripo robot prefab "+name);return null;}var body=new GameObject("Tripo "+name+" robot body").transform;body.SetParent(transform,false);Instantiate(prefab,body);var bounds=PharmacistModelRig.BoundsOf(body);float scale=height/Mathf.Max(.01f,bounds.size.y);foreach(Transform child in body){child.localScale*=scale;child.localPosition=new Vector3(-bounds.center.x,-bounds.min.y,-bounds.center.z)*scale;}foreach(var c in body.GetComponentsInChildren<Collider>())Destroy(c);return body;}
        public void Fire(){recoil=.18f;}
        public void Sync(ArenaSimulation model,int index,float dt){var robot=model.robots[index];empowered=robot.elite;support.gameObject.SetActive(!empowered);elite.gameObject.SetActive(empowered);transform.localScale=Vector3.one;spawnTime=Mathf.Max(0,spawnTime-dt);recoil=Mathf.Max(0,recoil-dt);
            Vector2 direction=model.fighters[robot.owner].aim;float nearest=15;int target=-1;var owner=model.fighters[robot.owner];foreach(var f in model.fighters){float d=Vector2.Distance(owner.position,f.position);if(f.Alive&&f.team!=owner.team&&d<nearest){nearest=d;target=f.id;}}if(target>=0)direction=(model.fighters[target].position-robot.position).normalized;
            foreach(var shot in model.shots)if(shot.active&&shot.owner==robot.owner&&(shot.damage==160||shot.damage==240)&&Vector2.Distance(shot.position,robot.position)<2){direction=shot.direction;break;}
            if(direction.sqrMagnitude>.01f)transform.rotation=Quaternion.LookRotation(new Vector3(direction.x,0,direction.y));var body=empowered?elite:support;body.localScale=Vector3.one*Mathf.Clamp01(1-spawnTime/.35f);body.localPosition=new Vector3(0,Mathf.Sin(Time.time*6)*.025f,-recoil*.35f);
            summon.enabled=spawnTime>0;summon.startColor=summon.endColor=empowered?new Color(1,.8f,.2f):Color.cyan;flash.gameObject.SetActive(recoil>0);flash.localPosition=new Vector3(0,empowered?1.4f:.8f,empowered?1.2f:.8f);flash.localScale=Vector3.one*(empowered?.4f:.22f)*Mathf.Sin(recoil/.18f*Mathf.PI);if(Camera.main)flash.rotation=Camera.main.transform.rotation;flashMaterial.color=empowered?new Color(1,.8f,.15f):new Color(1,.3f,.2f);
        }
        void OnEnable(){spawnTime=.35f;}void OnDisable(){recoil=0;if(flash)flash.gameObject.SetActive(false);if(summon)summon.enabled=false;}void OnDestroy(){if(flashMaterial)Destroy(flashMaterial);if(glow)Destroy(glow);}
    }
}
