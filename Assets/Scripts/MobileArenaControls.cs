using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace PharmaBrawl
{
    // One pointer owner per stick permits independent movement and aiming.
    public sealed class MobileArenaControls : MonoBehaviour
    {
        PharmaGame game; Font font; Sprite disc; Button skill, ultimate;
        Text skillLabel, ultimateLabel; ArenaTouchStick moveStick, attackStick;
        RectTransform Box(string name, Transform parent, Vector2 position, float size)
        {
            var obj=new GameObject(name,typeof(RectTransform));obj.transform.SetParent(parent,false);
            var rect=obj.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);
            rect.anchoredPosition=position;rect.sizeDelta=Vector2.one*size;return rect;
        }
        Image Circle(string name,Transform parent,Vector2 position,float size,Color color,bool interactive=false)
        {
            var image=Box(name,parent,position,size).gameObject.AddComponent<Image>();image.sprite=disc;
            image.color=color;image.raycastTarget=interactive;
            if(interactive)image.alphaHitTestMinimumThreshold=.1f;
            return image;
        }
        Text Label(Transform parent,string text,Vector2 position,float width,float height,int size)
        {
            var rect=Box(text,parent,position,width);rect.sizeDelta=new Vector2(width,height);
            var label=rect.gameObject.AddComponent<Text>();label.font=font;label.text=text;label.fontSize=size;
            label.color=Color.white;label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;return label;
        }
        static Sprite Disc()
        {
            const int size=256;var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
            texture.name="Mobile radial control";texture.wrapMode=TextureWrapMode.Clamp;
            var pixels=new Color[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++){
                float radius=new Vector2((x+.5f-size/2f)/(size/2f),(y+.5f-size/2f)/(size/2f)).magnitude;
                float edge=Mathf.Clamp01((1-radius)*size/2f);
                float rim=Mathf.Exp(-Mathf.Pow((radius-.94f)/.018f,2));
                float inner=Mathf.Exp(-Mathf.Pow((radius-.58f)/.008f,2))*.35f;
                float glow=Mathf.Exp(-Mathf.Pow((radius-.9f)/.07f,2))*.22f;
                float brightness=Mathf.Clamp01(rim+inner+glow);
                pixels[y*size+x]=new Color(Mathf.Lerp(.025f,1,brightness),Mathf.Lerp(.08f,1,brightness),Mathf.Lerp(.16f,1,brightness),edge*Mathf.Lerp(.76f,1,brightness));
            }
            texture.SetPixels(pixels);texture.Apply(); // Keep readable for circular hit testing.
            return Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),100);
        }
        public void Initialize(PharmaGame owner,Font uiFont)
        {
            game=owner;font=uiFont;disc=Disc();
            moveStick=Stick("360도 이동",new Vector2(-610,-115),280,false);
            attackStick=Stick("조준 / 공격",new Vector2(650,-255),220,true);
            skill=ActionButton("스킬",new Vector2(470,-160),130,new Color(.2f,.8f,1),game.MobileSkill,out skillLabel);
            ultimate=ActionButton("궁극기",new Vector2(650,-35),170,new Color(1,.78f,.2f),game.MobileUltimate,out ultimateLabel);
            Label(transform,"왼쪽 이동 · 오른쪽 조준 / 공격",new Vector2(0,-427),470,28,17);
        }
        ArenaTouchStick Stick(string name,Vector2 position,float size,bool attack)
        {
            var image=Circle(name,transform,position,size,new Color(.25f,.8f,1),true);
            var stick=image.gameObject.AddComponent<ArenaTouchStick>();stick.game=game;stick.attack=attack;
            stick.radius=size*.31f;
            stick.knob=Circle("손가락 방향",image.transform,Vector2.zero,size*.34f,new Color(.4f,.9f,1)).rectTransform;
            Label(image.transform,attack?"공격":"이동",new Vector2(0,-size*.33f),size*.6f,34,22);
            if(!attack){
                Label(image.transform,"▲",new Vector2(0,size*.36f),34,30,19);
                Label(image.transform,"◀",new Vector2(-size*.36f,0),34,30,19);
                Label(image.transform,"▶",new Vector2(size*.36f,0),34,30,19);
            }else Label(stick.knob,"✦",Vector2.zero,size*.3f,size*.3f,34);
            return stick;
        }
        Button ActionButton(string title,Vector2 position,float size,Color color,Action action,out Text label)
        {
            var image=Circle(title,transform,position,size,color,true);var button=image.gameObject.AddComponent<Button>();
            button.targetGraphic=image;button.onClick.AddListener(()=>action());
            var colors=button.colors;colors.highlightedColor=Color.white;colors.pressedColor=new Color(.6f,.8f,1);
            colors.disabledColor=new Color(.4f,.4f,.4f,.65f);button.colors=colors;
            label=Label(image.transform,title,Vector2.zero,size*.8f,size*.65f,24);return button;
        }
        public void UpdateState(ArenaSimulation.Fighter fighter)
        {
            skill.interactable=fighter.Alive&&fighter.skillTimer<=0;
            skillLabel.text=fighter.skillTimer>0?"스킬\n"+fighter.skillTimer.ToString("0.0")+"초":"스킬";
            float charge=Mathf.Clamp01(fighter.charge/fighter.data.ultimateRequirement);
            ultimate.interactable=fighter.Alive&&charge>=1;
            ultimateLabel.text=charge>=1?"궁극기\n준비 완료":"궁극기\n"+Mathf.FloorToInt(charge*100)+"%";
            if(!fighter.Alive)ResetInput();
        }
        public void ResetInput(){if(moveStick)moveStick.ResetInput();if(attackStick)attackStick.ResetInput();}
        void OnApplicationFocus(bool focus){if(!focus)ResetInput();}
        void OnApplicationPause(bool paused){if(paused)ResetInput();}
        void OnDisable(){ResetInput();}
        void OnDestroy(){if(disc){Destroy(disc.texture);Destroy(disc);}}
    }
    public sealed class ArenaTouchStick : MonoBehaviour,IPointerDownHandler,IDragHandler,IPointerUpHandler
    {
        public PharmaGame game;public bool attack;public float radius=70;public RectTransform knob;
        const int NoPointer=int.MinValue;int pointer=NoPointer;
        public void OnPointerDown(PointerEventData e){if(pointer!=NoPointer)return;pointer=e.pointerId;OnDrag(e);}
        public void OnDrag(PointerEventData e)
        {
            if(e.pointerId!=pointer)return;
            if(!RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,e.position,e.pressEventCamera,out Vector2 point))return;
            Vector2 raw=Vector2.ClampMagnitude(point/Mathf.Max(1,radius),1);
            if(knob)knob.anchoredPosition=raw*radius;
            // A radial dead zone prevents drift while preserving every angle and analog speed.
            float magnitude=raw.magnitude;
            Vector2 value=magnitude<=.12f?Vector2.zero:raw.normalized*((magnitude-.12f)/.88f);
            game.TouchInput(attack,value,false);
        }
        public void OnPointerUp(PointerEventData e){if(e.pointerId==pointer)ResetInput();}
        public void ResetInput(){pointer=NoPointer;if(knob)knob.anchoredPosition=Vector2.zero;if(game)game.TouchInput(attack,Vector2.zero,true);}
        void OnDisable(){ResetInput();}
    }
}
