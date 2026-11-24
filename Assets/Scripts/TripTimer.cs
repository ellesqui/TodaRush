using UnityEngine;

public class TripTimer : MonoBehaviour
{
    public float tripTimeLimit = 120f;
    public bool holdUntilGo = false;   // true while the 3-2-1-GO countdown plays (timer does not count yet)
    private float timeRemaining;
    private bool timerStarted = false;
    private GameManager gameManager;

    void Awake()
    {
        gameManager = GetComponent<GameManager>();
    }

    void Start()
    {
        timeRemaining = tripTimeLimit;
    }

    void Update()
    {
        if (gameManager == null)
        {
            return;
        }

        // Timer starts only when ALL passengers for this level are onboard (Level 1 = 3)
        // Level 3: passengers come one at a time (chain), so the timer starts on the first pickup
        int neededToStart = gameManager.levelNumber == 3 ? 1 : gameManager.CalculateRequiredPassengers();
        if (!timerStarted && !holdUntilGo && gameManager.passengersOnboard >= neededToStart)
        {
            timerStarted = true;
        }

        if (timerStarted && !holdUntilGo && !gameManager.gameWon && !gameManager.timeExpired)
        {
            timeRemaining -= Time.deltaTime;

            if (timeRemaining <= 0f)
            {
                timeRemaining = 0f;
                gameManager.timeExpired = true;

                if (gameManager.gameOverText != null)
                {
                    // GameOverText is a child of GameOverPanel, so show the whole panel first
                    gameManager.gameOverText.transform.parent.gameObject.SetActive(true);
                    gameManager.gameOverText.SetActive(true);
                }

                if (gameManager.restartButton != null)
                {
                    gameManager.restartButton.SetActive(true);
                }

                enabled = false;
                return;
            }
        }
    }

    public bool IsTimerStarted()
    {
        return timerStarted;
    }

    public void AddTime(float seconds)
    {
        timeRemaining += seconds;
    }

    public float GetTimeRemaining()
    {
        return timeRemaining;
    }
}
