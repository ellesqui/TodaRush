using UnityEngine;

// Plays a sound every time this panel is shown (Game Over / Level Complete).
public class PanelSound : MonoBehaviour
{
    public AudioClip clip;
    private AudioSource source;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        if (source == null) source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
    }

    void OnEnable()
    {
        if (clip != null) source.PlayOneShot(clip);
    }
}
