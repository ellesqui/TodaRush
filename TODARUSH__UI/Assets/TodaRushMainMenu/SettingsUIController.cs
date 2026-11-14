using UnityEngine;
using TMPro;
using System.Linq;
public sealed class SettingsUIController:MonoBehaviour
{
    [SerializeField] GameObject audioContent,graphicsContent,controlsContent;
    [SerializeField] UnityEngine.UI.Button audioButton,graphicsButton,controlsButton;
    [SerializeField] UnityEngine.UI.Slider musicSlider,sfxSlider;
    [SerializeField] UnityEngine.UI.Toggle muteToggle,fullscreenToggle;
    [SerializeField] TMP_Text musicPercentage,sfxPercentage;
    [SerializeField] TMP_Dropdown resolutionDropdown,qualityDropdown;
    Resolution[] resolutions;
    bool syncing;
    void OnEnable()
    {
        if(!Application.isPlaying)return;
        RefreshValues();ShowAudio();
    }
    public void RefreshValues()
    {
        syncing=true;var m=GameSettingsManager.Ensure();
        musicSlider.SetValueWithoutNotify(m.MusicVolume);sfxSlider.SetValueWithoutNotify(m.SfxVolume);
        muteToggle.SetIsOnWithoutNotify(m.Muted);SetKnob(muteToggle,m.Muted);UpdatePercentages();
        resolutions=m.Resolutions();resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(resolutions.Select(r=>r.width+" × "+r.height).ToList());
        resolutionDropdown.interactable=resolutions.Length>0;
        int width=PlayerPrefs.GetInt(GameSettingsManager.Prefix+"Width",Screen.width),height=PlayerPrefs.GetInt(GameSettingsManager.Prefix+"Height",Screen.height);
        int ri=System.Array.FindIndex(resolutions,r=>r.width==width&&r.height==height);
        resolutionDropdown.SetValueWithoutNotify(Mathf.Max(0,ri));resolutionDropdown.RefreshShownValue();
        qualityDropdown.ClearOptions();qualityDropdown.AddOptions(QualitySettings.names.ToList());qualityDropdown.SetValueWithoutNotify(QualitySettings.GetQualityLevel());qualityDropdown.RefreshShownValue();
        fullscreenToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(GameSettingsManager.Prefix+"Fullscreen",Screen.fullScreen?1:0)==1);SetKnob(fullscreenToggle,fullscreenToggle.isOn);syncing=false;
    }
    public void ShowAudio(){Show(0);}
    public void ShowGraphics(){Show(1);}
    public void ShowControls(){Show(2);}
    void Show(int index)
    {
        audioContent.SetActive(index==0);graphicsContent.SetActive(index==1);controlsContent.SetActive(index==2);
        var bs=new[]{audioButton,graphicsButton,controlsButton};
        for(int i=0;i<bs.Length;i++)bs[i].GetComponent<UnityEngine.UI.Image>().color=i==index?new Color32(242,140,40,255):new Color32(37,39,43,255);
    }
    void UpdatePercentages(){musicPercentage.text=Mathf.RoundToInt(musicSlider.value*100)+"%";sfxPercentage.text=Mathf.RoundToInt(sfxSlider.value*100)+"%";}
    public void MusicChanged(float v){if(syncing)return;GameSettingsManager.Ensure().SetMusic(v);UpdatePercentages();}
    public void SfxChanged(float v){if(syncing)return;GameSettingsManager.Ensure().SetSfx(v);UpdatePercentages();}
    public void MuteChanged(bool v){SetKnob(muteToggle,v);if(!syncing)GameSettingsManager.Ensure().SetMute(v);}
    public void ResolutionChanged(int index)
    {
        if(syncing||resolutions==null||index<0||index>=resolutions.Length)return;
        var r=resolutions[index];GameSettingsManager.Ensure().ApplyResolution(r.width,r.height,fullscreenToggle.isOn);
    }
    public void QualityChanged(int index){if(!syncing)GameSettingsManager.Ensure().SetQuality(index);}
    public void FullscreenChanged(bool v){SetKnob(fullscreenToggle,v);if(!syncing)GameSettingsManager.Ensure().SetFullscreen(v);}
    void SetKnob(UnityEngine.UI.Toggle toggle,bool on){var knob=toggle.transform.Find("Knob") as RectTransform;if(knob)knob.anchoredPosition=new Vector2(on?60:7,-6);}
    void OnDisable(){if(Application.isPlaying&&GameSettingsManager.Instance)GameSettingsManager.Instance.Save();}
}
