using UnityEngine;

// Spawns pedestrians in waves:
// 1. Wait 15 seconds
// 2. Spawn 7 pedestrians, one at a time, within 15 seconds
// 3. When all are gone, wait 15 seconds, then start the next wave
public class PedestrianSpawner : MonoBehaviour
{
    public GameObject pedestrianPrefab;
    public Transform pointA;
    public Transform pointB;
    public BoxCollider zone;             // detection zone of the lane

    public float waitSeconds = 15f;      // wait before a wave and after a wave
    public int pedestriansPerWave = 7;
    public float spawnGap = 2.14f;       // 7 spawns fit in about 15 seconds
    public float hurrySpeedMultiplier = 2f;

    private float timer;
    private int spawnedThisWave;
    private int alive;
    private bool spawning;
    private PlayerController player;

    void Start()
    {
        timer = waitSeconds;
        player = FindAnyObjectByType<PlayerController>();
    }

    void Update()
    {
        // Waiting before a wave
        if (!spawning)
        {
            if (alive > 0) return;          // wait until everyone is gone

            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                spawning = true;
                spawnedThisWave = 0;
                timer = 0f;
            }
            return;
        }

        // Spawning one by one
        timer -= Time.deltaTime;
        if (timer > 0f) return;

        SpawnOne();
        spawnedThisWave++;
        timer = spawnGap;

        if (spawnedThisWave >= pedestriansPerWave)
        {
            spawning = false;
            timer = waitSeconds;            // wait 15 s after the wave is gone
        }
    }

    void SpawnOne()
    {
        GameObject npc = Instantiate(pedestrianPrefab, pointA.position, Quaternion.identity, transform);
        npc.GetComponent<PedestrianNPC>().Setup(this, pointB);
        alive++;
    }

    // True when the Player is inside the lane's detection zone
    public bool IsPlayerInZone()
    {
        if (player == null || zone == null) return false;
        return zone.bounds.Contains(player.transform.position);
    }

    // Called by the horn
    public void OnHorn()
    {
        foreach (PedestrianNPC npc in GetComponentsInChildren<PedestrianNPC>())
        {
            if (npc.IsWalker())
                npc.Hurry(hurrySpeedMultiplier);   // walkers go faster
            else
                npc.PopOut();                      // hesitate/taunt disappear now
        }
    }

    public void OnPedestrianGone()
    {
        alive--;
    }
}
