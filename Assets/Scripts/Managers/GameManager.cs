using System.Linq;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [HideInInspector] public int destinationCount = 0;
    public int requiredLaps = 3;
    [HideInInspector] public int completedLaps = 0;
    public int damage = 10;
    [HideInInspector] public bool gameWon = false;
    [HideInInspector] public bool timeExpired = false;     // game over reason 1: the timer ran out (UIManager shows the time text)
    [HideInInspector] public bool scoreExpired = false;    // game over reason 2: no points at the Terminal
    public GameObject winText, gameOverText, restartButton;
    public UIManager uiManager;
    public PassengerPickup[] passengers = new PassengerPickup[5];
    [HideInInspector] public int passengersOnboard = 0;
    public ScoreManager scoreManager;
    public int levelNumber = 1; // 1 = MainMap, 2 = Level_02, 3 = Level_03

    [Header("Passenger spawn")]
    public GameObject[] passengerPrefabs;   // the 5 passenger models (random pick each game)

    [Header("Passenger speech")]
    public int hitsPerReaction = 3;   // every 3rd obstacle hit, the passenger reacts
    private int hitCount = 0;

    private const string NoPointsReason = "Walang naihatid na puntos sa terminal.";

    // Filipino name per model (the model's prefab name is the job)
    public static string NameForModel(string modelName)
    {
        switch (modelName)
        {
            case "JeepneyDriver": return "Mang Ben Reyes";
            case "Pulis":         return "PO1 Carlo Mendoza";
            case "OfficeWorker":  return "Ana dela Cruz";
            case "SIGA":          return "Lito Santos";
            case "Student":       return "Mika Villanueva";
            default:              return "Pasahero";
        }
    }

    // Passenger formula: Level 1 = 3, Level 2 = 4, Level 3 = 5
    public int CalculateRequiredPassengers()
    {
        return 3 + (levelNumber - 1);
    }

    void Awake()
    {
        // Set the victory count BEFORE any victory check runs.
        requiredLaps = CalculateRequiredPassengers();
        if (passengers.Length < requiredLaps)
        {
            System.Array.Resize(ref passengers, requiredLaps);
        }

        if (scoreManager == null) scoreManager = GetComponent<ScoreManager>();
    }

    void Start()
    {
        SpawnPassengers();
    }

    // Every passenger spot in the scene gets a random model this session.
    // Only the first passenger is shown; the next appears after each pickup.
    void SpawnPassengers()
    {
        if (passengerPrefabs == null || passengerPrefabs.Length == 0) return;

        PassengerPickup[] spots = FindObjectsByType<PassengerPickup>(FindObjectsInactive.Include)
            .OrderBy(p => p.name)
            .ToArray();

        for (int i = 0; i < spots.Length; i++)
        {
            PassengerPickup pax = spots[i];
            GameObject model = passengerPrefabs[Random.Range(0, passengerPrefabs.Length)];
            pax.SetupAtSpawn(model, NameForModel(model.name), pax.transform.position);
            pax.gameObject.SetActive(i == 0);
        }
    }

    // Called when the player touches the Terminal.
    // Win only after ALL passengers are delivered AND the player still has points.
    public bool CompleteLap()
    {
        TripTimer tripTimer = GetComponent<TripTimer>();

        if (tripTimer == null || !tripTimer.IsTimerStarted() || IsOver() || destinationCount < requiredLaps)
        {
            return false;
        }

        // Score is the player's performance. No points left at the Terminal = game over.
        if (scoreManager != null && scoreManager.score <= 0)
        {
            scoreExpired = true;   // stops the car
            ShowNoPointsGameOver();
            return false;
        }

        completedLaps = requiredLaps;
        gameWon = true;

        // Victory panel: WinText is a child of VictorySummaryPanel, so show the whole panel first
        winText.transform.parent.gameObject.SetActive(true);
        winText.SetActive(true);
        restartButton.SetActive(true);

        return true;
    }

    // True when the game is over for any reason (time ran out or no points at the Terminal)
    public bool IsOver()
    {
        return timeExpired || scoreExpired;
    }

    // Game over for "no points at the Terminal": shows the game over panel with its own reason.
    // The timer text is not touched, so the remaining time stays visible.
    void ShowNoPointsGameOver()
    {
        gameOverText.transform.parent.gameObject.SetActive(true);
        gameOverText.SetActive(true);
        restartButton.SetActive(true);

        if (uiManager != null)
        {
            SetText(uiManager.gameOverScoreValueText, (scoreManager != null ? scoreManager.score : 0).ToString());
            SetText(uiManager.gameOverPassengersValueText, destinationCount + " / " + requiredLaps);
            uiManager.SetGameOverTime();

            if (uiManager.failureReasonText != null)
            {
                uiManager.failureReasonText.text = NoPointsReason;
                uiManager.failureReasonText.gameObject.SetActive(true);
            }
        }
    }

    private void SetText(TextMeshProUGUI target, string value)
    {
        if (target != null) target.text = value;
    }

    public void TakeDamage()
    {
        TakeDamage(damage);
    }

    public void TakeDamage(int amount)
    {
        // Points lost (collisions, red light). Ignored after win or game over.
        if (gameWon || IsOver()) return;

        if (scoreManager != null) scoreManager.SubtractScore(amount);

        if (uiManager != null) uiManager.ShowDamagePopup(amount);

        // Hit sound
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(AudioManager.Instance.hitSound);

        // Every 3rd hit, a random boarded passenger reacts
        hitCount++;
        if (uiManager != null && hitCount % hitsPerReaction == 0) uiManager.PlayerHitReaction();
    }

    public bool DeliverPassenger(Destination destination)
    {
        TripTimer tripTimer = GetComponent<TripTimer>();

        if (tripTimer == null || !tripTimer.IsTimerStarted()) return false;

        foreach (PassengerPickup passenger in passengers)
        {
            if (passenger != null && passenger.hasBoarded && passenger.targetDestination == destination)
            {
                passengersOnboard--;
                passenger.targetDestination = null;

                // Walk the passenger out, then hide them (see PassengerPickup.DropRoutine)
                StartCoroutine(passenger.DropRoutine());
                return true;
            }
        }

        return false;
    }
}
