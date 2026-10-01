using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Runtime.InteropServices;

namespace PharmaBrawl
{
    public sealed class PharmaGame : MonoBehaviour
    {
        public CharacterDefinition[] roster;
        ArenaSimulation sim;
        Camera cam, portraitCam, podiumCam;
        RenderTexture portraitTexture, podiumTexture;
        Transform world, actorsRoot;
        readonly List<GameObject> worldObjects=new List<GameObject>();
        readonly Dictionary<Color,Material> materials=new Dictionary<Color,Material>();
        readonly Transform[] actors=new Transform[6], shotViews=new Transform[256], zoneViews=new Transform[48], dropViews=new Transform[48], robotViews=new Transform[12];
        readonly Transform[] healthBars=new Transform[6];
        readonly Transform[] beamViews=new Transform[6];
        readonly float[] beamLife=new float[6];
        readonly Text[] nameLabels=new Text[6];
        readonly List<Transform> coverViews=new List<Transform>();
        readonly Transform[] fx=new Transform[64];
        readonly float[] fxLife=new float[64], fxSize=new float[64];
        readonly Color[] fxColor=new Color[64];
        Canvas canvas;
        Font font;
        Sprite whiteSprite;
        GameObject titleScreen, lobby, hud, result, pausePanel;
        Text scoreBlue,scoreRed,timer,hpText,chargeText,skillText,status,feed,heroLabel,resultTitle,resultStats,detail,selectedName;
        Image hpFill,chargeFill;
        Button skillButton,ultButton;
        Transform aimLine;
        int selected,selectedMap;
        Light keyLight;
        ArenaMusic soundtrack;
        Text mapDetail,musicLabel;
        readonly Button[] mapButtons=new Button[4];
        GameObject movementOverlay;
        bool captureMap;
        bool playing,paused,skillRequested,ultimateRequested;
        float accumulator, shake, noticeTimer;
        Vector2 touchMove,touchAim=Vector2.up;
        bool mobileFire;
        AudioSource sfx;
        AudioClip[] tones;
        bool smoke,capture,captureLobby,captureResult,captureModels;
        string capturePath;
        float realTime;
        readonly Color navy=new Color(.035f,.065f,.14f), cream=new Color(.95f,.97f,1), blue=new Color(.12f,.68f,1), red=new Color(1,.3f,.38f);

