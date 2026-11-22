using UnityEngine;

// Traffic light cycle: Green -> Orange -> Red -> Orange -> Green (repeats).
// On Red, a round red zone is shown on the floor in front of the light.
// The Player is detected only when standing inside that circle and facing the light.
// Red + Player moving inside the zone = damage. Red = nearby parked cars stop.
public class TrafficLight : MonoBehaviour
{
    public enum LightState { Green, Orange, Red }

    [Header("Timing (seconds)")]
    public float greenSeconds = 30f;
    public float orangeSeconds = 5f;
    public float redSeconds = 5f;
    public float orangeAfterRedSeconds = 3f;
    public float blinkSeconds = 2f;       // last seconds of a color: lamp blinks
    public float blinkInterval = 0.25f;

    [Header("Red Zone")]
    public Vector3 frontDirection = new Vector3(0f, -1f, 0f); // lamps face local -Y // LOCAL direction the lamps face (only the flat part is used)
    public float aimDistance = 8f;       // how far in front of the light the zone center is
    public float zoneRadius = 6f;        // radius of the round red zone = detect radius
    public float spotAngle = 60f;        // spotlight width (visual only)
    public float detectRange = 25f;      // spotlight range (visual only)

    [Header("Detection")]
    public float facingAngle = 45f;      // how straight the Player must face the light
    public float stopRadius = 200f;       // parked cars inside this radius stop on red
    public float movingSpeed = 0.5f;
    public int violationDamage = 10;
    public float violationCooldown = 1f;

    private LightState state = LightState.Green;
    private float timer;
    private float violationTimer;
    private float blinkTimer;
    private bool blinkOn = true;
    private bool goingToRed = true;
    private PlayerController player;
    private GameManager gameManager;
    private BarrierPatrolCar[] cars;

    // Lamps indexed by LightState: 0 = Green, 1 = Orange, 2 = Red
    private Renderer[] lamps = new Renderer[3];
    private Material[][] onMats = new Material[3][];
    private Material[][] offMats = new Material[3][];
    private bool[] litCache = new bool[3];

    private Light redSpot;
    private GameObject redZone;
    private Vector3 zoneCenter;

    public LightState CurrentState => state;

    private void Start()
    {
        player = FindFirstObjectByType<PlayerController>();
        gameManager = FindFirstObjectByType<GameManager>();
        cars = FindObjectsByType<BarrierPatrolCar>(FindObjectsSortMode.None);
        timer = greenSeconds;

        string[] lampNames = { "Lamp_Green", "Lamp_Yellow", "Lamp_Red" };
        for (int i = 0; i < 3; i++)
        {
            Transform t = transform.Find(lampNames[i]);
            if (t == null) continue;
            lamps[i] = t.GetComponent<Renderer>();
            if (lamps[i] == null || lamps[i].sharedMaterials.Length < 2) continue;
            // Lamp materials are [Off, On]
            offMats[i] = new Material[] { lamps[i].sharedMaterials[0], lamps[i].sharedMaterials[0] };
            onMats[i] = new Material[] { lamps[i].sharedMaterials[1], lamps[i].sharedMaterials[1] };
        }
        SetupRedZone();
    }

    private void Update()
    {
        UpdateCycle();
        UpdateLamps();
        UpdateRedZone();
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

    // Shows the current color. In the last few seconds it blinks before changing.
    private void UpdateLamps()
    {
        if (timer <= blinkSeconds)
        {
            blinkTimer -= Time.deltaTime;
            if (blinkTimer <= 0f) { blinkTimer = blinkInterval; blinkOn = !blinkOn; }
        }
        else
        {
            blinkOn = true;
            blinkTimer = blinkInterval;
        }

        for (int i = 0; i < 3; i++)
        {
            if (lamps[i] == null || onMats[i] == null) continue;
            bool lit = i == (int)state && blinkOn;
            if (lit == litCache[i]) continue; // only change when needed
            litCache[i] = lit;
            lamps[i].sharedMaterials = lit ? onMats[i] : offMats[i];
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

    // Round red zone on the floor, straight in front of the light, plus a red spotlight aimed at it
    private void SetupRedZone()
    {
        Vector3 lampPos = lamps[2] != null ? lamps[2].bounds.center : transform.position;
        zoneCenter = new Vector3(lampPos.x, 0.5f, lampPos.z) + GetFlatFront() * aimDistance;

        // Spotlight (visual)
        GameObject spotObj = new GameObject("RedSpot");
        spotObj.transform.SetParent(transform, true);
        spotObj.transform.position = lampPos;
        spotObj.transform.rotation = Quaternion.LookRotation(zoneCenter - lampPos);
        redSpot = spotObj.AddComponent<Light>();
        redSpot.type = LightType.Spot;
        redSpot.color = Color.red;
        redSpot.intensity = 40f;
        redSpot.range = detectRange;
        redSpot.spotAngle = spotAngle;
        redSpot.shadows = LightShadows.None;
        redSpot.enabled = false;

        // Round red zone on the floor (flat cylinder, not parented so it stays horizontal)
        redZone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        redZone.name = "RedFloorZone";
        Destroy(redZone.GetComponent<Collider>());
        redZone.transform.position = zoneCenter;
        redZone.transform.rotation = Quaternion.identity;
        redZone.transform.localScale = new Vector3(zoneRadius * 2f, 0.02f, zoneRadius * 2f); // cylinder diameter = 1 at scale 1
        Material zoneMat = new Material(Shader.Find("Sprites/Default"));
        zoneMat.color = new Color(1f, 0f, 0f, 0.3f); // 30% opacity
        redZone.GetComponent<MeshRenderer>().sharedMaterial = zoneMat;
        redZone.SetActive(false);
    }

    private void UpdateRedZone()
    {
        bool isRed = state == LightState.Red;
        if (redSpot != null) redSpot.enabled = isRed;
        if (redZone != null) redZone.SetActive(isRed);
    }

    // Red = nearby cars stop. Green/Orange = they keep driving.
    private void UpdateCars()
    {
        if (state != LightState.Red) return; // on green/orange cars keep driving
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
        if (state != LightState.Red || player == null || gameManager == null || redZone == null) return;
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
