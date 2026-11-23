using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PassengerPickup : MonoBehaviour
{
    public UnityEvent Passenger;
    public Destination targetDestination;
    public GameManager gameManager;
    public bool hasBoarded = false;
    public Transform boardingPoint;
    public float boardingSpeed = 3f;      // walk speed toward the tricycle
    public float idleSeconds = 2f;        // idle for a moment after appearing, then walk to the tricycle
    private float spawnedAt;
    private bool isApproaching = false;
    public bool IsApproaching
    {
        get { return isApproaching; }
    }
    private Transform playerTransform;

    [Header("Identity (set at spawn)")]
    public string displayName = "Pasahero";   // Filipino name, tied to the model
    public string jobTitle = "";              // job = model prefab name

    [Header("Walking")]
    public float walkSeconds = 3f;   // how long the passenger walks after arriving (drop-off)
    public float walkSpeed = 1.5f;   // how fast the passenger walks back / drop-off

    private Vector3 spawnPosition;
    private bool isReturning = false;
    private int claimedSlot = -1;
    private Animator animator;
    private string lastState = "Idle";

    // Called by GameManager at the start of the game.
    // Replaces the placeholder "Model" child with the given random model, and sets name and job.
    public void SetupAtSpawn(GameObject modelPrefab, string name, Vector3 spot)
    {
        transform.localScale = Vector3.one;

        displayName = name;
        jobTitle = modelPrefab.name;

        Transform oldModel = transform.Find("Model");
        if (oldModel != null)
        {
            oldModel.name = "OldModel";
            Destroy(oldModel.gameObject);
        }

        Transform model = Instantiate(modelPrefab, transform).transform;
        model.name = "Model";
        model.localPosition = Vector3.zero;
        model.localRotation = Quaternion.identity;
        model.localScale = modelPrefab.transform.localScale;

        // Hide the capsule placeholders, but not the model itself
        HideCapsules(model);

        animator = model.GetComponentInChildren<Animator>();

        spawnPosition = spot;
        transform.position = spot;
        PlayState("Idle");
    }

    // Runs on its own, so the model shows even if GameManager does not spawn it
    void Start()
    {
        if (transform.Find("Model") == null) return;
        HideCapsules(transform.Find("Model"));
        animator = transform.Find("Model").GetComponentInChildren<Animator>();
        PlayState("Idle");
    }

    void HideCapsules(Transform model)
    {
        foreach (MeshRenderer capsule in GetComponentsInChildren<MeshRenderer>(true))
        {
            if (!capsule.transform.IsChildOf(model)) capsule.enabled = false;
        }
    }

    void OnEnable()
    {
        spawnedAt = Time.time;   // start the idle wait each time it appears
        PlayState(lastState);   // re-apply the animation once the Animator is active
    }

    void Update()
    {
        if (hasBoarded || gameManager == null || boardingPoint == null) return;

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;
            playerTransform = player.transform;
        }

        // Idle for a moment, then walk toward the tricycle (no distance check)
        if (!isApproaching && !isReturning && Time.time - spawnedAt >= idleSeconds)
        {
            StartApproach();
        }

        if (isApproaching)
        {
            Vector3 target = boardingPoint.position;
            target.y = transform.position.y;
            WalkTo(target, boardingSpeed);

            if (Vector3.Distance(transform.position, target) <= 0.1f)
            {
                Board();
            }
        }
        else if (isReturning)
        {
            WalkTo(spawnPosition, walkSpeed);

            if (Vector3.Distance(transform.position, spawnPosition) <= 0.1f)
            {
                isReturning = false;
                PlayState("Idle");
            }
        }
    }

    void StartApproach()
    {
        if (claimedSlot == -1)
        {
            for (int i = 0; i < gameManager.passengers.Length; i++)
            {
                if (gameManager.passengers[i] == null)
                {
                    gameManager.passengers[i] = this;
                    claimedSlot = i;
                    break;
                }
            }

            if (claimedSlot == -1) return; // no free slot
        }

        isReturning = false;
        isApproaching = true;
        PlayState("Walk");
    }

    void CancelApproach()
    {
        isApproaching = false;
        isReturning = true;

        if (claimedSlot >= 0) gameManager.passengers[claimedSlot] = null;
        claimedSlot = -1;

        PlayState("Walk");
    }

    void Board()
    {
        isApproaching = false;
        hasBoarded = true;
        gameManager.passengersOnboard++;

        // Fills this passenger's icon only (one at a time, as each one boards)
        Passenger.Invoke();

        if (gameManager.uiManager != null) gameManager.uiManager.PassengerBoarded(this);

        // Countdown starts only after ALL passengers are picked up
        if (gameManager.passengersOnboard >= gameManager.requiredLaps && gameManager.uiManager != null)
            gameManager.uiManager.StartCountdown();

        // Next passenger appears now (one at a time)
        ActivateNextPassenger();

        // Disappear as they board
        gameObject.SetActive(false);
    }

    // Passenger_01 -> Passenger_02 -> Passenger_03 (uses the object name)
    void ActivateNextPassenger()
    {
        if (transform.parent == null || name.Length < 2) return;
        string digits = name.Substring(name.Length - 2);
        int number;
        if (!int.TryParse(digits, out number)) return;
        Transform next = transform.parent.Find("Passenger_" + (number + 1).ToString("D2"));
        if (next != null) next.gameObject.SetActive(true);
    }

    void WalkTo(Vector3 target, float speed)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
    }

    void PlayState(string stateName)
    {
        lastState = stateName;
        if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null && animator.HasState(0, Animator.StringToHash(stateName)))
        {
            animator.Play(stateName);
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    [Header("Drop-off")]
    public float dropSeconds = 5f;        // how long the passenger jumps in place before disappearing
    public float groundOffset = 0.05f;    // keeps the feet just above the road surface
    private MeshFilter[] roadMeshes;

    // Called when the passenger arrives at the destination.
    // Starts on the GameManager, because this object is hidden at this point.
    // The DELIVERED notification is shown by Destination.DeliverPassenger, at the moment of drop-off.
    public IEnumerator DropRoutine()
    {
        // Appear beside the tricycle, placed on the road surface (not floating)
        Vector3 p = boardingPoint.position;
        transform.position = new Vector3(p.x, RoadHeightAt(p.x, p.z) + groundOffset, p.z);
        gameObject.SetActive(true);
        PlayState("Jump");

        // Drop-off speech
        if (gameManager.uiManager != null) gameManager.uiManager.PassengerDropped(this);

        // Stays in place and jumps for a few seconds, then disappears
        yield return new WaitForSeconds(dropSeconds);

        gameObject.SetActive(false);
    }

    // Road surface height under (x, z), from the GUIDE road meshes (the highest road surface)
    float RoadHeightAt(float x, float z)
    {
        if (roadMeshes == null)
        {
            var list = new System.Collections.Generic.List<MeshFilter>();
            foreach (var mf in FindObjectsByType<MeshFilter>())
                if (mf.name.StartsWith("GUIDE_ROAD_LOWPOLY_") && mf.sharedMesh != null) list.Add(mf);
            roadMeshes = list.ToArray();
        }

        float best = float.NaN;
        foreach (var mf in roadMeshes)
        {
            var v = mf.sharedMesh.vertices;
            var tri = mf.sharedMesh.triangles;
            Transform t = mf.transform;
            for (int i = 0; i < tri.Length; i += 3)
            {
                Vector3 a = t.TransformPoint(v[tri[i]]);
                Vector3 b = t.TransformPoint(v[tri[i + 1]]);
                Vector3 c = t.TransformPoint(v[tri[i + 2]]);
                float den = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z);
                if (Mathf.Abs(den) < 1e-6f) continue;
                float w1 = ((b.z - c.z) * (x - c.x) + (c.x - b.x) * (z - c.z)) / den;
                float w2 = ((c.z - a.z) * (x - c.x) + (a.x - c.x) * (z - c.z)) / den;
                float w3 = 1f - w1 - w2;
                if (w1 < -0.01f || w2 < -0.01f || w3 < -0.01f) continue;
                float y = w1 * a.y + w2 * b.y + w3 * c.y;
                if (float.IsNaN(best) || y > best) best = y;
            }
        }
        return float.IsNaN(best) ? boardingPoint.position.y : best;
    }
}
