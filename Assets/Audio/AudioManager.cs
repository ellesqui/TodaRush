using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The one place for all sound: sound effects, music, and the time-of-day environment sound.
// Volumes come from MainMixer (MasterVolume, SFXVolume, MusicVolume). The settings sliders and Mute All call the public methods below.
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public AudioMixer mixer;
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 1f;
    private bool muted = false;

    [Header("Main menu")]
    public AudioClip mainMusic;          // loops on the menus and does not stop between them
    public AudioClip buttonClick;        // plays on every button click

    [Header("Gameplay sound effects")]
    public AudioClip hitSound;           // plays when the player gets hurt
    public AudioClip pickupSound;        // plays when the player picks up an item

    [Header("Environment (time of day)")]
    public AudioClip ruralClip;          // morning
    public AudioClip urbanClip;          // afternoon
    public AudioClip nightClip;          // night
    public NightController sky;          // same object as TripTimer
    public float fadeSeconds = 2f;

    [Header("Settings UI (main menu)")]
    public Slider musicSlider;
    public Slider sfxSlider;
    public Toggle muteToggle;
    public TMP_Text musicPercentText;
    public TMP_Text sfxPercentText;

    private const string PrefMusic = "Settings_MusicVolume";
    private const string PrefSfx = "Settings_SfxVolume";
    private const string PrefMute = "Settings_MuteAll";

    private AudioSource sfxSource;
    private AudioSource musicSource;
    private TripTimer tripTimer;
    private AudioClip currentEnvironment;

    void Awake()
    {
        // Only one AudioManager should exist.
        // Menu music survives between menu scenes. Gameplay replaces it.
        if (Instance != null && Instance != this)
        {
            if (mainMusic != null && Instance.mainMusic != null)
            {
                Destroy(gameObject);   // menu copy already playing, so keep the old one
                return;
            }
            Destroy(Instance.gameObject);
        }

        Instance = this;

        // Menu music keeps playing when the scene changes
        if (mainMusic != null) DontDestroyOnLoad(gameObject);

        // Saved settings (Music, SFX, Mute All)
        musicVolume = PlayerPrefs.GetFloat(PrefMusic, musicVolume);
        sfxVolume = PlayerPrefs.GetFloat(PrefSfx, sfxVolume);
        muted = PlayerPrefs.GetInt(PrefMute, 0) == 1;

        sfxSource = gameObject.AddComponent<AudioSource>();
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.volume = 0f;

        // Send each source to its group in the mixer
        sfxSource.outputAudioMixerGroup = mixer.FindMatchingGroups("SFX")[0];
        musicSource.outputAudioMixerGroup = mixer.FindMatchingGroups("Music")[0];
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        if (Instance != this) return;

        if (sky != null) tripTimer = sky.GetComponent<TripTimer>();
        ApplyVolumes();
        SyncSettingsUI();

        // Main menu uses the main music. Gameplay uses the environment sound.
        if (mainMusic != null)
        {
            if (!musicSource.isPlaying) PlayMusic(mainMusic);
        }
        else PlayEnvironment(ruralClip);

        ConnectButtonSounds();
    }

    // Each new scene has new buttons and new sliders, so connect them again
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ConnectButtonSounds();
        SyncSettingsUI();
    }

    void Update()
    {
        // Change the environment sound when the sky changes
        if (sky == null || tripTimer == null || !sky.nightEnabled || !tripTimer.IsTimerStarted()) return;

        float left = tripTimer.GetTimeRemaining();
        if (left <= sky.nightStartSeconds) PlayEnvironment(nightClip);
        else if (left <= sky.duskStartSeconds) PlayEnvironment(urbanClip);
    }

    // ===== SETTINGS (called by the sliders and the Mute All toggle) =====

    public void SetMusicVolume(float value)
    {
        musicVolume = value;
        PlayerPrefs.SetFloat(PrefMusic, value);
        ApplyVolumes();
        UpdatePercent(musicPercentText, value);
    }

    public void SetSfxVolume(float value)
    {
        sfxVolume = value;
        PlayerPrefs.SetFloat(PrefSfx, value);
        ApplyVolumes();
        UpdatePercent(sfxPercentText, value);
    }

    // Mute All: silences the whole mixer (Master), without changing the saved music and SFX sliders
    public void SetMuteAll(bool on)
    {
        muted = on;
        PlayerPrefs.SetInt(PrefMute, on ? 1 : 0);
        ApplyVolumes();
    }

    // Sends the current values to the mixer
    public void ApplyVolumes()
    {
        mixer.SetFloat("MasterVolume", muted ? -80f : ToDecibel(masterVolume));
        mixer.SetFloat("SFXVolume", ToDecibel(sfxVolume));
        mixer.SetFloat("MusicVolume", ToDecibel(musicVolume));
        PlayerPrefs.Save();
    }

    // Puts the saved values on the sliders, toggle, and percentage texts (if this scene has them)
    void SyncSettingsUI()
    {
        if (musicSlider != null)
        {
            musicSlider.SetValueWithoutNotify(musicVolume);
            musicSlider.onValueChanged.RemoveAllListeners();
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }
        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(sfxVolume);
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.AddListener(SetSfxVolume);
        }
        if (muteToggle != null)
        {
            muteToggle.SetIsOnWithoutNotify(muted);
            muteToggle.onValueChanged.RemoveAllListeners();
            muteToggle.onValueChanged.AddListener(SetMuteAll);
        }
        UpdatePercent(musicPercentText, musicVolume);
        UpdatePercent(sfxPercentText, sfxVolume);
    }

    void UpdatePercent(TMP_Text label, float value)
    {
        if (label != null) label.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    // ===== SOUND PLAYBACK =====

    // Play one sound effect (for example the horn)
    public void PlaySfx(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip);
    }

    // Play background music right away (loops)
    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        musicSource.clip = clip;
        musicSource.volume = 1f;
        musicSource.Play();
    }

    // Play the environment sound for the time of day (it loops and fades)
    public void PlayEnvironment(AudioClip clip)
    {
        if (clip == null || clip == currentEnvironment) return;
        currentEnvironment = clip;
        StartCoroutine(SwitchEnvironment(clip));
    }

    // Fade out, change the clip, then fade in
    IEnumerator SwitchEnvironment(AudioClip clip)
    {
        float startVolume = musicSource.volume;
        for (float t = 0f; t < fadeSeconds; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(startVolume, 0f, t / fadeSeconds);
            yield return null;
        }

        musicSource.clip = clip;
        musicSource.Play();

        for (float t = 0f; t < fadeSeconds; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(0f, 1f, t / fadeSeconds);
            yield return null;
        }
        musicSource.volume = 1f;
    }

    // Every button in the scene plays the click sound (including buttons in closed panels)
    void ConnectButtonSounds()
    {
        if (Instance != this || buttonClick == null) return;

        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
        foreach (Button button in buttons)
        {
            // Remove first, so a button never gets the sound twice
            button.onClick.RemoveListener(PlayButtonClick);
            button.onClick.AddListener(PlayButtonClick);
        }
    }

    void PlayButtonClick()
    {
        PlaySfx(buttonClick);
    }

    // The mixer uses decibels, so 0 to 1 is converted here
    float ToDecibel(float volume)
    {
        return Mathf.Log10(Mathf.Max(volume, 0.0001f)) * 20f;
    }
}
