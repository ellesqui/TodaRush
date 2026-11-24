using System.Collections.Generic;
using UnityEngine;

// Places the level's destinations at random points ON THE ROAD, inside the area covered by the level's obstacles
// (the area is the bounding box of the Obstacles, leaving out cones, which are Level 3 props).
// Points near a Level 3 barrier (Level3Sign) are never used.
// Only runs when LevelData.randomDestinations is on. Level 1 (static destinations) is not affected.
public class RandomDestinationSpawner : MonoBehaviour
{
    public LevelData levelData;
    public Transform destinationsRoot;     // "Destinations"
    public Transform obstaclesRoot;        // "Obstacles"
    public Transform[] level3Signs;        // the Level3Sign barriers
    public Transform terminal;             // the tricycle terminal
    public float barrierRadius = 100f;     // no destination within this distance of a barrier
    public float groundOffset = 0.2f;      // same height as the other road objects
    public int maxAttempts = 3000;

    void Start()
    {
        if (levelData == null || !levelData.randomDestinations) return;
        if (destinationsRoot == null || obstaclesRoot == null) return;

        Bounds area = ObstacleArea();
        List<MeshFilter> roads = RoadMeshes();
        if (roads.Count == 0) return;

        List<Transform> destinations = new List<Transform>();
        foreach (Transform d in destinationsRoot)
        {
            if (d.name.StartsWith("Destination_")) destinations.Add(d);
        }

        int needed = Mathf.Min(levelData.destinationCount, destinations.Count);
        List<Vector3> placed = new List<Vector3>();

        for (int i = 0; i < needed; i++)
        {
            Vector3 point;
            if (FindRoadPoint(area, roads, placed, out point))
            {
                placed.Add(point);
                destinations[i].position = point + Vector3.up * groundOffset;
            }
        }
    }

    // Bounding box (XZ) of all the obstacles of the level, excluding cones and anything floating high up
    Bounds ObstacleArea()
    {
        Bounds b = new Bounds(Vector3.zero, Vector3.zero);
        bool first = true;
        foreach (Transform t in obstaclesRoot.GetComponentsInChildren<Transform>())
        {
            if (t == obstaclesRoot) continue;
            if (t.name.Contains("Cone")) continue;
            if (t.position.sqrMagnitude < 0.01f) continue;   // the empty group containers sit at (0,0,0)
            if (t.position.y > 40f) continue;                 // elevated objects are not on the road
            Vector3 p = new Vector3(t.position.x, 0f, t.position.z);
            if (first) { b = new Bounds(p, Vector3.zero); first = false; }
            else b.Encapsulate(p);
        }
        return b;
    }

    List<MeshFilter> RoadMeshes()
    {
        List<MeshFilter> list = new List<MeshFilter>();
        foreach (MeshFilter mf in FindObjectsByType<MeshFilter>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (mf.name.StartsWith("GUIDE_ROAD_LOWPOLY_") && mf.sharedMesh != null) list.Add(mf);
        }
        return list;
    }

    // Tries random points on the road guide meshes until one passes all the rules.
    // The point is taken directly from the road mesh, so it sits on the road (no raycast onto trees or slopes).
    bool FindRoadPoint(Bounds area, List<MeshFilter> roads, List<Vector3> placed, out Vector3 result)
    {
        result = Vector3.zero;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            MeshFilter guide = roads[Random.Range(0, roads.Count)];
            Mesh mesh = guide.sharedMesh;
            Vector3[] verts = mesh.vertices;
            int[] tris = mesh.triangles;

            int t = Random.Range(0, tris.Length / 3) * 3;
            Vector3 a = guide.transform.TransformPoint(verts[tris[t]]);
            Vector3 b = guide.transform.TransformPoint(verts[tris[t + 1]]);
            Vector3 c = guide.transform.TransformPoint(verts[tris[t + 2]]);

            float u = Random.value, v = Random.value;
            if (u + v > 1f) { u = 1f - u; v = 1f - v; }
            Vector3 p = a + u * (b - a) + v * (c - a);

            // Must be inside the obstacle area
            if (p.x < area.min.x || p.x > area.max.x || p.z < area.min.z || p.z > area.max.z) continue;

            // Must not be near a Level 3 barrier
            if (NearBarrier(p)) continue;

            // Must be far enough from the terminal and from the other destinations
            if (terminal != null && FlatDistance(p, terminal.position) < levelData.destinationMinFromTerminal) continue;
            bool tooClose = false;
            foreach (Vector3 q in placed)
            {
                if (FlatDistance(p, q) < levelData.destinationMinGap) { tooClose = true; break; }
            }
            if (tooClose) continue;

            result = p;
            return true;
        }
        return false;
    }

    bool NearBarrier(Vector3 p)
    {
        if (level3Signs == null) return false;
        foreach (Transform s in level3Signs)
        {
            if (s != null && FlatDistance(p, s.position) < barrierRadius) return true;
        }
        return false;
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f; b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
