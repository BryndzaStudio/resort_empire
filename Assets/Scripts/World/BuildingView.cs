using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// BRYN-19: Zobrazenie jednej budovy na ostrove.
// Skript patrí na prefab Building, ktorý má komponent SpriteRenderer.
// BRYN-29: Nad budovou sa zobrazuje počet miest (kapacita) na tmavom kruhu.
[RequireComponent(typeof(SpriteRenderer))]
public class BuildingView : MonoBehaviour, IPointerClickHandler
{
    // Konštanty na ladenie (hodnoty sú vo world jednotkách, nezávislé od scale budovy).
    private const float CapacityFontSize = 10f;       // veľkosť textu (musí sa zmestiť "0/6" do kruhu)
    private const float CapacityOffsetY = 0.6f;       // posun stredu kruhu nad vrch budovy
    private const float BadgeDiameter = 2.1f;         // priemer tmavého kruhu
    private const float OutlineWidth = 0.3f;          // hrúbka čierneho obrysu čísla
    private static readonly Color BadgeColor = new Color(0.05f, 0.08f, 0.12f, 0.85f);

    // Budovy majú sortingOrder = -y * 100 (malé čísla), preto je toto vždy nad nimi.
    private const int BadgeSortingOrder = 10000;
    private const int TextSortingOrder = BadgeSortingOrder + 1;

    // Kruh vytvoríme v kóde len raz a zdieľame ho medzi všetkými budovami.
    private static Sprite circleSprite;

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D clickArea;
    private TextMeshPro capacityText;
    private Transform badge;
    private BuildingState state;
    private int shownCapacity = -1;
    private int shownOccupied = -1;

    // BRYN-25: obrázky podľa levelu (lv1, lv2, lv3) a level, ktorý je práve zobrazený.
    private Sprite[] levelSprites;
    private int shownLevel = -1;
    private Vector2? customBadgePosition;

    // BRYN-21: obrázok staveniska a zobrazený stav (-1 = ešte nič, 0 = stavenisko, 1 = hotová budova).
    private Sprite constructionSprite;
    private int shownActive = -1;
    private bool flipSprite;

    // BRYN-22: obsluha budovy sa ukazuje odznakom s pracovníkom vľavo od čísla kapacity.
    // Obsluhovaná budova ho má farebný, budova bez obsluhy tmavý (prázdne miesto). Samotná budova sa nemení.
    // Obrázok sa priraďuje v prefabe Building (badge_worker z Assets/Art/ui).
    [SerializeField] private Sprite workerBadgeSprite;
    private const float WorkerBadgeDiameter = 1.6f;                       // priemer odznaku s pracovníkom
    private static readonly Vector2 WorkerBadgeOffset = new Vector2(-1.95f, 0f); // posun od odznaku s kapacitou
    private static readonly Color NoWorkerColor = new Color(0.2f, 0.2f, 0.2f, 0.7f);
    private SpriteRenderer workerBadge;
    private int shownStaffed = -1;

    // BRYN-37: "+1 personál" / "-1 personál" nad odznakom pri zmene obsluhy.
    private const float StaffChangeOffsetY = 1.3f;
    private StaffChangeIndicator staffChange;

    // BRYN-40: budova sa práve odomkla (stavenisko -> hotová), jej pracovník je nový člen personálu.
    private const string NewStaffLabel = "Nový personál!";
    private bool justActivated;

    // Typ budovy, ktorú tento objekt zobrazuje.
    public BuildingType Type { get; private set; }
    public BuildingState State => state;

    private float baseServiceSeconds = 10f;
    private bool isBoostActive;

    public bool IsBoostActive => isBoostActive;

    // Read staffing every frame so transfers affect services already in progress.
    // BRYN-72: the existing boost still halves the effective service time.
    public float ServiceSeconds
    {
        get
        {
            GameManager manager = GameManager.Instance;
            bool staffed = manager != null && manager.CurrentState != null && manager.CurrentState.IsStaffed(state);
            float emptyRate = manager != null && manager.Config != null ? manager.Config.unstaffedUtilization : 0.33f;
            float utilization = staffed ? 1f : Mathf.Clamp(emptyRate, 0.01f, 1f);
            return baseServiceSeconds * (isBoostActive ? 0.5f : 1f) / utilization;
        }
    }

