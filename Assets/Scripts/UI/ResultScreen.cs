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
// BRYN-80: hodnotenie vlny 1 – 5 hviezdičiek a priemerná spokojnosť hostí (BRYN-81).
// BRYN-83: časť "Čo vyšlo" – budovy, ktoré neboli bottleneck. BRYN-84: časť "Čo treba vylepšiť" – bottlenecky.
public class ResultScreen : MonoBehaviour
{
    // BRYN-93: limit na zobrazenie výsledku po skončení vlny.
    public const float MaxShowDelayMs = 100f;

    [Header("Voliteľný vzhľad")]
    [Tooltip("Obrázok panelu. Ak je prázdne, panel má jednoduchú farbu.")]
    [SerializeField] private Sprite panelSprite;
    [Tooltip("Obrázok tlačidla. Ak je prázdne, použije sa obrázok tlačidla Start Wave.")]
    [SerializeField] private Sprite buttonSprite;
    [Tooltip("BRYN-80: plná hviezdička (icon_star_full). Ak je prázdne, nakreslí sa zlatá hviezdička v kóde.")]
    [SerializeField] private Sprite starFullSprite;
    [Tooltip("BRYN-80: prázdna hviezdička (icon_star_empty). Ak je prázdne, nakreslí sa sivá hviezdička v kóde.")]
    [SerializeField] private Sprite starEmptySprite;

    // Rozloženie panelu (referenčné rozlíšenie 1920x1080).
    private static readonly Vector2 PanelSize = new Vector2(1100f, 860f);
    private const float StarSize = 90f;
    private const float StarSpacing = 105f;
    private static readonly Color StarFullColor = new Color(1f, 0.78f, 0.15f, 1f);
    private static readonly Color StarEmptyColor = new Color(0.45f, 0.4f, 0.35f, 0.45f);
    private static readonly Color GoodColor = new Color(0.12f, 0.5f, 0.2f, 1f);
    private static readonly Color BadColor = new Color(0.75f, 0.2f, 0.1f, 1f);

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
    private TextMeshProUGUI satisfactionText;
    private TextMeshProUGUI detailsText;
    private TextMeshProUGUI goodText;
    private TextMeshProUGUI badText;
    private readonly Image[] starImages = new Image[5];
    private Button continueButton;

    // Hviezdička nakreslená v kóde (keď nie sú priradené sprity), vytvorí sa len raz.
    private static Sprite generatedStar;

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

        // BRYN-80: hviezdičky.
        for (int i = 0; i < starImages.Length; i++)
        {
            ShowStar(starImages[i], i < result.stars);
        }

        // BRYN-81: priemerná spokojnosť hostí.
        satisfactionText.text = result.guestsRated > 0
            ? $"Spokojnosť hostí: {Mathf.RoundToInt(result.satisfaction)} %"
            : "Spokojnosť hostí: –";

        detailsText.text =
            $"Hostia: {result.guestsArrived}     " +
            $"Zarobené mince: {result.coinsEarned:+0;-0;0}     " +
            $"Trvanie: {Mathf.RoundToInt(result.durationSeconds)} s";

        // BRYN-83/84: budovy bez bottlenecku a s bottleneckom.
        goodText.text = BuildingList(result, false);
        badText.text = BuildingList(result, true);

        screen.transform.SetAsLastSibling(); // navrch nad ostatné UI
        screen.SetActive(true);

