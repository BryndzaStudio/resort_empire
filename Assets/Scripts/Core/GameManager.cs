
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private GameConfig config;

    [Header("Starting Values")]
    [SerializeField] private int startingResortValue = 5000;

    // Fallback values used only when no GameConfig asset is assigned.
    private const int DefaultStartingCoins = 1000;
    private const int DefaultStartingEnergy = 100;

    [Header("Starting Buildings")]
    [SerializeField] private string[] startingBuildingIds = { "reception", "hotel", "restaurant" };

    public GameState CurrentState { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (SaveSystem.HasSave())
        {
            CurrentState = SaveSystem.Load();
        }

        if (CurrentState == null)
        {
            InitializeNewGame();
        }
        else
        {
            // Older saves have no buildings yet.
            CurrentState.EnsureStartingBuildings(startingBuildingIds);
        }

        Debug.Log(
            $"Game loaded: Coins={CurrentState.coins}, " +
            $"Energy={CurrentState.energy}, " +
            $"ResortValue={CurrentState.resortValue}, " +
            $"Buildings={CurrentState.buildings.Count}"
        );
    }

    public void InitializeNewGame()
    {
        if (config == null)
        {
            Debug.LogWarning("GameManager: GameConfig is not assigned, using default starting values.");
        }

        int startingCoins = config != null ? config.startingCoins : DefaultStartingCoins;
        int startingEnergy = config != null ? config.startingEnergy : DefaultStartingEnergy;

        CurrentState = new GameState(
            startingCoins,
            startingEnergy,
            startingResortValue
        );
        CurrentState.EnsureStartingBuildings(startingBuildingIds);

        SaveGame();
        Debug.Log("New game initialized.");
    }

    public void SaveGame()
    {
        if (CurrentState != null)
        {
            SaveSystem.Save(CurrentState);
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused)
        {
            SaveGame();
        }
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }
}
