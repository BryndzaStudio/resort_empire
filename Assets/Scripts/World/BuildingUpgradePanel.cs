using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// BRYN-24: Panel na vylepšenie budovy. Otvorí sa ťuknutím na budovu (BuildingView).
// Upgrade je okamžitý: zvýši level, odpočíta mince a uloží hru. Obrázok a kapacitu budovy
// prepne BuildingView sám, mince v HUD prepíše TopBarHud.
// Skript patrí na prázdny objekt UpgradePanel v scéne. UI sa vytvára v kóde, HUD prefaby sa nemenia.
public class BuildingUpgradePanel : MonoBehaviour
{
    public static BuildingUpgradePanel Instance { get; private set; }

    [Header("Sprity z Assets/Art/ui (nepovinné, bez nich sa použijú farby)")]
    [SerializeField] private Sprite panelSprite;   // popup_panel
    [SerializeField] private Sprite buttonSprite;  // btn_primary
    [SerializeField] private Sprite closeSprite;   // btn_close
    [SerializeField] private Sprite coinSprite;    // icon_coin (Assets/Art/horny-panel), zobrazí sa pri cene

    // BRYN-27: sprity využíva aj BuildingUnlockPanel.
    public Sprite PanelSprite => panelSprite;
    public Sprite ButtonSprite => buttonSprite;
    public Sprite CloseSprite => closeSprite;
    public Sprite CoinSprite => coinSprite;

    // Vzhľad na ladenie (referenčné rozlíšenie 1920x1080).
    private static readonly Vector2 PanelSize = new Vector2(600f, 860f);
    private static readonly Color TextColor = new Color(0.35f, 0.2f, 0.08f);
    private static readonly Color WarningColor = new Color(0.75f, 0.15f, 0.1f);

    private GameObject root;
    private TMP_Text titleText;
    private TMP_Text infoText;
    private TMP_Text reasonText;
    private TMP_Text buttonText;
    private TMP_Text costText;
    private Image coinImage;
    private Button upgradeButton;
    private Button boostButton;
    private TMP_Text boostButtonText;
    private TMP_Text boostReasonText;
    private Button staffButton;
    private TMP_Text staffButtonText;

