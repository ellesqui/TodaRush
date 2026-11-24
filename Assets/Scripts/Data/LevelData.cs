using UnityEngine;

// One asset per level. Right-click in Project > Create > TodaRush > Level Data.
[CreateAssetMenu(menuName = "TodaRush/Level Data", fileName = "Level_New")]
public class LevelData : ScriptableObject
{
    public string levelName = "Level Name";
    public string sceneName = "";          // empty = not playable yet
    public bool playable = true;
    public string difficulty = "Easy";
    public int passengerCount = 3;
    public float timeLimit = 180f;         // seconds
    public Color accentColor = Color.white;

    [Header("Environment (fixed for this level)")]
    public Material skybox;                // the sky for this level (for example Skybox 1, 5, or 9)
    public AudioClip environmentSound;     // the environment sound for this level (Rural, Urban, or Night)
    public bool rainEnabled = false;       // true = rain (puddles and blur spots) can happen in this level

    [Header("Features (unlocked per level)")]
    public bool hornEnabled = false;       // true = horn (F key and horn button) works in this level

    [TextArea] public string subtitle;
    [TextArea] public string description;
}
