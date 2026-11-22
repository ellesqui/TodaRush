using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// A branch that hides, then every few seconds appears up high and falls to the road.
// Needs: an Obstacle component (for damage) and a trigger collider on the same object.
public class FallingBranch : MonoBehaviour
{
    public FallingBranchData data;       // required: settings asset

    private Renderer[] visuals;
    private Collider branchCollider;
    private Vector3 startPos;

    void Awake()
    {
        visuals = GetComponentsInChildren<Renderer>();
        branchCollider = GetComponent<Collider>();
        startPos = transform.position;
    }

    void Start()
    {
        if (data == null)
        {
            Debug.LogWarning(name + " has no FallingBranchData assigned!");
            return;
        }
        StartCoroutine(FallLoop());
    }

    IEnumerator FallLoop()
    {
        SetVisible(false);

        while (true)
        {
            yield return new WaitForSeconds(data.respawnInterval);

            // Appear at the start position
            transform.position = startPos;
            SetVisible(true);

            // Fall until we reach the road
            float groundY = FindRoadHeight(startPos);
            while (transform.position.y > groundY)
            {
                transform.position += Vector3.down * data.fallSpeed * Time.deltaTime;
                yield return null;
            }

            // Hit the road, disappear, and repeat
            SetVisible(false);
        }
    }

    // Finds the road height below the branch (the GUIDE road NavMesh)
    float FindRoadHeight(Vector3 position)
    {
        NavMeshQueryFilter roadFilter = new NavMeshQueryFilter { agentTypeID = -1512830617, areaMask = NavMesh.AllAreas };

        if (NavMesh.SamplePosition(position, out NavMeshHit hit, 50f, roadFilter))
        {
            return hit.position.y;
        }
        return position.y - 100f;
    }

    void SetVisible(bool on)
    {
        foreach (Renderer r in visuals) r.enabled = on;
        if (branchCollider != null) branchCollider.enabled = on;
    }
}
