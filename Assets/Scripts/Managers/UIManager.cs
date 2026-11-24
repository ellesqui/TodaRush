using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI scoreText, highScoreText, timerText, damagePopupText, speedText;
    public TextMeshProUGUI scoreValueText, passengersValueText, timeValueText;
    public TextMeshProUGUI gameOverScoreValueText, gameOverPassengersValueText, gameOverTimeValueText, failureReasonText;

    // NEW: the HIGH SCORE number on the Level Complete panel (HighScoreValue)
    public TextMeshProUGUI levelCompleteHighScoreText;

    public PassengerProgressWidget passengerProgress;
    public GameObject gameplayHUD, victoryDimOverlay, victorySummaryPanel, pauseOverlay, successNotification;
    public GameManager gameManager;
    public TripTimer tripTimer;
    public PlayerController playerController;
    public float popupDuration = 1.3f, popupRiseDistance = 50f, popupStartScale = 1.2f;

    [Header("Full Map")]
    public Camera fullMapCamera;
    public GameObject fullMapPanel;

    [Header("Passenger Dialogue HUD")]
    public GameObject passengerDialogueHUD;
    public UnityEngine.UI.Image damageFlash;
    private float flashAlpha;
    public TextMeshProUGUI passengerNameText;   // name label only
    public TextMeshProUGUI passengerJobText;    // "passenger job" label (job = model prefab name)
    public TextMeshProUGUI speechText;
    public TextMeshProUGUI objectiveMainText;   // OBJECTIVE panel main line (shows the mission while the timer runs)
    private const string MissionText = "DELIVER ALL · BEAT THE CLOCK";
    public TextMeshProUGUI countdownText;      // big 3 / 2 / 1 / GO text on the HUD
    public AudioClip countdownClip;            // Mario Start (plays with the countdown)
    public AudioClip deliveredClip;

    [Header("Passenger Sound Effects (random per state)")]
    public AudioClip[] boardingClips;
    public AudioClip[] dropOffClips;
    public AudioClip[] hitReactionClips;
    public AudioClip[] angryClips;
    public AudioClip[] fallOutClips;            // Delivered sound: plays with the DELIVERED notification
    private AudioSource sfx;
    private float baseObjectiveFontSize;
    private bool countdownStarted;
    public float speechSeconds = 4f;   // how long the passenger speech stays on screen

    [Header("Passenger Speech Lines (default for now)")]
    public string[] boardingLines = { "Salamat po, boss!", "Sige po, bilisan natin!", "Tara na po!" };
    public string[] reactionLines = { "Aray! Dahan-dahan naman, boss!", "Ingat po tayo!", "Diyos ko po, ingat!" };
    public string[] dropLines = { "Salamat po, boss!", "Ingat po kayo!" };

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
        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        if (objectiveMainText != null) baseObjectiveFontSize = objectiveMainText.fontSize;
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
        UpdateObjectiveText();

        if (gameManager.gameWon)
        {
            UpdateMissionSummary();
            return;
        }

        if (!gameOverShown && gameManager.timeExpired) UpdateGameOverStats();

        scoreText.text = "Score: " + gameManager.scoreManager.score;
        highScoreText.text = "High Score: " + gameManager.scoreManager.highScore;
        UpdateTimerText();
        speedText.text = playerController.currentSpeed.ToString("0");
    }

    // PASSENGER SPEECH (shows for a few seconds, then hides)
    public void PassengerBoarded(PassengerPickup passenger)
    {
        ShowPassengerSpeech(passenger, PickLine(boardingLines));
        PlayRandomClip(boardingClips);
    }

    public void PassengerDropped(PassengerPickup passenger)
    {
        ShowPassengerSpeech(passenger, PickLine(dropLines));
        PlayRandomClip(dropOffClips);
    }

    // Used by PassengerPickup for angry / falling-out lines (any line the passenger chooses)
    public void PassengerSays(PassengerPickup passenger, string line)
    {
        ShowPassengerSpeech(passenger, line);
    }

    // Called every few hits (see GameManager). A random boarded passenger reacts.
    public void PlayerHitReaction()
    {
        if (gameManager == null) return;

        List<PassengerPickup> boarded = new List<PassengerPickup>();
        foreach (PassengerPickup passenger in gameManager.passengers)
        {
            if (passenger != null && passenger.hasBoarded && passenger.targetDestination != null)
                boarded.Add(passenger);
        }

        if (boarded.Count == 0) return;

        PassengerPickup speaker = boarded[Random.Range(0, boarded.Count)];
        ShowPassengerSpeech(speaker, PickLine(reactionLines));
        PlayRandomClip(hitReactionClips);
    }

    private void ShowPassengerSpeech(PassengerPickup passenger, string line)
    {
        if (passengerDialogueHUD == null || speechText == null) return;

        // Name on the name label, job on the passenger job label
        if (passengerNameText != null) passengerNameText.text = passenger.displayName;
        if (passengerJobText != null) passengerJobText.text = passenger.jobTitle;

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

    // Angry and fall-out speech, each with its own sound list
    public void PassengerAngry(PassengerPickup passenger, string line)
    {
        ShowPassengerSpeech(passenger, line);
        PlayRandomClip(angryClips);
    }

    public void PassengerFallOut(PassengerPickup passenger, string line)
    {
        ShowPassengerSpeech(passenger, line);
        PlayRandomClip(fallOutClips);
    }

    // Picks one random clip from the list and plays it
    private void PlayRandomClip(AudioClip[] clips)
    {
        if (sfx == null || clips == null || clips.Length == 0) return;
        sfx.PlayOneShot(clips[Random.Range(0, clips.Length)]);
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
        // Delivery sound plays together with the DELIVERED notification
        if (message.StartsWith("DELIVERED") && deliveredClip != null && sfx != null) sfx.PlayOneShot(deliveredClip);
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

    // PAUSE SYSTEM
    private bool CanPause()
    {
        return gameManager != null && !gameManager.gameWon && !gameManager.timeExpired;
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
        damagePopupText.text = "-" + damageAmount + " POINTS";
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

    // Big 3 / 2 / 1 / GO countdown, fading out one number at a time (Mario Start plays with it)
    IEnumerator CountdownRoutine()
    {
        countdownText.gameObject.SetActive(true);
        if (countdownClip != null) sfx.PlayOneShot(countdownClip);
        string[] steps = { "3", "2", "1", "GO" };
        foreach (string s in steps)
        {
            countdownText.text = s;
            float startTime = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - startTime < 1f)
            {
                float t = Time.realtimeSinceStartup - startTime;
                countdownText.color = new Color(1f, 0.55f, 0f, 1f - t);
                yield return null;
            }
        }
        countdownText.gameObject.SetActive(false);
        if (tripTimer != null) tripTimer.holdUntilGo = false;   // timer counts down after GO
        if (playerController != null) playerController.canDrive = true;   // tricycle can move after GO
    }

    // Called by PassengerPickup after the last passenger boards: starts 3-2-1-GO once
    public void StartCountdown()
    {
        if (countdownStarted || countdownText == null) return;
        countdownStarted = true;
        if (tripTimer != null) tripTimer.holdUntilGo = true;   // timer waits until GO
        StartCoroutine(CountdownRoutine());
    }

    // Objective text flow: WAITING -> DELIVER ALL -> RETURN TO TERMINAL -> MISSION COMPLETE
    private void UpdateObjectiveText()
    {
        if (objectiveMainText == null || tripTimer == null) return;

        string wanted;
        if (gameManager.gameWon) wanted = "MISSION COMPLETE";
        else if ((gameManager.destinationCount >= gameManager.requiredLaps)) wanted = "RETURN TO TERMINAL";
        else if (tripTimer.IsTimerStarted()) wanted = MissionText;
        else wanted = "WAITING FOR PASSENGERS";

        if (objectiveMainText.text != wanted)
        {
            objectiveMainText.text = wanted;
        }

        // Same font size as the WAITING FOR PASSENGERS text
        objectiveMainText.enableAutoSizing = false;
        objectiveMainText.fontSize = baseObjectiveFontSize;
    }

    private void UpdateTimerText()
    {
        if (tripTimer == null || timerText == null) return;

        if (gameManager.timeExpired) timerText.text = "TIME'S UP!";
        else timerText.text = FormatTime(Mathf.Max(0f, tripTimer.GetTimeRemaining()));
    }

    // VICTORY SUMMARY (Level Complete panel)
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

        // HIGH SCORE: read from PlayerPrefs (saved by ScoreManager for this level)
        SetText(levelCompleteHighScoreText, gameManager.scoreManager.highScore.ToString());
    }

    // GAME OVER STATISTICS
    private void UpdateGameOverStats()
    {
        if (gameManager.scoreManager == null) return;

        gameOverShown = true;

        SetText(gameOverScoreValueText, gameManager.scoreManager.score.ToString());
        SetText(gameOverPassengersValueText, gameManager.destinationCount + " / " + gameManager.requiredLaps);
        SetGameOverTime();

        // Only the timer can produce this reason.
        if (failureReasonText != null)
        {
            failureReasonText.text = TimerExpiredReason;
            failureReasonText.gameObject.SetActive(gameManager.timeExpired);
        }
    }

    // TIME LEFT on the game over panel (same value as the win panel)
    public void SetGameOverTime()
    {
        if (tripTimer == null) return;
        SetText(gameOverTimeValueText, FormatTime(Mathf.Max(0f, tripTimer.GetTimeRemaining())));
    }

    private void SetText(TextMeshProUGUI target, string value)
    {
        if (target != null) target.text = value;
    }
}
