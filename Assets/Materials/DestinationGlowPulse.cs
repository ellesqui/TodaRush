using UnityEngine;

// Pulsing glow + vertical light beam for a destination or terminal marker.
// Attach to the visual root (e.g. DestinationVisual or TerminalVisual).
// - Emission pulses through a MaterialPropertyBlock, so materials are not duplicated every frame.
// - The beam is a translucent cylinder whose alpha fades to zero toward its top (GTA-style).
// - beamBrightness > 1 makes the beam HDR so the bloom post-process picks it up.
public class DestinationGlowPulse : MonoBehaviour
{
    [Header("Color")]
    [Tooltip("Glow color for the marker and beam. Green for destinations, orange for the Terminal.")]
    public Color glowColor = new Color(1f, 0.45f, 0f, 1f);

    [Header("Pulse")]
    [Tooltip("Lowest emission intensity during the pulse.")]
    public float minIntensity = 1f;

    [Tooltip("Highest emission intensity during the pulse.")]
    public float maxIntensity = 3f;

    [Tooltip("Pulses per second.")]
    public float pulseSpeed = 1.2f;

    [Header("Light Beam")]
    [Tooltip("Create a vertical translucent beam above the marker.")]
    public bool createBeam = true;

    [Tooltip("Beam height in world units.")]
    public float beamHeight = 35f;

    [Tooltip("Beam radius in world units.")]
    public float beamRadius = 1.2f;

    [Tooltip("Beam opacity at its base, 0 to 1. The beam fades to zero toward the top.")]
    [Range(0f, 1f)]
    public float beamAlpha = 0.5f;

    [Tooltip("HDR brightness of the beam. Values above 1 make the bloom glow.")]
    public float beamBrightness = 3f;

    private MeshRenderer[] renderers;
    private MaterialPropertyBlock block;
    private Transform beam;
    private MeshRenderer beamRenderer;
    private bool initialized;

    void Awake()
    {
        EnsureInitialized();
    }

    void OnEnable()
    {
        EnsureInitialized();
    }

    // Safe to call many times: sets up lookups and the beam only once.
    void EnsureInitialized()
    {
        if (initialized)
        {
            return;
        }

        block = new MaterialPropertyBlock();
        renderers = GetComponentsInChildren<MeshRenderer>(true);
        initialized = true;

        if (createBeam && beamRenderer == null)
        {
            CreateBeam();
        }
    }

    void Update()
    {
        EnsureInitialized();

        float wave = (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
        float intensity = Mathf.Lerp(minIntensity, maxIntensity, wave);

        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer r = renderers[i];
            // Skip destroyed renderers and the beam itself (it has its own color).
            if (r == null || r == beamRenderer)
            {
                continue;
            }

            r.GetPropertyBlock(block);
            block.SetColor("_EmissionColor", glowColor * intensity);
            r.SetPropertyBlock(block);
        }

        if (beamRenderer != null)
        {
            float alpha = beamAlpha * (0.6f + 0.4f * wave);
            beamRenderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", new Color(glowColor.r, glowColor.g, glowColor.b, alpha) * beamBrightness);
            beamRenderer.SetPropertyBlock(block);
        }
    }

    void CreateBeam()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "GlowBeam";
        beam = go.transform;
        beam.SetParent(transform, true);

        // Compensate for the parent's scale so the beam keeps its world size.
        Vector3 lossy = transform.lossyScale;
        beam.localScale = new Vector3(
            beamRadius * 2f / Mathf.Max(lossy.x, 0.0001f),
            beamHeight / Mathf.Max(lossy.y, 0.0001f),
            beamRadius * 2f / Mathf.Max(lossy.z, 0.0001f));
        beam.position = transform.position + Vector3.up * (beamHeight * 0.5f);
        beam.rotation = Quaternion.identity;

        Destroy(go.GetComponent<Collider>());

        beamRenderer = go.GetComponent<MeshRenderer>();
        beamRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        beamRenderer.receiveShadows = false;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            return;
        }

        Material mat = new Material(shader);
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.SetColor("_BaseColor", new Color(glowColor.r, glowColor.g, glowColor.b, beamAlpha) * beamBrightness);

        // Vertical alpha gradient: opaque at the base, fading to zero at the top.
        Texture2D fade = new Texture2D(1, 64, TextureFormat.RGBA32, false);
        fade.wrapMode = TextureWrapMode.Clamp;
        fade.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < 64; y++)
        {
            float t = y / 63f;
            float a = Mathf.Pow(1f - t, 1.6f);
            fade.SetPixel(0, y, new Color(1f, 1f, 1f, a));
        }
        fade.Apply();
        mat.mainTexture = fade;
        mat.SetTexture("_BaseMap", fade);

        beamRenderer.sharedMaterial = mat;
    }

    void OnDestroy()
    {
        if (beam != null)
        {
            Destroy(beam.gameObject);
        }
    }
}
