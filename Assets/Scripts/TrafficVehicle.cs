using System.Collections.Generic;
using UnityEngine;

// Drives one vehicle around the road grid forever.
// At every intersection it picks a random connected road.
// It avoids U-turns unless it reaches a dead end.
// The same script works for cars, motorcycles, jeeps, etc.
// Change the values in the Inspector to make each vehicle type different.
public class TrafficVehicle : MonoBehaviour
{
    [Header("Vehicle settings")]
    public float moveSpeed = 8f;      // units per second
    public float turnSpeed = 90f;     // degrees per second
    public float laneOffset = 1.5f;   // distance from road center (right lane)

    [Header("Start point (must be a road intersection)")]
    public Vector2 startNode = new Vector2(-75f, -75f);

    private const float GroundY = 0.2f;   // same height as the Player
    private const float GridStep = 75f;   // distance between intersections

    // Each intersection maps to the intersections it connects to.
    private Dictionary<Vector2, List<Vector2>> neighbors = new Dictionary<Vector2, List<Vector2>>();

    private Vector2 currentNode;
    private Vector2 previousNode;
    private Vector3 targetPoint;

    // Road pieces that exist in the scene, stored by their start point.
    // Horizontal roads connect (x, z) to (x + 75, z).
    private static readonly Vector2[] HorizontalStarts =
    {
        new Vector2(-150, -150), new Vector2(-150, -75), new Vector2(-150, 0), new Vector2(-150, 75), new Vector2(-150, 150),
        new Vector2(-75, -150),  new Vector2(-75, -75),  new Vector2(-75, 75),  new Vector2(-75, 150),
        new Vector2(0, -150),    new Vector2(0, -75),    new Vector2(0, 75),    new Vector2(0, 150),
        new Vector2(75, -150),   new Vector2(75, 0),     new Vector2(75, 75)
    };

    // Vertical roads connect (x, z) to (x, z + 75).
    private static readonly Vector2[] VerticalStarts =
    {
        new Vector2(-150, -75), new Vector2(-150, 0),
        new Vector2(-75, -150), new Vector2(-75, -75), new Vector2(-75, 0), new Vector2(-75, 75),
        new Vector2(0, -150),   new Vector2(75, -75),  new Vector2(75, 0),
        new Vector2(150, -150), new Vector2(150, -75), new Vector2(150, 0), new Vector2(150, 75)
    };

    void Start()
    {
        BuildNeighbors();

        currentNode = startNode;
        previousNode = startNode;
        PickNextRoad();

        // Start at the lane position so the vehicle doesn't jump on the first frame.
        transform.position = targetPoint;
    }

    void Update()
    {
        Vector3 toTarget = targetPoint - transform.position;
        toTarget.y = 0f;

        // Reached the target point, so choose the next road.
        if (toTarget.magnitude < 0.1f)
        {
            PickNextRoad();
            return;
        }

        // Turn toward the target slowly.
        Quaternion lookRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, turnSpeed * Time.deltaTime);

        // Move toward the target.
        transform.position = Vector3.MoveTowards(transform.position, targetPoint, moveSpeed * Time.deltaTime);
    }

    // Fills the neighbors list from the road piece lists above.
    private void BuildNeighbors()
    {
        neighbors.Clear();

        foreach (Vector2 start in HorizontalStarts)
        {
            Vector2 end = new Vector2(start.x + GridStep, start.y);
            AddConnection(start, end);
        }

        foreach (Vector2 start in VerticalStarts)
        {
            Vector2 end = new Vector2(start.x, start.y + GridStep);
            AddConnection(start, end);
        }
    }

    private void AddConnection(Vector2 a, Vector2 b)
    {
        if (!neighbors.ContainsKey(a)) neighbors[a] = new List<Vector2>();
        if (!neighbors.ContainsKey(b)) neighbors[b] = new List<Vector2>();

        neighbors[a].Add(b);
        neighbors[b].Add(a);
    }

    // Picks a random connected intersection and sets the lane point to drive to.
    private void PickNextRoad()
    {
        List<Vector2> options = neighbors[currentNode];

        // Prefer roads other than the one we came from (no U-turns).
        List<Vector2> choices = new List<Vector2>();
        foreach (Vector2 option in options)
        {
            if (option != previousNode) choices.Add(option);
        }

        // Dead end: the only road is the one we came from, so turn back.
        if (choices.Count == 0) choices = options;

        Vector2 nextNode = choices[Random.Range(0, choices.Count)];

        // Direction of travel on the grid.
        Vector2 direction = (nextNode - currentNode).normalized;

        // Right side of the road for this direction.
        Vector2 right = new Vector2(direction.y, -direction.x);

        targetPoint = new Vector3(
            nextNode.x + right.x * laneOffset,
            GroundY,
            nextNode.y + right.y * laneOffset);

        previousNode = currentNode;
        currentNode = nextNode;
    }
}
