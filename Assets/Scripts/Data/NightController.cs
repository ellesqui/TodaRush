using System.Collections;
using UnityEngine;

// Sky and light cycle for the last 3 minutes of the trip (timer starts at the first passenger):
// 1) Morning: Skybox 1 with warm daylight
// 2) Afternoon: Skybox 5 (last 2 minutes), orange light, darker
// 3) Night: Skybox 9 (last 1 minute), blue dark light, headlight on
// Put this on the same object as TripTimer (GameManager).
public class NightController : MonoBehaviour
{
    [Header("Test switch")]
    public bool nightEnabled = true;      // turn off for levels that should not change sky

    [Header("Skyboxes")]
    public Material daySky;               // Skybox 1
    public Material duskSky;              // Skybox 5
    public Material nightSky;             // Skybox 9

    [Header("Timing (seconds left)")]
    public float duskStartSeconds = 120f; // 2 minutes left
    public float nightStartSeconds = 60f; // 1 minute left
    public float transitionSeconds = 8f;  // how long the blend from one time to the next takes
    public Shader skyBlendShader;         // TodaRush/Skybox Blend

    [Header("Sun (morning / afternoon / night)")]
    public float daySunIntensity = 2.84f;
    public float duskSunIntensity = 1.2f;
    public float nightSunIntensity = 0.3f;
    public Color daySunColor = new Color(1f, 0.92f, 0.78f);
    public Color duskSunColor = new Color(1f, 0.5f, 0.2f);
    public Color nightSunColor = new Color(0.4f, 0.5f, 1f);

    [Header("Ambient light (morning / afternoon / night)")]
    public Color dayAmbient = new Color(0.21f, 0.23f, 0.26f);
    public Color duskAmbient = new Color(0.3f, 0.2f, 0.18f);
    public Color nightAmbient = new Color(0.05f, 0.06f, 0.1f);

    [Header("Headlight")]
    public float headlightIntensity = 20f;

    private TripTimer tripTimer;
    private Light sun;
    private Light headlight;
    private int phase = 0;                // 0 = morning, 1 = afternoon, 2 = night
    private float targetSun;
    private Color targetSunColor;
    private Color targetAmbient;
    private Material skyBlend;

    private void Start()
    {
        tripTimer = GetComponent<TripTimer>();
        foreach (var l in FindObjectsByType<Light>())
        {
            if (l.type == LightType.Directional) { sun = l; break; }
        }
        if (!nightEnabled) return;

        if (daySky != null) RenderSettings.skybox = daySky;
        SetSkyBrightness(1f); // reset brightness in case a test left it dark
        targetSun = daySunIntensity;
        targetSunColor = daySunColor;
        targetAmbient = dayAmbient;
        SetupHeadlight();
    }

    private void Update()
    {
        if (!nightEnabled || tripTimer == null || !tripTimer.IsTimerStarted()) return;

        float left = tripTimer.GetTimeRemaining();

        if (phase < 2 && left <= nightStartSeconds)
        {
            phase = 2;
            targetSun = nightSunIntensity;
            targetSunColor = nightSunColor;
            targetAmbient = nightAmbient;
            StartCoroutine(BlendSky(duskSky, nightSky));
        }
        else if (phase < 1 && left <= duskStartSeconds)
        {
            phase = 1;
            targetSun = duskSunIntensity;
            targetSunColor = duskSunColor;
            targetAmbient = duskAmbient;
            StartCoroutine(BlendSky(daySky, duskSky));
        }

        // Light slowly changes toward the current phase
        if (sun != null)
        {
            sun.intensity = Mathf.MoveTowards(sun.intensity, targetSun, Time.deltaTime * 0.5f);
            sun.color = Color.Lerp(sun.color, targetSunColor, Time.deltaTime * 0.5f);
        }
        RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, targetAmbient, Time.deltaTime * 0.5f);

        // Headlight is only on at night
        if (headlight != null) headlight.enabled = phase == 2;
    }

    // Blends straight from one sky to the next (no black fade)
    private IEnumerator BlendSky(Material fromSky, Material toSky)
    {
        if (toSky == null) yield break;
        if (fromSky == null || skyBlendShader == null) { RenderSettings.skybox = toSky; yield break; }

        if (skyBlend == null) skyBlend = new Material(skyBlendShader);
        skyBlend.SetTexture("_Tex", fromSky.GetTexture("_Tex"));
        skyBlend.SetTexture("_Tex2", toSky.GetTexture("_Tex"));
        skyBlend.SetColor("_Tint", fromSky.GetColor("_Tint"));
        skyBlend.SetFloat("_Exposure", 1f);
        skyBlend.SetFloat("_Blend", 0f);
        RenderSettings.skybox = skyBlend;

        float t = 0f;
        while (t < transitionSeconds)
        {
            t += Time.deltaTime;
            skyBlend.SetFloat("_Blend", Mathf.Clamp01(t / transitionSeconds));
            yield return null;
        }

        // Stay on the blend shader (fully on the new sky) instead of switching to the
        // original material, whose brightness can be wrong after tests
        skyBlend.SetFloat("_Blend", 1f);
    }

    // 0 = black sky, 1 = normal sky (uses the _Exposure value of the skybox shader)
    private void SetSkyBrightness(float amount)
    {
        Material sky = RenderSettings.skybox;
        if (sky != null && sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", amount);
    }

    // Creates a spotlight on the tricycle that faces the same way as the player
    private void SetupHeadlight()
    {
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player == null) return;

        Transform lampPart = player.transform.Find("TricycleDriver/Tricycle_Root/Moto_Headlight");
        Vector3 lampLocal = lampPart != null ? player.transform.InverseTransformPoint(lampPart.position) : Vector3.up;

        GameObject lightObj = new GameObject("Headlight");
        lightObj.transform.SetParent(player.transform, false);
        lightObj.transform.localPosition = lampLocal;
        lightObj.transform.localRotation = Quaternion.identity;

        headlight = lightObj.AddComponent<Light>();
        headlight.type = LightType.Spot;
        headlight.color = new Color(1f, 0.95f, 0.8f);
        headlight.intensity = headlightIntensity;
        headlight.range = 30f;
        headlight.spotAngle = 70f;
        headlight.shadows = LightShadows.None;
        headlight.enabled = false;
    }
}
