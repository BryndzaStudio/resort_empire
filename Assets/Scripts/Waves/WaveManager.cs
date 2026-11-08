using System;
using System.Collections.Generic;
using UnityEngine;

// BRYN-50: Správca vlny. Určuje, koľko hostí vlna privezie a v akom tempe vystupujú z lode.
// Hostia neprídu naraz, ale rovnomerne: medzi dvoma hosťami je vždy rovnaký odstup (ArrivalInterval).
// BRYN-92: celý príchod hostí z lode na ostrov musí trvať najviac 90 sekúnd.
// BRYN-52: vlna sa skončí, keď hostia už nepribúdajú (všetci došli na ostrov) a všetky rady sú prázdne.
// Loď (BoatController) si od správcu vlny berie počet hostí a odstup medzi nimi.
// Skript patrí na prázdny objekt WaveManager v scéne.
public class WaveManager : MonoBehaviour
{
    // BRYN-92: horný limit pre príchod hostí (od kliknutia na Start Wave po posledného hosťa na ostrove).
    public const float MaxArrivalSeconds = 90f;

    public static WaveManager Instance { get; private set; }

    [Header("Vlna")]
    [Tooltip("Koľko hostí privezie jedna vlna.")]
    [Min(1)] [SerializeField] private int guestsPerWave = 10;

    [Tooltip("Za koľko sekúnd vystúpia z lode všetci hostia (od prvého po posledného). " +
             "Zvyšok do 90 s je rezerva na plavbu lode a chôdzu po móle.")]
    [Range(0f, 75f)] [SerializeField] private float arrivalSpread = 60f;

    // Fázy vlny: nič sa nedeje / hostia prichádzajú / hostia už nepribúdajú, čakáme na prázdne rady.
    private enum WaveState
    {
        Idle,
        Arriving,
        WaitingForQueues
    }

    private WaveState state = WaveState.Idle;
    private int waveGuests;
    private int arrivedGuests;
    private float waveStartTime;
    private int coinsAtStart;
    private StartWaveButton startWaveButton;

    // BRYN-52: rady pri budovách. Každý rad sa prihlási funkciou, ktorá vráti true, keď je prázdny.
    private readonly List<Func<bool>> queues = new List<Func<bool>>();

    public int GuestsPerWave => guestsPerWave;

    // Rovnomerný odstup: prvý hosť vystúpi hneď, posledný po arrivalSpread sekundách.
    public float ArrivalInterval => guestsPerWave > 1 ? arrivalSpread / (guestsPerWave - 1) : 0f;

    // Beží vlna? (od kliknutia na Start Wave po jej skončenie)
    public bool IsWaveRunning => state != WaveState.Idle;

    // BRYN-52: hostia už nepribúdajú (všetci hostia vlny sú na ostrove).
    public bool ArrivalFinished => state == WaveState.WaitingForQueues;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        startWaveButton = FindAnyObjectByType<StartWaveButton>(FindObjectsInactive.Include);
    }

    private void OnEnable()
    {
        WaveEvents.StartWaveRequested += OnStartWaveRequested;
        GuestController.OnGuestArrived += OnGuestArrived;
    }

    private void OnDisable()
    {
        WaveEvents.StartWaveRequested -= OnStartWaveRequested;
        GuestController.OnGuestArrived -= OnGuestArrived;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // BRYN-52: rad (napr. pri budove) sa prihlási, aby vlna počkala, kým sa vyprázdni.
    // Príklad: WaveManager.Instance.RegisterQueue(() => guestsInQueue.Count == 0);
    // Kým sa žiadny rad neprihlási, považujú sa všetky rady za prázdne.
    public void RegisterQueue(Func<bool> isEmpty)
    {
        if (isEmpty != null && !queues.Contains(isEmpty))
        {
            queues.Add(isEmpty);
        }
    }

    public void UnregisterQueue(Func<bool> isEmpty)
    {
        queues.Remove(isEmpty);
    }

    private void OnStartWaveRequested()
    {
        if (state != WaveState.Idle)
        {
            return;
        }

        state = WaveState.Arriving;
        waveGuests = guestsPerWave;
        arrivedGuests = 0;
        waveStartTime = Time.time;
        coinsAtStart = CurrentCoins();

        Debug.Log($"Vlna začala: {waveGuests} hostí, odstup {ArrivalInterval:0.0} s.");
    }

    // Hosť došiel po móle na ostrov. Keď dôjde posledný, hostia už nepribúdajú.
    private void OnGuestArrived(GuestController guest)
    {
        if (state != WaveState.Arriving)
        {
            return;
        }

        arrivedGuests++;
        if (arrivedGuests < waveGuests)
        {
            return;
        }

        LogArrivalDuration(Time.time - waveStartTime);
        state = WaveState.WaitingForQueues;
        TryFinishWave();
    }

    // Rady sa môžu vyprázdniť kedykoľvek, preto to kontrolujeme každý frame.
    private void Update()
    {
        TryFinishWave();
    }

    // BRYN-52: koniec vlny = hostia už nepribúdajú a všetky rady sú prázdne.
    private void TryFinishWave()
    {
        if (state == WaveState.WaitingForQueues && AllQueuesEmpty())
        {
            FinishWave();
        }
    }

    private bool AllQueuesEmpty()
    {
        // Rady zo zničených objektov vyhodíme, inak by vlna nikdy neskončila.
        queues.RemoveAll(q => q.Target is UnityEngine.Object owner && owner == null);

        foreach (Func<bool> isEmpty in queues)
        {
            if (!isEmpty())
            {
                return false;
            }
        }

        return true;
    }

    private void FinishWave()
    {
        state = WaveState.Idle;

        WaveResult result = new WaveResult
        {
            guestsArrived = arrivedGuests,
            coinsEarned = CurrentCoins() - coinsAtStart,
            durationSeconds = Time.time - waveStartTime,
            finishedAtRealtime = Time.realtimeSinceStartup
        };

        Debug.Log($"Vlna skončila: hostia {result.guestsArrived}, mince {result.coinsEarned:+0;-0;0}, trvanie {result.durationSeconds:0.0} s.");
        WaveEvents.RaiseWaveFinished(result);

        // Po skončení vlny sa dá spustiť ďalšia.
        UnlockStartWaveButton();
    }

    private void UnlockStartWaveButton()
    {
        if (startWaveButton != null)
        {
            startWaveButton.SetInteractable(true);
        }
    }

    private static int CurrentCoins()
    {
        GameManager manager = GameManager.Instance;
        return manager != null && manager.CurrentState != null ? manager.CurrentState.coins : 0;
    }

    // BRYN-92: kontrola, že príchod hostí trval najviac 90 sekúnd.
    private void LogArrivalDuration(float duration)
    {
        if (duration <= MaxArrivalSeconds)
        {
            Debug.Log($"Všetci hostia vlny sú na ostrove ({arrivedGuests}), príchod trval {duration:0.0} s (limit {MaxArrivalSeconds:0} s).");
        }
        else
        {
            Debug.LogWarning($"Príchod hostí trval {duration:0.0} s, čo je viac ako {MaxArrivalSeconds:0} s (BRYN-92). Zníž Arrival Spread alebo počet hostí.");
        }
    }
}
