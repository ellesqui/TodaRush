using UnityEngine;
using UnityEngine.AI;

// Draws one orange route on the minimap from the player to the active passenger's destination.
// Attach to the Player object. Route uses the baked minimap NavMesh (NavMesh-Roads).
public class MinimapNavigationController : MonoBehaviour
{
    [Header("Route Display")]
    [Tooltip("The orange LineRenderer used for the dynamic route (on PlayerArrow).")]
    public LineRenderer routeLine;

    [Tooltip("The three existing static route lines. They are turned off during Play Mode.")]
    public LineRenderer[] staticRouteLines;

    [Tooltip("Height where the route is drawn, above the minimap road artwork.")]
    public float routeHeight = 100.5f;

    [Tooltip("Width of the route line.")]
    public float routeWidth = 2.5f;

    [Header("Passenger Source")]
    [Tooltip("The GameManager that holds the passengers array. Found automatically if left empty.")]
    public GameManager gameManager;

    [Header("NavMesh Sampling")]
    [Tooltip("How often the route is recalculated, in seconds.")]
    public float updateInterval = 0.25f;

    [Tooltip("Height used to search for the minimap NavMesh.")]
    public float navMeshSampleHeight = 100.5f;

    [Tooltip("Maximum distance from the player to the road NavMesh.")]
    public float playerSampleDistance = 3f;

    [Tooltip("Maximum distance from the destination to the road NavMesh.")]
    public float destinationSampleDistance = 1f;

    private NavMeshPath path;
    private float timer;
    private bool[] staticLineWasEnabled;

    void Awake()
    {
        path = new NavMeshPath();
    }

    void Start()
    {
        if (gameManager == null)
        {
            gameManager = FindAnyObjectByType<GameManager>();
        }

        // Guard against an unassigned array in the Inspector.
        if (staticRouteLines == null)
        {
            staticRouteLines = new LineRenderer[0];
        }

        // Remember each static route's state, then turn them off while this controller runs.
        staticLineWasEnabled = new bool[staticRouteLines.Length];
        for (int i = 0; i < staticRouteLines.Length; i++)
        {
            if (staticRouteLines[i] != null)
            {
                staticLineWasEnabled[i] = staticRouteLines[i].enabled;
                staticRouteLines[i].enabled = false;
            }
        }

        HideRoute();
    }

    void OnDisable()
    {
        // Restore the static routes to their original state.
        if (staticLineWasEnabled == null || staticRouteLines == null)
        {
            return;
        }

        for (int i = 0; i < staticLineWasEnabled.Length && i < staticRouteLines.Length; i++)
        {
            if (staticRouteLines[i] != null)
            {
                staticRouteLines[i].enabled = staticLineWasEnabled[i];
            }
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < updateInterval)
        {
            return;
        }

        timer = 0f;
        RefreshRoute();
    }

    void RefreshRoute()
    {
        PassengerPickup activePassenger = FindActivePassenger();
        if (activePassenger == null || routeLine == null)
        {
            HideRoute();
            return;
        }

        Vector3 playerRoadPoint;
        if (!TrySampleNavMesh(transform.position, playerSampleDistance, out playerRoadPoint))
        {
            // Player is off the road NavMesh, so do not draw a false shortcut.
            HideRoute();
            return;
        }

        Vector3 destinationRoadPoint;
        Vector3 destinationPosition = activePassenger.targetDestination.transform.position;
        if (!TrySampleNavMesh(destinationPosition, destinationSampleDistance, out destinationRoadPoint))
        {
            HideRoute();
            return;
        }

        bool calculated = NavMesh.CalculatePath(playerRoadPoint, destinationRoadPoint, NavMesh.AllAreas, path);
        if (!calculated || path.status != NavMeshPathStatus.PathComplete || path.corners.Length < 2)
        {
            HideRoute();
            return;
        }

        DrawRoute(path.corners);
    }

    // Returns the first boarded passenger that still has a destination.
    PassengerPickup FindActivePassenger()
    {
        if (gameManager == null || gameManager.passengers == null)
        {
            return null;
        }

        foreach (PassengerPickup passenger in gameManager.passengers)
        {
            if (passenger != null && passenger.hasBoarded && passenger.targetDestination != null)
            {
                return passenger;
            }
        }

        return null;
    }

    // Finds the nearest point on the road NavMesh, close enough to the requested position.
    bool TrySampleNavMesh(Vector3 worldPosition, float maxDistance, out Vector3 roadPoint)
    {
        Vector3 searchPoint = new Vector3(worldPosition.x, navMeshSampleHeight, worldPosition.z);
        NavMeshHit hit;

        if (NavMesh.SamplePosition(searchPoint, out hit, maxDistance, NavMesh.AllAreas))
        {
            roadPoint = hit.position;
            return true;
        }

        roadPoint = Vector3.zero;
        return false;
    }

    void DrawRoute(Vector3[] corners)
    {
        routeLine.enabled = true;
        routeLine.useWorldSpace = true;
        routeLine.startWidth = routeWidth;
        routeLine.endWidth = routeWidth;
        routeLine.positionCount = corners.Length;

        for (int i = 0; i < corners.Length; i++)
        {
            routeLine.SetPosition(i, new Vector3(corners[i].x, routeHeight, corners[i].z));
        }
    }

    void HideRoute()
    {
        if (routeLine != null)
        {
            routeLine.enabled = false;
        }
    }
}
