using UnityEngine;

public class Obstacle : Interactable
{
    public Animator reactionAnimator;
    public string reactionTrigger = "ObstacleBump";

    // Turn on for non-blocking potholes (trigger colliders) that should also damage the Player.
    public bool damageOnTrigger = false;

    // Stops repeated damage while the Player stays inside the trigger.
    private bool playerInside = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") && gameManager != null)
        {
            gameManager.TakeDamage();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (reactionAnimator != null)
        {
            reactionAnimator.SetTrigger(reactionTrigger);
        }

        if (damageOnTrigger && !playerInside && gameManager != null)
        {
            playerInside = true;
            gameManager.TakeDamage();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
        }
    }
}
