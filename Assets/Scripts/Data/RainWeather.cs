using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Turns rain on and off at random times.
// While it rains: random blurry spots fade in and out on the screen.
// Rain only happens if the level's LevelData has rainEnabled turned on.
public class RainWeather : MonoBehaviour
{
    [Header("Level settings")]
    public LevelData levelData;            // rain is only on when this level's rainEnabled is true

    public GameObject rainEffect;          // the RainEffect child object

    [Header("Rain sound (3D)")]
    public AudioClip rainSound;            // the rain.mp3 clip
    public float soundMinDistance = 5f;    // full volume this close to the rain
    public float soundMaxDistance = 40f;   // silent beyond this distance

    [Header("Timing (testing values)")]
    public float minWaitSeconds = 3f;      // time before rain starts
    public float maxWaitSeconds = 5f;
    public float minRainSeconds = 40f;     // how long the rain lasts
    public float maxRainSeconds = 90f;

    [Header("Screen blur spots")]
    public int maxBlurSpots = 10;          // how many blur spots at the same time
    public float blurMaxAlpha = 0.5f;      // how strong the blur looks (0 to 1)
    public float minBlurSize = 500f;       // size in pixels
    public float maxBlurSize = 1100f;

    private bool raining;
    private float timer;
    private Transform blurParent;
    private Sprite blurSprite;
    private int activeSpots;
    private AudioSource rainAudio;

    void Start()
    {
        rainEffect.SetActive(false);

        // This level has no rain: turn the whole system off and stop here
        if (levelData != null && !levelData.rainEnabled)
        {
            enabled = false;
            return;
        }

        SetupRainSound();
        MakeBlurCanvas();
        timer = Random.Range(minWaitSeconds, maxWaitSeconds);
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;

        if (raining) StopRain();
        else StartRain();
    }

    void StartRain()
    {
        raining = true;
        rainEffect.SetActive(true);
        if (rainAudio != null) rainAudio.Play();

        StartCoroutine(BlurLoop());
        timer = Random.Range(minRainSeconds, maxRainSeconds);
    }

    void StopRain()
    {
        raining = false;
        rainEffect.SetActive(false);
        if (rainAudio != null) rainAudio.Stop();

        timer = Random.Range(minWaitSeconds, maxWaitSeconds);
    }

    // Rain sound comes from the RainEffect object, so it is louder when the rain is close
    void SetupRainSound()
    {
        if (rainSound == null) return;

        rainAudio = rainEffect.AddComponent<AudioSource>();
        rainAudio.clip = rainSound;
        rainAudio.loop = true;
        rainAudio.playOnAwake = false;
        rainAudio.spatialBlend = 1f;   // 3D sound
        rainAudio.rolloffMode = AudioRolloffMode.Logarithmic;
        rainAudio.minDistance = soundMinDistance;
        rainAudio.maxDistance = soundMaxDistance;
        if (AudioManager.Instance != null)
            rainAudio.outputAudioMixerGroup = AudioManager.Instance.mixer.FindMatchingGroups("SFX")[0];
    }

    // Keeps making blur spots while it rains
    IEnumerator BlurLoop()
    {
        while (raining)
        {
            yield return new WaitForSeconds(Random.Range(0.2f, 0.8f));
            if (activeSpots < maxBlurSpots) StartCoroutine(BlurSpot());
        }
    }

    // One blur spot: fade in, stay a bit, fade out, then delete
    IEnumerator BlurSpot()
    {
        activeSpots++;

        GameObject spotObj = new GameObject("BlurSpot", typeof(RectTransform));
        spotObj.transform.SetParent(blurParent, false);
        Image img = spotObj.AddComponent<Image>();
        img.sprite = blurSprite;
        img.raycastTarget = false;

        float size = Random.Range(minBlurSize, maxBlurSize);
        img.rectTransform.sizeDelta = new Vector2(size, size);
        img.rectTransform.anchoredPosition = new Vector2(
            Random.Range(-Screen.width / 2f, Screen.width / 2f),
            Random.Range(-Screen.height / 2f, Screen.height / 2f));

        yield return Fade(img, 0f, blurMaxAlpha, Random.Range(0.5f, 1f));
        yield return new WaitForSeconds(Random.Range(0.3f, 1f));
        yield return Fade(img, blurMaxAlpha, 0f, Random.Range(1f, 2f));

        Destroy(spotObj);
        activeSpots--;
    }

    IEnumerator Fade(Image img, float from, float to, float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime;
            Color c = img.color;
            c.a = Mathf.Lerp(from, to, t / seconds);
            img.color = c;
            yield return null;
        }
    }

    // Makes one invisible overlay canvas under this object, used for the blur spots
    void MakeBlurCanvas()
    {
        GameObject canvasObj = new GameObject("BlurCanvas", typeof(RectTransform), typeof(Canvas));
        canvasObj.transform.SetParent(transform, false);

        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -10;   // behind the UI (Canvas is 0), so the win panel stays readable

        blurParent = canvasObj.transform;
        blurSprite = MakeBlurSprite();
    }

    // Creates a soft round white spot in code (no image file needed)
    Sprite MakeBlurSprite()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 center = new Vector2(size / 2f, size / 2f);

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center) / (size / 2f);
                float a = Mathf.Clamp01(1f - d);
                tex.SetPixel(x, y, new Color(0.9f, 0.95f, 1f, a * a));
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
