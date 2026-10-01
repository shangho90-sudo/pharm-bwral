using System;
using UnityEngine;
using UnityEngine.UI;
namespace PharmaBrawl
{
    public sealed class RoomBrowserView:MonoBehaviour
    {
        Font font;RoomClient client;InputField nickname,server,name;Text status;Transform list;
        int teamSize=4,page;RoomInfo[] available=new RoomInfo[0];
        readonly Color navy=new Color(.025f,.055f,.13f),cyan=new Color(.02f,.65f,.8f);
        RectTransform Box(Transform parent,string label,Vector2 p,Vector2 size){var go=new GameObject(label,typeof(RectTransform));go.transform.SetParent(parent,false);var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=p;r.sizeDelta=size;return r;}
        Image Fill(Transform parent,Vector2 p,Vector2 size,Color c){var i=Box(parent,"Panel",p,size).gameObject.AddComponent<Image>();i.color=c;return i;}
        Text Label(Transform parent,string value,Vector2 p,Vector2 size,int f=24){var t=Box(parent,value,p,size).gameObject.AddComponent<Text>();t.font=font;t.fontSize=f;t.color=Color.white;t.text=value;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.supportRichText=false;return t;}
        Button Button(Transform parent,string text,Vector2 p,Vector2 size,Action click){var bg=Fill(parent,p,size,cyan);var b=bg.gameObject.AddComponent<Button>();b.onClick.AddListener(()=>click());Label(bg.transform,text,Vector2.zero,size,23);return b;}
        InputField Input(string caption,Vector2 p,int width,string value){Label(transform,caption,p+new Vector2(0,46),new Vector2(width,35),21);var bg=Fill(transform,p,new Vector2(width,52),new Color(.08f,.13f,.23f));var input=bg.gameObject.AddComponent<InputField>();var text=Label(bg.transform,value,Vector2.zero,new Vector2(width-24,46),22);text.alignment=TextAnchor.MiddleLeft;input.textComponent=text;input.text=value;input.characterLimit=caption=="닉네임"?12:caption=="방 이름"?24:200;return input;}
        public void Initialize(Font f,RoomClient c,Action practice,Action back){
            font=f;client=c;Fill(transform,Vector2.zero,new Vector2(1600,900),navy);
            Label(transform,"팽브롤 · 함께할 약사 찾기",new Vector2(0,365),new Vector2(1200,80),42);
            Label(transform,"로그인 없이 닉네임으로 참가 · 최대 4 : 4",new Vector2(0,300),new Vector2(1200,45),23);
            nickname=Input("닉네임",new Vector2(-480,170),440,PlayerPrefs.GetString("Nickname","약사"));
            name=Input("방 이름",new Vector2(-480,60),440,"팽브롤 대전");
            server=Input("전용 게임 서버 주소",new Vector2(250,170),880,client.endpoint);
            for(int i=1;i<=4;i++){int size=i;Button(transform,i+" : "+i,new Vector2(-650+(i-1)*110,-35),new Vector2(100,55),()=>{teamSize=size;SetStatus(size+" : "+size+" 방을 만듭니다");});}
            Button(transform,"방 만들기",new Vector2(-480,-125),new Vector2(440,65),()=>{Save();client.Create(nickname.text,name.text,teamSize);});
            Button(transform,"방 새로고침",new Vector2(540,60),new Vector2(300,55),Refresh);
            Label(transform,"공개 대기방",new Vector2(45,60),new Vector2(200,55),26);
            list=Box(transform,"Rooms",new Vector2(250,-130),new Vector2(880,300));
            Button(transform,"이전",new Vector2(340,-290),new Vector2(160,45),()=>{page=Mathf.Max(0,page-1);DrawRooms();});Button(transform,"다음",new Vector2(530,-290),new Vector2(160,45),()=>{page=Mathf.Min(Mathf.Max(0,(available.Length-1)/5),page+1);DrawRooms();});
            Button(transform,"AI 연습 · 4 : 4",new Vector2(-480,-240),new Vector2(440,60),()=>{Save();client.Leave();practice();});
            Button(transform,"뒤로",new Vector2(-630,-380),new Vector2(180,55),back);
            status=Label(transform,"서버 주소 연결 후 공개 대기방을 조회할 수 있습니다",new Vector2(180,-350),new Vector2(1100,90),23);
        }
        void OnEnable(){if(server&&string.IsNullOrEmpty(server.text))server.text=client.endpoint;}
        void Save(){client.Configure(server.text);PlayerPrefs.SetString("Nickname",nickname.text);}
        public void SetStatus(string text){if(status)status.text=text;}
        public void Refresh(){Save();SetStatus("방을 조회하고 있습니다…");client.List(Show);}
        void Show(RoomList rooms){available=Array.FindAll(rooms.rooms??new RoomInfo[0],r=>!r.playing&&r.count<r.teamSize*2);page=0;DrawRooms();}
        void DrawRooms(){foreach(Transform t in list)Destroy(t.gameObject);SetStatus(available.Length==0?"대기 중인 방이 없습니다. 새 방을 만들어 주세요":"닉네임을 설정하고 참가할 방을 선택하세요 · "+(page+1)+"페이지");for(int i=0;i<5&&page*5+i<available.Length;i++){var room=available[page*5+i];Button(list,room.name+" · "+room.count+"/"+(room.teamSize*2)+" · "+room.teamSize+":"+room.teamSize,new Vector2(0,115-i*60),new Vector2(840,52),()=>{Save();client.Join(room.id,nickname.text);});}}

    }
}
