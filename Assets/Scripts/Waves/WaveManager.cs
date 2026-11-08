using UnityEngine;

// BRYN-50: Správca vlny. Určuje, koľko hostí vlna privezie a v akom tempe vystupujú z lode.
// Hostia neprídu naraz, ale rovnomerne: medzi dvoma hosťami je vždy rovnaký odstup (ArrivalInterval).
// BRYN-92: celý príchod hostí z lode na ostrov musí trvať najviac 90 sekúnd.
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

    // Meranie príchodu (kontrola BRYN-92).
    private float waveStartTime;
    private int arrivedGuests;
    private bool waveRunning;

    public int GuestsPerWave => guestsPerWave;

    // Rovnomerný odstup: prvý hosť vystúpi hneď, posledný po arrivalSpread sekundách.
    public float ArrivalInterval => guestsPerWave > 1 ? arrivalSpread / (guestsPerWave - 1) : 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
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

    private void OnStartWaveRequested()
    {
        waveStartTime = Time.time;
        arrivedGuests = 0;
        waveRunning = true;

        Debug.Log($"Vlna začala: {guestsPerWave} hostí, odstup {ArrivalInterval:0.0} s.");
    }

    // Hosť došiel po móle na ostrov. Keď dôjde posledný, vypíšeme, ako dlho príchod trval.
    private void OnGuestArrived(GuestController guest)
    {
        if (!waveRunning)
        {
            return;
        }

        arrivedGuests++;
        if (arrivedGuests < guestsPerWave)
        {
            return;
        }

        waveRunning = false;
        float duration = Time.time - waveStartTime;

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
