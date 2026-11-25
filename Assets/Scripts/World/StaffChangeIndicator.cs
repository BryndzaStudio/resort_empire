using System.Collections;
using TMPro;
using UnityEngine;

// BRYN-37: Hráč vidí, kde personál ubudol a kde pribudol. Keď sa budove zmení obsluha
// (GameState.IsStaffed), nad odznakom s pracovníkom sa objaví "+1 personál" (zelený)
// alebo "-1 personál" (červený) na tmavej podložke, vystúpi nahor a do 1 s zmizne.
// BRYN-91: zmena sa musí zobraziť do 100 ms. Text je vytvorený vopred a pri zmene sa len zapne,
// a to v tom istom frame, v ktorom ju BuildingView zistí (kontroluje ju každý frame).
// Komponent si pridáva BuildingView sám, do scény ani do prefabu ho netreba dávať.
// Nezáleží na tom, ako sa personál zmenil (presun personálu, nový pracovník, kláves W v teste).
// BRYN-40: keď sa po odomknutí budovy objaví nový člen personálu, text je "Nový personál!".
public class StaffChangeIndicator : MonoBehaviour
{
    // BRYN-91: limit na zobrazenie zmeny.
    public const float MaxShowDelayMs = 100f;

    // Vzhľad a animácia (world jednotky, nezávislé od scale budovy).
    private const float FontSize = 7f;
    private const float OutlineWidth = 0.3f;
    private const float Duration = 1f;          // ako dlho je text vidieť (s)
    private const float RiseDistance = 1.2f;    // o koľko vystúpi nahor
    private const float FadeStart = 0.6f;       // od akej časti trvania začne miznúť
    private static readonly Vector2 BackgroundPadding = new Vector2(0.5f, 0.3f);
    private static readonly Color AddedColor = new Color(0.3f, 0.9f, 0.35f, 1f);
    private static readonly Color RemovedColor = new Color(1f, 0.35f, 0.3f, 1f);
    // Tmavá podložka ako pri odznaku s kapacitou, aby text nesplýval s trávou.
    private static readonly Color BackgroundColor = new Color(0.05f, 0.08f, 0.12f, 0.85f);

    // Podložka v tvare kapsuly, vytvorí sa v kóde len raz.
    private const int PillWidth = 256;
    private const int PillHeight = 80;
    private static Sprite pillSprite;

    private TextMeshPro text;
    private SpriteRenderer background;
    private Vector3 startPosition;
    private Color color;
    private float timer = -1f;

    // Volá BuildingView pri vytváraní odznakov. compensation vyrovná scale budovy.
    public void Init(Vector3 compensation, int sortingOrder)
    {
        GameObject textObject = new GameObject("StaffChange");
        textObject.transform.SetParent(transform, false);
        textObject.transform.localScale = compensation;

        text = textObject.AddComponent<TextMeshPro>();
        text.fontSize = FontSize;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.outlineWidth = OutlineWidth;
        text.outlineColor = Color.black;
        text.sortingOrder = sortingOrder + 1;

        // Podložka je dieťa textu, takže sa hýbe, zväčšuje a mizne s ním.
        GameObject backgroundObject = new GameObject("Background");
        backgroundObject.transform.SetParent(textObject.transform, false);
        background = backgroundObject.AddComponent<SpriteRenderer>();
        background.sprite = GetPillSprite();
        background.sortingOrder = sortingOrder;

        textObject.SetActive(false);
    }

    // added = true: personál pribudol, false: ubudol. position = kde sa text objaví (vo svete).
    // label = vlastný text (napr. BRYN-40 "Nový personál!"); bez neho "+1 personál" / "-1 personál".
    public void Show(bool added, Vector3 position, string label = null)
    {
        if (text == null)
        {
            return;
        }

        float changedAt = Time.realtimeSinceStartup;

        text.text = label ?? (added ? "+1 personál" : "-1 personál");
        color = added ? AddedColor : RemovedColor;
        text.color = color;
        startPosition = position;
        text.transform.position = position;
        timer = 0f;
        text.gameObject.SetActive(true);
        FitBackground();

        StartCoroutine(MeasureShowDelay(changedAt, added));
    }

    private void Update()
    {
        if (timer < 0f)
        {
            return;
        }

        timer += Time.deltaTime;
        float t = timer / Duration;
        if (t >= 1f)
        {
            timer = -1f;
            text.gameObject.SetActive(false);
            return;
        }

        text.transform.position = startPosition + Vector3.up * (RiseDistance * t);

        float alpha = t < FadeStart ? 1f : 1f - (t - FadeStart) / (1f - FadeStart);
        text.color = new Color(color.r, color.g, color.b, alpha);
        background.color = new Color(BackgroundColor.r, BackgroundColor.g, BackgroundColor.b, BackgroundColor.a * alpha);
    }

    // Podložka sa prispôsobí veľkosti textu (+ okraj).
    private void FitBackground()
    {
        text.ForceMeshUpdate();
        Vector2 size = (Vector2)text.textBounds.size + BackgroundPadding * 2f;
        Vector2 spriteSize = background.sprite.bounds.size;
        background.transform.localPosition = text.textBounds.center;
        background.transform.localScale = new Vector3(size.x / spriteSize.x, size.y / spriteSize.y, 1f);
        background.color = BackgroundColor;
    }

    // Biela kapsula (obdĺžnik s polkruhovými koncami) s vyhladenou hranou.
    private static Sprite GetPillSprite()
    {
        if (pillSprite != null)
        {
            return pillSprite;
        }

        float radius = PillHeight / 2f;
        Texture2D texture = new Texture2D(PillWidth, PillHeight, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < PillHeight; y++)
        {
            for (int x = 0; x < PillWidth; x++)
            {
                // Vzdialenosť od úsečky v strede kapsuly; hrana je vo vzdialenosti radius.
                float cx = Mathf.Clamp(x + 0.5f, radius, PillWidth - radius);
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, radius));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(radius - distance)));
            }
        }
        texture.Apply();

        pillSprite = Sprite.Create(texture, new Rect(0, 0, PillWidth, PillHeight), new Vector2(0.5f, 0.5f), PillHeight);
        return pillSprite;
    }

    // BRYN-91: počkáme do konca frame (text je vykreslený) a zmeriame čas od zistenia zmeny.
    // BuildingView kontroluje personál každý frame, takže zmena sa zistí najneskôr o 1 frame
    // (pri 60 FPS asi 17 ms) – aj to je v limite 100 ms.
    private IEnumerator MeasureShowDelay(float changedAt, bool added)
    {
        yield return new WaitForEndOfFrame();

        float delayMs = (Time.realtimeSinceStartup - changedAt) * 1000f;
        string change = added ? "+1" : "-1";
        if (delayMs <= MaxShowDelayMs)
        {
            Debug.Log($"BRYN-91: {name} {change} personál zobrazený za {delayMs:0} ms (limit {MaxShowDelayMs:0} ms).");
        }
        else
        {
            Debug.LogWarning($"BRYN-91: {name} {change} personál sa zobrazil až za {delayMs:0} ms, limit je {MaxShowDelayMs:0} ms.");
        }
    }
}
