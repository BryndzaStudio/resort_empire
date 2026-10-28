
using System;
using System.Collections.Generic;

[Serializable]
public class GameState
{
    public int coins;
    public int energy;
    public int resortValue;
    public List<BuildingState> buildings;
    public int workerCount;

    public GameState(int startingCoins, int startingEnergy, int startingResortValue)
    {
        coins = startingCoins;
        energy = startingEnergy;
        resortValue = startingResortValue;

        // BRYN-11: the three starting buildings are active and at level 1.
        buildings = new List<BuildingState>
        {
            new BuildingState(BuildingType.Restaurant),
            new BuildingState(BuildingType.BeachBar),
            new BuildingState(BuildingType.Reception)
        };

        // BRYN-12: two workers are available when a new game starts.
        workerCount = 2;
    }
}
