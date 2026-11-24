using UnityEngine;
using Unity.Cinemachine;

// Automatic camera height for the tricycle:
// - Going DOWN a slope: the camera rises (higher view).
// - Going UP a slope: the camera lowers (lower view, behind the player).
public class CameraRaiseOnBlock : MonoBehaviour
{
    public float normalHeight = 2.6f;   // camera height on flat ground
    public float maxHeight = 9f;        // highest the camera can go
    public float minHeight = 1.5f;      // lowest the camera can go
    public float slopeGain = 12f;       // how much the camera moves for a slope (higher = stronger)
    public float lookAhead = 6f;        // how far ahead we check the ground
    public float speed = 6f;            // how fast it rises and falls

    private Transform player;
    private CinemachineFollow follow;
    private float currentHeight;

    void Start()
    {
        follow = GetComponent<CinemachineFollow>();
        GameObject p = GameObject.Find("Player");
        if (p != null) player = p.transform;
        currentHeight = normalHeight;
    }

    // Ground height at a spot, using the NavMesh (the road surface), so no colliders are needed
    float GroundY(Vector3 pos)
    {
        UnityEngine.AI.NavMeshHit navHit;
        if (UnityEngine.AI.NavMesh.SamplePosition(pos, out navHit, 10f, UnityEngine.AI.NavMesh.AllAreas))
            return navHit.position.y;
        return pos.y;
    }

    void LateUpdate()
    {
        if (follow == null || player == null) return;

        // Slope: ground ahead minus ground here. Negative = going down, positive = going up
        float here = GroundY(player.position);
        float ahead = GroundY(player.position + player.forward * lookAhead);
        float slope = (ahead - here) / lookAhead;
        float target = Mathf.Clamp(normalHeight - slope * slopeGain, minHeight, maxHeight);

        currentHeight = Mathf.MoveTowards(currentHeight, target, speed * Time.deltaTime);
        Vector3 offset = follow.FollowOffset;
        offset.y = currentHeight;
        follow.FollowOffset = offset;
    }
}
