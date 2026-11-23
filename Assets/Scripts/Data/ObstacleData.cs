using UnityEngine;

// Shared settings for obstacles. Right-click in Project > Create > TodaRush > Obstacle Data.
// One asset can be shared by many obstacles, or make a new asset for a different value set.
[CreateAssetMenu(menuName = "TodaRush/Obstacle Data", fileName = "Obstacle_New")]
public class ObstacleData : ScriptableObject
{
    [Header("Score penalty (points lost on hit)")]
    public int damage = 10;
    public float damageCooldown = 1f;
    public bool damageOnTrigger = false;

    [Header("Effect")]
    public EffectType effect = EffectType.None;
    public float effectSeconds = 1.5f;
    public float slowAmount = 0.5f;
    public float pushDegrees = 30f;

    [Header("Chasing (dogs only)")]
    public float detectRadius = 12f;
    public float loseRadius = 20f;
    public float walkSpeed = 3f;
    public float chaseSpeed = 8f;
    public float wanderRadius = 30f;
}
