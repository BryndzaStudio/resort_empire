using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// BRYN-54: Po skončení vlny sa zobrazí obrazovka s výsledkom (počet hostí, zarobené mince, trvanie).
// BRYN-93: Prechod na výsledok musí byť do 100 ms po skončení vlny. Preto sa celá obrazovka
// vytvorí vopred (v Awake) a pri skončení vlny sa len zapne – nič sa nenačítava ani nevytvára.
// Skript patrí na UIRoot (Canvas). Obrazovku si vytvorí sám v kóde, netreba nič skladať v editore.
// Tlačidlo Pokračovať obrazovku zavrie a vyvolá WaveEvents.ResultScreenClosed (WaveManager potom odomkne Start Wave).
// Tlačidlo Pokračovať vyzerá rovnako ako Start Wave: obrázok a farby pri kliknutí si zoberie z neho.
public class ResultScreen : MonoBehaviour
{
    // BRYN-93: limit na zobrazenie výsledku po skončení vlny.
    public const float MaxShowDelayMs = 100f;

    [Header("Voliteľný vzhľad")]
    [Tooltip("Obrázok panelu. Ak je prázdne, panel má jednoduchú farbu.")]
    [SerializeField] private Sprite panelSprite;
    [Tooltip("Obrázok tlačidla. Ak je prázdne, použije sa obrázok tlačidla Start Wave.")]
    [SerializeField] private Sprite buttonSprite;

    // Rovnaké rozmery ako tlačidlo Start Wave (StartWaveButton.prefab).
    private static readonly Vector2 ButtonSize = new Vector2(520f, 140f);
    // Text je posunutý doprava, lebo vľavo na obrázku je ikona vlny (ako pri Start Wave).
    private static readonly Vector2 LabelAnchorMin = new Vector2(0.22f, 0.15f);
    private static readonly Vector2 LabelAnchorMax = new Vector2(0.94f, 0.85f);

    private static readonly Color OverlayColor = new Color(0f, 0f, 0f, 0.6f);
    private static readonly Color PanelColor = new Color(0.98f, 0.93f, 0.80f, 1f);
    private static readonly Color ButtonColor = new Color(0.16f, 0.62f, 0.56f, 1f);
    private static readonly Color DarkText = new Color(0.25f, 0.16f, 0.08f, 1f);

    private GameObject screen;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI detailsText;
    private Button continueButton;

    public bool IsShown => screen != null && screen.activeSelf;

    private void Awake()
    {
        BuildScreen();
        screen.SetActive(false);
    }

    private void OnEnable()
    {
        WaveEvents.WaveFinished += Show;
        continueButton.onClick.AddListener(Close);
    }

    private void OnDisable()
    {
        WaveEvents.WaveFinished -= Show;
        continueButton.onClick.RemoveListener(Close);
    }

    // Volá sa hneď v momente skončenia vlny (v tom istom frame).
    private void Show(WaveResult result)
    {
        titleText.text = "Vlna skončila!";
        detailsText.text =
            $"Hostia: {result.guestsArrived}\n" +
            $"Zarobené mince: {result.coinsEarned:+0;-0;0}\n" +
            $"Trvanie: {Mathf.RoundToInt(result.durationSeconds)} s";

        screen.transform.SetAsLastSibling(); // navrch nad ostatné UI
        screen.SetActive(true);

        StartCoroutine(MeasureShowDelay(result.finishedAtRealtime));
    }

    private void Close()
    {
        screen.SetActive(false);
        WaveEvents.RaiseResultScreenClosed();
    }

    // BRYN-93: počkáme do konca frame (obrazovka je vykreslená) a zmeriame čas od skončenia vlny.
    private IEnumerator MeasureShowDelay(float finishedAtRealtime)
    {
        yield return new WaitForEndOfFrame();

        float delayMs = (Time.realtimeSinceStartup - finishedAtRealtime) * 1000f;
        if (delayMs <= MaxShowDelayMs)
        {
            Debug.Log($"Výsledok vlny zobrazený za {delayMs:0} ms (limit {MaxShowDelayMs:0} ms).");
        }
        else
        {
            Debug.LogWarning($"Výsledok vlny sa zobrazil až za {delayMs:0} ms, limit je {MaxShowDelayMs:0} ms (BRYN-93).");
        }
    }

