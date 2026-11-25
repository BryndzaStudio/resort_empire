
using System.IO;
using UnityEngine;

public static class SaveSystem
{
    private static string SavePath =>
        Path.Combine(Application.persistentDataPath, "resort_save.json");

    public static void Save(GameState state)
    {
        string json = JsonUtility.ToJson(state, true);
        File.WriteAllText(SavePath, json);
        Debug.Log("Game saved to: " + SavePath);
    }

    public static GameState Load()
    {
        if (!File.Exists(SavePath))
        {
            return null;
        }

        string json = File.ReadAllText(SavePath);
        return Deserialize(json);
    }

    public static GameState Deserialize(string json)
    {
        if (string.IsNullOrEmpty(json) || json.Trim() == "null")
        {
            return null;
        }

        // Missing fields in older saves keep the starting content. Existing
        // values, including zero workers or an empty building list, overwrite it.
        var state = new GameState(0, 0, 0);
        state.workerAssignments = null;
        JsonUtility.FromJsonOverwrite(json, state);

        // BRYN-21: budovy, ktoré starý save nepozná (bazén, bungalovy), sa doplnia ako stavenisko.
        state.EnsureAllBuildings();
        state.EnsureStaffing();

        // BRYN-28: kapacita sa počíta z levelu, tu už len ošetríme obsadené miesta.
        if (state.buildings != null)
        {
            foreach (BuildingState building in state.buildings)
            {
                building.EnsureCapacity();
            }
        }

        return state;
    }

    public static bool HasSave()
    {
        return File.Exists(SavePath);
    }
}
