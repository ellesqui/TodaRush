using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

// A floating item on the road. Touch it to get its effect, then it reappears later on a random road.
public class Pickup : MonoBehaviour
{
    public PickupData data;                     // required: settings asset
    public Transform visual;                    // the child that shows the icon (turns to face the camera)
    public Vector3 spawnCenter = new Vector3(29f, 23f, 16f);  // center of the road area
    public float spawnRadius = 260f;            // how far from the center to look for road
    public float floatHeight = 1f;              // how high it floats above the road
    public float bobHeight = 0.3f;              // how much it bobs up and down
    public float bobSpeed = 2f;
    public float minSpacing = 80f;              // same-type pickups stay at least this far apart (XZ)
    public float nearObstacleRadius = 25f;     // gas spawns within this distance of a Level 1/2 obstacle
    public float blockedDistance = 25f;        // never spawn this close to a Level 3 item
    public RandomDestinationSpawner roadSpawner;  // optional: use the destination road rules (Level 2+)

    private GameManager gameManager;
    private Collider pickupCollider;
    private float baseY;
    private bool collected = false;

    void Awake()
    {
        gameManager = FindAnyObjectByType<GameManager>();
        pickupCollider = GetComponent<Collider>();
    }

    void Start()
    {
        if (data == null)
        {
            Debug.LogWarning(name + " has no PickupData assigned!");
            return;
        }

        // Only show this item once the level is high enough
        if (gameManager == null || gameManager.levelNumber < data.minLevel)
        {
            gameObject.SetActive(false);
            return;
        }

        MoveToRandomRoad();
    }

    void Update()
    {
        // Keep the icon facing the camera
        if (visual != null && Camera.main != null)
        {
            visual.rotation = Camera.main.transform.rotation;
        }

        if (collected) return;

        // Floating effect
        float y = baseY + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected || data == null || !other.CompareTag("Player")) return;

        collected = true;
        ApplyEffect();
        PlayPlayerEffect();

        // Pick up sound
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(AudioManager.Instance.pickupSound);

        StartCoroutine(RespawnRoutine());
    }

    void ApplyEffect()
    {
        if (data.type == PickupType.Gas)
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                player.StartCoroutine(SpeedBoostRoutine(player));   // runs on the player so it always ends
            }
        }
        else if (data.type == PickupType.Timer)
        {
            TripTimer timer = FindAnyObjectByType<TripTimer>();
            if (timer != null) timer.AddTime(data.amount);
        }
    }

    // Plays the effect on the player's tricycle (GearFX, GasFX or TimerFX)
    void PlayPlayerEffect()
    {
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player == null) return;

        Transform fx = player.transform.Find("TricycleDriver/PlayerPickupFX/" + data.type + "FX");
        if (fx != null) fx.GetComponent<ParticleSystem>().Play(true);
    }

    // Multiplies the speed, then divides it back (works together with the slow effect)
    IEnumerator SpeedBoostRoutine(PlayerController player)
    {
        float factor = 1f + data.boostPercent;
        player.moveSpeed *= factor;
        player.boostActive = true;

        yield return new WaitForSeconds(data.boostSeconds);

        player.moveSpeed /= factor;
        player.boostActive = false;
    }

    IEnumerator RespawnRoutine()
    {
        SetVisible(false);

        yield return new WaitForSeconds(data.respawnDelay);

        MoveToRandomRoad();
        collected = false;
        SetVisible(true);
    }

    void SetVisible(bool on)
    {
        if (visual != null) visual.gameObject.SetActive(on);
        if (pickupCollider != null) pickupCollider.enabled = on;
    }

    // Random road spot NEAR a Level 1/2 obstacle (motor, dog, ball, pothole, car, traffic light),
    // away from Level 3 items (cones, branches, Level3Sign) and away from other pickups of this type.
    void MoveToRandomRoad()
    {
        NavMeshQueryFilter roadFilter = new NavMeshQueryFilter { agentTypeID = -1512830617, areaMask = NavMesh.AllAreas };
        var allowed = new System.Collections.Generic.List<Vector3>();
        var blocked = new System.Collections.Generic.List<Vector3>();

        foreach (Obstacle o in FindObjectsByType<Obstacle>(FindObjectsInactive.Include))
        {
            if (IsLevel3(o.name)) blocked.Add(o.transform.position);
            else allowed.Add(o.transform.position);
        }
        foreach (TrafficLight light in FindObjectsByType<TrafficLight>(FindObjectsInactive.Include))
            allowed.Add(light.transform.position);
        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Include))
            if (t.name.StartsWith("Level3Sign")) blocked.Add(t.position);

        if (allowed.Count == 0) return;

        for (int attempt = 0; attempt < 60; attempt++)
        {
            Vector3 center = allowed[Random.Range(0, allowed.Count)];
            Vector2 offset = Random.insideUnitCircle * nearObstacleRadius;
            Vector3 randomPoint = center + new Vector3(offset.x, 0f, offset.y);

            if (!NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 5f, roadFilter)) continue;
            if (IsNearAny(hit.position, blocked, blockedDistance)) continue;
            if (TooCloseToOtherPickup(hit.position)) continue;

            transform.position = hit.position + Vector3.up * floatHeight;
            baseY = transform.position.y;
            return;
        }
    }

    static bool IsLevel3(string objectName)
    {
        return objectName.StartsWith("Traffic Cone") || objectName.StartsWith("Branch");
    }

    static bool IsNearAny(Vector3 point, System.Collections.Generic.List<Vector3> spots, float distance)
    {
        foreach (Vector3 s in spots)
        {
            if (Vector2.Distance(new Vector2(point.x, point.z), new Vector2(s.x, s.z)) < distance) return true;
        }
        return false;
    }

    bool TooCloseToOtherPickup(Vector3 point)
    {
        foreach (Pickup other in FindObjectsByType<Pickup>(FindObjectsInactive.Include))
        {
            if (other == this || other.data == null || data == null || other.data.type != data.type) continue;
            if (Vector2.Distance(new Vector2(point.x, point.z), new Vector2(other.transform.position.x, other.transform.position.z)) < minSpacing) return true;
        }
        return false;
    }
}
