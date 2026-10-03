using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace PharmaBrawl
{
    // Native browser composition handles Korean IME and mobile keyboards.
    public sealed class BrowserTextInput:MonoBehaviour,IPointerClickHandler
    {
        InputField field;
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]static extern void RoomEditText(string target,string value,int limit,float x,float y,float width,float height);
        [DllImport("__Internal")]static extern void RoomEndText(string target);
#endif
        public void Initialize(InputField input){field=input;gameObject.name="BrowserInput_"+input.gameObject.name;}
        public void OnPointerClick(PointerEventData unused){
#if UNITY_WEBGL && !UNITY_EDITOR
            var corners=new Vector3[4];field.GetComponent<RectTransform>().GetWorldCorners(corners);
            var bottom=RectTransformUtility.WorldToScreenPoint(null,corners[0]);var top=RectTransformUtility.WorldToScreenPoint(null,corners[2]);
            WebGLInput.captureAllKeyboardInput=false;
            RoomEditText(gameObject.name,field.text,field.characterLimit,bottom.x/Screen.width,1-top.y/Screen.height,(top.x-bottom.x)/Screen.width,(top.y-bottom.y)/Screen.height);
#endif
        }
        public void OnBrowserText(string value){field.text=value;}
        public void OnBrowserDone(string value){field.text=value;
#if UNITY_WEBGL && !UNITY_EDITOR
            WebGLInput.captureAllKeyboardInput=true;
#endif
            field.DeactivateInputField();EventSystem.current.SetSelectedGameObject(null);
        }
        void OnDisable(){
#if UNITY_WEBGL && !UNITY_EDITOR
            RoomEndText(gameObject.name);WebGLInput.captureAllKeyboardInput=true;
#endif
        }
    }
}
