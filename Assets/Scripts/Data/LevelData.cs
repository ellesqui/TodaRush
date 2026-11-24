using UnityEngine;

// One asset per level. Right-click in Project > Create > TodaRush > Level Data.
[CreateAssetMenu(menuName = "TodaRush/Level Data", fileName = "Level_New")]
public class LevelData : ScriptableObject
{
    public string levelName = "Level Name";
    public string sceneName = "";          // empty = not playable yet
    public bool playable = true;
    public string difficulty = "Easy";
    public int passengerCount = 3;         // passengers picked up at the terminal (Level 1 = 3, Level 2 = 4, Level 3 = 5)
    public int destinationCount = 3;       // drop-off destinations for this level (one per passenger)
    public float timeLimit = 180f;         // seconds
    public Color accentColor = Color.white;

    [Header("Destinations")]
    public bool randomDestinations = false;          // true = destinations are placed randomly on the road each run (Level 2)
    public float destinationMinGap = 60f;            // minimum distance between two destinations (meters)
    public float destinationMinFromTerminal = 80f;   // minimum distance from the terminal (meters)

    [Header("Environment (fixed for this level)")]
    public Material skybox;                // the sky for this level (for example Skybox 1, 5, or 9)
    public AudioClip environmentSound;     // the environment sound for this level (Rural, Urban, or Night)
    public int lightingPreset = 0;         // 0 = day, 1 = afternoon, 2 = night (sun, ambient, and headlight)
    public bool rainEnabled = false;       // true = rain (puddles and blur spots) can happen in this level

    [Header("Features (unlocked per level)")]
    public bool hornEnabled = false;       // true = horn (F key and horn button) works in this level

    [TextArea] public string subtitle;
    [TextArea] public string description;
}
