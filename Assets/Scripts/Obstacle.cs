using System.Collections;
using UnityEngine;

public enum EffectType { None, Slow, Push, Spin }

public class Obstacle : Interactable
{
    public Animator reactionAnimator;
    public string reactionTrigger = "ObstacleBump";

    // Damage this obstacle deals to the player.
    public int damage = 10;

    // Turn on for non-blocking potholes (trigger colliders) that should also damage the Player.
    public bool damageOnTrigger = false;

    // Extra effect on the player after a hit. Pick it in the Inspector.
    public EffectType effect = EffectType.None;
    public float effectSeconds = 1.5f;   // how long Slow and Spin last
    public float slowAmount = 0.5f;      // 0.5 = half speed
    public float pushDegrees = 30f;      // how much Push turns the player

    // Stops repeated damage while the Player stays inside the trigger.
    private bool playerInside = false;

    private static bool isSlowed = false;
    private static float originalSpeed;

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        if (reactionAnimator != null) reactionAnimator.SetTrigger(reactionTrigger);

        if (gameManager != null)
        {
            gameManager.TakeDamage(damage);
            FlashRed();
            ApplyEffect();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (reactionAnimator != null) reactionAnimator.SetTrigger(reactionTrigger);

        if (damageOnTrigger && !playerInside && gameManager != null)
        {
            playerInside = true;
            gameManager.TakeDamage(damage);
            FlashRed();
            ApplyEffect();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) playerInside = false;
    }

    private void FlashRed()
    {
        UIManager ui = FindFirstObjectByType<UIManager>();
        if (ui != null) ui.ShowDamageFlash();
    }

    // Runs the effect picked in the Inspector
    private void ApplyEffect()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player == null) return;

        if (effect == EffectType.Slow && !isSlowed) StartCoroutine(SlowRoutine(player));
        if (effect == EffectType.Push) player.transform.Rotate(0f, pushDegrees, 0f);
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
