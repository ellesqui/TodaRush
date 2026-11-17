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

    [TextArea] public string subtitle;
    [TextArea] public string description;
}
