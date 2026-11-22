using UnityEngine;

// Settings for falling branches. Right-click in Project > Create > TodaRush > Falling Branch Data.
[CreateAssetMenu(menuName = "TodaRush/Falling Branch Data", fileName = "FallingBranch_New")]
public class FallingBranchData : ScriptableObject
{
    public float respawnInterval = 3f;   // seconds between each fall
    public float fallSpeed = 8f;         // how fast it drops
}
