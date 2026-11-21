using System.Globalization;
using TMPro;
using UnityEngine;

// BRYN-14: top bar with coins, energy and resort value.
// GameState has no change events yet, so the values are compared in Update
// and a text is redrawn only when its value changed.
public class TopBarHud : MonoBehaviour
{
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text energyText;
    [SerializeField] private TMP_Text resortValueText;

    // BRYN-16: forecast of the next wave, shown only while no wave is running.
    [SerializeField] private GameObject forecastRoot;
    [SerializeField] private TMP_Text forecastText;

    private static readonly NumberFormatInfo SpaceFormat = new NumberFormatInfo
    {
        NumberGroupSeparator = " ",
        NumberGroupSizes = new[] { 3 }
    };

    // "Ďalšia vlna: " did not fit the 300x80 counter background legibly, so the label is short.
    private const string ForecastLabel = "Vlna: ";

    private bool hasShown;
    private int lastCoins;
    private int lastEnergy;
    private int lastResortValue;

    private bool forecastKnown;
    private bool lastForecastVisible;
    private int lastForecastGuests = -1;

    private void Update()
    {
        // The forecast depends on WaveManager only, not on GameState.
        UpdateForecast();

        // GameManager sets Instance in Awake; wait until the state exists.
        GameManager manager = GameManager.Instance;
        if (manager == null || manager.CurrentState == null)
        {
            return;
        }

        GameState state = manager.CurrentState;

        if (!hasShown || state.coins != lastCoins)
        {
            lastCoins = state.coins;
            SetText(coinsText, state.coins);
        }

        if (!hasShown || state.energy != lastEnergy)
        {
            lastEnergy = state.energy;
            SetText(energyText, state.energy);
        }

        if (!hasShown || state.resortValue != lastResortValue)
        {
            lastResortValue = state.resortValue;
            SetText(resortValueText, state.resortValue);
        }

        hasShown = true;
    }

    // BRYN-16: "Ďalšia vlna: N hostí" while no wave runs; hidden during a wave
    // or when there is no WaveManager in the scene. Touches the UI only on change.
    private void UpdateForecast()
    {
        WaveManager wave = WaveManager.Instance;
        bool visible = wave != null && !wave.IsWaveRunning;

        if (forecastRoot != null && (!forecastKnown || visible != lastForecastVisible))
        {
            forecastRoot.SetActive(visible);
        }

        forecastKnown = true;
        lastForecastVisible = visible;

        if (!visible)
        {
            return;
        }

        int guests = wave.GuestsPerWave;
        if (guests != lastForecastGuests)
        {
            lastForecastGuests = guests;
            if (forecastText != null)
            {
                forecastText.text = ForecastLabel + Format(guests) + " hostí";
            }
        }
    }

    // 1250 -> "1 250"
    public static string Format(int value)
    {
        return value.ToString("N0", SpaceFormat);
    }

    private static void SetText(TMP_Text target, int value)
    {
        if (target != null)
        {
            target.text = Format(value);
        }
    }
}
