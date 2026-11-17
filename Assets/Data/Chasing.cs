using UnityEngine;
using UnityEngine.AI;

// Makes the dog walk around on the road, then chase the player when they get close.
// Needs: NavMeshAgent on the dog, and the road baked as a NavMesh.
public class Chasing : MonoBehaviour
{
    private float detectRadius = 12f;   // start chasing when the player is this close
    private float loseRadius = 20f;     // stop chasing when the player is this far
    private float walkSpeed = 3f;
    private float chaseSpeed = 8f;
    private float wanderRadius = 30f;   // how far to look for a random road point
    public ObstacleData data;          // required: settings asset

    private NavMeshAgent agent;
    private Transform player;
    private bool chasing;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (data == null) Debug.LogWarning(name + " has no ObstacleData assigned!");
        if (data != null)
        {
            detectRadius = data.detectRadius;
            loseRadius = data.loseRadius;
            walkSpeed = data.walkSpeed;
            chaseSpeed = data.chaseSpeed;
            wanderRadius = data.wanderRadius;
        }
        player = FindFirstObjectByType<PlayerController>().transform;
        PickRandomRoadPoint();
    }

    void Update()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        if (!chasing && distance < detectRadius) chasing = true;
        if (chasing && distance > loseRadius) chasing = false;

        if (chasing)
        {
            agent.speed = chaseSpeed;
            agent.SetDestination(player.position);
        }
        else
        {
            agent.speed = walkSpeed;
            if (!agent.pathPending && agent.remainingDistance < 0.5f)
                PickRandomRoadPoint();
        }
    }

    void PickRandomRoadPoint()
    {
        Vector3 randomPoint = transform.position + Random.insideUnitSphere * wanderRadius;

        // Only accept points that are on the NavMesh (the road), so the dog stays on it
        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
    }
}