    private BuildingState building;
    private BuildingView selectedView;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildUi();
        Hide();
    }

    private void Start()
    {
        // Ťuknutie na budovu (BoxCollider2D) zachytáva Physics2DRaycaster na kamere.
        Camera cam = Camera.main;
        if (cam != null && cam.GetComponent<Physics2DRaycaster>() == null)
        {
            cam.gameObject.AddComponent<Physics2DRaycaster>();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }


    public void Show(BuildingState state, BuildingView view = null)
    {
        building = state;
        selectedView = view;

        root.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        building = null;
        selectedView = null;
        root.SetActive(false);
    }


    // Kým je panel otvorený, stále sleduje mince a vlnu (napr. vlna sa môže spustiť, kým je panel otvorený).
    private void Update()
    {
        if (building != null)
        {
            Refresh();
        }
    }

    private void Refresh()
    {
        if (GameManager.Instance == null || GameManager.Instance.CurrentState == null)
        {
            return;
        }

        int cost = BuildingState.UpgradeCost(building.level);
        bool canUpgrade = CanUpgrade(out string reason);

        // BRYN-112: personál budovy (obsluhovaná / bez obsluhy) ako tretí riadok.
        string staff = GameManager.Instance.CurrentState.IsStaffed(building) ? "Personál: obsluhovaná" : "Personál: bez obsluhy";
        staffButton.interactable = building.isActive && GameManager.Instance.CanTransferStaff;
        staffButtonText.text = GameManager.Instance.CanTransferStaff ? "Presunúť personál" : "Personál (pauza)";

        // Na maxime nie je cena ani minca, preto text tlačidla zaberie celú šírku (inak pôvodné ukotvenie 0.05 - 0.55).
        RectTransform buttonTextRect = (RectTransform)buttonText.transform;
        buttonTextRect.anchorMax = new Vector2(cost < 0 ? 0.95f : 0.55f, buttonTextRect.anchorMax.y);

        titleText.text = DisplayName(building.type);
        if (cost < 0)
        {
            infoText.text = $"Level {building.level} (maximum)\nMiesta: {building.capacity}\n{staff}";
            buttonText.text = "Maximum";
            costText.text = "";
        }
        else
        {
            int next = building.level + 1;
            infoText.text = $"Level {building.level} -> {next}\nMiesta: {building.capacity} -> {BuildingState.CapacityForLevel(next)}\n{staff}";
            buttonText.text = "Upgrade";
            costText.text = TopBarHud.Format(cost);
        }

        // Minca pri cene sa ukáže len keď je čo platiť a obrázok je priradený.
        coinImage.gameObject.SetActive(cost >= 0 && coinSprite != null);

        reasonText.text = reason;
        upgradeButton.interactable = canUpgrade;


        // BRYN-72: Update the boost button and its availability.
        bool canBoost = selectedView != null
            && selectedView.CanActivateBoost;

        boostButton.interactable = canBoost;

        if (selectedView == null)
        {
            boostButtonText.text = "Boost nedostupný";
            boostReasonText.text = "";
        }
        else
        {
            int energyCost = selectedView.BoostEnergyCost;

            boostButtonText.text = selectedView.IsBoostActive
                ? "Boost aktívny"
                : $"Boost -50 % ({energyCost} energie)";

            if (selectedView.IsBoostActive)
            {
                boostReasonText.text = "Boost už je aktívny.";
            }
            else if (WaveManager.Instance == null ||
                     !WaveManager.Instance.IsWaveRunning)
            {
                boostReasonText.text = "Dostupné iba počas vlny.";
            }
            else if (GameManager.Instance.CurrentState.energy < energyCost)
            {
                boostReasonText.text = "Nedostatok energie.";
            }
            else
            {
                boostReasonText.text =
                    "Obsluha 2x rýchlejšia do konca vlny.";
            }
        }

    }

    // Upgrade je možný len ak budova nie je na maximálnom leveli, nebeží vlna a je dosť mincí.
    private bool CanUpgrade(out string reason)
    {
        reason = "";
        int cost = BuildingState.UpgradeCost(building.level);
        if (cost < 0)
        {
            reason = "Budova je na maximálnom leveli.";
            return false;
        }

        if (WaveManager.Instance != null && WaveManager.Instance.IsWaveRunning)
        {
            reason = "Počas vlny sa budovy nedajú vylepšovať.";
            return false;
        }

        int missing = cost - GameManager.Instance.CurrentState.coins;
        if (missing > 0)
        {
            reason = $"Nedostatok mincí, chýba {TopBarHud.Format(missing)}.";
            return false;
        }

        return true;
    }

    private void Upgrade()
    {
        if (building == null || !CanUpgrade(out _))
        {
            return;
        }

        GameManager.Instance.CurrentState.coins -= BuildingState.UpgradeCost(building.level);
        building.level++;
        GameManager.Instance.SaveGame();
        Refresh();
    }
    // BRYN-72: Activate a speed boost for the selected building.
    private void Boost()
    {
        if (selectedView == null)
            return;

        if (selectedView.TryActivateBoost())
            Refresh();
    }
    private static string DisplayName(BuildingType type)
    {
        return type switch
        {
            BuildingType.Restaurant => "Reštaurácia",
            BuildingType.BeachBar => "Beach bar",
            BuildingType.Reception => "Recepcia",
            _ => type.ToString()
        };
    }

    private void ShowStaff()
    {
        if (building == null || !GameManager.Instance.CanTransferStaff) return;
        BuildingState selected = building;
        Hide();
        StaffAllocationPanel.Show(selected);
    }

    // --- Vytvorenie UI v kóde ---

    private void BuildUi()
    {
        root = new GameObject("UpgradeCanvas");
        root.transform.SetParent(transform, false);

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>();

        // Stmavené pozadie: ťuknutie mimo panelu ho zatvorí.
        Image blocker = NewImage("Blocker", root.transform, null, new Color(0f, 0f, 0f, 0.45f), Vector2.zero, Vector2.one);
        Button blockerButton = blocker.gameObject.AddComponent<Button>();
        blockerButton.transition = Selectable.Transition.None;
        blockerButton.onClick.AddListener(Hide);

        Vector2 center = new Vector2(0.5f, 0.5f);
        Image panel = NewImage("Panel", root.transform, panelSprite, new Color(0.96f, 0.88f, 0.7f), center, center);
        ((RectTransform)panel.transform).sizeDelta = PanelSize;

        titleText = NewText("Title", panel.transform, 40f, TextColor, new Vector2(0.25f, 0.87f), new Vector2(0.75f, 0.97f));

        infoText = NewText(
            "Info", panel.transform, 34f, TextColor,
            new Vector2(0.12f, 0.65f),
            new Vector2(0.88f, 0.82f)
        );

        reasonText = NewText(
            "Reason", panel.transform, 26f, WarningColor,
            new Vector2(0.12f, 0.55f),
            new Vector2(0.88f, 0.63f)
        );

        Image buttonImage = NewImage(
            "UpgradeButton", panel.transform, buttonSprite,
            new Color(0.3f, 0.7f, 0.2f),
            new Vector2(0.18f, 0.43f),
            new Vector2(0.82f, 0.54f)
        );

        upgradeButton = buttonImage.gameObject.AddComponent<Button>();
        upgradeButton.targetGraphic = buttonImage;
        upgradeButton.onClick.AddListener(Upgrade);
        // V tlačidle: "Upgrade", cena a obrázok mince.
        buttonText = NewText("Label", buttonImage.transform, 36f, Color.white, new Vector2(0.05f, 0.1f), new Vector2(0.55f, 0.9f));
        buttonText.fontStyle = FontStyles.Bold;
        costText = NewText("Cost", buttonImage.transform, 36f, Color.white, new Vector2(0.55f, 0.1f), new Vector2(0.78f, 0.9f));
        costText.fontStyle = FontStyles.Bold;
        coinImage = NewImage("Coin", buttonImage.transform, coinSprite, Color.white, new Vector2(0.78f, 0.15f), new Vector2(0.95f, 0.85f));
        coinImage.preserveAspect = true;
        coinImage.raycastTarget = false;


        // BRYN-72: Feedback for the boost action.
        boostReasonText = NewText(
            "BoostReason", panel.transform, 24f, WarningColor,
            new Vector2(0.10f, 0.34f),
            new Vector2(0.90f, 0.41f)
        );

        // Second button, using the same sprite as Upgrade.
        Image boostImage = NewImage(
            "BoostButton", panel.transform, buttonSprite,
            new Color(0.2f, 0.6f, 0.85f),
            new Vector2(0.18f, 0.22f),
            new Vector2(0.82f, 0.33f)
        );

        boostButton = boostImage.gameObject.AddComponent<Button>();
        boostButton.targetGraphic = boostImage;
        boostButton.onClick.AddListener(Boost);

        boostButtonText = NewText(
            "BoostLabel", boostImage.transform, 32f,
            Color.white, Vector2.zero, Vector2.one
        );

        boostButtonText.fontStyle = FontStyles.Bold;

        Image staffImage = NewImage("StaffButton", panel.transform, buttonSprite,
            new Color(0.2f, 0.6f, 0.5f), new Vector2(0.18f, 0.07f), new Vector2(0.82f, 0.18f));
        staffButton = staffImage.gameObject.AddComponent<Button>();
        staffButton.targetGraphic = staffImage;
        staffButton.onClick.AddListener(ShowStaff);
        staffButtonText = NewText("Label", staffImage.transform, 32f, Color.white,
            new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f));


        Vector2 topRight = Vector2.one;
        Image closeImage = NewImage("Close", panel.transform, closeSprite, new Color(0.85f, 0.2f, 0.15f), topRight, topRight);
        RectTransform closeRect = (RectTransform)closeImage.transform;
        closeRect.sizeDelta = new Vector2(80f, 80f);
        closeRect.anchoredPosition = new Vector2(-25f, -25f);
        Button closeButton = closeImage.gameObject.AddComponent<Button>();
        closeButton.targetGraphic = closeImage;
        closeButton.onClick.AddListener(Hide);
        if (closeSprite == null)
        {
            NewText("Label", closeImage.transform, 40f, Color.white, Vector2.zero, Vector2.one).text = "X";
        }
    }

    // Prázdny RectTransform natiahnutý medzi dve kotvy (v zlomkoch rodiča).
    private static RectTransform NewRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static Image NewImage(string name, Transform parent, Sprite sprite, Color fallbackColor, Vector2 anchorMin, Vector2 anchorMax)
    {
        Image image = NewRect(name, parent, anchorMin, anchorMax).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = sprite != null ? Color.white : fallbackColor;
        return image;
    }

    private static TMP_Text NewText(string name, Transform parent, float size, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        TextMeshProUGUI text = NewRect(name, parent, anchorMin, anchorMax).gameObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.enableAutoSizing = true;
        text.fontSizeMin = size * 0.5f;
        text.fontSizeMax = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }
}
