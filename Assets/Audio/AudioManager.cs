using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The one place for all sound: sound effects, music, and the time-of-day environment sound.
// Volumes come from MainMixer (MasterVolume, SFXVolume, MusicVolume).
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    public AudioMixer mixer;
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float sfxVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 1f;

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

        // Main menu uses the main music. Gameplay uses the environment sound.
        if (mainMusic != null)
        {
            if (!musicSource.isPlaying) PlayMusic(mainMusic);
        }
        else PlayEnvironment(ruralClip);

        ConnectButtonSounds();
    }

    // Each new scene has new buttons, so connect the click sound again
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ConnectButtonSounds();
    }

    void Update()
    {
        // Change the environment sound when the sky changes
        if (sky == null || tripTimer == null || !sky.nightEnabled || !tripTimer.IsTimerStarted()) return;

        float left = tripTimer.GetTimeRemaining();
        if (left <= sky.nightStartSeconds) PlayEnvironment(nightClip);
        else if (left <= sky.duskStartSeconds) PlayEnvironment(urbanClip);
    }

    // Call this after changing a volume value
    public void ApplyVolumes()
    {
        mixer.SetFloat("MasterVolume", ToDecibel(masterVolume));
        mixer.SetFloat("SFXVolume", ToDecibel(sfxVolume));
        mixer.SetFloat("MusicVolume", ToDecibel(musicVolume));
    }

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
