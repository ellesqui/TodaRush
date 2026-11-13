
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI healthText;
    public Image healthBarFill;
    public Color healthNormalColor = new Color32(62, 155, 108, 255);
    public Color healthCriticalColor = new Color32(214, 40, 40, 255);
    [Range(0f, 1f)] public float criticalHealthThreshold = 0.3f;

    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI highScoreText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI damagePopupText;
    public TextMeshProUGUI lapText;
    public TextMeshProUGUI lapNotificationText;
    public TextMeshProUGUI missionSummaryText;

    // Victory statistics (WinPanel/ResultCard/StatsBox)
    public TextMeshProUGUI scoreValueText;
    public TextMeshProUGUI passengersValueText;
    public TextMeshProUGUI timeValueText;

    // Game Over statistics (GameOverPanel/ResultCard/StatsBox)
    public TextMeshProUGUI gameOverScoreValueText;
    public TextMeshProUGUI gameOverPassengersValueText;
    public TextMeshProUGUI gameOverHealthValueText;
    public TextMeshProUGUI failureReasonText;

    public GameObject gameplayHUD;
    public GameObject victoryDimOverlay;
    public GameObject victorySummaryPanel;

    public GameObject pauseOverlay;

    public GameManager gameManager;
    public TripTimer tripTimer;
    public PlayerController playerController;
    public TextMeshProUGUI speedText;

    public float popupDuration = 1.3f;
    public float popupRiseDistance = 50f;
    public float popupStartScale = 1.2f;
    public float notificationDuration = 2.5f;

    private Coroutine popupCoroutine;
    private Coroutine notificationCoroutine;

    private RectTransform popupRect;
    private Vector2 popupStartPosition;
    private Vector3 popupNormalScale;
    private Color popupColor;

    private int previousDisplayedLap = -1;
    private int previousCompletedLaps = 0;
    private bool summaryShown = false;
    private bool gameOverShown = false;

    private bool isPaused = false;

    private const string TimerExpiredReason = "The timer ran out before all passengers were delivered.";

    void Start()
    {
        Time.timeScale = 1f;
        isPaused = false;

        if (pauseOverlay != null)
        {
            pauseOverlay.SetActive(false);
        }

        CachePopup();

        if (damagePopupText != null)
        {
            damagePopupText.gameObject.SetActive(false);
        }

        if (lapNotificationText != null)
        {
            lapNotificationText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }

        if (gameManager == null)
        {
            return;
        }

        if (gameManager.gameWon)
        {
            UpdateMissionSummary();
            return;
        }

        if (!gameOverShown && (gameManager.timeExpired || gameManager.health <= 0))
        {
            UpdateGameOverStats();
        }

        UpdateHealthDisplay();

        scoreText.text = "Score: " + gameManager.scoreManager.score.ToString();
        highScoreText.text = "High Score: " + gameManager.scoreManager.highScore.ToString();

        UpdateTimerText();
        UpdateLapText();
        UpdateLapNotification();

        speedText.text = playerController.currentSpeed.ToString("0");
    }

    // HEALTH BAR

    private void UpdateHealthDisplay()
    {
        int maxHealth = Mathf.Max(1, gameManager.maxHealth);
        int currentHealth = Mathf.Clamp(gameManager.health, 0, maxHealth);

        float healthPercent = (float)currentHealth / maxHealth;

        if (healthText != null)
        {
            healthText.text = currentHealth.ToString() + " / " + maxHealth.ToString();
        }

        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = healthPercent;

            if (healthPercent <= criticalHealthThreshold)
            {
                healthBarFill.color = healthCriticalColor;
            }
            else
            {
                healthBarFill.color = healthNormalColor;
            }
        }
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

        if (!CanPause() || pauseOverlay == null)
        {
            return;
        }

        isPaused = true;
        pauseOverlay.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ResumePause()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pauseOverlay != null)
        {
            pauseOverlay.SetActive(false);
        }
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

    // LAP DISPLAY

    private void UpdateLapText()
    {
        if (lapText == null)
        {
            return;
        }

        int currentLap = Mathf.Min(gameManager.completedLaps + 1, gameManager.requiredLaps);

        if (currentLap == previousDisplayedLap)
        {
            return;
        }

        previousDisplayedLap = currentLap;

        lapText.text = "LAP " + currentLap.ToString() + " / " + gameManager.requiredLaps.ToString();
    }

    // DAMAGE POPUP

    public void ShowDamagePopup(int damageAmount)
    {
        if (damagePopupText == null)
        {
            return;
        }

        CachePopup();

        if (popupCoroutine != null)
        {
            StopCoroutine(popupCoroutine);
        }

        popupRect.anchoredPosition = popupStartPosition;
        popupRect.localScale = popupNormalScale * popupStartScale;
        damagePopupText.color = popupColor;
        damagePopupText.text = "-" + damageAmount.ToString() + " DAMAGE!";
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

            Color c = popupColor;
            c.a = popupColor.a * (1f - t);
            damagePopupText.color = c;

            yield return null;
        }

        damagePopupText.gameObject.SetActive(false);
        popupCoroutine = null;
    }

    private void CachePopup()
    {
        if (popupRect != null || damagePopupText == null)
        {
            return;
        }

        popupRect = damagePopupText.rectTransform;
        popupStartPosition = popupRect.anchoredPosition;
        popupNormalScale = popupRect.localScale;
        popupColor = damagePopupText.color;
    }

    // TIMER DISPLAY

    private void UpdateTimerText()
    {
        if (tripTimer == null || timerText == null)
        {
            return;
        }

        if (gameManager.timeExpired)
        {
            timerText.text = "TIME'S UP!";
        }
        else if (!tripTimer.IsTimerStarted())
        {
            timerText.text = "Waiting for passengers (" + gameManager.passengersOnboard.ToString() + "/" + tripTimer.requiredPassengerCount.ToString() + ")";
        }
        else
        {
            float remainingTime = Mathf.Max(0f, tripTimer.GetTimeRemaining());

            int minutes = Mathf.FloorToInt(remainingTime / 60f);
            int seconds = Mathf.FloorToInt(remainingTime % 60f);

            timerText.text = minutes.ToString("00") + ":" + seconds.ToString("00");
        }
    }

    // LAP NOTIFICATION

    private void UpdateLapNotification()
    {
        if (lapNotificationText == null)
        {
            return;
        }

        if (gameManager.completedLaps > previousCompletedLaps)
        {
            previousCompletedLaps = gameManager.completedLaps;

            if (notificationCoroutine != null)
            {
                StopCoroutine(notificationCoroutine);
            }

            if (gameManager.completedLaps >= gameManager.requiredLaps)
            {
                lapNotificationText.text = "RACE COMPLETE!";
            }
            else
            {
                lapNotificationText.text = "LAP " + gameManager.completedLaps.ToString() + " COMPLETE!";
            }

            lapNotificationText.gameObject.SetActive(true);
            notificationCoroutine = StartCoroutine(HideLapNotification());
        }
    }

    private IEnumerator HideLapNotification()
    {
        yield return new WaitForSeconds(notificationDuration);

        lapNotificationText.gameObject.SetActive(false);
        notificationCoroutine = null;
    }

    // VICTORY SUMMARY

    private void UpdateMissionSummary()
    {
        if (summaryShown)
        {
            return;
        }

        if (gameplayHUD == null || victoryDimOverlay == null || victorySummaryPanel == null || missionSummaryText == null || tripTimer == null || gameManager.scoreManager == null)
        {
            return;
        }

        summaryShown = true;

        gameplayHUD.SetActive(false);
        victoryDimOverlay.SetActive(true);
        victorySummaryPanel.SetActive(true);

        float remainingTime = tripTimer.GetTimeRemaining();

        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);

        // Original multiline summary, kept for compatibility during testing.
        missionSummaryText.text = "LAPS COMPLETED: " + gameManager.completedLaps.ToString() + " / " + gameManager.requiredLaps.ToString() + "\n" + "PASSENGERS DELIVERED: " + gameManager.destinationCount.ToString() + " / " + gameManager.requiredLaps.ToString() + "\n" + "FINAL SCORE: " + gameManager.scoreManager.score.ToString() + "\n" + "TIME REMAINING: " + minutes.ToString("00") + ":" + seconds.ToString("00");

        // Three-column statistics.
        SetText(scoreValueText, gameManager.scoreManager.score.ToString());
        SetText(passengersValueText, gameManager.destinationCount.ToString() + " / " + gameManager.requiredLaps.ToString());
        SetText(timeValueText, minutes.ToString("00") + ":" + seconds.ToString("00"));
    }

    // GAME OVER STATISTICS

    private void UpdateGameOverStats()
    {
        if (gameManager.scoreManager == null)
        {
            return;
        }

        gameOverShown = true;

        int maxHealth = Mathf.Max(1, gameManager.maxHealth);
        int currentHealth = Mathf.Clamp(gameManager.health, 0, maxHealth);

        SetText(gameOverScoreValueText, gameManager.scoreManager.score.ToString());
        SetText(gameOverPassengersValueText, gameManager.destinationCount.ToString() + " / " + gameManager.requiredLaps.ToString());
        SetText(gameOverHealthValueText, currentHealth.ToString());

        // Only the timer can produce this reason. Health depletion keeps it hidden.
        if (failureReasonText != null)
        {
            failureReasonText.text = TimerExpiredReason;
            failureReasonText.gameObject.SetActive(gameManager.timeExpired);
        }
    }

    private void SetText(TextMeshProUGUI target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }
}
