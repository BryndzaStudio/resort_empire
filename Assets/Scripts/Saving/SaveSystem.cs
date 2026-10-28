
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
        JsonUtility.FromJsonOverwrite(json, state);
        return state;
    }

    public static bool HasSave()
    {
        return File.Exists(SavePath);
    }
}
