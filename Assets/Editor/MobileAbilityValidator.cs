using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using PharmaBrawl;
public static class MobileAbilityValidator {
 static readonly BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static T Read<T>(PharmaGame game,string key)=>(T)typeof(PharmaGame).GetField(key,flags).GetValue(game);
 static void Require(bool ok,string reason){if(!ok)throw new Exception("Mobile ability validation: "+reason);}
 public static void Validate(){
  var root=new GameObject("Simultaneous mobile input QA");var game=root.AddComponent<PharmaGame>();typeof(PharmaGame).GetField("playing",flags).SetValue(game,true);
  var canvas=new GameObject("QA Canvas",typeof(Canvas)).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;var events=new GameObject("QA Events").AddComponent<EventSystem>();
  var movement=new GameObject("Movement pad",typeof(RectTransform)).AddComponent<ArenaTouchStick>();movement.transform.SetParent(canvas.transform,false);movement.game=game;movement.radius=70;
  var attack=new GameObject("Attack pad",typeof(RectTransform)).AddComponent<ArenaTouchStick>();attack.transform.SetParent(canvas.transform,false);attack.game=game;attack.attack=true;attack.radius=70;Canvas.ForceUpdateCanvases();
  Vector2 center=RectTransformUtility.WorldToScreenPoint(null,movement.transform.position);
  for(int angle=0;angle<360;angle+=10){var pointer=new PointerEventData(events){pointerId=1,position=center+new Vector2(Mathf.Cos(angle*Mathf.Deg2Rad),Mathf.Sin(angle*Mathf.Deg2Rad))*70};movement.OnPointerDown(pointer);Require(Vector2.Angle(Read<Vector2>(game,"touchMove"),(pointer.position-center))<.2f,"all 360 degree directions angle="+angle+" value="+Read<Vector2>(game,"touchMove"));movement.OnPointerUp(pointer);}
  var left=new PointerEventData(events){pointerId=11,position=center+new Vector2(45,45)};movement.OnPointerDown(left);
  var right=new PointerEventData(events){pointerId=22,position=RectTransformUtility.WorldToScreenPoint(null,attack.transform.position)+new Vector2(-50,30)};attack.OnPointerDown(right);
  Vector2 move=Read<Vector2>(game,"touchMove"),aim=Read<Vector2>(game,"touchAim");game.MobileSkill();game.MobileUltimate();Require(move.sqrMagnitude>.1f&&Read<bool>(game,"mobileFire")&&Read<bool>(game,"skillRequested")&&Read<bool>(game,"ultimateRequested"),"move, aim/fire, skill and ultimate concurrently");
  movement.OnPointerUp(right);Require(Read<Vector2>(game,"touchMove")==move,"foreign finger cannot release movement");attack.OnPointerUp(right);Require(!Read<bool>(game,"mobileFire")&&Read<Vector2>(game,"touchMove")==move&&Read<Vector2>(game,"touchAim")==aim,"attack release keeps movement and aim");movement.OnPointerUp(left);Require(Read<Vector2>(game,"touchMove")==Vector2.zero,"movement release clears");
  UnityEngine.Object.DestroyImmediate(canvas.gameObject);UnityEngine.Object.DestroyImmediate(events.gameObject);UnityEngine.Object.DestroyImmediate(root);Debug.Log("MOBILE_ABILITY_INPUT_PASS 36 radial angles and independent fingers with concurrent skill/ultimate; release ownership");
 }
}

