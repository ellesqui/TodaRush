using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class PassengerPickup : MonoBehaviour
{
    public UnityEvent Passenger;
    public Destination targetDestination;
    public GameManager gameManager;
    private bool isPickedUp = false;
    public bool hasBoarded = false;
    public Transform boardingPoint;
    public float boardingSpeed = 3f;
    private bool isApproaching = false;
    public bool IsApproaching
    {
        get { return isApproaching; }
    }
    public float pickupDistance = 6f;
    private Transform playerTransform;

    [Header("Drop-off walk")]
    public float walkSeconds = 3f;   // how long the passenger walks after arriving
    public float walkSpeed = 1.5f;   // how fast the passenger walks

    void Update()
    {
        if (!isPickedUp && !isApproaching && gameManager != null && boardingPoint != null)
        {
            if (playerTransform == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerTransform = player.transform;
                }
            }

            if (playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) <= pickupDistance)
            {
                for (int i = 0; i < gameManager.passengers.Length; i++)
                {
                    if (gameManager.passengers[i] == null)
                    {
                        isPickedUp = true;
                        isApproaching = true;
                        gameManager.passengers[i] = this;
                        break;
                    }
                }
            }
        }

        if (!isApproaching || boardingPoint == null || gameManager == null)
        {
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, boardingPoint.position, boardingSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, boardingPoint.position) <= 0.1f)
        {
            isApproaching = false;
            hasBoarded = true;
            gameManager.passengersOnboard++;
            Passenger.Invoke();

            // Boarding speech
            if (gameManager.uiManager != null) gameManager.uiManager.PassengerBoarded(this);
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // Called when the passenger arrives at the destination.
    // Starts on the GameManager, because this object is hidden at this point.
    public IEnumerator DropRoutine()
    {
        // Appear next to the tricycle, on the right side
        transform.position = boardingPoint.position;
        gameObject.SetActive(true);

        // Drop-off speech
        if (gameManager.uiManager != null) gameManager.uiManager.PassengerDropped(this);

        Vector3 walkDirection = boardingPoint.parent.right;

        // Walk for a few seconds
        for (float t = 0f; t < walkSeconds; t += Time.deltaTime)
        {
            transform.position += walkDirection * walkSpeed * Time.deltaTime;
            yield return null;
        }

        // Disappear, then show the success message
        gameObject.SetActive(false);
        FindFirstObjectByType<UIManager>()?.ShowSuccess("DELIVERED!");
    }
}
