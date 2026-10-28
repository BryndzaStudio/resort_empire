
using System;

[Serializable]
public class BuildingState
{
    public string buildingId;
    public int level;

    public BuildingState(string buildingId, int level)
    {
        this.buildingId = buildingId;
        this.level = level;
    }
}
