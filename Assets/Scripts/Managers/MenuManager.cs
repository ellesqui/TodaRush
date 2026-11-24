using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Only what Unity's built-in button events cannot do:
// settings tab highlight, master volume, restart of the current scene, the mouse parallax on the menu background,
// and the Next Level button (checks the XP needed for the next level).
public class MenuManager : MonoBehaviour
{
    public GameObject audioContent, graphicsContent, controlsContent;
    public Image audioTab, graphicsTab, controlsTab;

    // Mouse parallax for the menu background
    public Transform menuBackground;
    public float parallaxStrength = 20f;

    // Level this scene belongs to (only the levels that have a Next Level button set this)
    public LevelData levelData;

    // Restarts whichever level is currently loaded (MainMap, Level_02, ...)
    public void RestartGame() { SceneManager.LoadScene(SceneManager.GetActiveScene().name); }

    public void SetMasterVolume(float volume) { AudioListener.volume = volume; }

    // NEXT LEVEL button: loads the next level only if the player has enough Total XP.
    // If not, shows a notice with the XP needed (same layer as the DELIVERED notice).
    public void NextLevel()
    {
        if (levelData == null || levelData.nextLevelScene == "") return;

        int needed = levelData.nextLevelRequiredXP;
        if (ScoreManager.GetTotalXP() < needed)
        {
            UIManager ui = FindAnyObjectByType<UIManager>();
            if (ui != null) ui.ShowSuccess("KAILANGAN NG " + needed.ToString("N0") + " XP PARA MA-UNLOCK");
            return;
        }

        Time.timeScale = 1f;
        LoadingManager.LoadThroughLoadingScreen(levelData.nextLevelScene);
    }

    // Settings sidebar tabs: the selected tab shows its content and turns orange.
    public void ShowAudioSettings() { ShowTab(0); }
    public void ShowGraphicsSettings() { ShowTab(1); }
    public void ShowControlsSettings() { ShowTab(2); }

    void ShowTab(int index)
    {
        GameObject[] contents = { audioContent, graphicsContent, controlsContent };
        Image[] tabs = { audioTab, graphicsTab, controlsTab };
        Color orange = new Color32(242, 140, 40, 255);
        Color dark = new Color32(37, 39, 43, 255);

        for (int i = 0; i < 3; i++)
        {
            contents[i].SetActive(i == index);
            tabs[i].color = i == index ? orange : dark;
        }
    }

    void Update()
    {
        RectTransform bg = menuBackground as RectTransform;
        if (bg == null) return;

        // Mouse position from -1 to 1, moved in canvas space so it stays fitted on resize
        float x = (Input.mousePosition.x / Screen.width) * 2f - 1f;
        float y = (Input.mousePosition.y / Screen.height) * 2f - 1f;
        bg.anchoredPosition = new Vector2(x, y) * parallaxStrength;
    }
}