        void Start()
        {
            Application.targetFrameRate=60;
            QualitySettings.vSyncCount=0;
            font=Resources.Load<Font>("Fonts/NotoSansKR");
            if(!font)font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cam=new GameObject("Arena Camera").AddComponent<Camera>();cam.tag="MainCamera";cam.orthographic=true;cam.orthographicSize=14;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.025f,.055f,.1f);cam.transform.rotation=Quaternion.Euler(50,0,0);cam.nearClipPlane=.1f;cam.farClipPlane=100;
            var light=new GameObject("Warm key light").AddComponent<Light>();keyLight=light;light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(45,-30,0);light.intensity=1.25f;light.shadows=LightShadows.Soft;
            RenderSettings.ambientLight=new Color(.62f,.7f,.85f);RenderSettings.fog=false;
            world=new GameObject("Pharmacy arena").transform;actorsRoot=new GameObject("Pooled combat visuals").transform;
            portraitTexture=new RenderTexture(160,160,16);podiumTexture=new RenderTexture(1000,220,16);
            portraitCam=new GameObject("Portrait Camera").AddComponent<Camera>();portraitCam.orthographic=true;portraitCam.orthographicSize=1.3f;portraitCam.targetTexture=portraitTexture;portraitCam.cullingMask=1<<8;portraitCam.clearFlags=CameraClearFlags.SolidColor;portraitCam.backgroundColor=navy;portraitCam.enabled=false;
            podiumCam=new GameObject("Victory Podium Camera").AddComponent<Camera>();podiumCam.orthographic=true;podiumCam.orthographicSize=2.5f;podiumCam.targetTexture=podiumTexture;podiumCam.backgroundColor=navy;podiumCam.clearFlags=CameraClearFlags.SolidColor;podiumCam.enabled=false;podiumCam.transform.position=new Vector3(0,5,-11);podiumCam.transform.LookAt(new Vector3(0,1.25f,0));
            whiteSprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,Texture2D.whiteTexture.width,Texture2D.whiteTexture.height),new Vector2(.5f,.5f));
            MakeAudio();MakeUI();soundtrack.Play("menu");
            string[] args=Environment.GetCommandLineArgs();
            for(int i=0;i<args.Length;i++){if(args[i]=="--smoke-test")smoke=true;if((args[i]=="--capture" || args[i]=="--capture-lobby" || args[i]=="--capture-result") && i+1<args.Length){capture=true;captureLobby=args[i]=="--capture-lobby";captureResult=args[i]=="--capture-result";capturePath=args[i+1];}}
            for(int i=0;i<args.Length-1;i++)if(args[i]=="--capture-models"){capture=true;captureModels=true;capturePath=args[i+1];}
            for(int i=0;i<args.Length-1;i++)if(args[i]=="--map" && int.TryParse(args[i+1],out int mapPick))selectedMap=Mathf.Clamp(mapPick,0,3);
            for(int i=0;i<args.Length-1;i++)if(args[i]=="--capture-map"){capture=true;captureMap=true;capturePath=args[i+1];}
            SelectMap(selectedMap);
            if(captureMap){titleScreen.SetActive(false);lobby.SetActive(false);CreateMap(new ArenaSimulation(roster,selected,42,selectedMap));PositionCamera(Vector2.zero,0);Invoke(nameof(SaveCapture),3);return;}
            if(captureModels){ShowModelGallery();Invoke(nameof(SaveCapture),3);return;}
            if(smoke || (capture && !captureLobby))StartMatch();
            else {CreateMap(new ArenaSimulation(roster,selected,42,selectedMap));PositionCamera(Vector2.zero,0);if(captureLobby)Invoke(nameof(SaveCapture),2);}
        }
        Material Mat(Color c)
        {
            if(materials.TryGetValue(c,out var m))return m;
            m=new Material(Resources.Load<Material>("CombatMaterial"));m.color=c;m.SetFloat("_Glossiness",.2f);materials[c]=m;return m;
        }
        void ShowModelGallery()
        {
            titleScreen.SetActive(false);lobby.SetActive(false);
            cam.orthographicSize=5.4f;cam.backgroundColor=navy;
            cam.transform.position=new Vector3(0,9,-18);cam.transform.LookAt(new Vector3(0,1,3.2f));
            Label(canvas.transform,"PHARMA BRAWL  /  TRIPO 3D",new Vector2(0,390),new Vector2(1400,60),32,cream,TextAnchor.MiddleCenter);
            Label(canvas.transform,"10 PHARMACISTS  ·  10 MEDICAL WEAPONS",new Vector2(0,345),new Vector2(1400,40),17,blue,TextAnchor.MiddleCenter);
            for(int i=0;i<10;i++)
            {
                var root=new GameObject("Model gallery / "+roster[i].displayName).transform;
                if(roster[i].characterPrefab)root.gameObject.AddComponent<PharmacistModelRig>().Initialize(roster[i]);
                root.position=new Vector3((i%5-2)*3.2f,0,i<5?6.4f:0);
                root.rotation=Quaternion.Euler(0,135,0);
                Shape("Display plinth",PrimitiveType.Cylinder,root.position+Vector3.down*.09f,new Vector3(2.6f,.09f,2.6f),roster[i].color*.45f,null);
                Vector3 v=cam.WorldToViewportPoint(root.position+new Vector3(0,-.32f,-.15f));
                Label(canvas.transform,$"{i+1:00}  {roster[i].displayName}",new Vector2((v.x-.5f)*1600,(v.y-.5f)*900),new Vector2(280,40),21,cream,TextAnchor.MiddleCenter);
            }
        }
        Transform Shape(string name,PrimitiveType primitive,Vector3 position,Vector3 scale,Color color,Transform parent=null,bool shadow=true)
        {
            var g=GameObject.CreatePrimitive(primitive);g.name=name;var t=g.transform;t.SetParent(parent?parent:world,false);t.localPosition=position;t.localScale=scale;
            Destroy(g.GetComponent<Collider>());var r=g.GetComponent<Renderer>();r.sharedMaterial=Mat(color);if(!shadow)r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;return t;
        }
        static Vector3 P(Vector2 p,float y=0)=>new Vector3(p.x,y,p.y);
        void CreateMap(ArenaSimulation model)
        {
            foreach(Transform child in world){child.gameObject.SetActive(false);Destroy(child.gameObject);}coverViews.Clear();
            movementOverlay=null;
            world.name=model.map.name;
            new ArenaMapView(world,model.map).Build(model,coverViews);
            bool lab=model.map.theme==ArenaTheme.Laboratory;
            keyLight.intensity=lab?.8f:1.25f;
            RenderSettings.ambientLight=lab?new Color(.44f,.4f,.64f):model.map.theme==ArenaTheme.Desert?new Color(.83f,.74f,.58f):new Color(.65f,.75f,.87f);
            cam.backgroundColor=lab?new Color(.035f,.025f,.09f):model.map.theme==ArenaTheme.Desert?new Color(.38f,.23f,.11f):model.map.theme==ArenaTheme.Alpine?new Color(.2f,.32f,.45f):new Color(.21f,.35f,.32f);
        }
        void ToggleMovementOverlay()
        {
            if(movementOverlay){movementOverlay.SetActive(!movementOverlay.activeSelf);return;}
            movementOverlay=new GameObject("F2 movement inspection");movementOverlay.transform.SetParent(world,false);
            for(float x=-17.5f;x<=17.5f;x+=1)for(float z=-12.5f;z<=12.5f;z+=1)
                Shape(sim.Blocked(new Vector2(x,z))?"Blocked":"Walkable",PrimitiveType.Cube,new Vector3(x,.21f,z),new Vector3(.25f,.025f,.25f),sim.Blocked(new Vector2(x,z))?red:new Color(.22f,1,.46f),movementOverlay.transform,false);
        }
        Transform MakeActor(ArenaSimulation.Fighter f)
        {
            var root=new GameObject(f.data.displayName).transform;root.SetParent(actorsRoot);Color c=f.data.color;
            var ring=Shape("Team disc",PrimitiveType.Cylinder,new Vector3(0,.12f,0),new Vector3(1.6f,.03f,1.6f),f.team==0?blue:red,root,false);
            if(f.data.characterPrefab)
                root.gameObject.AddComponent<PharmacistModelRig>().Initialize(f.data);
            else
            {
            Shape("Boot L",PrimitiveType.Capsule,new Vector3(-.22f,.3f,0),new Vector3(.32f,.3f,.42f),navy,root);Shape("Boot R",PrimitiveType.Capsule,new Vector3(.22f,.3f,0),new Vector3(.32f,.3f,.42f),navy,root);
            float width=f.data.kind==AttackKind.Wave?1.1f:.8f;
            Shape("Pharmacist coat",PrimitiveType.Capsule,new Vector3(0,.94f,0),new Vector3(width,.65f,.65f),cream,root);
            Shape("Color lapel",PrimitiveType.Cube,new Vector3(0,1.14f,.34f),new Vector3(.24f,.6f,.08f),c,root);
            Shape("Head",PrimitiveType.Sphere,new Vector3(0,1.8f,0),new Vector3(.92f,.85f,.85f),new Color(1,.78f,.59f),root);
            Shape("Hair / cap",PrimitiveType.Sphere,new Vector3(0,2.08f,-.08f),new Vector3(.94f,.42f,.87f),c*.65f,root);
            Shape("Nose",PrimitiveType.Sphere,new Vector3(0,1.78f,.43f),new Vector3(.17f,.16f,.14f),new Color(1,.7f,.51f),root);
            for(int side=-1;side<=1;side+=2){Shape("Eyes",PrimitiveType.Sphere,new Vector3(side*.19f,1.88f,.38f),new Vector3(.11f,.12f,.08f),navy,root);Shape("Arm",PrimitiveType.Capsule,new Vector3(side*.55f,1.05f,.15f),new Vector3(.28f,.38f,.28f),cream,root);}
            Shape("Capsule blaster",PrimitiveType.Capsule,new Vector3(.56f,1.13f,.57f),new Vector3(.38f,.4f,.38f),c,root).localRotation=Quaternion.Euler(90,0,0);
            switch(f.data.kind)
            {
                case AttackKind.Lightning: Shape("Tesla coil",PrimitiveType.Cylinder,new Vector3(-.6f,1.5f,.45f),new Vector3(.3f,.65f,.3f),c,root);break;
                case AttackKind.Arrow: Shape("Long golden barrel",PrimitiveType.Cube,new Vector3(.52f,1.2f,1.1f),new Vector3(.2f,.22f,1.6f),c,root);break;
                case AttackKind.Wave: Shape("Purple shield",PrimitiveType.Cube,new Vector3(-.68f,1.03f,.6f),new Vector3(.65f,1.2f,.25f),c,root);break;
                case AttackKind.Burst: Shape("Pill magazine",PrimitiveType.Cylinder,new Vector3(.6f,1,.2f),new Vector3(.7f,.17f,.7f),c,root);break;
                case AttackKind.Artillery: Shape("Mortar backpack",PrimitiveType.Cylinder,new Vector3(0,1.3f,-.6f),new Vector3(.7f,.65f,.7f),c,root);break;
                case AttackKind.Assassin: Shape("Scarf",PrimitiveType.Cube,new Vector3(-.25f,1.5f,-.7f),new Vector3(.3f,.18f,1.2f),c,root);break;
                case AttackKind.Summoner: Shape("Robot antenna",PrimitiveType.Cylinder,new Vector3(0,2.45f,0),new Vector3(.08f,.3f,.08f),c,root);Shape("Antenna tip",PrimitiveType.Sphere,new Vector3(0,2.75f,0),Vector3.one*.25f,c,root);break;
                case AttackKind.Fan: Shape("Spread cannon",PrimitiveType.Cube,new Vector3(.5f,1.2f,.8f),new Vector3(.8f,.3f,.35f),c,root);break;
                case AttackKind.Poison: Shape("Toxic tank",PrimitiveType.Capsule,new Vector3(0,1.15f,-.58f),new Vector3(.7f,.55f,.55f),c,root);break;
            }
            }
            var hpRoot=new GameObject("World health").transform;hpRoot.SetParent(root);hpRoot.localPosition=new Vector3(0,2.8f,0);hpRoot.rotation=cam.transform.rotation;
            Shape("Health track",PrimitiveType.Cube,Vector3.zero,new Vector3(1.2f,.13f,.04f),navy,hpRoot,false);
            healthBars[f.id]=Shape("Health",PrimitiveType.Cube,new Vector3(0,0,-.03f),new Vector3(1.15f,.09f,.04f),f.team==0?blue:red,hpRoot,false);
            var worldCanvas=new GameObject("Nickname").AddComponent<Canvas>();worldCanvas.renderMode=RenderMode.WorldSpace;worldCanvas.transform.SetParent(root);worldCanvas.transform.localPosition=new Vector3(0,3.2f,0);worldCanvas.transform.rotation=cam.transform.rotation;worldCanvas.transform.localScale=Vector3.one*.008f;
            nameLabels[f.id]=Label(worldCanvas.transform,f.id==0?f.data.displayName+" · YOU":f.data.displayName+" · AI",Vector2.zero,new Vector2(250,35),22,cream,TextAnchor.MiddleCenter);return root;
        }
        void StartMatch()
        {
            foreach(Transform t in actorsRoot)Destroy(t.gameObject);
            sim=new ArenaSimulation(roster,selected,Environment.TickCount,selectedMap);sim.Event+=OnCombat;
            CreateMap(sim);soundtrack.Play(sim.map.key);for(int i=0;i<6;i++)actors[i]=MakeActor(sim.fighters[i]);
            foreach(Transform t in actors[0].GetComponentsInChildren<Transform>())t.gameObject.layer=8;
            for(int i=0;i<6;i++){beamViews[i]=Shape("Pooled ultimate beam",PrimitiveType.Cube,Vector3.zero,Vector3.one,roster[2].color,actorsRoot,false);beamViews[i].gameObject.SetActive(false);beamLife[i]=0;}
            portraitCam.enabled=true;podiumCam.enabled=false;
            for(int i=0;i<shotViews.Length;i++){shotViews[i]=Shape("Pooled pill",PrimitiveType.Capsule,Vector3.zero,new Vector3(.26f,.38f,.26f),cream,actorsRoot,false);shotViews[i].gameObject.SetActive(false);}
            for(int i=0;i<zoneViews.Length;i++){zoneViews[i]=Shape("Pooled hazard disc",PrimitiveType.Cylinder,Vector3.zero,Vector3.one,cream,actorsRoot,false);zoneViews[i].gameObject.SetActive(false);dropViews[i]=Shape("Pooled falling capsule",PrimitiveType.Capsule,Vector3.zero,Vector3.one,cream,actorsRoot,false);dropViews[i].gameObject.SetActive(false);}
            for(int i=0;i<robotViews.Length;i++){var r=new GameObject("Pharmacy robot").transform;r.SetParent(actorsRoot);Shape("Body",PrimitiveType.Cube,new Vector3(0,.7f,0),new Vector3(.7f,.65f,.7f),cream,r);Shape("Eye",PrimitiveType.Cube,new Vector3(0,.85f,.36f),new Vector3(.5f,.17f,.08f),blue,r);Shape("Wheels",PrimitiveType.Cylinder,new Vector3(0,.25f,0),new Vector3(.9f,.15f,.9f),navy,r);robotViews[i]=r;r.gameObject.SetActive(false);}
            for(int i=0;i<fx.Length;i++){fx[i]=Shape("Pooled shockwave",PrimitiveType.Sphere,Vector3.zero,Vector3.one,cream,actorsRoot,false);fx[i].gameObject.SetActive(false);fxLife[i]=0;}
            aimLine=Shape("Aim guide",PrimitiveType.Cube,Vector3.zero,new Vector3(.06f,.04f,4),blue,actorsRoot,false);
            playing=true;paused=false;accumulator=0;realTime=0;titleScreen.SetActive(false);lobby.SetActive(false);result.SetActive(false);pausePanel.SetActive(false);hud.SetActive(true);
            heroLabel.text=roster[selected].displayName+"  /  "+roster[selected].role;status.text=sim.map.name+"  ·  FIRST TO 20";noticeTimer=4;
        }
        void Update()
        {
            if(!playing){if(result && result.activeSelf && sim!=null){int team=sim.winner<0?0:sim.winner;for(int i=0;i<6;i++)if(sim.fighters[i].team==team)actors[i].position=new Vector3((i%3-1)*2.5f,Mathf.Sin(Time.time*3+i)*.07f,0);}return;}
            realTime+=Time.unscaledDeltaTime;
            if(Input.GetKeyDown(KeyCode.Escape)){paused=!paused;pausePanel.SetActive(paused);}
            soundtrack.Paused=paused;
            if(Input.GetKeyDown(KeyCode.F2))ToggleMovementOverlay();
            if(paused)return;
            var f=sim.fighters[0];
            Vector2 move=new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical"))+touchMove;
            Vector2 aim=touchAim;
            if(!Application.isMobilePlatform){Ray ray=cam.ScreenPointToRay(Input.mousePosition);Plane plane=new Plane(Vector3.up,Vector3.zero);if(plane.Raycast(ray,out float d)){Vector3 point=ray.GetPoint(d);aim=new Vector2(point.x,point.z)-f.position;}}
            bool fire=Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject() || mobileFire;
            skillRequested|=Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.E);ultimateRequested|=Input.GetKeyDown(KeyCode.Space);
            accumulator+=Mathf.Min(Time.deltaTime,.1f);
            if(smoke || captureResult)accumulator+=2;
            while(accumulator>=1f/60f){sim.Tick(1f/60f,move,aim,fire,skillRequested,ultimateRequested,smoke || capture);accumulator-=1f/60f;skillRequested=false;ultimateRequested=false;mobileFire=false;if(sim.finished)break;}
            RenderGame();UpdateHUD();
            if(sim.finished){Finish();if(smoke){Debug.Log("PHARMA_SMOKE_COMPLETE "+JsonUtility.ToJson(new SmokeReport(sim)));Application.Quit();}else if(captureResult)Invoke(nameof(SaveCapture),1);}
            if(capture && !captureResult && realTime>7)SaveCapture();
        }
        void SaveCapture()
        {
            // Render explicitly: hidden automated captures have no readable window backbuffer.
            var target=new RenderTexture(1600,900,24);var previous=RenderTexture.active;
            cam.targetTexture=target;
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=5;
            if(captureMap)canvas.enabled=false;
            Canvas.ForceUpdateCanvases();if(portraitCam.enabled)portraitCam.Render();if(podiumCam.enabled)podiumCam.Render();cam.Render();
            RenderTexture.active=target;var pixels=new Texture2D(1600,900,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1600,900),0,0);pixels.Apply();
            System.IO.File.WriteAllBytes(capturePath,pixels.EncodeToPNG());
            cam.targetTexture=null;RenderTexture.active=previous;target.Release();Destroy(target);Destroy(pixels);
            capture=false;Invoke(nameof(QuitCapture),1);
        }
        void QuitCapture()=>Application.Quit();
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern void PharmaExitToBlank();
#endif
        void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#elif UNITY_WEBGL
            PharmaExitToBlank();
