using UnityEngine;

// Owns two things:
// 1) Run score and per-level High Score (per run, saved per level).
// 2) The GLOBAL Tsuper profile: one TotalXP shared by every level.
//    Rank and rank progress are always derived from TotalXP (never stored as separate sources of truth).
public class ScoreManager : MonoBehaviour
{
    public int score = 0;

    // The old shared key. Read only to migrate its value into Level 1. Never written to.
    private const string OldSharedHighScoreKey = "TODA_RUSH_HighScore_v1";

    // Global profile key. This is the ONLY permanent progression value.
    public const string TotalXPKey = "PlayerTotalXP";

    // Rank thresholds (TotalXP lower bound of each rank) and titles
    public static readonly int[] RankThresholds = { 0, 5000, 15000, 30000, 50000, 75000 };
    public static readonly string[] RankTitles = { "Baguhang Tsuper", "Regular na Tsuper", "Beteranong Tsuper", "Bida ng Barangay", "Hari ng Kalsada", "TODA Legend" };

    // Per-level key, chosen in Awake from GameManager.levelNumber.
    private string highScoreKey = null;

    private GameManager gameManager;
    private bool finalScoreCommitted = false;   // one commit per run (score, high score, and XP)
    private int storedHighScore = 0;

    // Last committed run, used by the result panels to animate the rank bar once
    [HideInInspector] public bool hasPendingRankResult = false;
    [HideInInspector] public int runPreviousTotalXP = 0;
    [HideInInspector] public int runNewTotalXP = 0;
    [HideInInspector] public int runXPReward = 0;

    // High score for the UI. Reading it after the game ends locks in the final score first.
    public int highScore
    {
        get
        {
            CommitRunIfEnded();
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
        if (levelNumber == 1 && !PlayerPrefs.HasKey(highScoreKey) && PlayerPrefs.HasKey(OldSharedHighScoreKey))
        {
            PlayerPrefs.SetInt(highScoreKey, PlayerPrefs.GetInt(OldSharedHighScoreKey));
            PlayerPrefs.Save();
        }

        storedHighScore = PlayerPrefs.GetInt(highScoreKey, 0);
    }

    void Update()
    {
        CommitRunIfEnded();
    }

    // Points lost (collisions). Score never goes below 0.
    public void SubtractScore(int amount)
    {
        if (amount <= 0) return;
        score = Mathf.Max(0, score - amount);
    }

    // Points gained (tips, deliveries). The high score is NOT updated here, only at the end of the game.
    public void AddScore(int amount)
    {
        if (amount <= 0) return;
        score += amount;
    }

    // Runs once per run, when the run reaches Level Complete or Game Over.
    // Safe to call many times: only the first call after the run ends does anything.
    public void CommitRunIfEnded()
    {
        if (finalScoreCommitted || gameManager == null) return;
        if (!(gameManager.gameWon || gameManager.IsOver())) return;

        finalScoreCommitted = true;

        // High score for this level
        if (score > storedHighScore)
        {
            storedHighScore = score;
            if (highScoreKey != null)
            {
                PlayerPrefs.SetInt(highScoreKey, storedHighScore);
                PlayerPrefs.Save();
            }
        }

        // Global XP: the final run score (never below 0) is added to the one shared TotalXP
        runPreviousTotalXP = GetTotalXP();
        runXPReward = Mathf.Max(0, score);
        runNewTotalXP = runPreviousTotalXP + runXPReward;
        SetTotalXP(runNewTotalXP);
        hasPendingRankResult = true;
    }

    // ===== GLOBAL TSUPER PROFILE (shared by all levels) =====

    public static int GetTotalXP()
    {
        return Mathf.Max(0, PlayerPrefs.GetInt(TotalXPKey, 0));
    }

    public static void SetTotalXP(int xp)
    {
        PlayerPrefs.SetInt(TotalXPKey, Mathf.Max(0, xp));
        PlayerPrefs.Save();
    }

    // Rank index (0 = Baguhang Tsuper ... 5 = TODA Legend), from TotalXP only
    public static int GetRankIndex(int totalXP)
    {
        int rank = 0;
        for (int i = 0; i < RankThresholds.Length; i++)
        {
            if (totalXP >= RankThresholds[i]) rank = i;
        }
        return rank;
    }

    public static bool IsMaxRank(int rank)
    {
        return rank >= RankThresholds.Length - 1;
    }

    // Progress inside the current rank (0 to 1). Also returns the XP earned in this rank and the XP needed for the next rank.
    public static float RankProgress(int totalXP, out int currentRankXP, out int requiredXP)
    {
        int rank = GetRankIndex(totalXP);
        int lower = RankThresholds[rank];

        if (IsMaxRank(rank))
        {
            currentRankXP = totalXP - lower;
            requiredXP = 0;
            return 1f;
        }

        int upper = RankThresholds[rank + 1];
        requiredXP = upper - lower;
        currentRankXP = totalXP - lower;
        return Mathf.Clamp01((float)currentRankXP / requiredXP);
    }
}
