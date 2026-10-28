
using System;
using System.Collections.Generic;

[Serializable]
public class GameState
{
    public const int StartingBuildingLevel = 1;

    public int coins;
    public int energy;
    public int resortValue;
    public List<BuildingState> buildings = new List<BuildingState>();

    public GameState(int startingCoins, int startingEnergy, int startingResortValue)
    {
        coins = startingCoins;
        energy = startingEnergy;
        resortValue = startingResortValue;
    }

    public BuildingState GetBuilding(string buildingId)
    {
        return buildings.Find(b => b.buildingId == buildingId);
    }

    // Adds any missing starting buildings at level 1. Existing buildings keep their level.
    public void EnsureStartingBuildings(IEnumerable<string> startingBuildingIds)
    {
        if (buildings == null)
        {
            buildings = new List<BuildingState>();
        }

        foreach (string buildingId in startingBuildingIds)
        {
            if (string.IsNullOrEmpty(buildingId) || GetBuilding(buildingId) != null)
            {
                continue;
            }

            buildings.Add(new BuildingState(buildingId, StartingBuildingLevel));
        }
    }
}
