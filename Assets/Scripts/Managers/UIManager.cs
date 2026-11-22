using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI healthText, scoreText, highScoreText, timerText, damagePopupText, speedText;
    public TextMeshProUGUI scoreValueText, passengersValueText, timeValueText;
    public TextMeshProUGUI gameOverScoreValueText, gameOverPassengersValueText, gameOverHealthValueText, failureReasonText;
    public Image healthBarFill;
    public PassengerProgressWidget passengerProgress;
    public GameObject gameplayHUD, victoryDimOverlay, victorySummaryPanel, pauseOverlay, successNotification;
    public GameManager gameManager;
    public TripTimer tripTimer;
    public PlayerController playerController;
    public float healthFillDuration = 0.4f;
    public float popupDuration = 1.3f, popupRiseDistance = 50f, popupStartScale = 1.2f;

    [Header("Full Map")]
    public Camera fullMapCamera;
    public GameObject fullMapPanel;

    [Header("Passenger Dialogue HUD")]
    public GameObject passengerDialogueHUD;
    public UnityEngine.UI.Image damageFlash;
    private float flashAlpha;
    public TextMeshProUGUI passengerNameText, speechText;
    public float speechSeconds = 4f;   // how long the passenger speech stays on screen

    [Header("Passenger Speech Lines (default for now)")]
    public string[] boardingLines = { "Salamat po, boss!", "Sige po, bilisan natin!", "Tara na po!" };
    public string[] reactionLines = { "Aray! Dahan-dahan naman, boss!", "Ingat po tayo!", "Diyos ko po, ingat!" };
    public string[] dropLines = { "Salamat po, boss!", "Ingat po kayo!" };

    private float healthFillShown = -1f, healthFillFrom, healthFillTarget, healthFillTimer;
    private Coroutine popupCoroutine;
    private Coroutine speechCoroutine;
    private RectTransform popupRect;
    private Vector2 popupStartPosition;
    private Vector3 popupNormalScale;
    private Color popupColor;
    private bool summaryShown, gameOverShown, isPaused;

    private const string TimerExpiredReason = "The timer ran out before all passengers were delivered.";

    void Start()
    {
        Time.timeScale = 1f;
        isPaused = false;
        if (pauseOverlay != null) pauseOverlay.SetActive(false);
        CachePopup();
        if (passengerProgress != null) passengerProgress.Initialize(gameManager, UnityEngine.Object.FindAnyObjectByType<Queue>());
        if (damagePopupText != null) damagePopupText.gameObject.SetActive(false);
        if (passengerDialogueHUD != null) passengerDialogueHUD.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();
        if (Input.GetKeyDown(KeyCode.R)) ToggleFullMap();
        FadeDamageFlash();
        if (gameManager == null) return;

        if (gameManager.gameWon)
        {
            UpdateMissionSummary();
            return;
        }

        if (!gameOverShown && (gameManager.timeExpired || gameManager.health <= 0)) UpdateGameOverStats();

        UpdateHealthDisplay();
        scoreText.text = "Score: " + gameManager.scoreManager.score;
        highScoreText.text = "High Score: " + gameManager.scoreManager.highScore;
        UpdateTimerText();
        speedText.text = playerController.currentSpeed.ToString("0");
    }

    // PASSENGER SPEECH (shows for a few seconds, then hides)
    public void PassengerBoarded(PassengerPickup passenger)
    {
        ShowPassengerSpeech(passenger, PickLine(boardingLines));
    }

    public void PassengerDropped(PassengerPickup passenger)
    {
        ShowPassengerSpeech(passenger, PickLine(dropLines));
    }

    // Called every few hits (see GameManager)
    public void PlayerHitReaction()
    {
        foreach (PassengerPickup passenger in gameManager.passengers)
        {
            if (passenger != null && passenger.hasBoarded && passenger.targetDestination != null)
            {
                ShowPassengerSpeech(passenger, PickLine(reactionLines));
                return;
            }
        }
    }

    private void ShowPassengerSpeech(PassengerPickup passenger, string line)
    {
        if (passengerDialogueHUD == null || speechText == null) return;

        int index = System.Array.IndexOf(gameManager.passengers, passenger) + 1;
        if (passengerNameText != null) passengerNameText.text = "Passenger " + index.ToString("00");
        speechText.text = line;
        passengerDialogueHUD.SetActive(true);

        if (speechCoroutine != null) StopCoroutine(speechCoroutine);
        speechCoroutine = StartCoroutine(HideSpeechAfterDelay());
    }

    private IEnumerator HideSpeechAfterDelay()
    {
        yield return new WaitForSeconds(speechSeconds);
        passengerDialogueHUD.SetActive(false);
        speechCoroutine = null;
    }

    private string PickLine(string[] lines)
    {
        if (lines == null || lines.Length == 0) return "";
        return lines[Random.Range(0, lines.Length)];
    }

    // RED FLASH WHEN THE PLAYER GETS HURT
    public void ShowDamageFlash() { flashAlpha = 0.45f; }

    private void FadeDamageFlash()
    {
        if (damageFlash == null) return;
        flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, Time.unscaledDeltaTime * 1.5f);
        damageFlash.color = new Color(1f, 0f, 0f, flashAlpha);
    }

    // FULL MAP (R key or click minimap to open; R key or click full map to close)
    public void ToggleFullMap()
    {
        if (fullMapPanel == null) return;

        bool show = !fullMapPanel.activeSelf;
        fullMapPanel.SetActive(show);
        if (fullMapCamera != null) fullMapCamera.enabled = show;
    }

    // SETTINGS (opened from the pause menu)
    public GameObject settingsPanel;

    public void OpenSettings()
    {
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
    }

    // SETTINGS TABS (Audio / Graphics / Controls)
    public GameObject audioContent, graphicsContent, controlsContent;
    public Image audioTab, graphicsTab, controlsTab;

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

    // SUCCESS / DELIVERED NOTIFICATION (shows for 2 seconds)
    public void ShowSuccess(string message = "SUCCESS")
    {
        if (successNotification == null) return;

        successNotification.GetComponentInChildren<TextMeshProUGUI>(true).text = message;
        successNotification.SetActive(true);
        CancelInvoke(nameof(HideSuccess));
        Invoke(nameof(HideSuccess), 2f);
    }

    void HideSuccess()
    {
        successNotification.SetActive(false);
    }

    // HEALTH BAR
    private void UpdateHealthDisplay()
    {
        int maxHealth = Mathf.Max(1, gameManager.maxHealth);
        float healthPercent = (float)Mathf.Clamp(gameManager.health, 0, maxHealth) / maxHealth;

        if (healthText != null) healthText.text = Mathf.RoundToInt(healthPercent * 100f) + "%";
        UpdateHealthFill(healthPercent);
    }

    private void UpdateHealthFill(float targetPercent)
    {
        if (healthBarFill == null) return;

        targetPercent = Mathf.Clamp01(targetPercent);

        if (healthFillShown < 0f)
        {
            healthFillShown = healthFillFrom = healthFillTarget = targetPercent;
            healthFillTimer = healthFillDuration;
        }

        if (!Mathf.Approximately(targetPercent, healthFillTarget))
        {
            healthFillFrom = healthFillShown;
            healthFillTarget = targetPercent;
            healthFillTimer = 0f;
        }

        if (healthFillTimer < healthFillDuration)
        {
            healthFillTimer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(healthFillTimer / healthFillDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f); // approximates cubic-bezier(0.25, 1, 0.5, 1)
            healthFillShown = Mathf.Lerp(healthFillFrom, healthFillTarget, eased);
        }
        else
        {
            healthFillShown = healthFillTarget;
        }

        RectTransform barRect = healthBarFill.rectTransform.parent as RectTransform;
        float innerWidth = barRect != null ? barRect.rect.width : 364f;
        Vector2 size = healthBarFill.rectTransform.sizeDelta;
        healthBarFill.rectTransform.sizeDelta = new Vector2(innerWidth * Mathf.Clamp01(healthFillShown), size.y);
    }

    // PAUSE SYSTEM
    private bool CanPause()
    {
        return gameManager != null && !gameManager.gameWon && !gameManager.timeExpired && gameManager.health > 0;
    }

    public void TogglePause()
    {
        if (isPaused)
        {
            ResumePause();
            return;
        }

        if (!CanPause() || pauseOverlay == null) return;

        isPaused = true;
        pauseOverlay.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ResumePause()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pauseOverlay != null) pauseOverlay.SetActive(false);
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        SceneManager.LoadScene("MainMenu");
    }

    // DAMAGE POPUP
    public void ShowDamagePopup(int damageAmount)
    {
        if (damagePopupText == null) return;

        CachePopup();
        if (popupCoroutine != null) StopCoroutine(popupCoroutine);

        popupRect.anchoredPosition = popupStartPosition;
        popupRect.localScale = popupNormalScale * popupStartScale;
        damagePopupText.color = popupColor;
        damagePopupText.text = "-" + damageAmount + " DAMAGE!";
        damagePopupText.gameObject.SetActive(true);

        popupCoroutine = StartCoroutine(AnimatePopup());
    }

    private IEnumerator AnimatePopup()
    {
        float timer = 0f;

        while (timer < popupDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / popupDuration);

            popupRect.anchoredPosition = popupStartPosition + Vector2.up * (popupRiseDistance * t);
            popupRect.localScale = popupNormalScale * Mathf.Lerp(popupStartScale, 1f, Mathf.Clamp01(t * 4f));
            damagePopupText.color = new Color(popupColor.r, popupColor.g, popupColor.b, popupColor.a * (1f - t));

            yield return null;
        }

        damagePopupText.gameObject.SetActive(false);
        popupCoroutine = null;
    }

    private void CachePopup()
    {
        if (popupRect != null || damagePopupText == null) return;

        popupRect = damagePopupText.rectTransform;
        popupStartPosition = popupRect.anchoredPosition;
        popupNormalScale = popupRect.localScale;
        popupColor = damagePopupText.color;
    }

    // TIMER DISPLAY
    private string FormatTime(float seconds)
    {
        return Mathf.FloorToInt(seconds / 60f).ToString("00") + ":" + Mathf.FloorToInt(seconds % 60f).ToString("00");
    }

    private void UpdateTimerText()
    {
        if (tripTimer == null || timerText == null) return;

        if (gameManager.timeExpired) timerText.text = "TIME'S UP!";
        else timerText.text = FormatTime(Mathf.Max(0f, tripTimer.GetTimeRemaining()));
    }

    // VICTORY SUMMARY
    private void UpdateMissionSummary()
    {
        if (summaryShown) return;
        if (gameplayHUD == null || victoryDimOverlay == null || victorySummaryPanel == null || tripTimer == null || gameManager.scoreManager == null) return;

        summaryShown = true;
        gameplayHUD.SetActive(false);
        victoryDimOverlay.SetActive(true);
        victorySummaryPanel.SetActive(true);

        string time = FormatTime(tripTimer.GetTimeRemaining());
        int score = gameManager.scoreManager.score;

        SetText(scoreValueText, score.ToString());
        SetText(passengersValueText, gameManager.destinationCount + " / " + gameManager.requiredLaps);
        SetText(timeValueText, time);
    }

    // GAME OVER STATISTICS
    private void UpdateGameOverStats()
    {
        if (gameManager.scoreManager == null) return;

        gameOverShown = true;

        SetText(gameOverScoreValueText, gameManager.scoreManager.score.ToString());
        SetText(gameOverPassengersValueText, gameManager.destinationCount + " / " + gameManager.requiredLaps);
        SetText(gameOverHealthValueText, Mathf.Clamp(gameManager.health, 0, Mathf.Max(1, gameManager.maxHealth)).ToString());

        // Only the timer can produce this reason. Health depletion keeps it hidden.
        if (failureReasonText != null)
        {
            failureReasonText.text = TimerExpiredReason;
            failureReasonText.gameObject.SetActive(gameManager.timeExpired);
        }
    }

    private void SetText(TextMeshProUGUI target, string value)
    {
        if (target != null) target.text = value;
    }
}
