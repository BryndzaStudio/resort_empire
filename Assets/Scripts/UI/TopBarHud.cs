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

    private static readonly NumberFormatInfo SpaceFormat = new NumberFormatInfo
    {
        NumberGroupSeparator = " ",
        NumberGroupSizes = new[] { 3 }
    };

    private bool hasShown;
    private int lastCoins;
    private int lastEnergy;
    private int lastResortValue;

    private void Update()
    {
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
