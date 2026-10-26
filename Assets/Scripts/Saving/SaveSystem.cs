
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
        return JsonUtility.FromJson<GameState>(json);
    }

    public static bool HasSave()
    {
        return File.Exists(SavePath);
    }
}
