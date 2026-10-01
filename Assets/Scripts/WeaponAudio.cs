using UnityEngine;

namespace PharmaBrawl
{
    // Each attack family has its own generated Runway cue. Per-fighter throttles
    // prevent rapid bursts and crowded fights from producing excessive voices.
    public sealed class WeaponAudio : MonoBehaviour
    {
        readonly AudioClip[] shots = new AudioClip[10];
        readonly float[] nextShot = new float[8];
        readonly AudioClip[] skills=new AudioClip[10],ultimates=new AudioClip[10];
        AudioClip hit;
        AudioSource source;
        float nextHit;
        void Awake()
        {
            source=gameObject.AddComponent<AudioSource>();
            source.playOnAwake=false;source.volume=.45f;
            for(int i=0;i<shots.Length;i++){shots[i]=Resources.Load<AudioClip>("SFX/"+((AttackKind)i));skills[i]=Resources.Load<AudioClip>("SFX/"+((AttackKind)i)+"Skill");ultimates[i]=Resources.Load<AudioClip>("SFX/"+((AttackKind)i)+"Ultimate");}
            hit=Resources.Load<AudioClip>("SFX/Hit");
        }
        public void Fire(int actor, AttackKind kind, float distance)
        {
            if(actor<0 || actor>=nextShot.Length || Time.unscaledTime<nextShot[actor])return;
            nextShot[actor]=Time.unscaledTime+(kind==AttackKind.Burst?.25f:.12f);
            var clip=shots[(int)kind];
            if(clip)source.PlayOneShot(clip,Mathf.Lerp(1,.15f,Mathf.Clamp01(distance/20)));
        }
        public void Hit(bool local)
        {
            if(Time.unscaledTime<nextHit)return;
            nextHit=Time.unscaledTime+.09f;
            if(hit)source.PlayOneShot(hit,local?.8f:.22f);
        }
        public void Ability(AttackKind kind,bool ultimate,float distance){var clip=ultimate?ultimates[(int)kind]:skills[(int)kind];if(clip)source.PlayOneShot(clip,Mathf.Lerp(ultimate?1:.8f,.2f,Mathf.Clamp01(distance/20)));}
    }
}
