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

    [Header("Engine sound")]
    public AudioClip idleClip;           // Idle Motor
    public AudioClip drivingClip;        // Driving (loops)
    public AudioClip accelerateClip;     // Accelerate (plays once on boost)

    private Rigidbody rb;
    private float moveInput;
    private float turnInput;
    private float drivingSpeed = 0f;
    private AudioSource engineSource;
    private AudioSource boostSource;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
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
        if (gameManager.gameWon || gameManager.IsOver())
        {
            moveInput = 0f;
            turnInput = 0f;
            return;
        }

        moveInput = Input.GetAxisRaw("Vertical");
        turnInput = Input.GetAxis("Horizontal");
        if (!canDrive) { moveInput = 0f; turnInput = 0f; }   // locked until the GO countdown ends
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

    // Called by Pickup when the player gets a Gas (boost) item
    public void PlayBoostSound()
    {
        if (boostSource != null && accelerateClip != null) boostSource.PlayOneShot(accelerateClip);
    }
}