    private Vector2 guestEntranceOffset;
    private Vector2 guestQueueStep;
    public Vector3 GuestEntrancePosition => transform.position + (Vector3)guestEntranceOffset;

    public Vector3 GetQueuePosition(int index)
    {
        return GuestEntrancePosition + (Vector3)(guestQueueStep * (index + 1));
    }

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        clickArea = gameObject.AddComponent<BoxCollider2D>();
        CreateCapacityBadge();
    }

    // BRYN-24: ťuknutie na budovu otvorí panel s vylepšením.
    // Zachytáva ho Physics2DRaycaster na kamere (pridá ho BuildingUpgradePanel) a EventSystem z UIRoot.
    public void OnPointerClick(PointerEventData eventData)
    {
        if (BuildingUpgradePanel.Instance == null)
        {
            Debug.LogWarning("BuildingView: V scéne chýba objekt UpgradePanel s komponentom BuildingUpgradePanel.");
            return;
        }

        if (state != null && state.isActive)
        {
            BuildingUpgradePanel.Instance.Show(state, this);
        }
        else
        {
            // BRYN-26/27: ťuknutie na stavenisko otvorí panel na odomknutie budovy.
            BuildingUnlockPanel.Show(state);
        }
    }
    private void OnEnable()
    {
        WaveEvents.WaveFinished += OnWaveFinished;
    }

    private void OnDisable()
    {
        WaveEvents.WaveFinished -= OnWaveFinished;
    }

    // BRYN-72: Cost comes from the shared GameConfig.
    public int BoostEnergyCost
    {
        get
        {
            GameManager manager = GameManager.Instance;

            if (manager != null && manager.Config != null)
                return Mathf.Max(0, manager.Config.buildingBoostEnergyCost);

            return 10; // Fallback cost
        }
    }

    // Check whether this building can currently be boosted.
    public bool CanActivateBoost
    {
        get
        {
            GameManager manager = GameManager.Instance;
            WaveManager wave = WaveManager.Instance;

            return state != null
                && state.isActive
                && !isBoostActive
                && wave != null
                && wave.IsWaveRunning
                && manager != null
                && manager.CurrentState != null
                && manager.CurrentState.energy >= BoostEnergyCost;
        }
    }

    // Spend energy and activate the boost for this building.
    public bool TryActivateBoost()
    {
        if (!CanActivateBoost)
            return false;

        GameManager manager = GameManager.Instance;
        int cost = BoostEnergyCost;

        manager.CurrentState.energy -= cost;
        isBoostActive = true;

        manager.SaveGame();

        Debug.Log(
            $"BRYN-72: Boost activated for {Type} | " +
            $"Cost: {cost} energy | " +
            $"Service time: {ServiceSeconds:0.0}s"
        );

        return true;
    }

    // Remove the temporary speed boost after the wave.
    private void OnWaveFinished(WaveResult result)
    {
        isBoostActive = false;
    }

    // Oblasť ťuknutia = obrázok budovy. Prepočíta sa pri každej zmene obrázka.
    private void UpdateClickArea()
    {
        Sprite sprite = spriteRenderer.sprite;
        if (sprite == null)
        {
            return;
        }

        clickArea.size = sprite.bounds.size;
        clickArea.offset = sprite.bounds.center;
    }

    // Nastaví budove obrázok a poradie vykresľovania.
    // badgeLocalPosition: vlastná pozícia odznaku pod budovou, null = automaticky nad budovou.
    public void Setup(BuildingType type, Sprite sprite, Vector2? badgeLocalPosition = null,
        float serviceSeconds = 10f, Vector2? entranceOffset = null, Vector2? queueStep = null,
        Sprite[] sprites = null, Sprite construction = null, bool flip = false)
    {
        flipSprite = flip;
        Type = type;
        name = "Building_" + type;

        // Izometria: čo je nižšie na obrazovke, je bližšie ku kamere,
        // preto sa musí kresliť navrch (vyššie sortingOrder).
        spriteRenderer.sortingOrder = -Mathf.RoundToInt(transform.position.y * 100);

        customBadgePosition = badgeLocalPosition;

        state = GameManager.Instance.CurrentState.buildings.Find(b => b.type == type);
        levelSprites = sprites != null && sprites.Length > 0 ? sprites : new[] { sprite };
        constructionSprite = construction;
        baseServiceSeconds = serviceSeconds > 0f ? serviceSeconds : 10f;
        isBoostActive = false;
        guestEntranceOffset = entranceOffset ?? new Vector2(0f, 0.1f);
        guestQueueStep = queueStep ?? new Vector2(1.25f, -0.65f);
        if (state != null)
        {
            ShowVisual();
            ShowCapacity();
        }
    }

    // Ak sa isActive, level, obsadené miesta alebo kapacita v stave hry zmenia, obrázok a text sa prepíšu sami.
    private void Update()
    {
        if (state == null)
        {
            return;
        }

        if (state.level != shownLevel || (state.isActive ? 1 : 0) != shownActive)
        {
            ShowVisual();
        }

        if (state.isActive && (state.capacity != shownCapacity || state.occupied != shownOccupied))
        {
            ShowCapacity();
        }

        UpdateStaffedLook();
    }

    // BRYN-22: Odznak s pracovníkom je farebný, ak je budova obsluhovaná, inak tmavý.
    private void UpdateStaffedLook()
    {
        // BRYN-40: príznak platí len v tom frame, keď sa budova odomkla.
        bool activatedNow = justActivated;
        justActivated = false;

        GameManager manager = GameManager.Instance;
        if (workerBadge == null || manager == null || manager.CurrentState == null)
        {
            return;
        }

        bool staffed = state.isActive && manager.CurrentState.IsStaffed(state);
        int value = staffed ? 1 : 0;
        if (value == shownStaffed)
        {
            return;
        }

        int previous = shownStaffed;
        shownStaffed = value;
        workerBadge.color = staffed ? Color.white : NoWorkerColor;

        // BRYN-37/91: pri zmene (nie pri prvom zobrazení) ukážeme, či personál pribudol alebo ubudol.
        // BRYN-40: ak sa budova práve odomkla a dostala pracovníka, je to nový člen personálu.
        if (previous != -1 && staffChange != null)
        {
            string label = staffed && activatedNow ? NewStaffLabel : null;
            staffChange.Show(staffed, workerBadge.transform.position + Vector3.up * StaffChangeOffsetY, label);
        }
    }

    // BRYN-21: Dokončená budova (isActive) sa zobrazí podľa levelu (BRYN-25), nedokončená ako stavenisko.
    // Stavenisko nemá odznak s kapacitou a nie je zaregistrované u hostí; ťuknutím sa otvorí odomknutie (BRYN-26).
    private void ShowVisual()
    {
        bool active = state.isActive;
        // BRYN-40: zmena zo staveniska na hotovú budovu počas hry (nie prvé zobrazenie).
        justActivated = active && shownActive == 0;
        shownActive = active ? 1 : 0;
        shownLevel = state.level;

        Sprite sprite = active ? LevelSprite() : constructionSprite;
        spriteRenderer.sprite = sprite;
        // Zrkadlí sa len hotová budova, stavenisko ostáva v pôvodnom smere.
        spriteRenderer.flipX = active && flipSprite;
        spriteRenderer.enabled = sprite != null;
        clickArea.enabled = sprite != null; // BRYN-26: ťuknúť sa dá aj na stavenisko
        badge.gameObject.SetActive(active);
        capacityText.gameObject.SetActive(active);
        if (workerBadge != null)
        {
            workerBadge.gameObject.SetActive(active);
        }

        if (sprite != null)
        {
            UpdateClickArea();

            // Obrázky majú rôznu veľkosť, preto sa odznak nad budovou musí prepočítať.
            PlaceBadge();
        }

        if (active && GuestManager.Instance != null)
        {
            GuestManager.Instance.RegisterBuilding(this);
        }
    }

    // BRYN-25: obrázok podľa levelu (level 1 = prvok 0). Mimo rozsahu sa vezme najbližší.
    private Sprite LevelSprite()
    {
        int index = Mathf.Clamp(state.level - 1, 0, levelSprites.Length - 1);
        return levelSprites[index];
    }

    // Umiestni odznak s číslom: na pozíciu z Inspectora (IslandView), inak nad vrch obrázka budovy.
    private void PlaceBadge()
    {
        if (customBadgePosition.HasValue)
        {
            Vector3 localPosition = customBadgePosition.Value;
            badge.localPosition = localPosition;
            capacityText.transform.localPosition = localPosition;
        }
        else
        {
            Bounds bounds = spriteRenderer.bounds;
            Vector3 badgePosition = new Vector3(bounds.center.x, bounds.max.y + CapacityOffsetY, transform.position.z);
            badge.position = badgePosition;
            capacityText.transform.position = badgePosition;
        }

        // Odznak s pracovníkom je vždy vedľa odznaku s kapacitou.
        if (workerBadge != null)
        {
            workerBadge.transform.position = badge.position + (Vector3)WorkerBadgeOffset;
        }
    }

    // Zobrazí text v tvare obsadené/kapacita, napr. "0/6".
    private void ShowCapacity()
    {
        if (state == null)
        {
            return;
        }

        shownCapacity = state.capacity;
        shownOccupied = state.occupied;
        capacityText.text = shownOccupied + "/" + shownCapacity;
    }

    // Kruh aj text vytvárame v kóde, aby sa nemusel meniť prefab Building.
    private void CreateCapacityBadge()
    {
        // Budova má scale 0.7, deti by sa zmenšili s ňou. Vyrovnáme to,
        // aby konštanty hore platili vo world jednotkách.
        Vector3 parentScale = transform.localScale;
        Vector3 compensation = new Vector3(1f / parentScale.x, 1f / parentScale.y, 1f);

        GameObject badgeObject = new GameObject("CapacityBadge");
        badge = badgeObject.transform;
        badge.SetParent(transform, false);
        badge.localScale = compensation * BadgeDiameter;

        SpriteRenderer badgeRenderer = badgeObject.AddComponent<SpriteRenderer>();
        badgeRenderer.sprite = GetCircleSprite();
        badgeRenderer.color = BadgeColor;
        badgeRenderer.sortingOrder = BadgeSortingOrder;

        GameObject textObject = new GameObject("CapacityText");
        textObject.transform.SetParent(transform, false);
        textObject.transform.localScale = compensation;

        capacityText = textObject.AddComponent<TextMeshPro>();
        capacityText.fontSize = CapacityFontSize;
        capacityText.fontStyle = FontStyles.Bold;
        capacityText.alignment = TextAlignmentOptions.Center;
        capacityText.color = Color.white;
        capacityText.outlineWidth = OutlineWidth;
        capacityText.outlineColor = Color.black;
        capacityText.sortingOrder = TextSortingOrder;

        // BRYN-22: odznak s pracovníkom (len ak je priradený obrázok).
        if (workerBadgeSprite != null)
        {
            GameObject workerObject = new GameObject("WorkerBadge");
            workerObject.transform.SetParent(transform, false);
            workerObject.transform.localScale = compensation * (WorkerBadgeDiameter / workerBadgeSprite.bounds.size.x);

            workerBadge = workerObject.AddComponent<SpriteRenderer>();
            workerBadge.sprite = workerBadgeSprite;
            workerBadge.sortingOrder = BadgeSortingOrder;

            // BRYN-37: text o zmene personálu, navrch nad odznakmi.
            staffChange = gameObject.AddComponent<StaffChangeIndicator>();
            staffChange.Init(compensation, TextSortingOrder + 1);
        }
    }

    // Jednoduchý biely kruh s vyhladenou hranou, veľký presne 1 jednotku.
    private static Sprite GetCircleSprite()
    {
        if (circleSprite != null)
        {
            return circleSprite;
        }

        const int size = 64;
        float radius = size / 2f;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2(radius, radius);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Priehľadnosť klesá na hrane kruhu, vďaka tomu nie je zubatý.
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(radius - distance)));
            }
        }
        texture.Apply();

        circleSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return circleSprite;
    }
}