        StartCoroutine(MeasureShowDelay(result.finishedAtRealtime));
    }

    // BRYN-83 (bottleneck = false) / BRYN-84 (bottleneck = true): zoznam budov s priemerným čakaním.
    private static string BuildingList(WaveResult result, bool bottleneck)
    {
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        foreach (BuildingWaveResult building in result.buildings)
        {
            if (building.isBottleneck != bottleneck)
            {
                continue;
            }

            string name = WaveRating.DisplayName(building.type);
            if (building.guests == 0)
            {
                text.AppendLine($"• {name} – bez hostí");
            }
            else
            {
                text.AppendLine($"• {name} – čakanie {Mathf.RoundToInt(building.averageWaitSeconds)} s");
            }
        }

        if (text.Length == 0)
        {
            return bottleneck ? "Nič, všetko fungovalo plynulo." : "Žiadna budova – všade sa čakalo dlho.";
        }

        return text.ToString().TrimEnd();
    }

    private void ShowStar(Image image, bool full)
    {
        Sprite sprite = full ? starFullSprite : starEmptySprite;
        if (sprite != null)
        {
            image.sprite = sprite;
            image.color = Color.white;
        }
        else
        {
            image.sprite = GetGeneratedStar();
            image.color = full ? StarFullColor : StarEmptyColor;
        }
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
        panelRect.sizeDelta = PanelSize;
        Image panelImage = panel.AddComponent<Image>();
        ApplyLook(panelImage, panelSprite, PanelColor);

        titleText = CreateText("Title", panel.transform, 64f, FontStyles.Bold);
        Place(titleText.rectTransform, new Vector2(0f, 350f), new Vector2(1000f, 90f));

        // BRYN-80: 5 hviezdičiek v rade.
        for (int i = 0; i < starImages.Length; i++)
        {
            GameObject star = CreateUiObject("Star" + (i + 1), panel.transform);
            float x = (i - (starImages.Length - 1) / 2f) * StarSpacing;
            Place(star.GetComponent<RectTransform>(), new Vector2(x, 255f), new Vector2(StarSize, StarSize));
            starImages[i] = star.AddComponent<Image>();
            starImages[i].preserveAspect = true;
            starImages[i].raycastTarget = false;
        }

        satisfactionText = CreateText("Satisfaction", panel.transform, 42f, FontStyles.Bold);
        Place(satisfactionText.rectTransform, new Vector2(0f, 170f), new Vector2(1000f, 60f));

        detailsText = CreateText("Details", panel.transform, 32f, FontStyles.Normal);
        Place(detailsText.rectTransform, new Vector2(0f, 115f), new Vector2(1000f, 50f));

        // BRYN-83/84: dva stĺpce.
        BuildSection(panel.transform, "Čo vyšlo", GoodColor, -255f, out goodText);
        BuildSection(panel.transform, "Čo treba vylepšiť", BadColor, 255f, out badText);

        BuildContinueButton(panel.transform);
    }

    // Tlačidlo Pokračovať v rovnakom dizajne ako Start Wave.
    private void BuildContinueButton(Transform parent)
    {
        Button startWave = FindStartWaveButton();
        Image startWaveImage = startWave != null ? startWave.GetComponent<Image>() : null;
        Sprite sprite = buttonSprite != null ? buttonSprite : (startWaveImage != null ? startWaveImage.sprite : null);

        GameObject buttonObject = CreateUiObject("ContinueButton", parent);
        Place(buttonObject.GetComponent<RectTransform>(), new Vector2(0f, -330f), ButtonSize);

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

    // BRYN-83/84: nadpis stĺpca a zoznam budov pod ním.
    private static void BuildSection(Transform parent, string title, Color color, float x, out TextMeshProUGUI list)
    {
        TextMeshProUGUI header = CreateText(title, parent, 38f, FontStyles.Bold);
        header.text = title;
        header.color = color;
        header.alignment = TextAlignmentOptions.Left;
        Place(header.rectTransform, new Vector2(x, 45f), new Vector2(470f, 55f));

        list = CreateText(title + "List", parent, 30f, FontStyles.Normal);
        list.alignment = TextAlignmentOptions.TopLeft;
        list.lineSpacing = 10f;
        list.enableAutoSizing = true;
        list.fontSizeMin = 20f;
        list.fontSizeMax = 30f;
        Place(list.rectTransform, new Vector2(x, -95f), new Vector2(470f, 220f));
    }

    // BRYN-80: biela 5-cípa hviezdička (farbí sa cez Image.color). Kreslí sa v kóde, ako kruh v BuildingView.
    private static Sprite GetGeneratedStar()
    {
        if (generatedStar != null)
        {
            return generatedStar;
        }

        const int size = 128;
        const int samples = 4; // vyhladenie hrán: 4x4 vzoriek na pixel
        Vector2[] points = new Vector2[10];
        float outer = size * 0.48f;
        float inner = outer * 0.45f;
        for (int i = 0; i < points.Length; i++)
        {
            float angle = (90f + i * 36f) * Mathf.Deg2Rad;
            float radius = i % 2 == 0 ? outer : inner;
            points[i] = new Vector2(size / 2f + radius * Mathf.Cos(angle), size / 2f + radius * Mathf.Sin(angle));
        }

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int inside = 0;
                for (int sy = 0; sy < samples; sy++)
                {
                    for (int sx = 0; sx < samples; sx++)
                    {
                        Vector2 p = new Vector2(x + (sx + 0.5f) / samples, y + (sy + 0.5f) / samples);
                        if (IsInside(points, p))
                        {
                            inside++;
                        }
                    }
                }

                texture.SetPixel(x, y, new Color(1f, 1f, 1f, inside / (float)(samples * samples)));
            }
        }
        texture.Apply();

        generatedStar = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return generatedStar;
    }

    // Je bod vnútri mnohouholníka? (párny/nepárny počet prekročení hrany)
    private static bool IsInside(Vector2[] polygon, Vector2 point)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            if ((polygon[i].y > point.y) != (polygon[j].y > point.y) &&
                point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
            {
                inside = !inside;
            }
        }

        return inside;
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
