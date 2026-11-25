using TMPro;
using UnityEngine;
using UnityEngine.UI;

// BRYN-26, BRYN-27: Panel na odomknutie budovy (Pool, Bungalows). Otvorí sa ťuknutím na stavenisko (BuildingView).
// Pred míľnikom ukáže, po ktorej vlne sa budova odomkne; po ňom tlačidlo "Odomknúť" s cenou.
// Odomknutie odpočíta mince, nastaví isActive = true a uloží hru. Budovu zobrazí BuildingView sám.
// Panel sa vytvorí sám pri prvom použití (do scény ani do prefabov sa nič nepridáva), UI sa vytvára v kóde.
public class BuildingUnlockPanel : MonoBehaviour
{
    private static BuildingUnlockPanel instance;

    // Medzi BuildingUpgradePanel (100) a SettingsPanel (200).
    private const int SortingOrder = 150;

    // Vzhľad na ladenie (referenčné rozlíšenie 1920x1080), rovnaký ako pri BuildingUpgradePanel.
    private static readonly Vector2 PanelSize = new Vector2(520f, 663f);
    private static readonly Color TextColor = new Color(0.35f, 0.2f, 0.08f);
    private static readonly Color WarningColor = new Color(0.75f, 0.15f, 0.1f);

    private GameObject root;
    private TMP_Text titleText;
    private TMP_Text infoText;
    private TMP_Text reasonText;
    private TMP_Text buttonText;
    private TMP_Text costText;
    private Image coinImage;
    private Button unlockButton;
    private BuildingState building;

    // Otvorí panel pre danú budovu. Pre hotovú budovu alebo budovu, ktorá sa neodomyká, nerobí nič.
    public static void Show(BuildingState state)
    {
        if (state == null || state.isActive || BuildingState.UnlockCost(state.type) < 0)
        {
            return;
        }

        if (instance == null)
        {
            instance = new GameObject("BuildingUnlockPanel").AddComponent<BuildingUnlockPanel>();
        }

        instance.building = state;
        instance.root.SetActive(true);
        instance.Refresh();
    }

