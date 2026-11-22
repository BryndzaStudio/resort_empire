using UnityEngine;

// BRYN-18: Daj tento komponent na AudioSource a vyber, či ide o zvuky alebo hudbu.
// Komponent nastaví AudioSource.mute podľa nastavení hráča, aj keď sa nastavenie zmení za behu.
// Nenastavuj mute na tom istom AudioSource sám, prepísalo by sa.
[RequireComponent(typeof(AudioSource))]
public class AudioChannel : MonoBehaviour
{
    public enum Channel
    {
        Sound,
        Music
    }

    [SerializeField] private Channel channel = Channel.Sound;

    private AudioSource source;

    public Channel Kind
    {
        get => channel;
        set
        {
            channel = value;
            Apply();
        }
    }

    private void OnEnable()
    {
        GameAudioSettings.Changed += Apply;
        Apply();
    }

    private void OnDisable()
    {
        GameAudioSettings.Changed -= Apply;
    }

    private void Apply()
    {
        if (source == null)
        {
            source = GetComponent<AudioSource>();
        }

        source.mute = channel == Channel.Music ? GameAudioSettings.MusicMuted : GameAudioSettings.SoundMuted;
    }
}
