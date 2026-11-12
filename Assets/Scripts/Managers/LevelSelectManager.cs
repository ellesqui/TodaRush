using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Level select: pins choose a level, the info panel shows it, Start Mission loads it.
public class LevelSelectManager : MonoBehaviour
{
    // Assigned by the scene setup (each array should have 3 items: Level 1, 2, 3)
    public Image[] pinBodies;       // 3 pins: green, orange, red
    public GameObject[] pinHalos;   // pulsing halo for each pin
    public TMP_Text[] pinLabels;    // map name label for each pin
    public GameObject[] routes;     // dashed route to each pin
    public Image accentStrip;
    public TMP_Text badgeText;
    public TMP_Text titleText;
    public TMP_Text subtitleText;
    public TMP_Text descriptionText;
    public TMP_Text passengersText;
    public TMP_Text timeText;
    public TMP_Text difficultyText;
    public Button startButton;

    // Verified values: MainMap = 3 passengers, Level_02 = 4 passengers, both 180 s time limit
    private readonly string[] levelNames = { "Purok Residential", "Palengke District", "Bukid Hillside" };
    private readonly string[] levelScenes = { "MainMap", "Level_02", "" }; // Level 3 has no scene yet
    private readonly int[] passengerCounts = { 3, 4, 0 };
    private readonly string[] difficultyNames = { "Easy", "Medium", "Locked" };
    private readonly string[] subtitles = {
        "3 passengers · 03:00 time limit",
        "Crowded market streets · 4 passengers",
        "New route coming soon." };
    private readonly string[] descriptions = {
        "Deliver passengers through the purok streets before the clock runs out.",
        "Weave through market streets and traffic. Keep your wheels steady and deliver every passenger before the timer runs out.",
        "This route is currently under development. Check back for future missions." };
    private readonly string timeLimit = "03:00";
    private readonly Color[] levelColors = {
        new Color32(0x3E, 0x9B, 0x6C, 255), new Color32(0xF2, 0x8C, 0x28, 255), new Color32(0xD6, 0x28, 0x28, 255) };

    private int selected = 0;

    void Start()
    {
        SelectLevel(0);
    }

    void Update()
    {
        // Subtle pulse on the selected pin's halo (skipped if the halo is not assigned)
        if (!HasIndex(pinHalos, selected) || pinHalos[selected] == null) return;

        float s = 1f + 0.08f * Mathf.Sin(Time.time * 3f);
        pinHalos[selected].transform.localScale = new Vector3(s, s, 1f);
    }

    // Called by the pin buttons: 0 = Level 1, 1 = Level 2, 2 = Level 3
    public void SelectLevel(int index)
    {
        // Ignore invalid indexes so a wrong button can't crash the screen
        if (index < 0 || index >= levelNames.Length) return;

        selected = index;

        for (int i = 0; i < levelNames.Length; i++)
        {
            bool isSelected = i == selected;

            if (HasIndex(pinHalos, i) && pinHalos[i] != null) pinHalos[i].SetActive(isSelected);
            if (HasIndex(routes, i) && routes[i] != null) routes[i].SetActive(isSelected);
            if (HasIndex(pinLabels, i) && pinLabels[i] != null)
            {
                pinLabels[i].fontStyle = isSelected ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal;
            }
        }

        RefreshPanel();
    }

    void RefreshPanel()
    {
        Color c = levelColors[selected];
        bool playable = levelScenes[selected] != "";

        if (accentStrip != null) accentStrip.color = c;
        if (badgeText != null)
        {
            badgeText.text = "LEVEL 0" + (selected + 1);
            badgeText.color = c;
        }
        if (titleText != null) titleText.text = levelNames[selected];

        SetText(subtitleText, subtitles[selected]);
        SetText(descriptionText, descriptions[selected]);
        SetText(difficultyText, difficultyNames[selected]);

        if (playable)
        {
            SetText(passengersText, passengerCounts[selected].ToString());
            SetText(timeText, timeLimit);
        }
        else
        {
            SetText(passengersText, "--");
            SetText(timeText, "--");
        }

        if (startButton != null) startButton.interactable = playable;
    }

    // Start Mission: loads the selected playable level through the existing loading screen
    public void StartMission()
    {
        if (selected < 0 || selected >= levelScenes.Length) return;
        if (levelScenes[selected] == "") return;

        LoadingManager.LoadThroughLoadingScreen(levelScenes[selected]);
    }

    // Kept from the original script (no longer wired to a button)
    public void SelectLevel1()
    {
        LoadingManager.LoadThroughLoadingScreen("MainMap");
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    // Safe helpers: check the array length and null before using an index
    private static bool HasIndex<T>(T[] array, int index)
    {
        return array != null && index >= 0 && index < array.Length;
    }

    private static void SetText(TMP_Text label, string value)
    {
        if (label != null) label.text = value;
    }
}
