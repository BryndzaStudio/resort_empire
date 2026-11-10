
using System.Collections.Generic;
using UnityEngine;

public static class GuestNeedAssigner
{
    // BRYN-41: Assign a random need supported by an active building.
    public static bool TryAssignNeed(GameState gameState, out GuestNeed need)
    {
        need = default;

        if (gameState == null || gameState.buildings == null)
        {
            return false;
        }

        List<GuestNeed> availableNeeds = new List<GuestNeed>();

        foreach (BuildingState building in gameState.buildings)
        {
            if (building == null || !building.isActive)
            {
                continue;
            }

            switch (building.type)
            {
                case BuildingType.Restaurant:
                    AddUnique(availableNeeds, GuestNeed.Food);
                    break;

                case BuildingType.BeachBar:
                    AddUnique(availableNeeds, GuestNeed.Drink);
                    break;

                case BuildingType.Reception:
                    AddUnique(availableNeeds, GuestNeed.Room);
                    break;
            }
        }

        if (availableNeeds.Count == 0)
        {
            return false;
        }

        need = availableNeeds[Random.Range(0, availableNeeds.Count)];
        return true;
    }

    private static void AddUnique(List<GuestNeed> needs, GuestNeed need)
    {
        if (!needs.Contains(need))
        {
            needs.Add(need);
        }
    }
}
