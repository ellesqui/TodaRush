using UnityEngine;

// Settings for chasing/wandering obstacles (like the dog).
// Right-click in Project > Create > TodaRush > Obstacle Data.
[CreateAssetMenu(menuName = "TodaRush/Obstacle Data", fileName = "Obstacle_New")]
public class ObstacleData : ScriptableObject
{
    public float detectRadius = 12f;  // start chasing when player is this close
    public float loseRadius = 20f;    // stop chasing when player is this far
    public float walkSpeed = 3f;
    public float chaseSpeed = 8f;
    public float wanderRadius = 30f;
    public int damage = 10;
}
