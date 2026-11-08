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
    // BRYN-29: Východisková kapacita (počet miest) novej budovy. Tu sa dá jednoducho zmeniť.
    public const int DefaultCapacity = 6;

    public BuildingType type;
    public int level;
    public bool isActive;
    public int capacity;
    public int occupied;

    public BuildingState(BuildingType type)
    {
        this.type = type;
        level = GameState.StartingBuildingLevel;
        isActive = true;
        capacity = DefaultCapacity;
        occupied = 0;
    }

    // Staré savy nemajú pole capacity (JSON ho načíta ako 0) – doplníme východiskovú hodnotu.
    // Obsadené miesta nesmú byť záporné ani väčšie než kapacita. Chýbajúce occupied je 0.
    public void EnsureCapacity()
    {
        if (capacity <= 0)
        {
            capacity = DefaultCapacity;
        }

        occupied = Math.Clamp(occupied, 0, capacity);
    }
}
