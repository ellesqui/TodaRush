using UnityEngine;
using UnityEngine.AI;

// Parked car that drives around the road on the "Car" NavMesh agent type.
// Picks random destinations within patrolLength and turns around when a Level sign
// or another barrier car is ahead, so the cars keep looping along the roads.
public class BarrierPatrolCar : MonoBehaviour
{
    public float speed = 4f;
    public float patrolLength = 150f;          // roam radius for random destinations
    public float turnCooldown = 0.5f;
    public float heightOffset = 0.8f;          // car pivot above the road NavMesh surface
    public float lookAhead = 3f;               // how far ahead to check for a sign or car
    public int carAgentTypeID = -1512830617;   // "Car" agent type (Navigation settings)

    private int trafficStopUntilFrame = -1; // refreshed by TrafficLight while red

    // Called by TrafficLight every frame while its light is red
    public void StopForTraffic()
    {
        trafficStopUntilFrame = Time.frameCount + 1;
    }

    private NavMeshAgent agent;
    private Rigidbody rb;
    private BoxCollider box;
    private float cooldownTimer;
    private float stuckTimer;

    private void Awake()
    {
        box = GetComponent<BoxCollider>();
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        agent = GetComponent<NavMeshAgent>();
        if (agent == null) agent = gameObject.AddComponent<NavMeshAgent>();
        agent.agentTypeID = carAgentTypeID;
        agent.speed = speed;
        agent.acceleration = 8f;
        agent.angularSpeed = 180f;
        agent.stoppingDistance = 0.5f;
        agent.autoBraking = false;
        agent.baseOffset = 0f; // height is set from the road in LateUpdate
    }

    private void Start()
    {
        var filter = new NavMeshQueryFilter { agentTypeID = carAgentTypeID, areaMask = NavMesh.AllAreas };
        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 10f, filter))
        {
            Debug.LogWarning(name + " is not on the car NavMesh; it stays parked.", this);
            agent.enabled = false;
            return;
        }
        agent.Warp(hit.position);
        PickDestination(false);
    }

    // Stick to the road surface: read the car NavMesh height under the car and sit heightOffset above it
    private void LateUpdate()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;
        var filter = new NavMeshQueryFilter { agentTypeID = carAgentTypeID, areaMask = NavMesh.AllAreas };
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1.5f, filter))
        {
            Vector3 p = transform.position;
            transform.position = new Vector3(p.x, hit.position.y + heightOffset, p.z);
        }
    }

    private void FixedUpdate()
    {
        if (!agent.enabled || !agent.isOnNavMesh) return;

        // Red light: stop and wait
        if (Time.frameCount <= trafficStopUntilFrame) { agent.isStopped = true; stuckTimer = 0f; return; }
        if (agent.isStopped && cooldownTimer <= 0f) agent.isStopped = false; // resume after red

        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.fixedDeltaTime;
            if (cooldownTimer <= 0f) agent.isStopped = false; // resume after the turn
        }

        // Turn around before hitting a Level sign or another barrier car
        if (cooldownTimer <= 0f && BlockedAhead())
        {
            // Stop dead before the sign/car, then turn back
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
            PickDestination(true);
            cooldownTimer = turnCooldown;
        }

        // Reached the destination: pick the next random one
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
            PickDestination(false);

        // Stuck for 2 seconds: turn around
        stuckTimer = agent.velocity.sqrMagnitude < 0.01f ? stuckTimer + Time.fixedDeltaTime : 0f;
        if (stuckTimer > 2f)
        {
            stuckTimer = 0f;
            PickDestination(true);
        }
    }

    private bool BlockedAhead()
    {
        Vector3 dir = agent.velocity.sqrMagnitude > 0.01f ? agent.velocity.normalized : transform.forward;
        Vector3 center = transform.TransformPoint(box.center);
        Vector3 half = Vector3.Scale(box.size, transform.lossyScale) * 0.5f;
        // look far enough ahead to cover the distance travelled before the turn takes effect
        float reach = Mathf.Max(lookAhead, agent.speed * 1.0f + 2f);
        if (Physics.BoxCast(center, half, dir, out RaycastHit hit, transform.rotation, reach, ~0, QueryTriggerInteraction.Collide))
        {
            if (hit.collider.gameObject == gameObject) return false;
            if (hit.collider.GetComponent<BarrierPatrolCar>() != null) return true;
            string n = hit.collider.gameObject.name;
            return n.StartsWith("Level") && n.Contains("Sign");
        }
        return false;
    }

    // turnAround = true picks a destination behind the car
    private void PickDestination(bool turnAround)
    {
        var filter = new NavMeshQueryFilter { agentTypeID = carAgentTypeID, areaMask = NavMesh.AllAreas };
        Vector3 origin = transform.position;
        Vector3 fwd = agent.velocity.sqrMagnitude > 0.01f ? agent.velocity.normalized : transform.forward;

        for (int i = 0; i < 20; i++)
        {
            Vector2 r = Random.insideUnitCircle * patrolLength;
            Vector3 cand = origin + new Vector3(r.x, 0f, r.y);
            if (turnAround && Vector3.Dot(cand - origin, fwd) >= 0f) continue;
            if (NavMesh.SamplePosition(cand, out NavMeshHit hit, 20f, filter))
            {
                agent.SetDestination(hit.position);
                return;
            }
        }
    }
}
