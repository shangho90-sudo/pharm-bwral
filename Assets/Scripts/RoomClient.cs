using System;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
namespace PharmaBrawl
{
    public sealed class RoomClient:MonoBehaviour
    {
        public string endpoint,playerId;public bool connected;public RoomInfo room;
        public Action<RoomInfo> RoomChanged;public Action<NetMessage> Snapshot;public Action<string> Error;
        RoomJoin session;float retryAt,retryDelay=1;bool leaving;
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]static extern void RoomSocketConnect(string url,string target,string hello);
        [DllImport("__Internal")]static extern void RoomSocketSend(string message);
        [DllImport("__Internal")]static extern void RoomSocketClose();
        [DllImport("__Internal")]static extern int RoomIsTouch();
#endif
        public static bool TouchDevice {
            get{
#if UNITY_WEBGL && !UNITY_EDITOR
                return RoomIsTouch()!=0;
#else
                return Application.isMobilePlatform;
#endif
            }
        }
        void Awake(){gameObject.name="PharmaRoomClient";endpoint=PlayerPrefs.GetString("GameServerUrl","");if(string.IsNullOrEmpty(endpoint))StartCoroutine(DefaultEndpoint());}
        [Serializable]sealed class ServerConfiguration{public string endpoint;}
        IEnumerator DefaultEndpoint(){using(var request=UnityWebRequest.Get(Application.streamingAssetsPath+"/server-config.json")){yield return request.SendWebRequest();if(request.result==UnityWebRequest.Result.Success){endpoint=JsonUtility.FromJson<ServerConfiguration>(request.downloadHandler.text).endpoint??"";}}}
        public void Configure(string url){endpoint=(url??"").Trim().TrimEnd('/');PlayerPrefs.SetString("GameServerUrl",endpoint);}
        [Serializable]sealed class ApiError{public string error;}
        [Serializable]sealed class Request{public string nickname,name;public int teamSize=4;}
        public void List(Action<RoomList> callback)=>StartCoroutine(Http("/rooms",null,text=>callback(JsonUtility.FromJson<RoomList>(text))));
        public void Create(string nickname,string name,int size)=>StartCoroutine(Http("/rooms",JsonUtility.ToJson(new Request{nickname=nickname,name=name,teamSize=size}),JoinResult));
        public void Join(string id,string nickname)=>StartCoroutine(Http("/rooms/"+UnityWebRequest.EscapeURL(id)+"/join",JsonUtility.ToJson(new Request{nickname=nickname}),JoinResult));
        IEnumerator Http(string path,string body,Action<string> done){
            if(!Uri.TryCreate(endpoint,UriKind.Absolute,out var uri)|| (uri.Scheme!="http"&&uri.Scheme!="https")){Error?.Invoke("전용 게임 서버 주소를 설정해 주세요");yield break;}
            using(var request=new UnityWebRequest(endpoint+path,body==null?"GET":"POST")){
                request.downloadHandler=new DownloadHandlerBuffer();request.timeout=12;
                if(body!=null){request.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));request.SetRequestHeader("Content-Type","application/json");}
                yield return request.SendWebRequest();
                if(request.result!=UnityWebRequest.Result.Success){string message=request.downloadHandler.text;if(message.StartsWith("{")){try{message=JsonUtility.FromJson<ApiError>(message).error;}catch(ArgumentException){message="서버 연결 실패";}}Error?.Invoke(string.IsNullOrEmpty(message)?"서버 연결 실패: "+request.error:message);yield break;}
                done(request.downloadHandler.text);
            }
        }
        void JoinResult(string text){session=JsonUtility.FromJson<RoomJoin>(text);playerId=session.playerId;leaving=false;retryDelay=1;Connect();}
        void Connect(){
#if UNITY_WEBGL && !UNITY_EDITOR
            RoomSocketConnect(endpoint.Replace("https://","wss://").Replace("http://","ws://")+"/socket",gameObject.name,JsonUtility.ToJson(new NetCommand{type="join",roomId=session.roomId,token=session.token}));
#else
            Error?.Invoke("온라인 연결은 WebGL 빌드에서 사용할 수 있습니다");
#endif
        }
        public void Send(NetCommand command){
#if UNITY_WEBGL && !UNITY_EDITOR
            if(connected)RoomSocketSend(JsonUtility.ToJson(command));
#endif
        }
        public void OnSocketOpen(string unused){connected=true;retryDelay=1;}
        public void OnSocketMessage(string text){if(leaving)return;var message=JsonUtility.FromJson<NetMessage>(text);if(message.room!=null){room=message.room;RoomChanged?.Invoke(room);}if(message.type=="snapshot")Snapshot?.Invoke(message);else if(message.type=="error")Error?.Invoke(message.error);}
        public void OnSocketClose(string unused){connected=false;if(!leaving){retryAt=Time.unscaledTime+retryDelay;retryDelay=Mathf.Min(20,retryDelay*2);Error?.Invoke("서버 연결 복구 중…");}}
        void Update(){if(!connected&&!leaving&&session!=null&&retryAt>0&&Time.unscaledTime>=retryAt){retryAt=0;Connect();}}
        public void Leave(){Send(new NetCommand{type="leave"});leaving=true;connected=false;session=null;room=null;
#if UNITY_WEBGL && !UNITY_EDITOR
            RoomSocketClose();
#endif
        }
    }
}
