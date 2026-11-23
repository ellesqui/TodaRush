using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Press F or click the horn button to honk.
// The horn sound can be played anywhere, once every 30 seconds.
// Pedestrians only react when the Player is inside the lane's detection zone.
public class HornController : MonoBehaviour
{
    public AudioClip hornClip;
    public float cooldownSeconds = 30f;

    // Optional UI: the horn button and the text on it (shows F, then the countdown)
    public Button hornButton;
    public TMP_Text keyText;

    private PedestrianSpawner spawner;
    private float cooldownTimer;

    void Start()
    {
        spawner = FindAnyObjectByType<PedestrianSpawner>();

        if (hornButton != null)
            hornButton.onClick.AddListener(PressHorn);
    }

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.F))
            PressHorn();

        UpdateButton();
    }

    // Honk if the cooldown is over
    public void PressHorn()
    {
        if (cooldownTimer > 0f) return;

        AudioManager.Instance.PlaySfx(hornClip);
        cooldownTimer = cooldownSeconds;

        // Only the lane reacts, and only when the Player is inside the zone
        if (spawner != null && spawner.IsPlayerInZone())
            spawner.OnHorn();
    }

    // Show F when ready, or the countdown when waiting
    void UpdateButton()
    {
        bool ready = cooldownTimer <= 0f;

        if (hornButton != null)
            hornButton.interactable = ready;

        if (keyText != null)
            keyText.text = ready ? "F" : Mathf.CeilToInt(cooldownTimer).ToString();
    }
}
