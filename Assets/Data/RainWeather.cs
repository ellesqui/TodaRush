using System.Collections.Generic;
using UnityEngine;

// Turns rain on and off at random times.
// When rain starts, puddles appear on the road guides.
// When rain stops, the puddles are removed.
public class RainWeather : MonoBehaviour
{
    public GameObject rainEffect;          // the RainEffect object in the scene
    public GameObject puddlePrefab;        // the Puddle prefab
    public MeshFilter[] roadGuides;        // the 4 GUIDE_ROAD meshes
    public int puddleCount = 8;
    public float puddleScale = 3f;
    public float groundOffset = 0.2f;      // same height as the potholes

    public float minWaitSeconds = 20f;     // time before rain starts
    public float maxWaitSeconds = 60f;
    public float minRainSeconds = 40f;     // how long the rain lasts
    public float maxRainSeconds = 90f;

    private bool raining;
    private float timer;
    private List<GameObject> puddles = new List<GameObject>();

    void Start()
    {
        rainEffect.SetActive(false);
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
