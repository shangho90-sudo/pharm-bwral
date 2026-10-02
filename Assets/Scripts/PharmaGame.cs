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
        readonly Transform[] actors=new Transform[8], shotViews=new Transform[256], zoneViews=new Transform[48], dropViews=new Transform[48], robotViews=new Transform[12];
        readonly Transform[] healthBars=new Transform[8];
        readonly CapsuleProjectile[] projectileMeshes=new CapsuleProjectile[256];
        readonly Transform[] beamViews=new Transform[8];
        readonly float[] beamLife=new float[8];
        readonly Text[] nameLabels=new Text[8];
        readonly List<Transform> coverViews=new List<Transform>();
        readonly Transform[] fx=new Transform[64];
        readonly float[] fxLife=new float[64], fxSize=new float[64];
        readonly Color[] fxColor=new Color[64];
        Canvas canvas;
        Font font;
        Sprite whiteSprite;
        GameObject titleScreen, lobby, hud, result, pausePanel;
        GameObject roomScreen;RoomBrowserView browserView;RoomClient network;string networkRoomId;ArenaSimulation preparedSimulation;int localPlayerId,networkMatch=-1,inputSequence;float sendAt;
        Text scoreBlue,scoreRed,timer,hpText,chargeText,skillText,status,feed,heroLabel;
        Image hpFill,chargeFill;
        Button skillButton,ultButton;
        Transform aimLine;
        int selected,selectedMap;
        Light keyLight;
        ArenaMusic soundtrack;
        Text musicLabel;
        ReferenceSelectionView selectionView;
        ReferenceResultView resultView;
        ReferenceCombatView combatView;
        bool referenceOasis;
        GameObject globalMusicButton;
        GameObject movementOverlay;
        bool captureMap,captureSelect;
        bool playing,paused,skillRequested,ultimateRequested;
        float accumulator, shake, noticeTimer;
        Vector2 touchMove,touchAim=Vector2.up;
        bool mobileFire;
        MobileArenaControls mobileControls;
        AudioSource sfx;
        WeaponAudio weaponAudio;AbilityEffects abilityEffects;
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
            cam=new GameObject("Arena Camera").AddComponent<Camera>();cam.tag="MainCamera";cam.orthographic=true;cam.orthographicSize=14;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.025f,.055f,.1f);cam.transform.rotation=Quaternion.Euler(40,0,0);cam.nearClipPlane=.1f;cam.farClipPlane=100;
            var light=new GameObject("Warm key light").AddComponent<Light>();keyLight=light;light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(45,-30,0);light.intensity=1.25f;light.shadows=LightShadows.Soft;
            RenderSettings.ambientLight=new Color(.62f,.7f,.85f);RenderSettings.fog=false;
            world=new GameObject("Pharmacy arena").transform;actorsRoot=new GameObject("Pooled combat visuals").transform;
            portraitTexture=new RenderTexture(160,160,16);podiumTexture=new RenderTexture(1000,220,16);
            portraitCam=new GameObject("Portrait Camera").AddComponent<Camera>();portraitCam.orthographic=true;portraitCam.orthographicSize=1.3f;portraitCam.targetTexture=portraitTexture;portraitCam.cullingMask=1<<8;portraitCam.clearFlags=CameraClearFlags.SolidColor;portraitCam.backgroundColor=navy;portraitCam.enabled=false;
            podiumCam=new GameObject("Victory Podium Camera").AddComponent<Camera>();podiumCam.orthographic=true;podiumCam.orthographicSize=2.5f;podiumCam.targetTexture=podiumTexture;podiumCam.backgroundColor=navy;podiumCam.clearFlags=CameraClearFlags.SolidColor;podiumCam.enabled=false;podiumCam.transform.position=new Vector3(0,5,-11);podiumCam.transform.LookAt(new Vector3(0,1.25f,0));
            whiteSprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,Texture2D.whiteTexture.width,Texture2D.whiteTexture.height),new Vector2(.5f,.5f));
            network=new GameObject("PharmaRoomClient").AddComponent<RoomClient>();network.RoomChanged=OnRoom;network.Snapshot=OnSnapshot;network.Error=message=>{browserView?.SetStatus(message);selectionView?.SetNotice(message);if(status)status.text=message;};
            MakeAudio();MakeUI();soundtrack.Play("menu");
            string[] args=Environment.GetCommandLineArgs();
            for(int i=0;i<args.Length;i++){if(args[i]=="--smoke-test")smoke=true;if((args[i]=="--capture" || args[i]=="--capture-lobby" || args[i]=="--capture-result" || args[i]=="--capture-select") && i+1<args.Length){capture=true;captureSelect=args[i]=="--capture-select";captureLobby=args[i]=="--capture-lobby" || captureSelect;captureResult=args[i]=="--capture-result";capturePath=args[i+1];}}
            for(int i=0;i<args.Length-1;i++)if(args[i]=="--capture-models"){capture=true;captureModels=true;capturePath=args[i+1];}
            for(int i=0;i<args.Length-1;i++)if(args[i]=="--map" && int.TryParse(args[i+1],out int mapPick))selectedMap=Mathf.Clamp(mapPick,0,3);
            for(int i=0;i<args.Length-1;i++)if(args[i]=="--capture-map"){capture=true;captureMap=true;capturePath=args[i+1];}
            SelectMap(selectedMap);
            if(captureMap){titleScreen.SetActive(false);lobby.SetActive(false);CreateMap(new ArenaSimulation(roster,selected,42,selectedMap));PositionCamera(Vector2.zero,0);Invoke(nameof(SaveCapture),3);return;}
            if(captureModels){ShowModelGallery();Invoke(nameof(SaveCapture),3);return;}
            if(smoke || (capture && !captureLobby))StartMatch();
            else {CreateMap(new ArenaSimulation(roster,selected,42,selectedMap));PositionCamera(Vector2.zero,0);if(captureSelect){titleScreen.SetActive(false);lobby.SetActive(true);}if(captureLobby)Invoke(nameof(SaveCapture),2);}
        }
        static Color Glow(Color c,float alpha){c.a=alpha;return c;}
        Material Mat(Color c)
        {
            if(materials.TryGetValue(c,out var m))return m;
            m=new Material(Resources.Load<Material>("CombatMaterial"));if(c.a<1)m.shader=Resources.Load<Shader>("CombatGlow");m.color=c;m.SetFloat("_Glossiness",.2f);materials[c]=m;return m;
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
            foreach(var plate in world.GetComponents<ReferenceOasisView>())Destroy(plate);
            movementOverlay=null;
            world.name=model.map.name;
            referenceOasis=true;
            if(referenceOasis)world.gameObject.AddComponent<ReferenceOasisView>().Build(cam,model,coverViews);
            else new ArenaMapView(world,model.map).Build(model,coverViews);
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
            root.gameObject.AddComponent<TeamHalo>().Initialize(f.team==0?new Color(0,.65f,1):new Color(1,.13f,.35f),f.id==localPlayerId);
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
            var hpRoot=new GameObject("World health").transform;hpRoot.SetParent(root);hpRoot.localPosition=new Vector3(0,3.25f,0);hpRoot.rotation=cam.transform.rotation;
            Shape("Health track",PrimitiveType.Cube,Vector3.zero,new Vector3(1.2f,.13f,.04f),navy,hpRoot,false);
            healthBars[f.id]=Shape("Health",PrimitiveType.Cube,new Vector3(0,0,-.03f),new Vector3(1.15f,.09f,.04f),f.team==0?blue:red,hpRoot,false);
            var worldCanvas=new GameObject("Nickname").AddComponent<Canvas>();worldCanvas.renderMode=RenderMode.WorldSpace;worldCanvas.transform.SetParent(root);worldCanvas.transform.localPosition=new Vector3(0,3.6f,0);worldCanvas.transform.rotation=cam.transform.rotation;worldCanvas.transform.localScale=Vector3.one*.008f;
            root.position=P(f.position);nameLabels[f.id]=Label(worldCanvas.transform,(f.nickname??f.data.displayName)+(f.id==localPlayerId?" · YOU":f.human?"":" · AI"),Vector2.zero,new Vector2(250,35),22,cream,TextAnchor.MiddleCenter);return root;
        }
        void StartMatch()
        {
            if(network.room!=null && preparedSimulation==null){network.Send(new NetCommand{type="start"});return;}
            foreach(Transform t in actorsRoot)Destroy(t.gameObject);
            sim=preparedSimulation??new ArenaSimulation(roster,selected,Environment.TickCount,selectedMap,4);preparedSimulation=null;if(network.room==null){localPlayerId=0;sim.fighters[0].nickname=PlayerPrefs.GetString("Nickname","약사");sim.fighters[0].human=true;}sim.Event+=OnCombat;
            CreateMap(sim);soundtrack.Play(sim.map.key);for(int i=0;i<sim.fighters.Length;i++)actors[i]=MakeActor(sim.fighters[i]);
            var effectRoot=new GameObject("Character ability effects");effectRoot.transform.SetParent(actorsRoot,false);abilityEffects=effectRoot.AddComponent<AbilityEffects>();abilityEffects.Initialize(sim,localPlayerId);
            foreach(Transform t in actors[localPlayerId].GetComponentsInChildren<Transform>())t.gameObject.layer=8;
            for(int i=0;i<sim.fighters.Length;i++){beamViews[i]=Shape("Pooled ultimate beam",PrimitiveType.Cube,Vector3.zero,Vector3.one,roster[2].color,actorsRoot,false);beamViews[i].gameObject.SetActive(false);beamLife[i]=0;}
            portraitCam.enabled=true;podiumCam.enabled=false;
            for(int i=0;i<shotViews.Length;i++){var root=new GameObject("Pooled Tripo pill").transform;root.SetParent(actorsRoot,false);shotViews[i]=root;projectileMeshes[i]=root.gameObject.AddComponent<CapsuleProjectile>();projectileMeshes[i].Initialize();root.gameObject.SetActive(false);}
            for(int i=0;i<zoneViews.Length;i++){zoneViews[i]=Shape("Pooled hazard disc",PrimitiveType.Cylinder,Vector3.zero,Vector3.one,cream,actorsRoot,false);zoneViews[i].gameObject.SetActive(false);dropViews[i]=Shape("Pooled falling capsule",PrimitiveType.Capsule,Vector3.zero,Vector3.one,cream,actorsRoot,false);dropViews[i].gameObject.SetActive(false);}
            for(int i=0;i<robotViews.Length;i++){var r=new GameObject("Medical support drone").transform;r.SetParent(actorsRoot,false);r.gameObject.AddComponent<PharmacyDroneView>().Initialize();robotViews[i]=r;r.gameObject.SetActive(false);}
            for(int i=0;i<fx.Length;i++){fx[i]=Shape("Pooled shockwave",PrimitiveType.Sphere,Vector3.zero,Vector3.one,cream,actorsRoot,false);fx[i].gameObject.SetActive(false);fxLife[i]=0;}
            aimLine=Shape("Aim guide",PrimitiveType.Cube,Vector3.zero,new Vector3(.06f,.04f,4),blue,actorsRoot,false);
            playing=true;paused=false;mobileFire=false;touchMove=Vector2.zero;accumulator=0;realTime=0;titleScreen.SetActive(false);roomScreen.SetActive(false);lobby.SetActive(false);result.SetActive(false);pausePanel.SetActive(false);hud.SetActive(true);
            heroLabel.text=roster[selected].displayName+"  /  "+roster[selected].role;status.text=sim.map.name+"  ·  FIRST TO 20";noticeTimer=4;
        }
        void Update()
        {
            if(!playing){if(result && result.activeSelf && sim!=null){int team=sim.winner<0?0:sim.winner;for(int i=0;i<sim.fighters.Length;i++)if(sim.fighters[i].team==team)actors[i].position=new Vector3((i%sim.TeamSize-(sim.TeamSize-1)*.5f)*2.5f,Mathf.Sin(Time.time*3+i)*.07f,0);}return;}
            realTime+=Time.unscaledDeltaTime;
            if(Input.GetKeyDown(KeyCode.Escape)){paused=!paused;pausePanel.SetActive(paused);}
            soundtrack.Paused=paused;
            if(paused&&mobileControls)mobileControls.ResetInput();
            if(Input.GetKeyDown(KeyCode.F2))ToggleMovementOverlay();
            if(paused && network.room==null)return;
            var f=sim.fighters[localPlayerId];
            Vector2 move=new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical"))+touchMove;
            Vector2 aim=touchAim*f.data.range;
            if(!RoomClient.TouchDevice){Ray ray=cam.ScreenPointToRay(Input.mousePosition);Plane plane=new Plane(Vector3.up,Vector3.zero);if(plane.Raycast(ray,out float d)){Vector3 point=ray.GetPoint(d);aim=new Vector2(point.x,point.z)-f.position;}}
            bool fire=!paused && (RoomClient.TouchDevice?mobileFire:Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject() || mobileFire);
            skillRequested|=Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.E);ultimateRequested|=Input.GetKeyDown(KeyCode.Space);
            if(network.room!=null){
                if(Time.unscaledTime>=sendAt){sendAt=Time.unscaledTime+1/30f;network.Send(new NetCommand{type="input",seq=++inputSequence,mx=paused?0:move.x,mz=paused?0:move.y,ax=aim.x,az=aim.y,attack=!paused&&fire,skill=!paused&&skillRequested,ultimate=!paused&&ultimateRequested});skillRequested=ultimateRequested=false;}
                RenderGame();UpdateHUD();if(sim.finished)Finish();return;
            }
            accumulator+=Mathf.Min(Time.deltaTime,.1f);
            if(smoke || captureResult)accumulator+=2;
            while(accumulator>=1f/60f){sim.Tick(1f/60f,move,aim,fire,skillRequested,ultimateRequested,smoke || capture);accumulator-=1f/60f;skillRequested=false;ultimateRequested=false;if(sim.finished)break;}
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
            float aspect=(float)Screen.width/Screen.height;cam.orthographicSize=referenceOasis?Mathf.Max(12.35f,21.956f/aspect):Mathf.Max(16,23/aspect);
            Vector3 target=new Vector3(0,28,-33.3691f);
            cam.transform.position=dt==0?target:Vector3.Lerp(cam.transform.position,target,1-Mathf.Exp(-dt*5));
            if(shake>0 && !referenceOasis){shake-=dt;cam.transform.position+=new Vector3(Mathf.Sin(Time.time*73),0,Mathf.Cos(Time.time*61))*.10f*shake;}
        }
        void RenderGame()
        {
            PositionCamera(sim.fighters[localPlayerId].position,Time.deltaTime);
            for(int i=0;i<sim.fighters.Length;i++)
            {
                var f=sim.fighters[i];actors[i].gameObject.SetActive(f.Alive);if(!f.Alive)continue;
                actors[i].position=network.room==null?P(f.position):Vector3.Lerp(actors[i].position,P(f.position),1-Mathf.Exp(-Time.deltaTime*24));actors[i].rotation=Quaternion.LookRotation(P(f.aim));
                healthBars[i].parent.rotation=cam.transform.rotation;nameLabels[i].canvas.transform.rotation=cam.transform.rotation;
                healthBars[i].localScale=new Vector3(1.15f*f.hp/f.data.maxHp,.09f,.04f);healthBars[i].localPosition=new Vector3(-.575f*(1-f.hp/f.data.maxHp),0,-.03f);
            }
            var player=sim.fighters[localPlayerId];portraitCam.transform.position=actors[localPlayerId].position+P(player.aim*3,1.65f);portraitCam.transform.LookAt(actors[localPlayerId].position+Vector3.up*1.4f);aimLine.gameObject.SetActive(player.Alive);float range=Mathf.Min(9,player.data.range);aimLine.position=P(player.position+player.aim*range*.5f,.2f);aimLine.rotation=Quaternion.LookRotation(P(player.aim));aimLine.localScale=new Vector3(.055f,.025f,range);
            for(int i=0;i<shotViews.Length;i++){var s=sim.shots[i];var v=shotViews[i];v.gameObject.SetActive(s.active);if(!s.active)continue;v.position=P(s.position,1.1f);v.rotation=Quaternion.LookRotation(P(s.direction))*Quaternion.Euler(90,0,0);projectileMeshes[i].Paint(sim.fighters[s.owner].data.color,s.kind);}
            for(int i=0;i<zoneViews.Length;i++){var z=sim.zones[i];var v=zoneViews[i];v.gameObject.SetActive(z.active);dropViews[i].gameObject.SetActive(z.active && z.pending);if(!z.active)continue;v.position=P(z.position,.18f);float pulse=z.pending?.9f+.1f*Mathf.Sin(Time.time*14):1;v.localScale=new Vector3(z.radius*2*pulse,.025f,z.radius*2*pulse);v.GetComponent<Renderer>().sharedMaterial=Mat(Glow(z.pending?new Color(1,.62f,.35f):sim.fighters[z.owner].data.color,.18f));if(z.pending){dropViews[i].position=P(z.position,1+z.remaining*5);dropViews[i].localScale=z.kind==3?new Vector3(1.3f,1.4f,1.3f):new Vector3(.35f,.6f,.35f);dropViews[i].GetComponent<Renderer>().sharedMaterial=Mat(sim.fighters[z.owner].data.color);}}
            for(int i=0;i<robotViews.Length;i++){var r=sim.robots[i];var v=robotViews[i];v.gameObject.SetActive(r.active);if(r.active){v.position=P(r.position);var owner=sim.fighters[r.owner];v.rotation=Quaternion.LookRotation(P(owner.aim));v.localScale=Vector3.one*(r.elite?1.5f:1);}}
            for(int i=0;i<coverViews.Count;i++)coverViews[i].gameObject.SetActive(sim.covers[i].Active);
            for(int i=0;i<sim.fighters.Length;i++)if(beamLife[i]>0){beamLife[i]-=Time.deltaTime;beamViews[i].gameObject.SetActive(beamLife[i]>0);}
            for(int i=0;i<fx.Length;i++)if(fxLife[i]>0){fxLife[i]-=Time.deltaTime;float age=1-fxLife[i]/.45f;fx[i].localScale=new Vector3(1+age*fxSize[i]*2,.15f+age*.2f,1+age*fxSize[i]*2);fx[i].gameObject.SetActive(fxLife[i]>0);}
        }
        void OnCombat(ArenaSimulation.CombatEvent e)
        {
            if(abilityEffects)abilityEffects.Emit(e);
            if((e.type=="skill"||e.type=="ultimate")&&!smoke)weaponAudio.Ability(sim.fighters[e.actor].data.kind,e.type=="ultimate",Vector2.Distance(e.position,sim.fighters[localPlayerId].position));
            if(e.type=="beam" && beamViews[e.actor]){var b=beamViews[e.actor];b.position=P(e.position+sim.fighters[e.actor].aim*e.size*.5f,1.2f);b.rotation=Quaternion.LookRotation(P(sim.fighters[e.actor].aim));b.localScale=new Vector3(.55f,.4f,e.size);b.gameObject.SetActive(true);beamLife[e.actor]=.24f;}
            if(e.type=="death")for(int i=0;i<fx.Length;i++)if(fxLife[i]<=0 && fx[i]){fxLife[i]=.45f;fxSize[i]=e.size;fx[i].position=P(e.position,.5f);fx[i].GetComponent<Renderer>().sharedMaterial=Mat(Glow(sim.fighters[e.actor].data.color,.3f));fx[i].gameObject.SetActive(true);break;}
            if(e.type=="ultimate"){shake=.8f;status.text=sim.fighters[e.actor].data.displayName+"  ULTIMATE!  "+sim.fighters[e.actor].data.voiceLine;noticeTimer=2.5f;var voice=sim.fighters[e.actor].data.ultimateVoice;if(voice)sfx.PlayOneShot(voice);}
            else if(e.type=="death"){PlayTone(2);feed.text=sim.fighters[e.actor].data.displayName+"  DOWN  ·  +1 POINT";}
            else if(e.type=="shoot"){if(!smoke)weaponAudio.Fire(e.actor,sim.fighters[e.actor].data.kind,Vector2.Distance(e.position,sim.fighters[localPlayerId].position));}
            else if(e.type=="hit"){if(!smoke)weaponAudio.Hit(e.actor==localPlayerId);if(actors[e.actor])actors[e.actor].GetComponent<PharmacistModelRig>()?.ReactToHit();}
            else if(e.type=="explosion"){shake=.35f;}
        }
        void UpdateHUD()
        {
            scoreBlue.text=sim.score[0].ToString("00");scoreRed.text=sim.score[1].ToString("00");timer.text=$"{Mathf.CeilToInt(sim.timeLeft)/60:00}:{Mathf.CeilToInt(sim.timeLeft)%60:00}";
            var f=sim.fighters[localPlayerId];hpText.text=$"{Mathf.CeilToInt(f.hp)} / {f.data.maxHp:0} HP";hpFill.fillAmount=f.hp/f.data.maxHp;
            float q=f.charge/f.data.ultimateRequirement;chargeFill.fillAmount=q;chargeText.text=q>=1?"ULTIMATE READY  ·  SPACE":$"ULTIMATE  {Mathf.FloorToInt(q*100)}%";
            skillText.text=f.skillTimer<=0?"SKILL  /  RMB":$"SKILL  {f.skillTimer:0.0}s";skillButton.interactable=f.Alive && f.skillTimer<=0;ultButton.interactable=f.Alive && q>=1;ultButton.GetComponent<Image>().color=q>=1?Color.Lerp(new Color(.75f,.5f,.1f),new Color(1,.85f,.25f),.5f+.5f*Mathf.Sin(Time.time*8)):new Color(.75f,.5f,.1f);
            combatView.UpdateState(sim,feed.text,localPlayerId);
            if(mobileControls)mobileControls.UpdateState(f);
            if(!f.Alive){status.text=$"RESPAWNING IN {Mathf.CeilToInt(f.respawn)}  ·  TEAM SPAWN";noticeTimer=.2f;}
            else {noticeTimer-=Time.deltaTime;if(noticeTimer<=0)status.text=sim.map.name+"  ·  FIRST TO 20  ·  F2 이동 구역 표시";}
        }
        void Finish()
        {
            if(abilityEffects)abilityEffects.Clear();
            playing=false;soundtrack.Paused=false;soundtrack.Play("menu");portraitCam.enabled=false;podiumCam.enabled=true;hud.SetActive(false);result.SetActive(true);bool win=sim.winner==sim.fighters[localPlayerId].team;
            resultView.Show(sim,localPlayerId);PlayTone(win?4:2);
            // Winning trio forms a podium and holds a celebratory pose.
            foreach(var t in shotViews)t.gameObject.SetActive(false);foreach(var t in zoneViews)t.gameObject.SetActive(false);foreach(var t in dropViews)t.gameObject.SetActive(false);foreach(var t in robotViews)t.gameObject.SetActive(false);foreach(var t in fx)t.gameObject.SetActive(false);aimLine.gameObject.SetActive(false);
            foreach(var t in beamViews)t.gameObject.SetActive(false);
            int team=sim.winner<0?0:sim.winner;for(int i=0;i<sim.fighters.Length;i++){actors[i].gameObject.SetActive(sim.fighters[i].team==team);if(sim.fighters[i].team==team){actors[i].position=new Vector3((i%sim.TeamSize-(sim.TeamSize-1)*.5f)*2.5f,0,0);actors[i].rotation=Quaternion.Euler(0,180+(i%sim.TeamSize-(sim.TeamSize-1)*.5f)*12,0);foreach(Transform t in actors[i])if(t.name=="Arm")t.localRotation=Quaternion.Euler(0,0,t.localPosition.x<0?65:-65);}}
            PositionCamera(Vector2.zero,0);
        }
        void MakeAudio()
        {
            weaponAudio=gameObject.AddComponent<WeaponAudio>();sfx=gameObject.AddComponent<AudioSource>();sfx.volume=.3f;gameObject.AddComponent<AudioListener>();soundtrack=gameObject.AddComponent<ArenaMusic>();
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
            TitleHotspot("START",new Vector2(0,-173),new Vector2(405,105),()=>{titleScreen.SetActive(false);roomScreen.SetActive(true);});
            TitleHotspot("EXIT",new Vector2(0,-293),new Vector2(318,80),ExitGame);
            lobby=Rect("Reference hero and arena selection",canvas.transform,Vector2.zero,new Vector2(1600,900)).gameObject;
            selectionView=lobby.AddComponent<ReferenceSelectionView>();
            selectionView.Initialize(roster,font,soundtrack,ChooseHero,ChooseMap,StartMatch,()=>{network.Leave();lobby.SetActive(false);roomScreen.SetActive(true);selectionView.SetRoom(null,"");},ready=>network.Send(new NetCommand{type="ready",ready=ready}),team=>network.Send(new NetCommand{type="team",team=team}));
            roomScreen=Rect("Guest room browser",canvas.transform,Vector2.zero,new Vector2(1600,900)).gameObject;browserView=roomScreen.AddComponent<RoomBrowserView>();browserView.Initialize(font,network,()=>{roomScreen.SetActive(false);lobby.SetActive(true);selectionView.SetRoom(null,"");},()=>{roomScreen.SetActive(false);titleScreen.SetActive(true);});roomScreen.SetActive(false);
            var musicButton=Btn(canvas.transform,"",new Vector2(675,425),new Vector2(210,34),navy,()=>{soundtrack.ToggleMute();musicLabel.text=soundtrack.Muted?"음악 OFF":"음악 ON";});
            globalMusicButton=musicButton.gameObject;
            musicLabel=Label(musicButton.transform,soundtrack.Muted?"음악 OFF":"음악 ON",Vector2.zero,new Vector2(200,32),16,cream,TextAnchor.MiddleCenter);
            Select(0);
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
            
            foreach(Transform oldHud in hud.transform)oldHud.gameObject.SetActive(oldHud.GetComponent<ArenaTouchStick>()!=null);
            combatView=hud.AddComponent<ReferenceCombatView>();
            combatView.Initialize(font,soundtrack,()=>{paused=true;pausePanel.SetActive(true);},()=>skillRequested=true,()=>ultimateRequested=true);
            if(RoomClient.TouchDevice)MakeMobileControls();
            result=Rect("Reference victory and defeat",canvas.transform,Vector2.zero,new Vector2(1600,900)).gameObject;
            resultView=result.AddComponent<ReferenceResultView>();
            resultView.Initialize(font,soundtrack,()=>{result.SetActive(false);lobby.SetActive(true);soundtrack.Play("menu");},StartMatch);
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
        void ChooseHero(int index){if(network.room!=null)network.Send(new NetCommand{type="hero",hero=index});else Select(index);}
        void ChooseMap(int index){if(network.room!=null)network.Send(new NetCommand{type="map",map=index});else SelectMap(index);}
        void OnRoom(RoomInfo room){if(networkRoomId!=room.id){networkRoomId=room.id;networkMatch=-1;playing=false;hud.SetActive(false);result.SetActive(false);}selectionView.SetRoom(room,network.playerId);selectedMap=room.map;selectionView.SelectArena(room.map);foreach(var member in room.members)if(member.id==network.playerId){localPlayerId=member.slot;if(member.hero>=0)Select(member.hero);}if(!playing&&!result.activeSelf){titleScreen.SetActive(false);roomScreen.SetActive(false);lobby.SetActive(true);}}
        void OnSnapshot(NetMessage message){
            if(message.match!=networkMatch){networkMatch=message.match;inputSequence=0;var heroes=new int[message.fighters.Length];for(int i=0;i<heroes.Length;i++)heroes[i]=message.fighters[i].hero;preparedSimulation=new ArenaSimulation(roster,selected,42,message.room.map,message.fighters.Length/2,heroes);foreach(var member in message.room.members){preparedSimulation.fighters[member.slot].nickname=member.nickname;preparedSimulation.fighters[member.slot].human=true;}StartMatch();}
            if(sim==null)return;sim.timeLeft=message.timeLeft;sim.score[0]=message.score[0];sim.score[1]=message.score[1];sim.finished=message.finished;sim.winner=message.winner;
            for(int i=0;i<sim.fighters.Length;i++)message.fighters[i].Apply(sim.fighters[i]);
            foreach(var shot in sim.shots)shot.active=false;foreach(var shot in message.shots)sim.shots[shot.index]=shot.value;
            foreach(var zone in sim.zones)zone.active=false;foreach(var zone in message.zones)sim.zones[zone.index]=zone.value;
            foreach(var robot in sim.robots)robot.active=false;foreach(var robot in message.robots)sim.robots[robot.index]=robot.value;
            for(int i=0;i<sim.covers.Count;i++)sim.covers[i].hp=message.covers[i];foreach(var e in message.events)OnCombat(e);
        }
        void MakeMobileControls(){
            var root=Rect("Mobile circular controls",hud.transform,Vector2.zero,new Vector2(1600,900));
            mobileControls=root.gameObject.AddComponent<MobileArenaControls>();mobileControls.Initialize(this,font);
        }
        public void MobileSkill(){if(playing&&!paused)skillRequested=true;}
        public void MobileUltimate(){if(playing&&!paused)ultimateRequested=true;}
        void SelectMap(int index){selectedMap=index;selectionView.SelectArena(index);}
        void Select(int index){selected=index;selectionView.SelectHero(index);}
        void LateUpdate(){if(globalMusicButton)globalMusicButton.SetActive(!lobby.activeSelf && !result.activeSelf && !hud.activeSelf && !roomScreen.activeSelf);}
        public void TouchInput(bool attack,Vector2 value,bool release){if(attack){if(value.sqrMagnitude>.01f)touchAim=value.normalized;mobileFire=!release && value.sqrMagnitude>.01f;}else touchMove=release?Vector2.zero:Vector2.ClampMagnitude(value,1);}
    }
}
