
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 15f;
    public float acceleration = 6f;
    public float deceleration = 4f;
    public float braking = 8f;
    public float reverseSpeed = 4f;
    public float turnSpeed = 100f;

    public GameManager gameManager;
    public float currentSpeed = 0f;

    private Rigidbody rb;
    private float moveInput;
    private float turnInput;
    private float drivingSpeed = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (gameManager.health <= 0 || gameManager.gameWon || gameManager.timeExpired)
        {
            moveInput = 0f;
            turnInput = 0f;
            return;
        }

        moveInput = Input.GetAxisRaw("Vertical");
        turnInput = Input.GetAxis("Horizontal");
    }

    void FixedUpdate()
    {
        if (gameManager.health <= 0 || gameManager.gameWon || gameManager.timeExpired)
        {
            drivingSpeed = 0f;

            Vector3 stoppedVelocity = rb.linearVelocity;
            stoppedVelocity.x = 0f;
            stoppedVelocity.z = 0f;
            rb.linearVelocity = stoppedVelocity;

            currentSpeed = 0f;
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
    }
}
