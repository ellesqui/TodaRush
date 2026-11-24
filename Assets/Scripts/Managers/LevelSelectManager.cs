using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Level select: pins choose a level, the info panel shows it, Start Mission loads it.
// A level can only start when the player's GLOBAL TotalXP reaches its requirement.
public class LevelSelectManager : MonoBehaviour
{
    public GameObject[] pinHalos;   // pulsing halo for each pin (Level 1, 2, 3)
    public TMP_Text[] pinLabels;    // map name label for each pin
    public GameObject[] routes;     // dashed route to each pin
    public Image accentStrip;
    public TMP_Text badgeText, titleText, subtitleText, descriptionText, passengersText, timeText, difficultyText;
    public Button startButton;

    private readonly string[] levelNames = { "Purok Residential", "Palengke District", "Bukid Hillside" };
    private readonly string[] levelScenes = { "MainMap", "Level_02", "" }; // Level 3 has no scene yet
    private readonly int[] passengerCounts = { 3, 4, 5 };
    private readonly string[] timeLimits = { "02:00", "03:00", "04:30" };
    private readonly int[] requiredTotalXP = { 0, 3000, 10000 };   // global XP needed to unlock each level
    private readonly string[] difficultyNames = { "Easy", "Medium", "Locked" };
    private readonly string[] subtitles = {
        "3 passengers · 02:00 time limit",
        "4 passengers · 03:00 time limit",
        "New route coming soon." };
    private readonly string[] descriptions = {
        "Deliver passengers through the purok streets before the clock runs out.",
        "Weave through market streets and traffic. Keep your wheels steady and deliver every passenger before the timer runs out.",
        "This route is currently under development. Check back for future missions." };
    private readonly Color[] levelColors = {
        new Color32(0x3E, 0x9B, 0x6C, 255), new Color32(0xF2, 0x8C, 0x28, 255), new Color32(0xD6, 0x28, 0x28, 255) };

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
        if (index < 0 || index >= levelNames.Length) return;

        selected = index;

        for (int i = 0; i < levelNames.Length; i++)
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
        Color c = levelColors[selected];
        bool playable = levelScenes[selected] != "";
        bool unlocked = IsUnlocked(selected);

        if (accentStrip != null) accentStrip.color = c;
        if (badgeText != null)
        {
            badgeText.text = "LEVEL 0" + (selected + 1);
            badgeText.color = c;
        }

        SetText(titleText, levelNames[selected]);
        SetText(descriptionText, descriptions[selected]);
        SetText(difficultyText, difficultyNames[selected]);
        SetText(passengersText, playable ? passengerCounts[selected].ToString() : "--");
        SetText(timeText, playable ? timeLimits[selected] : "--");

        if (playable && !unlocked)
        {
            SetText(subtitleText, "Kailangan ng " + requiredTotalXP[selected].ToString("N0") + " Total XP para ma-unlock.");
        }
        else
        {
            SetText(subtitleText, subtitles[selected]);
        }

        if (startButton != null) startButton.interactable = playable && unlocked;
    }

    // Start Mission: loads the selected level through the loading screen, only if it is playable and unlocked
    public void StartMission()
    {
        if (levelScenes[selected] == "") return;
        if (!IsUnlocked(selected)) return;

        LoadingManager.LoadThroughLoadingScreen(levelScenes[selected]);
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
