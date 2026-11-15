using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("Destination — assign a confirmed scene in Build Settings")]
    [SerializeField] private string levelSelectionSceneName;
    [SerializeField] private string gameplaySceneName;
    [Header("Panels")]
    [SerializeField] private GameObject menuButtons;
    [SerializeField] private GameObject howToPlayPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private UnityEngine.UI.Button playButton;
    [SerializeField] private UnityEngine.UI.Slider masterVolume;
    [SerializeField] private UnityEngine.UI.Toggle muteToggle;
    private GameObject currentPanel;
    private float volume = 1f;
    private bool muted;
    private void Start()
    {
        GameSettingsManager.Ensure();
        Back();
    }
    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame && currentPanel) Back();
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Escape) && currentPanel) Back();
#endif
    }
    public void Play()
    {
        string scene = string.IsNullOrWhiteSpace(levelSelectionSceneName) ? gameplaySceneName : levelSelectionSceneName;
        if (string.IsNullOrWhiteSpace(scene)) { SetStatus("Choose a gameplay or level-selection scene on MainMenuController."); return; }
        if (!Application.CanStreamedLevelBeLoaded(scene)) { SetStatus("The configured scene is unavailable. Add it to Build Settings."); return; }
        SceneManager.LoadScene(scene);
    }
    public void HowToPlay() { Open(howToPlayPanel); var logo = transform.Find("LogoContainer"); if (logo) logo.gameObject.SetActive(false); }
    public void Settings() { Open(settingsPanel); var logo = transform.Find("LogoContainer"); if (logo) logo.gameObject.SetActive(false); }
    public void Credits() { Open(creditsPanel); var logo = transform.Find("LogoContainer"); if (logo) logo.gameObject.SetActive(false); }
    private void Open(GameObject panel)
    {
        if (currentPanel) currentPanel.SetActive(false);
        currentPanel = panel;
        if (menuButtons) menuButtons.SetActive(false);
        if (panel) { panel.SetActive(true); var b = panel.GetComponentInChildren<UnityEngine.UI.Button>(); if (b && EventSystem.current) EventSystem.current.SetSelectedGameObject(b.gameObject); }
        SetStatus("");
    }
    public void Back()
    {
        if (howToPlayPanel) howToPlayPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
        if (creditsPanel) creditsPanel.SetActive(false);
        currentPanel = null;
        var logo = transform.Find("LogoContainer"); if (logo) logo.gameObject.SetActive(true);
        if (GameSettingsManager.Instance) GameSettingsManager.Instance.Save();
        if (menuButtons) menuButtons.SetActive(true);
        if (playButton && EventSystem.current) EventSystem.current.SetSelectedGameObject(playButton.gameObject);
    }
    public void SetMasterVolume(float value) { volume = value; PlayerPrefs.SetFloat("TODARUSH.MasterVolume", value); ApplyVolume(); }
    public void SetMuted(bool value) { muted = value; PlayerPrefs.SetInt("TODARUSH.Muted", value ? 1 : 0); ApplyVolume(); }
    private void ApplyVolume() { if (Application.isPlaying) GameSettingsManager.Ensure().SetMute(muted); }
    private void SetStatus(string value) { if (statusText) statusText.text = value; }
    public void Quit()
    {
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        SetStatus("Quit requested. The built game will close.");
#else
        Application.Quit();
#endif
    }
    private void OnDisable() { PlayerPrefs.Save(); }
}
