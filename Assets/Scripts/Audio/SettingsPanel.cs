using TMPro;
using UnityEngine;
using UnityEngine.UI;

// BRYN-18: Panel Nastavenia (prefab SettingsPanel). Dva nezávislé prepínače: Zvuky a Hudba.
// Hra pod panelom beží ďalej (nemení sa Time.timeScale). Blocker za panelom zachytáva kliky,
// takže sa nedá ťuknúť na budovy; ťuknutie naň panel zatvorí.
public class SettingsPanel : MonoBehaviour
{
    [SerializeField] private Button blockerButton;
    [SerializeField] private Button closeButton;

    [Header("Zvuky")]
    [SerializeField] private Button soundButton;
    [SerializeField] private Image soundStateImage;
    [SerializeField] private TMP_Text soundStateText;

    [Header("Hudba")]
    [SerializeField] private Button musicButton;
    [SerializeField] private Image musicStateImage;
    [SerializeField] private TMP_Text musicStateText;

    private static readonly Color OnColor = Color.white;
    private static readonly Color OffColor = new Color(0.45f, 0.45f, 0.45f, 0.5f);

    public bool IsOpen => gameObject.activeSelf;

    private void Awake()
    {
        blockerButton.onClick.AddListener(Close);
        closeButton.onClick.AddListener(Close);
        soundButton.onClick.AddListener(ToggleSound);
        musicButton.onClick.AddListener(ToggleMusic);
    }

    private void OnEnable()
    {
        GameAudioSettings.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        GameAudioSettings.Changed -= Refresh;
    }

    public void Open()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    private static void ToggleSound()
    {
        GameAudioSettings.SetSoundMuted(!GameAudioSettings.SoundMuted);
    }

    private static void ToggleMusic()
    {
        GameAudioSettings.SetMusicMuted(!GameAudioSettings.MusicMuted);
    }

    private void Refresh()
    {
        ShowState(soundStateImage, soundStateText, !GameAudioSettings.SoundMuted);
        ShowState(musicStateImage, musicStateText, !GameAudioSettings.MusicMuted);
    }

    private static void ShowState(Image image, TMP_Text text, bool enabled)
    {
        image.color = enabled ? OnColor : OffColor;
        text.text = enabled ? "Zap" : "Vyp";
    }
}
