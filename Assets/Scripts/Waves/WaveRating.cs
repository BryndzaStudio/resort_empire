using System.Collections.Generic;
using UnityEngine;

// BRYN-81: Spokojnosť hosťa sa určuje z jeho čakania – od príchodu na ostrov (koniec móla)
//          po začiatok obsluhy v budove. Čas obsluhy sa nepočíta.
//          Čakanie do GameConfig.satisfiedWaitSeconds = 100 %, od unhappyWaitSeconds = 0 %, medzi tým rovnomerne.
// BRYN-80: Ukončená vlna sa vyhodnotí podľa priemernej spokojnosti hostí na 1 – 5 hviezdičiek.
//          Prahy sú konfigurovateľné v GameConfig (minSatisfactionFor2Stars ... For5Stars).
// BRYN-83/84: Pre každú aktívnu budovu sa zistí priemerné čakanie jej hostí. Budova je bottleneck,
//          ak je dlhšie ako GameConfig.bottleneckWaitSeconds.
// Statická trieda, do scény sa nepridáva. WaveManager volá BeginWave() pri štarte vlny
// a Complete(result) tesne pred WaveEvents.WaveFinished.
public static class WaveRating
{
    private sealed class BuildingStats
    {
        public int guests;
        public float totalWait;
    }

    private static readonly List<float> guestSatisfactions = new List<float>();
    private static readonly Dictionary<BuildingType, BuildingStats> buildingStats = new Dictionary<BuildingType, BuildingStats>();
    private static GameConfig defaults;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        // Udalosti sa nulujú pri štarte Play (SubsystemRegistration), preto sa prihlásime až po ňom.
        GuestManager.GuestServiceStarted -= OnGuestServiceStarted;
        GuestManager.GuestServiceStarted += OnGuestServiceStarted;
        Clear();
    }

    // Nastavenia z GameConfig; bez priradeného configu sa použijú predvolené hodnoty z GameConfig.cs.
    private static GameConfig Config
    {
        get
        {
            GameManager manager = GameManager.Instance;
            if (manager != null && manager.Config != null)
            {
                return manager.Config;
            }

            if (defaults == null)
            {
                defaults = ScriptableObject.CreateInstance<GameConfig>();
            }

            return defaults;
        }
    }

    public static void BeginWave()
    {
        Clear();
    }

    private static void Clear()
    {
        guestSatisfactions.Clear();
        buildingStats.Clear();
    }

    private static void OnGuestServiceStarted(GuestState guest, BuildingState building, float waitSeconds)
    {
        GameConfig config = Config;
        float satisfaction = GuestSatisfaction(waitSeconds, config.satisfiedWaitSeconds, config.unhappyWaitSeconds);
        guestSatisfactions.Add(satisfaction);

        if (!buildingStats.TryGetValue(building.type, out BuildingStats stats))
        {
            stats = new BuildingStats();
            buildingStats.Add(building.type, stats);
        }

        stats.guests++;
        stats.totalWait += waitSeconds;

        Debug.Log($"BRYN-81: hosť {guest.id} ({building.type}) čakal {waitSeconds:0.0} s, spokojnosť {satisfaction:0} %.");
    }

    // BRYN-81: spokojnosť jedného hosťa v % podľa čakania.
    public static float GuestSatisfaction(float waitSeconds, float satisfiedWait, float unhappyWait)
    {
        if (waitSeconds <= satisfiedWait)
        {
            return 100f;
        }

        if (waitSeconds >= unhappyWait || unhappyWait <= satisfiedWait)
        {
            return 0f;
        }

        return 100f * (unhappyWait - waitSeconds) / (unhappyWait - satisfiedWait);
    }

    // BRYN-80: počet hviezdičiek (1 – 5) podľa priemernej spokojnosti v %.
    public static int Stars(float satisfaction, GameConfig config)
    {
        int stars = 1;
        if (satisfaction >= config.minSatisfactionFor2Stars) stars = 2;
        if (satisfaction >= config.minSatisfactionFor3Stars) stars = 3;
        if (satisfaction >= config.minSatisfactionFor4Stars) stars = 4;
        if (satisfaction >= config.minSatisfactionFor5Stars) stars = 5;
        return stars;
    }

    // Doplní do výsledku vlny spokojnosť, hviezdičky a výsledky budov.
    public static void Complete(WaveResult result)
    {
        GameConfig config = Config;

        float sum = 0f;
        foreach (float value in guestSatisfactions)
        {
            sum += value;
        }

        result.guestsRated = guestSatisfactions.Count;
        result.satisfaction = guestSatisfactions.Count > 0 ? sum / guestSatisfactions.Count : 0f;
        result.stars = Stars(result.satisfaction, config);

        result.buildings.Clear();
        GameManager manager = GameManager.Instance;
        if (manager != null && manager.CurrentState != null)
        {
            foreach (BuildingState building in manager.CurrentState.buildings)
            {
                if (!building.isActive)
                {
                    continue;
                }

                buildingStats.TryGetValue(building.type, out BuildingStats stats);
                int guests = stats != null ? stats.guests : 0;
                float averageWait = guests > 0 ? stats.totalWait / guests : 0f;

                result.buildings.Add(new BuildingWaveResult
                {
                    type = building.type,
                    guests = guests,
                    averageWaitSeconds = averageWait,
                    isBottleneck = guests > 0 && averageWait > config.bottleneckWaitSeconds
                });
            }
        }

        Debug.Log($"BRYN-80: spokojnosť {result.satisfaction:0} % ({result.guestsRated} hostí) = {result.stars} hviezdičky.");
        Clear();
    }

    // Názov budovy pre hráča.
    public static string DisplayName(BuildingType type)
    {
        return type switch
        {
            BuildingType.Restaurant => "Reštaurácia",
            BuildingType.BeachBar => "Beach bar",
            BuildingType.Reception => "Recepcia",
            BuildingType.Pool => "Bazén",
            BuildingType.Bungalows => "Bungalovy",
            _ => type.ToString()
        };
    }
}
