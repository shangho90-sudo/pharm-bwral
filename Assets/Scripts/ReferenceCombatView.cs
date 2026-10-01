using System;
using UnityEngine;
using UnityEngine.UI;
namespace PharmaBrawl
{
    public sealed class ReferenceCombatView : MonoBehaviour
    {
        const float W=1673,H=940,S=1600f/W;
        Font font;ArenaMusic music;Texture2D atlas;
        Text blueScore,redScore,clock,mapName,heroName,hp,charge,killFeed,musicState,skillCooldown,ultimateState;
        Image hpBar,chargeBar;RawImage portrait;GameObject cooldown,muteLabel;Button skill,ultimate;
        Color Navy=>new Color(.025f,.04f,.09f,1);
        RectTransform Box(string name,Transform parent,Rect r){var g=new GameObject(name,typeof(RectTransform));g.transform.SetParent(parent,false);var t=g.GetComponent<RectTransform>();t.anchorMin=t.anchorMax=new Vector2(.5f,.5f);t.anchoredPosition=new Vector2((r.center.x-W/2)*S,(H/2-r.center.y)*S);t.sizeDelta=new Vector2(r.width*S,r.height*S);return t;}
        RawImage Art(string name,Rect r){var im=Box(name,transform,r).gameObject.AddComponent<RawImage>();im.texture=atlas;im.uvRect=new Rect(r.x/W,1-r.yMax/H,r.width/W,r.height/H);im.raycastTarget=false;return im;}
        Image Fill(Rect r,Color color){var im=Box("Live HUD background",transform,r).gameObject.AddComponent<Image>();im.color=color;im.raycastTarget=false;return im;}
        Text Label(string value,Rect r,int size,Color color){var t=Box(value,transform,r).gameObject.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=Mathf.RoundToInt(size*S);t.color=color;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Overflow;t.verticalOverflow=VerticalWrapMode.Overflow;return t;}
        Button Hotspot(string name,Rect r,Action action){var im=Box(name,transform,r).gameObject.AddComponent<Image>();im.color=Color.white;var b=im.gameObject.AddComponent<Button>();b.targetGraphic=im;b.onClick.AddListener(()=>action());var c=b.colors;c.normalColor=Color.clear;c.highlightedColor=new Color(1,1,1,.1f);c.selectedColor=Color.clear;c.pressedColor=new Color(1,1,1,.2f);c.disabledColor=new Color(0,0,0,.12f);b.colors=c;return b;}
        Image Bar(Rect r,Color color){var im=Fill(r,color);im.sprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,Texture2D.whiteTexture.width,Texture2D.whiteTexture.height),new Vector2(.5f,.5f));im.type=Image.Type.Filled;im.fillMethod=Image.FillMethod.Horizontal;return im;}
        public void Initialize(Font uiFont,ArenaMusic soundtrack,Action pause,Action useSkill,Action useUltimate)
        {
            font=uiFont;music=soundtrack;atlas=Resources.Load<Texture2D>("GameplayReference");
            foreach(var r in new[]{new Rect(19,17,61,59),new Rect(93,15,145,68),new Rect(568,10,539,78),new Rect(708,79,257,35),new Rect(1513,12,143,42),new Rect(1396,62,260,46),new Rect(20,768,575,148),new Rect(1318,757,149,151),new Rect(1486,757,151,151),new Rect(644,889,385,36)})Art("Reference HUD artwork",r);
            Hotspot("Pause",new Rect(19,17,61,59),pause);Hotspot("Music",new Rect(1513,12,143,42),()=>{music.ToggleMute();UpdateMusic();});
            Fill(new Rect(647,44,63,33),Navy);blueScore=Label("00",new Rect(647,42,63,36),34,Color.white);
            Fill(new Rect(964,43,63,34),Navy);redScore=Label("00",new Rect(964,42,63,36),34,Color.white);
            Fill(new Rect(777,25,120,48),Navy);clock=Label("03:00",new Rect(777,24,120,50),39,Color.white);
            Fill(new Rect(721,83,230,27),Navy);mapName=Label("",new Rect(721,82,230,30),17,Color.white);
            Fill(new Rect(1450,68,201,34),Navy);killFeed=Label("",new Rect(1404,67,241,35),16,Color.white);
            Fill(new Rect(166,784,413,37),Navy);heroName=Label("",new Rect(169,783,405,39),23,Color.white);
            Fill(new Rect(169,829,237,25),Navy);hpBar=Bar(new Rect(172,831,228,19),new Color(.07f,1,.13f));
            Fill(new Rect(410,826,166,31),Navy);hp=Label("",new Rect(409,826,167,33),20,Color.white);
            Fill(new Rect(170,870,232,20),Navy);chargeBar=Bar(new Rect(172,873,228,13),new Color(1,.82f,.1f));
            Fill(new Rect(408,863,169,36),Navy);charge=Label("",new Rect(407,863,170,38),20,Color.white);
            portrait=Art("Hero portrait",new Rect(28,777,125,129));
            skill=Hotspot("Skill RMB",new Rect(1318,757,149,151),useSkill);ultimate=Hotspot("Ultimate SPACE",new Rect(1486,757,151,151),useUltimate);
            cooldown=Fill(new Rect(1327,767,130,95),new Color(0,0,0,.58f)).gameObject;skillCooldown=Label("",new Rect(1327,771,130,85),36,Color.white);
            ultimateState=Label("",new Rect(1500,774,124,90),22,new Color(1,.87f,.2f));
            muteLabel=Fill(new Rect(1551,19,88,29),Navy).gameObject;musicState=Label("음악 OFF",new Rect(1547,17,96,33),19,Color.white);UpdateMusic();
        }
        void UpdateMusic(){muteLabel.SetActive(music.Muted);musicState.gameObject.SetActive(music.Muted);}
        void OnEnable(){if(muteLabel)UpdateMusic();}
        public void UpdateState(ArenaSimulation sim,string feed,int localPlayerId=0)
        {
            var f=sim.fighters[localPlayerId];blueScore.text=sim.score[0].ToString("00");redScore.text=sim.score[1].ToString("00");int sec=Mathf.CeilToInt(sim.timeLeft);clock.text=$"{sec/60:00}:{sec%60:00}";
            mapName.text=(sim.map.theme==ArenaTheme.Desert?"약초 오아시스":sim.map.name)+" · 20킬 선승";
            heroName.text=f.data.displayName+" / "+f.data.role;hp.text=$"{Mathf.CeilToInt(f.hp)} / {f.data.maxHp:0} HP";hpBar.fillAmount=f.hp/f.data.maxHp;
            float q=f.charge/f.data.ultimateRequirement;chargeBar.fillAmount=q;charge.text=f.Alive?"궁극기 "+Mathf.FloorToInt(q*100)+"%":"부활 "+Mathf.CeilToInt(f.respawn)+"초";
            skill.interactable=f.Alive && f.skillTimer<=0;ultimate.interactable=f.Alive && q>=1;cooldown.SetActive(f.skillTimer>0);skillCooldown.text=f.skillTimer>0?f.skillTimer.ToString("0.0"):"";ultimateState.text=q>=1?"READY":"";
            killFeed.text=feed.StartsWith("PHASE")?"":feed;
            int hero=(int)f.data.kind;
            if(hero==0){portrait.texture=atlas;portrait.uvRect=new Rect(28/W,1-906/H,125/W,129/H);}
            else {float[] x={35,270,494,714,940};portrait.texture=Resources.Load<Texture2D>("SelectionScreen");portrait.uvRect=new Rect((x[hero%5]+28)/1672f,1-((hero<5?137:416)+218)/941f,150/1672f,188/941f);}
        }
    }
}