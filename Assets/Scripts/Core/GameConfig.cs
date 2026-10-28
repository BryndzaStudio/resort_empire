
using UnityEngine;

// BRYN-9: Starting coins and energy are configurable parameters.
// Create the asset via: Assets > Create > Resort Empire > Game Config
[CreateAssetMenu(fileName = "GameConfig", menuName = "Resort Empire/Game Config")]
public class GameConfig : ScriptableObject
{
    [Header("Starting Values")]
    [Min(0)] public int startingCoins = 1000;
    [Min(0)] public int startingEnergy = 100;
}
