using UnityEngine;

// One pedestrian: walks to point B, then disappears.
// Each pedestrian picks one behavior at random:
// 0 = walk only, 1 = hesitate, 2 = taunt.
// Hesitate and taunt play only when the Player comes close (once).
public class PedestrianNPC : MonoBehaviour
{
    public float walkSpeed = 1.5f;
    public float triggerDistance = 15f;   // how close the Player must be
    public float reactionSeconds = 3f;    // how long hesitate/taunt last
    public float modelFacingOffset = 0f;  // turn the model if it walks backwards
    public float stopDistance = 0.3f;
    public Vector3 standTilt = new Vector3(270f, 0f, 0f); // makes the model stand up

    private PedestrianSpawner spawner;
    private Transform pointB;
    private Animator anim;
    private PlayerController player;
    private int behavior;
    private bool reactionDone;
    private float reactionTimer;
    private bool gone;
    private Quaternion facingRotation;

    public void Setup(PedestrianSpawner owner, Transform end)
    {
        spawner = owner;
        pointB = end;
        anim = GetComponent<Animator>();
        player = FindAnyObjectByType<PlayerController>();
        behavior = Random.Range(0, 3);

        Vector3 dir = pointB.position - transform.position;
        dir.y = 0f;
        float yaw = Quaternion.LookRotation(dir).eulerAngles.y + modelFacingOffset;
        facingRotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(standTilt);
    }

    // 0 = walker, 1 and 2 = hesitate or taunt
    public bool IsWalker()
    {
        return behavior == 0;
    }

    // Walk faster from now on
    public void Hurry(float multiplier)
    {
        walkSpeed *= multiplier;
    }

    // Disappear right away
    public void PopOut()
    {
        if (gone) return;
        gone = true;
        spawner.OnPedestrianGone();
        Destroy(gameObject);
    }

    private void Update()
    {
        if (pointB == null || gone) return;

        // Stand still while hesitating or taunting
        if (reactionTimer > 0f)
        {
            reactionTimer -= Time.deltaTime;
            return;
        }

        Vector3 target = new Vector3(pointB.position.x, transform.position.y, pointB.position.z);
        transform.position = Vector3.MoveTowards(transform.position, target, walkSpeed * Time.deltaTime);

        // Reached point B: disappear
        if (Vector3.Distance(transform.position, target) <= stopDistance)
        {
            PopOut();
            return;
        }

        // Hesitate or taunt when the Player is close (only once)
        if (!reactionDone && behavior != 0 && player != null
            && Vector3.Distance(transform.position, player.transform.position) <= triggerDistance)
        {
            reactionDone = true;
            reactionTimer = reactionSeconds;
            anim.SetTrigger(behavior == 1 ? "Hesitate" : "Taunt");
        }
    }

    // LateUpdate runs after the Animator, so the Animator can't reset the rotation
    private void LateUpdate()
    {
        if (pointB == null || gone) return;
        transform.rotation = facingRotation;
    }
}
