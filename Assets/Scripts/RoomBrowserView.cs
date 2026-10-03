using System;
using UnityEngine;
using UnityEngine.UI;
namespace PharmaBrawl
{
 public sealed class RoomBrowserView:MonoBehaviour
 {
  const float W=1672,H=941,S=1600f/W;
  Font font;Texture2D artwork;RoomClient client;InputField nickname,server,name;Text status,badge;GameObject statusCover,roomsCover;
  int teamSize=4,page;RoomInfo[] available=new RoomInfo[0];
  readonly RawImage[] modes=new RawImage[4];Transform list;
  RectTransform Box(string label,Rect r){var go=new GameObject(label,typeof(RectTransform));go.transform.SetParent(transform,false);var rt=go.GetComponent<RectTransform>();rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);rt.anchoredPosition=new Vector2((r.center.x-W/2)*S,(H/2-r.center.y)*S);rt.sizeDelta=r.size*S;return rt;}
  RawImage Art(string label,Rect r,Rect source){var a=Box(label,r).gameObject.AddComponent<RawImage>();a.texture=artwork;a.uvRect=new Rect(source.x/W,1-source.yMax/H,source.width/W,source.height/H);a.raycastTarget=false;return a;}
  Text Label(string value,Rect r,int size=25){var t=Box(value,r).gameObject.AddComponent<Text>();t.font=font;t.fontSize=Mathf.RoundToInt(size*S);t.text=value;t.color=Color.white;t.alignment=TextAnchor.MiddleCenter;t.supportRichText=false;t.raycastTarget=false;return t;}
  Button Hotspot(string label,Rect r,Action click){var im=Box(label,r).gameObject.AddComponent<Image>();var b=im.gameObject.AddComponent<Button>();b.targetGraphic=im;var c=b.colors;c.normalColor=Color.clear;c.selectedColor=Color.clear;c.highlightedColor=new Color(1,1,1,.08f);c.pressedColor=new Color(1,1,1,.18f);b.colors=c;b.onClick.AddListener(()=>click());return b;}
  InputField Input(string key,Rect rect,string value,string placeholder,int limit){
   Art(key+" clean interior",new Rect(rect.x+6,rect.y+4,rect.width-12,rect.height-8),new Rect(rect.xMax-45,rect.y+4,12,rect.height-8));
   var im=Box(key,rect).gameObject.AddComponent<Image>();im.color=Color.clear;var input=im.gameObject.AddComponent<InputField>();input.targetGraphic=im;
   var content=new Rect(rect.x+24,rect.y+2,rect.width-48,rect.height-4);
   var text=Label(value,content,25);text.alignment=TextAnchor.MiddleLeft;text.horizontalOverflow=HorizontalWrapMode.Overflow;text.verticalOverflow=VerticalWrapMode.Truncate;
   var hint=Label(placeholder,content,24);hint.alignment=TextAnchor.MiddleLeft;hint.color=new Color(.55f,.65f,.82f);input.textComponent=text;input.placeholder=hint;input.characterLimit=limit;input.caretColor=Color.cyan;input.customCaretColor=true;input.selectionColor=new Color(0,.8f,1,.3f);input.text=value;
   text.transform.SetParent(input.transform,true);hint.transform.SetParent(input.transform,true);return input;
  }
  public void Initialize(Font f,RoomClient c,Action practice,Action back){
   font=f;client=c;artwork=Resources.Load<Texture2D>("RoomLobbyScreen");if(!artwork)throw new InvalidOperationException("Room lobby artwork missing");
   Art("Premium room lobby",new Rect(0,0,W,H),new Rect(0,0,W,H));
   nickname=Input("닉네임",new Rect(130,331,474,52),PlayerPrefs.GetString("Nickname","약사"),"닉네임을 입력하세요",12);
   name=Input("방 이름",new Rect(130,442,474,56),"팽브롤 대전","방 이름을 입력하세요",24);
   nickname.gameObject.AddComponent<BrowserTextInput>().Initialize(nickname);name.gameObject.AddComponent<BrowserTextInput>().Initialize(name);
   server=Input("게임 서버 주소",new Rect(695,332,844,49),client.endpoint,"서버 주소를 입력하세요",200);
   Art("Clean mode area",new Rect(126,556,483,75),new Rect(611,556,6,75));
   for(int i=0;i<4;i++){int pick=i+1;var r=new Rect(130+i*122,562,112,65);modes[i]=Art("Mode background",r,new Rect(130,562,112,65));Hotspot("Mode "+pick,r,()=>{teamSize=pick;UpdateModes();});}
   UpdateModes();Hotspot("방 만들기",new Rect(127,649,477,80),()=>{Save();client.Create(nickname.text,name.text,teamSize);});
   Hotspot("AI 연습",new Rect(128,746,476,75),()=>{Save();client.Leave();practice();});Hotspot("뒤로",new Rect(49,860,191,65),back);
   Hotspot("방 새로고침",new Rect(1333,422,221,55),Refresh);
   Hotspot("이전 페이지",new Rect(1167,776,178,49),()=>{if(page>0){page--;DrawRooms();}});Hotspot("다음 페이지",new Rect(1366,776,177,49),()=>{if((page+1)*5<available.Length){page++;DrawRooms();}});
   roomsCover=Art("Live room area",new Rect(689,496,856,268),new Rect(820,503,12,12)).gameObject;roomsCover.SetActive(false);
   statusCover=Art("Live status background",new Rect(810,655,650,91),new Rect(820,503,12,12)).gameObject;statusCover.SetActive(false);
   status=Label("",new Rect(807,657,680,88),25);
   var badgeFill=Box("Connection badge text background",new Rect(1435,293,98,25)).gameObject.AddComponent<Image>();badgeFill.color=new Color(.14f,.075f,.18f);badgeFill.raycastTarget=false;badge=Label("연결 대기",new Rect(1432,286,103,37),18);badge.color=new Color(1,.45f,.58f);
   list=new GameObject("Room rows",typeof(RectTransform)).transform;list.SetParent(transform,false);list.GetComponent<RectTransform>().sizeDelta=new Vector2(1600,900);
  }
  void UpdateModes(){for(int i=0;i<4;i++){var r=new Rect(130+i*122,562,112,65);Rect src=i+1==teamSize?new Rect(491,560,116,67):new Rect(130,562,112,65);modes[i].uvRect=new Rect(src.x/W,1-src.yMax/H,src.width/W,src.height/H);}
   // Labels are separate from the sampled button background, so mode selection never changes the numbers.
   if(!transform.Find("Live mode labels")){var group=new GameObject("Live mode labels",typeof(RectTransform));group.transform.SetParent(transform,false);group.GetComponent<RectTransform>().sizeDelta=new Vector2(1600,900);for(int i=0;i<4;i++){float x=130+i*122;var clean=Art("Mode label cover",new Rect(x+15,575,82,37),new Rect(142,572,10,37));clean.transform.SetParent(group.transform,true);var t=Label((i+1)+":"+(i+1),new Rect(x+8,568,96,53),29);t.transform.SetParent(group.transform,true);}}
  }
  void OnEnable(){if(server&&string.IsNullOrEmpty(server.text))server.text=client.endpoint;}
  void Save(){client.Configure(server.text);PlayerPrefs.SetString("Nickname",nickname.text);}
  public void SetStatus(string text){if(status){status.gameObject.SetActive(true);statusCover.SetActive(true);status.text=text;if(text.Contains("실패")||text.Contains("주소를 설정")){badge.text="연결 대기";badge.color=new Color(1,.45f,.58f);}}}
  public void Refresh(){Save();SetStatus("방을 조회하고 있습니다…");badge.text="조회 중";client.List(Show);}
  void Show(RoomList rooms){badge.text="연결 완료";badge.color=Color.cyan;available=Array.FindAll(rooms.rooms??new RoomInfo[0],r=>!r.playing&&r.count<r.teamSize*2);page=0;DrawRooms();}
  void DrawRooms(){foreach(Transform t in list)Destroy(t.gameObject);roomsCover.SetActive(available.Length>0);statusCover.SetActive(available.Length==0);status.gameObject.SetActive(available.Length==0);if(available.Length==0){SetStatus("대기 중인 방이 없습니다\n새 방을 만들어 주세요");return;}for(int i=0;i<5&&page*5+i<available.Length;i++){var room=available[page*5+i];var r=new Rect(704,502+i*50,817,44);var im=Box("Room "+room.name,r).gameObject.AddComponent<Image>();im.color=new Color(.035f,.16f,.32f,.97f);im.transform.SetParent(list,true);var b=im.gameObject.AddComponent<Button>();b.targetGraphic=im;b.onClick.AddListener(()=>{Save();client.Join(room.id,nickname.text);});var text=Label(room.name+"   ·   "+room.count+"/"+(room.teamSize*2)+"   ·   "+room.teamSize+":"+room.teamSize,r,23);text.transform.SetParent(im.transform,true);}}
 }
}
