using System.Collections;
using UnityEngine;

public enum EffectType { None, Slow, Push, Spin }

public class Obstacle : Interactable
{
    public Animator reactionAnimator;
    public string reactionTrigger = "ObstacleBump";

    // Points lost from the score when this obstacle hits the player.
    public int damage = 10;

    // Seconds to wait before this obstacle can hurt the player again.
    public float damageCooldown = 1f;

    // Turn on for non-blocking potholes (trigger colliders) that should also damage the Player.
    public bool damageOnTrigger = false;

    // Extra effect on the player after a hit. Pick it in the Inspector.
    public EffectType effect = EffectType.None;
    public float effectSeconds = 1.5f;   // how long Slow and Spin last
    public float slowAmount = 0.5f;      // 0.5 = half speed
    public float pushDegrees = 30f;      // how much Push turns the player
    public float pushDistance = 1.5f;    // how far Push shoves the player sideways

    // Stops repeated damage while the Player stays inside the trigger.
    private bool playerInside = false;

    // Time of the last hit, used for the cooldown.
    private float lastHitTime = -999f;

    private static bool isSlowed = false;
    private static float originalSpeed;

    // Settings asset (Assets/Scripts/Data/Obstacles). Assign it in the Inspector.
    // When assigned, its values are used: damage (points lost), cooldown, trigger, and effect.
    public ObstacleData data;

    protected override void Awake()
    {
        base.Awake();
        if (data == null) return;

        damage = data.damage;
        damageCooldown = data.damageCooldown;
        damageOnTrigger = data.damageOnTrigger;
        effect = data.effect;
        effectSeconds = data.effectSeconds;
        slowAmount = data.slowAmount;
        pushDegrees = data.pushDegrees;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        if (reactionAnimator != null) reactionAnimator.SetTrigger(reactionTrigger);

        if (gameManager != null && CanHit())
        {
            gameManager.TakeDamage(damage);
            PassengerPickup.PlayerGotHit(gameManager);   // patience: one passenger loses 1 patience
            FlashRed();
            ApplyEffect();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (reactionAnimator != null) reactionAnimator.SetTrigger(reactionTrigger);

        if (damageOnTrigger && !playerInside && gameManager != null && CanHit())
        {
            playerInside = true;
            gameManager.TakeDamage(damage);
            PassengerPickup.PlayerGotHit(gameManager);   // patience: one passenger loses 1 patience
            FlashRed();
            ApplyEffect();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) playerInside = false;
    }

    // Returns true only if the cooldown has passed, then starts a new cooldown
    private bool CanHit()
    {
        if (Time.time - lastHitTime < damageCooldown) return false;
        lastHitTime = Time.time;
        return true;
    }

    private void FlashRed()
    {
        UIManager ui = FindAnyObjectByType<UIManager>();
        if (ui != null) ui.ShowDamageFlash();
    }

    // Runs the effect picked in the Inspector
    private void ApplyEffect()
    {
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player == null) return;

        if (effect == EffectType.Slow && !isSlowed) StartCoroutine(SlowRoutine(player));
        if (effect == EffectType.Push) StartCoroutine(PushRoutine(player));
        if (effect == EffectType.Spin) StartCoroutine(SpinRoutine(player));
    }

    private IEnumerator SlowRoutine(PlayerController player)
    {
        isSlowed = true;
        originalSpeed = player.moveSpeed;
        player.moveSpeed = originalSpeed * slowAmount;

        yield return new WaitForSeconds(effectSeconds);

        player.moveSpeed = originalSpeed;
        isSlowed = false;
    }

    // Shoves the player sideways, away from this obstacle, and turns them a bit
    private IEnumerator PushRoutine(PlayerController player)
    {
        Rigidbody rb = player.GetComponent<Rigidbody>();

        // Which side of the player is away from the obstacle
        float side = Vector3.Dot(player.transform.position - transform.position, player.transform.right) >= 0f ? 1f : -1f;

        player.transform.Rotate(0f, side * pushDegrees, 0f);

        float duration = 0.25f;
        float timer = 0f;
        while (timer < duration)
        {
            if (rb != null)
            {
                float step = pushDistance * Time.fixedDeltaTime / duration;
                rb.MovePosition(rb.position + player.transform.right * side * step);
            }
            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
    }

    private IEnumerator SpinRoutine(PlayerController player)
    {
        float timer = 0f;
        while (timer < effectSeconds)
        {
            player.transform.Rotate(0f, 360f * Time.deltaTime / effectSeconds, 0f);
            timer += Time.deltaTime;
            yield return null;
        }
    }
}
