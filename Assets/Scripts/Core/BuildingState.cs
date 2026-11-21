using System;

// Explicit values keep building types stable in JSON saves.
public enum BuildingType
{
    Restaurant = 0,
    BeachBar = 1,
    Reception = 2
}

[Serializable]
public class BuildingState
{
    // BRYN-28: Počet miest podľa levelu budovy: level 1 = 3, level 2 = 5, level 3 = 7.
    // Hodnoty sa menia len tu.
    private static readonly int[] CapacityByLevel = { 3, 5, 7 };

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

    // Volá sa po načítaní savu. Hostia sa neukladajú, takže po načítaní v budove nikto nie je:
    // obsadené miesta sa vynulujú (inak by zostali "obsadené" z minulej hry a nik by ich neuvoľnil).
    public void EnsureCapacity()
    {
        occupied = 0;
    }
}
