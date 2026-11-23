using TMPro;
using UnityEngine;

public class PassengerDialogue : MonoBehaviour
{
    public GameObject requestBubbleCanvas;
    public TMP_Text requestText;       // name line
    public TMP_Text jobText;           // passenger job line (optional)
    public Camera mainCamera;
    private PassengerPickup passengerPickup;

    void Awake()
    {
        passengerPickup = GetComponent<PassengerPickup>();

        // Level 1: no passenger request dialogue
        GameManager gameManager = FindAnyObjectByType<GameManager>();
        if (gameManager != null && gameManager.levelNumber == 1)
        {
            requestBubbleCanvas.SetActive(false);
            enabled = false;
        }
    }

    void Update()
    {
        bool showDialogue = !passengerPickup.hasBoarded && !passengerPickup.IsApproaching && passengerPickup.targetDestination != null;
        requestBubbleCanvas.SetActive(showDialogue);

        if (!showDialogue) return;

        // Name on the name line, job on the job line, destination request below
        requestText.text = passengerPickup.displayName + "\nPahatid po sa " + passengerPickup.targetDestination.destinationName + "!";
        if (jobText != null) jobText.text = passengerPickup.jobTitle;
        requestBubbleCanvas.transform.LookAt(mainCamera.transform);
        requestBubbleCanvas.transform.Rotate(0f, 180f, 0f);
    }
}
