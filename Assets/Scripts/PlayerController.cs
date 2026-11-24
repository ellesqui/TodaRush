using UnityEngine;
using UnityEngine.Audio;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 15f;
    public float acceleration = 6f;
    public float deceleration = 4f;
    public float braking = 8f;
    public float reverseSpeed = 4f;
    public float turnSpeed = 100f;

    public GameManager gameManager;
    public bool canDrive = false;   // set to true by UIManager when the GO countdown ends
    public float currentSpeed = 0f;

    [Header("Handbrake")]
    public bool handbrakeEnabled = true;           // turn off to disable the handbrake
    public KeyCode handbrakeKey = KeyCode.Space;   // hold this key to force the tricycle to stop
    public LevelData levelData;                    // the level this player is in (turns the handbrake on or off)
    public float handbrakeDeceleration = 12f;      // how fast the handbrake slows the tricycle (higher = shorter brake)

    [Header("Engine sound")]
    public AudioClip idleClip;           // Idle Motor
    public AudioClip drivingClip;        // Driving (loops)
    public AudioClip accelerateClip;     // Accelerate (plays once on boost)
    public AudioClip brakeClip;          // Brake (plays once when the handbrake is pressed)

    private Rigidbody rb;
    private float moveInput;
    private float turnInput;
    private float drivingSpeed = 0f;
    private bool handbrakeOn = false;
    private AudioSource engineSource;
    private AudioSource boostSource;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        // Handbrake is unlocked per level (LevelData.handbrakeEnabled)
        if (levelData != null) handbrakeEnabled = levelData.handbrakeEnabled;
        if (AudioManager.Instance == null) return;

        AudioMixerGroup sfxGroup = AudioManager.Instance.mixer.FindMatchingGroups("SFX")[0];

        // Engine loop (idle or driving)
        engineSource = gameObject.AddComponent<AudioSource>();
        engineSource.loop = true;
        engineSource.clip = idleClip;
        engineSource.outputAudioMixerGroup = sfxGroup;
        engineSource.Play();

        // Boost sound (one shot)
        boostSource = gameObject.AddComponent<AudioSource>();
        boostSource.outputAudioMixerGroup = sfxGroup;
    }

    void Update()
    {
        UpdateBoostSound();

        if (gameManager.gameWon || gameManager.IsOver())
        {
            moveInput = 0f;
            turnInput = 0f;
            return;
        }

        moveInput = Input.GetAxisRaw("Vertical");
        turnInput = Input.GetAxis("Horizontal");
        if (!canDrive) { moveInput = 0f; turnInput = 0f; }   // locked until the GO countdown ends

        // Handbrake: works only while the player is allowed to drive
        handbrakeOn = handbrakeEnabled && canDrive && Input.GetKey(handbrakeKey);
        if (Input.GetKeyDown(handbrakeKey) && handbrakeOn && boostSource != null && brakeClip != null)
            boostSource.PlayOneShot(brakeClip);
    }

    void FixedUpdate()
    {
        if (gameManager.gameWon || gameManager.IsOver())
        {
            drivingSpeed = 0f;

            Vector3 stoppedVelocity = rb.linearVelocity;
            stoppedVelocity.x = 0f;
            stoppedVelocity.z = 0f;
            rb.linearVelocity = stoppedVelocity;

            currentSpeed = 0f;
            if (engineSource != null) engineSource.Stop();
            return;
        }

        // HANDBRAKE: slows down smoothly while the key is held (no instant stop)
        if (handbrakeOn)
        {
            drivingSpeed = Mathf.MoveTowards(drivingSpeed, 0f, handbrakeDeceleration * Time.fixedDeltaTime);
            Vector3 brakeDir = transform.forward;
            brakeDir.y = 0f;
            brakeDir.Normalize();
            rb.linearVelocity = new Vector3(brakeDir.x * drivingSpeed, rb.linearVelocity.y, brakeDir.z * drivingSpeed);
            rb.angularVelocity = Vector3.zero;
            currentSpeed = Mathf.Abs(drivingSpeed) * 3.6f;
            UpdateEngineSound();
            return;
        }

        float targetSpeed = 0f;
        float speedChange = deceleration;

        bool changingDirection = (drivingSpeed > 0f && moveInput < 0f) || (drivingSpeed < 0f && moveInput > 0f);

        if (changingDirection)
        {
            speedChange = braking;
        }
        else if (moveInput > 0f)
        {
            targetSpeed = moveInput * moveSpeed;
            speedChange = acceleration;
        }
        else if (moveInput < 0f)
        {
            targetSpeed = moveInput * reverseSpeed;
            speedChange = acceleration;
        }

        drivingSpeed = Mathf.MoveTowards(drivingSpeed, targetSpeed, speedChange * Time.fixedDeltaTime);

        Vector3 direction = transform.forward;
        direction.y = 0f;
        direction.Normalize();

        Vector3 velocity = direction * drivingSpeed;
        velocity.y = rb.linearVelocity.y;
        rb.linearVelocity = velocity;

        // Stop any spin the physics system added from a collision.
        rb.angularVelocity = Vector3.zero;

        // Turn the tricycle directly, so bumps can't rotate it.
        transform.Rotate(0f, turnInput * turnSpeed * Time.fixedDeltaTime, 0f);

        Vector3 flatVelocity = new Vector3(
            rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        currentSpeed = flatVelocity.magnitude * 3.6f;

        UpdateEngineSound();
    }

    // Idle when stopped, driving when moving. Higher speed = higher pitch.
    void UpdateEngineSound()
    {
        if (engineSource == null) return;

        AudioClip wanted = currentSpeed > 1f ? drivingClip : idleClip;
        if (wanted != null && engineSource.clip != wanted)
        {
            engineSource.clip = wanted;
            engineSource.Play();
        }

        float percent = Mathf.Clamp01(currentSpeed / (moveSpeed * 3.6f));
        engineSource.pitch = Mathf.Lerp(0.8f, 1.4f, percent);
    }

    // Gas boost: set by Pickup while the boost lasts
    public bool boostActive = false;
    [Range(0f, 1f)] public float boostVolume = 0.4f;   // boost sound volume (lower = quieter)

    // Boost sound loops only while boosting AND the tricycle is moving.
    // It stops when the player stops or when the boost ends.
    void UpdateBoostSound()
    {
        if (boostSource == null || accelerateClip == null) return;

        bool shouldPlay = boostActive && currentSpeed > 1f;
        if (shouldPlay && !boostSource.isPlaying)
        {
            boostSource.clip = accelerateClip;
            boostSource.loop = true;
            boostSource.volume = boostVolume;
            boostSource.Play();
        }
        else if (!shouldPlay && boostSource.isPlaying)
        {
            boostSource.Stop();
        }
    }
}
