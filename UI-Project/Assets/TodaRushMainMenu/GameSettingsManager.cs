using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public sealed class GameSettingsManager : MonoBehaviour
{
    public static GameSettingsManager Instance { get; private set; }
    public float MusicVolume { get; private set; }
    public float SfxVolume { get; private set; }
    public bool Muted { get; private set; }
    public const string Prefix="TODARUSH.Settings.";
    readonly Dictionary<AudioSource, float> music=new Dictionary<AudioSource,float>();
    readonly Dictionary<AudioSource, float> sfx=new Dictionary<AudioSource,float>();
    public static GameSettingsManager Ensure()
    {
        if(!Instance) new GameObject("GameSettingsManager").AddComponent<GameSettingsManager>();
        return Instance;
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatic(){Instance=null;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap(){Ensure();}
    void Awake()
    {
        if(Instance && Instance!=this){Destroy(gameObject);return;}
        Instance=this;DontDestroyOnLoad(gameObject);
        LoadPreferences();SceneManager.sceneLoaded+=OnSceneLoaded;
    }
    public void LoadPreferences()
    {
        MusicVolume=Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix+"Music",.8f));
        SfxVolume=Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix+"Sfx",.7f));
        Muted=PlayerPrefs.GetInt(Prefix+"Mute",PlayerPrefs.GetInt("TODARUSH.Muted",0))==1;
        ApplyAudio();
        if(PlayerPrefs.HasKey(Prefix+"Quality")) SetQuality(PlayerPrefs.GetInt(Prefix+"Quality"));
        if(PlayerPrefs.HasKey(Prefix+"Width")) ApplyResolution(PlayerPrefs.GetInt(Prefix+"Width"),PlayerPrefs.GetInt(Prefix+"Height"),PlayerPrefs.GetInt(Prefix+"Fullscreen",Screen.fullScreen?1:0)==1);
        else if(PlayerPrefs.HasKey(Prefix+"Fullscreen")) Screen.fullScreen=PlayerPrefs.GetInt(Prefix+"Fullscreen")==1;
    }
    void OnSceneLoaded(Scene scene,LoadSceneMode mode){ApplyAudio();}
    public void Register(AudioSource source,bool isMusic)
    {
        if(!source)return;
        if(music.ContainsKey(source)||sfx.ContainsKey(source))return;
        (isMusic?music:sfx)[source]=source.volume;ApplyAudio();
    }
    public void Unregister(AudioSource source)
    {
        if(!source)return;
        float baseline;
        if(music.TryGetValue(source,out baseline)||sfx.TryGetValue(source,out baseline))source.volume=baseline;
        music.Remove(source);sfx.Remove(source);
    }
    public void SetMusic(float v){MusicVolume=Mathf.Clamp01(v);PlayerPrefs.SetFloat(Prefix+"Music",MusicVolume);ApplyAudio();}
    public void SetSfx(float v){SfxVolume=Mathf.Clamp01(v);PlayerPrefs.SetFloat(Prefix+"Sfx",SfxVolume);ApplyAudio();}
    public void SetMute(bool v){Muted=v;PlayerPrefs.SetInt(Prefix+"Mute",v?1:0);ApplyAudio();}
    void ApplyAudio()
    {
        AudioListener.volume=Muted?0:1;
        Apply(music,MusicVolume);Apply(sfx,SfxVolume);
    }
    void Apply(Dictionary<AudioSource,float> sources,float value)
    {
        foreach(var source in sources.Keys.ToArray()){if(!source){sources.Remove(source);continue;}source.volume=sources[source]*value;}
    }
    public Resolution[] Resolutions(){return Screen.resolutions.GroupBy(r=>new Vector2Int(r.width,r.height)).Select(g=>g.Last()).ToArray();}
    public void ApplyResolution(int width,int height,bool fullscreen)
    {
        var match=Resolutions().FirstOrDefault(r=>r.width==width&&r.height==height);
        if(match.width==0)return;
        Screen.SetResolution(width,height,fullscreen?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed);
        PlayerPrefs.SetInt(Prefix+"Width",width);PlayerPrefs.SetInt(Prefix+"Height",height);PlayerPrefs.SetInt(Prefix+"Fullscreen",fullscreen?1:0);
    }
    public void SetFullscreen(bool v)
    {
        Screen.fullScreenMode=v?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed;
        PlayerPrefs.SetInt(Prefix+"Fullscreen",v?1:0);
    }
    public void SetQuality(int index)
    {
        if(index<0||index>=QualitySettings.names.Length)return;
        QualitySettings.SetQualityLevel(index,true);PlayerPrefs.SetInt(Prefix+"Quality",index);
    }
    public void Save(){PlayerPrefs.Save();}
    void OnApplicationPause(bool paused){if(paused)Save();}
    void OnApplicationQuit(){Save();}
    void OnDestroy(){if(Instance==this){SceneManager.sceneLoaded-=OnSceneLoaded;Instance=null;}}
}