#else
            Application.Quit();
#endif
        }
        [Serializable] sealed class SmokeReport
        {
            public int blueScore,redScore,shots,skills,ultimates,respawns; public bool finished;public float elapsed;
            public SmokeReport(ArenaSimulation s){blueScore=s.score[0];redScore=s.score[1];shots=s.shotsFired;skills=s.skillsUsed;ultimates=s.ultimatesUsed;respawns=s.respawns;finished=s.finished;elapsed=180-s.timeLeft;}
        }
        void PositionCamera(Vector2 point,float dt)
        {
            // Limit follow at edges, keeping the playfield in view. Constant orthographic angle gives stable aiming.
            float aspect=(float)Screen.width/Screen.height;cam.orthographicSize=Mathf.Max(16,23/aspect);
            Vector3 target=new Vector3(0,28,-23.5f);
            cam.transform.position=dt==0?target:Vector3.Lerp(cam.transform.position,target,1-Mathf.Exp(-dt*5));
            if(shake>0){shake-=dt;cam.transform.position+=new Vector3(Mathf.Sin(Time.time*73),0,Mathf.Cos(Time.time*61))*.10f*shake;}
        }
        void RenderGame()
        {
            PositionCamera(sim.fighters[0].position,Time.deltaTime);
            for(int i=0;i<6;i++)
            {
                var f=sim.fighters[i];actors[i].gameObject.SetActive(f.Alive);if(!f.Alive)continue;
                actors[i].position=P(f.position);actors[i].rotation=Quaternion.LookRotation(P(f.aim));
                healthBars[i].parent.rotation=cam.transform.rotation;nameLabels[i].canvas.transform.rotation=cam.transform.rotation;
                healthBars[i].localScale=new Vector3(1.15f*f.hp/f.data.maxHp,.09f,.04f);healthBars[i].localPosition=new Vector3(-.575f*(1-f.hp/f.data.maxHp),0,-.03f);
            }
            var player=sim.fighters[0];portraitCam.transform.position=actors[0].position+P(player.aim*3,1.65f);portraitCam.transform.LookAt(actors[0].position+Vector3.up*1.4f);aimLine.gameObject.SetActive(player.Alive);float range=Mathf.Min(9,player.data.range);aimLine.position=P(player.position+player.aim*range*.5f,.2f);aimLine.rotation=Quaternion.LookRotation(P(player.aim));aimLine.localScale=new Vector3(.055f,.025f,range);
            for(int i=0;i<shotViews.Length;i++){var s=sim.shots[i];var v=shotViews[i];v.gameObject.SetActive(s.active);if(!s.active)continue;v.position=P(s.position,1.1f);v.rotation=Quaternion.LookRotation(P(s.direction))*Quaternion.Euler(90,0,0);v.localScale=s.kind==4?new Vector3(.5f,1.8f,.5f):new Vector3(.26f,.36f,.26f);v.GetComponent<Renderer>().sharedMaterial=Mat(sim.fighters[s.owner].data.color);}
            for(int i=0;i<zoneViews.Length;i++){var z=sim.zones[i];var v=zoneViews[i];v.gameObject.SetActive(z.active);dropViews[i].gameObject.SetActive(z.active && z.pending);if(!z.active)continue;v.position=P(z.position,.18f);float pulse=z.pending?.9f+.1f*Mathf.Sin(Time.time*14):1;v.localScale=new Vector3(z.radius*2*pulse,.025f,z.radius*2*pulse);v.GetComponent<Renderer>().sharedMaterial=Mat(z.pending?new Color(1,.62f,.35f):sim.fighters[z.owner].data.color*.72f);if(z.pending){dropViews[i].position=P(z.position,1+z.remaining*5);dropViews[i].localScale=z.kind==3?new Vector3(1.3f,1.4f,1.3f):new Vector3(.35f,.6f,.35f);dropViews[i].GetComponent<Renderer>().sharedMaterial=Mat(sim.fighters[z.owner].data.color);}}
            for(int i=0;i<robotViews.Length;i++){var r=sim.robots[i];var v=robotViews[i];v.gameObject.SetActive(r.active);if(r.active){v.position=P(r.position);v.localScale=Vector3.one*(r.elite?1.5f:1);}}
            for(int i=0;i<coverViews.Count;i++)coverViews[i].gameObject.SetActive(sim.covers[i].Active);
            for(int i=0;i<6;i++)if(beamLife[i]>0){beamLife[i]-=Time.deltaTime;beamViews[i].gameObject.SetActive(beamLife[i]>0);}
            for(int i=0;i<fx.Length;i++)if(fxLife[i]>0){fxLife[i]-=Time.deltaTime;float age=1-fxLife[i]/.45f;fx[i].localScale=new Vector3(1+age*fxSize[i]*2,.15f+age*.2f,1+age*fxSize[i]*2);fx[i].gameObject.SetActive(fxLife[i]>0);}
        }
        void OnCombat(ArenaSimulation.CombatEvent e)
        {
            if(e.type=="beam" && beamViews[e.actor]){var b=beamViews[e.actor];b.position=P(e.position+sim.fighters[e.actor].aim*e.size*.5f,1.2f);b.rotation=Quaternion.LookRotation(P(sim.fighters[e.actor].aim));b.localScale=new Vector3(.55f,.4f,e.size);b.gameObject.SetActive(true);beamLife[e.actor]=.24f;}
            if(e.type!="shoot")for(int i=0;i<fx.Length;i++)if(fxLife[i]<=0 && fx[i]){fxLife[i]=.45f;fxSize[i]=e.size;fx[i].position=P(e.position,.5f);fx[i].GetComponent<Renderer>().sharedMaterial=Mat(e.type=="hit"?cream:sim.fighters[e.actor].data.color);fx[i].gameObject.SetActive(true);break;}
            if(e.type=="ultimate"){shake=.8f;status.text=sim.fighters[e.actor].data.displayName+"  ULTIMATE!  "+sim.fighters[e.actor].data.voiceLine;noticeTimer=2.5f;var voice=sim.fighters[e.actor].data.ultimateVoice;if(voice)sfx.PlayOneShot(voice);else PlayTone(3);}
            else if(e.type=="death"){PlayTone(2);feed.text=sim.fighters[e.actor].data.displayName+"  DOWN  ·  +1 POINT";}
            else if(e.type=="shoot"){if(e.actor==0)PlayTone(0);}
            else if(e.type=="hit"){if(e.actor==0)PlayTone(1);}
            else if(e.type=="explosion"){shake=.35f;PlayTone(2);}
        }
        void UpdateHUD()
        {
            scoreBlue.text=sim.score[0].ToString("00");scoreRed.text=sim.score[1].ToString("00");timer.text=$"{Mathf.CeilToInt(sim.timeLeft)/60:00}:{Mathf.CeilToInt(sim.timeLeft)%60:00}";
            var f=sim.fighters[0];hpText.text=$"{Mathf.CeilToInt(f.hp)} / {f.data.maxHp:0} HP";hpFill.fillAmount=f.hp/f.data.maxHp;
            float q=f.charge/f.data.ultimateRequirement;chargeFill.fillAmount=q;chargeText.text=q>=1?"ULTIMATE READY  ·  SPACE":$"ULTIMATE  {Mathf.FloorToInt(q*100)}%";
            skillText.text=f.skillTimer<=0?"SKILL  /  RMB":$"SKILL  {f.skillTimer:0.0}s";skillButton.interactable=f.Alive && f.skillTimer<=0;ultButton.interactable=f.Alive && q>=1;ultButton.GetComponent<Image>().color=q>=1?Color.Lerp(new Color(.75f,.5f,.1f),new Color(1,.85f,.25f),.5f+.5f*Mathf.Sin(Time.time*8)):new Color(.75f,.5f,.1f);
            if(!f.Alive){status.text=$"RESPAWNING IN {Mathf.CeilToInt(f.respawn)}  ·  TEAM SPAWN";noticeTimer=.2f;}
            else {noticeTimer-=Time.deltaTime;if(noticeTimer<=0)status.text=sim.map.name+"  ·  FIRST TO 20  ·  F2 이동 구역 표시";}
        }
        void Finish()
        {
            playing=false;soundtrack.Paused=false;soundtrack.Play("menu");portraitCam.enabled=false;podiumCam.enabled=true;hud.SetActive(false);result.SetActive(true);bool win=sim.winner==0;resultTitle.text=sim.winner<0?"DRAW":win?"VICTORY":"DEFEAT";resultTitle.color=win?blue:red;
            string text=$"BLUE {sim.score[0]} : {sim.score[1]} RED\n\nPHARMACIST          K / D       DAMAGE       TAKEN       HEAL\n";
            foreach(var f in sim.fighters)text+=$"{f.data.displayName} {(f.id==0?"YOU":"AI")}     {f.kills} / {f.deaths}       {f.damageDealt:0}       {f.damageTaken:0}       {f.healing:0}\n";
            resultStats.text=text;PlayTone(win?4:2);
            // Winning trio forms a podium and holds a celebratory pose.
            foreach(var t in shotViews)t.gameObject.SetActive(false);foreach(var t in zoneViews)t.gameObject.SetActive(false);foreach(var t in dropViews)t.gameObject.SetActive(false);foreach(var t in robotViews)t.gameObject.SetActive(false);foreach(var t in fx)t.gameObject.SetActive(false);aimLine.gameObject.SetActive(false);
            foreach(var t in beamViews)t.gameObject.SetActive(false);
            int team=sim.winner<0?0:sim.winner;for(int i=0;i<6;i++){actors[i].gameObject.SetActive(sim.fighters[i].team==team);if(sim.fighters[i].team==team){actors[i].position=new Vector3((i%3-1)*2.5f,0,0);actors[i].rotation=Quaternion.Euler(0,180+(i%3-1)*12,0);foreach(Transform t in actors[i])if(t.name=="Arm")t.localRotation=Quaternion.Euler(0,0,t.localPosition.x<0?65:-65);}}
            PositionCamera(Vector2.zero,0);
        }
        void MakeAudio()
        {
            sfx=gameObject.AddComponent<AudioSource>();sfx.volume=.3f;gameObject.AddComponent<AudioListener>();soundtrack=gameObject.AddComponent<ArenaMusic>();
            tones=new AudioClip[6];float[] freqs={720,180,90,440,880,330};
            for(int j=0;j<6;j++){int len=j==3?12000:4000;var samples=new float[len];for(int i=0;i<len;i++){float t=i/24000f;float envelope=Mathf.Pow(1-(float)i/len,2);samples[i]=(Mathf.Sin(t*freqs[j]*Mathf.PI*2)+.3f*Mathf.Sin(t*freqs[j]*2*Mathf.PI*2))*envelope*.6f;}tones[j]=AudioClip.Create("Original synthesized cue "+j,len,1,24000,false);tones[j].SetData(samples,0);}
        }
        void PlayTone(int i){if(!smoke)sfx.PlayOneShot(tones[i]);}
        RectTransform Rect(string name,Transform parent,Vector2 pos,Vector2 size)
        {
            var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(parent,false);var r=g.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;return r;
        }
        Image Panel(string name,Transform parent,Vector2 pos,Vector2 size,Color c){var r=Rect(name,parent,pos,size);var im=r.gameObject.AddComponent<Image>();im.sprite=whiteSprite;im.material=Resources.Load<Material>("UIBase");im.color=c;return im;}
        Text Label(Transform parent,string value,Vector2 pos,Vector2 size,int fontSize,Color c,TextAnchor align=TextAnchor.MiddleLeft)
        {
            var r=Rect("Text",parent,pos,size);var t=r.gameObject.AddComponent<Text>();t.font=font;t.material=Resources.Load<Material>("UIFont");t.text=value;t.fontSize=fontSize;t.color=c;t.alignment=align;t.verticalOverflow=VerticalWrapMode.Overflow;t.raycastTarget=false;return t;
        }
        Button Btn(Transform parent,string title,Vector2 pos,Vector2 size,Color c,Action callback)
        {
            var im=Panel(title,parent,pos,size,c);var b=im.gameObject.AddComponent<Button>();b.targetGraphic=im;b.onClick.AddListener(()=>callback());var colors=b.colors;colors.highlightedColor=new Color(1,1,1);colors.pressedColor=new Color(.72f,.82f,.85f);colors.disabledColor=new Color(.35f,.4f,.46f);b.colors=colors;Label(im.transform,title,Vector2.zero,size-Vector2.one*8,18,cream,TextAnchor.MiddleCenter);return b;
        }
        void MakeUI()
        {
            var go=new GameObject("Pharma UI");canvas=go.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;var scaler=go.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;go.AddComponent<GraphicRaycaster>();
            var events=new GameObject("Event system");events.AddComponent<EventSystem>();events.AddComponent<StandaloneInputModule>();
            titleScreen=Rect("팽브롤 / Start screen",canvas.transform,Vector2.zero,new Vector2(1600,900)).gameObject;
            var poster=Panel("User supplied title artwork",titleScreen.transform,Vector2.zero,new Vector2(1600,900),Color.white);
            poster.sprite=Resources.Load<Sprite>("TitleScreen");poster.preserveAspect=true;poster.raycastTarget=false;
            TitleHotspot("START",new Vector2(0,-173),new Vector2(405,105),()=>{titleScreen.SetActive(false);lobby.SetActive(true);});
            TitleHotspot("EXIT",new Vector2(0,-293),new Vector2(318,80),ExitGame);
            lobby=Panel("Character select",canvas.transform,Vector2.zero,new Vector2(1600,900),new Color(navy.r,navy.g,navy.b,.94f)).gameObject;
            Label(lobby.transform,"PHARMA / BRAWL",new Vector2(-475,340),new Vector2(530,100),54,cream);
            Label(lobby.transform,"약사 브롤   ·   4 ARENAS",new Vector2(-470,273),new Vector2(530,45),20,blue);
            Btn(lobby.transform,"← START SCREEN",new Vector2(610,337),new Vector2(260,55),new Color(.15f,.28f,.4f),()=>{lobby.SetActive(false);titleScreen.SetActive(true);});
            Label(lobby.transform,"CHOOSE YOUR PHARMACIST",new Vector2(-450,215),new Vector2(570,55),23,cream);
            for(int i=0;i<10;i++){int pick=i;int col=i%5,row=i/5;var b=Btn(lobby.transform,roster[i].displayName+"\n"+roster[i].role,new Vector2(-580+col*215,140-row*110),new Vector2(197,98),roster[i].color*.62f,()=>Select(pick));Label(b.transform,((int)roster[i].kind+1).ToString("00"),new Vector2(-72,39),new Vector2(40,25),14,cream);}
            mapDetail=Label(lobby.transform,"",new Vector2(555,215),new Vector2(390,140),16,new Color(.7f,.83f,.9f));
            Label(lobby.transform,"SELECT ARENA",new Vector2(-545,-45),new Vector2(350,28),17,blue);
            for(int i=0;i<4;i++){
                int pick=i;
                mapButtons[i]=Btn(lobby.transform,"",new Vector2(-465+i*310,-116),new Vector2(296,102),new Color(.14f,.23f,.32f),()=>SelectMap(pick));
                var preview=Rect("Reference preview",mapButtons[i].transform,new Vector2(-82,0),new Vector2(120,90)).gameObject.AddComponent<RawImage>();preview.texture=Resources.Load<Texture2D>("Maps/"+ArenaMap.Keys[i]);preview.raycastTarget=false;
                Label(mapButtons[i].transform,ArenaMap.Names[i],new Vector2(64,12),new Vector2(147,55),17,cream,TextAnchor.MiddleCenter);
                Label(mapButtons[i].transform,(i+1).ToString("00")+" / 3 vs 3",new Vector2(64,-31),new Vector2(140,25),13,blue,TextAnchor.MiddleCenter);
            }
            var musicButton=Btn(canvas.transform,"",new Vector2(675,425),new Vector2(210,34),navy,()=>{soundtrack.ToggleMute();musicLabel.text=soundtrack.Muted?"음악 OFF":"음악 ON";});
            musicLabel=Label(musicButton.transform,soundtrack.Muted?"음악 OFF":"음악 ON",Vector2.zero,new Vector2(200,32),16,cream,TextAnchor.MiddleCenter);
            var info=Panel("Selected profile",lobby.transform,new Vector2(0,-266),new Vector2(1250,172),new Color(.09f,.15f,.23f));
            selectedName=Label(info.transform,"",new Vector2(-435,47),new Vector2(330,70),30,cream);detail=Label(info.transform,"",new Vector2(160,0),new Vector2(800,155),18,cream);
            Btn(lobby.transform,"START 3 vs 3  →",new Vector2(470,-380),new Vector2(310,65),new Color(.06f,.55f,.49f),StartMatch);
            Label(lobby.transform,"WASD 이동  ·  마우스 조준  ·  LMB 공격  ·  RMB 스킬  ·  SPACE 궁극기\n3분 / 20킬   ·   AI 5명 자동 참가   ·   ESC 일시정지",new Vector2(-250,-377),new Vector2(920,70),17,new Color(.62f,.74f,.83f));Select(0);
            hud=Rect("HUD",canvas.transform,Vector2.zero,new Vector2(1600,900)).gameObject;
            var top=Panel("Score",hud.transform,new Vector2(0,382),new Vector2(610,90),navy);Label(top.transform,"BLUE",new Vector2(-237,23),new Vector2(90,24),14,blue,TextAnchor.MiddleCenter);Label(top.transform,"RED",new Vector2(237,23),new Vector2(90,24),14,red,TextAnchor.MiddleCenter);
            scoreBlue=Label(top.transform,"00",new Vector2(-233,-9),new Vector2(100,55),36,cream,TextAnchor.MiddleCenter);scoreRed=Label(top.transform,"00",new Vector2(233,-9),new Vector2(100,55),36,cream,TextAnchor.MiddleCenter);timer=Label(top.transform,"03:00",Vector2.zero,new Vector2(160,75),34,cream,TextAnchor.MiddleCenter);
            status=Label(hud.transform,"",new Vector2(0,315),new Vector2(1250,44),18,navy,TextAnchor.MiddleCenter);
            feed=Label(hud.transform,"PHASE 01  /  PRACTICE",new Vector2(560,370),new Vector2(430,50),15,cream,TextAnchor.MiddleRight);
            var bottom=Panel("Vitals",hud.transform,new Vector2(-470,-366),new Vector2(600,119),navy);
            var portrait=Rect("3D character portrait",bottom.transform,new Vector2(-242,0),new Vector2(90,90)).gameObject.AddComponent<RawImage>();portrait.material=Resources.Load<Material>("UIBase");portrait.texture=portraitTexture;
            heroLabel=Label(bottom.transform,"",new Vector2(47,35),new Vector2(420,35),21,cream);
            Panel("HP track",bottom.transform,new Vector2(-35,-4),new Vector2(300,15),new Color(.18f,.25f,.33f));hpFill=Panel("HP fill",bottom.transform,new Vector2(-35,-4),new Vector2(300,15),new Color(.3f,.89f,.58f));hpFill.type=Image.Type.Filled;hpFill.fillMethod=Image.FillMethod.Horizontal;
            hpText=Label(bottom.transform,"",new Vector2(202,-4),new Vector2(150,30),14,cream,TextAnchor.MiddleCenter);
            Panel("Charge track",bottom.transform,new Vector2(-35,-33),new Vector2(300,10),new Color(.18f,.25f,.33f));chargeFill=Panel("Charge fill",bottom.transform,new Vector2(-35,-33),new Vector2(300,10),new Color(1,.78f,.22f));chargeFill.type=Image.Type.Filled;chargeFill.fillMethod=Image.FillMethod.Horizontal;
            chargeText=Label(bottom.transform,"",new Vector2(202,-33),new Vector2(155,30),12,cream,TextAnchor.MiddleCenter);
            skillButton=Btn(hud.transform,"",new Vector2(450,-362),new Vector2(155,85),new Color(.16f,.42f,.59f),()=>skillRequested=true);skillText=Label(skillButton.transform,"SKILL",Vector2.zero,new Vector2(150,80),17,cream,TextAnchor.MiddleCenter);
            ultButton=Btn(hud.transform,"ULTIMATE\nSPACE",new Vector2(636,-362),new Vector2(175,85),new Color(.75f,.5f,.1f),()=>ultimateRequested=true);
            Btn(hud.transform,"Ⅱ",new Vector2(-741,383),new Vector2(50,50),navy,()=>{paused=true;pausePanel.SetActive(true);});
            Label(hud.transform,"WASD MOVE   /   MOUSE AIM   /   LMB FIRE",new Vector2(0,-438),new Vector2(800,23),12,cream,TextAnchor.MiddleCenter);
            if(Application.isMobilePlatform){AddStick(hud.transform,new Vector2(-620,-260),false);AddStick(hud.transform,new Vector2(620,-210),true);}
            result=Panel("Results",canvas.transform,Vector2.zero,new Vector2(1400,770),new Color(navy.r,navy.g,navy.b,.95f)).gameObject;
            resultTitle=Label(result.transform,"VICTORY",new Vector2(0,260),new Vector2(1000,100),74,blue,TextAnchor.MiddleCenter);
            var podium=Rect("Winner trio",result.transform,new Vector2(0,140),new Vector2(1000,160)).gameObject.AddComponent<RawImage>();podium.material=Resources.Load<Material>("UIBase");podium.texture=podiumTexture;
            resultStats=Label(result.transform,"",new Vector2(0,-92),new Vector2(1140,285),20,cream,TextAnchor.MiddleCenter);
            Btn(result.transform,"REMATCH",new Vector2(220,-280),new Vector2(280,65),new Color(.06f,.55f,.49f),StartMatch);Btn(result.transform,"CHARACTER SELECT",new Vector2(-220,-280),new Vector2(300,65),new Color(.15f,.28f,.4f),()=>{result.SetActive(false);lobby.SetActive(true);soundtrack.Play("menu");});
            pausePanel=Panel("Pause",canvas.transform,Vector2.zero,new Vector2(740,430),new Color(navy.r,navy.g,navy.b,.98f)).gameObject;
            Label(pausePanel.transform,"PAUSED",new Vector2(0,135),new Vector2(650,80),44,cream,TextAnchor.MiddleCenter);
            Label(pausePanel.transform,"공격 / 피격 없이 3.5초 → 자동 회복\n사망 → 4초 뒤 부활\n적에게 피해 → 궁극기 충전\n상자 파괴 가능 · 벽/컨테이너 이동 불가\n수풀/얼음 통과 가능 · F2 이동 구역 표시",new Vector2(0,12),new Vector2(650,185),19,cream,TextAnchor.MiddleCenter);
            Btn(pausePanel.transform,"RESUME",new Vector2(0,-142),new Vector2(270,60),new Color(.06f,.55f,.49f),()=>{paused=false;pausePanel.SetActive(false);});
            lobby.SetActive(false);hud.SetActive(false);result.SetActive(false);pausePanel.SetActive(false);
        }
        void TitleHotspot(string name,Vector2 position,Vector2 size,Action action)
        {
            var im=Panel(name+" clickable area",titleScreen.transform,position,size,Color.white);
            var b=im.gameObject.AddComponent<Button>();b.targetGraphic=im;b.onClick.AddListener(()=>action());
            var c=b.colors;c.normalColor=Color.clear;c.highlightedColor=new Color(1,1,1,.10f);c.selectedColor=Color.clear;c.pressedColor=new Color(0,0,0,.14f);b.colors=c;
        }
        void SelectMap(int index)
        {
            selectedMap=index;var data=new ArenaMap(index);
            mapDetail.text=data.name+"\n"+data.description;
            for(int i=0;i<4;i++)if(mapButtons[i])mapButtons[i].GetComponent<Image>().color=i==index?new Color(.05f,.43f,.46f):new Color(.14f,.23f,.32f);
        }
        void Select(int index){selected=index;var d=roster[index];selectedName.text=d.displayName+"\n"+d.role;selectedName.color=d.color;detail.text=$"HP {d.maxHp:0}   /   SPEED {d.speed:0.0}   /   RANGE {d.range:0.0}\n일반: {d.attackDescription}\n스킬: {d.skillDescription}  ({d.skillCooldown:0}초)\n궁극기: {d.ultimateDescription}";}
        void AddStick(Transform parent,Vector2 position,bool attack)
        {
            var p=Panel(attack?"Attack joystick":"Move joystick",parent,position,new Vector2(170,170),new Color(.08f,.16f,.23f,.65f));var stick=p.gameObject.AddComponent<ArenaTouchStick>();stick.game=this;stick.attack=attack;Label(p.transform,attack?"AIM / RELEASE":"MOVE",Vector2.zero,new Vector2(165,40),15,cream,TextAnchor.MiddleCenter);
        }
        public void TouchInput(bool attack,Vector2 value,bool release){if(attack){if(value.sqrMagnitude>.01f)touchAim=value.normalized;if(release)mobileFire=true;}else touchMove=value;}
    }
    public sealed class ArenaTouchStick : MonoBehaviour,IPointerDownHandler,IDragHandler,IPointerUpHandler
    {
        public PharmaGame game;public bool attack;
        public void OnPointerDown(PointerEventData e)=>OnDrag(e);
        public void OnDrag(PointerEventData e){RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,e.position,e.pressEventCamera,out Vector2 p);game.TouchInput(attack,Vector2.ClampMagnitude(p/70,1),false);}
        public void OnPointerUp(PointerEventData e)=>game.TouchInput(attack,Vector2.zero,true);
    }
}
