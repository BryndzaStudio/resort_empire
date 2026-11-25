using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// BRYN-33/34/36: select a source or destination from the unlocked buildings.
// Follows the existing runtime-built building panels; no scene wiring is required.
public class StaffAllocationPanel : MonoBehaviour
{
    private static StaffAllocationPanel instance;
    private GameObject root;
    private TMP_Text title;
    private TMP_Text instruction;
    private TMP_Text status;
    private BuildingState selected;
    private readonly List<Button> buttons = new List<Button>();
    private readonly List<TMP_Text> labels = new List<TMP_Text>();
    private readonly List<BuildingState> candidates = new List<BuildingState>();
    private Sprite buttonSprite;
    private static readonly Color TextColor = new Color(0.35f, 0.2f, 0.08f);

    public static void Show(BuildingState building)
    {
        if (building == null || !building.isActive) return;
        if (instance == null)
        {
            instance = new GameObject("StaffAllocationPanel").AddComponent<StaffAllocationPanel>();
        }
        instance.selected = building;
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
        if (instance == this) instance = null;
    }

    private void Hide()
    {
        selected = null;
        root.SetActive(false);
    }

    private void Update()
    {
        if (selected != null) Refresh();
    }

    private void Refresh()
    {
        GameManager manager = GameManager.Instance;
        if (manager == null || manager.CurrentState == null || !selected.isActive)
        {
            Hide();
            return;
        }

        GameState state = manager.CurrentState;
        bool sending = state.IsStaffed(selected);
        title.text = "Personál: " + DisplayName(selected.type);
        instruction.text = sending ? "Vyber budovu, kam pracovníka presunúť." : "Vyber budovu, odkiaľ pracovníka presunúť.";
        status.text = manager.CanTransferStaff ? "Presun je zadarmo a okamžitý." : "Počas pauzy nie je možné presúvať personál.";
        candidates.Clear();
        foreach (BuildingState building in state.buildings)
        {
            if (building != null && building.isActive) candidates.Add(building);
        }
        for (int i = 0; i < buttons.Count; i++)
        {
            bool visible = i < candidates.Count;
            buttons[i].gameObject.SetActive(visible);
            if (!visible) continue;
            BuildingState candidate = candidates[i];
            labels[i].text = DisplayName(candidate.type) + (state.IsStaffed(candidate) ? "   1/1" : "   0/1");
            BuildingState source = sending ? selected : candidate;
            BuildingState target = sending ? candidate : selected;
            buttons[i].interactable = state.CanTransferWorker(source, target, !manager.CanTransferStaff);
        }
    }

    private void Transfer(int index)
    {
        if (selected == null || index >= candidates.Count || GameManager.Instance == null) return;
        GameManager manager = GameManager.Instance;
        bool sending = manager.CurrentState.IsStaffed(selected);
        BuildingState source = sending ? selected : candidates[index];
        BuildingState target = sending ? candidates[index] : selected;
        if (manager.TryTransferStaff(source, target)) Hide();
        else Refresh();
    }

    private void BuildUi()
    {
        BuildingUpgradePanel style = BuildingUpgradePanel.Instance;
        buttonSprite = style != null ? style.ButtonSprite : null;
        root = new GameObject("StaffCanvas", typeof(RectTransform));
        root.transform.SetParent(transform, false);
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;
        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>();

        Image blocker = Image("Blocker", root.transform, null, new Color(0, 0, 0, 0.45f), Vector2.zero, Vector2.one);
        blocker.gameObject.AddComponent<Button>().onClick.AddListener(Hide);
        Image panel = Image("Panel", root.transform, style != null ? style.PanelSprite : null,
            new Color(0.96f, 0.88f, 0.7f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        panel.rectTransform.sizeDelta = new Vector2(720, 800);
        title = Text("Title", panel.transform, 36, new Vector2(0.1f, 0.87f), new Vector2(0.82f, 0.97f));
        instruction = Text("Instruction", panel.transform, 28, new Vector2(0.08f, 0.77f), new Vector2(0.92f, 0.86f));
        status = Text("Status", panel.transform, 26, new Vector2(0.08f, 0.035f), new Vector2(0.92f, 0.12f));

        for (int i = 0; i < System.Enum.GetValues(typeof(BuildingType)).Length; i++)
        {
            float top = 0.75f - i * 0.12f;
            Image row = Image("Building_" + i, panel.transform, buttonSprite, new Color(0.2f, 0.6f, 0.5f),
                new Vector2(0.12f, top - 0.10f), new Vector2(0.88f, top));
            Button button = row.gameObject.AddComponent<Button>();
            int index = i;
            button.onClick.AddListener(() => Transfer(index));
            buttons.Add(button);
            TMP_Text label = Text("Label", row.transform, 30, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.95f));
            label.color = Color.white;
            labels.Add(label);
        }
        Image close = Image("Close", panel.transform, style != null ? style.CloseSprite : null,
            new Color(0.85f, 0.2f, 0.15f), Vector2.one, Vector2.one);
        close.rectTransform.sizeDelta = new Vector2(70, 70);
        close.rectTransform.anchoredPosition = new Vector2(-30, -30);
        close.gameObject.AddComponent<Button>().onClick.AddListener(Hide);
        if (close.sprite == null) Text("X", close.transform, 30, Vector2.zero, Vector2.one).text = "X";
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static Image Image(string name, Transform parent, Sprite sprite, Color color, Vector2 min, Vector2 max)
    {
        Image image = Rect(name, parent, min, max).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = sprite != null ? Color.white : color;
        return image;
    }

    private static TMP_Text Text(string name, Transform parent, float size, Vector2 min, Vector2 max)
    {
        TMP_Text text = Rect(name, parent, min, max).gameObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.enableAutoSizing = true;
        text.fontSizeMin = size * 0.65f;
        text.fontSizeMax = size;
        text.color = TextColor;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    private static string DisplayName(BuildingType type)
    {
        return type switch
        {
            BuildingType.Restaurant => "Reštaurácia",
            BuildingType.BeachBar => "Beach bar",
            BuildingType.Reception => "Recepcia",
            BuildingType.Pool => "Bazén",
            BuildingType.Bungalows => "Bungalovy",
            _ => type.ToString()
        };
    }
}
