using UnityEngine;
using UnityEngine.AI;

// Draws one route at a time on the minimap:
// - ORANGE: player -> active passenger's destination (during deliveries).
// - BLUE: player -> TODA Terminal (after all passengers are delivered).
// Attach to the Player object. Route uses the baked minimap NavMesh (NavMesh-Roads).
// Also drives the minimap camera: it follows the Player's XZ position and rotates so the Player's heading points to the top of the minimap.
public class MinimapNavigationController : MonoBehaviour
{
    [Header("Route Display")]
    [Tooltip("The orange LineRenderer used for the dynamic route (on PlayerArrow).")]
    public LineRenderer routeLine;

    [Tooltip("The blue LineRenderer used for the terminal return route (TerminalRouteLine).")]
    public LineRenderer terminalRouteLine;

    [Tooltip("The three existing static route lines. They are turned off during Play Mode.")]
    public LineRenderer[] staticRouteLines;

    [Tooltip("Height where the route is drawn, above the minimap road artwork.")]
    public float routeHeight = 100.5f;

    [Tooltip("Width of the route line.")]
    public float routeWidth = 2.5f;

    [Header("Passenger Source")]
    [Tooltip("The GameManager that holds the passengers array. Found automatically if left empty.")]
    public GameManager gameManager;

    [Header("Terminal Return Target")]
    [Tooltip("The existing /Terminal object. Used as the return target after all passengers are delivered.")]
    public Transform terminalTarget;

    [Header("NavMesh Sampling")]
    [Tooltip("How often the route is recalculated, in seconds.")]
    public float updateInterval = 0.25f;

    [Tooltip("Height used to search for the minimap NavMesh.")]
    public float navMeshSampleHeight = 100.5f;

    [Tooltip("Maximum distance from the player to the road NavMesh.")]
    public float playerSampleDistance = 3f;

    [Tooltip("Maximum distance from the destination to the road NavMesh.")]
    public float destinationSampleDistance = 1f;

    [Header("Heading-Up Minimap Camera")]
    [Tooltip("The production minimap camera (MinimapRedesignUICamera). Left empty, it is found by name.")]
    public Camera minimapCamera;

    [Tooltip("When on, the minimap camera follows the Player's XZ position and rotates with the Player's heading.")]
    public bool followMinimapCamera = true;

    [Tooltip("Fixed world height of the minimap camera. Keep it above the flattened road plane (Y 100.2). Used only in top-down orthographic mode.")]
    public float minimapCameraHeight = 500f;

    [Tooltip("Orthographic size of the minimap camera. Smaller values zoom in. Used only in top-down orthographic mode.")]
    public float minimapOrthoSize = 55f;

    [Header("Tilted Perspective Minimap")]
    [Tooltip("When on, the minimap uses a tilted Perspective camera instead of straight top-down orthographic.")]
    public bool usePerspectiveMinimap = true;

    [Tooltip("Field of view of the tilted minimap camera, in degrees.")]
    public float minimapFieldOfView = 50f;

    [Tooltip("World Y of the minimap map plane. The camera height is measured from this plane, not from the Player's Y.")]
    public float minimapMapPlaneY = 100.5f;

    [Tooltip("Camera height above the minimap map plane.")]
    public float minimapTiltHeight = 80f;

    [Tooltip("How far the camera sits behind the Player (along the Player's horizontal backward direction).")]
    public float minimapBackOffset = 55f;

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

        if (minimapCamera == null)
        {
            GameObject cameraObject = GameObject.Find("MinimapRedesignUICamera");
            if (cameraObject != null)
            {
                minimapCamera = cameraObject.GetComponent<Camera>();
            }
        }

        if (minimapCamera != null && followMinimapCamera)
        {
            if (usePerspectiveMinimap)
            {
                minimapCamera.orthographic = false;
                minimapCamera.fieldOfView = minimapFieldOfView;
            }
            else
            {
                minimapCamera.orthographic = true;
                minimapCamera.orthographicSize = minimapOrthoSize;
            }
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
        HideTerminalRoute();
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

    // Runs after all Player movement for the frame, so the minimap camera follows the vehicle without lag.
    void LateUpdate()
    {
        if (!followMinimapCamera || minimapCamera == null)
        {
            return;
        }

        // Heading on the horizontal plane. The camera's screen-up will point along it.
        Vector3 playerForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (playerForward.sqrMagnitude < 0.0001f)
        {
            return;
        }
        playerForward.Normalize();

        if (usePerspectiveMinimap)
        {
            // Tilted view: target is the Player's XZ on the minimap map plane.
            // The camera sits above and behind the Player, then looks at the target with the Player's heading as screen-up.
            Vector3 target = new Vector3(transform.position.x, minimapMapPlaneY, transform.position.z);
            Vector3 cameraPosition = target + Vector3.up * minimapTiltHeight - playerForward * minimapBackOffset;
            Vector3 lookDirection = target - cameraPosition;
            minimapCamera.transform.SetPositionAndRotation(cameraPosition, Quaternion.LookRotation(lookDirection, playerForward));
            return;
        }

        // Top-down orthographic: centered on the Player's XZ, at a fixed height.
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

        DrawRoute(routeLine, path.corners);
    }

    // Blue route: same NavMesh sampling and path calculation, but the target is the Terminal.
    void RefreshTerminalRoute()
    {
        if (terminalRouteLine == null || terminalTarget == null)
        {
            HideTerminalRoute();
            return;
        }

        Vector3 playerRoadPoint;
        if (!TrySampleNavMesh(transform.position, playerSampleDistance, out playerRoadPoint))
        {
            HideTerminalRoute();
            return;
        }

        Vector3 terminalRoadPoint;
        if (!TrySampleNavMesh(terminalTarget.position, destinationSampleDistance, out terminalRoadPoint))
        {
            HideTerminalRoute();
            return;
        }

        bool calculated = NavMesh.CalculatePath(playerRoadPoint, terminalRoadPoint, NavMesh.AllAreas, path);
        if (!calculated || path.status != NavMeshPathStatus.PathComplete || path.corners.Length < 2)
        {
            HideTerminalRoute();
            return;
        }

        DrawRoute(terminalRouteLine, path.corners);
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
        if (routeLine != null)
        {
            routeLine.enabled = false;
        }
    }

    void HideTerminalRoute()
    {
        if (terminalRouteLine != null)
        {
            terminalRouteLine.enabled = false;
        }
    }
}
