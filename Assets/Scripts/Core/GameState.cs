
using System;
using System.Collections.Generic;

[Serializable]
public class GameState
{
    public const int StartingBuildingLevel = 1;

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

        // BRYN-12: na začiatku hry je k dispozícii jeden pracovník.
        workerCount = 1;

        // BRYN-21: ostatné budovy (bazén, bungalovy) ešte nie sú dokončené, zobrazia sa ako stavenisko.
        EnsureAllBuildings();
    }

    // BRYN-22: Je budova obsluhovaná? Jednoduché pravidlo: pracovníci (workerCount) obsluhujú prvé
    // aktívne budovy v poradí zoznamu. Stavenisko (isActive = false) pracovníka nepotrebuje.
    // Keď pribudne skutočné priraďovanie pracovníkov, stačí nahradiť túto metódu.
    public bool IsStaffed(BuildingState building)
    {
        if (building == null || !building.isActive)
        {
            return false;
        }

        int staffed = 0;
        foreach (BuildingState other in buildings)
        {
            if (!other.isActive)
            {
                continue;
            }

            if (other == building)
            {
                return staffed < workerCount;
            }

            staffed++;
        }

        return false;
    }

    // BRYN-21: doplní budovy, ktoré v zozname chýbajú (napr. starý save), ako nedokončené (isActive = false).
    // Existujúce budovy sa nemenia.
    public void EnsureAllBuildings()
    {
        if (buildings == null)
        {
            buildings = new List<BuildingState>();
        }

        foreach (BuildingType type in Enum.GetValues(typeof(BuildingType)))
        {
            if (!buildings.Exists(b => b.type == type))
            {
                buildings.Add(new BuildingState(type) { isActive = false });
            }
        }
    }
}
