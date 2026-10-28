
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Starting Values")]
    [SerializeField] private int startingCoins = 1000;
    [SerializeField] private int startingEnergy = 100;
    [SerializeField] private int startingResortValue = 5000;

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

        Debug.Log(
            $"Game loaded: Coins={CurrentState.coins}, " +
            $"Energy={CurrentState.energy}, " +
            $"ResortValue={CurrentState.resortValue}, " +
            $"Buildings={CurrentState.buildings.Count}, " +
            $"Workers={CurrentState.workerCount}"
        );
    }

    public void InitializeNewGame()
    {
        CurrentState = new GameState(
            startingCoins,
            startingEnergy,
            startingResortValue
        );

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
