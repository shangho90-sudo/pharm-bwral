using UnityEngine;

namespace PharmaBrawl
{
    // Two sources crossfade both scene changes and loop seams, preserving the weapon mix.
    public sealed class ArenaMusic : MonoBehaviour
    {
        readonly AudioSource[] sources=new AudioSource[2];
        int current;
        float transition=1;
        bool muted;
        public bool Muted => muted;
        public bool Paused { get; set; }
        const float Fade=1.5f;
        void Awake()
        {
            for(int i=0;i<2;i++){sources[i]=gameObject.AddComponent<AudioSource>();sources[i].playOnAwake=false;sources[i].spatialBlend=0;sources[i].volume=0;}
            muted=PlayerPrefs.GetInt("MusicMuted",0)!=0;
        }
        public void ToggleMute(){muted=!muted;PlayerPrefs.SetInt("MusicMuted",muted?1:0);}
        public void Play(string key)
        {
            var clip=Resources.Load<AudioClip>("Music/"+key);
            if(!clip){Debug.LogError("Missing soundtrack: "+key);return;}
            if(sources[current].clip==clip && sources[current].isPlaying)return;
            Switch(clip);
        }
        void Switch(AudioClip clip)
        {
            current=1-current;sources[current].Stop();sources[current].clip=clip;sources[current].time=0;sources[current].volume=0;sources[current].Play();transition=0;
        }
        void Update()
        {
            var source=sources[current];
            if(source.clip && transition>=1 && source.isPlaying && source.time>=source.clip.length-Fade)Switch(source.clip);
            transition=Mathf.Min(1,transition+Time.unscaledDeltaTime/Fade);
            float volume=muted?0:Paused?.1f:.25f;
            sources[current].volume=volume*transition;sources[1-current].volume=volume*(1-transition);
            if(transition>=1 && sources[1-current].isPlaying)sources[1-current].Stop();
        }
    }
}
