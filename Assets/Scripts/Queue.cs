using UnityEngine;
using System.Collections;

public class Queue : MonoBehaviour
{
    public PassengerPickup[] terminalPassengers;
    public float nextPassengerArrivalDelay = 2f;
    private int nextTerminalPassengerIndex = 0;

    // All passengers are spawned by GameManager now, so nothing is hidden here.
    void Start()
    {
    }

    public void ShowNextTerminalPassenger()
    {
        StartCoroutine(ShowNextTerminalPassengerAfterDelay());
    }

    private IEnumerator ShowNextTerminalPassengerAfterDelay()
    {
        yield return new WaitForSeconds(nextPassengerArrivalDelay);

        if (terminalPassengers == null || nextTerminalPassengerIndex >= terminalPassengers.Length)
        {
            yield break;
        }

        PassengerPickup nextPassenger = terminalPassengers[nextTerminalPassengerIndex];

        nextTerminalPassengerIndex++;

        if (nextPassenger != null)
        {
            nextPassenger.gameObject.SetActive(true);
        }
    }
}
