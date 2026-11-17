using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public GameObject mainMenuPanel, settingsPanel, creditsPanel, howToPlayPanel, quitConfirmationPanel;
    public GameObject audioContent, graphicsContent, controlsContent;
    public Image audioTab, graphicsTab, controlsTab;
    public string mainMenuSceneName = "MainMenu";

    // Mouse parallax for the menu background
    public Transform menuBackground;
    public float parallaxStrength = 20f;

    public void PlayGame() { SceneManager.LoadScene("LevelSelection"); }
    public void ExitGame() { Application.Quit(); }
    public void RestartGame() { SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
    public void BackToMenu() { SceneManager.LoadScene(mainMenuSceneName); }
    public void SetMasterVolume(float volume) { AudioListener.volume = volume; }

    // Panels: open one, hide the main menu. Close: show the main menu again.
    public void OpenSettings() { Open(settingsPanel); }
    public void OpenCredits() { Open(creditsPanel); }
    public void OpenHowToPlay() { Open(howToPlayPanel); }
    public void OpenQuitConfirmation() { Open(quitConfirmationPanel); }

    public void BackToMainMenu()
    {
        mainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
        creditsPanel.SetActive(false);
    }

    public void CloseHowToPlay() { Close(howToPlayPanel); }
    public void CloseQuitConfirmation() { Close(quitConfirmationPanel); }

    // Settings sidebar tabs: the selected tab shows its content and turns orange.
    public void ShowAudioSettings() { ShowTab(0); }
    public void ShowGraphicsSettings() { ShowTab(1); }
    public void ShowControlsSettings() { ShowTab(2); }

    void Open(GameObject panel)
    {
        mainMenuPanel.SetActive(false);
        panel.SetActive(true);
    }

    void Close(GameObject panel)
    {
        panel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

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
