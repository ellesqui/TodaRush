using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Turns rain on and off at random times.
// While it rains: puddles appear on the roads, and random blurry spots fade in and out on the screen.
public class RainWeather : MonoBehaviour
{
    public GameObject rainEffect;          // the RainEffect child object
    public GameObject puddlePrefab;        // the Puddle prefab
    public MeshFilter[] roadGuides;        // the 4 GUIDE_ROAD meshes
    public int puddleCount = 8;
    public float puddleScale = 3f;
    public float groundOffset = 0.2f;      // same height as the potholes

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
    private List<GameObject> puddles = new List<GameObject>();
    private Transform blurParent;
    private Sprite blurSprite;
    private int activeSpots;

    void Start()
    {
        rainEffect.SetActive(false);
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

        for (int i = 0; i < puddleCount; i++)
            SpawnPuddle();

        StartCoroutine(BlurLoop());
        timer = Random.Range(minRainSeconds, maxRainSeconds);
    }

    void StopRain()
    {
        raining = false;
        rainEffect.SetActive(false);

        foreach (GameObject puddle in puddles)
            if (puddle != null) Destroy(puddle);
        puddles.Clear();

        timer = Random.Range(minWaitSeconds, maxWaitSeconds);
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

    void SpawnPuddle()
    {
        Vector3 point;
        if (!RandomRoadPoint(out point)) return;

        // Lay the puddle flat on the ground, turned randomly
        GameObject puddle = Instantiate(puddlePrefab, point + Vector3.up * groundOffset,
            Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        puddle.transform.localScale = Vector3.one * puddleScale;
        puddles.Add(puddle);
    }

    // Picks a random spot on one of the road guide meshes
    bool RandomRoadPoint(out Vector3 result)
    {
        result = Vector3.zero;

        MeshFilter guide = roadGuides[Random.Range(0, roadGuides.Length)];
        Mesh mesh = guide.sharedMesh;
        Vector3[] verts = mesh.vertices;
        int[] tris = mesh.triangles;

        // Pick a random triangle, then a random point inside it
        int t = Random.Range(0, tris.Length / 3) * 3;
        Vector3 a = verts[tris[t]];
        Vector3 b = verts[tris[t + 1]];
        Vector3 c = verts[tris[t + 2]];

        float u = Random.value;
        float v = Random.value;
        if (u + v > 1f) { u = 1f - u; v = 1f - v; }

        Vector3 worldPoint = guide.transform.TransformPoint(a + u * (b - a) + v * (c - a));

        // Drop a ray down to find the ground under that point
        Ray ray = new Ray(worldPoint + Vector3.up * 50f, Vector3.down);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f, ~0, QueryTriggerInteraction.Ignore))
        {
            result = hit.point;
            return true;
        }
        return false;
    }
}
