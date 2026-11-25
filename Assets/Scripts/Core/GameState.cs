
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
    // BRYN-39: one anonymous worker per listed building; stable enum IDs survive saves.
    public List<BuildingType> workerAssignments;

    // BRYN-26: počet dokončených vĺn (míľnik na odomknutie budov). Starý save ho nemá, preto začína na 0.
    public int wavesCompleted;

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

        // BRYN-21: ostatné budovy (bazén, bungalovy) ešte nie sú dokončené, zobrazia sa ako stavenisko.
        EnsureAllBuildings();
        EnsureStaffing();
    }

    // BRYN-22/39: badges and service read the same persistent assignments.
    public bool IsStaffed(BuildingState building)
    {
        return building != null && building.isActive && buildings.Contains(building) &&
            workerAssignments != null && workerAssignments.Contains(building.type);
    }

    // BRYN-35: repair legacy saves once, preserving valid assignments and all resources.
    public void EnsureStaffing()
    {
        if (workerAssignments == null) workerAssignments = new List<BuildingType>();
        int activeCount = buildings.FindAll(b => b != null && b.isActive).Count;
        workerCount = Math.Max(0, activeCount - 1);
        var seen = new HashSet<BuildingType>();
        workerAssignments.RemoveAll(type =>
            !buildings.Exists(b => b != null && b.isActive && b.type == type) || !seen.Add(type));
        while (workerAssignments.Count > workerCount)
        {
            workerAssignments.RemoveAt(workerAssignments.Count - 1);
        }
        foreach (BuildingState building in buildings)
        {
            if (workerAssignments.Count >= workerCount) break;
            if (building != null && building.isActive && !workerAssignments.Contains(building.type))
            {
                workerAssignments.Add(building.type);
            }
        }
    }

    public bool CanTransferWorker(BuildingState source, BuildingState target, bool paused)
    {
        return !paused && source != target && IsStaffed(source) && target != null &&
            target.isActive && buildings.Contains(target) && !IsStaffed(target);
    }

    // BRYN-33/34/36: one slot, unlocked buildings only, no currency changes.
    public bool TryTransferWorker(BuildingState source, BuildingState target, bool paused)
    {
        if (!CanTransferWorker(source, target, paused)) return false;
        workerAssignments[workerAssignments.IndexOf(source.type)] = target.type;
        return true;
    }

    // New unlocks bring their worker; the previous empty building stays empty.
    public void ActivateBuilding(BuildingState building)
    {
        if (building == null || building.isActive || !buildings.Contains(building)) return;
        building.isActive = true;
        if (workerAssignments == null) workerAssignments = new List<BuildingType>();
        workerAssignments.Add(building.type);
        EnsureStaffing();
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
