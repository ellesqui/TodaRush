using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public int score = 0;
    public int highScore = 0;

    // The old shared key. Read only to migrate its value into Level 1. Never written to.
    private const string OldSharedHighScoreKey = "TODA_RUSH_HighScore_v1";

    // Per-level key, chosen in Awake from GameManager.levelNumber.
    // Stays null if GameManager is missing, so nothing is saved to the wrong level.
    private string highScoreKey = null;

    void Awake()
    {
        score = 0;
        highScore = 0;

        GameManager gameManager = GetComponent<GameManager>();
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

        highScore = PlayerPrefs.GetInt(highScoreKey, 0);
    }

    public void AddScore(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        score += amount;

        if (score > highScore)
        {
            highScore = score;

            // Save only to this level's key. Never to the old shared key.
            if (highScoreKey != null)
            {
                PlayerPrefs.SetInt(highScoreKey, highScore);
                PlayerPrefs.Save();
            }
        }
    }
}
