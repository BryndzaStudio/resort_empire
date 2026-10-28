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
    public BuildingType type;
    public int level;
    public bool isActive;

    public BuildingState(BuildingType type)
    {
        this.type = type;
        level = 1;
        isActive = true;
    }
}
