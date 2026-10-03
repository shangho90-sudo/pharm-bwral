using System;
using UnityEngine;
using UnityEngine.UI;

namespace PharmaBrawl
{
    // Reference artwork is an atlas, not a flattened interaction surface.
    // All hotspots and changing data use the same 1672 x 941 artwork coordinates.
    public sealed class ReferenceSelectionView : MonoBehaviour
    {
        const float W=1672,H=941,S=1600f/W;
        Texture2D artwork;
        Font font;
        CharacterDefinition[] roster;
        ArenaMusic music;
        GameObject profile,heroMark,mapMark,firstHeroCheckCover,firstMapCheckCover;
        RawImage portrait;
        Text heroName,role,number,hp,speed,range,attack,skill,ultimate,musicState;
        Action<int> chooseHero,chooseArena;
        readonly Button[] heroButtons=new Button[10],mapButtons=new Button[4];
        readonly GameObject[] locks=new GameObject[10];
        readonly Text[] stars=new Text[10],owners=new Text[10];
        Button startButton;Text roomStatus,startStatus;Image startFill;
        readonly Button[] teamButtons=new Button[2];readonly Text[] teamLabels=new Text[2];
        RoomInfo currentRoom;RoomMember currentMember;
        readonly Rect[] heroes=new Rect[10],arenas=new Rect[4];
        Color Navy=>new Color(.018f,.045f,.12f,1);
        RectTransform Box(string name,Transform parent,Rect rect)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);
            var rt=go.GetComponent<RectTransform>();rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);
            rt.anchoredPosition=new Vector2((rect.center.x-W/2)*S,(H/2-rect.center.y)*S);
            rt.sizeDelta=new Vector2(rect.width*S,rect.height*S);return rt;
        }
        RawImage Art(string name,Transform parent,Rect destination,Rect source)
        {
            var raw=Box(name,parent,destination).gameObject.AddComponent<RawImage>();raw.texture=artwork;
            raw.uvRect=new Rect(source.x/W,1-source.yMax/H,source.width/W,source.height/H);raw.raycastTarget=false;return raw;
        }
        Image Fill(string name,Transform parent,Rect rect,Color color)
        {
            var image=Box(name,parent,rect).gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=false;return image;
        }
        Text TextAt(Transform parent,string value,Rect rect,int size,Color color,TextAnchor align=TextAnchor.MiddleLeft)
        {
            var text=Box(value,parent,rect).gameObject.AddComponent<Text>();text.font=font;text.text=value;text.fontSize=Mathf.RoundToInt(size*S);
            text.color=color;text.alignment=align;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Overflow;return text;
        }
        Button Hotspot(string name,Rect rect,Action action)
        {
            var image=Box(name,transform,rect).gameObject.AddComponent<Image>();image.color=Color.white;
            var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(()=>action());
            var colors=button.colors;colors.normalColor=Color.clear;colors.highlightedColor=new Color(1,1,1,.08f);
            colors.selectedColor=Color.clear;colors.pressedColor=new Color(1,1,1,.18f);button.colors=colors;return button;
        }
        GameObject Mark(string name,Rect rect,Color color)
        {
            var root=new GameObject(name,typeof(RectTransform));root.transform.SetParent(transform,false);
            var rt=root.GetComponent<RectTransform>();rt.sizeDelta=new Vector2(1600,900);
            Fill("top",root.transform,new Rect(rect.x+5,rect.y+1,rect.width-10,3),color);
            Fill("bottom",root.transform,new Rect(rect.x+5,rect.yMax-4,rect.width-10,3),color);
            Fill("left",root.transform,new Rect(rect.x+1,rect.y+5,3,rect.height-10),color);
            Fill("right",root.transform,new Rect(rect.xMax-4,rect.y+5,3,rect.height-10),color);
            // Reuse the reference's check artwork so typography and symbol stay identical.
            Art("selection check",root.transform,new Rect(rect.xMax-45,rect.y+9,35,35),name=="Selected arena"?new Rect(276,746,35,35):new Rect(210,143,36,36));
            return root;
        }
        void MoveMark(GameObject mark,Rect oldRect,Rect next)
        {
            mark.GetComponent<RectTransform>().anchoredPosition=new Vector2((next.x-oldRect.x)*S,-(next.y-oldRect.y)*S);
        }
        public void Initialize(CharacterDefinition[] definitions,Font uiFont,ArenaMusic soundtrack,Action<int> hero,Action<int> arena,Action start,Action back,Action<bool> ready,Action<int> team)
        {
            roster=definitions;font=uiFont;music=soundtrack;chooseHero=hero;chooseArena=arena;
            artwork=Resources.Load<Texture2D>("SelectionScreen");
            if(!artwork)throw new InvalidOperationException("SelectionScreen artwork missing");
            Art("User supplied selection artwork",transform,new Rect(0,0,W,H),new Rect(0,0,W,H));
            float[] x={35,270,494,714,940};float[] width={220,209,205,211,215};
            for(int i=0;i<10;i++){
                int pick=i;heroes[i]=new Rect(x[i%5],i<5?137:416,width[i%5],i<5?263:261);
                heroButtons[i]=Hotspot("Hero "+(i+1)+" "+roster[i].displayName,heroes[i],()=>chooseHero(pick));
            }
            float[] mapX={58,336,608,881};
            for(int i=0;i<4;i++){
                int pick=i;arenas[i]=new Rect(mapX[i],738,i==3?258:262,142);
                mapButtons[i]=Hotspot("Arena "+ArenaMap.Names[i],arenas[i],()=>chooseArena(pick));
            }
            startButton=Hotspot("Start selected hero and arena",new Rect(1180,800,460,94),()=>{if(currentRoom!=null&&currentMember!=null&&!currentMember.host)ready(!currentMember.ready);else start();});
            Hotspot("Back to title",new Rect(1508,29,132,50),back);
            Hotspot("Toggle music",new Rect(1302,28,178,50),()=>{music.ToggleMute();UpdateMusic();});
            // Hide only the baked first-card check when the user makes another choice.
            firstHeroCheckCover=Art("Unselected first hero check",transform,new Rect(210,143,36,36),new Rect(241,185,6,6)).gameObject;
            firstMapCheckCover=new GameObject("Unselected first arena",typeof(RectTransform));firstMapCheckCover.transform.SetParent(transform,false);firstMapCheckCover.GetComponent<RectTransform>().sizeDelta=new Vector2(1600,900);
            Art("Unselected arena frame",firstMapCheckCover.transform,arenas[0],arenas[1]);
            Art("Village thumbnail",firstMapCheckCover.transform,new Rect(63,743,251,100),new Rect(63,743,210,100));
            Fill("Village label background",firstMapCheckCover.transform,new Rect(63,845,250,29),Navy);
            TextAt(firstMapCheckCover.transform,"약국마을",new Rect(63,840,250,36),23,Color.white,TextAnchor.MiddleCenter);
            heroMark=Mark("Selected hero",heroes[1],new Color(.3f,.95f,1));
            mapMark=Mark("Selected arena",arenas[1],new Color(.1f,1,.75f));
            profile=new GameObject("Live selected hero profile",typeof(RectTransform));profile.transform.SetParent(transform,false);profile.GetComponent<RectTransform>().sizeDelta=new Vector2(1600,900);
            Fill("profile background",profile.transform,new Rect(1186,111,450,362),Navy);
            portrait=Art("Selected hero portrait",profile.transform,new Rect(1300,111,326,349),new Rect(282,161,185,196));
            number=TextAt(profile.transform,"",new Rect(1207,128,90,35),23,Color.white);
            heroName=TextAt(profile.transform,"",new Rect(1207,165,220,59),38,Color.white);
            role=TextAt(profile.transform,"",new Rect(1207,225,200,45),27,Color.cyan);
            Fill("live stats",profile.transform,new Rect(1207,484,414,58),Navy);
            Art("HP icon",profile.transform,new Rect(1212,490,45,47),new Rect(1212,490,45,47));
            Art("Speed icon",profile.transform,new Rect(1355,490,42,47),new Rect(1355,490,42,47));
            Art("Range icon",profile.transform,new Rect(1495,490,42,47),new Rect(1495,490,42,47));
            TextAt(profile.transform,"HP",new Rect(1263,489,74,20),15,Color.cyan);hp=TextAt(profile.transform,"",new Rect(1263,510,75,31),24,Color.white);
            TextAt(profile.transform,"SPEED",new Rect(1403,489,79,20),15,Color.cyan);speed=TextAt(profile.transform,"",new Rect(1403,510,75,31),24,Color.white);
            TextAt(profile.transform,"RANGE",new Rect(1546,489,75,20),15,Color.cyan);range=TextAt(profile.transform,"",new Rect(1546,510,75,31),24,Color.white);
            attack=Ability(559,"일반",new Color(1,.25f,.3f));skill=Ability(630,"스킬",new Color(1,.65f,.04f));ultimate=Ability(701,"궁극기",new Color(1,.8f,.04f));
            var musicLayer=Fill("muted music label",transform,new Rect(1354,36,114,34),Navy);
            musicState=TextAt(musicLayer.transform,"음악 OFF",new Rect(779,455,114,34),20,Color.white,TextAnchor.MiddleCenter);
            // Text's coordinate helper is global; align it locally to its label panel.
            musicState.rectTransform.anchoredPosition=Vector2.zero;
            for(int i=0;i<2;i++){
                int pick=i;var rect=new Rect(545+i*255,27,235,52);
                Fill(i==0?"BLUE team":"RED team",transform,rect,i==0?new Color(.03f,.28f,.62f):new Color(.65f,.08f,.18f));
                teamButtons[i]=Hotspot("Choose team "+i,rect,()=>team(pick));
                teamLabels[i]=TextAt(transform,i==0?"BLUE":"RED",rect,26,Color.white,TextAnchor.MiddleCenter);
            }
            roomStatus=TextAt(transform,"",new Rect(545,87,610,46),17,Color.cyan);
            // Cover the baked caption completely: exactly one live action label.
            startFill=Fill("Clean action button",transform,new Rect(1188,808,444,77),new Color(0,.77f,.81f));
            startStatus=TextAt(transform,"게임 시작",new Rect(1195,813,430,65),36,Navy,TextAnchor.MiddleCenter);
            for(int i=0;i<10;i++){locks[i]=Art("Reserved hero",transform,heroes[i],heroes[i]).gameObject;locks[i].GetComponent<RawImage>().material=new Material(Resources.Load<Shader>("GrayCard"));locks[i].SetActive(false);owners[i]=TextAt(transform,"",new Rect(heroes[i].x+5,heroes[i].yMax-72,heroes[i].width-10,28),19,Color.white,TextAnchor.MiddleCenter);owners[i].supportRichText=false;stars[i]=TextAt(transform,"★",new Rect(heroes[i].x+9,heroes[i].y+36,40,42),34,Color.yellow);stars[i].gameObject.SetActive(false);}
            UpdateMusic();SelectHero(0);SelectArena(0);SetRoom(null,"");
        }
        Text Ability(float y,string kind,Color color)
        {
            Fill("ability text background",profile.transform,new Rect(1280,y,335,56),Navy);
            TextAt(profile.transform,kind,new Rect(1287,y+9,70,40),21,color);
            return TextAt(profile.transform,"",new Rect(1360,y+3,251,53),18,Color.white);
        }
        void UpdateMusic(){musicState.transform.parent.gameObject.SetActive(music.Muted);}
        void OnEnable(){if(musicState)UpdateMusic();}
        public void SetRoom(RoomInfo room,string playerId){
            currentRoom=room;currentMember=null;
            if(room!=null)foreach(var member in room.members)if(member.id==playerId)currentMember=member;
            bool host=room==null||currentMember!=null&&currentMember.host;
            for(int i=0;i<10;i++){heroButtons[i].interactable=room==null||!room.playing;locks[i].SetActive(false);stars[i].gameObject.SetActive(false);owners[i].text="";}
            int blue=0,red=0,readyCount=0;
            if(room!=null)foreach(var member in room.members){
                if(member.team==0)blue++;else red++;if(member.ready)readyCount++;
                if(member.hero<0)continue;
                if(member.id!=playerId&&currentMember!=null&&member.team==currentMember.team){heroButtons[member.hero].interactable=false;locks[member.hero].SetActive(true);}
                stars[member.hero].gameObject.SetActive(member.host);
                owners[member.hero].text+=(owners[member.hero].text.Length>0?" / ":"")+(member.team==0?"B ":"R ")+member.nickname+(member.ready?" ✓":"");
            }
            foreach(var button in mapButtons)button.interactable=host&&(room==null||!room.playing);
            for(int i=0;i<2;i++){
                teamButtons[i].gameObject.SetActive(room!=null);teamLabels[i].gameObject.SetActive(room!=null);
                teamButtons[i].interactable=room!=null&&!room.playing;
                teamLabels[i].text=(i==0?"BLUE  "+blue:"RED  "+red)+(currentMember!=null&&currentMember.team==i?"  ✓":"");
                // Background panels follow the same online-only visibility.
                transform.Find(i==0?"BLUE team":"RED team").gameObject.SetActive(room!=null);
            }
            string block=RoomRules.StartBlockReason(room);
            startButton.interactable=room==null||currentMember!=null&&!room.playing&&(host?block.Length==0:currentMember.hero>=0);
            startStatus.text=host?"게임 시작":currentMember!=null&&currentMember.ready?"준비 취소":"준비 완료";
            startFill.color=startButton.interactable?new Color(0,.77f,.81f):new Color(.18f,.32f,.42f);
            startStatus.color=startButton.interactable?Navy:Color.white;
            roomStatus.text=room==null?"AI 연습 · 캐릭터와 맵 선택":room.name+" · BLUE "+blue+" : RED "+red+" · 준비 "+readyCount+"/"+room.count+"\n"+(block.Length==0?"준비 완료! 방장이 게임을 시작할 수 있습니다":block);
        }
        public void SetNotice(string message){if(roomStatus)roomStatus.text=message;}
        public void SelectHero(int index)
        {
            if(!profile)return;
            firstHeroCheckCover.SetActive(index!=0);heroMark.SetActive(index!=0);profile.SetActive(index!=0);
            MoveMark(heroMark,heroes[1],heroes[index]);
            var d=roster[index];var r=heroes[index];portrait.uvRect=new Rect((r.x+13)/W,1-(r.y+230)/H,(r.width-26)/W,198/H);
            number.text=(index+1).ToString("00");heroName.text=d.displayName;role.text=d.role;role.color=d.color;
            hp.text=d.maxHp.ToString("0");speed.text=d.speed.ToString("0.0");range.text=d.range.ToString("0.0");
            attack.text=d.attackDescription;skill.text=d.skillDescription+"\n재사용 "+d.skillCooldown.ToString("0")+"초";ultimate.text=d.ultimateDescription;
        }
        public void SelectArena(int index)
        {
            if(!mapMark)return;
            firstMapCheckCover.SetActive(index!=0);mapMark.SetActive(index!=0);MoveMark(mapMark,arenas[1],arenas[index]);
        }
    }
}