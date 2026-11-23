using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public int score = 0;

    // The old shared key. Read only to migrate its value into Level 1. Never written to.
    private const string OldSharedHighScoreKey = "TODA_RUSH_HighScore_v1";

    // Per-level key, chosen in Awake from GameManager.levelNumber.
    // Stays null if GameManager is missing, so nothing is saved to the wrong level.
    private string highScoreKey = null;

    private GameManager gameManager;
    private bool finalScoreCommitted = false;

    // Stored best final score for this level. Updated only at the end of a game.
    private int storedHighScore = 0;

    // High score for the UI. Reading it after the game ends locks in the FINAL score first,
    // so the Level Complete / Game Over screens always show the correct value.
    public int highScore
    {
        get
        {
            TryCommitFinalScore();
            return storedHighScore;
        }
        set
        {
            storedHighScore = value;
        }
    }

    void Awake()
    {
        score = 0;

        gameManager = GetComponent<GameManager>();
        if (gameManager == null)
        {
            Debug.LogError("ScoreManager: No GameManager on this GameObject. High Score will not load or save.");
            return;
        }

        int levelNumber = gameManager.levelNumber;
        highScoreKey = "TODA_RUSH_HighScore_Level_" + levelNumber;

        // One-time migration: Level 1 inherits the old shared record.
        // Other levels never copy it, and an existing per-level record is never overwritten.
        if (levelNumber == 1 && !PlayerPrefs.HasKey(highScoreKey) && PlayerPrefs.HasKey(OldSharedHighScoreKey))
        {
            PlayerPrefs.SetInt(highScoreKey, PlayerPrefs.GetInt(OldSharedHighScoreKey));
            PlayerPrefs.Save();
        }

        storedHighScore = PlayerPrefs.GetInt(highScoreKey, 0);
    }

    void Update()
    {
        TryCommitFinalScore();
    }

    // Points lost (collisions). Score never goes below 0.
    public void SubtractScore(int amount)
    {
        if (amount <= 0) return;
        score = Mathf.Max(0, score - amount);
    }

    // Points gained (tips). The high score is NOT updated here, only at the end of the game.
    public void AddScore(int amount)
    {
        if (amount <= 0) return;
        score += amount;
    }

    // Once the game has ended (win, time up, or no points), save the final score if it beats the record.
    // Runs only once per game.
    void TryCommitFinalScore()
    {
        if (finalScoreCommitted || gameManager == null) return;
        if (!(gameManager.gameWon || gameManager.IsOver())) return;

        finalScoreCommitted = true;

        if (score > storedHighScore)
        {
            storedHighScore = score;

            // Save only to this level's key. Never to the old shared key.
            if (highScoreKey != null)
            {
                PlayerPrefs.SetInt(highScoreKey, storedHighScore);
                PlayerPrefs.Save();
            }
        }
    }
}
