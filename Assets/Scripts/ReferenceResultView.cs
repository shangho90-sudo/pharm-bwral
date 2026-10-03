using System;
using UnityEngine;
using UnityEngine.UI;

namespace PharmaBrawl
{
    public sealed class ReferenceResultView : MonoBehaviour
    {
        const float W=1672,H=941,S=1600f/W;
        Font font;
        ArenaMusic music;
        Texture2D victory,defeat,portraits;
        RawImage backdrop;
        GameObject content,muteLabel;
        Color Navy=>new Color(.025f,.035f,.095f,1);
        RectTransform Box(string name,Transform parent,Rect r)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);
            var rt=go.GetComponent<RectTransform>();rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);
            rt.anchoredPosition=new Vector2((r.center.x-W/2)*S,(H/2-r.center.y)*S);rt.sizeDelta=new Vector2(r.width*S,r.height*S);return rt;
        }
        RawImage Art(string name,Transform parent,Texture2D texture,Rect r,Rect uv)
        {
            var im=Box(name,parent,r).gameObject.AddComponent<RawImage>();im.texture=texture;im.raycastTarget=false;
            im.uvRect=new Rect(uv.x/W,1-uv.yMax/H,uv.width/W,uv.height/H);return im;
        }
        Image Fill(Transform parent,Rect r,Color color)
        {
            var im=Box("Background",parent,r).gameObject.AddComponent<Image>();im.color=color;im.raycastTarget=false;return im;
        }
        Text Label(Transform parent,string value,Rect r,int size,Color color)
        {
            var t=Box(value,parent,r).gameObject.AddComponent<Text>();t.font=font;t.text=value;t.fontSize=Mathf.RoundToInt(size*S);
            t.color=color;t.alignment=TextAnchor.MiddleCenter;t.horizontalOverflow=HorizontalWrapMode.Overflow;t.verticalOverflow=VerticalWrapMode.Overflow;t.raycastTarget=false;
            var shadow=t.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.6f);shadow.effectDistance=new Vector2(0,-1);return t;
        }
        void Hotspot(string name,Rect r,Action action)
        {
            var im=Box(name,transform,r).gameObject.AddComponent<Image>();im.color=Color.white;
            var b=im.gameObject.AddComponent<Button>();b.targetGraphic=im;b.onClick.AddListener(()=>action());
            var c=b.colors;c.normalColor=Color.clear;c.highlightedColor=new Color(1,1,1,.1f);c.selectedColor=Color.clear;c.pressedColor=new Color(1,1,1,.2f);b.colors=c;
        }
        public void Initialize(Font uiFont,ArenaMusic soundtrack,Action select,Action rematch)
        {
            font=uiFont;music=soundtrack;victory=Resources.Load<Texture2D>("VictoryScreen");defeat=Resources.Load<Texture2D>("DefeatScreen");portraits=Resources.Load<Texture2D>("SelectionScreen");
            if(!victory || !defeat)throw new InvalidOperationException("Result reference artwork missing");
            backdrop=Art("Victory or defeat artwork",transform,victory,new Rect(0,0,W,H),new Rect(0,0,W,H));
            Hotspot("Character selection",new Rect(427,828,381,80),select);
            Hotspot("Rematch selected arena",new Rect(862,828,382,80),rematch);
            Hotspot("Toggle result music",new Rect(1475,15,180,47),()=>{music.ToggleMute();UpdateMusic();});
            muteLabel=Fill(transform,new Rect(1523,23,87,30),Navy).gameObject;
            var t=Label(muteLabel.transform,"음악 OFF",new Rect(W/2-50,H/2-16,100,32),19,Color.white);t.rectTransform.anchoredPosition=Vector2.zero;
            UpdateMusic();
        }
        void UpdateMusic(){muteLabel.SetActive(music.Muted);}
        void OnEnable(){if(muteLabel)UpdateMusic();}
        public void Show(ArenaSimulation sim,int localPlayerId=0)
        {
            if(content){content.SetActive(false);Destroy(content);}
            content=new GameObject("Actual match results",typeof(RectTransform));content.transform.SetParent(transform,false);content.GetComponent<RectTransform>().sizeDelta=new Vector2(1600,900);
            backdrop.texture=sim.winner==sim.fighters[localPlayerId].team?victory:defeat;
            Fill(content.transform,new Rect(741,188,78,54),Navy);Fill(content.transform,new Rect(864,188,79,54),Navy);
            Label(content.transform,sim.score[0].ToString(),new Rect(739,180,82,62),54,new Color(.72f,.94f,1));
            Label(content.transform,sim.score[1].ToString(),new Rect(861,180,85,62),54,new Color(1,.35f,.45f));
            if(sim.winner<0){Fill(content.transform,new Rect(583,4,511,181),Navy);Label(content.transform,"무승부\nDRAW",new Rect(600,15,478,160),58,Color.white);}
            float[] ys=new float[sim.fighters.Length];for(int i=0;i<ys.Length;i++)ys[i]=512+i*36+(i>=sim.TeamSize?8:0);
            if(sim.TeamSize==3)ys=new float[]{512,561,610,666,716,766};
            if(sim.TeamSize!=3){Fill(content.transform,new Rect(187,508,1298,306),Navy);Label(content.transform,"BLUE 팀",new Rect(195,520,115,130),23,Color.cyan);Label(content.transform,"RED 팀",new Rect(195,675,115,125),23,new Color(1,.3f,.45f));}
            float[] xs={410,655,830,1050,1280};float[] widths={215,143,188,191,191};
            for(int row=0;row<sim.fighters.Length;row++){
                // Explicit team order keeps the BLUE and RED table bands accurate.
                ArenaSimulation.Fighter f=null;int n=0;
                foreach(var candidate in sim.fighters)if(candidate.team==(row<sim.TeamSize?0:1)){if(n++==row%sim.TeamSize){f=candidate;break;}}
                if(f==null)throw new InvalidOperationException("Expected three fighters per team");
                string[] values={(f.nickname??f.data.displayName)+(f.id==localPlayerId?"  YOU":f.human?"":"  AI"),f.kills+" / "+f.deaths,f.damageDealt.ToString("0"),f.damageTaken.ToString("0"),f.healing.ToString("0")};
                for(int col=0;col<5;col++){
                    // Sample blank pixels from the same artwork row to retain its gradient.
                    var region=new Rect(xs[col],ys[row],widths[col],40);
                    if(sim.TeamSize==3)Art("Clear example cell",content.transform,(Texture2D)backdrop.texture,region,new Rect(574,ys[row],30,40));else Fill(content.transform,region,f.team==0?new Color(.03f,.16f,.34f):new Color(.25f,.05f,.14f));
                    Label(content.transform,values[col],region,sim.TeamSize==3?24:21,col==0 && f.id==localPlayerId?new Color(.35f,.95f,1):Color.white);
                }
                int hero=(int)f.data.kind;float[] cardX={35,270,494,714,940};
                float top=hero<5?137:416;
                // Headshot comes from the selected hero's reference card, matching the real roster.
                Art("Actual hero headshot",content.transform,portraits,new Rect(330,ys[row]-2,40,34),new Rect(cardX[hero%5]+35,top+20,140,165));
            }
            UpdateMusic();
        }
    }
}