
using UnityEngine;

// BRYN-9: Starting coins and energy are configurable parameters.
// BRYN-80, BRYN-81, BRYN-84: nastavenia hodnotenia vlny (spokojnosť hostí, hviezdičky, bottleneck).
// Create the asset via: Assets > Create > Resort Empire > Game Config
[CreateAssetMenu(fileName = "GameConfig", menuName = "Resort Empire/Game Config")]
public class GameConfig : ScriptableObject
{
    [Header("Starting Values")]
    [Min(0)] public int startingCoins = 1000;
    [Min(0)] public int startingEnergy = 100;

    // BRYN-81: spokojnosť hosťa podľa čakania od príchodu na ostrov po začiatok obsluhy.
    [Header("Spokojnosť hostí (BRYN-81)")]
    [Tooltip("Hosť, ktorý čakal najviac tento čas (s), je úplne spokojný (100 %).")]
    [Min(0)] public float satisfiedWaitSeconds = 8f;
    [Tooltip("Hosť, ktorý čakal aspoň tento čas (s), je úplne nespokojný (0 %). Medzi tým spokojnosť klesá rovnomerne.")]
    [Min(0)] public float unhappyWaitSeconds = 40f;

    // BRYN-80: hodnotenie vlny podľa priemernej spokojnosti hostí. 1 hviezdička je vždy minimum.
    [Header("Hodnotenie vlny – prahy spokojnosti v % (BRYN-80)")]
    [Range(0f, 100f)] public float minSatisfactionFor2Stars = 20f;
    [Range(0f, 100f)] public float minSatisfactionFor3Stars = 40f;
    [Range(0f, 100f)] public float minSatisfactionFor4Stars = 60f;
    [Range(0f, 100f)] public float minSatisfactionFor5Stars = 80f;

    // BRYN-83/84: budova je bottleneck, keď v nej hostia čakali priemerne dlhšie ako tento čas.
    [Header("Bottleneck (BRYN-83, BRYN-84)")]
    [Tooltip("Budova je bottleneck, ak hostia, ktorí do nej išli, čakali priemerne dlhšie ako tento čas (s).")]
    [Min(0)] public float bottleneckWaitSeconds = 15f;


    // BRYN-71: Energy awarded after completing a wave.
    [Header("Wave Energy Rewards (BRYN-71)")]
    [Tooltip("Energy awarded per star after a completed wave.")]
    [Min(0)]
    public int energyPerStar = 5;

}
