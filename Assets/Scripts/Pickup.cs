using System.Collections;
using UnityEngine;
using UnityEngine.AI;

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
                StartCoroutine(SpeedBoostRoutine(player));
                player.PlayBoostSound();   // Accelerate sound
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

    IEnumerator SpeedBoostRoutine(PlayerController player)
    {
        float original = player.moveSpeed;
        // Percentage increase: Speed x (1 + bonus%)
        player.moveSpeed = original * (1f + data.boostPercent);

        yield return new WaitForSeconds(data.boostSeconds);

        player.moveSpeed = original;
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

    // Picks random points inside the road area and snaps to the GUIDE road NavMesh,
    // so the item always sits on the road. Tries up to 30 times.
    void MoveToRandomRoad()
    {
        // Agent type of the GUIDE road NavMesh (Car)
        NavMeshQueryFilter roadFilter = new NavMeshQueryFilter { agentTypeID = -1512830617, areaMask = NavMesh.AllAreas };

        for (int attempt = 0; attempt < 30; attempt++)
        {
            Vector3 randomPoint = spawnCenter + new Vector3(Random.Range(-spawnRadius, spawnRadius), 0f, Random.Range(-spawnRadius, spawnRadius));

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 10f, roadFilter))
            {
                transform.position = hit.position + Vector3.up * floatHeight;
                baseY = transform.position.y;
                return;
            }
        }
    }
}
