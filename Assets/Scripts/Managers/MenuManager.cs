using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Only what Unity's built-in button events cannot do:
// settings tab highlight, master volume, restart of the current scene, and the mouse parallax on the menu background.
// Panel open/close and scene loads are button events set in the Inspector.
public class MenuManager : MonoBehaviour
{
    public GameObject audioContent, graphicsContent, controlsContent;
    public Image audioTab, graphicsTab, controlsTab;

    // Mouse parallax for the menu background
    public Transform menuBackground;
    public float parallaxStrength = 20f;

    // Restarts whichever level is currently loaded (MainMap, Level_02, ...)
    public void RestartGame() { SceneManager.LoadScene(SceneManager.GetActiveScene().name); }

    public void SetMasterVolume(float volume) { AudioListener.volume = volume; }

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
