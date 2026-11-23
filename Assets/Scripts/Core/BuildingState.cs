using System;

// Explicit values keep building types stable in JSON saves.
public enum BuildingType
{
    Restaurant = 0,
    BeachBar = 1,
    Reception = 2,
    // BRYN-21: nové budovy sa pridávajú len na koniec, aby sa nezmenili hodnoty v starých savoch.
    Pool = 3,
    Bungalows = 4
}

[Serializable]
public class BuildingState
{
    // BRYN-28: Počet miest podľa levelu budovy: level 1 = 3, level 2 = 5, level 3 = 7.
    // Hodnoty sa menia len tu.
    private static readonly int[] CapacityByLevel = { 3, 5, 7 };

    // BRYN-24: Cena vylepšenia v minciach: level 1 -> 2 = 200, level 2 -> 3 = 500. Hodnoty sa menia len tu.
    private static readonly int[] UpgradeCosts = { 200, 500 };

    // BRYN-26/27: Odomknutie budovy: po koľkých dokončených vlnách sa dá kúpiť a za koľko mincí.
    // Ceny sú dočasné. Hodnoty (vlny aj ceny) sa menia len tu.
    private static readonly (BuildingType type, int wave, int cost)[] Unlocks =
    {
        (BuildingType.Pool, 3, 500),
        (BuildingType.Bungalows, 6, 1000)
    };

    public BuildingType type;
    public int level;
    public bool isActive;
    public int occupied;

    // Kapacita sa počíta z levelu, preto sa neukladá do savu a nikdy nie je zastaralá
    // (ani po zmene levelu alebo po načítaní starého savu). Ostatné skripty ju čítajú ako doteraz.
    public int capacity => CapacityForLevel(level);

    public BuildingState(BuildingType type)
    {
        this.type = type;
        level = GameState.StartingBuildingLevel;
        isActive = true;
        occupied = 0;
    }

    // Kapacita pre daný level. Level mimo rozsahu (napr. 0 alebo 4) dostane najbližšiu hodnotu.
    public static int CapacityForLevel(int level)
    {
        int index = Math.Clamp(level - 1, 0, CapacityByLevel.Length - 1);
        return CapacityByLevel[index];
    }

    // Cena vylepšenia z daného levelu na ďalší. Vráti -1, ak je budova už na maximálnom leveli.
    public static int UpgradeCost(int level)
    {
        int index = Math.Max(level, 1) - 1;
        return index < UpgradeCosts.Length ? UpgradeCosts[index] : -1;
    }

    // BRYN-26: Po koľkých dokončených vlnách sa budova odomyká. Vráti -1, ak sa budova neodomyká.
    public static int UnlockWave(BuildingType type)
    {
        foreach (var unlock in Unlocks)
        {
            if (unlock.type == type)
            {
                return unlock.wave;
            }
        }

        return -1;
    }

    // BRYN-27: Cena odomknutia v minciach. Vráti -1, ak sa budova neodomyká.
    public static int UnlockCost(BuildingType type)
    {
        foreach (var unlock in Unlocks)
        {
            if (unlock.type == type)
            {
                return unlock.cost;
            }
        }

        return -1;
    }

    // Volá sa po načítaní savu. Hostia sa neukladajú, takže po načítaní v budove nikto nie je:
    // obsadené miesta sa vynulujú (inak by zostali "obsadené" z minulej hry a nik by ich neuvoľnil).
    public void EnsureCapacity()
    {
        occupied = 0;
    }
}
