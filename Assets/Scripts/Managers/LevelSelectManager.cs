using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Level select: pins choose a level, the info panel shows it, Start Mission loads it.
// All the values shown (name, difficulty, passengers, time, subtitle, description, color) come from each level's LevelData.
// A level can only start when the player's GLOBAL TotalXP reaches its requirement.
public class LevelSelectManager : MonoBehaviour
{
    public LevelData[] levels;      // Level_01, Level_02, Level_03 (in pin order)
    public GameObject[] pinHalos;   // pulsing halo for each pin
    public TMP_Text[] pinLabels;    // map name label for each pin
    public GameObject[] routes;     // dashed route to each pin
    public Image accentStrip;
    public TMP_Text badgeText, titleText, subtitleText, descriptionText, passengersText, timeText, difficultyText;
    public TMP_Text xpRequirementText;   // XP needed, shown on the right of the LEVEL badge
    public Button startButton;

    private readonly int[] requiredTotalXP = { 0, 3000, 10000 };   // global XP needed to unlock each level

    private int selected = 0;

    void Start()
    {
        SelectLevel(0);
    }

    void Update()
    {
        // Subtle pulse on the selected pin's halo
        if (selected >= pinHalos.Length || pinHalos[selected] == null) return;

        float s = 1f + 0.08f * Mathf.Sin(Time.time * 3f);
        pinHalos[selected].transform.localScale = new Vector3(s, s, 1f);
    }

    // Called by the pin buttons: 0 = Level 1, 1 = Level 2, 2 = Level 3
    public void SelectLevel(int index)
    {
        if (levels == null || index < 0 || index >= levels.Length) return;

        selected = index;

        for (int i = 0; i < levels.Length; i++)
        {
            bool isSelected = i == selected;
            if (i < pinHalos.Length && pinHalos[i] != null) pinHalos[i].SetActive(isSelected);
            if (i < routes.Length && routes[i] != null) routes[i].SetActive(isSelected);
            if (i < pinLabels.Length && pinLabels[i] != null) pinLabels[i].fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
        }

        RefreshPanel();
    }

    // True when the global profile has enough XP for this level
    bool IsUnlocked(int index)
    {
        return ScoreManager.GetTotalXP() >= requiredTotalXP[index];
    }

    void RefreshPanel()
    {
        LevelData data = levels[selected];
        bool unlocked = IsUnlocked(selected);
        bool canStart = data.playable && data.sceneName != "";

        if (accentStrip != null) accentStrip.color = data.accentColor;
        if (badgeText != null)
        {
            badgeText.text = "LEVEL 0" + (selected + 1);
            badgeText.color = data.accentColor;
            if (xpRequirementText != null)
                xpRequirementText.text = selected == 0 ? "" : "KAILANGAN: " + requiredTotalXP[selected].ToString("N0") + " XP";
        }

        SetText(titleText, data.levelName);
        SetText(descriptionText, data.description);
        SetText(difficultyText, data.difficulty);
        SetText(passengersText, data.passengerCount.ToString());
        SetText(timeText, FormatTime(data.timeLimit));

        if (!unlocked)
        {
            SetText(subtitleText, "Kailangan ng " + requiredTotalXP[selected].ToString("N0") + " Total XP para ma-unlock.");
        }
        else
        {
            SetText(subtitleText, data.subtitle);
        }

        if (startButton != null) startButton.interactable = canStart && unlocked;
    }

    // 270 seconds -> "04:30"
    string FormatTime(float seconds)
    {
        int total = Mathf.RoundToInt(seconds);
        return (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
    }

    // Start Mission: loads the selected level through the loading screen, only if it is playable and unlocked
    public void StartMission()
    {
        LevelData data = levels[selected];
        if (!(data.playable && data.sceneName != "")) return;
        if (!IsUnlocked(selected)) return;

        LoadingManager.LoadThroughLoadingScreen(data.sceneName);
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    private void SetText(TMP_Text label, string value)
    {
        if (label != null) label.text = value;
    }
}