    // ---- Vytvorenie obrazovky v kóde ----
    // Rozmery sú v jednotkách Canvasu (UIRoot má referenčné rozlíšenie 1920x1080).

    private void BuildScreen()
    {
        // Tmavé pozadie cez celú obrazovku. Zachytáva kliky, aby sa počas výsledku nedalo klikať pod ním.
        screen = CreateUiObject("ResultScreen", transform);
        Stretch(screen.GetComponent<RectTransform>());
        Image overlay = screen.AddComponent<Image>();
        overlay.color = OverlayColor;

        // Panel v strede.
        GameObject panel = CreateUiObject("Panel", screen.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(760f, 620f);
        Image panelImage = panel.AddComponent<Image>();
        ApplyLook(panelImage, panelSprite, PanelColor);

        titleText = CreateText("Title", panel.transform, 72f, FontStyles.Bold);
        Place(titleText.rectTransform, new Vector2(0f, 200f), new Vector2(680f, 110f));

        detailsText = CreateText("Details", panel.transform, 46f, FontStyles.Normal);
        detailsText.lineSpacing = 20f;
        Place(detailsText.rectTransform, new Vector2(0f, 10f), new Vector2(680f, 240f));

        BuildContinueButton(panel.transform);
    }

    // Tlačidlo Pokračovať v rovnakom dizajne ako Start Wave.
    private void BuildContinueButton(Transform parent)
    {
        Button startWave = FindStartWaveButton();
        Image startWaveImage = startWave != null ? startWave.GetComponent<Image>() : null;
        Sprite sprite = buttonSprite != null ? buttonSprite : (startWaveImage != null ? startWaveImage.sprite : null);

        GameObject buttonObject = CreateUiObject("ContinueButton", parent);
        Place(buttonObject.GetComponent<RectTransform>(), new Vector2(0f, -210f), ButtonSize);

        Image buttonImage = buttonObject.AddComponent<Image>();
        ApplyLook(buttonImage, sprite, ButtonColor);
        buttonImage.preserveAspect = true;

        continueButton = buttonObject.AddComponent<Button>();
        continueButton.targetGraphic = buttonImage;
        if (startWave != null)
        {
            continueButton.transition = startWave.transition;
            continueButton.colors = startWave.colors; // stmavnutie pri prejdení a kliknutí
        }

        TextMeshProUGUI label = CreateText("Label", buttonObject.transform, 36f, FontStyles.Bold);
        label.text = "Pokračovať";
        label.color = Color.white;
        label.enableAutoSizing = true;
        label.fontSizeMin = 20f;
        label.fontSizeMax = 60f;

        RectTransform labelRect = label.rectTransform;
        bool hasIcon = sprite != null && buttonSprite == null; // obrázok Start Wave má vľavo ikonu
        labelRect.anchorMin = hasIcon ? LabelAnchorMin : new Vector2(0.05f, 0.15f);
        labelRect.anchorMax = hasIcon ? LabelAnchorMax : new Vector2(0.95f, 0.85f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
    }

    // Tlačidlo Start Wave je v scéne pod UIRoot (StartWaveButton.prefab).
    private static Button FindStartWaveButton()
    {
        StartWaveButton startWave = FindAnyObjectByType<StartWaveButton>(FindObjectsInactive.Include);
        return startWave != null ? startWave.GetComponent<Button>() : null;
    }

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        go.transform.SetParent(parent, false);
        return go;
    }

    private static TextMeshProUGUI CreateText(string objectName, Transform parent, float size, FontStyles style)
    {
        TextMeshProUGUI text = CreateUiObject(objectName, parent).AddComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.color = DarkText;
        text.raycastTarget = false;
        return text;
    }

    private static void ApplyLook(Image image, Sprite sprite, Color fallbackColor)
    {
        if (sprite != null)
        {
            image.sprite = sprite;
            image.color = Color.white;
        }
        else
        {
            image.color = fallbackColor;
        }
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // Umiestni prvok vzhľadom na stred rodiča.
    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
