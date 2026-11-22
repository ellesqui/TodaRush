using UnityEngine;

public enum PickupType { Gear, Gas, Timer }

// Settings for a road pickup. Right-click in Project > Create > TodaRush > Pickup Data.
[CreateAssetMenu(menuName = "TodaRush/Pickup Data", fileName = "Pickup_New")]
public class PickupData : ScriptableObject
{
    public PickupType type = PickupType.Gear;
    public int minLevel = 1;                    // Gear = 1, Gas = 2, Timer = 3
    public float amount = 10f;                  // Gear: health to restore. Timer: seconds to add.
    public float boostPercent = 0.2f;           // Gas: speed bonus, 0.2 = +20% (placeholder, confirm later)
    public float boostSeconds = 10f;            // Gas: how long the boost lasts
    public float respawnDelay = 15f;            // seconds before it reappears on a random road
}
