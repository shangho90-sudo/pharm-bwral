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
        AudioClip capsuleImpact,capsuleEnd,electricImpact,electricEnd,goldImpact;float nextElectric,nextElectricEnd;
        void Awake()
        {
            source=gameObject.AddComponent<AudioSource>();
            source.playOnAwake=false;source.volume=.45f;
            for(int i=0;i<shots.Length;i++){shots[i]=Resources.Load<AudioClip>("SFX/"+((AttackKind)i));skills[i]=Resources.Load<AudioClip>("SFX/"+((AttackKind)i)+"Skill");ultimates[i]=Resources.Load<AudioClip>("SFX/"+((AttackKind)i)+"Ultimate");}
            hit=Resources.Load<AudioClip>("SFX/Hit");
            capsuleImpact=OriginalCue("Capsule impact",.42f,85,true);capsuleEnd=OriginalCue("Capsule fade",.15f,420,false);
            electricImpact=OriginalCue("Electrical target crackle",.18f,1700,true);electricEnd=OriginalCue("Electrical discharge fade",.13f,700,false);goldImpact=OriginalCue("Golden penetration impact",.2f,1200,true);
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
        public void CapsuleImpact(bool ultimate,float distance){source.PlayOneShot(capsuleImpact,Mathf.Lerp(ultimate?1:.65f,.12f,Mathf.Clamp01(distance/20)));}
        public void CapsuleEnd(float distance){source.PlayOneShot(capsuleEnd,Mathf.Lerp(.16f,.03f,Mathf.Clamp01(distance/20)));}
        public void ElectricImpact(float distance,bool ultimate){if(Time.unscaledTime<nextElectric)return;nextElectric=Time.unscaledTime+.06f;source.PlayOneShot(electricImpact,Mathf.Lerp(ultimate?.8f:.5f,.08f,Mathf.Clamp01(distance/20)));}
        public void ElectricEnd(float distance){if(Time.unscaledTime<nextElectricEnd)return;nextElectricEnd=Time.unscaledTime+.1f;source.PlayOneShot(electricEnd,Mathf.Lerp(.14f,.03f,Mathf.Clamp01(distance/20)));}
        public void GoldenImpact(float distance){source.PlayOneShot(goldImpact,Mathf.Lerp(.3f,.05f,Mathf.Clamp01(distance/20)));}
        static AudioClip OriginalCue(string name,float duration,float frequency,bool noise){
            const int rate=22050;var samples=new float[(int)(rate*duration)];var random=new System.Random(57);
            for(int i=0;i<samples.Length;i++){float t=(float)i/rate,age=(float)i/samples.Length;float tone=Mathf.Sin(t*frequency*(1-age*.4f)*Mathf.PI*2);samples[i]=(tone+(noise?(float)(random.NextDouble()*2-1)*.65f:0))*Mathf.Pow(1-age,3)*.5f;}
            var clip=AudioClip.Create(name,samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
        }
        void OnDestroy(){if(capsuleImpact)Destroy(capsuleImpact);if(capsuleEnd)Destroy(capsuleEnd);if(electricImpact)Destroy(electricImpact);if(electricEnd)Destroy(electricEnd);if(goldImpact)Destroy(goldImpact);}
    }
}
