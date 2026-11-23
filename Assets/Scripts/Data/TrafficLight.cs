using UnityEngine;

// Traffic light logic only: the Green -> Orange -> Red -> Orange cycle, stopping parked cars on Red,
// and the violation damage when the Player is inside the red zone while moving.
// The lamps, blinking, red zone and red spotlight are Unity animations (TrafficCycle clip on this object).
// The cycle lengths here must match that clip.
public class TrafficLight : MonoBehaviour
{
    public enum LightState { Green, Orange, Red }

    [Header("Timing (seconds)")]
    public float greenSeconds = 15f;
    public float orangeSeconds = 5f;
    public float redSeconds = 5f;
    public float orangeAfterRedSeconds = 3f;

    [Header("Red Zone (detection area)")]
    public Vector3 frontDirection = new Vector3(0f, -1f, 0f); // LOCAL direction the lamps face (only the flat part is used)
    public float aimDistance = 8f;       // how far in front of the light the zone center is
    public float zoneRadius = 6f;        // radius of the round red zone = detect radius

    [Header("Detection")]
    public float facingAngle = 45f;      // how straight the Player must face the light
    public float stopRadius = 200f;       // parked cars inside this radius stop on red
    public float movingSpeed = 0.5f;
    public int violationDamage = 10;
    public float violationCooldown = 1f;

    private LightState state = LightState.Green;
    private float timer;
    private float violationTimer;
    private bool goingToRed = true;
    private PlayerController player;
    private GameManager gameManager;
    private BarrierPatrolCar[] cars;
    private Vector3 zoneCenter;

    public LightState CurrentState => state;

    private void Start()
    {
        player = FindAnyObjectByType<PlayerController>();
        gameManager = FindAnyObjectByType<GameManager>();
        cars = FindObjectsByType<BarrierPatrolCar>();
        timer = greenSeconds;

        // Zone center, the same spot the red zone visual is placed at
        Transform redLamp = transform.Find("Lamp_Red");
        Vector3 lampPos = redLamp != null ? redLamp.GetComponent<Renderer>().bounds.center : transform.position;
        zoneCenter = new Vector3(lampPos.x, 0.5f, lampPos.z) + GetFlatFront() * aimDistance;
    }

    private void Update()
    {
        UpdateCycle();
        UpdateCars();
        CheckViolation();
    }

    // Moves to the next color when the timer runs out
    private void UpdateCycle()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;

        switch (state)
        {
            case LightState.Green:
                state = LightState.Orange;
                timer = orangeSeconds;
                goingToRed = true;
                break;
            case LightState.Orange:
                if (goingToRed) { state = LightState.Red; timer = redSeconds; }
                else { state = LightState.Green; timer = greenSeconds; }
                break;
            case LightState.Red:
                state = LightState.Orange;
                timer = orangeAfterRedSeconds;
                goingToRed = false;
                break;
        }
    }

    // Horizontal direction the light faces (flat, so the zone lands on the floor)
    private Vector3 GetFlatFront()
    {
        Vector3 front = transform.TransformDirection(frontDirection.normalized);
        front.y = 0f;
        if (front.sqrMagnitude < 0.01f) return Vector3.forward;
        return front.normalized;
    }

    // Red = nearby cars stop. Green/Orange = they keep driving.
    private void UpdateCars()
    {
        if (state != LightState.Red) return;
        foreach (var car in cars)
        {
            if (car == null) continue;
            if (Vector3.Distance(car.transform.position, transform.position) <= stopRadius)
                car.StopForTraffic();
        }
    }

    private void CheckViolation()
    {
        if (violationTimer > 0f) violationTimer -= Time.deltaTime;
        if (state != LightState.Red || player == null || gameManager == null) return;
        if (violationTimer > 0f) return;
        if (!PlayerInZone()) return;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb == null || rb.linearVelocity.magnitude < movingSpeed) return;

        gameManager.TakeDamage(violationDamage);
        violationTimer = violationCooldown;
    }

    // True when the Player stands inside the round zone and faces the light
    private bool PlayerInZone()
    {
        Vector3 flatFromCenter = player.transform.position - zoneCenter;
        flatFromCenter.y = 0f;
        if (flatFromCenter.magnitude > zoneRadius) return false;

        Vector3 flatToLight = transform.position - player.transform.position;
        flatToLight.y = 0f;
        if (Vector3.Angle(player.transform.forward, flatToLight) > facingAngle) return false;
        return true;
    }
}
