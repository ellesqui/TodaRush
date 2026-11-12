using UnityEngine;
[RequireComponent(typeof(AudioSource))]
public sealed class SettingsAudioChannel : MonoBehaviour
{
    public enum Channel { Music, SFX }
    [SerializeField] Channel channel=Channel.SFX;
    AudioSource source;
    void OnEnable(){source=GetComponent<AudioSource>();GameSettingsManager.Ensure().Register(source,channel==Channel.Music);}
    void OnDisable(){if(GameSettingsManager.Instance)GameSettingsManager.Instance.Unregister(source);}
}
