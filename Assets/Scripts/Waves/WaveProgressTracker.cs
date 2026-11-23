using UnityEngine;

// BRYN-26: Počíta dokončené vlny (GameState.wavesCompleted) a ukladá hru.
// Vytvorí sa sám pri štarte hry, do scény ho netreba pridávať. WaveManager sa nemení,
// stačí mu udalosť WaveEvents.WaveFinished.
public class WaveProgressTracker : MonoBehaviour
{
    // Uloženie sa nerobí v handleri udalosti: SaveGame je synchrónny zápis súboru a ResultScreen
    // (BRYN-93) meria čas po konci framu, takže by sa počítal do limitu 100 ms.
    private int saveAtFrame = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (FindAnyObjectByType<WaveProgressTracker>() != null)
        {
            return;
        }

        GameObject tracker = new GameObject("WaveProgressTracker");
        tracker.AddComponent<WaveProgressTracker>();
        DontDestroyOnLoad(tracker);
    }

    private void OnEnable()
    {
        WaveEvents.WaveFinished += OnWaveFinished;
    }

    private void OnDisable()
    {
        WaveEvents.WaveFinished -= OnWaveFinished;
    }

    private void OnWaveFinished(WaveResult result)
    {
        GameManager manager = GameManager.Instance;
        if (manager == null || manager.CurrentState == null)
        {
            return;
        }

        manager.CurrentState.wavesCompleted++;
        saveAtFrame = Time.frameCount + 1;
    }

    private void Update()
    {
        if (saveAtFrame < 0 || Time.frameCount < saveAtFrame)
        {
            return;
        }

        saveAtFrame = -1;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SaveGame();
        }
    }
}
