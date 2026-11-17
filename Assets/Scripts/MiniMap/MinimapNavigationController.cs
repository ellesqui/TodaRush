using UnityEngine;
using UnityEngine.AI;

// Draws one route at a time on the minimap:
// - ORANGE: player -> active passenger's destination (during deliveries).
// - BLUE: player -> TODA Terminal (after a delivery, while returning).
// Also drives the minimap camera: top-down, centered on the Player, rotated to the Player's heading.
// Attach to the Player object.
public class MinimapNavigationController : MonoBehaviour
{
    [Header("Route Display")]
    public LineRenderer routeLine;               // orange, on PlayerArrow
    public LineRenderer terminalRouteLine;       // blue, on TerminalRouteLine
    public LineRenderer[] staticRouteLines;      // turned off during Play Mode
    public float routeHeight = 100.5f;
    public float routeWidth = 2.5f;

    [Header("References")]
    public GameManager gameManager;              // found automatically if empty
    public Transform terminalTarget;

    [Header("NavMesh Sampling")]
    public float updateInterval = 0.25f;
    public float navMeshSampleHeight = 100.5f;
    public float playerSampleDistance = 3f;
    public float destinationSampleDistance = 1f;

    [Header("Minimap Camera")]
    public Camera minimapCamera;                 // found by name if empty
    public bool followMinimapCamera = true;
    public float minimapCameraHeight = 500f;
    public float minimapOrthoSize = 70f;

    [Header("Minimap Arrow")]
    public float minimapMapPlaneY = 100.5f;
    public float arrowHeightAbovePlane = 1f;

    private NavMeshPath path;
    private float timer;
    private bool[] staticLineWasEnabled;
    private Transform arrowTransform;

    void Awake()
    {
        path = new NavMeshPath();
    }

    void Start()
    {
        if (gameManager == null) gameManager = FindAnyObjectByType<GameManager>();

        if (minimapCamera == null)
        {
            GameObject cameraObject = GameObject.Find("MinimapRedesignUICamera");
            if (cameraObject != null) minimapCamera = cameraObject.GetComponent<Camera>();
        }

        Transform arrow = transform.Find("PlayerArrow");
        if (arrow != null) arrowTransform = arrow;

        if (minimapCamera != null && followMinimapCamera)
        {
            minimapCamera.orthographic = true;
            minimapCamera.orthographicSize = minimapOrthoSize;
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
        HideTerminalRoute();
    }

    void OnDisable()
    {
        // Restore the static routes to their original state.
        if (staticLineWasEnabled == null) return;

        for (int i = 0; i < staticLineWasEnabled.Length; i++)
        {
            if (staticRouteLines[i] != null) staticRouteLines[i].enabled = staticLineWasEnabled[i];
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer < updateInterval) return;

        timer = 0f;
        RefreshRoute();
    }

    // Runs after all Player movement for the frame, so the minimap follows without lag.
    void LateUpdate()
    {
        // Keep the arrow at a fixed height above the map plane. X and Z still follow the Player.
        if (arrowTransform != null)
        {
            arrowTransform.position = new Vector3(transform.position.x, minimapMapPlaneY + arrowHeightAbovePlane, transform.position.z);
        }

        if (!followMinimapCamera || minimapCamera == null) return;

        Vector3 playerForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (playerForward.sqrMagnitude < 0.0001f) return;
        playerForward.Normalize();

        // Top-down orthographic: centered on the Player's XZ, at a fixed height, heading-up.
        Vector3 topDownPosition = new Vector3(transform.position.x, minimapCameraHeight, transform.position.z);
        minimapCamera.transform.SetPositionAndRotation(topDownPosition, Quaternion.LookRotation(Vector3.down, playerForward));
    }

    void RefreshRoute()
    {
        // Mission finished: hide both routes.
        if (gameManager != null && gameManager.gameWon)
        {
            HideRoute();
            HideTerminalRoute();
            return;
        }

        // A delivered passenger still needs the return trip to the Terminal.
        if (gameManager != null && gameManager.destinationCount > gameManager.completedLaps)
        {
            HideRoute();
            RefreshTerminalRoute();
            return;
        }

        // Delivery phase: orange route to the active passenger's destination.
        HideTerminalRoute();

        PassengerPickup activePassenger = FindActivePassenger();
        if (activePassenger == null || routeLine == null)
        {
            HideRoute();
            return;
        }

        Vector3 playerRoadPoint;
        Vector3 destinationRoadPoint;
        Vector3 destinationPosition = activePassenger.targetDestination.transform.position;

        if (!TrySampleNavMesh(transform.position, playerSampleDistance, out playerRoadPoint) ||
            !TrySampleNavMesh(destinationPosition, destinationSampleDistance, out destinationRoadPoint))
        {
            HideRoute();
            return;
        }

        if (!CalculatePath(playerRoadPoint, destinationRoadPoint))
        {
            HideRoute();
            return;
        }

        DrawRoute(routeLine, path.corners);
    }

    // Blue route: same path logic, but the target is the Terminal.
    void RefreshTerminalRoute()
    {
        if (terminalRouteLine == null || terminalTarget == null)
        {
            HideTerminalRoute();
            return;
        }

        Vector3 playerRoadPoint;
        Vector3 terminalRoadPoint;

        if (!TrySampleNavMesh(transform.position, playerSampleDistance, out playerRoadPoint) ||
            !TrySampleNavMesh(terminalTarget.position, destinationSampleDistance, out terminalRoadPoint) ||
            !CalculatePath(playerRoadPoint, terminalRoadPoint))
        {
            HideTerminalRoute();
            return;
        }

        DrawRoute(terminalRouteLine, path.corners);
    }

    bool CalculatePath(Vector3 from, Vector3 to)
    {
        bool calculated = NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path);
        return calculated && path.status == NavMeshPathStatus.PathComplete && path.corners.Length >= 2;
    }

    // Returns the first boarded passenger that still has a destination.
    PassengerPickup FindActivePassenger()
    {
        if (gameManager == null || gameManager.passengers == null) return null;

        foreach (PassengerPickup passenger in gameManager.passengers)
        {
            if (passenger != null && passenger.hasBoarded && passenger.targetDestination != null) return passenger;
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

    void DrawRoute(LineRenderer line, Vector3[] corners)
    {
        line.enabled = true;
        line.useWorldSpace = true;
        line.startWidth = routeWidth;
        line.endWidth = routeWidth;
        line.positionCount = corners.Length;

        for (int i = 0; i < corners.Length; i++)
        {
            line.SetPosition(i, new Vector3(corners[i].x, routeHeight, corners[i].z));
        }
    }

    void HideRoute()
    {
        if (routeLine != null) routeLine.enabled = false;
    }

    void HideTerminalRoute()
    {
        if (terminalRouteLine != null) terminalRouteLine.enabled = false;
    }
}
