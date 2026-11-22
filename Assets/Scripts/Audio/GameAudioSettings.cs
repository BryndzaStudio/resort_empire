using System;
using UnityEngine;

// BRYN-18: Nastavenia zvuku. Drží dve voľby (zvuky a hudba stlmené / nie) a ukladá ich cez PlayerPrefs
// pri každej zmene. Zámerne nie je v GameState ani v resort_save.json.
// Názov nie je AudioSettings, lebo ten patrí Unity.
public static class GameAudioSettings
{
    public const string SoundMutedKey = "settings.soundMuted";
    public const string MusicMutedKey = "settings.musicMuted";

    // Vyvolá sa pri každej zmene niektorej voľby (aj za behu hry).
    public static event Action Changed;

    private static bool loaded;
    private static bool soundMuted;
    private static bool musicMuted;

    public static bool SoundMuted
    {
        get { EnsureLoaded(); return soundMuted; }
    }

    public static bool MusicMuted
    {
        get { EnsureLoaded(); return musicMuted; }
    }

    public static void SetSoundMuted(bool muted)
    {
        EnsureLoaded();
        if (soundMuted == muted)
        {
            return;
        }

        soundMuted = muted;
        Save(SoundMutedKey, muted);
        Changed?.Invoke();
    }

    public static void SetMusicMuted(bool muted)
    {
        EnsureLoaded();
        if (musicMuted == muted)
        {
            return;
        }

        musicMuted = muted;
        Save(MusicMutedKey, muted);
        Changed?.Invoke();
    }

    private static void EnsureLoaded()
    {
        if (loaded)
        {
            return;
        }

        soundMuted = PlayerPrefs.GetInt(SoundMutedKey, 0) != 0;
        musicMuted = PlayerPrefs.GetInt(MusicMutedKey, 0) != 0;
        loaded = true;
    }

    private static void Save(string key, bool value)
    {
        PlayerPrefs.SetInt(key, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    // Pri vypnutom domain reloade (Enter Play Mode Options) by ostala stará cache aj udalosť.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        loaded = false;
        Changed = null;
    }
}