    private void Awake()
    {
        BuildUi();
        Hide();
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void Hide()
    {
        building = null;
        root.SetActive(false);
    }

    // Kým je panel otvorený, sleduje mince a vlnu (vlna sa môže spustiť alebo skončiť, mince pribúdať).
    private void Update()
    {
        if (building != null)
        {
            Refresh();
        }
    }

    private void Refresh()
    {
        GameState state = GameManager.Instance != null ? GameManager.Instance.CurrentState : null;
        if (state == null || building.isActive)
        {
            Hide();
            return;
        }

        int requiredWaves = BuildingState.UnlockWave(building.type);
        int cost = BuildingState.UnlockCost(building.type);
        bool reached = state.wavesCompleted >= requiredWaves;

        titleText.text = DisplayName(building.type);
        unlockButton.gameObject.SetActive(reached);

        if (!reached)
        {
            infoText.text = $"Odomkne sa po {requiredWaves}. vlne (dokončené {state.wavesCompleted}/{requiredWaves})";
            reasonText.text = "";
            return;
        }

        infoText.text = $"Dosiahnutý míľnik: {requiredWaves}. vlna";
        buttonText.text = "Odomknúť";
        costText.text = TopBarHud.Format(cost);
        coinImage.gameObject.SetActive(coinImage.sprite != null);

        bool canUnlock = CanUnlock(out string reason);
        reasonText.text = reason;
        unlockButton.interactable = canUnlock;
    }

    // Odomknúť sa dá po dosiahnutí míľnika, keď nebeží vlna a je dosť mincí.
    private bool CanUnlock(out string reason)
    {
        reason = "";
        GameState state = GameManager.Instance != null ? GameManager.Instance.CurrentState : null;
        if (state == null || building == null || building.isActive)
        {
            return false;
        }

        int cost = BuildingState.UnlockCost(building.type);
        int requiredWaves = BuildingState.UnlockWave(building.type);
        if (cost < 0 || state.wavesCompleted < requiredWaves)
        {
            return false;
        }

        if (WaveManager.Instance != null && WaveManager.Instance.IsWaveRunning)
        {
            reason = "Počas vlny sa budovy nedajú odomykať.";
            return false;
        }

        int missing = cost - state.coins;
        if (missing > 0)
        {
            reason = $"Nedostatok mincí, chýba {TopBarHud.Format(missing)}.";
            return false;
        }

        return true;
    }

    private void Unlock()
    {
        if (!CanUnlock(out _))
        {
            return;
        }

        GameManager.Instance.CurrentState.coins -= BuildingState.UnlockCost(building.type);
        GameManager.Instance.CurrentState.ActivateBuilding(building);
        GameManager.Instance.SaveGame();
        Hide();
    }

    private static string DisplayName(BuildingType type)
    {
        return type switch
        {
            BuildingType.Pool => "Bazén",
            BuildingType.Bungalows => "Bungalovy",
            _ => type.ToString()
        };
    }

    // --- Vytvorenie UI v kóde (rovnako ako BuildingUpgradePanel) ---

    private void BuildUi()
    {
        // Sprity berieme z upgrade panelu, ak existuje; inak sa použijú farby.
        BuildingUpgradePanel sprites = BuildingUpgradePanel.Instance;
        Sprite panelSprite = sprites != null ? sprites.PanelSprite : null;
        Sprite buttonSprite = sprites != null ? sprites.ButtonSprite : null;
        Sprite closeSprite = sprites != null ? sprites.CloseSprite : null;
        Sprite coinSprite = sprites != null ? sprites.CoinSprite : null;

        root = new GameObject("UnlockCanvas");
        root.transform.SetParent(transform, false);

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;
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
        infoText = NewText("Info", panel.transform, 34f, TextColor, new Vector2(0.12f, 0.5f), new Vector2(0.88f, 0.8f));
        reasonText = NewText("Reason", panel.transform, 28f, WarningColor, new Vector2(0.12f, 0.31f), new Vector2(0.88f, 0.48f));

        Image buttonImage = NewImage("UnlockButton", panel.transform, buttonSprite, new Color(0.3f, 0.7f, 0.2f),
            new Vector2(0.08f, 0.1f), new Vector2(0.92f, 0.26f));
        unlockButton = buttonImage.gameObject.AddComponent<Button>();
        unlockButton.targetGraphic = buttonImage;
        unlockButton.onClick.AddListener(Unlock);
        // V tlačidle: "Odomknúť", cena a obrázok mince.
        // Text aj cena sú na jednom riadku (NoWrap), keď sa nevojdú, autosize ich zmenší. Cena "1 000" potrebuje viac miesta ako "500".
        // Tlačidlo je široké 0.84 panelu (437 px): "Odomknúť" 0.04-0.50, medzera 0.50-0.54, cena 0.54-0.80, minca 0.82-0.96.
        buttonText = NewText("Label", buttonImage.transform, 36f, Color.white, new Vector2(0.04f, 0.1f), new Vector2(0.50f, 0.9f));
        buttonText.fontStyle = FontStyles.Bold;
        buttonText.textWrappingMode = TextWrappingModes.NoWrap;
        buttonText.fontSizeMin = 36f * 0.8f; // "Odomknúť" sa zmenší najviac na 80 %
        costText = NewText("Cost", buttonImage.transform, 36f, Color.white, new Vector2(0.54f, 0.1f), new Vector2(0.80f, 0.9f));
        costText.fontStyle = FontStyles.Bold;
        costText.textWrappingMode = TextWrappingModes.NoWrap;
        coinImage = NewImage("Coin", buttonImage.transform, coinSprite, Color.white, new Vector2(0.82f, 0.15f), new Vector2(0.96f, 0.85f));
        coinImage.preserveAspect = true;
        coinImage.raycastTarget = false;

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
